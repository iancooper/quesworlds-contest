using QuestWorlds.Framing;

namespace QuestWorlds.Resolution;

/// <summary>
/// Resolves a contest by determining the outcome from dice rolls.
/// </summary>
public interface IContestResolver
{
    /// <summary>
    /// Resolves a contest with the given dice rolls. Deterministic and testable.
    /// </summary>
    ResolutionResult Resolve(ContestFrame frame, DiceRolls rolls);

    /// <summary>
    /// Resolves a contest by rolling dice internally. Convenience method for production use.
    /// </summary>
    ResolutionResult Resolve(ContestFrame frame);
}
