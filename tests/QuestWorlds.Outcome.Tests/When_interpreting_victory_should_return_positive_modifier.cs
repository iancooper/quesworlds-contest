using QuestWorlds.Framing;
using QuestWorlds.Outcome;
using QuestWorlds.Resolution;

namespace QuestWorlds.Outcome.Tests;

public class When_interpreting_victory_should_return_positive_modifier
{
    [Theory]
    [InlineData(0, 5)]   // Marginal victory → +5
    [InlineData(1, 10)]  // Minor victory → +10
    [InlineData(2, 15)]  // Major victory → +15
    [InlineData(3, 20)]  // Complete victory → +20
    [InlineData(4, 20)]  // Degree 4+ still capped at +20
    public void Victory_degree_maps_to_correct_modifier(int degree, int expectedModifier)
    {
        // Arrange
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(14, 0, 0));
        frame.SetPlayerAbility("Stealth", new Rating(15, 0));

        var result = new ResolutionResult
        {
            PlayerRoll = 10,
            ResistanceRoll = 15,
            PlayerSuccesses = 2 + degree, // Enough to give this degree
            ResistanceSuccesses = 2,
            Winner = ContestWinner.Player,
            Degree = degree
        };

        var interpreter = new OutcomeInterpreter();

        // Act
        var outcome = interpreter.Interpret(result, frame);

        // Assert
        Assert.Equal(expectedModifier, outcome.BenefitConsequenceModifier);
    }
}
