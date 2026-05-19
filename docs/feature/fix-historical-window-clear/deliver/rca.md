# RCA — Forecast Backtesting: Historical Window (Days) forces leading "1"

**ADO**: [Bug #5039](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5039) (Active, Release Notes)
**Reporter**: Gabor

## Symptom

In Forecast Backtesting on the Team detail page, the *Historical Window (Days)* input cannot be cleared and retyped cleanly. To enter `56`, the user has to type `156` and then delete the leading `1`. The cleared field immediately snaps back to `1`.

## Root cause

`Lighthouse.Frontend/src/pages/Teams/Detail/BacktestForecaster.tsx:281` clamps inside `onChange`:

```ts
onChange={(e) =>
  setHistoricalWindowDays(
    Math.max(1, Math.min(365, Number(e.target.value))),
  )
}
```

When the field is cleared, `Number("") === 0`, then `Math.max(1, 0) === 1` immediately writes `1` back into the controlled-input state, and React paints `"1"` into the field before the next keystroke is processed.

### Two coupled causes

1. **State conflates draft text with committed value.** `historicalWindowDays` is typed `number` (line 73, default `30`), so the field state must always satisfy the business invariant — forcing per-keystroke clamping.
2. **Existing tests lock in the bug.** `BacktestForecaster.test.tsx:207-212` asserts that typing `-5` clamps to `1` on the change event; `:218-223` does the same for `500 → 365`. The "clear field, type new number" UX path is untested. These tests must be rewritten as part of the fix.

## Files affected

- `Lighthouse.Frontend/src/pages/Teams/Detail/BacktestForecaster.tsx` — lines 73, 196, 273-287.
- `Lighthouse.Frontend/src/pages/Teams/Detail/BacktestForecaster.test.tsx` — lines 205-224 (rewrite + add regression test).

## Proposed fix

1. Change state to `useState<number | "">(30)` so the field can be transiently empty.
2. Replace per-keystroke clamp with a raw assignment in `onChange`:
   ```ts
   onChange={(e) => {
     const raw = e.target.value;
     setHistoricalWindowDays(raw === "" ? "" : Number(raw));
   }}
   ```
3. Add `onBlur` clamp:
   ```ts
   onBlur={() => {
     const n = typeof historicalWindowDays === "number" ? historicalWindowDays : 30;
     setHistoricalWindowDays(Math.max(1, Math.min(365, Number.isFinite(n) ? n : 30)));
   }}
   ```
4. Guard `handleRunBacktest` (line 196): coerce `""` to fallback `30` (or block submit) before computing dates.
5. Keep `slotProps.htmlInput.min/max` as the safety net.

## Risk

- **Blast radius**: low. `historicalWindowDays` state is local to `BacktestForecaster`; no other consumer.
- **Test rewrite required**: yes — two existing tests assert the buggy clamp-on-change behavior and must be rewritten as part of this fix.
- **Submit-time invariants preserved** by `onBlur` clamp + HTML `min`/`max` + `handleRunBacktest` guard.

## Acceptance criteria

1. Clearing the field and typing `5` produces `5` (not `15`) in the input.
2. Typing `56` produces `56` (not `156`).
3. Blurring an out-of-range value clamps to `[1, 365]`.
4. Blurring an empty field falls back to `30`.
5. Running the backtest with any valid input still succeeds end-to-end (no submit-time regression).
6. All existing tests pass (with the two clamp-on-change tests rewritten to assert clamp-on-blur).

## Out of scope

Other numeric inputs in the codebase that share related but distinct UX issues (`WipSettingDialog`, `ThroughputQuickSetting`, `PortfolioFeatureWipQuickSetting`, `AdvancedInputs`, `FeatureSizeComponent`, the NaN-family in `FlowMetricsConfigurationComponent`, `RefreshSettingUpdater`, `ForecastSettingsComponent`). User will handle these separately.
