using QuestWorlds.Framing;
using QuestWorlds.Resolution;

namespace QuestWorlds.Outcome;

/// <summary>
/// Interprets resolution results into displayable outcomes.
/// </summary>
public class OutcomeInterpreter : IOutcomeInterpreter
{
    private static readonly int[] VICTORY_MODIFIERS = [5, 10, 15, 20];

    public ContestOutcome Interpret(ResolutionResult result, ContestFrame frame)
    {
        var index = Math.Clamp(result.Degree, 0, 3);
        var modifier = VICTORY_MODIFIERS[index];

        return new ContestOutcome
        {
            BenefitConsequenceModifier = modifier
        };
    }
}
