using GoFish.Core;

public static class Viewer
{
    public const int Id = 0;

    public static bool Is(int playerId) => playerId == Id;

    public static string DisplayName(PlayerState player) => Is(player.PlayerId) ? "YOU" : player.Name.ToUpperInvariant();
}
