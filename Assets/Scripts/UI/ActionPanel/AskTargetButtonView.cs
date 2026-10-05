using UnityEngine;
using UnityEngine.UI;
using GoFish.Core;

public class AskTargetButtonView : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] GameBootstrap bootstrap;
    [SerializeField] PlayerActionPanel actionPanel;

    [Header("Target Player Id (Core)")]
    [Tooltip("Bot 1 (right), 2 (centre) or 3 (left)")]
    [SerializeField] int targetPlayerId;

    [Header("Optional")]
    [SerializeField] Button button;
    [SerializeField] GameObject selectedGlow;

    CanvasGroup _cg;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();

        if (bootstrap == null) bootstrap = FindFirstObjectByType<GameBootstrap>();
        if (actionPanel == null) actionPanel = FindFirstObjectByType<PlayerActionPanel>();

        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        if (actionPanel != null) actionPanel.SelectTarget(targetPlayerId);
    }

    void Update()
    {
        bool inPhase = bootstrap != null && bootstrap.Engine != null
                       && Viewer.Is(bootstrap.State.CurrentTurnPlayerId)
                       && bootstrap.Engine.Phase == TurnPhase.ChooseTargetRank;

        bool overlayBusy = actionPanel != null && actionPanel.overlayAnimator != null && actionPanel.overlayAnimator.IsBusy;
        bool allowClick = inPhase && !overlayBusy && actionPanel != null && actionPanel.SelectedTargetId < 0;

        SetVisible(inPhase, allowClick);

        if (selectedGlow != null)
            selectedGlow.SetActive(inPhase && actionPanel != null && actionPanel.SelectedTargetId == targetPlayerId);
    }

    void SetVisible(bool show, bool interactable)
    {
        _cg.alpha = show ? (interactable ? 1f : 0.35f) : 0f;
        _cg.interactable = interactable;
        _cg.blocksRaycasts = interactable;
    }
}
