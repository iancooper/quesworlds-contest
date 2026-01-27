using QuestWorlds.Framing;

namespace QuestWorlds.Framing.Tests;

public class When_parsing_rating_15_should_create_rating_with_base_15_and_no_masteries
{
    [Fact]
    public void It_should_have_base_15()
    {
        // Arrange
        var notation = "15";

        // Act
        var rating = Rating.Parse(notation);

        // Assert
        Assert.Equal(15, rating.Base);
    }

    [Fact]
    public void It_should_have_zero_masteries()
    {
        // Arrange
        var notation = "15";

        // Act
        var rating = Rating.Parse(notation);

        // Assert
        Assert.Equal(0, rating.Masteries);
    }

    [Fact]
    public void It_should_format_as_15_when_converted_to_string()
    {
        // Arrange
        var notation = "15";

        // Act
        var rating = Rating.Parse(notation);

        // Assert
        Assert.Equal("15", rating.ToString());
    }
}
