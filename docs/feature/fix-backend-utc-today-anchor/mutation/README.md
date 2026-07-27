# Mutation testing — Bug #5567 (backend, feature-scoped)

Stryker.NET 4.16.0, run 2026-07-27 against `origin/main..HEAD`.

## Result — PASSES the project's 80% gate

**83.86% (239 killed / 285 scored)** — 41 survived, 5 no-coverage, 0 timeouts, 0 errors.

| File | Killed | Scored | Score |
|---|---|---|---|
| `Models/InstanceCalendar.cs` | 3 | 3 | **100.00%** |
| `Services/.../Repositories/DeliveryMetricSnapshotRepository.cs` | 11 | 11 | **100.00%** |
| `Services/.../DomainEvents/ProcessBehaviorRecordingHandler.cs` | 38 | 38 | **100.00%** |
| `Services/.../DomainEvents/BlockedCountSnapshotRecordingHandler.cs` | 17 | 17 | **100.00%** |
| `Services/.../DomainEvents/PercentilesOverTimeRecordingHandler.cs` | 31 | 32 | 96.88% |
| `Services/Implementation/LighthouseClock.cs` | 14 | 15 | 93.33% |
| `Services/.../DomainEvents/DeliveryMetricSnapshotRecordingHandler.cs` | 24 | 28 | 85.71% |
| `Models/Delivery.cs` | 22 | 27 | 81.48% |
| `Models/WorkItemBase.cs` | 29 | 36 | 80.56% |
| `Models/Team.cs` | 7 | 9 | 77.78% |
| `Models/Feature.cs` | 24 | 31 | 77.42% |
| `Data/DeliveryMetricSnapshotDayCollisionGuard.cs` | 16 | 29 | 55.17% |
| `Services/Implementation/ServiceConfig.cs` | 3 | 9 | 33.33% |
| **Total** | **239** | **285** | **83.86%** |

The first run scored **77.19%** (220/285). Nineteen mutants were closed with seven new tests; no
test was deleted, weakened or `Stryker disable`d to get there, and no production file was touched.

## Scope

Fourteen production files, chosen as the arithmetic and persistence core of the fix rather than all
53 files the feature changed. The 39 files **not** covered are listed under "Not covered" below.

## What the surviving mutants are

### Genuine gaps, closed (19 mutants, 7 tests)

| Where | Mutant | Test that kills it |
|---|---|---|
| `InstanceCalendar.DayOf` ×3 | the `Kind == Local` branch (`==`→`!=`, and both ternary collapses) | `Models/InstanceCalendarTest.cs` — a Local-kind instant must be **converted**, an Unspecified one **relabelled** UTC. The file scored **0%** before: nothing exercised either branch, and this is the one function the whole fix funnels through. |
| `LighthouseClock` ×4 | `when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)` → `is not`; two message strings; the `catch (TimeZoneNotFoundException)` block in `LocalTimeZoneOrUtc` | `UnresolvableTimeZoneId_ExplainsWhichSettingToChange` and `ResolutionOrder_LocalThrowingFallsThroughToUtc`. The pre-existing `UnresolvableTimeZoneId_FailsFastAtStartup` **could not** see these: it asserts `failure.ToString()` contains the bad id, and the raw framework exception names it too — so it passes even when Lighthouse contributes nothing. |
| `WorkItemBase` ×2 | `StartedDate ?? CreatedDate` → `CreatedDate ?? StartedDate`, in both `CycleTime` and `WorkItemAge` | `..._HasBothDates_MeasuresFromStartedNotCreated` ×2. Every existing case set only one of the two dates, so the precedence was never pinned. |
| `DeliveryMetricSnapshotRecordingHandler.ForecastWindowEnd` ×6 | empty-list ternary (both collapses), `Max()`→`Min()`, horizon ternary (both collapses), `>`→`<` | three tests asserting the window handed to `IBlackoutPeriodService` for: no deliveries, deliveries ahead, deliveries all past. |
| `Delivery.GetGoverningFeature` ×1 | `GetLikelhoodForDate(...) >= 0` → `> 0` | `CalculateMetrics_FeatureWithZeroLikelihood_StillGovernsAndReportsForecastDates`. A likelihood of exactly 0 is what an **overdue** delivery scores; excluding it leaves no governing feature, so the recorder sees an empty when-distribution and persists NULL — the deliveries most in trouble would be the ones that stop reporting. |
| `Feature.GetLikelhoodForDate` ×2 | `>` → `>=` and `&&` → `\|\|` on the short-circuit guard | two tests. The existing `..._FeatureHasNoRemainingWork_Returns100` cannot kill either: a bare `Feature` has an empty aggregated forecast, which also answers 100, so both branches agree by accident. |
| `DeliveryMetricSnapshotDayCollisionGuard` ×1 | `GetPendingMigrations().Any(...)` → `.All(...)` | `LegacyInstantsSharingACalendarDay_AfterTheMigrationApplied_NoLongerBlockStartup`. `All()` over an empty pending list is `true`, so the mutant makes a *pre*-migration check into a permanent startup guard that would refuse to start for ever over data the new schema permits. |

### Equivalent mutants — cannot change observable behaviour (3)

1. **`WorkItemBase.CycleTime`, `closedDay >= startDay` → `>`.** When the two days are equal the true
   branch computes `GetDateDifference(d, d)` = `(0) + 1` = **1**, and the false branch is
   `return 1`. The inclusive `+1` makes the boundary case coincide with the fallback. Null on either
   side is `false` under both operators.
2. **`WorkItemBase.WorkItemAge`, `startDay <= today` → `<`.** Identical argument.
3. **`DeliveryMetricSnapshotRecordingHandler`, `latestDeliveryDate > today` → `>=`.** The ternary is
   `latest > today ? latest : today`; at `latest == today` both arms yield the same value.

Contorting a test to "kill" any of these would assert an equality that has no observable side, which
is precisely the tautology (root cause D) this bug was made of.

### Not worth killing — message text and diagnostics (17)

- **`DeliveryMetricSnapshotDayCollisionGuard`, 8 string mutants** in the abort message. The
  *load-bearing* content — the colliding `DeliveryId` and the colliding day, which is decision 9's
  acceptance criterion — is already asserted by
  `CollidingRows_AbortTheMigrationWithADiagnosticNamingThem`. What survives is the surrounding prose
  ("Cannot apply the …", "Lighthouse deliberately does NOT de-duplicate them …"). Asserting the
  wording verbatim restates the implementation.
- **`LighthouseClock:55`, 1 string mutant** — `"such as 'Europe/Zurich', or remove it…"`, the
  illustrative tail. The two parts an operator acts on (the rejected id, the setting name and its
  `__` spelling) are now asserted.
- **`DeliveryMetricSnapshotRecordingHandler`, 4** — two `stopwatch.Stop()` removals and the failure
  log message; `PercentilesOverTimeRecordingHandler`, 1 — a `LogRecordingFailure` call.
- **Property-initialiser string literals, 4** in `WorkItemBase` (`ReferenceId`, `ParentReferenceId`,
  `Type`, `State` = `string.Empty`) plus `Feature.OwningTeam`, `Delivery.Name`, `Feature.IsParentFeature`.

The repo's established pattern for these is `// Stryker disable once all: diagnostic log text is not
behaviour` (see `API/TeamMetricsController.cs:613`). Deliberately **not** applied here: the gate is
already met without shrinking the denominator, and this pass changed zero production files. The
existing disables in the mutated files were left exactly as they are (4 mutants ignored on that
reason during the run).

### Genuine gaps left open (5) — with the reason

1. **`DeliveryMetricSnapshotDayCollisionGuard:91`, `IsNpgsql() ? PostgresCollisionSql : Sqlite…`.**
   The mutant forces the SQLite statement on both providers, and it survives because
   `date("RecordedAt")` is *also* valid on Postgres — the difference is that a bare cast reduces a
   `timestamptz` using the **session** zone while the shipped SQL pins `AT TIME ZONE 'UTC'`, exactly
   as the comment above it warns. Killing it needs a Postgres container whose session `TimeZone` is
   not UTC, seeded with rows that collide in UTC but not in that zone. That is a worthwhile test and
   it is the highest-value item left on this file; it was not written here because it is a new
   container-backed scenario, not a gap in an existing one.
2. **`…Guard:102/104`** — `if (openedHere)` negated, and `connection.Close()` removed. Connection
   lifecycle hygiene; observable only by asserting on `DbConnection.State`, which is white-box.
3. **`…Guard:120/121` (no coverage)** — the `DateTime` and fallback arms of `DayOf(object)`. Npgsql
   returns `DateOnly` and SQLite returns text, so neither arm is reachable through the two shipped
   providers. Defensive code.
4. **`Delivery:71/78/79`, `Sum()` → `Max()` over `FeatureWork`; `Delivery:92`, `ThenBy` →
   `ThenByDescending`; `Feature:112`, `FirstOrDefault` → `First`; `Feature:118/123/137/143`;
   `Team:5`; `ServiceConfig:9/10`.** All pre-existing code that this feature only threaded a `today`
   parameter through — `ServiceConfig`'s six survivors are on the `BaseUrl` and `OAuthStateSecret`
   lines; the `TimeZone` line this feature added is fully killed. Left alone rather than padded with
   tests written to move a number.

## Production defects exposed

**None.** No surviving mutant traced back to a defect in the code under test.

One **test** defect was found by reading, not by Stryker, and is fixed here: five
`WorkItemBaseTest.GetWorkItemAge_*` cases combined a **fixed** fake clock (2026-07-27) with inputs
built from the live `DateTime.UtcNow`. They agreed only on the day they were written and would have
gone red on 2026-07-28. Their inputs now come from the same clock as the expectation; the assertions
are unchanged.

## Not covered by this run

- **39 of the 53 production files the feature changed**: the DTOs and `DtoExtensions`, the nine
  controllers, `Data/DatabaseConfigurator.cs`, `Data/LighthouseAppContext.cs`, `Program.cs`,
  `Factories/DemoDataFactory.cs`, `DemoDataService.cs`, the two demo backfill handlers,
  `BaseMetricsService.cs`, `TeamMetricsService.cs`, `PortfolioMetricsService.cs`,
  `BaselineValidationService.cs`, `Licensing/LicenseService.cs`, `WriteBackTriggerService.cs`,
  `WorkItems/WorkItemService.cs`, both work-tracking connectors, and the three interfaces.
  Reason: a full-solution `perTestInIsolation` run is hours; the fourteen files above are where the
  feature's arithmetic and its persistence decisions actually live.
- **`JiraWriteBackTest`** (`[Category("JiraIntegration")]`) is outside the `test-case-filter` and was
  not run. It reads back from live Jira over an eventually-consistent JQL query and flips red/green
  across identical binaries; including it would make the mutation score non-deterministic.

## Reproducing

`stryker.bug5567.json` beside this README is the config, but it is **not** runnable from here: its
`"solution": "../Lighthouse.sln"` and `"project"` paths only resolve from the test project, and
`.gitignore:442` (`**/stryker-config*.json`) is why none of the six existing backend configs are in
the repo at all. Same arrangement as `docs/feature/fix-widget-eager-fetch-by-category/mutation/` —
copy it across under the conventional name and run:

```
cp docs/feature/fix-backend-utc-today-anchor/mutation/stryker.bug5567.json \
   Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.fix-backend-utc-today-anchor.json
cd Lighthouse.Backend/Lighthouse.Backend.Tests
TZ=Europe/Zurich dotnet stryker --config-file stryker-config.fix-backend-utc-today-anchor.json
```

Two things cost a cycle and are worth keeping:

1. **`TZ=Europe/Zurich` on the command line is mandatory.** Stryker generates its own vstest
   runsettings and does **not** honour the `<RunSettingsFilePath>` pin in
   `Lighthouse.Backend.Tests.csproj`, so the TZ has to be inherited from the invoking shell. Without
   it the host runs UTC — the one offset at which this whole bug class cancels out, so the
   calendar-day tests would pass while proving nothing.
2. **The `test-case-filter` has to name every new fixture.** `perTestInIsolation` over all 3759
   tests is impractical, so the filter narrows the run to 669 tests — but a mutated file whose only
   tests are outside the filter reports `NoCoverage` and counts **against** the score. Run 2 scored
   `InstanceCalendar.cs` at 0% for exactly that reason: the tests existed and passed, and the filter
   did not match `InstanceCalendarTest`.
