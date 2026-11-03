# Slice 05 - Reports pages: universe, preserved set, delta

All timing figures in this slice are indicative (not quiet): the rewrite slices start before the
configuration spike (maintainer decision 2026-11-03). The machine was shared with a parallel session.

## Pre-slice commit

`98b5ca9f5255fd129c6e83be309504d62331f9c7` (HEAD of main when this was declared). The close-time alternating
before/after timing pairs check this commit out for the "before" side.

## Universe

Nothing outside this list may change in the slice.

### Search result: what is real logic inside the component

All under `src/pages/Reports/`:

- The pick limit: `MOST_PER_BULK_RUN = 50` lives in `src/models/Reports/ReportsAcrossOwners.ts` (shared with the
  bulk wizard's `OwnerStep.tsx`; not touched). It is applied in `useSelectionActions.tsx` by two private pure
  functions: `removeBlockedBecause(picked, permissionsFailed, notAdmin, getTerm)` (too many picked wins, then
  failed permissions, then picks on owners she cannot change) and `exportBlockedBecause(picked)` (nothing
  picked, then too many picked). `TOO_MANY_PICKED` is the shared sentence. They are pure but private to a file
  that is a hook module; no direct unit test, only reached by rendering the page and selecting 51 rows.
- The picked count: `picks.picked.length` rendered as `${n} selected` in `ReportsPage.tsx` (line 70), and as the
  `Remove (n)` / `Export PDF (n)` labels in `useSelectionActions.tsx`. The count itself is `selectedIds`
  resolved against the listed reports in `useRowPicks.ts` (`picked`, `reportOf`).
- Select all: the grid's own header checkbox (MUI DataGrid `checkboxSelection`) writes the ids into
  `useRowPicks`'s `selectedIds` through `onSelectionChange`. What the app owns is the resolving of ids to
  reports and dropping ids that are not in the list (`useRowPicks.ts`, line 15, the one uncovered branch).
- Already pure, no extraction: `moreReportsThanTheCeiling` and the other builders are test helpers.

### Proposed extractions (files beside the component)

| Extraction | New file | Unit-test file |
|---|---|---|
| Pick limit as exported pure functions over a count: `removeBlockedBecause`, `exportBlockedBecause` and `TOO_MANY_PICKED` moved out of the hook module (hook imports them) | `src/pages/Reports/pickLimit.ts` | `src/pages/Reports/pickLimit.test.ts` |
| Picked reports and picked count as pure functions over a row list and a list of ids (`pickedReports(reports, ids)`, `pickedCount`, the "N selected" sentence), `useRowPicks` keeps only the `useState` and ref | `src/pages/Reports/pickedReports.ts` | `src/pages/Reports/pickedReports.test.ts` |

Select all stays the grid's job: the 101-row page test becomes a direct check of `pickedReports` / `pickedCount`
over 101 rows plus one wiring test that clicks the header checkbox (see Target). The exact function split is
decided in the extraction step after reading the files closely; the file names above are the commitment.

### Production files that may change

- `src/pages/Reports/useSelectionActions.tsx` (import from `pickLimit.ts`)
- `src/pages/Reports/useRowPicks.ts` (use `pickedReports.ts`)
- `src/pages/Reports/ReportsPage.tsx` (only if the "N selected" sentence moves)
- the two new production files above

The pre-slice step note named `ReportsPage.tsx`; the logic is in `useSelectionActions.tsx` and `useRowPicks.ts`,
so those are named instead.

### Test files edited (existing)

- `src/pages/Reports/ReportsPage.removal.test.tsx`
- `src/pages/Reports/ReportsPage.combinedPdf.test.tsx`

### Allow-list entries (`src/tests/timeBudget/slowTests.ts`) belonging to these files

Six entries, keyed by file and name. May be removed or lowered only once the test runs under budget.

combinedPdf (`src/pages/Reports/ReportsPage.combinedPdf.test.tsx`, const `reportsCombinedPdfFile`):
1. `One PDF of several reports above 50 picked, Export PDF is disabled and says at most 50` (3.0 s)
2. `One PDF of several reports exactly 50 picked may still be exported as one PDF` (2.5 s)

removal (`src/pages/Reports/ReportsPage.removal.test.tsx`, const `reportsRemovalFile`):
3. `Picking reports Select all picks every row, on every page` (4.5 s)
4. `Picking reports above 50 picked, Remove is disabled and says at most 50` (3.0 s)
5. `Picking reports exactly 50 picked may still be removed` (2.5 s)
6. `Remove several reports at once a coach removes three trial reports on two Teams and a Portfolio, and they leave the list` (2.5 s)

Entry 6 is a removal-flow test, not a pick-limit one; it stays unless it runs under budget after the slice. The
two consts go with the entries that use them.

## Target

`ReportsPage.removal.test.tsx` :: `Picking reports Select all picks every row, on every page`
(describe "Picking reports", timeout 15 s).

- Timing: 1.97 s per the feature-delta record ("5th slowest test of 8,970"); 2.03 s in this declaration's run
  (indicative); calibration seed plain median 1.78 s, ceiling 4.5 s.
- What it does: renders the page with `moreReportsThanTheCeiling(ZENITH, 51)` (101 reports; Admin of ZENITH),
  clicks the `Select all` header checkbox.
- Assertion: `expect(screen.getByText(selectedCount(101), { exact: true })).toBeInTheDocument();`
  i.e. the page says 101 selected.

Its cousin in `ReportsPage.combinedPdf.test.tsx` (`above 50 picked, Export PDF is disabled and says at most 50`)
and the removal one (`above 50 picked, Remove is disabled and says at most 50`) do the same Select all over
more than 50 rows and assert the disabled button and its tooltip sentence; those are the other callers of the
pick limit.

## Preserved set (before)

`measurement/slice-05-ids-before.txt`: 44 ids, one per line, sorted, as `file :: fullName`, taken from a run of
the two files (44 passed, 0 skipped, 0 failed; `LIGHTHOUSE_TIME_BUDGET=off`, vitest json reporter). Surviving
ids must be unchanged at close.

## Before figures

Summed test durations of the two files, 3 runs (default local worker cap, 12 cores so 6 workers, budget off):
30.07 s, 29.36 s, 29.76 s; mean **29.73 s**. Indicative, shared machine.

Coverage, apart from timing: `LIGHTHOUSE_TIME_BUDGET=off pnpm exec vitest run src/pages/Reports --coverage
--coverage.include='src/pages/Reports/**'` (14 test files, 168 tests, all passed).

| Scope | % Stmts | % Branch | % Funcs | % Lines |
|---|---|---|---|---|
| All files (`src/pages/Reports/**`) | 99.32 | 96.35 | 99.3 | 99.75 |

Totals line: `All files | 99.32 | 96.35 | 99.3 | 99.75`. Only `useRowPicks.ts` is below 100 (branch 75, line 15).
Line coverage (99.75) and branch coverage (96.35) must not be lower at close.

## Delta (close)

Figures are indicative (not quiet): 5 alternating before/after pairs of the touched test files, same minute, same
worker count (default local cap), `LIGHTHOUSE_TIME_BUDGET=off`, machine shared with a parallel session (load average
about 14 on 12 cores). Before arm: a worktree of `98b5ca9f5` running the two page files. After arm: this checkout
running the two page files plus `pickLimit.test.ts` and `pickedReports.test.ts`. Summed test seconds per run:

| Pair | Before (44 tests) | After (62 tests) |
|---|---|---|
| 1 | 29.70 | 24.78 |
| 2 | 34.27 | 27.88 |
| 3 | 35.19 | 24.23 |
| 4 | 29.93 | 23.84 |
| 5 | 28.74 | 21.21 |
| Sum | 157.83 | 121.94 |

Relative change in summed test seconds: **-22.7 %** (mean 31.57 s to 24.39 s per run).

| Measure | Before | After |
|---|---|---|
| Test count (two page files) | 44 | 40 |
| Tests added (two new unit files) | n/a | 22 (14 `pickLimit`, 8 `pickedReports`) |
| Tests removed | n/a | 4 |
| Preserved ids missing | n/a | 4 (listed below) |
| Line coverage, Reports directory | 99.75 | 99.75 |
| Branch coverage, Reports directory | 96.35 | 96.35 |
| Statements / functions | 99.32 / 99.3 | 99.32 / 99.31 |
| Allow-list length | 28 | 23 |
| Allow-list entries removed / lowered | 6 declared | 5 removed, 0 lowered |

The sixth entry (a coach removes three trial reports) stays: it ran 1.3 s alone but 2.3 s in a whole-directory run,
over the 2 s budget, so deleting it made the guard report it. It is a removal-flow test, not a pick-limit one.

### Preserved ids

Before 44, after 40. Missing: 4, the tests that rendered 50 to 101 rows only to reach the limit:

- combinedPdf :: One PDF of several reports above 50 picked, Export PDF is disabled and says at most 50
- combinedPdf :: One PDF of several reports exactly 50 picked may still be exported as one PDF
- removal :: Picking reports above 50 picked, Remove is disabled and says at most 50
- removal :: Picking reports exactly 50 picked may still be removed

Added to the two files: 0. Surviving ids are unchanged. `Select all picks every row, on every page` keeps its id; it now
uses three reports (the 101-row check is in `pickedReports.test.ts`).

### Wiring that stays

The call sites are covered by page tests that remain. Broken one at a time in the component, each makes a test fail
(then reverted):

| Break | Failing tests |
|---|---|
| `useRowPicks`: picked reports built from only the first two ids | `Select all picks every row, on every page` ("Unable to find an element with the text: 3 selected") and 20 more |
| `useRowPicks`: `onSelectionChange` stores nothing | `Select all picks every row, on every page` and 7+ more |
| `useSelectionRemoval`: count passed to `removeBlockedBecause` is 0 | `with one picked report on an owner she only reads, Remove is disabled and says why`, `with two picked reports on owners she only reads, the reason says they are` |
| `useSelectionExport`: count passed to `exportBlockedBecause` is 1 | `with nothing picked Export PDF is disabled and says why` |
| `ReportsPage`: the sentence is given 0 instead of the picked count | `Select all picks every row, on every page`, `sorting keeps what was picked`, `Escape at the confirmation closes it, removes nothing and keeps what was picked` |

What is no longer proven through a render: that the grid's header checkbox reaches rows beyond the first page of 100
(the page now selects three rows). The grid's paging behaviour belongs to `DataGridBase`, outside this slice's universe.

## Moved assertions

Count removed `expect(` per file with `git diff -U0 fdc13fe86 -- <file> | grep -c '^-.*expect('`: removal 3, combinedPdf 3.
The diff shows the `expect(` of the two multi-line "exactly 50" assertions as the removed line; the Select all
assertion keeps its `expect(` line and changes only its argument (101 to 3), so it is a row below as well.

| Old test (file :: test) | Assertion text | New location (file) | New test (name) |
|---|---|---|---|
| ReportsPage.removal.test.tsx :: Picking reports above 50 picked, Remove is disabled and says at most 50 | `expect(remove).toBeDisabled();` | pickLimit.test.ts | The most reports that may be picked Remove is held back above 50, at %i picked |
| ReportsPage.removal.test.tsx :: Picking reports above 50 picked, Remove is disabled and says at most 50 | `expect(await screen.findByRole("tooltip")).toHaveTextContent(` | pickLimit.test.ts | The most reports that may be picked Remove is held back above 50, at %i picked |
| ReportsPage.removal.test.tsx :: Picking reports exactly 50 picked may still be removed | `expect(` (`removeButton(50)` `toBeEnabled()`) | pickLimit.test.ts | The most reports that may be picked Remove may still remove %i picked reports |
| ReportsPage.removal.test.tsx :: Picking reports Select all picks every row, on every page | `expect(screen.getByText(selectedCount(101), { exact: true }))` | pickedReports.test.ts | The reports picked Select all over 101 listed reports picks every one, on every page |
| ReportsPage.combinedPdf.test.tsx :: One PDF of several reports above 50 picked, Export PDF is disabled and says at most 50 | `expect(exportSeveral).toBeDisabled();` | pickLimit.test.ts | The most reports that may be picked Export PDF is held back above 50, at %i picked |
| ReportsPage.combinedPdf.test.tsx :: One PDF of several reports above 50 picked, Export PDF is disabled and says at most 50 | `expect(await screen.findByRole("tooltip")).toHaveTextContent(` | pickLimit.test.ts | The most reports that may be picked Export PDF is held back above 50, at %i picked |
| ReportsPage.combinedPdf.test.tsx :: One PDF of several reports exactly 50 picked may still be exported as one PDF | `expect(` (`exportButton(50)` `toBeEnabled()`) | pickLimit.test.ts | The most reports that may be picked Export PDF may still export %i picked reports |
