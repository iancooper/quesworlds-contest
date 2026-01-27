using QuestWorlds.Framing;
using QuestWorlds.Resolution;

namespace QuestWorlds.Resolution.Tests;

public class When_one_side_has_more_successes_should_win
{
    [Fact]
    public void Player_with_more_successes_wins()
    {
        // Arrange
        // Player: roll 5 vs TN 10 = 1 success, +1 mastery = 2 successes
        // Resistance: roll 15 vs TN 14 = 0 successes + 1 mastery = 1 success
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(14, 1, 0));
        frame.SetPlayerAbility("Stealth", new Rating(10, 1));
        var rolls = new DiceRolls(PlayerRoll: 5, ResistanceRoll: 15);
        var resolver = new ContestResolver();

        // Act
        var result = resolver.Resolve(frame, rolls);

        // Assert
        Assert.Equal(ContestWinner.Player, result.Winner);
        Assert.Equal(1, result.Degree); // 2 - 1 = degree 1
    }

    [Fact]
    public void Resistance_with_more_successes_wins()
    {
        // Arrange
        // Player: roll 15 vs TN 10 = 0 successes + 1 mastery = 1 success
        // Resistance: roll 5 vs TN 14 = 1 success + 2 masteries = 3 successes
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(14, 2, 0));
        frame.SetPlayerAbility("Stealth", new Rating(10, 1));
        var rolls = new DiceRolls(PlayerRoll: 15, ResistanceRoll: 5);
        var resolver = new ContestResolver();

        // Act
        var result = resolver.Resolve(frame, rolls);

        // Assert
        Assert.Equal(ContestWinner.Resistance, result.Winner);
        Assert.Equal(2, result.Degree); // 3 - 1 = degree 2
    }
}
