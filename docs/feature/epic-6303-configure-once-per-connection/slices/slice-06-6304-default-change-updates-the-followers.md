# Slice 06 — Story 6304 — A changed default updates exactly the owners that follow it

**ADO**: Story #6304 (Epic #6303) · **User story**: US-05 · **Estimate**: ~0.5 day · **Depends on**: slices 05a, 05b
**Release point**: no — #6304 releases after slice 07. Alone, this slice leaves `main` releasable (L8).
**Locked decisions**: D12, D13

## Goal
Saving the connection with a set or changed default queues, at once, the followers whose fetch fingerprint moved —
no more, no fewer — and slice 05a's save notice says how many of them are updating (AC-F.5).

## Why this is thin
Story 6307 already publishes `ConnectionFetchShapeChanged` for every owner when `ForConnection` moves
(`WorkTrackingSystemConnectionController.cs:157-193`) and the handler queues them
(`ConnectionFetchShapeChangedUpdateTriggerHandler.cs`). This slice narrows the owner list to those whose
`FetchFingerprint.For` moved and lets a default change trigger it.

## IN
- Compare each owner's fingerprint before/after the save; queue the movers.
- `startUpdates=false` honoured; the save notice still names the followers.
- Read-time defaults (Estimation, Story #6313 slice 12) queue nothing by construction.
- The updating count folded into the one save notice — never a second message.

## OUT
- New copy beyond the DISTILL-approved save notice.

## Learning hypothesis
Disproves "a per-owner fingerprint comparison at save is cheap enough on a large connection" if the save on the dev
instance's biggest connection gets noticeably slower (measure).

## Production data / dogfood
Dev instance: switch the Teams default between two fields; count Task Manager rows against the number of followers.

## Acceptance criteria
AC-5.1 – AC-5.3 (AC-5.3 is AC-F.5, the updating count in the one save notice).

## Changed assumptions
(filled during DELIVER)
