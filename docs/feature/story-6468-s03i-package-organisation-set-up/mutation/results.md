# Mutation testing — 6468 (S03i Package Organisation set-up)

Run 2026-11-25 in a detached worktree at `71bcab9fd` (set-up production code frozen at HEAD), Stryker.NET 4.14.1 (local
tool manifest) on Linux x64, in Stryker's own sandbox (never in place). Bar (D-15): at least 80 % of the moved code,
**and** no survivor or no-coverage mutant added by the move. The stricter of the two holds.

| stack | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend, `Modules/OrganisationSetup/**` | **80.37 %** | 326 | 262 | 22 | 42 | 6 m 50 s |
| Frontend | N/A — no frontend file changed (pure namespace move) | | | | | |

"Tested" counts killed + survived + no coverage over the 27 files under `Domain` and `Application`. Stryker also marked
78 mutants as compile errors (Safe Mode) and ignored 83 (block-mutant and default-ignored); the 9 `I*.cs` port files with
no body (`IAppSettingService`, `IBlackoutPeriodService`, `IFeatureOrdering`, `IFeatureOrderingPolicyProvider`,
`IFeatureRankSeeder`, `IFeatureRankingService`, `IRecurringBlackoutRuleService`, `ITerminologyService`,
`IOptionalFeatureApplier`) yield no mutant. Of the 24,171 mutants created for the whole backend, 284 were queued for
testing after coverage capture and 42 more were no-coverage (284 + 42 = 326), read from `mutation-report.json`; everything
else sits outside `Modules/OrganisationSetup` and was skipped by the `mutate` globs.

The score clears the bar by 0.37 points. It is a floor for the reason below: the three files that score worst are the
ones whose behaviour only the excluded host scenarios observe.

## Per file (killed / survived / no coverage)

| File | K / S / NC | Score of tested |
| --- | --- | --- |
| `Application/AppSettingService.cs` | 48 / 5 / 1 | 88.9 % |
| `Application/BlackoutPeriodService.cs` | 21 / 3 / 0 | 87.5 % |
| `Application/Connections/ChangedConnectionDefault.cs` | 6 / 0 / 0 | 100 % |
| `Application/Connections/ConnectionSaveService.cs` | 87 / 1 / 2 | 96.7 % |
| `Application/Connections/IConnectionSaveService.cs` | 16 / 0 / 0 | 100 % |
| `Application/Connections/IWorkTrackingSystemConnectionUsage.cs` | 1 / 0 / 0 | 100 % |
| `Application/Connections/WorkTrackingSystemConnectionUsage.cs` | 6 / 4 / 0 | 60.0 % |
| `Application/FeatureOrdering.cs` | 2 / 1 / 7 | 20.0 % |
| `Application/FeatureOrderingPolicyProvider.cs` | 0 / 0 / 5 | 0 % |
| `Application/FeatureRankSeeder.cs` | 4 / 2 / 2 | 50.0 % |
| `Application/FeatureRankingService.cs` | 0 / 0 / 24 | 0 % |
| `Application/OptionalFeatures/DefaultOptionalFeatureApplier.cs` | 2 / 0 / 1 | 66.7 % |
| `Application/OptionalFeatures/FeatureOrderingApplier.cs` | 6 / 1 / 0 | 85.7 % |
| `Application/OptionalFeatures/OptionalFeatureApplierRegistry.cs` | 2 / 0 / 0 | 100 % |
| `Application/RecurringBlackoutRuleService.cs` | 30 / 0 / 0 | 100 % |
| `Application/TerminologyService.cs` | 4 / 2 / 0 | 66.7 % |
| `Domain/RecurringBlackoutRuleExtensions.cs` | 15 / 3 / 0 | 83.3 % |
| `Domain/WeeklyRecurrence.cs` | 12 / 0 / 0 | 100 % |

Config: `stryker.6468.backend.json` (the two `mutate` globs `Modules/OrganisationSetup/{Domain,Application}`;
`Application/Connections` and `Application/OptionalFeatures` sit inside `Application`). Run from
`Lighthouse.Backend.Tests`, coverage analysis `perTestInIsolation`, concurrency 4, 226 tests in the filter.

Test filter (included): the set-up unit tests (`Lighthouse.Backend.Tests.Modules.OrganisationSetup`: the nine moved
classes), the mock-based controller tests that call set-up's ports (`AppSettingsControllerTest`,
`BlackoutPeriodsControllerTest`, `RecurringBlackoutRulesControllerTest`, `TerminologyControllerTest`,
`OptionalFeaturesControllerTest`), `RecurringBlackoutRulePersistenceTest`, `SurveyNudgeSettingsTest`,
`RecurringBlackoutRuleDtoTest` and the two Forecasting trigger-handler tests that use feature ordering and rank
(`FeatureOrderingPolicyChangedForecastTriggerHandlerTest`, `FeatureRankChangedForecastTriggerHandlerTest`).

Left out, and why:

- `API.Integration.*` (all of it): boots `WebApplicationFactory` per test. This holds the host scenarios that exercise
  set-up end to end (`ManualSortingAcceptanceTest`, `ConnectionDefaultsAcceptanceTest`, the `RecurringBlackoutRules*`
  integration tests, `BehaviourSettings*`, `FeatureMoveAuthOffIntegrationTest`, the forecast-filter and reality-check
  suites, `RefinementAtThePortTest`, `ReportsAtThePortTest`, `Slice08HealthThatChecksItselfSpecifications`).
- `Integration.Containers.*` (needs a Postgres container) and `Integration/UsageData/OptionalFeatureToggledEventTests`
  (`Integration` namespace, starts a host).
- `Services/Implementation/Seeding/OptionalFeatureSeederTests` (`IntegrationTestBase`, starts a host),
  `WorkTrackingSystemConnectionControllerTest` / `WorkTrackingSystemConnectionsControllerTest` (carry live-connector
  categories) and everything `WriteBack` (`BlackoutForecastShiftWriteBackTest`, `WorkTrackingSystemConnectionControllerWriteBackTest`),
  whose set-up writes to a real tracker.
- Every live-connector category (Jira, Linear, Ado, ServiceNow, Github) and `Dogfood`.
- The architecture tests (`Tests/Architecture`): they read IL, not behaviour, and kill no mutant.
- The `Acceptance/*` tests that mention set-up services in passing (cycle-time outliers, named cycle time, work-item
  refresh): they drive other modules and only use set-up as a collaborator; not needed for coverage of set-up's files.

## Survivors and no-coverage mutants

The third column is a reading of the code, not checked by a test. Most of the no-coverage block is behaviour that only
the excluded host scenarios observe.

| File:line | Mutation | Likely reason it survives |
| --- | --- | --- |
| `AppSettingService.cs:79`, `:141` | `await repository.Save()` removed | Unit tests use a repository mock that is not asked whether `Save` ran; the host scenarios read the stored value back. |
| `AppSettingService.cs:180` | `repository.Update(existing)` removed | Same: the mock repository is not verified for the update call. |
| `AppSettingService.cs:198` | statement removed | Same family (persist call on a mock). |
| `AppSettingService.cs:125` (no coverage) | block removed | Branch reached only through the host scenarios (`BehaviourSettings*`). |
| `AppSettingService.cs:217` | log message string → `""` | Log wording; no test asserts the message text. |
| `BlackoutPeriodService.cs:67`, `:84`, `:97` | message / log string → `""` | Message wording; no test asserts the text. |
| `ConnectionSaveService.cs:216` (×2, no coverage) | `logger.LogError(...)` call and its message removed | The "saved, but the change to its default could not be logged" path needs a failing change-log sink; reached by no included test. |
| `ConnectionSaveService.cs:259` | `AsEnumerable()` → `Reverse()` | Order of owners from `GetAllByPredicate` is not observable in the assertions: likely equivalent for the result as a set. |
| `WorkTrackingSystemConnectionUsage.cs:30` | string → `""` | Wording of a label; not asserted. |
| `WorkTrackingSystemConnectionUsage.cs:31` | `First()` → `FirstOrDefault()` | Group is never empty in the tests; likely equivalent (a group has at least one row). |
| `WorkTrackingSystemConnectionUsage.cs:36`, `:37` | `AsEnumerable()` → `Reverse()` | Team and portfolio order in the usage list is not asserted. Real small gap: a test with two owners and an order assertion kills both. |
| `FeatureOrdering.cs:20, 25, 30, 43` (7, no coverage) | policy conditionals, `OrderBy`/`ThenBy` direction, `!=` on the policy | Ordering is observed through the host scenarios (`ManualSortingAcceptanceTest`, `FeatureMoveAuthOffIntegrationTest`) and the forecast and feature-list suites that sit in `API.Integration`. |
| `FeatureOrdering.cs:40` | `ThenBy` → `ThenByDescending` | Tie-break among equal manual ranks is not asserted. |
| `FeatureOrderingPolicyProvider.cs:17, 22` (5, no coverage) | key match, `setting?.Enabled != true`, both branches | No included test reads the policy from a real optional-feature row; the host scenarios do. |
| `FeatureRankSeeder.cs:30`, `:44` (no coverage) | statements removed | Seeding a missing rank is observed only by the host start-up scenarios. |
| `FeatureRankSeeder.cs:39` | `&&` → `\|\|` | Real small gap: the unit test has no feature that is placed and unranked at the same time. |
| `FeatureRankSeeder.cs:47` | `?? 0` left removed | Same test data: every ranked feature has a non-null rank. |
| `FeatureRankingService.cs` (24, no coverage) | every mutant of `Move`, target lookup, index arithmetic, rank renumbering | No unit test of the service in this repo; its whole behaviour is pinned by `ManualSortingAcceptanceTest` (host), excluded. Not a gap of the move. |
| `DefaultOptionalFeatureApplier.cs:13` (no coverage) | string → `"Stryker was here!"` | Message of a no-op applier; not asserted. |
| `FeatureOrderingApplier.cs:38` | statement removed | Side effect on a mock, not verified. |
| `TerminologyService.cs:18` | `OrderBy` → `OrderByDescending` | Real small gap: the terminology list order is not asserted. |
| `TerminologyService.cs:35` | statement removed | Persist call on a mock repository, not verified. |
| `RecurringBlackoutRuleExtensions.cs:9` (×2) | `windowStart > rule.Start` → `>=` (equivalent when equal), and the conditional forced `true` | The first is equivalent (same value when equal). The forced-`true` one is a real gap: no test expands a rule that starts inside the window. |
| `RecurringBlackoutRuleExtensions.cs:10` | `rule.End.Value > windowEnd` → `>=` | Equivalent: when equal the two branches give the same date. |

No kill round: the score is above the bar and the survivors are persist calls on mocks, log or message wording, or paths the
excluded host scenarios observe. They are recorded rather than killed, as in 6366. A few are real unit-level gaps worth a
test if the maintainer wants a kill round: `FeatureRankSeeder.cs:39`, `TerminologyService.cs:18`,
`WorkTrackingSystemConnectionUsage.cs:36-37`, `RecurringBlackoutRuleExtensions.cs:9`, and a unit-level test of
`FeatureRankingService`/`FeatureOrderingPolicyProvider` (29 of the 42 no-coverage mutants).

## Against the D-15 bar

**Kill rate: met at 80.37 %** (thin margin, 0.37 points).

**No survivor or no-coverage mutant added by the move: met, shown mechanically.** The move is pure. For each of the 27
production `.cs` files that moved into `Modules/OrganisationSetup` (git renames between `987a393f2` and `HEAD`, all 27
under `Modules/OrganisationSetup`), the content without `using` and `namespace` lines, blank lines and carriage returns is
**identical to the pre-move file in 27 of 27 moved files**. Stricter: with only the `namespace` line removed, 19 of 27 are
identical and the other 8 (`AppSettingService`, `BlackoutPeriodService`, `ConnectionSaveService`, `FeatureRankingService`,
`DefaultOptionalFeatureApplier`, `FeatureOrderingApplier`, `OptionalFeatureApplierRegistry`, `RecurringBlackoutRuleService`)
differ also in `using` lines, which compile to no mutable code. The move changed only namespaces and usings, so no mutant
can be new, and each survivor above is one the pre-move file had. A mutant-by-mutant comparison against a run on the
pre-move revision was therefore not made.

Command used for the proof, per pair: `git show 987a393f2:<old> | tr -d '\r' | grep -vE '^\s*(using |namespace |$)' | md5sum`
against the same filter on `git show HEAD:<new>`.
