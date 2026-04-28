using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using ScrewGame.AI;
using ScrewGame.Core;
using ScrewGame.StateMachines;
using UnityEngine;

namespace ScrewGame.Diagnostics
{
    /// <summary>
    /// Drop-in MonoBehaviour that exercises the major subsystems and reports PASS / FAIL
    /// for each. Designed to run on the Bootstrap scene immediately after
    /// <see cref="SupabaseManager"/> finishes initialising.
    ///
    /// Tests:
    ///   1. Supabase client initialises (config resolution + Realtime handshake).
    ///   2. Auth round-trip (sign-up if necessary, then sign-in).
    ///   3. <see cref="DeckManager"/> builds + shuffles a deterministic-but-varied deck.
    ///   4. <see cref="GameStateMachine"/> transitions across phases.
    ///   5. AI agent picks a legal action from a synthetic state.
    /// </summary>
    [DisallowMultipleComponent]
    public class SmokeTest : MonoBehaviour
    {
        [Header("Run options")]
        [Tooltip("Runs all enabled tests automatically on Start().")]
        [SerializeField] private bool _runOnStart = true;

        [Tooltip("Maximum seconds to wait for SupabaseManager.IsInitialised before failing test 1.")]
        [SerializeField] private float _initTimeoutSeconds = 15f;

        [Header("Auth test (test #2)")]
        [Tooltip("Email used for sign-up / sign-in. Leave blank to skip auth test.")]
        [SerializeField] private string _testEmail = string.Empty;
        [SerializeField] private string _testPassword = string.Empty;

        [Header("Per-test toggles")]
        [SerializeField] private bool _testSupabaseInit = true;
        [SerializeField] private bool _testAuth         = true;
        [SerializeField] private bool _testDeck         = true;
        [SerializeField] private bool _testStateMachine = true;
        [SerializeField] private bool _testAi           = true;

        private readonly List<string> _results = new List<string>();

        private void Start()
        {
            if (_runOnStart)
                RunAllAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        public async UniTaskVoid RunAllAsync(CancellationToken ct)
        {
            _results.Clear();
            Debug.Log("=== [SmokeTest] starting ===");

            if (_testSupabaseInit) await Run("Supabase init", () => TestSupabaseInitAsync(ct));
            if (_testAuth)         await Run("Auth round-trip", () => TestAuthAsync(ct));
            if (_testDeck)         await Run("DeckManager",       () => TestDeckAsync());
            if (_testStateMachine) await Run("State machine",     () => TestStateMachineAsync());
            if (_testAi)           await Run("AI agent",          () => TestAiAsync());

            Debug.Log("=== [SmokeTest] summary ===\n" + string.Join("\n", _results));
        }

        private async UniTask Run(string label, Func<UniTask<string>> body)
        {
            try
            {
                var detail = await body();
                var line   = $"  PASS  {label}{(string.IsNullOrEmpty(detail) ? string.Empty : " — " + detail)}";
                Debug.Log("[SmokeTest] " + line);
                _results.Add(line);
            }
            catch (Exception ex)
            {
                var line = $"  FAIL  {label} — {ex.GetType().Name}: {ex.Message}";
                Debug.LogError("[SmokeTest] " + line);
                _results.Add(line);
            }
        }

        // -----------------------------------------------------------------
        // Test 1 — Supabase init
        // -----------------------------------------------------------------
        private async UniTask<string> TestSupabaseInitAsync(CancellationToken ct)
        {
            if (SupabaseManager.Instance == null)
                throw new InvalidOperationException("SupabaseManager.Instance is null — drop one in the Bootstrap scene.");

            var deadline = Time.realtimeSinceStartup + _initTimeoutSeconds;
            while (!SupabaseManager.Instance.IsInitialised)
            {
                if (Time.realtimeSinceStartup > deadline)
                    throw new TimeoutException($"SupabaseManager did not initialise within {_initTimeoutSeconds:F0}s.");
                await UniTask.Delay(100, cancellationToken: ct);
            }

            return "client ready";
        }

        // -----------------------------------------------------------------
        // Test 2 — Auth
        // -----------------------------------------------------------------
        private async UniTask<string> TestAuthAsync(CancellationToken ct)
        {
            if (string.IsNullOrEmpty(_testEmail) || string.IsNullOrEmpty(_testPassword))
                return "skipped (no credentials)";

            var manager = SupabaseManager.Instance ??
                          throw new InvalidOperationException("SupabaseManager.Instance is null.");

            var signedIn = await manager.SignInAsync(_testEmail, _testPassword, ct);
            if (!signedIn)
            {
                // First run: try to create the account, then sign in.
                var signedUp = await manager.SignUpAsync(_testEmail, _testPassword, ct);
                if (!signedUp)
                    throw new InvalidOperationException("Sign-up failed (see previous error).");

                signedIn = await manager.SignInAsync(_testEmail, _testPassword, ct);
                if (!signedIn)
                    throw new InvalidOperationException("Sign-in still failed after sign-up.");
            }

            var userId = manager.CurrentUserId;
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("CurrentUserId empty after sign-in.");

            return $"userId={userId}";
        }

        // -----------------------------------------------------------------
        // Test 3 — Deck
        // -----------------------------------------------------------------
        private UniTask<string> TestDeckAsync()
        {
            var deck = DeckManager.BuildDeck(GameMode.General);
            if (deck == null || deck.Count == 0)
                throw new InvalidOperationException("DeckManager.BuildDeck returned an empty deck.");

            // Trivial Fisher–Yates to verify the order can be shuffled in place.
            var rng     = new System.Random(42);
            var shuffled = deck.OrderBy(_ => rng.Next()).ToList();
            var movedIdx = 0;
            for (var i = 0; i < deck.Count; i++)
            {
                if (!ReferenceEquals(deck[i], shuffled[i]))
                    movedIdx++;
            }

            return UniTask.FromResult($"{deck.Count} cards, {movedIdx} moved by shuffle");
        }

        // -----------------------------------------------------------------
        // Test 4 — State machine
        // -----------------------------------------------------------------
        private UniTask<string> TestStateMachineAsync()
        {
            var sm = FindStateMachine();
            sm.TransitionTo(new InfoPhaseState(new Dictionary<string, object> { { "duration", 0.05f } }));
            sm.TransitionTo(new PlayerTurnState(new Dictionary<string, object>(), localPlayerId: "smoke-local"));
            sm.TransitionTo(new ResolveActionState(new Dictionary<string, object>()));
            sm.TransitionTo(new MatchEndState(new Dictionary<string, object>()));
            return UniTask.FromResult("Info → Turn → Resolve → End");
        }

        private static GameStateMachine FindStateMachine()
        {
#if UNITY_2023_1_OR_NEWER
            var sm = UnityEngine.Object.FindFirstObjectByType<GameStateMachine>();
#else
            var sm = UnityEngine.Object.FindObjectOfType<GameStateMachine>();
#endif
            if (sm == null)
                throw new InvalidOperationException("No GameStateMachine in the scene.");
            return sm;
        }

        // -----------------------------------------------------------------
        // Test 5 — AI agent picks a legal action
        // -----------------------------------------------------------------
        private UniTask<string> TestAiAsync()
        {
            const string botId   = "smoke-bot";
            var          players = new[] { botId, "smoke-human" };

            var agent = new ScrewAiAgent(botId, players, gridSize: 4, initialDifficulty: 0.5f);

            var state = new AiGameState
            {
                PlayerIds       = players,
                EstimatedScores = new[] { 12f, 18f },
                DiscardTopValue = 5f,
                DrawPileCount   = 30,
                ScrewDeclared   = false,
                ScrewCallerId   = null,
                HandSizes       = new[] { 4, 4 },
            };

            var legalActions = new List<AiAction>
            {
                new AiAction { ActionType = AiActionType.DrawFromStock },
                new AiAction { ActionType = AiActionType.SnatchDiscard, GridIndex = 0 },
                new AiAction { ActionType = AiActionType.DeclareScrew },
            };

            var pick = agent.ChooseAction(state, legalActions);
            if (pick == null)
                throw new InvalidOperationException("ScrewAiAgent.ChooseAction returned null.");

            return UniTask.FromResult($"chose {pick.ActionType}");
        }
    }
}
