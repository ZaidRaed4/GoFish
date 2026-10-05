using System;
using System.Collections.Generic;

namespace GoFish.Core
{
    public sealed class Deck
    {
        private readonly List<Card> _cards = new List<Card>(52);
        private readonly Random _rng;

        public int Count => _cards.Count;

        public Deck(int? seed = null)
        {
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();

            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
                for (int r = (int)Rank.Two; r <= (int)Rank.Ace; r++)
                    _cards.Add(new Card((Rank)r, suit));

            Shuffle();
        }

        internal Deck(IEnumerable<Card> drawOrder)
        {
            _rng = new Random(0);
            _cards.AddRange(drawOrder);
            _cards.Reverse();
        }

        private void Shuffle()
        {
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(0, i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
        }

        public bool TryDraw(out Card card)
        {
            int last = _cards.Count - 1;
            if (last < 0)
            {
                card = default;
                return false;
            }

            card = _cards[last];
            _cards.RemoveAt(last);
            return true;
        }
    }
}
