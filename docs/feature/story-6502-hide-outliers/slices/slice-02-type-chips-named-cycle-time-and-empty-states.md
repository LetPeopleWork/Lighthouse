# Slice 02: Type chips, named Cycle Time, date range and the empty states

Story: US-6502-03. Builds on slice 01. (Renumbered from the former slice 03 after the merge of slices 01 and 02, AFK-6.)

## Goal

The chip stays truthful while the reader changes what the chart shows: type chips, the Cycle Time selector, the date
range, and the cases where nothing, everything or no type is visible.

## IN scope

- Chip count and popover rows cover only outliers of visible Work Item types (an untyped item counts as visible, as it
  does for the dots); toggling a type updates both at once.
- The cut and the axis do not move when type chips are toggled (median and axis come from all items of the range).
- Named Cycle Time selected: the calculation runs on that named value and the popover names it (the definition's
  name replaces the Cycle Time term in the rule sentence).
- Date range changed: median, cut, chip and axis recompute from the items of the new range.
- Nothing is an outlier: no chip, axis as today.
- Every visible Work Item is an outlier (possible when the reader hides the other types): the plot frame, axes and
  lines stay, the legend row (including the chip and the type chips) stays, and the plot says "Every visible
  Work Item is an outlier. Open the chip to see them." Never "No data available".
- All types switched off (AFK-12): the legend row stays and the plot says "All Work Item types are hidden. Switch one
  on in the legend." Never "No data available" (that stays only for a chart with no items at all).
- Loading and failure: the chip is part of the widget and is dimmed or replaced with the rest; it never shows a count
  from the previous answer (the disabled-and-closed behaviour of AFK-11 is built in slice 01; this slice tests it
  across range changes). A page-level test asserts the chart's own "No data available" never shows while loading.

## OUT of scope

Outliers hidden by type are not disclosed (the reader hid the type).

## Learning hypothesis

Disproves: "readers understand that the chip follows the type chips." Confirm on the dev instance: hide a type that
holds the outlier, the chip disappears and returns when the type is shown again, and 3 readers are not surprised.
If they are, the chip needs a "(1 more in hidden types)" hint, which is a follow-up, not part of this slice.

## Acceptance criteria (summary)

- Count follows visible types; axis unchanged by toggles.
- Named Cycle Time and date range recompute; the popover uses the selected name.
- All-outliers, all-types-off and no-outliers states as above; the legend row survives the first two.
- No stale count and no "No data available" while loading.

## Dependencies

Slice 01. Premium: the named Cycle Time selector exists only with the premium licence; the calculation itself is not
gated.

## Effort and reference class

About 1 day. Reference class: earlier changes to `useChartVisibility` consumers and the named-cycle-time selector
story. Production data: dev instance (restored history) plus a named Cycle Time.
