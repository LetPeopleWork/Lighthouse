# Mutation testing: 6428 (Epic 6350 slice 01: every report in one place)

Run 2026-10-27 against local `main` @ `7aac0014b` plus the kill-round tests below. Both stacks. Gate: 80 % kill rate per
stack on the lines slice 01 changed (`git diff 504848e92..HEAD`). Most backend files and some frontend files existed
before the slice, so whole-file scores are given for context and survivors are triaged on the changed lines.

## Backend (Stryker.NET 5.0.0)

Config `stryker.6428.backend.json`, from `Lighthouse.Backend/Lighthouse.Backend.Tests/`, `StrykerOutput` removed first,
`perTestInIsolation`, concurrency 4. "Will be tested": **45** (of 22 805 created). First run 3 m 28 s.

Test filter: `ReportQueriesTest`, `ReportOwnerLookupTest`, `ReportLoggingTest`, `ReportCommandsTest`,
`ReportRepositoryTest`, `Models.Reports`, and the one acceptance class `Slice01EveryReportInOnePlaceScenarios` (9 tests).
That class is included on purpose: `ReportsController.GetReports` is reached only through it. The rest of
`API.Integration`, `Integration.Containers`, `Dogfood` and the live-connector categories are out.

`UsageDataEventName.cs` is an enum member (`AllReportsListed`), which has no mutable statement, so it is not mutated.

| file | whole file, first run | changed lines, first run | changed lines, after kill round |
| --- | --- | --- | --- |
| `API/ReportsController.cs` | 2 / 2, 100 % | 2 / 2 | 2 / 2 |
| `Reports/ReportQueries.cs` | 15 / 23, 65.22 % | 5 / 8 | **8 / 8** |
| `Reports/ReportOwnerLookup.cs` | 8 / 8, 100 % | 2 / 2 | 2 / 2 |
| `Repositories/ReportRepository.cs` | 12 / 17, 70.59 % | 4 / 4 | 4 / 4 |
| `Models/Reports/ReportReadModels.cs` | 0 / 1, 0 % | no mutant on a changed line | |
| **total** | 37 / 51 killed, 72.55 % | **13 / 16 = 81.25 %** | **16 / 16 = 100 %** |

After the kill round `ReportQueries.cs` is 18 / 23 = 78.26 % whole file (re-run with `stryker.6428.backend.kill.json`:
`ReportQueries.cs` only, unit classes only, 2 m 05 s). Whole-file total with the other four files from the first run:
40 / 51 = 78.4 %.

### Kill round (`ReportQueriesTest`, two tests)

- `ListAcross` with nothing readable: the early return (`:53`) was an equivalent-looking survivor because the repository
  returns nothing for no owners anyway. New test: no readable owner means the report reader is never asked.
- Order of `ListAcross` (`:59`, two `ThenBy` to `ThenByDescending`): new test with two Teams named `Bravo` and `alpha`
  pins owner name order ignoring case, and a Team's own reports oldest first.

### Survivors left, all on lines slice 01 did not change

`ReportQueries.cs` `:44` (two mutants in the older `List` path), `:94`, `:104` (no coverage, older), `:142` (a log
string); `ReportRepository.cs` `:27`, `:38`, `:40`, `:49` (older queries read by tests outside this filter);
`ReportReadModels.cs` `:63` (arithmetic on an older record).

## Frontend (StrykerJS 10.0.0)

Config `stryker.6428.frontend.json` and `vitest.stryker.6428.mutation.ts` (copied into `Lighthouse.Frontend/`, gitignored
there). `perTest`, `--incremental` (file `reports/mutation/stryker-incremental-6428.json`, gitignored), concurrency 2,
`inPlace`, `disableTypeChecks: false`, `TZ=Europe/Zurich`. `mutate` is whole files, so whole-file and changed-line
numbers both come from one run (676 mutants, 59 min); changed lines come from `git diff 504848e92..HEAD`. The vitest
include is `src/pages/Reports/**`, `src/pages/Common/Reports/**`, `ReportService.test.ts`, `Header/**`, `StyledLink/**`.

| run | whole file | changed lines |
| --- | --- | --- |
| first run | 539 / 676 = 79.73 % | 143 / 197 = 72.59 % |
| after kill round (incremental, 217 mutants re-tested, 22 min) | 563 / 676 = **83.28 %** | 165 / 197 = **83.76 %** |

| file | changed lines, first | changed lines, after | whole file, after |
| --- | --- | --- | --- |
| `ReportsPage.tsx` | 23 / 40 | 33 / 40 | 82.50 % |
| `useAllReports.ts` | 3 / 3 | 3 / 3 | 100 % |
| `useAllReportsListedReporter.ts` | 12 / 17 | 17 / 17 | 100 % |
| `ReportList.tsx` | 51 / 72 | 58 / 72 | 86.21 % |
| `reportOwner.ts`, `thenRebuildCopy.ts`, `useMayEditReports.ts` | all | all | 100 % |
| `useReportDeletion.tsx` | 8 / 10 | 8 / 10 | 84.38 % |
| `useReportEditing.tsx` | 13 / 18 | 13 / 18 | 80.85 % |
| `useReportPdfExport.ts` | 11 / 14 | 11 / 14 | 82.09 % |
| `ReportWizard.tsx` | 4 / 4 | 4 / 4 | 97.44 % |
| `ReportService.ts` | 2 / 3 | 2 / 3 | 75.00 % |
| `Header.tsx` | 3 / 3 | 3 / 3 | 43.08 % (older code) |
| `StyledLink.tsx` | no mutant on a changed line | | 28.57 % (older code) |

### Kill round (new tests)

- `ReportsPage.reading.test.tsx`: busy (`aria-busy`) while the list is on its way and not once shown; the list stays on
  screen, dimmed, while read again; a row's PDF icon starts that row's export and holds the other rows' icons back; the
  Owner header exists and sorts by owner name; a Team row shows the Team icon and a Portfolio row the Portfolio icon;
  the filter ignores case and surrounding spaces; the filter matches a Type title.
- `ReportsPage.listedOnce.test.tsx`: reading the list again is not another listing; a listing the browser could not yet
  tell about is told once it can, and only once.
- `ReportList.test.tsx`: a list given no actions has no Actions column and no icon.
- `ReportsTab.createWizard.test.tsx`, `ReportView.editWizard.test.tsx`: creating or saving a report makes the Reports
  page's list of every report be read again. These pin the known gap in `ReportWizard.tsx`; Stryker has no mutant that
  removes the `ALL_REPORTS_QUERY_KEY` invalidation (the four mutants on those lines were killed already), so the score
  does not show it, the tests do.

### Accepted survivors (28 on changed lines)

- Layout and style, no behaviour jsdom computes (15): `ReportsPage.tsx` `:41` `maxWidth`, `:42` `:49` `:50` `:51` `sx`;
  `ReportList.tsx` `:176` `:191` `:286` `sx` objects and gap values. MUI `sx` is the dominant equivalent class here.
- Fast path and unreachable strings (5): `ReportList.tsx` `:242` (`needle === ""`: an empty needle matches every row
  anyway), `:222` and `:227` (default `""` that no query can reach), `:293` storage key of the grid state.
- `ReportList.tsx` `:208` `:209` (field and header name of the Owner column): still reported as survivors although a test
  now clicks the Owner header and checks the sort order. Cause not established; StrykerJS is known to report false
  survivors in this repo, so this is recorded as open rather than justified.
- Defence in depth behind a held-back icon (8): `useReportEditing.tsx` `:75`, `useReportPdfExport.ts` `:112`,
  `useReportDeletion.tsx` `:64`. The row's icon is already held back while a read or a PDF is under way (tested), and the
  page always passes the owner, so the guard clauses cannot be reached through the UI.
- `ReportsPage.tsx` `:24` and `:58`, one mutant each (`ConditionalExpression` to `true`): the first makes the page
  report its listing without checking the status, the second dims content before a first answer exists. Both are
  covered by the neighbouring checks in the same expression for every state a test can reach; not killed.
- `ReportService.ts` `:118`: the message of a stub that belongs to a later slice (creating for several owners).

## Gates after the kill round

Only tests changed since the full suites last passed, so the gates were scoped. Backend: `dotnet build` 0 warnings, 0
errors; `ReportQueriesTest` + `ReportLoggingTest` 24 / 24. Frontend: `pnpm build` clean (Biome rewrote three new test
files); the five changed or new test files 89 / 89.
