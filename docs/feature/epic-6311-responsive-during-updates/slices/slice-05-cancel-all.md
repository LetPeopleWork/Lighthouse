# Slice 05 — Cancel all

**Story** #6322 (B1) / US-04 · **Job** `job-operator-stop-a-refresh-that-is-doing-harm`

## Goal

One click in the Task Manager stops the running update and clears everything queued. Scheduled updates
carry on afterwards.

## IN scope

- A System-Administrator-only endpoint (D9) and a Cancel all control at the right end of the Activity
  heading (NMD-11): no confirm dialog; "Cancelling…" and disabled while in flight; "Could not cancel. Try
  again." and the rows reverting on failure; hidden when no row could be cancelled.
- Every admitted non-delete key is cancelled through the existing per-key path (S14), on every replica. The
  message is delivered at most once (NMD-15).
- Coalesced follow-ups and held forecasts admitted before the call are dropped, and their rounds left, so
  other staged write-back still flushes (D8, S15).
- Deletes untouched.

## OUT of scope

- Pausing scheduled updates, showing held updates, server-side "Stopping…" for every admin (Epic #6321).
- Per-row Stop dropping follow-ups.

## Learning hypothesis

**Disproves that per-key cancel plus dropping follow-ups and holds empties the list.** If rows still
reappear after Cancel all on the restored DB with *Update All* pressed, some other source admits work
(for example domain-event triggers fired by a cancelled run), and the mechanism needs a server-side pause,
which is Epic #6321's territory.

## Acceptance criteria

AC-05.1 to AC-05.8. Key: **AC-05.2** (no follow-up or hold runs afterwards), **AC-05.3** (later triggers
run), **AC-05.5** (non-administrators are refused, administrators succeed; only a System Administrator sees
the control), **AC-05.6** (with several replicas, the other replica stops too), **AC-05.8** (the in-flight,
failure and hidden looks, no confirm dialog), **AC-05.7 (production data, manual gate)** *Update All* then
Cancel all on the restored dev instance; pass when the list holds only removals once the running row ends
and no cancelled row comes back before the next periodic tick re-admits stale entities.

## Dependencies

**Slice 02**, so that a running forecast stops promptly (D11). The DISTILL UI walk-through (UI-3 to UI-5).

## Effort / reference class

About 1 day. Reference class: epic-5511 slice 04/07 (per-row Stop and `Stopping…`).

## Pre-slice SPIKE

None.
