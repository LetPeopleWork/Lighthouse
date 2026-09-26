# Slice 04 — The answer opens in a dialog: words first, every forecast next to its actual beneath

**Feature**: epic-4172-forecast-backtest-sweep · **ADO**: Story #6094 (child of Epic #4172) · **Story**: US-04
**Estimate**: ~7h · **Job**: `job-forecaster-check-the-forecast-against-what-happened`
**Depends on**: slices 01 + 02 (shipped, `9851b4ea3`) · **Decisions**: `feature-delta.md`, *Story #6094*, 6094-D1..D10

**Reference class**: the shipped `ForecastRealityCheck` card — same button, same request, same usage
event. This slice moves its answer into a dialog and replaces the band-row evidence with a table of the
numbers the response already carries.

## Goal

Maria Santos presses **"Run reality check"** on Team → Forecasts → Forecast Backtesting. A dialog opens at
once, fills in with one line per confidence level and the sampling-window sentence, and beneath them a
table of every check: each period's actual printed once, each forecast beside it with its miss in Work
Items and whether it held.

## IN scope

- **The dialog** (6094-D1, D8): opens immediately in an announced loading state; "Run again" inside it;
  closing and reopening re-runs; no second run while one is in flight; a failed request leaves the dialog
  open with a plain message and "Run again". `TeamForecastRealityCheckRun` once per answer received —
  unchanged.
- **The text result first** (6094-D4, D7 without the closeness clause): one line per level —
  *"85th: held 15 of 16 (should be about 14)"* plus the DES-16 reading; then the rule-A window sentence
  with its standing / not-tested / could-not-run clauses; the two findings; the denominator and
  non-comparability statement. Nothing behind a toggle or tooltip.
- **The table** (6094-D5): rows period × window (16 or 20), grouped by period (1 / 2 / 4 / 8 weeks), the
  actual and the period's dates once per group; columns 50th / 70th / 85th / 95th, headers carrying the
  `ForecastLevel` name and icon; the Team's own window labelled "your setting". Each evaluable cell:
  forecast value, signed miss in Work Items (`actual − forecast`), ✓ *held* / ✗ *did not hold*.
  Unevaluable rows: reason and numbers in words across the four columns.
- **Accessibility** (6094-D10): caption, scoped headers, row groups; focus trap, Escape, focus returns to
  the button; full-screen dialog and horizontally scrolling table on narrow screens, nothing dropped.
- **Retirement** (6094-D4): delete `RealityCheckEvidence` (incl. `NominalRateLines` as a component),
  `RealityCheckBandRow`, the "Show the evidence" toggle, `RealityCheckBandRow.test.ts`, the US-02 panel
  scenarios in `TeamForecastView.realityCheck.test.tsx`, and copy helpers only they used.
- **The Playwright walking skeleton**: rewrite the *"Forecast reality check"* step in
  `TeamsDetail.spec.ts` through `TeamDetailPage` — press the button, the dialog opens, the text result and
  the table are visible, Escape closes it. Demo scenario 0's first Team. Replace the card-scoped
  locators. Run locally before commit.

## OUT of scope

- Margin percentages, bands, shades, legend, Brown credit, the "within 10%" count — slice 05.
- Any backend change. The response already carries `forecastValue`, `actualCompleted` and `held` per cell.
- Any per-window summary, ordering or highlight (I-a); any control that writes a Team setting (I-b).
- The one-pager (slice 03, deferred).

## Learning hypothesis

**Disproves, if it fails**: that 64-80 cells grouped by period are readable on real history — the warning
Epic D2 made a ruling, and which the maintainer's ask turned into a question. If three real Teams on the
dev instance cannot be read in under a minute each, the fallback is fewer horizons shown at once, never a
per-window roll-up.

**Also disproves**: that moving the answer behind one click costs nothing. If the dialog's words are not
what Maria reads first, the order inside the dialog is wrong, not the dialog.

**Confirms, if it succeeds**: slice 05 is shading and one clause on a table that already works.

## Production / demo data

Dogfood on the dev instance's real history (three Teams; write the finding into this brief). E2E on demo
scenario 0. Worked example for tests: Ocean Explorer, last 8 weeks, actual 42; 30-day row 48 / 40 / 36 /
31 → −6 ✗, +2 ✓, +6 ✓, +11 ✓.

## Acceptance criteria

AC-4.1 through AC-4.9, in `feature-delta.md` under *Story #6094 / US-04*.

## Notes for the implementer

- Reuse `realityCheckCopy.ts` (`windowVerdict`, `levelReadingCopy`, `findings`, `denominatorStatement`,
  `whyChecksCouldNotRun`); the level line changes "about N expected" to "should be about N".
- Terminology via `getTerm` — Team, Work Item. "Throughput" in no label.
- Assert on the filled result in E2E, never on the spinner.
