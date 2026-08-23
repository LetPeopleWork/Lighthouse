# ADR-183: Cancellation is an ambient scoped token, and `IWorkTrackingConnector` widens only where it pages

- **Status**: **Proposed** (DESIGN, 2026-08-23)
- **Date**: 2026-08-23
- **Feature**: epic-5511-task-manager (ADO Epic #5511, slice 04 / ADO #5842)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

Nothing in Lighthouse can be cancelled. `UpdateQueueService` holds an unbounded
`Channel<Func<Task>>` with a single reader loop and no `CancellationTokenSource` anywhere.
`EnqueueAndAwaitAsync` takes a `cancellationToken`, but it cancels *the caller's wait*, not the work —
the update runs to completion regardless. The only way to stop a running refresh today is to restart the
process, which takes everything else down with it.

Two facts decide how far a token can reach.

None of `IWorkTrackingConnector`'s sixteen methods takes a `CancellationToken`. And Epic #5687's Data
Center dogfood went from 468 856 ms to 2 087 ms by changing how the connector pages, on an identical
scanned set — which says the wall-clock of a refresh is overwhelmingly *inside* connector paging. A
checkpoint that can only be placed outside the connector would therefore, for a single wedged Team,
cancel nothing the operator would notice within any useful interval.

There is an established shape for an ambient per-execution value: `WriteBackRoundContext`, an
`AsyncLocal` written only by the queue, read from anywhere inside an update execution without being
threaded through every signature. `UpdateQueueService.ExecuteUpdateTask` sets it immediately before
creating the scope, precisely so that anything resolved out of that scope already knows.

## Decision

**Two parts, both needed.**

1. **`UpdateCancellationContext`** — a new `AsyncLocal`-backed sibling of `WriteBackRoundContext`,
   written only by `UpdateQueueService`, holding the token for the execution running right now. The
   queue owns a `CancellationTokenSource` per admitted `UpdateKey` and disposes it alongside
   `statusStore.Remove`.

2. **`IWorkTrackingConnector` widens on the six paging methods only** — the two `GetWorkItemsForTeam`
   overloads, the two `GetFeaturesForProject` overloads, `SweepWorkItemsForTeam` and
   `SweepFeaturesForPortfolio` (and `SweepParentFeatures` where it pages). Not `ValidateConnection`,
   not `GetPredefinedAdditionalFields`, not `WriteFieldsToWorkItems` — those do not page and have no
   use for a token.

**`Cancelled` is appended to `UpdateProgress` after `Failed`.** `Advance` compares ordinals, so
position is semantic: appending preserves every value already in flight and keeps `Cancelled` reachable
from any earlier state.

**Cancelling is per `UpdateKey`.** One entity's refresh stopping does not stop another's.

**The cancel route is idempotent.** Cancelling an update that has already finished is accepted and
changes nothing.

**The granularity is measured, not asserted.** The slice-04 probe instruments a real Team refresh,
records time per phase, and records which phases can observe the token. AC-04.2 asserts the measured
interval as a number. If the probe shows the phase boundaries alone are sufficient, the port widening
is not done — the decision rule outranks the decision.

## Consequences

**Positive.** Cancel bites between pages, which is where the time is, so the button does what the word
implies. The context follows a shape already in the codebase, so there is one idiom for "ambient value
the queue sets", not two. The port change is confined to methods that loop.

**Negative.** A signature change on `IWorkTrackingConnector` ripples through five connector
implementations. That is real cost, and it is why the probe runs early — during slices 02 and 03 —
rather than at the start of slice 04: if the measurement says phase boundaries suffice, the cost is not
paid at all.

**The invariant that is easy to miss.** A cancelled run must still flush or explicitly abandon its
`WriteBackRound`, and must still release anything held behind its key by
`HoldUntilQueuedWorkClears`. A round that never finishes silently drops every write it staged; held
work behind a key that never released stays parked until something unrelated pokes the same key. This
is the same invariant the failure path carries — the two reach it through different doors, and both
must.

`AsyncLocal` does not escape an async method, so the token is confined to the execution the queue set
it for. Work the queue starts once that call has returned — a coalesced follow-up — gets its own
source, which is correct: it is new work, and cancelling the run that preceded it should not cancel it.

**Enforced by**: an ArchUnit rule that nothing but `UpdateQueueService` writes
`UpdateCancellationContext`.

## Alternatives considered

**Phase boundaries only, connector port untouched.** Zero connector change and the smallest diff.
Rejected on the #5687 evidence: it would ship a Cancel button that, for the single wedged Team an
operator most wants to stop, does nothing observable for a long time.

**Widen all sixteen port methods.** Uniform. Rejected: it puts a `CancellationToken` on
`ValidateConnection` and `GetPredefinedAdditionalFields`, which do not loop and cannot honour one,
which teaches the next reader that the parameter is decorative.

**Extend `WriteBackRoundContext` to also carry the token.** One context instead of two. Rejected: its
`WriteBackRound` has `Join` / `Leave` / `HasFinished` semantics a token has no use for, and an
execution with no write-back would then have to carry a round in order to carry a token. Same shape,
separate state.

**Hard abort of the running task.** Immediate. Rejected: half-written work items, an orphaned
write-back round, and a `RefreshLog` row describing a run that did not happen.

**Dequeue-only.** Cancel removes a `Queued` item; `InProgress` cannot be stopped. Rejected by the
maintainer: it leaves the Epic's stated pain — a runaway refresh — unfixed.
