# QuestWorlds.InMemorySessionStore

Keeps sessions in memory, for a deployment that does not need them to survive a restart.

## Overview

This is one of two implementations of `IAmASessionStore`, the port `QuestWorlds.Session` declares.
It is not a default and is not privileged: it is a peer of `QuestWorlds.SqliteSessionStore`, and a
host picks one. `QuestWorlds.Session` knows about neither.

```
   ┌──────────────────────────────────┐
   │ QuestWorlds.InMemorySessionStore │   Ce = 1
   └────────────────┬─────────────────┘
                    │ implements IAmASessionStore
                    ▼
   ┌──────────────────────────────────┐
   │ QuestWorlds.Session              │   Ce = 0
   └──────────────────────────────────┘
```

## Key Types

| Type | Description |
|------|-------------|
| `InMemorySessionStore` | Holds sessions in a `ConcurrentDictionary` |
| `ServiceCollectionExtensions` | `AddInMemorySessionStore()` |

## Usage

```csharp
services.AddSessionModule();
services.AddInMemorySessionStore();   // exactly one store registration
```

Without dependency injection:

```csharp
var coordinator = SessionModule.CreateCoordinator(new InMemorySessionStore());
```

## Design Decisions

- **Registered as a singleton**: the store *is* the state, so a second instance is a second set of
  sessions
- **`GetAsync` returns a copy**, built with `Session.Rehydrate`, exactly as a database-backed store
  must. A store that handed back its own instance would make the default development configuration
  the one place where a caller's missing `SaveAsync` is invisible (ADR-0008 D7)
- **Thread-safe**: hub callbacks for one session can arrive concurrently. It does not arbitrate two
  concurrent writes to the same session; that race is pre-existing and recorded in ADR-0007

## Limitations

- Sessions are lost when the process stops — the reason `QuestWorlds.SqliteSessionStore` exists
- Single server only: the dictionary is this process's

## Naming

The namespace and the type share a name, so an unqualified `InMemorySessionStore` in a file under
the `QuestWorlds` namespace binds to the namespace rather than the type. Consumers that name the
type need a `using` alias; consumers that only call `AddInMemorySessionStore()` do not.

## Related ADRs

- [ADR-0007: Session Storage Port](../../docs/adr/0007-session-storage-port.md)
- [ADR-0008: Session Store Module Composition](../../docs/adr/0008-session-store-module-composition.md)
