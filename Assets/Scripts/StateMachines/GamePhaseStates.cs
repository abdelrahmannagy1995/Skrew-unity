using System.Collections;
using System.Collections.Generic;
using ScrewGame.UI;
using UnityEngine;

namespace ScrewGame.StateMachines
{
    /// <summary>
    /// The information/peek phase at the start of a match.
    /// Players may view exactly the two rightmost cards in their grid
    /// for a server-governed window (default 8 seconds).
    /// </summary>
    public class InfoPhaseState : GameState
    {
        private float _duration;
        private float _elapsed;
        private bool  _revealed;

        public InfoPhaseState(Dictionary<string, object> payload) : base(payload) { }

        public override void OnEnter(GameStateMachine machine)
        {
            _duration = GetPayloadValue("duration_secs", 8f);
            _elapsed  = 0f;
            _revealed = false;

            // Reveal the two rightmost cards (indices 2 and 3 of the local grid)
            PlayerGrid.LocalInstance?.RevealCard(2);
            PlayerGrid.LocalInstance?.RevealCard(3);
            _revealed = true;

            // Update HUD countdown
            HUDController.Instance?.ShowCountdown(_duration, "Info Phase – peek your cards!");
        }

        public override void OnUpdate(GameStateMachine machine)
        {
            _elapsed += Time.deltaTime;

            if (_elapsed >= _duration)
            {
                // Forcibly hide cards regardless of client state
                if (_revealed)
                {
                    PlayerGrid.LocalInstance?.HideCard(2);
                    PlayerGrid.LocalInstance?.HideCard(3);
                    _revealed = false;
                }

                // Transition to the first player's turn
                string localId = ScrewGame.Core.SupabaseManager.Instance?.CurrentUserId ?? string.Empty;
                machine.TransitionTo(new PlayerTurnState(new Dictionary<string, object>
                {
                    { "current_seat", 0 },
                    { "reason", "info_phase_expired" },
                }, localId));
            }
        }

        public override void OnExit(GameStateMachine machine)
        {
            HUDController.Instance?.HideCountdown();
        }
    }

    // =========================================================================
    // PlayerTurnState
    // =========================================================================

    /// <summary>
    /// Active player turn. Unlocks the local player's UI controls if it is their seat.
    /// </summary>
    public class PlayerTurnState : GameState, IBasraHandler, IPingPongHandler
    {
        private readonly string _localPlayerId;
        private int             _currentSeat;
        private bool            _isLocalTurn;

        public PlayerTurnState(Dictionary<string, object> payload, string localPlayerId)
            : base(payload)
        {
            _localPlayerId = localPlayerId;
        }

        public override void OnEnter(GameStateMachine machine)
        {
            _currentSeat = GetPayloadValue("current_seat", 0);
            int localSeat = PlayerSeatRegistry.GetLocalSeat(_localPlayerId);
            _isLocalTurn  = (_currentSeat == localSeat);

            HUDController.Instance?.HighlightActiveSeat(_currentSeat);
            TurnControlPanel.Instance?.SetInteractable(_isLocalTurn);

            if (_isLocalTurn)
                HUDController.Instance?.ShowMessage("Your turn!");
            else
                HUDController.Instance?.ShowMessage($"Waiting for player {_currentSeat + 1}…");
        }

        public override void OnExit(GameStateMachine machine)
        {
            TurnControlPanel.Instance?.SetInteractable(false);
        }

        // Basra is always available regardless of whose turn it is
        public void HandleBasraSuccess(Dictionary<string, object> payload)
        {
            string playerId = payload.TryGetValue("player_id", out var pid) ? pid?.ToString() : null;
            VisualEffects.Instance?.PlayBasraSuccess(playerId);
        }

        public void HandleBasraFailed(Dictionary<string, object> payload)
        {
            string playerId = payload.TryGetValue("player_id", out var pid) ? pid?.ToString() : null;
            VisualEffects.Instance?.PlayBasraFailure(playerId);
        }

        public void HandlePingPongSkip(Dictionary<string, object> payload)
        {
            HUDController.Instance?.ShowMessage("Turn skipped by Ping/Pong!");
        }
    }

    // =========================================================================
    // ResolveActionState
    // =========================================================================

    /// <summary>
    /// Resolves a command card effect. Pauses turn progression until complete.
    /// </summary>
    public class ResolveActionState : GameState
    {
        public ResolveActionState(Dictionary<string, object> payload) : base(payload) { }

        public override void OnEnter(GameStateMachine machine)
        {
            HUDController.Instance?.ShowMessage("Resolving action…");
            TurnControlPanel.Instance?.SetInteractable(false);
        }

        public override void OnExit(GameStateMachine machine)
        {
            HUDController.Instance?.HideMessage();
        }
    }

    // =========================================================================
    // MatchEndState
    // =========================================================================

    /// <summary>
    /// Handles the endgame reveal, Thief guessing modal, and score display.
    /// </summary>
    public class MatchEndState : GameState, IThiefHandler
    {
        public MatchEndState(Dictionary<string, object> payload) : base(payload) { }

        public override void OnEnter(GameStateMachine machine)
        {
            bool thiefPending = GetPayloadValue("thief_swap_pending", false);

            // Flip all cards face-up
            AllGridsRevealController.Instance?.RevealAll();

            if (thiefPending)
            {
                // Show the Thief guessing modal to the Screw caller
                string callerId = GetPayloadValue<string>("caller_id", null);
                string localId  = ScrewGame.Core.SupabaseManager.Instance?.CurrentUserId;

                if (callerId == localId)
                    ThiefGuessModal.Instance?.Show();
            }
            else
            {
                ShowFinalScores();
            }
        }

        public void HandleThiefGuessResult(Dictionary<string, object> payload)
        {
            ThiefGuessModal.Instance?.Hide();
            ShowFinalScores();
        }

        private void ShowFinalScores()
        {
            var scores  = GetPayloadValue<object>("scores", null);
            var winner  = GetPayloadValue<string>("winner_id", null);
            ScoreboardUI.Instance?.Show(scores, winner);
        }
    }
}
