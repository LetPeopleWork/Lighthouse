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
| D | **Reader's layout comes back on reopen**: "$context comes back with the reader's own layout, ..." | 12 (2 kept, 10 removed) | 3.90 (0.70 kept, 3.22 removed) | **Kept**: `estimation` (Work Item rows) and `featureSize` (Feature rows, where `owningTeam` shifts which column is "the next one"). **Removed** (closedItems, arrivals, inProgress, aging, blocked, stale, workDistribution, startedAndClosed, featureChildren, deliveryTimeline): the persistence is `DataGridBase`'s and keyed by context and owner, which `WorkItemsDialog.layout.test.tsx` (untouched) asserts with hide, turn on, reopen, other context and other owner; the per-context reopen assertion is dropped: what differs per context (last default, next offered column) is groups A and C on the function, and a reopen itself is exercised only by the generic storage-key tests and the two kept cases |
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
| D (reopen, 10 removed; the per-context reopen assertion is dropped, not moved) | the storage key follows context and owner | the generic storage-key tests of untouched `WorkItemsDialog.layout.test.tsx` (7: hide + reopen, other context, other owner, turn on, reset, stale save, switched context) plus the two kept reopen renders, `estimation` and `featureSize`; no test reopens each of the other ten contexts | `storageKey` without `context`; without `ownerKind` |
| E, F (cell values and headers) | `getTerm`, `ageOn`, `cycleTimeScope`, `namedCycleTimeDefinitions` and the descriptors are passed on | "the configured word for Cycle Time heads its column" (`getTerm`); "Age on a past day's point is the age the item had on that day" (`ageOn`); "a named percentile's dialog reads that named cycle time ..." (`cycleTimeScope` and `namedCycleTimeDefinitions`: header, order, panel); "aging neither shows nor offers Time in State ..." (absent without the setting) and "Time in State keeps its own badge ..." and `WorkItemsDialog.test.tsx` "renders a 'Time in State' column with badges when timeInStateColumn provided" (present with it) | each setting dropped from the object, one at a time: `getTerm` replaced by `(key) => key`, `ageOn`, `cycleTimeScope`, `namedCycleTimeDefinitions`, `timeInState` forced to `{}` |
| G (catalogue facts), and the settings no removed case exercises | `parentReferences`, `daysContributed`, `sle`, the band, risk and warnings descriptors reach the function | `WorkItemsDialog.loading.test.tsx` "asks once for the names of the shown rows' parents while Parent is shown, and shows them" (`parentReferences`); "Cumulative Time per State keeps Days Contributed only ..." (`daysContributed`); "Cycle Time keeps its SLE colouring and bold ..." (`sle`); `WorkItemsDialog.test.tsx` "Work Item Age Band column ... names each item's band ...", "SLE Risk column ... heads the column ... shows a whole-number percentage on every row", "Warnings column carries whatever the caller found worth warning about" (`ageBand`, `sleRisk`, `warnings`) | each of those six settings dropped, one at a time |

Rules that sit in the component and not in the function stay on renders only: `hasOwningTeams`, `sortedItems` (time
in state pre-sort), `status` (loading, error and empty), the enlarge toggle, the export file name. They are not part of
the cut.

## Delta and moved assertions

Commits (pre-slice `92e40dca8`): `aed76322b` the test commit (`contextGridLayout.test.ts` added, 65 cases removed from
`WorkItemsDialog.contexts.test.tsx`); this report is the close-report commit. **No production file changed, no
extraction commit**: `git diff --name-only 92e40dca8..aed76322b` is exactly the two test files. RED: the new
`contextGridLayout.test.ts` was seen to fail against a deliberately wrong `sortBy` returned by `contextGridLayout`
(`"size"` for every context): 12 of 88 failed (the eleven contexts whose sort column is not Size, and Cumulative Time
per State); production restored with `git checkout`. The preserved files (`WorkItemsDialog.test.tsx`,
`.layout.`, `.loading.`, `.paging.`, `workItemsDialogContexts.test.ts`) were not edited, their ids are unchanged.

| Measure | Before | After |
|---|---|---|
| Tests in the directory (7 files after, 6 before) | 243 | 254 |
| `WorkItemsDialog.contexts.test.tsx` | 108 | 43 |
| Tests added (`contextGridLayout.test.ts`, no render) | n/a | 76 |
| Tests removed | n/a | 65 (rows below); 0 added in the old files |
| Surviving ids from `slice-09-ids-before.txt` | 243 | 178, all unchanged (sorted lists compared: 65 missing, 0 new in the old files; every missing id is in the contexts file) |
| `expect(` in `WorkItemsDialog.contexts.test.tsx` | 100 | 65 |
| Line coverage, `src/components/Common/WorkItemsDialog/**` | 97.08 % (200/206) | 97.08 % (200/206) |
| Branch coverage, same | 91.48 % (129/141) | 91.48 % (129/141) |
| `WorkItemsDialog.tsx` | lines 34/34, branches 28/30 | lines 34/34, branches 28/30 |
| `workItemColumns.tsx` | lines 156/156, statements 171/172, branches 101/105 | the same |
| Allow-list length | 13 | 13 (the one entry of the universe, in the paging file, is untouched) |

Coverage did not rise: the 108 renders already reached every line and branch of `workItemColumns.tsx` that the new
tests reach, so a direct test of the function adds none. The six lines of `workItemsDialogRows.ts` stay uncovered
here as before (that file is exercised from other directories).

The two kept reopen cases (Estimation, Feature Size) and the describe titles keep their text: the block "every chart
context that offers more columns follows the same rules" now holds two cases and the block "a Feature's child items
show their progress at a glance" holds one, and the ids of those cases did not change.

### Time (indicative, not quiet)

Five alternating before/after pairs run back to back, `LIGHTHOUSE_TEST_WORKERS=2` both sides, load average 2.27 at the
start and 2.01 at the end (so not quiet), only the touched test files: before
= `WorkItemsDialog.contexts.test.tsx` from a temporary worktree of `92e40dca8`; after = the contexts file plus
`contextGridLayout.test.ts`. Figure compared: the sum of per-test durations from the JSON report (wall clock was not
recorded: no `bc` here).

| Pair | Before (s, 108 tests) | After (s, 131 tests) |
|---|---|---|
| 1 | 15.42 | 5.63 |
| 2 | 14.77 | 5.71 |
| 3 | 15.81 | 5.45 |
| 4 | 15.53 | 5.62 |
| 5 | 14.77 | 5.59 |
| Sum | 76.30 | 28.00 |

Relative change of the summed test seconds of the touched files: **-63.3 %**. The declaration's expectation (65 cases
are 9.55 s of the 15.70 s, the function tests add well under 0.1 s) is confirmed: the 88 function tests take about
0.02 s in all. The declaration named all six files plus the new one for the pairs; the close brief narrowed it to the
touched files, which is what was run, so this figure is larger than a six-file figure would be (the other five files,
13.5 s before, are unchanged).

### Call sites proven by mutation

Each argument, prop or setting named in the table was changed in `WorkItemsDialog.tsx` (nothing else; it is not
every line of the call site, see the review revision for the rest), the whole `WorkItemsDialog` directory run
(266 tests at the time of the first close, before the review revision), then restored with `git checkout`. The failing tests are all survivors (in the 178 preserved ids); the
tests removed by this slice no longer exist, so none of them is among the failures.

| Change | Failed | Failing surviving tests (first ones) |
|---|---|---|
| `getTerm` replaced by `(key) => key` | 10 | "the configured word for Cycle Time heads its column"; Work Item Age Band column tests |
| `parentReferences` dropped | 6 | "asks once for the names of the shown rows' parents while Parent is shown, and shows them"; "links a parent's name to the parent ..." |
| `timeInState` (the column definition) dropped | 9 | "Time in State keeps its own badge when the list is sorted by Age"; "orders by time in state on a Time in State header click" |
| `sle` dropped | 10 | "Cycle Time keeps its SLE colouring and bold where the caller passes the SLE"; "blocked icon appears with SLE coloring" |
| `ageOn` dropped | 1 | "Age on a past day's point is the age the item had on that day" |
| `cycleTimeScope` dropped | 1 | "a named percentile's dialog reads that named cycle time under its name, and offers each named cycle time" |
| `namedCycleTimeDefinitions` dropped | 1 | the same test |
| `daysContributed` dropped | 1 | "Cumulative Time per State keeps Days Contributed only and offers nothing else" |
| `ageBand` dropped | 18 | "does not take over which column the dialog opens sorted by"; "sits beside the band it belongs with when both are offered" |
| `sleRisk` dropped | 18 | "orders SLE Risk lowest first, then highest first, ..."; "inProgress: every column named here sorts on a header click" |
| `warnings` dropped | 2 | "carries whatever the caller found worth warning about"; "deliveryTimeline: every column named here sorts on a header click" |
| `items` replaced by `[]` | 20 | "a Team's Features worked on show in the Size column ..."; "a bubble on a Team's Estimation ..." |
| `context` replaced by `"closedItems"` | 108 | "a bubble on a Portfolio's Estimation ..." and 107 more |
| `...contextLayout.columns` replaced by `...[]` | 120 | "a Team's Features worked on show in the Size column ..." and 119 more |
| `defaultColumnVisibilityModel` replaced by `{}` | 100 | "a bubble on a Team's Estimation ..."; "a column turned on appears in its place in the fixed order"; "Manage columns offers every other column the rows can fill, all of them off, after the defaults" |
| `initialSortModel` field replaced by `"referenceId"` | 18 | "a bubble on a Team's Estimation ... longest Cycle Time first"; "a Feature Size dot lists the largest Feature first"; "aging lists the oldest item first, not the one longest in its state" |
| `initialSortModel` direction `"asc"` | 20 | the same, plus "arrivals leaves a finished item's Work Item Age ..." |
| `storageKey` without `context` | 3 | `WorkItemsDialog.layout.test.tsx`: "a column turned on stays on in its own context only"; "hiding Closed in the Estimation dialog leaves the Cycle Time scatter's dialog as it was"; "the same dialog switched to another context while open shows that context's layout" |
| `storageKey` without `ownerKind` | 1 | `WorkItemsDialog.layout.test.tsx`: "hiding Closed in a Team's Estimation dialog leaves every Portfolio's Estimation dialog as it was" |

Every change in this table was caught by a surviving render; the call-site lines the table leaves out are covered in
the review revision section. The dropped first-row assertion of group A is accepted on the terms of the
brief: the `initialSortModel` mutation (field and direction) is caught by 18 and 20 surviving renders, and
`contextGridLayout.test.ts` asserts `sortBy` exactly for each of the 12 contexts (and Cumulative Time per State).

### Moved assertions

Old tests are in `WorkItemsDialog.contexts.test.tsx`. "Function test" is a test in `contextGridLayout.test.ts`;
`shownIn` is the entries of the returned `visibilityModel` that are true, in key order, `fieldsOf` all its keys in
order, `valueIn` the column's `valueGetter` on a row, `headerName` the column's header string. Owned by is added by
the dialog itself, so every Feature context's lists in the function test leave `owningTeam` out; the dialog's own
rule stays asserted by "a bubble on a Portfolio's Estimation ...", "Feature rows keep Owned by beside the fixed
columns" and "Feature rows that name no owning team leave Owned by out".

| Old tests (describe :: name) | `expect(` | New location: function test | Assertion |
|---|---|---|---|
| "every chart's dialog ..." :: "$context opens with exactly its default columns, in order, sorted by $sortedBy" for closedItems, estimation, arrivals, inProgress, aging, blocked, stale, workDistribution, featureSize, startedAndClosed (10) | `contextColumnsShown()` equals the default columns | "$context opens with exactly its default columns, in order, sorted by $sortedBy" | `shownIn(layout)` equals the same list (featureSize without `owningTeam`) |
| same | `sortedDescendingBy()` is the sort column | same test | `layout.sortBy` is the same field; the grid being given it, descending, is the `initialSortModel` rows of the mutation table |
| same | `rowOrder()[0]` is the context's first row | none: dropped | The row order is MUI's sort over the sort column. Kept renders assert it for the same rows and columns: "a bubble on a Team's Estimation ..." (cycleTime; closedItems and estimation use the same rows and sort), "a Feature Size dot ...", "aging lists the oldest item first ..." (workItemAge, the same `inFlight` rows as blocked, stale, inProgress), "arrivals leaves a finished item's ..." (workItemAge, the arrivals/workDistribution/startedAndClosed rows); the mutation of the sort model fails 18 of them |
| "a Feature's child items ..." :: "a Feature's child items open with Started, Closed, Work Item Age, Cycle Time and Estimate, oldest first" | `contextColumnsShown()`; `sortedDescendingBy()` | "featureChildren opens with exactly its default columns, in order, sorted by workItemAge" | `shownIn(layout)`; `layout.sortBy` |
| "every chart context that offers more columns ..." :: "$context neither shows nor offers an Estimate where estimation is not set up" for the same 10 | `contextColumnsShown()` equals the defaults without estimate; `catalogueOffered()` fields equal the offered list without estimate | "$context neither shows nor offers an Estimate where estimation is not set up" | `shownIn(layout)` and `fieldsOf(layout)` of the rows with `estimate: null` equal the same two lists |
| "a Feature's child items ..." :: same name for featureChildren and deliveryTimeline | the same two | same test | the same two |
| "every chart context ..." :: "$context lists its defaults, then every other column the rows can fill in the one fixed order" for the same 10, and "a Feature's child items ..." for featureChildren and deliveryTimeline | `catalogueOffered()` fields equal the offered list | "$context lists its defaults, then every other column the rows can fill in the one fixed order" | `fieldsOf(layout)` equals the offered list; and the shown flags are true for the defaults and false for the rest. The panel is the visibility model in key order: "Manage columns offers every other column ..." (kept) asserts `{field, shown}` in order |
| "every chart context ..." :: "$context comes back with the reader's own layout, ..." for closedItems, arrivals, inProgress, aging, blocked, stale, workDistribution, startedAndClosed (8), and "a Feature's child items ..." for featureChildren and deliveryTimeline (2) | `contextColumnsShown()` equals the defaults without the hidden one, plus the next offered | none: dropped per context | the shown flags of the returned `visibilityModel` are `true` for the defaults and `false` for every other column, per context ("$context lists its defaults, then every other column ..."), which says which column is the last default and which is the first one off. That a hidden default and a turned-on column come back after a reopen is the grid's saved layout keyed by context and owner. No test reopens each of the ten other contexts: reopen rests on the generic storage-key tests in `WorkItemsDialog.layout.test.tsx` and the two kept renders for `estimation` and `featureSize` (storage-key mutations in the table above) |
| "the Estimation dialog ..." :: "a category estimate shows its category name" | `cellText("estimate","ST-1")` is `M` | "a category estimate has its category name as its value" | `valueIn(layout, "estimate", row)` is `M`; the kept "each row shows the estimate the chart plots it at ..." renders an estimate cell |
| same :: "the Estimate header reads plain Estimate ..." (3 cases: shared unit, no unit, different units) | `headerText("estimate")` is `Estimate` | "the Estimate header reads plain Estimate when %s" (the same 3 row sets) | the column's `headerName` is `Estimate` |
| same :: "dates show the local day as YYYY-MM-DD, and a date the item does not have stays empty" | three `cellText` on closedDate, startedDate, closedDate of a not finished item | "a date shows the local day as YYYY-MM-DD, and a date the item does not have stays empty" | `valueIn` gives `2026-09-30`, `2026-09-27` and `""`; the kept "Blocked since shows the day an item was blocked ..." renders a date cell |
| "every chart's dialog ..." :: "inProgress neither shows nor offers Time in State when the caller says nothing about how to measure it" (the `aging` case stays as the kept render of the same title, and is not repeated on the function) | `columnsShown()` does not contain timeInState; `catalogueOffered()` fields do not | "inProgress neither shows nor offers Time in State when the caller says nothing about how to measure it" (inProgress only) | `shownIn(layout)` and `fieldsOf(layout)` do not contain `timeInState` |
| same :: "workDistribution / startedAndClosed / featureChildren leaves a finished item's Work Item Age and an unfinished item's Cycle Time empty, never 0, and lists the empty ages last" (the `arrivals` case stays) | four `cellText` on ST-1 and WIP-1 | "%s leaves a finished item's Work Item Age and an unfinished item's Cycle Time empty, never 0" (all four contexts) | `valueIn` gives `null`, `20`, `30`, `null`; the cell shows nothing for `null` (the kept arrivals render) |
| same | `rowOrder()` is WIP-1, WIP-3, ST-1 | none: dropped | Same rows, same sort column as the kept "arrivals leaves ...", which asserts this order; the sort column is asserted per context in the function test |
| same :: "Age on a past day's point still shows for an item finished since, because it was unfinished on that day" | `cellText("workItemAge","ST-1")` is `10` | the test of that name | `valueIn(...)` is `10` |
| same :: "an item not started yet leaves its Work Item Age empty, never 0, and lists it below the started ones" | `cellText` of TODO-1 is empty; `rowOrder()` is WIP-1, WIP-3, TODO-1 | "an item not started yet leaves its Work Item Age empty, never 0" | `valueIn` is `null` for TODO-1, `30` and `3` for the others; the order is dropped, as above (empty last is the grid's, asserted by the kept arrivals render) |
| same :: "Age on a day given as its first moment, as a chosen range end is, is the age on that calendar day" | `cellText` WIP-1 `10`; ST-1 empty | the test of that name | `valueIn` is `10` and `null` |
| same :: "Age on a past day stays empty for an item that had finished by that day" | `cellText` empty | the test of that name | `valueIn` is `null` |
| same :: "Age on a past day's point stays empty for an item that had not started by that day" | `cellText` empty | the test of that name | `valueIn` is `null` |
| "what each catalogue column says ..." :: "Age on a day counts an item started later that day, and not one finished that day or never started" | three `cellText` (`1`, empty, empty) | the test of that name | `valueIn` is `1`, `null`, `null` |
| same :: "Cycle Time reads the named cycle time it is scoped to, and each named column reads its own" | `headerText("cycleTime")`; the three Cycle Time cells; the three named column cells after turning it on | the test of that name | `headerName` is `Review Cycle`; `valueIn` gives `5, null, null`; the named column is in `columns`, off in `visibilityModel`, and its values are `2, 4, null` |
| "US-02 ... explain its point" :: "Feature rows add Size and Forecasted Completion, last, to what Manage columns offers" | the last two offered fields | the test of that name | `fieldsOf(layout).slice(-2)` is `size`, `forecasts`, and `blockedSince` is not offered for Feature rows |
| "what each catalogue column says ..." :: "heads every column the catalogue offers with its name" | the offered headings (7); `headerText("size")`; `headerText("daysContributed")` | the test of that name | the `headerName`s of `columns` in order (Closed, Cycle Time, Started, Work Item Age, Estimate, Parent, Blocked since); `Size`; `Days Contributed` |
| "a Feature's child items ..." :: "the Delivery timeline keeps Warnings as its only extra column and never offers Estimate or Blocked since" | `contextColumnsShown()` (`owningTeam`, `warnings`); offered contains `startedDate`; does not contain `estimate`; does not contain `blockedSince` | the test of that name | `shownIn(layout)` is `warnings`; the same three on `fieldsOf(layout)` for a Feature row that carries an estimate |
| "what each catalogue column says ..." :: "lists every warning a row has, one sentence after another" | `cellText("warnings","FTR-1")` | the test of that name | `valueIn` joins both sentences with a space |

The function file also holds assertions with no old counterpart (a column for each field the model lists, the
returned columns in the model's key order; Cumulative Time per State; the configured word for Cycle Time; the
named-percentile header and offered list) so the settings the removed cases reached are asserted on the function as well.

### Deviations from the declaration

- The declaration listed the pairs on "the six files plus the new file"; the close brief said to run only the touched
  files, which is what was run (see the time section).
- The function file has 76 tests (88 before the review revision), not a count the declaration fixed; the extra ones are listed above.
- Coverage is unchanged rather than raised (see above).

## Review revision

Commit `3e9ad21ec` (test) and the commit carrying this section. No production file changed; `WorkItemsDialog.tsx` was
mutated temporarily and restored (`git diff` of it is empty). The surviving ids in the contexts file are unchanged.

Test ids that changed in `contextGridLayout.test.ts` (new in this slice, so none of them is in the 243 preserved ids):

- Removed (12): "$context has a column to turn on after its last default, which stays off until it is turned on" for each of the
  12 contexts.
- Removed (1): "aging neither shows nor offers Time in State when the caller says nothing about how to measure it". The
  "inProgress ..." case keeps its id.
- Added (1): "heads the columns the caller describes, the forecast and each named cycle time with their names".
- Same id, stronger body: "a named percentile's dialog reads that named cycle time under its name, and offers each named
  cycle time"; "heads every column the catalogue offers with its name" unchanged.

Counts: the function file 88 to 76 tests; the directory 266 to 254 (178 preserved + 76).

| Finding | What was done |
|---|---|
| M1 per-context "column after the last default" case implied by the visibility-model test | Deleted. It was the only home of a moved-assertion row (reopen); that row now says the per-context assertion is dropped and the shown flags per context are asserted by "$context lists its defaults, then every other column ..." |
| M2 reopen claim | Said plainly in the group D row, the wiring row and the moved-assertion row: the per-context reopen assertion for the ten removed contexts is dropped. Reopen rests on the generic storage-key tests in `WorkItemsDialog.layout.test.tsx` and the two kept renders (`estimation`, `featureSize`). No test reopens each of the other ten. |
| M3 header test checked only some headers | Kept the first test as it was (the closed-items catalogue, Size, Days Contributed) and added one test that asserts the headers of the aging layout (Work Item Age, Work Item Age Band, SLE Risk, Time in State, Started, Closed, Cycle Time, Parent, Blocked since), Forecasted Completion, Warnings, and the two named cycle times (Dev Cycle, Review Cycle). |
| M4 named percentile values unasserted | Added: the `cycleTime` values read through `valueIn` for the three rows equal `[2, 15, 6]`. |
| M5 aging duplicated | The case is now `inProgress` only, a plain `it` with the id the `it.each` produced; the doc row says so. |
| M6 call-site claim too broad | See below. The doc now says the mutation table covers the arguments and settings it lists, not every line. |
| M7 `row as never` | The row is handed over as a copy (`{ ...row }`), which the column's row type accepts; no `never`, no `any`. `tsc -b` is clean. |

### M6 mutations not covered in the first table

Each applied alone to `WorkItemsDialog.tsx`, then `LIGHTHOUSE_TIME_BUDGET=warn pnpm exec vitest run
src/components/Common/WorkItemsDialog`, then `git checkout` (diff empty afterwards).

| Change | Failed | Failing surviving tests |
|---|---|---|
| `key={storageKey}` replaced by a constant key | 1 | `WorkItemsDialog.layout.test.tsx`: "each context remembers its own layout ... the same dialog switched to another context while open shows that context's layout" |
| `hasOwningTeams` always false | 6 | "a bubble on a Portfolio's Estimation vs. Cycle Time opens the same way, with Owned by still beside the Features"; "featureSize comes back with the reader's own layout ..."; "Feature rows keep Owned by beside the fixed columns"; `WorkItemsDialog.loading.test.tsx` "a dialog whose items are still on their way lays the context's defaults over rows that arrive after it opened"; two `WorkItemsDialog.test.tsx` "Feature owning team functionality" cases |
| `sortedItems` returns `items` unsorted | 1 | "of two items equally old, lists the one longer in its state first" |
| `parentReferences` without the dedupe | 0 | no failure |
| `parentReferences` without the sort | 1 | `WorkItemsDialog.loading.test.tsx`: "asks once for the same rows' parents, whatever order the rows come in" |

The one "no failure" (dedupe) was also run at the pre-slice commit `92e40dca8` (temporary worktree, same mutation, same
run, removed afterwards): 0 failures there too. It is a pre-existing gap, not a regression of this slice: no test hands
the dialog two rows with the same parent and looks at what is asked for. Not closed here (no production or new render
test in a behaviour-free slice); it can go to the test-authoring track.

Full frontend suite after the revision (`LIGHTHOUSE_TIME_BUDGET=warn pnpm test`): Test Files 597 passed (597), Tests 9234 passed (9234).
