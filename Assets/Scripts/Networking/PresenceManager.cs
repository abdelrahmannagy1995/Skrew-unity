using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Supabase.Realtime;
using UnityEngine;

namespace ScrewGame.Networking
{
    /// <summary>
    /// Manages Supabase Realtime Presence for the current match.
    /// Tracks player connections and triggers bot-fallback when a client disconnects.
    /// </summary>
    public class PresenceManager : MonoBehaviour
    {
        public static PresenceManager Instance { get; private set; }

        private RealtimeChannel _channel;
        private string          _matchId;

        // -----------------------------------------------------------------------
        // Events
        // -----------------------------------------------------------------------
        public event Action<string> OnPlayerConnected;
        public event Action<string> OnPlayerDisconnected;

        // -----------------------------------------------------------------------
        // Unity lifecycle
        // -----------------------------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            LeaveAsync(destroyCancellationToken).Forget();
        }

        // -----------------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------------

        public async UniTask JoinAsync(string matchId, CancellationToken ct = default)
        {
            _matchId = matchId;
            var client = Core.SupabaseManager.Instance.Client;

            _channel = client.Channel($"presence:match:{matchId}");
            _channel.OnPresenceSync  += HandlePresenceSync;
            _channel.OnPresenceJoin  += HandlePresenceJoin;
            _channel.OnPresenceLeave += HandlePresenceLeave;

            await _channel.Subscribe();
            await _channel.Track(new System.Collections.Generic.Dictionary<string, object>
            {
                { "user_id", Core.SupabaseManager.Instance.CurrentUserId },
                { "online",  true },
            });
        }

        public async UniTask LeaveAsync(CancellationToken ct = default)
        {
            if (_channel != null)
                await _channel.Unsubscribe();
        }

        // -----------------------------------------------------------------------
        // Handlers
        // -----------------------------------------------------------------------

        private void HandlePresenceSync(object sender, EventArgs e)
        {
            Debug.Log($"[Presence] Sync – {_channel.Presences?.Count ?? 0} players online.");
        }

        private void HandlePresenceJoin(object sender, PresenceEventArgs args)
        {
            var joins = args.Response?.Joins;
            if (joins == null) return;
            var enumerator = joins.Keys.GetEnumerator();
            if (enumerator.MoveNext())
                OnPlayerConnected?.Invoke(enumerator.Current);
        }

        private void HandlePresenceLeave(object sender, PresenceEventArgs args)
        {
            var leaves = args.Response?.Leaves;
            if (leaves == null) return;
            var enumerator = leaves.Keys.GetEnumerator();
            if (enumerator.MoveNext())
            {
                string userId = enumerator.Current;
                Debug.LogWarning($"[Presence] Player {userId} disconnected. Spawning AI bot fallback.");
                OnPlayerDisconnected?.Invoke(userId);
            }
        }
    }
}
