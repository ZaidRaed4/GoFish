using System;
using System.Collections.Generic;

namespace GoFish.Core
{
    public sealed class GameState
    {
        private readonly List<PlayerState> _players;
        public IReadOnlyList<PlayerState> Players => _players;

        public Deck Deck { get; }
        public int CurrentTurnPlayerId { get; private set; }
        public int TotalBooksOnTable { get; private set; }
        public List<string> PublicLog { get; } = new List<string>(128);

        public int PlayerCount => _players.Count;
        public int DeckCount => Deck.Count;

        // Bumped whenever anyone lays a book; the table compares lay areas with BooksLaid
        public int BooksSequenceId { get; private set; }

        public GameState(IReadOnlyList<string> playerNames, int? seed = null, int startingPlayerId = 0)
            : this(playerNames, new Deck(seed), startingPlayerId)
        {
        }

        internal GameState(IReadOnlyList<string> playerNames, Deck deck, int startingPlayerId)
        {
            if (playerNames == null) throw new ArgumentNullException(nameof(playerNames));
            if (playerNames.Count < 3 || playerNames.Count > 4)
                throw new ArgumentOutOfRangeException(nameof(playerNames), "Player count must be 3 or 4.");

            _players = new List<PlayerState>(playerNames.Count);
            for (int i = 0; i < playerNames.Count; i++)
                _players.Add(new PlayerState(i, playerNames[i]));

            Deck = deck;
            CurrentTurnPlayerId = startingPlayerId >= 0 && startingPlayerId < _players.Count ? startingPlayerId : 0;
        }

        public void DealInitial(int cardsPerPlayer = 4)
        {
            for (int c = 0; c < cardsPerPlayer; c++)
                foreach (var player in _players)
                    if (Deck.TryDraw(out var card)) player.Hand.Add(card);

            foreach (var player in _players)
                ApplyAutoBooks(player);
        }

        public void AdvanceTurnCounterClockwise() => CurrentTurnPlayerId = PrevPlayerId(CurrentTurnPlayerId);

        public int PrevPlayerId(int playerId) => (playerId - 1 + _players.Count) % _players.Count;

        public PlayerState GetPlayer(int playerId)
        {
            if (playerId < 0 || playerId >= _players.Count)
                throw new ArgumentOutOfRangeException(nameof(playerId));
            return _players[playerId];
        }

        public void Log(string message)
        {
            if (!string.IsNullOrWhiteSpace(message)) PublicLog.Add(message);
        }

        public void ApplyAutoBooks(PlayerState player)
        {
            var laidRanks = player.Hand.RemoveCompletedBooks();
            if (laidRanks.Count == 0) return;

            player.AddBooks(laidRanks);
            TotalBooksOnTable += laidRanks.Count;
            BooksSequenceId++;

            foreach (var rank in laidRanks)
                Log($"{player.Name} laid a book of {Card.RankShort(rank)}s");
        }

        // The leader has more books than second place could still reach
        public bool IsLeaderUnbeatable()
        {
            int leader = -1, second = -1;
            foreach (var p in _players)
            {
                int books = p.BooksCount;
                if (books > leader)
                {
                    second = leader;
                    leader = books;
                }
                else if (books > second)
                {
                    second = books;
                }
            }

            return leader > second + (Rules.TotalBooks - TotalBooksOnTable);
        }

        public bool IsGameOver() => TotalBooksOnTable >= Rules.TotalBooks || IsLeaderUnbeatable();
    }
}
