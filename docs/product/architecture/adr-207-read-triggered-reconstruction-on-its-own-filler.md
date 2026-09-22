# ADR-207: A series read asks for the days it is missing; a filler of its own does the work

**Status**: Accepted (DESIGN, 2026-09-22; interaction mode PROPOSE)
**Date**: 2026-09-22
**Feature**: story-6053-reconstruct-over-time-history (ADO User Story #6053)
**Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

---

## Context

Epic 5427's recorder is forward-only: a day exists in `PercentilesOverTimeSnapshot` /
`ProcessBehaviorSnapshot` only if the instance was up and refreshing on that day. The measured dev
instance holds **4 recorded days against roughly 365 reconstructible ones**, every recorded day a
weekend or a Monday. On a standalone install that is the normal case.

DISCUSS locked the shape (`docs/feature/story-6053-reconstruct-over-time-history/feature-delta.md`):
a missing day is **recomputed** from stored work items with the recorder's own service call, window
shifted (D1); the **trigger is the series read detecting a gap in its requested range** (D2); the
**read never blocks** (D3); the walk is bounded (D4) and floored at the owner's `UpdateTime` (D5).

Three facts about the shipped code decide the mechanism, all read on 2026-09-22.

*The update queue is one lane today.* `UpdateQueueService` holds a single
`Channel<Func<Task>>` drained by a single reader task that awaits each item to completion.
[ADR-195](./adr-195-update-queue-is-three-lanes-one-channel-each.md) designed three lanes to fix the
starvation that causes — its Context records a field report of **3h38m with zero team updates** behind
one Portfolio refresh that would not finish — but story-5877 was **shipped and reverted** (see the
`### Relationship to story-5877-update-queue-lanes` note at the end of `brief.md`: "#5877 is reverted
and shelved"). ADR-195/196/197 describe a design that is not in the code. The lane is single.

*The queue's surface is an operator surface.* `UpdateType` reaches the browser through
`UpdateController`'s task list, where `ActivitySection.tsx` maps it through three exhaustive tables
(`WORK_VERBS`, `WORK_NOUNS`, `WORK_SUBJECT_TERMS`, each `satisfies Record<UpdateTaskType, …>`) and
renders a row with a **Stop** button. `UpdateEntityKinds.Of` is a total switch with a `CS8524`
suppression so a new member cannot land nowhere. A new `UpdateType` is therefore a user-visible,
cancellable, terminology-rendered piece of work, not a private detail.

*The natural key is already unique.* [ADR-106](./adr-106-percentiles-over-time-snapshot-table-shape.md)
makes `(OwnerId, OwnerType, MetricType, Horizon, RecordedAt)` a unique index, and
`(OwnerId, OwnerType, MetricType, RecordedAt)` likewise on the PBC table. Two writers racing on one
day cannot produce two rows; the loser gets a constraint violation.

## Decision

**The read asks. A filler of its own answers. The unit of the ask is the owner and a window, never a
day and never a metric family.**

### 1. The ask lives in a decorator over each series query port

`IPercentilesOverTimeSeriesQuery` and `IProcessBehaviorSeriesQuery` keep their read-only signatures and
their existing implementations unchanged. Two new decorating implementations wrap them, are registered
as the interface in the composition root, and do exactly two things: delegate the read, then hand the
rows and the requested window to the reconciler before returning them untouched.

The controller is not the seam, because gap detection needs the series, the window, the owner and the
floor together — putting that in `TeamMetricsController` and `PortfolioMetricsController` is four call
sites and a controller that computes. The inner query is not the seam either, because a component
called `…SeriesQuery` that also schedules work is a component whose name is a lie.

**The read path may only ask.** The reconciler's port exposes one method that returns nothing and
carries no repository. It cannot write a snapshot row; only the filler can. The bug class "a GET wrote
to the database on the request thread" is therefore not representable from the read side, rather than
being a thing the tests happen not to catch.

### 2. The carrier is a filler of this feature's own, not the update queue

`IOverTimeHistoryFiller` — a hosted background service owning one bounded `Channel<ReconstructionRequest>`,
one reader task, a `ConcurrentDictionary` of keys already queued or running, an `IServiceScopeFactory`
for a per-pass DI scope, and a `DrainAsync` on shutdown. That is the `UpdateQueueService` *pattern*,
reused; it is not `UpdateQueueService`, for the reasons in Alternatives.

Per-key dedupe (`TryAdd` on enqueue, removed as the pass ends) means two dashboard loads racing on one
owner produce one pass. A full channel drops the newest request rather than blocking the reader — a
dropped request costs nothing, because the next read re-asks.

### 3. The unit of work is `(owner, window)`, and the pass covers every family

One request per owner per read. The pass walks **all** percentile families (CT-30/60/90 + WIA) and all
PBC families for that scope. A reader on the WIA tab pays for the cycle-time tabs too, deliberately:
US-02's whole point is that no tab may contradict another about how much history exists, and a
family-keyed unit would make the tabs fill at different times and at different depths. It also makes
US-02 AC5 — "switching tabs does not trigger a second reconstruction" — a structural property rather
than a behaviour to test for, because both tabs produce the same key.

### 4. Gap detection on the read path is a predicate, not a computation

The decorator has the rows it is about to return and the window it was asked for. It asks one
question: *is there a day in the clamped window with no row for the family just read?* That is a scan
over a list of at most a few hundred items already in memory. **No extra query runs on the read
path** — in particular the data floor is not resolved there, because resolving it means touching
`WorkItems`, and the KPI budget for a gap-discovering request is 50 ms over today's p95.

The precise per-family gap set is computed by the pass itself, in its own scope, where it belongs.

### 5. The clamp, and D4's cap

| Bound | Value | Why |
|---|---|---|
| Ceiling | `min(requested window end, DateOnly(owner.UpdateTime))` | D5's trailing-gap floor. `UpdateTime` is on the loaded owner, so this costs nothing on the read path. |
| Floor | `max(earliest stored item day, today − DoneItemsCutoffDays)` | D4. Resolved **in the pass**, not on the read. |
| Budget | at most **90 newly written days per pass**, and a wall-clock budget per pass | SPIKE-01: ~1.6 s for 90 days of percentiles and ~5–6 s including PBC on 621 items — with an explicit inability to extrapolate to tens of thousands. |

**D4's cap is locked at 90 days *per pass*, not 90 days of reach.** A pass walks back from the window's
ceiling and stops after 90 days it actually wrote, or when the budget expires, whichever comes first;
the floor stops it in any case. A user who widens the picker to a year gets the most recent 90 days on
the first load and the next stretch on the load after that, until the floor. This is the honest answer
to SPIKE-01's stated inability to bound cost on a large instance: **the walk is resumable by
construction**, because the thing that triggers it is a read that will happen again. A fixed 90-day
*reach* would have capped depth forever for a reason that is about cost, not about truth.

90 is the number because it is the portfolio dashboard's default window, it exceeds the team default
of 30, it covers the review period both journeys describe, and it is what the story's KPI asks for
("from 4 days to ≥ 90 days"). It is a constant in the filler, not an `AppSettings` row — there is no
evidence yet that anyone wants to tune it, and an untuned setting is a support question nobody asked.

### 6. Convergence is a designed property, and the memo is what makes it one

Once a window is dense the predicate is false and nothing is enqueued: cost goes to zero. The failure
mode that would stop it going to zero is a **permanently unfillable day** — one before the floor, one
the absence gate refuses (D7), one whose PBC chart is not `Ready`. Re-detected on every read, such a
day would enqueue a pass forever that writes nothing forever.

So the filler keeps a **reconciliation memo**, in memory, per owner: the floor it resolved, and the
days it refused to write and why. The decorator's predicate subtracts the memo before deciding to ask.
The memo is **optimisation only and never correctness**: it is invalidated by `TeamDataRefreshed` /
`PortfolioFeaturesRefreshed` (new items can make a refused day computable), it is bounded by owners ×
window, and losing it to a restart costs exactly one wasted pass.

### 7. Idempotency and the race

Three layers, deliberately not one:

- **Enqueue** — the in-flight key set collapses concurrent asks for one owner into one pass.
- **Write** — reconstruction is **fill-if-absent**, never update-in-place. A day already carrying a
  row, recorded or reconstructed, is left exactly as it is (US-01 AC8). This is a different policy
  from the recorder's latest-write-wins upsert, and [ADR-208](./adr-208-a-past-day-is-computed-by-the-recorders-own-code.md) is where the two live.
- **Collision** — two replicas can both run a pass for one owner, because nothing coordinates them.
  The unique index is the backstop: the loser gets a `DbUpdateException` on the natural key, which the
  pass absorbs **per day** and continues. Duplicate work is wasted CPU on one window; it is never a
  duplicate row and never a wrong value, because both writers computed the same thing.

**Testing note, because it will bite**: `Microsoft.EntityFrameworkCore.InMemory` does not enforce
unique indexes, so the collision path is invisible to the unit suite and needs a SQLite-backed
integration test.

## Alternatives Considered

**Enqueue on `IUpdateQueueService`.** The default answer, and the one to beat. It would come with
cross-replica single-flight (ADR-076's per-key advisory lock), coalescing, a drain, a wall-time bound
(ADR-197) and an operator-visible row for free. **Rejected on the shipped code.** The queue is one
sequential lane — ADR-195's three-lane fix was reverted — so a 90-day multi-family pass, measured at
5–6 s on 621 items and unbounded above that, would sit in front of every Team and Portfolio refresh on
the instance. That ranks a cosmetic chart backfill ahead of the product's core function, and it makes
worse the exact starvation ADR-195 was written about. Two further costs, either of which would be
survivable alone: a new `UpdateType` is a **user-visible cancellable task row** that
`ActivitySection.tsx` must be taught a verb, a noun and a terminology key for, and whose TypeScript
union is a second hand-maintained declaration of the backend enum with no compile-time link — ship the
backend half alone and the row reads `undefined Team 'X'`; and an admitted key keeps `HasActiveWork()`
true, which is what holds database maintenance off.

**Reuse `IUpdateExecutionLock` alone**, for cross-replica single-flight, without joining the queue.
Rejected: `AcquireAsync` is keyed by `UpdateKey`, so it needs an `UpdateType` member that must then
**never** be admitted to the status store — an invariant with nothing enforcing it, bought to prevent
duplicate work that the unique index already makes harmless.

**A scheduled/periodic backfiller.** This is the shape
[ADR-107](./adr-107-percentiles-recording-handler-on-refresh-events.md) rejected as "a scheduler with
no independent-trigger justification", and that rejection stands on its own terms — see the
reconciliation in ADR-107's amendment. It is also rejected here on its own merits: it fills history
nobody may ever look at, on every owner, on a cadence that is exactly what is missing on the instances
with the worst gaps.

**Fill inline on the request thread.** SPIKE-01 measured ~1.6 s for 90 days of percentiles, which is
less catastrophic than it looked at DISCUSS time — but the 365-day full-family case is ~20–25 s on a
small fixture with no upper bound on a large one, and it lands on a dashboard load. D3 rejected it and
the measurement does not reopen it.

**`Task.Run` and forget.** No dedupe, no bound on concurrency, no graceful shutdown, and a DI scope
whose lifetime is nobody's. Rejected.

**Detect the gap with a query** (`SELECT DISTINCT RecordedAt …`) rather than over the rows already
read. Rejected: the rows are already in memory and already exactly the answer; a second query is a
round trip on the path with the 50 ms budget.

## Consequences

**Positive.** No request ever gets slower than it is today, and the added work on the read path is a
list scan and a dictionary lookup. The filler runs beside the update queue rather than inside it, so
reconstruction cannot starve a refresh and a refresh cannot starve reconstruction. Convergence to zero
cost is a property of the design rather than a hope. The unit of work makes US-02 AC5 structural.

**Negative / accepted.**

- **The chart is not complete on the first load after upgrade.** This is D3 accepted in full, and
  US-04's copy has to be true of it.
- **Multi-replica duplicates work.** Two pods serving reads for one owner can both run a pass. Bounded
  by the unique index into wasted CPU. Accepted rather than solved, because solving it costs an
  `UpdateType` member that must never be admitted.
- **One concurrent writer is added.** On SQLite the filler's `SaveChanges` can overlap the update
  queue's. WAL is on and `busy_timeout` is 10 000 ms, which ADR-195 already described as "a ceiling,
  not a guarantee". Expected is not known — see Earned Trust.
- **The memo does not survive a restart**, and does not cross replicas. Both cost one wasted pass.
- **Reconstruction is not visible in the Task Manager**, so it cannot be cancelled and an operator
  cannot see it running. Deliberate: it is not an operator task, it holds no tracker rate limit, and
  the only thing that wanted the signal is one sentence of empty-state copy — which slice 04 answers
  with a sentence true of both states instead (see the ADR-108 amendment).
- **A first read on a range entirely before the floor enqueues one pass that writes nothing**, because
  the read path deliberately does not resolve the floor. The memo absorbs every read after it.

## Earned Trust

Every dependency here can lie. Each claim below has a probe that makes it answer.

| Assumption | Probe |
|---|---|
| A request that discovers a 47-day gap is not slower than one with no gap | Timed acceptance test over both shapes against the restored dev DB, asserting the KPI's < 50 ms. A test that merely asserts a 200 passes against an inline implementation. |
| The read path issues no query it did not issue before | Count the commands on the `DbContext` across a gap-discovering request and a gap-free one, and assert equality. Prose cannot hold this; a helpful refactor resolving the floor "just once" breaks it silently. |
| Nothing on the read path can write a snapshot row | ArchUnit-style test: the reconciler port's implementations take no snapshot repository, and no type reachable from a controller action depends on one. |
| Two concurrent passes for one owner produce one pass | Two reads racing on a held-open filler; assert exactly one pass ran. |
| Two replicas racing on one day produce one row and no failed pass | Integration test on a **real SQLite file** — `InMemory` does not enforce unique indexes and will pass against a broken implementation. |
| Cost converges to zero once dense | Read the same dense window twice and assert the second read enqueues nothing. Then the sharper one: a window containing a **permanently refused** day, read twice, still enqueues nothing the second time. The first alone passes against an implementation with no memo. |
| The budget actually stops the walk | A pass against an owner with 365 reconstructible days writes at most 90 and leaves the rest; a second pass reaches further back. |
| SQLite tolerates the filler saving beside the update queue | Integration test driving concurrent saves from both against a real file with the production PRAGMAs, asserting no `SQLITE_BUSY`. Run before the design is expensive to change. |
| The filler drains on shutdown | Work in flight; `DrainAsync` returns only after it finished or the timeout elapsed, and nothing is left marked in-flight. |
| The memo never changes what gets written | Run a pass with the memo cleared and with it warm; assert the rows written are identical. A memo that is load-bearing for correctness is a cache that has become a database. |

## Cross-reference

- [ADR-106](./adr-106-percentiles-over-time-snapshot-table-shape.md) — the unique natural key this
  design uses as its concurrency backstop, and the `NoHorizon = 0` sentinel a reconstructed WIA row
  must carry for the same two mechanical reasons.
- [ADR-107](./adr-107-percentiles-recording-handler-on-refresh-events.md) — the forward recorder, which
  is unchanged in trigger and placement. Its rejection of "a separate scheduled/background recorder" is
  reconciled in its own amendment: reconstruction's trigger is independent, which is the thing that
  rejection said was missing.
- [ADR-108](./adr-108-percentiles-over-time-series-http-contract.md) — the read contract. Its slice-03b
  "never a recompute trigger" property is amended there.
- [ADR-109](./adr-109-demo-percentiles-backfill-handler.md) — the demo backfill. Its rejection of
  real-tenant backfill narrows to synthesis, amended there.
- [ADR-195](./adr-195-update-queue-is-three-lanes-one-channel-each.md) — **describes a design that was
  reverted.** Read alongside `brief.md`'s story-5877 note before believing the lane count.
- [ADR-208](./adr-208-a-past-day-is-computed-by-the-recorders-own-code.md) — what a pass actually does
  once it has a day.
