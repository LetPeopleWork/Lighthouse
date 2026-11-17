# Architecture brief: story\-6650\-tables\-fit\-and\-show\-overflow

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Pinning (Open item resolved by DESIGN): the actions column is pinned with a custom sticky column inside DataGridBase on the community @mui/x-data-grid 9.17. A column whose fit kind is actions gets a Lighthouse class through the community cellClassName and headerClassName props, styled position sticky, right 0, an opaque cell background, a z-index above other cells and a left shadow line; it is always ordered last (the column-order merge forces it to the end, whatever a saved order says), is not resizable, and ignores any stored width. DataGrid gets a large columnBufferPx so every column is rendered (all shared grids have under twenty columns and rows are paged), because a virtualised-away cell cannot stick. Rejected: the Pro grid pinning (a paid per-developer licence and a licence key in an open-source app, for one feature); rejected: writing the community grid internal pinnedColumns state through the api ref (internal API, and it unlocks a Pro feature without its licence); rejected: moving Delete into a row menu (scope out). Checked against the cue (the right cue is drawn at the left edge of the pinned column, never under it), user-set widths (other columns still resize; the pinned one does not), column reorder (pinned stays last) and hide (hiding the actions column still works as today), phone width (pinned column plus the scrolling rest) and the stored grid state (shape unchanged).
```

#### D2

```text
Fit carrier: DataGridColumn gains an optional fit choice, ColumnFit = { kind: text, minWidth? } | { kind: content } | { kind: actions }. When a column declares none, it is derived: a column with flex is text, any other column is content. So S1 changes every DataGridBase table at once without editing them, and S3 and S4 only mark columns where the derivation is wrong. The single owner of the fit rule is a new pure module columnFit.ts (fitColumn(column, storedWidth) returns the MUI column props: flex, width, minWidth, resizable, cell and header class). DataGridBase calls it in its column memo and keeps no width rule of its own. Text: flex = column.flex or 1, minWidth = fit.minWidth or DEFAULT_TEXT_MIN_WIDTH 120 px, cell does not wrap and truncates. Content: fixed width, minWidth = that width, never flex, never ellipsised. Actions: content plus pinned. The fit itself is the community grid flex pass (leftover width shared among flex columns, never below minWidth); when the minima do not fit, the grid scroller scrolls.
```

#### D3

```text
Truncation: text cells render through a new TruncatedCell component in the DataGrid folder: a nowrap box with overflow hidden and text-overflow ellipsis, inside an MUI Tooltip that carries the full text. The full text is the column value (valueGetter or field value as a string); a renderCell output keeps its own element inside the box and the tooltip text comes from the value. The tooltip opens on hover and while the cell or an element inside it has keyboard focus (focusin and focusout on the box, plus the grid focused-cell state for the cell itself), and only when the text is cut (scrollWidth greater than clientWidth at open). The full text stays in the DOM, so assistive technology reads it whole. Content and actions cells keep the current centred cell layout and are never cut. Row height stays auto.
```

#### D4

```text
Stored widths: on a resize commit DataGridBase stores only the width of the column the person resized, merged into the stored widths. Today it stores every column width after any resize, which turns flex off for all columns and is one reason Delete drifts off screen. A stored text width is used as set (flex off for that column, minWidth kept); a stored content width is used but never below the content width; a stored actions width is ignored. Widths stored by earlier versions stay in place as the person set them; Reset in the toolbar (clearState, unchanged) clears them and returns to the fitted layout. The localStorage shape PersistedGridState and its sanitiser do not change.
```

#### D5

```text
Edge cue: a new pure function overflowEdges(scrollLeft, viewportWidth, contentWidth) in horizontalOverflow.ts returns a closed value none | left | right | both, with a one-pixel tolerance. A hook useHorizontalOverflow(apiRef) in the same module reads the grid root dimensions (apiRef getRootDimensions: content size and viewport inner size) and the scroll position, and recomputes on the grid events scrollPositionChange, resize, columnWidthChange and columnVisibilityModelChange. DataGridBase renders two decorative overlays (aria-hidden, pointer-events none) from that value: the left one at the left edge of the scroller, the right one at the left edge of the pinned actions column (or the right edge when there is none). The wrapper carries data-overflow with the same value, and a visually hidden text referenced from the grid with aria-describedby says that the table scrolls sideways and columns are hidden on the named side, so the cue is not shadow alone. Keyboard: the grid already scrolls a focused cell into view; on cellFocusIn DataGridBase additionally scrolls left by the overlap when the focused cell ends under the pinned actions column, because the community grid does not know a custom pinned column.
```

#### D6

```text
Phone width: the DataGridBase root box keeps width 100 percent and gets minWidth 0, so the grid scroller, not the page, scrolls sideways; text columns stop at their minimum widths, the cue shows and the pinned actions stay in view. A card-per-row layout is out of scope (Confirmed).
```

#### D7

```text
Loading and failure: confirmed no new look. The feature adds no fetch; each grid keeps its existing loading overlay and its callers keep their failure looks. The cue is computed from the rendered grid, so a loading grid with no rows shows no cue.
```

#### D8

```text
Proposed decisions confirmed by DESIGN: text columns shrink and truncate, number, date, state and action columns are protected (fit kind content or actions); the tooltip shows the full text on hover and keyboard focus and the cell keeps the full text for assistive technology; a stored width is kept and the rest fits around it, Reset returns to the fit, and stored widths that overflow scroll with the cue; the cue is on the side with hidden columns, decorative, not clickable; the cue is not the only signal (focus scrolls cells into view, aria description of the overflow, MUI grid roles kept); the behaviour applies to every DataGridBase user through the shared component, delivered by slice.
```

#### D9

```text
Open for the maintainer at the DELIVER sketch review (they change what a user sees): (1) minimum widths and protected columns per table; DESIGN proposes DEFAULT_TEXT_MIN_WIDTH 120 px and the report list Name 160 px, Owner 120 px; (2) shadow or fade as the cue style, and whether a visible text hint is added; (3) the wording of the screen-reader overflow description. The crafter uses the proposed values until the sketch decides.
```

#### D10

```text
Formal coverage: the fit and cue safety claims (content never cut, text never below its minimum, stored text width kept, no overflow when the minima fit, no cue when the table fits, no right cue at the end of the scroll range, a cue only on a side with hidden columns) are modelled in Quint, because they are state invariants over a finite abstraction of widths and scroll position. Quint covers them; TLA+ is not needed (no temporal claim) and Agda is not needed (no proof beyond the bounded model). Liveness is not applicable: the fit and the cue are pure functions of the current widths and scroll position recomputed on each grid event; nothing waits for eventual progress.
```

#### D11

```text
Layers: domain is not affected (no business rule). Application/presentation: columnFit (states: column fit kind text, content, actions with optional stored width; operation fitColumn; observation the MUI column props) and horizontalOverflow (state: scrollLeft, viewport width, content width; operation overflowEdges; observation none, left, right, both). Adapter: DataGridBase maps those onto the community DataGrid props and events and onto localStorage through usePersistedGridState; its failure translation is unchanged (storage errors are logged and the grid falls back to defaults). Infrastructure/recovery: none.
```

#### D12

```text
Context map: cross-cutting UI infrastructure; no bounded context, glossary term, domain event or UI surface changes, so ADR-270, ADR-271 and ARCHITECTURE.md section 4 stay unchanged.
```

#### D13

```text
Test substrate: jsdom has no layout, so the rules live in pure functions tested in .test.ts files (columnFit.test.ts, horizontalOverflow.test.ts, fast-check 4.10.2 available for the broad laws); the wiring is tested in DataGridBase component tests that stub apiRef getRootDimensions and fire grid events; the only real-layout check is one Playwright walking skeleton on the report list through the ReportsListPage page object (desktop width and 375 px), which proves the sticky column and the fit in a browser. No screenshot changes are planned; screenshots are a finalize decision. The Lighthouse test-writing skill wins over nWave timing and PBT density rules.
```

### Constraints

#### K1

```text
No new dependency: stay on community @mui/x-data-grid ^9.17.0 and the existing MUI Tooltip; no Pro or Premium package and no licence key.
```

#### K2

```text
The persisted grid state PersistedGridState in localStorage (key lighthouse:datagrid:<storageKey>:state) keeps its shape and sanitiser; old stored widths are honoured, never rewritten.
```

#### K3

```text
No API route, DTO or backend change; row actions keep their existing useRbac gating; no column, row action or column content is added, removed or reordered by a table.
```

#### K4

```text
Bounds: text minimum width defaults to 120 px (proposed, Open at the sketch); content and actions columns never below their declared width; widths are whole pixels; the authority gives no upper bound for a stored width and the design adds none (an over-wide stored width scrolls with the cue).
```

### Reuse

DataGridBase

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.tsx\:201

Extend

```text
The one shared grid every affected table uses; the cause (all widths persisted, cells wrap, no pinning, no cue) lives here, so the fit, pinning and cue wiring extend it rather than a wrapper per table.
```

DataGridColumn

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/types\.ts\:22

Extend

```text
Existing column carrier of every table; gains the optional fit choice so a table marks text, content or actions columns without a second column type.
```

withNewColumnsWhereDeclared

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.tsx\:94

Extend

```text
Owns the merge of saved and declared column order; it also places the pinned actions column last, so no second ordering rule exists.
```

usePersistedGridState

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/hooks\.ts\:143

Reuse

```text
Stores and sanitises columnWidths already; only what DataGridBase writes into it changes (the resized column, not every column).
```

useColumnWidths

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/hooks\.ts\:222

Reuse

```text
Holds the live width map; unchanged.
```

fitColumn

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/columnFit\.ts\:1

CreateNew

```text
No existing owner decides column flex, minWidth and protection from a column kind; DataGridBase mixes a persisted-width rule into its memo. A pure owner keeps the rule testable without layout (jsdom).
```

overflowEdges

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/horizontalOverflow\.ts\:1

CreateNew

```text
Nothing in the app detects horizontal overflow of a grid; a pure function of the scroll measures plus one hook owns it.
```

TruncatedCell

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/TruncatedCell\.tsx\:1

CreateNew

```text
Searched for an ellipsis-with-tooltip cell: none is shared; cells wrap today. One component used by DataGridBase for every text column.
```

Tooltip

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridToolbar\.tsx\:5

Reuse

```text
The MUI Tooltip the grid toolbar already uses gives the full text of a truncated cell.
```

DataGrid

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.tsx\:12

Reuse

```text
The community grid (@mui/x-data-grid 9.17) already shares leftover width among flex columns and respects minWidth, and offers cellClassName, headerClassName, columnBufferPx and the scroll and dimension API; the fit and the pinned column use those instead of a Lighthouse width solver or the Pro grid.
```

### Impact

None.

### Agreement

PersistedGridState in localStorage lighthouse\:datagrid\:\<storageKey\>\:state

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.tsx\:460

Producer

UnchangedCompatible

```text
Writes the same shape; after this change it writes only resized widths, a subset of what it wrote before.
```

PersistedGridState in localStorage lighthouse\:datagrid\:\<storageKey\>\:state

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/hooks\.ts\:143

Consumer

UnchangedCompatible

```text
Reads and sanitises the same shape; states stored by earlier versions load unchanged and are honoured as set by the person.
```

DataGridColumn column carrier

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/types\.ts\:22

Producer

UnchangedCompatible

```text
fit is optional and derived when absent, so every existing table compiles and keeps its columns.
```

### Not applicable

architectural_tests

```text
No dependency, boundary or owner law in authority is reachable by this change: it stays inside the presentation component DataGridBase and its callers; the hexagonal backend rules and the useRbac rule are not touched.
```

contract_tests

```text
Producer and consumer of the stored grid state and of DataGridColumn are in one deployable and change in the same commit; the primary oracles cover them.
```

### Boundaries

```text
DataGridBase props (rows, columns with optional fit, storageKey) as rendered by a table component, and the person interacting with the rendered grid (resize, scroll, keyboard focus, Reset)
```

#### Driven ports

```text
community @mui/x-data-grid DataGrid (props, apiRef events and root dimensions)
```

```text
browser localStorage through usePersistedGridState
```

```text
Table components depend on DataGridBase; DataGridBase depends on the pure modules columnFit and horizontalOverflow and on the community grid; the pure modules depend on nothing but types.
```

#### Failures

None.

### Observations

```text
Fitted column props per column: flex, width, minWidth, resizable, pinned class
```

FittedColumn

```text
the rendered pixel width, which the community grid computes in a browser only
```

```text
Overflow edges of a rendered grid
```

none \| left \| right \| both

```text
how many columns are hidden on a side
```

```text
In a browser at a given viewport width: bounding box of the Delete button of the first report row against the grid box
```

inside \| outside

```text
which column shrank to make room
```

### Equality

fitColumn

```text
Two fitted columns are equal when flex, width, minWidth, resizable and pinned flag agree; header text and renderers are passed through unchanged.
```

### Constructors

None.

### Laws

#### L1

```text
A content or actions column never gets a width or minWidth below its declared width, whatever stored width it has.
```

Invariant

Application

columnFit\.ts

##### Treatment

Broad

###### Generator

```text
declared width 40..400, stored width absent or 1..800, fit kind content or actions
```

one column per case

fast\-check default seed\, printed on failure

###### Must include

```text
stored width below declared width
```

None.

```text
fast-check integer shrinking
```

```text
minWidth and width of the fitted props compared with the declared width written out
```

```text
fitColumn takes a stored width below the declared width for a content column
```

##### Evidence

None.

#### L2

```text
A text column without a stored width is flex with minWidth equal to its declared or default minimum; with a stored width it is not flex and its width equals the stored width.
```

Invariant

Application

columnFit\.ts

##### Treatment

Broad

###### Generator

```text
minWidth absent or 40..300, flex absent or 1..3, stored width absent or 40..800
```

one column per case

fast\-check default seed

###### Must include

```text
stored width present
```

None.

```text
fast-check integer shrinking
```

```text
flex, width, minWidth of the fitted props
```

```text
stored width replaced by the fitted flex, or flex left on a resized column
```

##### Evidence

None.

#### L3

```text
overflowEdges is none exactly when contentWidth is within viewportWidth plus one pixel; right exactly when scrollLeft plus viewportWidth is more than one pixel short of contentWidth; left exactly when scrollLeft is more than one pixel.
```

Invariant

Application

horizontalOverflow\.ts

##### Treatment

Broad

###### Generator

```text
viewportWidth 0..2000, contentWidth 0..4000, scrollLeft 0..max(0, contentWidth - viewportWidth)
```

one measure triple per case

fast\-check default seed

###### Must include

```text
scrollLeft at 0, at the end of the range, contentWidth equal to viewportWidth
```

None.

```text
fast-check integer shrinking
```

```text
the closed value compared with the comparisons written out in the test
```

```text
right still reported at the end of the scroll range, or a cue on a table that fits
```

##### Evidence

None.

#### L4

```text
The pinned actions column is ordered last for any saved column order.
```

Invariant

Application

DataGridBase\.tsx

##### Treatment

Finite

###### Cases

```text
no saved order
```

```text
saved order with actions first
```

```text
saved order without the actions field
```

```text
finite set of order shapes
```

##### Evidence

None.

#### L5

```text
A resize commit stores only the resized column width merged into the stored widths.
```

Invariant

Adapter

DataGridBase\.tsx

##### Treatment

Finite

###### Cases

```text
first resize of Name
```

```text
second resize of Owner keeps Name
```

```text
Reset clears both
```

```text
finite interaction sequence
```

##### Evidence

None.

### Refuted

None.

### Claims

#### C1

```text
A number, date or action cell is never ellipsised or clipped
```

```text
a reader sees a cut number or cannot press Delete
```

columnFit\.ts

```text
DataGridColumn with fit content or actions (declared or derived)
```

fitColumn

DataGridBase

```text
cell shows its whole content; in the browser skeleton Delete is fully inside the grid box
```

```text
a table that renders its own text into a content column wider than the declared width; a caller passing raw GridColDef props
```

### States

Admitted

```text
text column narrower than its text: ellipsis plus tooltip
```

None.

Excluded

```text
content or actions column below its declared width
```

None.

Excluded

```text
actions column scrolled out of view
```

None.

Unknown

```text
declared width of a content column smaller than its real content (a long localised date or a large number)
```

acceptance designer and crafter per table in S3 and S4

### Models

Quint

docs\/feature\/story\-6650\-tables\-fit\-and\-show\-overflow\/columnFit\.qnt

##### Properties

Inv

Invariant

```text
content never cut, text never below minimum, stored text width kept, fits when minima fit, no cue when it fits, no right cue at the end, cue only with hidden columns
```

fitColumn and overflowEdges

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/columnFit\.ts and Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/horizontalOverflow\.ts

##### Atoms

State

cStored

PersistedGridState\.columnWidths

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/types\.ts

None.

State

scrollLeft

grid scroll position

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/horizontalOverflow\.ts

None.

Action

pick

grid event recompute

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.tsx

None.

##### Assumptions

```text
two text columns, one content column and one pinned actions column; widths 40..1200 px; the grid flex pass is abstracted as equal share then clamp to minimum
```

##### Mutants

###### N1

```text
MUTANT 1: a stored content width below the content width is used as is
```

###### Kills

None.

###### N2

```text
MUTANT 2: a stored text width is replaced by the fitted share
```

###### Kills

None.

###### N3

```text
MUTANT 3: the right cue stays at the end of the scroll range
```

###### Kills

None.

### Closure

```text
fit and cue invariants
```

Safety

Pass

DesignedNotBuilt

```text
Quint model columnFit.qnt: quint run 20000 samples and quint verify (Apalache, bounded) found no violation; three behavioural mutants each violated Inv in quint run
```

##### Evidence

None.

```text
a fitted grid with a pinned actions column
```

Inhabitation

Pass

DesignedNotBuilt

```text
any DataGridColumn list builds fitted props through fitColumn; the report list columns are an inhabitant
```

##### Evidence

None.

```text
Delete reachable on the report list
```

PublicReachability

Pass

DesignedNotBuilt

```text
ReportList columns -> fitColumn -> DataGridBase -> community DataGrid -> Reports tab of a Team; observed by the S1 Playwright skeleton
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Assumption: the community grid renders each row as a horizontal strip inside one scroller so position sticky works (negated by an MUI DOM change in an upgrade)
```

```text
Delete reachable by scrolling
```

```text
pinning lost until fixed
```

```text
Delete unreachable
```

```text
the S1 Playwright skeleton fails on an upgrade that breaks sticky
```

crafter

#### Y2

SinglePointOfFailure

```text
columnBufferPx large enough to render every column
```

```text
pinned cell exists
```

```text
a little more DOM per row
```

```text
actions cell virtualised away
```

```text
a table with very many columns pays render cost; all current tables have under twenty
```

crafter

#### Y3

Bypass

```text
Widths stored by earlier versions for every column
```

```text
the person widths
```

```text
the table overflows and shows the cue
```

```text
their widths silently replaced
```

```text
Reset returns to the fit; pinned actions keep Delete in view meanwhile
```

maintainer \(accepted by P5\)

#### Y4

Stressor

```text
Second set, walked over the finished design: right-to-left layout, browser zoom at 200 percent, a grid inside a dialog (WorkItemsDialog), rows with checkbox selection
```

```text
fit and pinned actions
```

```text
cue on the mirrored side is not designed (the app has no RTL locale)
```

```text
the checkbox column treated as text and shrunk
```

```text
checkbox selection column is MUI-owned and fixed width; zoom only lowers available width and is covered by the minima; dialogs use the same component
```

crafter

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/columnFit\.ts

Create

```text
pure owner of the fit rule (fitColumn, ColumnFit, DEFAULT_TEXT_MIN_WIDTH)
```

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/TruncatedCell\.tsx

Create

```text
ellipsis cell with full-text tooltip on hover and keyboard focus
```

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.tsx

Extend

```text
calls fitColumn, renders text cells through TruncatedCell, pins the actions column (sticky class, last in order, columnBufferPx), stores only the resized width
```

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/types\.ts

Extend

```text
DataGridColumn gains optional fit
```

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/index\.ts

Extend

```text
exports ColumnFit for table components
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportList\.tsx

Extend

```text
marks Name and Owner as text with their minimum widths, Created and Template as content, actions as actions
```

Lighthouse\.EndToEndTests\/tests\/specs\/reports\/ReportListTableFit\.spec\.ts

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/components/Common/DataGrid src/pages/Common/Reports/ReportList.test.tsx src/pages/Common/Reports/ReportsTab.test.tsx
pnpm --dir Lighthouse.EndToEndTests exec playwright test tests/specs/reports/ReportListTableFit.spec.ts
```

#### Supports

Lighthouse\.EndToEndTests\/tests\/models\/reports\/ReportsListPage\.ts

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/columnFit\.test\.ts

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/TruncatedCell\.test\.tsx

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.test\.tsx

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.defaults\.test\.tsx

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.paging\.test\.tsx

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/ColumnOrderDialog\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportList\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportsTab\.test\.tsx

#### Public oracle

```text
In a browser, a Team with several reports, one with a name longer than its column; the person opens the Reports tab at 1280 px wide, hovers the long name, then reloads with a stored Name width and presses Reset; then the same at 375 px wide and scrolls the table to the left end.
```

```text
At 1280 px the Delete button of every row lies inside the grid box with the scroller at scrollLeft 0, the long name ends with an ellipsis and the tooltip shows the full name, the Created date shows whole; a stored Name width is used as set after reload and Reset returns the fitted layout; at 375 px Delete is still inside the grid box at scrollLeft 0 and at the end.
```

```text
Delete outside the grid box at 1280 px or at 375 px, the full name not reachable by tooltip, a date with an ellipsis, or the stored width replaced on reload.
```

##### Promises

P1

P2

P3

P4

P5

P12

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

ReportList

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportList\.tsx\:259

Extend

```text
the report list columns are the walking-skeleton table; only fit marks change
```

DataGridBase

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/index\.ts\:1

Extend

```text
The DataGrid barrel; it also exports the ColumnFit type for the tables.
```

##### Impact

module

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/columnFit\.ts\:1

Changed

```text
new
```

component

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/TruncatedCell\.tsx\:1

Changed

```text
new
```

component

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.tsx\:201

Changed

```text
fit, pinning, truncation, resize persistence; every table using it changes layout at once
```

type

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/types\.ts\:22

Changed

```text
optional fit added; callers compile unchanged
```

export

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/index\.ts\:1

Changed

```text
exports ColumnFit
```

component

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportList\.tsx\:168

Changed

```text
fit marks
```

consumer

Lighthouse\.Frontend\/src\/components\/Common\/DataOverviewTable\/DataOverviewTable\.tsx\:106

UnchangedCompatible

```text
gets the derived fit from S1 (flex columns text, others content); its own marks come in S3
```

consumer

Lighthouse\.Frontend\/src\/components\/Common\/FeatureGrid\/FeatureGrid\.tsx\:1

UnchangedCompatible

```text
derived fit until S4; its tests are S4 supports
```

test

Lighthouse\.EndToEndTests\/tests\/models\/reports\/ReportsListPage\.ts\:22

Changed

```text
page object gains the grid box, a row Delete locator and the scroll position
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
The resize-persistence change and the fit call are the behaviour itself; no behaviour-preserving move makes them easier, and DataGridBase has no seam to extract first that the slice does not create anyway.
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

None.

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/horizontalOverflow\.ts

Create

```text
overflowEdges and useHorizontalOverflow
```

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.tsx

Extend

```text
renders the edge overlays, data-overflow, the aria description, minWidth 0 on the root, and the focus scroll past the pinned column
```

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.overflow\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/components/Common/DataGrid
pnpm --dir Lighthouse.EndToEndTests exec playwright test tests/specs/reports/ReportListTableFit.spec.ts
```

#### Supports

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/horizontalOverflow\.test\.ts

Lighthouse\.EndToEndTests\/tests\/specs\/reports\/ReportListTableFit\.spec\.ts

Lighthouse\.EndToEndTests\/tests\/models\/reports\/ReportsListPage\.ts

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.test\.tsx

#### Public oracle

```text
DataGridBase rendering the report list columns with the grid root dimensions stubbed to content 900 px in a 400 px viewport; the test fires scrollPositionChange at left 0, in the middle and at the end, then resizes the viewport to 1000 px; then focuses a cell that ends under the pinned column with the keyboard. In the browser skeleton at 375 px the person sees the table, not the page, scroll.
```

```text
data-overflow is right at 0, both in the middle, left at the end and none at 1000 px; the right overlay shows only when right or both and the left only when left or both; the grid aria description names the hidden side while there is one and is absent at none; the keyboard-focused cell is scrolled clear of the pinned column; at 375 px the document does not scroll sideways and the grid scroller does.
```

```text
a cue at the end of the range or on a table that fits, overflow shown only as a shadow with no text state, a focused cell left under the pinned column, or the page scrolling sideways.
```

##### Promises

P6

P7

P8

P11

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

None.

##### Impact

module

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/horizontalOverflow\.ts\:1

Changed

```text
new
```

component

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.tsx\:364

Changed

```text
cue overlays, aria description, root minWidth, focus scroll
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
Builds on the S1 structure; adds a hook and overlays with no existing code to move first.
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

None.

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Frontend\/src\/components\/Common\/DataOverviewTable\/DataOverviewTable\.tsx

Extend

```text
marks name and tags as text, remaining features, deliveries, reports and last updated as content, actions as actions; drops the isMobile width switches that the fit replaces
```

Lighthouse\.Frontend\/src\/components\/Common\/DataOverviewTable\/DataOverviewTable\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/components/Common/DataOverviewTable
```

#### Supports

Lighthouse\.Frontend\/src\/components\/Common\/DataOverviewTable\/DataOverviewTable\.reportsAndDeliveries\.test\.tsx

Lighthouse\.EndToEndTests\/tests\/models\/overview\/OverviewPage\.ts

#### Public oracle

```text
The Overview table rendered with Teams whose names are longer than their column; the person hovers a long name and the grid reports horizontal overflow.
```

```text
the name cell truncates with the full name in its tooltip, the actions column carries the pinned class and is the last column, number and date cells keep their whole content, and the overflow cue state is exposed as on the report list.
```

```text
the actions column not pinned or not last, a long name wrapping or lost, or no overflow state.
```

##### Promises

P9

P12

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

DataOverviewTable

Lighthouse\.Frontend\/src\/components\/Common\/DataOverviewTable\/DataOverviewTable\.tsx\:106

Extend

```text
the Overview table; only its column fit marks change
```

##### Impact

component

Lighthouse\.Frontend\/src\/components\/Common\/DataOverviewTable\/DataOverviewTable\.tsx\:106

Changed

```text
fit marks; isMobile width switches removed
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
Only column definitions change; nothing to move first.
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

None.

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

### S4

#### Targets

Lighthouse\.Frontend\/src\/components\/Common\/FeatureGrid\/FeatureGrid\.tsx

Extend

```text
marks each column text, content or actions where the derived fit is wrong
```

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.tsx

Extend

```text
marks each column text, content or actions where the derived fit is wrong
```

Lighthouse\.Frontend\/src\/components\/Common\/WorkItemsDialog\/WorkItemsDialog\.tsx

Extend

```text
marks each column text, content or actions where the derived fit is wrong
```

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/ArchivedFeatureGrid\.tsx

Extend

```text
marks each column text, content or actions where the derived fit is wrong
```

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.tsx

Extend

```text
marks each column text, content or actions where the derived fit is wrong
```

Lighthouse\.Frontend\/src\/pages\/Settings\/SystemInfo\/ThirdPartyPackagesSection\.tsx

Extend

```text
marks each column text, content or actions where the derived fit is wrong
```

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/RefinementGrid\.tsx

Extend

```text
marks each column text, content or actions where the derived fit is wrong
```

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/components/Common/FeatureGrid src/components/Common/FeatureListDataGrid src/components/Common/WorkItemsDialog src/pages/Portfolios/Detail/Components/DeliveryGrid src/pages/Settings/Connections src/pages/Settings/SystemInfo src/pages/Teams/Detail/Refinement
```

#### Supports

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.moveActions\.test\.tsx

Lighthouse\.Frontend\/src\/components\/Common\/FeatureGrid\/FeatureGrid\.test\.tsx

Lighthouse\.Frontend\/src\/components\/Common\/WorkItemsDialog\/WorkItemsDialog\.test\.tsx

Lighthouse\.Frontend\/src\/components\/Common\/WorkItemsDialog\/WorkItemsDialog\.layout\.test\.tsx

Lighthouse\.Frontend\/src\/components\/Common\/WorkItemsDialog\/WorkItemsDialog\.paging\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/ArchivedFeatureGrid\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.list\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Settings\/SystemInfo\/ThirdPartyPackagesSection\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/refinementColumns\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/RefinementView\.test\.tsx

#### Public oracle

```text
Each of the Feature, Feature list, Work Item dialog, archived Feature, Connections, third-party packages and Refinement tables rendered with a long text value and its actions column, if any.
```

```text
in each, the long text truncates with its full text in a tooltip, number, date and state cells show whole, the actions column is pinned and last, and the overflow state is exposed when the grid overflows.
```

```text
any of these tables wraps or loses a long text, cuts a number or date, or leaves its actions column unpinned.
```

##### Promises

P10

P12

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

DataGridBase

Lighthouse\.Frontend\/src\/components\/Common\/FeatureGrid\/FeatureGrid\.tsx\:237

Extend

```text
The table keeps its DataGridBase use and its columns; only the fit marks of its columns are added where the derived fit is wrong.
```

DataGridBase

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.tsx\:134

Extend

```text
The table keeps its DataGridBase use and its columns; only the fit marks of its columns are added where the derived fit is wrong.
```

DataGridBase

Lighthouse\.Frontend\/src\/components\/Common\/WorkItemsDialog\/WorkItemsDialog\.tsx\:303

Extend

```text
The table keeps its DataGridBase use and its columns; only the fit marks of its columns are added where the derived fit is wrong.
```

DataGridBase

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/ArchivedFeatureGrid\.tsx\:67

Extend

```text
The table keeps its DataGridBase use and its columns; only the fit marks of its columns are added where the derived fit is wrong.
```

DataGridBase

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.tsx\:224

Extend

```text
The table keeps its DataGridBase use and its columns; only the fit marks of its columns are added where the derived fit is wrong.
```

DataGridBase

Lighthouse\.Frontend\/src\/pages\/Settings\/SystemInfo\/ThirdPartyPackagesSection\.tsx\:161

Extend

```text
The table keeps its DataGridBase use and its columns; only the fit marks of its columns are added where the derived fit is wrong.
```

DataGridBase

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/RefinementGrid\.tsx\:93

Extend

```text
The table keeps its DataGridBase use and its columns; only the fit marks of its columns are added where the derived fit is wrong.
```

##### Impact

component

Lighthouse\.Frontend\/src\/components\/Common\/FeatureGrid\/FeatureGrid\.tsx\:1

Changed

```text
column fit marks only
```

component

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.tsx\:1

Changed

```text
column fit marks only
```

component

Lighthouse\.Frontend\/src\/components\/Common\/WorkItemsDialog\/WorkItemsDialog\.tsx\:1

Changed

```text
column fit marks only
```

component

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/ArchivedFeatureGrid\.tsx\:1

Changed

```text
column fit marks only
```

component

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.tsx\:1

Changed

```text
column fit marks only
```

component

Lighthouse\.Frontend\/src\/pages\/Settings\/SystemInfo\/ThirdPartyPackagesSection\.tsx\:1

Changed

```text
column fit marks only
```

component

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/RefinementGrid\.tsx\:1

Changed

```text
column fit marks only
```

##### Agreement

None.

##### Not applicable

prefactoring

```text
Only column definitions change per table; nothing to move first.
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

None.

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

## ADRs

None.
