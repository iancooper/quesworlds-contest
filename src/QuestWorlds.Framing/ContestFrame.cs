namespace QuestWorlds.Framing;

/// <summary>
/// Represents a framed contest with a prize at stake and resistance to overcome.
/// The contest frame captures all the information needed before dice are rolled,
/// including the prize, resistance target number, player ability, and any modifiers.
/// </summary>
public class ContestFrame
{
    private readonly List<Modifier> _modifiers = new();

    /// <summary>
    /// Gets the prize at stake in this contest (what the player is trying to achieve).
    /// </summary>
    public string Prize { get; }

    /// <summary>
    /// Gets the resistance target number that the player must overcome.
    /// </summary>
    public TargetNumber Resistance { get; }

    /// <summary>
    /// Gets the name of the player's chosen ability for this contest.
    /// </summary>
    public string? PlayerAbilityName { get; private set; }

    /// <summary>
    /// Gets the rating of the player's chosen ability.
    /// </summary>
    public Rating? PlayerRating { get; private set; }

    /// <summary>
    /// Gets the list of modifiers applied to this contest.
    /// </summary>
    public IReadOnlyList<Modifier> Modifiers => _modifiers.AsReadOnly();

    /// <summary>
    /// Creates a new contest frame with the specified prize and resistance.
    /// </summary>
    /// <param name="prize">The prize at stake (cannot be empty).</param>
    /// <param name="resistance">The resistance target number to overcome.</param>
    /// <exception cref="ArgumentException">Thrown when prize is null or whitespace.</exception>
    public ContestFrame(string prize, TargetNumber resistance)
    {
        if (string.IsNullOrWhiteSpace(prize))
            throw new ArgumentException("Prize cannot be empty", nameof(prize));

        Prize = prize;
        Resistance = resistance;
    }

    /// <summary>
    /// Sets the player's ability for this contest.
    /// </summary>
    /// <param name="abilityName">The name of the ability (cannot be empty).</param>
    /// <param name="rating">The rating of the ability.</param>
    /// <exception cref="ArgumentException">Thrown when abilityName is null or whitespace.</exception>
    public void SetPlayerAbility(string abilityName, Rating rating)
    {
        if (string.IsNullOrWhiteSpace(abilityName))
            throw new ArgumentException("Ability name cannot be empty", nameof(abilityName));

        PlayerAbilityName = abilityName;
        PlayerRating = rating;
    }

    /// <summary>
    /// Applies a modifier to this contest.
    /// </summary>
    /// <param name="modifier">The modifier to apply.</param>
    public void ApplyModifier(Modifier modifier)
    {
        _modifiers.Add(modifier);
    }

    /// <summary>
    /// Calculates the player's effective target number after all modifiers are applied.
    /// </summary>
    /// <returns>The effective target number, or null if player ability has not been set.</returns>
    public TargetNumber? GetPlayerTargetNumber()
    {
        if (PlayerRating is null) return null;

        var totalModifier = _modifiers.Sum(m => m.Value);
        return TargetNumber.FromRating(PlayerRating.Value, totalModifier);
    }

    /// <summary>
    /// Gets a value indicating whether all required information is present for resolution.
    /// Returns true when prize, player ability name, and player rating are all set.
    /// </summary>
    public bool IsReadyForResolution =>
        !string.IsNullOrEmpty(Prize) &&
        PlayerAbilityName is not null &&
        PlayerRating is not null;
}
