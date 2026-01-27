using QuestWorlds.Framing;
using QuestWorlds.Resolution;

namespace QuestWorlds.Outcome;

/// <summary>
/// Interprets resolution results into displayable outcomes.
/// </summary>
public class OutcomeInterpreter : IOutcomeInterpreter
{
    private static readonly int[] BENEFIT_MODIFIERS = [5, 10, 15, 20];
    private static readonly int[] CONSEQUENCE_MODIFIERS = [-5, -10, -15, -20];

    public ContestOutcome Interpret(ResolutionResult result, ContestFrame frame)
    {
        var index = Math.Clamp(result.Degree, 0, 3);
        var isPlayerVictory = result.Winner == ContestWinner.Player;
        var modifier = isPlayerVictory
            ? BENEFIT_MODIFIERS[index]
            : CONSEQUENCE_MODIFIERS[index];

        return new ContestOutcome
        {
            BenefitConsequenceModifier = modifier
        };
    }
}
