using System.Collections.Generic;
using UnityEngine;

public class HandSlotsBuilder : MonoBehaviour
{
    [SerializeField] RectTransform slotsContainer;
    [SerializeField] RectTransform dragLayer;

    [SerializeField] RectTransform slotPrefab;
    [SerializeField] RectTransform cardPrefab;

    [SerializeField, Range(1, 40)] int slotCount = 7;

    readonly List<RectTransform> slots = new();
    readonly List<CardDragSnap> cards = new();

    public int SlotCount => slots.Count;
    public RectTransform GetSlot(int index) => index >= 0 && index < slots.Count ? slots[index] : null;
    public CardDragSnap GetCard(int index) => index >= 0 && index < cards.Count ? cards[index] : null;

    void Start()
    {
        if (slotsContainer != null && slotPrefab != null && cardPrefab != null)
            Rebuild();
    }

    [ContextMenu("Rebuild Slots")]
    public void Rebuild()
    {
        for (int i = slotsContainer.childCount - 1; i >= 0; i--)
            DestroyImmediate(slotsContainer.GetChild(i).gameObject);

        slots.Clear();
        cards.Clear();
        EnsureSlots(slotCount);
    }

    public void EnsureSlots(int desired)
    {
        if (slotsContainer == null || slotPrefab == null || cardPrefab == null) return;

        for (int i = slots.Count; i < desired; i++)
        {
            var slot = Instantiate(slotPrefab, slotsContainer);
            slot.name = $"Slot_{i}";
            slots.Add(slot);

            var card = Instantiate(cardPrefab, slot);
            card.name = $"Card_{i}";
            card.anchoredPosition = Vector2.zero;

            var drag = card.GetComponent<CardDragSnap>();
            if (drag != null)
            {
                drag.homeSlot = slot;
                drag.dragLayer = dragLayer;
            }
            cards.Add(drag);
        }
    }
}
