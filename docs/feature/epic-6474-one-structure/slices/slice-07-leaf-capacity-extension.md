# Slice 07 — Leaf capacity inputs move to Forecasting's extension

Epic #6474 · invisible · `job_id: infrastructure-only` · ≤ 1 day · depends on: 03

`infrastructure_rationale`: ADR-270 Decision 8 puts Feature WIP and the throughput window in Forecasting's own
extension keyed by unit id, so the unit holds only structure. Taken over from Epic 6264 S03c's schema step
(D20). Prepares `job-forecaster-know-when-each-promise-level-lands`.

## Problem

Tom Becker tunes Feature WIP and the throughput window of "Payments Crew", and Jonas Weber's forecasts depend on
them. A unit that is a Team carries forecasting inputs no other unit has. While they sit on the Team, "one unit type"
still has a Team-shaped special case, and #6455's level-3 units would inherit fields they must not have.

## Learning hypothesis

The capacity inputs move with an expand-only change and the settings payload stays identical. **Disproved if**
the move needs a breaking payload change (then the payload maps from the extension and says so).

## IN

- Feature WIP, automatically adjust Feature WIP, throughput window (days, fixed dates, start/end), forecast
  filter rules: one record per leaf unit, keyed by unit id, owned by Forecasting.
- Migration copies today's values; Team settings payloads unchanged.

## OUT

- Moving Forecasting's folder (6264 S03c keeps that). Refinement settings (stay).

## Domain examples

1. "Payments Crew": Feature WIP 2, auto-adjust on, throughput 90 days → same values after the move.
2. "Search": fixed dates 2026-06-01 to 2026-08-31 → same window, same manual forecast.
3. "Platform" with forecast filter rules excluding Bugs → same filtered throughput.

## UAT scenarios

### Scenario: A Team's forecasting inputs survive the move

Given "Payments Crew" has Feature WIP 2 and a 90-day window
When the install is upgraded
Then its settings show Feature WIP 2 and a 90-day window

### Scenario: A manual forecast gives the same answer

Given the pinned manual forecast for "Search" with fixed dates
When the install is upgraded
Then the manual forecast for "Search" equals the pinned one

### Scenario: Editing the inputs still works

Given an upgraded install
When Tom sets "Payments Crew" to Feature WIP 3
Then the next forecast uses 3

## Acceptance criteria

- [ ] Values copied for every Team; payloads unchanged.
- [ ] Edits land in the extension.
- [ ] Slice 01 check green.

## Outcome KPI

OUT-1, OUT-4.

## Technical notes

- Expand-only: old columns kept, no longer written, removed in a later release.
- No non-default CLR initialiser on new persisted properties (ci-learnings EF).
