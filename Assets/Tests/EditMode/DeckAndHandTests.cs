using System.Collections.Generic;
using NUnit.Framework;
using static GoFish.Core.Tests.TestTable;

namespace GoFish.Core.Tests
{
    public class DeckAndHandTests
    {
        [Test]
        public void Deck_HoldsEveryCardOnce()
        {
            var deck = new Deck(7);
            var seen = new HashSet<Card>();
            while (deck.TryDraw(out var card))
                Assert.IsTrue(seen.Add(card), $"{card} drawn twice");

            Assert.AreEqual(52, seen.Count);
        }

        [Test]
        public void Deck_SameSeedDealsTheSameOrder()
        {
            var a = new Deck(42);
            var b = new Deck(42);
            while (a.TryDraw(out var card))
            {
                Assert.IsTrue(b.TryDraw(out var other));
                Assert.AreEqual(card, other);
            }
        }

        [Test]
        public void CardIds_CoverTheWholeSpriteTable()
        {
            var ids = new HashSet<int>();
            var deck = new Deck(1);
            while (deck.TryDraw(out var card))
                ids.Add(card.ToId0To51());

            Assert.AreEqual(52, ids.Count);
            Assert.IsTrue(ids.Contains(0) && ids.Contains(51));
        }

        [Test]
        public void Hand_RemovesNothingUnlessEveryCardIsThere()
        {
            var hand = new Hand();
            hand.AddRange(Cards("8C 8D"));

            Assert.IsFalse(hand.TryRemoveExact(Rank.Eight, new[] { Suit.Clubs, Suit.Hearts }, out _));
            Assert.AreEqual(2, hand.Count);

            Assert.IsTrue(hand.TryRemoveExact(Rank.Eight, new[] { Suit.Clubs }, out var removed));
            CollectionAssert.AreEqual(Cards("8C"), removed);
            CollectionAssert.AreEqual(Cards("8D"), hand.Cards);
        }

        [Test]
        public void Hand_LaysOnlyCompleteBooks()
        {
            var hand = new Hand();
            hand.AddRange(Cards("5C 9C 5D 9D 5H 5S 9H"));

            CollectionAssert.AreEqual(new[] { Rank.Five }, hand.RemoveCompletedBooks());
            CollectionAssert.AreEqual(Cards("9C 9D 9H"), hand.Cards);
        }

        [Test]
        public void Turns_GoFromYouToBot3ThenBot2ThenBot1()
        {
            var state = new GameState(new[] { "You", "Bot 1", "Bot 2", "Bot 3" }, 1);
            var order = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                state.AdvanceTurnCounterClockwise();
                order.Add(state.CurrentTurnPlayerId);
            }

            CollectionAssert.AreEqual(new[] { 3, 2, 1, 0 }, order);
        }
    }
}
