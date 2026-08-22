# ADR-172: A Delivery exports one settled table that the calling surface builds, not the grid read back through the toolbar

- **Status**: **Accepted** (DELIVER, 2026-08-22) — supersedes [ADR-162](./adr-162-export-header-block-as-generic-toolbar-input.md)
- **Date**: 2026-08-22
- **Feature**: epic-5698-deliveries-as-durable-records (ADO Epic #5698, slice 01b)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

ADR-162 shipped: `DataGridToolbar` grew an `exportHeaderRows` prop, the Delivery section assembled
label/value pairs, and the toolbar emitted them above the grid it read back through the MUI-X
`apiRef`. The maintainer opened the resulting file and it was unusable.

Two separate faults, and only one of them was the header block.

**The header block did not line up.** Nine labelled values in two columns, sitting above a grid with
eleven — so the Delivery's own numbers and its Features' numbers were never in the same column.
Nothing could be sorted or filtered, and the two things a reader most wants to compare were the two
the file would not put side by side.

**Reading the grid back is not reading the grid.** `getGridData` used
`apiRef.getCellValue(rowId, field)`, which returns the value behind a column — and half the columns
on this grid are drawn rather than stored. Forecasts came out as the raw array they are kept in.
Likelihood was always empty, because `field: "likelihood"` does not exist on `IFeature` at all; the
chip is built from `delivery.featureLikelihoods`. Dependencies exported the `.length` that column's
`valueGetter` returns *for sorting*. Team and Progress were blank. Warnings said `false`.

Five columns shipped wrong. No test caught them, because no test asserted what a *rendered* cell
exports as — the toolbar's tests covered the toolbar, and the column tests covered rendering.

## Decision

**The export is a document, not a picture of the grid. A grid may hand the toolbar the finished
table:**

```
exportTable?: (orderedRowIds: GridRowId[]) => { headers: string[]; rows: string[][] }
```

1. **One table, the Delivery as its first data row.** Headings, then the Delivery named
   `<name> (<Delivery term>)` carrying its own progress, forecasts and likelihood in the same columns
   the Features use, then one row per Feature. Nothing sits above the headings.

2. **The column set is settled and no longer follows the screen.** Hiding or reordering a column
   changes nothing in the file. Two people looking at the same Delivery through differently arranged
   grids produce the same document — which is the point, because the file is compared against last
   week's file by someone who never saw either grid. The reader's **sort and filter still decide the
   rows**: the toolbar passes `apiRef.getSortedRowIds()` into the callback.

3. **The builder is a pure function, and there is no second export path.** `DataGridToolbar`,
   `DataGridBase`, the premium gate, the CSV escaping and the clipboard HTML are untouched. A grid
   that passes nothing keeps the old scraping behaviour exactly. An archived Delivery supplies a
   sibling builder over its pinned rows and reuses the whole mechanism — which is what ADR-161's
   "no separate code path for the archived read" actually cashes out to.

This **reverses ADR-162's central claim**. That ADR said the export must read what is on screen, and
made the toolbar the assembler. Both halves were wrong: what is on screen is drawn, not stored, so it
cannot be read back; and the assembler has to be the surface that knows what the values mean.

## Alternatives considered

- **Per-column export accessors** — an `exportValue?: (row) => string` on `DataGridColumn`, so the
  toolbar keeps assembling. **Rejected.** It preserves column visibility and order in the file, which
  is the behaviour being deliberately given up, and it cannot express one UI column becoming four
  file columns (one Forecast chip → `Forecast 50/70/85/95`). A synthetic Delivery row would also have
  to survive accessors written against `IFeature`.
- **Keep the header block, fix only the cells.** **Rejected** — it leaves the Delivery's numbers in
  different columns from its Features', which was the maintainer's first complaint.
- **Adapt a pinned row into an `IFeature` so one builder serves both.** **Rejected** — it means
  inventing `getTotalWorkForTeam`, `dependsOn` and `stateCategory` on a record that never held them.
  Two builders sharing one `HEADERS` definition, with a test asserting they emit an identical header
  row, keeps them from drifting without fixture theatre in production code.

## Consequences

- **Positive**: derived columns export what they say. A test now asserts a drawn cell's exported
  value — the gap that let five columns ship wrong.
- **Positive**: the archived export reuses the live mechanism whole; only the row builder differs.
- **Negative**: **AC-01.5 was rewritten.** It promised "only the columns currently visible, in their
  current order"; hiding a column no longer changes the file. This is a deliberate behaviour
  reversal, not a regression, and the acceptance scenarios were rewritten to say so.
- **Negative**: two row builders exist. They share `HEADERS`, the forecast probabilities and the
  absent-value constant, and a test pins that they emit the same header row.
- **Reuse verdict**: `DataGridToolbar` → **EXTEND** (one optional prop replaces `exportHeaderRows`).
  `DataGridBase`, `FeatureListDataGrid` → **EXTEND** (thread it). `DeliverySection`,
  `ArchivedDeliveriesSection` → **EXTEND** (supply a builder). `deliveryExportHeader.ts` → **DELETE**.
- Cross-refs [ADR-161](./adr-161-archived-delivery-read-path-cannot-see-live-features.md) (the
  archived rows this export reads are the pinned ones).

## Lesson

The failure was not the mechanism, it was the absence of a claim about the output. Every test asserted
how a cell *renders*; none asserted how it *exports*. A file assembled from a different source than
the screen needs its own assertion, or it will be wrong in exactly the places nobody is looking.
