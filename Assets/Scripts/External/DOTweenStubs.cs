// Compile-time stubs for DOTween. Define DOTWEEN_REAL to disable these stubs.
// All animation calls are no-ops — they snap to target immediately.
// Install the real DOTween package and add DOTWEEN_REAL to Scripting Define Symbols
// to replace these stubs with actual animations.
#if !DOTWEEN_REAL
using System;
using UnityEngine;
using UnityEngine.UI;

namespace DG.Tweening
{
    // ─── Enums ───────────────────────────────────────────────────────────────
    public enum Ease
    {
        Linear,
        InSine, OutSine, InOutSine,
        InQuad, OutQuad, InOutQuad,
        InCubic, OutCubic, InOutCubic,
        InQuart, OutQuart, InOutQuart,
        InQuint, OutQuint, InOutQuint,
        InExpo, OutExpo, InOutExpo,
        InCirc, OutCirc, InOutCirc,
        InElastic, OutElastic, InOutElastic,
        InBack, OutBack, InOutBack,
        InBounce, OutBounce, InOutBounce,
    }

    public enum LoopType { Restart, Yoyo, Incremental }

    // ─── Tween ───────────────────────────────────────────────────────────────
    public class Tween
    {
        private Action _onComplete;

        public Tween SetEase(Ease ease)         => this;
        public Tween SetDelay(float delay)      { _onComplete?.Invoke(); return this; }
        public Tween SetLoops(int loops, LoopType lt = LoopType.Restart) => this;
        public Tween SetUpdate(bool isIndep)    => this;

        public Tween OnComplete(Action cb)
        {
            _onComplete = cb;
            cb?.Invoke();
            return this;
        }

        public void Kill(bool complete = false) { }
    }

    // ─── Sequence ────────────────────────────────────────────────────────────
    public class Sequence : Tween
    {
        public Sequence Append(Tween t)            => this;
        public Sequence Join(Tween t)              => this;
        public Sequence Insert(float atPos, Tween t) => this;
        public new Sequence OnComplete(Action cb)  { cb?.Invoke(); return this; }
        public Sequence AppendInterval(float t)    => this;
        public Sequence AppendCallback(Action cb)  { cb?.Invoke(); return this; }
    }

    // ─── DOTween static ──────────────────────────────────────────────────────
    public static class DOTween
    {
        public static Sequence Sequence() => new Sequence();
        public static void Kill(object target, bool complete = false) { }
        public static void KillAll(bool complete = false) { }
    }

    // ─── Transform extensions ────────────────────────────────────────────────
    public static class TransformExtensions
    {
        public static Tween DOMove(this Transform t, Vector3 to, float dur)
        { if (t) t.position = to; return new Tween(); }

        public static Tween DOLocalMove(this Transform t, Vector3 to, float dur)
        { if (t) t.localPosition = to; return new Tween(); }

        public static Tween DOScale(this Transform t, Vector3 to, float dur)
        { if (t) t.localScale = to; return new Tween(); }

        public static Tween DOScale(this Transform t, float to, float dur)
        { if (t) t.localScale = Vector3.one * to; return new Tween(); }

        public static Tween DOScaleX(this Transform t, float to, float dur)
        { if (t) { Vector3 s = t.localScale; s.x = to; t.localScale = s; } return new Tween(); }

        public static Tween DOScaleY(this Transform t, float to, float dur)
        { if (t) { Vector3 s = t.localScale; s.y = to; t.localScale = s; } return new Tween(); }

        public static Tween DORotate(this Transform t, Vector3 to, float dur)
        { if (t) t.eulerAngles = to; return new Tween(); }

        public static Tween DOPunchScale(this Transform t, Vector3 punch, float dur, int vibrato = 10, float elasticity = 1f)
        { return new Tween(); }

        public static Tween DOPunchPosition(this Transform t, Vector3 punch, float dur, int vibrato = 10, float elasticity = 1f)
        { return new Tween(); }

        public static void DOKill(this Transform t, bool complete = false) { }
    }

    // ─── RectTransform extensions ─────────────────────────────────────────────
    public static class RectTransformExtensions
    {
        public static Tween DOAnchorPos(this RectTransform rt, Vector2 to, float dur, bool snapping = false)
        { if (rt) rt.anchoredPosition = to; return new Tween(); }

        public static Tween DOAnchorPosX(this RectTransform rt, float to, float dur, bool snapping = false)
        { if (rt) { Vector2 p = rt.anchoredPosition; p.x = to; rt.anchoredPosition = p; } return new Tween(); }

        public static Tween DOAnchorPosY(this RectTransform rt, float to, float dur, bool snapping = false)
        { if (rt) { Vector2 p = rt.anchoredPosition; p.y = to; rt.anchoredPosition = p; } return new Tween(); }

        public static Tween DOSizeDelta(this RectTransform rt, Vector2 to, float dur, bool snapping = false)
        { if (rt) rt.sizeDelta = to; return new Tween(); }

        public static Tween DOScale(this RectTransform rt, float to, float dur)
        { if (rt) rt.localScale = Vector3.one * to; return new Tween(); }

        public static Tween DOScale(this RectTransform rt, Vector3 to, float dur)
        { if (rt) rt.localScale = to; return new Tween(); }

        public static Tween DOPunchScale(this RectTransform rt, Vector3 punch, float dur, int vibrato = 10, float elasticity = 1f)
        { return new Tween(); }

        public static void DOKill(this RectTransform rt, bool complete = false) { }
    }

    // ─── CanvasGroup extensions ───────────────────────────────────────────────
    public static class CanvasGroupExtensions
    {
        public static Tween DOFade(this CanvasGroup cg, float to, float dur)
        { if (cg) cg.alpha = to; return new Tween(); }
    }

    // ─── UI Image / Graphic extensions ───────────────────────────────────────
    public static class UIExtensions
    {
        public static Tween DOFade(this Image img, float to, float dur)
        { if (img) { Color c = img.color; c.a = to; img.color = c; } return new Tween(); }

        public static Tween DOColor(this Image img, Color to, float dur)
        { if (img) img.color = to; return new Tween(); }

        public static Tween DOFade(this TMPro.TextMeshProUGUI tmp, float to, float dur)
        { if (tmp) { Color c = tmp.color; c.a = to; tmp.color = c; } return new Tween(); }

        public static Tween DOFade(this TMPro.TextMeshPro tmp, float to, float dur)
        { if (tmp) { Color c = tmp.color; c.a = to; tmp.color = c; } return new Tween(); }
    }

    // ─── Tween kill extension ─────────────────────────────────────────────────
    public static class TweenExtensions
    {
        public static void Kill(this Tween t, bool complete = false) { }
    }
}
#endif
