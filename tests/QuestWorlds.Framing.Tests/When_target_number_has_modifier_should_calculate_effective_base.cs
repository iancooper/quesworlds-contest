using QuestWorlds.Framing;

namespace QuestWorlds.Framing.Tests;

public class When_target_number_has_modifier_should_calculate_effective_base
{
    [Fact]
    public void It_should_add_positive_modifier_to_base()
    {
        // Arrange
        var tn = new TargetNumber(10, 0, 5);

        // Act
        var effectiveBase = tn.EffectiveBase;

        // Assert
        Assert.Equal(15, effectiveBase);
    }

    [Fact]
    public void It_should_subtract_negative_modifier_from_base()
    {
        // Arrange
        var tn = new TargetNumber(10, 0, -5);

        // Act
        var effectiveBase = tn.EffectiveBase;

        // Assert
        Assert.Equal(5, effectiveBase);
    }

    [Fact]
    public void It_should_clamp_effective_base_to_maximum_20()
    {
        // Arrange
        var tn = new TargetNumber(18, 0, 5);

        // Act
        var effectiveBase = tn.EffectiveBase;

        // Assert
        Assert.Equal(20, effectiveBase);
    }

    [Fact]
    public void It_should_clamp_effective_base_to_minimum_1()
    {
        // Arrange
        var tn = new TargetNumber(3, 0, -5);

        // Act
        var effectiveBase = tn.EffectiveBase;

        // Assert
        Assert.Equal(1, effectiveBase);
    }

    [Fact]
    public void It_should_preserve_masteries()
    {
        // Arrange
        var tn = new TargetNumber(10, 2, 5);

        // Act & Assert
        Assert.Equal(2, tn.Masteries);
    }

    [Fact]
    public void It_should_preserve_original_base()
    {
        // Arrange
        var tn = new TargetNumber(10, 0, 5);

        // Act & Assert
        Assert.Equal(10, tn.Base);
    }
}
