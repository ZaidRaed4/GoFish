using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoFish.Core;

public class TurnOverlayAnimator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] GameBootstrap bootstrap;
    [SerializeField] PlayerActionPanel actionPanel;
    [SerializeField] TurnOverlayView view;

    [Header("Timings (game time: they stop while paused)")]
    [SerializeField] float fadeIn = 0.18f;
    [SerializeField] float fadeOut = 0.18f;
    [Tooltip("Portraits sliding in during your intro")]
    [SerializeField] float slideInDuration = 0.28f;
    [Tooltip("Header moving up to the top of the board")]
    [SerializeField] float liftDuration = 0.24f;
    [SerializeField] float holdAfterIntro = 0.15f;

    [Tooltip("Your own actions: pause before the answer is revealed")]
    [SerializeField] float flashDelay = 0.15f;
    [Tooltip("Your own actions: how long the answer stays")]
    [SerializeField] float flashHold = 0.55f;

    [Header("Pacing")]
    [Tooltip("Your own intro (after you pick a target) plays at this fraction of its length.")]
    [SerializeField, Range(0.1f, 1f)] float viewerIntroSpeed = 0.4f;

    [Header("Bots' beats: who, then what, then the answer (a tap on the board skips ahead)")]
    [Tooltip("Bots: the header (who asks whom) on its own before the question")]
    [SerializeField] float othersWhoHold = 0.6f;
    [Tooltip("Bots: how long the asked rank shows before the answer")]
    [SerializeField] float othersQuestionHold = 1.5f;
    [Tooltip("Bots: how long the ask's answer stays on screen, so it can be read and remembered")]
    [SerializeField] float othersAnswerHold = 1.5f;
    [Tooltip("Bots: how long a count or suit guess shows before the answer")]
    [SerializeField] float othersGuessHold = 1.2f;
    [Tooltip("Bots: how long a count or suit answer stays on screen")]
    [SerializeField] float othersGuessAnswerHold = 1.3f;
    [Tooltip("The value box pops in from this scale to draw the eye")]
    [SerializeField] float valuePopFrom = 0.6f;
    [SerializeField] float valuePopDuration = 0.18f;

    [Header("Behavior")]
    [SerializeField] bool blockInputWhileShowing = true;

    public bool IsBusy => _routine != null || _pending.Count > 0;

    readonly Queue<GameEvent> _pending = new Queue<GameEvent>();
    int _nextEvent;
    GameEvent _current;
    bool _currentRevealed;
    bool _skip;
    Coroutine _routine;

    int _lastTurnPlayerId = -1;
    bool _viewerTurnPending;
    int _askerId = -1;
    int _targetId = -1;
    bool _hasTargetSelection;

    TurnPhase _boardPhase;
    int _boardTargetId = -1;
    bool _boardValid;

    public int FirstUnrevealedIndex
    {
        get
        {
            if (_current != null && !_currentRevealed) return _current.Index;
            if (_pending.Count > 0) return _pending.Peek().Index;

            var engine = bootstrap != null ? bootstrap.Engine : null;
            if (engine != null)
                for (int i = _nextEvent; i < engine.Events.Count; i++)
                    if (IsOverlayEvent(engine.Events[i])) return i;
            return int.MaxValue;
        }
    }

    public int ShowingActorId
    {
        get
        {
            if (_routine != null && _current != null) return _current.ActorId;
            if (_pending.Count > 0) return _pending.Peek().ActorId;

            var engine = bootstrap != null ? bootstrap.Engine : null;
            if (engine != null)
                for (int i = _nextEvent; i < engine.Events.Count; i++)
                    if (IsOverlayEvent(engine.Events[i])) return engine.Events[i].ActorId;
            return -1;
        }
    }

    static bool IsOverlayEvent(GameEvent ev) =>
        ev.Type == GameEventType.Ask || ev.Type == GameEventType.Count || ev.Type == GameEventType.Suits;

    void Awake()
    {
        if (bootstrap == null) bootstrap = FindFirstObjectByType<GameBootstrap>();
        if (actionPanel == null) actionPanel = FindFirstObjectByType<PlayerActionPanel>();
        if (view == null) view = FindFirstObjectByType<TurnOverlayView>(FindObjectsInactive.Include);
        if (view != null) view.Init(this);
    }

    public void Skip()
    {
        if (_routine != null && _current != null && _current.ActorId != Viewer.Id) _skip = true;
    }

    public void OnTargetSelected(int askerId, int targetId)
    {
        if (bootstrap == null || bootstrap.Engine == null || view == null) return;

        _askerId = askerId;
        _targetId = targetId;
        _hasTargetSelection = true;
        _boardValid = false;

        _current = null;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(IntroAndStay());
    }

    void Update()
    {
        if (bootstrap == null || bootstrap.State == null || bootstrap.Engine == null) return;

        var s = bootstrap.State;
        var e = bootstrap.Engine;

        var events = e.Events;
        while (_nextEvent < events.Count)
        {
            var ev = events[_nextEvent++];
            if (IsOverlayEvent(ev)) _pending.Enqueue(ev);
        }

        if (s.CurrentTurnPlayerId != _lastTurnPlayerId)
        {
            _lastTurnPlayerId = s.CurrentTurnPlayerId;
            _viewerTurnPending = _lastTurnPlayerId == Viewer.Id;
        }

        if (_routine != null) return;

        if (_pending.Count > 0)
        {
            PlayEvent(s, _pending.Dequeue());
            return;
        }

        if (_viewerTurnPending)
        {
            _viewerTurnPending = false;
            Sfx.Play(SfxId.YourTurn);

            _hasTargetSelection = false;
            _askerId = -1;
            _targetId = -1;
            _boardValid = false;

            if (actionPanel != null) actionPanel.ClearTargetSelection();
            if (view != null) view.Hide();
            return;
        }

        if (s.CurrentTurnPlayerId != Viewer.Id) return;

        if (e.Phase == TurnPhase.ChooseTargetRank && !_hasTargetSelection)
        {
            if (view != null) view.SetVisibleInstant(false, false);
            return;
        }

        if (!_hasTargetSelection) return;
        if (_boardValid && _boardPhase == e.Phase && _boardTargetId == _targetId) return;

        _boardPhase = e.Phase;
        _boardTargetId = _targetId;
        _boardValid = true;

        ShowBoard(s);
        view.SetHeaderOnly(false);

        switch (e.Phase)
        {
            case TurnPhase.ChooseTargetRank: SetBodyLabelAsk(s, _askerId); break;
            case TurnPhase.GuessCount: SetBodyLabelCount(s, _askerId); break;
            case TurnPhase.GuessSuits: SetBodyLabelSuit(s, _askerId); break;
            default: return;
        }
        view.SetValue("", TurnOverlayView.ValueState.Neutral);
    }

    void PlayEvent(GameState s, GameEvent ev)
    {
        _current = ev;
        _currentRevealed = false;
        _skip = false;
        _boardValid = false;

        _askerId = ev.ActorId;
        _targetId = ev.TargetId;
        _hasTargetSelection = true;

        IEnumerator sequence = ev.Type switch
        {
            GameEventType.Ask => ev.ActorId == Viewer.Id ? ShowYourAsk(s, ev) : ShowBotAsk(s, ev),
            GameEventType.Count => ShowCount(s, ev),
            _ => ShowSuits(s, ev)
        };
        _routine = StartCoroutine(sequence);
    }

    IEnumerator IntroAndStay()
    {
        view.PrepareIntro(blockInputWhileShowing);
        ShowPlayers(bootstrap.State);

        float k = viewerIntroSpeed;
        yield return view.PlayIntro(fadeIn * k, slideInDuration * k, 0.18f * k, holdAfterIntro * k, liftDuration * k);
        _routine = null;
    }

    IEnumerator ShowBotAsk(GameState s, GameEvent ev)
    {
        ShowBoard(s);
        view.SnapHeaderToTop();

        view.SetHeaderOnly(true);
        yield return view.Fade(0f, 1f, fadeIn);
        yield return Hold(othersWhoHold);

        view.SetHeaderOnly(false);
        SetBodyLabelAsk(s, _askerId);
        string rank = Card.RankShort(ev.Rank);
        view.SetValue(rank, TurnOverlayView.ValueState.Neutral);
        yield return view.PopValue(valuePopFrom, valuePopDuration);
        yield return Hold(othersQuestionHold);

        Reveal(rank, ev.Success, ev.Success ? "YES" : "NO - GO FISH");
        yield return Hold(othersAnswerHold);

        if (!ev.Success)
            yield return view.FadeOutAndHide(fadeOut);

        _routine = null;
    }

    IEnumerator ShowYourAsk(GameState s, GameEvent ev)
    {
        ShowBoard(s);

        if (!view.HeaderIsTop) view.SnapHeaderToTop();

        view.SetHeaderOnly(false);
        SetBodyLabelAsk(s, _askerId);

        string rank = Card.RankShort(ev.Rank);
        view.SetValue(rank, TurnOverlayView.ValueState.Neutral);
        yield return new WaitForSeconds(flashDelay);

        Reveal(rank, ev.Success, ev.Success ? "YES" : "NO - GO FISH");
        yield return new WaitForSeconds(flashHold);

        if (!ev.Success)
        {
            _hasTargetSelection = false;
            yield return view.FadeOutAndHide(fadeOut);
        }

        _routine = null;
    }

    IEnumerator ShowCount(GameState s, GameEvent ev)
    {
        ShowBoard(s);
        view.SetHeaderOnly(false);
        SetBodyLabelCount(s, _askerId);

        string guess = ev.Guess.ToString();
        view.SetValue(guess, TurnOverlayView.ValueState.Neutral);
        if (_askerId != Viewer.Id) yield return view.PopValue(valuePopFrom, valuePopDuration);
        yield return Hold(QuestionHold(_askerId));

        Reveal(guess, ev.Success, ev.Success ? "RIGHT" : "WRONG");
        yield return Hold(AnswerHold(_askerId));

        if (!ev.Success)
        {
            if (_askerId == Viewer.Id) _hasTargetSelection = false;
            yield return view.FadeOutAndHide(fadeOut);
        }

        _routine = null;
    }

    IEnumerator ShowSuits(GameState s, GameEvent ev)
    {
        ShowBoard(s);
        view.SetHeaderOnly(false);
        SetBodyLabelSuit(s, _askerId);

        // A forced result shows the only possible suits; it always completes a book, so nothing is given away
        string suits = SuitsToText(ev.Suits);
        view.SetValue(suits, TurnOverlayView.ValueState.Neutral);
        if (_askerId != Viewer.Id) yield return view.PopValue(valuePopFrom, valuePopDuration);
        yield return Hold(QuestionHold(_askerId));

        Reveal(suits, ev.Success, ev.Forced ? "FORCED" : ev.Success ? "RIGHT" : "WRONG");
        yield return Hold(AnswerHold(_askerId));

        if (_askerId == Viewer.Id) _hasTargetSelection = false;
        yield return view.FadeOutAndHide(fadeOut);

        _routine = null;
    }

    float QuestionHold(int askerId) => askerId == Viewer.Id ? flashDelay : othersGuessHold;
    float AnswerHold(int askerId) => askerId == Viewer.Id ? flashHold : othersGuessAnswerHold;

    IEnumerator Hold(float seconds)
    {
        _skip = false;
        for (float t = 0f; t < seconds && !_skip; t += Time.deltaTime)
            yield return null;
        _skip = false;
    }

    void Reveal(string value, bool ok, string word)
    {
        var state = ok ? TurnOverlayView.ValueState.Correct : TurnOverlayView.ValueState.Wrong;
        view.SetValue(value, state);
        view.SetAnswer(word, state);
        Sfx.Play(ok ? SfxId.Yes : SfxId.No);
        _currentRevealed = true;
    }

    void ShowBoard(GameState s)
    {
        view.ShowAtTop(blockInputWhileShowing);
        ShowPlayers(s);
    }

    void ShowPlayers(GameState s)
    {
        view.SetHeaderNames(s.GetPlayer(_askerId).Name, s.GetPlayer(_targetId).Name);
        view.SetHeaderPortraits(_askerId, _targetId);
    }

    void SetBodyLabelAsk(GameState s, int askerId)
        => view.SetLeftLabel(askerId == Viewer.Id ? "YOU ASK FOR" : $"{s.GetPlayer(askerId).Name} ASKS FOR");

    void SetBodyLabelCount(GameState s, int askerId)
        => view.SetLeftLabel(askerId == Viewer.Id ? "YOUR COUNT GUESS" : $"{s.GetPlayer(askerId).Name} COUNT GUESS");

    void SetBodyLabelSuit(GameState s, int askerId)
        => view.SetLeftLabel(askerId == Viewer.Id ? "YOUR SUIT GUESS" : $"{s.GetPlayer(askerId).Name} SUIT GUESS");

    static string SuitsToText(IEnumerable<Suit> suits)
    {
        if (suits == null) return "";
        var text = "";
        foreach (var suit in suits)
            text += SuitText.Icon(suit);
        return text;
    }
}
