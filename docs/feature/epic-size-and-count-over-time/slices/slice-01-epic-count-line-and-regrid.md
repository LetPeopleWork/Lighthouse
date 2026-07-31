# Slice 01 — Epic-count line lands as the fourth chart (2×2 regrid)

**Feature**: epic-size-and-count-over-time · **ADO**: Epic #5585 · **Story**: US-01 · **Estimate**: ~4h
**Reference class**: `DeliveryPredictabilityChart` (same tab, same history prop, same empty state)

## Goal

A forecaster opening a delivery's Metrics tab sees a fourth card whose line says how many epics were in
the delivery each day — using history that is **already recorded**, so the chart has real data on day one.

## IN scope

- New `Lighthouse.Frontend/src/components/Common/Charts/DeliveryEpicSizeChart.tsx` — line-only for now,
  count per point derived from `point.featureBreakdown.length` (D3).
- Title + legend header via `getTerm(TERMINOLOGY_KEYS.FEATURES)` (D10) — passed in as a prop from
  `DeliverySection.tsx:150`, which already resolves the term.
- Forward-only empty state, wording copied from `DeliveryBurnupChart.tsx:15-16`.
- Help text stating the 0-size-epic caveat (D3).
- `DeliverySection.tsx:589-620`: insert the new `EnlargeableChart` third, drop
  `gridColumn: { lg: "1 / -1" }` from the fever chart's `Box` → 2×2.
- Vitest: AC-1.1 … AC-1.6, including a fixture built from a **pre-feature** 4-field `featureBreakdown`.

## OUT of scope

- Any bar (slice 02). Any backend change at all — this slice is frontend-only.
- Legend interaction (slice 04), hatching (slice 03), burnup fix (slice 05).
- Docs page + screenshot (owed at DELIVER, after slice 02 gives the chart its bars).

## Learning hypothesis

**Disproves** "epic-count history is already in the database and needs no new column" **if** real
recorded snapshots turn out to have empty or absent `featureBreakdown` on most days — in which case D3
collapses, the count becomes a new forward-only field, and slice 01 loses its retroactive-data selling
point and merges into slice 02.
**Confirms**, if it holds, that the chart delivers value before a single new byte is recorded.

## Verify the premise first (10 min, before writing the component)

On the dogfood instance, hit `GET .../deliveries/{id}/metrics-history` for a delivery with weeks of
history and check that `points[].featureBreakdown` is non-empty on the great majority of days. If it is
mostly empty, stop and re-plan per the hypothesis above.

## Acceptance criteria

AC-1.1 … AC-1.6 verbatim from `feature-delta.md`. The two that carry the slice:

- A history of `featureBreakdown` lengths `[7, 7, 9]` renders a line reading 7, 7, 9.
- A history whose entries carry **only** `referenceId/name/completion/likelihood` still renders the line
  (AC-1.6) — this is the retroactive-history proof.

## Dependencies

None. `DeliveryMetricsHistory` already reaches the tab; `featureBreakdown` is already parsed.

## Dogfood moment

Same day: open the dogfood instance's longest-running delivery → Metrics tab → the line should already
show weeks of history, and the 2×2 grid should look like the sketch in ADO 5585.
