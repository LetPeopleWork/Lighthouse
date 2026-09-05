# Upstream changes requested by DESIGN — story-5884-work-item-age-bands

Two acceptance criteria locked in DISCUSS describe grid behaviour the shipped code does not have.
Neither was discoverable from the DISCUSS-wave reading, because both live below the call sites that
reading covered — inside MUI-X's own selectors. Both were found by reading the installed package.

The DISCUSS sections are **not** edited. This file is the request; `feature-delta.md` →
`## Changed Assumptions` carries the quoted originals.

---

## UPSTREAM-1 — Export does not respect an active column filter

**Affected**: US-03 AC 4 in `docs/feature/story-5884-work-item-age-bands/feature-delta.md`, and
`step-carry-it-out` in `docs/product/journeys/story-5884-work-item-age-bands.yaml`.

**As written**

> Given a band filter is applied and a premium licence is present, when Paul exports to CSV, then the
> file contains only the filtered rows in the displayed order, and the band column holds the label
> string.

**What the code does**

`useDataGridExport.getGridData` (`Lighthouse.Frontend/src/components/Common/DataGrid/DataGridToolbar.tsx:49`)
takes its row set from `apiRef.current.getSortedRowIds()`. In the installed `@mui/x-data-grid@^9.12.0`
that resolves to `gridSortedRowIdsSelector`, which reads `state.sorting.sortedRows`
(`hooks/features/sorting/gridSortingSelector.mjs`). That array is built by the default sorting
strategy `flatSortingMethod`, which sorts **every** child of the root row group
(`hooks/features/sorting/useGridSorting.mjs:177-185`). Filtering never removes rows from the tree; it
maintains a separate visibility lookup, and the filtered-and-sorted list is a different selector,
`gridExpandedSortedRowIdsSelector`.

So today the exported file contains rows the reader has filtered out. This is existing behaviour for
every grid in the product, not something this feature introduces. Both the comment above the call
(`DataGridToolbar.tsx:51-52`) and the name of the variable holding the result — `visibleRows` — assert
the opposite, and both are wrong.

**Two ways forward**

1. **Swap the selector** — `getGridData` reads the expanded (filtered) sorted ids instead. One line,
   and it makes every grid's export match the list the reader is looking at, which is what the code's
   own comment and its own variable name already claim. It is a defect fix on its own terms; the
   acceptance criterion is only what surfaced it. The comment and the variable name become true rather
   than aspirational.
2. **Narrow the acceptance criterion** — drop the "only the filtered rows" clause and keep the
   band-holds-the-label clause, accepting that export means "everything in the dialog, in sort order".

**DESIGN proposed option 1.** The maintainer chose **option 2** on 2026-09-05: a toolbar shared by every
grid should not have what it writes to disk changed as a side effect of adding a column to one dialog.
DDD-13 in `feature-delta.md` is rewritten to record that, US-03's export criterion is narrowed, and
`DataGridToolbar` / `useDataGridExport` moves to REUSE AS-IS in the Reuse Analysis — this feature no
longer edits that file at all.

**Status of the underlying defect: open, unowned, out of scope for #5884.** Export still writes rows the
reader has filtered out, in every grid. `DataGridToolbar.tsx:51-52`'s comment and its `visibleRows`
variable still assert otherwise and are still wrong. This section is the record so the next reader
finds it rather than rediscovering it from a bug report.

**Now verified** (the DESIGN wave had no shell; the orchestrator ran it): **no existing test pins the
current unfiltered behaviour.** `DataGridToolbar.test.tsx:43` mocks `getSortedRowIds: vi.fn(() => [1, 2,
3])`, so the suite asserts nothing about filter semantics in either direction. Whoever picks the defect
up writes the first test of it, and will not be fighting an existing one.

---

## UPSTREAM-2 — The filter model is not persisted

**Affected**: `shared_artifacts` on `step-cut-to-the-outliers` in
`docs/product/journeys/story-5884-work-item-age-bands.yaml`.

**As written**

> DataGrid filterModel (persisted per storageKey "work-items-dialog", localStorage)

**What the code does**

`PersistedGridState.filterModel` is declared (`components/Common/DataGrid/types.ts:83`) but nothing
reads or writes it. `DataGridBase` passes neither `filterModel` nor `onFilterModelChange` to the
`DataGrid`, so the filter is uncontrolled and lives only in MUI-X's in-memory state. Sort model,
column visibility, column order and column widths are all persisted; the filter is not.

**Accepted — the journey note is corrected and the persistence is not built.** `step-cut-to-the-outliers`
now reads "in-memory only for the life of the dialog".

**Original recommendation: correct the journey note, do not build the persistence.** No acceptance criterion
depends on a filter surviving a dialog close, and a filter that silently persists across sessions is
arguably the worse behaviour for a dialog that a coach opens once per review. Recording it here so the
next reader does not find the declared-but-unused field and assume it works.
