using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Postgrest.Models;
using UnityEngine;

namespace ScrewGame.Social
{
    [Serializable]
    public class ChatMessage : BaseModel
    {
        [JsonProperty("id")]         public string Id        { get; set; }
        [JsonProperty("match_id")]   public string MatchId   { get; set; }
        [JsonProperty("user_id")]    public string UserId    { get; set; }
        [JsonProperty("content")]    public string Content   { get; set; }
        [JsonProperty("created_at")] public string CreatedAt { get; set; }
    }

    /// <summary>
    /// Persistent in-game text chat backed by the Supabase messages table.
    /// Uses Realtime postgres_changes subscriptions to receive new messages live.
    /// </summary>
    public class ChatManager : MonoBehaviour
    {
        public static ChatManager Instance { get; private set; }

        private string _matchId;
        private Supabase.Realtime.RealtimeChannel _channel;

        public event Action<ChatMessage> OnNewMessage;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // -----------------------------------------------------------------------
        // Subscribe to match chat
        // -----------------------------------------------------------------------

        public async UniTask JoinChatAsync(string matchId, CancellationToken ct = default)
        {
            _matchId = matchId;
            var client = Core.SupabaseManager.Instance.Client;

            // Subscribe to postgres_changes on the messages table for this match
            _channel = client.Channel($"chat:match:{matchId}");

            _channel.OnPostgresChange += (sender, change) =>
            {
                if (change.Payload?.Data?.Record == null) return;
                try
                {
                    var msg = JsonConvert.DeserializeObject<ChatMessage>(
                        change.Payload.Data.Record.ToString());
                    if (msg != null) OnNewMessage?.Invoke(msg);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Chat] Failed to parse message: {ex.Message}");
                }
            };

            await _channel.Subscribe();
        }

        // -----------------------------------------------------------------------
        // Load chat history
        // -----------------------------------------------------------------------

        public async UniTask<List<ChatMessage>> LoadHistoryAsync(int limit = 50, CancellationToken ct = default)
        {
            var client = Core.SupabaseManager.Instance.Client;
            var result = await client
                .From<ChatMessage>()
                .Select("*")
                .Filter("match_id", Postgrest.Constants.Operator.Equals, _matchId)
                .Order("created_at", Postgrest.Constants.Ordering.Ascending)
                .Limit(limit)
                .Get();

            return result?.Models ?? new List<ChatMessage>();
        }

        // -----------------------------------------------------------------------
        // Send message
        // -----------------------------------------------------------------------

        public async UniTask<bool> SendMessageAsync(string content, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(content) || content.Length > 500)
                return false;

            var client = Core.SupabaseManager.Instance.Client;
            var msg = new ChatMessage
            {
                MatchId = _matchId,
                UserId  = Core.SupabaseManager.Instance.CurrentUserId,
                Content = content.Trim(),
            };

            var result = await client.From<ChatMessage>().Insert(msg);
            return result?.Models?.Count > 0;
        }
    }
}
