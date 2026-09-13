# Requirements

> **Note**: This document captures user requirements and needs. Technical design decisions and implementation details should be documented in an Architecture Decision Record (ADR) in `docs/adr/`.

**Linked Issue**: [#2 — Session should expose storage as a driven port, not hide it entirely](https://github.com/iancooper/quesworlds-contest/issues/2)

> **Revised** to follow the issue's revised description and [the refinement comment](https://github.com/iancooper/quesworlds-contest/issues/2#issuecomment-5655430214). The earlier draft had the in-memory store staying inside `QuestWorlds.Session` as an internal default, registered via `TryAddSingleton`, with the no-argument `CreateCoordinator()` preserved — and conceded that exporting the port "widens the module's interface". That concession was withdrawn. See *What changed and why* at the foot of this document.

## Problem Statement

As a **host application author** I would like to **choose which session store my deployment uses**, so that **sessions can survive a process restart, or be shared across several web servers, without editing `QuestWorlds.Session`**.

As a **test author** I would like to **substitute a session store the way I substitute any other module**, so that **I can test storage-dependent behaviour without `InternalsVisibleTo` and without reaching into the module under test**.

As a **conference speaker presenting "Modules in C#: Where Did We All Go Wrong"** I would like to **show `Session` with a real driven port and a store module behind it**, so that **the Ports & Adapters slide can show a session store instead of the speaker having to admit `Session` has no port at all**.

Today `QuestWorlds.Session` hides its storage completely. `ISessionRepository` and `InMemorySessionRepository` are both `internal`, `AddSessionModule()` registers the pair unconditionally, and `SessionModule.CreateCoordinator()` news up `InMemorySessionRepository` directly. Sessions are therefore in-memory permanently, by construction.

**Sessions surviving a restart is a functional requirement, not a refinement. A design that cannot express it is not a narrower design, it is an incomplete one.**

The module is also inconsistent with how the neighbouring assembly already solves the same problem — `QuestWorlds.Web` declares `IContestFrameStore` as a public port with an implementation behind it.

## Proposed Solution

**The store is another module. `Session` holds only the interface.**

`QuestWorlds.Session` declares `ISessionRepository` and contains no implementation of it. The in-memory store moves out into a module of its own, which implements the interface and therefore references `Session`. A persistent store would be a second implementation of the same interface, in its own module, with nothing in `Session` changing. The host picks which store to use at its composition root.

### Why this is not a widened interface

`ISessionRepository` is not `Session` leaking an internal. It is the seam between two modules, which is what module interfaces are for. **A module that cannot name its dependency on another module is not narrower, it is incomplete.**

It also lines the code up with the account of test doubles the talk gives: a double replaces *another module*, never a collaborator inside the module under test. Today a `Session` test cannot substitute storage at all.

### Which way the dependency points

`Session` declares the interface and references nothing. The store implements it and therefore references `Session`. The arrow points inward — this is not `Session` reaching out for a database.

The coupling numbers say the same thing. `Session`'s efferent coupling **stays 0** and its afferent coupling goes 1 → 2, so it becomes *more* stable, not less:

| Module | Ce | Ca | I |
|---|---|---|---|
| Framing | 0 | 3 | 0.00 |
| Session | 0 | 2 | 0.00 |
| Resolution | 1 | 3 | 0.25 |
| SessionStore | 1 | 1 | 0.50 |
| DiceRoller | 1 | 1 | 0.50 |
| Outcome | 2 | 1 | 0.67 |
| Web | 6 | 0 | 1.00 |

A module gains a substitutable dependency and its instability does not move, because the adapter is the unstable thing.

*(Verified against the code: `QuestWorlds.Session.csproj` has zero `ProjectReference` elements today, and `QuestWorlds.Web.csproj` has five — going to six when it references one store module.)*

#### Consequence for the coupling table

The table above is the issue's, and it assumes **one** store module called `SessionStore`. Two things move it. Per C4 the stores are named for their technology, so that row is really `InMemorySessionStore`; and per FR11 a second store ships in the same change. Because `QuestWorlds.Web` registers only the in-memory store (FR13), it references only that one, so **`Web`'s Ce stays 6 as the issue's table says**, and the new module simply appears as an extra row with no consumer in the app:

| Module | Ce | Ca | I | note |
|---|---|---|---|---|
| Session | 0 | 3 | 0.00 | Ca 1 → 3: Web, and both stores |
| InMemorySessionStore | 1 | 1 | 0.50 | referenced by Web |
| SqliteSessionStore | 1 | 0 | 1.00 | referenced by its tests only |
| Web | 6 | 0 | 1.00 | unchanged from the issue's figure |

`Session`'s Ce stays 0 and its Ca improves further than the issue predicted — 1 → **3** rather than 1 → 2, because both stores reference it. The claim the talk makes gets stronger, not weaker.

> **Open for the ADR**: if `Web` referenced both stores and chose between them by configuration, `Web`'s Ce would become 7 and `SqliteSessionStore`'s Ca 1. That buys a live demo — flip a setting, sessions survive a restart — at the cost of the published figure. FR13 takes the cheaper option; the ADR may revisit it.

## Requirements

### Functional Requirements

**FR1 — Storage is an exported port**
`ISessionRepository` becomes public, and is `QuestWorlds.Session`'s second export alongside `ISessionCoordinator`. A host or test in any assembly can implement it.

**FR2 — `Session` contains no store**
`QuestWorlds.Session` contains no implementation of `ISessionRepository`. The in-memory store moves out of the assembly entirely.

**FR3 — The in-memory store is its own module**
`QuestWorlds.InMemorySessionStore` implements `ISessionRepository` in memory and references `QuestWorlds.Session`. It is a peer of the persistent store, not a privileged default.

**FR4 — `Session` takes on no new coupling**
`QuestWorlds.Session.csproj` continues to have zero `ProjectReference` elements. Ce stays 0.

**FR5 — The store is chosen at the composition root**
`AddSessionModule()` stops registering any `ISessionRepository`. The host registers the store module it wants. `QuestWorlds.Web`, as the only composition root in the solution today, registers the in-memory store and so continues to behave as it does now.

**FR6 — Container-free construction requires a store**
`SessionModule.CreateCoordinator(ISessionRepository)` takes the store. Because `Session` can no longer construct one, **the no-argument `CreateCoordinator()` is removed**. This is a deliberate breaking change and the one place the cost of this design is felt.

**FR7 — Id generation stays hidden**
`ISessionIdGenerator` and `SessionIdGenerator` remain internal to `QuestWorlds.Session`. Generating an id is a genuine implementation detail; nobody substitutes it at deployment time. This feature is about storage only.

**FR8 — The coordinator writes every session change back through the port**
Every operation that changes a session must persist that change via the port, rather than relying on holding a live object reference. This is what makes the port genuinely substitutable: an out-of-process store hands back a *copy*, so a mutation never written back is silently lost.

> This is a real defect today, not a hypothetical: `SessionCoordinator.JoinSession` calls `session.AddPlayer(player)` and never calls `_repository.Update(session)` (`src/QuestWorlds.Session/SessionCoordinator.cs:31`). It works only because `InMemorySessionRepository` returns the same object its dictionary holds. Against a persistent adapter the player would vanish. Shipping the port without fixing this would export a seam that does not actually work — and it matters more now than in the earlier draft, because the store is no longer even in the same assembly.

**FR9 — A stored session can be reconstructed by a store module**
A store module must be able to turn stored data back into a `Session` carrying its id, GM, players, and state, using only `QuestWorlds.Session`'s public surface. An out-of-assembly implementer has no other option.

**FR10 — The coordinator becomes async, and so does the hub's use of it**
`ISessionCoordinator` calls the port, so an async port (C2) makes the coordinator async too; blocking on the result with `.Result` or `.GetAwaiter().GetResult()` is not an acceptable way to avoid this. `ISessionCoordinator`'s methods therefore become async, and `ContestHub` awaits them.

> Cost check: all seven of `ContestHub`'s calls to the coordinator already sit inside `public async Task` methods (`CreateSession`, `JoinSession`, `FrameContest`, `SubmitAbility`, `ResolveContest`, `StartNewContest`), so this is a mechanical `await` insertion, not a restructuring. SignalR hub methods are async by nature. This does mean `ISessionCoordinator` — a contract the earlier draft placed out of scope — now changes.

**FR11 — A persistent store ships as a second module**
`QuestWorlds.SqliteSessionStore` implements the same `ISessionRepository` against SQLite, in its own module, referencing `QuestWorlds.Session` and nothing else of ours. It is what makes the seam more than an assertion: a session written by it survives the process exiting.

**FR12 — The two stores are interchangeable**
Both stores satisfy the same behavioural contract, and a suite of contract tests is run against each. Substituting one for the other requires no change in `QuestWorlds.Session`, in `ISessionCoordinator`, or in `ContestHub` — only a different registration at the composition root.

**FR13 — `QuestWorlds.Web` keeps today's behaviour**
The web app registers the in-memory store, so a user sees no change. `QuestWorlds.SqliteSessionStore` is proven by its own tests rather than by being wired into the running app.

### Non-functional Requirements

- **Stability of `Session`**: Ce stays 0, Ca goes 1 → 2. The module becomes more stable, not less. This is the measurable claim the talk makes and the change must not contradict it.
- **Contract minimalism**: `QuestWorlds.Session`'s public surface grows by exactly one interface. The id generator, the coordinator implementation, and the session internals stay hidden.
- **No new dependencies in `Session`**: `QuestWorlds.Session` takes on no persistence technology, project reference, or serialization concern. It declares the port; adapters live elsewhere.
- **Concurrency**: the in-memory store remains safe for concurrent access from SignalR hub callbacks, as it is today.
- **Behavioural continuity for the running app**: a user of the web app sees no change. The default deployment still stores sessions in memory.
- **Documentation**: the exported port carries XML documentation to the standard of the module's other public types, stating the contract an implementer must honour — including the write-back expectation in FR8.

### Constraints and Assumptions

- **C1**: The module targets `net9.0`. `QuestWorlds.Session` references `Microsoft.Extensions.DependencyInjection.Abstractions` as a *package*, which does not count against Ce as the issue measures it (`ProjectReference` elements). The new store module will need the same package if it ships its own registration extension.
- **C2 (settled)**: **The port is asynchronous.** It is synchronous today (`void Add`, `Session? Get`, …), but it is now public and crosses a module boundary, so changing its shape later would break a published seam. An async contract is what implementers will expect of a store. The in-memory store therefore implements an async contract over a synchronous data structure, completing immediately. *(Note: that needs `Task.FromResult`/`Task.CompletedTask`, not `TaskCompletionSource` — there is nothing to signal. Whether the port returns `Task` or `ValueTask`, and whether it takes a `CancellationToken`, is left to the ADR.)*
- **C3 (settled)**: **Each store module gets its own test project**, because each is now a public module in its own right rather than an internal detail of `Session`. New projects are added to `QuestWorlds.slnx` under `/src/` and `/tests/` respectively.
- **C4 (settled)**: **Store modules are named for their technology**: `QuestWorlds.InMemorySessionStore` and `QuestWorlds.SqliteSessionStore`. Neither is the generic `SessionStore` of the issue's coupling table — see *Consequence for the coupling table* below.
- **A1**: `Session` can be rehydrated through its existing public surface — `new Session(id, gm)`, then `AddPlayer` per player, then `TransitionTo(state)` — and `Participant` is already a public record. To be confirmed in design; if it does not hold, FR9 requires a further change to `Session`.
- **C5 (new)**: `QuestWorlds.SqliteSessionStore` takes a SQLite dependency (`Microsoft.Data.Sqlite`, or an ORM). Choice of library, schema, where the database file lives, and how the schema is created are **design decisions for the ADR**. The dependency is confined to that module; `Session` takes on nothing (FR4).
- **C6 (new)**: A `Session` persisted to SQLite carries each participant's `ConnectionId` — a SignalR value that is meaningless once the process restarts. Sessions therefore survive a restart *as data*, but the participants' connections do not, and reconnection is out of scope. The ADR should say plainly what a reloaded session is good for.
- **A3**: The `Session` object graph is small (an id, a GM, a handful of players, a state) so a store-and-reload model is viable without incremental change tracking.
- **A4**: `QuestWorlds.Web` is the only composition root in the solution, so FR5 has exactly one production call site to update.

### Out of Scope

- ~~Writing an actual persistent store.~~ **No longer out of scope** — FR11 delivers `QuestWorlds.SqliteSessionStore`. Stores for other technologies (Redis, Cosmos, SQL Server, distributed cache) remain out of scope.
- Wiring the SQLite store into `QuestWorlds.Web`, or a configuration switch to choose a store at startup. Web registers the in-memory store (FR13); see *Consequence for the coupling table*.
- Session expiry, eviction, TTL, or cleanup of abandoned sessions.
- Reconnection behaviour: reattaching a participant after a dropped SignalR connection, and the `ConnectionId`-refresh problem that comes with it.
- Making `IContestFrameStore` (in `QuestWorlds.Web`) a module of its own, or unifying it with session storage.
- Exporting `ISessionIdGenerator`, or any other `Session` internal, as a substitution point.
- ~~Changing `ISessionCoordinator`'s contract as seen by `QuestWorlds.Web`.~~ **No longer out of scope** — see FR10. An async port forces an async coordinator.
- Multi-server SignalR concerns such as a backplane.

## Acceptance Criteria

From the issue's acceptance list, plus what FR8/FR9 add:

- [ ] **AC1** — `QuestWorlds.Session` contains no implementation of `ISessionRepository`.
- [ ] **AC2** — `QuestWorlds.Session.csproj` still has **zero** `ProjectReference` elements — Ce stays 0.
- [ ] **AC3** — The in-memory store is its own module and references `QuestWorlds.Session`.
- [ ] **AC4** — A test project can supply its own `ISessionRepository` without `InternalsVisibleTo` and without touching `QuestWorlds.Session`.
- [ ] **AC5** — `ISessionIdGenerator` remains `internal`.
- [ ] **AC6** — The existing Session tests pass. They go through `SessionCoordinatorBuilder`, which calls the no-argument `CreateCoordinator()`; the builder passes the in-memory store instead. **That is the only change expected to existing tests.**
- [ ] **AC7** — `QuestWorlds.Web` runs unchanged in behaviour: its composition root registers the in-memory store, and session handling works end to end as before.
- [ ] **AC8** — With a substitute store that returns a *copy* on `Get` (mimicking an out-of-process adapter), a player who joins a session is still present when the session is next read. **This test fails today.**
- [ ] **AC9** — A substitute store can reconstruct a `Session` with its id, GM, players, and state from data alone, using only `QuestWorlds.Session`'s public API.
- [ ] **AC10** — `QuestWorlds.SqliteSessionStore` is its own module, references `QuestWorlds.Session`, and its SQLite dependency appears nowhere else.
- [ ] **AC11** — One suite of contract tests passes against **both** stores, so "interchangeable" is demonstrated rather than asserted.
- [ ] **AC12** — A session written through the SQLite store is readable by a *different* store instance over the same database — the store-and-reload path a restart takes. This is the criterion the in-memory store cannot satisfy and the spec's title depends on.
- [ ] **AC13** — Substituting SQLite for in-memory requires no change to `QuestWorlds.Session`, `ISessionCoordinator`, or `ContestHub` — only a different registration.
- [ ] **AC14** — `ISessionCoordinator` and the port are async, and no implementation blocks on a task (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`).
- [ ] **AC15** — The whole solution builds and every existing test across all test projects passes.

**Testing approach**: xUnit, following the repository's existing `When_<scenario>_should_<expectation>` class-per-scenario convention, with builders for arrangement. Three test projects are involved:

- `tests/QuestWorlds.Session.Tests` — references `QuestWorlds.Session` as an ordinary consumer; its inability to see internals is precisely what AC4 asserts. Gains a reference to the in-memory store for AC6. The copy-returning substitute store is the main new double and is what makes AC8 meaningful.
- `tests/QuestWorlds.InMemorySessionStore.Tests` and `tests/QuestWorlds.SqliteSessionStore.Tests` (C3) — each runs the shared contract suite (AC11) plus whatever is specific to it. The SQLite project owns AC12.

Where the shared contract suite physically lives so that two test projects can run it is a **design decision for the ADR**.

**Definition of done**: all acceptance criteria met; all four new projects (two store modules, two test projects) added to `QuestWorlds.slnx`; ADR recorded in `docs/adr/` and linked from `specs/0002-persistent_sessions/.adr-list`; `README.md` module table updated to list both store modules; issue #2 updated with the scope change it does not currently describe, then closed.

## Additional Context

Current state of the code under discussion:

```csharp
// src/QuestWorlds.Session/SessionRepository.cs
internal interface ISessionRepository { ... }
internal class InMemorySessionRepository : ISessionRepository { ... }

// src/QuestWorlds.Session/ServiceCollectionExtensions.cs
services.AddSingleton<ISessionIdGenerator, SessionIdGenerator>();
services.AddSingleton<ISessionRepository, InMemorySessionRepository>();
services.AddSingleton<ISessionCoordinator, SessionCoordinator>();

// src/QuestWorlds.Session/SessionModule.cs
var repository = new InMemorySessionRepository();
```

The precedent to follow is `src/QuestWorlds.Web/Services/IContestFrameStore.cs`: a public port with an implementation behind it.

**Why now**: this repository supports the talk *"Modules in C#: Where Did We All Go Wrong."* The Ports & Adapters slide shows the driven side as `Frame store` and `SignalR clients` only, and the speaker notes have to admit that `Session` keeps its storage internal and so has no port at all. With this change the slide can show a session store — in-memory here, a database in production — with nothing in the centre changing.

**Naming note**: the spec directory is `0002-persistent_sessions`. With FR11 in scope the name is now accurate — the deliverable is the seam, an in-memory store, and a SQLite store that genuinely persists.

### What changed and why

The issue was revised after the first draft of these requirements. The revision withdraws the original's concession that exporting the port "widens the module's interface", on the grounds that the store is another module and naming a dependency on another module is not widening. That reframing changes the shape of the work:

| Area | First draft | Now |
|---|---|---|
| In-memory store | Stays `internal` in `QuestWorlds.Session` as the default adapter | **Moves out** into its own module (FR2, FR3) |
| `AddSessionModule()` | Registers in-memory via `TryAddSingleton` so a host can override | **Registers no repository at all**; host chooses (FR5) |
| `CreateCoordinator()` | No-arg form kept, defaulting to in-memory; overload added | **No-arg form removed** (FR6) |
| Default behaviour | Unchanged for any caller doing nothing | Unchanged *for the app*, but a host must now wire a store (FR5, AC7) |
| Framing | A trade-off: narrower interface sacrificed for substitutability | Not a trade-off: the seam between two modules (see *Why this is not a widened interface*) |
| Coupling | Not addressed | An explicit, checkable claim: Ce 0, Ca 1 → 2 (FR4, AC2) |

Carried over unchanged from the first draft, as neither is mentioned in the issue: **FR8** (the `JoinSession` write-back defect) and **FR9** (rehydration through the public surface). The SQLite store (FR11) turns both from arguments into demonstrations — it genuinely returns copies, so an unwritten mutation is genuinely lost. Both grew in importance — with the store in a different assembly, an implementer has only the public API to work with, and a mutation that is never written back is now a cross-module bug rather than an internal one.

### Design decisions since settled

The three questions this document left open have been answered, and one answer widened the scope beyond what issue #2 describes:

| Question | Decision | Where |
|---|---|---|
| Sync or async port? | **Async.** The port is public and crosses a module boundary, so its shape is a published contract; async is what implementers expect. Ripples into `ISessionCoordinator` and `ContestHub`. | C2, FR10, AC14 |
| What is the store module called? | **Named for its technology**: `QuestWorlds.InMemorySessionStore`, `QuestWorlds.SqliteSessionStore` — not the issue's generic `SessionStore`. | C4 |
| Does it get its own test project? | **Yes**, one per store module, now that each is a public module rather than an internal detail. | C3 |
| Is the SQLite store built now? | **Yes** — it ships in this spec, not a later one. `QuestWorlds.Web` still registers in-memory, so the running app is unchanged. | FR11, FR13, AC10–AC13 |

**Issue #2 does not yet describe this scope.** It says the deliverable is the port plus the in-memory module, with a persistent store as a later second implementation; FR11 brings that forward. The issue should be updated before implementation starts, and its coupling table reviewed against *Consequence for the coupling table* above.

Still open for the ADR: SQLite library, schema, and database file location (C5); what a reloaded session means given stale `ConnectionId`s (C6); where the shared contract test suite lives; `Task` vs `ValueTask` and whether the port takes a `CancellationToken` (C2); and whether `Web` should reference both stores and switch by configuration.
