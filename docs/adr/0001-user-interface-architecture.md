# 0001. User Interface Architecture

Date: 2026-01-26

## Status

Proposed

## Context

**Parent Requirement**: [specs/0001-questworlds-contest/requirements.md](../../specs/0001-questworlds-contest/requirements.md)

**Scope**: This ADR focuses on the overall application architecture, technology choices, and module organization. Subsequent ADRs will detail the internal design of each module (Session, Framing, Resolution, Outcome).

We need to build a web application that allows GMs and players to run QuestWorlds simple contests online. The application must:

- Provide distinct views for GM and Player roles
- Support real-time communication between participants
- Be accessible via modern web browsers without installation
- Be mobile-friendly for players on phones/tablets

Key forces:

- The team has C# expertise
- We want a simple, maintainable architecture
- We need clear separation of concerns for testability
- Real-time updates are required for session state synchronization

## Decision

We will build the application using **ASP.NET Core with Razor Pages** for the user interface, organized into **four domain modules** (assemblies) that encapsulate the core functionality.

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    QuestWorlds.Web                              │
│                  (ASP.NET Core + Razor Pages)                   │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐             │
│  │  GM Views   │  │Player Views │  │ SignalR Hub │             │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘             │
└─────────┼────────────────┼────────────────┼─────────────────────┘
          │                │                │
          ▼                ▼                ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Domain Modules                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────┐│
│  │   Session   │  │   Framing   │  │ Resolution  │  │ Outcome ││
│  │             │  │             │  │             │  │         ││
│  │ - Create    │  │ - Prize     │  │ - Roll dice │  │ - Winner││
│  │ - Join      │  │ - Abilities │  │ - Successes │  │ - Degree││
│  │ - Manage    │  │ - Modifiers │  │ - Compare   │  │ - Conseq││
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────┘│
└─────────────────────────────────────────────────────────────────┘
```

### Module Responsibilities

Each module is a separate C# assembly with clear responsibilities:

#### 1. QuestWorlds.Session

**Role**: Session Coordinator
**Responsibilities**:

- **Knowing**: Current sessions, participants, session state
- **Doing**: Create sessions, join sessions, track participants
- **Deciding**: Session validity, participant authorization

#### 2. QuestWorlds.Framing

**Role**: Contest Setup Manager
**Responsibilities**:

- **Knowing**: Prize, resistance TN, player abilities, modifiers
- **Doing**: Accept ability input, apply modifiers, validate ratings
- **Deciding**: Whether framing is complete and ready for resolution

#### 3. QuestWorlds.Resolution

**Role**: Dice Resolver
**Responsibilities**:

- **Knowing**: Target numbers, mastery rules, success calculation rules
- **Doing**: Roll D20, calculate successes, compare results
- **Deciding**: Winner determination, tie-breaking

#### 4. QuestWorlds.Outcome

**Role**: Result Interpreter
**Responsibilities**:

- **Knowing**: Degree of victory/defeat rules, benefit/consequence table
- **Doing**: Calculate degree, look up consequences
- **Deciding**: Final outcome interpretation

### Project Structure

```
QuestWorlds.sln
├── src/
│   ├── QuestWorlds.Web/           # ASP.NET Core web application
│   │   ├── Pages/
│   │   │   ├── GM/                # GM-specific Razor Pages
│   │   │   │   ├── Index.cshtml   # GM dashboard/session creation
│   │   │   │   └── Contest.cshtml # Contest management view
│   │   │   └── Player/            # Player-specific Razor Pages
│   │   │       ├── Join.cshtml    # Session join page
│   │   │       └── Contest.cshtml # Player contest view
│   │   ├── Hubs/
│   │   │   └── ContestHub.cs      # SignalR hub for real-time updates
│   │   └── wwwroot/               # Static assets
│   ├── QuestWorlds.Session/       # Session management module
│   ├── QuestWorlds.Framing/       # Contest framing module
│   ├── QuestWorlds.Resolution/    # Dice resolution module
│   └── QuestWorlds.Outcome/       # Outcome determination module
└── tests/
    ├── QuestWorlds.Session.Tests/
    ├── QuestWorlds.Framing.Tests/
    ├── QuestWorlds.Resolution.Tests/
    └── QuestWorlds.Outcome.Tests/
```

### Technology Choices

| Component | Technology | Rationale |
|-----------|------------|-----------|
| Web Framework | ASP.NET Core 9 | Modern, cross-platform, team expertise |
| UI | Razor Pages | Server-rendered, simple for form-based workflows |
| Real-time | SignalR | Built-in ASP.NET Core support, WebSocket abstraction |
| Styling | Bootstrap 5 | Mobile-friendly, minimal CSS needed |
| Testing | xUnit + FakeItEasy | Standard .NET testing stack |

### Key Interfaces (Roles)

```csharp
// Session module
public interface ISessionCoordinator
{
    Session CreateSession();
    Session? GetSession(string sessionId);
    void JoinSession(string sessionId, Participant participant);
}

// Framing module
public interface IContestFramer
{
    ContestFrame FrameContest(string prize, TargetNumber resistanceTn);
    void SetPlayerAbility(ContestFrame frame, string abilityName, Rating rating);
    void ApplyModifier(ContestFrame frame, Modifier modifier);
    bool IsReadyForResolution(ContestFrame frame);
}

// Resolution module
public interface IDiceResolver
{
    ResolutionResult Resolve(ContestFrame frame);
}

// Outcome module
public interface IOutcomeInterpreter
{
    ContestOutcome Interpret(ResolutionResult result);
}
```

### Data Flow

1. **GM creates session** → Session module creates and returns session ID
2. **Player joins** → Session module validates and adds participant
3. **GM frames contest** → Framing module captures prize, resistance TN
4. **Player submits ability** → Framing module records ability and rating
5. **GM applies modifiers** → Framing module adjusts target numbers
6. **GM triggers resolution** → Resolution module rolls dice, calculates successes
7. **System determines outcome** → Outcome module interprets results
8. **Results broadcast** → SignalR pushes outcome to all participants

## Consequences

### Positive

- **Clear separation of concerns**: Each module has a single, well-defined responsibility
- **Testability**: Domain modules can be unit tested independently of the web layer
- **Maintainability**: Changes to one module don't ripple through others
- **Team parallelism**: Different team members can work on different modules
- **Reusability**: Domain modules could be used with different UI technologies

### Negative

- **More projects to manage**: Four domain assemblies plus web and tests
- **Cross-module coordination**: Need clear contracts between modules
- **Potential over-engineering**: For a simple app, this structure might be more than needed

### Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Module boundaries unclear | Start with clear interface definitions; refactor if needed |
| SignalR complexity | Use simple hub patterns; consider polling fallback |
| Mobile responsiveness | Use Bootstrap's responsive grid; test on real devices |

## Alternatives Considered

### 1. Single Assembly

All domain logic in one project. Rejected because:

- Harder to test in isolation
- Less clear boundaries between concerns
- More difficult to reason about

### 2. Blazor Server/WebAssembly

Rich client-side interactivity. Rejected because:

- More complex for a form-based workflow
- SignalR already provides needed real-time capability
- Razor Pages simpler for this use case

### 3. Vertical Slice Architecture

Feature-based organization. Considered but:

- Our features map naturally to workflow stages
- Module approach better fits the domain model
- Could migrate to slices later if needed

## References

- Requirements: [specs/0001-questworlds-contest/requirements.md](../../specs/0001-questworlds-contest/requirements.md)
- Related ADRs:
  - (Planned) 0002-session-management - Details of Session module
  - (Planned) 0003-framing-module - Details of Framing module
  - (Planned) 0004-resolution-module - Details of Resolution module
  - (Planned) 0005-outcome-module - Details of Outcome module
- External references:
  - [ASP.NET Core Razor Pages](https://docs.microsoft.com/aspnet/core/razor-pages/)
  - [SignalR documentation](https://docs.microsoft.com/aspnet/core/signalr/)
  - [Responsibility-Driven Design](https://www.wirfs-brock.com/Design.html)
