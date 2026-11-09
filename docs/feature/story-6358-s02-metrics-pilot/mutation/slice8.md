# Story 6358 (S02 Flow metrics pilot): slice 8 mutation run, after the move

This run measures the same 42 Flow metrics production files as the [baseline](baseline.md), now at their new paths
under `Lighthouse.Backend/Lighthouse.Backend/Modules/FlowMetrics/`, on frozen code. The design's bar for this
behaviour-free move: no file may score lower than its baseline, with the same test filter.

| Item | Value |
|---|---|
| Commit | `50c4a7367` (detached worktree outside the repository, after feature C's candidate review) |
| Date | 2026-11-09, started 11:05:26 +01:00 |
| Tool | Stryker.NET 5.0.0 (`Lighthouse.Backend/.config/dotnet-tools.json`) |
| Config | [`stryker.s02-slice8.json`](stryker.s02-slice8.json): the baseline config with only the 42 `mutate` paths changed to the module paths |
| Test filter | unchanged from the baseline (class-name terms; the test classes kept their names when they moved) |
| Coverage analysis | `perTest`, concurrency 4 |
| Run time | 6 min 44 s (Stryker's "Time Elapsed 00:06:43") |
| Report | [`slice8-mutation-report.json`](slice8-mutation-report.json): the run's report reduced to the 42 module files, without the `testFiles` section |

## Scope check

Checked in `mutation-report.json`: 42 files under `Modules/FlowMetrics` are in the report, and no Killed or Survived
mutant lies outside them. Stryker tested 1,477 mutants, the same number as the baseline.

## Result: identical to the baseline, file by file

| | Killed | Survived | No coverage | Timeout | Compile error | Ignored | Score |
|---|---:|---:|---:|---:|---:|---:|---:|
| Baseline (`56029f201`, old paths) | 1,116 | 360 | 260 | 1 | 183 | 524 | 64.31 % |
| Slice 8 (`50c4a7367`, module paths) | 1,116 | 360 | 260 | 1 | 183 | 524 | 64.31 % |

Joined by file name, all 42 files have the same killed, survived, no-coverage and timeout counts as in the
baseline, so every per-file score is unchanged (see the per-file table in [baseline.md](baseline.md)). The bar is met:
no file scores lower.

## The project's 80 % minimum

The Story did not change behaviour, so this run cannot raise a score, and it does not reach the project's 80 %
minimum in total. The files below the minimum were below it before the Story:

| File | Score | Why it is low |
|---|---:|---|
| `Application/PortfolioMetricsService.cs` | 27.60 % | Pinned mostly by the `API.Integration` scenarios, which the filter leaves out (a host per test makes a run take over an hour) |
| `Application/TeamMetricsService.cs` | 30.58 % | Same |
| `Application/Policies/DemoBlockedHistoryBackfillHandler.cs` | 50.75 % | 24 no-coverage mutants, same reason |
| `Application/PercentilesOverTimeSeriesQuery.cs` | 66.67 % | 1 survivor of 3 |
| `Application/BaseMetricsService.cs` | 67.75 % | 79 survivors, 50 no coverage |

Killing those survivors means new unit tests for the metrics services. That is behaviour-test work outside a
behaviour-free move, so it is left for the maintainer to schedule as its own improvement item.

## Command

From `Lighthouse.Backend/Lighthouse.Backend/` of a worktree at the commit above, after building both migration
projects (the backend references their DLLs by path):

```bash
dotnet tool restore
dotnet stryker --config-file <path>/stryker.s02-slice8.json
```
