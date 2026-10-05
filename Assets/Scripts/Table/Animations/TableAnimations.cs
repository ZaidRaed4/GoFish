using UnityEngine;
using GoFish.Core;

public class TableAnimations : MonoBehaviour
{
    [SerializeField] TurnOverlayAnimator overlay;
    [SerializeField] CardDrawAnimator draw;
    [SerializeField] CardTransferAnimator transfer;

    public bool IsBusy =>
        (overlay != null && overlay.IsBusy) || (draw != null && draw.IsBusy) || (transfer != null && transfer.IsBusy);

    public int ShowingActorId => overlay != null ? overlay.ShowingActorId : -1;

    public int ShownTurnPlayerId(GameState state) => ShowingActorId >= 0 ? ShowingActorId : state.CurrentTurnPlayerId;

    void Awake()
    {
        if (overlay == null) overlay = FindFirstObjectByType<TurnOverlayAnimator>();
        if (draw == null) draw = FindFirstObjectByType<CardDrawAnimator>();
        if (transfer == null) transfer = FindFirstObjectByType<CardTransferAnimator>();
    }
}
