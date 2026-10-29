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
