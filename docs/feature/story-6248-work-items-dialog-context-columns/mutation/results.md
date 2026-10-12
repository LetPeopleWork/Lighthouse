# Mutation testing — 6248 (context columns in the Work Items dialog)

Run 2026-10-12 against `main` @ `9d61098ad` (the story's 42 commits on top of `origin/main`), plus
the survivor-killing tests and the one production fix listed below. Gate: 80 % kill rate per stack.

| stack | score | tested | killed | survived | no coverage | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0), the story's lines | **100 %** | 17 | 17 | 0 | 0 | 0 | 7 m 50 s |
| Backend, whole files (headline Stryker prints) | 67.92 % | 689 | 467 | 153 | 68 | 1 | (same run) |
| Frontend (StrykerJS 10.0.0), final run | **92.99 %** | 585 | 544 | 39 | 2 | 0 | 24 m 59 s |
| Frontend, first run (before triage) | 82.85 % | 583 | 483 | 97 | 3 | 0 | 36 m 00 s |

Configs: `stryker.6248.backend.json`, `stryker.6248.frontend.json`, `vitest.stryker.mutation.ts`.

How to rerun:

- Backend, from `Lighthouse.Backend/Lighthouse.Backend.Tests/`:
  `dotnet stryker --config-file ../../docs/feature/story-6248-work-items-dialog-context-columns/mutation/stryker.6248.backend.json`.
  "20789 mutants created" is the whole backend; the scope line is "621 total mutants will be
  tested" (plus 68 that no filtered test reaches, reported as no coverage).
- Frontend, from `Lighthouse.Frontend/`: copy `vitest.stryker.mutation.ts` to
  `vitest.stryker.6248.ts` (gitignored), then
  `TZ=Europe/Zurich pnpm exec stryker run ../docs/feature/story-6248-work-items-dialog-context-columns/mutation/stryker.6248.frontend.json`.
  `TZ` matters: `pnpm test` pins it and several date specs depend on it. `coverageAnalysis` is
  `perTest` (the vitest-runner patch in `patches/` makes that work); with `off` every mutant would
  rerun all ~975 tests of the 29 included specs.

## Backend

Stryker.NET mutates whole files only, so four of the six files are mostly code this story did not
touch. The table scores the mutants that start on a line the story changed
(`git diff origin/main..HEAD -U0`) separately from the whole file. Every story-line mutant was
killed; the whole-file survivors sit in untouched code and were not chased.

| file | story lines: tested / killed | whole file: tested / killed / survived / no coverage | whole-file score |
| --- | --- | --- | --- |
| `Services/Implementation/EstimateNormalizer.cs` | 3 / 3 (+4 compile errors, not scored) | 23 / 23 / 0 / 0 | 100 % |
| `API/DTO/WorkItemEstimateDto.cs` | 5 / 5 | 5 / 5 / 0 / 0 | 100 % |
| `API/DTO/WorkItemDto.cs` | 0 (the story added an auto-property and doc comments) | 8 / 5 / 3 / 0 | 62.50 % |
| `Services/Implementation/BaseMetricsService.cs` | 2 / 2 | 405 / 265 / 89 / 51 | 65.43 % |
| `Services/Implementation/WorkTrackingConnectors/Csv/CsvWorkTrackingConnector.cs` | 4 / 4 (+2 ignored: block already covered) | 152 / 102 / 34 / 16 | 67.11 % |
| `Factories/DemoDataFactory.cs` | 3 / 3 | 96 / 68 / 27 / 1 | 70.83 % |

Whole-file mutation was kept for all six, deliberately: `EstimateNormalizer` and
`WorkItemEstimateDto` are essentially this story's files, and for the four large ones the story-line
filter above gives the honest number while the whole-file run still proves the test filter reaches
them. The filter covers `EstimateNormalizer`, `WorkItemEstimateDto`, `WorkItemDto`,
`BaseMetricsService`, `TeamMetricsService`, `PortfolioMetricsService`, the metrics and features
controllers, `WorkItemAgeAsOfDate`, `Csv` and `DemoData` tests, and excludes the acceptance and
integration suites.

### Closed by this pass

Nothing to close: no story-line survivor.

### Accepted survivors

None on the story's lines. Untouched legacy lines: `WorkItemDto` 3 survivors, `BaseMetricsService`
89 survivors + 51 no coverage, `CsvWorkTrackingConnector` 34 + 16, `DemoDataFactory` 27 + 1.

### Not mutated

- `API/TeamMetricsController.cs`, `API/PortfolioMetricsController.cs`, `API/FeaturesController.cs`:
  the story's change there is one `{ Estimate = WorkItemEstimateDto.For(owner, item) }` initializer
  per endpoint, in files of several hundred untouched lines. The wiring is pinned end to end by
  `WorkItemEstimateApiIntegrationTest` and `RunChartPayloadContractIntegrationTest`, which belong to
  the acceptance suite that mutation runs exclude for runtime (80+ minutes).
- `Factories/DemoData/Team Zenith.csv`: data, pinned by `DemoDataFactoryTest`.

## Frontend

StrykerJS honours line ranges, so the three files the story created or rewrote
(`workItemColumns.tsx`, `workItemsDialogRows.ts`, `useFeatureChildItems.ts`) are mutated whole and
the rest by the story's changed lines only. StrykerJS does not mutate JSX text, so copy inside JSX is
pinned by the specs rather than measured here.

| file | scope | tested | killed | survived | no coverage | score |
| --- | --- | --- | --- | --- | --- | --- |
| `WorkItemsDialog/workItemColumns.tsx` | whole file (the column catalogue, new in this story) | 396 | 368 | 27 | 1 | 92.93 % |
| `WorkItemsDialog/WorkItemsDialog.tsx` | story lines | 68 | 60 | 7 | 1 | 88.24 % |
| `DataGrid/DataGridBase.tsx` | story lines (defaults and override-only persistence) | 57 | 54 | 3 | 0 | 94.74 % |
| `WorkItemsDialog/workItemsDialogContexts.ts` | story lines | 35 | 35 | 0 | 0 | 100 % |
| `WorkItemsDialog/workItemsDialogRows.ts` | whole file | 16 | 15 | 1 | 0 | 93.75 % |
| `FeatureListDataGrid/useFeatureChildItems.ts` | whole file | 12 | 11 | 1 | 0 | 91.67 % |
| `Charts/LineRunChart.tsx` | story lines (`ageOn`) | 1 | 1 | 0 | 0 | 100 % |

### A production fix a survivor proved necessary

`DataGridBase` held the whole visibility model in state, built once from the defaults at mount. The
Work Items dialog mounts its grid while the rows are still loading and computes the defaults from the
rows, so when they arrived any column that is offered only for those rows came up shown instead of
hidden: a Portfolio's in-progress list opened while loading showed Size and Forecasted Completion,
which the context hides. The survivor that exposed it was the `applyVisibility` dependency array (a
stale-defaults closure the tests could not tell apart). The grid now holds only the user's overrides
and lays the current defaults under them on every render, which is what its own doc comment already
promised ("a column they never touched always follows the current defaults"). Pinned by
`DataGridBase.defaults.test.tsx` ("lays defaults that change while it is shown…", red before the fix)
and `WorkItemsDialog.loading.test.tsx` ("lays the context's defaults over rows that arrive after it
opened").

### Closed by this pass

| survivor(s) | test that now kills it | scenario |
| --- | --- | --- |
| `DataGridBase` `applyVisibility` deps `[]` | defaults: "does not take a column the defaults hid for a while as one the user hid" | defaults change while shown, the user hides another column, the reopened grid follows the old defaults again |
| `DataGridBase` `model[field] ?? true` (`&&`, and `false` with no coverage) | defaults: "remembers Show all across a reopen…" | Show all drops the keys from the grid's model; those columns must still be remembered as shown |
| `DataGridBase` `defaults[field] ?? true` → `&&` | defaults: "forgets a column the user turned off and on again…" | a column back at its default is not remembered, so it follows later defaults |
| `WorkItemsDialog` `status === "error"` → `true` | loading: "says nothing about failing to load once the items are in" | |
| `WorkItemsDialog` parent references: no `filter`, no `sort`, comparator gone; `workItemColumns` `enabled: length > 0` (`true`, `>= 0`) | loading: "asks for no names when none of the shown rows has a parent", "asks once for the same rows' parents, whatever order the rows come in" | |
| `workItemColumns` `parent.url ?? ""` (`&&`, string) | loading: "links a parent's name to the parent, and one without an address to nowhere" | |
| `WorkItemsDialog` `exportFileName` regex (`\s`, `\S+`, `""`) | `WorkItemsDialog.test.tsx` "the exported file is named after the dialog's title, each run of spaces one underscore" | premium export of a title with runs of spaces |
| `WorkItemsDialog` `hasOwningTeams` trim (`trim()` removed, `!== "Stryker"`, `true`) | contexts: "Feature rows that name no owning team leave Owned by out" | |
| `WorkItemsDialog` time-in-state pre-sort (comparator, `+`, no sort, `?? null` → `&& null`, `daysInStateOf` gone) | contexts: "of two items equally old, lists the one longer in its state first" | tie on the sort column |
| `workItemColumns` header names: Started, Closed, Parent, `Blocked since`, Size, Days Contributed | contexts: "heads every column the catalogue offers with its name" | |
| `workItemColumns` `sortable: true` → `false` on day, number, parent, Time in State, SLE Risk, Warnings columns | contexts: "$context: every column named here sorts on a header click" (three contexts) | |
| `workItemColumns` parent `valueGetter` gone | contexts: "orders by parent on a Parent header click" | |
| `workItemColumns` Time in State `valueGetter` gone / `?? null` → `&& null` | contexts: "orders by time in state on a Time in State header click" | |
| `workItemColumns` `estimate?.value` → `estimate.value` | contexts: "orders by estimate on an Estimate header click, a row an older server sent none for included" | crashed on a row with no estimate |
| `workItemColumns` `asLocalDay` "no day" text; Blocked since `valueGetter` gone | contexts: "Blocked since shows the day an item was blocked, and nothing for one that is not" | |
| `workItemColumns` `field === settings.sortBy` → `true` | contexts: "judges only the column the rows are sorted by" | |
| `workItemColumns` `value === null` guard → `false` | contexts: "leaves an empty cell of the sort column unjudged" | |
| `workItemColumns` `MS_PER_DAY` arithmetic (3), `started <= day` → `<`, `started !== null` → `true` | contexts: "Age on a day counts an item started later that day, and not one finished that day or never started" | |
| `workItemColumns` named cycle time: `find` predicate `true`, both optional chains, scope lookup `true`, named column getter gone | contexts: "Cycle Time reads the named cycle time it is scoped to, and each named column reads its own" | scope on the second definition; rows missing one or all values |
| `workItemColumns` judgement wash `style: {}` | contexts: "washes a judged cell in a tenth of its judgement's colour" | |
| `workItemColumns` SLE Risk `direction === "desc"` → `true` | contexts: "orders SLE Risk lowest first, then highest first, with a row it cannot place last both ways" | |
| `workItemColumns` SLE Risk unplaced-first `? 1 : 0` → `false` | contexts: "keeps rows SLE Risk cannot place in the order they came, below the rest" | |
| `workItemColumns` SLE Risk tooltip (`true`, `!==`) | contexts: "shows a row's SLE Risk disclosure when the reader hovers over it" | |
| `workItemColumns` warnings `join(" ")` → `join("")` | contexts: "lists every warning a row has, one sentence after another" | |
| `workItemColumns` `blockedSince ?? null` → `&& null` (Time in State badge) | contexts: "Time in State counts a blocked item as stale from the day it was blocked" | |

### Accepted survivors (41)

Equivalent mutants (cannot change what a reader sees or what is exported):

- `workItemColumns` `asLocalDay` guard (4) and `utcDayOf` guard (4): without the guard, `null` and
  `undefined` still become the epoch or an invalid date, which both fall into the "no day" branch.
- `workItemColumns` SLE Risk comparator `worstFirst * (a - b)` → `worstFirst / (a - b)`: same sign,
  and the grid reads only the sign.
- `workItemColumns` SLE Risk `firstRisk === undefined` → `false`, and `secondRisk === undefined ? 1 : 0`
  → `true`: with one unplaced row the comparator still puts it last in every comparison order V8's
  sort makes on lists under 64 rows; the comparator only goes wrong on longer lists.
- `workItemColumns` SLE Risk `disclosure === undefined ? cell : tooltip` → `false`: a tooltip whose
  title is `undefined` shows nothing.
- `workItemColumns` `renderCell?.()` and `daysContributed?.daysFor`: the optional call is never on
  `undefined` (the Features grid column always has a renderer; the column is offered only when the
  descriptor exists).
- `workItemColumns` `featureRows` `some` → `every`: differs only for a mixed row set, which no dialog
  has, or no rows at all, where it changes only which hidden columns Manage columns lists while
  loading.
- `workItemColumns` parent-names query key `"work-items-dialog-parents"` → `""`: the key is still unique
  within the dialog's cache.
- `WorkItemsDialog` `isFeatureRow(item) && owningTeam` → `||`: a non-Feature row has no owning team, so
  the expression is falsy either way. `timeInStateColumn?.now` → `.now`: only called when the column is
  given.
- `DataGridBase` `col.hideable === false && sanitized[...] === false` → `… && true`: writes an explicit
  `true` for a column the grid already shows. `persistedState?.columnVisibilityModel` → `.`:
  `usePersistedGridState` always returns an object. Sanitising effect deps → `[]`: after mount it would
  only matter if a column became unhideable, or defaults started hiding the Name column, neither of
  which any caller does; the Manage columns panel offers no hide control for an unhideable column.
- `useFeatureChildItems` `useCallback(..., [])` → `["Stryker was here"]`: a constant dependency list
  either way.

Type-invalid (no TypeScript checker is configured for the run; `tsc` rejects them):

- `workItemsDialogRows` `status: "ready"` → `""`, and the `WorkItemsDialog` default `status = "ready"`
  → `""`: `""` is not a `WorkItemsDialogStatus`; every reader branches on `"loading"` and `"error"` only.

Unreachable:

- `workItemColumns` `parentReferences = []` default (no coverage): the dialog always passes the list.
- `WorkItemsDialog` Owned by cell's non-Feature branch `""` (no coverage): the column exists only when
  the rows are Features.

Presentation only (spacing, alignment, typography tokens; no behaviour, nothing a test can assert
without pinning MUI internals):

- `workItemColumns`: `fontWeight: "normal"`, the blocked icon's `sx`, the judgement cell's `padding`,
  `display`, `alignItems`, the unjudged wash `"transparent"` → `""` (both no background), the warnings
  cell's `color` tokens (2) and its `sx` (3).
- `WorkItemsDialog`: the could-not-load row's `sx` (3) and the grid wrapper's `sx={{ mt: 2 }}`.

## Not mutated

Other files the story touched on the frontend (the chart components, `BaseMetricsView.tsx`,
`WidgetShell.tsx`, the Delivery and Feature list pages, `Feature.ts`/`WorkItem.ts`) only pass a context
id, an owner kind or a status into the dialog, or add one decoded field; those are out of this run's
brief and are pinned by their own specs.
