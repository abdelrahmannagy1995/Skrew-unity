using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using ScrewGame.Entities;
using ScrewGame.StateMachines;
using UnityEngine;

namespace ScrewGame.Core
{
    /// <summary>
    /// Central game-loop controller. Bridges the Supabase Realtime broadcast events
    /// to the client-side state machine. Single entry-point for all match-scoped logic.
    /// </summary>
    public partial class GameLoop : MonoBehaviour
    {
        // -----------------------------------------------------------------------
        // Inspector references
        // -----------------------------------------------------------------------
        [Header("Dependencies")]
        [SerializeField] private GameStateMachine _stateMachine;

        // -----------------------------------------------------------------------
        // State
        // -----------------------------------------------------------------------
        public static GameLoop Instance { get; private set; }

        private string _matchId;
        private string _localPlayerId;
        private Supabase.Realtime.RealtimeChannel _matchChannel;

        // -----------------------------------------------------------------------
        // Unity lifecycle
        // -----------------------------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            LeaveChannelAsync(destroyCancellationToken).Forget();
        }

        // -----------------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------------

        /// <summary>Call this after the match lobby is confirmed.</summary>
        public async UniTask JoinMatchAsync(string matchId, CancellationToken ct = default)
        {
            _matchId      = matchId;
            _localPlayerId = SupabaseManager.Instance.CurrentUserId;

            await SubscribeToMatchChannelAsync(ct);
        }

        // -----------------------------------------------------------------------
        // Realtime channel subscription
        // -----------------------------------------------------------------------
        private async UniTask SubscribeToMatchChannelAsync(CancellationToken ct)
        {
            var client = SupabaseManager.Instance.Client;
            _matchChannel = client.Channel($"match:{_matchId}");

            // Receive broadcast events
            _matchChannel.OnBroadcast += OnBroadcastReceived;

            // Presence: track connected players
            _matchChannel.OnPresenceSync += OnPresenceSync;

            await _matchChannel.Subscribe();

            // Announce presence
            await _matchChannel.Track(new Dictionary<string, object>
            {
                { "user_id",   _localPlayerId },
                { "status",    "connected" },
                { "joined_at", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
            });

            Debug.Log($"[GameLoop] Subscribed to match channel: {_matchId}");
        }

        private async UniTaskVoid LeaveChannelAsync(CancellationToken ct)
        {
            if (_matchChannel != null)
            {
                await _matchChannel.Unsubscribe();
            }
        }

        // -----------------------------------------------------------------------
        // Broadcast event dispatcher
        // -----------------------------------------------------------------------
        private void OnBroadcastReceived(object sender, Supabase.Realtime.Broadcast.BaseBroadcast e)
        {
            var payload = e.Payload<Dictionary<string, object>>();
            var eventName = payload?.ContainsKey("event") == true ? payload["event"]?.ToString() : null;

            if (string.IsNullOrEmpty(eventName)) return;

            switch (eventName)
            {
                case "info_phase_start":
                    _stateMachine.TransitionTo(new InfoPhaseState(payload));
                    break;

                case "turn_changed":
                    _stateMachine.TransitionTo(new PlayerTurnState(payload, _localPlayerId));
                    break;

                case "basra_success":
                    _stateMachine.OnBasraSuccess(payload);
                    break;

                case "basra_failed":
                    _stateMachine.OnBasraFailed(payload);
                    break;

                case "obligatory_screw":
                    _stateMachine.TransitionTo(new MatchEndState(payload));
                    break;

                case "match_completed":
                    _stateMachine.TransitionTo(new MatchEndState(payload));
                    break;

                case "thief_guess_result":
                    _stateMachine.OnThiefGuessResult(payload);
                    break;

                case "ping_pong_skip":
                    _stateMachine.OnPingPongSkip(payload);
                    break;

                default:
                    Debug.Log($"[GameLoop] Unhandled broadcast event: {eventName}");
                    break;
            }
        }

        // -----------------------------------------------------------------------
        // Presence
        // -----------------------------------------------------------------------
        private void OnPresenceSync(object sender, EventArgs e)
        {
            var presences = _matchChannel.Presences;
            Debug.Log($"[GameLoop] Presence sync – {presences?.Count ?? 0} players connected.");
            // Notify UI to update player connection indicators
            GameEvents.OnPresenceUpdated?.Invoke(presences);
        }

        // -----------------------------------------------------------------------
        // Shared Edge Function caller (used by GameLoopActions.cs partial methods)
        // -----------------------------------------------------------------------
        internal async UniTask<bool> CallEdgeFunctionAsync(string functionName, object body, CancellationToken ct)
        {
            try
            {
                var client = SupabaseManager.Instance.Client;
                var json   = JsonConvert.SerializeObject(body);
                var result = await client.Functions.Invoke(functionName, json);
                return result != null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameLoop] Edge function '{functionName}' failed: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>Global game events (static delegates).</summary>
    public static class GameEvents
    {
        public static Action<object> OnPresenceUpdated;
        public static Action<string> OnBasraOpportunity; // broadcast card value
        public static Action<string> OnTurnChanged;
    }
}
