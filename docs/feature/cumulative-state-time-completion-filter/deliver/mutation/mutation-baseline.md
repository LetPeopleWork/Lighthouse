# Mutation Baseline — cumulative-state-time-completion-filter (ADO #5144)

Strategy: per-feature (CLAUDE.md), gate ≥ 80% kill. Frontend Stryker (`@stryker-mutator` 9.6).

Config: `stryker.config.cumulative-state-time-completion-filter.mjs` + `vitest.stryker.cumulative-state-time-completion-filter.config.ts`
Tests exercised: `CumulativeStateTimeChart.test.tsx`, `BaseMetricsView.test.tsx`.

## Result (behavioral surface)

**92.86% kill (26 killed / 28, 2 survived)** — passes the 80% gate.

| File | Score | Killed | Survived |
|---|---|---|---|
| BaseMetricsView.tsx (wiring 927-933) | 100.00% | 3 | 0 |
| CumulativeStateTimeChart.tsx (toggle logic) | 92.00% | 23 | 2 |

Mutated ranges = this feature's behavioral surface only: the `completionClasses` memo + `useTypesVisibility` hook (91-102), the series-filter predicate (178-184), the `completionFilterEnabled &&` chip gate (202), the two `LegendChip` `visible`/`onToggle` props (207-208, 213-214), the module constants (28-29), and the host wiring `completionFilterEnabled={selectedItemIds.length === 0}` (BaseMetricsView 927-933).

## Surviving mutants — both equivalent

1. `CumulativeStateTimeChart.tsx:29` `NO_COMPLETION_CLASSES = []` → `["Stryker was here"]`.
   Equivalent: this array is fed to `useTypesVisibility` only when the filter is **disabled**, and while disabled the series filter short-circuits on `!completionFilterEnabled` and no chips render — so the array's *content* is never observable. It exists only as a stable, non-`["Completed","Ongoing"]` reference that resets visibility on re-enable; emptiness is not a behavioral contract.
2. `CumulativeStateTimeChart.tsx:93` `completionFilterEnabled = false` (default) → `true`.
   Near-equivalent: every call site (BaseMetricsView, and all tests) passes the prop explicitly, so the default is never reached. The off-by-default contract is now additionally pinned by the test "offers no completion chips unless the filter is explicitly enabled" (renders the chart with the prop omitted and asserts no chips), added after this run; a re-run would kill this mutant.

## Note on the earlier full-render run

An initial run mutating the entire render block (159-218) scored 70% because it included ~14 pure-presentational mutants (MUI `sx` layout values — `height:"100%"`, `justifyContent`, `alignItems`, `minHeight`, `mb`, object-literal `sx→{}` — and series `label`/`color`/`stack` string literals). These are equivalent/not-worth-asserting: pinning exact `sx` values is brittle test theater, consistent with the documented `state-time-cumulative-view` baseline (chart ~40%, "mostly MUI-presentational"). The scoped run above measures the feature's decision logic, which is the contract that matters.
