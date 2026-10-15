# fix-pbc-baseline-viewer-day

## Wave: DELIVER / [REF] Problem

A Process Behaviour Chart (PBC) on a Team or Portfolio with no configured baseline showed
"Baseline end date must not be in the future." instead of the chart whenever the viewer's
calendar day was ahead of the instance's day — every night, for as long as the zone offset
(Europe/Zurich against a UTC instance: 22:00–24:00 UTC). The Throughput PBC then also lost its
"Use filtered Throughput" toggle, which only renders on a Ready chart. CI caught it in
`ForecastFilter.spec.ts` (runs 38648741423 / 38648717256, 2026-10-14 ~22:41 UTC).

## Wave: DELIVER / [REF] Root cause

With no baseline configured, the PBC builders reuse the display window as the baseline. The
display window ends on the viewer's local today (the frontend sends `YYYY-MM-DD` in the
viewer's calendar), but the validator's "end must not be in the future" rule — written for a
baseline someone picked in settings — is anchored on the instance's day (`Clock.Today`). The
resolve-and-validate block was copied four times (three builders in `BaseMetricsService`, Feature
Size in `PortfolioMetricsService`), and no test had the viewer's day ahead of the instance's.

The baseline picker in settings had the mirror defect: its latest day was the UTC day, while
the server judges a picked end against the instance's day, which the browser cannot know.

## Wave: DELIVER / [REF] Maintainer decisions (2026-10-15)

- The display window used as a baseline skips only the future-end rule; every other rule stays.
  One helper replaces the four copies.
- A configured baseline may end at most one day after the instance's day (on save and at
  render), so any viewer can pick their own today; two or more days ahead is still refused. The
  picker's latest day becomes the viewer's local today. The picked value keeps travelling as UTC
  midnight of the chosen day — that encoding is the wire contract and is unchanged.
- CI keeps running the browser in Europe/Zurich against a UTC backend on purpose; the comment in
  `playwright.config.ts` that claimed otherwise is corrected.
- No sketch: the picker shows the same field, label and layout.

## Wave: DELIVER / [REF] Plan

See `deliver/roadmap.json`: 01-01 validator, 01-02 metrics services helper + regression tests on
every PBC kind (Team and Portfolio), 02-01 picker + E2E config comment.
