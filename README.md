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
