# Slice 02 — Order, cut and export the in-flight list by Age Band

**Feature**: `story-5884-work-item-age-bands` | **Stories**: US-03 | **Estimate**: ~0.5-1 day

## Goal

A flow coach sorts the in-flight list worst-band-first, filters it down to a single band, and takes that list out of Lighthouse as a CSV whose band column reads in words.

## Learning hypothesis

**Band triage in the dialog makes the separate table view unnecessary.**

This is the ADO's own stated bet — the 2026-08-31 call chose the dialog enhancement over a separate table view on the flipside of the chart, on the reasoning that it is cheaper and "may make the separate view unnecessary". This slice is what puts that bet to the test, because sorting, filtering and export are the three capabilities the separate view would have existed to provide.

Confirms if it succeeds: Paul Brown runs flow reviews off the dialog and records on ADO #5884 that the flipside table view is no longer wanted. The deferred feature is closed rather than merely postponed.

Disproves if it fails: that a column inside a modal dialog can carry a table-shaped reading task. Plausible failure modes to watch for, each of which would argue for the separate view rather than against the band: the column header ⋮ menu is too obscure a route to filtering to be found (there is no toolbar filter button); or the modal is too small to read a long list in; or coaches want the band alongside data the dialog does not carry.

Note that failure here does **not** invalidate slice 01 — the band remains readable and correct either way. It invalidates only the claim that the dialog replaces the table view.

## Production data

Same restored-production-backup team as slice 01. The export assertion is made on a real CSV opened in a spreadsheet, not on an in-memory string: the band column must read `Above 95th`, never `3` and never blank. `getCellValue` returning a raw rank is exactly the defect this slice exists to avoid, and it is invisible in any test that inspects the column value rather than the produced file.

## Dogfood moment

Same day: on the dogfood instance, filter the Work Item Age widget's View Data dialog to `Above 95th`, export, open the file, and paste the result into the next flow review's notes. The artifact-in-a-doc step is the point — it is the capability the chart never had.

## IN scope

- Enable sorting on the band column with a `sortComparator` mapping label to rank 0-4 (D9). Descending order: `Above 95th`, `85th-95th`, `70th-85th`, `50th-70th`, `Below 50th`, `No history`.
- `No history` at rank -1, so it sorts first ascending and last descending — never heading a worst-first list.
- Make the column `type: "singleSelect"` with the six labels as `valueOptions` (D14), so the built-in column filter offers a dropdown rather than a free-text contains box. Configuration of the built-in control, not a control of our own — D3 holds.
- Confirm `valueGetter` returns the **label string** so `getCellValue` puts words in the CSV (D12). No `exportTable` plumbing is added.
- Vitest coverage: rank-based sort order in both directions, `No history` placement in both directions, filter dropdown options, and an export assertion reading the produced rows.
- Confirm both dialogs' default sorts are unchanged — the band is sortable, not the new default.

## OUT scope

- A toolbar filter button (D3). The column header ⋮ menu is the only route and that discoverability cost is accepted, not designed around.
- Any chip row or bespoke filter control (D3).
- Any change to export's premium gate. Copy and download stay disabled without a licence, exactly as for every other grid; sorting and filtering work regardless.
- Multi-band filter selection (e.g. "85th-95th or above"). `singleSelect` filters one value at a time; if reviews turn out to need two bands at once, that is a follow-up informed by the hypothesis above, not a guess made now.
- Changing either dialog's default sort.
- Any backend change.

## Acceptance criteria

- Every AC listed under US-03 in `feature-delta.md`.

## Dependencies

**Slice 01 must land first.** This slice turns on sorting and filtering over the classifier slice 01 builds; there is nothing to order until the value exists and is trusted.

## Reference class

`bug-5571` and `story-5877`-era grid work — configuring an existing MUI-X DataGrid column's sort and filter behaviour, no new component. Consistently well under a day. The only novel part is the export assertion, which is one test.

## Pre-slice SPIKE

Not needed. The one thing worth confirming early — whether export reads rendered cells or raw values — was already answered during the pre-DISCUSS reality check by reading `useDataGridExport` in `DataGridToolbar.tsx`: it calls `apiRef.current.getCellValue`, so it reads values. That answer is what D12 is built on and it removed the only real unknown.
