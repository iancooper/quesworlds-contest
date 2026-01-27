using QuestWorlds.Framing;
using QuestWorlds.Resolution;

namespace QuestWorlds.Outcome;

/// <summary>
/// Interprets resolution results into displayable outcomes.
/// </summary>
public interface IOutcomeInterpreter
{
    ContestOutcome Interpret(ResolutionResult result, ContestFrame frame);
}
