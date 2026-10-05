using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GoFish.Core;

public class CardTransferAnimator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] GameBootstrap bootstrap;
    [SerializeField] CardSpriteLibrary spriteLibrary;

    [Header("Fly FX")]
    [SerializeField] DealCardFly flyPrefab;
    [SerializeField] RectTransform flyLayer;

    [Header("Seats")]
    [SerializeField] TableSeats seats;

    [Header("Your Hand")]
    [SerializeField] HandSlotsBuilder handSlotsBuilder;
    [SerializeField] PlayerHandView playerHandView;

    [Header("Your Visual Layer (IMPORTANT)")]
    [Tooltip("Where your hand's card pictures live; leaving cards fly out from there")]
    [SerializeField] RectTransform yourVisualLayer;

    [Header("Options")]
    [SerializeField] bool animateBotVsBot = true;

    [Header("Tuning")]
    [SerializeField] float flyDuration = 0.30f;
    [SerializeField] float perCardStagger = 0.04f;

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
            if (ev.Type == GameEventType.Transfer && ev.Cards.Count > 0) _pending.Enqueue(ev);
        }

        if (_busy || _pending.Count == 0 || _answerGate.IsClosedFor(_pending.Peek())) return;

        var transfer = _pending.Dequeue();
        int fromId = transfer.TargetId;
        int toId = transfer.ActorId;

        if (Viewer.Is(toId))
            StartCoroutine(AnimateToYou(fromId, transfer.Cards));
        else if (Viewer.Is(fromId))
            StartCoroutine(AnimateFromYou(toId, transfer.Cards));
        else if (animateBotVsBot)
            StartCoroutine(AnimateBetweenBots(fromId, toId, transfer.Cards.Count));
        else if (bootstrap.presenter != null)
            bootstrap.presenter.Render();
    }

    IEnumerator AnimateToYou(int fromPlayerId, IReadOnlyList<Card> cards)
    {
        _busy = true;
        Sfx.Play(SfxId.Transfer);

        var hand = bootstrap.State.GetPlayer(Viewer.Id).Hand;
        if (handSlotsBuilder != null)
            handSlotsBuilder.EnsureSlots(hand.Count);

        var fromPoint = seats.CardPoint(fromPlayerId);
        foreach (var card in cards)
        {
            RectTransform dest = seats.CardPoint(Viewer.Id);
            int index = hand.IndexOf(card);
            var slot = index >= 0 && playerHandView != null ? playerHandView.GetSlot(index) : null;
            if (slot != null) dest = slot;

            var fly = Instantiate(flyPrefab, flyLayer);
            fly.SetSprite(spriteLibrary.GetFace(card));
            StartCoroutine(fly.Fly(fromPoint, dest, flyLayer, flyDuration));

            if (perCardStagger > 0f)
                yield return new WaitForSeconds(perCardStagger);
        }

        yield return new WaitForSeconds(flyDuration + 0.02f);

        if (playerHandView != null)
            playerHandView.RenderFromState(bootstrap.State);
        if (bootstrap.presenter != null)
            bootstrap.presenter.Render();
        _busy = false;
    }

    IEnumerator AnimateFromYou(int toPlayerId, IReadOnlyList<Card> cardsLeaving)
    {
        _busy = true;
        Sfx.Play(SfxId.Transfer);

        var toPoint = seats.CardPoint(toPlayerId);
        var hidden = new List<(Image img, Color original)>(cardsLeaving.Count);

        foreach (var card in cardsLeaving)
        {
            var face = spriteLibrary.GetFace(card);

            // Fly a copy out of the card you can see, and hide the original meanwhile
            RectTransform from = seats.CardPoint(Viewer.Id);
            var source = FindImageInVisualLayer(face);
            if (source != null)
            {
                from = source.rectTransform;
                var color = source.color;
                hidden.Add((source, color));
                color.a = 0f;
                source.color = color;
            }

            var fly = Instantiate(flyPrefab, flyLayer);
            fly.SetSprite(face);
            StartCoroutine(fly.Fly(from, toPoint, flyLayer, flyDuration));

            if (perCardStagger > 0f)
                yield return new WaitForSeconds(perCardStagger);
        }

        yield return new WaitForSeconds(flyDuration + 0.02f);

        foreach (var (img, original) in hidden)
            if (img != null) img.color = original;

        if (playerHandView != null)
            playerHandView.RenderFromState(bootstrap.State);
        if (bootstrap.presenter != null)
            bootstrap.presenter.Render();
        _busy = false;
    }

    IEnumerator AnimateBetweenBots(int fromId, int toId, int count)
    {
        _busy = true;
        Sfx.Play(SfxId.Transfer);

        var from = seats.CardPoint(fromId);
        var to = seats.CardPoint(toId);
        for (int i = 0; i < count; i++)
        {
            var fly = Instantiate(flyPrefab, flyLayer);
            fly.SetSprite(spriteLibrary.cardBack);
            StartCoroutine(fly.Fly(from, to, flyLayer, flyDuration));

            if (perCardStagger > 0f)
                yield return new WaitForSeconds(perCardStagger);
        }

        yield return new WaitForSeconds(flyDuration + 0.02f);

        if (bootstrap.presenter != null)
            bootstrap.presenter.Render();
        _busy = false;
    }

    Image FindImageInVisualLayer(Sprite sprite)
    {
        if (yourVisualLayer == null || sprite == null) return null;

        foreach (var img in yourVisualLayer.GetComponentsInChildren<Image>(true))
            if (img.sprite == sprite && img.enabled && img.color.a > 0.01f)
                return img;
        return null;
    }
}
