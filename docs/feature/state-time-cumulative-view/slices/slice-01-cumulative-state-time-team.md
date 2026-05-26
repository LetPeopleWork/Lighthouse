# Slice 01 — Cumulative Time-per-State chart for team scope (walking skeleton)

**Feature**: `state-time-cumulative-view` (Epic 4144 MVP-bundle, slice B3)
**Stories**: US-01
**Effort estimate**: 1 day
**Reference class**: similar in shape to sibling F slice-01 (`aging-pace-percentiles/slice-01-per-state-bands-team.md`) — new metrics endpoint + new chart widget, both per-state, both reading `WorkItemStateTransition`. Sibling F's slice 01 estimate informs this one.

## Goal (one sentence)

Ship the walking-skeleton end-to-end path: new team-scope `cumulativeStateTime` endpoint reading `WorkItemStateTransition` rows + `WorkItem.currentStateEnteredAt`, applied through the D5 full-duration arithmetic (D12 inclusion), surfaced as a new `CumulativeStateTimeChart` widget registered in the `flow-metrics` category on the team detail page, so a Delivery Lead can open `/teams/{teamId}` and read the cumulative-time-per-state bars at a glance for the first time.

## IN scope

- New endpoint `GET /api/teams/{teamId}/metrics/cumulativeStateTime?startDate&endDate`
- New service method `TeamMetricsService.GetCumulativeStateTimeForTeam(team, startDate, endDate)` implementing D5 (full-duration attribution with D12 inclusion) and D6 (completed vs ongoing segment split)
- New chart widget `CumulativeStateTimeChart.tsx` with stacked completed/ongoing bars in workflow order (D3) and tooltip per US-01 AC
- Adaptive display unit (D16): a `formatDuration` util choosing one unit per render (minutes → hours → days → weeks) from the largest bar, applied uniformly; axis/legend labelled with the chosen unit
- New entries in `widgetInfoMetadata.ts` (`stateTimeCumulative` description + status guidance, including the D5/D12 inclusion-rule explanation that replaces the withdrawn US-03 tooltip line) and `categoryMetadata.ts` (place in `flow-metrics`, size `large`, no `ownerFilter`)
- New `computeCumulativeStateTimeRag` function in `ragRules.ts` with 40% / 60% thresholds per AC (computed on the whole in-scope set, D18)
- Integration test for the endpoint asserting bar-height arithmetic on a known fixture
- Vitest test for the chart widget asserting bar geometry (segment heights, tooltip content) on a known data response
- Empty-state and zero-contributing-state behavior verified

## OUT scope (deferred to later slices)

- Portfolio-scope endpoint and parity (slice 02)
- In-chart item picker / subset filter (US-05 — slice 04)
- Per-item drill-down on bar click (US-04 — slice 03)
- Any cross-feature shared-aggregation work with sibling F (DESIGN-time decision, not slice scope)
- Any UI to drive the date range — uses the existing date-range selector unchanged

## Learning hypothesis

- **Disproves if it fails**: that the cumulative-time-per-state computation can be expressed purely from the four `WorkItemStateTransition` fields plus `WorkItem.currentStateEnteredAt` (D9). If during implementation we discover that an additional field is needed (e.g. blocked-state attribution, sync-time-vs-business-time disambiguation), this would force a coordination request to sibling 1's DELIVER — a costly outcome we want to surface immediately.
- **Confirms if it succeeds**: that D5's full-duration arithmetic is well-defined for the edge cases (item entering before window, item exiting after window, item still in state now, re-entries through a state) AND that the chart's bar-stacking visual + adaptive unit read correctly to a user reviewing the screenshot.

## Acceptance criteria

- AC items from US-01 in `feature-delta.md` apply unchanged.
- Integration test fixture: 5 work items with known (state, enter, exit) intervals + 2 in-flight items at known `currentStateEnteredAt`; assert exact bar totals AND exact completed/ongoing segment heights for each of 3 states.
- Frontend test asserts: chart renders 3 bars in workflow order, each bar has a base and a hatched segment with the height ratio matching the fixture data, tooltip contains the named fields with the expected values.
- Empty-state test: filter that matches zero items renders the empty-state message; no crash.
- Zero-contributing-state test: a workflow state with no items contributing renders a labeled placeholder with height 0.
- `pnpm build` clean; `dotnet build` zero warnings; SonarCloud quality gate passes on PR; mutation testing ≥80% kill rate on new code.

## Dependencies

- **HARD (pre-DELIVER)**: sibling 1 `time-in-state-and-staleness` slice 01 merged so `WorkItemStateTransition` exists in production with at least some accumulated rows.
- **HARD (pre-this-slice)**: `bug-5016-cache-thread-safety` merged (already in place 2026-05-17 per CLAUDE.md and sibling 1's pre-reqs).
- **SOFT**: sibling F (`aging-pace-percentiles`) DESIGN — D10 cross-coordination opportunity, not a blocker.

## Production data requirement

This slice must be exercised against real `WorkItemStateTransition` data, not synthetic fixtures only. Acceptance: dogfood the chart against the dev Lighthouse instance's own team (the team registered in seed data) once sibling 1 has been running long enough for transitions to accumulate. The Vitest fixtures cover the arithmetic correctness; the dogfood moment confirms the chart "reads" as intended end-to-end.

## Dogfood moment (same-day)

After the slice merges, take a screenshot of the chart in the dev environment for the dev-team's most-active team, paste into the slice's PR description with a one-line "what I noticed" caption. This is the first chance to validate D5 full-duration semantics + D6 stacked-segment visuals + the D16 adaptive unit against real data. If the screenshot does not clearly convey "Review is the constraint" (or the equivalent for that team's data), it is a signal that D3 ordering or D6 visual treatment needs revisiting before slice 02.

## Pre-slice SPIKE

NONE required. The reference class (sibling F slice 01) covers all the uncertainty about endpoint shape, chart-widget registration, and `ragRules` integration. The only new complexity is the segment-stacking arithmetic, which is well-specified in D5/D6 of the feature-delta.
