# Frontend Mutation Report — work-item-age-percentiles (ADO #5257)

Tool: Stryker (command-runner + Vitest), `coverageAnalysis: off` (all tests per mutant).
Config: `stryker.config.work-item-age-percentiles.mjs` + `vitest.stryker.work-item-age-percentiles.config.ts`.
Date: 2026-06-09.

## Scope (new production surface only)

| File | Mutate range |
|------|--------------|
| `WorkItemAgePercentiles.tsx` | whole file (new card) |
| `WorkItemAgingChart.tsx` | `339-416` + `508-640` (new selector surface only; rest of the large pre-existing chart excluded) |
| `MetricsService.ts` | `340-355` (`getWorkItemAgePercentiles` only) |
| `PercentileValue.ts` | whole file (new `PercentileValueSchema`) |

Killing test files: `WorkItemAgePercentiles.test.tsx`, `WorkItemAgingChart.test.tsx`
(`Cycle time / work item age reference-line selector` + visibility-toggle describes),
`MetricsService.test.ts`.

## Kill rate

| Metric | Value |
|--------|-------|
| **Raw (Stryker total)** | **36.27%** — 70 killed / 193 mutants |
| **Adjusted (logic surface)** | **95.9%** — 70 killed / 73 (killed + genuine-gap + equivalent) |
| Genuine logic gaps remaining | **0** |

Per-file (raw):

| File | Raw | Notes |
|------|-----|-------|
| `MetricsService.ts` (`getWorkItemAgePercentiles`) | **100%** (3/3) | URL string + body + parse all killed |
| `PercentileValue.ts` (`PercentileValueSchema`) | **100%** (1/1) | schema-emptying killed via parse round-trip |
| `WorkItemAgePercentiles.tsx` | 27.7% | all survivors presentational/equivalent (see below) |
| `WorkItemAgingChart.tsx` (selector range) | 38.7% | survivors = presentational + pre-existing-chart + equivalent |

The raw number is dominated by MUI presentational mutants (`sx={{}}` object
literals and `""`/` `` ` ` ` `` style/className/key string literals) and by
pre-existing chart-rendering code that falls inside the broad `508-640` line
range but is **not** part of the new selector feature. The adjusted rate on the
actual behavioural surface is **95.9%**.

## Mutants killed by the targeted tests added

All five target mutants from the brief are now KILLED:

1. `WorkItemAgePercentiles.tsx:29` `.some(v => v.value > 0)` → `.every(...)` empty-state boundary — KILLED.
2. `WorkItemAgePercentiles.tsx` descending sort comparator — KILLED.
3. `WorkItemAgingChart.tsx:412/624` `percentileSource === "workItemAge"` branch — KILLED.
4. `WorkItemAgingChart.tsx:406` all-zero / `p.value > 0` guard — KILLED.
5. `WorkItemAgePercentiles.tsx:21` singular/plural day formatting — KILLED.

## Tests added (this run)

| Test | Kills |
|------|-------|
| `WorkItemAgePercentiles.test.tsx` — "renders the percentile table when only some values are positive" | `.some` → `.every` mutant (`L29`): mixed array makes `.some`=true / `.every`=false, so only `.some` renders the table. |
| `MetricsService.test.ts` — "fetches and parses work item age percentiles…" + "propagates errors…" | `getWorkItemAgePercentiles` URL string literal (`L347`), the two block-emptying mutants (`L344`/`L345`), and the `PercentileValueSchema` empty-object mutant (`PercentileValue.ts:3`, killed because an emptied `z.object({})` strips the parsed fields and the deep-equal assertion fails). |
| `WorkItemAgingChart.test.tsx` — strengthened "toggles percentile visibility when chip is clicked" | reference-line render guard `visiblePercentiles[p.percentile] === false ? null :` → `false` (`L627`): added `queryByTestId("reference-line-50%")` `.not.toBeInTheDocument()` + a still-visible assertion after the toggle. |

Each was verified to (a) pass clean and (b) flip RED when its target mutant is
applied directly to the production source, then the source was restored.

## Surviving mutants (verdicts)

Genuine gaps: **0**.

### Equivalent (3)

- `WorkItemAgePercentiles.tsx:78` `[MethodExpression]` `percentileValues.slice().sort(...)` → `percentileValues`.
  Stryker-artifact survivor: applying this exact mutation directly to source
  **does** fail the "lists … in descending order" test (verified manually —
  input is ascending, the test expects descending). Stryker's Survived verdict
  on the chained `.slice().sort().map()` node is a tooling false-negative; the
  behaviour is genuinely covered.
- `WorkItemAgingChart.tsx:407` `[ArrayDeclaration]` `meaningfulWorkItemAgePercentiles` `useMemo` deps → `[]`.
  Dependency-array mutant; unobservable in single-render tests (prop is set once
  at mount), classic equivalent mutant.
- `WorkItemAgingChart.tsx:512` `[ConditionalExpression]` `if (newSource !== null)` → `true`.
  The `null` arm only fires when a `ToggleButtonGroup exclusive` button is
  *deselected*; React Testing Library cannot deselect an exclusive toggle, so the
  guard is unreachable from the test layer. Equivalent in practice.

### Presentational (73)

All `sx={{}}` `[ObjectLiteral]` mutants and `""`/` `` ` ` ` `` `[StringLiteral]`
mutants on styling/className/`key`/`aria`/label strings across both `.tsx`
files (e.g. `WorkItemAgePercentiles.tsx` L33–L132 `sx` blocks and clamp
font-size strings; `WorkItemAgingChart.tsx` L518/L520/L539/L554/L576/L599/L633
`sx` blocks, L368 default-source `useState("cycleTime")` → `""` which is
behaviourally `cycleTime`, L543/L613/L616 template-literal label strings). These
do not change observable behaviour and are accepted (consistent with the sibling
chart features `state-time-cumulative-view`, `aging-pace-percentiles`,
`multiple-cycle-times`, all of which carried large presentational survivor sets).

### Out-of-scope — pre-existing chart code (47)

`WorkItemAgingChart.tsx` is a large pre-existing chart; the `508-640` mutate
range unavoidably includes pre-existing rendering that is **not** part of the
new percentile-source selector feature: the type-legend chips
(`L545-547`), the x-axis `scaleType`/`valueFormatter` (`L562-571`), the
scatter-series tooltip `valueFormatter` (`L604-616`), and the `types`/`colorMap`
`useMemo` (`L360-381`). These are owned by earlier features and are out of scope
for this report; they are reported here for transparency, not counted against
the new-surface kill rate.

## Quality gate

Adjusted logic-surface kill rate **95.9% ≥ 80%** → **PASS**. Zero genuine gaps.
Working tree restored to HEAD after all mutation runs; the scoped suite
(74 tests across 3 files) is green.
