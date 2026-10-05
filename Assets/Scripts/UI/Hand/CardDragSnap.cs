using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class CardDragSnap : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler,
    IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Slot + Drag Layer")]
    public RectTransform homeSlot;
    [Tooltip("Parent while dragging")]
    public RectTransform dragLayer;

    [Header("Drag")]
    [SerializeField] float dragThreshold = 10f;

    [Header("Snap")]
    [SerializeField] float snapDuration = 0.12f;
    [SerializeField] bool snapUseUnscaledTime = true;

    public event Action<CardDragSnap> DragStarted;
    public event Action<CardDragSnap> DragEnded;
    public event Action<CardDragSnap> Clicked;

    RectTransform rt;
    Canvas canvas;
    CanvasGroup group;

    Vector2 pointerDownPos;
    Vector2 pointerOffsetLocal;

    bool draggingActive;
    bool hovered;
    Coroutine _snapRoutine;

    public RectTransform Rect => rt;
    public bool IsDragging => draggingActive;
    public bool IsHovered => hovered;
    public bool IsSnapping => _snapRoutine != null;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
    }

    void OnDisable()
    {
        hovered = false;
        _snapRoutine = null;

        if (draggingActive)
        {
            draggingActive = false;
            if (group != null) group.blocksRaycasts = true;
            DragEnded?.Invoke(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => hovered = true;
    public void OnPointerExit(PointerEventData eventData) => hovered = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownPos = eventData.position;
        CacheOffset(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!draggingActive)
            Clicked?.Invoke(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        CacheOffset(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        if (!draggingActive)
        {
            float moved = Vector2.Distance(pointerDownPos, eventData.position);
            if (moved < dragThreshold) return;

            draggingActive = true;

            group.blocksRaycasts = false;

            if (dragLayer != null)
            {
                if (_snapRoutine != null) { StopCoroutine(_snapRoutine); _snapRoutine = null; }
                rt.SetParent(dragLayer, worldPositionStays: true);
                CacheOffset(eventData);
            }

            DragStarted?.Invoke(this);
        }

        var parentRect = (RectTransform)rt.parent;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, eventData.position, eventData.pressEventCamera, out var localPoint))
        {
            rt.anchoredPosition = localPoint - pointerOffsetLocal;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!draggingActive) return;

        draggingActive = false;
        group.blocksRaycasts = true;

        SmoothSnapToHome();

        DragEnded?.Invoke(this);
    }

    void CacheOffset(PointerEventData eventData)
    {
        var parentRect = (RectTransform)rt.parent;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, eventData.position, eventData.pressEventCamera, out var pointerLocal))
        {
            pointerOffsetLocal = pointerLocal - rt.anchoredPosition;
        }
    }

    public void SnapToHome()
    {
        if (homeSlot == null) return;

        rt.SetParent(homeSlot, worldPositionStays: false);
        rt.anchoredPosition = Vector2.zero;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
    }

    void SmoothSnapToHome()
    {
        if (homeSlot == null) return;

        rt.SetParent(homeSlot, worldPositionStays: true);
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;

        if (_snapRoutine != null) StopCoroutine(_snapRoutine);
        _snapRoutine = StartCoroutine(SnapToAnchoredRoutine(Vector2.zero, snapDuration));
    }

    IEnumerator SnapToAnchoredRoutine(Vector2 target, float duration)
    {
        Vector2 start = rt.anchoredPosition;

        if (duration > 0f)
            yield return Tween.Run(duration, k => rt.anchoredPosition = Vector2.Lerp(start, target, k), snapUseUnscaledTime);

        rt.anchoredPosition = target;
        _snapRoutine = null;
    }
}
