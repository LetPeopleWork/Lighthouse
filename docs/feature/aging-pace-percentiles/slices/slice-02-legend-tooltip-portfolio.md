# Slice 02: Legend Chip Group + Tooltip Annotation + Portfolio-Scope Parity

**Feature**: aging-pace-percentiles
**Stories shipped**: US-02 (independent legend toggle), US-03 (in-flight item tooltip annotation), and the portfolio-scope half of US-01 (the team-scope half ships in slice 01).
**Estimate**: ~2 crafter days
**Reference class**: similar to existing `PercentileLegend` chip-group extensions + existing chart-tooltip extension patterns. Portfolio endpoint mirrors the team endpoint from slice 01.

## Goal
Make the new per-state bands as usable as possible: let users toggle them independently of
the existing CT bands, surface the per-state pace assessment in the tooltip text (no need to
eye-ball Y positions), and reach parity with the portfolio-scope chart.

## IN scope
- New backend endpoint: `GET /api/portfolios/{portfolioId}/metrics/ageInStatePercentiles?startDate&endDate`. Same shape as the team endpoint from slice 01, scoped to the portfolio (locked D8).
- Frontend service + hook + `BaseMetricsView` wiring for the portfolio scope (mirrors the team-scope plumbing from slice 01).
- `PercentileLegend` extension: add a second chip group with sub-header `Age-in-State %iles (per state)`, mirroring the existing `Cycle Time %iles (overall)` group. Independent toggle state per chip (already supported by `useChartVisibility` for the existing percentile chips; extend that hook OR add a parallel `useChartVisibility` invocation for the new series — DESIGN choice).
- Low-sample tooltip on the per-state legend group: when ANY state in the rendered set has `sampleSize < 10`, the chip group's tooltip text appends `Note: some states have low sample sizes — hover the chart bands for per-state counts.`
- Per-band hover tooltip: hovering a per-state segment shows `<percentile>th %ile for <state>: <value>d (n=<sampleSize>)`.
- `WorkItemAgingChart` tooltip extension for in-flight item dots: append one new line per US-03 to the existing tooltip text, using the new `perStatePercentileValues` prop and the dot's `daysInState` to assign one of the buckets (`below 50`, `at 50-70`, `at 70-85`, `at 85-95`, `above 95`, `no historical data`). Computed client-side — no extra round-trip.

## OUT scope
- Configurable percentiles → out (D4)
- In-flight dot recolour by percentile bucket → out (would conflict with existing colour semantics)
- Server-persisted legend toggle state → out (session-only, matches existing chip behaviour per US-02 AC)

## Learning hypothesis
**Confirms if it succeeds**: flow coaches use the legend toggle to switch between the two pace lenses during real conversations (validated by `OUT-aging-pace-legend-toggled` KPI ≥15% within 4 weeks), and the tooltip annotation reduces the time-to-identify a pace outlier (anecdotal during dogfood — no formal metric, but observable when watching a coach drive the view in a flow review).
**Disproves if it fails**: legend chip group is ignored OR tooltip annotation creates noise that coaches turn off — would signal that per-state bands are too subtle a signal to drive a workflow change and we should consider a stronger visual (which then conflicts with our locked "no dot recolour" decision; this would be a re-DISCUSS trigger).

## Acceptance criteria
See US-02 and US-03 in `../feature-delta.md`. Slice-02-specific additions:
- Portfolio-scope acceptance: open `/portfolios/{portfolioId}` (the metrics view), confirm per-state bands render with the same behaviour as team scope.
- Legend independence test: toggle the `Age-in-State 85` chip — confirm only the 85th per-state segments hide; CT bands and other per-state percentiles unaffected.
- Tooltip annotation test (per US-03 AC): hover a dot whose current state has `daysInState = 12` and per-state 85th percentile for that state is `10`. Assert tooltip contains `Pace: above 85th percentile for <stateName>`.

## Dependencies
Slice 01 (this slice consumes the team-scope endpoint pattern, the chart-prop pattern, and
the legend pattern established there). Cannot start until slice 01's chart-extension shape is
stable enough that the legend chip group can be wired in without churn.

## Production data requirement
**Required.** Both scopes verified on the project's own Lighthouse instance: team chart shows
both band series with independent toggles; portfolio chart (using whichever portfolio
contains the dev team) shows the same; tooltip annotation visible on at least one in-flight
item.

## Dogfood moment
During the next internal flow review (or simulated one if the timing does not match), the
flow coach drives the team detail page → toggles off the `Cycle Time %iles` group to focus
on per-state pace only → hovers a flagged item → reads the tooltip aloud ("`Pace: above 85th
percentile for Review`") → demonstrates that the conversation can name the specific state
without consulting another view.

## Pre-slice spike candidates
- 15 min: confirm `useChartVisibility` can manage two independent percentile-chip groups OR plan the parallel-hook refactor.
- 15 min: confirm chart-tooltip extension point in MUI-X-charts `ChartsTooltip` API for the new annotation line — if API does not accept text injection, fall back to custom tooltip component (which is a heavier change; flag as a re-estimate trigger).
- 15 min: verify portfolio metrics view in `BaseMetricsView` mirrors team metrics view closely enough that the chart wiring is mechanical (it should — `BaseMetricsView` is shared across team and portfolio, per `BaseMetricsView.tsx:789` rendering `WorkItemAgingChart` from shared ctx).
