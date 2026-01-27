using QuestWorlds.Resolution;

namespace QuestWorlds.Outcome;

/// <summary>
/// Complete outcome of a contest, ready for display.
/// </summary>
public sealed record ContestOutcome
{
    // Contest context
    public required string Prize { get; init; }
    public required string PlayerAbilityName { get; init; }
    public required string PlayerRating { get; init; }
    public required string ResistanceTargetNumber { get; init; }

    // Resolution details
    public required int PlayerRoll { get; init; }
    public required int ResistanceRoll { get; init; }
    public required int PlayerSuccesses { get; init; }
    public required int ResistanceSuccesses { get; init; }

    // Outcome
    public required ContestWinner Winner { get; init; }
    public required int Degree { get; init; }

    /// <summary>
    /// True if the player won (not resistance, not tie).
    /// </summary>
    public bool IsPlayerVictory => Winner == ContestWinner.Player;

    /// <summary>
    /// The modifier to apply for benefits (victory) or consequences (defeat).
    /// Range: -20 to +20
    /// </summary>
    public required int BenefitConsequenceModifier { get; init; }

    /// <summary>
    /// Human-readable summary of the outcome.
    /// </summary>
    public string Summary => Winner switch
    {
        ContestWinner.Player => $"{Degree} Degrees of Victory for the player!",
        ContestWinner.Resistance => $"{Degree} Degrees of Defeat for the player.",
        ContestWinner.Tie => "The contest is a tie.",
        _ => "Unknown outcome"
    };
}
