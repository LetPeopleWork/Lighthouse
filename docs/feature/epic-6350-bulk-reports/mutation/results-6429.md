# Mutation testing: 6429 (Epic 6350 slice 02a: create reports for several Teams in one go)

Run 2026-10-29 against local `main` @ `7169c5ca2` plus the kill-round tests below. Gate: 80 % kill rate per stack on the
lines slice 02a changed (`git diff 6530cab2d..HEAD`). Several backend files existed before the slice, so whole-file
scores are given for context and survivors are triaged on the changed lines.

## Backend (Stryker.NET 5.0.0)

Config `stryker.6429.backend.json`, from `Lighthouse.Backend/Lighthouse.Backend.Tests/`, `StrykerOutput` removed first,
`perTestInIsolation`, concurrency 4. "Will be tested": **123** (of 22 893 created), plus 10 without coverage. First run
5 m 58 s.

Test filter: `ReportBulkCommandsTest`, `API.ReportsControllerTest`, `API.TeamReportsControllerTest`,
`API.ReportOwnerWriteAccessTest`, `ReportQueriesTest`, `ReportLoggingTest`, `ReportCommandsTest`,
`TeamReportMetricSeriesTest`, `PortfolioReportMetricSeriesTest`, `ReportRepositoryTest`, `Models.Reports`, and the one
acceptance class `Slice02aCreateForSeveralTeamsScenarios` (18 tests). That class is included on purpose:
`ReportsController.CreateForEach` and `GetCreationOptions` are reached end to end only through it. The rest of
`API.Integration`, `Integration.Containers`, `Dogfood` and the live-connector categories are out.

Not mutated, because nothing in them can be mutated or they are wiring: `IReportBulkCommands.cs`,
`IReportOwnerWriteAccess.cs`, `IReportQueries.cs`, `IReportWriter.cs` (interfaces; the `MostOwnersPerRun` constant is a
`const`), `UsageDataEventName.cs` (enum members), `Program.cs` (one DI registration, exercised by the acceptance class),
`Lighthouse.Backend.csproj` (a package bump). `NewReportsForOwnersRequest.cs` and the changed records in
`ReportReadModels.cs` were in `mutate` but hold only auto-properties and positional records, so no mutant lands on their
changed lines. `TeamReportMetricSeries.cs` and `PortfolioReportMetricSeries.cs` changed one line each (the throw of the
new `ReportOwnerGoneException`), on which Stryker places no mutant.

| file | whole file, first run | changed lines, first run | changed lines, after kill round |
| --- | --- | --- | --- |
| `Services/Implementation/Reports/ReportBulkCommands.cs` | 22 / 24, 91.67 % | 22 / 24 | **24 / 24** |
| `API/ReportsController.cs` | 3 / 7, 42.86 % | 2 / 5 | **5 / 5** |
| `API/ReportsControllerBase.cs` | 15 / 24, 62.50 % | 3 / 3 | 3 / 3 |
| `API/ReportOwnerWriteAccess.cs` | 0 / 1, 0 % | 0 / 1 | 0 / 1 (accepted) |
| `API/DTO/Reports/NewReportRequest.cs` | 10 / 11, 90.91 % | 10 / 11 | 10 / 11 (accepted) |
| `Services/Implementation/Reports/ReportQueries.cs` | 24 / 29, 82.76 % | 6 / 6 | 6 / 6 |
| `Services/Implementation/Repositories/ReportRepository.cs` | 17 / 22, 77.27 % | 4 / 5 | 4 / 5 (accepted) |
| `Models/Reports/ReportRefusalCodes.cs` | 1 / 1, 100 % | 1 / 1 | 1 / 1 |
| `Models/Reports/ReportOwnerGoneException.cs` | 0 / 1, 0 % | 0 / 1 | **1 / 1** |
| `Models/Reports/ReportReadModels.cs` | 0 / 1, 0 % | no mutant on a changed line | |
| `Services/Implementation/Reports/TeamReportMetricSeries.cs` | 3 / 6, 50 % | no mutant on a changed line | |
| `Services/Implementation/Reports/PortfolioReportMetricSeries.cs` | 3 / 6, 50 % | no mutant on a changed line | |
| **total** | 98 / 133 killed, 73.68 % | **48 / 57 = 84.21 %** | **54 / 57 = 94.74 %** |

Kill round re-run with `stryker.6429.backend.kill.json`: `ReportBulkCommands.cs`, `ReportsController.cs`,
`ReportOwnerGoneException.cs` and `ReportRepository.cs` only, with their unit classes and the acceptance class,
54 mutants, 4 m 48 s, 49 / 54 = 90.74 %. Whole-file after the round: `ReportBulkCommands.cs` 24 / 24,
`ReportsController.cs` 6 / 7, `ReportOwnerGoneException.cs` 1 / 1, `ReportRepository.cs` 18 / 22 (the re-run also carried a
probe test for `:58`, since dropped, which happened to kill the older `:64` equality mutant; without it the file stays
at the first run's 17 / 22). Whole-file total with the other files from the first run: 104 / 133 = 78.20 %.

### Kill round (five tests)

- `ReportBulkCommandsTest`: a slow run (above twenty seconds) warns with how many owners were asked, done and not done
  (`:92`, `asked - done` to `asked + done` in the slow-warning line); a run whose Now length was not given logs Now as
  long as Then (`:82`, dropping `?? thenLengthDays`).
- `ReportsControllerTest`: a run whose `Owners` is missing altogether is refused saying a run needs at least one owner,
  without reaching the bulk commands (`:65`, `request.Owners ?? []` to `request.Owners`, which threw; and `:46`, the
  refusal's wording); a run over the ceiling is refused naming the ceiling (`:51`, the wording).
- `TeamReportMetricSeriesTest`: a gone Team's exception message is exactly `Team <id> does not exist`, so it names the
  owner's kind and id and never its name (`ReportOwnerGoneException.cs:5`).

### Accepted survivors on changed lines

- `NewReportRequest.cs:30`, `TemplateKey ?? string.Empty` with the empty string mutated (no coverage). A request without
  a template key becomes a report request for template `""`; the mutant makes it `"Stryker was here!"`. Both are keys no
  template offers, so both are refused `template-not-supported` and logged as template `unknown`. Equivalent.
- `ReportOwnerWriteAccess.cs:19`, the message of the `ArgumentOutOfRangeException` for an owner kind that is neither Team
  nor Portfolio. The controller refuses such a kind with a 400 before any access check, so the throw is a defensive
  guard unreachable through the API, and its message is read by no one.
- `ReportRepository.cs:58`, removing `entry.CurrentValues.SetValues(entry.OriginalValues)` in `DiscardPendingChanges`.
  Equivalent under EF Core: setting a Modified entry's state to Unchanged on the next line already restores its
  original values. Checked by hand: with the line removed, a report renamed but not saved still reads back with its
  stored name from a tracked query, and every `ReportRepositoryTest` passes. The line is redundant, not wrong; it is left
  as it is because it states the intent.

### Survivors left on lines slice 02a did not change

`ReportsController.cs:26` (`Kind == Team` to `!=` in the readable-owner split: the method moved in this slice but the
line is slice 01's; every acceptance owner here is a Team, and `Slice01EveryReportInOnePlaceScenarios`, out of this
filter, covers the mixed case); `ReportsControllerBase.cs:38`, `:41` (two), `:48` (three), `:56`, `:61`, `:78` (the
single-owner create, delete and refusal paths, pinned by suites outside this filter); `ReportQueries.cs:44` (two),
`:117`, `:127`, `:165` (the older `List` and `Get` paths and a log string); `ReportRepository.cs:38`, `:40`, `:64` (two)
(older queries read by tests outside this filter); `ReportReadModels.cs:70` (arithmetic on an older record);
`TeamReportMetricSeries.cs:18` (two), `:44` and `PortfolioReportMetricSeries.cs:18` (two), `:45` (the older cycle-time
filter and a log string).

## Frontend (StrykerJS 10.0.0)

Config `stryker.6429.frontend.json` and `vitest.stryker.6429.mutation.ts`. The vitest config is copied into
`Lighthouse.Frontend/`, where it is gitignored. The Stryker config runs from its place here
(`pnpm exec stryker run ../docs/feature/epic-6350-bulk-reports/mutation/stryker.6429.frontend.json --incremental`),
because a copy at the package root would not be ignored. Settings: `perTest`, `--incremental` (file
`reports/mutation/stryker-incremental-6429.json`, gitignored), concurrency 2, `inPlace`, `disableTypeChecks: false`,
`TZ=Europe/Zurich`. `mutate` is whole files, so whole-file and changed-line numbers come from one run. Changed lines are
taken from `git diff 6530cab2d..HEAD`. The vitest include is `src/pages/Reports/**`, `src/pages/Common/Reports/**`,
`ReportService.test.ts` and `src/models/Reports/**`. The vitest config sets `testTimeout: 30000`: the first attempt
stopped in the dry run, where coverage instrumentation, on a machine also running the backend mutation run, pushed one
wizard test past the 5 s default.

`UsageDataService.ts` is not mutated. Its change is one entry, `ReportBulkCreationStarted`, in an `as const` object. Its
string literal is the one mutable token, and the usage-data test that expects the event by that name would kill it.

| run | whole file | changed lines |
| --- | --- | --- |
| first run (829 mutants, 136 min, on a machine shared with the backend run) | 729 / 829 = 87.94 % | 325 / 403 = 80.65 % |
| after kill round (incremental: 520 re-tested, 309 reused, 29 min) | 762 / 829 = **91.92 %** | 358 / 403 = **88.83 %** |

| file | changed lines, first | changed lines, after | whole file, after |
| --- | --- | --- | --- |
| `BulkReportWizard.tsx` (new) | 93 / 126 | 110 / 126 | 87.30 % |
| `BulkResult.tsx` (new) | 40 / 54 | 46 / 54 | 85.19 % |
| `OwnerStep.tsx` (new) | 17 / 22 | 17 / 22 | 77.27 % |
| `ReportsPage.tsx` | 61 / 72 | 64 / 72 | 86.49 % |
| `useBulkReportCreation.ts` (new) | 13 / 16 | 14 / 16 | 87.50 % |
| `useReportCreationOptions.ts` (new) | 9 / 10 | 9 / 10 | 90.00 % |
| `ReportsAcrossOwners.ts` | 25 / 28 | 28 / 28 | 100 % |
| `Report.ts` | 1 / 1 | 1 / 1 | 100 % |
| `reportRefusalCopy.ts` | 23 / 26 | 25 / 26 | 97.27 % |
| `reportTypes.ts` | 10 / 10 | 10 / 10 | 100 % |
| `thenAndNowDraft.ts` | 20 / 25 | 21 / 25 | 94.93 % |
| `ReportService.ts` | 13 / 13 | 13 / 13 | 93.33 % |
| `useMayEditReports.ts`, `NowStep.tsx`, `ThenStep.tsx`, `WindowLengthControl.tsx` | no mutant on a changed line | | 100 %, 100 %, 92.00 %, 76.19 % (older code) |

### Kill round (new tests)

- `BulkReportWizard.test.tsx`, "Moving through the wizard":
  - The wizard is titled Create reports and names its five steps.
  - Back leads from Then to the Type step, and the Type step has no Back.
  - Next waits while Then's length is not a number of days.
  - The owner step offers Create, not Next, and Create stays disabled while nobody is picked.
  - A Team picked a second time is no longer picked.
  - Only the Teams the reader is Admin of are listed.
  - Escape leaves the result open while the run is under way and closes it once the run has answered.
  - One report created is counted in the singular, and its line names its Team.
  - An owner the answer leaves unnamed is called "this Team".

  "Who may press Create reports":
  - An Admin of one Portfolio alone may press it.
  - For an Admin on Premium it carries no tooltip.
- `ReportsPage.usageData.test.tsx`: two reports created and one not are reported as two `ReportCreated` events. The
  existing test had one of each, which `===` mutated to `!==` also passes.
- `ReportService.test.ts`: an owner kind the server leaves out of `metricKeysByOwnerKind` is offered no metric, and a
  metric that is not named by a string is refused.
- `reportRefusalCopy.test.ts`: a bulk line with no reason, or a reason this version does not know, asks to try again.
- `thenAndNowDraft.test.ts`: a Now length that was entered is sent to every owner as its days.

The existing test "while the run is under way the result says so and cannot be closed" presses Escape while focus is on
the page body. The Create button has just unmounted, so the dialog never receives the key, and that check cannot fail;
it is why all four `:208` mutants survived the first run. The new Escape test sends the key to the dialog. The old
assertion is left as it is.

### Accepted survivors on changed lines (45)

- Module-level constants that StrykerJS never activates (5): `BulkReportWizard.tsx` `:30` (`TITLE_ID`), `:32`
  (`chosen`), and three on `:37` (the step labels). Stryker reports them as uncovered survivors. Checked by hand: with
  `chosen = ""` the whole test file fails to load. With the title id or any step label blanked, the new step-names test
  fails.
- Layout and style that jsdom does not compute (11): `sx` objects and values in `BulkResult.tsx` `:84` `:85` and two on
  `:118`, `OwnerStep.tsx` `:59`–`:62` (four), and `ReportsPage.tsx` `:137` (two). Also the `text.disabled` colour while
  Retry asks again (`BulkReportWizard.tsx:270`).
- Equivalent (25):
  - `BulkReportWizard.tsx:64` (six): any step index outside the type's own steps reads `undefined` from the array
    anyway.
  - `:60` and `:173` (`useMemo` deps to `[]`): one wizard instance is keyed to one opening, and roles are known before
    the button is enabled.
  - `:157`: a status of `""` still falls through to the result list.
  - `BulkResult.tsx:86`: dropping `.trim()` leaves a trailing space that nobody sees.
  - `BulkResult.tsx:88` (three): the server sends a report id exactly when it says created.
  - `OwnerStep.tsx:48`: the picked set only ever holds listed Teams.
  - `ReportsPage.tsx:52` and `:63` (`retrying` start value, and its reset on close): every opening resets it.
  - `ReportsPage.tsx:59`: opening keys only need to differ from one another.
  - `ReportsPage.tsx:74`: after a close the query has no key and so no data.
  - `useBulkReportCreation.ts:36` (two): invalidating every query instead of `["reports"]` still re-reads every list.
  - `reportRefusalCopy.ts:132`: `isRefusalCode(undefined)` is false, so the second half of the condition catches a
    missing code.
  - `thenAndNowDraft.ts:74` (four): `subDays(today, null)` is today itself, and an unparsable today becomes an Invalid
    Date that the `isValid` fallback turns back into `templates.today`. The guard changes nothing.
- Left open (4):
  - `BulkReportWizard.tsx:57`: the `ownerKind === "team"` half of the owner filter. Removing it would list a Portfolio
    under Teams. Pinning that now would mean asserting the opposite of the pending Portfolio scenarios in
    `BulkReportWizard.portfolios.test.tsx`, which replace this filter.
  - `ReportsPage.tsx:44` and `:45` (optional chaining on `adminTeamIds` and `adminPortfolioIds`): both lists are
    optional on the wire, but `BulkReportsTestKit` always sends both. A test needs a kit option that leaves one out.
  - `useReportCreationOptions.ts:16` (`refetchOnWindowFocus: false`): the network coming back is pinned, a window
    regaining focus is not.

## Frontend gates after the kill round

`pnpm test`: 564 files passed, 6 skipped; 8651 tests passed, 54 skipped. The skipped ones are the pending scenarios of
later slices. `pnpm build`: exit 0, clean; Biome reformatted the five test files. No production file is changed, and
`git status` shows no mutant left in `src/`.
