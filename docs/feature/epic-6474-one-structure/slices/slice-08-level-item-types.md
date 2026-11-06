# Slice 08 — Decide once what counts as a Work Item for the whole organisation

> **Amended 2026-11-06.** Item types moved to the connection's mapping. The last section of `../design/upstream-changes.md` wins over this file; apply it at DISTILL.

Epic #6474 · **visible** · `job_id: job-config-admin-say-item-types-once` (proposed) · ≤ 1 day · depends on: 04, 05

## Elevator Pitch

- **Before**: to start counting Jira "Task" as a Work Item, Priya edits twelve Teams one by one.
- **After**: she adds "Task" to the Team level once; removing "Bug" shows the four Teams that read it and asks
  first.
- **Decision enabled**: Priya decides what counts as a Work Item for the whole organisation, seeing who is
  affected before anything changes.

## Problem

Priya keeps the type lists of 12 Teams in step by hand. When she removes a type, nothing tells her which Teams'
throughput will drop.

## Learning hypothesis

A preview listing affected units is enough to make removal safe. **Disproved if** walk-through testers remove a
type without reading the preview (then removal needs typing the type name).

## IN

- In the level edit dialog: the item types as chips (reuse `WorkItemTypesComponent`); add and remove. System
  Admin; free tier allowed (types are free today).
- Adding a type ticks it for **no** existing unit (M2); the dialog says "N units could read it".
- Removing a type that units tick is allowed after the confirmation naming them, sketched in
  `feature-delta.md` (M3); those units stop reading it at their next refresh.
- This is the **only** place a type is added to a level (M4): System Admin, in Organisation.
- `LevelItemTypesChanged` published; Work Items refreshes the affected units.

## OUT

- A unit ticking types (09). Types of levels above (#6455).

## Domain examples

1. Priya adds "Task" to level 1; no Team's numbers move; the dialog says "12 Teams could read Task".
2. She removes "Bug": "4 Teams read Bug today: Payments Crew, Checkout Crew, Search, Platform." She confirms.
3. She removes "User Story", which no Team ticks: no confirmation needed.

## UAT scenarios

### Scenario: Adding a type changes nobody's numbers

Given 12 Teams on level 1
When Priya adds "Task" to level 1
Then no Team reads Task until it is ticked on that Team

### Scenario: Removing a type that Teams read asks first, naming them

Given 4 Teams tick "Bug"
When Priya removes "Bug" from level 1
Then she is shown the 4 Teams and that their throughput and forecasts will change
And nothing changes unless she confirms

### Scenario: The affected Teams stop reading the removed type

Given Priya confirmed removing "Bug"
When the affected Teams refresh
Then none of them reads Bug

### Scenario: Only System Admins change a level's types

Given Tom is Admin of "Payments Crew" only
When he opens the levels
Then he cannot edit level 1's types

## Acceptance criteria

- [ ] Add and remove on a level, System Admin only, free tier allowed.
- [ ] Adding ticks nothing and says how many units could read it; removing names the affected units and needs
  confirmation.
- [ ] Affected units refresh after a removal.

## Outcome KPI

OUT-2.

## Technical notes

- The preview reads the units' subsets from the server; loading and failure looks as in 05.
