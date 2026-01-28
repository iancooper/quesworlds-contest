using QuestWorlds.Framing;

namespace QuestWorlds.Resolution;

/// <summary>
/// Resolves contests by calculating successes and determining the winner.
/// Implements QuestWorlds simple contest resolution rules.
/// </summary>
public class ContestResolver : IContestResolver
{
    /// <inheritdoc />
    public ResolutionResult Resolve(ContestFrame frame, DiceRolls rolls)
    {
        if (!frame.IsReadyForResolution)
            throw new InvalidOperationException("Contest frame is not ready for resolution. Player ability must be set.");

        var adjudicator = new Adjudicator(frame.GetPlayerTargetNumber()!.Value, frame.Resistance);
        var (winner, degree, playerSuccesses, resistanceSuccesses) = adjudicator.Determine(rolls);

        return new ResolutionResult(playerRoll: rolls.PlayerRoll, resistanceRoll: rolls.ResistanceRoll,
            playerSuccesses: playerSuccesses, resistanceSuccesses: resistanceSuccesses, winner: winner, degree: degree);
    }

    /// <inheritdoc />
    public ResolutionResult Resolve(ContestFrame frame)
    {
        var rolls = new DiceRolls(
            PlayerRoll: Random.Shared.Next(1, 21),
            ResistanceRoll: Random.Shared.Next(1, 21));

        return Resolve(frame, rolls);
    }
}
