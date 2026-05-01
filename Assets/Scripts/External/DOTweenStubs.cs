// Compile-time stubs for DOTween. Define DOTWEEN_REAL to disable.
#if !DOTWEEN_REAL
using UnityEngine;

namespace DG.Tweening
{
    public enum Ease
    {
        Linear,
        InSine,    OutSine,    InOutSine,
        InQuad,    OutQuad,    InOutQuad,
        InCubic,   OutCubic,   InOutCubic,
        InQuart,   OutQuart,   InOutQuart,
        InQuint,   OutQuint,   InOutQuint,
        InExpo,    OutExpo,    InOutExpo,
        InCirc,    OutCirc,    InOutCirc,
        InElastic, OutElastic, InOutElastic,
        InBack,    OutBack,    InOutBack,
        InBounce,  OutBounce,  InOutBounce,
    }

    public class Tween
    {
        public Tween SetEase(Ease ease) => this;
    }

    public static class TransformExtensions
    {
        public static Tween DOLocalMove(this Transform t, Vector3 target, float duration)
        {
            // No-op stub: snap immediately. Replace by real DOTween for animation.
            if (t != null) t.localPosition = target;
            return new Tween();
        }

        public static Tween DOMove(this Transform t, Vector3 target, float duration)
        {
            if (t != null) t.position = target;
            return new Tween();
        }
    }
}
#endif
