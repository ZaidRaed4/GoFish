using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GoFish.Core;

public class GameUIController : MonoBehaviour
{
    [Header("Match Setup")]
    [Range(3, 4)] public int playerCount = 3;
    public int seed = 123;

    [Header("UI Refs")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI phaseText;
    public TextMeshProUGUI handText;

    public TMP_Dropdown targetDropdown;
    public TMP_Dropdown rankDropdown;
    public Button askButton;

    [Header("Count Guess UI")]
    public GameObject countPanel;
    public TMP_Dropdown countDropdown;
    public Button submitCountButton;

    [Header("Suit Guess UI")]
    public GameObject suitPanel;
    public TMP_Dropdown suitSubsetDropdown;
    public Button submitSuitsButton;
    public TextMeshProUGUI candidateInfoText; // optional (can be null)

    public TextMeshProUGUI logText;
    public ScrollRect logScroll;

    [Header("Debug")]
    public bool debugShowAllHands = true;
    public TextMeshProUGUI allHandsText;

    private GameState _state;
    private GameEngine _engine;

    private int _lastLogIndex = 0;

    // Dropdown mappings (because dropdown indices won’t equal ids/ranks)
    private readonly List<int> _targetIdByOption = new List<int>(4);
    private readonly List<Rank> _rankByOption = new List<Rank>(13);

    // For suit subset dropdown: each option corresponds to a list of suits to submit
    private readonly List<List<Suit>> _suitSubsetByOption = new List<List<Suit>>(16);

    // Cache to avoid rebuilding dropdowns every frame
    private int _cachedTurnPlayerId = -1;
    private TurnPhase _cachedPhase = (TurnPhase)255;
    private string _cachedRankKey = "";

    void Start()
    {
        // Create players
        var names = new List<string>();
        for (int i = 0; i < playerCount; i++)
            names.Add(i == 0 ? "You" : $"Bot {i}");

        _state = new GameState(names, seed: seed, startingPlayerId: 0);
        _state.DealInitial(4);
        _engine = new GameEngine(_state);

        // Hook buttons
        askButton.onClick.AddListener(OnAskClicked);
        submitCountButton.onClick.AddListener(OnSubmitCountClicked);
        submitSuitsButton.onClick.AddListener(OnSubmitSuitsClicked);

        BuildCountDropdown(); // static 1..4
        ForceRebuildAllDropdowns();
        RefreshUI();
        AppendNewLogs();
    }

    void Update()
    {
        if (_engine == null) return;

        // Rebuild dropdowns if turn player or phase changed
        if (_cachedTurnPlayerId != _state.CurrentTurnPlayerId || _cachedPhase != _engine.Phase)
        {
            ForceRebuildAllDropdowns();
        }
        else
        {
            // If same turn+phase, ranks can still change after draw/transfer,
            // so we update ranks if the set of ranks changed.
            TryRefreshRankDropdownIfChanged();
        }

        RefreshUI();
        AppendNewLogs();
    }

    void ForceRebuildAllDropdowns()
    {
        _cachedTurnPlayerId = _state.CurrentTurnPlayerId;
        _cachedPhase = _engine.Phase;

        BuildTargetDropdownForCurrentTurnPlayer();
        BuildRankDropdownForCurrentTurnPlayer();

        if (_engine.Phase == TurnPhase.GuessSuits)
            BuildSuitSubsetDropdown();
        else
            ClearSuitSubsetDropdown();
    }

    // ----------- Dropdown Builders -----------

    void BuildTargetDropdownForCurrentTurnPlayer()
    {
        targetDropdown.ClearOptions();
        _targetIdByOption.Clear();

        int actorId = _state.CurrentTurnPlayerId;

        var opts = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < _state.PlayerCount; i++)
        {
            if (i == actorId) continue; // cannot target self
            opts.Add(new TMP_Dropdown.OptionData(_state.GetPlayer(i).Name));
            _targetIdByOption.Add(i);
        }

        targetDropdown.AddOptions(opts);
        targetDropdown.value = 0;
        targetDropdown.RefreshShownValue();
    }

    void BuildRankDropdownForCurrentTurnPlayer()
    {
        rankDropdown.ClearOptions();
        _rankByOption.Clear();

        int actorId = _state.CurrentTurnPlayerId;
        var actor = _state.GetPlayer(actorId);

        var ranks = actor.Hand.GetDistinctRanks();

        var opts = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < ranks.Count; i++)
        {
            _rankByOption.Add(ranks[i]);
            opts.Add(new TMP_Dropdown.OptionData(Card.RankShort(ranks[i])));
        }

        rankDropdown.AddOptions(opts);
        rankDropdown.value = 0;
        rankDropdown.RefreshShownValue();

        // cache key for ranks
        _cachedRankKey = string.Join(",", ranks);
    }

    void TryRefreshRankDropdownIfChanged()
    {
        int actorId = _state.CurrentTurnPlayerId;
        var ranks = _state.GetPlayer(actorId).Hand.GetDistinctRanks();
        string key = string.Join(",", ranks);

        if (key != _cachedRankKey)
        {
            BuildRankDropdownForCurrentTurnPlayer();
        }
    }

    void BuildCountDropdown()
    {
        countDropdown.ClearOptions();
        var opts = new List<TMP_Dropdown.OptionData>
        {
            new TMP_Dropdown.OptionData("1"),
            new TMP_Dropdown.OptionData("2"),
            new TMP_Dropdown.OptionData("3"),
            new TMP_Dropdown.OptionData("4"),
        };
        countDropdown.AddOptions(opts);
        countDropdown.value = 0;
        countDropdown.RefreshShownValue();
    }

    void BuildSuitSubsetDropdown()
    {
        suitSubsetDropdown.ClearOptions();
        _suitSubsetByOption.Clear();

        var pool = _engine.CandidatePool;
        int k = _engine.RequiredCount;

        var opts = new List<TMP_Dropdown.OptionData>();

        // Generate all combinations of pool choose k
        var combos = GenerateSuitCombos(pool, k);
        for (int i = 0; i < combos.Count; i++)
        {
            var combo = combos[i];
            _suitSubsetByOption.Add(combo);

            string label = "{ " + string.Join(", ", combo.ConvertAll(Card.SuitSymbol)) + " }";
            opts.Add(new TMP_Dropdown.OptionData(label));
        }

        suitSubsetDropdown.AddOptions(opts);
        suitSubsetDropdown.value = 0;
        suitSubsetDropdown.RefreshShownValue();
    }

    void ClearSuitSubsetDropdown()
    {
        suitSubsetDropdown.ClearOptions();
        _suitSubsetByOption.Clear();
        suitSubsetDropdown.RefreshShownValue();
    }

    // ----------- Button Actions -----------

    void OnAskClicked()
    {
        if (_engine.Phase != TurnPhase.ChooseTargetRank) return;

        if (_targetIdByOption.Count == 0)
        {
            Debug.LogWarning("No valid targets.");
            return;
        }

        if (_rankByOption.Count == 0)
        {
            Debug.LogWarning("Current player has no ranks in hand (cannot ask).");
            return;
        }

        int targetId = _targetIdByOption[Mathf.Clamp(targetDropdown.value, 0, _targetIdByOption.Count - 1)];
        Rank selectedRank = _rankByOption[Mathf.Clamp(rankDropdown.value, 0, _rankByOption.Count - 1)];

        if (!_engine.TryAskRank(targetId, selectedRank, out var err))
            Debug.LogWarning(err);

        ForceRebuildAllDropdowns();
        RefreshUI();
        AppendNewLogs();
    }

    void OnSubmitCountClicked()
    {
        if (_engine.Phase != TurnPhase.GuessCount) return;

        int guess = countDropdown.value + 1; // options are "1..4" in order

        if (!_engine.TryGuessCount(guess, out var err))
            Debug.LogWarning(err);

        ForceRebuildAllDropdowns();
        RefreshUI();
        AppendNewLogs();
    }

    void OnSubmitSuitsClicked()
    {
        if (_engine.Phase != TurnPhase.GuessSuits) return;

        if (_suitSubsetByOption.Count == 0)
        {
            Debug.LogWarning("No suit subsets available (unexpected).");
            return;
        }

        int idx = Mathf.Clamp(suitSubsetDropdown.value, 0, _suitSubsetByOption.Count - 1);
        var suits = _suitSubsetByOption[idx];

        if (!_engine.TryGuessSuits(suits, out var err))
            Debug.LogWarning(err);

        ForceRebuildAllDropdowns();
        RefreshUI();
        AppendNewLogs();
    }

    // ----------- UI Refresh -----------

    void RefreshUI()
    {
        var current = _state.GetPlayer(_state.CurrentTurnPlayerId);

        turnText.text = $"Turn: {current.Name}";
        phaseText.text = $"Phase: {_engine.Phase}";

        // For testing: show the CURRENT TURN player's hand (so you can play as bots too)
        handText.text = $"{current.Name} Hand ({current.Hand.Count}): {FormatCards(current.Hand.Cards)}";

        // Panels
        countPanel.SetActive(_engine.Phase == TurnPhase.GuessCount);
        suitPanel.SetActive(_engine.Phase == TurnPhase.GuessSuits);

        // Ask button only when choosing
        askButton.interactable = (_engine.Phase == TurnPhase.ChooseTargetRank);

        // Candidate info (optional)
        if (candidateInfoText != null)
        {
            if (_engine.Phase == TurnPhase.GuessSuits && _engine.CandidatePool != null)
            {
                candidateInfoText.text =
                    $"Candidate Pool: {FormatSuits(_engine.CandidatePool)}\n" +
                    $"Need: {_engine.RequiredCount} suit(s)";
            }
            else
            {
                candidateInfoText.text = "";
            }
        }

        // Debug: ALL hands
        if (allHandsText != null)
        {
            if (debugShowAllHands)
            {
                var lines = new List<string>(_state.PlayerCount);
                for (int i = 0; i < _state.PlayerCount; i++)
                {
                    var p = _state.GetPlayer(i);
                    lines.Add($"{p.Name} | Books: {p.BooksCount} | Hand({p.Hand.Count}): {FormatCards(p.Hand.Cards)}");
                }
                allHandsText.text = "ALL HANDS (DEBUG)\n" + string.Join("\n", lines);
            }
            else
            {
                allHandsText.text = "";
            }
        }
    }

    void AppendNewLogs()
    {
        var log = _state.PublicLog;
        if (_lastLogIndex >= log.Count) return;

        bool wasNearBottom = true;
        if (logScroll != null)
            wasNearBottom = logScroll.verticalNormalizedPosition <= 0.02f;

        for (; _lastLogIndex < log.Count; _lastLogIndex++)
            logText.text += log[_lastLogIndex] + "\n";

        // Force layout rebuild so Content size updates
        Canvas.ForceUpdateCanvases();
        var content = logScroll != null ? logScroll.content : null;
        if (content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        // Auto-scroll only if user was already near bottom
        if (logScroll != null && wasNearBottom)
            logScroll.verticalNormalizedPosition = 0f;
    }

    // ----------- Helpers -----------

    static string FormatCards(IReadOnlyList<Card> cards)
    {
        var list = new List<string>(cards.Count);
        for (int i = 0; i < cards.Count; i++)
            list.Add(cards[i].ToString());
        list.Sort();
        return string.Join(" ", list);
    }

    static string FormatSuits(IReadOnlyList<Suit> suits)
    {
        if (suits == null) return "{ }";
        var list = new List<string>(suits.Count);
        for (int i = 0; i < suits.Count; i++)
            list.Add(Card.SuitSymbol(suits[i]));
        list.Sort();
        return "{ " + string.Join(", ", list) + " }";
    }

    // Generates all combinations of pool choose k (pool size max 4, so this is tiny)
    static List<List<Suit>> GenerateSuitCombos(IReadOnlyList<Suit> pool, int k)
    {
        var result = new List<List<Suit>>();
        if (pool == null) return result;
        if (k <= 0) { result.Add(new List<Suit>()); return result; }
        if (k > pool.Count) return result;

        void Dfs(int start, List<Suit> current)
        {
            if (current.Count == k)
            {
                result.Add(new List<Suit>(current));
                return;
            }

            for (int i = start; i < pool.Count; i++)
            {
                current.Add(pool[i]);
                Dfs(i + 1, current);
                current.RemoveAt(current.Count - 1);
            }
        }

        Dfs(0, new List<Suit>(k));
        return result;
    }
}
