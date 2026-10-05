using System.Collections.Generic;
using NUnit.Framework;

namespace GoFish.Core.Tests
{
    static class TestTable
    {
        static readonly string[] Names = { "You", "Bot 1", "Bot 2", "Bot 3" };

        public static GameState Create(string you, string bot1, string bot2 = "QC", string bot3 = "QD", string deck = "")
        {
            var state = new GameState(Names, new Deck(Cards(deck)), 0);
            string[] hands = { you, bot1, bot2, bot3 };
            for (int i = 0; i < hands.Length; i++)
                state.GetPlayer(i).Hand.AddRange(Cards(hands[i]));
            return state;
        }

        public static List<Card> Cards(string text)
        {
            var cards = new List<Card>();
            foreach (var part in text.Split(' '))
                if (part.Length > 0) cards.Add(C(part));
            return cards;
        }

        public static Card C(string text)
        {
            string rank = text.Substring(0, text.Length - 1);
            var suit = (Suit)"CDHS".IndexOf(text[text.Length - 1]);
            return rank switch
            {
                "J" => new Card(Rank.Jack, suit),
                "Q" => new Card(Rank.Queen, suit),
                "K" => new Card(Rank.King, suit),
                "A" => new Card(Rank.Ace, suit),
                _ => new Card((Rank)int.Parse(rank), suit)
            };
        }

        public static string FullRanks(params string[] ranks)
        {
            var text = "";
            foreach (var rank in ranks)
                text += $"{rank}C {rank}D {rank}H {rank}S ";
            return text;
        }

        public static void AssertEvents(GameEngine engine, params GameEventType[] expected)
        {
            var actual = new List<GameEventType>();
            foreach (var ev in engine.Events)
                actual.Add(ev.Type);
            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
