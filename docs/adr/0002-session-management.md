# 0002. Session Management

Date: 2026-01-26

## Status

Accepted

## Context

**Parent Requirement**: [specs/0001-questworlds-contest/requirements.md](../../specs/0001-questworlds-contest/requirements.md)

**Scope**: This ADR focuses on the Session module design, including session creation, participant management, and real-time state synchronization. See [ADR-0001](0001-user-interface-architecture.md) for overall architecture.

The Session module must support:

- GM creates a session and receives a shareable session ID
- Players join using the session ID
- Session state persists for the duration of the game
- Real-time updates propagate to all participants within 1 second
- Session IDs must be sufficiently random to prevent guessing
- Support at least 6 players per session

Key forces:

- Sessions are ephemeral (no persistence beyond the game)
- Need real-time synchronization between GM and players
- Must distinguish between GM and Player roles
- Security: prevent unauthorized session access

## Decision

We will implement the Session module with an in-memory session store coordinated through SignalR for real-time updates.

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                      QuestWorlds.Web                            │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                    ContestHub (SignalR)                   │  │
│  │  - JoinSession(sessionId, name)                          │  │
│  │  - CreateContest(prize, resistanceTn)                    │  │
│  │  - SubmitAbility(abilityName, rating)                    │  │
│  │  - ResolveContest()                                      │  │
│  └─────────────────────────┬────────────────────────────────┘  │
└────────────────────────────┼────────────────────────────────────┘
                             │
                             ▼
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

### Key Components

#### Roles and Responsibilities

**ISessionCoordinator** (Role: Coordinator)

- **Knowing**: Nothing (stateless orchestrator)
- **Doing**: Coordinate session creation, joining, and state transitions
- **Deciding**: Whether a join request is valid

**ISessionIdGenerator** (Role: Identity Provider)

- **Knowing**: ID generation algorithm/format
- **Doing**: Generate unique, unguessable session IDs
- **Deciding**: Nothing

**ISessionRepository** (Role: Session Store)

- **Knowing**: Current sessions and their state
- **Doing**: Store, retrieve, and update sessions
- **Deciding**: Nothing (pure storage)

**Session** (Entity)

- **Knowing**: Session ID, GM connection, participants, current contest state
- **Doing**: Add participants, track state
- **Deciding**: Nothing (data holder)

**Participant** (Value Object)

- **Knowing**: Name, role (GM/Player), connection ID
- **Doing**: Nothing
- **Deciding**: Nothing

### Domain Model

```csharp
namespace QuestWorlds.Session;

public enum ParticipantRole
{
    GM,
    Player
}

public record Participant(
    string Name,
    ParticipantRole Role,
    string ConnectionId
);

public enum SessionState
{
    WaitingForPlayers,
    FramingContest,
    AwaitingPlayerAbility,
    ResolvingContest,
    ShowingOutcome
}

public class Session
{
    public string Id { get; }
    public Participant GM { get; }
    public IReadOnlyList<Participant> Players => _players.AsReadOnly();
    public SessionState State { get; private set; }

    private readonly List<Participant> _players = new();

    public Session(string id, Participant gm)
    {
        Id = id;
        GM = gm;
        State = SessionState.WaitingForPlayers;
    }

    public void AddPlayer(Participant player)
    {
        if (player.Role != ParticipantRole.Player)
            throw new InvalidOperationException("Only players can join as participants");
        _players.Add(player);
    }

    public void TransitionTo(SessionState newState) => State = newState;
}
```

### Interfaces

```csharp
namespace QuestWorlds.Session;

// Internal - implementation detail for ID generation
internal interface ISessionIdGenerator
{
    string Generate();
}

// Internal - implementation detail for storage
internal interface ISessionRepository
{
    void Add(Session session);
    Session? Get(string sessionId);
    void Update(Session session);
    void Remove(string sessionId);
}

// Public - the main entry point for consumers
public interface ISessionCoordinator
{
    Session CreateSession(string gmName, string connectionId);
    Session? GetSession(string sessionId);
    void JoinSession(string sessionId, string playerName, string connectionId);
    IEnumerable<string> GetParticipantConnectionIds(string sessionId);
}
```

### Session ID Generation

Session IDs will be 6-character alphanumeric codes (uppercase letters and digits, excluding ambiguous characters like 0/O, 1/I/L):

```csharp
// Internal - implementation detail
internal class SessionIdGenerator : ISessionIdGenerator
{
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int IdLength = 6;

    public string Generate()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[IdLength];
        rng.GetBytes(bytes);

        return new string(bytes.Select(b => Alphabet[b % Alphabet.Length]).ToArray());
    }
}
```

This provides:

- 30^6 = 729 million possible IDs
- Easy to read aloud and type
- Cryptographically random

### SignalR Integration

The web layer's `ContestHub` will use `ISessionCoordinator` to manage sessions:

```csharp
// In QuestWorlds.Web
public class ContestHub : Hub
{
    private readonly ISessionCoordinator _sessions;

    public async Task<string> CreateSession(string gmName)
    {
        var session = _sessions.CreateSession(gmName, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, session.Id);
        return session.Id;
    }

    public async Task JoinSession(string sessionId, string playerName)
    {
        _sessions.JoinSession(sessionId, playerName, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        await Clients.Group(sessionId).SendAsync("PlayerJoined", playerName);
    }
}
```

### Access Modifiers and Encapsulation

Only types needed by consumers outside the module are public. Internal types can be refactored freely.

**Public API** (visible to other modules):

```csharp
public interface ISessionCoordinator { ... }
public class Session { ... }
public record Participant { ... }
public enum ParticipantRole { ... }
public enum SessionState { ... }
```

**Internal Implementation** (hidden from consumers):

```csharp
internal interface ISessionIdGenerator { ... }
internal interface ISessionRepository { ... }
internal class SessionIdGenerator : ISessionIdGenerator { ... }
internal class SessionCoordinator : ISessionCoordinator { ... }
internal class InMemorySessionRepository : ISessionRepository { ... }
```

**Dependency Injection Registration** (in module):

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSessionModule(this IServiceCollection services)
    {
        services.AddSingleton<ISessionIdGenerator, SessionIdGenerator>();
        services.AddSingleton<ISessionRepository, InMemorySessionRepository>();
        services.AddSingleton<ISessionCoordinator, SessionCoordinator>();
        return services;
    }
}
```

**Testing Strategy**:

- Tests target `ISessionCoordinator` (public interface) only
- Internal collaborators (`ISessionIdGenerator`, `ISessionRepository`) are implementation details
- Use `[assembly: InternalsVisibleTo("QuestWorlds.Session.Tests")]` only if absolutely necessary for edge case testing

### Implementation Approach

1. **In-Memory Storage**: Use `ConcurrentDictionary<string, Session>` for thread-safe session storage
2. **SignalR Groups**: Each session maps to a SignalR group for targeted broadcasts
3. **Connection Tracking**: Store SignalR connection IDs with participants for disconnection handling
4. **State Machine**: Session state transitions controlled by the coordinator

## Consequences

### Positive

- **Simple**: In-memory storage is fast and requires no external dependencies
- **Real-time**: SignalR provides immediate state propagation
- **Testable**: Interfaces allow easy mocking for unit tests
- **Scalable for single server**: ConcurrentDictionary handles concurrent access well

### Negative

- **No persistence**: Sessions lost on server restart (acceptable per requirements)
- **Single server only**: In-memory storage doesn't scale horizontally (out of scope for MVP)
- **Memory growth**: Long-running server could accumulate abandoned sessions

### Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Abandoned sessions consume memory | Implement session timeout (e.g., 4 hours of inactivity) |
| Session ID collision | 729M possibilities + check on creation |
| Disconnection handling | Track connection IDs; allow reconnection with same name |
| Concurrent state modifications | Use locking in SessionRepository if needed |

## Alternatives Considered

### 1. Database-Backed Sessions

Store sessions in SQLite or similar. Rejected because:

- Adds complexity for ephemeral data
- No requirement for persistence beyond game duration
- Overkill for MVP

### 2. Redis for Session Storage

Distributed cache for horizontal scaling. Rejected because:

- Out of scope (single server is fine for MVP)
- Adds infrastructure dependency
- Could be added later if needed

### 3. Longer/Shorter Session IDs

- Longer (e.g., GUIDs): Harder to share verbally
- Shorter (e.g., 4 chars): Higher collision risk
- 6 characters balances usability and uniqueness

## References

- Requirements: [specs/0001-questworlds-contest/requirements.md](../../specs/0001-questworlds-contest/requirements.md)
- Related ADRs:
  - [0001-user-interface-architecture.md](0001-user-interface-architecture.md) - Overall architecture
  - (Planned) 0003-framing-module.md - Contest framing
  - (Planned) 0004-resolution-module.md - Dice resolution
  - (Planned) 0005-outcome-module.md - Outcome determination
- External references:
  - [SignalR documentation](https://docs.microsoft.com/aspnet/core/signalr/)
  - [ConcurrentDictionary best practices](https://docs.microsoft.com/dotnet/api/system.collections.concurrent.concurrentdictionary-2)
