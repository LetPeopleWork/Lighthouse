# Evolution: cumulative-state-time-completion-filter

- **Date finalized**: 2026-06-01
- **ADO**: Story #5144 — "Allow to hide Completed/Ongoing Items in Cumulative Time in State Chart" (tagged Release Notes)
- **Status**: Shipped to `main`, CI-green. Builds on the shipped `state-time-cumulative-view` (B3) chart.
- **Workspace (history)**: `docs/feature/cumulative-state-time-completion-filter/`

## What shipped

A click-to-toggle **Completed / Ongoing** legend on the *Cumulative Time per State*
chart (team + portfolio detail). It lets a delivery lead isolate the in-flight-only
or completed-only picture of where time sits across workflow states, instead of
mentally subtracting the two stacked segments.

- Two `LegendChip` chips ("Completed", "Ongoing"), left of the chart title, both
  active by default. Clicking one hides that stacked series client-side; the bars
  and value axis rescale to the remaining series. Re-click restores.
- **Pure client-side segment hide** — no backend, no API/DTO change, no network
  call on toggle. The per-state Mean/Median/Items aggregates are not user-visible
  (hidden test-only DOM), so there is nothing to recompute.
- **Suppressed while a work item is picked**: the chips render only when the item
  picker has no selection (`completionFilterEnabled = selectedItemIds.length === 0`).
  When a selection exists, the picker is the only filter and both segments show;
  clearing it restores the chips all-visible (hide-state resets).

## Reuse over build (the key decision, D8)

MUI X Charts v9 has no native legend-click series toggle. Rather than build a
custom control, the feature reuses the in-house mechanic the cycle-time
scatterplot already uses for work-item types:

- `LegendChip` (`components/Common/Charts/LegendChip.tsx`) — the clickable chip.
- `useTypesVisibility(["Completed","Ongoing"])` (`hooks/useChartVisibility.ts`) —
  visibility state. Its built-in "can't hide the last visible type" guard makes an
  empty chart impossible by construction (no extra code for that edge case).

Net new logic was a `completionFilterEnabled` prop, a memoised class list, a
series-filter predicate, and one wiring line in `BaseMetricsView`.

## Slices / steps

| Step | Scope | Commit |
|------|-------|--------|
| 01-01 | Chart toggle: LegendChip + useTypesVisibility, series filtering, `completionFilterEnabled` gate | `ac5594a2` |
| 01-02 | `BaseMetricsView` wiring (suppress when item picked) + TBU-defense test | `801f663e` |
| 01-03 | Review revision: chips moved left, next to the title | `b6d64e95` |
| — | Phase-4 review nit (module-const hoist) | `ef24c4f0` |
| — | Mutation hardening + scoped Stryker config + baseline | `ae834378` |

## Quality

- Acceptance: 7 component scenarios (`CumulativeStateTimeChart.test.tsx`) +
  1 host-wiring test (`BaseMetricsView.test.tsx`). Full FE suite 3221 green.
- Adversarial review: APPROVED (0 blockers).
- Mutation (per-feature, Stryker): **92.86%** on the behavioral surface
  (wiring 100%); 2 surviving mutants documented equivalent. Pure-presentational
  `sx`/label/color survivors excluded by scoping, consistent with the prior chart
  baseline. See `deliver/mutation/mutation-baseline.md`.
- DES integrity: 3 steps, complete RED→GREEN→COMMIT traces.

## Notes / lessons

- Delivered concurrently with `lighthouse-user-survey` on the same trunk; the
  survey agent's pushes carried these commits to `origin/main` as they landed.
- `docs/product/jobs.yaml` gained two `refined_by` traces (to
  `job-delivery-lead-spot-workflow-constraint` and `job-po-deep-dive-item-state-time`)
  rather than a new job — this is a refinement of an existing capability, not a new one.
- DESIGN wave was intentionally skipped: the single design question (control
  mechanism) resolved in DISCUSS via the LegendChip reuse decision.
