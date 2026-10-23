# Slice 01: Automatic rounds that change nothing skip the forecast; every button still recomputes (ADO #6354)

**Goal:** a periodic or event-triggered forecast whose inputs match what the last completed forecast of that Portfolio
recorded computes nothing. The round's summary line says so, a short Forecast row marked skipped lands in the refresh
log, and Refresh History shows the skip without letting it pull the forecast duration figures down. Every button a
person presses makes the next forecast compute, and every start of the app does too.

*(Re-cut at the DISTILL review gate: all six button forgets and the Refresh History marker moved here, so this slice
ships alone with nothing a person can miss. Slice 02 keeps only the confirmed-now stamp.)*

## IN scope
- A per-Portfolio fingerprint of what the simulation is handed (DDD-1 in `feature-delta.md`), compared after the inputs
  are gathered and before the simulation, and recorded by compare-and-set when a run completes (DDD-3).
- Fail towards forecasting: nothing recorded, a forget token, a failed or a stopped last run, and an exception while
  fingerprinting all mean compute (D7, DDD-4, DDD-5).
- All six buttons forget the fingerprint of every Portfolio they touch, before triggering: the two "Update forecast"
  buttons (`forecast/update/{id}`, `update-portfolios-for-team/{teamId}`), which guarantee a recompute, and the four
  refresh buttons (Team, all Teams, Portfolio, all Portfolios), which recompute unless a forecast of that Portfolio ran
  in between (MD-10).
- Every start of the app forgets every fingerprint (DDD-14).
- The Feature read narrowed at query level to the Portfolio's Teams, and a Debug line timing gather and publish (DDD-2).
- Summary line ` | forecast=skipped after {N}ms (inputs unchanged)`; a round reads skipped only if all its forecasts
  skipped (DDD-8).
- `RefreshLogs.Skipped` (database default `false`) and `Portfolios.ForecastInputFingerprint`, expand-only.
- Refresh History: the Last Run marker `<time> · skipped (nothing changed)`, the count line
  `Skipped (nothing changed): N of the last M runs`, and every duration figure over computed runs only, with `—` when
  none computed and a gap in the duration line at each skip (MD-9, MD-11, MD-12).
- A guard test naming every fingerprint member (D8).

## OUT of scope
- The confirmed-now stamp on Feature `lastUpdated` (slice 02).
- Any Task Manager change (MD-8).
- Coalescing (MD-4), seeding, partial forecasts.

## Learning hypothesis
- **Disproves** "the fingerprint captures every forecast input" if, on the dev instance (`:5169`, real history), any
  input change from the AC-1.2 list leaves the next background round skipping. It is also disproved if the
  reporter's next log shows a skipped round right after a change he made.
- **Confirms** it if every listed change forecasts on the next round, and most periodic rounds on an idle day log a
  skip with a forecast cost of a few seconds.

## Acceptance criteria
US-01 AC-1.1 to AC-1.7 and US-02 AC-2.1 in `feature-delta.md`.

## Dependencies
None. Builds on the existing `FetchFingerprint` pattern, `RefreshRoundSummaryLine` and the `ISeeder` start-up hook.

## Effort
About 1½ days. Reference class: the FetchFingerprint introduction (a hashed member list, one comparison point, a guard
test), plus the six one-line forgets, a seeder, and the Refresh History figures. The migration goes through
`CreateMigration`, followed by the ordered migration build pass in the DISTILL pre-requisites. The seeder registration
touches `Program.cs`, so that push runs every live-connector category.

## Dogfood
On the dev instance, let the periodic refresh run twice with nothing changing remotely. Read the second round's log
line, then open Settings → System Info → Refresh History and check the Last Run marker, the count line, and that the
average duration still reads like a computed forecast. Then add a blackout and watch the next round forecast, and press
"Update forecast" on a quiet Portfolio and watch it compute.
