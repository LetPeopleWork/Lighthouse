# ADR-249: A cancelled forecast stops between runs, on a token its updater hands down

**Status**: Accepted (maintainer, 2026-10-18, DESIGN; recommendation taken).
**Feature**: `epic-6311-responsive-during-updates` (Bug #6308, slice 02)
**Extends**: [ADR-183](./adr-183-cancellation-ambient-token-with-paging-widened.md) (the ambient update token, widened
explicitly where it is used).

## Context

Stop on a running forecast left its Task Manager row on "Stopping…" for more than 10 minutes. `UpdateQueueService`
sets `UpdateCancellationContext.Current` for every update execution, but only `WorkItemService` reads it.
`ForecastService` takes no token, so nothing ever throws the `OperationCanceledException` that `RunUpdateAsync` maps to
`Cancelled`.

The previous forecast is already kept when the throw happens inside the simulation. Feature forecasts are written after
the simulation returns and saved after that. The write-back staging and `PortfolioForecastsUpdated` come after both.
`ForecastUpdater` already records `Cancelled` on the `RefreshLog` row and rethrows.

## Decision

1. `IForecastService.UpdateForecastsForPortfolio` takes a `CancellationToken`. Its only caller, `ForecastUpdater.Update`,
   resolves `UpdateCancellationContext` from its scope and passes the current token.
2. `ForecastService` hands the token to the simulation as `ParallelOptions.CancellationToken`. `Parallel.For` stops
   starting runs once it is cancelled, and throws `OperationCanceledException`. The check sits **between runs**.
3. The manual `When`, multi-team and probe paths pass no token and run uncancelled, exactly as today.

## Alternatives considered

- **Inject `UpdateCancellationContext` into `ForecastService`**, the `WorkItemService` precedent: `ForecastService` is
  already at 7 constructor parameters, so this trips S107. It also hides a dependency on the update pipeline inside the
  simulation, which ADR-183 chose to state explicitly for connectors.
- **A checkpoint inside a run (each simulated day)**: only needed if a single run is long. After the 5-year horizon
  (ADR-251) a run is at most 1,827 days. Kept as the fallback if the production-data check misses KPI-2.
- **Hard abort**: cancellation stays cooperative.

## Consequences

**Positive**
- Stop on a forecast reaches `Cancelled` within one run per worker. Features keep their dates, in Lighthouse and in the
  tracker; write-back other work staged in the round still flushes.
- One signature changes, at a port with one production caller.

**Negative / accepted**
- Test doubles of `IForecastService` change (grep the short and the fully-qualified name in the test projects).

## Enforcement

- Instrumented draw stream: after the cancel is seen, runs begun ≤ runs at cancel + one in flight per worker.
- Persisted histogram rows and `CreationTime` unchanged; no forecast write-back staged; `RefreshLog` has
  `Cancelled = true`, `Success = false`.
- ArchUnitNET: `Lighthouse.Backend.Models` does not depend on `Services.Implementation.BackgroundServices.Update`.
