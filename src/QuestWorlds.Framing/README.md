# QuestWorlds.Framing

Contest framing module for setting up QuestWorlds simple contests.

## Overview

This module handles contest setup, including the prize at stake, resistance target number, player abilities, and modifiers. It captures all the information needed before dice are rolled.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    QuestWorlds.Framing                          │
│                                                                 │
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

## Key Types

| Type | Description |
|------|-------------|
| `ContestFrame` | Aggregate root that holds all contest framing data |
| `Rating` | Value object representing an ability rating with optional masteries (e.g., "5M", "6M2") |
| `TargetNumber` | Value object for dice resolution with base, masteries, and modifiers |
| `Modifier` | Value object representing a modifier (±5 or ±10) |
| `ModifierType` | Enum for modifier types (Stretch, Situational, Augment, Hindrance, BenefitConsequence) |

## QuestWorlds Rating Notation

Ratings use QuestWorlds notation where masteries represent +20 to the ability:

| Notation | Meaning |
|----------|---------|
| `15` | Base 15, no masteries |
| `5M` | Base 5 with 1 mastery (equivalent to 25) |
| `6M2` | Base 6 with 2 masteries (equivalent to 46) |

## Modifier Rules

| Modifier Type | Allowed Values | Description |
|---------------|----------------|-------------|
| Stretch | -5, -10 | Penalty when ability doesn't quite fit |
| Situational | ±5, ±10 | Bonus/penalty based on circumstances |
| Augment | +5, +10 | Bonus from supporting abilities or help |
| Hindrance | -5, -10 | Penalty from obstacles or opposition |
| BenefitConsequence | ±5, ±10 | Modifier from previous contest outcome |

## Usage

```csharp
// Create a contest frame
var resistance = Rating.Parse("10");
var frame = new ContestFrame("Sneak past the guards", TargetNumber.FromRating(resistance));

// Player submits their ability
var playerRating = Rating.Parse("15M");
frame.SetPlayerAbility("Stealth", playerRating);

// GM applies modifiers
frame.ApplyModifier(new Modifier(ModifierType.Situational, 5)); // +5 for darkness

// Check if ready for resolution
if (frame.IsReadyForResolution)
{
    var playerTn = frame.GetPlayerTargetNumber();
    // Ready to roll dice
}
```

## Design Decisions

- Value objects are immutable and self-validating
- `ContestFrame` is mutable but tracks its readiness state
- Rating parsing handles the QuestWorlds mastery notation
- Modifiers validate their sign based on type (Stretch must be negative, Augment must be positive)

## Related ADRs

- [ADR-0003: Framing Module](../../docs/adr/0003-framing-module.md)
