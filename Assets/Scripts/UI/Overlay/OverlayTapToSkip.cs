using UnityEngine;
using UnityEngine.EventSystems;

public class OverlayTapToSkip : MonoBehaviour, IPointerClickHandler
{
    public TurnOverlayAnimator animator;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (animator != null) animator.Skip();
    }
}
