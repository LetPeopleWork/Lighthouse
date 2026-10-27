# Bug 6424 — Delivery forecasts don't match with blackout rules

## Wave: DELIVER / [REF] Maintainer decisions

- Root cause in `rca.md` confirmed (2026-10-27).
- The write-back window bug found during the RCA is fixed in this bug, in its own commit.
- Only recurring rules are affected; one-off blackout periods are returned whole and already matched.
- Rigor profile `standard` (crafter Sonnet). **Deviation:** the adversarial review ran on **Opus**, not
  the profile's Haiku, at the maintainer's request, because forecast correctness is critical.
- After review → mutation testing → rebase onto `origin/main` → push → `/clean-ci` (authorised 2026-10-27).

## Wave: DELIVER / [REF] Implementation summary

A Delivery's percentile dates, the per-Feature dates in its row, its daily snapshots and the Jira Release
forecast are projected against blackout days fetched to the latest of: the target date + 14 days, each
Feature's 95th-percentile reach, and each Delivery's own (cross-Team) 95th-percentile reach. Before this
fix, the window stopped at the target date + 14 days, so recurring weekends beyond it counted as working
days. A forecast date written back to a tracker field is sized from the day being written, not from
`today + N` calendar days.

## Wave: DELIVER / [REF] Commits

- `1d96b3f7a` refactor(forecast): move FeatureForecastWindow where services can use it
- `8d1eec3de` fix(deliveries): project Delivery dates against blackout days as far out as the forecast reaches
- `eff94faa6` fix(writeback): project written-back forecast dates against blackout days as far out as the forecast reaches
- `785bfd57c` fix(deliveries): cover a multi-Team Delivery's own forecast when reading blackout days (Opus review H1, H2)
- `97ea0d194` fix(writeback): size the blackout window by the percentile being written (review M2)
- `b65dba5a6` test(deliveries): assert blackout-window tests against independent dates (review M1, L1)
- mutation kill-round tests: see `mutation/results.md`

## Wave: DELIVER / [REF] Quality gates

- Opus review: NEEDS_REVISION (H1 multi-Team Delivery reach, H2 a test that could not fail, M1 a
  same-reduction oracle, M2 the write-back window, L1 the overload type), then APPROVED after step 01-03.
- Accepted risk (MEDIUM, performance): a Delivery whose own 95th is beyond the horizon makes the
  active-Delivery list read ~10 years of blackout days, the same cost the Feature list already pays. The
  fix, if it shows: a `HashSet<DateOnly>` lookup in `BlackoutDaysExtensions`.
- Pre-existing, out of scope: the window's ×2 density assumption fails for rules blocking more than
  half of all days; this is shared with the Feature endpoints.
- Filtered backend suite green (11,532 passed, 0 failed, 10 skipped); `dotnet build` 0 warnings.
- Mutation: 86.24 % on the first run, 93.46 % after the kill round (`mutation/results.md`).
- DES integrity: all 3 steps have complete traces.
