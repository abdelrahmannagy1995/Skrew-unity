using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using ScrewGame.Entities;
using UnityEngine;

namespace ScrewGame.Core
{
    /// <summary>
    /// Extension of GameLoop that contains all player-action Edge Function call methods.
    /// Split into a partial class for readability.
    /// </summary>
    public partial class GameLoop : MonoBehaviour
    {
        // -----------------------------------------------------------------------
        // Draw from stock
        // -----------------------------------------------------------------------

        public async UniTask<bool> DrawFromStockAsync(CancellationToken ct = default)
            => await CallEdgeFunctionAsync("draw-from-stock",
                new { match_id = _matchId, player_id = _localPlayerId }, ct);

        // -----------------------------------------------------------------------
        // Swap drawn card into grid
        // -----------------------------------------------------------------------

        public async UniTask<bool> SwapDrawnCardAsync(int gridIndex, CancellationToken ct = default)
            => await CallEdgeFunctionAsync("swap-drawn-card",
                new { match_id = _matchId, player_id = _localPlayerId, grid_index = gridIndex }, ct);

        // -----------------------------------------------------------------------
        // Discard drawn card without swapping
        // -----------------------------------------------------------------------

        public async UniTask<bool> DiscardDrawnCardAsync(CancellationToken ct = default)
            => await CallEdgeFunctionAsync("discard-drawn-card",
                new { match_id = _matchId, player_id = _localPlayerId }, ct);

        // -----------------------------------------------------------------------
        // Snatch from discard pile
        // -----------------------------------------------------------------------

        public async UniTask<bool> SnatchDiscardAsync(CancellationToken ct = default)
            => await CallEdgeFunctionAsync("snatch-discard",
                new { match_id = _matchId, player_id = _localPlayerId }, ct);

        // -----------------------------------------------------------------------
        // Declare Screw
        // -----------------------------------------------------------------------

        public async UniTask<bool> DeclareScrewAsync(CancellationToken ct = default)
            => await CallEdgeFunctionAsync("declare-screw",
                new { match_id = _matchId, player_id = _localPlayerId }, ct);

        // -----------------------------------------------------------------------
        // Attempt Basra (out-of-turn match)
        // -----------------------------------------------------------------------

        public async UniTask<bool> AttemptBasraAsync(int cardIndex, CancellationToken ct = default)
            => await CallEdgeFunctionAsync("basra-resolve", new
            {
                match_id     = _matchId,
                player_id    = _localPlayerId,
                card_index   = cardIndex,
                timestamp_ms = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            }, ct);

        // -----------------------------------------------------------------------
        // Command card: Peek opponent card (privately pushed to this client)
        // -----------------------------------------------------------------------

        public async UniTask<bool> PeekOpponentCardAsync(string opponentId, int cardIndex, CancellationToken ct = default)
            => await CallEdgeFunctionAsync("peek-opponent",
                new { match_id = _matchId, player_id = _localPlayerId, opponent_id = opponentId, card_index = cardIndex }, ct);

        // -----------------------------------------------------------------------
        // Command card: Basra Self – discard one of your own cards
        // -----------------------------------------------------------------------

        public async UniTask<bool> DiscardOwnCardAsync(int cardIndex, CancellationToken ct = default)
            => await CallEdgeFunctionAsync("basra-self",
                new { match_id = _matchId, player_id = _localPlayerId, card_index = cardIndex }, ct);

        // -----------------------------------------------------------------------
        // Command card: Khod w Hat (Blind Swap)
        // -----------------------------------------------------------------------

        public async UniTask<bool> BlindSwapAsync(int ownIndex, string opponentId, int oppIndex, CancellationToken ct = default)
            => await CallEdgeFunctionAsync("blind-swap", new
            {
                match_id    = _matchId,
                player_id   = _localPlayerId,
                own_index   = ownIndex,
                opponent_id = opponentId,
                opp_index   = oppIndex,
            }, ct);

        // -----------------------------------------------------------------------
        // Command card: Khod Bas (Give Only)
        // -----------------------------------------------------------------------

        public async UniTask<bool> GiveCardToOpponentAsync(int ownIndex, string targetOpponentId, CancellationToken ct = default)
            => await CallEdgeFunctionAsync("give-card", new
            {
                match_id    = _matchId,
                player_id   = _localPlayerId,
                own_index   = ownIndex,
                opponent_id = targetOpponentId,
            }, ct);

        // -----------------------------------------------------------------------
        // Command card: Kaab Dayer – see all players' one card each
        // -----------------------------------------------------------------------

        public async UniTask<bool> KaabDayerAllPlayersAsync(CancellationToken ct = default)
            => await CallEdgeFunctionAsync("kaab-dayer",
                new { match_id = _matchId, player_id = _localPlayerId }, ct);

        // -----------------------------------------------------------------------
        // Command card: 3agab ma 3agab
        // -----------------------------------------------------------------------

        public async UniTask<bool> AgabMaAgabAsync(string opponentId, int cardIndex, CancellationToken ct = default)
            => await CallEdgeFunctionAsync("agab-ma-agab", new
            {
                match_id    = _matchId,
                player_id   = _localPlayerId,
                opponent_id = opponentId,
                card_index  = cardIndex,
            }, ct);

        // -----------------------------------------------------------------------
        // Special: Force swap Thief into grid
        // -----------------------------------------------------------------------

        public async UniTask<bool> ForceSwapThiefAsync(int slotIndex, CancellationToken ct = default)
            => await CallEdgeFunctionAsync("force-swap-thief",
                new { match_id = _matchId, player_id = _localPlayerId, slot_index = slotIndex }, ct);

        // -----------------------------------------------------------------------
        // Special: Play Ping or Pong
        // -----------------------------------------------------------------------

        public async UniTask<bool> PlayPingPongAsync(SpecialCardId pingOrPong, CancellationToken ct = default)
            => await CallEdgeFunctionAsync("play-ping-pong", new
            {
                match_id  = _matchId,
                player_id = _localPlayerId,
                card_type = pingOrPong.ToString(),
            }, ct);

        // -----------------------------------------------------------------------
        // Thief guess (end of match)
        // -----------------------------------------------------------------------

        public async UniTask<bool> SubmitThiefGuessAsync(string guessedPlayerId, CancellationToken ct = default)
            => await CallEdgeFunctionAsync("thief-guess", new
            {
                match_id          = _matchId,
                caller_id         = _localPlayerId,
                guessed_player_id = guessedPlayerId,
            }, ct);
    }
}
