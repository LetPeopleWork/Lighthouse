# Slice 03 — Every Team and Portfolio is a unit on its level

Epic #6474 · invisible · `job_id: infrastructure-only` · ≤ 1 day · depends on: 02

`infrastructure_rationale`: D7 makes Teams and Portfolios one unit type on a level before #6455 adds one. This
slice gives each of them its place on a level without changing any id. Prepares
`job-forecaster-know-when-each-promise-level-lands`.

## Problem

Priya Raman will want a programme level above her Portfolios (#6455). Today Lighthouse has two unrelated kinds,
Team and Portfolio, each numbered on its own. A level cannot say "my
units" while they stay two kinds, and #6455 would need a third.

## Learning hypothesis

A unit identity with a level can sit beside today's tables (expand-only) while every id the API, clients, RBAC
scopes, reports and snapshots use stays the same. **Disproved if** keeping the ids needs a table rewrite (then
DESIGN picks another shape and records why).

## IN

- Each Team is a unit on level 1, each Portfolio a unit on level 2, created by the migration.
- Creating a Team or Portfolio creates its unit; deleting removes it. `UnitCreated` and `UnitDeleted` domain
  events are published by Organisation set-up (replacing `TeamDeleted` from a controller; Portfolio deletion's
  inline clean-up becomes a handler of `UnitDeleted`).
- `/teams/{id}` and `/portfolios/{id}` keep their ids.

## OUT

- Reading types from the level (04). Moving fields off Team or Portfolio (07, 10). Refresh (12).

## Domain examples

1. Team 3 "Payments Crew" and Portfolio 3 "Checkout Revamp" both exist; both are units, both still answer at
   `/teams/3` and `/portfolios/3`.
2. Tom creates Team "Search"; its unit appears on level 1.
3. Priya deletes Portfolio "Legacy Platform"; its unit goes, its members, reports and snapshots are cleaned up
   as today.

## UAT scenarios

### Scenario: Existing Teams and Portfolios keep their addresses

Given Team 3 "Payments Crew" and Portfolio 3 "Checkout Revamp"
When the install is upgraded
Then "Payments Crew" is a unit on level 1 and "Checkout Revamp" a unit on level 2
And both still open at their old addresses

### Scenario: A new Team lands on the leaf level

Given an upgraded install
When Tom creates Team "Search"
Then "Search" is a unit on level 1

### Scenario: Deleting a Portfolio cleans up as before

Given Portfolio "Legacy Platform" with two members and one report
When Priya deletes it
Then its unit, members and report are gone, as today

## Acceptance criteria

- [ ] After the upgrade every Team belongs to the Team level and every Portfolio to the Feature level.
- [ ] Every Team and Portfolio still opens at its old address, for the UI and for the CLI and MCP.
- [ ] A new Team or Portfolio lands on its level; a deleted one takes its members, reports and history with it.
- [ ] Slice 01 check green.

## Outcome KPI

OUT-1, OUT-4.

## Technical notes

- `UnitCreated`/`UnitDeleted` are published after commit. ADR-027: in-process dispatcher, no outbox. Awaited `Save()` in handlers (ci-learnings).
- FK and cascade behaviour tested on SQLite, not InMemory.
- Usage-data names `TeamCreated`/`PortfolioDeleted` stay as they are (platform lane).
