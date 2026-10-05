using GoFish.Core;

public static class SuitText
{
    public static string Icon(Suit suit) => suit switch
    {
        Suit.Clubs => "<sprite name=\"SUIT_C\" tint=1>",
        Suit.Diamonds => "<sprite name=\"SUIT_D\" tint=1>",
        Suit.Hearts => "<sprite name=\"SUIT_H\" tint=1>",
        Suit.Spades => "<sprite name=\"SUIT_S\" tint=1>",
        _ => "?"
    };

    public static bool IsRed(Suit suit) => suit == Suit.Diamonds || suit == Suit.Hearts;
}
