# Mutation testing — 6657 (Place the write-back round in Forecasting)

Run 2026-11-26 in a detached worktree at `6dbfb9536` (production frozen since the S2 move `e22d88adf`), Stryker.NET
(local tool manifest) on Linux x64, in Stryker's own sandbox. Gate: at least 80 % of the moved code, **and** no survivor
or no-coverage mutant added by the move.

| stack | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend, `Modules/Forecasting/Application/WriteBack/**` | **89.17 %** | 157 | 140 | 10 | 7 | 4 m 48 s |
| Frontend | N/A — no frontend file changed (namespace move and rule files only) | | | | | |

Config: `stryker.6657.backend.json`. The filter takes the round's own unit tests (now under
`Tests/Modules/Forecasting/Application/WriteBack`), the updater and queue unit tests that start a round, and
`RoundWriteBackDuringDatabaseMaintenanceTest`; it leaves out the host-starting suites, Dogfood, the live connector
categories and `JiraWriteBackTest` / `AzureDevOpsWriteBackTest`, which write to a real tracker in their set-up.

## Nothing was added by the move

All 14 production files the Story touched are byte-identical to their pre-move state once `namespace` and `using`
lines are set aside (8 moved round files, plus `ForecastUpdater`, `UpdateQueueService`, `TeamUpdater`,
`PortfolioUpdater`, `UpdateServiceBase` and `Program.cs`, which only gained a `using`). No production file changed after
the move. So every survivor below was already there before this Story; the move cannot have added one. No kill round
was run: the score is above the bar, and killing pre-existing survivors is not this behaviour-free Story's scope.

## Per file (killed / survived / no coverage)

| File | K / S / NC | Score of tested |
| --- | --- | --- |
| `WriteBackCollector.cs` | 8 / 2 / 0 | 80.0 % |
| `WriteBackRound.cs` | 16 / 3 / 3 | 72.7 % |
| `WriteBackService.cs` | 50 / 3 / 2 | 90.9 % |
| `WriteBackTriggerService.cs` | 66 / 2 / 2 | 94.3 % |
| `WriteBackRoundContext.cs`, `IWriteBackCollector.cs`, `IWriteBackService.cs`, `IWriteBackTriggerService.cs` | no mutant | — |

The glob also matched the `Models/WriteBack` value types that share the folder name (`NotificationSuppression`,
`WriteBackAppliesTo`, …); they yield no mutant and are not part of this Story.

## Accepted survivors (all pre-existing)

- `WriteBackCollector.cs` L52 block removal, L56 statement removal.
- `WriteBackRound.cs` L57 logical mutation and the no-coverage equality/boolean mutants at L57 and statement at L59
  (the skipped-forecast branch), L80 null-coalescing (remove left), L117 statement removal.
- `WriteBackService.cs` L39 statement removal, L94 `Order()` → `OrderDescending()`, L169 string (no coverage),
  L182 equality (no coverage), L183 null-coalescing (remove left).
- `WriteBackTriggerService.cs` L46 equality (no coverage), L191 statement and L192 string, L362 string (no coverage).

They are candidates for a later improvement item on the write-back round's tests, not for this Story.
