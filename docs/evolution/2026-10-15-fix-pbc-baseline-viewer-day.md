# Fix — a Process Behaviour Chart never calls the viewer's own today "the future"

Delivered 2026-10-15. Found by CI, not by a user: `ForecastFilter.spec.ts` failed on every run started
between 22:00 and 24:00 UTC (runs 38648741423 and 38648717256), because the E2E browser runs in
Europe/Zurich against a UTC backend.

## Summary

A Team or Portfolio with no Process Behaviour Chart baseline showed *"Baseline end date must not be in
the future."* on every PBC instead of the chart, for as long each night as the viewer's calendar day was
ahead of the instance's. The Throughput PBC also lost its *Use filtered Throughput* toggle, which only
renders on a ready chart. The baseline picker in settings had the mirror defect: its latest pickable
day was UTC's, not the viewer's.

## Root cause

With no baseline configured, the PBC builders reuse the display window as the baseline. The display
window ends on the viewer's local today — the browser sends `YYYY-MM-DD` in its own calendar — while the
validator's "end must not be in the future" rule, written for a baseline someone picks in settings, is
anchored on the instance's day. Two earlier decisions are each right on their own and disagree here: the
instance's day is "today" on the server, and the viewer's local day is the one the dashboards show. The
resolve-and-validate block had been copied into four builders, and no test had the viewer's day ahead of
the instance's.

## What changed

- `BaselineValidationService.ValidateDisplayWindowAsBaseline` runs every baseline rule except the
  future-end one (end after start, at least 14 days, start inside the cutoff window still apply).
- A configured baseline may end one day after the instance's day, on save and when drawn; two or more
  days ahead is still refused. The browser cannot know the instance's day, and one day covers a viewer
  whose zone is up to a day ahead of the instance's.
- `BaseMetricsService.ResolveBaseline` picks the rule (configured vs display window) and
  `InvalidBaselineChart` builds the refusal; the three builders and Portfolio's Feature Size PBC use
  both instead of four copies.
- The *PBC Baseline End* picker's latest day is the viewer's local today. The picked value still
  travels as UTC midnight of the chosen day — the contract the backend reads.
- `playwright.config.ts` now says the CI zone mismatch (browser Zurich, backend UTC) is deliberate.

## Decisions

| Decision | Why |
| --- | --- |
| Skip only the future-end rule for the display window | It is the viewer's own calendar, not user input; every other rule still protects the limits. |
| One day of tolerance for a configured baseline, not "skip" | A picked end two or more days out is a real mistake worth refusing; one day is what a viewer's zone can put them ahead. Zones 25–26 h apart (UTC−12 / UTC+14) are not covered — accepted. |
| Keep the CI time-zone mismatch | It is what caught this; setting `Lighthouse__TimeZone` in CI would hide the class of bug from CI while real instances (chart default UTC) still have it. |
| Change only the picker's `max` | The value encoding is the wire contract; the start picker's UTC-based limit errs on the safe side and was left. |

## Commits

`b6b031a5d` validator · `f6ed22110` one helper + regression tests on every PBC kind (Team and
Portfolio) · `7c62eaeb3` picker · `ab104c4a2` refactor + comment corrections after review ·
`0721cc90c` picker test names its day, E2E comment trimmed · `2c82d62d0` one-bound baseline refused
(mutation kill).

## Quality gates

- Backend suite (connector categories excluded): 10 025 passed, 0 failed. Frontend: 481 files / 7 699
  tests passed; `pnpm build` clean.
- Adversarial review: NEEDS_REVISION → all findings fixed in one pass (a false "at most one day ahead"
  claim in a comment, two bug numbers in comments, duplicated invalid-chart construction, a circular
  test oracle, a missing two-days-ahead unit case). An ArchUnit pin counting places that build a
  not-ready chart moved 6 → 3 because four copies became one; the rule it guards still checks the helper.
- Mutation: backend 90.8 % on the changed regions (one real survivor killed); frontend 0 mutants on the
  one changed line, which the regression test pins. See `docs/feature/fix-pbc-baseline-viewer-day/mutation/results.md`.
- DES integrity: all 3 steps have complete traces.

## Follow-ups

- `docs/teams/edit.md` (and the Portfolio counterpart) still say Lighthouse shows no PBC without a
  baseline; it has fallen back to the display window since February 2026. Raised with the maintainer.
