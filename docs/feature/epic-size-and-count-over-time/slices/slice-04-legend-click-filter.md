# Slice 04 — Click the legend to isolate epics

**Feature**: epic-size-and-count-over-time · **ADO**: Epic #5585 · **Story**: US-04 · **Estimate**: ~3h
**Reference class**: the existing chart-legend interaction patterns already used in the metrics widgets
(MUI-X legend + local component state).

## Goal

A delivery with a dozen epics stays readable: click epics in the legend to show only their bars.

## IN scope

- Legend built from the **union of epics across the whole window**, so an epic that left the delivery is
  still listed (D7 / AC-4.1).
- Multi-select toggle semantics (D8): click adds, click again removes, empty selection = show all.
- A "Show all" reset control.
- Selection held in component-local state — two expanded deliveries filter independently (AC-4.6).
- Bars filter; the count line does not (AC-4.5); y-axis rescales to the selection.
- Vitest AC-4.1 … AC-4.6.

## OUT of scope

- Persisting the selection across reloads or into the URL.
- Filtering any of the other three charts on the tab.
- Drill-through from a segment to the Work Items tab (named out-of-scope at feature level).

## Learning hypothesis

**Disproves** "a stacked bar per day is the right form for a real delivery" **if** a 12-epic dogfood
delivery is still unreadable even filtered — that would say the chart needs a different form (e.g.
small multiples), not more controls, and the finding is worth more than the slice.
**Confirms**, if it holds, that the chart scales to the deliveries Lighthouse users actually run.

## Acceptance criteria

AC-4.1 … AC-4.6 verbatim from `feature-delta.md`. Load-bearing pair:

- Clicking one legend entry isolates it; clicking a second shows both (AC-4.2).
- The count line is unaffected by any selection (AC-4.5).

## Added on review of slice 01 (Benjamin, 2026-08-02)

**Collapse the legend by default.** Slice 01's card already runs tall on the dogfood instance, and the
cause is the neighbouring fever chart's legend — one entry per epic, wrapping to eight lines. Slice 02's
per-epic bars will produce exactly the same legend on this chart. Filtering by legend is a special-case
action, so paying one extra click to open the legend is the right trade against every user carrying
eight lines of chrome on every visit.

Applies to **both** charts — the new one and `DeliveryFeverChart` — so the two behave the same way on
the same tab. Treat the fever chart's legend as in scope for this slice rather than filing it separately.

## Dependencies

Slice 02 (bars must exist to filter). Independent of slice 03 — hatching and filtering do not interact
beyond both rendering segments.

## Dogfood moment

Same day: open the dogfood delivery with the most epics, isolate the two currently under discussion,
and confirm the trajectory is legible.
