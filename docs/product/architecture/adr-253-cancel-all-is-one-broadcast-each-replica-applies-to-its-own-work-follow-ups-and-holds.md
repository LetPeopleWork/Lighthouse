# ADR-253: Cancel all is one broadcast each replica applies to its own work, follow-ups and holds

**Status**: Accepted (maintainer, 2026-10-18, DESIGN; recommendation taken; at-most-once and the Redis scope confirmed by
NMD-15 at the review gate).
**Feature**: `epic-6311-responsive-during-updates` (Story #6322, slice 05)
**Extends**: [ADR-183](./adr-183-cancellation-ambient-token-with-paging-widened.md) and the per-key cancellation of
epic-5511 (`IUpdateCancellationNotifier`, `CancelIfStillWaiting`).

## Context

Per-row Stop publishes one key through `IUpdateCancellationNotifier` to whichever replica admitted the work. Emptying the
Task Manager that way needs one click per row, and it still leaves work that comes back:
- **Coalesced follow-ups** (`pendingReruns`) are requeued when a run ends, including a cancelled one.
- **Held updates** (`heldUpdates`) are parked until their Teams clear. They are never admitted, so the shared status
  store cannot see them. Holds are forecast-only: `ForecastUpdater.HoldTheForecastUntil` is the only caller of
  `HoldUntilQueuedWorkClears`.

Both live in process, on each replica. Deletes (`TeamDelete`, `PortfolioDelete`) are never cancelled.

## Decision

1. `POST api/latest/update/tasks/cancel` (and `api/v1/…`, which the controller also serves) on `UpdateController`,
   guarded `[RbacGuard(SystemAdmin)]`. It answers 204, also when there is nothing to cancel, and is idempotent. A
   fourth route about the same subsystem fits the controller's existing justification for being one controller.
2. It calls `IUpdateQueueService.CancelAllAsync()`. That publishes one message through the notifier port's new pair,
   **`PublishCancelAllAsync()` / `SubscribeToCancelAll(Action onCancelAll)`**, which sits beside the per-key pair. In
   process the message is a direct call. Redis carries it on a channel of its own. The Redis multi-replica setup is a
   proof-of-concept path, so it gets only the minimum: the channel and one real-Redis test, no hardening.
3. Each replica applies the message, on receipt, to its own state, in this order:
   1. drop its non-delete follow-ups, so a run cancelled next cannot requeue itself;
   2. drop every held update (all of them forecasts) and leave its write-back round, so writes other work staged still
      flush;
   3. for each non-delete key it admitted, stop the token, mark the key `Cancelled` if it is still waiting (the per-key
      path), then tell listeners.
   Queued work already in the channel stops at its start-of-run check and leaves its round.
4. **The race, and an ordering it needs.**
   - What is admitted when the message arrives is cancelled. Anything admitted later runs, and scheduled refreshes
     carry on.
   - For "admitted" to mean "cancellable", a key's token source must exist **before** the key is visible in the status
     store. `EnqueueUpdate` already calls `cancellations.Admit` before `statusStore.TryAdmit`.
   - `EnqueueAndAwaitAsync` does it the other way round today. It is reordered: `cancellations.Admit` before
     `statusStore.TryAdmit`, as in `EnqueueUpdate`.
5. **The guarantee is at most once** (NMD-15). A replica reconnecting to Redis when the message is published misses it
   and keeps its work. The rescue is the list itself: the rows are still shown, so the admin clicks Cancel all again or
   stops the row. Per-row Stop already behaves the same way.

## Alternatives considered

- **List admitted keys from the status store and publish each**, plus a second message for follow-ups and holds: two
  messages, a list that is stale by design, and holds are invisible to the store.
- **A "drop the follow-up" flag on the per-key message**: still cannot reach holds, and it changes per-row Stop.
- **At-least-once delivery** (a stored request each replica acknowledges): new state and a sweeper for a
  proof-of-concept deployment, against a failure the admin can already see and retry.
- **A domain event**: no second subscriber; the queue is the only owner of what is cancelled.
- **A server-side pause**: Epic #6321.

## Consequences

**Positive**
- One click empties the list on every replica that hears it; nothing stopped comes back as a follow-up.
- No constructor change (`UpdateQueueService` stays at 7 parameters).

**Negative / accepted**
- At most once: a missed message leaves visible work, which the admin recovers by clicking again.
- `IUpdateQueueService` and `IUpdateCancellationNotifier` widen. Grep the short and the fully-qualified names in the
  test projects for doubles.

## Enforcement

- Queue tests:
  - no non-delete `Queued` row after the call;
  - no follow-up or hold runs afterwards, and their rounds are left;
  - deletes complete;
  - a trigger after the call runs.
- An awaited enqueue cancelled straight after admission reaches `Cancelled`; there is no window in which its key has no
  token source.
- Holds are forecast-only: a test pins that `HoldUntilQueuedWorkClears` is only reached for `UpdateType.Forecasts`.
- One real-Redis test: work on the other replica stops.
- Controller test: 403 for non-admins, 204 on an empty list.
