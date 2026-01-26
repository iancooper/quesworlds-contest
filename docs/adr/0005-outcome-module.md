# 0005. Outcome Module

Date: 2026-01-26

## Status

Proposed

## Context

**Parent Requirement**: [specs/0001-questworlds-contest/requirements.md](../../specs/0001-questworlds-contest/requirements.md)

**Scope**: This ADR focuses on the Outcome module design, which interprets resolution results into narrative outcomes with benefits and consequences. See [ADR-0001](0001-user-interface-architecture.md) for overall architecture and [ADR-0004](0004-resolution-module.md) for the ResolutionResult input.

The Outcome module must:

- Interpret the ResolutionResult into a human-readable outcome
- Map degree of victory/defeat to descriptive labels (Marginal, Minor, Major, Complete)
- Look up the benefit/consequence modifier based on degree (-20 to +20)
- Provide all information needed for display to GM and players

Key forces:

- The benefit/consequence table is well-defined in QuestWorlds rules
- Outcomes should be immutable and self-contained for display
- The module should be simple - primarily lookup and formatting
- Results must include context from the original contest (prize, abilities)

## Decision

We will implement the Outcome module with a simple interpreter that combines the ResolutionResult with contest context to produce a complete ContestOutcome for display.

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    QuestWorlds.Outcome                          │
│                                                                 │
│  Input: ResolutionResult + ContestFrame                         │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │        IOutcomeInterpreter (Role: Result Interpreter)    │   │
│  │  - Interpret(result, frame) → ContestOutcome             │   │
│  └─────────────────────────┬───────────────────────────────┘   │
│                            │                                    │
│                            ▼                                    │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │            IBenefitConsequenceLookup                     │   │
│  │            (Role: Modifier Table)                        │   │
│  │  - GetModifier(degree, isVictory) → int                  │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Output:                                                        │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              ContestOutcome (Immutable)                  │   │
│  │                                                          │   │
│  │  Context:                                                │   │
│  │  - Prize, PlayerAbilityName, PlayerRating                │   │
│  │  - ResistanceTN                                          │   │
│  │                                                          │   │
│  │  Resolution:                                             │   │
│  │  - PlayerRoll, ResistanceRoll                            │   │
│  │  - PlayerSuccesses, ResistanceSuccesses                  │   │
│  │                                                          │   │
│  │  Outcome:                                                │   │
│  │  - Winner, Degree, DegreeDescription                     │   │
│  │  - IsVictory, BenefitConsequenceModifier                 │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### Key Components

#### Roles and Responsibilities

**IOutcomeInterpreter** (Role: Result Interpreter)

- **Knowing**: How to combine resolution and context
- **Doing**: Create ContestOutcome from inputs
- **Deciding**: Nothing (assembly only)

**IBenefitConsequenceLookup** (Role: Modifier Table)

- **Knowing**: The benefit/consequence table from QuestWorlds rules
- **Doing**: Look up modifier for a given degree and outcome
- **Deciding**: Nothing (pure lookup)

**ContestOutcome** (Value Object)

- **Knowing**: All information needed for display
- **Doing**: Nothing
- **Deciding**: Nothing

### Domain Model

```csharp
namespace QuestWorlds.Outcome;

/// <summary>
/// Descriptive label for degree of victory/defeat.
/// </summary>
public enum DegreeDescription
{
    Marginal,  // 0 degree
    Minor,     // 1 degree
    Major,     // 2 degree
    Complete   // 3+ degree
}

/// <summary>
/// Complete outcome of a contest, ready for display.
/// </summary>
public sealed record ContestOutcome
{
    // Contest context
    public required string Prize { get; init; }
    public required string PlayerAbilityName { get; init; }
    public required string PlayerRating { get; init; }
    public required string ResistanceTargetNumber { get; init; }

    // Resolution details
    public required int PlayerRoll { get; init; }
    public required int ResistanceRoll { get; init; }
    public required int PlayerSuccesses { get; init; }
    public required int ResistanceSuccesses { get; init; }
    public required bool PlayerBigSuccess { get; init; }
    public required bool ResistanceBigSuccess { get; init; }

    // Outcome
    public required ContestWinner Winner { get; init; }
    public required int Degree { get; init; }
    public required DegreeDescription DegreeDescription { get; init; }

    /// <summary>
    /// True if the player won (not resistance, not tie).
    /// </summary>
    public bool IsPlayerVictory => Winner == ContestWinner.Player;

    /// <summary>
    /// The modifier to apply for benefits (victory) or consequences (defeat).
    /// Range: -20 to +20
    /// </summary>
    public required int BenefitConsequenceModifier { get; init; }

    /// <summary>
    /// Human-readable summary of the outcome.
    /// </summary>
    public string Summary => Winner switch
    {
        ContestWinner.Player => $"{DegreeDescription} Victory for the player!",
        ContestWinner.Resistance => $"{DegreeDescription} Defeat for the player.",
        ContestWinner.Tie => "The contest is a tie.",
        _ => "Unknown outcome"
    };
}
```

### Interfaces and Implementation

```csharp
namespace QuestWorlds.Outcome;

/// <summary>
/// Looks up benefit/consequence modifiers from the QuestWorlds table.
/// Internal - implementation detail.
/// </summary>
internal interface IBenefitConsequenceLookup
{
    int GetModifier(int degree, bool isVictory);
}

/// <summary>
/// Interprets resolution results into displayable outcomes.
/// Public - the main entry point for consumers.
/// </summary>
public interface IOutcomeInterpreter
{
    ContestOutcome Interpret(ResolutionResult result, ContestFrame frame);
}

// Internal - implementation detail
internal class BenefitConsequenceLookup : IBenefitConsequenceLookup
{
    // QuestWorlds benefit/consequence table
    // Victory: +5, +10, +15, +20 for degrees 0, 1, 2, 3+
    // Defeat:  -5, -10, -15, -20 for degrees 0, 1, 2, 3+
    private static readonly int[] VictoryModifiers = { 5, 10, 15, 20 };
    private static readonly int[] DefeatModifiers = { -5, -10, -15, -20 };

    public int GetModifier(int degree, bool isVictory)
    {
        // Clamp degree to valid index (0-3)
        var index = Math.Clamp(degree, 0, 3);

        return isVictory
            ? VictoryModifiers[index]
            : DefeatModifiers[index];
    }
}

// Internal - implementation detail
internal class OutcomeInterpreter : IOutcomeInterpreter
{
    private readonly IBenefitConsequenceLookup _lookup;

    public OutcomeInterpreter(IBenefitConsequenceLookup lookup)
    {
        _lookup = lookup;
    }

    public ContestOutcome Interpret(ResolutionResult result, ContestFrame frame)
    {
        var degreeDescription = GetDegreeDescription(result.Degree);

        var isVictory = result.Winner == ContestWinner.Player;
        var modifier = result.Winner == ContestWinner.Tie
            ? 0
            : _lookup.GetModifier(result.Degree, isVictory);

        return new ContestOutcome
        {
            // Context
            Prize = frame.Prize,
            PlayerAbilityName = frame.PlayerAbilityName!,
            PlayerRating = frame.PlayerRating!.Value.ToString(),
            ResistanceTargetNumber = FormatTargetNumber(frame.Resistance),

            // Resolution
            PlayerRoll = result.PlayerRoll,
            ResistanceRoll = result.ResistanceRoll,
            PlayerSuccesses = result.PlayerSuccesses,
            ResistanceSuccesses = result.ResistanceSuccesses,
            PlayerBigSuccess = result.PlayerBigSuccess,
            ResistanceBigSuccess = result.ResistanceBigSuccess,

            // Outcome
            Winner = result.Winner,
            Degree = result.Degree,
            DegreeDescription = degreeDescription,
            BenefitConsequenceModifier = modifier
        };
    }

    private static DegreeDescription GetDegreeDescription(int degree) => degree switch
    {
        0 => DegreeDescription.Marginal,
        1 => DegreeDescription.Minor,
        2 => DegreeDescription.Major,
        _ => DegreeDescription.Complete // 3 or higher
    };

    private static string FormatTargetNumber(TargetNumber tn) =>
        tn.Masteries == 0 ? tn.Base.ToString() :
        tn.Masteries == 1 ? $"{tn.Base}M" :
        $"{tn.Base}M{tn.Masteries}";
}
```

### Access Modifiers and Encapsulation

Only the interpreter interface and outcome types are public. The lookup table is an implementation detail.

**Public API** (visible to other modules):
```csharp
public interface IOutcomeInterpreter { ... }
public sealed record ContestOutcome { ... }
public enum DegreeDescription { ... }
```

**Internal Implementation** (hidden from consumers):
```csharp
internal interface IBenefitConsequenceLookup { ... }
internal class BenefitConsequenceLookup : IBenefitConsequenceLookup { ... }
internal class OutcomeInterpreter : IOutcomeInterpreter { ... }
```

**Dependency Injection Registration** (in module):
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOutcomeModule(this IServiceCollection services)
    {
        services.AddSingleton<IBenefitConsequenceLookup, BenefitConsequenceLookup>();
        services.AddSingleton<IOutcomeInterpreter, OutcomeInterpreter>();
        return services;
    }
}
```

**Testing Strategy**:
- Tests target `IOutcomeInterpreter` (public interface)
- Create `ResolutionResult` and `ContestFrame` inputs, verify `ContestOutcome` output
- No need for InternalsVisibleTo - all inputs and outputs are deterministic

### Usage Example

```csharp
// In the web layer or SignalR hub
public async Task ResolveContest(string sessionId)
{
    var session = _sessions.GetSession(sessionId);
    var frame = session.CurrentContestFrame;

    // Resolve the contest
    var result = _resolver.Resolve(frame);

    // Interpret into displayable outcome
    var outcome = _interpreter.Interpret(result, frame);

    // Broadcast to all participants
    await Clients.Group(sessionId).SendAsync("ContestResolved", outcome);
}
```

### Display Data

The `ContestOutcome` provides all data needed for the UI:

| Field | Example Value | UI Usage |
|-------|---------------|----------|
| Prize | "Sneak past the guards" | Show what was at stake |
| PlayerAbilityName | "Stealth" | Show ability used |
| PlayerRating | "15M" | Show player's rating |
| ResistanceTargetNumber | "10" | Show difficulty |
| PlayerRoll | 8 | Show die result |
| PlayerSuccesses | 3 | Show total successes |
| Winner | Player | Highlight winner |
| DegreeDescription | Minor | Show victory/defeat level |
| BenefitConsequenceModifier | +10 | Show suggested modifier |
| Summary | "Minor Victory for the player!" | Headline text |

## Consequences

### Positive

- **Self-contained**: ContestOutcome has everything needed for display
- **Immutable**: Cannot be accidentally modified after creation
- **Simple**: Mostly data assembly with minimal logic
- **Testable**: Pure functions, easy to verify

### Negative

- **Data duplication**: Some fields copied from ContestFrame and ResolutionResult
- **Tight coupling to display**: ContestOutcome is shaped for UI needs

### Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Benefit/consequence table changes | Table values in single location, easy to update |
| Degree descriptions differ by game | Could make DegreeDescription configurable |
| Missing required fields | Use `required` keyword for compile-time safety |

## Alternatives Considered

### 1. Return ResolutionResult Directly to UI

Let the UI interpret results. Rejected because:

- Duplicates interpretation logic in UI
- Less encapsulation of game rules
- Harder to test

### 2. Combine Resolution and Outcome Modules

Merge into single module. Rejected because:

- Different responsibilities (calculation vs interpretation)
- Resolution is reusable; Outcome is display-focused
- Cleaner separation of concerns

### 3. Rich Domain Events Instead of Value Objects

Use events like `PlayerWonContest` and `PlayerLostContest`. Rejected because:

- Overkill for this simple workflow
- No event sourcing requirement
- Value object is simpler

## References

- Requirements: [specs/0001-questworlds-contest/requirements.md](../../specs/0001-questworlds-contest/requirements.md)
- Related ADRs:
  - [0001-user-interface-architecture.md](0001-user-interface-architecture.md) - Overall architecture
  - [0002-session-management.md](0002-session-management.md) - Session management
  - [0003-framing-module.md](0003-framing-module.md) - ContestFrame
  - [0004-resolution-module.md](0004-resolution-module.md) - ResolutionResult input
- External references:
  - [QuestWorlds SRD - Benefits and Consequences](https://questworlds.chaosium.com/)
