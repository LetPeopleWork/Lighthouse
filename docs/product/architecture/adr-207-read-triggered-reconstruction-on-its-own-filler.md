# ADR-207: A series read asks for the days it is missing; a filler of its own does the work

**Status**: Accepted (DESIGN, 2026-09-22; interaction mode PROPOSE)
**Date**: 2026-09-22
**Amended**: 2026-09-22 (DEVOPS: the maintenance gate); 2026-09-24 (the fill ships behind an opt-in switch; the walk order corrected to what was built) — both at the end
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

*(Amended 2026-09-24: a read that has found days to ask for now makes exactly one more query - it
reads the instance-wide switch. A read that finds nothing missing still makes none. See the amendment
at the end.)*

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

*(Corrected 2026-09-24: the code never walked back from the ceiling. A pass works **oldest first**,
the cap counts days it **worked out**, not days it wrote, and both the ceiling and the floor are
applied inside the pass, not on the read. The table row above and this paragraph describe a walk that
was never built; the amendment at the end describes the one that was, and why.)*

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


## Amendment (DEVOPS, 2026-09-22) — `HasActiveWork()` was a safety property, not a cost; the gate is taught directly

**Status**: Accepted. The Decision — the filler is its own component and does not join
`IUpdateQueueService` — is **unchanged**, and the three other reasons for it still hold. This amendment
corrects one reason in the rejection above and adds the obligation that correction creates.

### What the rejection got wrong

The rejection of `IUpdateQueueService` lists, among "two further costs":

> *"an admitted key keeps `HasActiveWork()` true, which is what holds database maintenance off."*

Factually correct, and wrongly classified. Being visible to `HasActiveWork()` is not a **cost** of joining
the queue — it is a **safety property** the queue happens to carry, and it is the only thing that stops
`DatabaseMaintenanceGate` granting `CreateBackup`, `RestoreBackup` or `ClearDatabase` while background work
is writing. Declining the queue discarded the property along with the mechanism, and nothing replaced it.

### The consequence this ADR shipped without noticing

A pass runs entirely outside `IUpdateStatusStore`, so `HasActiveWork()` is `false` throughout and the gate
grants itself:

- **`RestoreBackup` mid-pass** — the filler holds a scoped `DbContext` writing snapshot rows while the
  database is replaced underneath it. Unambiguously bad.
- **`ClearDatabase` mid-pass** — the same shape.
- **`CreateBackup` mid-pass** — least severe: fill-if-absent (ADR-208) makes each day an independent row,
  so a captured window is partially filled but never torn. Still a backup taken at a moment the operator
  did not choose.

This class of bug is already documented in the codebase. `IUpdateStatusStore.CancelIfStillWaiting`'s XML
doc warns that marking running work cancelled *"takes it out of `HasActiveWork` while it is still talking
to a tracker, and everything that waits for this instance to go idle stops waiting — including the gate
that holds database maintenance off."* Work that runs while invisible to `HasActiveWork()` is a known
hazard here, and this ADR created a second instance of it.

### Decision (additive)

**The gate learns about the filler directly, and the filler defers to the gate. Both directions.** One
direction alone leaves the check-then-act race that same XML doc warns about.

1. `DatabaseMaintenanceGate` consults a pass-in-flight predicate owned by the filler, alongside
   `statusStore.HasActiveWork()`. **No `UpdateKey`, no `UpdateType`, no task row, and no change to
   `IUpdateStatusStore`** — so none of the four reasons for staying out of the queue is weakened. The gate
   is the component that needs to know; it is the component taught.
2. The filler checks for an active maintenance operation before each day and abandons the pass if it finds
   one.

Presence and admission are **not** separable through the existing store: every `IUpdateStatusStore` method
is keyed on `UpdateKey`, which requires an `UpdateType` member — exactly the user-visible cancellable task
row rejected above. "Just register presence" is unavailable without reopening that rejection, which is why
the gate gains a second signal rather than the filler gaining a key.

### Why abandoning is free, and why that is specific to this component

The filler is the only background work in the system that is safely abandonable at any instant: each day is
an independent fill-if-absent write, already-written days stay, and the walk is resumable by construction
because a read will happen again. No other queue participant has that property — which is a further reason
the queue's heavier machinery was the wrong fit. Abandoning costs at most one wasted partial pass.

### Consequence for the wall-clock budget

This ADR left the per-pass budget's value open. It now has a principled bound rather than an arbitrary one:
**the budget is the longest a `RestoreBackup` may be held waiting.** An operator who clicks Restore should
not wait on a history backfill, which makes the budget a small number of seconds measured against operator
patience rather than a throughput knob.

Full reasoning and the gates it adds: `docs/feature/story-6053-reconstruct-over-time-history/feature-delta.md`
→ "Wave: DEVOPS / [REF] Production readiness".


## Amendment (2026-09-24) — the fill ships behind an instance-wide switch, off by default; and the walk as it was actually built

**Status**: Accepted (DESIGN amendment, interaction mode PROPOSE). The Decision above is unchanged in
shape: a read asks, a filler of its own answers, the unit is the owner. What changes is that on an
instance where nobody has switched the fill on, nothing asks and nothing is answered. The second half
of this amendment corrects §5, which described a walk the code never did.

### Why there is a switch at all

The maintainer decided, before the story shipped, that the fill reaches an instance only when a System
Admin chooses it. Three reasons, all about the fill rather than about the switch:

- **It writes rows it never takes back.** A filled day cannot be told apart from a recorded one, so
  nothing could single the filled days out for removal later. Adopting it is one-way for the data.
- **It works a past day out against today's configuration.** An instance whose state mappings,
  cycle-time definition or blocked rules changed recently gets a past it may not recognise, and
  fidelity across such a change is still unmeasured.
- **It costs background work when a chart opens**, measured so far on one instance only.

So the switch is one instance-wide row in the existing optional-features list (Settings →
Configuration → Behaviour Settings): seeded **off** on fresh and upgraded instances alike, flagged
**Preview**, not premium, changed only by a System Admin through the guard that list already has.
Turning it off keeps every day already filled; nothing is purged. The route to "always on" is two
planned follow-ups outside this story: switch the default to on (#6083), then remove the switch (#6084).

**The switch governs the fill and nothing else.** These ship on for everyone and must not be gated
later by accident: the recorder refusing to write an all-zero percentile row (ADR-208 §3); the shared
computation that judges a past day's process limits as of that day (ADR-208 §2); the shared
day-writers themselves; the demo synthesiser's backdated rows (ADR-109); the memo forgetting an owner
when its refresh events arrive; and the database maintenance gate.

### Where it gates — two checks of one question

A new, narrow port answers one question — *is filling in past days switched on for this instance?* —
by reading the optional-feature row by its key each time it is asked. A missing row reads as **off**,
so an instance between an upgrade landing and its seeder running behaves as it did before the story.
It is scoped, like the repository beneath it, and holds nothing between calls.

1. **The reconciler, after it has found days to ask for and before it asks.** This is the one place a
   fill enters: `OverTimeGapReconciler` is the only caller of the filler's `AskFor`, reached from both
   series decorators, so a check there covers the UI and Lighthouse-Clients reads alike. Nothing else
   starts a pass — no refresh-event handler, no startup job, not the demo loader (which writes its own
   rows directly and never asks the filler), and not the maintenance gate, which only reads whether a
   pass is running.
2. **The filler, at the start of each pass, in the pass's own scope, before it loads the owner.** An ask
   queued while the switch was on must not start a fill after it went off. The queue holds up to 256
   owners and each pass may run for ten seconds, so "let the queue drain" could mean a fill carrying on
   for many minutes after an admin said stop. The filler is a singleton, so it resolves the switch from
   the pass's scope rather than taking it in its constructor — a switch captured there would be read
   once, at start-up, which is exactly the restart the switch must not need.

One entry point, gated, is pinned by a structural test in the style the story already uses: exactly one
production file calls the filler's `AskFor`, and it is the reconciler; the switch's key constant is
referenced only by the key list, the seeder and the switch's own implementation, so no caller can read
the row directly and get its polarity wrong.

### The read-path cost, and what "no restart" requires (amends §4)

§4 said no extra query runs on the read path. Now:

- a read that finds **nothing missing** — a dense window, or every missing day already known to be
  unfillable — makes **no** extra query, as before;
- a read with **no start date** asks for nothing and makes no extra query, as before;
- a read that **has found days to ask for** makes **exactly one** extra query: the switch, looked up by
  primary key in a table of a handful of rows.

With the switch off and gaps present, that one lookup is paid on every such read, indefinitely, because
nothing ever fills the gaps. That is accepted: a primary-key read of one small row against a budget of
50 ms is noise, and it lands only on requests that already carry a gap.

Two alternatives were rejected. **Caching the switch, invalidated when an admin changes it**, saves that
one lookup but breaks "no restart" on every replica other than the one that served the change, unless
the cache also expires on a timer — at which point the promise becomes "no restart, after a delay", and
the settings write gains a side effect it otherwise does not need. **Reading the switch before the
predicate** puts a query on every read, including the dense steady state the design converges to, which
is the one case §6 promises costs nothing. Letting the memo remember "off" is the first alternative
under another name.

"No restart" therefore requires three things, and each is a way to break it: the switch is read per
use, never held in a field, an options snapshot or a start-up value; the port is scoped, not a
singleton; and the filler reaches it through the pass's scope, as above.

### When the switch goes off while a fill is running or waiting

- **A pass already running finishes.** It is bounded by the 90 days it may work out and its ten-second
  budget, and the days it writes stay, as every filled day does. Stopping it at the next day instead
  would cost a lookup per day to bring "off" forward by at most ten seconds, and would add a third
  reason to leave the walk with its own way to go wrong. If that is ever wanted, it must leave the walk
  with `break`, never `return`, so the cache is still invalidated for the days already written, and it
  must not record the days it did not reach as worked out.
- **A pass not yet started is dropped** by the check at pass start: the owner is not loaded, nothing is
  written, nothing is computed and so no cache needs invalidating, the memo is not touched, and the
  owner's in-flight key is released. Emptying the queue from the settings write was rejected: it
  reaches only the replica that served the write, and it would couple the settings endpoint to the
  filler.
- **The memo needs nothing across an off→on flip.** While off, no pass records anything. The two
  refresh-event handlers keep clearing an owner's notes while the switch is off, and must stay ungated —
  otherwise the memo would still be refusing days that new items have since made computable when the
  switch comes back on. Everything it held before stays true. It remains an optimisation only: losing
  it still costs one wasted pass, and nothing may be asserted from it. The two silent traps from DELIVER
  apply unchanged: the filler asks the gate `IsMaintenanceOperationActive`, never `IsBlocked` (which
  includes the filler's own pass, so it would always stand down); and a day not attempted — abandoned by
  the budget, or now not reached because an ask was dropped — never enters the memo as worked out.

### The maintenance gate does not change

The gate asks *is something writing right now?*, not *is filling allowed?*, and it must not consult
the switch. A pass that is finishing after the switch went off is still writing, and a gate that took
"off" to mean "nothing is writing" would hand out `RestoreBackup` in the middle of that pass — the exact
hazard the DEVOPS amendment above closed. The filler keeps standing down per day for an active
maintenance operation, finishing tail included. With the switch off and no pass finishing, the filler is
never in flight, so the gate is never held by it. One refinement for the crafter: settle the pass-start
check before the pass counts as in flight, so that a dropped ask never shows an operator "the chart is
filling in days it was missing" on an instance where filling is off.

### The read contract does not change

No route, request, response or field changes. With the switch off, both series endpoints return
exactly what they returned before this story, and on that instance a read once again never triggers a
recomputation — ADR-108's original slice-03b property holds there. With it on, ADR-108's amendment
("read-only **in the response**") applies. ADR-108 needs no further amendment, and the frontend never
learns the mode: one empty-state sentence is true in both positions, because filled days live in the
same store as recorded ones.

### Correction: the walk as it was built (supersedes the walk described in §5)

§5 says a pass "walks back from the window's ceiling and stops after 90 days it actually wrote", so a
year-wide picker would get the most recent 90 days first. The code never did that. As built:

- **The read hands over every missing day of the requested window, oldest first**, taken from the rows
  the read already holds minus what the memo rules out. Its only bound is a guard of about ten years,
  so a hand-typed range spanning centuries cannot queue centuries of days. The read loads no owner, so
  it applies **no ceiling**.
- **The pass works through those days oldest first.** Days before the owner's earliest finished stored
  item (the floor) or after its last observed day (the ceiling) are stepped over and **not counted**.
  The cap is **90 days worked out** per pass — attempted, whether or not a row resulted — plus the
  ten-second budget and the per-day maintenance stand-down.

Why the cap moved into the pass and counts days worked out: when the read chose the first 90 missing
days itself, a period reaching back past the owner's history handed over only days before the floor —
which only a pass can discover — and the first load wrote nothing at all, even for the part of the
period the history covers. Why oldest first stays: for an owner nobody syncs any more, the newest days
of a window are the ones past its last observation, refused again on every visit; a walk that starts
there begins every visit with days it will refuse, rather than with the older days it can fill.

**Accepted cost of the order:** on a range much wider than 90 fillable days, the oldest stretch fills
first and the most recent days arrive on later visits. Accepted rather than reversed, for the reason
just given.

### Deferred to the default flip (#6083)

The seeder never overwrites an on/off value already stored, which is what keeps an admin's choice
across upgrades. It also means that when the default later flips to on (#6083), an instance that
upgraded through this release and never touched the switch still holds the **seeded** off, and nothing
can tell that apart from an admin who deliberately switched it off. Flipping the default would then
reach fresh instances only. Recording, at the moment an admin changes the switch, that a person chose
(one extra key/value row written by a dedicated applier for this key, no migration) is cheap now and
impossible to reconstruct later. The maintainer chose not to record it now: #6083 decides, when it
flips the default, whether to reach fresh instances only or every instance that is off, deliberate
offs included.

### Earned Trust — changed and added probes

| Assumption | Probe |
|---|---|
| Off means nothing is even queued, not merely that nothing is written | Switch off; open a chart over a period the stored items can fill; switch on **without** opening it again; drain the filler: no row is written. Open it again and drain: rows are written. The second half proves the arrangement can fill, so the first half cannot pass for free, and the sequence also proves the switch needs no restart. A gate that existed only at pass start would fail the first half. |
| An ask queued before the switch went off does not start a fill after it | Switch on; hold one owner's pass in flight; open a second owner's chart so its ask queues; switch off; release and drain: the first owner's pass completes its walk (a running pass finishes), the second owner has no rows. |
| A missing row reads as off | Delete the row and open a fillable period: nothing is queued, nothing written. |
| The read path makes exactly the queries it should | Count database commands for a gap-free read and for a gap-finding read: equal to before for the first, exactly one more for the second, in either switch position. This replaces the "no query it did not issue before" probe above, which the suite does not yet contain. |
| The budget stops the walk | As above, restated: a pass works out at most 90 days, not writes at most 90. |
| Only one place starts a fill | The structural test described under "Where it gates". |
