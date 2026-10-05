using UnityEngine;
using GoFish.Core;

public class PlayerHandView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] RectTransform slotsContainer;
    [SerializeField] CardSpriteLibrary spriteLibrary;
    [Tooltip("Found on this object when empty")]
    [SerializeField] HandSlotsBuilder slotsBuilder;

    int SlotCount
    {
        get
        {
            if (slotsBuilder == null) slotsBuilder = GetComponent<HandSlotsBuilder>();
            if (slotsBuilder != null && slotsBuilder.SlotCount > 0) return slotsBuilder.SlotCount;
            return slotsContainer != null ? slotsContainer.childCount : 0;
        }
    }

    public RectTransform GetSlot(int index)
    {
        if (index < 0 || index >= SlotCount) return null;
        if (slotsBuilder != null && slotsBuilder.SlotCount > 0) return slotsBuilder.GetSlot(index);
        return (RectTransform)slotsContainer.GetChild(index);
    }

    GameObject CardBase(int index)
    {
        if (slotsBuilder != null && slotsBuilder.SlotCount > 0)
        {
            var card = slotsBuilder.GetCard(index);
            return card != null ? card.gameObject : null;
        }

        var slot = GetSlot(index);
        return slot != null && slot.childCount > 0 ? slot.GetChild(0).gameObject : null;
    }

    public void RenderFromState(GameState state)
    {
        if (state == null || spriteLibrary == null || slotsContainer == null) return;

        var hand = state.GetPlayer(Viewer.Id).Hand.Cards;
        for (int i = 0; i < SlotCount; i++)
        {
            var cardBase = CardBase(i);
            if (cardBase == null) continue;

            bool active = i < hand.Count;
            if (cardBase.activeSelf != active) cardBase.SetActive(active);
            if (!active) continue;

            var spawner = cardBase.GetComponent<CardVisualSpawner>();
            if (spawner != null && spawner.VisualInstance != null)
                spawner.VisualInstance.SetCardSprite(spriteLibrary.GetFace(hand[i]));
        }
    }

    public void ClearAll()
    {
        if (slotsContainer == null) return;

        for (int i = 0; i < SlotCount; i++)
        {
            var cardBase = CardBase(i);
            if (cardBase != null) cardBase.SetActive(false);
        }
    }

    public RectTransform GetDealTarget(int index)
    {
        if (slotsContainer == null || SlotCount == 0) return null;

        var slot = GetSlot(Mathf.Clamp(index, 0, SlotCount - 1));
        return slot.childCount > 0 ? (RectTransform)slot.GetChild(0) : slot;
    }
}
