using QuestWorlds.Framing;
using QuestWorlds.Resolution;

namespace QuestWorlds.Resolution.Tests;

public class When_successes_are_tied_should_use_higher_roll_as_tiebreaker
{
    [Fact]
    public void Player_wins_with_higher_roll()
    {
        // Arrange
        // Player: roll 8 vs TN 10 = 1 success (no masteries)
        // Resistance: roll 5 vs TN 10 = 1 success (no masteries)
        // Both have 1 success, player roll 8 > resistance roll 5 → Player wins
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(10, 0, 0));
        frame.SetPlayerAbility("Stealth", new Rating(10, 0));
        var rolls = new DiceRolls(PlayerRoll: 8, ResistanceRoll: 5);
        var resolver = new ContestResolver();

        // Act
        var result = resolver.Resolve(frame, rolls);

        // Assert
        Assert.Equal(ContestWinner.Player, result.Winner);
        Assert.Equal(0, result.Degree); // Tied successes = degree 0
    }

    [Fact]
    public void Resistance_wins_with_higher_roll()
    {
        // Arrange
        // Player: roll 5 vs TN 10 = 1 success (no masteries)
        // Resistance: roll 8 vs TN 10 = 1 success (no masteries)
        // Both have 1 success, resistance roll 8 > player roll 5 → Resistance wins
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(10, 0, 0));
        frame.SetPlayerAbility("Stealth", new Rating(10, 0));
        var rolls = new DiceRolls(PlayerRoll: 5, ResistanceRoll: 8);
        var resolver = new ContestResolver();

        // Act
        var result = resolver.Resolve(frame, rolls);

        // Assert
        Assert.Equal(ContestWinner.Resistance, result.Winner);
        Assert.Equal(0, result.Degree); // Tied successes = degree 0
    }

    [Fact]
    public void Tie_when_same_roll_and_successes()
    {
        // Arrange
        // Player: roll 7 vs TN 10 = 1 success (no masteries)
        // Resistance: roll 7 vs TN 10 = 1 success (no masteries)
        // Both have 1 success and same roll → Tie
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(10, 0, 0));
        frame.SetPlayerAbility("Stealth", new Rating(10, 0));
        var rolls = new DiceRolls(PlayerRoll: 7, ResistanceRoll: 7);
        var resolver = new ContestResolver();

        // Act
        var result = resolver.Resolve(frame, rolls);

        // Assert
        Assert.Equal(ContestWinner.Tie, result.Winner);
        Assert.Equal(0, result.Degree);
    }
}
