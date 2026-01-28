namespace QuestWorlds.Framing;

/// <summary>
/// Represents a target number for dice resolution.
/// The effective base is calculated by applying the modifier and clamping to 1-20.
/// </summary>
public readonly record struct TargetNumber
{
    /// <summary>
    /// Gets the base value before modifiers are applied (1-20).
    /// </summary>
    public int Base { get; }

    /// <summary>
    /// Gets the number of masteries (each adds 1 success during resolution).
    /// </summary>
    public int Masteries { get; }

    /// <summary>
    /// Gets the total modifier applied to this target number.
    /// </summary>
    public int Modifier { get; }

    /// <summary>
    /// Creates a new target number with the specified values.
    /// </summary>
    /// <param name="baseValue">The base value.</param>
    /// <param name="masteries">The number of masteries (default is 0).</param>
    /// <param name="modifier">The modifier to apply (default is 0).</param>
    public TargetNumber(int baseValue, int masteries = 0, int modifier = 0)
    {
        Base = baseValue;
        Masteries = masteries;
        Modifier = modifier;
    }

    /// <summary>
    /// Creates a TargetNumber from a Rating, optionally applying a modifier.
    /// </summary>
    /// <param name="rating">The rating to convert.</param>
    /// <param name="modifier">The modifier to apply (default is 0).</param>
    /// <returns>A new TargetNumber based on the rating.</returns>
    public static TargetNumber FromRating(Rating rating, int modifier = 0) =>
        new(rating.Base, rating.Masteries, modifier);

    /// <summary>
    /// Gets the effective base for dice comparison (after applying modifier, clamped to 1-20).
    /// </summary>
    public int EffectiveBase => Math.Clamp(Base + Modifier, 1, 20);
}
