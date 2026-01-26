namespace QuestWorlds.Framing;

/// <summary>
/// Represents a QuestWorlds ability rating with optional masteries.
/// Examples: "15" = base 15, "5M" = 5 with 1 mastery, "6M2" = 6 with 2 masteries
/// </summary>
public readonly record struct Rating
{
    public int Base { get; }
    public int Masteries { get; }

    public Rating(int baseValue, int masteries = 0)
    {
        Base = baseValue;
        Masteries = masteries;
    }

    public static Rating Parse(string notation)
    {
        var baseValue = int.Parse(notation);
        return new Rating(baseValue, 0);
    }

    public override string ToString() => Base.ToString();
}
