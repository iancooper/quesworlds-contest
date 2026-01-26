using QuestWorlds.Framing;
using QuestWorlds.Resolution;

namespace QuestWorlds.Resolution.Tests;

public class When_roll_is_above_target_number_should_return_zero_successes
{
    [Theory]
    [InlineData(11, 10)]  // Roll 11 vs TN 10 = 0 successes
    [InlineData(20, 10)]  // Roll 20 vs TN 10 = 0 successes
    public void Returns_zero_successes(int playerRoll, int targetNumberBase)
    {
        // Arrange
        var resolver = new ContestResolver();
        var frame = new ContestFrame("Test prize", new TargetNumber(targetNumberBase));
        frame.SetPlayerAbility("Test ability", Rating.Parse("10"));

        // Act
        var result = resolver.Resolve(frame, new DiceRolls(playerRoll, 20));

        // Assert
        Assert.Equal(0, result.PlayerSuccesses);
    }
}
