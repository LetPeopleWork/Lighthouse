# ADR-195: The update queue is three lanes, one channel and one reader each, keyed by update type

**Status**: Accepted (DESIGN, 2026-09-18; interaction mode PROPOSE)
**Date**: 2026-09-18
**Feature**: story-5877-update-queue-lanes (ADO User Story #5877, slice 01)
**Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

---

## Context

`UpdateQueueService` holds one `Channel<Func<Task>>` (`UpdateQueueService.cs:11`) drained by one
`await foreach` loop that awaits each item to completion before taking the next
(`:602-621`). Teams, Portfolios, Forecasts and both delete operations share it. A refresh that does
not return holds that loop until the process restarts.

A field report on 26.8.14.1 standalone (Tauri, macOS, SQLite, Jira Data Center) shows the
consequence: a Portfolio refresh ran 77.7 minutes before failing on a DNS error, its coalesced
follow-up immediately retook the loop, and every Team refresh from 06:11 until a 09:49 restart logged
"already queued or being processed" — 3h38m with zero team updates. After the restart the same three
teams completed in 23 s, 31 s and 26 s. Nothing was deadlocked. Everything was queued behind one
refresh that would not finish.

Four facts constrain the fix, all read from the code on 2026-09-18.

*The cross-replica execution lock is already per key.* `IUpdateExecutionLock.AcquireAsync(updateKey, …)`
is acquired inside the execution, per `UpdateKey`. Running two keys at once on one replica does not
weaken ADR-076's INV-4; the lock is what enforces it, and it is untouched here.

*Each execution already runs in its own DI scope*, and `IWriteBackCollector` is `Scoped`
(`Program.cs:1386`). Two concurrent executions get two collectors. The only shared mutable object on
the path is the `WriteBackRound` itself — see ADR-196.

*On SQLite each `DbContext` gets its own connection*, because `DatabaseConfigurator` uses the
`(provider, options)` `AddDbContext` overload and rebuilds options per context (`:23`, `:60-83`). WAL
is on and `busy_timeout` is 10 000 ms (`:71-73`), so concurrent writers serialise at the file rather
than failing. That is a ceiling, not a guarantee, and the reporting user is on SQLite.

*A queue item today carries no identity.* The reader's backstop `catch` logs
`"Error processing update task"` and names nothing (`:618`), so a failure outside an update's own
`try` is unattributable.

## Decision

**Three lanes. One `Channel` and one reader task per lane. The lane an item belongs to is a total
function of its `UpdateType`, evaluated at enqueue time.**

| Lane | `UpdateType` members |
|---|---|
| Team | `Team`, `TeamDelete` |
| Portfolio | `Features`, `PortfolioDelete` |
| Forecast | `Forecasts` |

Deletes share their entity type's lane. A delete and a refresh of the same entity type serialising is
the conservative reading, and deletes are awaited by an HTTP caller already holding a response open.

**Within a lane, work stays serial by construction** — one reader per channel, awaiting each item to
completion — rather than by a filter or a semaphore that can be got wrong. Same-type work is the work
most likely to share a connection and a rate limit, and the deployment that reported this is an
on-premise Data Center instance already close to its limits. Per-tracker concurrency stays at one per
update type.

**The lane machinery is extracted into its own type, `UpdateLanes`**, owning the channels, the reader
tasks, the write path and the drain. `UpdateQueueService` is 649 lines and the lane concern is
cohesive; extracting it also makes the drain unit-testable without standing up the whole service.

**The queue item gains its key.** The channel carries a small record of `(UpdateKey, Func<Task>)`
instead of a bare delegate, so the reader's backstop names what failed and so the lane is derivable
from the item itself.

**Drain covers every lane.** `DrainAsync` completes all three writers and awaits all three reader
tasks together under the caller's shutdown token, which is bounded by the existing 30-second
`Shutdown:TimeoutSeconds`.

**The lane holder is resolved per lane on the read path.** `UpdateController` currently calls
`admitted.FirstOrDefault(work => work.Status == InProgress)` the lane holder (`:61-62`) — correct only
while exactly one thing can ever run. It becomes the running row in the asking row's own lane, and
absent when that lane is free. The response shape is unchanged; only the meaning of `WaitingBehind`
is corrected.

**Hold bookkeeping is serialised.** `HoldUntilQueuedWorkClears` / `ReleaseClearedHolds` were written
against a single reader and are now reached from three. Two consequences are designed for explicitly:

- Mutating `heldUpdates` and scanning it for releases happens under one lock inside the queue. There
  is one production caller (forecast coalescing), the work under the lock is non-blocking and
  synchronous throughout, and it restores the serialisation the mechanism assumed without restoring
  the single lane.
- **A hold that displaces an earlier hold for the same key must give the displaced round its place
  back.** Each hold calls `RoundForNewWork()`, which joins the running round. Today
  `heldUpdates[heldFor] = …` overwrites last-write-wins and the displaced entry is dropped without
  leaving its round — latent, because one caller guards it with `IsHeld` first, and live the moment
  two lanes can reach that guard at once. A round that never finishes silently drops every write it
  staged.

`roundBeingHandedOver` needs no change: it is an `AsyncLocal`, so each lane's reader flow has its own
value, and the handover is a synchronous call chain within one flow.

`AdmittedWorkOrdering.InTheOrderTheQueueWillReachThem` keeps its comparator — running first, then
longest-waiting first. Its claim narrows from the instance to a lane, and the per-lane truth an
operator needs is carried by `WaitingBehind`. A queue position across lanes is not a number that
exists.

## Alternatives Considered

**N generic readers over one shared channel.** Rejected twice over. Mechanically, a channel reader
cannot decline an item it has taken, so "readers keyed by update type" would have to dequeue and
requeue — reordering the queue and spinning when every pending item belongs to a busy type.
Semantically, it lets two Portfolio refreshes run at once, which multiplies tracker load in exactly
the deployment that reported this.

**One shared channel, N generic readers, plus a `SemaphoreSlim(1)` per update type.** Keeps
same-type serialisation, and is the closest working variant of the above. Rejected because the
guarantee becomes a runtime gate rather than a structural property: with N readers all blocked on one
type's semaphore, head-of-line blocking returns in a form that looks like the bug we just fixed and
is far harder to see. One reader per lane cannot have that failure mode.

**A dispatcher that reads one channel and fans out to per-lane channels.** Identical outcome to the
decision, with one more hop, one more task, and one more place for an item to be lost. Rejected on
cost with no benefit.

**Per-connection lanes**, so that two Portfolios on two different trackers do not wait for each other.
Genuinely attractive and rejected on three counts, the first of which is fatal. **A Portfolio's
connection is not a function of the Portfolio** — its Features can come from several work tracking
system connections — so "the lane of this work" would not be well defined. Second, the connection is
not known at enqueue time without a repository read on a path that today touches no database. Third,
it makes the lane set dynamic, which makes the drain, the reader-task set and the read model's
lane-holder resolution all moving targets. Recorded as revisitable: if a real instance shows two
trackers starving each other, the cheaper next step is a per-connection concurrency limit *inside* the
Portfolio lane, which raises concurrency without making the lane set dynamic or changing the read
model.

**Per-entity lanes.** Maximum concurrency, and it dissolves the reported starvation completely.
Rejected: it is unbounded concurrency against one tracker, which is the failure the reporting
deployment is closest to, and it would put an arbitrary number of concurrent writers on a SQLite file
whose `busy_timeout` is a ceiling.

**Fold Forecasts into the Portfolio lane**, so that a Portfolio refresh and the forecast it triggers
stay serial and `WriteBackRound` never sees two executions. This looks like it removes the need for
ADR-196 entirely. **It does not**, and the reason is decisive: a forecast is also triggered from
inside a *Team* execution, by the handler for a refreshed team's data, and it joins the Team's round.
A round spans the Team lane and the forecast's lane whatever the forecast's lane is. Merging the two
would therefore buy no round safety at all while reintroducing head-of-line blocking between
forecasts and portfolio refreshes.

**Priority or preemption between lanes.** Rejected as unneeded: lanes are independent, and nothing in
the reported pain is about one kind of work deserving to go first.

## Consequences

**Positive.** The reported starvation is gone by construction rather than by tuning: a Team refresh
and a Portfolio refresh are in different lanes and neither can be behind the other. ADR-076's known
Gap 2 — a blocking `pg_advisory_lock` awaited inside the reader loop stalls every unrelated update
behind it — narrows from the whole replica to one lane. It is not closed; it is bounded.

**Negative / accepted.**

- Concurrent `SaveChanges` against SQLite goes from one to at most three. WAL plus a 10 s
  `busy_timeout` is expected to absorb it, and nothing in the design holds a write transaction across
  a connector call, so the window a writer holds the file is the save itself. Expected is not known —
  see Earned Trust.
- Several rows can read *Running* at once. The task list already renders per row, so this is correct
  as drawn; one comment in the activity section explains the single spinner by asserting "there is
  only ever one of those", and that sentence stops being true.
- `Slice02SeeWhatIsRunningSpecifications` pins the lane holder and **survives unchanged**: its
  scenarios queue one Team behind another, which is one lane either way. That is worth stating rather
  than assuming, because it means the existing suite cannot detect the change at all — the cross-lane
  case it never had is new coverage, not a rewrite.
- Three reader tasks means a lane can die alone. The backstop `catch` stays inside each loop and now
  names the key, so a lane that stops is attributable rather than merely quiet.

## Earned Trust

Every assumption below is a claim about a dependency that can lie, and each has a probe that makes it
answer.

| Assumption | Probe |
|---|---|
| A Portfolio refresh that never returns does not stop a Team refresh | Acceptance test with a connector double **held open** until released — not merely slow. A test that waits for a slow refresh to finish passes against the bug. |
| Within a lane, work is still serial | With a Portfolio refresh held open, a second Portfolio refresh stays `Queued`. |
| Every `UpdateType` has a lane | Test enumerating `Enum.GetValues<UpdateType>()` and asserting the mapping is total. A sixth member must not land nowhere silently. |
| Deletes run in their entity type's lane | Test asserting the mapping for both delete members, so D6's grouping is a pinned fact rather than a reading of the table above. |
| Shutdown drains every lane | Work in flight in two lanes; `DrainAsync` returns only after both finished or the timeout elapsed, and neither is left admitted. |
| Cancelling in one lane leaves another lane running | Acceptance test across two lanes. |
| A queued row names the holder of **its own** lane | Asserted over repeated reads. The store hands back a dictionary's buckets or a hash's fields, so one correct read proves nothing. |
| Work whose lane is free reports no holder | Explicit negative assertion — the previous behaviour returned an arbitrary running row. |
| **SQLite tolerates three lanes saving at once** | An integration test driving concurrent saves across three lanes against a real SQLite file with the production PRAGMAs, asserting no `SQLITE_BUSY`. The substrate that lies here is the `busy_timeout`: it is a ceiling on how long a writer waits, not a promise that it wins. Run early enough that the answer can still change the design. |
| A displaced hold gives its round back | Test: hold the same key twice; the first hold's round is left, so the round can still finish and its staged writes still reach the tracker. |
| Two lanes releasing holds concurrently release each hold once | Test driving `ReleaseClearedHolds` from two lanes against one satisfied hold. |

## Cross-reference

- [ADR-076](./adr-076-cluster-aware-update-queue.md) — the per-key advisory lock and the monotonic
  Redis status store. Per-replica lanes sit underneath both and weaken neither: INV-1 (monotonic
  advance) and INV-4 (one active lifecycle per `UpdateKey`) are enforced by the store and the lock,
  not by the reader count. Lanes partially mitigate that ADR's recorded **Gap 2**; **Gap 1**
  (a coalesced follow-up lost across pods) is untouched and still open.
- [ADR-183](./adr-183-cancellation-ambient-token-with-paging-widened.md) — the ambient cancellation
  token. Its confinement argument holds per lane, because `AsyncLocal` does not escape an async
  method and each lane's reader is its own flow.
- [ADR-196](./adr-196-write-back-round-concurrency-contract.md) — the round safety this ADR makes a
  precondition. Removing the single lane removes the only thing currently making the round safe.
- [ADR-181](./adr-181-update-activity-is-a-read-through-the-status-store.md) — the read the lane
  holder is resolved on. No store change is needed: `GetAdmittedWork()` already returns everything,
  and grouping by lane happens in the controller.
- [ADR-144](./adr-144-writeback-collection-seam.md) — its Context states "there is no 'refresh round'
  object anywhere in the system; there are independent queue items." That was true when it was
  written and is no longer: `WriteBackRound` exists, and this ADR is what makes two of its executions
  concurrent.
