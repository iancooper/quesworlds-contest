# QuestWorlds.Outcome

Outcome interpretation module for creating displayable contest results.

## Overview

This module interprets resolution results into narrative outcomes with benefits and consequences. It combines the resolution result with contest context to produce a complete `ContestOutcome` for display to GM and players.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    QuestWorlds.Outcome                          │
│                                                                 │
│  Input: ResolutionResult + ContestFrame                         │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │        IOutcomeInterpreter (Role: Result Interpreter)    │   │
│  │  - Interpret(result, frame) → ContestOutcome             │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Output:                                                        │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              ContestOutcome (Immutable)                  │   │
│  │                                                          │   │
│  │  Context: Prize, PlayerAbilityName, PlayerRating,        │   │
│  │           ResistanceTargetNumber                         │   │
│  │                                                          │   │
│  │  Resolution: Rolls, Successes                            │   │
│  │                                                          │   │
│  │  Outcome: Winner, Degree, BenefitConsequenceModifier,    │   │
│  │           IsPlayerVictory, Summary                       │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

## Key Types

| Type | Description |
|------|-------------|
| `IOutcomeInterpreter` | Interface for interpreting resolution results |
| `OutcomeInterpreter` | Implementation that applies the benefit/consequence table |
| `ContestOutcome` | Immutable record containing all displayable outcome data |
| `ServiceCollectionExtensions` | Extension methods for dependency injection |

## Benefit/Consequence Table

Based on the degree of victory or defeat, the module determines the modifier for future contests:

| Outcome | Modifier |
|---------|----------|
| 3+ degree defeat | -20 |
| 2 degree defeat | -15 |
| 1 degree defeat | -10 |
| 0 degree defeat | -5 |
| Tie | 0 |
| 0 degree victory | +5 |
| 1 degree victory | +10 |
| 2 degree victory | +15 |
| 3+ degree victory | +20 |

## Usage

```csharp
// After resolving a contest
var result = _contestResolver.Resolve(frame, rolls);

// Interpret into a displayable outcome
var outcome = _outcomeInterpreter.Interpret(result, frame);

// Access outcome data
Console.WriteLine(outcome.Summary);           // "2 Degrees of Victory for the player!"
Console.WriteLine(outcome.Prize);             // "Sneak past the guards"
Console.WriteLine(outcome.BenefitConsequenceModifier); // 15
Console.WriteLine(outcome.IsPlayerVictory);   // true
```

## ContestOutcome Fields

| Field | Description | Example |
|-------|-------------|---------|
| `Prize` | What was at stake | "Sneak past the guards" |
| `PlayerAbilityName` | Ability used | "Stealth" |
| `PlayerRating` | Player's rating | "15M" |
| `ResistanceTargetNumber` | Difficulty | "10" |
| `PlayerRoll` | Player's die result | 8 |
| `ResistanceRoll` | Resistance's die result | 12 |
| `PlayerSuccesses` | Player's total successes | 3 |
| `ResistanceSuccesses` | Resistance's total successes | 1 |
| `Winner` | Who won | Player |
| `Degree` | Degree of victory/defeat | 2 |
| `BenefitConsequenceModifier` | Suggested modifier | +15 |
| `IsPlayerVictory` | True if player won | true |
| `Summary` | Human-readable summary | "2 Degrees of Victory..." |

## Design Decisions

- **Self-contained**: `ContestOutcome` has everything needed for display
- **Immutable**: Cannot be accidentally modified after creation
- **Simple**: Primarily data assembly with lookup logic for benefit/consequence
- **Testable**: Pure functions with deterministic output

## Related ADRs

- [ADR-0005: Outcome Module](../../docs/adr/0005-outcome-module.md)
- [ADR-0004: Resolution Module](../../docs/adr/0004-resolution-module.md) - Provides ResolutionResult input
