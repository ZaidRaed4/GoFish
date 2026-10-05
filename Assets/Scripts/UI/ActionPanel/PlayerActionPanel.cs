using System.Collections.Generic;
using UnityEngine;
using GoFish.Core;

public class PlayerActionPanel : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] GameBootstrap bootstrap;
    [SerializeField] TurnPromptView prompt;

    [Header("Rank Buttons UI")]
    [SerializeField] GameObject rankButtonsRow;
    [SerializeField] RectTransform rankButtonsContainer;
    [SerializeField] RankButtonItem rankButtonPrefab;

    [Header("Count Buttons UI")]
    [SerializeField] GameObject countButtonsRow;
    [SerializeField] RectTransform countButtonsContainer;
    [SerializeField] CountButtonItem countButtonPrefab;

    [Header("Suit Buttons UI")]
    [SerializeField] GameObject suitButtonsRow;
    [SerializeField] RectTransform suitButtonsContainer;
    [SerializeField] SuitButtonItem suitButtonPrefab;

    [Header("Overlay (optional)")]
    public TurnOverlayAnimator overlayAnimator;

    const string RedSuitColor = "#A3333D";
    const string BlackSuitColor = "#2A1519";

    GameState _state;
    GameEngine _engine;
    TablePresenter _presenter;

    int _selectedTargetId = -1;
    public int SelectedTargetId => _selectedTargetId;
    public int SelectedSuitCount => _selectedSuits.Count;

    readonly List<RankButtonItem> _rankButtons = new();
    readonly List<Rank> _shownRanks = new();
    readonly List<Rank> _heldRanks = new();
    int _shownRanksOwnerId = -1;
    readonly List<CountButtonItem> _countButtons = new();
    readonly List<SuitButtonItem> _suitButtons = new();
    readonly HashSet<Suit> _selectedSuits = new();

    TurnPhase _lastPhase;
    bool _hasLastPhase;
    int _lastActorId = -999;

    bool IsYourTurn => _state != null && Viewer.Is(_state.CurrentTurnPlayerId);

    void Awake()
    {
        if (bootstrap == null) bootstrap = FindFirstObjectByType<GameBootstrap>();
        if (prompt == null) prompt = FindFirstObjectByType<TurnPromptView>();
        SetRows(false, false, false);
    }

    void Start()
    {
        if (overlayAnimator == null) overlayAnimator = FindFirstObjectByType<TurnOverlayAnimator>();
        BuildCountButtons();
        BuildSuitButtons();
        SetRows(false, false, false);
    }

    void Update()
    {
        if (_engine == null)
        {
            if (bootstrap == null || bootstrap.State == null || bootstrap.Engine == null)
            {
                SetRows(false, false, false);
                return;
            }

            _state = bootstrap.State;
            _engine = bootstrap.Engine;
            _presenter = bootstrap.presenter;
        }

        if (_state.CurrentTurnPlayerId != _lastActorId)
        {
            _lastActorId = _state.CurrentTurnPlayerId;
            _selectedTargetId = -1;
            _selectedSuits.Clear();
            _shownRanksOwnerId = -1;
        }

        if (!_hasLastPhase || _engine.Phase != _lastPhase)
        {
            OnPhaseChanged(_engine.Phase);
            _lastPhase = _engine.Phase;
            _hasLastPhase = true;
        }

        RefreshUI();
    }

    public void ClearTargetSelection()
    {
        _selectedTargetId = -1;
        _selectedSuits.Clear();
        if (_engine != null) RefreshUI();
    }

    void OnPhaseChanged(TurnPhase phase)
    {
        if (phase == TurnPhase.GuessSuits) _selectedSuits.Clear();
        if (phase != TurnPhase.ChooseTargetRank) _selectedTargetId = -1;
        _shownRanksOwnerId = -1;
    }

    // Called by AskTargetButtonView
    public void SelectTarget(int playerId)
    {
        if (_engine == null || !IsYourTurn || _engine.Phase != TurnPhase.ChooseTargetRank) return;

        if (playerId == _state.CurrentTurnPlayerId)
        {
            ShowMessage("You can't target yourself.");
            return;
        }

        _selectedTargetId = playerId;
        Sfx.Play(SfxId.Click);

        if (overlayAnimator != null)
            overlayAnimator.OnTargetSelected(_state.CurrentTurnPlayerId, _selectedTargetId);

        RefreshUI();
    }

    // Called by RankButtonItem
    public void OnRankButtonPressed(Rank rank)
    {
        if (_engine == null || !IsYourTurn || _engine.Phase != TurnPhase.ChooseTargetRank) return;

        if (_selectedTargetId < 0)
        {
            ShowMessage("Pick a target first.");
            return;
        }

        Sfx.Play(SfxId.Click);
        if (!_engine.TryAskRank(_selectedTargetId, rank, out var error))
        {
            ShowError(error);
            return;
        }

        _selectedTargetId = -1;
        if (_presenter != null) _presenter.Render();
    }

    // Called by CountButtonItem
    public void OnCountButtonPressed(int count)
    {
        if (_engine == null || !IsYourTurn || _engine.Phase != TurnPhase.GuessCount) return;

        Sfx.Play(SfxId.Click);
        if (!_engine.TryGuessCount(count, out var error))
        {
            ShowError(error);
            return;
        }

        if (_presenter != null) _presenter.Render();
    }

    // Called by SuitButtonItem; the guess is sent as soon as enough suits are picked
    public void ToggleSuit(Suit suit)
    {
        if (_engine == null || !IsYourTurn || _engine.Phase != TurnPhase.GuessSuits) return;
        if (!HasSuit(_engine.CandidatePool, suit)) return;

        if (_selectedSuits.Remove(suit))
        {
            ApplyCandidatePoolToSuitButtons();
            return;
        }

        int required = _engine.RequiredCount;
        if (_selectedSuits.Count >= required) _selectedSuits.Clear();

        Sfx.Play(SfxId.Click);
        _selectedSuits.Add(suit);
        ApplyCandidatePoolToSuitButtons();

        if (_selectedSuits.Count == required)
            SubmitSuits();
    }

    void SubmitSuits()
    {
        if (!_engine.TryGuessSuits(new List<Suit>(_selectedSuits), out var error))
        {
            ShowError(error);
            return;
        }

        _selectedSuits.Clear();
        ApplyCandidatePoolToSuitButtons();
        if (_presenter != null) _presenter.Render();
    }

    void ShowError(string error)
    {
        Debug.LogWarning(error);
        ShowMessage(error);
    }

    void ShowMessage(string text)
    {
        if (prompt != null) prompt.ShowMessage(text);
    }

    void RefreshUI()
    {
        int actorId = _state.CurrentTurnPlayerId;
        bool yourTurn = Viewer.Is(actorId);
        var phase = _engine.Phase;

        bool overlayBusy = overlayAnimator != null && overlayAnimator.IsBusy;
        if (!yourTurn || overlayBusy)
        {
            SetRows(false, false, false);
            return;
        }

        bool choosing = phase == TurnPhase.ChooseTargetRank;
        SetRows(choosing && _selectedTargetId >= 0 && _selectedTargetId != actorId,
                phase == TurnPhase.GuessCount,
                phase == TurnPhase.GuessSuits);

        if (choosing) BuildRankButtons(actorId);
        else if (phase == TurnPhase.GuessCount) ShowPossibleCounts(actorId);
        else if (phase == TurnPhase.GuessSuits) ApplyCandidatePoolToSuitButtons();
    }

    void SetRows(bool ranks, bool counts, bool suits)
    {
        if (rankButtonsRow) rankButtonsRow.SetActive(ranks);
        if (countButtonsRow) countButtonsRow.SetActive(counts);
        if (suitButtonsRow) suitButtonsRow.SetActive(suits);
    }

    void BuildRankButtons(int actorId)
    {
        if (rankButtonsContainer == null || rankButtonPrefab == null) return;

        CollectRanks(_state.GetPlayer(actorId).Hand, _heldRanks);
        var ranks = _heldRanks;
        if (_shownRanksOwnerId == actorId && SameRanks(_shownRanks, ranks)) return;

        _shownRanksOwnerId = actorId;
        _shownRanks.Clear();
        _shownRanks.AddRange(ranks);

        while (_rankButtons.Count < ranks.Count)
            _rankButtons.Add(Instantiate(rankButtonPrefab, rankButtonsContainer));

        for (int i = 0; i < _rankButtons.Count; i++)
        {
            bool active = i < ranks.Count;
            _rankButtons[i].gameObject.SetActive(active);
            if (active) _rankButtons[i].Bind(this, ranks[i]);
        }
    }

    static void CollectRanks(Hand hand, List<Rank> into)
    {
        into.Clear();
        var cards = hand.Cards;
        for (int i = 0; i < cards.Count; i++)
        {
            var rank = cards[i].Rank;
            int at = 0;
            while (at < into.Count && into[at] < rank) at++;
            if (at == into.Count || into[at] != rank) into.Insert(at, rank);
        }
    }

    static bool SameRanks(List<Rank> a, List<Rank> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    void BuildCountButtons()
    {
        if (countButtonsContainer == null || countButtonPrefab == null || _countButtons.Count > 0) return;

        for (int i = 1; i <= Rules.CardsPerRank; i++)
        {
            var item = Instantiate(countButtonPrefab, countButtonsContainer);
            item.Bind(this, i);
            _countButtons.Add(item);
        }
    }

    // Holding k cards of the asked rank means the target has at most 4 - k
    void ShowPossibleCounts(int actorId)
    {
        int maxPossible = Rules.CardsPerRank - _state.GetPlayer(actorId).Hand.CountOfRank(_engine.CurrentRank);
        foreach (var button in _countButtons)
        {
            bool possible = button.Count <= maxPossible;
            if (button.gameObject.activeSelf != possible)
                button.gameObject.SetActive(possible);
        }
    }

    void BuildSuitButtons()
    {
        if (suitButtonsContainer == null || suitButtonPrefab == null || _suitButtons.Count > 0) return;

        foreach (var suit in new[] { Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades })
        {
            var item = Instantiate(suitButtonPrefab, suitButtonsContainer, false);
            item.Bind(this, suit, $"<color={(SuitText.IsRed(suit) ? RedSuitColor : BlackSuitColor)}>{SuitText.Icon(suit)}</color>");
            _suitButtons.Add(item);
        }
    }

    void ApplyCandidatePoolToSuitButtons()
    {
        var pool = _engine.CandidatePool;
        if (pool == null) return;

        foreach (var button in _suitButtons)
        {
            bool allowed = HasSuit(pool, button.SuitValue);
            button.SetInteractable(allowed);

            if (!allowed) _selectedSuits.Remove(button.SuitValue);
            button.SetSelected(_selectedSuits.Contains(button.SuitValue));
        }
    }

    static bool HasSuit(IReadOnlyList<Suit> suits, Suit suit)
    {
        if (suits == null) return false;
        for (int i = 0; i < suits.Count; i++)
            if (suits[i] == suit) return true;
        return false;
    }
}
