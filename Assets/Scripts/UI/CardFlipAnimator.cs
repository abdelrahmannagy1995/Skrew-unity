using DG.Tweening;
using UnityEngine;
using ScrewGame.Entities;

namespace ScrewGame.UI
{
    /// <summary>
    /// Adds smooth DOTween flip animation to a CardObject.
    /// Scales X: 1→0 (half-way: swap face), then 0→1.
    /// </summary>
    [RequireComponent(typeof(CardObject))]
    public class CardFlipAnimator : MonoBehaviour
    {
        [SerializeField] private float _flipDuration = 0.35f;
        [SerializeField] private Ease  _flipEase     = Ease.InOutSine;

        private CardObject _card;
        private bool _isFlipping;

        private void Awake() => _card = GetComponent<CardObject>();

        // ─── Public API ───────────────────────────────────────────
        public void FlipToFaceUp(System.Action onComplete = null)
        {
            if (_isFlipping) return;
            if (_card.IsFaceUp)  { onComplete?.Invoke(); return; }
            AnimateFlip(() => _card.SetFaceUp(), onComplete);
        }

        public void FlipToFaceDown(System.Action onComplete = null)
        {
            if (_isFlipping) return;
            if (!_card.IsFaceUp) { onComplete?.Invoke(); return; }
            AnimateFlip(() => _card.SetFaceDown(), onComplete);
        }

        public void FlipToggle(System.Action onComplete = null)
        {
            if (_card.IsFaceUp) FlipToFaceDown(onComplete);
            else                FlipToFaceUp(onComplete);
        }

        // ─── Core animation ───────────────────────────────────────
        private void AnimateFlip(System.Action swapAction, System.Action onComplete)
        {
            _isFlipping = true;
            float half = _flipDuration * 0.5f;
            Vector3 origScale = transform.localScale;

            // First half: squash to X=0
            transform.DOScaleX(0f, half)
                .SetEase(_flipEase)
                .OnComplete(() =>
                {
                    swapAction?.Invoke();
                    // Second half: restore scale
                    transform.DOScaleX(origScale.x, half)
                        .SetEase(_flipEase)
                        .OnComplete(() =>
                        {
                            _isFlipping = false;
                            onComplete?.Invoke();
                        });
                });
        }

        // ─── Deal animation (fly from deck) ───────────────────────
        public void AnimateDealFrom(Vector3 worldOrigin, float travelDuration = 0.4f,
                                     System.Action onArrived = null)
        {
            Vector3 dest = transform.position;
            transform.position = worldOrigin;
            transform.localScale = Vector3.zero;

            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOMove(dest, travelDuration).SetEase(Ease.OutBack));
            seq.Join(transform.DOScale(Vector3.one, travelDuration * 0.8f).SetEase(Ease.OutBack));
            seq.OnComplete(() => onArrived?.Invoke());
        }
    }
}
