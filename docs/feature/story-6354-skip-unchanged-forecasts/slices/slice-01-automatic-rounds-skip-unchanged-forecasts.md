# Slice 01: Automatic rounds that change nothing skip the forecast (ADO #6354)

**Goal:** a periodic or event-triggered forecast whose inputs match the last completed run's fingerprint computes
nothing. The round's summary line says so, a short Forecast row lands in `RefreshLog`, and the two explicit "update
forecast" buttons still forecast.

## IN scope
- A per-Portfolio fingerprint of every forecast input (D1 in `feature-delta.md`), compared after the inputs are
  gathered and before the simulation, and stored when a run completes.
- Fail towards forecasting: no stored fingerprint, a failed or a cancelled last run all count as changed (D7).
- `ForecastController` `forecast/update/{id}` and `update-portfolios-for-team/{teamId}` bypass the skip.
- Summary-line wording for a skipped forecast (draft `forecast=skipped (nothing changed)`; DESIGN settles it), plus a
  `RefreshLog` Forecast row for the skipped run.
- A guard test naming every fingerprint member (D8).

## OUT of scope
- Bypass for Team or Portfolio refresh buttons and refresh-all (slice 02).
- The confirmed-now stamp on `lastUpdated` (slice 02).
- Coalescing (MD-4), seeding, partial forecasts.

## Learning hypothesis
- **Disproves** "the fingerprint captures every forecast input" if, on the dev instance (`:5169`, real history), any
  input change from the AC-1.2 list leaves the next background round skipping. It is also disproved if the
  reporter's next log shows a skipped round right after a change he made.
- **Confirms** it if every listed change forecasts on the next round, and most periodic rounds on an idle day log a
  skip with a forecast cost of a few seconds.

## Acceptance criteria
US-01 AC-1.1 to AC-1.7 in `feature-delta.md`.

## Dependencies
None. Builds on the existing `FetchFingerprint` pattern and `RefreshRoundSummaryLine`.

## Effort
About 1 day. Reference class: the FetchFingerprint introduction (a hashed member list, one comparison point, a guard
test), plus about ten acceptance cases. The expand-only migration for the stored fingerprint goes through
`CreateMigration`.

## Dogfood
On the dev instance, let the periodic refresh run twice with nothing changing remotely, and watch the second round's
log line and the Task Manager row. Then add a blackout and watch the next round forecast.
