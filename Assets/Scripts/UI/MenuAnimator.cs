using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ScrewGame.UI
{
    /// <summary>
    /// Drives entrance animations for the Menu scene:
    ///   – Title card swoops in from top
    ///   – Mode buttons cascade in from bottom with stagger
    ///   – Background floats gently
    /// Also handles "How to Play" button tap → InstructionsOverlay.Show().
    /// </summary>
    public class MenuAnimator : MonoBehaviour
    {
        [Header("Title")]
        [SerializeField] private RectTransform _titleRect;
        [SerializeField] private CanvasGroup   _titleGroup;

        [Header("Mode Buttons (top to bottom)")]
        [SerializeField] private RectTransform[] _modeButtons;
        [SerializeField] private float _buttonStagger = 0.07f;

        [Header("How to Play Button")]
        [SerializeField] private Button _howToPlayBtn;

        [Header("Background")]
        [SerializeField] private RectTransform _bgRect;

        private void Awake()
        {
            // Hide everything initially
            if (_titleGroup != null)  _titleGroup.alpha = 0f;
            if (_titleRect  != null)  _titleRect.anchoredPosition += Vector2.up * 80f;

            foreach (RectTransform btn in _modeButtons)
            {
                if (btn == null) continue;
                CanvasGroup cg = btn.GetComponent<CanvasGroup>() ?? btn.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                btn.anchoredPosition += Vector2.down * 40f;
            }
        }

        private void Start()
        {
            PlayEntranceAnimation();
            if (_howToPlayBtn != null)
                _howToPlayBtn.onClick.AddListener(() => InstructionsOverlay.Instance?.Show());

            StartBackgroundFloat();
        }

        // ─── Entrance sequence ────────────────────────────────────
        private void PlayEntranceAnimation()
        {
            Sequence seq = DOTween.Sequence();

            // Title
            if (_titleGroup != null && _titleRect != null)
            {
                Vector2 finalPos = _titleRect.anchoredPosition - Vector2.up * 80f;
                seq.Append(_titleGroup.DOFade(1f, 0.45f).SetEase(Ease.OutCubic));
                seq.Join(_titleRect.DOAnchorPos(finalPos, 0.45f).SetEase(Ease.OutBack));
            }

            // Buttons cascade
            float delay = 0.2f;
            for (int i = 0; i < _modeButtons.Length; i++)
            {
                RectTransform btn = _modeButtons[i];
                if (btn == null) continue;
                CanvasGroup cg = btn.GetComponent<CanvasGroup>();
                Vector2 finalBtnPos = btn.anchoredPosition + Vector2.down * 40f;
                float d = delay + i * _buttonStagger;
                seq.Insert(d, cg.DOFade(1f, 0.35f).SetEase(Ease.OutCubic));
                seq.Insert(d, btn.DOAnchorPos(finalBtnPos, 0.35f).SetEase(Ease.OutBack));
            }
        }

        // ─── Idle background float ────────────────────────────────
        private void StartBackgroundFloat()
        {
            if (_bgRect == null) return;
            _bgRect.DOAnchorPosY(_bgRect.anchoredPosition.y + 8f, 3.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }
}
