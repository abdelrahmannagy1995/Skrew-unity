using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ScrewGame.AI;
using ScrewGame.Core;
using ScrewGame.Entities;
using UnityEngine;

namespace ScrewGame.AI
{
    /// <summary>
    /// Unity MonoBehaviour wrapper for the AI agent.
    /// Drives the <see cref="ScrewAiAgent"/> per game turn and submits actions
    /// via <see cref="GameLoop"/> Edge Function calls.
    /// </summary>
    public class AiPlayerController : MonoBehaviour
    {
        // -----------------------------------------------------------------------
        // Configuration
        // -----------------------------------------------------------------------
        [Header("AI Settings")]
        [SerializeField, Range(0f, 1f)] private float _initialDifficulty = 0.5f;
        [SerializeField]                private string _botPlayerId       = "bot_0";
        [SerializeField]                private int    _thinkDelayMs      = 800; // simulate thinking

        // -----------------------------------------------------------------------
        // State
        // -----------------------------------------------------------------------
        private ScrewAiAgent _agent;
        private AiGameState  _lastKnownState;
        private List<string> _allPlayerIds;

        // -----------------------------------------------------------------------
        // Initialisation
        // -----------------------------------------------------------------------

        public void Initialise(List<string> allPlayerIds, int gridSize)
        {
            _allPlayerIds = allPlayerIds;
            _agent = new ScrewAiAgent(_botPlayerId, allPlayerIds, gridSize, _initialDifficulty);
            Debug.Log($"[AIController] AI '{_botPlayerId}' initialised with difficulty {_initialDifficulty}.");
        }

        // -----------------------------------------------------------------------
        // Called when it's the bot's turn
        // -----------------------------------------------------------------------

        public async UniTaskVoid TakeTurnAsync(AiGameState gameState, CancellationToken ct = default)
        {
            _lastKnownState = gameState;

            // Simulate human-like thinking delay
            await UniTask.Delay(_thinkDelayMs, cancellationToken: ct);

            var legalActions = BuildLegalActions(gameState);
            var chosenAction = _agent.ChooseAction(gameState, legalActions);

            if (chosenAction == null)
            {
                Debug.LogWarning("[AIController] No legal actions available.");
                return;
            }

            await ExecuteActionAsync(chosenAction, ct);

            // Apply memory decay after this turn
            _agent.OnTurnAdvanced();
        }

        // -----------------------------------------------------------------------
        // Build legal actions from current game state
        // -----------------------------------------------------------------------

        private List<AiAction> BuildLegalActions(AiGameState state)
        {
            var actions = new List<AiAction>
            {
                new AiAction { ActionType = AiActionType.DrawFromStock },
                new AiAction { ActionType = AiActionType.SnatchDiscard },
            };

            int botIndex = System.Array.IndexOf(state.PlayerIds, _botPlayerId);
            if (botIndex >= 0 && state.EstimatedScores[botIndex] <= 5f)
                actions.Add(new AiAction { ActionType = AiActionType.DeclareScrew });

            return actions;
        }

        // -----------------------------------------------------------------------
        // Execute the chosen action via GameLoop
        // -----------------------------------------------------------------------

        private async UniTask ExecuteActionAsync(AiAction action, CancellationToken ct)
        {
            switch (action.ActionType)
            {
                case AiActionType.DrawFromStock:
                    await GameLoop.Instance.DrawFromStockAsync(ct);
                    // After drawing, decide whether to swap or discard
                    await DecideAfterDrawAsync(ct);
                    break;

                case AiActionType.SnatchDiscard:
                    await GameLoop.Instance.SnatchDiscardAsync(ct);
                    break;

                case AiActionType.DeclareScrew:
                    await GameLoop.Instance.DeclareScrewAsync(ct);
                    break;
            }
        }

        private async UniTask DecideAfterDrawAsync(CancellationToken ct)
        {
            // Heuristic: swap if drawn card is < estimated mean of worst known card in grid
            // For AI bots we call the swap-drawn-card edge function with best grid index
            int swapIndex = FindWorstKnownGridIndex();
            if (swapIndex >= 0)
                await GameLoop.Instance.SwapDrawnCardAsync(swapIndex, ct);
            else
                await GameLoop.Instance.DiscardDrawnCardAsync(ct);
        }

        private int FindWorstKnownGridIndex()
        {
            // Stub: in full implementation, query AiMemoryMatrix for highest known card
            return Random.Range(0, 4);
        }

        // -----------------------------------------------------------------------
        // DDA callback – called by GameManager after each round
        // -----------------------------------------------------------------------

        public void NotifyRoundResult(float humanScoreDelta)
        {
            _agent.AdjustDifficulty(humanScoreDelta);
        }

        // -----------------------------------------------------------------------
        // Memory update callbacks
        // -----------------------------------------------------------------------

        public void NotifyCardRevealed(string playerId, int index, CardData card)
            => _agent.OnCardRevealed(playerId, index, card);

        public void NotifyBlindSwap(string playerId, int index)
            => _agent.OnBlindSwapOccurred(playerId, index);
    }
}
