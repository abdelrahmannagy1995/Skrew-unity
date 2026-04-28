using System;
using System.Collections.Generic;
using System.Linq;
using ScrewGame.Core;
using ScrewGame.Entities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ScrewGame.AI
{
    // =========================================================================
    // AI Memory System with Intentional Forgetting
    // =========================================================================

    /// <summary>
    /// Simulates the AI's knowledge of card positions across all grids.
    /// Implements "intentional forgetting" to mimic human cognitive constraints:
    ///   - After a blind swap the swapped card's value is forgotten.
    ///   - Memory decays probabilistically each turn based on difficulty.
    /// </summary>
    public class AiMemoryMatrix
    {
        // memory[playerId][gridIndex] = known card value, or null if unknown
        private readonly Dictionary<string, CardData?[]> _memory = new();

        // Probability that memory is retained each turn (1.0 = perfect, 0.0 = instant forget)
        private float _retentionRate;

        public AiMemoryMatrix(IEnumerable<string> playerIds, int gridSize, float retentionRate)
        {
            _retentionRate = Mathf.Clamp01(retentionRate);
            foreach (var pid in playerIds)
                _memory[pid] = new CardData?[gridSize];
        }

        // -----------------------------------------------------------------------
        // Write operations
        // -----------------------------------------------------------------------

        /// <summary>Record a revealed card.</summary>
        public void Learn(string playerId, int index, CardData card)
        {
            if (_memory.TryGetValue(playerId, out var grid))
                grid[index] = card;
        }

        /// <summary>Forget a card after a blind swap.</summary>
        public void ForgetAfterBlindSwap(string playerId, int index)
        {
            if (_memory.TryGetValue(playerId, out var grid))
                grid[index] = null;
        }

        // -----------------------------------------------------------------------
        // Decay tick (called once per turn)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Apply probabilistic memory decay to all known cards.
        /// Cards are forgotten with probability (1 - retentionRate).
        /// </summary>
        public void ApplyDecay()
        {
            foreach (var grid in _memory.Values)
            {
                for (int i = 0; i < grid.Length; i++)
                {
                    if (grid[i] != null && Random.value > _retentionRate)
                        grid[i] = null;
                }
            }
        }

        // -----------------------------------------------------------------------
        // Read operations
        // -----------------------------------------------------------------------

        public CardData? GetKnownCard(string playerId, int index)
        {
            if (_memory.TryGetValue(playerId, out var grid) && index < grid.Length)
                return grid[index];
            return null;
        }

        public int KnownCount(string playerId)
        {
            if (!_memory.TryGetValue(playerId, out var grid)) return 0;
            return grid.Count(c => c != null);
        }

        public float EstimateScore(string playerId, float unknownCardMean)
        {
            if (!_memory.TryGetValue(playerId, out var grid)) return float.MaxValue;
            float total = 0f;
            foreach (var card in grid)
                total += card.HasValue ? card.Value.Value : unknownCardMean;
            return total;
        }

        public void UpdateRetentionRate(float rate)
        {
            _retentionRate = Mathf.Clamp01(rate);
        }
    }

    // =========================================================================
    // MCTS Node
    // =========================================================================

    /// <summary>Represents a node in the Monte Carlo Tree Search.</summary>
    internal class MctsNode
    {
        public AiGameState State { get; }
        public MctsNode Parent { get; }
        public AiAction ActionFromParent { get; }

        public List<MctsNode> Children { get; } = new();
        public int Visits { get; private set; }
        public float TotalReward { get; private set; }

        public MctsNode(AiGameState state, MctsNode parent = null, AiAction action = null)
        {
            State            = state;
            Parent           = parent;
            ActionFromParent = action;
        }

        public float Ucb1(float explorationConstant = 1.414f)
        {
            if (Visits == 0) return float.MaxValue;
            float exploitation = TotalReward / Visits;
            float exploration  = explorationConstant * Mathf.Sqrt(Mathf.Log(Parent?.Visits ?? 1) / Visits);
            return exploitation + exploration;
        }

        public void Update(float reward)
        {
            Visits++;
            TotalReward += reward;
        }

        public bool IsFullyExpanded(List<AiAction> legalActions)
            => Children.Count >= legalActions.Count;
    }

    // =========================================================================
    // AI Action types
    // =========================================================================

    public enum AiActionType
    {
        DrawFromStock,
        SnatchDiscard,
        SwapDrawnCard,
        DiscardDrawnCard,
        DeclareScrew,
    }

    public class AiAction
    {
        public AiActionType ActionType;
        public int GridIndex;   // which grid slot to swap into / discard from
        public string TargetPlayerId;
    }

    // =========================================================================
    // Lightweight game state for MCTS simulations
    // =========================================================================

    public class AiGameState
    {
        public string[] PlayerIds;
        public float[]  EstimatedScores;    // one per player
        public float    DiscardTopValue;
        public int      DrawPileCount;
        public bool     ScrewDeclared;
        public string   ScrewCallerId;
        public int[]    HandSizes;          // cards remaining per player

        public AiGameState Clone()
        {
            return new AiGameState
            {
                PlayerIds      = (string[])PlayerIds.Clone(),
                EstimatedScores = (float[])EstimatedScores.Clone(),
                DiscardTopValue = DiscardTopValue,
                DrawPileCount   = DrawPileCount,
                ScrewDeclared   = ScrewDeclared,
                ScrewCallerId   = ScrewCallerId,
                HandSizes       = (int[])HandSizes.Clone(),
            };
        }
    }

    // =========================================================================
    // MCTS AI Agent
    // =========================================================================

    /// <summary>
    /// Monte Carlo Tree Search AI with determinization for imperfect information.
    /// Implements intentional memory decay and Dynamic Difficulty Adjustment (DDA).
    /// </summary>
    public class ScrewAiAgent
    {
        // -----------------------------------------------------------------------
        // Configuration
        // -----------------------------------------------------------------------

        private const int BaseMctsIterations = 200;
        private const float UnknownCardMean  = 4.5f; // average point value assumption

        private readonly string     _aiPlayerId;
        private readonly AiMemoryMatrix _memory;

        // DDA parameters (adjusted at runtime)
        private float _difficultyLevel;   // 0.0 = easy, 1.0 = expert
        private int   _mctsIterations;

        // -----------------------------------------------------------------------
        // Constructor
        // -----------------------------------------------------------------------

        public ScrewAiAgent(string aiPlayerId, IEnumerable<string> allPlayerIds, int gridSize, float initialDifficulty = 0.5f)
        {
            _aiPlayerId       = aiPlayerId;
            _difficultyLevel  = Mathf.Clamp01(initialDifficulty);
            _mctsIterations   = Mathf.RoundToInt(BaseMctsIterations * _difficultyLevel);

            float retention = Mathf.Lerp(0.4f, 1.0f, _difficultyLevel);
            _memory = new AiMemoryMatrix(allPlayerIds, gridSize, retention);
        }

        // -----------------------------------------------------------------------
        // DDA – adjust difficulty in response to human performance
        // -----------------------------------------------------------------------

        /// <summary>
        /// Call after each round with the human's score relative to the AI.
        /// Positive delta means human is winning easily → increase difficulty.
        /// </summary>
        public void AdjustDifficulty(float humanScoreDelta)
        {
            float adjustment = humanScoreDelta > 0 ? 0.1f : -0.1f;
            _difficultyLevel = Mathf.Clamp01(_difficultyLevel + adjustment);
            _mctsIterations  = Mathf.Max(50, Mathf.RoundToInt(BaseMctsIterations * _difficultyLevel));
            float retention  = Mathf.Lerp(0.4f, 1.0f, _difficultyLevel);
            _memory.UpdateRetentionRate(retention);

            Debug.Log($"[AI] DDA updated – difficulty={_difficultyLevel:F2}, iterations={_mctsIterations}, retention={retention:F2}");
        }

        // -----------------------------------------------------------------------
        // Memory update
        // -----------------------------------------------------------------------

        public void OnCardRevealed(string playerId, int index, CardData card)
            => _memory.Learn(playerId, index, card);

        public void OnBlindSwapOccurred(string playerId, int index)
            => _memory.ForgetAfterBlindSwap(playerId, index);

        public void OnTurnAdvanced()
            => _memory.ApplyDecay();

        // -----------------------------------------------------------------------
        // Decision – choose best action via MCTS
        // -----------------------------------------------------------------------

        public AiAction ChooseAction(AiGameState currentState, List<AiAction> legalActions)
        {
            if (legalActions.Count == 0)
                return null;

            // Low difficulty: pick randomly or use simple heuristic
            if (_difficultyLevel < 0.2f)
                return legalActions[Random.Range(0, legalActions.Count)];

            // High difficulty: run full MCTS
            var root = new MctsNode(currentState.Clone());

            for (int i = 0; i < _mctsIterations; i++)
            {
                var node   = Select(root, legalActions);
                var child  = Expand(node, legalActions);
                float reward = Simulate(child.State, child.ActionFromParent);
                Backpropagate(child, reward);
            }

            // Pick child with most visits (most robust action)
            MctsNode bestChild = null;
            int maxVisits = -1;
            foreach (var child in root.Children)
            {
                if (child.Visits > maxVisits)
                {
                    maxVisits = child.Visits;
                    bestChild = child;
                }
            }

            return bestChild?.ActionFromParent ?? legalActions[0];
        }

        // -----------------------------------------------------------------------
        // MCTS phases
        // -----------------------------------------------------------------------

        private MctsNode Select(MctsNode node, List<AiAction> legalActions)
        {
            while (node.Children.Count > 0 && node.IsFullyExpanded(legalActions))
            {
                MctsNode best = null;
                float bestUcb = float.MinValue;
                foreach (var child in node.Children)
                {
                    float ucb = child.Ucb1();
                    if (ucb > bestUcb) { bestUcb = ucb; best = child; }
                }
                node = best;
            }
            return node;
        }

        private MctsNode Expand(MctsNode node, List<AiAction> legalActions)
        {
            var triedActions = new HashSet<AiActionType>(
                node.Children.Select(c => c.ActionFromParent.ActionType));

            var untried = legalActions.Where(a => !triedActions.Contains(a.ActionType)).ToList();
            if (untried.Count == 0) return node;

            var action   = untried[Random.Range(0, untried.Count)];
            var newState = ApplyAction(node.State.Clone(), action);
            var child    = new MctsNode(newState, node, action);
            node.Children.Add(child);
            return child;
        }

        private float Simulate(AiGameState state, AiAction firstAction)
        {
            // Roll out a random game and return a reward inversely proportional to AI score
            var sim = state.Clone();
            int maxDepth = 20;

            for (int depth = 0; depth < maxDepth && !sim.ScrewDeclared; depth++)
            {
                // Random action during rollout
                var actions = GetLegalActions(sim);
                if (actions.Count == 0) break;
                var action = actions[Random.Range(0, actions.Count)];
                sim = ApplyAction(sim, action);
            }

            int aiIndex = Array.IndexOf(sim.PlayerIds, _aiPlayerId);
            float aiScore = aiIndex >= 0 ? sim.EstimatedScores[aiIndex] : float.MaxValue;

            // Higher reward = lower score (inversion)
            return Mathf.Max(0f, 50f - aiScore);
        }

        private void Backpropagate(MctsNode node, float reward)
        {
            while (node != null)
            {
                node.Update(reward);
                node = node.Parent;
            }
        }

        // -----------------------------------------------------------------------
        // Simulation helpers
        // -----------------------------------------------------------------------

        private AiGameState ApplyAction(AiGameState state, AiAction action)
        {
            var next = state.Clone();
            int aiIdx = Array.IndexOf(next.PlayerIds, _aiPlayerId);
            if (aiIdx < 0) return next;

            switch (action.ActionType)
            {
                case AiActionType.DrawFromStock:
                    // Unknown card drawn; assume mean value
                    next.DrawPileCount = Mathf.Max(0, next.DrawPileCount - 1);
                    break;

                case AiActionType.SnatchDiscard:
                    // Replace highest-known card with discard top value
                    next.EstimatedScores[aiIdx] -= UnknownCardMean;
                    next.EstimatedScores[aiIdx] += next.DiscardTopValue;
                    break;

                case AiActionType.DeclareScrew:
                    next.ScrewDeclared = true;
                    next.ScrewCallerId = _aiPlayerId;
                    break;
            }

            return next;
        }

        private List<AiAction> GetLegalActions(AiGameState state)
        {
            var actions = new List<AiAction>
            {
                new AiAction { ActionType = AiActionType.DrawFromStock },
                new AiAction { ActionType = AiActionType.SnatchDiscard },
            };

            // Consider declaring Screw if estimated score is low enough
            int aiIdx = Array.IndexOf(state.PlayerIds, _aiPlayerId);
            if (aiIdx >= 0 && state.EstimatedScores[aiIdx] <= 5f)
                actions.Add(new AiAction { ActionType = AiActionType.DeclareScrew });

            return actions;
        }
    }
}
