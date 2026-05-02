using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ScrewGame.Core;
using ScrewGame.Entities;
using ScrewGame.Localization;

namespace ScrewGame.UI
{
    /// <summary>
    /// Bootstraps the Gameplay scene:
    ///   1. Starts the LocalGameSimulator with the mode chosen on the Menu.
    ///   2. Wires Draw / Snatch / Screw buttons.
    ///   3. Initialises CardObject visuals for all dealt cards.
    ///   4. Enables CardClickHandler on each slot once a card has been drawn.
    ///   5. Handles Play Again / Back to Menu from ScoreboardUI.
    /// </summary>
    public class GameplayInputController : MonoBehaviour
    {
        public static GameplayInputController Instance { get; private set; }

        // ─── Inspector ─────────────────────────────────────────────────────────
        [Header("Turn Control Buttons")]
        [SerializeField] private Button _btnDraw;
        [SerializeField] private Button _btnSnatch;
        [SerializeField] private Button _btnScrew;

        [Header("Score / End Screen")]
        [SerializeField] private Button _btnPlayAgain;
        [SerializeField] private Button _btnBackToMenu;

        [Header("Card slots (local player) — 4 CardObjects in order 0-3")]
        [SerializeField] private CardObject[] _localCardObjects = new CardObject[4];

        // ─── Private ───────────────────────────────────────────────────────────
        private CardClickHandler[] _handlers = new CardClickHandler[4];
        private LocalGameSimulator _sim;
#pragma warning disable 414
        private bool _waitingForSlotPick;
#pragma warning restore 414

        // ─── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            AutoWireButtons();
            AutoWireCardObjects();

            _sim = LocalGameSimulator.Instance;
            if (_sim == null)
            {
                // Simulator must exist in scene; create one if missing
                var go = new GameObject("LocalGameSimulator");
                _sim = go.AddComponent<LocalGameSimulator>();
            }

            // Subscribe
            _sim.OnTurnChanged  += OnTurnChanged;
            _sim.OnCardDealt    += OnCardDealt;
            _sim.OnMessage      += OnGameMessage;
            _sim.OnGameEnded    += OnGameEnded;

            // Wire buttons
            _btnDraw   ?.onClick.AddListener(OnDrawPressed);
            _btnSnatch ?.onClick.AddListener(OnSnatchPressed);
            _btnScrew  ?.onClick.AddListener(OnScrewPressed);
            _btnPlayAgain  ?.onClick.AddListener(() => SceneManager.LoadScene(2));
            _btnBackToMenu ?.onClick.AddListener(() => SceneManager.LoadScene(1));

            // Wire card click handlers
            for (int i = 0; i < _localCardObjects.Length; i++)
            {
                if (_localCardObjects[i] == null) continue;
                var h = _localCardObjects[i].GetComponent<CardClickHandler>()
                        ?? _localCardObjects[i].gameObject.AddComponent<CardClickHandler>();
                // Set grid index via reflection-free approach: serialise it
                var so = new System.Reflection.FieldInfo[]{ };
                typeof(CardClickHandler)
                    .GetField("_gridIndex",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(h, i);
                _handlers[i] = h;
            }

            // Start the game
            _sim.Configure(MenuController.ChosenMode, 2); // 2 players: human + 1 AI
            _sim.StartGame();

            TurnControlPanel.Instance?.SetInteractable(false);
        }

        private void OnDestroy()
        {
            if (_sim == null) return;
            _sim.OnTurnChanged -= OnTurnChanged;
            _sim.OnCardDealt   -= OnCardDealt;
            _sim.OnMessage     -= OnGameMessage;
            _sim.OnGameEnded   -= OnGameEnded;
        }

        // ─── Button handlers ──────────────────────────────────────────────────
        private void OnDrawPressed()
        {
            if (_sim.TryDraw())
                EnterSlotPickMode("msg_drew_card");
        }

        private void OnSnatchPressed()
        {
            if (_sim.TrySnatch())
                EnterSlotPickMode("msg_snatched");
        }

        private void OnScrewPressed()
        {
            _sim.TryDeclareScrew();
        }

        // ─── Slot pick mode ───────────────────────────────────────────────────
        private void EnterSlotPickMode(string msgKey)
        {
            _waitingForSlotPick = true;
            HUDController.Instance?.ShowMessage(
                LocalizationManager.T("msg_select_your_card"));

            // Enable highlight on all 4 card slots
            foreach (var h in _handlers)
                h?.SetSelectable(true);

            // Also enable Discard button via right-click on any card or reuse Snatch as "Discard"
            // (simplest: hide Draw/Snatch, show a "Discard drawn" button)
            _btnDraw  ?.gameObject.SetActive(false);
            _btnSnatch?.gameObject.SetActive(false);
        }

        public void ClearCardHighlights()
        {
            _waitingForSlotPick = false;
            foreach (var h in _handlers) h?.SetSelectable(false);
            _btnDraw  ?.gameObject.SetActive(true);
            _btnSnatch?.gameObject.SetActive(true);
        }

        // ─── Simulator events ─────────────────────────────────────────────────
        private void OnTurnChanged(int seat)
        {
            ClearCardHighlights();
            bool local = seat == 0;
            TurnControlPanel.Instance?.SetInteractable(local);
            if (local)
                HUDController.Instance?.ShowMessage(LocalizationManager.T("msg_your_turn"));
            else
                HUDController.Instance?.ShowMessage(LocalizationManager.T("msg_waiting"));
        }

        private void OnCardDealt(int seat, CardData data)
        {
            if (seat != 0) return;  // only handle local player cards here
            // Find which index this is (dealt in order 0-3)
            for (int i = 0; i < _localCardObjects.Length; i++)
            {
                var co = _localCardObjects[i];
                if (co == null) continue;
                if (co.Data != null) continue; // already initialised
                co.Initialise(data, i);
                co.GetComponent<CardVisualController>()?.Refresh();
                co.SetFaceDown();
                break;
            }
        }

        private void OnGameMessage(string msg)
        {
            HUDController.Instance?.ShowMessage(msg);
        }

        private void OnGameEnded(int winnerSeat)
        {
            string winnerId = winnerSeat == 0 ? "You" : "AI";
            // ScoreboardUI already called by simulator; wire Play Again / Menu buttons
            if (_btnPlayAgain  != null) _btnPlayAgain .gameObject.SetActive(true);
            if (_btnBackToMenu != null) _btnBackToMenu.gameObject.SetActive(true);
        }

        // ─── Auto-wire helpers ────────────────────────────────────────────────
        private void AutoWireButtons()
        {
            if (_btnDraw   == null) _btnDraw   = FindBtn("Btn_Draw");
            if (_btnSnatch == null) _btnSnatch = FindBtn("Btn_Snatch");
            if (_btnScrew  == null) _btnScrew  = FindBtn("Btn_Screw");
            if (_btnPlayAgain   == null) _btnPlayAgain   = FindBtn("Btn_PlayAgain");
            if (_btnBackToMenu  == null) _btnBackToMenu  = FindBtn("Btn_BackToMenu");
        }

        private void AutoWireCardObjects()
        {
            if (_localCardObjects[0] != null) return; // already wired in inspector
            // Find PlayerGridSlots and collect 4 CardObjects
            var slotParent = GameObject.Find("PlayerGridSlots");
            if (slotParent == null) return;
            int idx = 0;
            for (int i = 0; i < slotParent.transform.childCount && idx < 4; i++)
            {
                var co = slotParent.transform.GetChild(i).GetComponent<CardObject>();
                if (co != null) _localCardObjects[idx++] = co;
            }
        }

        private static Button FindBtn(string name)
        {
            var go = GameObject.Find(name);
            return go != null ? go.GetComponent<Button>() : null;
        }
    }
}
