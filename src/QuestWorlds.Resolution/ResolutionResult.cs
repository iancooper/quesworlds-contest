namespace QuestWorlds.Resolution;

/// <summary>
/// Immutable result of contest resolution.
/// </summary>
public sealed record ResolutionResult
{
    public int PlayerRoll { get; init; }
    public int ResistanceRoll { get; init; }
    public int PlayerSuccesses { get; init; }
    public int ResistanceSuccesses { get; init; }
    public ContestWinner Winner { get; init; }
    public int Degree { get; init; }
}
