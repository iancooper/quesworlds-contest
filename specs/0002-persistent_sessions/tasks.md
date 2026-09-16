# Implementation Tasks

**Spec**: 0002-persistent_sessions
**Requirements**: [requirements.md](requirements.md) — approved (v4)
**Design**: ADRs [0007](../../docs/adr/0007-session-storage-port.md), [0008](../../docs/adr/0008-session-store-module-composition.md), [0009](../../docs/adr/0009-sqlite-session-store.md), [0010](../../docs/adr/0010-contest-frame-storage-port.md) — all Accepted

---

## Read this before starting

**One behavioural change in the whole feature.** Almost everything here is structural — renames, moves, async conversion, new projects. Per Tidy First, structural and behavioural changes never share a commit, and structural goes first. Structural tasks are validated by *existing* tests continuing to pass; they get no new tests, because nothing new is being specified.

**The one sequencing trap — Phase 3.** ADR-0008 D7 makes stores return copies. ADR-0007 D5 makes the coordinator save what it changes. Ship copies before the save and `JoinSession` starts losing players **in the default configuration**, not just under SQLite. They are one change. Do not split Phase 3 across commits, and do not reorder around it.

**The likeliest bug in the feature** is registering a store class twice and getting two instances (ADR-0010 D4). Task 6.3 exists solely to catch it.

**Checkable at any point**: `QuestWorlds.Session.csproj` and `QuestWorlds.Framing.csproj` must each have **zero** `ProjectReference` elements. If either gains one, a wrong turn was taken.

**On `/test-first`**: every TEST + IMPLEMENT task below stops for approval after the test is written. Per `.agent_instructions/testing.md`, tests only exercise **exports** from an assembly — never internals, and never via `InternalsVisibleTo`.

---

## Phase 1 — Reshape the port in place (structural)

The port stays `internal` throughout this phase. Nothing about the module's public surface changes yet, so existing tests are the safety net.

- [x] **1.1 Rename the port and collapse its methods** *(structural)*
  - `ISessionRepository` → `IAmASessionStore` (ADR-0007 D2); keep it `internal` for now
  - `Add` and `Update` → one `Save` (ADR-0007 D4); `Get` and `Remove` unchanged
  - Rename `InMemorySessionRepository` → `InMemorySessionStore`; rename the file to match
  - Update `SessionCoordinator` and `SessionModule.CreateCoordinator`
  - **Validated by**: all four existing `QuestWorlds.Session.Tests` classes pass unchanged
  - **Commit**: structural only

- [x] **1.2 Make the port and the coordinator async** *(structural)*
  - Port: `SaveAsync`, `GetAsync`, `RemoveAsync`, each returning `Task` and taking `CancellationToken cancellationToken = default` (ADR-0007 D3)
  - `InMemorySessionStore` completes immediately with `Task.CompletedTask` / `Task.FromResult(...)` — **not** `TaskCompletionSource`; there is nothing to signal
  - `ISessionCoordinator` → `CreateSessionAsync`, `GetSessionAsync`, `JoinSessionAsync`, `GetParticipantConnectionIdsAsync`
  - `ContestHub`: `await` at all seven call sites; every one is already inside an `async Task` method
  - Existing tests become `async Task` and `await` the coordinator
  - **No blocking anywhere** — no `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` (AC14)
  - **Validated by**: existing tests pass; solution builds
  - **Commit**: structural only

---

## Phase 2 — Reconstruction (behaviour)

`Session` must be rebuildable from data alone before any store can return a copy or load a row.

- [x] **2.1 TEST + IMPLEMENT: a session can be reconstructed from its stored parts**
  - **USE COMMAND**: `/test-first when rehydrating a session should restore id gm players and state`
  - Test location: `tests/QuestWorlds.Session.Tests`
  - Test file: `When_rehydrating_a_session_should_restore_id_gm_players_and_state.cs`
  - Test should verify:
    - A session rebuilt from an id, a GM, two players and `SessionState.AwaitingPlayerAbility` has all four back
    - `Players` come back **in the order supplied** — a store reading rows must be able to preserve order
    - Rehydrating with an empty player list yields a session with no players, not a failure
    - Rehydration does **not** enforce `AddPlayer`'s rule: a participant list is accepted as given, because the rule was satisfied when the player joined (ADR-0007 D6)
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add `public static Session Rehydrate(string id, Participant gm, IEnumerable<Participant> players, SessionState state)` to `src/QuestWorlds.Session/Session.cs`
    - Populate `_players` directly rather than calling `AddPlayer`, so no domain rule is replayed
    - Leave the existing `new Session(id, gm)` constructor alone — it means "a session is starting", which is a different intention

---

## Phase 3 — Copies and write-back ⚠️ ONE COMMIT

> **Do not split this phase.** 3.1 without 3.2 makes `JoinSession` lose players in the default configuration. The point of doing them together is that 3.1 is what makes 3.2's absence *visible* — see ADR-0008 D7.

- [x] **3.1 TEST + IMPLEMENT: joining a session persists the player**
  - **USE COMMAND**: `/test-first when a player joins a session the store should hold the player`
  - Test location: `tests/QuestWorlds.Session.Tests`
  - Test file: `When_a_player_joins_a_session_should_be_persisted_to_the_store.cs`
  - Test should verify:
    - A player joins, then the session is **read back from the coordinator** and contains that player
    - A second player joins and both are present, in join order
    - A state transition made through the coordinator survives a read back (ADR-0007 **D7**)
    - This test **fails before the fix** — that is the point; confirm red before implementing
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Make `InMemorySessionStore.GetAsync` return `Session.Rehydrate(...)` — a **copy**, never the dictionary's instance (ADR-0008 D7)
    - Add the missing `await _store.SaveAsync(session, ct)` to `SessionCoordinator.JoinSessionAsync` (`SessionCoordinator.cs:31` today)
    - Add `TransitionSessionStateAsync(sessionId, newState, ct)` to `ISessionCoordinator`, which loads, transitions and saves (ADR-0007 D7); `ContestHub`'s four `session.TransitionTo(...)` calls go through it instead
    - Audit **every** coordinator path that mutates a session and save there too — the defect is a missing save, so a second missing save is the same bug
    - Existing tests that relied on a live reference will fail; that is the defect surfacing, and they should be corrected to read back through the coordinator
  - **Commit**: behavioural — on its own, separate from every structural commit

---

## Phase 4 — Extract the in-memory store into a module (structural)

- [x] **4.1 Export the port** *(structural)*
  - `IAmASessionStore` becomes `public` with XML documentation stating the three obligations from ADR-0007: `SaveAsync` is an upsert, `GetAsync` may return a copy, `RemoveAsync` is idempotent
  - `ISessionIdGenerator` and `SessionIdGenerator` stay `internal` (AC5)

- [x] **4.2 Create `QuestWorlds.InMemorySessionStore`** *(structural)*
  - New project under `src/`, added to `QuestWorlds.slnx`; references `QuestWorlds.Session` only
  - Move `InMemorySessionStore` into it and make it `public`
  - `QuestWorlds.Session` now contains **no** implementation of the port (AC1)
  - Verify `QuestWorlds.Session.csproj` still has zero `ProjectReference` elements (AC2)

- [x] **4.3 Move store selection to the composition root** *(structural)*
  - `AddSessionModule()` registers the coordinator and id generator, and **no store** (ADR-0008 D2)
  - Add `AddInMemorySessionStore()` to the new module
  - `SessionModule.CreateCoordinator(IAmASessionStore store)` replaces the no-argument form (ADR-0008 D6)
  - `QuestWorlds.Session.Tests` references `QuestWorlds.InMemorySessionStore`; `SessionCoordinatorBuilder` passes one. **This is the only change expected to existing tests** (AC6)
  - `QuestWorlds.Web` calls `AddInMemorySessionStore()` for now — the configuration switch arrives in Phase 8

- [x] **4.4 Fail fast when no store is registered** *(structural)*
  - Enable `ValidateOnBuild` and `ValidateScopes` in `QuestWorlds.Web`'s host (ADR-0008 D3)
  - Confirm by hand that removing the store registration fails at **startup**, naming `IAmASessionStore`, rather than on the first hub call

---

## Phase 5 — The store contract, as one suite (behaviour)

- [x] **5.1 Create the contract suite** *(structural scaffolding)*
  - New test-support project `tests/QuestWorlds.SessionStore.ContractTests`, added to `QuestWorlds.slnx`
  - References `QuestWorlds.Session` and xUnit only — **never** either store project (ADR-0008 D5)
  - `public abstract class SessionStoreContract` with `protected abstract IAmASessionStore CreateStore();`
  - New project `tests/QuestWorlds.InMemorySessionStore.Tests` with `When_an_in_memory_store_is_used_as_a_session_store : SessionStoreContract`

- [x] **5.2 TEST + IMPLEMENT: a store honours the session storage contract**
  - **USE COMMAND**: `/test-first when a store is used as a session store it should honour the storage contract`
  - Test location: `tests/QuestWorlds.SessionStore.ContractTests`
  - Test file: `SessionStoreContract.cs` — the one place in this spec where multiple cases share a file, because they are one contract and the shared abstract fixture is the point (`.agent_instructions/testing.md` permits this for shared complex set-up)
  - Test should verify:
    - `SaveAsync` for an id not yet held stores it; `GetAsync` returns it
    - `SaveAsync` for an id already held **replaces** it and does not throw — the upsert obligation
    - `GetAsync` for an unknown id returns `null`
    - `RemoveAsync` for an id not held **succeeds** — the idempotency obligation
    - `GetAsync` returns a **copy**: mutate what comes back, do not save, read again, and the stored session is unchanged (AC19)
    - A session with no players, and one with several, both round-trip with players in order
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Make `InMemorySessionStore` pass every case; the copy case should already pass from task 3.1
    - Any case that fails is a real gap in the in-memory store, not a reason to weaken the contract

---

## Phase 6 — The contest frame port (behaviour + structural)

> Closes the defect design review found: with SQLite selectable, a restart restored a session without its contest. See ADR-0010.

- [x] **6.1 TEST + IMPLEMENT: a contest frame can be reconstructed from its stored parts**
  - **USE COMMAND**: `/test-first when rehydrating a contest frame should restore prize resistance ability and modifiers`
  - Test location: `tests/QuestWorlds.Framing.Tests`
  - Test file: `When_rehydrating_a_contest_frame_should_restore_prize_resistance_ability_and_modifiers.cs`
  - Test should verify:
    - Prize, resistance, player ability name and rating, and modifiers all come back
    - Modifiers come back **in order**
    - A frame with **no** player ability yet — the state between framing and submission — rehydrates with nulls rather than failing
    - `ApplyModifier`'s validation is **not** re-run on load (ADR-0010 D3)
    - `IsReadyForResolution` and `GetPlayerTargetNumber()` give the same answers on a rehydrated frame as on the original
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add `public static ContestFrame Rehydrate(string prize, TargetNumber resistance, string? playerAbilityName, Rating? playerRating, IEnumerable<Modifier> modifiers)` to `src/QuestWorlds.Framing/ContestFrame.cs`
    - Populate backing fields directly; do not call `SetPlayerAbility` or `ApplyModifier`

- [x] **6.2 Move the frame port into `QuestWorlds.Framing`** *(structural)*
  - Declare `public interface IAmAContestFrameStore` in `QuestWorlds.Framing` with `SaveFrameAsync`, `GetFrameAsync`, `ClearFrameAsync` (ADR-0010 D1, D2)
  - Delete `QuestWorlds.Web.Services.IContestFrameStore` and `InMemoryContestFrameStore`
  - `ContestHub` depends on the new port and awaits it; all six call sites are already in `async Task` methods
  - `InMemorySessionStore` implements **both** ports
  - Verify `QuestWorlds.Framing.csproj` still has zero `ProjectReference` elements (AC15)

- [ ] **6.3 TEST + IMPLEMENT: one store instance serves both ports**
  - **USE COMMAND**: `/test-first when a store is registered it should serve both ports from one instance`
  - Test location: `tests/QuestWorlds.InMemorySessionStore.Tests`
  - Test file: `When_a_store_is_registered_should_serve_both_ports_from_one_instance.cs`
  - Test should verify:
    - Build a `ServiceCollection`, call `AddInMemorySessionStore()`, resolve `IAmASessionStore` **and** `IAmAContestFrameStore`
    - A frame saved through the frame port is visible to a store resolved through the session port
    - `ReferenceEquals` on the two resolved instances — this is the assertion that catches the double-registration mistake (AC16)
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Register the concrete class as a singleton, then forward **both** interfaces to it with `sp => sp.GetRequiredService<InMemorySessionStore>()` (ADR-0010 D4)
    - Registering `AddSingleton<IAmASessionStore, InMemorySessionStore>()` and `AddSingleton<IAmAContestFrameStore, InMemorySessionStore>()` separately produces two instances and fails this test — which is the point

- [ ] **6.4 TEST + IMPLEMENT: a store honours the frame storage contract**
  - **USE COMMAND**: `/test-first when a store is used as a frame store it should honour the frame contract`
  - Test location: `tests/QuestWorlds.SessionStore.ContractTests`
  - Test file: `SessionStoreContract.cs` — extend the existing suite; change `CreateStore()` to return a type implementing both ports
  - Test should verify:
    - Save, get, and clear a frame by session id; get after clear returns `null`
    - `ClearFrameAsync` for a session with no frame succeeds
    - A frame **and** its session round-trip together and are both readable afterwards
    - Removing a **session** removes its frame — no orphan (AC18)
    - Frames for two different sessions do not collide
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Make `InMemorySessionStore` pass; keep frames in a second `ConcurrentDictionary` keyed by session id
    - `RemoveAsync` must clear the frame as well as the session

- [ ] **6.5 TEST + IMPLEMENT: the contest a player answers survives being read back** ⚠️ ONE COMMIT
  - **USE COMMAND**: `/test-first when a player submits an ability the store should hold the ability`
  - **Added during 6.2, not in the approved plan.** `ContestHub.SubmitAbility` calls `frame.SetPlayerAbility(...)` and `ApplyModifier` calls `frame.ApplyModifier(...)`, and neither saves. It works only because the in-memory store returns the live instance. Under SQLite the ability and every modifier are dropped and `ResolveContest` can only ever answer *"Contest is not ready for resolution"* — a contest that can never be resolved. This is ADR-0008 D7's argument applied to frames, and Phase 3's defect a second time
  - **Do not split it.** The copy is what makes the missing save visible; ship the copy alone and the default configuration starts losing modifiers. Same rule, same reason, as Phase 3
  - Test location: `tests/QuestWorlds.Web.Tests` for the write-back, `tests/QuestWorlds.SessionStore.ContractTests` for the copy obligation
  - Test should verify:
    - A player submits an ability, and the frame **read back from the store** has it — the web test, which fails before the fix
    - A modifier applied through the hub is in the frame read back, and two modifiers are both there, in order
    - `GetFrameAsync` returns a **copy**: mutate what comes back, do not save, read again, and the stored frame is unchanged — the contract case, mirroring AC19 for sessions
    - A contest framed, answered, modified and resolved still reaches `ContestResolved` — the whole workflow, which is what silently breaks
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Make `InMemorySessionStore.GetFrameAsync` return `ContestFrame.Rehydrate(...)`, exactly as `GetAsync` returns `Session.Rehydrate(...)`
    - Add `await _frameStore.SaveFrameAsync(sessionId, frame, ct)` after the mutation in `SubmitAbility` and in `ApplyModifier`
    - Audit every hub path that mutates a frame, not just those two — the defect is a missing save, so a second missing save is the same bug
  - **Commit**: behavioural — on its own, and both halves together

---

## Phase 7 — The SQLite store

> `.agent_instructions/testing.md` accepts **test-after for I/O implementations**, so 7.2's mapping is written against the inherited contract suite rather than test-first. The behaviours unique to persistence, 7.3 and 7.4, are still test-first.

- [ ] **7.1 Create `QuestWorlds.SqliteSessionStore`** *(structural)*
  - New project under `src/`, added to `QuestWorlds.slnx`; references `QuestWorlds.Session`, `QuestWorlds.Framing`, `Microsoft.Data.Sqlite` (ADR-0009 D1)
  - Schema per ADR-0009 D2: `Sessions`, `Participants`, `ContestFrames`, `ContestModifiers`; enums stored as **names**, not ordinals (D3)
  - Initialiser as an `IHostedService` registered by `AddSqliteSessionStore(connectionString)`, plus a directly callable `InitialiseAsync` for tests (D6)
  - WAL mode; a connection per operation (D5)
  - Add `*.db`, `*.db-wal`, `*.db-shm` to `.gitignore`

- [ ] **7.2 Implement both ports over SQLite** *(test-after, driven by the contract suite)*
  - New project `tests/QuestWorlds.SqliteSessionStore.Tests` with `When_a_sqlite_store_is_used_as_a_session_store : SessionStoreContract`, each test owning a temp database file and deleting it on dispose (ADR-0009 D7)
  - Make every inherited case pass — the suite is the specification, already approved in 5.2 and 6.4
  - Save replaces the whole aggregate in **one transaction**: upsert the row, delete and reinsert children (D4)
  - Register one instance behind both ports, exactly as 6.3 requires of any store

- [ ] **7.3 TEST + IMPLEMENT: a session and its contest survive a restart**
  - **USE COMMAND**: `/test-first when a sqlite store is reopened should return the session and its contest frame`
  - Test location: `tests/QuestWorlds.SqliteSessionStore.Tests`
  - Test file: `When_a_sqlite_store_is_reopened_should_return_the_session_and_its_contest_frame.cs`
  - Test should verify:
    - Save a session with a GM and two players, and a frame with an ability and modifiers, through **one** store instance
    - Dispose it; construct a **new** store over the same database file — the path a restart takes
    - Both come back complete, with players and modifiers in order (AC17)
    - This is the criterion the in-memory store cannot satisfy, and the one the spec's title rests on
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Load via `Session.Rehydrate` and `ContestFrame.Rehydrate`, splitting participant rows by `Role`
    - Use the `Ordinal` columns to restore order
    - Fix whatever the test exposes in the mapper rather than relaxing the assertion

- [ ] **7.4 TEST + IMPLEMENT: a fully populated session round-trips without loss**
  - **USE COMMAND**: `/test-first when a fully populated session is stored and read it should be unchanged`
  - Test location: `tests/QuestWorlds.SqliteSessionStore.Tests`
  - Test file: `When_a_fully_populated_session_is_stored_and_read_should_be_unchanged.cs`
  - Test should verify:
    - Every field of `Session`, `Participant`, `ContestFrame`, `Modifier`, `Rating` and `TargetNumber` survives — including a `Rating` with masteries and a `TargetNumber` with a modifier
    - Each `SessionState` and each `ModifierType` value round-trips by **name**, so reordering an enum cannot silently reinterpret stored rows (ADR-0009 D3)
    - An unknown enum name on load fails loudly rather than defaulting
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - This is the guard against a field being added to `Session` or `ContestFrame` later and silently not persisted — assert whole-object equality, not field-by-field spot checks

---

## Phase 8 — Composition (behaviour)

- [ ] **8.1 TEST + IMPLEMENT: the host selects its store by configuration**
  - **USE COMMAND**: `/test-first when session store provider is configured should select the matching store`
  - Test location: `tests/QuestWorlds.Web.Tests`
  - Test file: `When_session_store_provider_is_configured_should_select_the_matching_store.cs`
  - Test should verify:
    - **No** configuration → `IAmASessionStore` resolves to the in-memory store, so an unconfigured app behaves exactly as today (AC7)
    - `SessionStore:Provider = "Sqlite"` with a connection string → resolves to the SQLite store (AC7a)
    - `SessionStore:Provider = "Postgres"` → startup throws, and the message names both the bad value and the valid ones (AC7b)
    - `"Sqlite"` with **no** connection string → fails at startup, not on the first save
  - **⛔ STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Add `internal static AddConfiguredSessionStore(this IServiceCollection, IConfiguration)` to `QuestWorlds.Web` (ADR-0008 D4)
    - `QuestWorlds.Web` references **both** store projects; call it from `Program.cs` in place of the direct registration from 4.3
    - Add `SessionStore:Provider` and `ConnectionStrings:SessionStore` to `appsettings.Development.json`, defaulting the provider to `InMemory` (ADR-0009 D7)
    - An unrecognised value throws — never a silent fallback to in-memory

- [ ] **8.2 Confirm the switch by hand** *(manual verification — the demo this feature exists for)*
  - Run with the default settings; play through a contest; restart; confirm the session is gone, as today
  - Set `SessionStore:Provider` to `Sqlite`; play through to mid-contest; restart the app
  - Confirm the session **and its frame** are still in the database and load
  - Confirm the known limitation directly: participants cannot be messaged until they reconnect, because their `ConnectionId`s are stale (ADR-0009 D8). This is expected, and is what the next spec is for

---

## Phase 9 — Finishing

- [ ] **9.1 Full solution green**
  - `dotnet build` and `dotnet test` across every project (AC20)
  - Grep for `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` in changed code — there should be none (AC14)
  - Confirm zero `ProjectReference` in both `QuestWorlds.Session.csproj` and `QuestWorlds.Framing.csproj` (AC2, AC15)

- [ ] **9.2 Documentation**
  - `README.md` module table: add both store modules, and say what each holds — reviewers will not expect frames in something called a session store (ADR-0010 D5)
  - Document `SessionStore:Provider` and its two values, **with** the reconnection limitation stated next to it

- [ ] **9.3 Close the loop**
  - Walk the 20 acceptance criteria in `requirements.md` and tick them off against real tests
  - Push `feature/2-session-storage-port` and open the PR
  - Close issue #2

---

## Dependencies

```
1.1 ─► 1.2 ─► 2.1 ─► [3.1 ⚠ one commit] ─► 4.1 ─► 4.2 ─► 4.3 ─► 4.4
                                                          │
                                        ┌─────────────────┘
                                        ▼
                                  5.1 ─► 5.2
                                        │
                     ┌──────────────────┘
                     ▼
              6.1 ─► 6.2 ─► 6.3 ─► 6.4 ─► [6.5 ⚠ one commit]
                                                │
                     ┌──────────────────────────┘
                     ▼
              7.1 ─► 7.2 ─► 7.3 ─► 7.4 ─► 8.1 ─► 8.2 ─► 9.x
```

- **2.1 gates 3.1** — copies need `Rehydrate`
- **3.1 gates everything after it** — the defect must be fixed before the port is published, or a broken seam ships
- **4.1 gates 4.2** — the store cannot leave the assembly until the port is public
- **5.2 and 6.4 gate 7.2** — the contract is the SQLite store's specification, so it must exist first
- **6.2 gates 7.1** — the SQLite store implements the frame port, so the port must exist
- **6.5 gates 7.2** — a SQLite store returning copies into a hub that does not save back is a contest that cannot be resolved
- **8.1 needs both stores** — it chooses between them

## Risks

| Risk | Where it bites | Mitigation |
|---|---|---|
| 3.1 split across commits | `JoinSession` loses players in the **default** configuration, not just SQLite | Phase 3 is one commit. This is the note at the top of this file for a reason |
| A store class registered twice | Two instances; a frame the session store cannot see. Silent, and only in composition | 6.3 asserts `ReferenceEquals`; every store must satisfy it |
| A coordinator path mutates without saving | The same defect, somewhere else | 3.1 audits every mutating path, not just `JoinSession` |
| A **hub** path mutates a **frame** without saving | The ability and modifiers vanish under SQLite; the contest can never resolve. Invisible while the in-memory store returns live references | 6.5, added during 6.2. One commit, copy and write-back together, and it audits every mutating hub path |
| A field added to `Session`/`ContestFrame` later, not persisted | Silent data loss under SQLite only | 7.4 asserts whole-object equality, so a new field fails rather than vanishes |
| Enum reordered later | Stored rows silently reinterpreted | Names not ordinals (ADR-0009 D3); 7.4 covers every value |
| Structural and behavioural mixed in a commit | Review cannot tell which change broke a test | Only 3.1, and the implement halves of the TEST + IMPLEMENT tasks, are behavioural |
| `Session` or `Framing` gains a `ProjectReference` | The central claim of the talk quietly becomes false | Checked in 4.2, 6.2 and 9.1 |
| Two concurrent hub callbacks interleave, one save lost | Pre-existing; the port makes it visible but does not fix it | **Explicitly not solved here.** Recorded in ADR-0007's risks; needs optimistic concurrency and its own spec |
| Stale `ConnectionId`s read as "sessions are resumable" | Someone enables SQLite and expects a seamless restart | 8.2 confirms the limitation deliberately; 9.2 documents it beside the setting |
