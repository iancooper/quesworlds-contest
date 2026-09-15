# 0007. Session Storage Port

Date: 2026-09-13

## Status

Accepted

## Context

**Parent Requirement**: [specs/0002-persistent_sessions/requirements.md](../../specs/0002-persistent_sessions/requirements.md)

**Scope**: This ADR decides **the storage port itself** — whether it is exported, what it is called, what shape its methods take, and who is responsible for keeping stored state current. It does **not** decide how the two store modules are laid out or registered, nor anything about SQLite; those are separate decisions and belong in their own ADRs. See *Deferred to later ADRs* below.

This ADR partly supersedes [ADR-0002](0002-session-management.md), which recorded `ISessionRepository` as `internal` and rejected database-backed sessions as "overkill for MVP".

### The problem

`QuestWorlds.Session` hides its storage completely. `ISessionRepository` and `InMemorySessionRepository` are both `internal`, `AddSessionModule()` registers the pair, and `SessionModule.CreateCoordinator()` constructs the in-memory store directly. A host cannot substitute a store without editing the assembly.

Sessions surviving a restart is a functional requirement. A design that cannot express it is not a narrower design, it is an incomplete one.

### Forces

- **The store is another module, not a leaked internal.** `ISessionRepository` is the seam between two modules, which is what module interfaces are for. This is the argument that makes exporting it a completion rather than a widening.
- **Direction of dependency must not reverse.** `QuestWorlds.Session` declares the interface and references nothing; a store implements it and therefore references `Session`. Ce stays 0, Ca goes 1 → 3. The arrow points inward.
- **The contract becomes public.** Once other assemblies implement it, its shape — sync or async, what the methods are called, what they promise — is expensive to change. It is worth getting right now rather than after two implementations exist.
- **The current contract has a latent bug.** `SessionCoordinator.JoinSession` mutates a session and never stores the result. It works only because the in-memory store returns the same object its dictionary holds. Exporting the port as-is would publish a seam that does not work for the very case it exists to enable.
- **Out-of-assembly implementers have only the public surface.** Anything a store needs in order to reconstruct a `Session` must be reachable from outside `QuestWorlds.Session`.
- **This repository is a teaching example.** It supports the talk *"Modules in C#: Where Did We All Go Wrong."* Where the repository's own design principles and its existing code disagree, the principles should win, because the principles are the subject.

### Constraints

- `QuestWorlds.Session.csproj` must keep **zero** `ProjectReference` elements (FR4).
- `ISessionIdGenerator` stays internal (FR7). This ADR concerns storage only.
- `QuestWorlds.Web` must behave exactly as it does today (FR13).

## Decision

**Export storage as a single async port named `IAmASessionStore`, with an upsert-shaped contract, and make the coordinator responsible for storing the result of every change it makes.**

Six decisions, each with its reason.

### D1. The port is public; `QuestWorlds.Session` keeps no implementation

`QuestWorlds.Session` declares the interface and contains no class implementing it. The module's exports become `ISessionCoordinator` and `IAmASessionStore`.

*Why*: a module that cannot name its dependency on another module is incomplete. Hiding the seam does not make storage a secret; it makes it unchangeable.

### D2. The port is named `IAmASessionStore`

Per [design_principles.md](../../.agent_instructions/design_principles.md): *"If an interface describes a role that an implementor provides, use the naming convention `IAmA*`."* This interface is precisely that — a role an implementor provides — so the convention applies.

*Why not `ISessionRepository`*: "Repository" names a pattern, not a role, and the implementing modules are called `InMemorySessionStore` and `SqliteSessionStore`. Interface and implementations should agree.

> **Known inconsistency**: no existing interface in this repository follows the convention (`ISessionCoordinator`, `IContestFrameStore`, `ISessionIdGenerator`). This ADR applies it to the new port only. Renaming the others is a structural change with no behavioural content and should be done separately under the boy-scout rule, not smuggled into this feature.

### D3. The port is asynchronous, returning `Task`, and takes a `CancellationToken`

```csharp
Task<Session?> GetAsync(string sessionId, CancellationToken cancellationToken = default);
```

*Why async*: the port is public and crosses a module boundary, so its shape is a published contract. A store backed by a database is doing I/O, and a synchronous contract would force either blocking (`.Result`, deadlock-prone, and forbidden by AC14) or a breaking change later.

*Why `Task`, not `ValueTask`*: `ValueTask` saves an allocation when a call completes synchronously, which is exactly the in-memory store's case. But the saving is irrelevant at this volume — a handful of calls per contest — and `ValueTask` carries real consumption hazards (it may be awaited only once, and must not be stored or awaited concurrently). *Prefer simplicity*; *if the implementation is hard to explain, it's a bad idea*.

*Why a `CancellationToken`*: it is the standard shape for an async I/O contract, and adding it later changes every signature. SignalR supplies one via `Context.ConnectionAborted`. A defaulted parameter keeps it out of the way of callers who do not care.

### D4. `Add` and `Update` collapse into one `SaveAsync`

```csharp
Task SaveAsync(Session session, CancellationToken cancellationToken = default);
```

*Why*: `Add` versus `Update` forces the caller to know whether a session is already stored — a *deciding* responsibility it should not hold, and the source of the FR8 bug. An upsert removes the decision. *There should be one — and preferably only one — obvious way to do it.*

### D5. The coordinator stores the result of every change it makes

`ISessionCoordinator` calls `SaveAsync` after any operation that alters a session. It may not assume that a `Session` handed back by `GetAsync` is a live reference into the store.

*Why*: the coordinator's role is Coordinator — it decides what happens to a session, so persisting that outcome is its responsibility, not the store's. The store stays a pure information holder with no knowledge of when it is being told about a change.

*Why it needs enforcing, not just stating*: an in-memory store makes the mistake invisible. The contract test suite therefore includes a store whose `GetAsync` returns a **copy**, which is how any out-of-process store behaves. Under that store, today's `JoinSession` loses the player.

### Architecture Overview

```
┌──────────────────────────────────────────────────────────────┐
│                      QuestWorlds.Web                         │
│                  ContestHub  (awaits the coordinator)        │
└───────────────────────────┬──────────────────────────────────┘
                            │ ISessionCoordinator
                            ▼
┌──────────────────────────────────────────────────────────────┐
│   QuestWorlds.Session          Ce = 0   (references nothing) │
│                                                              │
│   ISessionCoordinator  ──────►  IAmASessionStore             │
│   (Coordinator)                 (Information Holder)         │
│          │                              ▲                    │
│          │                              │  declared here,    │
│   SessionCoordinator  (internal)        │  implemented       │
│   ISessionIdGenerator (internal)        │  elsewhere         │
│   Session, Participant  (public)        │                    │
└─────────────────────────────────────────┼────────────────────┘
                                          │ implements
                    ┌─────────────────────┴─────────────────────┐
                    │                                           │
    ┌───────────────┴──────────────┐      ┌─────────────────────┴────────┐
    │ QuestWorlds.InMemorySessionStore │  │ QuestWorlds.SqliteSessionStore │
    │ (returns live references)        │  │ (returns copies)               │
    └──────────────────────────────────┘  └────────────────────────────────┘

    The arrow into Session points inward. Session is not reaching for a database.
```

### Key Components

Following the responsibility-driven style of [ADR-0002](0002-session-management.md):

**`IAmASessionStore`** (Role: Information Holder)

- **Knowing**: which sessions exist, and the state of each
- **Doing**: save, retrieve, and remove sessions on request
- **Deciding**: nothing — it is told what to store, and never infers whether a save is a create or an update

**`ISessionCoordinator`** (Role: Coordinator) — *responsibility added by this ADR*

- **Knowing**: nothing (still stateless)
- **Doing**: coordinate session creation, joining, and state transitions — **and store the result of each** (D5)
- **Deciding**: whether a join request is valid; **when a change is complete and must be stored**

**`Session`** (Role: Information Holder) — *responsibility added by this ADR*

- **Knowing**: id, GM, players, state
- **Doing**: add players, transition state, **and describe itself fully enough for a store to reconstruct it** (D6 below)
- **Deciding**: whether a participant may be added as a player

### D6. `Session` gains an explicit rehydration factory

Assumption A1 in the requirements was that a store could rebuild a `Session` through the existing public surface — `new Session(id, gm)`, then `AddPlayer` per player, then `TransitionTo(state)`. **That is technically true but is rejected**, because it makes a store replay domain behaviour in order to restore state: `AddPlayer` enforces the rule that only players may be added, so loading a row from a database re-runs a business rule that was already satisfied when the player joined. Validation belongs at the point of decision, not the point of loading.

Instead:

```csharp
public static Session Rehydrate(
    string id,
    Participant gm,
    IEnumerable<Participant> players,
    SessionState state);
```

*Why a named factory rather than a constructor*: `new Session(id, gm)` means "a new session is starting"; rehydration means "this session already existed". Two different intentions deserve two different names. *Reveal intention; be explicit to support future readers.*

> **This amends a requirement.** The NFR "the public surface grows by exactly one interface" was written before this was examined. The surface grows by one interface **and one factory method on an existing public type**. The alternative — a store replaying domain rules to load a row — is worse. The requirement should be read as amended by this ADR.

### D7. State transitions go through the coordinator, which saves them

*Added after D1–D6 were accepted, during implementation of task 3.1.*

```csharp
Task TransitionSessionStateAsync(string sessionId, SessionState newState, CancellationToken cancellationToken = default);
```

`ContestHub` today calls `session.TransitionTo(...)` on the instance `GetSessionAsync` returned, at four sites — `FrameContest`, `SubmitAbility`, `ResolveContest` and `StartNewContest` — and never saves. That works only because the in-memory store hands back a live reference. Under D5's licence for `GetAsync` to return a copy, all four transition an object that is then discarded.

*Why this was not caught earlier*: the Key Components table above already assigns the coordinator "coordinate session creation, joining, **and state transitions** — and store the result of each". The contract published under *The resulting contract* did not, and neither did the requirements' functional list. The responsibility and the interface disagreed, and the interface was the one that got implemented.

*Why it matters more than a missing method*: `Complete_contest_workflow_tests` asserts only the `SessionStateChanged` events the hub pushes to its clients. Nothing reads state back out of storage, so shipping the copy without this change loses every transition **silently**, in the default configuration — the precise failure mode Phase 3 of the task list exists to prevent.

*Why not a `SaveSessionAsync(session)` on the coordinator instead*: it would let the hub mutate and then save, which puts "remember to save" back in the caller's hands. That is the defect D4 removed from `Add`/`Update`, reintroduced one layer up. A caller that forgets fails silently; a coordinator that owns the transition cannot forget.

The hub still reads the session first where it needs to distinguish "not found" from "transitioned", so its error messages are unchanged.

> **This amends the requirements again.** The public surface grows by one interface, `Session.Rehydrate`, **and one method on `ISessionCoordinator`**. Read alongside D6's note.

### The resulting contract

```csharp
namespace QuestWorlds.Session;

/// <summary>
/// Stores the sessions a deployment knows about. Implemented by a store module;
/// QuestWorlds.Session declares this port and implements none of it.
/// </summary>
public interface IAmASessionStore
{
    /// <summary>
    /// Stores <paramref name="session"/>, replacing any session already held
    /// under the same id. Implementations must not distinguish between a first
    /// save and a later one: the caller does not know which it is.
    /// </summary>
    Task SaveAsync(Session session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the session held under <paramref name="sessionId"/>, or null.
    /// The result may be a copy. Callers must not assume that mutating it
    /// changes what is stored; they must call <see cref="SaveAsync"/>.
    /// </summary>
    Task<Session?> GetAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the session held under <paramref name="sessionId"/>.
    /// Removing a session that is not held is not an error.
    /// </summary>
    Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default);
}
```

Three obligations on implementers, stated in the contract because they are not inferable from the signatures:

1. **`SaveAsync` is an upsert.** Never an error because a session does or does not already exist.
2. **`GetAsync` may return a copy.** This is what licenses D5 and is the whole point of the port.
3. **`RemoveAsync` is idempotent.** Removing what is not there succeeds.

A fourth obligation, **thread safety**, follows from SignalR: hub callbacks for one session can arrive concurrently, so an implementation must tolerate concurrent calls. What it must *not* be asked to do is arbitrate two concurrent writes to the same session — see Risks.

### Consequential change: `ISessionCoordinator` becomes async

D3 makes the coordinator async, because it calls the port and blocking is not an acceptable alternative:

```csharp
Task<Session> CreateSessionAsync(string gmName, string connectionId, CancellationToken ct = default);
Task<Session?> GetSessionAsync(string sessionId, CancellationToken ct = default);
Task JoinSessionAsync(string sessionId, string playerName, string connectionId, CancellationToken ct = default);
Task<IEnumerable<string>> GetParticipantConnectionIdsAsync(string sessionId, CancellationToken ct = default);
Task TransitionSessionStateAsync(string sessionId, SessionState newState, CancellationToken ct = default);  // D7
```

The cost is low and was checked against the code: all seven of `ContestHub`'s calls to the coordinator already sit inside `public async Task` methods, so this is `await` insertion, not restructuring.

### Implementation Approach

Structural and behavioural changes stay in separate commits, per Tidy First:

| # | Change | Kind |
|---|---|---|
| 1 | Rename `ISessionRepository` → `IAmASessionStore`; `Add`/`Update` → `SaveAsync`, etc.; still `internal`, still sync | Structural |
| 2 | Make the port async; `await` through `SessionCoordinator`, `ISessionCoordinator`, `ContestHub` | Structural |
| 3 | Add `Session.Rehydrate` | Structural |
| 4 | **Fix the write-back defect**: `JoinSession` and every other mutating path call `SaveAsync`; state transitions move behind `TransitionSessionStateAsync` (D7) | **Behavioural** |
| 5 | Make the port `public`; move the in-memory store out of the assembly | Structural |

Step 4 is the only behavioural change and is the only one with a failing test written first. Step 5 is last because the port's shape should be settled before it is published.

## Consequences

### Positive

- **A host can choose its store** without editing `QuestWorlds.Session` — the requirement that started this.
- **`Session` gets more stable, not less**: Ce stays 0, Ca goes 1 → 3. The module gains a substitutable dependency and its instability does not move, because the adapter is the unstable thing.
- **A real bug is fixed.** The write-back defect is invisible today and would have become a data-loss bug the moment a persistent store was introduced.
- **Test doubles replace a module, not a collaborator.** This is the account of test doubles the talk gives, and the code now matches it. Today a `Session` test cannot substitute storage at all.
- **The contract states its obligations.** Copy-vs-reference, upsert, idempotent removal — the things that actually differ between stores are written down rather than discovered.

### Negative

- **The public surface grows by more than the requirements anticipated**: one interface, plus `Session.Rehydrate` (D6), plus `ISessionCoordinator.TransitionSessionStateAsync` (D7).
- **`ISessionCoordinator` changes shape.** Async is contagious; `ContestHub` changes even though nothing about the web layer prompted this work. D7 changes it again: the hub no longer calls `Session.TransitionTo` itself.
- **`CreateCoordinator()` loses its no-argument form.** `Session` can no longer construct a store, so every caller — including `SessionCoordinatorBuilder` in the tests — must supply one.
- **The naming convention is now applied inconsistently.** `IAmASessionStore` sits beside `ISessionCoordinator` and `IContestFrameStore` until those are boy-scouted.
- **Async over an in-memory dictionary is ceremony.** `Task.FromResult` on every call buys nothing for the default store; it is paid for the store that does real I/O.

### Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| A future mutating path forgets to call `SaveAsync` — the same bug, again | A copy-returning store in the shared contract suite makes the omission fail a test rather than pass one |
| Two hub callbacks for one session interleave: both `GetAsync`, both mutate, the later `SaveAsync` wins and the earlier change is lost | Real today and not made worse by this ADR, but the port makes it visible. Out of scope; the contract explicitly does not ask stores to arbitrate. Needs its own decision — optimistic concurrency on the session — before a store is used across servers |
| `Task.FromResult` misused as `TaskCompletionSource` in the in-memory store | Nothing is being signalled; the contract test suite runs identically against both stores, so a wrong idiom shows up as a hang, not a silent pass |
| `Session.Rehydrate` used by application code as a back door around `AddPlayer`'s validation | Document it as a store-facing factory; its four-argument shape makes accidental use unlikely |
| `Session.TransitionTo` stays public, so a caller can still transition without saving — the D7 defect, reachable again | Out of scope here: narrowing it is a change to `Session`'s own surface. The contract suite reads state back through the store, so a coordinator path that stops saving fails a test |

## Alternatives Considered

### 1. Keep the port synchronous, let the SQLite store block internally

Smallest change: `ISessionCoordinator` and `ContestHub` stay untouched. Rejected because a store doing synchronous I/O under an async web stack is how thread-pool starvation happens, and because the port is public — changing it to async later breaks every implementer. The cost of async here is seven `await`s in methods that are already `async Task`.

### 2. Keep `Add` and `Update` as separate methods

Closer to the current code and to the vocabulary of ADR-0002. Rejected because it requires the caller to know whether a session is already stored, which is exactly the decision that produced the FR8 defect.

### 3. Make `Session` immutable, so mutation returns a new instance

`AddPlayer` would return a new `Session`, making it impossible to change a session without producing a value that has to be stored. This eliminates the write-back bug by construction rather than by test, which is genuinely better. Rejected **for now** as too large for this change: it alters `Session`'s public surface substantially and touches every call site in `ContestHub`. Worth its own ADR if the bug class recurs.

### 4. Have the store track changes, so no explicit save is needed

A unit-of-work or change-tracking design. Rejected: it makes every implementer responsible for identity mapping and dirty-tracking, which is a large obligation to impose on a port whose default implementation is a dictionary. *Do not add new types without necessity.*

### 5. Rehydrate through the existing public surface (`new` + `AddPlayer` + `TransitionTo`)

Adds nothing to `Session`, and satisfies the requirement's assumption A1 literally. Rejected: a store would replay a domain rule — "only players may be added" — in order to load a row that had already satisfied it. Validation belongs where the decision is made.

### 6. Name the port `ISessionStore` or keep `ISessionRepository`

`ISessionRepository` is what issue #2 and ADR-0002 both say, so keeping it would avoid a rename. Rejected because "Repository" names a pattern rather than a role, and because the repository's own design principles specify `IAmA*` for role interfaces. In a repository that exists to demonstrate design, its stated principles outrank both its habit and the issue's wording.

## Deferred to later ADRs

| Question | Why it is not decided here |
|---|---|
| Module layout, project names, registration at the composition root, where the shared contract-test suite lives | Independent of the port's shape; reversible without touching the contract |
| SQLite library, schema, `Session`-to-rows mapping, database file location, connection lifetime | Entirely behind the port; a different answer changes nothing in `QuestWorlds.Session` |
| What a reloaded session means, given that its `ConnectionId`s are stale after a restart (C6) | A question about reconnection semantics, not about storage |
| Whether `QuestWorlds.Web` should reference both stores and choose by configuration | A composition-root decision; costs Ce 6 → 7. **Resolved by [ADR-0008](0008-session-store-module-composition.md) D4: yes, by configuration, defaulting to in-memory** |
| Optimistic concurrency on a session | Pre-existing, surfaced by this ADR, wider than it |

## References

- Requirements: [specs/0002-persistent_sessions/requirements.md](../../specs/0002-persistent_sessions/requirements.md)
- Issue: [#2 — Session should expose storage as a driven port, not hide it entirely](https://github.com/iancooper/quesworlds-contest/issues/2)
- Related ADRs:
  - [0002-session-management.md](0002-session-management.md) — **partly superseded**: recorded `ISessionRepository` as internal and rejected database-backed sessions
  - [0001-user-interface-architecture.md](0001-user-interface-architecture.md) — overall architecture
  - (Planned) 0008 — session store module layout and composition
  - (Planned) 0009 — SQLite session store
- Design principles: [.agent_instructions/design_principles.md](../../.agent_instructions/design_principles.md)
- External references:
  - Parnas, [*On the Criteria To Be Used in Decomposing Systems into Modules*](https://dl.acm.org/doi/10.1145/361598.361623)
  - Wirfs-Brock & McKean, *Object Design: Roles, Responsibilities, and Collaborations*
  - Martin, [*Stable Dependencies / Stable Abstractions principles*](https://en.wikipedia.org/wiki/Package_principles)
  - [Understanding the Whys, Whats, and Whens of ValueTask](https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/)
