using System.Text.RegularExpressions;

namespace QuestWorlds.Framing;

/// <summary>
/// Represents a QuestWorlds ability rating with optional masteries.
/// Examples: "15" = base 15, "5M" = 5 with 1 mastery, "6M2" = 6 with 2 masteries.
/// Each mastery represents +20 to the ability value and adds 1 success during resolution.
/// </summary>
public readonly record struct Rating
{
    private static readonly Regex NotationPattern = new(@"^(\d+)(M(\d+)?)?$", RegexOptions.IgnoreCase);

    /// <summary>
    /// Gets the base value of the rating (1-20).
    /// </summary>
    public int Base { get; }

    /// <summary>
    /// Gets the number of masteries (each mastery represents +20 to the ability).
    /// </summary>
    public int Masteries { get; }

    /// <summary>
    /// Creates a new rating with the specified base value and optional masteries.
    /// </summary>
    /// <param name="baseValue">The base value (must be between 1 and 20).</param>
    /// <param name="masteries">The number of masteries (default is 0).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when baseValue is not between 1 and 20.</exception>
    public Rating(int baseValue, int masteries = 0)
    {
        if (baseValue < 1 || baseValue > 20)
            throw new ArgumentOutOfRangeException(nameof(baseValue), baseValue, "Base must be between 1 and 20");

        Base = baseValue;
        Masteries = masteries;
    }

    /// <summary>
    /// Parses a rating from QuestWorlds notation.
    /// </summary>
    /// <param name="notation">The notation string (e.g., "15", "5M", "6M2").</param>
    /// <returns>The parsed rating.</returns>
    /// <exception cref="FormatException">Thrown when the notation is invalid.</exception>
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

    /// <summary>
    /// Returns the QuestWorlds notation for this rating.
    /// </summary>
    /// <returns>A string like "15", "5M", or "6M2".</returns>
    public override string ToString() =>
        Masteries == 0 ? Base.ToString() :
        Masteries == 1 ? $"{Base}M" :
        $"{Base}M{Masteries}";
}
