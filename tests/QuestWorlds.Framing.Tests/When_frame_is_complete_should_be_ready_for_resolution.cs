using Xunit;

namespace QuestWorlds.Framing.Tests;

public class When_frame_is_complete_should_be_ready_for_resolution
{
    [Fact]
    public void When_frame_has_only_prize_and_resistance_should_not_be_ready()
    {
        // Arrange
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(10));

        // Act
        var isReady = frame.IsReadyForResolution;

        // Assert
        Assert.False(isReady);
    }

    [Fact]
    public void When_frame_has_prize_resistance_and_player_ability_should_be_ready()
    {
        // Arrange
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(10));
        frame.SetPlayerAbility("Stealth", new Rating(15));

        // Act
        var isReady = frame.IsReadyForResolution;

        // Assert
        Assert.True(isReady);
    }

    [Fact]
    public void When_frame_has_modifiers_but_no_player_ability_should_not_be_ready()
    {
        // Arrange
        var frame = new ContestFrame("Climb the wall", new TargetNumber(10));
        frame.ApplyModifier(new Modifier(ModifierType.Situational, 5));

        // Act
        var isReady = frame.IsReadyForResolution;

        // Assert
        Assert.False(isReady);
    }

    [Fact]
    public void When_frame_has_player_ability_with_masteries_should_be_ready()
    {
        // Arrange
        var frame = new ContestFrame("Defeat the dragon", new TargetNumber(15, 1));
        frame.SetPlayerAbility("Combat", new Rating(5, 2));

        // Act
        var isReady = frame.IsReadyForResolution;

        // Assert
        Assert.True(isReady);
    }

    [Fact]
    public void When_frame_has_player_ability_and_modifiers_should_be_ready()
    {
        // Arrange
        var frame = new ContestFrame("Epic battle", new TargetNumber(15));
        frame.SetPlayerAbility("Combat", new Rating(10));
        frame.ApplyModifier(new Modifier(ModifierType.Augment, 5));
        frame.ApplyModifier(new Modifier(ModifierType.Stretch, -5));

        // Act
        var isReady = frame.IsReadyForResolution;

        // Assert
        Assert.True(isReady);
    }
}
