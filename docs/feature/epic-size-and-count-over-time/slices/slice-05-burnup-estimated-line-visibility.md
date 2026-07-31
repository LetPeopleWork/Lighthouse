# Slice 05 — The burnup's estimated line stays visible over the Done area

**Feature**: epic-size-and-count-over-time · **ADO**: Epic #5585 · **Story**: US-05 · **Estimate**: ~2h
**Reference class**: `DeliveryBurnupChart.tsx` itself — one file, existing tests as the safety net.

## Goal

When the estimated (not-broken-down) total is lower than the done count, the forecaster can still read
the line instead of concluding there is no estimated work left.

## The defect, precisely

`DeliveryBurnupChart.tsx:62-79` — the Done series carries `area: true`, so MUI-X paints a filled region
for it. The estimated series (`:71-79`, `theme.palette.warning.main`, `strokeDasharray "2 4"`) is pushed
last but is drawn *underneath* that fill wherever `estimatedItemCount < doneWork`. It disappears exactly
when it matters most — late in a delivery, when the remaining guessed scope is the interesting number.

## IN scope

- One rendering fix in `DeliveryBurnupChart.tsx` — paint order and/or Done-area fill opacity, whichever
  keeps the Done area reading as an area (AC-5.2).
- Vitest AC-5.1 … AC-5.4, including a fixture where `estimatedItemCount < doneWork` on every point.

## OUT of scope

- Changing what `estimatedItemCount` means or how it is recorded.
- Re-colouring or re-styling the burnup beyond what the fix requires.
- Applying the same treatment to the predictability or fever charts.

## Learning hypothesis

**Disproves** "the line is hidden purely by paint order" **if** raising it above the fill still reads
badly against a saturated Done area — then the fix is an encoding change (marker, second axis,
annotation), not a z-order tweak, and the slice is re-scoped rather than declared done.
**Confirms**, if it holds, that the fix is a two-line change and Chris's side-note is closed.

## Acceptance criteria

AC-5.1 … AC-5.4 verbatim from `feature-delta.md`. Load-bearing pair:

- With `estimatedItemCount < doneWork` throughout, the estimated series is rendered and distinguishable
  from the Done fill (AC-5.1).
- The Done series is still a filled area and all existing `DeliveryBurnupChart.test.tsx` assertions pass
  unchanged (AC-5.2 / AC-5.4).

## Dependencies

None — independent of slices 01-04. Scheduled last only because it touches a chart users already rely
on, so it lands when review attention is cheapest.

## Dogfood moment

Same day: open a dogfood delivery that is well into its Done curve and confirm the dashed estimated line
is legible where it previously vanished.
