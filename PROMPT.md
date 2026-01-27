# QuestWorlds Contest - Project Context

> **Purpose**: This file provides context for AI assistants (Claude) to resume work on this project.

## Project Status: Complete ✅

All 6 phases implemented with **132 tests passing**.

## Project Overview

A web application for running QuestWorlds simple contests online between a GM and players.

**Core Workflow**:

1. GM creates a session and shares the 6-character code with players
2. GM frames a contest (prize + resistance TN)
3. Player submits their ability and rating
4. GM applies modifiers (stretch, situational, augment, hindrance)
5. System rolls D20 for both sides, calculates successes, determines winner
6. Both see the outcome with suggested benefit/consequence modifier

## Quick Start

```bash
# Run the application
cd src/QuestWorlds.Web
dotnet run

# Run all tests
dotnet test
```

Then open https://localhost:5001 (or http://localhost:5000)

## Web Pages

| Page | Purpose |
|------|---------|
| `/` | Home page with role selection (GM / Player) |
| `/GM/Index` | Create session, get 6-character code |
| `/GM/Contest` | Frame contest, apply modifiers, resolve |
| `/Player/Join` | Enter session code and name |
| `/Player/Contest` | Submit ability, view outcome |

## Technology Stack

- **Framework**: ASP.NET Core 9 with Razor Pages
- **Real-time**: SignalR for session synchronization
- **Styling**: Bootstrap 5
- **Testing**: xUnit with SignalR integration tests

## Architecture

Five domain modules (assemblies) + web layer:

| Module | Public API | Responsibility |
|--------|------------|----------------|
| `QuestWorlds.Session` | `ISessionCoordinator`, `Session`, `Participant` | Session creation, joining, state |
| `QuestWorlds.Framing` | `ContestFrame`, `Rating`, `TargetNumber`, `Modifier` | Contest setup, abilities, modifiers |
| `QuestWorlds.DiceRoller` | `IDiceRoller` | Random D20 generation |
| `QuestWorlds.Resolution` | `IContestResolver`, `ResolutionResult`, `DiceRolls` | Success calculation (deterministic) |
| `QuestWorlds.Outcome` | `IOutcomeInterpreter`, `ContestOutcome` | Result interpretation |

## Test Coverage

| Module | Tests |
|--------|-------|
| Framing | 65 |
| Session | 20 |
| Outcome | 20 |
| Resolution | 15 |
| Web (integration) | 9 |
| DiceRoller | 3 |
| **Total** | **132** |

## Key Files

| File | Purpose |
|------|---------|
| `specs/0001-questworlds-contest/requirements.md` | User requirements |
| `specs/0001-questworlds-contest/tasks.md` | Implementation tasks |
| `docs/adr/*.md` | Architecture Decision Records (6 ADRs) |
| `src/QuestWorlds.Web/Hubs/ContestHub.cs` | SignalR hub for real-time |
| `src/QuestWorlds.Web/Pages/GM/*.cshtml` | GM interface |
| `src/QuestWorlds.Web/Pages/Player/*.cshtml` | Player interface |

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

**Benefit/Consequence Modifiers**:
- Degree 0: ±5
- Degree 1: ±10
- Degree 2: ±15
- Degree 3+: ±20

## Key Design Decisions

- **No `InternalsVisibleTo`** - test only public interfaces
- **Deterministic Resolution** - `DiceRolls` passed to `Resolve(frame, rolls)`
- **Separate DiceRoller** - web layer orchestrates: roll → resolve → interpret
- **Session IDs** - 6-char codes using `ABCDEFGHJKMNPQRSTUVWXYZ23456789` (no ambiguous chars)
- **SignalR Groups** - session-scoped broadcasting
- **Safe DOM** - no innerHTML, prevents XSS

## Branch

Working branch: `feature/questworlds-contest`

---

## Implementation Summary

### Phase 1: Framing Module ✅
Rating, Modifier, TargetNumber value objects; ContestFrame aggregate

### Phase 2: Resolution Module ✅
Success calculator, winner decider, contest resolver (deterministic)

### Phase 3: Outcome Module ✅
Benefit/consequence lookup, contest outcome with full context

### Phase 4: Session Module ✅
Session coordinator, participant management, state transitions

### Phase 5: Web Integration ✅
DiceRoller, DI registration, SignalR ContestHub, GM/Player Razor Pages

### Phase 6: End-to-End Testing ✅
SignalR integration tests for complete contest workflow

---

## Future Enhancements (Not Implemented)

- Multiple players per session
- Contest history/logging
- Persistent sessions (database)
- Authentication
- Mobile app
