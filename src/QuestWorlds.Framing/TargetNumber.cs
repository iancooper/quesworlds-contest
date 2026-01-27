namespace QuestWorlds.Framing;

/// <summary>
/// Represents a target number for dice resolution.
/// The effective base is calculated by applying the modifier and clamping to 1-20.
/// </summary>
public readonly record struct TargetNumber
{
    public int Base { get; }
    public int Masteries { get; }
    public int Modifier { get; }

    public TargetNumber(int baseValue, int masteries = 0, int modifier = 0)
    {
        Base = baseValue;
        Masteries = masteries;
        Modifier = modifier;
    }

    /// <summary>
    /// Creates a TargetNumber from a Rating, optionally applying a modifier.
    /// </summary>
    public static TargetNumber FromRating(Rating rating, int modifier = 0) =>
        new(rating.Base, rating.Masteries, modifier);

    /// <summary>
    /// The effective base for dice comparison (after applying modifier, clamped to 1-20).
    /// </summary>
    public int EffectiveBase => Math.Clamp(Base + Modifier, 1, 20);
}
