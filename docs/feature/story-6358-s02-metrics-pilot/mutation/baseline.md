# Story 6358 (S02 Flow metrics pilot): baseline mutation run, before the move

This run measures the Flow metrics production files at their current (pre-move) paths. Slice 8 runs the same
measurement on `Modules/FlowMetrics/**` after the move, and compares the scores file by file (old path to new
path by type name). Survivors were not chased here: this is a baseline.

| Item | Value |
|---|---|
| Commit | `56029f20136aa3123689c35df4595724e1711de3` (`56029f201`, detached worktree outside the repository) |
| Date | 2026-11-08, started 18:27:30 +01:00 |
| Tool | Stryker.NET 5.0.0 (`Lighthouse.Backend/.config/dotnet-tools.json`) |
| Config | [`stryker.s02-baseline.json`](stryker.s02-baseline.json) |
| Mutate | 42 whole-file entries: every file of the "Component decomposition" table whose change is MOVE. No line spans. |
| Test filter | 36 class-name terms (38 unit test classes) over the metrics types; excludes `API.Integration`, `Integration.Containers` and `TestCategory=Integration`. 923 tests found. |
| Coverage analysis | `perTest`, concurrency 4 |
| Run time | 6 min 46 s (Stryker's "Time Elapsed 00:06:46") |
| Report | [`baseline-mutation-report.json`](baseline-mutation-report.json): the run's `mutation-report.json` reduced to the 42 scoped files (the full report was 19 MB, because it lists every file of the project) |

## Command

From `Lighthouse.Backend/Lighthouse.Backend/` of a worktree at the commit above:

```bash
dotnet tool restore
nohup dotnet stryker --config-file ../../docs/feature/story-6358-s02-metrics-pilot/mutation/stryker.s02-baseline.json > stryker.log 2>&1 &
```

## Scope check

Checked in `mutation-report.json`, not in the created-count (Stryker created 23,935 mutants for the whole
project before the filter; 1,477 were tested).

- All 42 `mutate` entries are in the report. No tested mutant is outside them.
- 12 files have no mutants at all, and that is correct: the 11 interfaces (`Services/Interfaces/**`) hold only
  declarations and records, and `ProcessBehaviorSeriesQuery.cs` is a one-line pass-through to the repository with
  nothing to mutate. They stay in the config so that slice 8 compares the same file set.
- 524 scoped mutants are `Ignored` (Stryker comments in the source and the "block already covered" filter).
- 183 scoped mutants are `CompileError`. They were never tested, so they are neither killed nor survived. Most
  are in `BaseMetricsService.cs` (65), `TeamMetricsService.cs` (36) and `PortfolioMetricsService.cs` (30).

## Per-file scores

Score = (killed + timeout) / (killed + survived + no coverage + timeout), the same formula as Stryker's.

| File (under `Lighthouse.Backend/Lighthouse.Backend/`) | Killed | Survived | No coverage | Timeout | Compile error | Score |
|---|---:|---:|---:|---:|---:|---:|
| `Services/Implementation/BackgroundServices/OverTimeFillTarget.cs` | 16 | 0 | 0 | 0 | 0 | 100.00 % |
| `Services/Implementation/BackgroundServices/OverTimeHistoryFiller.cs` | 80 | 10 | 0 | 0 | 4 | 88.89 % |
| `Services/Implementation/BackgroundServices/ReconstructionMemo.cs` | 20 | 0 | 0 | 0 | 4 | 100.00 % |
| `Services/Implementation/BackgroundServices/RetentionEdge.cs` | 21 | 1 | 0 | 0 | 2 | 95.45 % |
| `Services/Implementation/BaseMetricsService.cs` | 270 | 79 | 50 | 1 | 65 | 67.75 % |
| `Services/Implementation/BaselineValidationService.cs` | 42 | 0 | 0 | 0 | 1 | 100.00 % |
| `Services/Implementation/BlockedCountSeriesBuilder.cs` | 16 | 2 | 0 | 0 | 0 | 88.89 % |
| `Services/Implementation/DomainEvents/BlackoutConfigurationChangedMetricsInvalidationHandler.cs` | 2 | 0 | 0 | 0 | 0 | 100.00 % |
| `Services/Implementation/DomainEvents/BlockedCountSnapshotRecordingHandler.cs` | 17 | 0 | 0 | 0 | 2 | 100.00 % |
| `Services/Implementation/DomainEvents/DemoBlockedHistoryBackfillHandler.cs` | 34 | 9 | 24 | 0 | 7 | 50.75 % |
| `Services/Implementation/DomainEvents/DemoPercentilesBackfillHandler.cs` | 71 | 14 | 2 | 0 | 7 | 81.61 % |
| `Services/Implementation/DomainEvents/PercentilesOverTimeRecordingHandler.cs` | 15 | 0 | 0 | 0 | 0 | 100.00 % |
| `Services/Implementation/DomainEvents/PortfolioFeaturesRefreshedMetricsInvalidationHandler.cs` | 3 | 0 | 0 | 0 | 0 | 100.00 % |
| `Services/Implementation/DomainEvents/ProcessBehaviorRecordingHandler.cs` | 15 | 0 | 0 | 0 | 0 | 100.00 % |
| `Services/Implementation/GapAskingPercentilesOverTimeSeriesQuery.cs` | 1 | 0 | 0 | 0 | 0 | 100.00 % |
| `Services/Implementation/GapAskingProcessBehaviorSeriesQuery.cs` | 1 | 0 | 0 | 0 | 0 | 100.00 % |
| `Services/Implementation/MetricsCacheKey.cs` | 2 | 0 | 0 | 0 | 0 | 100.00 % |
| `Services/Implementation/OverTimeGapReconciler.cs` | 13 | 1 | 0 | 0 | 8 | 92.86 % |
| `Services/Implementation/PercentileCalculator.cs` | 10 | 1 | 0 | 0 | 6 | 90.91 % |
| `Services/Implementation/PercentileFamilies.cs` | 2 | 0 | 0 | 0 | 0 | 100.00 % |
| `Services/Implementation/PercentileSnapshotWriter.cs` | 37 | 0 | 1 | 0 | 2 | 97.37 % |
| `Services/Implementation/PercentilesOverTimeSeriesQuery.cs` | 2 | 1 | 0 | 0 | 0 | 66.67 % |
| `Services/Implementation/PortfolioMetricsService.cs` | 69 | 113 | 68 | 0 | 30 | 27.60 % |
| `Services/Implementation/ProcessBehaviorSeriesQuery.cs` | 0 | 0 | 0 | 0 | 0 | n/a (no mutants) |
| `Services/Implementation/ProcessBehaviorSnapshotWriter.cs` | 34 | 0 | 0 | 0 | 5 | 100.00 % |
| `Services/Implementation/Repositories/BlockedCountSnapshotRepository.cs` | 8 | 0 | 0 | 0 | 0 | 100.00 % |
| `Services/Implementation/Repositories/PercentilesOverTimeSnapshotRepository.cs` | 14 | 0 | 0 | 0 | 0 | 100.00 % |
| `Services/Implementation/Repositories/ProcessBehaviorSnapshotRepository.cs` | 12 | 0 | 0 | 0 | 0 | 100.00 % |
| `Services/Implementation/SleRiskCalculator.cs` | 20 | 2 | 0 | 0 | 2 | 90.91 % |
| `Services/Implementation/TeamMetricsService.cs` | 100 | 112 | 115 | 0 | 36 | 30.58 % |
| `Services/Implementation/XmRCalculator.cs` | 169 | 15 | 0 | 0 | 2 | 91.85 % |
| `Services/Interfaces/BackgroundServices/IOverTimeHistoryFiller.cs` | 0 | 0 | 0 | 0 | 0 | n/a (no mutants) |
| `Services/Interfaces/IOverTimeGapReconciler.cs` | 0 | 0 | 0 | 0 | 0 | n/a (no mutants) |
| `Services/Interfaces/IPercentileSnapshotWriter.cs` | 0 | 0 | 0 | 0 | 0 | n/a (no mutants) |
| `Services/Interfaces/IPercentilesOverTimeSeriesQuery.cs` | 0 | 0 | 0 | 0 | 0 | n/a (no mutants) |
| `Services/Interfaces/IPortfolioMetricsService.cs` | 0 | 0 | 0 | 0 | 0 | n/a (no mutants) |
| `Services/Interfaces/IProcessBehaviorSeriesQuery.cs` | 0 | 0 | 0 | 0 | 0 | n/a (no mutants) |
| `Services/Interfaces/IProcessBehaviorSnapshotWriter.cs` | 0 | 0 | 0 | 0 | 0 | n/a (no mutants) |
| `Services/Interfaces/ITeamMetricsService.cs` | 0 | 0 | 0 | 0 | 0 | n/a (no mutants) |
| `Services/Interfaces/Repositories/IBlockedCountSnapshotRepository.cs` | 0 | 0 | 0 | 0 | 0 | n/a (no mutants) |
| `Services/Interfaces/Repositories/IPercentilesOverTimeSnapshotRepository.cs` | 0 | 0 | 0 | 0 | 0 | n/a (no mutants) |
| `Services/Interfaces/Repositories/IProcessBehaviorSnapshotRepository.cs` | 0 | 0 | 0 | 0 | 0 | n/a (no mutants) |
| **Total (42 files)** | **1,116** | **360** | **260** | **1** | **183** | **64.31 %** |

Score over covered mutants only (no-coverage left out): 75.63 %.

## Reading the low scores

`TeamMetricsService.cs` (30.58 %) and `PortfolioMetricsService.cs` (27.60 %) carry 225 of the 360 survivors and
183 of the 260 no-coverage mutants. Their behaviour is pinned mostly by the `API.Integration` acceptance
scenarios, which this run leaves out on purpose (a `WebApplicationFactory` per test makes a run take over an hour).
`DemoBlockedHistoryBackfillHandler.cs` (50.75 %) has 24 no-coverage mutants for the same reason. These are
baseline figures, not findings: slice 8 must not score lower per file, with the same filter.

## What slice 8 changes in the config

Only the `mutate` paths: the same 42 files under `Modules/FlowMetrics/{Domain,Application,Adapters}/`. The test
class names stay the same after the test move (D-9), so the `test-case-filter` carries over unchanged. Compare file
by file, not by the total.
