namespace QuestWorlds.Resolution;

/// <summary>
/// Immutable result of contest resolution containing all dice rolls, successes, winner, and degree.
/// </summary>
public sealed record ResolutionResult
{
    public ResolutionResult()
    {
    }

    public ResolutionResult(int playerRoll, int resistanceRoll, int playerSuccesses, int resistanceSuccesses, ContestWinner winner, int degree)
    {
        PlayerRoll = playerRoll;
        ResistanceRoll = resistanceRoll;
        PlayerSuccesses = playerSuccesses;
        ResistanceSuccesses = resistanceSuccesses;
        Winner = winner;
        Degree = degree;
    }

    /// <summary>
    /// Gets the player's D20 roll result.
    /// </summary>
    public int PlayerRoll { get; init; }

    /// <summary>
    /// Gets the resistance's D20 roll result.
    /// </summary>
    public int ResistanceRoll { get; init; }

    /// <summary>
    /// Gets the player's total successes (base successes plus masteries).
    /// </summary>
    public int PlayerSuccesses { get; init; }

    /// <summary>
    /// Gets the resistance's total successes (base successes plus masteries).
    /// </summary>
    public int ResistanceSuccesses { get; init; }

    /// <summary>
    /// Gets the winner of the contest.
    /// </summary>
    public ContestWinner Winner { get; init; }

    /// <summary>
    /// Gets the degree of victory or defeat (the difference in successes, 0-3+).
    /// </summary>
    public int Degree { get; init; }
}
