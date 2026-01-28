# QuestWorlds.Web

ASP.NET Core web application for running QuestWorlds simple contests online.

## Overview

This is the main web application that provides the user interface and real-time communication for QuestWorlds contest resolution. It uses Razor Pages for the UI and SignalR for real-time updates between GM and players.

## Architecture

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
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌────────┐│
│  │ Session  │ │ Framing  │ │DiceRoller│ │Resolution│ │ Outcome││
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘ └────────┘│
└─────────────────────────────────────────────────────────────────┘
```

## Project Structure

```
QuestWorlds.Web/
├── Hubs/
│   ├── ContestHub.cs         # SignalR hub for real-time communication
│   └── IContestHubClient.cs  # Client interface for type-safe hub calls
├── Pages/
│   ├── GM/
│   │   ├── Index.cshtml      # GM dashboard/session creation
│   │   └── Contest.cshtml    # Contest management view
│   └── Player/
│       ├── Join.cshtml       # Session join page
│       └── Contest.cshtml    # Player contest view
├── Services/
│   └── IContestFrameStore.cs # Stores contest frames during workflow
├── wwwroot/                  # Static assets (CSS, JS)
└── Program.cs                # Application entry point and DI configuration
```

## Key Components

### ContestHub (SignalR)

The `ContestHub` is the central coordinator for real-time contest operations:

| Method | Description |
|--------|-------------|
| `CreateSession(gmName)` | Creates a new session, returns session ID |
| `JoinSession(sessionId, playerName)` | Adds a player to a session |
| `FrameContest(sessionId, prize, resistanceTn)` | GM sets up a contest |
| `SubmitAbility(sessionId, abilityName, rating)` | Player submits their ability |
| `ApplyModifier(sessionId, type, value)` | GM applies a modifier |
| `ResolveContest(sessionId)` | Rolls dice and determines outcome |
| `StartNewContest(sessionId)` | Begins a new contest in the session |

### IContestHubClient

Defines callbacks that clients receive:

| Callback | Description |
|----------|-------------|
| `SessionCreated(sessionId)` | Session was created |
| `PlayerJoined(playerName)` | A player joined the session |
| `SessionStateChanged(state)` | Session state transitioned |
| `ContestFramed(prize, resistanceTn)` | Contest was framed |
| `AbilitySubmitted(abilityName, rating)` | Player submitted ability |
| `ModifierApplied(type, value)` | Modifier was applied |
| `ContestResolved(outcome)` | Contest was resolved |
| `Error(message)` | An error occurred |

### IContestFrameStore

Stores contest frames during the contest workflow:

```csharp
public interface IContestFrameStore
{
    void SetFrame(string sessionId, ContestFrame frame);
    ContestFrame? GetFrame(string sessionId);
    void ClearFrame(string sessionId);
}
```

## Data Flow

1. **GM creates session** → `ContestHub.CreateSession()` → Session module creates session
2. **Player joins** → `ContestHub.JoinSession()` → Session module adds participant
3. **GM frames contest** → `ContestHub.FrameContest()` → Framing module creates frame
4. **Player submits ability** → `ContestHub.SubmitAbility()` → Frame updated with ability
5. **GM applies modifiers** → `ContestHub.ApplyModifier()` → Modifiers added to frame
6. **GM triggers resolution** → `ContestHub.ResolveContest()`:
   - DiceRoller generates rolls
   - Resolution module calculates result
   - Outcome module interprets result
   - Result broadcast to all participants

## Configuration

The web application is configured in `Program.cs`:

```csharp
// Add domain modules
builder.Services.AddSessionModule();
builder.Services.AddDiceRollerModule();
builder.Services.AddResolutionModule();
builder.Services.AddOutcomeModule();

// Add web services
builder.Services.AddSingleton<IContestFrameStore, InMemoryContestFrameStore>();
builder.Services.AddSignalR();
```

## Technology Stack

| Component | Technology |
|-----------|------------|
| Web Framework | ASP.NET Core 9 |
| UI | Razor Pages |
| Real-time | SignalR |
| Styling | Bootstrap 5 |

## Related ADRs

- [ADR-0001: User Interface Architecture](../../docs/adr/0001-user-interface-architecture.md)
- [ADR-0002: Session Management](../../docs/adr/0002-session-management.md)
