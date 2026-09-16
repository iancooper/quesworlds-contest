# QuestWorlds.SqliteSessionStore

Keeps sessions and their contest frames in a SQLite database, so they survive a restart.

## Overview

This is one of two implementations of `IAmASessionStore` and `IAmAContestFrameStore`, the ports
`QuestWorlds.Session` and `QuestWorlds.Framing` declare. It is a peer of
`QuestWorlds.InMemorySessionStore`, and a host picks one. Neither module that declares a port knows
this one exists.

```
   ┌────────────────────────────────────┐
   │ QuestWorlds.SqliteSessionStore     │   Ce = 2 + Microsoft.Data.Sqlite
   └───────┬──────────────────────┬─────┘
           │ IAmASessionStore     │ IAmAContestFrameStore
           ▼                      ▼
   ┌───────────────────┐  ┌───────────────────┐
   │ QuestWorlds       │  │ QuestWorlds       │
   │ .Session   Ce = 0 │  │ .Framing   Ce = 0 │
   └───────────────────┘  └───────────────────┘
```

`Microsoft.Data.Sqlite` appears in this project and nowhere else — the containment is the point
(ADR-0009 D1).

## Key Types

| Type | Description |
|------|-------------|
| `SqliteSessionStoreInitialiser` | An `IHostedService` that creates the schema and sets WAL mode, once, at startup |
| `ServiceCollectionExtensions` | `AddSqliteSessionStore(connectionString)` |

## Usage

```csharp
services.AddSessionModule();
services.AddSqliteSessionStore(builder.Configuration.GetConnectionString("SessionStore")!);
```

Tests do not run a host, so they create the schema directly:

```csharp
await SqliteSessionStoreInitialiser.InitialiseAsync($"Data Source={path}");
```

## Schema

Four tables, holding one aggregate each way round: a session with its participants, and that
session's contest frame with its modifiers.

| Table | Holds |
|-------|-------|
| `Sessions` | Id and state |
| `Participants` | The GM and the players, one row each, ordered by `Ordinal` |
| `ContestFrames` | One row per session: prize, resistance, and the player's ability if it has arrived |
| `ContestModifiers` | The frame's modifiers, ordered by `Ordinal` |

## Design Decisions

- **`Microsoft.Data.Sqlite` used directly, no ORM.** One aggregate, saved whole and loaded whole,
  with no queries — every EF Core feature would be unused, and its change tracking would duplicate a
  responsibility ADR-0007 gave the coordinator (ADR-0009 D1)
- **Four tables, not a JSON blob**: the mapping is the demonstration, and the database should be
  readable (ADR-0009 D2)
- **The GM is a `Participants` row**, not columns on `Sessions`. `Participant` is one type with a
  `Role`, so it maps to one table; columns would encode the GM/player distinction twice
- **Enums are stored as names**, never ordinals, so reordering a `SessionState` cannot silently
  reinterpret stored rows (ADR-0009 D3)
- **The schema is created by a hosted service** the module registers, so choosing the store is the
  only decision the host makes (ADR-0009 D6)
- **A connection per operation**, with the database in WAL mode. The store holds a connection string
  and nothing else, which is what makes it safe as a singleton (ADR-0009 D5)
- **Where the database lives is the host's decision**: the module takes a connection string and does
  not read configuration or default to a path (ADR-0009 D7)

## Limitations

- **A restored session is durable history, not a resumable game.** Every stored `ConnectionId` points
  at a SignalR connection that no longer exists, so nobody can be messaged through a reloaded session
  until they reconnect and are re-associated. The game state is whole; only the transport is dead
  (ADR-0009 D8)
- **A session and its frame are saved by separate calls**, so each is internally transactional but the
  two are not atomic with each other. A crash between them can leave a frame one step behind its
  session; reframing re-derives it (ADR-0009 D4)
- **SQLite is single-writer.** This makes sessions durable, not shareable across servers
- **The mapping is hand-written and hand-maintained.** Adding a field to `Session` or `ContestFrame`
  means touching the schema, the writer, and the reader

## Naming

The namespace and the type share a name, so an unqualified `SqliteSessionStore` in a file under a
sibling `QuestWorlds` namespace binds to the namespace rather than the type — CS0118. Consumers that
name the type need a `using` alias inside their namespace declaration; consumers that only call
`AddSqliteSessionStore()` do not.

## Related ADRs

- [ADR-0007: Session Storage Port](../../docs/adr/0007-session-storage-port.md)
- [ADR-0008: Session Store Module Composition](../../docs/adr/0008-session-store-module-composition.md)
- [ADR-0009: SQLite Session Store](../../docs/adr/0009-sqlite-session-store.md)
- [ADR-0010: Contest Frame Storage Port](../../docs/adr/0010-contest-frame-storage-port.md)
