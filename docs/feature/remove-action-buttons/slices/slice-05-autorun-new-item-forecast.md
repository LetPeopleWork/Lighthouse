# Slice 05 — Auto-run the new-item-creation forecast on valid input

Story: US-5077-05 | job: job-see-forecast-update-live | Should Have | PARALLEL track

## Goal
`NewItemForecaster` recomputes automatically when its inputs (from/to/target dates + ≥1 work-item
type) are valid; the "Forecast" button is gone. Reuses the shipped `TeamForecastView` auto-run pattern.

## IN scope
- New-item auto-run via the parent's existing orchestration (`hasInteractedRef` no-run-on-mount +
  `requestSeqRef` stale-guard + `DEBOUNCE_MS` debounce + run-only-when-valid).
- Remove the "Forecast" `ActionButton` (`NewItemForecaster.tsx` ~line 197).
- On-mount suppression; stale-guard; incomplete-inputs → no run + clear prior result.

## OUT of scope
- Backtest (Slice 6).

## Learning hypothesis
**Confirms** that lifting `NewItemForecaster`'s inputs into the parent's existing auto-run gives the
same fluid feel as the How Many/When forecast, with no on-mount run and no stale-result flicker.
**Disproves** the lift-into-parent approach if the new-item input set resists the parent's
orchestration (e.g. work-item-type selection needs a separate validity gate) — then replicate the
pattern locally rather than lift.

## Acceptance criteria
- [ ] Valid new-item input change auto-runs (debounced), no "Forecast" button.
- [ ] No run on mount (hasInteractedRef); no result until an input changes.
- [ ] Incomplete inputs → no run; prior result cleared.
- [ ] Stale responses discarded; only the latest run's result shown (requestSeqRef).

## Dependencies
None on the settings track. Depends only on the already-shipped forecast pattern.

## Effort estimate
~1 day.

## Reference class
`TeamForecastView.tsx` How Many/When auto-run (lines 69-72, 129-196) — the exact shipped pattern;
`ManualForecaster.tsx` as the dumb-controlled-child model; `NewItemForecaster.tsx` is the surface.

## Cross-cutting
- RBAC: N/A — forecast running has no write-permission gate today; parity = no new gate.
- Clients: N/A (reuses `runItemPrediction` POST unchanged).
- Website: N/A.

## Dogfood moment
On the Atlas team forecast page (demo data), widen the historical window and watch the new-item
forecast recompute live.
