using System;
using System.Collections;
using System.Collections.Generic;
using ScrewGame.Entities;
using ScrewGame.UI;
using UnityEngine;

namespace ScrewGame.Core
{
    /// <summary>
    /// Fully self-contained offline game loop.
    /// Deals cards, manages turns, handles Draw / Snatch / Screw locally
    /// without any network calls.  AI opponents use ScrewAiAgent logic.
    /// </summary>
    public class LocalGameSimulator : MonoBehaviour
    {
        public static LocalGameSimulator Instance { get; private set; }

        // ── Configuration ─────────────────────────────────────────────────────
        [Header("Game Settings")]
        [SerializeField] private GameMode _mode = GameMode.General;
        [SerializeField] private int _totalPlayers = 2;   // 2-4

        // ── Public state ──────────────────────────────────────────────────────
        public int         CurrentSeat   { get; private set; }
        public bool        IsLocalTurn   => CurrentSeat == 0;   // seat 0 = human
        public bool        HasDrawnCard  { get; private set; }
        public CardData    DrawnCard     { get; private set; }
        public bool        GameRunning   { get; private set; }

        // ── Internal state ────────────────────────────────────────────────────
        private List<CardData>          _deck;
        private CardData[]              _discardPile  = Array.Empty<CardData>();
        private CardData[][]            _hands;       // [seat][cardIndex]
        private bool[]                  _cardRevealed; // which local cards are revealed to player
        private int                     _screwCalledBy = -1;
        private bool                    _infoPhaseActive;
        private float                   _infoPhaseTimer;
        private const float             InfoPhaseDuration = 8f;

        // Events other controllers subscribe to
        public event Action<int>        OnTurnChanged;         // seat index
        public event Action<int, CardData> OnCardDealt;        // seat, cardData
        public event Action<string>     OnMessage;
        public event Action<int[]>      OnScoresReady;         // scores per seat
        public event Action<int>        OnGameEnded;           // winner seat

        // ─── Unity ───────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ─── Public API ───────────────────────────────────────────────────────

        public void Configure(GameMode mode, int totalPlayers)
        {
            _mode         = mode;
            _totalPlayers = Mathf.Clamp(totalPlayers, 2, 4);
        }

        public void StartGame()
        {
            GameRunning    = true;
            HasDrawnCard   = false;
            DrawnCard      = null;
            _screwCalledBy = -1;
            CurrentSeat    = 0;

            BuildAndDeal();
            StartInfoPhase();
        }

        // ── Player actions ────────────────────────────────────────────────────

        public bool TryDraw()
        {
            if (!IsLocalTurn || HasDrawnCard) return false;
            if (_deck.Count == 0) { ReshuffleDiscard(); }
            if (_deck.Count == 0) return false;

            DrawnCard   = _deck[0]; _deck.RemoveAt(0);
            HasDrawnCard = true;
            OnMessage?.Invoke(ScrewGame.Localization.LocalizationManager.T("msg_drew_card"));
            return true;
        }

        public bool TrySnatch()
        {
            if (!IsLocalTurn || HasDrawnCard) return false;
            if (_discardPile.Length == 0) return false;

            var top = _discardPile[_discardPile.Length - 1];
            var newPile = new CardData[_discardPile.Length - 1];
            Array.Copy(_discardPile, newPile, newPile.Length);
            _discardPile = newPile;

            DrawnCard    = top;
            HasDrawnCard = true;
            OnMessage?.Invoke(ScrewGame.Localization.LocalizationManager.T("msg_snatched"));
            return true;
        }

        /// <summary>Swap the drawn card into slot <paramref name="gridIndex"/>,
        /// discard the old card, and end the local turn.</summary>
        public bool TrySwapIntoSlot(int gridIndex)
        {
            if (!HasDrawnCard || DrawnCard == null) return false;
            if (gridIndex < 0 || gridIndex >= 4) return false;

            CardData old = _hands[0][gridIndex];
            _hands[0][gridIndex] = DrawnCard;
            Discard(old);

            // Activate command card effect if applicable
            if (DrawnCard.CardType == CardType.Command)
                TriggerCommandEffect(DrawnCard, 0, gridIndex);

            DrawnCard    = null;
            HasDrawnCard = false;
            EndLocalTurn();
            return true;
        }

        /// <summary>Discard the drawn card without swapping, then end turn.</summary>
        public bool TryDiscardDrawn()
        {
            if (!HasDrawnCard || DrawnCard == null) return false;

            if (DrawnCard.CardType == CardType.Command)
                TriggerCommandEffect(DrawnCard, 0, -1);

            Discard(DrawnCard);
            DrawnCard    = null;
            HasDrawnCard = false;
            EndLocalTurn();
            return true;
        }

        public bool TryDeclareScrew()
        {
            if (!IsLocalTurn) return false;
            _screwCalledBy = 0;
            OnMessage?.Invoke(ScrewGame.Localization.LocalizationManager.T("msg_screw_called"));
            // One more round for others, then end
            StartCoroutine(EndRoundAfterOneMore());
            return true;
        }

        public CardData GetLocalCard(int index) => _hands[0][index];
        public bool IsCardRevealed(int index) => _cardRevealed != null && index < _cardRevealed.Length && _cardRevealed[index];

        // ── Internal logic ────────────────────────────────────────────────────

        private void BuildAndDeal()
        {
            _deck   = DeckManager.BuildDeck(_mode);
            Shuffle(_deck);
            _hands          = new CardData[_totalPlayers][];
            _cardRevealed   = new bool[4];
            _discardPile    = Array.Empty<CardData>();

            for (int s = 0; s < _totalPlayers; s++)
            {
                _hands[s] = new CardData[4];
                for (int c = 0; c < 4; c++)
                {
                    _hands[s][c] = _deck[0]; _deck.RemoveAt(0);
                    OnCardDealt?.Invoke(s, _hands[s][c]);
                }
            }
        }

        private void StartInfoPhase()
        {
            _infoPhaseActive = true;
            _infoPhaseTimer  = InfoPhaseDuration;
            // Reveal local player's right-hand cards (indices 2, 3)
            _cardRevealed[2] = true;
            _cardRevealed[3] = true;
            PlayerGrid.LocalInstance?.RevealCard(2);
            PlayerGrid.LocalInstance?.RevealCard(3);
            HUDController.Instance?.ShowCountdown(InfoPhaseDuration,
                ScrewGame.Localization.LocalizationManager.T("msg_info_phase"));
            OnMessage?.Invoke(ScrewGame.Localization.LocalizationManager.T("msg_info_phase"));
        }

        private void Update()
        {
            if (!GameRunning) return;
            if (_infoPhaseActive)
            {
                _infoPhaseTimer -= Time.deltaTime;
                HUDController.Instance?.UpdateCountdown(Mathf.CeilToInt(_infoPhaseTimer));
                if (_infoPhaseTimer <= 0f)
                {
                    _infoPhaseActive = false;
                    _cardRevealed[2] = _cardRevealed[3] = false;
                    PlayerGrid.LocalInstance?.HideCard(2);
                    PlayerGrid.LocalInstance?.HideCard(3);
                    HUDController.Instance?.HideCountdown();
                    BeginTurn(0);
                }
            }
        }

        private void BeginTurn(int seat)
        {
            CurrentSeat = seat;
            HasDrawnCard = false;
            DrawnCard    = null;
            OnTurnChanged?.Invoke(seat);
            HUDController.Instance?.HighlightActiveSeat(seat);

            if (seat == 0)
            {
                TurnControlPanel.Instance?.SetInteractable(true);
                OnMessage?.Invoke(ScrewGame.Localization.LocalizationManager.T("msg_your_turn"));
            }
            else
            {
                TurnControlPanel.Instance?.SetInteractable(false);
                OnMessage?.Invoke(ScrewGame.Localization.LocalizationManager.T("msg_waiting"));
                // AI takes its turn after a short delay
                StartCoroutine(RunAiTurn(seat));
            }
        }

        private IEnumerator RunAiTurn(int seat)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.8f, 1.5f));
            if (!GameRunning) yield break;

            // Simple AI: always draw, swap worst card
            if (_deck.Count > 0)
            {
                var drawn = _deck[0]; _deck.RemoveAt(0);
                int worstIdx = WorstCardIndex(seat);
                var old = _hands[seat][worstIdx];
                _hands[seat][worstIdx] = drawn;
                Discard(old);
            }
            EndTurn(seat);
        }

        private void EndLocalTurn()
        {
            TurnControlPanel.Instance?.SetInteractable(false);
            EndTurn(0);
        }

        private void EndTurn(int seat)
        {
            if (_screwCalledBy >= 0) return; // round already ending
            int next = (seat + 1) % _totalPlayers;
            BeginTurn(next);
        }

        private IEnumerator EndRoundAfterOneMore()
        {
            // Let each other player take one more turn
            for (int i = 1; i < _totalPlayers; i++)
            {
                int seat = (0 + i) % _totalPlayers;
                yield return StartCoroutine(RunAiTurn(seat));
                yield return new WaitForSeconds(0.5f);
            }
            EndGame();
        }

        private void EndGame()
        {
            GameRunning = false;
            TurnControlPanel.Instance?.SetInteractable(false);
            AllGridsRevealController.Instance?.RevealAll();

            int[] scores = new int[_totalPlayers];
            for (int s = 0; s < _totalPlayers; s++)
                for (int c = 0; c < 4; c++)
                    scores[s] += _hands[s][c].Value;

            // Double the screw caller's score if they don't have the lowest
            if (_screwCalledBy >= 0)
            {
                int lowestScore = int.MaxValue;
                for (int s = 0; s < _totalPlayers; s++)
                    if (scores[s] < lowestScore) lowestScore = scores[s];
                if (scores[_screwCalledBy] != lowestScore)
                    scores[_screwCalledBy] *= 2;
            }

            int winnerSeat = 0;
            for (int s = 1; s < _totalPlayers; s++)
                if (scores[s] < scores[winnerSeat]) winnerSeat = s;

            OnScoresReady?.Invoke(scores);
            OnGameEnded?.Invoke(winnerSeat);

            string winMsg = winnerSeat == 0
                ? ScrewGame.Localization.LocalizationManager.T("msg_match_won")
                : ScrewGame.Localization.LocalizationManager.T("msg_match_lost");
            OnMessage?.Invoke(winMsg);

            ScoreboardUI.Instance?.Show(scores, winnerSeat == 0 ? "local" : "ai_" + winnerSeat);
        }

        // ── Command card effects ──────────────────────────────────────────────
        private void TriggerCommandEffect(CardData card, int seat, int slotIndex)
        {
            switch (card.CommandId)
            {
                case CommandCardId.PeekSelf:
                    // Reveal a random unrevealed card briefly
                    for (int i = 0; i < 4; i++)
                    {
                        if (!_cardRevealed[i])
                        {
                            _cardRevealed[i] = true;
                            PlayerGrid.LocalInstance?.RevealCard(i);
                            StartCoroutine(HideCardAfter(i, 3f));
                            OnMessage?.Invoke("Peek: card " + i + " = " + _hands[0][i].Value);
                            break;
                        }
                    }
                    break;

                case CommandCardId.Basra:
                    OnMessage?.Invoke(ScrewGame.Localization.LocalizationManager.T("msg_basra_success"));
                    break;

                default:
                    OnMessage?.Invoke("Command: " + card.CommandId);
                    break;
            }
        }

        private IEnumerator HideCardAfter(int index, float delay)
        {
            yield return new WaitForSeconds(delay);
            _cardRevealed[index] = false;
            PlayerGrid.LocalInstance?.HideCard(index);
        }

        // ── Utilities ─────────────────────────────────────────────────────────
        private void Discard(CardData card)
        {
            var newPile = new CardData[_discardPile.Length + 1];
            Array.Copy(_discardPile, newPile, _discardPile.Length);
            newPile[_discardPile.Length] = card;
            _discardPile = newPile;
        }

        private void ReshuffleDiscard()
        {
            if (_discardPile.Length == 0) return;
            _deck.AddRange(_discardPile);
            _discardPile = Array.Empty<CardData>();
            Shuffle(_deck);
        }

        private int WorstCardIndex(int seat)
        {
            int worst = 0;
            for (int i = 1; i < 4; i++)
                if (_hands[seat][i].Value > _hands[seat][worst].Value) worst = i;
            return worst;
        }

        private static void Shuffle(List<CardData> list)
        {
            var rng = new System.Random();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
