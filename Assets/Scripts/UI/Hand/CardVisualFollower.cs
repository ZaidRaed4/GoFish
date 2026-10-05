using UnityEngine;
using UnityEngine.UI;

public class CardVisualFollower : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] float positionSharpness = 20f;
    [SerializeField] float rotationSharpness = 18f;
    [SerializeField] bool useUnscaledTime = true;

    [Header("Tilt")]
    [Tooltip("Degrees of tilt per unit of speed")]
    [SerializeField] float tiltStrength = 0.012f;
    [SerializeField] float maxTiltDeg = 12f;
    [SerializeField] float tiltSharpness = 16f;

    [Header("Drag Visual Boost")]
    [SerializeField] float dragScale = 1.12f;
    [SerializeField] float scaleSharpness = 18f;

    [Header("Hover")]
    [Tooltip("How far the card rises under the pointer (UI units, 4 = one art pixel). Only the picture moves, not the click area.")]
    [SerializeField] float hoverLift = 16f;

    [Header("Card Art")]
    [Tooltip("Found by the name CardArt when empty")]
    [SerializeField] Image cardArtImage;

    [Header("Shadow")]
    [Tooltip("Found by the name Shadow when empty")]
    [SerializeField] RectTransform shadowRect;
    [SerializeField] CanvasGroup shadowGroup;
    [SerializeField] float shadowFadeSharpness = 18f;
    [SerializeField] float shadowMaxAlpha = 0.35f;
    [SerializeField] float shadowDragYOffset = -26f;
    [SerializeField] float shadowIdleYOffset = -14f;
    [SerializeField] float shadowDragScale = 1.10f;
    [SerializeField] float shadowIdleScale = 0.95f;
    [SerializeField] float shadowMoveSharpness = 18f;
    [Tooltip("Sideways shadow shift per degree of tilt")]
    [SerializeField] float shadowTiltXOffset = 0.6f;

    CardDragSnap _baseCard;
    RectTransform _baseRt;
    RectTransform _rt;
    HandCurveLayout _layout;

    Vector3 _lastBasePos;
    float _tiltX, _tiltZ;
    Vector3 _currentScale = Vector3.one;
    float _shadowAlpha;
    Vector2 _shadowPos;
    Vector3 _shadowScale = Vector3.one;

    void Awake()
    {
        _rt = (RectTransform)transform;
        _currentScale = _rt.localScale;

        if (shadowRect == null)
        {
            var t = transform.Find("Shadow");
            if (t != null) shadowRect = (RectTransform)t;
        }

        if (shadowRect != null && shadowGroup == null)
        {
            shadowGroup = shadowRect.GetComponent<CanvasGroup>();
            if (shadowGroup == null) shadowGroup = shadowRect.gameObject.AddComponent<CanvasGroup>();
        }

        if (shadowGroup != null)
            shadowGroup.blocksRaycasts = false;

        if (cardArtImage == null)
        {
            var t = transform.Find("CardArt");
            if (t != null) cardArtImage = t.GetComponent<Image>();
        }
    }

    public void Bind(CardDragSnap baseCard)
    {
        _baseCard = baseCard;
        _baseRt = baseCard != null ? baseCard.Rect : null;
        if (_baseRt == null) return;

        _lastBasePos = _baseRt.position;
        _rt.position = _baseRt.position;
        _rt.rotation = _baseRt.rotation;
        _rt.localScale = Vector3.one;
        _currentScale = Vector3.one;

        if (shadowRect != null)
        {
            _shadowPos = shadowRect.anchoredPosition;
            _shadowScale = shadowRect.localScale;
        }

        if (shadowGroup != null)
            _shadowAlpha = shadowGroup.alpha;
    }

    public void SetCardSprite(Sprite sprite)
    {
        if (cardArtImage != null) cardArtImage.sprite = sprite;
    }

    void LateUpdate()
    {
        if (_baseRt == null) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (dt <= 0f) return;

        float posT = 1f - Mathf.Exp(-positionSharpness * dt);
        float rotT = 1f - Mathf.Exp(-rotationSharpness * dt);
        float tiltT = 1f - Mathf.Exp(-tiltSharpness * dt);
        float scaleT = 1f - Mathf.Exp(-scaleSharpness * dt);

        if (_layout == null && _baseCard.homeSlot != null && _baseCard.homeSlot.parent != null)
            _layout = _baseCard.homeSlot.parent.GetComponent<HandCurveLayout>();

        Vector3 basePos = _baseRt.position;
        bool lifted = _baseCard.IsHovered && !_baseCard.IsDragging && (_layout == null || !_layout.IsDragging);
        Vector3 target = lifted ? basePos + _baseRt.TransformVector(new Vector3(0f, hoverLift, 0f)) : basePos;
        _rt.position = Vector3.Lerp(_rt.position, target, posT);

        Vector3 velocity = (basePos - _lastBasePos) / dt;
        _lastBasePos = basePos;

        _tiltZ = Mathf.Lerp(_tiltZ, Mathf.Clamp(-velocity.x * tiltStrength, -maxTiltDeg, maxTiltDeg), tiltT);
        _tiltX = Mathf.Lerp(_tiltX, Mathf.Clamp(velocity.y * tiltStrength, -maxTiltDeg, maxTiltDeg), tiltT);
        _rt.rotation = Quaternion.Slerp(_rt.rotation, _baseRt.rotation * Quaternion.Euler(_tiltX, 0f, _tiltZ), rotT);

        bool dragging = _baseCard.IsDragging;
        _currentScale = Vector3.Lerp(_currentScale, Vector3.one * (dragging ? dragScale : 1f), scaleT);
        _rt.localScale = _currentScale;

        UpdateShadow(dt, dragging);
    }

    void UpdateShadow(float dt, bool dragging)
    {
        if (shadowRect == null || shadowGroup == null) return;

        float fadeT = 1f - Mathf.Exp(-shadowFadeSharpness * dt);
        float moveT = 1f - Mathf.Exp(-shadowMoveSharpness * dt);

        _shadowAlpha = Mathf.Lerp(_shadowAlpha, dragging ? shadowMaxAlpha : 0.12f, fadeT);
        shadowGroup.alpha = _shadowAlpha;

        var targetPos = new Vector2(-_tiltZ * shadowTiltXOffset, dragging ? shadowDragYOffset : shadowIdleYOffset);
        _shadowPos = Vector2.Lerp(_shadowPos, targetPos, moveT);
        shadowRect.anchoredPosition = _shadowPos;

        _shadowScale = Vector3.Lerp(_shadowScale, Vector3.one * (dragging ? shadowDragScale : shadowIdleScale), moveT);
        shadowRect.localScale = _shadowScale;
    }
}
