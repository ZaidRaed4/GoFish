public static class SpelledOut
{
    static readonly string[] Words = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten" };

    public static string Number(int n) => n >= 0 && n < Words.Length ? Words[n] : n.ToString();

    // "Bot 3" -> "Bot Three"
    public static string Name(string name)
    {
        var parts = name.Split(' ');
        for (int i = 0; i < parts.Length; i++)
            if (int.TryParse(parts[i], out int n) && n >= 0 && n < Words.Length)
                parts[i] = char.ToUpperInvariant(Words[n][0]) + Words[n].Substring(1);
        return string.Join(" ", parts);
    }
}
