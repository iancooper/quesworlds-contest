using QuestWorlds.Framing;

namespace QuestWorlds.Framing.Tests;

public class When_creating_target_number_from_rating_should_preserve_masteries
{
    [Fact]
    public void It_should_copy_base_from_rating()
    {
        // Arrange
        var rating = new Rating(5, 2);

        // Act
        var tn = TargetNumber.FromRating(rating);

        // Assert
        Assert.Equal(5, tn.Base);
    }

    [Fact]
    public void It_should_copy_masteries_from_rating()
    {
        // Arrange
        var rating = new Rating(5, 2);

        // Act
        var tn = TargetNumber.FromRating(rating);

        // Assert
        Assert.Equal(2, tn.Masteries);
    }

    [Fact]
    public void It_should_default_modifier_to_zero()
    {
        // Arrange
        var rating = new Rating(10, 1);

        // Act
        var tn = TargetNumber.FromRating(rating);

        // Assert
        Assert.Equal(0, tn.Modifier);
        Assert.Equal(10, tn.EffectiveBase);
    }

    [Fact]
    public void It_should_apply_modifier_when_provided()
    {
        // Arrange
        var rating = new Rating(10, 1);

        // Act
        var tn = TargetNumber.FromRating(rating, -5);

        // Assert
        Assert.Equal(-5, tn.Modifier);
        Assert.Equal(5, tn.EffectiveBase);
    }
}
