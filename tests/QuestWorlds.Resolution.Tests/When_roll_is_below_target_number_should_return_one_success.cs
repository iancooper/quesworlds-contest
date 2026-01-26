using QuestWorlds.Framing;
using QuestWorlds.Resolution;

namespace QuestWorlds.Resolution.Tests;

public class When_roll_is_below_target_number_should_return_one_success
{
    [Theory]
    [InlineData(5, 10)]   // Roll 5 vs TN 10 = 1 success
    [InlineData(1, 10)]   // Roll 1 vs TN 10 = 1 success
    [InlineData(9, 10)]   // Roll 9 vs TN 10 = 1 success
    public void Returns_one_success(int playerRoll, int targetNumberBase)
    {
        // Arrange
        var resolver = new ContestResolver();
        var frame = new ContestFrame("Test prize", new TargetNumber(targetNumberBase));
        frame.SetPlayerAbility("Test ability", Rating.Parse("10"));

        // Act
        var result = resolver.Resolve(frame, new DiceRolls(playerRoll, 20));

        // Assert
        Assert.Equal(1, result.PlayerSuccesses);
    }
}
