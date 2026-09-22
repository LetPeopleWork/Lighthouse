# Slice 02 — The evidence you can look at

**Feature**: epic-4172-forecast-backtest-sweep · **ADO**: to create under Epic #4172 · **Story**: US-02
**Estimate**: ~6h · **Job**: `job-forecaster-check-the-forecast-against-what-happened`
**Depends on**: slice 01 (the response shape)

**Reference class**: the shipped `BacktestForecaster` result display already renders one forecast's four
percentiles against one actual. This slice renders sixteen of those, grouped four to a panel.

## Goal

A forecaster who is about to repeat the verdict to someone who will push back can expand it and see the
sixteen checks themselves — each one's forecast range with the Team's actual marked against it — plus
four lines saying how often each confidence level was beaten and how often it should have been.

## Why this is second and not fourth

M1 scores **3 out of 5 on HONESTY**, the criterion carrying the most weight in the DIVERGE matrix, and a
single point on that criterion moves it from first place to third. The evidence view is the mitigation
that earns the point. `recommendation.md` §3 states the condition plainly: if DISCUSS will not commit to
the three honesty requirements, the recommendation changes to R1. Two of the three live in slice 01; this
one is the third, and deferring it past second is how "evidence on request" quietly becomes "no evidence".

## IN scope

- **Four panels, one per sampling window**, titled by window length. No fifth panel and no
  confidence-level control (D2).
- **One row per horizon inside each panel.** The forecast is drawn as a band spanning the four confidence
  levels, with a single mark at the Team's actual completed count. **The mark's position within the band
  is what reports which levels held** — there is no separate per-level rendering, because a Monte Carlo
  forecast read at four places is one distribution, not four forecasts.
- **The unevaluable row state**, per ADR-194: words where the band would be, naming the reason and the
  numbers (*"3 days with completed Work Items, 5 needed"*). Visually distinct from every evaluable outcome
  and never blank. A panel whose rows are all unevaluable still renders — an omitted panel reads as "this
  window was fine".
- **The nominal-rate lines**, one per confidence level: beaten-count over evaluable checks, the count its
  nominal rate expects, and a plain reading of the two. A level never beaten is called over-forecasting,
  not excellent.
- The denominator and non-comparability copy stays on screen whether or not the evidence is expanded.

## OUT of scope

- Any change to slice 01's computation. This slice consumes the response and computes nothing.
- Apply. Slice 03.
- The export. Slice 04.
- **Any new charting dependency.** `@mui/x-charts` 9.0.1 is what is installed; there is no
  `@mui/x-charts-pro` and therefore no Heatmap component — which is moot, because a heatmap is forbidden
  on its own merits (below).
- A matrix or a ranked list. A matrix is a coordinate system and a ranked list is a league table, and D6
  says these sixteen cells are neither.

## Learning hypothesis

**Disproves, if it fails**: D2's claim that the confidence dimension costs zero panels and zero controls.
Four bands of four levels each, stacked four to a panel, is a dense picture and the density argument is
made on paper here. If real data proves it unreadable, the fallback is **not** a per-level panel explosion
— it is showing fewer horizons per panel, because the horizon axis is the one D6 already says cannot be
ranked anyway.

**Also disproves**: the assumption that a Team's cells are mostly evaluable. If most real Teams produce
mostly unevaluable rows, the panels are mostly words, and the feature's honest shape is closer to R1
("your setting, on trial") than to a sweep. That would be a finding worth recording rather than designing
around.

**Confirms, if it succeeds**: the HONESTY mitigation is built and the recommendation's conditional in
`recommendation.md` §3 is discharged.

## Acceptance criteria

AC-2.1 through AC-2.6, in `feature-delta.md` under US-02.

## Notes for the implementer

- **ADR-194 is the governing precedent and is not amended.** Its finding: on this product's charts a blank
  region already means "too little history", so an unevaluable result and a calm one must never render
  alike, and "leave it empty" is the option that ADR already ruled out. It is also the precedent for
  refusing a rendering whose grammar overclaims — it retired a heatmap-like ladder for exactly that
  reason, which is the direct argument against a matrix here.
- Terminology: every configurable term renders from the instance's own vocabulary. "Work Item" is
  configurable, "throughput" is configurable. The words "Epic", "Initiative" and "Story" never appear.
- Dogfood against this project's own Lighthouse instance with real history. A synthetic Team with a tidy
  distribution will not tell you whether the band is readable.
