# Mutation report — slice-03 graceful shutdown (US-03 / ADO #5309)

**Date**: 2026-06-20
**Config**: `Lighthouse.Backend.Tests/stryker-config.epic-5305-graceful-shutdown.json`
**Tool**: Stryker.NET, `coverage-analysis: perTestInIsolation`

## Verdict: introduced surface ≥ 80% (behavioral 100%)

The kill rate on the surface **slice-03 introduced** is 100% on behaviour. The
only survivors on introduced lines are one error-path log statement (two
non-behavioral mutants). The raw whole-config score (46.88%) is dominated by
**pre-existing** `UpdateQueueService` code that slice-03 did not touch and that
had never been under mutation test in any prior slice — it is out of this
slice's scope.

## Introduced surface (slice-03)

| File | Killed | Survived | Score |
|------|-------:|---------:|------:|
| `Health/ReadinessState.cs` | 1 | 0 | 100% |
| `Health/DrainReadinessHealthCheck.cs` | 5 | 0 | 100% |
| `Health/GracefulShutdownService.cs` | 1 | 0 | 100% |
| `UpdateQueueService.DrainAsync` (L36–48) | behavioral all killed | 2 (logging) | 100% behavioral |

`DrainAsync` behavioral mutants killed:
- `queue.Writer.TryComplete()` removal → consumer never completes → `DrainAsync`
  hangs → drain test times out (Stryker timeout = killed).
- `await processingTask.WaitAsync(cancellationToken)` → the bounded-drain test
  (`DrainAsync_QueueExceedsTimeout_…`) catches a regression to an unbounded wait.

### Justified survivors on introduced lines

| Loc | Mutant | Justification |
|-----|--------|---------------|
| `DrainAsync` L46 | `logger.LogWarning(...)` statement removal | Error-path logging only; no behavioral contract. House style excludes log-only mutants. |
| `DrainAsync` L46 | log message string → `""` | Same — log text is not asserted behaviour. |

Killing these would require asserting on the `ILogger` (an extension method,
brittle to verify) for no behavioural gain — excluded per the project's
non-behavioral-logging convention.

## Out of scope — pre-existing `UpdateQueueService` surface

`UpdateQueueService.cs` is in the mutate set because `DrainAsync` lives in it,
but the file's other 21 survivors / no-coverage mutants are in code slice-03
never modified: `EnqueueUpdate`, `EnqueueAndAwaitAsync`, `RegisterCancellation`,
`ExecuteUpdateAsync`, `ExecuteAwaitableUpdateAsync`, `StartProcessingQueue`,
`NotifyListeners`. These are predominantly `LogInformation` strings, fire-and-
forget `NotifyListeners` statements, and the `RegisterCancellation`
continuation. They pre-date slice-03 and were never under a mutation gate;
hardening them is tracked as inherited debt, not a slice-03 deliverable.

## Strengthening applied this slice

- `DrainReadinessHealthCheck` / `GracefulShutdownService`: added
  `ArgumentNullException` ctor-guard tests (killed the null-coalescing mutants).
- `DrainReadinessHealthCheck`: assert `result.Description` is non-empty in both
  states (killed the health-description string mutants — operator-facing text).
