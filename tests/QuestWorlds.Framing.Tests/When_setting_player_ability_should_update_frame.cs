using Xunit;

namespace QuestWorlds.Framing.Tests;

public class When_setting_player_ability_should_update_frame
{
    [Fact]
    public void When_setting_valid_ability_should_update_player_ability_name()
    {
        // Arrange
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(10));
        var abilityName = "Stealth";
        var rating = new Rating(15);

        // Act
        frame.SetPlayerAbility(abilityName, rating);

        // Assert
        Assert.Equal(abilityName, frame.PlayerAbilityName);
    }

    [Fact]
    public void When_setting_valid_ability_should_update_player_rating()
    {
        // Arrange
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(10));
        var abilityName = "Stealth";
        var rating = new Rating(15);

        // Act
        frame.SetPlayerAbility(abilityName, rating);

        // Assert
        Assert.Equal(rating, frame.PlayerRating);
    }

    [Fact]
    public void When_setting_ability_with_masteries_should_preserve_masteries()
    {
        // Arrange
        var frame = new ContestFrame("Defeat the dragon", new TargetNumber(15, 1));
        var abilityName = "Combat";
        var rating = new Rating(5, 2);

        // Act
        frame.SetPlayerAbility(abilityName, rating);

        // Assert
        Assert.Equal(5, frame.PlayerRating!.Value.Base);
        Assert.Equal(2, frame.PlayerRating!.Value.Masteries);
    }

    [Fact]
    public void When_setting_empty_ability_name_should_throw_argument_exception()
    {
        // Arrange
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(10));
        var emptyAbilityName = "";
        var rating = new Rating(15);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => frame.SetPlayerAbility(emptyAbilityName, rating));
        Assert.Contains("ability", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void When_setting_whitespace_ability_name_should_throw_argument_exception()
    {
        // Arrange
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(10));
        var whitespaceAbilityName = "   ";
        var rating = new Rating(15);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => frame.SetPlayerAbility(whitespaceAbilityName, rating));
        Assert.Contains("ability", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void When_setting_null_ability_name_should_throw_argument_exception()
    {
        // Arrange
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(10));
        string? nullAbilityName = null;
        var rating = new Rating(15);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => frame.SetPlayerAbility(nullAbilityName!, rating));
        Assert.Contains("ability", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
