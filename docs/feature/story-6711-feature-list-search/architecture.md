# Architecture brief: story\-6711\-feature\-list\-search

## Paradigm

functional

## Shared records

### Decisions

#### D1

```text
Paradigm: the repository frontend is hooks and pure functions inside an object-oriented project; this feature adds no class. Carriers: search text is a useState in DataGridBase (per mount, never persisted, empty on every open); the narrowing itself is MUI filterModel.quickFilterValues, set with apiRef.setQuickFilterValues(quickSearchValues(text)); the pure core is the new DataGridBase-adjacent module quickSearch.ts (quickSearchValues and the per-column quick filter). Effects stay in DataGridBase and the toolbar; the core calls neither.
```

#### D2

```text
Matching rule (D2, as the cell reads): use the MUI quick filter, not a custom row pre-filter. The whole trimmed text is ONE literal value, case-insensitive substring, matched against each shown column text. Shown text = the column valueFormatter result when the column has one, else its raw value when that is a string or number. Objects, arrays, undefined and booleans are never matched, so typing obj never matches every row via [object Object] and true never matches Active Work. MUI already excludes hidden columns and gives no quick filter to boolean and actions columns. Columns with no text value (Progress, Forecasted Start, Forecasted Completion, Likelihood, Warnings, Active Work) are not searchable; this is the declared residue. Why the quick filter: it already narrows together with column filters (P7) and opens the first page on a changed value (P8, MUI useGridPaginationModel), which a custom pre-filter would have to rebuild as OpensFirstPageOnSearch does. Rejected: matching every column raw value (wrong for count, boolean and object columns), per-column getApplyQuickFilterFn overrides on each page column (many sites, easy to forget), one custom filter over rendered text (renderCell is a React node, no text).
```

#### D3

```text
Name text: the shown Name cell text is getWorkItemName(name, referenceId) (utils/featureName.ts is the only owner; unparented names and GUID ids show the name alone). createNameColumn exposes it as valueFormatter first (so search finds the ID) and then, in the Name filter slice, as valueGetter plus a sortComparator over row.name using gridStringOrNumberComparator, so column sort and any getCellValue consumer keep seeing the name as today while the column filter sees the shown text. The Delivery Work Items tab name column is a second copy of the same cell and adopts the shared factory.
```

#### D4

```text
UI placement (agreed sketch): DataGridToolbar gets an optional search prop and renders the new GridSearchBox at the left of the toolbar row (search icon, placeholder, clear button); the other toolbar icons stay right-aligned. DataGridBase takes an optional search prop (placeholder, later the no-match copy) and renders no box without it. FeatureListDataGrid is the only caller that passes it, so Refinement, Archived, FeatureGrid, WorkItemsDialog, ReportList, ConnectionsSettings, ThirdPartyPackages and DataOverviewTable gain nothing. Copy uses the configured term: placeholder Search ${features term}; no match: No ${features term} match "${text}". (Confirms the Proposed placeholder/no-match decision; DELIVER sketch already agreed.) Reuse verdict: FilterBar (full-width own row, Ctrl+F, portfolio/team wording) and the ThirdPartyPackages TextField (pre-filters rows, own page-reset hook) are not the target; MUI TextField is reused inside a new small GridSearchBox.
```

#### D5

```text
Loading and failure (Open decision F47, resolved): the box lives inside the grid, so it is shown whenever the grid is mounted and stays usable while data loads (it narrows what is loaded; the existing loading overlay or dimming shows). Wherever the page already replaces the grid (lone spinner with nothing to dim, the could-not-load look, the Delivery tab Loading and no-Features texts) there is no box and those looks stay exactly as they are. No new loading or failure look is added. The no-Features message is untouched and distinct from the no-match message because the grid shows noRowsLabel for an empty list and noResultsOverlayLabel for a narrowed-to-nothing list.
```

#### D6

```text
Checker map: No model for the search narrowing: it is a stateless pure row filter over an already loaded list; there is no shared state, multi-step protocol, retry, crash or concurrency window (the only asynchrony is MUI zero-delay debounced page reset, which cannot change which rows match). Quint 0.33.1 is installed but no claim needs it. The laws are checked by native property tests (fast-check 4.10.3, declared in package.json) on the pure core plus component examples; nothing is claimed formally verified. Liveness: no liveness claim, the filter is synchronous and always enabled. MUI behaviours relied on were read in the installed @mui/x-data-grid 9.18.0 source (quick filter reads getRowFormattedValue; excludes hidden columns; boolean and actions columns have a null quick filter; filterModel change navigates to page 0; localeText noResultsOverlayLabel; apiRef.setQuickFilterValues) and are pinned by the component oracles.
```

#### D7

```text
Cross-layer algebra. Domain (pure core, quickSearch.ts): states = query text; operations = quickSearchValues(text) -> [] or [trimmed text], shownTextQuickFilter(value) -> null or predicate over one cell; observation = which rows remain. Application (FeatureListDataGrid/DataGridBase): passes the search prop, owns the text state, calls setQuickFilterValues; outcomes = rows narrowed, no-match message, page 0. Adapter (MUI DataGrid): applies the quick filter together with column filters. Infrastructure and recovery: none, nothing is stored or fetched; a reload or remount returns an empty box, which is the specified behaviour.
```

#### D8

```text
Test substrate: Vitest + React Testing Library under Lighthouse.Frontend, driving port = the rendered FeatureListDataGrid (and the four page components for the every-list slice) with the shared grid kit mock of @mui/x-data-grid (tests/SharedGridFitKit.tsx), pure laws on quickSearch.ts as .test.ts with fast-check. Runner argv: pnpm --dir Lighthouse.Frontend exec cross-env TZ=Europe/Zurich vitest run <file>. Default per-test and suite budgets apply, fixtures are 3 to 5 Features, the page-reset test uses PAGE_SIZE + 1 rows; no new E2E: search is client-side over a loaded list and no new user flow is created, so the existing component layer is the lowest layer that proves it. Prerequisite for the host: the worktree has no node_modules; run pnpm install in Lighthouse.Frontend before RED.
```

#### D9

```text
No backend, API, DTO, RBAC, client, usage-data or persistence change; export and clipboard keep copying every row in sort order, unaffected by the search exactly as a column filter does today.
```

### Constraints

#### K1

```text
Input bounds: the search text has no authority-given length limit and none is added; it is treated as a literal (regex characters escaped), trimmed at both ends, a whitespace-only box is the same as an empty box; case-insensitive; part of a word is enough; no multi-word tokenising (the whole text is one substring). Search text is never persisted and column filters, sort, layout persistence are unchanged.
```

#### K2

```text
Scope: only FeatureListDataGrid (Features page, Portfolio Features tab, Team Features tab, Delivery Work Items tab). A Delivery Work Items tab without Features keeps its own text and shows no box. Column filter on Parent is unchanged.
```

#### K3

```text
Deviation recorded: Name column filter now also matches the ID in the other lists that reuse createNameColumn (Refinement grid): the shown text is what is filtered, consistent with the cell. No other behaviour of those grids changes (their sort order stays by name).
```

#### K4

```text
Terminology: copy uses useTerminology(TERMINOLOGY_KEYS.FEATURES), never Epic or Story. No nWave scenario tags in code. Move menu neighbours are still computed from the hide-completed list, not the searched list, exactly as for a column filter today; not changed here.
```

#### K5

```text
Search box on the full feature list only (four pages named in scope); pickers, Refinement and Archived grids excluded and suggested for later.
```

#### K6

```text
Search matches every visible column as the cell value reads; named fields (reference ID, name, parent ID and name) must be findable.
```

#### K7

```text
Name column filter also matches the Feature reference ID.
```

#### K8

```text
Search text is not remembered; box is empty each time a page opens.
```

#### K9

```text
Fact: the Parent column filter already matches both ID and name (its value is the same 'ID: name' text the cell shows); no change.
```

### Reuse

None.

### Impact

None.

### Agreement

None.

### Not applicable

None.

### Boundaries

```text
Rendered FeatureListDataGrid props (features, columns, storageKey, status, emptyStateMessage) as mounted by FeaturesView, PortfolioFeatureList, TeamFeatureList and WorkItemsTab; user stimulus = typing in the search role textbox
```

#### Driven ports

```text
None (no backend call). Inward only: @mui/x-data-grid apiRef.setQuickFilterValues and localeText
```

```text
pages -> FeatureListDataGrid -> DataGridBase -> DataGridToolbar -> GridSearchBox; DataGridBase -> quickSearch.ts (pure, imports nothing from React or MUI components); columns.tsx -> utils/featureName.ts. DataGrid knows nothing of Features; the term strings come in through the search prop.
```

#### Failures

None.

### Observations

```text
Rows listed by the grid after a search text is typed: the row set shown, and the grid overlay text when no row remains
```

rows shown \(set of Feature ids\, paged\) plus overlay text

```text
Order of rows (sort owns it); which column matched; a column with no text value (Progress, forecasts, Likelihood, Warnings, Active Work) matching
```

### Equality

rows shown after a search

```text
Two searches are equal when the same set of rows is shown on the first page and the same overlay text, whatever mix of case, surrounding spaces or typing history produced the text.
```

### Constructors

quickSearchValues

\(text\: string\) \=\> string\[\]

shownTextQuickFilter

\(value\: string\) \=\> null \| \(\(\_\, row\, column\, apiRef\) \=\> boolean\)

### Laws

#### L1

```text
Inhabitation and case: for any shown cell text and any substring of it, in any letter case, the shown text matches the query; Reference-ID and parent-text searches are instances.
```

Invariant

Domain

quickSearch\.ts

##### Treatment

Broad

###### Generator

```text
shown text: strings of 0..40 chars over letters, digits, space and regex specials ( ) [ ] . * + ? ^ $ \ | : - ; query: a random slice of it with random per-character case flips
```

numRuns 100\; text length 40

fixed seed in the test\, replayed from the printed fast\-check seed

###### Must include

```text
regex specials; empty slice; whole text; an ID-like PROJ-12 prefix
```

single call\, no nesting

```text
fast-check built-in shrinking of the string and slice bounds
```

```text
Reference: shown.toLowerCase().includes(query.toLowerCase()), not regex
```

```text
An implementation that does not escape regex specials, is case-sensitive, or tests the raw value instead of the shown text fails on a generated special character or flipped case.
```

##### Evidence

None.

#### L2

```text
Safety: a cell whose shown value is an object, array, undefined, null or boolean never matches any non-empty query (typing obj or true narrows nothing by those cells).
```

Invariant

Domain

quickSearch\.ts

##### Treatment

Finite

###### Cases

```text
object value with query obj -> no match
```

```text
array of objects with query object -> no match
```

```text
true/false with query true -> no match
```

```text
undefined/null -> no match
```

```text
number 12 with query 12 -> match
```

```text
Finite closed set of value kinds
```

##### Evidence

None.

#### L3

```text
Monotone narrowing and empty identity: extending the query never adds rows; an empty or whitespace-only query yields no quick filter value and so shows every row.
```

Algebraic

Domain

quickSearch\.ts

##### Treatment

Broad

###### Generator

```text
rows: 0..8 shown strings as in the first law; query: random string then a random extension
```

numRuns 100\; 8 rows

fixed seed\, replayable

###### Must include

```text
whitespace-only query; extension to empty result
```

single call

```text
fast-check built-in
```

```text
Reference filter by lowercase includes over the same rows
```

```text
A matcher that trims differently per call or treats whitespace-only as a literal fails the identity law.
```

##### Evidence

None.

#### L4

```text
Combination and page: search and column filter both apply (AND), and a changed search text opens page 0; the box clear restores the unfiltered row set.
```

Invariant

Application

DataGridBase\.tsx

##### Treatment

Finite

###### Cases

```text
column filter set + search: only rows meeting both
```

```text
page 2 + new search: first page of results
```

```text
clear box: every row shown again
```

```text
Behaviour of the MUI adapter, a few finite stimuli through the rendered port
```

##### Evidence

None.

### Refuted

None.

### Claims

#### C1

```text
A search text can only narrow rows by shown cell text and never by an object, count or boolean value
```

```text
Typing a few letters lists unrelated Features or hides the one the user wants
```

quickSearch\.ts

```text
search text string; cell formatted value of any kind
```

quickSearchValues

DataGridBase\.tsx apiRef\.setQuickFilterValues

```text
rows listed by FeatureListDataGrid
```

```text
A column that sets its own getApplyQuickFilterFn in a caller; MUI own quick filter UI; closure checked by reading, enforced only by the oracles (TypeScript does not exclude it)
```

### States

Admitted

```text
Any text string typed in the box, including regex characters and surrounding spaces
```

DataGridBase\.tsx

Excluded

```text
A quick filter value that is empty or whitespace-only; a cell value that is an object matching a query
```

quickSearch\.ts

Unknown

```text
Searchability of forecast, progress and likelihood cell text (not text values today)
```

product owner

### Models

None.

### Closure

```text
quick filter matches shown text only
```

Safety

Pass

DesignedNotBuilt

```text
Object, array, boolean and undefined values return no match in the pure core; MUI source read for hidden, boolean and actions columns. Type strength: string-typed props only; invalid cell kinds are representable and excluded at the predicate, not by types.
```

##### Evidence

None.

```text
search by reference ID, name, parent
```

Inhabitation

Pass

DesignedNotBuilt

```text
Name valueFormatter returns getWorkItemName; Parent valueGetter already returns the shown text; both reach quick filter via getRowFormattedValue
```

##### Evidence

None.

```text
typing in the toolbar box narrows the list on all four pages
```

PublicReachability

Pass

DesignedNotBuilt

```text
input: user typing -> GridSearchBox onChange -> DataGridBase state -> setQuickFilterValues -> MUI rows -> FeatureListDataGrid render; FeatureListDataGrid is mounted by FeaturesView:68, PortfolioFeatureList:97, TeamFeatureList:118, WorkItemsTab:50
```

##### Evidence

None.

```text
search narrowing
```

Liveness

Pass

Indeterminate

```text
No liveness claim: the filter is a synchronous total function over loaded rows, no step that can wait or be starved
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Assumption negated: a column value equals the shown text (it does not for object, count and boolean columns)
```

```text
Search finds what the reader sees in text columns
```

```text
Columns with no text value are simply not searchable
```

```text
A search matching [object Object], a count or the word true
```

```text
Forecast dates, progress and likelihood text cannot be searched; a later slice may add a shown-text valueFormatter per column
```

quickSearch\.ts \/ each page column definition

#### Y2

Stressor

```text
Assumption negated: MUI keeps its quick filter semantics across upgrades (@mui/x-data-grid ^9.18)
```

```text
AND with column filters, page 0 on change, hidden columns excluded
```

```text
An upgrade changes internals but not these
```

```text
Silent loss of page reset or of column-filter combination
```

```text
Pinned only by the component oracles; a major MUI upgrade must rerun them
```

DataGridBase\.tsx

#### Y3

Bypass

```text
Second writer of the quick filter: a user opening MUI own filter UI or another caller of apiRef.setQuickFilterValues
```

```text
One writer (DataGridBase search state)
```

```text
None: DataGridBase shows no MUI toolbar quick filter
```

```text
A second box or stale text that disagrees with the filter model
```

```text
A future grid enabling MUI GridToolbarQuickFilter would fight the same filter model; not reachable today
```

DataGridBase\.tsx

#### Y4

SinglePointOfFailure

```text
Name text derives from one function getWorkItemName
```

```text
Cell, search and column filter agree on the shown Name text
```

```text
Future wording change in that function reaches all three at once
```

```text
A copy of the Name cell that formats differently (the Delivery tab copy)
```

```text
Removed in the every-list slice by adopting the factory
```

utils\/featureName\.ts

#### Y5

Stressor

```text
Second stressor set (walk of the finished design): very long list of 500 Features
```

```text
Typing stays responsive
```

```text
One filter pass per keystroke over loaded rows
```

```text
Network call or re-fetch on typing
```

```text
Per-keystroke filtering of a few hundred rows is walked, not measured; MUI debounces nothing here
```

DataGridBase\.tsx

#### Y6

Stressor

```text
Second stressor set: the list reloads or Hide Completed toggles while text is typed
```

```text
Text stays in the box and keeps narrowing the new rows
```

```text
Rows change under the text
```

```text
Box showing text that is not applied
```

```text
Page failure look unmounts the grid, so text is lost on retry; accepted, not persisted by decision
```

FeatureListDataGrid\.tsx

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/quickSearch\.ts

Create

```text
Pure core: quickSearchValues(text) and shownTextQuickFilter; one owner of the matching rule
```

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/GridSearchBox\.tsx

Create

```text
The search text box with search icon and clear button, one component per file
```

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridToolbar\.tsx

Extend

```text
Renders GridSearchBox at the left when a search prop is given
```

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.tsx

Extend

```text
Holds the search text state, calls setQuickFilterValues, gives every plain text column the shown-text quick filter
```

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/types\.ts

Extend

```text
Adds the optional search prop to DataGridBaseProps and DataGridToolbarProps
```

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.tsx

Extend

```text
Passes the search prop with the configured Features term
```

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/columns\.tsx

Extend

```text
createNameColumn gets valueFormatter = getWorkItemName(name, referenceId); new exported projectNamesText(feature) helper
```

Lighthouse\.Frontend\/src\/pages\/Features\/FeaturesView\.tsx

Extend

```text
Projects column gets valueFormatter = projectNamesText so Portfolio names are searchable on the Features page
```

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.search\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec cross-env TZ=Europe/Zurich vitest run --reporter=json src/components/Common/FeatureListDataGrid/FeatureListDataGrid.search.test.tsx
pnpm --dir Lighthouse.Frontend exec cross-env TZ=Europe/Zurich vitest run --reporter=json src/components/Common/DataGrid/quickSearch.test.ts src/components/Common/DataGrid/DataGridBase.search.test.tsx
```

#### Supports

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/quickSearch\.test\.ts

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.search\.test\.tsx

None.

None.

#### Public oracle

```text
Render FeatureListDataGrid (Name and Parent columns, 3 Features, one PROJ-12: Checkout under parent PROJ-3: Shop), type proj-12 into the textbox named Search Features, then type checkout, then clear the box
```

```text
Typing proj-12 lists only rows whose shown text contains it (PROJ-12: Checkout); checkout finds the Feature through its Parent text PROJ-3: Checkout too; clearing shows all three rows again; the box sits in the toolbar row before the icons
```

```text
Name column without valueFormatter (search by ID misses), a case-sensitive or regex-unescaped matcher, or an object column matching obj makes the stated rows differ
```

##### Promises

P1

P3

P4

P6

#### Prefactoring

```text
Add no behaviour first: nothing to move. Prefactoring decision per touched file: DataGridBase.tsx one component plus helpers, no several types; DataGridToolbar.tsx one component; FeatureListDataGrid.tsx one component; FeaturesView.tsx one component; columns.tsx factories with one local row type; types.ts holds several distinct interfaces imported across the DataGrid folder and its callers, so no split in this slice (a split would rewrite every importer without adding the observation) - the new optional props go in as they are
```

```text
Every existing DataGridBase, toolbar, columns and FeatureListDataGrid assertion unchanged
```

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.test\.tsx

##### Verification

```text
pnpm --dir Lighthouse.Frontend exec cross-env TZ=Europe/Zurich vitest run --reporter=json src/components/Common/DataGrid/DataGridBase.test.tsx src/components/Common/DataGrid/DataGridToolbar.test.tsx src/components/Common/FeatureListDataGrid/FeatureListDataGrid.test.tsx src/components/Common/FeatureListDataGrid/columns.test.tsx
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

DataGridBase

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.tsx\:470

Extend

```text
Owns the apiRef and the grid; the only place that can call setQuickFilterValues; an opt-in prop leaves the eight other callers untouched
```

DataGridToolbar

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridToolbar\.tsx\:175

Extend

```text
Already the toolbar row where the agreed sketch puts the box; customActions sits to the right of it and is not the place for a left-aligned box
```

GridSearchBox

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/GridSearchBox\.tsx\:1

CreateNew

```text
No candidate fits: FilterBar is a full-width own row with Ctrl+F and portfolio/team wording (FilterBar.tsx:22), the ThirdPartyPackages TextField (ThirdPartyPackagesSection.tsx:154) pre-filters rows outside the grid; a small component wrapping MUI TextField is the simplest owner of box + clear button
```

quickSearch

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/quickSearch\.ts\:1

CreateNew

```text
No existing owner decides which cell text a query matches; getGridStringQuickFilterFn in MUI would match [object Object] for object values. SEMANTIC duplicate check: ThirdPartyPackagesSection.tsx:99 lowercases and includes over its own rows, it is a different list and not a shared rule
```

createNameColumn

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/columns\.tsx\:39

Extend

```text
The shared Name column; its shown text getWorkItemName is already the cell text, so exposing it as valueFormatter makes search agree with the cell
```

getWorkItemName

Lighthouse\.Frontend\/src\/utils\/featureName\.ts\:4

Reuse

```text
Sole owner of the shown Name text; consumed, not changed
```

parentWorkItemText

Lighthouse\.Frontend\/src\/components\/Common\/ParentWorkItemCell\/ParentWorkItemCell\.tsx\:14

Reuse

```text
Parent column valueGetter already returns the shown text, so search finds the parent ID and name with no change
```

DataGridBaseProps

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/types\.ts\:57

Extend

```text
Owns the grid props; optional search prop added here
```

FeatureListDataGrid

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.tsx\:29

Extend

```text
Single caller that opts in; owns the configured term
```

FeaturesView

Lighthouse\.Frontend\/src\/pages\/Features\/FeaturesView\.tsx\:18

Extend

```text
Owns the projects column on the Features page
```

##### Impact

component

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.tsx\:470

Changed

```text
New optional search prop; without it behaviour is identical
```

component

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridToolbar\.tsx\:175

Changed

```text
New optional search prop rendered left of the icons
```

type

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/types\.ts\:57

Changed

```text
Optional fields only, every existing literal still compiles
```

page

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.tsx\:134

Changed

```text
Passes search to DataGridBase; all four pages inherit it
```

column\-factory

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/columns\.tsx\:39

Changed

```text
Name column gains valueFormatter; displayed cell unchanged; tooltip fullText now equals the cell text
```

page

Lighthouse\.Frontend\/src\/pages\/Features\/FeaturesView\.tsx\:45

Changed

```text
Projects column gains valueFormatter
```

consumer

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportList\.tsx\:307

UnchangedCompatible

```text
Passes no search prop; no box, no quick filter
```

consumer

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.tsx\:231

UnchangedCompatible

```text
Passes no search prop
```

consumer

Lighthouse\.Frontend\/src\/pages\/Settings\/SystemInfo\/ThirdPartyPackagesSection\.tsx\:161

UnchangedCompatible

```text
Keeps its own TextField above the grid; passes no search prop
```

consumer

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/RefinementGrid\.tsx\:93

UnchangedCompatible

```text
Out of scope; passes no search prop
```

consumer

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/ArchivedFeatureGrid\.tsx\:67

UnchangedCompatible

```text
Out of scope; passes no search prop
```

consumer

Lighthouse\.Frontend\/src\/components\/Common\/FeatureGrid\/FeatureGrid\.tsx\:237

UnchangedCompatible

```text
Delivery dialog picker, out of scope; passes no search prop
```

consumer

Lighthouse\.Frontend\/src\/components\/Common\/WorkItemsDialog\/WorkItemsDialog\.tsx\:304

UnchangedCompatible

```text
Passes no search prop
```

consumer

Lighthouse\.Frontend\/src\/components\/Common\/DataOverviewTable\/DataOverviewTable\.tsx\:355

UnchangedCompatible

```text
Passes no search prop
```

test

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridToolbar\.test\.tsx\:69

UnchangedCompatible

```text
Existing toolbar tests pass no search prop
```

test

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/columns\.test\.tsx\:1

UnchangedCompatible

```text
Name column render assertions keep reading the rendered cell
```

build\-metadata

Lighthouse\.Frontend\/package\.json\:35

UnchangedCompatible

```text
No new dependency: @mui/x-data-grid ^9.18.0, fast-check 4.10.3 already declared
```

new\-module

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/quickSearch\.ts\:1

Changed

```text
New pure module, imported only by DataGridBase
```

new\-module

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/GridSearchBox\.tsx\:1

Changed

```text
New component, imported only by DataGridToolbar
```

##### Agreement

None.

##### Not applicable

agreement

```text
The search prop is passed between FeatureListDataGrid and DataGridBase in one deployable and one commit; no persisted or wire shape changes
```

contract_tests

```text
Producer and consumer of the new props change together in one bundle; the primary oracle covers the real producer through the real consumer
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

###### T1

```text
The search box is opt-in: DataGridBase and DataGridToolbar render no search textbox unless a search prop is given, so the grids outside the four pages gain none
```

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.search\.test\.tsx

```text
pnpm --dir Lighthouse.Frontend exec cross-env TZ=Europe/Zurich vitest run --reporter=json src/components/Common/DataGrid/DataGridBase.search.test.tsx
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.tsx

Extend

```text
search.noMatchMessage(text) becomes localeText.noResultsOverlayLabel while the text is non-empty
```

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/types\.ts

Extend

```text
search prop gains noMatchMessage: (text: string) => string
```

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.tsx

Extend

```text
Supplies the No Features match copy from the configured term
```

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.searchEdges\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec cross-env TZ=Europe/Zurich vitest run --reporter=json src/components/Common/FeatureListDataGrid/FeatureListDataGrid.searchEdges.test.tsx
```

#### Supports

None.

None.

None.

#### Public oracle

```text
Render FeatureListDataGrid with PAGE_SIZE + 1 Features and a state column filter set, go to page 2, type a text that matches nothing (zzz), then a text matching some; also render with an empty features array
```

```text
Typing narrows together with the column filter (only rows meeting both); typing from page 2 shows the first page of results; zzz shows No Features match "zzz". in words different from the emptyStateMessage shown for the empty list
```

```text
A pre-filter that keeps the open page, an OR instead of AND with the column filter, or reusing the empty-list message for no match makes the observed rows or text differ
```

##### Promises

P5

P7

P8

#### Prefactoring

```text
None: no move. Per touched file, DataGridBase.tsx, types.ts and FeatureListDataGrid.tsx are decided in S1 and nothing in them blocks this change; no preserving move
```

```text
S1 behaviour and the existing no-rows message
```

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.test\.tsx

##### Verification

```text
pnpm --dir Lighthouse.Frontend exec cross-env TZ=Europe/Zurich vitest run --reporter=json src/components/Common/DataGrid/DataGridBase.test.tsx src/components/Common/FeatureListDataGrid/FeatureListDataGrid.test.tsx
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

noRowsLabel

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.tsx\:728

Extend

```text
localeText already carries the empty-list message; MUI shows noResultsOverlayLabel for a list narrowed to nothing, keeping the two messages distinct with no new overlay slot
```

DataGridBaseProps

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/types\.ts\:57

Extend

```text
search shape gains noMatchMessage
```

FeatureListDataGrid

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.tsx\:29

Extend

```text
Supplies the no-match copy from the configured term
```

##### Impact

component

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.tsx\:728

Changed

```text
noResultsOverlayLabel set only when a search prop is given and its text is non-empty; otherwise MUI default stays
```

type

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/types\.ts\:57

Changed

```text
Extends the optional search shape; no existing literal affected
```

page

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.tsx\:134

Changed

```text
Passes noMatchMessage built from the Features term
```

##### Agreement

None.

##### Not applicable

agreement

```text
Same deployable, same commit; no shared persisted or wire contract
```

contract_tests

```text
Props between two modules of one bundle; the primary oracle runs the real producer through the real consumer
```

architectural_tests

```text
No dependency, boundary or ownership law is reachable beyond the opt-in law already recorded in S1
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

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFeatureRow\.tsx

Extend

```text
Delivery name column adopts createNameColumn (minWidth and no fixed width kept); owningTeam gets a shown-text valueFormatter sharing the OwningTeams rule
```

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamFeatureList\.tsx

Extend

```text
Projects column gets valueFormatter = projectNamesText
```

Lighthouse\.Frontend\/src\/pages\/Common\/featureListSearch\.everyList\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec cross-env TZ=Europe/Zurich vitest run --reporter=json src/pages/Common/featureListSearch.everyList.test.tsx
```

#### Supports

None.

None.

None.

#### Public oracle

```text
Mount PortfolioFeatureList, TeamFeatureList and the Delivery WorkItemsTab with a Feature of reference PROJ-12 and type proj-12 into each Search Features box
```

```text
Each of the three shows the same box above its list and lists the PROJ-12 Feature; typing the Team name on the Delivery tab finds Features that team works on
```

```text
Delivery tab keeping its own name column (name only) misses PROJ-12; a page not mounting FeatureListDataGrid shows no box
```

##### Promises

P9

#### Prefactoring

```text
None: no move. Per touched file: deliveryFeatureRow.tsx holds several components (DeliveryRowActions, SelectionModeMarker, OwningTeams) that are not variants of one sum type; splitting them is not needed to make this change easier and would widen the diff of an unrelated delivery file, so no split here and OwningTeams stays in place; TeamFeatureList.tsx holds one component
```

```text
deliveryFeatureRow.test.tsx assertions on the Name cell, Team cell and column order
```

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFeatureRow\.test\.tsx

##### Verification

```text
pnpm --dir Lighthouse.Frontend exec cross-env TZ=Europe/Zurich vitest run --reporter=json src/pages/Portfolios/Detail/Components/DeliveryGrid/deliveryFeatureRow.test.tsx
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

createNameColumn

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/columns\.tsx\:39

Reuse

```text
The Delivery tab name column (deliveryFeatureRow.tsx:200) is a second copy of the same cell with the same getWorkItemName; replacing it gives search by ID there with one owner of the Name text
```

OwningTeams

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFeatureRow\.tsx\:155

Extend

```text
Its teamsWithWork rule and Unassigned text become the owner of the shown Team text, also used by the column valueFormatter, so cell and search cannot disagree
```

FeatureListDataGrid

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.tsx\:29

Reuse

```text
Opt-in already inherited by PortfolioFeatureList:97, TeamFeatureList:118 and WorkItemsTab:50; no per-page code needed for the box
```

projects

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamFeatureList\.tsx\:77

Extend

```text
Existing Portfolios column gains valueFormatter using the helper created in S1
```

TeamFeatureList

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamFeatureList\.tsx\:32

Extend

```text
Owns the projects column on the Team tab
```

##### Impact

page

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFeatureRow\.tsx\:200

Migrated

```text
Own name column replaced by the shared factory spread with minWidth 120 and width undefined; same renderCell output
```

page

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFeatureRow\.tsx\:213

Changed

```text
owningTeam gains valueFormatter
```

page

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamFeatureList\.tsx\:77

Changed

```text
projects gains valueFormatter
```

page

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioFeatureList\.tsx\:97

UnchangedCompatible

```text
Gets the box through FeatureListDataGrid; Name factory and Parent column already search by shown text
```

page

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/WorkItemsTab\.tsx\:50

UnchangedCompatible

```text
Gets the box through FeatureListDataGrid; its Loading and no-Features texts stay
```

test

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFeatureRow\.test\.tsx\:1

UnchangedCompatible

```text
Asserts the rendered Name cell and column set, which the factory reproduces
```

##### Agreement

None.

##### Not applicable

agreement

```text
Page column definitions and the grid are one deployable changed in one commit; no shared wire or persisted shape
```

contract_tests

```text
No parties that can change apart; the primary oracle mounts the real pages
```

architectural_tests

```text
Opt-in law is recorded in S1; this slice adds no dependency edge
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

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/columns\.tsx

Extend

```text
createNameColumn: valueFormatter replaced by valueGetter = getWorkItemName plus sortComparator over row.name (gridStringOrNumberComparator)
```

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.nameFilter\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec cross-env TZ=Europe/Zurich vitest run --reporter=json src/components/Common/FeatureListDataGrid/FeatureListDataGrid.nameFilter.test.tsx
pnpm --dir Lighthouse.Frontend exec cross-env TZ=Europe/Zurich vitest run --reporter=json src/components/Common/FeatureListDataGrid/columns.test.tsx src/components/Common/FeatureListDataGrid/FeatureListDataGrid.search.test.tsx
```

#### Supports

None.

None.

None.

#### Public oracle

```text
Render FeatureListDataGrid with a Feature PROJ-12 Checkout, open the Name column filter and type PROJ-12; also sort the Name column ascending over names not in ID order
```

```text
The PROJ-12 Feature is listed; sort order is by name as before, not by ID prefix
```

```text
Name column without valueGetter finds nothing for PROJ-12; a valueGetter without sortComparator reorders the sort by ID prefix
```

##### Promises

P2

P10

#### Prefactoring

```text
None: no move. Per touched file, columns.tsx holds factory functions and one local row type, nothing to split
```

```text
Name sort order by name, Name cell rendering, S1 search by ID
```

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/columns\.test\.tsx

##### Verification

```text
pnpm --dir Lighthouse.Frontend exec cross-env TZ=Europe/Zurich vitest run --reporter=json src/components/Common/FeatureListDataGrid/columns.test.tsx
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

createNameColumn

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/columns\.tsx\:39

Extend

```text
Column filter and MUI value both read valueGetter; one getter gives filter, search and tooltip the shown text; the comparator keeps today sort order by name
```

##### Impact

column\-factory

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/columns\.tsx\:39

Changed

```text
Name column value is the shown text for filter; sort comparator keeps name order
```

consumer

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/refinementColumns\.tsx\:115

Migrated

```text
Refinement Name filter also matches the ID (accepted side effect, shown text); its sort stays by name; it has no export
```

consumer

Lighthouse\.Frontend\/src\/pages\/Features\/FeaturesView\.tsx\:43

UnchangedCompatible

```text
Takes the factory; filter now finds the ID
```

consumer

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioFeatureList\.tsx\:53

UnchangedCompatible

```text
Takes the factory; filter now finds the ID
```

consumer

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamFeatureList\.tsx\:57

UnchangedCompatible

```text
Takes the factory; filter now finds the ID
```

consumer

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFeatureRow\.tsx\:200

UnchangedCompatible

```text
Already on the factory since S3
```

consumer

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridToolbar\.tsx\:1

UnchangedCompatible

```text
Export reads getCellValue, which would now be the shown Name text; no grid using the factory exports through getCellValue today (Delivery passes exportTable, Refinement has no export)
```

##### Agreement

Name column value \(valueGetter\) as the shown ID\: name text

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/columns\.tsx\:39

Producer

Migrated

```text
Producer of the Name cell value: filter and search now see the shown text, sort keeps name
```

Name column value \(valueGetter\) as the shown ID\: name text

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/refinementColumns\.tsx\:115

Consumer

Migrated

```text
Filter finds the ID; sort and layout unchanged
```

Name column value \(valueGetter\) as the shown ID\: name text

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFeatureRow\.tsx\:200

Consumer

UnchangedCompatible

```text
Adopted the factory in S3
```

Name column value \(valueGetter\) as the shown ID\: name text

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridToolbar\.tsx\:1

Consumer

UnchangedCompatible

```text
getCellValue export reader; none of the grids using the factory exports through it today
```

##### Not applicable

contract_tests

```text
All parties are modules of one bundle changed in one commit; no persisted or wire data outlives the producer
```

architectural_tests

```text
No dependency or ownership law is reachable; the single owner of the Name text (getWorkItemName) is unchanged
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
