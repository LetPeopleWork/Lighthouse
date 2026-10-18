# ADR-253: Cancel all is one broadcast each replica applies to its own work, follow-ups and holds

**Status**: Accepted (maintainer, 2026-10-18, DESIGN; recommendation taken).
**Feature**: `epic-6311-responsive-during-updates` (Story #6322, slice 05)
**Extends**: [ADR-183](./adr-183-cancellation-ambient-token-with-paging-widened.md) and the per-key cancellation of
epic-5511 (`IUpdateCancellationNotifier`, `CancelIfStillWaiting`).

## Context

Per-row Stop publishes one key through `IUpdateCancellationNotifier` to whichever replica admitted the work. Emptying the
Task Manager that way needs one click per row, and it still leaves work that comes back:
- **Coalesced follow-ups** (`pendingReruns`) are requeued when a run ends, including a cancelled one.
- **Held forecasts** (`heldUpdates`) are parked until their Teams clear. They are never admitted, so the shared status
  store cannot see them.

Both live in process, on each replica. Deletes (`TeamDelete`, `PortfolioDelete`) are never cancelled.

## Decision

1. `POST api/latest/update/tasks/cancel` on `UpdateController`, guarded `[RbacGuard(SystemAdmin)]`. It answers 204,
   also when there is nothing to cancel, and is idempotent. A fourth route about the same subsystem fits the controller's
   existing justification for one controller.
2. It calls `IUpdateQueueService.CancelAllAsync()`, which publishes **one cancel-all message** through
   `IUpdateCancellationNotifier`. Redis uses a channel of its own, so a replica on the previous version ignores the
   message instead of misreading it. In process, it is a direct call.
3. Each replica applies the message, on receipt, to its own state, in this order:
   1. drop its non-delete follow-ups, so a run cancelled next cannot requeue itself;
   2. drop every held update and leave its write-back round, so writes other work staged still flush;
   3. for each non-delete key it admitted, stop the token and mark it `Cancelled` if it is still waiting (the per-key
      path), then tell listeners.
   Queued work already in the channel stops at its start-of-run check and leaves its round.
4. **The race**: what is admitted when the message arrives is cancelled; anything admitted later runs. Scheduled
   refreshes carry on.

## Alternatives considered

- **List admitted keys from the status store and publish each**, plus a second message for follow-ups and holds: two
  messages, a list that is stale by design, and holds are invisible to the store.
- **A "drop the follow-up" flag on the per-key message**: still cannot reach holds, and it changes per-row Stop.
- **A domain event**: no second subscriber; the queue is the only owner of what is cancelled.
- **A server-side pause**: Epic #6321.

## Consequences

**Positive**
- One click empties the list on every replica; nothing stopped comes back as a follow-up.
- No constructor change (`UpdateQueueService` stays at 7 parameters).

**Negative / accepted**
- `IUpdateQueueService` and `IUpdateCancellationNotifier` widen. Grep the short and the fully-qualified names in the
  test projects for doubles.
- During a rolling deploy, an old replica ignores Cancel all.

## Enforcement

- Queue tests: no non-delete `Queued` row after the call; no follow-up or hold runs afterwards; rounds left; deletes
  complete; a trigger after the call runs.
- Acceptance under the Redis substrate: work on the other replica stops.
- Controller test: 403 for non-admins, 204 on an empty list.
