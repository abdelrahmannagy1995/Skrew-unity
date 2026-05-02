using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Supabase.Realtime;
using UnityEngine;

namespace ScrewGame.Networking
{
    /// <summary>
    /// Handles ephemeral emoji/sticker broadcasts over Supabase Realtime Broadcast.
    /// Messages are NOT persisted to the database – purely real-time overlays.
    /// </summary>
    public class EmojiStickerBroadcast : MonoBehaviour
    {
        public static EmojiStickerBroadcast Instance { get; private set; }

        private RealtimeChannel _channel;
        private string          _matchId;

        public event Action<string, string> OnEmojiReceived;    // senderId, emojiCode
        public event Action<string, string> OnStickerReceived;  // senderId, stickerId

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public async UniTask JoinAsync(string matchId, CancellationToken ct = default)
        {
            _matchId = matchId;
            var client = Core.SupabaseManager.Instance.Client;

            _channel = client.Channel($"social:match:{matchId}");
            _channel.OnBroadcast += HandleBroadcast;
            await _channel.Subscribe();
        }

        public async UniTask SendEmojiAsync(string emojiCode, CancellationToken ct = default)
        {
            await _channel.Send("broadcast", "emoji", new
            {
                sender_id  = Core.SupabaseManager.Instance.CurrentUserId,
                emoji_code = emojiCode,
            });
        }

        public async UniTask SendStickerAsync(string stickerId, CancellationToken ct = default)
        {
            await _channel.Send("broadcast", "sticker", new
            {
                sender_id  = Core.SupabaseManager.Instance.CurrentUserId,
                sticker_id = stickerId,
            });
        }

        private void HandleBroadcast(object sender, Supabase.Realtime.Broadcast.BaseBroadcast e)
        {
            var payload = e.Payload<System.Collections.Generic.Dictionary<string, object>>();
            if (payload == null) return;

            string eventType = payload.TryGetValue("event", out var ev) ? ev?.ToString() : null;
            string senderId  = payload.TryGetValue("sender_id", out var sid) ? sid?.ToString() : null;

            if (eventType == "emoji" && senderId != null)
            {
                string code = payload.TryGetValue("emoji_code", out var ec) ? ec?.ToString() : null;
                if (code != null) OnEmojiReceived?.Invoke(senderId, code);
            }
            else if (eventType == "sticker" && senderId != null)
            {
                string id = payload.TryGetValue("sticker_id", out var si) ? si?.ToString() : null;
                if (id != null) OnStickerReceived?.Invoke(senderId, id);
            }
        }
    }
}
