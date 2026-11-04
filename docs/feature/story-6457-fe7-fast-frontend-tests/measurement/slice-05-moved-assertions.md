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
- The picked count: `picks.picked.length` rendered as `${n} selected` in `ReportsPage.tsx` (line 70 at the pre-slice commit), and as the
  `Remove (n)` / `Export PDF (n)` labels in `useSelectionActions.tsx`. The count itself is `selectedIds`
  resolved against the listed reports in `useRowPicks.ts` (`picked`, `reportOf`).
- Select all: the grid's own header checkbox (MUI DataGrid `checkboxSelection`) writes the ids into
  `useRowPicks`'s `selectedIds` through `onSelectionChange`. What the app owns is the resolving of ids to
  reports and dropping ids that are not in the list (`useRowPicks.ts`, line 15 at the pre-slice commit, the one uncovered branch).
- Already pure, no extraction: `moreReportsThanTheCeiling` and the other builders are test helpers.

### Proposed extractions (files beside the component)

| Extraction | New file | Unit-test file |
|---|---|---|
| Pick limit as exported pure functions over a count: `removeBlockedBecause`, `exportBlockedBecause` and `TOO_MANY_PICKED` moved out of the hook module (hook imports them) | `src/pages/Reports/pickLimit.ts` | `src/pages/Reports/pickLimit.test.ts` |
| Picked reports and picked count as pure functions over a row list and a list of ids (`pickedReports(reports, ids)`, `selectedSentence(count)`, the "N selected" sentence), `useRowPicks` keeps only the `useState` and ref | `src/pages/Reports/pickedReports.ts` | `src/pages/Reports/pickedReports.test.ts` |

Select all stays the grid's job: the 101-row page test becomes a direct check of `pickedReports` / `selectedSentence`
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
| Test count (the two old page files, as measured at the first close) | 44 | 40 |
| Tests added (two new unit files) | n/a | 22 (14 `pickLimit`, 8 `pickedReports`) |
| Tests removed | n/a | 4 at the first close, restored by the review revision |
| Preserved ids missing | n/a | 4 at the first close, 0 after the revision (5 moved file, see below) |
| Line coverage, Reports directory | 99.75 | 99.75 |
| Branch coverage, Reports directory | 96.35 | 96.35 |
| Statements / functions | 99.32 / 99.3 | 99.32 / 99.31 |
| Allow-list length | 28 | 23 |
| Allow-list entries removed / lowered | 6 declared | 5 removed, 0 lowered |

The sixth entry (a coach removes three trial reports) stays: it ran 1.3 s alone but 2.3 s in a whole-directory run,
over the 2 s budget, so deleting it made the guard report it. It is a removal-flow test, not a pick-limit one.

### Review revision

The first close deleted four page tests and shrank Select all to three rows. Review found two gaps: no page test
drove the over-50 path any more (the disabled expression in `useSelectionActions.tsx` could change and every test
would still pass), and nothing proved that Select all reaches rows on other pages. So the four tests and Select all
are page tests again, in a new file, `ReportsPage.pickLimit.test.tsx`. The grid starts on a page of ten rows there, so
50 or 51 listed reports render ten and the page tests stay quick. `pickLimit.test.ts` and `pickedReports.test.ts`
stay as the cheap, direct checks of the same rules.

The page size seam: `DataGridBase` passes `pageSizeOptions={[PAGE_SIZE]}` but no page size, and MUI starts on 100 by
its own default. Mocking `PAGE_SIZE` alone left all 51 rows on one page (51 rendered rows with the constant at 10 or
at 100). The new file therefore also wraps `DataGrid` from `@mui/x-data-grid` so it starts with
`initialState.pagination.paginationModel.pageSize` at 10, and sets `PAGE_SIZE` to 10 so the footer shows. With that,
10 rows are rendered while "51 selected" is on the page.

Test names that claimed more than they checked were renamed (they are new in this slice): `pickedReports.test.ts`
"returns every listed report when every id is picked"; `pickLimit.test.ts` "gives no reason to hold back at %i
picked" (Remove and Export PDF); and the notAdmin test, "says one of the picked reports is on an owner she is not
Admin of". The allow-list reason of the kept removal entry now says why it stays.

Breaks tried against `ReportsPage.pickLimit.test.tsx` (5 tests), each reverted afterwards:

| Break | Result |
|---|---|
| Export PDF `disabled={blockedBecause !== ""}` becomes `disabled={picked.length === 0}` | 1 failed: `above 50 picked, Export PDF is disabled and says at most 50` |
| Remove's disabled expression loses `\|\| blockedBecause !== ""` | 1 failed: `above 50 picked, Remove is disabled and says at most 50` |
| Page size set back to 100 in the test | 5 passed. Select all cannot fail on this: the grid selects every row whatever the page size. What the ten-row page adds is the render check above (10 rows shown, 51 selected). |

### Preserved ids

Before 44, after 44. Five ids kept their test name and suite title and changed the file part, from the two page
files to `ReportsPage.pickLimit.test.tsx`. Four of them kept their body as well. "Select all picks every row, on every
page" changed its body too: it was 101 rows over the production page size of 100 and now is 51 rows, so the second page
is proven at a test-only page size of 10, not at the production size of 100 (see Wiring that stays for the seam).
The five ids:

- removal :: Picking reports Select all picks every row, on every page
- removal :: Picking reports above 50 picked, Remove is disabled and says at most 50
- removal :: Picking reports exactly 50 picked may still be removed
- combinedPdf :: One PDF of several reports above 50 picked, Export PDF is disabled and says at most 50
- combinedPdf :: One PDF of several reports exactly 50 picked may still be exported as one PDF

Page tests: 39 in the two old files plus 5 in the new one, 44. Added unit tests: 22 (14 `pickLimit`, 8 `pickedReports`).
The Reports directory runs 190 tests in 17 files (it was 168 in 14).

New file, plain run, every test under the 2 s budget (no allow-list entry added): Select all 0.69 s, Remove above 50
0.45 s, Remove at 50 0.39 s, Export PDF above 50 0.30 s, Export PDF at 50 0.29 s. With the machine busier an earlier
run took 0.7 to 1.5 s per test. The guard reports 0 offenders on the Reports directory.

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

The first close left one thing unproven through a render: that Select all reaches rows beyond the first page. The
Review revision above puts that check back (ten rows rendered, 51 selected).

## Moved assertions

Each old assertion is still asserted on the page, now in `ReportsPage.pickLimit.test.tsx` under the same test name and
suite title, and the limit and the count are also checked without a render in `pickLimit.test.ts` and
`pickedReports.test.ts`.

| Old test (file :: test) | Assertion text | New location (file) | New test (name) |
|---|---|---|---|
| ReportsPage.removal.test.tsx :: Picking reports above 50 picked, Remove is disabled and says at most 50 | `expect(remove).toBeDisabled();` | ReportsPage.pickLimit.test.tsx | Picking reports above 50 picked, Remove is disabled and says at most 50 |
| ReportsPage.removal.test.tsx :: Picking reports above 50 picked, Remove is disabled and says at most 50 | `expect(await screen.findByRole("tooltip")).toHaveTextContent(` | ReportsPage.pickLimit.test.tsx | Picking reports above 50 picked, Remove is disabled and says at most 50 |
| ReportsPage.removal.test.tsx :: Picking reports exactly 50 picked may still be removed | `expect(` (`removeButton(50)` `toBeEnabled()`) | ReportsPage.pickLimit.test.tsx | Picking reports exactly 50 picked may still be removed |
| ReportsPage.removal.test.tsx :: Picking reports Select all picks every row, on every page | `expect(screen.getByText(selectedCount(101), { exact: true }))` | ReportsPage.pickLimit.test.tsx | Picking reports Select all picks every row, on every page (51 listed, ten per page, `selectedCount(51)`) |
| ReportsPage.combinedPdf.test.tsx :: One PDF of several reports above 50 picked, Export PDF is disabled and says at most 50 | `expect(exportSeveral).toBeDisabled();` | ReportsPage.pickLimit.test.tsx | One PDF of several reports above 50 picked, Export PDF is disabled and says at most 50 |
| ReportsPage.combinedPdf.test.tsx :: One PDF of several reports above 50 picked, Export PDF is disabled and says at most 50 | `expect(await screen.findByRole("tooltip")).toHaveTextContent(` | ReportsPage.pickLimit.test.tsx | One PDF of several reports above 50 picked, Export PDF is disabled and says at most 50 |
| ReportsPage.combinedPdf.test.tsx :: One PDF of several reports exactly 50 picked may still be exported as one PDF | `expect(` (`exportButton(50)` `toBeEnabled()`) | ReportsPage.pickLimit.test.tsx | One PDF of several reports exactly 50 picked may still be exported as one PDF |
