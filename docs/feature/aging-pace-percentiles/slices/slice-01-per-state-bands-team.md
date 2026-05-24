# Slice 01: Per-State Age-in-State Percentile Bands — Team Scope

**Feature**: aging-pace-percentiles
**Stories shipped**: US-01 (team scope only — portfolio scope rolls into slice 02 alongside the legend / tooltip work)
**Estimate**: ~3 crafter days
**Reference class**: similar to extending `cycleTimePercentiles` (existing) to a sibling metric endpoint + extending an existing chart with a new optional prop series.

## Goal
Ship per-state age-at-state-exit percentile bands on the team-scope Work Item Aging chart,
computed from `WorkItemStateTransition` data (sibling feature) for completed items in the
team's history window. Validate the per-state percentile math is correct on a known fixture
and that the chart renders the new bands alongside the existing full-width CT bands without
regression.

## IN scope
- New backend endpoint: `GET /api/teams/{teamId}/metrics/ageInStatePercentiles?startDate&endDate`. Returns `[{ state, sampleSize, percentiles: [{ percentile, value }] }]`. Empty array for states with zero completed-item samples; included entry with computed values + correct `sampleSize` for states with ≥1 sample (no minimum-sample threshold — the low-sample warning is presentational only, lives in the legend tooltip from US-02 in slice 02 not here).
- Backend computation: for each completed item in `[startDate, endDate]`, walk its `WorkItemStateTransition` rows; for each `Doing`-category state the item passed through, compute `ageAtStateExit = exitTransition.timestamp - entryTransition.timestamp`. Bucket by state name. Compute 50/70/85/95 per bucket using the existing `PercentileCalculator`.
- Frontend: `WorkItemAgingChart` accepts new optional `perStatePercentileValues?: { state: string; percentiles: IPercentileValue[] }[]` prop. When present, render short dashed horizontal segments anchored above each state column at each percentile value, using the same `ChartsReferenceLine` mechanism currently used for full-width bands but with the segment's X range constrained to the column.
- Frontend service: `MetricsService.getAgeInStatePercentiles(teamId, startDate, endDate)`.
- `useMetricsData` hook: extend to fetch the new endpoint alongside `cycleTimePercentiles` and stash result in the ctx as `perStatePercentileValues`.
- `BaseMetricsView`: pass `perStatePercentileValues={ctx.perStatePercentileValues}` to the `<WorkItemAgingChart>` instance.

## OUT scope (deferred to slice 02 or to later features)
- Portfolio-scope endpoint and chart wiring → slice 02
- Legend chip group for `Age-in-State %iles` toggle (US-02) → slice 02
- In-flight item tooltip "above 85th percentile for state X" annotation (US-03) → slice 02
- Configurable percentiles per team → out of scope entirely (locked D4 — ADO #5076)
- Visual treatment differentiation (one chip group vs two) → minimal in slice 01; the bands just render with the existing legend behaviour. Slice 02 adds the dedicated chip group + independent toggle.

## Learning hypothesis
**Confirms if it succeeds**: per-state bands derived from `WorkItemStateTransition` are accurate to the fixture and chart-renderable without restructuring `WorkItemAgingChart`'s axis system. Flow coaches see the new short segments rendered above each state column and can visually compare an in-flight dot against the band for ITS state.
**Disproves if it fails**: either (a) the transitions data is too sparse in the first weeks post-sibling-ship to give meaningful bands for most states (forces postponing this slice until N weeks have accumulated), or (b) anchoring `ChartsReferenceLine` to per-column X ranges is not supported by the existing MUI charts setup and we need to switch chart-rendering primitives (in which case the slice grows and needs DESIGN re-scoping).

## Acceptance criteria
See US-01 in `../feature-delta.md`. Slice-01-specific subset (portfolio + legend toggle + tooltip carry to slice 02):
- Acceptance fixture: a synthetic team with 20 completed items, each having known per-state durations in `In Progress`, `Review`, `Test`. Integration test asserts the endpoint returns exact 50/70/85/95 values per state.
- Chart test: render the chart with a non-empty `perStatePercentileValues` prop; assert that the rendered DOM contains the expected number of `ChartsReferenceLine` (or equivalent) elements at the expected Y positions, anchored to the expected state columns.
- No-regression test: render the chart with `perStatePercentileValues` undefined; assert pixel-identical (or test-id-identical) rendering to the existing chart test snapshot.

## Dependencies
**Hard**: `time-in-state-and-staleness` slice 01 merged to main (transitions table exists, ADO + Jira connectors emit rows). Sibling DESIGN starts after this DISCUSS — coordinate with sibling DESIGN/DELIVER to ensure the sibling does not change `WorkItemStateTransition` schema before this slice DELIVERs (D11 explicitly preserves the schema; this is the formal coordination point).

## Production data requirement
**Required.** The Lighthouse project's own ADO team must be observable through the new chart after sibling-slice-01 has been running for ≥2 weeks (giving real completed items with transitions in the window). DEVOPS smoke against the project's own production instance.

## Dogfood moment
After deploy, on the project's own development Lighthouse instance, open `/teams/{ownTeamId}` → Work Item Aging chart → see short dashed segments above each state column. Confirm the bands' heights look plausible against a manually-checked sample of recently-completed items.

## Pre-slice spike candidates
- 30 min: verify `ChartsReferenceLine` (or another MUI-X-charts element) can be constrained to a sub-range of the X axis. If not, identify the alternative primitive (custom SVG layer over the chart canvas) and re-estimate.
- 30 min: profile the new endpoint with a team that has 6 months of transitions (~thousands of rows) to confirm we do not need caching/materialisation as part of slice 01.
- 15 min: confirm `PercentileCalculator.CalculatePercentile` accepts the input shape we will produce (list of `double` durations in days, same as cycle-time inputs).
