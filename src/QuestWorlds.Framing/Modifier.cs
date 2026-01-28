namespace QuestWorlds.Framing;

/// <summary>
/// Represents a modifier applied to a contest.
/// Modifiers adjust the target number by ±5 or ±10.
/// </summary>
public readonly record struct Modifier
{
    /// <summary>
    /// Gets the type of this modifier.
    /// </summary>
    public ModifierType Type { get; }

    /// <summary>
    /// Gets the value of this modifier (must be ±5 or ±10).
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Creates a new modifier with the specified type and value.
    /// </summary>
    /// <param name="type">The type of modifier.</param>
    /// <param name="value">The modifier value (must be ±5 or ±10).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is not ±5 or ±10.</exception>
    /// <exception cref="ArgumentException">Thrown when a Stretch modifier has a positive value.</exception>
    public Modifier(ModifierType type, int value)
    {
        if (value != -10 && value != -5 && value != 5 && value != 10)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Modifier must be ±5 or ±10");

        Type = type;

        Value = type switch
        {
            ModifierType.Stretch when value > 0 => throw new ArgumentException("Stretch must be negative", nameof(value)),
            _ => value
        };
    }
}
