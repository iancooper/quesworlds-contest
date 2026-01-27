using QuestWorlds.Resolution;

namespace QuestWorlds.DiceRoller;

/// <summary>
/// Generates random D20 dice rolls for contest resolution.
/// </summary>
public interface IDiceRoller
{
    /// <summary>
    /// Rolls two D20 dice - one for the player and one for the resistance.
    /// </summary>
    /// <returns>A DiceRolls value containing both roll results (1-20 each)</returns>
    DiceRolls Roll();
}
