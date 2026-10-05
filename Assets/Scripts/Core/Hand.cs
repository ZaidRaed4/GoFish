using System.Collections.Generic;

namespace GoFish.Core
{
    public sealed class Hand
    {
        private readonly List<Card> _cards = new List<Card>(16);

        public int Count => _cards.Count;
        public IReadOnlyList<Card> Cards => _cards;

        public void Add(Card card) => _cards.Add(card);
        public int IndexOf(Card card) => _cards.IndexOf(card);

        public void AddRange(IEnumerable<Card> cards)
        {
            if (cards != null) _cards.AddRange(cards);
        }

        public bool HasRank(Rank rank)
        {
            for (int i = 0; i < _cards.Count; i++)
                if (_cards[i].Rank == rank) return true;
            return false;
        }

        public int CountOfRank(Rank rank)
        {
            int count = 0;
            for (int i = 0; i < _cards.Count; i++)
                if (_cards[i].Rank == rank) count++;
            return count;
        }

        public bool Contains(Rank rank, Suit suit)
        {
            for (int i = 0; i < _cards.Count; i++)
                if (_cards[i].Rank == rank && _cards[i].Suit == suit) return true;
            return false;
        }

        public HashSet<Suit> GetOwnedSuits(Rank rank)
        {
            var suits = new HashSet<Suit>();
            for (int i = 0; i < _cards.Count; i++)
                if (_cards[i].Rank == rank) suits.Add(_cards[i].Suit);
            return suits;
        }

        public List<Rank> GetDistinctRanks()
        {
            var set = new HashSet<Rank>();
            for (int i = 0; i < _cards.Count; i++)
                set.Add(_cards[i].Rank);

            var list = new List<Rank>(set);
            list.Sort();
            return list;
        }

        public bool TryRemoveExact(Rank rank, IReadOnlyCollection<Suit> suits, out List<Card> removed)
        {
            removed = new List<Card>(suits?.Count ?? 0);
            if (suits == null || suits.Count == 0) return true;

            foreach (var suit in suits)
                if (!Contains(rank, suit)) return false;

            foreach (var suit in suits)
            {
                int i = _cards.FindIndex(c => c.Rank == rank && c.Suit == suit);
                removed.Add(_cards[i]);
                _cards.RemoveAt(i);
            }

            return true;
        }

        public List<Rank> RemoveCompletedBooks()
        {
            var suitsByRank = new Dictionary<Rank, HashSet<Suit>>();
            foreach (var card in _cards)
            {
                if (!suitsByRank.TryGetValue(card.Rank, out var suits))
                    suitsByRank[card.Rank] = suits = new HashSet<Suit>();
                suits.Add(card.Suit);
            }

            var laid = new List<Rank>();
            foreach (var pair in suitsByRank)
                if (pair.Value.Count == Rules.CardsPerRank) laid.Add(pair.Key);

            if (laid.Count == 0) return laid;

            _cards.RemoveAll(c => laid.Contains(c.Rank));
            laid.Sort();
            return laid;
        }
    }
}
