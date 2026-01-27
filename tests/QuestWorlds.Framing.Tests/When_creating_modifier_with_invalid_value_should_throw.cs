using QuestWorlds.Framing;

namespace QuestWorlds.Framing.Tests;

public class When_creating_modifier_with_invalid_value_should_throw
{
    [Theory]
    [InlineData(-10)]
    [InlineData(-5)]
    [InlineData(5)]
    [InlineData(10)]
    public void It_should_allow_valid_values(int value)
    {
        // Arrange
        var type = ModifierType.Situational; // Can be positive or negative

        // Act
        var modifier = new Modifier(type, value);

        // Assert
        Assert.Equal(value, modifier.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(-3)]
    [InlineData(7)]
    [InlineData(-7)]
    [InlineData(15)]
    [InlineData(-15)]
    [InlineData(1)]
    [InlineData(-1)]
    public void It_should_throw_for_invalid_values(int value)
    {
        // Arrange
        var type = ModifierType.Situational;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Modifier(type, value));
    }
}
