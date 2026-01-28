# QuestWorlds.Session

Session management module for coordinating GM and player participation.

## Overview

This module manages contest sessions, including session creation, player joining, and state tracking. Sessions are ephemeral (in-memory) and persist for the duration of the game.

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
│  │                   ISessionRepository                     │   │
│  │                 (Role: Session Store)                    │   │
│  │                                                          │   │
│  │  - Sessions stored in ConcurrentDictionary               │   │
│  │  - Thread-safe access for concurrent requests            │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

## Key Types

### Public Types

| Type | Description |
|------|-------------|
| `ISessionCoordinator` | Main entry point for session operations |
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
| `ISessionRepository` | Stores and retrieves sessions |
| `InMemorySessionRepository` | In-memory storage using ConcurrentDictionary |
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

// Then inject ISessionCoordinator where needed
public class MyService(ISessionCoordinator sessions)
{
    public void CreateGame(string gmName, string connectionId)
    {
        var session = sessions.CreateSession(gmName, connectionId);
        // Share session.Id with players
    }

    public void PlayerJoins(string sessionId, string playerName, string connectionId)
    {
        sessions.JoinSession(sessionId, playerName, connectionId);
    }
}
```

### Without Dependency Injection

```csharp
var coordinator = SessionModule.CreateCoordinator();
var session = coordinator.CreateSession("GameMaster", "connection-123");
```

## Design Decisions

- **In-Memory Storage**: Simple and fast; no external dependencies
- **Thread-Safe**: Uses `ConcurrentDictionary` for concurrent access
- **No Persistence**: Sessions are ephemeral (acceptable per requirements)
- **Internal Implementation**: Only `ISessionCoordinator` is public; implementation details are internal
- **Connection Tracking**: Stores SignalR connection IDs for real-time updates

## Limitations

- Single server only (in-memory storage doesn't scale horizontally)
- Sessions lost on server restart
- No session timeout (abandoned sessions accumulate)

## Related ADRs

- [ADR-0002: Session Management](../../docs/adr/0002-session-management.md)
- [ADR-0001: User Interface Architecture](../../docs/adr/0001-user-interface-architecture.md)
