# 0009. SQLite Session Store

Date: 2026-09-14

## Status

Proposed

## Context

**Parent Requirement**: [specs/0002-persistent_sessions/requirements.md](../../specs/0002-persistent_sessions/requirements.md)

**Scope**: This ADR decides **what goes inside `QuestWorlds.SqliteSessionStore`** — data access library, schema, how a `Session` and its `ContestFrame` map to rows, how the schema comes into being, connection handling, and what a reloaded session actually means. It takes [ADR-0007](0007-session-storage-port.md)'s contract, [ADR-0008](0008-session-store-module-composition.md)'s composition, and [ADR-0010](0010-contest-frame-storage-port.md)'s frame port as given.

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

Per [ADR-0010](0010-contest-frame-storage-port.md) this store also holds the contest frame, which is one row per session plus its modifiers:

```sql
CREATE TABLE IF NOT EXISTS ContestFrames (
    SessionId          TEXT PRIMARY KEY NOT NULL,
    Prize              TEXT NOT NULL,
    ResistanceBase     INTEGER NOT NULL,
    ResistanceMasteries INTEGER NOT NULL,
    ResistanceModifier INTEGER NOT NULL,
    PlayerAbilityName  TEXT NULL,
    PlayerRatingBase   INTEGER NULL,
    PlayerRatingMasteries INTEGER NULL,
    FOREIGN KEY (SessionId) REFERENCES Sessions(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS ContestModifiers (
    SessionId TEXT NOT NULL,
    Ordinal   INTEGER NOT NULL,
    Type      TEXT NOT NULL,
    Value     INTEGER NOT NULL,
    PRIMARY KEY (SessionId, Ordinal),
    FOREIGN KEY (SessionId) REFERENCES ContestFrames(SessionId) ON DELETE CASCADE
);
```

*Why `TargetNumber` and `Rating` become columns rather than a formatted string*: both are `readonly record struct`s of two or three ints with public constructors. Storing `Base`/`Masteries`/`Modifier` separately means loading needs a constructor call and no parser, and `Rating.Parse` stays a thing the UI does to user input rather than something the database depends on.

*Why the player's ability and rating are nullable*: a frame exists from the moment the GM sets a prize and resistance, and the player's side arrives later. The nullability in the schema is the nullability already on `ContestFrame`.

*Why the cascade runs from `Sessions`*: removing a session removes its frame and that frame's modifiers. A frame has no meaning without its session.

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

`SaveFrameAsync` follows the same shape — upsert the `ContestFrames` row, delete and reinsert `ContestModifiers`, in one transaction.

**The two saves are separate calls, and that is a deliberate limit.** ADR-0010 D4 puts both ports on one class so that a single transaction *can* cover a session and its frame, and within one call it does. It does not make `SaveAsync` and `SaveFrameAsync` atomic with each other, because the coordinator and the hub call them at different moments for different reasons. What the shared class buys is one database and one connection, so the two writes cannot end up in different places or different files — not a distributed transaction across two calls. A crash between them can still leave a saved session whose frame is one step behind; the frame is re-derivable by reframing, and that is the same exposure the application has today.

### D5. A connection per operation; the store holds only a connection string

`Microsoft.Data.Sqlite` pools connections, so opening one per operation is cheap and avoids sharing a connection across concurrent SignalR callbacks. The store is therefore stateless beyond its connection string and is safely registered as a singleton (ADR-0008 D2).

The database is opened in **WAL** mode so a reader is not blocked by a writer:

```
PRAGMA journal_mode = WAL;
```

### D6. The schema is created once at startup, by a hosted service the module registers

```csharp
// QuestWorlds.SqliteSessionStore
public static IServiceCollection AddSqliteSessionStore(this IServiceCollection services, string connectionString);
// also registers SqliteSessionStoreInitialiser : IHostedService

// available directly for tests, which do not run a host
public static Task InitialiseAsync(string connectionString, CancellationToken ct = default);
```

`AddSqliteSessionStore` registers both the store and an `IHostedService` that runs the `CREATE TABLE IF NOT EXISTS` statements and sets WAL mode when the application starts.

*Why a hosted service rather than an explicit call from the host*: ADR-0008 D4 selects the store by configuration, so an explicit call would have to be guarded by the same condition — `if (provider == "Sqlite")` written twice, in two files, with a silent failure mode if the second one is forgotten. Registering the initialiser alongside the store means choosing the store is the only decision the host makes, and initialisation cannot be left out.

*Why not lazily on first use*: it would put a "have I initialised?" check, and the lock that protects it, on the hot path of every operation, to save nothing.

*Why not at registration time*: registering services should not open a database. A hosted service runs at **start**, not at registration, so this objection does not apply to it.

*Why not migrations*: there is one version of one schema. A migration framework is a dependency and a set of files to justify the day there is a second version. *Do not add new types without necessity.*

### D7. Where the database lives is the host's decision

The module takes a connection string and nothing else. It does not read configuration, choose a directory, or default to a path — that would make the module responsible for a deployment decision, which is the mistake this whole feature exists to correct.

`QuestWorlds.Web` supplies it from `ConnectionStrings:SessionStore` (ADR-0008 D4), with a development default in `appsettings.Development.json`:

```json
{
  "SessionStore": { "Provider": "InMemory" },
  "ConnectionStrings": { "SessionStore": "Data Source=questworlds-sessions.db" }
}
```

A relative `Data Source` resolves against the process working directory. The file belongs in `.gitignore`.

*Why the default provider stays `InMemory` even though both are wired*: the running application must behave exactly as it does today for someone who changes nothing. Persistence is opt-in, and opting in is one setting — which is the demonstration.

Tests use a temporary file per test rather than SQLite's in-memory mode. *Why*: AC12 requires that a session written by one store instance is readable by a **different** instance over the same database — the path a restart takes. Shared-cache in-memory SQLite can be made to do this, but a temp file tests the thing the requirement actually names, and deletes just as easily.

### D8. `ConnectionId` is persisted, and is stale on reload

The store round-trips what it was given, including each participant's `ConnectionId`. It does not blank the column, and it does not try to detect staleness.

*Why*: ADR-0007 makes the store an information holder that decides nothing. A store that edited the data it was handed would be making a judgement about the meaning of a field — and it is not the store's judgement to make. The alternative, storing an empty string, replaces a value that is *known to be stale* with one that is *silently wrong*, which is worse.

**What this means, stated plainly**: a session reloaded after a restart is correct as data — its id, its GM, its players, their names, its state — **and now its contest frame too**, since ADR-0010 brought frame storage into this module. What does not survive is the connections: every stored `ConnectionId` points at a SignalR connection that no longer exists. Nobody can be messaged through a reloaded session until they reconnect and are re-associated.

So the gap has narrowed but not closed. Before ADR-0010 a restored session was missing its contest entirely and could only answer *"No contest has been framed"*. Now the game state is whole and only the transport is dead. **Reconnection remains out of scope**, so a reloaded session is still not a resumable game — but it is now one requirement away from being one, rather than two.

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
   │   ...Initialiser          : IHostedService        │
   │       ↳ CREATE IF NOT EXISTS, WAL, once at start  │
   │                                                   │
   │   Microsoft.Data.Sqlite ─────────────────────┐    │
   └──────────────────────────────────────────────┼────┘
                                                  │
                          the dependency stops here — Session has none

   Save:  Session ──► rows  ──► [ transaction: upsert + delete + insert ]
   Get:   rows    ──► Session.Rehydrate(id, gm, players, state)   ← a copy, always
   ... and the same for ContestFrame, over the same database and connection
```

### Key Components

**`SqliteSessionStore`** (Role: Information Holder)

- **Knowing**: how to reach the database
- **Doing**: save, get, and remove sessions, each in its own connection
- **Deciding**: nothing — not whether a save is a create or an update, and not what a stored `ConnectionId` is worth

**`SessionRecordMapper`** (Role: Interfacer)

- **Knowing**: how a `Session` and a `ContestFrame` correspond to rows, and how enums are spelled
- **Doing**: turning them into rows and back via `Session.Rehydrate` and `ContestFrame.Rehydrate`
- **Deciding**: nothing

**`SqliteSessionStoreInitialiser`** (Role: Service Provider — an `IHostedService`)

- **Knowing**: the schema
- **Doing**: creating it if absent and setting WAL mode, once, when the application starts
- **Deciding**: nothing

### Implementation Approach

| # | Change | Kind |
|---|---|---|
| 1 | Project, `Microsoft.Data.Sqlite` reference, added to `QuestWorlds.slnx` | Structural |
| 2 | The initialiser (`IHostedService`) and the registration extensions | Structural |
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
- **The substitution can be performed.** Set `SessionStore:Provider` to `Sqlite`, restart mid-session, and the session is still there — the demonstration that the port exists at all.

### Negative

- **Hand-written mapping is hand-maintained.** Adding a field to `Session` means touching the schema, the writer, and the reader, with nothing to remind you.
- **A reloaded session is not yet a resumable game** (D8). The spec's title still promises more than its scope delivers, though ADR-0010 reduced the gap to reconnection alone.
- **The schema is four tables, not two.** Hand-written mapping grows with it, and `ContestFrame`'s nullable player side is the fiddliest part of it.
- **Whole-aggregate rewriting on every save** is wasteful in principle, though not at this size.
- **The store is now on a supported path through the application**, per ADR-0008 D4, so its failure modes are the host's problem too: a bad connection string or an unwritable directory fails startup rather than a test.
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
| The development database file is committed by accident | `questworlds-sessions.db` and its `-wal`/`-shm` companions are added to `.gitignore` |
| Someone switches the provider to `Sqlite` and expects a mid-session restart to be seamless | D8 — the session reloads, the connections do not. Say so in the README beside the setting |

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
