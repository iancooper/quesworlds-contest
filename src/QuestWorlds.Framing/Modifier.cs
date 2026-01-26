namespace QuestWorlds.Framing;

/// <summary>
/// Represents a modifier applied to a contest.
/// Modifiers adjust the target number by ±5 or ±10.
/// </summary>
public readonly record struct Modifier
{
    public ModifierType Type { get; }
    public int Value { get; }

    public Modifier(ModifierType type, int value)
    {
        Type = type;

        Value = type switch
        {
            ModifierType.Stretch when value > 0 => throw new ArgumentException("Stretch must be negative", nameof(value)),
            _ => value
        };
    }
}
