using QuestWorlds.Framing;

namespace QuestWorlds.Framing.Tests;

public class When_creating_rating_with_invalid_base_should_throw
{
    [Fact]
    public void It_should_throw_when_base_is_zero()
    {
        // Arrange
        var invalidBase = 0;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Rating(invalidBase));
    }

    [Fact]
    public void It_should_throw_when_base_is_negative()
    {
        // Arrange
        var invalidBase = -1;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Rating(invalidBase));
    }

    [Fact]
    public void It_should_throw_when_base_exceeds_20()
    {
        // Arrange
        var invalidBase = 21;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Rating(invalidBase));
    }

    [Fact]
    public void It_should_allow_base_of_1()
    {
        // Arrange
        var validBase = 1;

        // Act
        var rating = new Rating(validBase);

        // Assert
        Assert.Equal(1, rating.Base);
    }

    [Fact]
    public void It_should_allow_base_of_20()
    {
        // Arrange
        var validBase = 20;

        // Act
        var rating = new Rating(validBase);

        // Assert
        Assert.Equal(20, rating.Base);
    }
}
