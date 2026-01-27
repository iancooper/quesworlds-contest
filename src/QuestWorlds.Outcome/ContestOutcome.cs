using QuestWorlds.Resolution;

namespace QuestWorlds.Outcome;

/// <summary>
/// Complete outcome of a contest, ready for display.
/// </summary>
public sealed record ContestOutcome
{
    /// <summary>
    /// The modifier to apply for benefits (victory) or consequences (defeat).
    /// Range: -20 to +20
    /// </summary>
    public required int BenefitConsequenceModifier { get; init; }
}
