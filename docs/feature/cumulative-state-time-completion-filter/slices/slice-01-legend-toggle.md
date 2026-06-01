# Slice 01: Completion-class legend toggle

ADO #5144 — single thin slice (the whole feature). Brownfield, frontend-only, ≤1 day.
Story: **US-5144-01** — Isolate completed-only or ongoing-only time in the Cumulative Time per State chart.
job_id: `job-delivery-lead-spot-workflow-constraint` (refined); secondary `job-po-deep-dive-item-state-time`.

## Goal

A delivery lead can click a "Completed" or "Ongoing" legend entry on the *Cumulative Time per State*
chart to hide that stacked segment (client-side), with the bars and value axis rescaling to the
remaining series — and the toggle is suppressed while a work item is selected in the picker.

## IN scope

- Two `LegendChip` chips on `CumulativeStateTimeChart.tsx` ("Completed", "Ongoing"), both active by
  default, driven by `useTypesVisibility(["Completed","Ongoing"])` — reusing the existing
  cycle-time-scatterplot show/hide mechanic verbatim (no new control built).
- Client-side hide of the corresponding stacked series; y-axis rescales to remaining series.
- Re-click restores the hidden series.
- Hide-state held in `BaseMetricsView.tsx` next to `selectedItemIds`; suppressed and reset when a
  work item is selected; restored (both active) when the selection clears.
- Identical behaviour on team detail and portfolio detail.
- One analytics funnel event for the toggle (or documented qualitative fallback).

## OUT scope

- Any backend / API / DTO change (pure client-side, per D7).
- Changing the hidden `display:none` tooltip Box (test-only, never visible).
- Persistence of the toggle across reloads (session-local only).
- Any change to the item picker's own behaviour.
- A third combined/"Total" toggle, per-state hiding, or a chronology lens.

## Learning hypothesis

Delivery leads can read the systemic constraint faster from an isolated single-class view than from
the stacked view. **Disproved if**: users never engage the toggle (≈0% adoption in chart-engaged
sessions over 60 days) or report the isolated view is no easier to interpret than the stacked one —
indicating the stacked bar already answered the question and the toggle adds noise.

## Acceptance criteria

- [ ] (a) Both segments + both legend entries active by default (no item selected).
- [ ] (b) Click "Completed" → completed segment hidden everywhere; bars + axis show ongoing-only.
- [ ] (c) Click "Ongoing" → ongoing segment hidden everywhere; bars + axis show completed-only.
- [ ] (d) Re-click a hidden entry restores it.
- [ ] (e) `selectedItemIds.length > 0` → toggle absent/disabled; both segments shown.
- [ ] (f) Selecting an item resets hide-state; clearing restores legend with both entries active.
- [ ] (g) Identical on team and portfolio detail.
- [ ] Guardrail: no network request fires on toggle.

## Dependencies

- `state-time-cumulative-view` feature SHIPPED (chart, series, item picker, host wiring in `main`).
- No data migration, config, or feature flag.

## Effort estimate

≤ 1 day. Frontend-only, single interaction on an existing component. 7 UAT scenarios.

## Reference class

Comparable to prior in-chart interaction refinements on this same component during the
`state-time-cumulative-view` build (e.g. the item-picker drill-down / picker-layout revisions, B3
review-revision slices) — each landed inside a day as a localized FE change with Vitest coverage.

## Reuse note (D8)

MUI X Charts v9 (`@mui/x-charts@9.0.1`) has **no** native legend-click series toggle — so reuse the
in-house mechanic rather than building one: `LegendChip` (`components/Common/Charts/LegendChip.tsx`) +
`useTypesVisibility` (`hooks/useChartVisibility.ts`), exactly as `CycleTimeScatterPlotChart.tsx`
(lines 268-273) filters work-item types. Pass only series with `visibleTypes[label] !== false` to
`<BarChart>`. The hook's built-in "can't hide the last visible type" guard prevents an empty chart, so
"hide both" needs no special handling.
