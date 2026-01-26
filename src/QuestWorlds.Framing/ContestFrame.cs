namespace QuestWorlds.Framing;

/// <summary>
/// Represents a framed contest with a prize at stake and resistance to overcome.
/// </summary>
public class ContestFrame
{
    private readonly List<Modifier> _modifiers = new();

    public string Prize { get; }
    public TargetNumber Resistance { get; }
    public string? PlayerAbilityName { get; private set; }
    public Rating? PlayerRating { get; private set; }
    public IReadOnlyList<Modifier> Modifiers => _modifiers.AsReadOnly();

    public ContestFrame(string prize, TargetNumber resistance)
    {
        if (string.IsNullOrWhiteSpace(prize))
            throw new ArgumentException("Prize cannot be empty", nameof(prize));

        Prize = prize;
        Resistance = resistance;
    }

    public void SetPlayerAbility(string abilityName, Rating rating)
    {
        if (string.IsNullOrWhiteSpace(abilityName))
            throw new ArgumentException("Ability name cannot be empty", nameof(abilityName));

        PlayerAbilityName = abilityName;
        PlayerRating = rating;
    }

    public void ApplyModifier(Modifier modifier)
    {
        _modifiers.Add(modifier);
    }

    /// <summary>
    /// Calculate the player's effective target number after all modifiers.
    /// </summary>
    public TargetNumber? GetPlayerTargetNumber()
    {
        if (PlayerRating is null) return null;

        var totalModifier = _modifiers.Sum(m => m.Value);
        return TargetNumber.FromRating(PlayerRating.Value, totalModifier);
    }
}
