using QuestWorlds.Framing;
using QuestWorlds.Resolution;

namespace QuestWorlds.Resolution.Tests;

public class When_target_number_has_masteries_should_add_to_success_total
{
    [Fact]
    public void Roll_below_TN_with_one_mastery_returns_two_successes()
    {
        // Arrange
        var resolver = new ContestResolver();
        var frame = new ContestFrame("Test prize", new TargetNumber(10));
        frame.SetPlayerAbility("Test ability", Rating.Parse("10M")); // 10 with 1 mastery

        // Act
        var result = resolver.Resolve(frame, new DiceRolls(PlayerRoll: 5, ResistanceRoll: 20));

        // Assert - 1 base success + 1 mastery = 2 total
        Assert.Equal(2, result.PlayerSuccesses);
    }

    [Fact]
    public void Big_success_with_two_masteries_returns_four_successes()
    {
        // Arrange
        var resolver = new ContestResolver();
        var frame = new ContestFrame("Test prize", new TargetNumber(10));
        frame.SetPlayerAbility("Test ability", Rating.Parse("10M2")); // 10 with 2 masteries

        // Act
        var result = resolver.Resolve(frame, new DiceRolls(PlayerRoll: 10, ResistanceRoll: 20));

        // Assert - 2 big success + 2 masteries = 4 total
        Assert.Equal(4, result.PlayerSuccesses);
    }

    [Fact]
    public void Roll_above_TN_with_one_mastery_returns_one_success()
    {
        // Arrange
        var resolver = new ContestResolver();
        var frame = new ContestFrame("Test prize", new TargetNumber(10));
        frame.SetPlayerAbility("Test ability", Rating.Parse("10M")); // 10 with 1 mastery

        // Act
        var result = resolver.Resolve(frame, new DiceRolls(PlayerRoll: 15, ResistanceRoll: 20));

        // Assert - 0 base success + 1 mastery = 1 total
        Assert.Equal(1, result.PlayerSuccesses);
    }
}
