# Slice 01 — Export a Delivery record

**Epic** #5698 · **Story** US-01 · **ADO** #4309 · **Estimate** ≤1 day

> **Superseded in part by [slice 01b](slice-01b-one-table-export.md).** Verification found the
> artifact this slice shipped unreadable: the header block below could not be sorted or filtered, and
> the grid beneath it exported a raw JSON array where a forecast belonged and blanks where progress,
> team and likelihood belonged. Slice 01b replaces the header block with one table whose first row is
> the Delivery, and settles the column set instead of following whichever columns are on screen.
> The export section of this document describes what was shipped, not what is there now; the goal,
> the premium gate and the absent-value rule still hold.

## Goal
A Delivery's headline numbers and its Feature grid leave Lighthouse as one CSV file or one clipboard
paste.

## IN scope
- Forward `enableExport` / `exportFileName` through `FeatureListDataGrid` to `DataGridBase` (the prop
  stops there today).
- Opt the Delivery's Feature grid into the existing export toolbar.
- A header block prepended to both the CSV and the clipboard payload: Delivery Name, Delivery Date,
  Forecast 70%, Forecast 85%, Forecast 95%, Likelihood, Total Work Items, Completed Work Items,
  Remaining Work Items — then one blank line, then the grid.
- Terminology-aware field labels in the header block.
- Absent-value rendering: empty, never `null` / `NaN` / a fabricated `0`.

## OUT of scope
- Any other grid opting into export.
- Exporting the metrics-history charts.
- Changing the premium gate, its tooltip or its disabled affordance.
- Any backend change. This slice is frontend only.

## Learning hypothesis
**Disproves if it fails**: that the existing generic grid export is reusable for a
header-plus-grid artifact. If the toolbar cannot carry a header without contorting `DataGridToolbar`
into knowing about Deliveries, then Slice 05's "export the frozen record with no separate code path"
(D8) is also wrong, and the archived read view needs its own export — discovered now, for a day, not
in Slice 05.
**Confirms if it succeeds**: the header + grid shape is stable enough that the pinned closure record
of Slice 04 can be defined to serve exactly it.

## Acceptance criteria
AC-01.1 … AC-01.8 in `../feature-delta.md`.

## Dependencies
None.

## Reference class
`WorkItemsDialog.tsx:300` — the one existing grid that opts into export. Same toolbar, same gate.

## Dogfood moment
Export the team's own live Delivery and paste it into the next status update, same day.
