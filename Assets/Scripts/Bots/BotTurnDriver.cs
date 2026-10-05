using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoFish.Core;

public class BotTurnDriver : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] GameBootstrap bootstrap;

    [Tooltip("Bots wait until these are idle")]
    [SerializeField] TableAnimations animations;

    [Header("Enable")]
    [SerializeField] bool enableBots = true;

    [Header("Difficulty")]
    [SerializeField] BotDifficulty bot1 = BotDifficulty.Easy;
    [SerializeField] BotDifficulty bot2 = BotDifficulty.Easy;
    [SerializeField] BotDifficulty bot3 = BotDifficulty.Easy;

    [Header("Timing")]
    [SerializeField] float thinkDelay = 1.0f;

    [Tooltip("Small gap between steps AFTER overlay/animations finish (keeps pacing readable).")]
    [SerializeField] float betweenStepsDelay = 0.6f;

    [Tooltip("Tiny buffer after a turn ends (visual breath).")]
    [SerializeField] float afterTurnDelay = 0.8f;

    [Header("Hard tuning")]
    [Tooltip("Chance per step that a Hard bot plays like an Easy one")]
    [SerializeField, Range(0f, 0.5f)] float hardMistakeChance = 0.06f;

    System.Random _rng;

    void Awake()
    {
        if (bootstrap == null) bootstrap = FindFirstObjectByType<GameBootstrap>();
        if (animations == null) animations = FindFirstObjectByType<TableAnimations>();
    }

    IEnumerator Start()
    {
        yield return null;

        // Seeded from the match, so a fixed seed replays the same bot choices
        _rng = new System.Random((bootstrap != null ? bootstrap.seed : 123) ^ 0x51C0FFEE);

        if (MatchSettings.Configured)
            bot1 = bot2 = bot3 = MatchSettings.Difficulty;

        while (true)
        {
            if (!enableBots || bootstrap == null || bootstrap.State == null || bootstrap.Engine == null)
            {
                yield return null;
                continue;
            }

            if (bootstrap.Engine.Phase == TurnPhase.GameOver)
                yield break;

            int actorId = bootstrap.State.CurrentTurnPlayerId;
            if (Viewer.Is(actorId))
            {
                yield return null;
                continue;
            }

            yield return WaitForIdle();
            if (thinkDelay > 0f)
                yield return new WaitForSeconds(thinkDelay);

            var engine = bootstrap.Engine;
            if (engine.Phase == TurnPhase.GameOver)
                yield break;
            if (bootstrap.State.CurrentTurnPlayerId != actorId)
                continue;

            var difficulty = GetDifficulty(actorId);
            bool acted = engine.Phase switch
            {
                TurnPhase.ChooseTargetRank => StepAsk(actorId, difficulty),
                TurnPhase.GuessCount => StepGuessCount(difficulty),
                TurnPhase.GuessSuits => StepGuessSuits(difficulty),
                _ => false
            };

            if (acted)
            {
                yield return WaitForIdle();
                if (betweenStepsDelay > 0f)
                    yield return new WaitForSeconds(betweenStepsDelay);
            }
            else
            {
                yield return null;
            }

            if (afterTurnDelay > 0f && bootstrap.State.CurrentTurnPlayerId != actorId)
                yield return new WaitForSeconds(afterTurnDelay);
        }
    }

    BotDifficulty GetDifficulty(int playerId) => playerId switch
    {
        1 => bot1,
        2 => bot2,
        3 => bot3,
        _ => BotDifficulty.Easy
    };

    IEnumerator WaitForIdle()
    {
        while (PauseMenu.IsPaused || (animations != null && animations.IsBusy))
            yield return null;
    }

    bool PlaysPerfectly(BotDifficulty difficulty) => difficulty == BotDifficulty.Hard && !Roll(hardMistakeChance);

    bool StepAsk(int actorId, BotDifficulty difficulty)
    {
        var state = bootstrap.State;
        var ranks = state.GetPlayer(actorId).Hand.GetDistinctRanks();
        if (ranks.Count == 0) return false;

        int targetId;
        Rank rank;
        if (PlaysPerfectly(difficulty))
            PickAskHard(state, actorId, ranks, out targetId, out rank);
        else
            PickAskEasy(state, actorId, ranks, out targetId, out rank);

        return bootstrap.Engine.TryAskRank(targetId, rank, out _);
    }

    bool StepGuessCount(BotDifficulty difficulty)
    {
        var engine = bootstrap.Engine;

        int guess = PlaysPerfectly(difficulty) ? Mathf.Clamp(engine.RequiredCount, 1, Rules.CardsPerRank) : BiasedCountGuess();
        return engine.TryGuessCount(guess, out _);
    }

    bool StepGuessSuits(BotDifficulty difficulty)
    {
        var engine = bootstrap.Engine;
        int required = engine.RequiredCount;
        var pool = engine.CandidatePool;
        if (pool == null || pool.Count == 0) return false;

        var suits = new List<Suit>(required);
        if (PlaysPerfectly(difficulty))
        {
            // Hard looks at the target's real suits
            var trueSuits = SuitsOfRank(bootstrap.State.GetPlayer(engine.CurrentTargetId).Hand, engine.CurrentRank);
            foreach (var suit in pool)
                if (trueSuits.Contains(suit)) suits.Add(suit);

            if (suits.Count != required)
                PickRandomSuits(pool, required, suits);
        }
        else
        {
            PickRandomSuits(pool, required, suits);
        }

        return engine.TryGuessSuits(suits, out _);
    }

    void PickAskEasy(GameState state, int actorId, List<Rank> ranks, out int targetId, out Rank rank)
    {
        rank = PickRankWeightedByCount(state.GetPlayer(actorId).Hand, ranks);
        targetId = PickRandomOpponent(state.PlayerCount, actorId);
    }

    void PickAskHard(GameState state, int actorId, List<Rank> ranks, out int targetId, out Rank rank)
    {
        int bestTarget = -1;
        Rank bestRank = ranks[0];
        int bestScore = int.MinValue;

        var actorHand = state.GetPlayer(actorId).Hand;
        foreach (var candidate in ranks)
        {
            int actorCount = actorHand.CountOfRank(candidate);
            for (int t = 0; t < state.PlayerCount; t++)
            {
                if (t == actorId) continue;

                int targetCount = state.GetPlayer(t).Hand.CountOfRank(candidate);
                if (targetCount <= 0) continue;

                int score = targetCount * 10 + actorCount * 2;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = t;
                    bestRank = candidate;
                }
            }
        }

        if (bestTarget >= 0)
        {
            targetId = bestTarget;
            rank = bestRank;
            return;
        }

        targetId = PickRandomOpponent(state.PlayerCount, actorId);
        rank = PickRankWeightedByCount(actorHand, ranks);
    }

    int PickRandomOpponent(int playerCount, int actorId)
    {
        for (int tries = 0; tries < 16; tries++)
        {
            int t = _rng.Next(0, playerCount);
            if (t != actorId) return t;
        }
        return (actorId + 1) % playerCount;
    }

    // The more copies of a rank in hand, the likelier it is asked for
    Rank PickRankWeightedByCount(Hand hand, List<Rank> ranks)
    {
        int total = 0;
        foreach (var rank in ranks)
            total += Mathf.Max(1, hand.CountOfRank(rank));

        int roll = _rng.Next(0, total);
        foreach (var rank in ranks)
        {
            roll -= Mathf.Max(1, hand.CountOfRank(rank));
            if (roll < 0) return rank;
        }
        return ranks[0];
    }

    // 1: 40%, 2: 35%, 3: 18%, 4: 7%
    int BiasedCountGuess()
    {
        int roll = _rng.Next(0, 100);
        if (roll < 40) return 1;
        if (roll < 75) return 2;
        if (roll < 93) return 3;
        return 4;
    }

    void PickRandomSuits(IReadOnlyList<Suit> pool, int required, List<Suit> result)
    {
        result.Clear();
        if (required <= 0) return;

        var shuffled = new List<Suit>(pool);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        for (int i = 0; i < required && i < shuffled.Count; i++)
            result.Add(shuffled[i]);
    }

    static HashSet<Suit> SuitsOfRank(Hand hand, Rank rank)
    {
        var suits = new HashSet<Suit>();
        foreach (var card in hand.Cards)
            if (card.Rank == rank) suits.Add(card.Suit);
        return suits;
    }

    bool Roll(float chance)
    {
        if (chance <= 0f) return false;
        if (chance >= 1f) return true;
        return _rng.NextDouble() < chance;
    }
}
