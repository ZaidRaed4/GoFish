using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoFish.Core;

// Flies each drawn card from the deck to its player, one draw at a time
public class CardDrawAnimator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] GameBootstrap bootstrap;
    [SerializeField] CardSpriteLibrary spriteLibrary;

    [Header("Fly FX")]
    [SerializeField] DealCardFly flyPrefab;
    [SerializeField] RectTransform flyLayer;

    [Header("Points")]
    [SerializeField] RectTransform deckPoint;
    [SerializeField] TableSeats seats;

    [Header("Your Hand (for exact slot landing)")]
    [SerializeField] HandSlotsBuilder handSlotsBuilder;
    [SerializeField] PlayerHandView playerHandView;

    [Header("Tuning")]
    [SerializeField] float flyDuration = 0.28f;

    [Tooltip("Longest wait for the turn overlay to reveal the answer this card follows")]
    [SerializeField] float maxWaitForAnswer = 6f;

    readonly Queue<GameEvent> _pending = new Queue<GameEvent>();
    int _nextEvent;
    bool _busy;
    AnswerGate _answerGate;

    public bool IsBusy => _busy || _pending.Count > 0;

    void Awake()
    {
        if (bootstrap == null) bootstrap = FindFirstObjectByType<GameBootstrap>();
        if (seats == null) seats = FindFirstObjectByType<TableSeats>();
        _answerGate = new AnswerGate(FindFirstObjectByType<TurnOverlayAnimator>(), maxWaitForAnswer);
    }

    void Update()
    {
        if (bootstrap == null || bootstrap.Engine == null) return;

        var events = bootstrap.Engine.Events;
        while (_nextEvent < events.Count)
        {
            var ev = events[_nextEvent++];
            if (ev.Type == GameEventType.Draw) _pending.Enqueue(ev);
        }

        if (_busy || _pending.Count == 0 || _answerGate.IsClosedFor(_pending.Peek())) return;

        var draw = _pending.Dequeue();
        StartCoroutine(AnimateDraw(draw.ActorId, draw.Card));
    }

    IEnumerator AnimateDraw(int playerId, Card card)
    {
        _busy = true;

        var to = seats.CardPoint(playerId);

        int index = Viewer.Is(playerId) ? bootstrap.State.GetPlayer(playerId).Hand.IndexOf(card) : -1;
        if (index >= 0 && playerHandView != null)
        {
            if (handSlotsBuilder != null) handSlotsBuilder.EnsureSlots(index + 1);
            var slot = playerHandView != null ? playerHandView.GetSlot(index) : null;
            if (slot != null) to = slot;
        }

        Sfx.Play(SfxId.Draw);
        var fly = Instantiate(flyPrefab, flyLayer);
        fly.SetSprite(Viewer.Is(playerId) ? spriteLibrary.GetFace(card) : spriteLibrary.cardBack);
        yield return fly.Fly(deckPoint, to, flyLayer, flyDuration);

        if (Viewer.Is(playerId) && playerHandView != null)
            playerHandView.RenderFromState(bootstrap.State);
        if (bootstrap.presenter != null)
            bootstrap.presenter.Render();

        _busy = false;
    }

}
