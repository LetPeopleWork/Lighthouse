# Slice 02 — A team's ServiceNow work becomes flow metrics and a forecast

**Goal**: A flow coach points a Lighthouse team at a ServiceNow query and sees Throughput, a
request-to-resolution time and a working forecast — the epic's minimum shippable value ("we may start
with team only").

> **RE-PLANNED 2026-07-29 — the stop condition below fired.** Q4 measured `work_start` empty, and the
> only real started-timestamp source (`metric_instance`) is 403 for every read-only role. Per
> **ADR-117**: `ClosedDate` = `resolved_at` (fallback `closed_at`), `StartedDate` = `opened_at`
> (fallback `sys_created_on`). The resulting span is **request-to-resolution, not time-in-progress**,
> and must be labelled as such rather than shipped as an unqualified "Cycle Time". True time-in-Doing
> moves to slice 04 with its documented `itil` escalation cost. Throughput and the forecast are
> unaffected and work read-only.

**Stories**: US-02 (value).

## IN scope
- `GetWorkItemsForTeam` against the configured **ITSM `task`-derived** table + user-authored `sysparm_query`, with pagination.
- Field mapping → Lighthouse work item: id, title, type, state, created/started/closed — built for ITSM columns (D4), with started/closed per **ADR-117**. Note `closed_at` is EMPTY on Resolved (state 6); keying on it alone silently drops resolved-but-not-closed records from Throughput.
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

**Q4 half of that hypothesis is already answered, and the answer is the unwelcome one.** No defensible
started timestamp exists on the record. The plan was NOT reordered — instead ADR-117 keeps slice 02 on
a read-only account by measuring request-to-resolution from `opened_at`, and moves true time-in-Doing
to slice 04. What remains to be learned in this slice is the Q3 half: whether `sysparm_query` can
express a team boundary a flow coach would recognise.

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
