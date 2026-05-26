# Frontend Mutation Testing — aging-pace-percentiles

Stryker (TypeScript/React) feature-scoped run via the command-runner pattern (vitest as a
subprocess, scoped `include`) to avoid the vitest-runner OOM.

- Config: `Lighthouse.Frontend/stryker.config.aging-pace-percentiles.mjs`
- Scoped vitest config: `Lighthouse.Frontend/vitest.stryker.aging-pace-percentiles.config.ts`
- Mutate scope: TS line-ranges scoped to the feature's new functions (line-ranges work fine
  for the TS runner — only the .NET glob-range is broken):
  - `WorkItemAgingChart.tsx:39` (`STATE_BAND_HALF_WIDTH`) + `:57-129` (`computePaceBandRects` + `PaceBandOverlay`)
  - `PercentileLegend.tsx:20` (`PACE_PERCENTILES_LABEL`) + `:93-117` (the Pace-percentiles chip)
  - `MetricsService.ts:271-282` (`getAgeInStatePercentiles`)
  - `useMetricsData.ts:216-221` (the parallel `Promise.all` fetch wiring)
- Baseline gate: 71 tests green before mutating → 76 green after new behaviour tests.

## Verdict: PASS

Overall feature-surface mutation score **89.58%** (43 killed / 48 total, 5 survivors),
above the 80% threshold. The geometry (`computePaceBandRects`) is at 97%, the service and
hook wiring are at 100%, and every survivor is either pure Material-UI presentational `sx`
styling or a provably-equivalent defensive guard.

## Per-file kill rate

| File | Score | Killed / Total | Survivors | Note |
|------|-------|----------------|-----------|------|
| `WorkItemAgingChart.tsx` (`computePaceBandRects` + `PaceBandOverlay`) | 97.06% | 33 / 34 | 1 | geometry: x-span, y-stacking, sort, palette, keys, skip-guard arms all killed |
| `MetricsService.ts` (`getAgeInStatePercentiles`) | 100.00% | 2 / 2 | 0 | URL shape + return value killed |
| `useMetricsData.ts` (parallel fetch) | 100.00% | 1 / 1 | 0 | per-state fetch wiring killed |
| `PercentileLegend.tsx` (Pace chip) | 63.64% | 7 / 11 | 4 | the 4 survivors are pure MUI `sx`/style literals; the chip's behaviour (label, render-when-available, toggle) is killed |

## Survivors killed in this session (mutant → test added)

Progression: first run 70.83% (34/48) → after new tests 89.58% (43/48). All new tests are in
`WorkItemAgingChart.test.tsx`, asserting the pure `computePaceBandRects` geometry directly:

1. **`@70` skip-guard — `percentiles.length === 0` arm + `return []` (ArrayDeclaration) + the
   `||` LogicalOperator.** Added "emits no rects for a mapped state whose percentile set is
   empty" (state in workflow order but empty percentiles → 0 rects) and "emits no rects for a
   state that carries observations but is not in the workflow order" (stateIndex undefined,
   percentiles non-empty → 0 rects). Together these exercise each arm of the `||` independently,
   killing the per-arm ConditionalExpression mutants, the `||`→`&&` mutant, the block-removal
   mutant, and the `return []`→`["Stryker was here"]` mutant.

2. **`@74` `.sort()` removal (MethodExpression), `@75` `() => undefined` (ArrowFunction), `@75`
   `a.value - b.value`→`a.value + b.value` (ArithmeticOperator).** Added "orders the stacked
   bands by ascending percentile value regardless of input order" — feeds percentiles in
   `[95,50,85,70]` order and asserts the stacked y-boundaries come out `[0,3],[3,5],[5,8],[8,12]`.
   Removing the sort, neutering the comparator, or flipping `-` to `+` all break the ordering.

3. **`@88` rect-key StringLiteral (`` `${state}-${percentile}` ``→`` `` ``).** Added "keys each
   rect by its state and percentile so React reconciles them stably" — asserts
   `rects.map(r => r.key)` equals `["Review-50","Review-70"]`. Also added "renders one band per
   percentile for a mapped state that has observations" to pin the rect count.

These took `WorkItemAgingChart.tsx` from 70.59% → 97.06%.

## Survivors remaining (each justified equivalent / non-feature)

**`PercentileLegend.tsx` (4) — pure Material-UI presentational styling:**
- `@97` the chip `sx={{...}}` ObjectLiteral → `{}`.
- `@100` `borderStyle: "dashed"` StringLiteral → `""`.
- `@104` `backgroundColor: ... : "transparent"` StringLiteral → `""`.
- `@105` the `"&:hover": {...}` ObjectLiteral → `{}`.

  These do not change the chip's observable contract — its label (`"Pace percentiles"`), its
  presence only when band data is available, and its click-toggle are all covered (those mutants
  were killed). Asserting exact `sx`/border/hover values would be Testing Theater (asserting HOW
  it looks, not WHAT it does), which the project conventions forbid. Same accepted-MUI-sx policy
  as the sibling time-in-state run.

**`WorkItemAgingChart.tsx` (1) — equivalent defensive guard:**
- `@70` the whole `if (stateIndex === undefined || percentiles.length === 0)` ConditionalExpression
  → `false` (guard never fires). This is equivalent: in every case where rects are actually built,
  the guard condition is already `false`, so never-firing it produces identical output; the cases
  where the guard SHOULD fire are covered by the two skip tests above, which kill the per-arm and
  `→true` mutants. A whole-condition `→false` mutant on a pure skip-guard with no else-branch is the
  classic equivalent mutant. Killing it would require asserting an internal branch that has no
  observable effect on the returned rects.

## New / modified files

Test files (kept):
- `Lighthouse.Frontend/src/components/Common/Charts/WorkItemAgingChart.test.tsx` (modified — 5 new `computePaceBandRects` geometry tests)

Config files (kept, mutation-tooling):
- `Lighthouse.Frontend/stryker.config.aging-pace-percentiles.mjs`
- `Lighthouse.Frontend/vitest.stryker.aging-pace-percentiles.config.ts`

No production source under `Lighthouse.Frontend/src/` was modified (Stryker sandboxes mutations).
No frontend dependency was touched (the `qs` audit gate is untouched). Biome clean on all touched
files.
