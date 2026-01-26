# 0004. Resolution Module

Date: 2026-01-26

## Status

Accepted

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
│  │           IContestResolver (Role: Dice Resolver)            │   │
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

**IContestResolver** (Role: Contest Resolver)

- **Knowing**: QuestWorlds success rules, comparison and tie-breaking rules
- **Doing**: Calculate successes from rolls, determine winner, generate random rolls
- **Deciding**: Who wins and by how much

**DiceRolls** (Value Object)

- **Knowing**: Player roll and resistance roll values
- **Doing**: Nothing
- **Deciding**: Nothing

**ResolutionResult** (Value Object)

- **Knowing**: All resolution data (rolls, successes, winner, degree)
- **Doing**: Nothing
- **Deciding**: Nothing

Note: Internal helper methods or classes for success calculation and winner determination may emerge through refactoring as the implementation grows.

### Domain Model

```csharp
namespace QuestWorlds.Resolution;

/// <summary>
/// Represents a pair of D20 dice rolls for a contest.
/// </summary>
public readonly record struct DiceRolls(int PlayerRoll, int ResistanceRoll);

/// <summary>
/// The winner of a contest.
/// </summary>
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
}
```

### Interfaces

```csharp
namespace QuestWorlds.Resolution;

/// <summary>
/// Resolves a contest by determining the outcome from dice rolls.
/// </summary>
public interface IContestResolver
{
    /// <summary>
    /// Resolves a contest with the given dice rolls. Deterministic and testable.
    /// </summary>
    ResolutionResult Resolve(ContestFrame frame, DiceRolls rolls);

    /// <summary>
    /// Resolves a contest by rolling dice internally. Convenience method for production use.
    /// </summary>
    ResolutionResult Resolve(ContestFrame frame);
}
```

### Implementations

The implementation starts simple. Internal helper classes may emerge through refactoring as complexity grows.

```csharp
namespace QuestWorlds.Resolution;

public class ContestResolver : IContestResolver
{
    /// <summary>
    /// Resolves a contest with known dice rolls. Deterministic and testable.
    /// </summary>
    public ResolutionResult Resolve(ContestFrame frame, DiceRolls rolls)
    {
        if (!frame.IsReadyForResolution)
            throw new InvalidOperationException("Contest frame is not ready for resolution");

        // Calculate successes for each side
        var playerSuccesses = CalculateSuccesses(rolls.PlayerRoll, frame.GetPlayerTargetNumber()!.Value);
        var resistanceSuccesses = CalculateSuccesses(rolls.ResistanceRoll, frame.Resistance);

        // Determine winner
        var (winner, degree) = DecideWinner(
            playerSuccesses, rolls.PlayerRoll,
            resistanceSuccesses, rolls.ResistanceRoll);

        return new ResolutionResult
        {
            PlayerRoll = rolls.PlayerRoll,
            ResistanceRoll = rolls.ResistanceRoll,
            PlayerSuccesses = playerSuccesses,
            ResistanceSuccesses = resistanceSuccesses,
            Winner = winner,
            Degree = degree
        };
    }

    /// <summary>
    /// Resolves a contest by rolling dice internally.
    /// </summary>
    public ResolutionResult Resolve(ContestFrame frame)
    {
        var rolls = new DiceRolls(
            PlayerRoll: Random.Shared.Next(1, 21),
            ResistanceRoll: Random.Shared.Next(1, 21));

        return Resolve(frame, rolls);
    }

    private int CalculateSuccesses(int roll, TargetNumber targetNumber)
    {
        // Base successes from the roll
        int baseSuccesses = roll == targetNumber.EffectiveBase ? 2  // Big success
                          : roll < targetNumber.EffectiveBase ? 1   // Success
                          : 0;                                       // Failure

        return baseSuccesses + targetNumber.Masteries;
    }

    private (ContestWinner Winner, int Degree) DecideWinner(
        int playerSuccesses, int playerRoll,
        int resistanceSuccesses, int resistanceRoll)
    {
        var difference = playerSuccesses - resistanceSuccesses;

        if (difference > 0)
            return (ContestWinner.Player, difference);
        if (difference < 0)
            return (ContestWinner.Resistance, Math.Abs(difference));

        // Tie-breaker: higher roll wins with 0 degree
        if (playerRoll > resistanceRoll)
            return (ContestWinner.Player, 0);
        if (resistanceRoll > playerRoll)
            return (ContestWinner.Resistance, 0);

        return (ContestWinner.Tie, 0);
    }
}
```

### Access Modifiers and Encapsulation

Only the resolver interface and result types are public. Internal collaborators enable clean separation but are hidden from consumers.

**Public API** (visible to other modules):

```csharp
public interface IContestResolver { ... }
public readonly record struct DiceRolls { ... }
public sealed record ResolutionResult { ... }
public enum ContestWinner { ... }
```

**Internal Implementation** (hidden from consumers, emerge through refactoring):

```csharp
// Dice rolling is handled internally - no need for public IDiceRoller
// Success calculation and winner determination logic starts in ContestResolver
// and may be extracted to internal helper classes through refactoring
```

**Dependency Injection Registration** (in module):

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResolutionModule(this IServiceCollection services)
    {
        services.AddSingleton<IContestResolver, ContestResolver>();
        return services;
    }
}
```

### Testing Strategy

Tests target `IContestResolver` (public interface). The resolver exposes a deterministic overload that accepts `DiceRolls`, making tests simple and predictable:

```csharp
[Fact]
public void Resolve_WithKnownRolls_ReturnsExpectedOutcome()
{
    var resolver = new ContestResolver();
    var frame = new ContestFrame("Test prize", new TargetNumber(10));
    frame.SetPlayerAbility("Swordfighting", new Rating(15));

    // Player rolls 5 (below TN 15 = success), Resistance rolls 12 (above TN 10 = fail)
    var result = resolver.Resolve(frame, new DiceRolls(5, 12));

    Assert.Equal(ContestWinner.Player, result.Winner);
}
```

**Important**: We do NOT use `InternalsVisibleTo`. Internal classes should emerge through refactoring as the implementation grows, not be test-driven directly. Tests always go through the public `IContestResolver` interface with the deterministic `Resolve(frame, rolls)` overload.

## Consequences

### Positive

- **Testable**: Deterministic overload `Resolve(frame, rolls)` allows predictable testing
- **Simple**: Single class implementation, internal helpers emerge through refactoring
- **Immutable Results**: ResolutionResult cannot be accidentally modified
- **Clear Contract**: Well-defined input (ContestFrame, DiceRolls) and output (ResolutionResult)

### Negative

- **Two Overloads**: Must maintain both deterministic and random versions (minimal overhead)

### Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Random.Shared thread safety | Random.Shared is thread-safe in .NET 6+ |
| Edge case in success calculation | Comprehensive unit tests for all combinations |
| Tie-breaking rule confusion | Clear documentation and tests |

## Alternatives Considered

### 1. Injectable IDiceRoller Interface

Make `IDiceRoller` public and inject it for testing. Rejected because:

- Dice rolling is an implementation detail, not a public concern
- Adds unnecessary complexity to the public API
- The deterministic overload provides simpler testability

### 2. InternalsVisibleTo for Testing

Expose internals to test project. Rejected because:

- Tests should only target public interfaces
- Internal classes should emerge from refactoring, not be test-driven
- Couples tests to implementation details

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
