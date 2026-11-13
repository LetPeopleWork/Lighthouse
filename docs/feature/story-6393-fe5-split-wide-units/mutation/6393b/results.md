# Mutation testing — Story 6393 part B (FE5, split wide frontend units)

Run 2026-11-13 on `main` @ `f7f3ff78b` (part B's code frozen after its final refactor and review), StrykerJS, frontend only.
Gate: 80 % kill rate. Backend: N/A, part B changed no backend file.

| run | score | killed | survived | no coverage | runtime error | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| 1, plain | **77.18 %** | 301 | 69 | 20 | 0 | 1 m 57 s |
| 2, incremental after one kill round (tests only) | **91.00 %** | 354 | 34 | 1 | 1 | 49 s |

Configs: `stryker.6393b.frontend.json`, `vitest.stryker.6393b.mutation.ts` (copy both into `Lighthouse.Frontend/` to run).
Survivor lists: `survivors-run1.txt`, `survivors-run2.txt`. Logs: `run1.log`, `run2.log`.

## Scope

The nine files part B moved code into, mutated against their direct tests only (the slice oracles, `useMetricsData.test.ts`
and the kill-round files):

| file | run 1 | run 2 |
| --- | --- | --- |
| `src/hooks/metricsApplicability.ts` | 94.64 % | 94.64 % |
| `src/hooks/metricsOwnerKey.ts` | 100 % | 100 % |
| `src/hooks/useMetricsQuery.ts` | 85.71 % | 100 % |
| `src/pages/Common/MetricsView/useNamedPercentilesScope.ts` | 82.50 % | 95.00 % |
| `src/pages/Common/MetricsView/useStateDrillDown.ts` | 87.50 % | 95.00 % |
| `DeliveryGrid/deliveryFeatureRow.tsx` | 38.38 % | 78.57 % |
| `DeliveryGrid/deliveryFormRules.ts` | 94.83 % | 94.83 % |
| `DeliveryGrid/deliveryHeaderCopy.tsx` | 93.75 % | 93.75 % |
| `DeliveryGrid/UnbindConfirmationDialog.tsx` | 83.33 % | 91.67 % |

Run 1's low figure for `deliveryFeatureRow.tsx` came from code whose older tests are the `DeliverySection.*` component
tests, outside this scope. The kill round added unit-level rows next to the code: `deliveryFeatureRow.test.tsx` (19),
`useNamedPercentilesScope.test.ts` (10), `useStateDrillDown.test.tsx` (6), `useMetricsQuery.test.tsx` (6),
`UnbindConfirmationDialog.test.tsx` (3).

## Remaining survivors, recorded not chased

- `deliveryFeatureRow.tsx`: 21 survivors, all `sx` style objects and their strings (position, sizes, colours). They
  change only how a cell looks, which jsdom cannot observe without asserting CSS; equivalent for behaviour.
- `deliveryFeatureRow.tsx:71` RuntimeError: removing the optional chain on `onArchive?.(delivery)` throws when no
  archive handler is passed, which a row covers; Stryker counts it as an error, not a survivor.
- `useStateDrillDown.ts:61` no coverage: the `?? ""` fallback in the query function cannot run, because the query is
  enabled only once a state is chosen.
- The few left in `metricsApplicability.ts`, `deliveryFormRules.ts`, `deliveryHeaderCopy.tsx`, `useNamedPercentilesScope.ts`
  are listed in `survivors-run2.txt`; each file is over 90 %, so they were not chased.
