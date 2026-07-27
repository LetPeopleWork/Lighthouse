# Slice 02 — A team's ServiceNow work becomes flow metrics and a forecast

**Goal**: A flow coach points a Lighthouse team at a ServiceNow query and sees Throughput, Cycle Time
and a working forecast — the epic's minimum shippable value ("we may start with team only").

**Stories**: US-02 (value).

## IN scope
- `GetWorkItemsForTeam` against the configured **ITSM `task`-derived** table + user-authored `sysparm_query`, with pagination.
- Field mapping → Lighthouse work item: id, title, type, state, created/started/closed — built for ITSM columns (D4).
- State values surfaced as **display labels, not raw numeric choices**, in the existing state-mapping UI (US-02 AC3).
- `ValidateTeamSettings` with actionable messages for a bad query or unresolvable table.
- Configurable table name as a connection/team option, defaulting to ITSM (D4) — an Agile 2.0 shop can point at `rm_story` without a code change.
- `SupportsTransitionHistory => false` (D6) and confirmation that every history-dependent widget shows its documented unsupported state.
- One walking-skeleton E2E: connect → team sync → Throughput renders.

## OUT of scope
- Portfolio / parents (slice 03). Real transition history (slice 04). Docs and demo script (slice 05).
- Blocked rules, wait states, named cycle times — all downstream of transition history.

## Learning hypothesis
**Disproves** "ITSM records map onto Lighthouse's team+query+state-mapping concepts without a bespoke
UX" **if** the query cannot express a team boundary (SPIKE Q3), or if no defensible *started*
timestamp exists on `task` (SPIKE Q4) — the latter would make cycle time depend on transition history
and reorder the whole slice plan by pulling slice 04 forward.
**Confirms** the epic's core value on the cheapest possible surface — and, given D4, confirms it for
the data model most ServiceNow prospects are assumed to be on.

## Acceptance criteria
See US-02 AC1–AC7 in `feature-delta.md`.

## Dependencies
- Slice 01 (a validated connection).
- **SPIKE Q2, Q3, Q4, Q7** — table model, query language, field sources, pagination/rate limits.

## Effort / reference class
≤1 day of implementation **only because the SPIKE has already made these calls by hand**. Without
that, this is a multi-day discovery exercise — stated openly in the DoR (item 6) rather than
estimated optimistically. Reference class: `LinearWorkTrackingConnector.GetWorkItemsForTeam` +
`FilterIssuesForStates` + `GetWithPagination`.

## Pre-slice SPIKE
**Mandatory** — Q2/Q3/Q4/Q7. If Q4 reveals no started date, **stop and re-plan**: cycle time then
depends on slice 04, and this brief's scope is wrong.

## Dogfood moment
Same day: seed the dev instance by hand (the script is slice 05), sync a team, confirm Throughput and
a "how many by date" forecast render, and confirm the history-dependent widgets degrade honestly
rather than showing an empty chart.
