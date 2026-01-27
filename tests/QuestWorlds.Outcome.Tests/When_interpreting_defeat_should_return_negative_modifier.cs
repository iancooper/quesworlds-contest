using QuestWorlds.Framing;
using QuestWorlds.Outcome;
using QuestWorlds.Resolution;

namespace QuestWorlds.Outcome.Tests;

public class When_interpreting_defeat_should_return_negative_modifier
{
    [Theory]
    [InlineData(0, -5)]   // Marginal defeat → -5
    [InlineData(1, -10)]  // Minor defeat → -10
    [InlineData(2, -15)]  // Major defeat → -15
    [InlineData(3, -20)]  // Complete defeat → -20
    [InlineData(4, -20)]  // Degree 4+ still capped at -20
    public void Defeat_degree_maps_to_correct_modifier(int degree, int expectedModifier)
    {
        // Arrange
        var frame = new ContestFrame("Sneak past guards", new TargetNumber(14, 0, 0));
        frame.SetPlayerAbility("Stealth", new Rating(15, 0));

        var result = new ResolutionResult
        {
            PlayerRoll = 15,
            ResistanceRoll = 10,
            PlayerSuccesses = 1,
            ResistanceSuccesses = 1 + degree, // Enough to give this degree
            Winner = ContestWinner.Resistance,
            Degree = degree
        };

        var interpreter = new OutcomeInterpreter();

        // Act
        var outcome = interpreter.Interpret(result, frame);

        // Assert
        Assert.Equal(expectedModifier, outcome.BenefitConsequenceModifier);
    }
}
