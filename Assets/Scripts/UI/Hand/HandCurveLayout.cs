using System.Collections.Generic;
using UnityEngine;
using GoFish.Core;

public class HandCurveLayout : MonoBehaviour
{
    [SerializeField] RectTransform slotsContainer;
    [SerializeField] HandCurveProfile profile;
    [SerializeField] HandSlotsBuilder slotsBuilder;

    [Tooltip("Lay the hand out every frame; dragging and the live hand need it")]
    [SerializeField] bool liveUpdate = true;

    [Header("Hand")]
    [SerializeField] GameBootstrap bootstrap;
    [SerializeField] int playerId = 0;

    [Tooltip("Widest distance between the first and last card centres; spacing shrinks to fit big hands.")]
    [SerializeField] float maxSpan = 1150f;

    [Tooltip("How far a clicked card rises above the others (UI units, 4 = one art pixel).")]
    [SerializeField] float raisedOffset = 40f;

    readonly List<Card> _order = new();
    readonly HashSet<Card> _raised = new();
    readonly List<RectTransform> _slots = new();
    readonly List<int> _byPlace = new();
    readonly HashSet<CardDragSnap> _hooked = new();
    int[] _place = new int[0];
    int _activeCount;
    System.Comparison<int> _compareDraw, _compareSlots;

    CardDragSnap _dragged;
    Card _draggedCard;
    bool _draggedCardKnown;

    public bool IsDragging => _dragged != null;

    void Awake()
    {
        if (slotsContainer == null) slotsContainer = (RectTransform)transform;
        if (bootstrap == null) bootstrap = FindFirstObjectByType<GameBootstrap>();
        if (slotsBuilder == null) slotsBuilder = GetComponentInParent<HandSlotsBuilder>();
        CreateComparers();
    }

    void CreateComparers()
    {
        if (_compareDraw == null) _compareDraw = (a, b) => DrawKey(a).CompareTo(DrawKey(b));
        if (_compareSlots == null) _compareSlots = (a, b) => SlotKey(a).CompareTo(SlotKey(b));
    }

    void Update()
    {
        if (liveUpdate) Apply();
    }

    void LateUpdate()
    {
        SortVisuals();
    }

    [ContextMenu("Apply Curve Layout")]
    public void Apply()
    {
        if (profile == null || slotsContainer == null) return;

        CreateComparers();
        CollectSlots();
        int slotCount = _slots.Count;
        if (slotCount == 0) return;

        HookCards(slotCount);

        var hand = HandCards();
        int n = hand != null ? Mathf.Min(hand.Count, slotCount) : ActiveSlots();
        _activeCount = n;

        if (hand != null)
        {
            SyncOrder(hand);
            FollowDrag(hand, n);
        }
        BuildPlaces(hand, n, slotCount);
        SortSlots(slotCount);
        ReturnStrayCards(slotCount);
        if (n == 0) return;

        float spacing = Spacing(n);
        float mid = (n - 1) * 0.5f;

        for (int i = 0; i < slotCount; i++)
        {
            var slot = _slots[i];
            int p = _place[i];

            float t = (n == 1) ? 0.5f : (float)p / (n - 1);

            float x = profile.centerX + (p - mid) * spacing;

            float y = profile.yCurve.Evaluate(t) * profile.yAmplitude;
            if (hand != null && i < n && _raised.Contains(hand[i])) y += raisedOffset;

            float r = profile.rotCurve.Evaluate(t) * profile.rotAmplitude;

            slot.anchoredPosition = new Vector2(x, y);
            slot.localRotation = Quaternion.Euler(0f, 0f, r);
            slot.localScale = Vector3.one;
        }
    }

    IReadOnlyList<Card> HandCards() =>
        bootstrap != null && bootstrap.State != null ? bootstrap.State.GetPlayer(playerId).Hand.Cards : null;

    float Spacing(int n) => n > 1 ? Mathf.Min(profile.xSpacing, maxSpan / (n - 1)) : 0f;

    void CollectSlots()
    {
        _slots.Clear();
        if (slotsBuilder != null && slotsBuilder.SlotCount > 0)
        {
            for (int i = 0; i < slotsBuilder.SlotCount; i++) _slots.Add(slotsBuilder.GetSlot(i));
        }
        else
        {
            for (int i = 0; i < slotsContainer.childCount; i++) _slots.Add((RectTransform)slotsContainer.GetChild(i));
        }
    }

    CardDragSnap CardAt(int slotIndex)
    {
        if (slotsBuilder != null && slotsBuilder.SlotCount > 0) return slotsBuilder.GetCard(slotIndex);
        return slotIndex < _slots.Count ? _slots[slotIndex].GetComponentInChildren<CardDragSnap>(true) : null;
    }

    int SlotIndexOf(CardDragSnap card)
    {
        for (int i = 0; i < _slots.Count; i++)
            if (CardAt(i) == card) return i;
        return -1;
    }

    void HookCards(int slotCount)
    {
        for (int i = 0; i < slotCount; i++)
        {
            var card = CardAt(i);
            if (card == null || !_hooked.Add(card)) continue;
            card.Clicked += OnCardClicked;
            card.DragStarted += OnDragStarted;
            card.DragEnded += OnDragEnded;
        }
    }

    int ActiveSlots()
    {
        int n = 0;
        for (int i = 0; i < _slots.Count; i++)
        {
            var card = CardAt(i);
            if (card != null && card.gameObject.activeSelf) n++;
        }
        return n;
    }

    static int IndexIn(IReadOnlyList<Card> hand, int count, Card card)
    {
        for (int i = 0; i < count; i++)
            if (hand[i] == card) return i;
        return -1;
    }

    void SyncOrder(IReadOnlyList<Card> hand)
    {
        for (int k = _order.Count - 1; k >= 0; k--)
        {
            if (IndexIn(hand, hand.Count, _order[k]) >= 0) continue;
            _raised.Remove(_order[k]);
            _order.RemoveAt(k);
        }

        for (int i = 0; i < hand.Count; i++)
            if (!_order.Contains(hand[i])) _order.Add(hand[i]);
    }

    void FollowDrag(IReadOnlyList<Card> hand, int n)
    {
        if (_dragged == null || !_draggedCardKnown || n < 2) return;

        int from = _order.IndexOf(_draggedCard);
        if (from < 0 || from >= n) return;

        float spacing = Spacing(n);
        if (spacing <= 0f) return;

        float x = slotsContainer.InverseTransformPoint(_dragged.Rect.position).x;
        int to = Mathf.Clamp(Mathf.RoundToInt((x - profile.centerX) / spacing + (n - 1) * 0.5f), 0, n - 1);
        if (to == from) return;

        _order.RemoveAt(from);
        _order.Insert(to, _draggedCard);
    }

    void BuildPlaces(IReadOnlyList<Card> hand, int n, int slotCount)
    {
        if (_place.Length != slotCount) _place = new int[slotCount];

        for (int i = 0; i < slotCount; i++) _place[i] = i;

        if (hand != null)
        {
            int p = 0;
            for (int k = 0; k < _order.Count; k++)
            {
                int i = IndexIn(hand, n, _order[k]);
                if (i >= 0) _place[i] = p++;
            }
        }

        for (int i = n; i < slotCount; i++) _place[i] = Mathf.Max(n - 1, 0); // unused slots wait at the right end
    }

    void SortSlots(int slotCount)
    {
        _byPlace.Clear();
        for (int i = 0; i < slotCount; i++) _byPlace.Add(i);
        _byPlace.Sort(_compareSlots);

        for (int k = 0; k < _byPlace.Count; k++)
        {
            var slot = _slots[_byPlace[k]];
            if (slot.GetSiblingIndex() != k) slot.SetSiblingIndex(k);
        }
    }

    int SlotKey(int i) => i < _activeCount ? _place[i] : _activeCount + i;

    void ReturnStrayCards(int slotCount)
    {
        for (int i = 0; i < slotCount; i++)
        {
            var card = CardAt(i);
            if (card == null || card.Rect == null || card.IsDragging || card.IsSnapping || card.homeSlot == null) continue;
            if (card.Rect.parent != card.homeSlot) card.SnapToHome();
        }
    }

    int DrawKey(int i)
    {
        var card = CardAt(i);
        bool onTop = card != null && (card.IsDragging || card.IsSnapping);
        return onTop ? 1000 + _place[i] : _place[i];
    }

    void SortVisuals()
    {
        int n = Mathf.Min(_activeCount, _slots.Count);
        if (n == 0 || _place.Length < n) return;
        CreateComparers();

        _byPlace.Clear();
        for (int i = 0; i < n; i++) _byPlace.Add(i);
        _byPlace.Sort(_compareDraw);

        int k = 0;
        for (int j = 0; j < _byPlace.Count; j++)
        {
            var card = CardAt(_byPlace[j]);
            var spawner = card != null ? card.GetComponent<CardVisualSpawner>() : null;
            var visual = spawner != null ? spawner.VisualInstance : null;
            if (visual == null) continue;

            var t = visual.transform;
            if (t.GetSiblingIndex() != k) t.SetSiblingIndex(k);
            k++;
        }
    }

    void OnCardClicked(CardDragSnap card)
    {
        var hand = HandCards();
        int i = SlotIndexOf(card);
        if (hand == null || i < 0 || i >= hand.Count) return;

        if (!_raised.Remove(hand[i])) _raised.Add(hand[i]);
    }

    void OnDragStarted(CardDragSnap card)
    {
        var hand = HandCards();
        int i = SlotIndexOf(card);

        _dragged = card;
        _draggedCardKnown = hand != null && i >= 0 && i < hand.Count;
        if (_draggedCardKnown) _draggedCard = hand[i];
    }

    void OnDragEnded(CardDragSnap card)
    {
        if (_dragged != card) return;
        _dragged = null;
        _draggedCardKnown = false;
    }
}
