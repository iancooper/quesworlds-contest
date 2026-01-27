using QuestWorlds.Framing;
using QuestWorlds.Resolution;

namespace QuestWorlds.Resolution.Tests;

public class When_frame_is_not_ready_should_throw
{
    [Fact]
    public void Resolving_frame_without_player_ability_throws_InvalidOperationException()
    {
        // Arrange
        // Frame has prize and resistance but NO player ability set
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(14, 0, 0));
        var rolls = new DiceRolls(PlayerRoll: 10, ResistanceRoll: 10);
        var resolver = new ContestResolver();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(frame, rolls));
        Assert.Contains("not ready", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
