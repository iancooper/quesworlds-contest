# 0010. Contest Frame Storage Port

Date: 2026-09-14

## Status

Proposed

## Context

**Parent Requirement**: [specs/0002-persistent_sessions/requirements.md](../../specs/0002-persistent_sessions/requirements.md)

**Scope**: This ADR does for the **contest frame** what [ADR-0007](0007-session-storage-port.md) did for the session: moves its storage port out of the host, makes it async, and lets a store module implement it. It also decides that **one store class implements both ports**.

### The problem this fixes

[ADR-0008](0008-session-store-module-composition.md) D4 made the SQLite store a supported configuration of the running application. Review of that decision found a hole it opened.

`IContestFrameStore` lives in `QuestWorlds.Web.Services` with an in-memory implementation beside it. It is not covered by this spec — it was explicitly out of scope. So with `SessionStore:Provider = "Sqlite"`, a restart in the middle of a contest produces:

- the **session** restored from SQLite, with `State = AwaitingPlayerAbility`
- the **frame** gone, because it was only ever in a dictionary in the web process

`ContestHub.SubmitAbility` handles the missing frame safely — it answers `"No contest has been framed"` ([ContestHub.cs:119](../../src/QuestWorlds.Web/Hubs/ContestHub.cs#L119)) — so nothing crashes. But the player is looking at a restored session whose state says *submit your ability*, which can now only ever return an error. The session is unusable until the GM reframes, and nothing says so.

**Persisting half of a contest is worse than persisting none of it.** A session and its frame are one unit of state or they are not worth storing.

### Forces

- **A store module cannot reference `QuestWorlds.Web`.** `Web` is the composition root, at I = 1.00; a store depending on it would invert the dependency the whole feature exists to establish. So the port cannot stay where it is.
- **The port belongs with the type it stores.** `ContestFrame` lives in `QuestWorlds.Framing`, which has **zero** project references — Ce 0, exactly like `QuestWorlds.Session`. It can declare a port without taking on any coupling.
- **The two pieces of state must be restored consistently.** A restart that produced a session without its frame, or a frame without its session, would reintroduce the bug this ADR exists to close.
- **`ContestFrame` cannot currently be reconstructed.** `PlayerAbilityName`, `PlayerRating` and `Modifiers` all have private setters and are reached only through `SetPlayerAbility` and `ApplyModifier`. The same problem `Session` had, and it needs the same answer.
- **Two ports, or one?** The stores could implement each port separately, or one class could implement both.

## Decision

**Move the frame port into `QuestWorlds.Framing` as `IAmAContestFrameStore`, make it async, and have a single store class in each store module implement both it and `IAmASessionStore`.**

### D1. The port moves to `QuestWorlds.Framing` and is renamed

```csharp
namespace QuestWorlds.Framing;

public interface IAmAContestFrameStore
{
    Task SaveFrameAsync(string sessionId, ContestFrame frame, CancellationToken cancellationToken = default);
    Task<ContestFrame?> GetFrameAsync(string sessionId, CancellationToken cancellationToken = default);
    Task ClearFrameAsync(string sessionId, CancellationToken cancellationToken = default);
}
```

`QuestWorlds.Web.Services.IContestFrameStore` and `InMemoryContestFrameStore` are deleted.

*Why `Framing` rather than `Session`*: the port stores a `ContestFrame`, and a port should be declared by the module that owns the type crossing it. Putting it in `Session` would make `QuestWorlds.Session` reference `QuestWorlds.Framing` and break its Ce 0.

*Why renamed*: `IAmA*` for role interfaces, per [design_principles.md](../../.agent_instructions/design_principles.md), consistent with `IAmASessionStore` (ADR-0007 D2). Since the type is moving assemblies anyway, this rename is free.

*Why `SetFrame` becomes `SaveFrameAsync`*: consistent with `IAmASessionStore.SaveAsync`, and "set" understates that this may reach a database.

### D2. The port is async, for the same reason the session port is

Same argument as ADR-0007 D3: a public port implemented by a SQLite adapter is doing I/O, and a synchronous contract would force blocking. `Task`, not `ValueTask`; `CancellationToken` defaulted.

All six of `ContestHub`'s frame-store calls are already inside `async Task` methods, so this is `await` insertion.

### D3. `ContestFrame` gains a rehydration factory

`ContestFrame` has the problem `Session` had and takes the same answer (ADR-0007 D6):

```csharp
public static ContestFrame Rehydrate(
    string prize,
    TargetNumber resistance,
    string? playerAbilityName,
    Rating? playerRating,
    IEnumerable<Modifier> modifiers);
```

*Why not replay `SetPlayerAbility` and `ApplyModifier` on load*: both enforce rules — `ApplyModifier` validates modifier values, `SetPlayerAbility` guards the workflow — and loading a row should not re-run a rule that was satisfied when the GM applied it. Validation belongs at the point of decision.

`Modifier`, `Rating` and `TargetNumber` are already public `readonly record struct`s with public constructors, so nothing else needs to change.

### D4. One store class implements both ports

```csharp
// QuestWorlds.SqliteSessionStore
internal sealed class SqliteSessionStore : IAmASessionStore, IAmAContestFrameStore
```

Each store module references **both** `QuestWorlds.Session` and `QuestWorlds.Framing`, and registers its single implementation against both ports:

```csharp
services.AddSingleton<SqliteSessionStore>();
services.AddSingleton<IAmASessionStore>(sp => sp.GetRequiredService<SqliteSessionStore>());
services.AddSingleton<IAmAContestFrameStore>(sp => sp.GetRequiredService<SqliteSessionStore>());
```

*Why one class rather than two*: the two pieces of state are restored together or the application is in the inconsistent position this ADR exists to fix. One class over one database can write both inside **one transaction** and read both from one connection. Two independent stores could not offer that without inventing a shared transaction abstraction, which is a far larger thing to build than the problem deserves.

*Why this is allowed under the design principles*: *"A class can implement one or more roles. If it implements multiple roles, they should be related."* Storing a session and storing that session's contest frame are the same responsibility — persisting the state of one game — keyed by the same id.

*Why registration goes through a concrete singleton*: registering the class twice would produce **two instances**, which for the in-memory store means two dictionaries and a frame that the session store cannot see. The resolved-forwarding above guarantees one instance behind both ports. This is the single most likely implementation mistake in the whole feature.

### D5. Module names do not change

The modules stay `QuestWorlds.InMemorySessionStore` and `QuestWorlds.SqliteSessionStore`, even though they now store frames too.

*Why*: "session store" reads as *the store for a session's state*, and a contest frame is part of a session's state — it is keyed by session id and has no life outside one. Renaming to `QuestWorlds.InMemoryStore` would be marginally more accurate and would churn a name already settled in the requirements (C4) and in two accepted-or-proposed ADRs. Not worth it.

### Architecture Overview

```
   QuestWorlds.Session            QuestWorlds.Framing
     IAmASessionStore  ◄──┐     ┌──►  IAmAContestFrameStore
     Ce = 0               │     │     Ce = 0
                          │     │
                    ┌─────┴─────┴───────────────────────┐
                    │  one class, both ports            │
                    │                                   │
                    │  InMemorySessionStore             │
                    │  SqliteSessionStore               │
                    │     ↳ one transaction covers      │
                    │       the session and its frame   │
                    └───────────────────────────────────┘
                                    ▲
                                    │ registers, and chooses which
                          ┌─────────┴──────────┐
                          │   QuestWorlds.Web  │
                          │   ContestHub       │
                          └────────────────────┘

   Two modules with Ce 0 each declare a port. One adapter satisfies both.
   Neither Session nor Framing knows the other exists.
```

Coupling, updated from [ADR-0008](0008-session-store-module-composition.md):

| Module | Ce | Ca | I | change |
|---|---|---|---|---|
| Session | 0 | 3 | 0.00 | — |
| Framing | 0 | **5** | 0.00 | Ca 3 → 5: both stores now reference it |
| InMemorySessionStore | **2** | 1 | 0.67 | Ce 1 → 2: Session and Framing |
| SqliteSessionStore | **2** | 1 | 0.67 | Ce 1 → 2: Session and Framing |
| Web | 7 | 0 | 1.00 | unchanged — it already referenced Framing |

`Web`'s Ce does not move: it already referenced `QuestWorlds.Framing`, and it loses a type (`InMemoryContestFrameStore`) rather than gaining a reference. **Both modules that declare ports stay at Ce 0**, and `Framing` becomes the most depended-upon module in the solution without depending on anything — which is the shape the talk is arguing for.

### Key Components

**`IAmAContestFrameStore`** (Role: Information Holder)

- **Knowing**: which session has a frame in progress, and what is in it
- **Doing**: save, get, clear
- **Deciding**: nothing

**`ContestFrame`** (Role: Information Holder) — *responsibility added here*

- **Knowing**: prize, resistance, the player's ability and rating, modifiers
- **Doing**: accept an ability and modifiers; **describe itself fully enough to be reconstructed**
- **Deciding**: whether a modifier is valid, and whether it is ready for resolution

**`SqliteSessionStore` / `InMemorySessionStore`** (Role: Information Holder, two ports)

- **Knowing**: sessions and their frames
- **Doing**: saving and loading both, consistently
- **Deciding**: nothing

### Implementation Approach

| # | Change | Kind |
|---|---|---|
| 1 | Add `ContestFrame.Rehydrate` | Structural |
| 2 | Move the port to `QuestWorlds.Framing` as `IAmAContestFrameStore`; make it async; delete the Web copies | Structural |
| 3 | Point `ContestHub` at the new port and `await` it | Structural |
| 4 | Implement the frame port on both stores; register one instance behind both ports | Structural |
| 5 | Extend the contract suite to cover the frame port and session/frame consistency | New tests |

All structural. No behavioural change beyond the write-back fix already recorded in ADR-0007.

## Consequences

### Positive

- **A restart no longer restores half a contest.** The hole ADR-0008 D4 opened is closed, rather than documented.
- **`QuestWorlds.Web` loses a responsibility it should never have had.** Storage was sitting in the composition root because there was nowhere else to put it; now there is.
- **Two Ce-0 modules each own a port.** `Session` and `Framing` both declare what they need and implement none of it — the same shape, arrived at twice, which is a better argument than one example.
- **One transaction covers a game's state.** Consistency is a property of the adapter, not a thing callers must coordinate.
- **The in-memory and SQLite stores stay interchangeable**, because both ports are on both stores and the contract suite covers both.

### Negative

- **Scope grew again**, and this time mid-design: the frame store was out of scope in an approved requirements document.
- **A second rehydration factory.** `ContestFrame` joins `Session` in exposing a constructor that exists for adapters. The pattern is consistent, but it is now a pattern rather than an exception.
- **One class, two ports** is a legitimate thing to dislike. It is defended above, but a reviewer who wants one class per role will find it here.
- **The registration is subtle.** Anyone who registers the class twice gets two instances and a silent inconsistency, which is why D4 spells it out.
- **`Framing`'s Ca rises to 5**, making it the most depended-upon module in the solution. That is the intent for a stable module, but it makes its public surface correspondingly expensive to change.

### Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Two instances registered, so the frame store and session store disagree | D4's forwarding registration, plus a contract test asserting that a frame saved through one port is visible via a store resolved from the other |
| A session is saved and its frame is not, leaving the same inconsistency inside the database | One transaction per save in the SQLite store; a contract test that restores both after a simulated restart |
| `ContestFrame` gains a field that the mapper does not persist | Round-trip equality test over a fully populated frame, as for `Session` |
| `ClearFrameAsync` leaves orphaned rows when a session is removed | `ON DELETE CASCADE` from the session row, plus a test that removing a session removes its frame |
| Reviewers read `SessionStore` and do not expect frames in it | D5 explains the name; the README module table should say what each store holds |

## Alternatives Considered

### 1. Leave the frame store in `QuestWorlds.Web`, and document the limitation

Cheapest, and defensible if SQLite were test-only — which was true until ADR-0008 D4. Rejected: a supported configuration that restores a session into an unusable state is a defect, not a limitation, and "durable history, not a resumable game" understates it when the contest state is missing too.

### 2. Two separate store classes, one per port

One class per role, which is the tidier reading of the design principles. Rejected: nothing then guarantees that a session and its frame are saved together, and providing that guarantee across two classes means inventing a shared unit of work — much more machinery than the problem justifies.

### 3. Declare the frame port in `QuestWorlds.Session` beside `IAmASessionStore`

Both ports in one place, one module for stores to reference. Rejected: it would make `QuestWorlds.Session` reference `QuestWorlds.Framing` for the `ContestFrame` in its signatures, taking its Ce from 0 to 1 and contradicting FR4 — the single figure this whole feature is built to protect.

### 4. Fold the frame into `Session` and store one aggregate

Arguably the real domain model: a session has a current contest. Rejected as far too large for this spec — it merges two modules, changes `ISessionCoordinator`, and rewrites `ContestHub`. Worth its own ADR if the two keep needing to move together.

### 5. Keep the frame port synchronous while the session port is async

Less churn in `ContestHub`. Rejected: the SQLite store would then have to block, and two ports implemented by one class would disagree about their threading model for no reason.

## References

- Requirements: [specs/0002-persistent_sessions/requirements.md](../../specs/0002-persistent_sessions/requirements.md)
- Issue: [#2](https://github.com/iancooper/quesworlds-contest/issues/2)
- Related ADRs:
  - [0007-session-storage-port.md](0007-session-storage-port.md) — the session port this mirrors
  - [0008-session-store-module-composition.md](0008-session-store-module-composition.md) — D4 made SQLite a supported configuration, which is what exposed this
  - [0009-sqlite-session-store.md](0009-sqlite-session-store.md) — schema and mapping, extended for frames
  - [0003-framing-module.md](0003-framing-module.md) — the Framing module this port now belongs to
- Design principles: [.agent_instructions/design_principles.md](../../.agent_instructions/design_principles.md)
