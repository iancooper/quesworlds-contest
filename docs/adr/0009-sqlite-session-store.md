# 0009. SQLite Session Store

Date: 2026-09-14

## Status

Proposed

## Context

**Parent Requirement**: [specs/0002-persistent_sessions/requirements.md](../../specs/0002-persistent_sessions/requirements.md)

**Scope**: This ADR decides **what goes inside `QuestWorlds.SqliteSessionStore`** — data access library, schema, how a `Session` maps to rows, how the schema comes into being, connection handling, and what a reloaded session actually means. It takes [ADR-0007](0007-session-storage-port.md)'s contract and [ADR-0008](0008-session-store-module-composition.md)'s composition as given.

Everything decided here is behind the port. A different answer to any of it changes nothing in `QuestWorlds.Session`, in `ISessionCoordinator`, or in `ContestHub` — which is the claim the module exists to demonstrate.

### The problem

`QuestWorlds.InMemorySessionStore` cannot show that the port works, because it is the implementation the port was extracted from. It returns live references, so a coordinator that forgets to save still passes. A second store that genuinely round-trips through bytes is what turns the seam from an assertion into a demonstration.

### Forces

- **The aggregate is small and always handled whole.** A session is an id, a GM, a handful of players, and a state. It is never queried by player, never paged, never partially loaded.
- **`Session` has no setters.** Its state is reached through `Id`, `GM`, `Players`, `State` and rebuilt through `Session.Rehydrate` (ADR-0007 D6). Any mapping approach that needs to write to properties is fighting the type.
- **This store must return copies.** That is not incidental — it is the property that makes the contract suite's write-back test meaningful.
- **The dependency must not leak.** Whatever library is chosen appears in this project and nowhere else. `QuestWorlds.Session` stays at Ce 0.
- **It is demonstration code.** It has to be honest — a real database, real serialization, real reload — while staying small enough to read on a slide.
- **`ConnectionId` is a SignalR handle.** Persisting it stores something that is meaningless the moment the process it referred to is gone.

## Decision

**`Microsoft.Data.Sqlite` over a two-table schema, replacing the whole aggregate on save, with the schema created once at startup.**

### D1. `Microsoft.Data.Sqlite`, not EF Core

The ADO.NET provider, used directly. No ORM.

*Why*: EF Core's value is change tracking, identity mapping, migrations, and LINQ over a large model. This module has one aggregate, saved whole and loaded whole, with no queries. Every EF Core feature is unused, and two of them actively fight the design — change tracking duplicates the responsibility ADR-0007 assigned to the coordinator, and mapping a type with no setters and a static rehydration factory requires backing-field configuration to work around exactly the encapsulation the module is demonstrating.

*The cost*: the mapping is hand-written. For this aggregate that is roughly sixty lines, and those sixty lines are the interesting part of the demonstration rather than boilerplate hidden behind an ORM.

### D2. Two tables, with the GM stored as a participant

```sql
CREATE TABLE IF NOT EXISTS Sessions (
    Id     TEXT PRIMARY KEY NOT NULL,
    State  TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Participants (
    SessionId    TEXT NOT NULL,
    Ordinal      INTEGER NOT NULL,
    Name         TEXT NOT NULL,
    Role         TEXT NOT NULL,
    ConnectionId TEXT NOT NULL,
    PRIMARY KEY (SessionId, Ordinal),
    FOREIGN KEY (SessionId) REFERENCES Sessions(Id) ON DELETE CASCADE
);
```

*Why normalized rather than a JSON blob in one column*: a blob would be less code, and for an aggregate this small it is a defensible engineering choice. It is rejected because it would make the SQLite store a thin disguise over the in-memory one — the same object graph, serialized — and the point of this module is to show an adapter doing the real mapping work that a port makes possible. A reader should be able to open the database and see the session.

*Why the GM is a `Participants` row rather than columns on `Sessions`*: `Participant` is one type with a `Role`, so it maps to one table. Splitting the GM into `GmName`/`GmConnectionId` columns would encode the GM/player distinction twice — once in `ParticipantRole`, once in the schema — and *knowledge should not be duplicated*. Loading splits the rows by role; `Session.Rehydrate` takes the GM and the players separately, which is exactly the shape that comes back.

*Why `Ordinal`*: `Session.Players` is an ordered list, and a table is a set. The column preserves join order across a round trip so that a reloaded session equals the one that was stored.

### D3. Enums are stored as text, not integers

`State` and `Role` are written as their enum names (`AwaitingPlayerAbility`, `Player`).

*Why*: an enum's numeric value is positional. Inserting a new `SessionState` member in the middle — plausible, since the states describe a workflow — would silently reinterpret every stored row. Text survives reordering, and makes the table readable. The cost is a parse on load and a few bytes per row.

### D4. `SaveAsync` replaces the whole aggregate, inside a transaction

```
BEGIN
  INSERT INTO Sessions(Id, State) VALUES(@id, @state)
    ON CONFLICT(Id) DO UPDATE SET State = excluded.State
  DELETE FROM Participants WHERE SessionId = @id
  INSERT INTO Participants(...) VALUES(...)   -- GM first, then players in order
COMMIT
```

*Why delete-and-reinsert rather than diffing the participants*: diffing requires identity for a `Participant`, which is a value object with no id, and it would put change-tracking responsibility in the store — the very thing ADR-0007 D5 placed in the coordinator. Rewriting a handful of rows is cheap and has one obvious meaning.

*Why a transaction*: between the `DELETE` and the last `INSERT` the session has no participants. A concurrent `GetAsync` must not observe that. The upsert satisfies ADR-0007's obligation that `SaveAsync` never fails because a session does or does not already exist.

### D5. A connection per operation; the store holds only a connection string

`Microsoft.Data.Sqlite` pools connections, so opening one per operation is cheap and avoids sharing a connection across concurrent SignalR callbacks. The store is therefore stateless beyond its connection string and is safely registered as a singleton (ADR-0008 D2).

The database is opened in **WAL** mode so a reader is not blocked by a writer:

```
PRAGMA journal_mode = WAL;
```

### D6. The schema is created once, at startup, by an explicit call

```csharp
// QuestWorlds.SqliteSessionStore
public static IServiceCollection AddSqliteSessionStore(this IServiceCollection services, string connectionString);
public static Task InitialiseSqliteSessionStoreAsync(this IServiceProvider services, CancellationToken ct = default);
```

The host calls the initialiser during startup. It runs the `CREATE TABLE IF NOT EXISTS` statements and sets WAL mode.

*Why not lazily on first use*: it would put a "have I initialised?" check, and the lock that protects it, on the hot path of every operation, to save one line at startup.

*Why not migrations*: there is one version of one schema. A migration framework is a dependency and a set of files to justify the day there is a second version. *Do not add new types without necessity.*

*Why not at registration time*: registering services should not open a database. Side effects at registration make the composition root lie about what it does.

### D7. Where the database lives is the host's decision

The module takes a connection string and nothing else. It does not read configuration, choose a directory, or default to a path. `QuestWorlds.Web` does not call it at all (ADR-0008 D4), so the only caller today is the test suite, which uses a temporary file per test.

*Why a file rather than SQLite's in-memory mode in tests*: AC12 requires that a session written by one store instance is readable by a **different** instance over the same database — the path a restart takes. Shared-cache in-memory SQLite can be made to do this, but a temp file tests the thing the requirement actually names, and deletes just as easily.

### D8. `ConnectionId` is persisted, and is stale on reload

The store round-trips what it was given, including each participant's `ConnectionId`. It does not blank the column, and it does not try to detect staleness.

*Why*: ADR-0007 makes the store an information holder that decides nothing. A store that edited the data it was handed would be making a judgement about the meaning of a field — and it is not the store's judgement to make. The alternative, storing an empty string, replaces a value that is *known to be stale* with one that is *silently wrong*, which is worse.

**What this means, stated plainly**: a session reloaded after a restart is correct as data — its id, its GM, its players, their names, its state — and its connection ids point at connections that no longer exist. Nobody can be messaged through a reloaded session until they reconnect and are re-associated. **Reconnection is out of scope** for this spec, so today a reloaded session is durable history rather than a resumable game. Closing that gap is a separate piece of work and needs its own requirement.

### Architecture Overview

```
   IAmASessionStore  (declared in QuestWorlds.Session)
            ▲
            │ implements
   ┌────────┴──────────────────────────────────────────┐
   │ QuestWorlds.SqliteSessionStore                    │
   │                                                   │
   │   SqliteSessionStore      : IAmASessionStore      │
   │       ↳ holds a connection string, nothing else   │
   │   SessionRecordMapper     : Session ⇄ rows        │
   │   SchemaInitialiser       : CREATE IF NOT EXISTS  │
   │                                                   │
   │   Microsoft.Data.Sqlite ─────────────────────┐    │
   └──────────────────────────────────────────────┼────┘
                                                  │
                          the dependency stops here — Session has none

   Save:  Session ──► rows  ──► [ transaction: upsert + delete + insert ]
   Get:   rows    ──► Session.Rehydrate(id, gm, players, state)   ← a copy, always
```

### Key Components

**`SqliteSessionStore`** (Role: Information Holder)

- **Knowing**: how to reach the database
- **Doing**: save, get, and remove sessions, each in its own connection
- **Deciding**: nothing — not whether a save is a create or an update, and not what a stored `ConnectionId` is worth

**`SessionRecordMapper`** (Role: Interfacer)

- **Knowing**: how a `Session` corresponds to rows in two tables, and how enums are spelled
- **Doing**: turning a session into rows and rows back into a session via `Session.Rehydrate`
- **Deciding**: nothing

**`SchemaInitialiser`** (Role: Service Provider)

- **Knowing**: the schema
- **Doing**: creating it if absent, and setting WAL mode
- **Deciding**: nothing

### Implementation Approach

| # | Change | Kind |
|---|---|---|
| 1 | Project, `Microsoft.Data.Sqlite` reference, added to `QuestWorlds.slnx` | Structural |
| 2 | `SchemaInitialiser` and the registration extensions | Structural |
| 3 | `SessionRecordMapper`, driven by round-trip tests | Structural + new tests |
| 4 | `SqliteSessionStore`, made to pass the inherited `SessionStoreContract` | Structural + new tests |
| 5 | The reload-across-instances test (AC12) — the one the in-memory store cannot satisfy | New test |

No behavioural change to existing code. This module is additive.

## Consequences

### Positive

- **The seam is demonstrated, not asserted.** A store that serializes to disk and hands back copies is what makes ADR-0007 D5 observable, and what makes the write-back defect a caught bug rather than a latent one.
- **Sessions survive a restart**, which is the requirement that named this spec.
- **The dependency is contained.** `Microsoft.Data.Sqlite` appears in one project. `QuestWorlds.Session` stays at Ce 0.
- **The database is readable.** Two tables with text enums can be opened and understood, which matters for a demonstration.
- **The mapping is visible.** Sixty lines showing what an adapter does is the point, not an inconvenience.

### Negative

- **Hand-written mapping is hand-maintained.** Adding a field to `Session` means touching the schema, the writer, and the reader, with nothing to remind you.
- **A reloaded session is not yet a resumable game** (D8). The spec's title promises more than the spec's scope delivers, and the gap is reconnection.
- **Whole-aggregate rewriting on every save** is wasteful in principle, though not at this size.
- **The store is not exercised by the application**, only by tests, per ADR-0008 D4.
- **SQLite is single-writer.** This store makes sessions durable; it does not make them shareable across servers, which is the other half of why the port exists.

### Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| A field added to `Session` is silently not persisted | A round-trip test asserting full equality of a fully-populated session, so an unmapped field fails rather than disappears |
| Enum member added or reordered | D3 stores names, so reordering is safe; an unknown name on load fails loudly rather than defaulting |
| `Players` come back in a different order | `Ordinal` (D2), asserted by the round-trip test |
| Two writers interleave and one save is lost | Not solved here — the lost-update risk recorded in ADR-0007 stands, and SQLite's single-writer lock narrows but does not close it. Needs optimistic concurrency, which is a separate decision |
| Someone concludes from this module that sessions now survive a restart *usefully* | D8 states the limitation in the ADR, and it should be stated wherever the feature is described |
| Tests leave temp database files behind | Each test owns its file and deletes it on dispose |

## Alternatives Considered

### 1. EF Core

Mapping, migrations, and provider portability for free. Rejected: every feature it brings is unused here, its change tracking duplicates a responsibility ADR-0007 deliberately placed elsewhere, and mapping a type with no setters and a static rehydration factory means configuring backing fields to defeat the encapsulation this module exists to showcase.

### 2. Dapper

A middle path — hand-written SQL, less ceremony than raw ADO.NET. A reasonable choice, rejected only because `Microsoft.Data.Sqlite` alone is sufficient at this size and is one fewer dependency to explain. If the mapping grows, this is the first thing to reconsider.

### 3. One table with the session as a JSON column

Least code, no join, no ordinal, trivially tolerant of shape changes. Rejected because it makes the SQLite store a serialized copy of the in-memory one and skips the mapping that is the demonstration's substance. Worth revisiting if the aggregate grows awkward to map.

### 4. A different store technology — Redis, LiteDB, Postgres

Redis fits ephemeral session data well and would also solve the multi-server half of the problem. Rejected for this spec: it needs a running server, which turns "clone and run the tests" into "clone, install, and run the tests". SQLite is a file. The port means this choice can be revisited without touching `QuestWorlds.Session` — which is the whole argument.

### 5. Blanking `ConnectionId` on load, or refusing to persist it

Avoids handing back values that cannot be used. Rejected in D8: it makes the store decide what a field means, and replaces a knowably-stale value with a silently-wrong one.

## References

- Requirements: [specs/0002-persistent_sessions/requirements.md](../../specs/0002-persistent_sessions/requirements.md)
- Issue: [#2](https://github.com/iancooper/quesworlds-contest/issues/2)
- Related ADRs:
  - [0007-session-storage-port.md](0007-session-storage-port.md) — the contract this implements
  - [0008-session-store-module-composition.md](0008-session-store-module-composition.md) — where this module sits and how it is tested
  - [0002-session-management.md](0002-session-management.md) — rejected database-backed sessions as "overkill for MVP"; this supersedes that
- Design principles: [.agent_instructions/design_principles.md](../../.agent_instructions/design_principles.md)
- External references:
  - [Microsoft.Data.Sqlite documentation](https://learn.microsoft.com/dotnet/standard/data/sqlite/)
  - [SQLite write-ahead logging](https://www.sqlite.org/wal.html)
  - [SQLite UPSERT](https://www.sqlite.org/lang_UPSERT.html)
