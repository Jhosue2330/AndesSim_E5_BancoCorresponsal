using System;
using System.Collections;
using UnityEngine;

namespace AndesSim
{
    /// <summary>Curvas de easing y un tween por corrutina (tiempo no escalado por defecto).</summary>
    public static class Ease
    {
        public static float Linear(float t) => t;
        public static float OutCubic(float t) { t = 1f - t; return 1f - t * t * t; }
        public static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
        public static float OutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            t -= 1f;
            return 1f + c3 * t * t * t + c1 * t * t;
        }
        public static float OutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c4 = 2f * Mathf.PI / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        public static IEnumerator Tween(float duration, Action<float> apply, Func<float, float> ease = null, bool unscaled = true)
        {
            ease ??= OutCubic;
            if (duration <= 0f) { apply(1f); yield break; }
            for (float t = 0f; t < 1f; t += (unscaled ? Time.unscaledDeltaTime : Time.deltaTime) / duration)
            {
                apply(ease(Mathf.Clamp01(t)));
                yield return null;
            }
            apply(1f);
        }
    }
}
