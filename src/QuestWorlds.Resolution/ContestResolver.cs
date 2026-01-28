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

        var playerSuccesses = CalculateSuccesses(rolls.PlayerRoll, frame.GetPlayerTargetNumber()!.Value);
        var resistanceSuccesses = CalculateSuccesses(rolls.ResistanceRoll, frame.Resistance);

        var (winner, degree) = DetermineWinner(playerSuccesses, resistanceSuccesses, rolls);

        return new ResolutionResult
        {
            PlayerRoll = rolls.PlayerRoll,
            ResistanceRoll = rolls.ResistanceRoll,
            PlayerSuccesses = playerSuccesses,
            ResistanceSuccesses = resistanceSuccesses,
            Winner = winner,
            Degree = degree
        };
    }

    private (ContestWinner Winner, int Degree) DetermineWinner(int playerSuccesses, int resistanceSuccesses, DiceRolls rolls)
    {
        if (playerSuccesses > resistanceSuccesses)
            return (ContestWinner.Player, playerSuccesses - resistanceSuccesses);

        if (resistanceSuccesses > playerSuccesses)
            return (ContestWinner.Resistance, resistanceSuccesses - playerSuccesses);

        // Tied successes - use higher roll as tiebreaker
        if (rolls.PlayerRoll > rolls.ResistanceRoll)
            return (ContestWinner.Player, 0);

        if (rolls.ResistanceRoll > rolls.PlayerRoll)
            return (ContestWinner.Resistance, 0);

        return (ContestWinner.Tie, 0);
    }

    /// <inheritdoc />
    public ResolutionResult Resolve(ContestFrame frame)
    {
        var rolls = new DiceRolls(
            PlayerRoll: Random.Shared.Next(1, 21),
            ResistanceRoll: Random.Shared.Next(1, 21));

        return Resolve(frame, rolls);
    }

    private int CalculateSuccesses(int roll, TargetNumber targetNumber)
    {
        int baseSuccesses;

        if (roll == targetNumber.EffectiveBase)
            baseSuccesses = 2; // Big success
        else if (roll < targetNumber.EffectiveBase)
            baseSuccesses = 1;
        else
            baseSuccesses = 0;

        return baseSuccesses + targetNumber.Masteries;
    }
}
