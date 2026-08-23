# Slice 01 — A failed refresh says it failed

**Epic** #5511 Task Manager · **ADO** Bug **#5788** · **Story** US-01 ·
**Job** `job-operator-trust-that-a-finished-refresh-tells-the-truth`

## Goal

Make the terminal status the browser receives agree with what actually happened, so that every surface
built on it afterwards can be believed.

## IN scope

- `UpdateServiceBase.TriggerUpdate` stops swallowing the exception from `Update()`. The `finally` —
  write-back flush, round summary — still runs; the failure then reaches `UpdateQueueService`.
- `RunUpdateAsync` records `UpdateProgress.Failed` and `NotifyListeners` pushes it.
- The refresh indicators already shipped on Team detail and Portfolio detail render the failed state.

## OUT of scope

- Any Task Manager UI. No icon, no popover.
- A failure *reason*. `RefreshLog.Success` stays a bare bool (deferred item E).
- `Cancelled` as a status — slice 04.
- Changing what the `finally` writes.

## Learning hypothesis

**Disproves that the existing status pipeline can carry a truthful terminal state.**

If it succeeds: the pipeline is sound and the whole Epic can be built on it.
If it fails — because letting the exception through breaks a caller that awaits an update, or strands a
`WriteBackRound`, or leaves held work parked — then the Task Manager cannot read from this pipeline and
needs its own record of what happened, which resizes slices 02 and 03 substantially.

## Acceptance criteria

See US-01 in `feature-delta.md` — AC-01.1 through AC-01.6. The two that carry the risk:

- **AC-01.3 / AC-01.4** — write-back flush and held-work release must still happen on the failure path.
  A round that never finishes silently drops every write it had staged, and held work parked behind a
  failed key stays parked until something unrelated pokes the same key.
- **AC-01.5** — enumerate every caller of `EnqueueAndAwaitAsync` and assert none of them starts throwing
  where it previously returned. `RunAwaitableUpdateAsync` already calls `tcs.TrySetException(ex)` on its
  own catch, so the awaitable path may already behave correctly and only the fire-and-forget path is
  wrong. Establish which before changing anything.

## Dependencies

None. This is the first slice and blocks every other one (D6).

## Effort

Half a day. The change itself is small; the blast-radius work in AC-01.5 is most of it.

## Reference class

`fix-portfolio-refresh-race`, `bug-5586-getlikelihood-evidence-and-buckets` — small correctness fixes in
the update path where the cost was establishing who else depended on the old behaviour.

## Pre-slice SPIKE

None. The mechanism is fully understood; the unknown is the caller set, which is a grep, not a probe.
