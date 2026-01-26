# 0004. Resolution Module

Date: 2026-01-26

## Status

Proposed

## Context

**Parent Requirement**: [specs/0001-questworlds-contest/requirements.md](../../specs/0001-questworlds-contest/requirements.md)

**Scope**: This ADR focuses on the Resolution module design, which handles dice rolling, success calculation, and winner determination. See [ADR-0001](0001-user-interface-architecture.md) for overall architecture and [ADR-0003](0003-framing-module.md) for the ContestFrame input.

The Resolution module must:

- Roll 1D20 for both player and resistance
- Calculate successes based on QuestWorlds rules:
  - Roll below TN = 1 success
  - Roll equal to TN = 2 successes (big success)
  - Roll above TN = 0 successes
  - Each mastery adds 1 success
- Determine the winner (more successes wins)
- Handle ties (higher roll wins)
- Calculate the degree of victory/defeat (difference in successes)

Key forces:

- Dice rolling must be testable (need to inject randomness)
- Success calculation rules are well-defined and deterministic
- The module receives a complete ContestFrame from the Framing module
- Results must be immutable for display and audit purposes

## Decision

We will implement the Resolution module with a clear separation between dice rolling (injectable) and success calculation (pure functions), producing an immutable ResolutionResult.

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                   QuestWorlds.Resolution                        │
│                                                                 │
│  Input: ContestFrame (from Framing module)                      │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │           IDiceResolver (Role: Dice Resolver)            │   │
│  │  - Resolve(ContestFrame) → ResolutionResult              │   │
│  └─────────────────────────┬───────────────────────────────┘   │
│                            │                                    │
│           ┌────────────────┼────────────────┐                  │
│           ▼                ▼                ▼                  │
│  ┌─────────────┐  ┌─────────────────┐  ┌─────────────────┐    │
│  │ IDiceRoller │  │ ISuccessCalc    │  │ IWinnerDecider  │    │
│  │ (Randomness)│  │ (Pure Logic)    │  │ (Comparison)    │    │
│  └─────────────┘  └─────────────────┘  └─────────────────┘    │
│                                                                 │
│  Output:                                                        │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              ResolutionResult (Immutable)                │   │
│  │  - PlayerRoll, ResistanceRoll                            │   │
│  │  - PlayerSuccesses, ResistanceSuccesses                  │   │
│  │  - Winner (Player/Resistance/Tie)                        │   │
│  │  - Degree (0-3+)                                         │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### Key Components

#### Roles and Responsibilities

**IDiceResolver** (Role: Dice Resolver - Orchestrator)

- **Knowing**: Nothing (stateless)
- **Doing**: Coordinate rolling, success calculation, and winner determination
- **Deciding**: Nothing (delegates to collaborators)

**IDiceRoller** (Role: Randomness Provider)

- **Knowing**: Nothing
- **Doing**: Generate random D20 rolls (1-20)
- **Deciding**: Nothing

**ISuccessCalculator** (Role: Success Calculator)

- **Knowing**: QuestWorlds success rules
- **Doing**: Calculate successes from roll and target number
- **Deciding**: Nothing (pure calculation)

**IWinnerDecider** (Role: Winner Decider)

- **Knowing**: Comparison and tie-breaking rules
- **Doing**: Compare successes, apply tie-breaker
- **Deciding**: Who wins and by how much

**ResolutionResult** (Value Object)

- **Knowing**: All resolution data (rolls, successes, winner, degree)
- **Doing**: Nothing
- **Deciding**: Nothing

### Domain Model

```csharp
namespace QuestWorlds.Resolution;

public enum ContestWinner
{
    Player,
    Resistance,
    Tie
}

/// <summary>
/// Immutable result of contest resolution.
/// </summary>
public sealed record ResolutionResult
{
    public int PlayerRoll { get; init; }
    public int ResistanceRoll { get; init; }
    public int PlayerSuccesses { get; init; }
    public int ResistanceSuccesses { get; init; }
    public ContestWinner Winner { get; init; }
    public int Degree { get; init; }

    /// <summary>
    /// True if the player achieved a "big success" (roll == TN).
    /// </summary>
    public bool PlayerBigSuccess { get; init; }

    /// <summary>
    /// True if the resistance achieved a "big success" (roll == TN).
    /// </summary>
    public bool ResistanceBigSuccess { get; init; }
}

/// <summary>
/// Intermediate result of a single die roll evaluation.
/// </summary>
public readonly record struct RollResult(
    int Roll,
    int TargetNumber,
    int Masteries,
    int BaseSuccesses,
    int TotalSuccesses,
    bool IsBigSuccess
);
```

### Interfaces

```csharp
namespace QuestWorlds.Resolution;

/// <summary>
/// Provides random D20 rolls. Injectable for testing.
/// </summary>
public interface IDiceRoller
{
    int RollD20();
}

/// <summary>
/// Calculates successes from a roll and target number.
/// </summary>
public interface ISuccessCalculator
{
    RollResult Calculate(int roll, TargetNumber targetNumber);
}

/// <summary>
/// Determines the winner and degree of victory.
/// </summary>
public interface IWinnerDecider
{
    (ContestWinner Winner, int Degree) Decide(
        int playerSuccesses, int playerRoll,
        int resistanceSuccesses, int resistanceRoll);
}

/// <summary>
/// Resolves a contest by rolling dice and determining the outcome.
/// </summary>
public interface IDiceResolver
{
    ResolutionResult Resolve(ContestFrame frame);
}
```

### Implementations

```csharp
namespace QuestWorlds.Resolution;

public class DiceRoller : IDiceRoller
{
    private readonly Random _random = Random.Shared;

    public int RollD20() => _random.Next(1, 21);
}

public class SuccessCalculator : ISuccessCalculator
{
    public RollResult Calculate(int roll, TargetNumber targetNumber)
    {
        var effectiveBase = targetNumber.EffectiveBase;

        // Determine base successes from the roll
        int baseSuccesses;
        bool isBigSuccess = false;

        if (roll == effectiveBase)
        {
            baseSuccesses = 2; // Big success
            isBigSuccess = true;
        }
        else if (roll < effectiveBase)
        {
            baseSuccesses = 1; // Success
        }
        else
        {
            baseSuccesses = 0; // Failure
        }

        // Add mastery successes
        var totalSuccesses = baseSuccesses + targetNumber.Masteries;

        return new RollResult(
            Roll: roll,
            TargetNumber: effectiveBase,
            Masteries: targetNumber.Masteries,
            BaseSuccesses: baseSuccesses,
            TotalSuccesses: totalSuccesses,
            IsBigSuccess: isBigSuccess
        );
    }
}

public class WinnerDecider : IWinnerDecider
{
    public (ContestWinner Winner, int Degree) Decide(
        int playerSuccesses, int playerRoll,
        int resistanceSuccesses, int resistanceRoll)
    {
        var difference = playerSuccesses - resistanceSuccesses;

        if (difference > 0)
        {
            return (ContestWinner.Player, difference);
        }
        else if (difference < 0)
        {
            return (ContestWinner.Resistance, Math.Abs(difference));
        }
        else
        {
            // Tie-breaker: higher roll wins with 0 degree
            if (playerRoll > resistanceRoll)
                return (ContestWinner.Player, 0);
            else if (resistanceRoll > playerRoll)
                return (ContestWinner.Resistance, 0);
            else
                return (ContestWinner.Tie, 0);
        }
    }
}

public class DiceResolver : IDiceResolver
{
    private readonly IDiceRoller _diceRoller;
    private readonly ISuccessCalculator _successCalculator;
    private readonly IWinnerDecider _winnerDecider;

    public DiceResolver(
        IDiceRoller diceRoller,
        ISuccessCalculator successCalculator,
        IWinnerDecider winnerDecider)
    {
        _diceRoller = diceRoller;
        _successCalculator = successCalculator;
        _winnerDecider = winnerDecider;
    }

    public ResolutionResult Resolve(ContestFrame frame)
    {
        if (!frame.IsReadyForResolution)
            throw new InvalidOperationException("Contest frame is not ready for resolution");

        // Roll dice
        var playerRoll = _diceRoller.RollD20();
        var resistanceRoll = _diceRoller.RollD20();

        // Calculate successes
        var playerResult = _successCalculator.Calculate(
            playerRoll,
            frame.GetPlayerTargetNumber()!.Value);

        var resistanceResult = _successCalculator.Calculate(
            resistanceRoll,
            frame.Resistance);

        // Determine winner
        var (winner, degree) = _winnerDecider.Decide(
            playerResult.TotalSuccesses, playerRoll,
            resistanceResult.TotalSuccesses, resistanceRoll);

        return new ResolutionResult
        {
            PlayerRoll = playerRoll,
            ResistanceRoll = resistanceRoll,
            PlayerSuccesses = playerResult.TotalSuccesses,
            ResistanceSuccesses = resistanceResult.TotalSuccesses,
            PlayerBigSuccess = playerResult.IsBigSuccess,
            ResistanceBigSuccess = resistanceResult.IsBigSuccess,
            Winner = winner,
            Degree = degree
        };
    }
}
```

### Testing Strategy

The separation of concerns enables focused testing:

```csharp
// Test success calculation with known inputs
[Theory]
[InlineData(5, 10, 0, 1)]   // Roll 5 vs TN 10 = 1 success
[InlineData(10, 10, 0, 2)]  // Roll 10 vs TN 10 = 2 successes (big)
[InlineData(15, 10, 0, 0)]  // Roll 15 vs TN 10 = 0 successes
[InlineData(5, 10, 1, 2)]   // Roll 5 vs TN 10 + 1M = 2 successes
[InlineData(5, 10, 2, 3)]   // Roll 5 vs TN 10 + 2M = 3 successes
public void SuccessCalculator_CalculatesCorrectly(
    int roll, int tn, int masteries, int expected)
{
    var calc = new SuccessCalculator();
    var result = calc.Calculate(roll, new TargetNumber(tn, masteries));
    Assert.Equal(expected, result.TotalSuccesses);
}

// Test with fake dice roller for deterministic results
public class FakeDiceRoller : IDiceRoller
{
    private readonly Queue<int> _rolls;
    public FakeDiceRoller(params int[] rolls) => _rolls = new Queue<int>(rolls);
    public int RollD20() => _rolls.Dequeue();
}
```

## Consequences

### Positive

- **Testable**: IDiceRoller injection allows deterministic testing
- **Single Responsibility**: Each component has one job
- **Immutable Results**: ResolutionResult cannot be accidentally modified
- **Clear Contract**: Well-defined input (ContestFrame) and output (ResolutionResult)

### Negative

- **Multiple Small Classes**: More files to maintain
- **Indirection**: Orchestrator delegates to multiple collaborators
- **Dependency Injection**: Requires wiring up dependencies

### Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Random.Shared thread safety | Random.Shared is thread-safe in .NET 6+ |
| Edge case in success calculation | Comprehensive unit tests for all combinations |
| Tie-breaking rule confusion | Clear documentation and tests |

## Alternatives Considered

### 1. Single Monolithic Resolver

All logic in one class. Rejected because:

- Harder to test (would need to mock Random)
- Less clear separation of concerns
- Violates single responsibility principle

### 2. Static Methods for Calculation

Use static utility methods instead of interfaces. Rejected because:

- Still need interface for dice rolling (testability)
- Consistency: all roles as interfaces
- Easier to extend/modify later

### 3. Return Tuple Instead of ResolutionResult

Return `(int, int, int, int, ...)` instead of a record. Rejected because:

- Less readable
- No named properties
- Harder to extend

## References

- Requirements: [specs/0001-questworlds-contest/requirements.md](../../specs/0001-questworlds-contest/requirements.md)
- Related ADRs:
  - [0001-user-interface-architecture.md](0001-user-interface-architecture.md) - Overall architecture
  - [0002-session-management.md](0002-session-management.md) - Session management
  - [0003-framing-module.md](0003-framing-module.md) - ContestFrame input
  - (Planned) 0005-outcome-module.md - Consumes ResolutionResult
- External references:
  - [QuestWorlds SRD - Simple Contests](https://questworlds.chaosium.com/)
