using QuestWorlds.Framing;

namespace QuestWorlds.Resolution;

/// <summary>
/// Resolves a contest by calculating successes and determining the winner from dice rolls.
/// </summary>
public interface IContestResolver
{
    /// <summary>
    /// Resolves a contest with the given dice rolls. This method is deterministic and testable.
    /// </summary>
    /// <param name="frame">The contest frame containing target numbers and ability information.</param>
    /// <param name="rolls">The dice rolls for both player and resistance.</param>
    /// <returns>The resolution result containing successes, winner, and degree of victory/defeat.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the frame is not ready for resolution.</exception>
    ResolutionResult Resolve(ContestFrame frame, DiceRolls rolls);

    /// <summary>
    /// Resolves a contest by generating dice rolls internally. Convenience method for production use.
    /// </summary>
    /// <param name="frame">The contest frame containing target numbers and ability information.</param>
    /// <returns>The resolution result containing successes, winner, and degree of victory/defeat.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the frame is not ready for resolution.</exception>
    ResolutionResult Resolve(ContestFrame frame);
}
