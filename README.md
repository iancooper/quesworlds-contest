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

The structure here is intended to show the division of an application into modules. As with all presentations that support talks, the actual division here is a little granular for the complexity of the problem. However, if we were to add persistance of contests, sequences (a more complec form of contest) the division would begin to more useful. The code would also be more complex.

| Module       | Purpose                                                    |
|--------------|------------------------------------------------------------|
| `DiceRoller` | Rolls a dice                                               |
| `Framing`    | An aggregate representing the contest                      |
| `Outcome`    | Turns the result into an outcome                           |
| `Resolution` | Engine that runs the framed contest to produce the outcome |
| `Session`    | Creates the session that players join                      |
| `Web`        | The web interface, powered by SignalR                      |
