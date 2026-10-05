using System.Collections.Generic;

namespace GoFish.Core
{
    public sealed class PlayerState
    {
        public int PlayerId { get; }
        public string Name { get; }
        public Hand Hand { get; } = new Hand();
        public int BooksCount { get; private set; }

        private readonly List<Rank> _booksLaid = new List<Rank>(Rules.TotalBooks);
        public IReadOnlyList<Rank> BooksLaid => _booksLaid;

        public PlayerState(int playerId, string name)
        {
            PlayerId = playerId;
            Name = string.IsNullOrWhiteSpace(name) ? $"Player {playerId + 1}" : name;
        }

        public void AddBooks(IReadOnlyList<Rank> ranks)
        {
            if (ranks == null) return;

            BooksCount += ranks.Count;
            foreach (var rank in ranks)
                if (!_booksLaid.Contains(rank)) _booksLaid.Add(rank);
        }
    }
}
