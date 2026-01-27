using QuestWorlds.Framing;
using QuestWorlds.Outcome;
using QuestWorlds.Resolution;

namespace QuestWorlds.Outcome.Tests;

public class When_interpreting_should_include_contest_context
{
    [Fact]
    public void Outcome_includes_prize_from_frame()
    {
        // Arrange
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(14, 0, 0));
        frame.SetPlayerAbility("Stealth", new Rating(15, 0));

        var result = new ResolutionResult
        {
            PlayerRoll = 10,
            ResistanceRoll = 15,
            PlayerSuccesses = 1,
            ResistanceSuccesses = 0,
            Winner = ContestWinner.Player,
            Degree = 1
        };

        var interpreter = new OutcomeInterpreter();

        // Act
        var outcome = interpreter.Interpret(result, frame);

        // Assert
        Assert.Equal("Sneak past guards", outcome.Prize);
    }

    [Fact]
    public void Outcome_includes_player_ability_name()
    {
        // Arrange
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(14, 0, 0));
        frame.SetPlayerAbility("Stealth", new Rating(15, 0));

        var result = new ResolutionResult
        {
            PlayerRoll = 10,
            ResistanceRoll = 15,
            PlayerSuccesses = 1,
            ResistanceSuccesses = 0,
            Winner = ContestWinner.Player,
            Degree = 1
        };

        var interpreter = new OutcomeInterpreter();

        // Act
        var outcome = interpreter.Interpret(result, frame);

        // Assert
        Assert.Equal("Stealth", outcome.PlayerAbilityName);
    }

    [Fact]
    public void Outcome_includes_player_rating_formatted()
    {
        // Arrange
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(14, 0, 0));
        frame.SetPlayerAbility("Stealth", new Rating(5, 2)); // 5M2

        var result = new ResolutionResult
        {
            PlayerRoll = 10,
            ResistanceRoll = 15,
            PlayerSuccesses = 3,
            ResistanceSuccesses = 0,
            Winner = ContestWinner.Player,
            Degree = 3
        };

        var interpreter = new OutcomeInterpreter();

        // Act
        var outcome = interpreter.Interpret(result, frame);

        // Assert
        Assert.Equal("5M2", outcome.PlayerRating);
    }

    [Fact]
    public void Outcome_includes_resistance_target_number()
    {
        // Arrange
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(14, 1, 0)); // 14M
        frame.SetPlayerAbility("Stealth", new Rating(15, 0));

        var result = new ResolutionResult
        {
            PlayerRoll = 10,
            ResistanceRoll = 15,
            PlayerSuccesses = 1,
            ResistanceSuccesses = 2,
            Winner = ContestWinner.Resistance,
            Degree = 1
        };

        var interpreter = new OutcomeInterpreter();

        // Act
        var outcome = interpreter.Interpret(result, frame);

        // Assert
        Assert.Equal("14M", outcome.ResistanceTargetNumber);
    }

    [Fact]
    public void Outcome_includes_resolution_rolls()
    {
        // Arrange
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(14, 0, 0));
        frame.SetPlayerAbility("Stealth", new Rating(15, 0));

        var result = new ResolutionResult
        {
            PlayerRoll = 8,
            ResistanceRoll = 17,
            PlayerSuccesses = 1,
            ResistanceSuccesses = 0,
            Winner = ContestWinner.Player,
            Degree = 1
        };

        var interpreter = new OutcomeInterpreter();

        // Act
        var outcome = interpreter.Interpret(result, frame);

        // Assert
        Assert.Equal(8, outcome.PlayerRoll);
        Assert.Equal(17, outcome.ResistanceRoll);
    }

    [Fact]
    public void Outcome_includes_success_counts()
    {
        // Arrange
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(14, 0, 0));
        frame.SetPlayerAbility("Stealth", new Rating(15, 0));

        var result = new ResolutionResult
        {
            PlayerRoll = 10,
            ResistanceRoll = 15,
            PlayerSuccesses = 2,
            ResistanceSuccesses = 1,
            Winner = ContestWinner.Player,
            Degree = 1
        };

        var interpreter = new OutcomeInterpreter();

        // Act
        var outcome = interpreter.Interpret(result, frame);

        // Assert
        Assert.Equal(2, outcome.PlayerSuccesses);
        Assert.Equal(1, outcome.ResistanceSuccesses);
    }

    [Fact]
    public void Outcome_includes_winner_and_degree()
    {
        // Arrange
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(14, 0, 0));
        frame.SetPlayerAbility("Stealth", new Rating(15, 0));

        var result = new ResolutionResult
        {
            PlayerRoll = 10,
            ResistanceRoll = 15,
            PlayerSuccesses = 3,
            ResistanceSuccesses = 1,
            Winner = ContestWinner.Player,
            Degree = 2
        };

        var interpreter = new OutcomeInterpreter();

        // Act
        var outcome = interpreter.Interpret(result, frame);

        // Assert
        Assert.Equal(ContestWinner.Player, outcome.Winner);
        Assert.Equal(2, outcome.Degree);
        Assert.True(outcome.IsPlayerVictory);
    }

    [Fact]
    public void Victory_summary_includes_degree()
    {
        // Arrange
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(14, 0, 0));
        frame.SetPlayerAbility("Stealth", new Rating(15, 0));

        var result = new ResolutionResult
        {
            PlayerRoll = 10,
            ResistanceRoll = 15,
            PlayerSuccesses = 2,
            ResistanceSuccesses = 0,
            Winner = ContestWinner.Player,
            Degree = 2
        };

        var interpreter = new OutcomeInterpreter();

        // Act
        var outcome = interpreter.Interpret(result, frame);

        // Assert
        Assert.Contains("2", outcome.Summary);
        Assert.Contains("Victory", outcome.Summary);
    }

    [Fact]
    public void Defeat_summary_includes_degree()
    {
        // Arrange
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(14, 0, 0));
        frame.SetPlayerAbility("Stealth", new Rating(15, 0));

        var result = new ResolutionResult
        {
            PlayerRoll = 18,
            ResistanceRoll = 10,
            PlayerSuccesses = 0,
            ResistanceSuccesses = 1,
            Winner = ContestWinner.Resistance,
            Degree = 1
        };

        var interpreter = new OutcomeInterpreter();

        // Act
        var outcome = interpreter.Interpret(result, frame);

        // Assert
        Assert.Contains("1", outcome.Summary);
        Assert.Contains("Defeat", outcome.Summary);
    }

    [Fact]
    public void Tie_summary_indicates_tie()
    {
        // Arrange
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(14, 0, 0));
        frame.SetPlayerAbility("Stealth", new Rating(15, 0));

        var result = new ResolutionResult
        {
            PlayerRoll = 10,
            ResistanceRoll = 10,
            PlayerSuccesses = 1,
            ResistanceSuccesses = 1,
            Winner = ContestWinner.Tie,
            Degree = 0
        };

        var interpreter = new OutcomeInterpreter();

        // Act
        var outcome = interpreter.Interpret(result, frame);

        // Assert
        Assert.Contains("tie", outcome.Summary.ToLower());
    }
}
