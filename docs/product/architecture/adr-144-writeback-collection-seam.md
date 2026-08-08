# ADR-144: Write-back intents are staged by a resolver and flushed once per update execution — the trigger service stops writing and starts returning a plan

**Status**: Accepted (2026-08-08; D-A7-R ratified by the user the same day — see "The residue, and how it is closed")
**Date**: 2026-08-08
**Feature**: `quiet-jira-writeback` (ADO Epic #5500 "Quiet write-back", slice 01 / Story #5502)
**Decider**: Morgan (Solution Architect), DESIGN application layer, interaction mode = PROPOSE

---

## Context

Write-back fires inline from **four** call sites, none of which knows about the others:

| Site | Call |
|---|---|
| `PortfolioUpdater.cs:79` | `TriggerFeatureWriteBackForPortfolio` after the Features refresh |
| `PortfolioUpdater.cs:85` | `TriggerForecastWriteBackForPortfolio` after forecasts — a **second** pass over overlapping items |
| `ForecastUpdater.cs:43` | a **third** pass, on the `UpdateType.Forecasts` queue key |
| `TeamUpdater.cs:53` | Team-level write-back |

The passes do not deduplicate, and the mechanism is precise: `WriteBackService` calls the connector and
returns results but **never writes the new value into the local `AdditionalFieldValues`**. The stored copy
holds the pre-write value until the next inbound sync, so a later pass in the same round compares a fresh
forecast against a stale local copy, `currentAdditionalFieldValue != update.Value` fires again, and the
same field is written again.

**Three verified facts constrain any fix, and one of them corrects the DISCUSS arithmetic.**

*The passes are not all in one process moment.* `TeamDataRefreshedForecastTriggerHandler` does not call
the forecast updater — it calls `IForecastUpdater.TriggerUpdate(portfolio.Id)`, which **enqueues** onto
`UpdateQueueService`. Each queued update runs later, in its own DI scope, one at a time on a single-reader
channel. There is no "refresh round" object anywhere in the system; there are independent queue items.

*The queue already coalesces the amplifier DISCUSS counted.* `EnqueueUpdate` admits a key through
`statusStore.TryAdmit`; a duplicate `(UpdateType.Forecasts, portfolioId)` trigger is refused admission and
parks a **single** `pendingReruns` entry, which `TryScheduleRerun` runs once when the in-flight execution
finishes. So N Teams triggering the same Portfolio produce **at most two** forecast executions, not N.
DISCUSS's "N+2 passes per refresh round" and the KPI "1, down from N+2 for N teams" are an upper bound
that the queue already caps. The real portfolio-level count is **≈4** — one Features execution carrying
two passes, plus up to two coalesced Forecasts executions carrying one each.

*The dispatcher cannot carry the accumulator.* `DomainEventDispatcher` is a singleton
(`Program.cs:1052`) that calls `serviceScopeFactory.CreateScope()` per publish, so handlers never share
the publisher's scope. A scoped accumulator injected into an updater and read by a handler is two
different instances, and the naive design silently collects nothing.

## Decision

**Split resolution from execution. Resolve intents where the data is fresh, stage them, flush once at the
end of the update execution.**

1. **`IWriteBackTriggerService` becomes a resolver and stops performing I/O.** Its three methods return
   `IReadOnlyList<WriteBackFieldUpdate>` — a plan value — instead of awaiting `IWriteBackService`. The
   percentile, size and age resolution it already does (`ResolveTeamUpdates:115`,
   `ResolvePortfolioUpdates`) is pure over its inputs and becomes provably so: no repository writes, no
   HTTP, nothing to swallow. This is the functional-core/imperative-shell split applied to the one part
   of the pipeline where "did this write?" is currently unanswerable from the type.
2. **`IWriteBackCollector` (scoped) is the staging seam.** `Stage(connection, updates)` and
   `Task<IReadOnlyList<WriteBackResult>> FlushAsync()`. Scoped is the correct lifetime and not an
   accident: `UpdateQueueService.ExecuteUpdateTask` creates exactly one scope per queued update and
   passes its provider into `Update(id, serviceProvider)`, so one collector instance spans the whole
   execution — including both of `PortfolioUpdater`'s passes.
3. **Deduplication key is `(connectionId, workItemId, targetFieldReference)`, last stage wins.** The
   later pass in an execution has, by construction, the fresher value; staging is a dictionary upsert,
   not an append.
4. **The flush is in one place: `UpdateServiceBase.TriggerUpdate`.** The enqueued lambda already wraps
   `Update(id, serviceProvider)` in a try/catch; the flush goes in a `finally` beside it. Every update
   type inherits it, ordering is not registration-order-coupled, and no updater can forget it. Flush
   failures keep today's swallow-and-log semantics — `WriteBackTriggerService.cs:56` already swallows
   everything, so no signal is newly lost.
5. **Explicit ordering is preserved.** `PortfolioUpdater` still reads top-to-bottom as
   features → resolve → forecasts → resolve → flush. Nothing moves into `Program.cs` registration order.
6. **No new domain event.** The seam is a scope-lifetime collaborator and an explicit terminal call, not
   a published fact. Slice 01's name ("event-driven write-back collection") describes the intent, not the
   mechanism; introducing a `PortfolioUpdateCompleted` event purely to signal "now flush" would put the
   ordering contract into DI registration order — exactly what the slice brief's own design constraints
   forbid.

### The residue, and how it is closed — a scoped exception to D11

A scoped collector cannot span two queue executions. The Features execution's forecast pass and a later
coalesced Forecasts execution would still write the same field twice, because the local
`AdditionalFieldValues` copy is stale in both. Staging alone takes portfolio-level amplification from ≈4
passes to ≈2; it cannot reach 1 by coordination, because there is nothing to coordinate *through*.

**Decision: after a successful write, `WriteBackService` writes the value into the item's local
`AdditionalFieldValues`.** The stored copy then says what the tracker now holds, the existing
`currentAdditionalFieldValue != update.Value` guard sees no change on the next pass, and the duplicate
disappears **by construction rather than by coordination** — no accumulator lifetime has to reach across
queue executions, because there is no longer anything for it to reach across.

**This is a scoped exception to D11, and it is not D11 being ignored.** D11 rules out designing around
*forecast re-simulation jitter*: no hysteresis, no write threshold, and no slice built on writing values
locally in order to damp a Monte Carlo value that moves by a day. That reasoning is untouched here. A
re-simulated percentile genuinely differs from the last one, the guard correctly fires, and Lighthouse
still writes it — this change does nothing to suppress that and must not be extended to. The exception is
narrow and stated as a rule: **persist only a value that was just successfully written to the tracker.**
Its effect is to make the stored copy *true* rather than stale, which is a correctness improvement
independent of write-back volume; the duplicate-pass fix is a consequence of the copy being true, not the
justification for lying about it.

Three properties bound the exception:

- **Success only.** A failed or partially-failed write persists nothing. The local copy may lag reality;
  it may never lead it.
- **The written value only.** Not a rounded value, not a threshold, not a previous value retained to damp
  movement. Whatever went over the wire is what is stored.
- **Inbound sync still wins.** The next refresh overwrites the local copy from the tracker, so a write
  that appeared to succeed but did not take effect self-corrects within one cycle.

Two alternatives to reach the same end state were rejected:

- **A singleton "recently written" memo** keyed `(connectionId, workItemId, fieldRef) → value` with a TTL.
  **Rejected**: it is a cache of a fact the database already models, its correct TTL is exactly "until the
  next inbound sync" which it cannot observe, and its failure direction — believing a write landed when it
  did not, with no sync to correct it — is the dangerous one.
- **Accept ≈2 passes and change nothing.** Viable, and the design is correct without the exception.
  **Rejected** because it leaves the local copy knowingly stale, which is a standing untruth in the
  database that any future reader of `AdditionalFieldValues` inherits.

## Alternatives Considered

**Leave the call sites and deduplicate inside `WriteBackService`.** A per-connection set of already-written
`(item, field, value)` triples, populated as writes succeed. No collector, no signature changes.
**Rejected**: `WriteBackService` is resolved per scope too, so it has exactly the same lifetime reach as
the collector and closes exactly the same subset of duplicates — while hiding the seam inside a service
whose job is one flush, and leaving slice 02's grouping to operate on whatever each pass happens to bring
rather than on the whole execution.

**A singleton collector keyed by a correlation id, flushed on a quiescence signal** (for example, when the
update queue reports no active work). This *would* span queue executions and reach one write per round.
**Rejected**: it invents a "refresh round" the system does not have, its flush trigger is a race against
`UpdateQueueService`'s own admission window — the same two-statement window CI run 31203153029 already
found once — and a write-back that fires on quiescence is decoupled in time from the data that produced
it, so a failure has no execution to be logged against.

**Publish a terminal domain event and let a handler flush.** Rejected by the slice's own verified
constraints: the dispatcher creates its own scope, so the handler cannot see the collector; `PublishAsync`
awaits inline in registration order, so publishing defers nothing; and the ordering contract would move
from a readable method body into `Program.cs`.

## Consequences

**Positive**

- `PortfolioUpdater`'s two passes over overlapping items collapse to one flush, deterministically, with
  no reliance on queue timing.
- The resolver's purity makes the whole "did this write?" question answerable from the signature.
  `IWriteBackTriggerService` returning a plan is the contract shape that makes "a resolver silently
  wrote" non-representable rather than merely untested.
- Slice 02 groups once, against the whole execution's intents, instead of once per pass — which is why
  the slice order is seam-first.
- One flush site means one place where premium gating, logging and failure semantics live, instead of
  four.
- With the D11 exception, the cross-execution duplicate is gone **by construction**: the guard that
  already exists starts seeing the truth, so no component has to remember anything across scopes.

**Negative / accepted**

- `IWriteBackTriggerService`'s three methods change return type — a shared contract, so usages are
  grepped and `PortfolioUpdaterTest` / `TeamUpdaterTest` / `ForecastUpdaterTest` mocks extended before the
  change lands.
- A flush in `UpdateServiceBase` means every update type pays a collector resolution even when nothing
  was staged. The empty case is a dictionary-count check and one early return.
- The D11 exception writes to `AdditionalFieldValues` from the write-back path, which until now only ever
  read it. The rule bounding that (success only, written value only, inbound sync still wins) is a
  convention held by one method and must be asserted, not assumed — see Earned Trust.

## Earned Trust

| Assumption | Probe |
|---|---|
| One collector instance really does span an execution | Integration test: stage from two points inside one `Update` → one flush, one connector call |
| A flush failure does not abort the refresh | Test: collector throws → `Update` still completes, refresh log records, failure logged (AC-04.6) |
| An empty cycle costs nothing | Test: no changed value → zero connector calls (AC-04.3, the D8 no-op guard) |
| ADO is unaffected | Test: ADO path through the collector still passes `suppressNotifications: true` (AC-04.4) |
| The queue really coalesces as read | Existing `UpdateQueueService` coalescing tests; the ≈4-not-N+2 claim rests on them |
| A failed write never updates the local copy | Test: connector reports failure → `AdditionalFieldValues` unchanged, and the next pass still attempts the write |
| A successful write suppresses the next pass | Integration test: two executions over the same unchanged forecast → exactly one connector call in total |
| Inbound sync still overrides | Test: a locally-persisted value is overwritten by the next refresh from the tracker, so an apparent-success-that-was-not self-corrects |
| The exception does not creep into jitter damping | Test: a genuinely re-simulated percentile still writes (D11 stands — no hysteresis, no threshold) |

## Cross-reference

- [ADR-027](./adr-027-target-architecture-modular-monolith-domain-events-cqrs-lite.md) D2 — the dispatcher
  is a thin router with no outbox; this seam deliberately does not use it, for the scope reason above.
- [ADR-143](./adr-143-batched-writeback-with-unbatched-retry.md) — what the flushed set is grouped into.
- `UpdateQueueService.EnqueueUpdate` / `TryScheduleRerun` — the coalescing that caps the forecast
  amplifier and corrects the DISCUSS "N+2" arithmetic.
