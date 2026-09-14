# 0008. Session Store Module Composition

Date: 2026-09-14

## Status

Proposed

## Context

**Parent Requirement**: [specs/0002-persistent_sessions/requirements.md](../../specs/0002-persistent_sessions/requirements.md)

**Scope**: This ADR decides **how the store modules are laid out, registered, and tested as a set** — project structure, who registers what, what happens when a host registers nothing, and where the shared contract tests live. It takes [ADR-0007](0007-session-storage-port.md)'s port as given and says nothing about SQLite, which is [ADR-0009](0009-sqlite-session-store.md).

### The problem

ADR-0007 exports `IAmASessionStore` and removes every implementation of it from `QuestWorlds.Session`. That leaves questions it deliberately did not answer: where the implementations live, how a host picks one, what happens when a host picks none, and how two independent implementations are held to the same contract.

### Forces

- **`QuestWorlds.Session` must stay at Ce 0.** It declares the port and references nothing. Any composition scheme that makes `Session` reference a store is disqualified, however convenient.
- **Removing the default removes a safety net.** Today `AddSessionModule()` always yields a working system. After ADR-0007 it yields one only if the host also registers a store. A host that forgets must find out quickly and legibly.
- **Two implementations are only interchangeable if something checks.** "Both satisfy the same contract" is an assertion until one suite of tests runs against both. The obligations ADR-0007 wrote into the contract — upsert, copies, idempotent removal — are exactly the ones a single implementation will accidentally satisfy and a second will not.
- **The coupling figures are published, and the demo is the artefact.** Issue #2's table is talk material, and composition decides whether `Web`'s Ce stays 6 or becomes 7. But people reach this repository after the talk, without a speaker to narrate it, so a substitution they can *perform* is worth more than a figure that stays round.
- **Test projects are not modules.** The coupling table counts production modules. A test-support project that both store test projects share does not belong in it, and should not be allowed to blur what "module" means in a talk about modules.

## Decision

**Two store modules, each with its own test project, each registering itself; the host composes; one shared contract suite runs against both.**

### D1. Two store modules, named for their technology

| Project | Contains | References |
|---|---|---|
| `src/QuestWorlds.InMemorySessionStore` | `InMemorySessionStore` | `QuestWorlds.Session`, `QuestWorlds.Framing` |
| `src/QuestWorlds.SqliteSessionStore` | `SqliteSessionStore` | `QuestWorlds.Session`, `QuestWorlds.Framing`, `Microsoft.Data.Sqlite` |

Each holds a session **and its contest frame**, implementing one port from each of the two modules — see [ADR-0010](0010-contest-frame-storage-port.md), which also explains why the names do not change.

Each is a peer implementation of `IAmASessionStore`. Neither is a default, and neither is privileged in `QuestWorlds.Session`, which knows about neither.

*Why not one `QuestWorlds.SessionStore` module holding both*: it would force every host to take the SQLite dependency in order to use a dictionary. The whole point is that a deployment takes only the store it uses.

### D2. Each store module registers itself

A store module owns its own registration extension, because it is the only thing that knows what its implementation needs:

```csharp
// QuestWorlds.InMemorySessionStore
services.AddInMemorySessionStore();

// QuestWorlds.SqliteSessionStore
services.AddSqliteSessionStore(connectionString);
```

Both register their implementation as a **singleton**, forwarded to the same instance behind both storage ports — see [ADR-0010](0010-contest-frame-storage-port.md) D4, which is where registering it twice would quietly give you two stores. The in-memory store must be a singleton, because it *is* the state. The SQLite store may be, because it holds only a connection string (see ADR-0009).

`AddSessionModule()` registers `ISessionCoordinator` and the internal id generator, and **no store**.

*Why `AddSingleton`, not `TryAddSingleton`*: `TryAdd` silently keeps whatever was registered first, so a host that called two store extensions by mistake would get one of them with no indication which. An explicit `Add` means the last registration wins, which is the documented behaviour of the container and is at least predictable. Choosing a store is the host's job and should look like a choice.

### D3. A missing store fails at startup, not at first contest

`AddSessionModule()` without a store registration produces a container that cannot construct `SessionCoordinator`. By default that surfaces as an exception on the first SignalR call — late, and during a game.

`QuestWorlds.Web` therefore enables container validation so the failure happens when the application starts:

```csharp
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateOnBuild = true;
    options.ValidateScopes  = true;
});
```

The resulting message names the missing service: *Unable to resolve service for type 'QuestWorlds.Session.IAmASessionStore' while attempting to activate 'QuestWorlds.Session.SessionCoordinator'.*

*Why not a fluent builder that makes a store mandatory at compile time* — `AddSessionModule().WithStore(...)` — *which would be better still*: it adds a builder type and a second way to register the module, for a mistake that startup validation already catches on the first run. *Do not add new types without necessity*; *there should be one obvious way to do it*.

### D4. `QuestWorlds.Web` references both stores and chooses by configuration

```csharp
// Program.cs — the composition root, and the only place that knows both stores exist
builder.Services.AddSessionModule();
builder.Services.AddConfiguredSessionStore(builder.Configuration);
```

```csharp
// QuestWorlds.Web — internal; keeps Program.cs a list of registrations
internal static IServiceCollection AddConfiguredSessionStore(
    this IServiceCollection services, IConfiguration configuration)
{
    var provider = configuration.GetValue("SessionStore:Provider", "InMemory");

    return provider switch
    {
        "InMemory" => services.AddInMemorySessionStore(),
        "Sqlite"   => services.AddSqliteSessionStore(SqliteConnectionString(configuration)),
        _          => throw new InvalidOperationException(
                          $"Unknown SessionStore:Provider '{provider}'. Expected 'InMemory' or 'Sqlite'.")
    };
}
```

Behaviour follows three rules:

| Configuration | Result |
|---|---|
| absent | In-memory — today's behaviour, unchanged for anyone who does nothing |
| `SessionStore:Provider = "Sqlite"` | SQLite, using `ConnectionStrings:SessionStore` |
| anything else | Startup fails, naming the bad value and the valid ones |

*Why the unknown case throws rather than falling back to in-memory*: a typo in a setting must not silently produce a server that loses sessions on restart while appearing to be configured not to. Failing loudly at startup is the whole point of having the switch in one place.

*Why an internal extension in `QuestWorlds.Web` rather than inline in `Program.cs`*: it keeps the composition root a flat list of registrations, per *avoid more than one level of indentation in a method*. It is `internal` because choosing a store is this host's business and not a reusable abstraction — a shared "store chooser" would have to reference every store that exists, which is how a composition root leaks into a library.

*What it costs*: `QuestWorlds.Web` gains a project reference it does not use in its default configuration, taking its Ce from 6 to **7**. Issue #2's published coupling table says 6 and will need correcting.

*Why it is worth paying*: people arrive at this repository **after** the talk, without the speaker. A passing test asserts that the seam works; changing one setting, restarting, and finding the session still there demonstrates it. The demonstration is the artefact's purpose, and one honest project reference at the composition root — which is the one place in a system that is *supposed* to know about every implementation — is a fair price. A composition root's job is to be unstable so that nothing else has to be.

### D5. One contract suite, run against both stores

The obligations in ADR-0007 are shared, so the tests for them are written once:

```
tests/QuestWorlds.SessionStore.ContractTests/   (test-support library, not a module)
    SessionStoreContract.cs                     abstract; every obligation from ADR-0007 and ADR-0010

tests/QuestWorlds.InMemorySessionStore.Tests/
    When_an_in_memory_store_is_used_as_a_session_store.cs
        : SessionStoreContract                  inherits every contract test

tests/QuestWorlds.SqliteSessionStore.Tests/
    When_a_sqlite_store_is_used_as_a_session_store.cs
        : SessionStoreContract                  inherits every contract test
    ... plus what only SQLite has: reload across instances, schema creation
```

`SessionStoreContract` is an abstract xUnit class with one abstract member — `protected abstract TStore CreateStore()` returning something that implements both `IAmASessionStore` and `IAmAContestFrameStore`. xUnit discovers `[Fact]`s on the concrete subclass, so each store's test project runs the full suite under its own name. Covering both ports in one suite is what enforces ADR-0010's rule that a session and its frame stay consistent.

**`QuestWorlds.Session.Tests` references `QuestWorlds.InMemorySessionStore`** and uses the real store — for `SessionCoordinatorBuilder` (D6) and for the write-back tests. It needs no test double of its own, because D7 makes the real in-memory store return copies. The `CopyReturningSessionStore` an earlier draft of this ADR proposed is not built.

*Why a shared project rather than linked source files or one combined test project*: linked files duplicate the suite in two places at build time and make a failure ambiguous about which store failed. One combined project forces the SQLite dependency on anyone running the in-memory tests. A referenced abstract base keeps one copy of the rules and one clear answer to "which store broke".

*Why it is not in the coupling table*: it is a test-support library. It references `QuestWorlds.Session` and xUnit, and nothing in production references it. Counting it would inflate `Session`'s Ca with something that ships to nobody.

### D6. `SessionModule.CreateCoordinator` requires a store

```csharp
public static ISessionCoordinator CreateCoordinator(IAmASessionStore store);
```

The no-argument form is removed, as it can no longer construct a store. `SessionCoordinatorBuilder` in `QuestWorlds.Session.Tests` passes an in-memory store, which is the only change expected to existing tests.

### D7. The in-memory store returns copies, like every other store

`InMemorySessionStore.GetAsync` returns a **copy**, built with `Session.Rehydrate` (ADR-0007 D6). It does not hand back the object its dictionary holds.

*Why*: ADR-0007 D5 makes the coordinator responsible for saving what it changes, and a store that returns live references makes forgetting invisible. The default development configuration would then be the one configuration where the bug cannot be observed — so a missing `SaveAsync` would pass every test and every manual check, and fail only in the SQLite deployment. Making the in-memory store copy means **both** stores behave the same way, and the bug class is eliminated rather than tested for.

*What it costs*: one small allocation per `GetAsync` — a session is an id, a GM, a handful of players and an enum. Against that, the previous design had to introduce a `CopyReturningSessionStore` test double purely to make the behaviour observable. **That double is no longer needed and is not built**; the real store does the job. *Do not add new types without necessity.*

*A consequence worth stating*: with copies, `coordinator.GetSession(id)` followed by a mutation no longer changes anything unless the coordinator saves. That is the whole point, and it is why the fix in ADR-0007's implementation step 4 must land in the same change as this decision — otherwise `JoinSession` starts losing players in the default configuration, not just the SQLite one.

### Architecture Overview

```
                          composition root
        ┌────────────────────────────────────────────────────┐
        │                  QuestWorlds.Web                   │
        │   AddSessionModule()                               │
        │   AddConfiguredSessionStore(configuration)         │
        │        │                                           │
        │        └── SessionStore:Provider ──┬── "InMemory"  │
        │                                    └── "Sqlite"    │
        └────────┬───────────────────────────────┬───────────┘
                 │ references both               │
                 ▼                               ▼
   ┌──────────────────────────────────┐  ┌────────────────────────────────┐
   │ QuestWorlds.InMemorySessionStore │  │ QuestWorlds.SqliteSessionStore │
   └────────────────┬─────────────────┘  └───────────────┬────────────────┘
                    │        implements IAmASessionStore │
                    └───────────────┬────────────────────┘
                                    ▼
                     ┌─────────────────────────────┐
                     │ QuestWorlds.Session         │
                     │   ISessionCoordinator       │
                     │   IAmASessionStore          │
                     │   Ce = 0                    │
                     └─────────────────────────────┘

   Both stores depend on Session. Session depends on neither.
   Web depends on both, and is the only thing that does.
```

Resulting figures:

| Module | Ce | Ca | I | note |
|---|---|---|---|---|
| Session | 0 | 3 | 0.00 | Web + both stores |
| Framing | 0 | 5 | 0.00 | Ca 3 → 5: both stores now reference it too (ADR-0010) |
| InMemorySessionStore | 2 | 1 | 0.67 | Session and Framing |
| SqliteSessionStore | 2 | 1 | 0.67 | Session and Framing |
| Web | **7** | 0 | 1.00 | **was 6 in issue #2's table** |

Three corrections to the published table, all to be carried back to issue #2:

- `Session`'s Ca reaches **3**, not the 2 the issue predicted, because both stores reference it. The stability claim holds more strongly than advertised.
- `Web`'s Ce becomes **7**, not 6, because D4 references both stores. `Web` is the composition root; its instability is already 1.00 and cannot get worse. This is the module that is *supposed* to absorb knowledge of every implementation so that no other module has to.
- `Framing` joins `Session` as a **Ce-0 module that declares a port** (ADR-0010), and becomes the most depended-upon module in the solution at Ca 5. Two modules arriving at the same shape independently is a better argument than one.

### Key Components

**`InMemorySessionStore`** (Role: Information Holder, implementing both storage ports)

- **Knowing**: the sessions and frames held in `ConcurrentDictionary`s
- **Doing**: save, get, remove — **returning copies, never the stored instance** (D7)
- **Deciding**: nothing

**`SqliteSessionStore`** (Role: Information Holder, implementing both storage ports) — detail in ADR-0009

- **Knowing**: how to reach the database, and how a session and its frame map to rows
- **Doing**: save, get, remove
- **Deciding**: nothing

**`SessionStoreContract`** (Role: Structurer — test support)

- **Knowing**: every obligation ADR-0007 and ADR-0010 place on a store
- **Doing**: exercising those obligations against whatever store a subclass supplies
- **Deciding**: nothing

### Implementation Approach

| # | Change | Kind |
|---|---|---|
| 1 | Create the two store projects and two test projects; add all four to `QuestWorlds.slnx` | Structural |
| 2 | Move `InMemorySessionRepository` out of `QuestWorlds.Session` into its own module as `InMemorySessionStore`, public | Structural |
| 3 | Create `QuestWorlds.SessionStore.ContractTests`; write the contract suite; make the in-memory store pass it, returning copies (D7) | Structural + new tests |
| 4 | Remove the store registration from `AddSessionModule()`; add `AddInMemorySessionStore()`; add `AddConfiguredSessionStore` and the `SessionStore` settings to `QuestWorlds.Web`; enable `ValidateOnBuild` | Structural |
| 5 | Replace `CreateCoordinator()` with `CreateCoordinator(store)`; update `SessionCoordinatorBuilder` | Structural |

All structural. The behavioural change in this feature is the write-back fix in ADR-0007.

## Consequences

### Positive

- **A deployment takes only the store it uses.** Nobody pays for SQLite to use a dictionary.
- **"Interchangeable" is checked rather than asserted.** One suite, two stores, one place to add a rule when the contract grows.
- **The choice of store is visible at the composition root**, in one line, next to the other module registrations.
- **The substitution can be performed, not just read about.** Set one configuration value, restart, and the session is still there. For a repository people reach after the talk, a demonstration beats an assertion.
- **A forgotten store fails at startup** with a message naming the missing type; a mistyped one fails naming the bad value.
- **Both stores behave identically** (D7). The default development configuration is no longer the one place where a missing save is invisible, and a test double exists to be deleted rather than written.

### Negative

- **The solution gains four projects** — two modules and two test projects — for a feature whose production code is a few hundred lines.
- **Nothing forces a host to register a store at compile time.** D3 catches it at startup, not at build.
- **`Web`'s Ce goes from 6 to 7**, contradicting the coupling table published in issue #2. The table needs correcting, and anyone who has already screenshotted it has a stale figure.
- **The default path is now one indirection deeper.** Reading `Program.cs` no longer tells you which store is in use; you also have to read the configuration.
- **The in-memory store allocates on every read** (D7), where previously it handed back a reference. Immaterial at this size, but it is a real change to the cheapest store's cost.
- **Each store module now depends on two modules**, not one, since it implements a port from each (ADR-0010). Ce 1 → 2.
- **A new test-support project sits outside the module story** and needs explaining if the solution layout appears on a slide.

### Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| A host registers two stores and gets a surprising winner | Last registration wins, per D2, which is predictable; `ValidateOnBuild` does not catch it, so document the intent that exactly one store extension is called |
| The contract suite drifts into testing one store's internals | It may only use `IAmASessionStore` and `QuestWorlds.Session`'s public types; it has no reference to either store project |
| A contract obligation is added to the suite but only the in-memory store is run | Both test projects inherit the same base, so a new `[Fact]` appears in both automatically |
| D7 lands before ADR-0007's write-back fix, so `JoinSession` starts losing players in the default configuration | They are one change and must be sequenced together; the tasks list must not separate them |
| `ValidateOnBuild` slows startup or surfaces unrelated pre-existing registration problems | It runs once at startup; if it reveals other problems, those are real and worth fixing |
| A mistyped provider value silently yields in-memory, and sessions vanish on a restart that was meant to preserve them | D4 throws on any unrecognised value rather than falling back |
| A deployment sets `Sqlite` but no connection string | The connection string is read eagerly during registration and its absence fails startup, not the first save |

## Alternatives Considered

### 1. Keep an in-memory default inside `QuestWorlds.Session`, registered with `TryAddSingleton`

This was the original shape of issue #2 and of the first draft of the requirements: the module keeps a default, a host overrides it. Rejected upstream in the issue's revision — the store is another module, so `Session` holding one makes it incomplete in a different way. Recorded here because it is the obvious design and its rejection is the interesting part.

### 2. One `QuestWorlds.SessionStore` module containing both implementations

Fewer projects, one registration extension with a parameter. Rejected: every host would take the SQLite dependency regardless of the store it uses, which defeats the substitution the feature exists to provide.

### 3. `QuestWorlds.Web` registers the in-memory store only

The cheaper option, and the one this ADR originally took: `Web` references one store, its Ce stays at the 6 issue #2 publishes, and SQLite is proven by its test suite. Rejected because a passing test asserts the seam works where a working configuration switch demonstrates it, and this repository is read after the talk by people without a speaker to fill the gap. The figure it protects belongs to the composition root, which is the module whose instability is least interesting — `Web` is already at I = 1.00 either way.

### 4. One combined store test project

Simpler layout, no shared test-support project. Rejected: it forces the SQLite dependency on anyone running the in-memory tests, and it couples two modules' test suites into one build unit.

### 5. Duplicate the contract tests in each store's test project

No shared project at all. Rejected: *do not duplicate knowledge*. Two copies of the rules drift, and the copy that drifts is the one for the store nobody is currently working on.

## References

- Requirements: [specs/0002-persistent_sessions/requirements.md](../../specs/0002-persistent_sessions/requirements.md)
- Issue: [#2](https://github.com/iancooper/quesworlds-contest/issues/2)
- Related ADRs:
  - [0007-session-storage-port.md](0007-session-storage-port.md) — the port this composes around
  - [0009-sqlite-session-store.md](0009-sqlite-session-store.md) — what goes inside the SQLite module
  - [0002-session-management.md](0002-session-management.md) — partly superseded
- Design principles: [.agent_instructions/design_principles.md](../../.agent_instructions/design_principles.md)
- External references:
  - [Dependency injection guidelines — ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection)
  - [Shared test context and inheritance in xUnit](https://xunit.net/docs/shared-context)
