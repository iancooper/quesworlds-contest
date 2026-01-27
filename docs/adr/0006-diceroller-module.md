# 0006. DiceRoller Module

Date: 2026-01-26

## Status

Accepted

## Context

**Parent Requirement**: [specs/0001-questworlds-contest/requirements.md](../../specs/0001-questworlds-contest/requirements.md)

**Scope**: This ADR focuses on the DiceRoller module design, which handles random dice generation. See [ADR-0001](0001-user-interface-architecture.md) for overall architecture and [ADR-0004](0004-resolution-module.md) for the Resolution module that consumes the dice rolls.

The DiceRoller module must:

- Generate random D20 rolls for both player and resistance
- Provide a public interface that the web layer can use
- Produce a `DiceRolls` value that can be passed to the Resolution module

Key forces:

- Randomness should be pushed to the edges of the system
- Domain logic (Resolution module) should be purely deterministic
- The web layer orchestrates the interaction between DiceRoller and Resolution
- Testing the Resolution module should not require mocking dice

## Decision

We will implement the DiceRoller module as a simple assembly with a single public interface `IDiceRoller` that generates random dice rolls. The web layer will use this to generate rolls before passing them to the Resolution module.

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     QuestWorlds.Web                             │
│                                                                 │
│  1. Call IDiceRoller.Roll() → DiceRolls                         │
│  2. Call IContestResolver.Resolve(frame, rolls) → Result        │
│                                                                 │
└───────────────┬───────────────────────────┬─────────────────────┘
                │                           │
                ▼                           ▼
┌───────────────────────────┐   ┌───────────────────────────────┐
│  QuestWorlds.DiceRoller   │   │   QuestWorlds.Resolution      │
│                           │   │                               │
│  IDiceRoller              │   │   IContestResolver            │
│  - Roll() → DiceRolls     │   │   - Resolve(frame, rolls)     │
│                           │   │                               │
│  (Randomness here)        │   │   (Pure deterministic logic)  │
└───────────────────────────┘   └───────────────────────────────┘
```

### Key Components

#### Roles and Responsibilities

**IDiceRoller** (Role: Random Number Generator)

- **Knowing**: Valid D20 range (1-20)
- **Doing**: Generate random dice rolls
- **Deciding**: Nothing (pure randomness)

**DiceRolls** (Value Object - defined in Resolution module)

- **Knowing**: Player roll and resistance roll values
- **Doing**: Nothing
- **Deciding**: Nothing

### Domain Model

The `DiceRolls` type is defined in the Resolution module since it's consumed there:

```csharp
// In QuestWorlds.Resolution
namespace QuestWorlds.Resolution;

public readonly record struct DiceRolls(int PlayerRoll, int ResistanceRoll);
```

### Interfaces

```csharp
namespace QuestWorlds.DiceRoller;

/// <summary>
/// Generates random D20 dice rolls for contest resolution.
/// </summary>
public interface IDiceRoller
{
    /// <summary>
    /// Rolls two D20 dice - one for the player and one for the resistance.
    /// </summary>
    /// <returns>A DiceRolls value containing both roll results (1-20 each)</returns>
    DiceRolls Roll();
}
```

### Implementations

```csharp
namespace QuestWorlds.DiceRoller;

internal class DiceRoller : IDiceRoller
{
    public DiceRolls Roll()
    {
        return new DiceRolls(
            PlayerRoll: Random.Shared.Next(1, 21),
            ResistanceRoll: Random.Shared.Next(1, 21));
    }
}
```

### Access Modifiers and Encapsulation

Only the interface is public. The implementation is internal.

**Public API** (visible to other modules):

```csharp
public interface IDiceRoller { ... }
```

**Internal Implementation** (hidden from consumers):

```csharp
internal class DiceRoller : IDiceRoller { ... }
```

**Dependency Injection Registration** (in module):

```csharp
namespace QuestWorlds.DiceRoller;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiceRollerModule(this IServiceCollection services)
    {
        services.AddSingleton<IDiceRoller, DiceRoller>();
        return services;
    }
}
```

### Testing Strategy

The DiceRoller module itself requires minimal testing:

```csharp
[Fact]
public void Roll_ReturnsValuesInValidRange()
{
    var roller = new DiceRoller();

    for (int i = 0; i < 100; i++)
    {
        var rolls = roller.Roll();
        Assert.InRange(rolls.PlayerRoll, 1, 20);
        Assert.InRange(rolls.ResistanceRoll, 1, 20);
    }
}
```

The key benefit is that tests for the Resolution module don't need this module at all - they simply pass known `DiceRolls` values directly.

### Usage in Web Layer

```csharp
// In ContestHub.cs or similar
public async Task ResolveContest(string sessionId)
{
    var frame = GetContestFrame(sessionId);

    // Generate random rolls
    var rolls = _diceRoller.Roll();

    // Calculate result (deterministic)
    var result = _resolver.Resolve(frame, rolls);

    // Interpret outcome
    var outcome = _interpreter.Interpret(result);

    // Broadcast to participants
    await Clients.Group(sessionId).SendAsync("ContestResolved", outcome);
}
```

## Consequences

### Positive

- **Clear Separation**: Randomness is isolated in its own module
- **Testable Domain Logic**: Resolution module is purely deterministic
- **Simple Interface**: Single method, easy to understand
- **Thread Safe**: Uses `Random.Shared` which is thread-safe in .NET 6+

### Negative

- **Additional Assembly**: One more project to maintain (minimal overhead)
- **Coordination Required**: Web layer must orchestrate the two modules

### Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Random.Shared thread safety | Random.Shared is thread-safe in .NET 6+ |
| Distribution quality | Random.Shared uses cryptographically suitable algorithm |
| Caller forgets to roll | DiceRolls is required parameter in Resolution - compile-time enforcement |

## Alternatives Considered

### 1. Keep Dice Rolling in Resolution Module

Have Resolution module generate its own random numbers. Rejected because:

- Mixes concerns - calculation and randomness
- Makes Resolution module impure
- Requires mocking for tests

### 2. Pass Random Instance to Resolution

Inject `Random` into `IContestResolver`. Rejected because:

- Still couples Resolution to randomness
- More complex interface
- Harder to test

### 3. No Separate Module - Just a Utility Class

Put `IDiceRoller` in a shared utilities project. Rejected because:

- Module boundaries are clearer
- Consistent with the rest of the architecture
- Minimal additional complexity

## References

- Requirements: [specs/0001-questworlds-contest/requirements.md](../../specs/0001-questworlds-contest/requirements.md)
- Related ADRs:
  - [0001-user-interface-architecture.md](0001-user-interface-architecture.md) - Overall architecture
  - [0004-resolution-module.md](0004-resolution-module.md) - Consumes DiceRolls
