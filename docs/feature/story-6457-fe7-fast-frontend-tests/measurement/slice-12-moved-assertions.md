# Slice 12 - The sweep of the remaining slow tests: universe, preserved set, delta

All timing figures in this slice are indicative (not quiet): the machine was shared with parallel sessions (load
average 10 to 14 on 4 cores at the start of the run, 4 to 10 later).

## Pre-slice commit

`2e71d46519feb264947100f8f71b0277f0983b9b` (HEAD of main when this was declared). The close-time alternating
before/after timing pairs check this commit out for the "before" side.

## Universe

Nothing outside this list may change in the slice. The slice is behaviour-free: no production behaviour changes, no
assertion is weakened or dropped without a row in the table at the end, no surviving test id changes, no added
skip/todo/only/fails, no looser matcher, no longer timeout.

### The tail, by name

`src/tests/timeBudget/slowTests.ts` as it stands after the last shipped rewrite slice lists 13 entries, over 10 files.
The 11th file is `featureChildItemsDialog.test.tsx`, named by the step although it has no entry. Slice 11 (metrics
views) was dropped on measured payoff (12-01, `slice-11-moved-assertions.md`), so `BaseMetricsView*` is not part of
this sweep; `DashboardHeader.test.tsx` is, as it has an entry.

Seconds below are the summed per-test durations of the file in one run of all 11 files (161 tests, all passed,
`LIGHTHOUSE_TIME_BUDGET=warn`, `LIGHTHOUSE_TEST_WORKERS=2`), and the slowest test of an entry in that run. The
ceiling is the entry's allowed seconds on a quiet machine.

| # | File and entry | Tests | File seconds | Entry test, this run | Ceiling |
|---|---|---|---|---|---|
| 1 | `components/Common/DataGrid/DataGridBase.paging.test.tsx`, "...never scrolls for a page change the reader did not ask for with an arrow" | 28 | 7.50 | 1.18 | 3.0 |
| 2 | `components/Common/ProjectSettings/ModifyProjectSettings.queryCommit.test.tsx`, "...saves the edited query once, after the field is left" | 6 | 2.81 | 0.81 | 3.0 |
| 3 | `components/Common/Team/ModifyTeamSettings.queryCommit.test.tsx`, "...saves a changed name after the debounce..." | 10 | 5.17 | 1.26 | 2.5 |
| 4 | same file, "...saves the edited query once, after the field is left" | | | 0.78 | 3.0 |
| 5 | same file, "...saves the latest edit after the save already on its way..." | | | 0.99 | 3.0 |
| 6 | `components/Common/Team/ModifyTeamSettings.refinementNeed.test.tsx`, "...keeps an edited Ready rule when the Yes votes needed change..." | 47 | 14.81 | 1.21 | 2.5 |
| 7 | same file, "...saves a changed Ready rule and leaves the Being refined rule as it was" | | | 1.03 | 2.5 |
| 8 | `components/Common/WorkItemsDialog/WorkItemsDialog.paging.test.tsx`, "...Priya, on the second page of 243, filters to 64..." | 1 | 1.55 | 1.55 | 3.5 |
| 9 | `pages/Common/MetricsView/DashboardHeader.test.tsx`, "...opens popover which calls handlers" | 13 | 1.33 | 1.20 | 2.5 |
| 10 | `pages/Reports/ReportsPage.removal.test.tsx`, "...a coach removes three trial reports on two Teams and a Portfolio..." | 21 | 9.03 | 0.86 | 2.5 |
| 11 | `pages/Reports/ReportsPage.usageData.test.tsx`, "...a Retry and a Leave it out on one PDF of several reports..." | 10 | 4.25 | 0.79 | 2.5 |
| 12 | `pages/Settings/SystemInfo/ThirdPartyPackagesSection.paging.test.tsx`, "...keeps the reader's sort and column filter when a search opens the first page" | 8 | 6.32 | 3.18 | 2.5 |
| 13 | `pages/Teams/Detail/Refinement/RefinementView.paging.test.tsx`, "...Jonas pages to GR-0131... and votes on it" | 2 | 8.03 | 6.40 | 5.0 |
| - | `components/Common/FeatureListDataGrid/featureChildItemsDialog.test.tsx` (no entry) | 15 | 5.34 | slowest test under 0.7 | - |
| | Sum | 161 | 66.13 | | |

### Decision per entry

Rule: an entry that renders a whole form or page by design, and is under its ceiling when quiet, stays. Only a quiet
run (13-03) may delete an entry on timing alone, so every entry that runs under its ceiling here is marked "stays
until the quiet run decides". A rewrite is declared only where the saving is about 1 s or more per file and the logic
can be asserted on a function without losing a wiring test.

| # | Decision | Reason | Expected saving |
|---|---|---|---|
| 1 DataGridBase.paging | stays until the quiet run decides | One test steps through the pages with the keyboard and checks the scroll after each step; the scroll is a property of the rendered grid. 1.18 s under load of 10, well under 3.0 | 0 |
| 2 ModifyProjectSettings.queryCommit | stays until the quiet run decides | The whole form types a query and leaves the field: the blur-to-save wiring is the point. 0.81 s | 0 |
| 3-5 ModifyTeamSettings.queryCommit | stays until the quiet run decides | Debounce, blur and in-flight save are timing behaviour of the form with real timers; moving them to a function test would lose the wiring. 0.78 to 1.26 s | 0 |
| 6-7 ModifyTeamSettings.refinementNeed | stays until the quiet run decides | Edits a stage rule in the real form and checks what the save carries; 1.0 to 1.2 s. The file's other 45 tests are already fast (0.3 s each on average) | 0 |
| 8 WorkItemsDialog.paging | stays until the quiet run decides | One test, 243 rows, second page, filter to 64: the single render of a long list the file exists for (already merged down in an earlier slice). 1.55 s | 0 |
| 9 DashboardHeader | stays until the quiet run decides | The first open of the popover loads the date picker lazily: that load is the cost, not logic. 1.20 s | 0 |
| 10 ReportsPage.removal | stays until the quiet run decides | The note on the entry says about 1.3 s alone; 0.86 s here. Renders the page and removes three reports across Teams and a Portfolio: wiring | 0 |
| 11 ReportsPage.usageData | stays until the quiet run decides | Walks failed export, retry and leave-out through the page: usage-data reporting is wiring. 0.79 s | 0 |
| 12 ThirdPartyPackagesSection.paging | stays; candidate for 13-02 only if the quiet run still reads above 2.5 s | 3.18 s under load, the only entry over its ceiling together with 13. One test walks sort, column filter and search through one render of a long list; a split would add a render, not remove one. No saving of 1 s is demonstrable from the file | 0 declared (up to about 1 s if a repeated render can be shared, to be shown by the quiet run) |
| 13 RefinementView.paging | stays; candidate for 13-02 only if the quiet run still reads above 5.0 s | 6.40 s under load. The file was already rewritten (page boundaries, numbering and the filtered result sit on the functions, the 143 rows render once). What is left is the one render of 143 rows, paging to the 131st and a vote: wiring | 0 declared |
| FeatureListDataGrid `featureChildItemsDialog` | stays, no entry needed | 15 tests, 5.34 s, none above 0.7 s; nothing to delete and no single test to rewrite | 0 |

Expected saving of the sweep: no rewrite is declared, since none of the 11 files shows a single removable cost of
about 1 s on the measured run. The declared output of the slice is therefore the quiet run (13-03): entries that run
well under their ceiling when quiet are deleted from `slowTests.ts` (the run itself fails an entry that is well under
budget), entries 12 and 13 are re-examined if the quiet run puts them over their ceiling. Any rewrite that step 13-02
takes on needs a row below and must name the saving it expects before it is made.

### Extractions

None declared.

### Production files that may change

None.

### Test files edited (existing)

None declared. If 13-02 edits one, it is added here with a row in the table below before the edit.

### Allow-list entries (`src/tests/timeBudget/slowTests.ts`)

13 entries today (table above). Only the quiet run may delete one; a deleted entry needs its measured quiet seconds
in the close report. No ceiling is raised.

## Preserved set (before)

`measurement/slice-12-ids-before.txt`: 161 ids, one per line, sorted, as `file :: fullName` (paths relative to
`Lighthouse.Frontend/`), from a run of the 11 files (161 passed, 0 skipped, 0 failed). Per file: DataGridBase.paging 28,
featureChildItemsDialog 15, ModifyProjectSettings.queryCommit 6, ModifyTeamSettings.queryCommit 10,
ModifyTeamSettings.refinementNeed 47, WorkItemsDialog.paging 1, DashboardHeader 13, ReportsPage.removal 21,
ReportsPage.usageData 10, ThirdPartyPackagesSection.paging 8, RefinementView.paging 2. Surviving ids must be unchanged
at close; at close an `slice-12-ids-after.txt` is saved from a run of the same 11 files, and any id missing from it
needs a row in the table below.

Command (in `Lighthouse.Frontend`):

```
LIGHTHOUSE_TEST_WORKERS=2 LIGHTHOUSE_TIME_BUDGET=warn TZ=Europe/Zurich pnpm exec vitest run \
  src/components/Common/DataGrid/DataGridBase.paging.test.tsx \
  src/components/Common/ProjectSettings/ModifyProjectSettings.queryCommit.test.tsx \
  src/components/Common/Team/ModifyTeamSettings.queryCommit.test.tsx \
  src/components/Common/Team/ModifyTeamSettings.refinementNeed.test.tsx \
  src/components/Common/WorkItemsDialog/WorkItemsDialog.paging.test.tsx \
  src/pages/Common/MetricsView/DashboardHeader.test.tsx \
  src/pages/Reports/ReportsPage.removal.test.tsx \
  src/pages/Reports/ReportsPage.usageData.test.tsx \
  src/pages/Settings/SystemInfo/ThirdPartyPackagesSection.paging.test.tsx \
  src/pages/Teams/Detail/Refinement/RefinementView.paging.test.tsx \
  src/components/Common/FeatureListDataGrid/featureChildItemsDialog.test.tsx --reporter=json
```

(The json report lands in `test-results.json`, which is git-ignored.)

## Before figures (indicative, not quiet)

Summed per-test durations of the touched files, one run: see the "File seconds" column above. Sum 66.13 s over 161
tests. The close-time alternating pairs compare these against the pre-slice commit.

## Coverage (before)

Directories of the 11 files, from one run of all tests in them (148 files, 2755 tests, all passed):

```
LIGHTHOUSE_TEST_WORKERS=2 LIGHTHOUSE_TIME_BUDGET=off TZ=Europe/Zurich pnpm exec vitest run \
  src/components/Common/DataGrid src/components/Common/ProjectSettings src/components/Common/Team \
  src/components/Common/WorkItemsDialog src/components/Common/FeatureListDataGrid src/pages/Common/MetricsView \
  src/pages/Reports src/pages/Settings/SystemInfo src/pages/Teams/Detail/Refinement \
  --coverage.enabled --coverage.reportOnFailure --coverage.include='<each directory>/**' --coverage.reporter=text
```

| Directory | % Stmts | % Branch | % Funcs | % Lines |
|---|---|---|---|---|
| All nine directories | 96.45 | 92.52 | 95.72 | 96.75 |
| `src/components/Common/DataGrid` | 83.91 | 83.33 | 91.26 | 84.80 |
| `src/components/Common/FeatureListDataGrid` | 97.45 | 88.97 | 93.10 | 97.41 |
| `src/components/Common/ProjectSettings` | 87.17 | 77.67 | 54.54 | 87.01 |
| `src/components/Common/ProjectSettings/Advanced` | 95.65 | 74.28 | 88.23 | 95.55 |
| `src/components/Common/Team` | 96.25 | 86.30 | 89.36 | 96.09 |
| `src/components/Common/WorkItemsDialog` | 99.54 | 95.74 | 100 | 100 |
| `src/pages/Common/MetricsView` | 97.73 | 95.31 | 97.50 | 97.84 |
| `src/pages/Reports` | 99.33 | 96.29 | 99.31 | 99.75 |
| `src/pages/Settings/SystemInfo` | 97.33 | 90.43 | 96.10 | 98.12 |
| `src/pages/Teams/Detail/Refinement` | 100 | 99.07 | 100 | 100 |

A first coverage run under load of 10 timed out in three tests (`ModifyTeamSettings.refinement.test.tsx` two,
`DataGridBase.paging` one); the rerun above passed all 2755.

## Table of removed or shrunk assertions

No row: nothing is removed or shrunk at declaration. Rows are added here before any edit in 13-02.

## Entries 12 and 13 under load (2026-11-04)

Entries 12 (`ThirdPartyPackagesSection.paging`, 3.18 s) and 13 (`RefinementView.paging`, 6.40 s) ran over their
ceilings under load, so a local `pnpm test` on a busy machine can fail on them. The maintainer's decision on local
load (2026-11-03) is to leave the guard enforcing and to re-run when the only failures are these load-only ones.

## Allow-list review from CI timings (13-02, 2026-11-04)

13-03 (the local quiet run) was dropped by the maintainer, so the CI timings of the final code decide. Source: the
`test-timings-frontend` artifacts of main runs 41812134355 and 41819715132, both passed. They hold one row per test
(file, name, duration in ms, outcome), so per-test seconds can be read. Both runs are in the slow group: the Vitest
Duration was 1,109 s and 1,133 s, the CSV test sums 1,597 s and 1,634 s (9,367 and 9,378 rows, all Passed). These are
coverage runs, so a reading is judged at limit x 3 (unlisted limit 6 s, 6.31 s for a first test in a file).

Rule used: delete an entry only if both CI readings are under 4.5 s and the loaded local reading in the table above
is under 1.5 s. Otherwise it stays.

| # | Entry (test) | CI run 41812134355 (s) | CI run 41819715132 (s) | Local, loaded (s) | Decision | Reason |
|---|---|---|---|---|---|---|
| 1 | DataGridBase.paging, never scrolls for a page change ... | 4.19 | 4.44 | 1.18 | Deleted | Both under 4.5, local under 1.5 |
| 2 | ModifyProjectSettings.queryCommit, saves the edited query once | 2.30 | 2.36 | 0.81 | Deleted | Same |
| 3 | ModifyTeamSettings.queryCommit, saves a changed name after the debounce | 3.25 | 3.14 | 1.26 | Deleted | Same |
| 4 | ModifyTeamSettings.queryCommit, saves the edited query once | 2.40 | 2.36 | 0.78 | Deleted | Same |
| 5 | ModifyTeamSettings.queryCommit, saves the latest edit ... | 3.08 | 3.20 | 0.99 | Deleted | Same |
| 6 | ModifyTeamSettings.refinementNeed, keeps an edited Ready rule | 3.04 | 3.50 | 1.21 | Deleted | Same |
| 7 | ModifyTeamSettings.refinementNeed, saves a changed Ready rule | 2.40 | 2.96 | 1.03 | Deleted | Same |
| 8 | WorkItemsDialog.paging, Priya on the second page of 243 | 4.83 | 4.99 | 1.55 | Stays, ceiling 3.5 kept | CI over 4.5 and local over 1.5; renders 243 Work Items |
| 9 | DashboardHeader, opens popover | 3.08 | 3.19 | 1.20 | Deleted | Both under 4.5, local under 1.5 |
| 10 | ReportsPage.removal, removes three trial reports | 3.00 | 2.54 | 0.86 | Deleted | Same |
| 11 | ReportsPage.usageData, Retry and Leave it out | 3.32 | 3.65 | 0.79 | Deleted | Same |
| 12 | ThirdPartyPackagesSection.paging, keeps sort and column filter | 3.16 | 3.54 | 3.18 | Stays, ceiling 2.5 kept | Local reading 3.18 is over 1.5 |
| 13 | RefinementView.paging, Jonas pages to GR-0131 | 8.74 | 8.63 | 6.40 | Stays, ceiling 5.0 kept | CI and local both high; one render of 143 Work Items |
| R1 | ModifyTeamSettings.readiness, back on at its last allowed threshold | 3.74 | 3.89 | n/a | No entry | Both CI readings under 4.5 |
| R2 | ModifyTeamSettings.readiness, back on at the threshold it was changed to | 2.64 | 2.78 | n/a | No entry | Both CI readings under 4.5 |

Ceilings: none raised, none lowered. A lower ceiling backed by CI would be 2.0 for entry 8 (4.99 / 3), 1.5 for entry 12
(3.54 / 3) and 3.0 for entry 13 (8.74 / 3), but the loaded local readings (1.55, 3.18, 6.40) sit at or over those
values, so lowering would only fail local runs on a busy machine. The quiet local figure that would justify them was
the dropped step 13-03.

Allow-list length: 13 before, 3 after (the seed step set it at 30). Budget B stays 2 s: the D4 rule needs every test
over its threshold in the calibration samples to sit in a file a rewrite slice names, and the three remaining entries
already are the tail; moving to 1 s would put the many 1 to 2 s tests unlisted.

Unlisted offenders and stale findings: see the run result recorded in `feature-delta.md` Phase 13.

Preserved ids: `slice-12-ids-after.txt` is saved from a run of the same 11 files and compared with the before list;
see the line below.

Result of the final code (2026-11-04): `LIGHTHOUSE_TIME_BUDGET=warn pnpm test` passed 607 files and 9,378 tests with no
stale finding. Five unlisted offenders showed on this shared machine, each only just over the 2 s limit (2.02 to 2.68 s,
loaded, local); their CI readings are all under 4.5 s, so none gets an entry. `slice-12-ids-after.txt` (161 ids, run of
the same 11 files) is identical to `slice-12-ids-before.txt`: no id changed, none missing.
