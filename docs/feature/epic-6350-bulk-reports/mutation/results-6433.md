# Mutation testing: 6433 (Epic 6350 slice 05: remove reports in bulk)

Run 2026-10-30 against local `main` @ `d1fb37858` (production code frozen) plus the kill-round tests below. Gate: 80 %
kill rate per stack on the lines slice 05 changed.

The slice's commits are the `step 06-0x` commits from `84a66897a` (06-01) on, plus `742537a1e`, `35b5d4ec2`,
`ec4a04531` and `d7956f614`. Other sessions' commits are interleaved, so the changed lines are the lines that
`git blame HEAD` attributes to one of those commits, not a plain range diff. Type-only and import-only lines are left
out.

## Backend (Stryker.NET 5.0.0)

Config `stryker.6433.backend.json`, from `Lighthouse.Backend/Lighthouse.Backend.Tests/`, with `StrykerOutput` removed
first. Settings: `perTestInIsolation`, concurrency 4. "Will be tested": **79** (of 22 950 created). The first run took
3 m 17 s.

Test filter: `ReportBulkRemoverTest`, `ReportBulkCommandsTest`, `API.ReportsControllerTest`, `ReportRepositoryTest`,
`Models.Reports`, and the one acceptance class `Slice05RemoveReportsInBulkScenarios` (9 tests). That class is in on
purpose. Before this pass, `ReportsController.RemoveEach` was reached only through it, because no unit test called the
action. The rest of `API.Integration`, `Integration.Containers`, `Dogfood` and the live-connector categories are out.

Changed lines (from `git blame`):
- `ReportBulkRemover.cs`: new, the whole file.
- `ReportsController.cs`: the constructor and the `bulk-removal` action (`:13`, `:58`–`:74`).
- `ReportRepository.cs`: `OwnerOf` (`:26`–`:35`).
- `ReportBulkCommands.cs`: four lines from the extraction refactor (`:8`, `:22`, `:52`, `:64`).
- `ReportBulkOutcomes.cs`: the removal status enum and records (`:28`–`:46`).
- `ReportRefusalCodes.cs`: `AlreadyGone` (`:42`–`:44`).

Not mutated:
- `IReportBulkRemover.cs`, `IReportBulkCommands.cs` and `IReportReader.cs`: interfaces.
- `ReportRemovalRequest.cs`: one auto-property.
- `UsageDataEventName.cs`: an enum member.
- `Program.cs`: one DI registration, exercised by the acceptance class.

| file | whole file, first run | changed lines, first run | changed lines, after kill round |
| --- | --- | --- | --- |
| `Services/Implementation/Reports/ReportBulkRemover.cs` (new) | 18 / 20 | 18 / 20 | **20 / 20** |
| `API/ReportsController.cs` | 5 / 9 | 0 / 2 | **2 / 2** |
| `Services/Implementation/Repositories/ReportRepository.cs` | 22 / 25 | 3 / 3 | 3 / 3 |
| `Services/Implementation/Reports/ReportBulkCommands.cs` | 24 / 24 | no mutant on a changed line | |
| `Models/Reports/ReportRefusalCodes.cs` | 1 / 1 | no mutant on a changed line | |
| `Models/Reports/ReportBulkOutcomes.cs` | no mutant | no mutant on a changed line | |
| **total** | 70 / 79 = 88.61 % | **21 / 25 = 84.00 %** | **25 / 25 = 100 %** |

The kill round was re-run with `stryker.6433.backend.kill.json`. It mutates only `ReportBulkRemover.cs` and
`ReportsController.cs`, with their unit classes and the acceptance class: 29 mutants, 2 m 30 s, 27 / 29 = 93.10 %.
The whole-file total, with the other files taken from the first run, is 74 / 79 = 93.67 %.

### Kill round (four tests)

- `ReportBulkRemoverTest`:
  - A slow removal run warns with how many reports were asked, removed and not removed. This kills `:91`, where
    `asked - done` became `asked + done` in the slow-warning line. The existing slow test pinned only the level, the
    message and the elapsed time.
  - A report already gone is logged as done and never as refused. This kills `:84`, where `&&` became `||` in the
    refused-by-code filter, which would count an already-gone report under `already-gone`.
- `ReportsControllerTest`:
  - A removal naming no report is refused, saying a removal needs one (`:63`).
  - A removal over the ceiling is refused, naming the ceiling (`:68`).

  Both tests also check that the remover is never reached. These are the wordings of the two validation problems,
  which no test read before.

### Survivors left on lines slice 05 did not change

- `ReportsController.cs:26` and `:27`: `Kind == Team` / `Portfolio` in the readable-owner split. This is slice 01's
  code, and it was already listed in 6429.
- `ReportRepository.cs:37`, `:68` and `:74`: older queries and `DiscardPendingChanges`. `:68` is the
  `SetValues(OriginalValues)` line that 6429 recorded as equivalent at `:58`; adding `OwnerOf` moved it down.

## Frontend (StrykerJS 10.0.0)

Config `stryker.6433.frontend.json` and `vitest.stryker.6433.mutation.ts`. As in 6429, the vitest config is committed
here and copied into `Lighthouse.Frontend/`, where it is gitignored. The Stryker config runs from its place here.

Settings:
- `perTest`.
- `--incremental`, with the file `reports/mutation/stryker-incremental-6433.json` (gitignored).
- Concurrency 2.
- `inPlace`.
- `disableTypeChecks: false`.
- `TZ=Europe/Zurich`.

`mutate` lists the changed line ranges:
- `RemoveSelectedDialog.tsx` and `useBulkReportRemoval.ts`: new, whole files.
- `ReportsPage.tsx`: `removeBlockedBecause` and `useSelectionRemoval` (`:145`–`:218`) and the four lines that wire
  them in.
- `BulkResult.tsx`: `reportsWord`, `NotConfirmed` and its use.
- `ReportList.tsx`: the filter clearing the picks, and `checkboxSelection={picking}`.
- `reportRefusalCopy.ts`: `noLongerAdminOf`, its use for `not-admin`, `COULD_NOT_DELETE` and `notRemovedLineFor`.
- `DataGridBase.tsx`: `useRowPicking` and the two lines that wire it in.
- `ReportService.ts`: `removeReportsInBulk`.
- `ReportsAcrossOwners.ts`: `ReportRemovalOutcomesSchema`.

So every score here is a changed-line score.

Skipped:
- The type-only and import-only lines: `RowPicking`, `IReportRemovalOutcome`, the prop interfaces and the imports.
- `useReportDeletion.tsx`, whose only changed line is an import.
- `UsageDataService.ts`, whose change is one entry, `ReportBulkRemovalStarted`, in an `as const` object. As in 6429,
  the usage-data test that expects the event by that name would kill its one string literal.

The vitest include is:
- `src/pages/Reports/**`
- `src/pages/Common/Reports/**`
- `src/components/Common/DataGrid/DataGridBase*.test.tsx`
- `ReportService.test.ts`
- `src/models/Reports/**`

`ActionButton.test.tsx` is left out on purpose, as in 6431 and 6432: one of its tests leaves a rejected promise
unhandled, which crashes Stryker's dry run.

| file | first run | after kill round |
| --- | --- | --- |
| `pages/Reports/RemoveSelectedDialog.tsx` (new) | 53 / 82 | 71 / 82 (86.59 %) |
| `pages/Reports/ReportsPage.tsx` | 51 / 58 | 51 / 58 (87.93 %) |
| `pages/Reports/useBulkReportRemoval.ts` (new) | 16 / 20 | 17 / 20 (85.00 %) |
| `components/Common/DataGrid/DataGridBase.tsx` | 15 / 18 | 15 / 18 (83.33 %) |
| `pages/Common/Reports/wizard/reportRefusalCopy.ts` | 11 / 11 | 11 / 11 |
| `pages/Reports/BulkResult.tsx` | 7 / 7 | 7 / 7 |
| `models/Reports/ReportsAcrossOwners.ts` | 5 / 5 | 5 / 5 |
| `services/Api/ReportService.ts` | 3 / 3 | 3 / 3 |
| `pages/Common/Reports/ReportList.tsx` | 2 / 4 | 2 / 3, plus 1 runtime error |
| **total** | 163 / 208 = **78.37 %** | **182 / 207 = 87.92 %** |

The first run missed the gate. Run times: the first run took 19 m 19 s, and the incremental kill-round re-run
13 m 59 s.

`ReportList.tsx:292`, which drops the `?.` on `picking`, is reported as a `RuntimeError` after the round. The new
filter test makes it throw a `TypeError`, so the mutant is detected. Stryker leaves runtime errors out of the score,
which is why the round's denominator is 207, not 208.

That re-run also carried a test that a list nobody picks rows on offers no checkboxes. It was dropped before the
commit, because it killed nothing that the first run had not already killed: `DataGridBase.tsx` is 15 / 18 in both
runs.

### Kill round

Tests added:

- `ReportsPage.removal.test.tsx`:
  - The result counts only the reports removed, and names, as "name + reason", only the one that was not removed.
    This kills the headline's `filter` and its predicate (`:29`, `:30`), the not-removed `filter` and its predicate
    (`:120`, `:121`), and `report?.name ?? ""` turned into `&&` (`:98`).
  - The confirmation warns in the plural when two Thens would be lost (`:43`, and `:45`, which had no coverage).
  - A picked report without a Then & Now carries no warning: no alert at all. This kills `:40` (dropping the `?.` on
    `thenAndNow`) and `:59` (rendering the warning alert whatever it holds).
  - Escape at the confirmation closes it, removes nothing and keeps what was picked. This kills the `close` handler's
    `if (isPending)` turned into `true`, its body emptied, and the `onCancel` branch, which had no coverage
    (`:155`–`:158`).
  - Escape on the result closes it and clears what was picked. This kills the three `isAnswered` mutants (`:154`),
    which would fall back to Cancel and keep the picks.
- `ReportsPage.usageData.test.tsx`: a bulk removal tells one deleted report for each report it removed, and none for
  one already gone (`useBulkReportRemoval.ts:22`, `code === undefined` turned into `!==`). The existing test had one
  report of each kind, so the mutant also gave one event.
- `ReportList.test.tsx`: a list nobody picks rows on still narrows to the reports the filter matches
  (`ReportList.tsx:292`, the `?.` on `picking`).

### Accepted survivors (25)

- Layout that jsdom does not compute (8): the `sx` objects and values in `RemoveSelectedDialog.tsx` `:60`, `:84`
  (two), `:97` and `:98`, and in `ReportsPage.tsx:248` (three).
- Equivalent (14):
  - `DataGridBase.tsx:160` (three): the `selectedIds === undefined` and `onSelectionChange === undefined` halves, and
    `||` turned into `&&`. `RowPicking` requires both fields, so the two halves are either both undefined (no
    picking) or both defined. Each half alone, and both together, decide the same way.
  - `ReportList.tsx:292`, `ReportsPage.tsx:166` and `:209` (three): the picks cleared, or started, as
    `["Stryker was here"]` instead of `[]`. The page counts as picked only the ids it finds among the listed reports,
    so an id no row has reads as nothing picked. "0 selected" and a disabled Remove look the same either way.
  - `ReportsPage.tsx:172`, `reports?.find` turned into `reports.find`. Nobody can pick a row before the list has
    loaded, so `reports` is defined whenever `selectedIds` is not empty.
  - `RemoveSelectedDialog.tsx:98` and `:101` (four): `report?.name` and `report?.ownerName`, and their `""` fallbacks
    (two with no coverage). The result looks a report up among the ones sent, and the server answers only the ids it
    was asked about, so the report is always found. The guard is defensive.
  - `useBulkReportRemoval.ts:22`, `status === "removed"` turned into `true`. That would also count a not-removed
    outcome that carries no code, but the server never sends one: `ReportRemovalOutcome.NotRemoved` takes a non-null
    code.
  - `useBulkReportRemoval.ts:28` (two): invalidating every query instead of `["reports"]` still re-reads every report
    list. This is the same reasoning as `useBulkReportCreation.ts:36` in 6429.
- Left open (3):
  - `RemoveSelectedDialog.tsx:23`, `TITLE_ID` blanked. The dialog would lose its accessible name ("Remove reports"),
    and no test looks the dialog up by name. A `getByRole("dialog", { name: "Remove reports" })` would kill it.
  - `RemoveSelectedDialog.tsx:86`, the under-way line "Removing 3 reports…" blanked. The under-way test checks only
    the progress bar.
  - `ReportsPage.tsx:172`, `?? []` turned into `?? ["Stryker was here"]`. This would count a pick whose report has
    left the list while it is still picked. Picks are cleared whenever a removal ends, so reaching it needs a list
    re-read from elsewhere while the reader holds picks.

No survivor revealed a bug.

## Commands

From `Lighthouse.Backend/Lighthouse.Backend.Tests/`:

```
rm -rf StrykerOutput
dotnet stryker --config-file ../../docs/feature/epic-6350-bulk-reports/mutation/stryker.6433.backend.json
dotnet stryker --config-file ../../docs/feature/epic-6350-bulk-reports/mutation/stryker.6433.backend.kill.json
```

From `Lighthouse.Frontend/`, after
`cp ../docs/feature/epic-6350-bulk-reports/mutation/vitest.stryker.6433.mutation.ts .`:

```
TZ=Europe/Zurich pnpm exec stryker run ../docs/feature/epic-6350-bulk-reports/mutation/stryker.6433.frontend.json --incremental
```

The two stacks were run one after the other, never at the same time.
