namespace QuestWorlds.Framing;

/// <summary>
/// Represents a framed contest with a prize at stake and resistance to overcome.
/// </summary>
public class ContestFrame
{
    public string Prize { get; }
    public TargetNumber Resistance { get; }
    public string? PlayerAbilityName { get; private set; }
    public Rating? PlayerRating { get; private set; }

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
}
