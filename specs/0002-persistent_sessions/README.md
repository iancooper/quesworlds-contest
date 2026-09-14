# Persistent Sessions

**Spec ID**: 0002
**Created**: 2026-09-13
**Status**: Tasks drafted — awaiting approval, then implementation

## Overview

Export session storage as a driven port (`IAmASessionStore`) so a host can choose its store without
editing `QuestWorlds.Session`. The store is another module: the in-memory implementation moves out of
`QuestWorlds.Session` into its own assembly, a SQLite store ships alongside it as a second
implementation, and the host picks one by configuration at its composition root. The contest frame's
port moves out of `QuestWorlds.Web` into `QuestWorlds.Framing`, and one store class implements both
ports so a session and its contest are restored together. Both ports are async; both stores return
copies. The id generator stays internal, and an unconfigured app still behaves exactly as today.

**Linked issue**: [#2](https://github.com/iancooper/quesworlds-contest/issues/2)

## Status Checklist

- [x] **Requirements** — `requirements.md` drafted (`/spec:requirements`)
- [x] **Requirements approved** — `.requirements-approved` (`/spec:approve requirements`)
- [x] **Design** — ADRs recorded in `docs/adr/`, listed in `.adr-list` (`/spec:design`)
- [x] **Design approved** — `.design-approved` (`/spec:approve design`)
- [x] **Tasks** — `tasks.md` drafted (`/spec:tasks`)
- [ ] **Tasks approved** — `.tasks-approved` (`/spec:approve tasks`)
- [ ] **Implementation** — TDD implementation complete (`/spec:implement`)

## Artifacts

| Artifact | Location | State |
|---|---|---|
| Requirements | `specs/0002-persistent_sessions/requirements.md` | Approved (v4) |
| Design (ADRs) | `docs/adr/0007`–`0010` (see `.adr-list`) | All four accepted |
| Tasks | `specs/0002-persistent_sessions/tasks.md` | Drafted — awaiting approval |
