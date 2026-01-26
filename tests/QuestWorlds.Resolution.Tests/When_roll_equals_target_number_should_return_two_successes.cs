using QuestWorlds.Framing;
using QuestWorlds.Resolution;

namespace QuestWorlds.Resolution.Tests;

public class When_roll_equals_target_number_should_return_two_successes
{
    [Fact]
    public void Returns_two_successes()
    {
        // Arrange
        var resolver = new ContestResolver();
        var frame = new ContestFrame("Test prize", new TargetNumber(10));
        frame.SetPlayerAbility("Test ability", Rating.Parse("10"));

        // Act
        var result = resolver.Resolve(frame, new DiceRolls(PlayerRoll: 10, ResistanceRoll: 20));

        // Assert
        Assert.Equal(2, result.PlayerSuccesses);
    }
}
