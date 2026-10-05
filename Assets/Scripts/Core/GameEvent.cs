using System.Collections.Generic;

namespace GoFish.Core
{
    public enum GameEventType : byte
    {
        Ask = 0,       // ActorId asked TargetId for Rank; Success = YES
        Count = 1,     // ActorId guessed Guess cards of Rank; Required was the answer
        Suits = 2,     // ActorId guessed Suits; Forced = there was only one possible answer
        Draw = 3,      // ActorId drew Card
        Transfer = 4   // TargetId gave Cards to ActorId
    }

    public sealed class GameEvent
    {
        public readonly GameEventType Type;
        public readonly int Index;
        public readonly int ActorId;
        public readonly int TargetId;
        public readonly Rank Rank;
        public readonly bool Success;
        public readonly bool Forced;
        public readonly int Guess;
        public readonly int Required;
        public readonly Card Card;
        public readonly IReadOnlyList<Card> Cards;
        public readonly IReadOnlyList<Suit> Suits;

        static readonly Card[] NoCards = new Card[0];
        static readonly Suit[] NoSuits = new Suit[0];

        GameEvent(GameEventType type, int index, int actorId, int targetId, Rank rank, bool success, bool forced,
                  int guess, int required, Card card, IReadOnlyList<Card> cards, IReadOnlyList<Suit> suits)
        {
            Type = type;
            Index = index;
            ActorId = actorId;
            TargetId = targetId;
            Rank = rank;
            Success = success;
            Forced = forced;
            Guess = guess;
            Required = required;
            Card = card;
            Cards = cards ?? NoCards;
            Suits = suits ?? NoSuits;
        }

        public static GameEvent Ask(int index, int askerId, int targetId, Rank rank, bool yes) =>
            new GameEvent(GameEventType.Ask, index, askerId, targetId, rank, yes, false, 0, 0, default, null, null);

        public static GameEvent Count(int index, int askerId, int targetId, Rank rank, int guess, int required) =>
            new GameEvent(GameEventType.Count, index, askerId, targetId, rank, guess == required, false, guess, required, default, null, null);

        public static GameEvent SuitGuess(int index, int askerId, int targetId, Rank rank, IEnumerable<Suit> suits, bool correct, bool forced) =>
            new GameEvent(GameEventType.Suits, index, askerId, targetId, rank, correct, forced, 0, 0, default, null, new List<Suit>(suits));

        public static GameEvent Draw(int index, int playerId, Card card) =>
            new GameEvent(GameEventType.Draw, index, playerId, -1, default, true, false, 0, 0, card, null, null);

        public static GameEvent Transfer(int index, int fromId, int toId, Rank rank, IEnumerable<Card> cards) =>
            new GameEvent(GameEventType.Transfer, index, toId, fromId, rank, true, false, 0, 0, default, new List<Card>(cards), null);
    }
}
