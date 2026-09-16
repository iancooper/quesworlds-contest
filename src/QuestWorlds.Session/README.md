# QuestWorlds.Session

Session management module for coordinating GM and player participation.

## Overview

This module manages contest sessions, including session creation, player joining, and state tracking. It declares where sessions are kept — `IAmASessionStore` — and implements none of it; a host chooses a store module and registers it.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    QuestWorlds.Session                          │
│  ┌─────────────────────┐  ┌─────────────────────────────────┐  │
│  │  ISessionCoordinator │  │      ISessionIdGenerator       │  │
│  │  (Role: Coordinator) │  │    (Role: Identity Provider)   │  │
│  └──────────┬──────────┘  └──────────────┬──────────────────┘  │
│             │                            │                      │
│             ▼                            ▼                      │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                    IAmASessionStore                      │   │
│  │               (Role: Information Holder)                 │   │
│  │                                                          │   │
│  │  - Declared here, implemented in a store module          │   │
│  │  - SaveAsync upserts; GetAsync may return a copy;        │   │
│  │    RemoveAsync is idempotent                             │   │
│  └─────────────────────────────────────────────────────────┘   │
└──────────────────────────────┼──────────────────────────────────┘
                               │ implemented by
                               ▼
              QuestWorlds.InMemorySessionStore (and, later, Sqlite)
```

The arrow points inward: this module references nothing, and the stores reference it.

## Key Types

### Public Types

| Type | Description |
|------|-------------|
| `ISessionCoordinator` | Main entry point for session operations |
| `IAmASessionStore` | Port a store module implements to hold sessions |
| `Session` | Represents a contest session with GM and players |
| `Participant` | Represents a participant (GM or Player) |
| `ParticipantRole` | Enum for participant roles (GM, Player) |
| `SessionState` | Enum for session workflow states |
| `SessionModule` | Factory for creating coordinator without DI |
| `ServiceCollectionExtensions` | Extension methods for dependency injection |

### Internal Types (Implementation Details)

| Type | Description |
|------|-------------|
| `ISessionIdGenerator` | Generates unique session IDs |
| `SessionIdGenerator` | Implementation using cryptographic random |
| `SessionCoordinator` | Implementation of ISessionCoordinator |

## Session States

| State | Description |
|-------|-------------|
| `WaitingForPlayers` | Initial state after session creation |
| `FramingContest` | GM is setting up the contest |
| `AwaitingPlayerAbility` | Waiting for player to submit ability |
| `ResolvingContest` | Dice are being rolled and results calculated |
| `ShowingOutcome` | Results are being displayed |

## Session ID Format

Session IDs are 6-character alphanumeric codes:
- Uses alphabet: `ABCDEFGHJKMNPQRSTUVWXYZ23456789` (excludes ambiguous characters like 0/O, 1/I/L)
- Provides 30^6 = 729 million possible IDs
- Easy to read aloud and type
- Cryptographically random

## Usage

### With Dependency Injection

```csharp
services.AddSessionModule();
services.AddInMemorySessionStore();  // or any other store module; exactly one

// Then inject ISessionCoordinator where needed
public class MyService(ISessionCoordinator sessions)
{
    public async Task CreateGame(string gmName, string connectionId)
    {
        var session = await sessions.CreateSessionAsync(gmName, connectionId);
        // Share session.Id with players
    }

    public async Task PlayerJoins(string sessionId, string playerName, string connectionId)
    {
        await sessions.JoinSessionAsync(sessionId, playerName, connectionId);
    }
}
```

### Without Dependency Injection

```csharp
var coordinator = SessionModule.CreateCoordinator(new InMemorySessionStore());
var session = await coordinator.CreateSessionAsync("GameMaster", "connection-123");
```

## Design Decisions

- **Storage is a port**: `IAmASessionStore` is public and this module implements none of it, so a host can choose its store (ADR-0007, ADR-0008)
- **The coordinator saves what it changes**: a store may hand back a copy, so a change that is not saved is a change that did not happen (ADR-0007 D5, D7)
- **Internal Implementation**: id generation and coordination are internal; only the ports and the types they carry are public
- **Connection Tracking**: Stores SignalR connection IDs for real-time updates

## Limitations

- Durability, scale-out and start-up behaviour are the chosen store's properties, not this module's
- No session timeout (abandoned sessions accumulate)

## Related ADRs

- [ADR-0007: Session Storage Port](../../docs/adr/0007-session-storage-port.md)
- [ADR-0008: Session Store Module Composition](../../docs/adr/0008-session-store-module-composition.md)
- [ADR-0002: Session Management](../../docs/adr/0002-session-management.md)
- [ADR-0001: User Interface Architecture](../../docs/adr/0001-user-interface-architecture.md)
