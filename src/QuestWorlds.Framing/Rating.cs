using System.Text.RegularExpressions;

namespace QuestWorlds.Framing;

/// <summary>
/// Represents a QuestWorlds ability rating with optional masteries.
/// Examples: "15" = base 15, "5M" = 5 with 1 mastery, "6M2" = 6 with 2 masteries
/// </summary>
public readonly record struct Rating
{
    private static readonly Regex NotationPattern = new(@"^(\d+)(M(\d+)?)?$", RegexOptions.IgnoreCase);

    public int Base { get; }
    public int Masteries { get; }

    public Rating(int baseValue, int masteries = 0)
    {
        Base = baseValue;
        Masteries = masteries;
    }

    public static Rating Parse(string notation)
    {
        var match = NotationPattern.Match(notation.Trim());
        if (!match.Success)
            throw new FormatException($"Invalid rating notation: {notation}");

        var baseValue = int.Parse(match.Groups[1].Value);
        var masteries = match.Groups[2].Success
            ? (match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 1)
            : 0;

        return new Rating(baseValue, masteries);
    }

    public override string ToString() =>
        Masteries == 0 ? Base.ToString() :
        Masteries == 1 ? $"{Base}M" :
        $"{Base}M{Masteries}";
}
