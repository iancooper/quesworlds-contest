namespace QuestWorlds.Resolution;

/// <summary>
/// Represents a pair of D20 dice rolls for a contest.
/// </summary>
/// <param name="PlayerRoll">The player's D20 roll result (1-20).</param>
/// <param name="ResistanceRoll">The resistance's D20 roll result (1-20).</param>
public readonly record struct DiceRolls(int PlayerRoll, int ResistanceRoll);
