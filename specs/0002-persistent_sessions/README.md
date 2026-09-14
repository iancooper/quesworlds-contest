# Persistent Sessions

**Spec ID**: 0002
**Created**: 2026-09-13
**Status**: Design in progress — ADR-0007 (storage port) proposed; module layout and SQLite ADRs still to write

## Overview

Export session storage as a driven port (`ISessionRepository`) so a host can choose its store without
editing `QuestWorlds.Session`. The store is another module: the in-memory implementation moves out of
`QuestWorlds.Session` into its own assembly, a SQLite store ships alongside it as a second
implementation, and the host wires one at its composition root. The port is async. The id generator
stays internal. `QuestWorlds.Web` keeps registering the in-memory store, so the app is unchanged.

**Linked issue**: [#2](https://github.com/iancooper/quesworlds-contest/issues/2)

## Status Checklist

- [x] **Requirements** — `requirements.md` drafted (`/spec:requirements`)
- [x] **Requirements approved** — `.requirements-approved` (`/spec:approve requirements`)
- [ ] **Design** — ADRs recorded in `docs/adr/`, listed in `.adr-list` (`/spec:design`)
- [ ] **Design approved** — `.design-approved` (`/spec:approve design`)
- [ ] **Tasks** — `tasks.md` drafted (`/spec:tasks`)
- [ ] **Tasks approved** — `.tasks-approved` (`/spec:approve tasks`)
- [ ] **Implementation** — TDD implementation complete (`/spec:implement`)

## Artifacts

| Artifact | Location | State |
|---|---|---|
| Requirements | `specs/0002-persistent_sessions/requirements.md` | Approved (v3) |
| Design (ADRs) | `docs/adr/0007-session-storage-port.md` | Proposed — 0008/0009 still to write |
| Tasks | `specs/0002-persistent_sessions/tasks.md` | Not created |
