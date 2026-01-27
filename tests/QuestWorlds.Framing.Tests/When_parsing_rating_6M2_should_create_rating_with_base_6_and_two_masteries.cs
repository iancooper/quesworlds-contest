using QuestWorlds.Framing;

namespace QuestWorlds.Framing.Tests;

public class When_parsing_rating_6M2_should_create_rating_with_base_6_and_two_masteries
{
    [Fact]
    public void It_should_have_base_6()
    {
        // Arrange
        var notation = "6M2";

        // Act
        var rating = Rating.Parse(notation);

        // Assert
        Assert.Equal(6, rating.Base);
    }

    [Fact]
    public void It_should_have_two_masteries()
    {
        // Arrange
        var notation = "6M2";

        // Act
        var rating = Rating.Parse(notation);

        // Assert
        Assert.Equal(2, rating.Masteries);
    }

    [Fact]
    public void It_should_format_as_6M2_when_converted_to_string()
    {
        // Arrange
        var notation = "6M2";

        // Act
        var rating = Rating.Parse(notation);

        // Assert
        Assert.Equal("6M2", rating.ToString());
    }
}
