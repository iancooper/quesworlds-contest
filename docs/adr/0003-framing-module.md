# 0003. Framing Module

Date: 2026-01-26

## Status

Proposed

## Context

**Parent Requirement**: [specs/0001-questworlds-contest/requirements.md](../../specs/0001-questworlds-contest/requirements.md)

**Scope**: This ADR focuses on the Framing module design, which handles contest setup including the prize, resistance, player abilities, and modifiers. See [ADR-0001](0001-user-interface-architecture.md) for overall architecture.

The Framing module must support:

- GM defines the prize (what's at stake in the contest)
- GM sets the resistance Target Number (using standard difficulties or custom values)
- Player submits their chosen ability name and rating (including masteries)
- GM applies modifiers: stretch, situational, augment, hindrance, benefit/consequence
- Validate that all required information is present before resolution

Key forces:

- QuestWorlds uses a specific notation for abilities with masteries (e.g., "5M", "6M2")
- Modifiers have specific allowed values (±5 or ±10)
- The framing phase must be complete before dice can be rolled
- Both GM and player contribute information during framing

## Decision

We will implement the Framing module with value objects representing the domain concepts (Rating, TargetNumber, Modifier) and a ContestFrame aggregate that tracks the framing state.

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    QuestWorlds.Framing                          │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              IContestFramer (Role: Setup Manager)        │   │
│  │  - FrameContest(prize, resistanceTn)                     │   │
│  │  - SetPlayerAbility(frame, name, rating)                 │   │
│  │  - ApplyModifier(frame, modifier)                        │   │
│  │  - IsReadyForResolution(frame)                           │   │
│  └─────────────────────────────┬───────────────────────────┘   │
│                                │                                │
│                                ▼                                │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              ContestFrame (Aggregate Root)               │   │
│  │                                                          │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐  │   │
│  │  │    Prize    │  │  Resistance │  │  PlayerAbility  │  │   │
│  │  │   (string)  │  │(TargetNumber)│  │ (name + Rating) │  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────────┘  │   │
│  │                                                          │   │
│  │  ┌─────────────────────────────────────────────────────┐│   │
│  │  │              Modifiers (List<Modifier>)             ││   │
│  │  │  Stretch | Situational | Augment | Hindrance | B/C  ││   │
│  │  └─────────────────────────────────────────────────────┘│   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Value Objects:                                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐  │
│  │    Rating    │  │ TargetNumber │  │      Modifier        │  │
│  │  Base + M    │  │  Base + M    │  │  Type + Value (±5/10)│  │
│  └──────────────┘  └──────────────┘  └──────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### Key Components

#### Roles and Responsibilities

**IContestFramer** (Role: Setup Manager)

- **Knowing**: Nothing (stateless service)
- **Doing**: Create contest frames, update with abilities/modifiers
- **Deciding**: Whether framing is complete and ready for resolution

**ContestFrame** (Aggregate Root)

- **Knowing**: Prize, resistance, player ability, applied modifiers
- **Doing**: Accept updates to its state, calculate effective target numbers
- **Deciding**: Nothing (data holder with computed properties)

**Rating** (Value Object)

- **Knowing**: Base value (1-20), mastery count
- **Doing**: Parse from string notation, calculate effective TN
- **Deciding**: Nothing

**TargetNumber** (Value Object)

- **Knowing**: Base value, mastery count, applied modifier total
- **Doing**: Calculate effective value for dice comparison
- **Deciding**: Nothing

**Modifier** (Value Object)

- **Knowing**: Type (stretch/situational/augment/hindrance/benefit-consequence), value
- **Doing**: Nothing
- **Deciding**: Nothing

### Domain Model

```csharp
namespace QuestWorlds.Framing;

/// <summary>
/// Represents a QuestWorlds ability rating with optional masteries.
/// Examples: "15" = base 15, "5M" = 5 with 1 mastery, "6M2" = 6 with 2 masteries
/// </summary>
public readonly record struct Rating
{
    public int Base { get; }
    public int Masteries { get; }

    public Rating(int baseValue, int masteries = 0)
    {
        if (baseValue < 1 || baseValue > 20)
            throw new ArgumentOutOfRangeException(nameof(baseValue), "Base must be 1-20");
        if (masteries < 0)
            throw new ArgumentOutOfRangeException(nameof(masteries), "Masteries cannot be negative");

        Base = baseValue;
        Masteries = masteries;
    }

    /// <summary>
    /// Parse rating from string notation: "15", "5M", "6M2"
    /// </summary>
    public static Rating Parse(string notation)
    {
        if (string.IsNullOrWhiteSpace(notation))
            throw new ArgumentException("Rating notation cannot be empty");

        // Pattern: digits, optional M, optional digits
        var match = Regex.Match(notation.Trim().ToUpperInvariant(), @"^(\d+)(M(\d+)?)?$");
        if (!match.Success)
            throw new FormatException($"Invalid rating notation: {notation}");

        var baseValue = int.Parse(match.Groups[1].Value);
        var masteries = 0;

        if (match.Groups[2].Success) // Has 'M'
        {
            masteries = match.Groups[3].Success
                ? int.Parse(match.Groups[3].Value)
                : 1;
        }

        return new Rating(baseValue, masteries);
    }

    public override string ToString() =>
        Masteries == 0 ? Base.ToString() :
        Masteries == 1 ? $"{Base}M" :
        $"{Base}M{Masteries}";
}

/// <summary>
/// Represents a target number for dice resolution.
/// </summary>
public readonly record struct TargetNumber
{
    public int Base { get; }
    public int Masteries { get; }
    public int Modifier { get; }

    public TargetNumber(int baseValue, int masteries = 0, int modifier = 0)
    {
        Base = Math.Clamp(baseValue + modifier, 1, 20);
        Masteries = masteries;
        Modifier = modifier;
    }

    public static TargetNumber FromRating(Rating rating, int modifier = 0) =>
        new(rating.Base, rating.Masteries, modifier);

    /// <summary>
    /// The effective base for dice comparison (after applying modifier, clamped to 1-20)
    /// </summary>
    public int EffectiveBase => Math.Clamp(Base, 1, 20);
}

public enum ModifierType
{
    Stretch,
    Situational,
    Augment,
    Hindrance,
    BenefitConsequence
}

public readonly record struct Modifier
{
    public ModifierType Type { get; }
    public int Value { get; }

    public Modifier(ModifierType type, int value)
    {
        if (value != -10 && value != -5 && value != 5 && value != 10)
            throw new ArgumentOutOfRangeException(nameof(value), "Modifier must be ±5 or ±10");

        // Validate sign based on type
        Type = type;
        Value = type switch
        {
            ModifierType.Stretch => value < 0 ? value : throw new ArgumentException("Stretch must be negative"),
            ModifierType.Hindrance => value < 0 ? value : throw new ArgumentException("Hindrance must be negative"),
            ModifierType.Augment => value > 0 ? value : throw new ArgumentException("Augment must be positive"),
            _ => value // Situational and BenefitConsequence can be either
        };
    }
}

public enum ResistanceDifficulty
{
    Simple = 0,
    Easy = 0,
    Routine = 0,
    Straightforward = 5,
    Base = 10,
    Challenging = 15,
    Hard = 20,
    // Punishing and Exceptional have masteries, handled separately
}
```

### ContestFrame Aggregate

```csharp
namespace QuestWorlds.Framing;

public class ContestFrame
{
    public string Prize { get; }
    public TargetNumber Resistance { get; }
    public string? PlayerAbilityName { get; private set; }
    public Rating? PlayerRating { get; private set; }
    public IReadOnlyList<Modifier> Modifiers => _modifiers.AsReadOnly();

    private readonly List<Modifier> _modifiers = new();

    public ContestFrame(string prize, TargetNumber resistance)
    {
        if (string.IsNullOrWhiteSpace(prize))
            throw new ArgumentException("Prize cannot be empty", nameof(prize));

        Prize = prize;
        Resistance = resistance;
    }

    public void SetPlayerAbility(string abilityName, Rating rating)
    {
        if (string.IsNullOrWhiteSpace(abilityName))
            throw new ArgumentException("Ability name cannot be empty", nameof(abilityName));

        PlayerAbilityName = abilityName;
        PlayerRating = rating;
    }

    public void ApplyModifier(Modifier modifier)
    {
        _modifiers.Add(modifier);
    }

    public void ClearModifiers() => _modifiers.Clear();

    /// <summary>
    /// Calculate the player's effective target number after all modifiers.
    /// </summary>
    public TargetNumber? GetPlayerTargetNumber()
    {
        if (PlayerRating is null) return null;

        var totalModifier = _modifiers.Sum(m => m.Value);
        return TargetNumber.FromRating(PlayerRating.Value, totalModifier);
    }

    /// <summary>
    /// Check if all required information is present for resolution.
    /// </summary>
    public bool IsReadyForResolution =>
        !string.IsNullOrEmpty(Prize) &&
        PlayerAbilityName is not null &&
        PlayerRating is not null;
}
```

### Interface

```csharp
namespace QuestWorlds.Framing;

// Public - the main entry point for consumers
public interface IContestFramer
{
    ContestFrame FrameContest(string prize, TargetNumber resistance);
    void SetPlayerAbility(ContestFrame frame, string abilityName, Rating rating);
    void ApplyModifier(ContestFrame frame, Modifier modifier);
    bool IsReadyForResolution(ContestFrame frame);
}

// Internal - implementation detail
internal class ContestFramer : IContestFramer
{
    public ContestFrame FrameContest(string prize, TargetNumber resistance) =>
        new ContestFrame(prize, resistance);

    public void SetPlayerAbility(ContestFrame frame, string abilityName, Rating rating) =>
        frame.SetPlayerAbility(abilityName, rating);

    public void ApplyModifier(ContestFrame frame, Modifier modifier) =>
        frame.ApplyModifier(modifier);

    public bool IsReadyForResolution(ContestFrame frame) =>
        frame.IsReadyForResolution;
}
```

### Access Modifiers and Encapsulation

Value objects and the aggregate are public because they're used by other modules (Resolution, Outcome). The service implementation is internal.

**Public API** (visible to other modules):
```csharp
public interface IContestFramer { ... }
public class ContestFrame { ... }
public readonly record struct Rating { ... }
public readonly record struct TargetNumber { ... }
public readonly record struct Modifier { ... }
public enum ModifierType { ... }
public enum ResistanceDifficulty { ... }
```

**Internal Implementation** (hidden from consumers):
```csharp
internal class ContestFramer : IContestFramer { ... }
```

**Dependency Injection Registration** (in module):
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFramingModule(this IServiceCollection services)
    {
        services.AddTransient<IContestFramer, ContestFramer>();
        return services;
    }
}
```

**Testing Strategy**:
- Tests target `IContestFramer` (public interface)
- Value objects (`Rating`, `TargetNumber`, `Modifier`) are tested directly since they're public and contain validation logic
- `ContestFrame` can be tested directly for its public behavior

### Implementation Approach

1. **Value Objects**: `Rating`, `TargetNumber`, and `Modifier` are immutable value objects with validation
2. **Aggregate Root**: `ContestFrame` encapsulates the framing state and enforces invariants
3. **Parsing**: `Rating.Parse()` handles the QuestWorlds mastery notation
4. **Modifier Validation**: Enforce ±5/±10 values and correct sign per type
5. **Readiness Check**: Simple boolean check that all required fields are present

## Consequences

### Positive

- **Domain-driven**: Value objects capture QuestWorlds concepts precisely
- **Self-validating**: Invalid states are rejected at construction time
- **Testable**: Pure domain logic with no dependencies
- **Clear contract**: `IsReadyForResolution` makes the completion requirement explicit

### Negative

- **More types**: Several small value objects to maintain
- **Parsing complexity**: Need to handle mastery notation correctly
- **Modifier validation**: Type-specific sign rules add complexity

### Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Rating parsing errors | Comprehensive unit tests for all notation formats |
| Modifier sign confusion | Validate at construction; clear error messages |
| Forgotten readiness checks | Make `IsReadyForResolution` required before resolution |

## Alternatives Considered

### 1. Single TargetNumber Type for Everything

Use one type for both ratings and target numbers. Rejected because:

- Ratings and target numbers have different semantics
- Modifiers only apply to target numbers, not ratings
- Clearer domain model with separate types

### 2. Stringly-Typed Modifiers

Use strings like "stretch:-5" instead of structured types. Rejected because:

- No compile-time safety
- Easier to make mistakes
- Harder to validate

### 3. Mutable Rating/TargetNumber

Allow modification after creation. Rejected because:

- Value objects should be immutable
- Prevents accidental state corruption
- Easier to reason about

## References

- Requirements: [specs/0001-questworlds-contest/requirements.md](../../specs/0001-questworlds-contest/requirements.md)
- Related ADRs:
  - [0001-user-interface-architecture.md](0001-user-interface-architecture.md) - Overall architecture
  - [0002-session-management.md](0002-session-management.md) - Session management
  - (Planned) 0004-resolution-module.md - Dice resolution (consumes ContestFrame)
  - (Planned) 0005-outcome-module.md - Outcome determination
- External references:
  - [QuestWorlds SRD - Abilities and Ratings](https://questworlds.chaosium.com/)
