# ADR-257: A forecast round runs on its own threads, off the request thread pool

**Status**: Proposed (DESIGN, 2026-10-23, PROPOSE mode under the maintainer's "continue autonomously").
**Feature**: `epic-6400-fast-with-many-features` (Story #6404, slice 01)
**Relies on**: [ADR-248](./adr-248-a-forecast-simulation-runs-on-at-most-half-the-cores-whoever-asked-for-it.md) (each
simulation runs on at most half the cores; this ADR takes up the fallback ADR-248 named: "dedicated threads … kept as
the fallback if the slice's production-data check still shows pages hanging with the bound in place"),
[ADR-249](./adr-249-a-cancelled-forecast-stops-between-runs-on-a-token-its-updater-hands-down.md) (cancellation token),
[ADR-154](./adr-154-addressable-draw-streams-for-the-feature-forecast.md) (how runs are split across workers leaves no
trace in the answer).

## Context

On v26.10.19.10, which carries ADR-248's half-the-cores bound, one reporting instance logged nine Kestrel heartbeat
warnings of 1.4–49.3 s, all during forecast rounds of 264–483 s over 8 Teams. A forecast round today holds shared
thread-pool threads for its whole length:

- the update queue's loop runs the round inline on a pool thread, including the **synchronous** gather (every Feature of
  the involved Teams with all forecast histograms) and the save that rewrites them;
- `ForecastService.Simulate` calls `Task.Run`, which always schedules on the shared pool, and `Parallel.For` inside it
  uses up to ⌊cores ÷ 2⌋ pool workers;
- every page request that arrives meanwhile blocks one more pool thread in its own synchronous read.

The pool's minimum is the core count, and it grows slowly once starved, so a round plus a few heavy requests starve
everything else. K3 (no heartbeat warning through a round, pages within 1.2 × idle) is an exit criterion of the Epic.

## Decision

1. **A singleton `ForecastLane`** owns `ForecastSimulationLimits.MostRunsAtOnce` dedicated, named, background threads
   (`max(1, ⌊cores ÷ 2⌋)`) and exposes them as a `TaskScheduler`. It runs only background forecast rounds.
2. **One hop, at the edge of the round.** `ForecastUpdater.Update` runs its whole body (Portfolio read, gather,
   fingerprint, simulate, save, confirm, stage write-back, publish, record) as one task on the lane:
   `Task.Factory.StartNew(body, CancellationToken.None, TaskCreationOptions.DenyChildAttach, lane.Scheduler).Unwrap()`,
   and awaits it. The awaits inside the body resume on `TaskScheduler.Current`, which is the lane, so the round's own
   frames stay on lane threads. Round code therefore must not use `ConfigureAwait(false)`; doing so would send the
   rest of that method to the pool. `ForecastUpdater` resolves the lane from the scope's service provider, as it
   already resolves its other collaborators, so its constructor stays at 7.
3. **What stays on the pool.** The update queue's loop (`UpdateQueueService`, the dequeue-and-dispatch loop) keeps
   running on the pool; while a round runs it is suspended in an `await`, holding no thread. EF Core and the database
   drivers complete their own I/O continuations on the pool internally (they use `ConfigureAwait(false)`); those are
   short and are accepted. The claim is therefore "a round's own frames run on the lane", not "a round never touches a
   pool thread".
4. **The simulation follows whoever runs it.** `ForecastService.Simulate` stops calling `Task.Run`. It starts the
   simulation with `Task.Factory.StartNew(…, cancellationToken, TaskCreationOptions.DenyChildAttach,
   TaskScheduler.Current)` and awaits it, and sets `ParallelOptions.TaskScheduler = TaskScheduler.Current`, with
   `MaxDegreeOfParallelism` unchanged. On the lane, the current scheduler is the lane. For a manual forecast asked from
   a page (`When`), it is the shared pool, and the request thread is released while the simulation runs, exactly as
   today, so a manual forecast never queues behind a background round (ADR-248's NMD-14 holds).
5. **Lifecycle.** The lane has **one creation point**: the composition root constructs it once, during service
   registration, from `ForecastSimulationLimits.Default` (the instance the forecasts already use), and registers it as
   a singleton through a factory that returns that instance, so the container still owns and disposes it (an
   instance handed to the container directly would never be disposed). The startup banner reads that same instance,
   so it states the lane's real size before any round runs. The lane lives for the process. On shutdown the update queue drains
   first (its hosted service stops before the container disposes singletons); an in-flight round is cancelled by its
   token before its save (ADR-249). The lane is disposed after the hosted services stop: it completes its queue, joins
   its background threads with a bounded timeout, and logs any thread that did not finish. Being background threads,
   they never hold the process open.
6. **Nothing else moves.** The queue stays one sequential reader; holds, Stop and Cancel all, the per-update
   cancellation token and the write-back round are unchanged. Domain-event handlers keep opening their own scope.
7. **The startup banner reads the lane**: "Forecast rounds run on N dedicated threads; each forecast uses up to N of M
   cores", with "on 1 dedicated thread" (singular) when the lane has one thread, as it does on one or two cores.

## Alternatives considered

- **Raise the pool's minimum thread count** (`System.Threading.ThreadPool.MinThreads`): one line, and it hides pool
  starvation from any cause, but the round still shares the pool and the CPU with pages, a global knob changes every
  workload, and each extra pool thread costs about 1 MB of reserved stack plus more context switching when they all
  run. Kept as the **control experiments** of Task 01a (C: raised minimum during a round; C0: raised minimum with page
  traffic only), which tell pool starvation apart from GC or CPU, not as the fix.
- **A forecast worker in its own process or container**: the strongest isolation (its own GC heap), but a new
  deployable, inter-process calls and a second database client, and it breaks the single-binary standalone and Docker
  installs. Rejected for cost.
- **A separate hosted service with its own queue for forecasts**: takes forecasts out of the update queue and so out of
  Epic 6311's holds, status rows, Stop and Cancel all. Rejected.
- **`ConcurrentExclusiveSchedulerPair` or `LongRunning` tasks**: the first limits concurrency but still runs on pool
  threads; the second gives the round body one thread until its first `await`, while `Parallel.For` workers stay on the
  pool. Neither isolates.
- **Below-normal thread priority**: not honoured in a Linux container without extra privileges. Not relied on.
- **Async request paths** (DIVERGE Dissent B): the request side of starvation; a separate Epic if S_q > 0.3.

## Consequences

**Positive**
- A round's own frames (gather, simulation workers, save) hold no shared pool thread; page requests compete with each
  other and with short driver continuations only.
- Forecast outputs cannot change: the same simulation runs the same draws; only which threads run it moves.
- One place decides where rounds run, and it is visible in the banner.

**Negative / accepted**
- CPU is still shared: the lane's threads use up to half the cores, as today.
- Whether the Kestrel heartbeat itself runs on the pool or on its own thread is unverified (OQ-1). A stop-the-world GC
  pause or a CPU-throttled container stalls every thread, the lane's included. Task 01a classifies the stalls with fixed
  thresholds; if they are GC or throttling, this ADR helps less, so delivery stops after the measurement and the
  readings go to the maintainer before this ADR is built (maintainer decision of 2026-10-24, replacing the earlier
  plan to build it anyway and move slices 05 and 03 ahead of 02).
- A task that blocks on another lane task from a lane thread could deadlock once all lane threads are busy.
  `Parallel.For` runs its first share on the calling thread, so the round always progresses; no other code may block
  on lane work.
- A stray `ConfigureAwait(false)` in round code silently moves frames back to the pool; the probe below catches it.

## Enforcement

- Behavioural probe (CI): during a queued round on a seeded set, every **round frame** (the gather, each simulation
  worker body, the save call site, the publish) records `Thread.CurrentThread.IsThreadPoolThread == false` and the
  lane's thread name; driver-internal continuations are not probed. A manual `When` forecast's simulation workers record
  `true`, and its request thread is released while it runs.
- `Simulate` scheduler test: started under a custom test scheduler, the simulation and every `Parallel.For` body run
  on that scheduler; started under the default scheduler, they run on the pool; the returned task is awaited (no
  fire-and-forget).
- Lifecycle test: stopping the host during a lane round cancels the round, leaves the previous forecasts, and disposes
  the lane with every thread joined within the timeout.
- Structural (ArchUnitNET, plus a source scan of `ForecastService.cs` and `ForecastUpdater.cs`, because a call made
  inside an `async` body can vanish from the Release build's IL and an IL-only rule would then pass vacuously):
  `ForecastService` does not call `Task.Run`, does not call `Task.Factory.StartNew` without an explicit scheduler
  argument, does not touch `ThreadPool.*` (`QueueUserWorkItem`, `UnsafeQueueUserWorkItem`, `RegisterWaitForSingleObject`
  and the rest), and calls `Parallel.*` only with a `ParallelOptions` whose `TaskScheduler` is set; only
  `ForecastUpdater` and the composition root depend on `ForecastLane`; no `ConfigureAwait(false)` in
  `ForecastUpdater`, `ForecastService` or `ForecastLane`.
- Banner: the line reads the registered lane's thread count, singular for one thread, plural otherwise.
- Identity: seeded percentiles, histogram rows and the Story 6354 fingerprint are identical before and after, on
  SQLite and PostgreSQL (C12).
- Cancel all and per-row Stop end a lane round within seconds, leaving the previous forecasts (ADR-249).
