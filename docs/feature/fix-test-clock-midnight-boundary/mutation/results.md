# Mutation testing — fix-test-clock-midnight-boundary

Run 2026-10-23 against `fix-test-clock-midnight-boundary` @ `47838c3e2` (first run, production code
frozen there) and @ `5b5c5c312` (kill-round re-run: same production code plus the tests written off
the first run). Gate is 80 % kill rate on the backend.

| stack | run | score | tested | killed | survived | no coverage | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0) | first run, 28 files | **64.22 %** | 668 | 464 | 196 | 67 | 8 | 23 m 23 s |
| Backend (Stryker.NET 5.0.0) | kill round, 17 files | 82.04 % | 655 | 538 | 107 | 13 | 10 | 22 m 19 s |
| Backend | combined (first run for the 11 files not re-run, kill round for the 17 that were) | **83.67 %** | 722 | 604 | 107 | 13 | 11 | — |

Combined arithmetic: 615 detected (killed + timeout) of 735 scored mutants (tested + no coverage).
The 11 files that were not re-run had no survivors on the first run and no production change since.

**Frontend: N/A** — the fix changed no file under `Lighthouse.Frontend/`.

Configs: `stryker.clock.backend.json` (first run), `stryker.clock.backend.killround.json` (re-run).
Both run from `Lighthouse.Backend/Lighthouse.Backend.Tests/`. Mutation is whole-file; the test-case
filter takes each mutated file's unit tests and excludes `API.Integration`, `Dogfood`, `*IntegrationTest`
and the live-connector categories (752 tests, 5 s for the first run). No acceptance suite is included.

## Backend, per file

| file | first run (K / T / S / NC) | first score | kill round (K / T / S / NC) | kill-round score |
| --- | --- | --- | --- | --- |
| API/DatabaseManagementController.cs | 23 / 0 / 3 / 0 | 88.5 % | 26 / 0 / 0 / 0 | 100.0 % |
| API/DeliveryNotesController.cs | 29 / 0 / 16 / 15 | 48.3 % | 50 / 0 / 7 / 3 | 83.3 % |
| API/LogsController.cs | 11 / 0 / 1 / 0 | 91.7 % | 11 / 0 / 1 / 0 | 91.7 % |
| Cache/Cache.cs | 9 / 0 / 0 / 0 | 100.0 % | not re-run | — |
| Cache/CacheItem.cs | 1 / 0 / 0 / 0 | 100.0 % | not re-run | — |
| Models/Auth/ApiKey.cs | 0 / 0 / 5 / 0 | 0.0 % | 0 / 0 / 5 / 0 | 0.0 % |
| Models/Auth/UserProfile.cs | 0 / 0 / 2 / 0 | 0.0 % | 0 / 0 / 2 / 0 | 0.0 % |
| Models/Forecast/ForecastBase.cs | 30 / 0 / 4 / 0 | 88.2 % | 30 / 0 / 4 / 0 | 88.2 % |
| Models/Forecast/HowManyForecast.cs | 2 / 0 / 0 / 0 | 100.0 % | not re-run | — |
| Models/Forecast/StartForecast.cs | 2 / 0 / 0 / 0 | 100.0 % | not re-run | — |
| Models/Forecast/WhenForecast.cs | 8 / 0 / 0 / 0 | 100.0 % | not re-run | — |
| Models/WorkTrackingSystemOptionsOwner.cs | 17 / 0 / 13 / 10 | 42.5 % | 40 / 0 / 0 / 0 | 100.0 % |
| Services/Implementation/Auth/ApiKeyService.cs | 56 / 1 / 17 / 2 | 75.0 % | 71 / 3 / 2 / 0 | 97.4 % |
| Services/Implementation/Auth/CurrentUserProfileService.cs | 31 / 1 / 0 / 0 | 100.0 % | not re-run | — |
| Services/Implementation/Auth/OidcGroupSnapshotWriter.cs | 6 / 0 / 7 / 0 | 46.2 % | 10 / 1 / 2 / 0 | 84.6 % |
| Services/Implementation/BackgroundServices/Update/ForecastUpdater.cs | 28 / 4 / 3 / 3 | 84.2 % | 28 / 4 / 3 / 3 | 84.2 % |
| Services/Implementation/BackgroundServices/Update/PortfolioUpdater.cs | 29 / 0 / 10 / 3 | 69.0 % | 35 / 0 / 5 / 2 | 83.3 % |
| Services/Implementation/BackgroundServices/Update/TeamUpdater.cs | 21 / 0 / 5 / 4 | 70.0 % | 27 / 0 / 3 / 0 | 90.0 % |
| Services/Implementation/BackgroundServices/Update/UpdateServiceBase.cs | 49 / 0 / 21 / 5 | 65.3 % | 54 / 0 / 19 / 2 | 72.0 % |
| Services/Implementation/DatabaseManagement/DatabaseManagementService.cs | 32 / 0 / 63 / 19 | 28.1 % | 80 / 0 / 31 / 3 | 70.2 % |
| Services/Implementation/DomainEvents/FeatureBlockedTransitionCaptureHandler.cs | 7 / 0 / 0 / 0 | 100.0 % | not re-run | — |
| Services/Implementation/DomainEvents/FeatureBlockedTransitionCloseHandler.cs | 6 / 0 / 0 / 0 | 100.0 % | not re-run | — |
| Services/Implementation/DomainEvents/WorkItemBlockedTransitionCaptureHandler.cs | 7 / 0 / 0 / 3 | 70.0 % | 10 / 0 / 0 / 0 | 100.0 % |
| Services/Implementation/DomainEvents/WorkItemBlockedTransitionCloseHandler.cs | 6 / 0 / 0 / 3 | 66.7 % | 9 / 0 / 0 / 0 | 100.0 % |
| Services/Implementation/Forecast/ForecastService.cs | 54 / 2 / 26 / 0 | 68.3 % | 57 / 2 / 23 / 0 | 72.0 % |

`ForecastSimulation.cs`, `ApiKeyPermission.cs` and `UserPermission.cs` produced no mutants (a
positional record and two property bags).

Every mutant on a line this fix changed is killed: the clock reads in `Cache`, `CacheItem`, the
forecast models, `ForecastService.CreationInstant`, `UpdateServiceBase.CurrentInstant` and the three
updaters' `ExecutedAt` / refresh-after arithmetic, the blocked-transition handlers, the auth stamps,
the log and backup file names and the backup manifest's `createdAt`.

The first run's 2,111 `CompileError` mutants are project-wide (19,125 more were removed by the mutate
filter); none of them count toward the score.

## Closed by this pass

Tests in `5b5c5c312`:

- **DatabaseManagementService** — a full backup → restore round trip through the provider (files,
  a nested file, an empty directory entry), the manifest's exact keys and values including the clock
  instant and leaving `serverVersion` out when the provider has none, working directories and the
  plain-text intermediate zip cleaned up, `Executing` while the provider works (backup, restore,
  clear), failure reasons for a provider that throws (restore, clear), a wrong password, an encrypted
  non-zip, a file too short for the header, a zip entry escaping the target directory, an unknown or
  vanished artifact, seeding after a clear. 48 kills.
- **DeliveryNotesController** — newest-written first even when stored in the other order, the
  refusal text for an empty add and an emptied correction, an archived Delivery refusing add, correct
  and withdraw. `DeliveryNoteAuthorshipTest` (an existing unit fixture missing from the first run's
  filter) was added to the kill-round filter and covers delete and the authorship predicate. 21 kills.
- **WorkTrackingSystemOptionsOwner** — default state lists and query, no connection default followed
  by the base owner, an owned estimation bundle, `RefreshUpdateTime` / `ResetUpdateTime`, eight
  `IsCycleTimeDefinitionValid` cases (empty mapping, partly-known mapping, unknown state, each side).
  23 kills.
- **ApiKeyService** — empty scope grants and saves nothing, a scope saves its grants, delete removes
  and saves, a key that vanishes between check and read, the owner-resolution order (linked id over a
  stale subject, deleted link falling back to subject, display name, case-insensitive email, blank
  creator stays unlinked), the resolved owner persisted on the key. 17 kills.
- **TeamUpdater / PortfolioUpdater / UpdateServiceBase** — the refresh-after boundary is inclusive,
  the licence limits (three teams still refresh; a fourth team or a second portfolio refreshes nothing
  and logs one error), a vanished entity is a no-op, connection health hears success, failure and
  nothing for a cancel. 17 kills.
- **OidcGroupSnapshotWriter** (new `OidcGroupSnapshotWriterTest`, SQLite in-memory) — creates the
  profile with `sub`, the groups and the clock instant; updates only the matching profile in the
  store; refuses a blank subject and a null group list. 5 kills.
- **WorkItem blocked-transition handlers** — only that item's open transition counts (capture) or is
  closed (close). 6 kills.
- **ForecastService** — `HowMany` runs exactly the configured trials; a Feature shared with a Team
  outside the Portfolio is still forecast, and the result is saved. 3 kills.
- **DatabaseManagementController** — the three refusal texts are asserted. 3 kills.

## Accepted survivors (kill round)

- **Log text and log calls (61)** — `DatabaseManagementService` 24, `ForecastService` 18,
  `UpdateServiceBase` 14 (including the `ExecuteAsync` start/stop/done lines), `PortfolioUpdater` 5
  (the stale-Delivery note, its `!` guard which only chooses whether to log, the licence error text,
  the cleanup warning), `TeamUpdater` 1, `OidcGroupSnapshotWriter` 2, `LogsController` 1. Diagnostic
  wording is not behaviour; the licence refusals are pinned by "one error is logged", not by its words.
- **`HttpContext?.RequestAborted ?? default` (7, DeliveryNotesController)** — every unit test runs
  without an `HttpContext`, so both sides are `default`; only a cancelled live request differs.
- **Default `string.Empty` initialisers (7, `ApiKey`, `UserProfile`)** — every writer sets these
  properties; the default is never read.
- **Equivalent mutants** — `ApiKeyService:154,160` (`IsValid` already defaults to false);
  `DatabaseManagementService:329` (`Aes.Create()` already generates an IV), `:378` (`overwrite`
  into a fresh directory), `:149` (extraction creates each entry's directory), `:312/314` (the
  decrypted file sits in the working directory that is deleted anyway), `:224` (`ToString("")` still
  yields a unique 12-character id); the three `stopwatch.Stop()` calls (the elapsed time is read once,
  immediately after); `ForecastBase:38` (one cached entry rebuilt from the same list), `:105/112`
  (the lists are empty when the only caller fills them), `:13` (the comparer constructor is used only
  by EF materialisation); `ForecastService:98` (the argument is always a list already), `:147` (an
  empty plan yields no starts either way).
- **Not cheap to observe (kept for a later pass, none on a changed line)** —
  `DatabaseManagementService:212` (`ApplyMigrations` against the test's real SQLite file);
  `ForecastService:238/259` (day order inside a dictionary every reader re-sorts), `:335` (chip
  status copy), `:280/282` (log); `ForecastUpdater:42,102,119` (hold/queue hand-over, covered
  end-to-end by the acceptance suite), `:146,151` (unreachable `NotSupportedException` texts);
  `UpdateServiceBase:48,118,122,134,140` (refresh-round summary hand-off), `:269/318` (start delay,
  would need a wall-clock wait); `TeamUpdater:81` (`throw` after an unreadable-secret reason; the
  queue double's outcome is the same exception either way); `DeliveryNotesController:85,121,129`
  (scope-refusal and not-found blocks on update/delete; `NoCoverage` under this filter).

## Not mutated

Each file below changed by 1–8 lines in a 1,000–3,600-line file. Whole-file mutation would bury the
change under hundreds of mutants on untouched code (Stryker.NET ignores line spans), so the changed
lines are pinned by clock-driven tests instead:

| file | size / changed | covering tests |
| --- | --- | --- |
| TeamMetricsService.cs | 996 / 1 | `TeamMetricsServiceTests.UpdateTeamMetrics_RefreshesUpdateTimeForTeam` (frozen clock instant) |
| RbacAdministrationService.cs | 1,433 / 6 | `RbacAdministrationServiceTest.EveryKindOfGrant_IsStampedWithTheClockInstant` |
| WorkItemService.cs | 1,446 / 6 | `WorkItemServiceTest.UpdateFeaturesForPortfolio_StampsTheRefreshWithTheClockInstant`, `UpdateFeaturesForProject_UseCalculatedDefault_SamplesTheHistoryEndingAtTheClockInstant`, `UpdateWorkItemsForTeam_TrackerKeepsNoHistory_RecordsTheStateChangeAtTheClocksInstant` |
| AzureDevOpsWorkTrackingConnector.cs | 1,794 / 8 | `AzureDevOpsWorkTrackingConnectorTest.HistoryCutoff_IsTheClockInstantMinusHistoryDays` |
| JiraWorkTrackingConnector.cs | 3,610 / 4 | `JiraWorkTrackingConnectorTest.HistoryCutoff_IsTheClockInstantMinusHistoryDays` |
| Program.cs | 1,831 / 1 | DI registration of `ForecastSimulation`; `ServiceProviderValidationTest` and every host-starting test resolve `ForecastService` through it |

## Environment notes

The full filtered backend suite after the new tests: 11,101 passed, 1 failed —
`TeamDeleteSerialisationTests.DeleteTeam_NoOtherActivity_RemovesTeamFeatureWorkAndRefreshLogs`
("Timed out waiting for active updates to drain") under the parallel load; it passes alone and its
area is untouched by this change.
