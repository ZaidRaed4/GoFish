using NUnit.Framework;
using static GoFish.Core.Tests.TestTable;

namespace GoFish.Core.Tests
{
    public class GameEngineTests
    {
        [Test]
        public void AskForMissingRank_DrawsOneAndPassesTheTurn()
        {
            var state = Create(you: "8C", bot1: "5C", deck: "2H");
            var engine = new GameEngine(state);

            Assert.IsTrue(engine.TryAskRank(1, Rank.Eight, out _));

            CollectionAssert.AreEqual(Cards("8C 2H"), state.GetPlayer(0).Hand.Cards);
            Assert.AreEqual(3, state.CurrentTurnPlayerId);
            Assert.AreEqual(TurnPhase.ChooseTargetRank, engine.Phase);
            AssertEvents(engine, GameEventType.Ask, GameEventType.Draw);
            Assert.IsFalse(engine.Events[0].Success);
        }

        [Test]
        public void AskForHeldRank_MovesOnToTheCountGuess()
        {
            var state = Create(you: "8C", bot1: "8D 8H", deck: "2H");
            var engine = new GameEngine(state);

            Assert.IsTrue(engine.TryAskRank(1, Rank.Eight, out _));

            Assert.AreEqual(TurnPhase.GuessCount, engine.Phase);
            Assert.AreEqual(2, engine.RequiredCount);
            Assert.AreEqual(0, state.CurrentTurnPlayerId);
            Assert.AreEqual(1, state.DeckCount);
            AssertEvents(engine, GameEventType.Ask);
            Assert.IsTrue(engine.Events[0].Success);
        }

        [Test]
        public void AskForRankYouDontHold_IsRejected()
        {
            var state = Create(you: "8C", bot1: "5C");
            var engine = new GameEngine(state);

            Assert.IsFalse(engine.TryAskRank(1, Rank.Five, out var error));

            Assert.IsNotNull(error);
            Assert.AreEqual(TurnPhase.ChooseTargetRank, engine.Phase);
            Assert.IsEmpty(engine.Events);
        }

        [Test]
        public void AskYourself_IsRejected()
        {
            var engine = new GameEngine(Create(you: "8C", bot1: "5C"));

            Assert.IsFalse(engine.TryAskRank(0, Rank.Eight, out _));
            Assert.IsEmpty(engine.Events);
        }

        [Test]
        public void GuessesBeforeAnAsk_AreRejected()
        {
            var engine = new GameEngine(Create(you: "8C", bot1: "8D"));

            Assert.IsFalse(engine.TryGuessCount(1, out var countError));
            Assert.IsFalse(engine.TryGuessSuits(new[] { Suit.Diamonds }, out var suitsError));

            StringAssert.Contains("ChooseTargetRank", countError);
            StringAssert.Contains("ChooseTargetRank", suitsError);
            Assert.IsEmpty(engine.Events);
        }

        [Test]
        public void WrongCount_DrawsOneAndPassesTheTurn()
        {
            var state = Create(you: "8C", bot1: "8D 8H", deck: "2H");
            var engine = new GameEngine(state);
            engine.TryAskRank(1, Rank.Eight, out _);

            Assert.IsTrue(engine.TryGuessCount(1, out _));

            Assert.AreEqual(3, state.CurrentTurnPlayerId);
            CollectionAssert.AreEqual(Cards("8C 2H"), state.GetPlayer(0).Hand.Cards);
            CollectionAssert.AreEqual(Cards("8D 8H"), state.GetPlayer(1).Hand.Cards);
            AssertEvents(engine, GameEventType.Ask, GameEventType.Count, GameEventType.Draw);
            Assert.IsFalse(engine.Events[1].Success);
        }

        [Test]
        public void RightCount_WithOnlyOnePossibleSetOfSuits_TakesTheCards()
        {
            // You hold clubs and diamonds, so their two eights can only be hearts and spades
            var state = Create(you: "8C 8D 3C", bot1: "8H 8S 5C");
            var engine = new GameEngine(state);
            engine.TryAskRank(1, Rank.Eight, out _);

            Assert.IsTrue(engine.TryGuessCount(2, out _));

            AssertEvents(engine, GameEventType.Ask, GameEventType.Count, GameEventType.Suits, GameEventType.Transfer);
            Assert.IsTrue(engine.Events[2].Forced);
            CollectionAssert.AreEqual(new[] { Rank.Eight }, state.GetPlayer(0).BooksLaid);
            CollectionAssert.AreEqual(Cards("3C"), state.GetPlayer(0).Hand.Cards);
            Assert.AreEqual(0, state.CurrentTurnPlayerId);
            Assert.AreEqual(TurnPhase.ChooseTargetRank, engine.Phase);
        }

        [Test]
        public void RightCount_LeavesOnlySuitsYouDontHold()
        {
            var engine = new GameEngine(Create(you: "8C 3C", bot1: "8D 5C"));
            engine.TryAskRank(1, Rank.Eight, out _);

            Assert.IsTrue(engine.TryGuessCount(1, out _));

            Assert.AreEqual(TurnPhase.GuessSuits, engine.Phase);
            CollectionAssert.AreEquivalent(new[] { Suit.Diamonds, Suit.Hearts, Suit.Spades }, engine.CandidatePool);
        }

        [Test]
        public void RightSuits_TakeTheCardsAndPlayAgain()
        {
            var state = Create(you: "8C 3C", bot1: "8D 5C", deck: "2H");
            var engine = new GameEngine(state);
            engine.TryAskRank(1, Rank.Eight, out _);
            engine.TryGuessCount(1, out _);

            Assert.IsTrue(engine.TryGuessSuits(new[] { Suit.Diamonds }, out _));

            CollectionAssert.AreEqual(Cards("8C 3C 8D"), state.GetPlayer(0).Hand.Cards);
            CollectionAssert.AreEqual(Cards("5C"), state.GetPlayer(1).Hand.Cards);
            Assert.AreEqual(0, state.CurrentTurnPlayerId);
            Assert.AreEqual(1, state.DeckCount);
            AssertEvents(engine, GameEventType.Ask, GameEventType.Count, GameEventType.Suits, GameEventType.Transfer);
            Assert.IsFalse(engine.Events[2].Forced);
        }

        [Test]
        public void WrongSuits_DrawOneAndPassTheTurn()
        {
            var state = Create(you: "8C 3C", bot1: "8D 5C", deck: "2H");
            var engine = new GameEngine(state);
            engine.TryAskRank(1, Rank.Eight, out _);
            engine.TryGuessCount(1, out _);

            Assert.IsTrue(engine.TryGuessSuits(new[] { Suit.Hearts }, out _));

            CollectionAssert.AreEqual(Cards("8C 3C 2H"), state.GetPlayer(0).Hand.Cards);
            CollectionAssert.AreEqual(Cards("8D 5C"), state.GetPlayer(1).Hand.Cards);
            Assert.AreEqual(3, state.CurrentTurnPlayerId);
            AssertEvents(engine, GameEventType.Ask, GameEventType.Count, GameEventType.Suits, GameEventType.Draw);
        }

        [Test]
        public void SuitGuess_MustNameAsManySuitsAsTheCount()
        {
            var engine = new GameEngine(Create(you: "8C 3C", bot1: "8D 5C"));
            engine.TryAskRank(1, Rank.Eight, out _);
            engine.TryGuessCount(1, out _);

            Assert.IsFalse(engine.TryGuessSuits(new[] { Suit.Diamonds, Suit.Hearts }, out _));
            Assert.AreEqual(TurnPhase.GuessSuits, engine.Phase);
        }

        [Test]
        public void SuitGuess_CannotNameASuitYouHold()
        {
            var engine = new GameEngine(Create(you: "8C 3C", bot1: "8D 5C"));
            engine.TryAskRank(1, Rank.Eight, out _);
            engine.TryGuessCount(1, out _);

            Assert.IsFalse(engine.TryGuessSuits(new[] { Suit.Clubs }, out _));
            Assert.AreEqual(TurnPhase.GuessSuits, engine.Phase);
        }

        [Test]
        public void EmptyHandAtTurnStart_DrawsOne()
        {
            var state = Create(you: "", bot1: "5C", deck: "4C");
            var engine = new GameEngine(state);

            CollectionAssert.AreEqual(Cards("4C"), state.GetPlayer(0).Hand.Cards);
            Assert.AreEqual(0, state.CurrentTurnPlayerId);
            AssertEvents(engine, GameEventType.Draw);
        }

        [Test]
        public void EmptyHandAndEmptyDeck_SkipsTheTurn()
        {
            var state = Create(you: "", bot1: "5C");
            var engine = new GameEngine(state);

            Assert.AreEqual(3, state.CurrentTurnPlayerId);
            Assert.AreEqual(TurnPhase.ChooseTargetRank, engine.Phase);
        }

        [Test]
        public void FourOfARank_IsLaidAsSoonAsItIsComplete()
        {
            var state = Create(you: "9C 9D 9H 3C", bot1: "5C", deck: "9S");
            var engine = new GameEngine(state);

            engine.TryAskRank(1, Rank.Three, out _);

            CollectionAssert.AreEqual(new[] { Rank.Nine }, state.GetPlayer(0).BooksLaid);
            CollectionAssert.AreEqual(Cards("3C"), state.GetPlayer(0).Hand.Cards);
            Assert.AreEqual(1, state.TotalBooksOnTable);
        }

        [Test]
        public void LeaderWhoCannotBeCaught_EndsTheGame()
        {
            // Seven books with six left: nobody else can reach seven
            var state = Create(you: FullRanks("4", "6", "7", "8", "9", "10", "J") + "3C", bot1: "5C");
            state.ApplyAutoBooks(state.GetPlayer(0));
            var engine = new GameEngine(state);

            Assert.AreEqual(TurnPhase.GameOver, engine.Phase);
            Assert.IsFalse(engine.TryAskRank(1, Rank.Three, out _));
        }

        [Test]
        public void LeaderWhoCanStillBeCaught_GameGoesOn()
        {
            var state = Create(you: FullRanks("4", "6", "7", "8", "9", "10") + "3C", bot1: "5C");
            state.ApplyAutoBooks(state.GetPlayer(0));
            var engine = new GameEngine(state);

            Assert.AreEqual(6, state.GetPlayer(0).BooksCount);
            Assert.AreEqual(TurnPhase.ChooseTargetRank, engine.Phase);
        }

        [Test]
        public void Events_AreNumberedInTheOrderTheyHappened()
        {
            var engine = new GameEngine(Create(you: "8C 3C", bot1: "8D 5C", deck: "2H"));
            engine.TryAskRank(1, Rank.Eight, out _);
            engine.TryGuessCount(1, out _);
            engine.TryGuessSuits(new[] { Suit.Hearts }, out _);

            Assert.AreEqual(4, engine.Events.Count);
            for (int i = 0; i < engine.Events.Count; i++)
                Assert.AreEqual(i, engine.Events[i].Index);
        }
    }
}
