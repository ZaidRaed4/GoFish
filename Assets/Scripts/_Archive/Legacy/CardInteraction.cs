using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardInteraction : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{

    [Header("Home Slot")]
    public RectTransform homeSlot;
    public bool reparentToHomeSlotOnRelease = true;

    [Header("Drag")]
    [Tooltip("How far (pixels) the pointer must move before we consider it a drag (not a click).")]
    public float dragThresholdPixels = 12f;

    [Tooltip("If true, the card returns to its original anchored position when released.")]
    public bool snapBackOnRelease = true;

    [Tooltip("Smooth snap back animation time in seconds.")]
    public float snapBackTime = 0.10f;

    [Tooltip("Keep the card inside the canvas bounds while dragging.")]
    public bool keepInsideCanvas = true;

    [Header("Selection")]
    [Tooltip("If true, clicking (without dragging) toggles selected state.")]
    public bool toggleSelectOnClick = true;

    [Tooltip("Optional: a Selectable (Button) on the same object for visual states.")]
    public Selectable selectable;

    [Header("Optional Drag Layer (recommended later)")]
    [Tooltip("If set, the card will temporarily re-parent here while dragging (so it renders above everything).")]
    public RectTransform dragLayer;

    // Runtime
    RectTransform rt;
    Canvas canvas;
    CanvasGroup group;

    Vector2 pointerDownScreenPos;
    Vector2 pointerOffsetLocal;

    Vector2 startAnchoredPos;
    Transform startParent;

    bool isDragging;
    bool suppressClick;

    bool selected;

    public bool Selected => selected;

    void Awake()
    {
        rt = GetComponent<RectTransform>();

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            Debug.LogError("CardInteraction must be under a Canvas.");

        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();

        if (selectable == null)
            selectable = GetComponent<Selectable>();
    }

    // ---- Pointer ----
    public void OnPointerDown(PointerEventData eventData)
    {
        if (canvas == null) return;

        pointerDownScreenPos = eventData.position;
        suppressClick = false;

        startParent = rt.parent;


        CachePointerOffset(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Click only if we didn't drag past the threshold
        if (!suppressClick && !isDragging)
        {
            float moved = Vector2.Distance(pointerDownScreenPos, eventData.position);
            if (moved <= dragThresholdPixels)
            {
                OnClicked();
            }
        }
    }

    // ---- Drag ----
    public void OnBeginDrag(PointerEventData eventData)
    {
        // We don't instantly mark it as drag until it passes threshold in OnDrag,
        // but we should still prepare.
        CachePointerOffset(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        // Only start dragging after passing threshold
        if (!isDragging)
        {
            float moved = Vector2.Distance(pointerDownScreenPos, eventData.position);
            if (moved < dragThresholdPixels) return;

            isDragging = true;
            suppressClick = true;

            // Let raycasts go through while dragging (useful later for drop zones)
            group.blocksRaycasts = false;

            // Optional: bring to front using a drag layer
            if (dragLayer != null)
            {
                rt.SetParent(dragLayer, worldPositionStays: false);
            }
        }

        // Move with pointer while keeping the "grab point" stable
        var parentRect = (RectTransform)rt.parent;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint))
        {
            Vector2 target = localPoint - pointerOffsetLocal;

            if (keepInsideCanvas)
                target = ClampToCanvas((RectTransform)canvas.transform, target, parentRect);

            rt.anchoredPosition = target;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;
        group.blocksRaycasts = true;

        // Return parent back (if we used drag layer)
        if (dragLayer != null && startParent != null)
        {
            rt.SetParent(startParent, worldPositionStays: false);
        }

        if (snapBackOnRelease)
        {
            if (homeSlot == null)
            {
                // fallback: go back to original parent position
                StopAllCoroutines();
                StartCoroutine(SnapBackRoutine(rt.anchoredPosition));
                return;
            }

            // Optionally reparent to the slot first so anchoredPosition is in slot space
            if (reparentToHomeSlotOnRelease)
                rt.SetParent(homeSlot, worldPositionStays: false);

            StopAllCoroutines();
            StartCoroutine(SnapToSlotRoutine(homeSlot));
        }

    }

    // ---- Click / Selection ----
    void OnClicked()
    {
        if (toggleSelectOnClick)
        {
            SetSelected(!selected);
        }
        else
        {
            // If you want "click does something else", hook it here later
            SetSelected(true);
        }
    }

    public void SetSelected(bool value)
    {
        selected = value;

        // Use Selectable state if available
        if (selectable != null)
        {
            if (selected) selectable.Select();
            // If you want “unselect” visuals, we’ll do a custom highlight later.
        }

        // Quick visual feedback now (optional):
        // (If your card has an Image, you can tint it here)
        var img = GetComponent<Image>();
        if (img != null)
            img.color = selected ? new Color(1f, 0.92f, 0.5f, 1f) : Color.white;
    }

    // ---- Helpers ----
    void CachePointerOffset(PointerEventData eventData)
    {
        var parentRect = (RectTransform)rt.parent;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventData.pressEventCamera,
                out var pointerLocal))
        {
            pointerOffsetLocal = pointerLocal - rt.anchoredPosition;
        }
    }

    IEnumerator SnapBackRoutine(Vector2 target)
    {
        Vector2 start = rt.anchoredPosition;

        if (snapBackTime <= 0f)
        {
            rt.anchoredPosition = target;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / snapBackTime;
            rt.anchoredPosition = Vector2.Lerp(start, target, t);
            yield return null;
        }

        rt.anchoredPosition = target;
    }

    // Clamp anchored position so the card stays inside the canvas bounds
    Vector2 ClampToCanvas(RectTransform canvasRect, Vector2 desiredInParent, RectTransform parentRect)
    {
        // Convert "desired position in parent space" -> position in canvas space is messy if parent != canvas.
        // For now, we clamp only when parent == canvas (most common in your early layout).
        if (parentRect != canvasRect) return desiredInParent;

        var canvasSize = canvasRect.rect.size;
        var cardSize = rt.rect.size;

        float left = cardSize.x * rt.pivot.x;
        float right = cardSize.x * (1f - rt.pivot.x);
        float bottom = cardSize.y * rt.pivot.y;
        float top = cardSize.y * (1f - rt.pivot.y);

        float minX = -canvasSize.x * 0.5f + left;
        float maxX = canvasSize.x * 0.5f - right;
        float minY = -canvasSize.y * 0.5f + bottom;
        float maxY = canvasSize.y * 0.5f - top;

        desiredInParent.x = Mathf.Clamp(desiredInParent.x, minX, maxX);
        desiredInParent.y = Mathf.Clamp(desiredInParent.y, minY, maxY);
        return desiredInParent;
    }

    IEnumerator SnapToSlotRoutine(RectTransform slot)
    {
        // When parent is slot, home position is just (0,0) anchored
        Vector2 target = Vector2.zero;
        Vector2 start = rt.anchoredPosition;

        if (snapBackTime <= 0f)
        {
            rt.anchoredPosition = target;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / snapBackTime;
            rt.anchoredPosition = Vector2.Lerp(start, target, t);
            yield return null;
        }

        rt.anchoredPosition = target;
    }

}
