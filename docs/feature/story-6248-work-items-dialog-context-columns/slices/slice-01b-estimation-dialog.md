# Slice 01b: The Estimation dialog shows each item's estimate

**Goal:** from Estimation vs. Cycle Time, the Work Items dialog shows Estimate, Cycle Time and Closed per row, with
the rest of the catalogue one click away, and its layout remembered for that context alone. (Split from slice 01 in
review round 1, R1.)

## IN scope
- `IWorkItem` gains optional `estimate?: IWorkItemEstimate | null` (`value`, `displayValue`, `unit`): the TypeScript
  mirror of 01a's DTO for rows that arrive as raw JSON. The Feature zod schema does **not** carry `estimate` (DDD-12).
- The column catalogue `workItemColumns.tsx` (D1, D2, DDD-7), with the existing ageBand / sleRisk / warnings
  builders moved in by a refactor commit first.
- The one declared defaults map `workItemsDialogContexts.ts` (D4, DDD-5) with the `estimation` entry; a dialog
  without a context renders exactly as today (AC-1.6).
- `DataGridBase`: a default visibility model, override-only persistence, *Reset layout* to the defaults (DDD-9);
  other grids pass nothing and behave as today.
- Per-context layout key `work-items-dialog:<contextId>:<ownerKind>` and the `ownerKind` prop (DDD-6, M2).
- Estimate header and cells (S1), dates as `YYYY-MM-DD` (S2), column order (S3, R4).
- The E2E POM reads by header; the walking skeleton on Team Zenith's demo data is un-skipped (S7).

## OUT of scope
- Every other context's new defaults (slices 02, 03). They look exactly as today (AC-1.6).
- Loading and could-not-load looks (slices 02, 03): the Estimation dialog's rows are already loaded.
- Clients (D5), tags.

## Learning hypothesis
- **Disproves** "one defaults map can drive every context" if the Estimation context needs logic the map can't
  express (e.g. a value only the chart knows), forcing per-call-site code again.
- **Confirms** it if the Estimation context is one map entry plus the generic catalogue.

## Acceptance criteria
AC-1.1 to AC-1.6 in `../feature-delta.md` (US-01). Value on its own: Steve's request, answered.

## Dependencies
Slice 01a (the estimate on the row).

## Effort
About 1 to 1.5 days: the catalogue and map are new files, and `DataGridBase`'s override-only persistence is the
delicate part (the sanitize effect must write through the same diff).

## Dogfood
Demo data, Team Zenith: click a bubble and see `Estimate (Story Points)` per row; hide Closed and reopen: Closed
stays hidden, and a dialog not yet adopted (e.g. the Cycle Time scatter's) is unchanged; open a Portfolio's
Estimation dialog and confirm the Team's layout did not follow it (M2).
