using Xunit;

namespace QuestWorlds.Framing.Tests;

public class When_creating_contest_frame_should_require_prize_and_resistance
{
    [Fact]
    public void When_creating_with_valid_prize_and_resistance_should_succeed()
    {
        // Arrange
        var prize = "Sneak past guards";
        var resistance = new TargetNumber(10);

        // Act
        var frame = new ContestFrame(prize, resistance);

        // Assert
        Assert.Equal(prize, frame.Prize);
        Assert.Equal(resistance, frame.Resistance);
    }

    [Fact]
    public void When_creating_with_empty_prize_should_throw_argument_exception()
    {
        // Arrange
        var emptyPrize = "";
        var resistance = new TargetNumber(10);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new ContestFrame(emptyPrize, resistance));
        Assert.Contains("prize", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void When_creating_with_whitespace_prize_should_throw_argument_exception()
    {
        // Arrange
        var whitespacePrize = "   ";
        var resistance = new TargetNumber(10);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new ContestFrame(whitespacePrize, resistance));
        Assert.Contains("prize", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void When_creating_with_null_prize_should_throw_argument_exception()
    {
        // Arrange
        string? nullPrize = null;
        var resistance = new TargetNumber(10);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new ContestFrame(nullPrize!, resistance));
        Assert.Contains("prize", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
