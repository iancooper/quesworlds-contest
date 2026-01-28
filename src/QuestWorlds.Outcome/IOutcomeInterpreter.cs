using QuestWorlds.Framing;
using QuestWorlds.Resolution;

namespace QuestWorlds.Outcome;

/// <summary>
/// Interprets resolution results into displayable outcomes with benefits and consequences.
/// </summary>
public interface IOutcomeInterpreter
{
    /// <summary>
    /// Interprets a resolution result into a complete contest outcome.
    /// </summary>
    /// <param name="result">The resolution result from the contest.</param>
    /// <param name="frame">The original contest frame with context information.</param>
    /// <returns>A complete contest outcome ready for display.</returns>
    ContestOutcome Interpret(ResolutionResult result, ContestFrame frame);
}
