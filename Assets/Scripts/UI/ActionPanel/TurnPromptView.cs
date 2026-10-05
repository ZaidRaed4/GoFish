using System.Text;
using TMPro;
using UnityEngine;
using GoFish.Core;

public class TurnPromptView : MonoBehaviour
{
    [SerializeField] GameBootstrap bootstrap;
    [SerializeField] PlayerActionPanel actionPanel;
    [SerializeField] TableAnimations animations;

    [Header("Top Text")]
    [SerializeField] TextMeshProUGUI turnInfoText;
    [SerializeField] TextMeshProUGUI promptText;

    [Header("Log")]
    [SerializeField] TextMeshProUGUI logText;
    [Tooltip("Lines kept in the log text")]
    [SerializeField] int maxLogLines = 30;

    const string PromptHighlight = "#C85A6E";

    static readonly string[] ThinkingTexts = { "Thinking.", "Thinking..", "Thinking..." };

    int _shownTurnId = -1;

    // The phase prompt is rebuilt only when what it says changes
    (TurnPhase phase, int target, int suits) _promptShown;
    bool _promptValid;

    int _logShownCount = -1;
    readonly StringBuilder _log = new();

    void Awake()
    {
        if (bootstrap == null) bootstrap = FindFirstObjectByType<GameBootstrap>();
        if (actionPanel == null) actionPanel = FindFirstObjectByType<PlayerActionPanel>();
        if (animations == null) animations = FindFirstObjectByType<TableAnimations>();
    }

    void Update()
    {
        if (bootstrap == null || bootstrap.State == null || bootstrap.Engine == null) return;
        var state = bootstrap.State;

        int shownTurn = animations != null ? animations.ShownTurnPlayerId(state) : state.CurrentTurnPlayerId;
        if (shownTurn != _shownTurnId)
        {
            _shownTurnId = shownTurn;
            if (turnInfoText) turnInfoText.text = "TURN: " + Viewer.DisplayName(state.GetPlayer(_shownTurnId));
        }

        UpdatePrompt(state, bootstrap.Engine, shownTurn);
        UpdateLog(state);
    }

    public void ShowMessage(string text)
    {
        if (promptText) promptText.text = text;
    }

    void UpdatePrompt(GameState state, GameEngine engine, int shownTurn)
    {
        if (promptText == null) return;

        bool caughtUp = animations == null || animations.ShowingActorId < 0;
        if (engine.Phase == TurnPhase.GameOver && caughtUp) ShowFixedPrompt("Game over");
        else if (!Viewer.Is(shownTurn)) ShowFixedPrompt(ThinkingTexts[(int)(Time.time * 2.5f) % 3]);
        else if (caughtUp && engine.Phase != TurnPhase.GameOver) ShowPhasePrompt(state, engine);
    }

    void ShowFixedPrompt(string text)
    {
        promptText.text = text;
        _promptValid = false;
    }

    void ShowPhasePrompt(GameState state, GameEngine engine)
    {
        int target = actionPanel != null ? actionPanel.SelectedTargetId : -1;
        int suits = actionPanel != null ? actionPanel.SelectedSuitCount : 0;

        var shown = (engine.Phase, target, suits);
        if (_promptValid && shown == _promptShown) return;

        promptText.text = PromptForPhase(state, engine, target, suits);
        _promptShown = shown;
        _promptValid = true;
    }

    static string PromptForPhase(GameState state, GameEngine engine, int target, int suits)
    {
        switch (engine.Phase)
        {
            case TurnPhase.ChooseTargetRank:
                if (target < 0 || target == state.CurrentTurnPlayerId)
                    return "Choose a player to ask";
                return $"Ask <color={PromptHighlight}>{SpelledOut.Name(state.GetPlayer(target).Name)}</color> for a rank";

            case TurnPhase.GuessCount:
                return "Guess the count, then pick suits";

            case TurnPhase.GuessSuits:
                int left = engine.RequiredCount - suits;
                string count = SpelledOut.Number(left) + (suits > 0 ? " more" : "");
                return $"Pick {count} suit{(left == 1 ? "" : "s")}";

            default:
                return "";
        }
    }

    // Only while the log is visible, and only its last lines
    void UpdateLog(GameState state)
    {
        if (logText == null || !logText.isActiveAndEnabled) return;

        var log = state.PublicLog;
        if (_logShownCount == log.Count) return;
        _logShownCount = log.Count;

        _log.Clear();
        for (int i = Mathf.Max(0, log.Count - maxLogLines); i < log.Count; i++)
            _log.Append(log[i]).Append('\n');
        logText.text = _log.ToString();
    }
}
