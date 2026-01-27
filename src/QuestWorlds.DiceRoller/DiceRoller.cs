using QuestWorlds.Resolution;

namespace QuestWorlds.DiceRoller;

internal class DiceRoller : IDiceRoller
{
    public DiceRolls Roll() =>
        new(
            PlayerRoll: Random.Shared.Next(1, 21),
            ResistanceRoll: Random.Shared.Next(1, 21));
}
