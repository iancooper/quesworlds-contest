using QuestWorlds.Framing;

namespace QuestWorlds.Framing.Tests;

public class When_rehydrating_a_contest_frame_should_restore_prize_resistance_ability_and_modifiers
{
    private const string STORED_PRIZE = "Sneak past the guards";
    private const string STORED_ABILITY = "Thief of Nochet";

    private static readonly TargetNumber StoredResistance = new(14);
    private static readonly Rating StoredRating = new(6, 2);

    private static readonly Modifier FirstModifier = new(ModifierType.Stretch, -5);
    private static readonly Modifier SecondModifier = new(ModifierType.Augment, 10);
    private static readonly Modifier ThirdModifier = new(ModifierType.Hindrance, -10);

    [Fact]
    public void Rehydrated_frame_should_have_the_stored_prize_and_resistance()
    {
        // Arrange
        var storedModifiers = new[] { FirstModifier };

        // Act
        var frame = ContestFrame.Rehydrate(STORED_PRIZE, StoredResistance, STORED_ABILITY, StoredRating, storedModifiers);

        // Assert
        Assert.Equal(STORED_PRIZE, frame.Prize);
        Assert.Equal(StoredResistance, frame.Resistance);
    }

    [Fact]
    public void Rehydrated_frame_should_have_the_stored_player_ability_and_rating()
    {
        // Arrange
        var storedModifiers = new[] { FirstModifier };

        // Act
        var frame = ContestFrame.Rehydrate(STORED_PRIZE, StoredResistance, STORED_ABILITY, StoredRating, storedModifiers);

        // Assert
        Assert.Equal(STORED_ABILITY, frame.PlayerAbilityName);
        Assert.Equal(StoredRating, frame.PlayerRating);
    }

    [Fact]
    public void Rehydrated_frame_should_have_the_stored_modifiers_in_order()
    {
        // Arrange
        var storedInTheOrderTheGmAppliedThem = new[] { FirstModifier, SecondModifier, ThirdModifier };

        // Act
        var frame = ContestFrame.Rehydrate(STORED_PRIZE, StoredResistance, STORED_ABILITY, StoredRating, storedInTheOrderTheGmAppliedThem);

        // Assert
        Assert.Equal(storedInTheOrderTheGmAppliedThem, frame.Modifiers);
    }

    [Fact]
    public void Rehydrating_a_frame_with_no_player_ability_yet_should_give_back_nulls()
    {
        // Arrange
        var framedButNotYetAnswered = Array.Empty<Modifier>();

        // Act
        var frame = ContestFrame.Rehydrate(STORED_PRIZE, StoredResistance, null, null, framedButNotYetAnswered);

        // Assert
        Assert.Null(frame.PlayerAbilityName);
        Assert.Null(frame.PlayerRating);
        Assert.Empty(frame.Modifiers);
        Assert.False(frame.IsReadyForResolution);
        Assert.Null(frame.GetPlayerTargetNumber());
    }

    [Fact]
    public void Rehydrating_should_not_re_run_the_rules_that_were_satisfied_when_the_frame_was_made()
    {
        // Arrange
        var prizeTheConstructorWouldReject = string.Empty;

        // Act
        var frame = ContestFrame.Rehydrate(prizeTheConstructorWouldReject, StoredResistance, STORED_ABILITY, StoredRating, Array.Empty<Modifier>());

        // Assert
        Assert.Equal(prizeTheConstructorWouldReject, frame.Prize);
    }

    [Fact]
    public void Rehydrated_frame_should_answer_readiness_and_target_number_as_the_original_did()
    {
        // Arrange
        var original = new ContestFrame(STORED_PRIZE, StoredResistance);
        original.SetPlayerAbility(STORED_ABILITY, StoredRating);
        original.ApplyModifier(FirstModifier);
        original.ApplyModifier(SecondModifier);
        original.ApplyModifier(ThirdModifier);

        // Act
        var restored = ContestFrame.Rehydrate(original.Prize, original.Resistance, original.PlayerAbilityName, original.PlayerRating, original.Modifiers);

        // Assert
        Assert.Equal(original.IsReadyForResolution, restored.IsReadyForResolution);
        Assert.Equal(original.GetPlayerTargetNumber(), restored.GetPlayerTargetNumber());
    }
}
