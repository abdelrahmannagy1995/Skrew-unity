using System.Collections;
using TMPro;
using UnityEngine;

namespace ScrewGame.UI
{
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

        public void UpdateCountdown(int seconds)
        {
            if (_countdownText == null) return;
            _countdownText.text    = seconds.ToString();
            _countdownText.enabled = true;
        }

        public void HighlightActiveSeat(int seatIndex)
        {
            if (_seatHighlights == null) return;
            for (int i = 0; i < _seatHighlights.Length; i++)
                if (_seatHighlights[i] != null)
                    _seatHighlights[i].SetActive(i == seatIndex);
        }
    }
}
