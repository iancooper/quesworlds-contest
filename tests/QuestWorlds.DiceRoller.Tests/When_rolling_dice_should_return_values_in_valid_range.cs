using QuestWorlds.DiceRoller;

namespace QuestWorlds.DiceRoller.Tests;

public class When_rolling_dice_should_return_values_in_valid_range
{
    [Fact]
    public void Roll_returns_player_roll_between_1_and_20()
    {
        // Arrange
        var roller = DiceRollerModule.CreateRoller();

        // Act & Assert
        for (int i = 0; i < 100; i++)
        {
            var rolls = roller.Roll();
            Assert.InRange(rolls.PlayerRoll, 1, 20);
        }
    }

    [Fact]
    public void Roll_returns_resistance_roll_between_1_and_20()
    {
        // Arrange
        var roller = DiceRollerModule.CreateRoller();

        // Act & Assert
        for (int i = 0; i < 100; i++)
        {
            var rolls = roller.Roll();
            Assert.InRange(rolls.ResistanceRoll, 1, 20);
        }
    }

    [Fact]
    public void Roll_returns_varying_values_over_multiple_calls()
    {
        // Arrange
        var roller = DiceRollerModule.CreateRoller();
        var distinctPlayerRolls = new HashSet<int>();
        var distinctResistanceRolls = new HashSet<int>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var rolls = roller.Roll();
            distinctPlayerRolls.Add(rolls.PlayerRoll);
            distinctResistanceRolls.Add(rolls.ResistanceRoll);
        }

        // Assert - with 100 rolls we should get at least 5 distinct values
        Assert.True(distinctPlayerRolls.Count >= 5,
            $"Expected at least 5 distinct player rolls but got {distinctPlayerRolls.Count}");
        Assert.True(distinctResistanceRolls.Count >= 5,
            $"Expected at least 5 distinct resistance rolls but got {distinctResistanceRolls.Count}");
    }
}
