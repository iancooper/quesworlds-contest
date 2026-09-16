# Contest Runner

This is a contest runner for use with Chaosium's [QuestWorlds RPG](https://github.com/ChaosiumInc/QuestWorlds).

It allows a GM to create a session and invite players to join.

In that session the GM can create contests, the system will run the contest, and return the results

## Web Pages

| Page | Purpose |
|------|---------|
| `/` | Home page with role selection (GM / Player) |
| `/GM/Index` | Create session, get 6-character code |
| `/GM/Contest` | Frame contest, apply modifiers, resolve |
| `/Player/Join` | Enter session code and name |
| `/Player/Contest` | Submit ability, view outcome |


## Purpose

The main purpose of this repository is to support a talk on modules in C# and not to provide a contest runner tool, however, if you find it valuable, please use it.

## Design

The structure here is intended to show the division of an application into modules. As with all presentations that support talks, the actual division here is a little granular for the complexity of the problem. However, as capabilities are added — persistence of sessions and contests has since arrived, and sequences, a more complex form of contest, have not — the division begins to be more useful. The code also becomes more complex.

| Module                 | Purpose                                                                       |
|------------------------|-------------------------------------------------------------------------------|
| `DiceRoller`           | Rolls a dice                                                                  |
| `Framing`              | An aggregate representing the contest                                         |
| `Outcome`              | Turns the result into an outcome                                              |
| `Resolution`           | Engine that runs the framed contest to produce the outcome                    |
| `Session`              | Creates the session that players join                                         |
| `InMemorySessionStore` | Keeps sessions **and their contest frames** in memory; lost on restart         |
| `SqliteSessionStore`   | Keeps sessions **and their contest frames** in a SQLite file; survives restart |
| `Web`                  | The web interface, powered by SignalR, and the composition root               |

Both stores hold contest frames as well as sessions, which the name does not say. A session without
its contest is not a restored game — it is a restored lobby — so one class implements both ports and
writes both through a single unit of work. See ADR-0010.

`Session` and `Framing` each own a storage port and reference no other module: `IAmASessionStore`
lives in `Session`, `IAmAContestFrameStore` in `Framing`. A port belongs to the module that owns the
type it stores, not to the module that consumes it, so both projects hold zero project references.
`Web` is the only module that knows both stores exist, because choosing one is a deployment decision
and the composition root is where deployment decisions belong.

## Configuration

Which store the application runs on is one setting. Changing nothing gets you the in-memory store,
exactly as before persistence existed.

| Setting | Values | Default |
|---------|--------|---------|
| `SessionStore:Provider` | `InMemory` or `Sqlite` | `InMemory` |
| `ConnectionStrings:SessionStore` | A SQLite connection string, e.g. `Data Source=questworlds-sessions.db` | *(required when the provider is `Sqlite`)* |

Anything else in `SessionStore:Provider` stops the application at startup, naming the value it did not
recognise and the two it accepts. It never falls back silently: a typo must not produce a server that
loses sessions on restart while looking configured not to.

A relative `Data Source` resolves against the process working directory, which for `dotnet run` is
`src/QuestWorlds.Web`. The database file is gitignored.

### A restored session is durable history, not yet a resumable game

With `Sqlite`, a session and its contest survive a restart — the state is whole, and can be read back
from the database by a new process. What does not survive is the **transport**. Stored `ConnectionId`s
belong to the process that died, so nothing can be pushed to those participants until they reconnect,
and the application currently has no way for them to do so: the GM page offers only *create session*,
and a player rejoining is recorded as a second participant rather than recognised as a returning one.

This is a known limitation, not a defect — reconnection is the next piece of work. See ADR-0009 D8.
