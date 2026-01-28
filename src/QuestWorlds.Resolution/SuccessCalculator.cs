using QuestWorlds.Framing;

namespace QuestWorlds.Resolution;

/// <summary>
/// Calculates the number of successes for a dice roll against a target number.
/// </summary>
internal class SuccessCalculator
{
    private readonly TargetNumber _targetNumber;

    public SuccessCalculator(TargetNumber targetNumber)
    {
        _targetNumber = targetNumber;
    }

    public int Determine(int roll)
    {
        int baseSuccesses;

        if (roll == _targetNumber.EffectiveBase)
            baseSuccesses = 2; // Big success
        else if (roll < _targetNumber.EffectiveBase)
            baseSuccesses = 1;
        else
            baseSuccesses = 0;

        return baseSuccesses + _targetNumber.Masteries;
    }
}

