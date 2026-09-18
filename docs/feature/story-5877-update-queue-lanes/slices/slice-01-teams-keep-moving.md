# Slice 01 — Teams keep moving while a Portfolio refresh runs

**Story** #5877 / US-01 · **Job** `job-operator-keep-teams-moving-while-a-portfolio-refreshes`

## Goal

A slow Portfolio refresh stops being a stopped instance: every update type gets its own lane, and a
queued row says what is holding *its* lane rather than naming whatever happened to be running.

## IN scope

- **Precursor commit — `WriteBackRound` survives two executions at once.** `Stage`, `TakeStaged`,
  `ReportRefresh`, `ReportForecast` and `TakeSummary` mutate plain `Dictionary` fields with no
  synchronisation (`WriteBackRound.cs:15-17,79-107`). A Portfolio refresh and the Forecast it triggers
  already share one round; they are safe only because they run one after the other. This lands **before**
  the second consumer exists, not after.
- **Per-type lanes.** `Team`, `Features`, `Forecasts` each get an independent consumer; `TeamDelete` and
  `PortfolioDelete` share their entity type's lane (D6). Within a type, work stays serial (D3).
- **`waitingBehind` corrected.** `UpdateController.cs:61` picks `FirstOrDefault(… InProgress)` and calls
  it the lane holder. It becomes the holder of the asking row's own lane, and absent when that lane is
  free (D4).
- **Shutdown drain across lanes.** `DrainAsync` returns only once every lane has finished or the
  shutdown timeout has elapsed.

## OUT of scope

- The wall-time bound — slice 02.
- Two refreshes of the same type in parallel (D3).
- Per-connection rather than per-type granularity (handoff item 4).
- Priority or preemption between lanes.
- The orphaned-feature cleanup path — its own Bug.
- Persisting queue wait in `RefreshLog` (handoff item 5).

## Learning hypothesis

**Disproves that head-of-line blocking is the whole of "hung".**

If lanes land and a Team refresh still stalls behind a held-open Portfolio refresh, the cause is
somewhere else — the per-key execution lock, the SQLite write path, the connector's own HTTP
connection limit — and per-type lanes were the wrong fix, cheaply. That failure would be worth more
than the success, because the reported symptom would still be unexplained and the remaining candidates
are all narrower.

If it succeeds, the reported complaint is answered and slice 02 changes from a rescue to a tidy-up —
which is why it is sequenced second and may legitimately be dropped.

## Acceptance criteria

AC-01.1 through AC-01.8 in `feature-delta.md` — 8 ACs. The four carrying the risk:

- **AC-01.1** — the Portfolio refresh must be **held open**, not merely slow. Starvation is the failure
  mode, and a test that waits for a slow refresh to finish passes against the bug.
- **AC-01.3** — asserted over **repeated reads**. `FirstOrDefault` over a concurrent store is
  non-deterministic, so one correct read proves nothing.
- **AC-01.5** — the round's staged set after two concurrent stagings equals the union. This is the
  precursor commit's only guard, and it is stated as behaviour rather than as "a lock exists".
- **AC-01.8** — **production data.** Two overlapping `RefreshLog` rows on Tenant Zero against real
  connections. Doubles prove the lanes exist; only this proves they survive a real connector, a real
  database and a real write-back round.

## Dependencies

- #5511 slices 01–08, all pushed. Every surface this touches was built by them.
- Nothing else in flight touches `UpdateQueueService`, `WriteBackRound` or `UpdateController`.

## Effort estimate

**~6h of crafter dispatch**, including the precursor commit. Two classes changed, one read-model line,
one frontend assertion.

## Reference class

`epic-5511-task-manager` slice 04 — the last change to `UpdateQueueService`'s execution path, and the
source of the cancellation evidence this story leans on. It ran to estimate.

## Pre-slice SPIKE

**None.** The three questions a spike would ask are already answered with citations: cancellation reach
(S5, measured), the execution lock's per-key scope (S6), and the DI scoping of each execution (S7).

One thing to **check before starting, not spike**: on SQLite, two lanes saving at once serialise at the
file under WAL with a `busy_timeout` of 10 000 ms (`DatabaseConfigurator.cs:71-73`). That is a ceiling,
not a guarantee, and the reporting user is on SQLite. Run one two-lane save against a SQLite instance
early enough that the answer can still change the design.
