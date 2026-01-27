using Xunit;

namespace QuestWorlds.Framing.Tests;

public class When_getting_player_target_number_should_include_all_modifiers
{
    [Fact]
    public void When_no_modifiers_should_return_target_number_from_rating()
    {
        // Arrange
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(10));
        frame.SetPlayerAbility("Stealth", new Rating(15));

        // Act
        var targetNumber = frame.GetPlayerTargetNumber();

        // Assert
        Assert.NotNull(targetNumber);
        Assert.Equal(15, targetNumber!.Value.EffectiveBase);
        Assert.Equal(0, targetNumber.Value.Masteries);
    }

    [Fact]
    public void When_single_positive_modifier_should_add_to_base()
    {
        // Arrange
        var frame = new ContestFrame("Convince the merchant", new TargetNumber(10));
        frame.SetPlayerAbility("Bargaining", new Rating(10));
        frame.ApplyModifier(new Modifier(ModifierType.Augment, 5));

        // Act
        var targetNumber = frame.GetPlayerTargetNumber();

        // Assert
        Assert.Equal(15, targetNumber!.Value.EffectiveBase);
    }

    [Fact]
    public void When_single_negative_modifier_should_subtract_from_base()
    {
        // Arrange
        var frame = new ContestFrame("Climb the wall", new TargetNumber(10));
        frame.SetPlayerAbility("Athletics", new Rating(15));
        frame.ApplyModifier(new Modifier(ModifierType.Stretch, -5));

        // Act
        var targetNumber = frame.GetPlayerTargetNumber();

        // Assert
        Assert.Equal(10, targetNumber!.Value.EffectiveBase);
    }

    [Fact]
    public void When_augment_and_stretch_cancel_out_should_return_original_base()
    {
        // Arrange
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(10));
        frame.SetPlayerAbility("Stealth", new Rating(12));
        frame.ApplyModifier(new Modifier(ModifierType.Augment, 5));
        frame.ApplyModifier(new Modifier(ModifierType.Stretch, -5));

        // Act
        var targetNumber = frame.GetPlayerTargetNumber();

        // Assert
        Assert.Equal(12, targetNumber!.Value.EffectiveBase);
    }

    [Fact]
    public void When_multiple_modifiers_should_sum_all_values()
    {
        // Arrange
        var frame = new ContestFrame("Epic battle", new TargetNumber(15));
        frame.SetPlayerAbility("Combat", new Rating(10));
        frame.ApplyModifier(new Modifier(ModifierType.Augment, 5));
        frame.ApplyModifier(new Modifier(ModifierType.Augment, 10));
        frame.ApplyModifier(new Modifier(ModifierType.Hindrance, -5));

        // Act
        var targetNumber = frame.GetPlayerTargetNumber();

        // Assert
        Assert.Equal(20, targetNumber!.Value.EffectiveBase); // 10 + 5 + 10 - 5 = 20
    }

    [Fact]
    public void When_modifiers_exceed_max_should_clamp_to_20()
    {
        // Arrange
        var frame = new ContestFrame("Impossible task", new TargetNumber(20));
        frame.SetPlayerAbility("Mastery", new Rating(18));
        frame.ApplyModifier(new Modifier(ModifierType.Augment, 10));

        // Act
        var targetNumber = frame.GetPlayerTargetNumber();

        // Assert
        Assert.Equal(20, targetNumber!.Value.EffectiveBase); // 18 + 10 = 28, clamped to 20
    }

    [Fact]
    public void When_modifiers_below_min_should_clamp_to_1()
    {
        // Arrange
        var frame = new ContestFrame("Desperate attempt", new TargetNumber(10));
        frame.SetPlayerAbility("Weak skill", new Rating(3));
        frame.ApplyModifier(new Modifier(ModifierType.Stretch, -10));

        // Act
        var targetNumber = frame.GetPlayerTargetNumber();

        // Assert
        Assert.Equal(1, targetNumber!.Value.EffectiveBase); // 3 - 10 = -7, clamped to 1
    }

    [Fact]
    public void When_no_player_ability_set_should_return_null()
    {
        // Arrange
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(10));

        // Act
        var targetNumber = frame.GetPlayerTargetNumber();

        // Assert
        Assert.Null(targetNumber);
    }

    [Fact]
    public void When_rating_has_masteries_should_preserve_masteries()
    {
        // Arrange
        var frame = new ContestFrame("Face the dragon", new TargetNumber(15, 1));
        frame.SetPlayerAbility("Dragon Slaying", new Rating(5, 2));
        frame.ApplyModifier(new Modifier(ModifierType.Augment, 5));

        // Act
        var targetNumber = frame.GetPlayerTargetNumber();

        // Assert
        Assert.Equal(10, targetNumber!.Value.EffectiveBase); // 5 + 5 = 10
        Assert.Equal(2, targetNumber.Value.Masteries);
    }
}
