# Slice 02 — Stop ends a running forecast and keeps the last one

**Story** Bug #6308 / US-02 · **Job** `job-operator-stop-a-refresh-that-is-doing-harm`

## Goal

Stopping a running forecast in the Task Manager ends it within seconds. The Features keep the dates they
had, in Lighthouse and in the tracker.

## IN scope

- The update's cancellation (`UpdateCancellationContext`, S3) reaches the simulation and is checked between
  runs (D4).
- The terminal state is `Cancelled`; the `RefreshLog` row records it (already done by `ForecastUpdater` once
  something throws).
- Proof that nothing is half-written: the persisted forecasts are unchanged and no forecast write-back is
  staged (S5, S6).

## OUT of scope

- Cancelling a manual (HTTP) forecast when the browser goes away. Server-side "Stopping…" for all admins
  (Epic #6321). Dropping a coalesced follow-up on Stop (per-row Stop keeps today's semantics; Cancel all is
  slice 05).

## Learning hypothesis

**Disproves that a check between runs is prompt enough.** One run of a low-throughput Feature can step tens
of thousands of days before slice 04. If the click-to-`Cancelled` time on the restored DB is still minutes,
the check has to move inside a run (a day-loop checkpoint), and slice 04's horizon becomes the real fix.

## Acceptance criteria

AC-02.1 to AC-02.7. Key: **AC-02.1** (no run starts after the cancel is seen, counted by an instrumented
draw stream), **AC-02.3** (persisted forecasts unchanged), **AC-02.4** (no forecast write staged; the round's
other writes still flush), **AC-02.7 (production data, manual gate)** click-to-`Cancelled` on the slowest
Portfolio of the restored dev instance; pass when each of 3 tries takes 5 s or less and the Features' dates
are unchanged (KPI-2).

## Dependencies

None mechanically. Touches the same loop as slice 01, so it lands after it.

## Effort / reference class

About 4h. Reference class: epic-5511 slice 04 (token reaching the connector's paging loop).

## Pre-slice SPIKE

None.
