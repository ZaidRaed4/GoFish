using System;
using System.Collections.Generic;

namespace GoFish.Core
{
    public enum TurnPhase : byte
    {
        ChooseTargetRank = 0,
        GuessCount = 1,
        GuessSuits = 2,
        GameOver = 3
    }

    // Turn rules: ask for a rank, guess the count, guess the suits. Input and bots call the Try* methods.
    public sealed class GameEngine
    {
        public GameState State { get; }
        public TurnPhase Phase { get; private set; } = TurnPhase.ChooseTargetRank;

        private AttemptContext _ctx;

        private readonly List<GameEvent> _events = new List<GameEvent>(256);
        public IReadOnlyList<GameEvent> Events => _events;

        private static readonly Suit[] AllSuits = { Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades };

        public GameEngine(GameState state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            State.Log($"Match started. Turn: {CurrentPlayer.Name}");

            StartTurnOrSkipIfStalled();
            CheckGameOverAndLog();
        }

        public PlayerState CurrentPlayer => State.GetPlayer(State.CurrentTurnPlayerId);

        public int CurrentAskerId => _ctx.AskerId;
        public int CurrentTargetId => _ctx.TargetId;
        public Rank CurrentRank => _ctx.Rank;
        public int RequiredCount => _ctx.RequiredCount;
        public IReadOnlyList<Suit> CandidatePool => _ctx.CandidatePool;

        // Ask another player for a rank you hold. NO: draw one and the turn passes. YES: guess the count next.
        public bool TryAskRank(int targetPlayerId, Rank rank, out string error)
        {
            if (!IsPhase(TurnPhase.ChooseTargetRank, out error)) return false;

            int before = State.CurrentTurnPlayerId;
            StartTurnOrSkipIfStalled();
            if (Phase == TurnPhase.GameOver)
            {
                error = "Game is over.";
                return false;
            }
            if (State.CurrentTurnPlayerId != before)
            {
                error = "Turn changed due to empty-hand rule. UI should refresh and try again.";
                return false;
            }

            int askerId = State.CurrentTurnPlayerId;

            if (targetPlayerId < 0 || targetPlayerId >= State.PlayerCount)
            {
                error = "Invalid target player.";
                return false;
            }

            if (targetPlayerId == askerId)
            {
                error = "You must target another player.";
                return false;
            }

            var asker = State.GetPlayer(askerId);
            var target = State.GetPlayer(targetPlayerId);

            if (!asker.Hand.HasRank(rank))
            {
                error = $"Asking restriction: you must already have {Card.RankShort(rank)} in your hand.";
                return false;
            }

            _ctx = AttemptContext.Create(askerId, targetPlayerId, rank);
            State.Log($"{asker.Name} asked {target.Name} for {Card.RankShort(rank)}s");

            int targetCount = target.Hand.CountOfRank(rank);
            if (targetCount <= 0)
            {
                State.Log($"{target.Name} answered NO");
                _events.Add(GameEvent.Ask(_events.Count, askerId, targetPlayerId, rank, false));

                DrawAndPassTurn(asker);
                return true;
            }

            State.Log($"{target.Name} answered YES");
            _events.Add(GameEvent.Ask(_events.Count, askerId, targetPlayerId, rank, true));

            // The true count and suits stay private; a wrong guess reveals nothing
            _ctx.RequiredCount = targetCount;
            _ctx.TargetSuits = CollectTargetSuits(target.Hand, rank);

            Phase = TurnPhase.GuessCount;
            return true;
        }

        // Guess how many cards of the rank the target holds. Wrong: draw one and the turn passes.
        public bool TryGuessCount(int guessCount, out string error)
        {
            if (!IsPhase(TurnPhase.GuessCount, out error)) return false;

            var asker = State.GetPlayer(_ctx.AskerId);

            State.Log($"{asker.Name} guessed COUNT = {guessCount} for {Card.RankShort(_ctx.Rank)}s");
            _events.Add(GameEvent.Count(_events.Count, _ctx.AskerId, _ctx.TargetId, _ctx.Rank, guessCount, _ctx.RequiredCount));

            if (guessCount != _ctx.RequiredCount)
            {
                State.Log("Count guess FAILED");

                DrawAndPassTurn(asker);
                return true;
            }

            State.Log("Count guess SUCCESS");

            // The target can only hold suits the asker doesn't have in that rank
            _ctx.CandidatePool = BuildCandidatePool(asker.Hand.GetOwnedSuits(_ctx.Rank));

            if (_ctx.RequiredCount > _ctx.CandidatePool.Count)
            {
                State.Log("Warning: inconsistent state (required count > candidate pool). Ending turn.");
                EndTurnAdvance();
                return true;
            }

            // Only one possible set of suits: no guess needed
            if (_ctx.RequiredCount == _ctx.CandidatePool.Count)
            {
                State.Log("Suit outcome FORCED (no suit guess needed)");
                _events.Add(GameEvent.SuitGuess(_events.Count, _ctx.AskerId, _ctx.TargetId, _ctx.Rank, _ctx.CandidatePool, true, true));

                TransferAndExtraTurn();
                return true;
            }

            Phase = TurnPhase.GuessSuits;
            return true;
        }

        // Guess exactly which suits the target holds. Right: take the cards and play again. Wrong: draw one.
        public bool TryGuessSuits(IReadOnlyCollection<Suit> guessedSuits, out string error)
        {
            if (!IsPhase(TurnPhase.GuessSuits, out error)) return false;

            if (guessedSuits == null)
            {
                error = "Guessed suits cannot be null.";
                return false;
            }

            if (guessedSuits.Count != _ctx.RequiredCount)
            {
                error = $"You must guess exactly {_ctx.RequiredCount} suit(s).";
                return false;
            }

            var guessSet = new HashSet<Suit>();
            foreach (var suit in guessedSuits)
            {
                if (!guessSet.Add(suit))
                {
                    error = "Duplicate suit in guess.";
                    return false;
                }
                if (_ctx.CandidatePool == null || !_ctx.CandidatePool.Contains(suit))
                {
                    error = "Suit guess must be from the candidate pool.";
                    return false;
                }
            }

            var asker = State.GetPlayer(_ctx.AskerId);
            State.Log($"{asker.Name} guessed SUITS = {FormatSuits(guessSet)} for {Card.RankShort(_ctx.Rank)}s");

            bool success = _ctx.TargetSuits != null && _ctx.TargetSuits.SetEquals(guessSet);
            _events.Add(GameEvent.SuitGuess(_events.Count, _ctx.AskerId, _ctx.TargetId, _ctx.Rank, guessSet, success, false));

            if (!success)
            {
                State.Log("Suit guess FAILED");

                DrawAndPassTurn(asker);
                return true;
            }

            State.Log("Suit guess SUCCESS");
            TransferAndExtraTurn();
            return true;
        }

        private bool IsPhase(TurnPhase expected, out string error)
        {
            error = Phase == TurnPhase.GameOver ? "Game is over."
                  : Phase != expected ? $"Invalid action. Current phase is {Phase}."
                  : null;
            return error == null;
        }

        private void DrawAndPassTurn(PlayerState asker)
        {
            DrawOneHidden(asker);
            State.ApplyAutoBooks(asker);
            EndTurnAdvance();
        }

        private void TransferAndExtraTurn()
        {
            var asker = State.GetPlayer(_ctx.AskerId);
            var target = State.GetPlayer(_ctx.TargetId);

            if (!target.Hand.TryRemoveExact(_ctx.Rank, _ctx.TargetSuits ?? new HashSet<Suit>(), out var removed))
            {
                State.Log("Error: transfer failed due to missing cards. Ending turn.");
                EndTurnAdvance();
                return;
            }

            asker.Hand.AddRange(removed);
            _events.Add(GameEvent.Transfer(_events.Count, target.PlayerId, asker.PlayerId, _ctx.Rank, removed));

            State.Log($"{asker.Name} took {removed.Count} card(s) of {Card.RankShort(_ctx.Rank)} from {target.Name}");

            State.ApplyAutoBooks(asker);
            if (CheckGameOverAndLog()) return;

            Phase = TurnPhase.ChooseTargetRank;
            StartTurnOrSkipIfStalled();
            if (Phase == TurnPhase.GameOver) return;

            if (State.CurrentTurnPlayerId == asker.PlayerId)
                State.Log($"{asker.Name} earned an EXTRA TURN");
            else
                State.Log($"Turn -> {CurrentPlayer.Name}");
        }

        private void EndTurnAdvance()
        {
            if (CheckGameOverAndLog()) return;

            State.AdvanceTurnCounterClockwise();
            Phase = TurnPhase.ChooseTargetRank;

            StartTurnOrSkipIfStalled();
            if (Phase == TurnPhase.GameOver) return;

            State.Log($"Turn -> {CurrentPlayer.Name}");
        }

        private void DrawOneHidden(PlayerState player)
        {
            if (!State.Deck.TryDraw(out var drawn))
            {
                State.Log("Deck is empty (no draw)");
                return;
            }

            player.Hand.Add(drawn);
            _events.Add(GameEvent.Draw(_events.Count, player.PlayerId, drawn));
            State.Log($"{player.Name} drew 1 card");
        }

        // An empty hand at turn start draws one card, or skips the turn when the deck is empty too
        private void StartTurnOrSkipIfStalled()
        {
            if (Phase == TurnPhase.GameOver) return;

            for (int safety = State.PlayerCount; safety > 0 && Phase != TurnPhase.GameOver; safety--)
            {
                var player = CurrentPlayer;
                if (player.Hand.Count > 0) return;

                if (State.Deck.Count > 0)
                {
                    State.Log($"{player.Name} had an empty hand — auto draw");
                    DrawOneHidden(player);
                    State.ApplyAutoBooks(player);

                    if (CheckGameOverAndLog()) return;
                    if (player.Hand.Count > 0) return;
                }
                else
                {
                    State.Log($"{player.Name} has empty hand and deck is empty — turn skipped");
                    State.AdvanceTurnCounterClockwise();
                }
            }

            ForceGameOver("No playable cards remain (stall safety).");
        }

        private void ForceGameOver(string reason)
        {
            if (Phase == TurnPhase.GameOver) return;
            Phase = TurnPhase.GameOver;

            var winners = Leaders(out int best);
            if (winners.Count == 1)
                State.Log($"GAME OVER — {reason} Winner: {winners[0].Name} with {best} books");
            else
                State.Log($"GAME OVER — {reason} Tie with {best} books");
        }

        private bool CheckGameOverAndLog()
        {
            if (!State.IsGameOver()) return false;
            Phase = TurnPhase.GameOver;

            var winners = Leaders(out int best);
            if (winners.Count == 1)
            {
                State.Log($"GAME OVER — Winner: {winners[0].Name} with {best} books");
            }
            else
            {
                var names = winners.ConvertAll(p => p.Name);
                State.Log($"GAME OVER — Tie between: {string.Join(", ", names)} with {best} books");
            }

            return true;
        }

        private List<PlayerState> Leaders(out int best)
        {
            best = -1;
            var leaders = new List<PlayerState>();
            foreach (var p in State.Players)
            {
                if (p.BooksCount > best)
                {
                    best = p.BooksCount;
                    leaders.Clear();
                }
                if (p.BooksCount == best) leaders.Add(p);
            }
            return leaders;
        }

        private static HashSet<Suit> CollectTargetSuits(Hand hand, Rank rank)
        {
            var suits = new HashSet<Suit>();
            foreach (var card in hand.Cards)
                if (card.Rank == rank) suits.Add(card.Suit);
            return suits;
        }

        private static List<Suit> BuildCandidatePool(HashSet<Suit> knownSuits)
        {
            var pool = new List<Suit>(AllSuits.Length);
            foreach (var suit in AllSuits)
                if (!knownSuits.Contains(suit)) pool.Add(suit);
            return pool;
        }

        private static string FormatSuits(IReadOnlyCollection<Suit> suits)
        {
            var parts = new List<string>(suits.Count);
            foreach (var suit in suits)
                parts.Add(Card.SuitSymbol(suit));

            parts.Sort();
            return "{ " + string.Join(", ", parts) + " }";
        }

        // The current ask: who, from whom, which rank, and the private answer
        private struct AttemptContext
        {
            public int AskerId;
            public int TargetId;
            public Rank Rank;
            public int RequiredCount;
            public HashSet<Suit> TargetSuits;
            public List<Suit> CandidatePool;

            public static AttemptContext Create(int askerId, int targetId, Rank rank) =>
                new AttemptContext { AskerId = askerId, TargetId = targetId, Rank = rank };
        }
    }
}
