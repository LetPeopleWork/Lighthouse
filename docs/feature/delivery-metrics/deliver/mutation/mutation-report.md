# Mutation Report — delivery-metrics (Slice 1, backend)

## Backend (Stryker.NET)

Tool: `dotnet-stryker` 4.14.2 · config `Lighthouse.Backend.Tests/stryker-config.delivery-metrics.json`
Scope: feature production code only (controller + updater dispatch lines scoped by line range so pre-existing code is not diluted).
Test filter: the 9 feature test classes (`DeliveryMetricsHistoryReadApiIntegrationTest`, `DeliveryMetricSnapshotCascadeDeleteIntegrationTest`, `DeliveryMetricsHistoryDtoTest`, `DeliveryMetricSnapshotRecordingHandlerTest`, `DeliveryMetricSnapshotRepositoryTest`, `PortfolioUpdaterTest`, `ForecastUpdaterTest`, `DeliveriesControllerTest`, `DeliveriesControllerUtcTest`).

### Result

**Final score: 85.29 % (29 killed / 34 covered mutants). Verdict: PASS (≥ 80 %).**

Baseline before hardening: 70.59 % (24 killed / 30 covered + 4 no-coverage). Five targeted tests added; all genuine gaps closed. The 5 remaining survivors are justified equivalents (4 logging-only + 1 Stryker static-initializer limitation).

### Per-file kill rate

| File (mutate scope) | Killed / Covered | Rate |
|---|---|---|
| `API/DTO/DeliveryMetricsHistoryDto.cs` (whole) | 9 / 10 | 90 % |
| `Services/Implementation/DomainEvents/DeliveryMetricSnapshotRecordingHandler.cs` (whole) | 5 / 9 | 56 %\* |
| `Services/Implementation/Repositories/DeliveryMetricSnapshotRepository.cs` (whole) | 15 / 15 | 100 % |
| `API/DeliveriesController.cs` (lines 38-60, 299-312) | all covered, 0 survivors | 100 % |
| `BackgroundServices/Update/PortfolioUpdater.cs` (line 86) | all covered, 0 survivors | 100 % |
| `BackgroundServices/Update/ForecastUpdater.cs` (line 46) | all covered, 0 survivors | 100 % |

\* The handler's behaviorally-significant logic (Sum aggregation, done-work arithmetic, idempotent get-or-create, rethrow-on-failure) is fully killed; the 4 surviving handler mutants are all logging diagnostics with no observable effect — see justifications below. The real-logic surface of the handler is 100 % killed.

### Survivors killed (genuine test gaps closed)

| # | File · mutant | Why it survived | Test added |
|---|---|---|---|
| 1 | `DeliveryMetricSnapshotRecordingHandler` L24 `Sum() → Max()` (TotalWork) | every existing handler test seeded a delivery with a single `FeatureWork`, so `Sum == Max` | `HandleAsync_DeliverySpanningMultipleFeatures_RecordsSummedWorkAcrossThemNotASingleFeature` — seeds two features (10+5 total, 6+3 remaining); asserts TotalWork=15 (Max would give 10) |
| 2 | `DeliveryMetricSnapshotRecordingHandler` L25 `Sum() → Max()` (RemainingWork) | same single-feature gap | same test — asserts RemainingWork=9 (Max would give 6) |
| 3 | `DeliveryMetricSnapshotRecordingHandler` L47 `throw; → ;` (catch-block rethrow) | no test exercised the failure path, so swallowing the exception went unnoticed | `HandleAsync_WhenSnapshotPersistenceFails_PropagatesTheException` — Moq `IDeliveryMetricSnapshotRepository.Save()` throws; asserts the handler rethrows `InvalidOperationException` |
| 4 | `DeliveryMetricSnapshotRepository` L16 `RecordedAt < nextDay → <= nextDay` (day-window upper bound) | no test placed a snapshot exactly at next-midnight | `GetOrCreateForDay_SnapshotAtNextMidnightBoundary_IsExcludedSoANewRowIsCreated` — a row at next-midnight must NOT be reused; asserts a NEW same-day row is created (count=2) |
| 5 | `DeliveryMetricSnapshotRepository` L15 `OrderBy → OrderByDescending` (which same-day row is returned) | no test had two same-day rows, so ordering was unobservable | `GetOrCreateForDay_TwoSnapshotsSameDay_ReturnsTheEarliestRecorded` — two same-day rows; asserts the earliest is returned (descending would return the later) |

Additionally, the camelCase-JSON parse path is now covered by `From_ParsesCamelCaseWhenDistributionJson_IntoProbabilityAndExpectedDatePoints` (added for survivor #6 below; it kills the parse mutants and behaviorally guards the case-insensitive option, even though Stryker's static-init isolation can't credit it).

### Survivors justified as equivalent / uncatchable

| File · mutant | Classification | Reasoning |
|---|---|---|
| `DeliveryMetricsHistoryDto` L23 `PropertyNameCaseInsensitive = true → false` (`"static": true`) | Stryker tooling limitation, behavior IS covered | This is a **static field initializer** mutant. The `JsonSerializerOptions` instance is constructed once at type-init and cached; under `coverage-analysis: perTestInIsolation` the mutated initializer never re-runs in the isolated context, so Stryker cannot kill it regardless of test quality (`coveredBy` is populated, `killedBy` empty). The behavior is genuinely guarded: a standalone probe confirms that with `false`, camelCase keys (`probability`/`expectedDate`) fail to bind to the PascalCase positional record params (Probability=0, default date), and `From_ParsesCamelCaseWhenDistributionJson_…` asserts the parsed values are 0.85 and the real date — that test fails if the option is flipped. Not a real gap. |
| `DeliveryMetricSnapshotRecordingHandler` L35 `stopwatch.Stop(); → ;` (happy path) | Equivalent (diagnostics-only) | `Stop()` only freezes `ElapsedMilliseconds` for the subsequent info log. Removing it changes a logged timing number, not any observable behavior or persisted state. |
| `DeliveryMetricSnapshotRecordingHandler` L42 `stopwatch.Stop(); → ;` (catch path) | Equivalent (diagnostics-only) | Same — only affects the elapsed-ms value in the error log. The observable failure behavior (rethrow) is covered and killed at L47. |
| `DeliveryMetricSnapshotRecordingHandler` L43 `logger.LogError(...); → ;` | Equivalent (diagnostics-only) | Removes the error-log statement. No observable behavior change — the exception still propagates (L47 killed). Asserting on a log call/message would be implementation-mirroring testing-theater, explicitly rejected by the project's test conventions. |
| `DeliveryMetricSnapshotRecordingHandler` L45 `"Failed to record…" → ""` | Equivalent (diagnostics-only) | Mutates the error-log message text. Pure diagnostics; no behavioral contract. |

### Working-tree safety

After the Stryker run, `git status` shows **zero mutated production source files** under `Lighthouse.Backend/Lighthouse.Backend/`. Only the three test files and the new Stryker config are modified/added. In-scope tests re-run green after restore (80 passed, 2 ignored future-slice, 0 failed). `StrykerOutput/` is gitignored and not committed.
