# Mutation testing — Story 6452 (Delivery Metrics info popovers)

Frontend only; the backend has no changed files, so it needs no run.

Config: `stryker.6452.frontend.json` (StrykerJS, `coverageAnalysis: perTest`, incremental), with
`vitest.stryker.mutation.ts` narrowing the suite to the delivery chart specs. Run on frozen code at
`b64c8e4c8`, 2026-10-30.

## Score

| File | Score | Killed | Survived |
|---|---|---|---|
| `deliveryChartInfo.ts` (new, whole file) | 100 % | 20 | 0 |
| `DeliveryBurnupChart.tsx` (changed lines) | 100 % | 3 | 0 |
| `DeliveryEpicSizeChart.tsx` (changed lines) | 100 % | 5 | 0 |
| `DeliveryFeverChart.tsx` (changed lines) | 100 % | 1 | 0 |
| `DeliveryPredictabilityChart.tsx` (changed lines) | 100 % | 1 | 0 |
| `DeliveryMetricsTab.tsx` (changed lines) | — | 0 mutants | — |
| `ChartTitle.tsx` (new, whole file) | 20 % | 2 | 8 |
| **All** | **80.00 %** | **32** | **8** |

`DeliveryMetricsTab.tsx`'s changed lines are term look-ups and JSX props, which Stryker does not
mutate. The tab's wiring is still pinned: `DeliveryMetricsTab.test.tsx` renders it with renamed terms
and checks every chart's popover text and link.

A first run scoped to whole hunks scored 73.28 %. The extra survivors were MUI `sx` literals on lines
this story did not change, picked up because the ranges spanned them. The recorded run mutates exactly
the changed lines.

## Survivors (8, all accepted)

All eight are in `ChartTitle.tsx`'s two `sx` objects: `display: "flex"`, `alignItems: "center"`,
`justifyContent: "space-between"` and `flexWrap: "wrap"` blanked, or the whole object emptied. They are
styling. jsdom computes no layout, so no unit test can see the difference, and asserting style values
would pin CSS rather than behaviour. That is the same call earlier features made for spacing and wrap
literals. What the component *does* (it shows the title, the `About <title>` button, the popover text,
the Learn More link and the optional action) is killed by the chart and tab tests.

## Re-run after the popover restructure (2026-10-30, rebased onto origin/main)

The maintainer asked for each popover to split into "What you see" and "How to read it" lists, which
changed `deliveryChartInfo.ts` and `ChartTitle.tsx` after the run above. An incremental re-run on the
same config scored **82.28 %** (65 killed, 14 survived). `deliveryChartInfo.ts` is at 100 % (51
killed), because the tab test pins every bullet in order. All 14 survivors are `sx` literals in
`ChartTitle.tsx`: the header row, the section heading's `display`/`lineHeight`, the list's margin and
indent, and `display: "list-item"`. They are styling, accepted on the same grounds as above.
