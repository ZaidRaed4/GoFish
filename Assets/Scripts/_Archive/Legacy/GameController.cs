// Assets/Scripts/GameController.cs
using System.Collections.Generic;
using UnityEngine;
using GoFish.Core;

public class GameController : MonoBehaviour
{
    [Header("Match Setup")]
    [Range(3, 4)] public int playerCount = 3;
    public int seed = 123; // change to any number to get different shuffles (same seed = same match)

    [Header("Debug Input (Inspector)")]
    public int targetPlayerId = 1;
    public Rank askRank = Rank.Five;
    [Range(1, 4)] public int guessCount = 1;

    public bool guessSuit_Clubs;
    public bool guessSuit_Diamonds;
    public bool guessSuit_Hearts;
    public bool guessSuit_Spades;

    private GameState _state;
    private GameEngine _engine;

    private int _lastLogIndex = 0;

    void Start()
    {
        // 1) Create players
        var names = new List<string>();
        for (int i = 0; i < playerCount; i++)
            names.Add(i == 0 ? "You" : $"Bot {i}");

        // 2) Create state + deal
        _state = new GameState(names, seed: seed, startingPlayerId: 0);
        _state.DealInitial(4);

        // 3) Create engine
        _engine = new GameEngine(_state);

        DumpPrivateHandsToConsole(); // debug: you can remove later
        PrintNewLogs();
        PrintTurnHelp();
    }

    void Update()
    {
        if (_engine == null) return;

        // Press A to perform "Ask rank"
        if (Input.GetKeyDown(KeyCode.A))
        {
            DoAsk();
        }

        // Press C to submit count guess (only valid when engine is in GuessCount phase)
        if (Input.GetKeyDown(KeyCode.C))
        {
            DoGuessCount();
        }

        // Press S to submit suit guess (only valid when engine is in GuessSuits phase)
        if (Input.GetKeyDown(KeyCode.S))
        {
            DoGuessSuits();
        }

        // Press H to print your current allowed ranks (asking restriction helper)
        if (Input.GetKeyDown(KeyCode.H))
        {
            PrintYourAllowedRanks();
        }

        // Press L to print new public logs
        if (Input.GetKeyDown(KeyCode.L))
        {
            PrintNewLogs();
        }
    }

    void DoAsk()
    {
        if (_engine.TryAskRank(targetPlayerId, askRank, out var err))
        {
            PrintNewLogs();
            PrintPhaseHint();
        }
        else
        {
            Debug.LogWarning(err);
        }
    }

    void DoGuessCount()
    {
        if (_engine.TryGuessCount(guessCount, out var err))
        {
            PrintNewLogs();
            PrintPhaseHint();
        }
        else
        {
            Debug.LogWarning(err);
        }
    }

    void DoGuessSuits()
    {
        var suits = new List<Suit>();
        if (guessSuit_Clubs) suits.Add(Suit.Clubs);
        if (guessSuit_Diamonds) suits.Add(Suit.Diamonds);
        if (guessSuit_Hearts) suits.Add(Suit.Hearts);
        if (guessSuit_Spades) suits.Add(Suit.Spades);

        if (_engine.TryGuessSuits(suits, out var err))
        {
            PrintNewLogs();
            PrintPhaseHint();
        }
        else
        {
            Debug.LogWarning(err);
        }
    }

    void PrintNewLogs()
    {
        var log = _state.PublicLog;
        while (_lastLogIndex < log.Count)
        {
            Debug.Log(log[_lastLogIndex]);
            _lastLogIndex++;
        }
    }

    void PrintTurnHelp()
    {
        Debug.Log("Controls:");
        Debug.Log("A = Ask (uses Inspector fields targetPlayerId + askRank)");
        Debug.Log("C = Guess Count (uses Inspector field guessCount)");
        Debug.Log("S = Guess Suits (uses Inspector toggles)");
        Debug.Log("H = Show your allowed ranks (asking restriction)");
        Debug.Log("L = Print new logs");
        Debug.Log("Tip: Watch Engine Phase in Inspector by pausing and checking logs/behavior.");
    }

    void PrintPhaseHint()
    {
        Debug.Log($"Phase: {_engine.Phase}");

        if (_engine.Phase == TurnPhase.GuessCount)
        {
            Debug.Log("Next: press C to submit COUNT guess.");
        }
        else if (_engine.Phase == TurnPhase.GuessSuits)
        {
            Debug.Log("Next: press S to submit SUIT guess.");
            Debug.Log($"Candidate Pool: {FormatSuits(_engine.CandidatePool)}");
            Debug.Log($"Required Count: {_engine.RequiredCount}");
        }
        else if (_engine.Phase == TurnPhase.ChooseTargetRank)
        {
            Debug.Log("Next: press A to ask again (if you earned extra turn) or on your next turn.");
        }
        else if (_engine.Phase == TurnPhase.GameOver)
        {
            Debug.Log("Game finished.");
        }
    }

    void PrintYourAllowedRanks()
    {
        var you = _state.GetPlayer(0);
        var ranks = you.Hand.GetDistinctRanks();
        Debug.Log("Your allowed ranks to ask for: " + string.Join(", ", ranks));
    }

    // Debug: show all hands in Console (REMOVE later; violates hidden info for real play)
    void DumpPrivateHandsToConsole()
    {
        for (int i = 0; i < _state.PlayerCount; i++)
        {
            var p = _state.GetPlayer(i);
            Debug.Log($"{p.Name} hand: {FormatCards(p.Hand.Cards)}");
        }
    }

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
        if (suits == null) return "{}";
        var list = new List<string>(suits.Count);
        for (int i = 0; i < suits.Count; i++)
            list.Add(Card.SuitSymbol(suits[i]));
        list.Sort();
        return "{ " + string.Join(", ", list) + " }";
    }
}
