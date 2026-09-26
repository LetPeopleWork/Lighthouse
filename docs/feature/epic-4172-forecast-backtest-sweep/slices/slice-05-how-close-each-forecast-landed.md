# Slice 05 — How close each forecast landed, on Nick Brown's scale

**Feature**: epic-4172-forecast-backtest-sweep · **ADO**: Story #6094 (child of Epic #4172) · **Story**: US-05
**Estimate**: ~5h · **Job**: `job-forecaster-check-the-forecast-against-what-happened`
**Depends on**: slice 04 (the dialog and the table) · **Decisions**: 6094-D2, D3, D6, D7, D10

**Reference class**: Brown, *The Full Monte*, ASOS Tech Blog —
<https://medium.com/asos-techblog/the-full-monte-901d721b8532> (verified at source by the maintainer
2026-09-26): *"correct (i.e. the team completed the exact OR more than number of items)"*, margin
*"within +/- 10% ... of the actual result"*, bands ≤10% / 10-25% / >25%, shade by closeness.

## Goal

In the dialog, every forecast cell is graded — green when it held, red when it did not, darker the closer
it landed — with the percentage of the actual beside the Work Item miss, and each level's line says how
close it usually landed, so Maria can tell a tight 85th from a padded one.

## IN scope

- **The grading rule** (6094-D2): held = `actual >= forecast`; margin = `|actual − forecast| / actual`,
  banded on the unrounded value (≤10%, >10-25%, >25%, symmetric). Actual 0: no percentage; forecast > 0
  did not hold (Work Items only, dark red), forecast 0 held exactly. Defined once — where is DESIGN's call.
- **Six grades** (6094-D3) on a scale distinct from `ForecastLevel` colours, legible in both themes; every
  cell also shows its percentage in text and ✓ / ✗ with its word. Unevaluable rows take no grade colour.
- **A legend** naming the six grades and the not-checked state in words.
- **Each level's line** gains *"within 10% in N"* and, only when more than half of that level's graded
  checks share one band and direction, *"Usually low by more than a quarter."* (or the matching phrase).
- **The credit**: Brown's correct / incorrect grading and closeness shading credited to him; the
  always-held-is-under-forecasting reading and the 95th level stated as this product's, in the same place.

## OUT of scope

- Any summary by window (I-a) — the new counts are per level only.
- A minimum actual below which the percentage is hidden (the Work Item miss is the guard).
- Brown's 12-week period and his history ranges; the Epic's ladder and horizons are not reopened.
- A new usage-data event or property.

## Learning hypothesis

**Disproves, if it fails**: that shading by closeness makes over-caution visible (S2, 12.6) without making
small periods look worse than they are (S3). Watch two things on real Teams: whether the 95th column reads
as "light green = padded" rather than "green = good", and whether a 1-week row with a small actual reads as
a disaster despite its "−1".

**Confirms, if it succeeds**: the check answers "how far off" in the method's own vocabulary, and the
launch post can show the table with Brown credited and our departures stated.

## Production / demo data

Dogfood on the same three dev-instance Teams as slice 04. Worked examples for tests:

| Case | Actual | Forecast | Shows | Grade |
|---|---|---|---|---|
| Ocean Explorer 8 wk, 70th | 42 | 40 | +2, 5% ✓ | held, within 10% |
| Ocean Explorer 8 wk, 85th | 42 | 36 | +6, 14% ✓ | held, 10-25% |
| Ocean Explorer 8 wk, 95th | 42 | 31 | +11, 26% ✓ | held, more than 25% |
| Ocean Explorer 8 wk, 50th | 42 | 48 | −6, 14% ✗ | did not hold, 10-25% |
| Coastal Survey 1 wk, 50th | 3 | 4 | −1, 33% ✗ | did not hold, more than 25% |
| Harbour Pilots 1 wk, 95th | 0 | 0 | 0 ✓ | held, exact |
| Harbour Pilots 1 wk, 50th | 0 | 2 | −2 ✗, no % | did not hold, dark red |

## Acceptance criteria

AC-5.1 through AC-5.6, in `feature-delta.md` under *Story #6094 / US-05*.

## Notes for the implementer

- The displayed whole-number percentage must never contradict its band (10.4% shows in 10-25%) —
  DESIGN fixes the rounding.
- Screenshots at Epic finalization show this table, one per theme.
