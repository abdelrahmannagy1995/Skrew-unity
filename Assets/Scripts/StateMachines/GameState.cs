using System.Collections.Generic;
using UnityEngine;

namespace ScrewGame.StateMachines
{
    // =========================================================================
    // Abstract base state
    // =========================================================================

    /// <summary>
    /// Abstract base class for all game phase states.
    /// Each concrete state receives a Payload dictionary from the Supabase
    /// Realtime broadcast event that triggered the transition.
    /// </summary>
    public abstract class GameState
    {
        protected Dictionary<string, object> Payload { get; }

        protected GameState(Dictionary<string, object> payload = null)
        {
            Payload = payload ?? new Dictionary<string, object>();
        }

        /// <summary>Called once when the state becomes active.</summary>
        public abstract void OnEnter(GameStateMachine machine);

        /// <summary>Called every Unity Update tick while the state is active.</summary>
        public virtual void OnUpdate(GameStateMachine machine) { }

        /// <summary>Called once when the state is leaving.</summary>
        public virtual void OnExit(GameStateMachine machine) { }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------
        protected T GetPayloadValue<T>(string key, T defaultValue = default)
        {
            if (Payload.TryGetValue(key, out var raw) && raw is T typed)
                return typed;
            return defaultValue;
        }
    }

    // =========================================================================
    // GameStateMachine
    // =========================================================================

    /// <summary>
    /// MonoBehaviour state machine.  Owns the currently active <see cref="GameState"/>
    /// and routes broadcast events from <see cref="Core.GameLoop"/> to it.
    /// </summary>
    public class GameStateMachine : MonoBehaviour
    {
        private GameState _currentState;

        // -----------------------------------------------------------------------
        // Transition
        // -----------------------------------------------------------------------

        /// <summary>Transition to a new state immediately.</summary>
        public void TransitionTo(GameState newState)
        {
            if (_currentState != null)
            {
                Debug.Log($"[StateMachine] Exiting: {_currentState.GetType().Name}");
                _currentState.OnExit(this);
            }

            _currentState = newState;
            Debug.Log($"[StateMachine] Entering: {_currentState.GetType().Name}");
            _currentState.OnEnter(this);
        }

        // -----------------------------------------------------------------------
        // Unity lifecycle
        // -----------------------------------------------------------------------
        private void Update()
        {
            _currentState?.OnUpdate(this);
        }

        // -----------------------------------------------------------------------
        // Event passthrough from GameLoop
        // -----------------------------------------------------------------------

        public void OnBasraSuccess(Dictionary<string, object> payload)
        {
            (_currentState as IBasraHandler)?.HandleBasraSuccess(payload);
        }

        public void OnBasraFailed(Dictionary<string, object> payload)
        {
            (_currentState as IBasraHandler)?.HandleBasraFailed(payload);
        }

        public void OnThiefGuessResult(Dictionary<string, object> payload)
        {
            (_currentState as IThiefHandler)?.HandleThiefGuessResult(payload);
        }

        public void OnPingPongSkip(Dictionary<string, object> payload)
        {
            (_currentState as IPingPongHandler)?.HandlePingPongSkip(payload);
        }
    }

    // =========================================================================
    // Handler interfaces
    // =========================================================================

    public interface IBasraHandler
    {
        void HandleBasraSuccess(Dictionary<string, object> payload);
        void HandleBasraFailed(Dictionary<string, object> payload);
    }

    public interface IThiefHandler
    {
        void HandleThiefGuessResult(Dictionary<string, object> payload);
    }

    public interface IPingPongHandler
    {
        void HandlePingPongSkip(Dictionary<string, object> payload);
    }
}
