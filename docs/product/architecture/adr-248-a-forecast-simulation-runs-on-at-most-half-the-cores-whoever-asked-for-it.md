# ADR-248: A forecast simulation runs on at most half the cores, whoever asked for it

**Status**: Accepted (maintainer, 2026-10-18, DESIGN; recommendation taken).
**Feature**: `epic-6311-responsive-during-updates` (Bug #6308, slice 01)
**Relies on**: [ADR-154](./adr-154-addressable-draw-streams-for-the-feature-forecast.md) (draws addressed by run, Team and
day, so how runs are split across workers leaves no trace in the answer).

## Context

While one Portfolio forecast ran, every page of a 4-core Docker instance hung. The completion and start simulation
runs its 10,000 runs in `Parallel.For` with no `ParallelOptions` (`ForecastService.RunMonteCarloSimulation`), inside a
`Task.Run`. It therefore takes every core and as many pool threads as the pool will give it. Background Portfolio
forecasts and the manual forecast a Team page asks for (`When`) both go through that method. Reality checks and
backtests use `HowMany`, a separate single-threaded loop that already uses one core.

## Decision

1. The simulation passes `ParallelOptions.MaxDegreeOfParallelism = max(1, ⌊cores ÷ 2⌋)` to `Parallel.For`. On a 1- or
   2-core host that is 1; on 4 cores it is 2; on 32 it is 16.
2. The bound is carried by `ForecastSimulationLimits`, the singleton `ForecastService` already receives. Its `Default`
   derives the bound from `Environment.ProcessorCount`, which .NET reads from the container's CPU quota and cpuset.
   The core count is an input, so tests pin it. Existing fixtures that construct their own limits keep compiling.
3. `Task.Run` + `Parallel.For` stay. With a bound of N, at most N pool threads are busy, and the pool's minimum is the
   core count, so about half of it is free for requests at once.
4. The startup banner states the bound ("Forecasts use up to N of M cores"), so an operator can see what the host
   reported.

## Alternatives considered

- **`ProcessorCount − 1`**: leaves one core for pages on a 4-core host, the incident's shape.
- **Dedicated threads or a below-normal-priority scheduler**: better isolation, more code. Kept as the fallback if the
  slice's production-data check still shows pages hanging with the bound in place.
- **A process-wide gate across simulations**: a manual forecast during a background one can briefly use twice the
  bound. Manual forecasts cover one Team and are short, so the gate is not worth its cost now.
- **A configurable setting**: out of scope; no requirement for it.

## Consequences

**Positive**
- Every When/Start simulation is bounded in one place, background and HTTP alike.
- The answer does not change: runs are added up independently of which worker ran them.

**Negative / accepted**
- A forecast on an idle many-core host takes up to twice as long as today. Slice 04's shorter horizon is expected to
  more than make that back (KPI-3).
- The bound is per simulation, not per process.

## Enforcement

- Unit test with an instrumented draw stream: concurrent runs never exceed the bound for injected core counts 1, 2, 4
  and 32. Seeded percentiles are identical bounded and unbounded.
- Acceptance: a read completes while a forecast is held open (no wall clock asserted).
