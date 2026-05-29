# Slice 06 — Auto-run backtesting on valid input

Story: US-5077-06 | job: job-see-forecast-update-live | Should Have | PARALLEL track

## Goal
`BacktestForecaster` runs automatically when start/end + historical window are valid; the
"Run Backtest" button is gone. Reuses the shipped auto-run pattern.

## IN scope
- Backtest auto-run via the parent's orchestration, tuned for the richer input set.
- Remove the "Run Backtest" `ActionButton` (`BacktestForecaster.tsx` ~line 361).
- Mode toggle (rolling ↔ date-range) mid-edit must NOT fire a half-configured run.
- Debounce + stale-guard; on-mount suppression.

## OUT of scope
- None (last slice).

## Learning hypothesis
**Confirms** that the backtest's richer input set (rolling vs date-range modes, historical window)
can be driven by the same debounced valid-input auto-run without firing on every keystroke or on a
mid-edit mode toggle.
**Disproves** the shared-debounce assumption if the richer input set needs a different debounce /
validity-stability window than the simpler forecasts — then tune per-surface.

## Acceptance criteria
- [ ] Valid backtest input change auto-runs (debounced), no "Run Backtest" button.
- [ ] Mode toggle mid-edit fires no run until the chosen mode's inputs are valid and stable.
- [ ] No run on mount (hasInteractedRef).
- [ ] Stale runs discarded; only the latest run's result shown (requestSeqRef).

## Dependencies
None on the settings track. Depends only on the already-shipped forecast pattern.

## Effort estimate
~1 day.

## Reference class
`TeamForecastView.tsx` auto-run orchestration (shipped); `BacktestForecaster.tsx` is the surface
(richer inputs — mode toggle + historical window). Slice 5 warms up the pattern-reuse first.

## Cross-cutting
- RBAC: N/A — backtest running has no write-permission gate today; parity = no new gate.
- Clients: N/A (reuses `runBacktest` POST unchanged).
- Website: N/A.

## Dogfood moment
On the Atlas team (demo data), switch rolling ↔ date-range and adjust the window; backtest
recomputes live, stale runs never overwrite fresh.
