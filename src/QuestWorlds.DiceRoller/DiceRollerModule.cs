namespace QuestWorlds.DiceRoller;

/// <summary>
/// Factory for creating DiceRoller module components.
/// </summary>
public static class DiceRollerModule
{
    /// <summary>
    /// Creates a new dice roller instance.
    /// </summary>
    public static IDiceRoller CreateRoller() => new DiceRoller();
}
