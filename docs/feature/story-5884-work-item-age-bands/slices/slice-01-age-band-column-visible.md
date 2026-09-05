# Slice 01 — Age Band column visible in both dialogs

**Feature**: `story-5884-work-item-age-bands` | **Stories**: US-01, US-02 | **Estimate**: ~1 day

## Goal

A flow coach opening either work item dialog on the Work Item Age widget reads, on every row, which pace band that item's age falls into for the state it is in — and the value always agrees with the coloured zone the chart draws under the same dot.

## Learning hypothesis

**The dialog can reproduce the chart's band classification exactly.**

Confirms if it succeeds: `computePaceBandRects`' three rules — half-open `(lower, upper]` boundaries, carry-forward of the preceding state's percentiles, case-insensitive state matching — are fully specified by the code and can be shared between the chart's geometry and a row-level classifier without the two drifting.

Disproves if it fails: that the chart's zone rule is expressible as a per-item function at all. If any real in-flight item's row band contradicts the zone under its dot, the shared-rule premise is wrong and the whole feature's credibility goes with it — a dialog that disagrees with the chart beside it is worse than no dialog column. Failure here means stopping and reconciling before slice 02 builds sorting and filtering on top of a wrong value.

## Production data

Verified against a **real team on the dev instance restored from a production backup** (`Restore-DbBackup.ps1`), not synthetic fixtures. The acceptance bar is: for every in-flight item on that team, the band rendered in the dialog matches the colour of the chart zone the item's dot sits in. Synthetic percentiles would prove the arithmetic and miss the thing that actually breaks — real teams have states with no exit history, states added mid-flow, and case-mismatched state names, which is precisely what carry-forward and case-insensitive matching exist for.

A team whose `Analysis` state has never been exited by a completed item is required in the sample, so the `No history` sentinel is exercised on real data rather than asserted in a unit test alone.

## Dogfood moment

Same day: open the Lighthouse dogfood instance's own team metrics view, click **View Data** on the Work Item Age widget, and read the band column against the chart with the pace overlay toggled on. Two surfaces, same screen, immediate contradiction check.

## IN scope

- A band classifier taking an item's age, its state, `perStatePercentileValues` and `doingStates`, returning one of the six labels — sharing its boundary and carry-forward rules with `computePaceBandRects` rather than restating them.
- The six labels from D7: `Below 50th`, `50th-70th`, `70th-85th`, `85th-95th`, `Above 95th`, `No history`.
- A new **optional** prop on `WorkItemsDialog` carrying percentiles + Doing states (D17). Absent, the dialog behaves exactly as today — no other call site is edited.
- Widening `ViewDataPayload` (`WidgetShell.tsx:40`) and `ViewDataInputs` / `buildViewData` (`BaseMetricsView.tsx:530`, aging payload L685) with the same optional field, so the View Data dialog receives it. `WidgetShell` forwards an opaque value and learns nothing about percentiles. This is a precursor commit inside this slice, not a slice of its own.
- Passing the same data at the dot-click call site (`WorkItemAgingChart.tsx:750`), where both inputs are already props.
- Column header `${workItemAgeTerm} Band` (D11) with a `description` tooltip carrying the full sense.
- Colouring from `PACE_BAND_COLORS_LOW_TO_HIGH` indexed by band rank, rendered in the existing `getColumnColor` treatment — tinted text plus 10%-alpha background via `hexToRgba` (D4, D15). `No history` renders in `text.secondary`, unpainted.
- Column omitted entirely when no state has a non-empty percentile list (D13).
- Column placed immediately after the age column, and visible even when a column order predating the feature is persisted under the `work-items-dialog` storage key.
- Vitest coverage: boundary-exactly-at-a-percentile, carry-forward, case-mismatched state name, `No history` for a state preceding all percentile columns, `No history` for a state absent from `doingStates`, column omission, and presence with the chart overlay off.

## OUT scope

- Sorting, filtering and export behaviour — slice 02. The column ships **`sortable: false, filterable: false`** so it is honestly informational rather than shipping the alphabetical sort that D9 exists to prevent.
- Any backend change. The endpoint, DTO and `ComputeAgeInStatePercentiles` are untouched.
- Any other widget's View Data dialog, including `ItemsInProgress.tsx` (D6).
- Widening the dot-click dialog's population (D1).
- Colouring the age column beside it — it is uncoloured today because no `sle` is passed, and stays so.
- Any per-cell caveat on the last Doing state (D10).
- Docs and screenshots — those land at feature finalization, after slice 02.

## Acceptance criteria

- Every AC listed under US-01 and US-02 in `feature-delta.md`.
- Plus, specific to this slice: the band column renders with sorting and filtering disabled, and the dialog's existing default sort (age descending in View Data, Time in State on dot-click) is unchanged.

## Dependencies

None outstanding. Every input — the endpoint, both props, the palette, the `getColumnColor` precedent, `sortComparator` on `DataGridColumn` — was verified present in code during the pre-DISCUSS reality check.

## Reference class

`aging-pace-percentiles` slice 01 (the chart overlay this classifier must agree with) and `bug-5571-category-scoped-fetching` (a change confined to the metrics view's prop plumbing). Both landed inside a day.

## Pre-slice SPIKE

Not needed. Uncertainty is low: the rule to reproduce is fully readable in `computePaceBandRects` (L92-163) and the data is already in the component. The risk is in fidelity, not in feasibility, and fidelity is what the dogfood check above measures.
