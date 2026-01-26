using QuestWorlds.Framing;

namespace QuestWorlds.Framing.Tests;

public class When_creating_stretch_modifier_with_positive_value_should_throw
{
    [Fact]
    public void It_should_succeed_with_negative_5()
    {
        // Arrange
        var type = ModifierType.Stretch;
        var value = -5;

        // Act
        var modifier = new Modifier(type, value);

        // Assert
        Assert.Equal(ModifierType.Stretch, modifier.Type);
        Assert.Equal(-5, modifier.Value);
    }

    [Fact]
    public void It_should_succeed_with_negative_10()
    {
        // Arrange
        var type = ModifierType.Stretch;
        var value = -10;

        // Act
        var modifier = new Modifier(type, value);

        // Assert
        Assert.Equal(ModifierType.Stretch, modifier.Type);
        Assert.Equal(-10, modifier.Value);
    }

    [Fact]
    public void It_should_throw_with_positive_5()
    {
        // Arrange
        var type = ModifierType.Stretch;
        var value = 5;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Modifier(type, value));
    }

    [Fact]
    public void It_should_throw_with_positive_10()
    {
        // Arrange
        var type = ModifierType.Stretch;
        var value = 10;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Modifier(type, value));
    }
}
