# QuestWorlds.DiceRoller

Random dice generation module for QuestWorlds contest resolution.

## Overview

This module is responsible for generating random D20 dice rolls. It isolates randomness to the edge of the system, allowing the core domain logic (Resolution module) to remain purely deterministic and easily testable.

## Architecture

```
┌───────────────────────────┐
│  QuestWorlds.DiceRoller   │
│                           │
│  IDiceRoller              │
│  - Roll() → DiceRolls     │
│                           │
│  (Randomness here)        │
└───────────────────────────┘
```

## Key Types

| Type | Description |
|------|-------------|
| `IDiceRoller` | Public interface for generating dice rolls |
| `DiceRollerModule` | Factory for creating dice roller instances without DI |
| `ServiceCollectionExtensions` | Extension methods for dependency injection registration |

## Responsibilities

**Role**: Random Number Generator

- **Knowing**: Valid D20 range (1-20)
- **Doing**: Generate random dice rolls for both player and resistance
- **Deciding**: Nothing (pure randomness)

## Usage

### With Dependency Injection

```csharp
services.AddDiceRollerModule();

// Then inject IDiceRoller where needed
public class MyService(IDiceRoller diceRoller)
{
    public void DoSomething()
    {
        var rolls = diceRoller.Roll();
        // rolls.PlayerRoll and rolls.ResistanceRoll contain 1-20 values
    }
}
```

### Without Dependency Injection

```csharp
var diceRoller = DiceRollerModule.CreateRoller();
var rolls = diceRoller.Roll();
```

## Design Decisions

- Uses `Random.Shared` which is thread-safe in .NET 6+
- Returns a `DiceRolls` value object (defined in Resolution module) containing both roll results
- The module is intentionally minimal - it has a single responsibility

## Related ADRs

- [ADR-0006: DiceRoller Module](../../docs/adr/0006-diceroller-module.md)
- [ADR-0004: Resolution Module](../../docs/adr/0004-resolution-module.md) - Consumes the DiceRolls output
