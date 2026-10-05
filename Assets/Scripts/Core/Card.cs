using System;

namespace GoFish.Core
{
    public enum Suit : byte
    {
        Clubs = 0,
        Diamonds = 1,
        Hearts = 2,
        Spades = 3
    }

    public enum Rank : byte
    {
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        Ace = 14
    }

    [Serializable]
    public readonly struct Card : IEquatable<Card>, IComparable<Card>
    {
        public Rank Rank => _rank;
        public Suit Suit => _suit;

        private readonly Rank _rank;
        private readonly Suit _suit;

        public Card(Rank rank, Suit suit)
        {
            _rank = rank;
            _suit = suit;
        }

        public bool Equals(Card other) => _rank == other._rank && _suit == other._suit;
        public override bool Equals(object obj) => obj is Card c && Equals(c);
        public override int GetHashCode() => ((int)_rank << 2) | (int)_suit;

        public static bool operator ==(Card a, Card b) => a.Equals(b);
        public static bool operator !=(Card a, Card b) => !a.Equals(b);

        public int CompareTo(Card other)
        {
            int byRank = ((int)_rank).CompareTo((int)other._rank);
            return byRank != 0 ? byRank : ((int)_suit).CompareTo((int)other._suit);
        }

        public override string ToString() => $"{RankShort(_rank)}{SuitSymbol(_suit)}";

        public static string SuitSymbol(Suit suit) => suit switch
        {
            Suit.Clubs => "♣",
            Suit.Diamonds => "♦",
            Suit.Hearts => "♥",
            Suit.Spades => "♠",
            _ => "?"
        };

        public static string RankShort(Rank rank) => rank switch
        {
            Rank.Ten => "10",
            Rank.Jack => "J",
            Rank.Queen => "Q",
            Rank.King => "K",
            Rank.Ace => "A",
            _ => ((int)rank).ToString()
        };

        // Index into a 52-card sprite table: suit * 13 + (rank - 2)
        public int ToId0To51() => (int)_suit * 13 + ((int)_rank - 2);
    }
}
