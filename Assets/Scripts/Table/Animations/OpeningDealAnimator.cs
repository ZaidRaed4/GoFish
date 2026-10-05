using System.Collections;
using UnityEngine;

public class OpeningDealAnimator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] GameBootstrap bootstrap;
    [SerializeField] CardSpriteLibrary spriteLibrary;

    [SerializeField] DealCardFly flyPrefab;
    [SerializeField] RectTransform flyLayer;

    [Header("Deal Points")]
    [SerializeField] RectTransform deckPoint;
    [SerializeField] TableSeats seats;

    [Header("Hand View")]
    [SerializeField] PlayerHandView playerHandView;

    [Header("Tuning")]
    [SerializeField] int cardsPerPlayer = 4;
    [SerializeField] float flyDuration = 0.45f;
    [SerializeField] float betweenRounds = 0.03f;

    [Header("Lay Areas Fade In")]
    [SerializeField] CanvasGroup[] layAreaGroups;
    [SerializeField] float layFadeDuration = 0.25f;

    [Header("Center HUD Fade In")]
    [SerializeField] CanvasGroup centerHudGroup;
    [SerializeField] float centerHudFadeDuration = 0.25f;

    IEnumerator Start()
    {
        Hide(layAreaGroups);
        Hide(centerHudGroup);

        // Slots and card visuals spawn on the first frame
        yield return null;

        if (bootstrap == null) bootstrap = FindFirstObjectByType<GameBootstrap>();
        if (seats == null) seats = FindFirstObjectByType<TableSeats>();
        if (bootstrap == null || bootstrap.State == null) yield break;

        if (playerHandView != null) playerHandView.ClearAll();

        yield return Deal();

        var state = bootstrap.State;
        for (int p = 0; p < state.PlayerCount; p++)
            state.ApplyAutoBooks(state.GetPlayer(p));

        if (playerHandView != null)
            playerHandView.RenderFromState(state);

        yield return FadeIn(layFadeDuration, layAreaGroups);

        bootstrap.CreateEngineAfterDeal();
        yield return FadeIn(centerHudFadeDuration, centerHudGroup);
    }

    IEnumerator Deal()
    {
        var state = bootstrap.State;

        for (int round = 0; round < cardsPerPlayer; round++)
        {
            for (int p = 0; p < state.PlayerCount; p++)
            {
                if (!state.Deck.TryDraw(out var card)) continue;
                state.GetPlayer(p).Hand.Add(card);

                Sfx.Play(SfxId.Deal);
                var fly = Instantiate(flyPrefab, flyLayer);
                fly.SetSprite(Viewer.Is(p) ? spriteLibrary.GetFace(card) : spriteLibrary.cardBack);

                int player = p;
                yield return fly.Fly(deckPoint, GetTargetForPlayer(p, round), flyLayer, flyDuration,
                    fadeOutDuration: 0.10f,
                    onArrive: () =>
                    {
                        if (Viewer.Is(player) && playerHandView != null)
                            playerHandView.RenderFromState(state);
                        if (bootstrap.presenter != null)
                            bootstrap.presenter.Render();
                    });

                if (betweenRounds > 0f)
                    yield return new WaitForSeconds(betweenRounds);
            }
        }
    }

    RectTransform GetTargetForPlayer(int playerId, int yourCardIndex)
    {
        var target = Viewer.Is(playerId) && playerHandView != null ? playerHandView.GetDealTarget(yourCardIndex) : null;
        return target != null ? target : seats.CardPoint(playerId);
    }

    static void Hide(params CanvasGroup[] groups)
    {
        if (groups == null) return;
        foreach (var g in groups)
        {
            if (!g) continue;
            g.alpha = 0f;
            g.interactable = false;
            g.blocksRaycasts = false;
        }
    }

    static IEnumerator FadeIn(float duration, params CanvasGroup[] groups)
    {
        if (groups == null) yield break;

        Hide(groups);
        yield return Tween.Run(duration, k =>
        {
            foreach (var g in groups)
                if (g) g.alpha = k;
        });

        foreach (var g in groups)
        {
            if (!g) continue;
            g.alpha = 1f;
            g.interactable = true;
            g.blocksRaycasts = true;
        }
    }
}
