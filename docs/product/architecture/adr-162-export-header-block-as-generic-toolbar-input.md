# ADR-162: The export header block is a generic key/value input on the existing grid toolbar, not a second export action owned by the Delivery surface

- **Status**: **Proposed** (DESIGN, 2026-08-21)
- **Date**: 2026-08-21
- **Feature**: epic-5698-deliveries-as-durable-records (ADO Epic #5698, slice 01)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

D7 requires the export to be **one artifact**: a header block, a blank line, then the Feature grid.
D8 requires it to read what is on screen, including for an archived Delivery.

The existing `DataGridToolbar` already owns both export actions (download CSV, copy to clipboard),
both premium gates, the CSV escaping, and the "visible columns, current order, current sort" reading
via the MUI-X `apiRef`. It knows nothing about a header, and nothing about Deliveries.

`FeatureListDataGrid` does not forward `enableExport` to `DataGridBase` at all, so the Delivery grid
has no export button today. That precursor edit sits in Slice 01 regardless of this decision.

## Decision

**`DataGridToolbar` gains one optional, Delivery-ignorant prop, threaded through `DataGridBase` and
`FeatureListDataGrid`:**

```
exportHeaderRows?: ReadonlyArray<{ label: string; value: string }>
```

Three points that are part of the decision:

1. **The toolbar stays generic.** It receives label/value pairs and knows nothing about what they
   mean. Any grid in the app can grow a header block later without touching the toolbar again.

2. **One artifact, both formats.** For CSV, the header rows are emitted as two-column rows through the
   *same* escaping helper as the grid body, then a blank line, then the grid. For the clipboard, the
   header rows, the blank row and the grid rows are emitted as one HTML `<table>`, so pasting into a
   spreadsheet lands the whole thing in cells as a single contiguous block matching the CSV's
   structure — rather than two tables the user has to reconcile.

3. **The Delivery section assembles the values, because only it can.** Every label resolves through
   the instance's own Terminology (`Delivery`, `Feature`, `Work Item`), which is a frontend concern
   the toolbar has no business knowing. D8 follows for free: the section passes whatever it is
   currently rendering, so an archived Delivery exports its pinned numbers without the export path
   knowing that archived is a thing.

## Alternatives considered

- **The Delivery section supplies its own export action** via the toolbar's existing `customActions`
  slot. **Rejected** — it produces two export buttons unless the built-in one is suppressed, and it
  would have to re-implement `getGridData`, the CSV escaping and the premium gate to get the same
  bytes. D7 asked for one artifact; this is the option that most easily yields two.

- **A Delivery-aware prop on the toolbar** (`deliveryHeader?: IDelivery`). **Rejected** — it pushes
  Terminology resolution and delivery-shaped knowledge into a component shared by every grid in the
  app, coupling the generic toolbar to one feature's model.

- **Server-rendered export.** A new endpoint returning the finished CSV. **Rejected** — it
  contradicts D8 directly: the server does not know which columns are visible, in what order, or
  under what sort, so the artifact would stop matching what is on screen.

## Consequences

- **Positive**: the premium gate, the escaping and the visible-columns/sort reading are inherited
  unchanged, so AC-01.5 and AC-01.7 need no new logic.
- **Positive**: `FeatureListDataGrid` forwarding `enableExport` is a strictly additive fix that also
  unblocks any other consumer of that grid.
- **Negative**: two call sites (`handleExportToCSV`, `handleCopyToClipboard`) both have to prepend the
  header, so they can diverge. They already share `getGridData`; the header prepend goes in the same
  shared helper, and a Vitest case asserts the two outputs carry the same header rows.
- **Negative**: an empty `exportHeaderRows` array and an omitted prop must behave identically (no
  stray blank leading line), which is a real off-by-one worth a test rather than a code comment.
- **Reuse verdict**: `DataGridToolbar` → **EXTEND** (one optional prop).
  `DataGridBase` → **EXTEND** (thread the prop).
  `FeatureListDataGrid` → **EXTEND** (forward `enableExport`, `exportFileName` and the new prop —
  currently forwards none of them). `DeliverySection` → **EXTEND** (assemble the rows).
  No new component.
- Cross-refs [ADR-121](./adr-121-delivery-metrics-history-client-projection.md) (the client-side
  Delivery projection whose fields feed the header block),
  [ADR-161](./adr-161-archived-delivery-read-path-cannot-see-live-features.md) (why the archived row
  the export reads is already the pinned one).
