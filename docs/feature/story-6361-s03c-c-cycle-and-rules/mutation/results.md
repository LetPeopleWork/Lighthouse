# Mutation — Story #6361 S03c (Package Forecasting), final gate

Run on frozen code, after the whole-candidate review and EXAMINE of feature C (HEAD 1bcea3c1f, 2026-11-15).

## Bar (maintainer, hold 1, 2026-11-14; scope option A chosen 2026-11-15)

- Moved code: no survivor or no-coverage mutant added.
- New code: 80 % or more.

## Moved code — proved by construction, not by a run

`moved_diff.sh` compares every production file under `Modules/Forecasting` at HEAD with the file of the same name at
`39a7f598f`, the last revision before the move. Output: `moved-files-vs-39a7f598f.txt`.

- 30 of 32 files: identical apart from `using` and `namespace` lines.
- `ForecastFilterRuleService.cs`: only its class line gains `, IThroughputFilter`.
- `ForecastingAnswersToFlowMetrics.cs`: new (below).

Identical code under the same tests yields the same mutants with the same outcomes, so no survivor can be added.
`TeamMetricsService` and `BaseMetricsService` changed only the declared type of an injected dependency; every call
and branch is the same as before (diff in the commit body of 9232beb3f), so the same argument holds.

## New code — Stryker.NET

Config: `stryker.story6361c.json` (whole-file `mutate`, `perTest`, Flow metrics and Forecasting unit tests).
Report: `mutation-report.adapter.json`. Scope line from the run: `1 total mutants will be tested`.

| File | Tested | Killed | Survived | Score |
|---|---|---|---|---|
| `Modules/Forecasting/Application/ForecastingAnswersToFlowMetrics.cs` | 1 | 0 | 1 (equivalent) | 100 % of non-equivalent mutants |

The two answering methods are single delegating expressions, for which Stryker.NET has no mutator.

**Survivor, line 18, equivalent:** `field ??= serviceProvider.GetRequiredService<IForecastService>()` becomes
`field = serviceProvider.GetRequiredService<IForecastService>()`. The mutant asks the scope's service provider again on
every How-many question instead of keeping the first answer. `IForecastService` is registered scoped, and this class is
scoped, so the provider returns the same instance each time: no observable difference, and no test can tell them
apart. Recorded, not killed.

## Verdict

Bar met: no survivor added in moved code; the only new logic has no non-equivalent survivor.
