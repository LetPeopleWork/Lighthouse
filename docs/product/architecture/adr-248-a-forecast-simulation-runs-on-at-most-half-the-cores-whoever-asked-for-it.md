# ADR-248: Each forecast simulation runs on at most half the cores, whoever asked for it

**Status**: Accepted (maintainer, 2026-10-18, DESIGN; recommendation taken; "per simulation" confirmed by NMD-14 at the
review gate).
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

1. **Per simulation**, `Parallel.For` gets `ParallelOptions.MaxDegreeOfParallelism = max(1, ⌊cores ÷ 2⌋)`. On a 1- or
   2-core host that is 1; on 4 cores it is 2; on 32 it is 16. "Whoever asked for it" means every caller of the
   simulation is bounded: background, manual and multi-Team. It does not mean the callers share one bound.
2. The bound is carried by `ForecastSimulationLimits`, the singleton `ForecastService` already receives. Its `Default`
   derives the bound from `Environment.ProcessorCount`, which .NET reads from the container's CPU quota and cpuset, and
   which `DOTNET_PROCESSOR_COUNT` overrides. The core count is an input, so tests pin it. Existing fixtures that
   construct their own limits keep compiling.
3. `Task.Run` + `Parallel.For` stay. With a bound of N, at most N pool threads are busy per simulation, and the pool's
   minimum is the core count, so about half of it is free for requests at once.
4. The startup banner states the bound ("Forecasts use up to N of M cores"), so an operator can see what the host
   reported.

## Alternatives considered

- **`ProcessorCount − 1`**: leaves one core for pages on a 4-core host, the incident's shape.
- **Dedicated threads or a below-normal-priority scheduler**: better isolation, more code. Kept as the fallback if the
  slice's production-data check still shows pages hanging with the bound in place.
- **One gate shared by every simulation in the process**: a manual forecast would then wait behind a long background
  run. Rejected (NMD-14).
- **A configurable setting**: out of scope. `DOTNET_PROCESSOR_COUNT` already moves the bound without one.

## Consequences

**Positive**
- Every When/Start simulation is bounded in one place, background and HTTP alike.
- The answer does not change: runs are added up independently of which worker ran them.

**Negative / accepted**
- The bound is **per simulation, not per process**. A manual forecast during a background one gets its own half, so
  for its few seconds the two together can use every core (NMD-14).
- A forecast on an idle many-core host takes up to twice as long as today. Slice 04's shorter horizon is expected to
  more than make that back (KPI-3).

## Enforcement

- Unit test with an instrumented draw stream: concurrent runs within one simulation never exceed the bound, for
  injected core counts 1, 2, 4 and 32. Seeded percentiles are identical bounded and unbounded.
- Acceptance: a read completes while a forecast is held open (no wall clock asserted).
