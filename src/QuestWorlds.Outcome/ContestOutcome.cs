using QuestWorlds.Resolution;

namespace QuestWorlds.Outcome;

/// <summary>
/// Complete outcome of a contest, ready for display.
/// Contains all context, resolution details, and outcome information.
/// </summary>
public sealed record ContestOutcome
{
    /// <summary>
    /// Gets the prize that was at stake in this contest.
    /// </summary>
    public required string Prize { get; init; }

    /// <summary>
    /// Gets the name of the player's ability used in this contest.
    /// </summary>
    public required string PlayerAbilityName { get; init; }

    /// <summary>
    /// Gets the player's ability rating in QuestWorlds notation.
    /// </summary>
    public required string PlayerRating { get; init; }

    /// <summary>
    /// Gets the resistance target number in QuestWorlds notation.
    /// </summary>
    public required string ResistanceTargetNumber { get; init; }

    /// <summary>
    /// Gets the player's D20 roll result.
    /// </summary>
    public required int PlayerRoll { get; init; }

    /// <summary>
    /// Gets the resistance's D20 roll result.
    /// </summary>
    public required int ResistanceRoll { get; init; }

    /// <summary>
    /// Gets the player's total successes.
    /// </summary>
    public required int PlayerSuccesses { get; init; }

    /// <summary>
    /// Gets the resistance's total successes.
    /// </summary>
    public required int ResistanceSuccesses { get; init; }

    /// <summary>
    /// Gets the winner of the contest.
    /// </summary>
    public required ContestWinner Winner { get; init; }

    /// <summary>
    /// Gets the degree of victory or defeat (0-3+).
    /// </summary>
    public required int Degree { get; init; }

    /// <summary>
    /// Gets a value indicating whether the player won the contest.
    /// </summary>
    public bool IsPlayerVictory => Winner == ContestWinner.Player;

    /// <summary>
    /// Gets the modifier to apply for benefits (victory) or consequences (defeat).
    /// Range: -20 to +20 based on the degree of victory/defeat.
    /// </summary>
    public required int BenefitConsequenceModifier { get; init; }

    /// <summary>
    /// Gets a human-readable summary of the outcome.
    /// </summary>
    public string Summary => Winner switch
    {
        ContestWinner.Player => $"{Degree} Degrees of Victory for the player!",
        ContestWinner.Resistance => $"{Degree} Degrees of Defeat for the player.",
        ContestWinner.Tie => "The contest is a tie.",
        _ => "Unknown outcome"
    };
}
