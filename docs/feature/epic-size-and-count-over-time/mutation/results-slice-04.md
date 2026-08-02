# Mutation testing — Epic 5585 slice 04 (US-04, ADO #5617)

**Stack**: frontend (StrykerJS) · **Date**: 2026-08-02 · **Score**: **80.75%** (172 killed / 213 mutants)

Scope is the whole slice, not one file: the legend (`ChartLegend.tsx`), its selection state
(`useLegendFilter.ts`), the shared colour map (`deliveryEpicColors.ts`) and both charts that consume
them. Slices 01-03 mutated `DeliveryEpicSizeChart.tsx` alone, so the fever chart's numbers appear here
for the first time.

| File | Score | Killed | Survived |
|---|---|---|---|
| `deliveryEpicColors.ts` | 100.00 | 3 | 0 |
| `DeliveryEpicSizeChart.tsx` | 90.60 | 106 | 10 (+1 no coverage) |
| `useLegendFilter.ts` | 89.47 | 17 | 2 |
| `DeliveryFeverChart.tsx` | 70.00 | 28 | 12 |
| `ChartLegend.tsx` | 52.94 | 18 | 16 |
| **All files** | **80.75** | **172** | **40 (+1)** |

## First pass was 74.18%, and the gap was real in eleven places

Nine scenarios closed it. Each one names a behaviour that had shipped untested — none is padding:

| Mutant that survived | What it proved was untested |
|---|---|
| `ITEMS_AXIS_LABEL` → `""` | The items axis' label was never read. The `ChartsYAxis` mock now forwards `label`. |
| `itemsAxis: []` → `["Stryker was here"]` | "No items axis" was asserted as *absent*, which any junk entry also satisfies. Now the axis list is asserted to be exactly `[count]`. |
| `hasSizes &&` → `false` | The items axis was only ever asserted absent, never present. |
| `epics.length > 0 &&` → `true` | A history with no sizes would have rendered an empty legend control and no scenario minded. |
| `useId().replace(/[^a-zA-Z0-9]/g, "")` → junk | The sanitising is there because React 19's guillemets are invalid inside `url(#…)`; nothing pinned it. Now the pattern id must match `^hatch-[a-zA-Z0-9]+-\d+$`. |
| `estimatedByDay[dataIndex] ?? false` → `true` | A formatter called for a day outside the recorded window had no expected answer. |
| `title = "Delivery Progress"` → `""` | The fever chart's default title. |
| `maxLength > 1` → `>= 1` | The Run control on a single-snapshot delivery — nothing to animate, so no button. |
| `FeverZoneBands` (no coverage) | The three zone bands were never rendered by any test, because the `ScatterChart` mock dropped its children. The mock now renders them and the band fills are asserted. |

## The 41 that remain

**37 are `sx` style literals** — `ObjectLiteral → {}` and `StringLiteral → ""` on `display`, `alignItems`,
`borderRadius`, `p`, `gap`, `margin`. Killing them means asserting layout CSS in a jsdom test, which pins
appearance rather than behaviour. This is the same call slice 03 recorded. It is also why
`ChartLegend.tsx` scores 52.94%: the file is a presentational component, and **all 16** of its survivors
are of this kind — there is no untested behaviour hiding in that number.

**4 are equivalent mutants:**

- `useLegendFilter.ts:29,31` — `useCallback` dependency arrays `[]` → `["Stryker was here"]`. A dep array
  changes callback *identity*, not behaviour; no observable output differs.
- `DeliveryEpicSizeChart.tsx:69` — `if (metric.totalItems !== null)` → `true` in `sizesOn`. With the guard
  forced open the map stores `null`, and the caller's `sizes.get(id) ?? null` collapses to the same cell.
  No input can distinguish the two.
- `DeliveryEpicSizeChart.tsx:117` — the `localeCompare` options object → `{}`. Only reference ids differing
  by case alone would order differently, which the stack order does not depend on.

**1 has no coverage**: `"brightness(120%)"` on the bar renderer's highlight branch — MUI drives
`ownerState.isHighlighted` on hover, which the slot-contract scenarios exercise directly rather than
through the chart.

## Reproducing

`Lighthouse.Frontend/stryker.5585.frontend.json` + `vitest.stryker.mutation.ts` (copies alongside this
file as `stryker.5585.frontend.slice-04.json` / `vitest.stryker.mutation.slice-04.ts`). The vitest config
narrows `include` to the eight specs covering the mutated files — sweeping all 291 OOMs the node heap
(Bug #5628 precedent).
