using System;
using System.Collections;
using UnityEngine;

public static class Tween
{
    const float MaxFrameStep = 1f / 20f;

    public static IEnumerator Run(float duration, Action<float> step, bool unscaledTime = false)
    {
        for (float t = 0f; t < 1f; )
        {
            float dt = Mathf.Min(unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime, MaxFrameStep);
            t += dt / Mathf.Max(0.0001f, duration);
            step(Mathf.Clamp01(t));
            yield return null;
        }
    }

    public static float EaseOutCubic(float x)
    {
        float inv = 1f - x;
        return 1f - inv * inv * inv;
    }
}
