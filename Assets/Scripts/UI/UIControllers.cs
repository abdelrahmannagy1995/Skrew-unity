using System;
using System.Collections;
using DG.Tweening;
using ScrewGame.Entities;
using ScrewGame.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrewGame.UI
{
    // =========================================================================
    // PlayerGrid – manages the local player's 4-card grid
    // =========================================================================

    /// <summary>
    /// Manages the visual 4-card grid for the local player.
    /// Cards are rendered face-down initially; Peek phase reveals indices 2 & 3.
    /// </summary>
    public class PlayerGrid : MonoBehaviour
    {
        public static PlayerGrid LocalInstance { get; private set; }

        [SerializeField] private CardObject[] _cardObjects = new CardObject[4];

        private void Awake()
        {
            LocalInstance = this;
        }

        public void RevealCard(int index)
        {
            if (index < 0 || index >= _cardObjects.Length) return;
            _cardObjects[index]?.SetFaceUp();
        }

        public void HideCard(int index)
        {
            if (index < 0 || index >= _cardObjects.Length) return;
            _cardObjects[index]?.SetFaceDown();
        }

        public void AnimateCardDeal(int index, Vector3 fromPosition)
        {
            if (index < 0 || index >= _cardObjects.Length) return;
            var card = _cardObjects[index];
            if (card == null) return;

            card.transform.position = fromPosition;
            card.transform
                .DOLocalMove(Vector3.zero, 0.4f)
                .SetEase(Ease.OutBack);
        }

        public void AnimateSwap(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _cardObjects.Length) return;
            if (toIndex < 0   || toIndex   >= _cardObjects.Length) return;

            var a = _cardObjects[fromIndex].transform;
            var b = _cardObjects[toIndex].transform;

            var aPos = a.position;
            var bPos = b.position;

            a.DOMove(bPos, 0.3f).SetEase(Ease.InOutSine);
            b.DOMove(aPos, 0.3f).SetEase(Ease.InOutSine);
        }
    }

    // =========================================================================
    // HUDController
    // =========================================================================

    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private TextMeshProUGUI _countdownText;
        [SerializeField] private GameObject[]    _seatHighlights;

        private Coroutine _countdownCoroutine;

        private void Awake() { Instance = this; }

        public void ShowMessage(string message)
        {
            if (_messageText == null) return;
            _messageText.text    = message;
            _messageText.enabled = true;
        }

        public void HideMessage()
        {
            if (_messageText != null) _messageText.enabled = false;
        }

        public void ShowCountdown(float duration, string prefix = "")
        {
            if (_countdownCoroutine != null) StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = StartCoroutine(CountdownRoutine(duration, prefix));
        }

        public void HideCountdown()
        {
            if (_countdownCoroutine != null) StopCoroutine(_countdownCoroutine);
            if (_countdownText != null) _countdownText.enabled = false;
        }

        private IEnumerator CountdownRoutine(float duration, string prefix)
        {
            float remaining = duration;
            if (_countdownText != null) _countdownText.enabled = true;

            while (remaining > 0f)
            {
                if (_countdownText != null)
                    _countdownText.text = $"{prefix} {Mathf.CeilToInt(remaining)}s";
                remaining -= Time.deltaTime;
                yield return null;
            }

            if (_countdownText != null) _countdownText.enabled = false;
        }

        public void HighlightActiveSeat(int seatIndex)
        {
            if (_seatHighlights == null) return;
            for (int i = 0; i < _seatHighlights.Length; i++)
            {
                if (_seatHighlights[i] != null)
                    _seatHighlights[i].SetActive(i == seatIndex);
            }
        }
    }

    // =========================================================================
    // TurnControlPanel
    // =========================================================================

    public class TurnControlPanel : MonoBehaviour
    {
        public static TurnControlPanel Instance { get; private set; }

        [SerializeField] private Button _drawButton;
        [SerializeField] private Button _snatchButton;
        [SerializeField] private Button _screwButton;

        private void Awake()
        {
            Instance = this;
            SetInteractable(false);
        }

        public void SetInteractable(bool interactable)
        {
            if (_drawButton   != null) _drawButton.interactable   = interactable;
            if (_snatchButton != null) _snatchButton.interactable = interactable;
            if (_screwButton  != null) _screwButton.interactable  = interactable;
        }
    }

    // =========================================================================
    // VisualEffects
    // =========================================================================

    /// <summary>
    /// Handles Basra impact effects, screen shake, and particle bursts.
    /// Uses Cinemachine for screen shake and Unity ParticleSystem for burst FX.
    /// </summary>
    public class VisualEffects : MonoBehaviour
    {
        public static VisualEffects Instance { get; private set; }

        [SerializeField] private ParticleSystem _basraParticleBurst;
        [SerializeField] private Cinemachine.CinemachineVirtualCamera _gameCamera;
        [SerializeField] private float _shakeAmplitude = 2f;
        [SerializeField] private float _shakeDuration  = 0.3f;

        private Cinemachine.CinemachineBasicMultiChannelPerlin _noise;

        private void Awake()
        {
            Instance = this;
            if (_gameCamera != null)
                _noise = _gameCamera.GetCinemachineComponent<Cinemachine.CinemachineBasicMultiChannelPerlin>();
        }

        public void PlayBasraSuccess(string playerId)
        {
            if (_basraParticleBurst != null)
                _basraParticleBurst.Play();

            StartCoroutine(ShakeRoutine());
        }

        public void PlayBasraFailure(string playerId)
        {
            // Subtle red flash – handled by shader / UI overlay
        }

        private IEnumerator ShakeRoutine()
        {
            if (_noise != null)
            {
                _noise.m_AmplitudeGain = _shakeAmplitude;
                yield return new WaitForSeconds(_shakeDuration);
                _noise.m_AmplitudeGain = 0f;
            }
        }
    }

    // =========================================================================
    // ScoreboardUI, ThiefGuessModal, AllGridsRevealController (stubs)
    // =========================================================================

    public class ScoreboardUI : MonoBehaviour
    {
        public static ScoreboardUI Instance { get; private set; }
        private void Awake() { Instance = this; }
        public void Show(object scores, string winnerId) { gameObject.SetActive(true); }
    }

    public class ThiefGuessModal : MonoBehaviour
    {
        public static ThiefGuessModal Instance { get; private set; }
        private void Awake() { Instance = this; }
        public void Show() { gameObject.SetActive(true); }
        public void Hide() { gameObject.SetActive(false); }
    }

    public class AllGridsRevealController : MonoBehaviour
    {
        public static AllGridsRevealController Instance { get; private set; }
        private void Awake() { Instance = this; }
        public void RevealAll() { /* flip all card objects to face-up */ }
    }

    public class OpponentGridManager : MonoBehaviour
    {
        public static OpponentGridManager Instance { get; private set; }
        private void Awake() { Instance = this; }
        public void HideCard(string opponentId, int cardIndex) { }
    }

    /// <summary>UI helper for command-card interaction flows.</summary>
    public class CommandUI : MonoBehaviour
    {
        public static CommandUI Instance { get; private set; }
        private void Awake() { Instance = this; }

        public void RequestOwnCardSelection(Action<int> callback) { }
        public void RequestOpponentCardSelection(Action<string, int> callback) { }
        public void RequestOwnAndOpponentCardSelection(Action<int, string, int> callback) { }
        public void RequestOwnCardAndOpponentSelection(Action<int, string> callback) { }
        public void ShowKaabDayerChoice(Action<bool> callback) { }
        public void ShowCommandCardChoice(Action<CommandCardId> callback) { }
        public void ShowThiefNotification() { }
    }

    /// <summary>Registry mapping player IDs to seat indices.</summary>
    public static class PlayerSeatRegistry
    {
        private static readonly System.Collections.Generic.Dictionary<string, int> _seatMap = new();

        public static void Register(string playerId, int seatIndex) => _seatMap[playerId] = seatIndex;
        public static int GetLocalSeat(string playerId) => _seatMap.TryGetValue(playerId, out int seat) ? seat : 0;
    }
}
