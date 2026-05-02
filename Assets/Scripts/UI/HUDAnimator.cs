using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrewGame.UI
{
    /// <summary>
    /// Drives HUD entrance / exit animations:
    ///   – Message bar fade-in slide
    ///   – Countdown pulse when ≤ 5 s
    ///   – Turn indicator glow pulse
    ///   – Score pop on change
    /// </summary>
    public class HUDAnimator : MonoBehaviour
    {
        public static HUDAnimator Instance { get; private set; }

        [Header("Message Bar")]
        [SerializeField] private CanvasGroup _messageGroup;
        [SerializeField] private RectTransform _messageRect;
        [SerializeField] private float _slideOffset = 40f;

        [Header("Countdown")]
        [SerializeField] private TextMeshProUGUI _countdownText;
        [SerializeField] private Color _urgentColor = new Color(1f, 0.25f, 0.15f);
        [SerializeField] private Color _normalColor  = Color.white;

        [Header("Turn Indicator")]
        [SerializeField] private Image _turnGlow;
        private Tween _glowTween;

        [Header("Score")]
        [SerializeField] private RectTransform _scoreRect;

        private void Awake() => Instance = this;
        private void Start()
        {
            if (_messageGroup != null) _messageGroup.alpha = 0f;
            StartTurnGlow();
        }

        // ─── Message bar ─────────────────────────────────────────
        public void ShowMessage(string msg, float duration = 2.5f)
        {
            if (_messageGroup == null) return;
            TextMeshProUGUI tmp = _messageRect != null
                ? _messageRect.GetComponentInChildren<TextMeshProUGUI>()
                : null;
            if (tmp != null) tmp.text = msg;

            Vector2 startPos = _messageRect != null
                ? _messageRect.anchoredPosition + Vector2.up * _slideOffset
                : Vector2.zero;

            DOTween.Kill(_messageGroup);
            if (_messageRect != null)
            {
                _messageRect.anchoredPosition = startPos;
                _messageRect.DOAnchorPosY(startPos.y - _slideOffset, 0.3f).SetEase(Ease.OutCubic);
            }
            _messageGroup.DOFade(1f, 0.25f).OnComplete(() =>
                _messageGroup.DOFade(0f, 0.35f).SetDelay(duration));
        }

        // ─── Countdown ───────────────────────────────────────────
        public void UpdateCountdown(int seconds)
        {
            if (_countdownText == null) return;
            _countdownText.text = seconds.ToString();
            bool urgent = seconds <= 5;
            _countdownText.color = urgent ? _urgentColor : _normalColor;
            if (urgent)
            {
                _countdownText.transform.DOKill();
                _countdownText.transform
                    .DOPunchScale(Vector3.one * 0.25f, 0.35f, 3, 0.4f);
            }
        }

        // ─── Turn glow ───────────────────────────────────────────
        private void StartTurnGlow()
        {
            if (_turnGlow == null) return;
            _glowTween?.Kill();
            _glowTween = _turnGlow.DOFade(0.3f, 0.8f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        public void PulseHighlight()
        {
            if (_turnGlow == null) return;
            _glowTween?.Kill();
            _turnGlow.DOFade(1f, 0.15f)
                .OnComplete(StartTurnGlow);
        }

        // ─── Score pop ───────────────────────────────────────────
        public void AnimateScorePop()
        {
            if (_scoreRect == null) return;
            _scoreRect.DOKill();
            _scoreRect.DOPunchScale(Vector3.one * 0.3f, 0.4f, 4, 0.5f);
        }
    }
}
