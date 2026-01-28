using QuestWorlds.Framing;

namespace QuestWorlds.Resolution;

/// <summary>
/// Adjudicates contests by determining the winner and degree of victory.
/// </summary>
internal class Adjudicator(TargetNumber playerTargetNumber, TargetNumber resistanceTargetNumber)
{
    private readonly SuccessCalculator _playerCalculator = new(playerTargetNumber);
    private readonly SuccessCalculator _resistanceCalculator = new(resistanceTargetNumber);

    public (ContestWinner Winner, int Degree, int PlayerSuccesses, int ResistanceSuccesses) Determine(DiceRolls rolls)
    {
        var playerSuccesses = _playerCalculator.Determine(rolls.PlayerRoll);
        var resistanceSuccesses = _resistanceCalculator.Determine(rolls.ResistanceRoll);

        if (playerSuccesses > resistanceSuccesses)
            return (ContestWinner.Player, playerSuccesses - resistanceSuccesses, playerSuccesses, resistanceSuccesses);

        if (resistanceSuccesses > playerSuccesses)
            return (ContestWinner.Resistance, resistanceSuccesses - playerSuccesses, playerSuccesses, resistanceSuccesses);

        // Tied successes - use higher roll as tiebreaker
        if (rolls.PlayerRoll > rolls.ResistanceRoll)
            return (ContestWinner.Player, 0, playerSuccesses, resistanceSuccesses);

        if (rolls.ResistanceRoll > rolls.PlayerRoll)
            return (ContestWinner.Resistance, 0, playerSuccesses, resistanceSuccesses);

        return (ContestWinner.Tie, 0, playerSuccesses, resistanceSuccesses);
    }
}

