# QuestWorlds Contest - Project Context

> **Purpose**: This file provides context for AI assistants (Claude) to resume work on this project.

## Project Overview

A web application for running QuestWorlds simple contests online between a GM and players.

**Core Workflow**:

1. GM creates a session and shares the ID with players
2. GM frames a contest (prize + resistance TN)
3. Player submits their ability and rating
4. GM applies modifiers (stretch, situational, augment, hindrance)
5. System rolls D20 for both sides, calculates successes, determines winner
6. Both see the outcome with suggested benefit/consequence modifier

## Technology Stack

- **Framework**: ASP.NET Core 9 with Razor Pages
- **Real-time**: SignalR for session synchronization
- **Styling**: Bootstrap 5
- **Testing**: xUnit + FakeItEasy

## Architecture

Five domain modules (assemblies) + web layer:

| Module | Public API | Responsibility |
|--------|------------|----------------|
| `QuestWorlds.Session` | `ISessionCoordinator`, `Session`, `Participant` | Session creation, joining, state |
| `QuestWorlds.Framing` | `IContestFramer`, `ContestFrame`, `Rating`, `TargetNumber`, `Modifier` | Contest setup, abilities, modifiers |
| `QuestWorlds.DiceRoller` | `IDiceRoller` | Random D20 generation |
| `QuestWorlds.Resolution` | `IContestResolver`, `ResolutionResult`, `DiceRolls`, `ContestWinner` | Success calculation (purely deterministic) |
| `QuestWorlds.Outcome` | `IOutcomeInterpreter`, `ContestOutcome`, `DegreeDescription` | Result interpretation, benefit/consequence lookup |

**Key Design Principles**:
- Only public interfaces are tested. Implementations are internal.
- Resolution module is purely deterministic - web layer uses DiceRoller to generate rolls, then passes them to Resolution.

## Key Files

| File | Purpose |
|------|---------|
| `specs/0001-questworlds-contest/requirements.md` | User requirements and acceptance criteria |
| `specs/0001-questworlds-contest/tasks.md` | Implementation task list with TDD workflow |
| `docs/adr/0001-user-interface-architecture.md` | Overall architecture |
| `docs/adr/0002-session-management.md` | Session module design |
| `docs/adr/0003-framing-module.md` | Framing module design |
| `docs/adr/0004-resolution-module.md` | Resolution module design (deterministic) |
| `docs/adr/0005-outcome-module.md` | Outcome module design |
| `docs/adr/0006-diceroller-module.md` | DiceRoller module design (randomness) |

## QuestWorlds Rules Reference

**Success Calculation**:

- Roll < TN = 1 success
- Roll = TN = 2 successes (big success)
- Roll > TN = 0 successes
- Each mastery = +1 success

**Winner Determination**:

- More successes wins
- Tie: higher roll wins
- Degree = difference in successes

**Rating Notation**: `15` (base), `5M` (5 + 1 mastery), `6M2` (6 + 2 masteries)

## Commands

- `/spec:status` - Show specification status
- `/spec:review` - Review current phase
- `/spec:implement` - Begin implementation
- `/test-first [behavior]` - TDD workflow for a task

## Branch

Working branch: `feature/questworlds-contest`

---

## What's Next

<!-- Update this section manually when resuming work -->

**Current Phase**: Phase 3 - Outcome Module

**Next Task**: Outcome interpreter maps degree to description
- Run `/spec:implement` to continue TDD implementation
- Test: Degree 0 → Marginal, Degree 1 → Minor, Degree 2 → Major, Degree 3+ → Complete

**Completed**:
- [x] Requirements approved
- [x] All 5 ADRs accepted
- [x] Tasks approved (31 tasks)
- [x] Phase 0: Solution structure created (`QuestWorlds.slnx`)
- [x] **Phase 1: Framing Module** (12 tasks)
  - **Rating Value Object** (4 tasks)
    - Rating parses simple numeric notation (`"15"` → Base=15, Masteries=0)
    - Rating parses mastery notation with single mastery (`"5M"` → Base=5, Masteries=1)
    - Rating parses mastery notation with multiple masteries (`"6M2"` → Base=6, Masteries=2)
    - Rating rejects invalid base values (must be 1-20)
  - **Modifier Value Object** (2 tasks)
    - Modifier validates stretch must be negative
    - Modifier validates allowed values (±5 or ±10)
  - **TargetNumber Value Object** (2 tasks)
    - TargetNumber calculates effective base with modifier (clamped 1-20)
    - TargetNumber creates from Rating
  - **ContestFrame Aggregate** (4 tasks)
    - ContestFrame requires prize and resistance
    - ContestFrame tracks player ability
    - ContestFrame calculates player target number with modifiers
    - ContestFrame knows when ready for resolution
- [x] **Phase 2: Resolution Module** (8 tasks) ✅
  - **Success Calculator** (4 tasks) ✅
    - Returns 1 success when roll below TN
    - Returns 2 successes when roll equals TN (big success)
    - Returns 0 successes when roll above TN
    - Adds mastery successes to total
  - **Winner Decider** (2 tasks) ✅
    - Determines winner by success count
    - Breaks ties with higher roll
  - **Contest Resolver** (2 tasks) ✅
    - Orchestrates full resolution
    - Rejects incomplete frames

**Test Count**: 80 tests passing (65 Framing + 15 Resolution)

**Key Design Decisions**:
- No `InternalsVisibleTo` - test only public interfaces
- `DiceRolls` record passed to `Resolve(frame, rolls)` - Resolution is purely deterministic
- `IDiceRoller` in separate module - web layer orchestrates: roll dice, then resolve with known rolls
- All tests pass known `DiceRolls` values - no mocking required

**Blockers**: None
