using QuestWorlds.Framing;

namespace QuestWorlds.Framing.Tests;

public class When_parsing_rating_5M_should_create_rating_with_base_5_and_one_mastery
{
    [Fact]
    public void It_should_have_base_5()
    {
        // Arrange
        var notation = "5M";

        // Act
        var rating = Rating.Parse(notation);

        // Assert
        Assert.Equal(5, rating.Base);
    }

    [Fact]
    public void It_should_have_one_mastery()
    {
        // Arrange
        var notation = "5M";

        // Act
        var rating = Rating.Parse(notation);

        // Assert
        Assert.Equal(1, rating.Masteries);
    }

    [Fact]
    public void It_should_format_as_5M_when_converted_to_string()
    {
        // Arrange
        var notation = "5M";

        // Act
        var rating = Rating.Parse(notation);

        // Assert
        Assert.Equal("5M", rating.ToString());
    }
}
