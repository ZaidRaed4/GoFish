using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DealCardFly : MonoBehaviour
{
    [Header("Refs (optional, auto-found if null)")]
    [SerializeField] Image image;
    [SerializeField] CanvasGroup canvasGroup;

    RectTransform _rt;

    void Awake()
    {
        _rt = (RectTransform)transform;

        if (image == null) image = GetComponent<Image>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (image != null)
        {
            image.raycastTarget = false;
            image.preserveAspect = true;
        }

        canvasGroup.blocksRaycasts = false;
    }

    public void SetSprite(Sprite sprite)
    {
        if (image != null) image.sprite = sprite;
    }

    public IEnumerator Fly(RectTransform from, RectTransform to, RectTransform flyLayer,
                           float duration = 0.6f, float fadeOutDuration = 0.12f, Action onArrive = null)
    {
        if (from == null || to == null || flyLayer == null)
        {
            Destroy(gameObject);
            yield break;
        }

        _rt.SetParent(flyLayer, false);
        canvasGroup.alpha = 1f;

        Vector2 start = flyLayer.InverseTransformPoint(from.position);
        Vector2 end = flyLayer.InverseTransformPoint(to.position);
        _rt.anchoredPosition = start;

        Quaternion startRot = from.rotation;
        Quaternion endRot = to.rotation;

        Vector2 dir = end - start;
        float dist = dir.magnitude;
        Vector2 dirN = dist > 0.001f ? dir / dist : Vector2.right;
        Vector2 perp = new Vector2(-dirN.y, dirN.x);

        float curveForward = Mathf.Clamp(dist * 0.25f, 120f, 260f);
        float sideDrift = Mathf.Clamp(dist * 0.08f, 30f, 110f);
        float lift = Mathf.Clamp(dist * 0.12f, 70f, 160f);
        float sideSign = UnityEngine.Random.value < 0.5f ? -1f : 1f;

        Vector2 p1 = start + dirN * curveForward + perp * (sideDrift * sideSign) + Vector2.up * (lift * 0.55f);
        Vector2 p2 = end - dirN * curveForward + perp * (sideDrift * sideSign * 0.45f) + Vector2.up * lift;

        float wobbleAmp = Mathf.Clamp(dist * 0.015f, 6f, 14f);
        const float wobbleFreq = 1.4f;
        const float rotAmp = 4f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, duration);
            float eased = EaseInOutCubic(Mathf.Clamp01(t));

            float decay = (1f - eased) * (1f - eased);
            float wobbleWindow = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.55f, 0.90f, eased));
            float wave = eased * wobbleFreq * Mathf.PI * 2f;

            Vector2 pos = Bezier(start, p1, p2, end, eased);
            _rt.anchoredPosition = pos + perp * (Mathf.Sin(wave) * wobbleAmp * decay * wobbleWindow);

            float z = Mathf.Sin(wave + 1.2f) * rotAmp * decay * wobbleWindow;
            _rt.rotation = Quaternion.Slerp(startRot, endRot, eased) * Quaternion.AngleAxis(z, Vector3.forward);

            yield return null;
        }

        _rt.anchoredPosition = end;
        _rt.rotation = endRot;

        onArrive?.Invoke();

        if (fadeOutDuration > 0f)
        {
            for (float ft = 0f; ft < 1f; )
            {
                ft += Time.deltaTime / fadeOutDuration;
                canvasGroup.alpha = 1f - Mathf.Clamp01(ft);
                yield return null;
            }
        }

        Destroy(gameObject);
    }

    static float EaseInOutCubic(float t) => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

    static Vector2 Bezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
    }
}
