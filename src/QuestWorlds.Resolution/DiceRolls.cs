namespace QuestWorlds.Resolution;

/// <summary>
/// Represents a pair of D20 dice rolls for a contest.
/// </summary>
public readonly record struct DiceRolls(int PlayerRoll, int ResistanceRoll);
