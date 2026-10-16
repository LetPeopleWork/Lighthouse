# Slice 06 — Story 6304 — A changed default updates exactly the owners that follow it

**ADO**: Story #6304 (Epic #6303) · **User story**: US-05 · **Estimate**: ~0.5 day · **Depends on**: slices 05a, 05b
**Release point**: yes — #6304's last slice; #6304 is releasable after this slice (slice 07 dropped, UI-4) (L8).
**Locked decisions**: D12 (as amended), D13

## Goal
Saving the connection with a set or changed default queues, at once, the followers whose fetch fingerprint moved —
no more, no fewer — and the save dialog asks whether to update the followers now, saying how many (AC-F.5).

## Why this is thin
Story 6307 already publishes `ConnectionFetchShapeChanged` for every owner when `ForConnection` moves
(`WorkTrackingSystemConnectionController.cs:157-193`) and the handler queues them
(`ConnectionFetchShapeChangedUpdateTriggerHandler.cs`). This slice narrows the owner list to those whose
`FetchFingerprint.For` moved and lets a default change trigger it.

## IN
- Compare each owner's fingerprint before/after the save; queue the movers.
- `startUpdates=false` (Save only) honoured; the log entry still names the followers.
- Read-time defaults (Estimation, Story #6313 slice 12) queue nothing by construction.
- The dialog's update-question variant ("Update 3 Teams and 0 Portfolios?" — Cancel / Save only / Save and update)
  for a changed fetch-time default with followers; the plain variant for an Estimation-only change with followers;
  no dialog when nobody follows (UI-3). The updates show in Task Manager → Activity.

## OUT
- New copy beyond the maintainer-approved dialog (UI-3).

## Learning hypothesis
Disproves "a per-owner fingerprint comparison at save is cheap enough on a large connection" if the save on the dev
instance's biggest connection gets noticeably slower (measure).

## Production data / dogfood
Dev instance: switch the Teams default between two fields; count Task Manager rows against the number of followers.

## Acceptance criteria
AC-5.1 – AC-5.3 (AC-5.3 is AC-F.5, the follower count and the update question in the save dialog).

## Changed assumptions
- 2026-10-16 (DESIGN, maintainer P-5, UI-3, UI-4): the updating count lives in the pre-save dialog, not an after-save
  notice; this slice is now #6304's release point because slice 07 was dropped.
