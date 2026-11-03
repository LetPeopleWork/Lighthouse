# Slice 09 - Work Items dialog: universe, preserved set, delta

All timing figures in this slice are indicative (not quiet): the machine was shared with a parallel session (load
average 2.7 at the start of the timed run, 5.4 over the preceding five minutes, on 4 cores). The payoff has to come
from the rewrite; no configuration change is in play.

## Pre-slice commit

`92e40dca88158f3ad20f1c90ee4442e9e7f87995` (HEAD of main when this was declared). The close-time alternating
before/after timing pairs check this commit out for the "before" side.

## Universe

Nothing outside this list may change in the slice. The slice is behaviour-free: no production behaviour changes, and
no assertion is weakened or dropped without a row in the table at the end of the close report.

### Does any production file change? NONE

The roadmap pointed the 108 context cases at "the existing, already tested context function in
`workItemsDialogContexts.ts`". Read first, and that is only half true:

- `workItemsDialogContexts.ts` (93 lines) is a **table**, not a function: for each of the 13 contexts the columns it
  opens with, the column it sorts by, whether the rest of the catalogue is offered, and the columns it withholds.
  Its test file (`workItemsDialogContexts.test.ts`, 28 tests, 0.01 s) holds the table to its own rules (one entry per
  context, sort column is one of the defaults, only known column names, only Cumulative Time per State offers no
  catalogue) and says in its header comment that which columns each context opens with "is pinned through the dialog
  itself". So the per-context answers are **not** pinned anywhere except the 108-case dialog file.
- The function that turns that table plus the rows into the dialog's columns is `contextGridLayout(context, items,
  settings)` in `workItemColumns.tsx` (line 499, **already exported**). It returns `{ columns, visibilityModel,
  sortBy }`: the default columns shown, then every other column the rows can fill hidden, in the one catalogue order;
  a default these rows cannot fill is left out (estimate when no row carries one, blocked-since for Feature rows, the
  Feature-only columns for Work Item rows, the descriptor columns when no descriptor is passed); `withheld` columns
  never appear. It has **no direct test today** (`grep contextGridLayout src` finds only the dialog and the file
  itself). Each column it returns carries its `headerName`, `valueGetter` (cell value), `sortable` and sort
  comparator.
- Checked it can be called without a render: a throwaway probe (deleted, nothing committed) called
  `contextGridLayout("estimation", ...)` and `("featureSize", ...)` directly. The visibility model came back
  `{estimate, cycleTime, closedDate: true; startedDate, workItemAge, parent, blockedSince: false}` with `sortBy`
  `cycleTime`, and `featureSize` came back `{size, workItemAge, cycleTime, closedDate: true; startedDate, parent,
  forecasts: false}` with `sortBy` `size`, which is exactly the DOM the render tests assert, minus `owningTeam`.
  `closedDate.valueGetter(undefined, row)` returned `2026-09-20`.
- `owningTeam` ("Owned by") is **not** in that function: `WorkItemsDialog.tsx` adds it inline (`hasOwningTeams`: some
  Feature row with a non-blank owning team) ahead of the context columns. The function tests therefore expect each
  Feature context's shown and offered lists **without** `owningTeam`; the inline rule stays asserted by renders (see
  the wiring table).

So no production file changes: the seam a function test needs is already exported. Planned in 10-02:

- **No extraction commit.** (Paging arithmetic: `WorkItemsDialog.tsx` has none. Pages, the footer and the filter are
  `DataGridBase`'s; the one paging test in the directory stays whole, see below.)
- Test commit: a new `src/components/Common/WorkItemsDialog/contextGridLayout.test.ts` (calls the function; no
  render), and `WorkItemsDialog.contexts.test.tsx` shrinks as the groups below say.
- Close-report commit.

### Test files in the universe

All in `Lighthouse.Frontend/src/components/Common/WorkItemsDialog/`:

- `WorkItemsDialog.contexts.test.tsx` (108 tests): **edited** in 10-02.
- `contextGridLayout.test.ts`: **added** in 10-02.
- `WorkItemsDialog.test.tsx` (83), `WorkItemsDialog.layout.test.tsx` (7), `WorkItemsDialog.loading.test.tsx` (16),
  `WorkItemsDialog.paging.test.tsx` (1), `workItemsDialogContexts.test.ts` (28): **preserved untouched**. Their ids
  are in the before set and must be unchanged at close; they hold wiring named below. If 10-02 changes any of them it
  must say so in its close report.
- `src/tests/timeBudget/slowTests.ts`: only if an entry is fixed (none planned, see allow-list).

Also drive this directory's code but are **outside the universe** (22 other test files import the dialog; the six
that use the shared kit are listed): `LineRunChart.dialog.test.tsx`, `DataGridBase.defaults.test.tsx`,
`DataOverviewTable.reportsAndDeliveries.test.tsx`, `featureChildItemsDialog.test.tsx`,
`BaseMetricsView.cumulativeStateTimeDialog.test.tsx`, `BaseMetricsView.test.tsx`. They stay green, and the full
frontend suite is the terminating run of 10-02.

Not changed: `WorkItemsDialog.tsx`, `workItemColumns.tsx`, `workItemsDialogContexts.ts`, `workItemsDialogRows.ts`,
`WorkItemsDialogTestKit.tsx`.

## Preserved set (before)

`measurement/slice-09-ids-before.txt`: 243 ids, one per line, sorted, as `file :: fullName` (file paths relative to
`Lighthouse.Frontend/`), from a run of the six test files (243 passed, 0 skipped, 0 failed). Per file:
`WorkItemsDialog.contexts.test.tsx` 108, `WorkItemsDialog.test.tsx` 83, `workItemsDialogContexts.test.ts` 28,
`WorkItemsDialog.loading.test.tsx` 16, `WorkItemsDialog.layout.test.tsx` 7, `WorkItemsDialog.paging.test.tsx` 1.
Surviving ids must be unchanged at close. A test that goes keeps a row in the table below; a test that moves keeps
its file and name or is listed there.

Command (in `Lighthouse.Frontend`):

```
D=src/components/Common/WorkItemsDialog
LIGHTHOUSE_TIME_BUDGET=warn TZ=Europe/Zurich pnpm exec vitest run \
  $D/WorkItemsDialog.contexts.test.tsx $D/WorkItemsDialog.test.tsx $D/WorkItemsDialog.paging.test.tsx \
  $D/WorkItemsDialog.layout.test.tsx $D/WorkItemsDialog.loading.test.tsx $D/workItemsDialogContexts.test.ts \
  --reporter=default --reporter=json
```

The config pins the JSON report to the gitignored `Lighthouse.Frontend/test-results.json`; the ids and per-test
seconds were read from that file (copied to scratch). Nothing in the repo changed apart from the new ids file.

## Before figures (indicative, not quiet)

Summed per-test durations of the files, one run (the figure the close-time pairs compare, with the new file added to
the "after" side):

| File | Tests | Summed seconds |
|---|---|---|
| `WorkItemsDialog.contexts.test.tsx` | 108 | 15.70 |
| `WorkItemsDialog.test.tsx` | 83 | 6.56 |
| `WorkItemsDialog.layout.test.tsx` | 7 | 3.29 |
| `WorkItemsDialog.paging.test.tsx` | 1 | 2.02 |
| `WorkItemsDialog.loading.test.tsx` | 16 | 1.65 |
| `workItemsDialogContexts.test.ts` | 28 | 0.01 |
| Sum | 243 | 29.23 |

Wall clock of the six files together (vitest "Duration"): 18.09 s. The contexts file is the slow one by **count, not
by any single test**: 108 tests averaging 0.145 s, the slowest 0.51 s ("a column turned on appears in its place in the
fixed order"), none over 0.5 s apart from that, so none is near an allow-list ceiling. The cost sits in the three
parameterised blocks that render the whole dialog once per context to ask the same question of each:

| Block | Tests | Summed seconds |
|---|---|---|
| "neither shows nor offers an Estimate where estimation is not set up" (12 contexts) | 12 | 2.07 |
| "lists its defaults, then every other column ... in the one fixed order" (12) | 12 | 1.95 |
| "comes back with the reader's own layout ... the next time it opens" (12) | 12 | 3.90 |
| "opens with exactly its default columns ... sorted by" (10) | 10 | 0.55 |
| The 74 other tests | 74 | 7.23 |

(The reopen block is the most expensive per test, 0.30 to 0.39 s, because each renders, opens Manage columns, turns two
columns, unmounts and renders again.)

## Coverage (before)

`src/components/Common/WorkItemsDialog/**`, from a run of the whole directory (6 test files, 243 tests, all passed):

```
LIGHTHOUSE_TIME_BUDGET=off TZ=Europe/Zurich pnpm exec vitest run src/components/Common/WorkItemsDialog \
  --coverage.enabled --coverage.include='src/components/Common/WorkItemsDialog/**' \
  --coverage.reporter=text --coverage.reporter=json-summary
```

| Measure | Before |
|---|---|
| Lines | 97.08 % (200 of 206) |
| Statements | 96.84 % (215 of 222) |
| Functions | 99.03 % (103 of 104) |
| Branches | 91.48 % (129 of 141) |

Per file: `WorkItemsDialog.tsx` 100 % lines, branches 28 of 30 (lines 202, 254); `workItemColumns.tsx` 100 % lines,
statements 171 of 172, branches 101 of 105 (lines 196, 374, 461-473); `workItemsDialogContexts.ts` 100 %;
`workItemsDialogRows.ts` **0 % (6 lines, 1 function, 6 branches)**: that file is exercised by tests in other
directories (`useFeatureChildItems.ts`, `BaseMetricsView.tsx` import it), not by this directory's tests, so the
directory figure carries 6 uncovered branches and 6 lines that are not this slice's. Without that file the
directory is 200 of 200 lines. Coverage must not fall: the branch and line numbers of `WorkItemsDialog.tsx` and
`workItemColumns.tsx` must be at least what they are here, and the new function test adds none of its own sources.
(`coverage/` and the JSON and Sonar reports are gitignored.)

## Allow-list entries (`src/tests/timeBudget/slowTests.ts`) for these files

One entry in the whole allow-list names a file of the universe:

| # | File | Test | Ceiling (s) | Before (s, not quiet) |
|---|---|---|---|---|
| 1 | `WorkItemsDialog.paging.test.tsx` | A Work Items dialog with more than 100 Work Items Priya, on the second page of 243, filters to 64 and sees all of them with no footer | 3.5 | 2.02 (timed run), 2.50 (coverage run) |

It stays: the test is the dialog's only large-list render (243 rows, second page, a State filter that leaves 64, no
footer), it is in the preserved set untouched, and the page split and filter are `DataGridBase`'s, shared by every
list in the app and outside this slice. The contexts, layout, loading and `WorkItemsDialog.test.tsx` files have no
entry. They do set an explicit 20 s per-test timeout (`RENDER_HEAVY = 20000`) on every test in the contexts and layout
files; it is a timeout, not a budget exemption, and 10-02 leaves it as it is unless a test it keeps is rewritten.

## The 108 context cases, grouped by what they assert

Seconds are from the before run (indicative, not quiet). **Kept** = stays a dialog render. **Function** = the
case moves to `contextGridLayout.test.ts`, asserting on what the function returns (`visibilityModel` in order,
`sortBy`, `columns[].headerName`, `columns[].valueGetter`), the id leaves the contexts file and gets a row in the
close report. **Removed** = a render whose per-case part is the data the function answers and whose remaining part
a named survivor already asserts.

Cut: 65 of the 108 leave the contexts file (9.55 s of 15.70 s); 43 stay (6.15 s). New function tests add well under
0.1 s in total (no render).

| # | Group (what the case asserts) | Cases | Seconds | Planned destination |
|---|---|---|---|---|
| A | **Default columns and sort column per context**: "$context opens with exactly its default columns, in order, sorted by $sortedBy" for 10 chart contexts, plus "a Feature's child items open with Started, Closed, Work Item Age, Cycle Time and Estimate, oldest first" (featureChildren) | 10 + 1 | 0.55 + 0.05 | **Function** `contextGridLayout.test.ts`: the shown columns (visibility model entries that are true, in order, without `owningTeam`) and `sortBy`, as a table over the same 12 context cases (`columns`, `sortedBy` from the existing data). The third assertion of the 10 (`rowOrder()[0]` is the `firstRow`) is a row-order check the function cannot answer: dropped as a per-context render; the sort column is asserted per context on the function, and row order under it survives in the renders named in the wiring table |
| B | **Estimate absent when estimation is not set up**: "$context neither shows nor offers an Estimate where estimation is not set up" (10 chart contexts + featureChildren + deliveryTimeline) | 12 | 2.07 | **Function**: with `estimate: null` rows the shown and the offered lists equal the default ones without `estimate`. Survivors that render it: "rows from a backend that sends no estimate at all read as estimation not set up" (panel included) and "without estimation on the child items' Team, the child items open without an Estimate" |
| C | **Manage columns order**: "$context lists its defaults, then every other column the rows can fill in the one fixed order" (same 12) | 12 | 1.95 | **Function**: the keys of `visibilityModel` equal the `offered` list (minus `owningTeam`) with the first N true. Survivor that renders the panel with state: "Manage columns offers every other column the rows can fill, all of them off, after the defaults" |
| D | **Reader's layout comes back on reopen**: "$context comes back with the reader's own layout, ..." | 12 (2 kept, 10 removed) | 3.90 (0.70 kept, 3.22 removed) | **Kept**: `estimation` (Work Item rows) and `featureSize` (Feature rows, where `owningTeam` shifts which column is "the next one"). **Removed** (closedItems, arrivals, inProgress, aging, blocked, stale, workDistribution, startedAndClosed, featureChildren, deliveryTimeline): the persistence is `DataGridBase`'s and keyed by context and owner, which `WorkItemsDialog.layout.test.tsx` (untouched) asserts with hide, turn on, reopen, other context and other owner; what differs per context (last default, next offered column) is groups A and C on the function |
| E | **Estimate cell, header and value** (US-01): category estimate shows its name; the "Estimate" header with a shared unit, no unit and mixed units (3 cases); dates show the local day and an absent date stays empty | 5 | 0.37 | **Function**: `estimate.valueGetter` gives `displayValue` (`"M"`); `estimate.headerName` is `"Estimate"` for the three row sets; `startedDate` and `closedDate` `valueGetter` give `2026-09-27`/`2026-09-30` and `""` for the epoch. Survivors: "each row shows the estimate the chart plots it at, and an empty cell where there is nothing the chart could place" (cell text rendered), "Blocked since shows the day an item was blocked, and nothing for one that is not" (a date cell rendered) |
| F | **Age and Cycle Time cell rules**: Time in State neither shown nor offered when the caller says nothing (inProgress case); finished item's Age and unfinished item's Cycle Time empty, never 0, empty ages last (workDistribution, startedAndClosed, featureChildren cases); Age on a past day still shows for an item finished since; not-started item's Age empty; Age given as a first moment; empty for an item finished by that day; empty for an item not started by that day; "Age on a day counts an item started later that day, and not one finished that day or never started"; "Cycle Time reads the named cycle time it is scoped to, and each named column reads its own" | 1 + 3 + 5 + 1 + 1 | 0.16 + 0.13 + 0.20 + 0.03 + 0.21 = 0.73 | **Function**: `workItemAge.valueGetter` (with and without `ageOn`) and `cycleTime.valueGetter` per row; `namedCycleTime:7` column and `cycleTime` under `cycleTimeScope`; `timeInState` absent from the visibility model without a `timeInState` setting. Survivors: "Age on a past day's point is the age the item had on that day", "arrivals leaves a finished item's Work Item Age and an unfinished item's Cycle Time empty, never 0, and lists the empty ages last" (rendered; row order with empty values), "aging neither shows nor offers Time in State ...", "a named percentile's dialog reads that named cycle time ..." |
| G | **Catalogue facts of one kind of row**: "Feature rows add Size and Forecasted Completion, last, to what Manage columns offers"; "heads every column the catalogue offers with its name" (3 renders); Delivery timeline keeps Warnings only and never offers Estimate or Blocked since; "lists every warning a row has, one sentence after another" | 4 | 0.61 | **Function**: the offered order of Feature rows ends `size`, `forecasts`; `headerName` of the catalogue columns (`Closed`, `Cycle Time`, `Started`, `Work Item Age`, `Estimate`, `Parent`, `Blocked since`; `Size`; `Days Contributed`); `withheld` removes `estimate` for `deliveryTimeline` even when a row carries one, and `blockedSince` is not offered for Feature rows; `warnings.valueGetter` joins the sentences. Survivors: "a Feature's forecast is offered as the Features grid's Forecasted Completion column, right after Size" (panel positions, forecast cells), "Work Item rows neither show nor offer Owned by", the `Warnings column` tests of `WorkItemsDialog.test.tsx` |
| H | **Kept as dialog tests (behaviour only a render shows)** | 43 | 6.15 | Kept, see the next table (D's two kept cases, 0.70 s, are among them) |

Counts reconcile: ids that leave are A 11 + B 12 + C 12 + D 10 + E 5 + F 11 + G 4 = 65 (0.60 + 2.07 + 1.95 + 3.22 +
0.37 + 0.73 + 0.61 = 9.55 s); ids that stay are 43 (6.15 s); 65 + 43 = 108 and 9.55 + 6.15 = 15.70 s. The ids
themselves are the full names in `slice-09-ids-before.txt`; 10-02 lists every removed id and every removed `expect(`
(the file has 100 `expect(` calls in 108 tests before the cut) in its close report's moved-assertions table.

## Distinct panel behaviours that still need a render, and the assertion each keeps

One dialog render per behaviour; every row is a test that stays in the contexts file with its id unchanged.

| Behaviour that needs a render | Kept test | Assertion it keeps |
|---|---|---|
| The context's default columns, the sort column and the row order reach the grid (columns, visibility model and sort model passed on) | "a bubble on a Team's Estimation vs. Cycle Time opens with Estimate, Cycle Time and Closed, longest Cycle Time first" | shown context columns; `aria-sort` descending on Cycle Time; row order ST-1, ST-3, ST-2 |
| Owned by sits beside Features, ahead of the context columns, and only when a Feature names a team (inline in the dialog, not in the function) | "a bubble on a Portfolio's Estimation ...", "Feature rows keep Owned by beside the fixed columns", "Feature rows that name no owning team leave Owned by out"; also `WorkItemsDialog.test.tsx` "Feature owning team functionality" | shown columns start `owningTeam`, `estimate`; cell `Team Voyager`; absent for a blank team |
| Largest Feature first, oldest item first (the sort column's values drive MUI's row order) | "a Feature Size dot lists the largest Feature first"; "$context lists the oldest item first, not the one longest in its state" (aging, stale, inProgress); "of two items equally old, lists the one longer in its state first" | row orders FTR-2, FTR-1, FTR-3; WIP-1, WIP-2, WIP-3; WIP-B, WIP-A (the last is the dialog's own pre-sort, `sortedItems`) |
| Rows with no value sort last | "arrivals leaves a finished item's Work Item Age ... empty, never 0, and lists the empty ages last" | Age/Cycle Time cell text and order WIP-1, WIP-3, ST-1 |
| The panel lists the columns and their on/off state | "Manage columns offers every other column the rows can fill, all of them off, after the defaults"; "rows from a backend that sends no estimate ..."; "Work Item rows neither show nor offer Owned by" | `{field, shown}` list in order; absence of estimate |
| Turning a column on, per context | "a column turned on appears in its place in the fixed order" | contextColumnsShown after turning on `parent`, `startedDate` |
| Reader's layout survives a reopen | "estimation comes back with ..." and "featureSize comes back with ..." (and the untouched layout tests) | columns shown after hide + turn on + reopen |
| Header text, with the instance's own word and a named cycle time | "the configured word for Cycle Time heads its column"; "a named percentile's dialog reads that named cycle time under its name, and offers each named cycle time" | `Lead Time`; `Dev Cycle`, row order, `namedCycleTime:7/9` offered |
| Cell styling (SLE colour and bold, blocked marker, judged-only-the-sort-column, wash) | "Cycle Time keeps its SLE colouring and bold ...", "a blocked item still carries the blocked marker ...", "judges only the column the rows are sorted by", "leaves an empty cell of the sort column unjudged", "washes a judged cell ..." | computed style and test ids |
| Time in State badge, SLE Risk disclosure tooltip | "Time in State keeps its own badge ...", "Time in State gives the blocked time ...", "shows a row's SLE Risk disclosure ..." | badge text/aria-label; tooltip text |
| Header-click sorting (MUI comparator on our values) | "every column named here sorts on a header click" (3), "orders by parent/time in state/estimate", "sorting by Estimate follows the configured category order", SLE Risk order (2) | `aria-sort` changes; row orders |
| Days Contributed only, nothing else offered | "Cumulative Time per State keeps Days Contributed only and offers nothing else" | shown `daysContributed`; offered `[daysContributed]` |
| Forecast cells for Features, Size on a Team's Features | "a Feature's forecast is offered as the Features grid's Forecasted Completion column, right after Size"; "a Team's Features worked on show in the Size column the size the Team's answer carries" | checkbox positions, three forecast cells; cell `4` |
| Age on a past day reaches the cell | "Age on a past day's point is the age the item had on that day" | cell `10` |
| Time in State is neither shown nor offered without its setting | "aging neither shows nor offers Time in State ..." | not in shown, not in panel |

## Wiring to keep

The component's call site is `WorkItemsDialog.tsx` line 163 (`contextGridLayout(context, items, {...12
settings})`) and its use (`...contextLayout.columns` at line 259, `defaultColumnVisibilityModel` at 310,
`initialSortModel` at 312, `key` and `storageKey`). A function test cannot fail when the call site breaks, so for every page-level test 10-02 deletes there
must be a surviving render that does. 10-02 proves each row below by breaking the call site temporarily (the mutation
column) and reverting, as in earlier slices; none was mutated in this declaration step, so "must fail" is the claim to
verify, not a finding. A label or reason string is not state (the survivors below assert shown columns, `aria-sort`,
`{field, shown}` and row order, not just text); no test is shrunk under its own name (cases leave, no case is rendered
smaller), and the paging file with its 243-row render is untouched, so the page-local versus whole-list lesson does not
apply: nothing here computes a page offset.

| Removed group | What the render proved that the function cannot | Surviving test that must fail if the call site breaks | Mutation 10-02 applies |
|---|---|---|---|
| A (defaults and sort per context) | the dialog passes the function's `columns`, `visibilityModel` and `sortBy` on, for the right `context` | "a bubble on a Team's Estimation ..." (columns + `aria-sort` + order); "Work Item rows neither show nor offer Owned by" (closedItems shown columns); "a Feature Size dot ..." (featureSize sort); the three "oldest item first" cases and `WorkItemsDialog.test.tsx` "displays items sorted by cycle time/age" | `sortBy` replaced by `"referenceId"`; `visibilityModel` replaced by `undefined`; `context` replaced by `"closedItems"`; `columns` not appended |
| B (estimate absent) | the call passes the rows (`items`) the function filters by | "rows from a backend that sends no estimate at all read as estimation not set up"; "without estimation on the child items' Team ..." (both assert shown and offered) | `items` replaced by `[]` (everything the rows could fill disappears) and by the rows with `estimate` kept when the render passes none |
| C (panel order) | the panel is the visibility model in key order | "Manage columns offers every other column the rows can fill, all of them off, after the defaults" (field and shown state, in order) | `visibilityModel` replaced by one that is all `true` |
| D (reopen, 10 removed) | the storage key follows context and owner | untouched `WorkItemsDialog.layout.test.tsx` (7: hide + reopen, other context, other owner, turn on, reset, stale save, switched context) and the kept `estimation` and `featureSize` cases | `storageKey` without `context`; without `ownerKind` |
| E, F (cell values and headers) | `getTerm`, `ageOn`, `cycleTimeScope`, `namedCycleTimeDefinitions` and the descriptors are passed on | "the configured word for Cycle Time heads its column" (`getTerm`); "Age on a past day's point is the age the item had on that day" (`ageOn`); "a named percentile's dialog reads that named cycle time ..." (`cycleTimeScope` and `namedCycleTimeDefinitions`: header, order, panel); "aging neither shows nor offers Time in State ..." (absent without the setting) and "Time in State keeps its own badge ..." and `WorkItemsDialog.test.tsx` "renders a 'Time in State' column with badges when timeInStateColumn provided" (present with it) | each setting dropped from the object, one at a time: `getTerm` replaced by `(key) => key`, `ageOn`, `cycleTimeScope`, `namedCycleTimeDefinitions`, `timeInState` forced to `{}` |
| G (catalogue facts), and the settings no removed case exercises | `parentReferences`, `daysContributed`, `sle`, the band, risk and warnings descriptors reach the function | `WorkItemsDialog.loading.test.tsx` "asks once for the names of the shown rows' parents while Parent is shown, and shows them" (`parentReferences`); "Cumulative Time per State keeps Days Contributed only ..." (`daysContributed`); "Cycle Time keeps its SLE colouring and bold ..." (`sle`); `WorkItemsDialog.test.tsx` "Work Item Age Band column ... names each item's band ...", "SLE Risk column ... heads the column ... shows a whole-number percentage on every row", "Warnings column carries whatever the caller found worth warning about" (`ageBand`, `sleRisk`, `warnings`) | each of those six settings dropped, one at a time |

Rules that sit in the component and not in the function stay on renders only: `hasOwningTeams`, `sortedItems` (time
in state pre-sort), `status` (loading, error and empty), the enlarge toggle, the export file name. They are not part of
the cut.

## Delta (filled in at close)

To be completed by the 10-02 close report, in the shape of slice 7: commit hashes against the pre-slice commit, a table
of every removed id with its destination and every removed `expect(` ("moved to ... asserts ..." or "dropped because
..."), the five alternating before/after pairs on the six files plus the new file (labelled indicative (not quiet)),
the allow-list length (23 now; 1 entry belongs to the universe and is untouched), tests added and removed, coverage
after (lines and branches of `WorkItemsDialog.tsx` and `workItemColumns.tsx` not below the figures above), and the
mutation proof for each row of the wiring table. Declared up front, so a reviewer can check the cut:

- Production files changed: none.
- Ids that leave: 65 of 108 in `WorkItemsDialog.contexts.test.tsx` (groups A to G; D leaves 10 of its 12). Ids that
  stay: 43 there plus all 135 others, i.e. 178 of 243. Ids added: the function test's cases, in the new file.
- The describe titles of the contexts file keep their text (ids unchanged), even where a block then holds two cases.
- Time expectation, for the close report to confirm or refute: the 65 cases are 9.55 s of the 29.23 s summed; the
  function tests add well under 0.1 s.
