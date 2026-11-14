# Mutation testing - Story 6502 (hide outliers), frontend

Run 2026-11-14 against code frozen at `c4acda4ca`. StrykerJS, `coverageAnalysis: perTest`, concurrency 2,
`disableTypeChecks: false`, break 80. Backend: N/A (one enum member and a data row, nothing to mutate).

Files: `stryker.6502.mjs`, `vitest.stryker.6502.config.ts` (copy both next to `package.json` to run; Frontend
gitignores them). The incremental file lives in `reports/mutation/` and is not committed.

| Run | Score | Killed | Survived | Notes |
|---|---|---|---|---|
| 1 (plain, 295 mutants, ~12 min) | 74.58 % | 220 | 74 (+1 no coverage) | below gate |
| 3 (S4, incremental, frozen at `6d6346a66`, 6.5 min, 202 mutants) | **96.53 %** | 195 | 7 | gate met. `OutlierChip.tsx` 55.56 % (4 survivors): the phone-query mutant is killed afterwards by "drops the words only below the phone breakpoint"; the two `sx` mutants are styling only; the disabled-tooltip text mutant is not observable because MUI shows no tooltip on a disabled child. `outliers.ts` keeps its 3 known equivalent mutants. Every other file 100 %. |
| 2 (incremental, after one round of new tests) | **88.47 %** | 261 | 34 | gate met; stopped here |

## Per file (run 1 -> run 2)

| File | Run 1 | Run 2 | Survivors left |
|---|---|---|---|
| outliers.ts | 91.25 | 95.00 | 4 |
| offScaleLines.ts | 100 | 100 | 0 |
| outlierWording.ts | 69.77 | 76.74 | 10 |
| OutlierChip.tsx | 58.33 | 77.78 | 8 |
| OutlierPopoverContent.tsx | 42.11 | 52.63 | 9 |
| OffScaleLineMarkers.tsx | 40.00 | 100 | 0 |
| outlierHeaderChip.tsx | 90.00 | 90.00 | 2 |
| outlierPlot.ts | 71.88 | 98.44 | 1 |
| widgetHeaderSlot.ts | 76.92 | 100 | 0 |

## Closed by the new tests (all `*.mutation.test.ts(x)` files, listed in `include`)

- `src/utils/charts/outlierWording.mutation.test.ts`: plural/singular, loading words, list title, column heads, cut and above-axis sentences.
- `src/utils/charts/outliers.mutation.test.ts`: unsorted median input, zero median gives no outliers, "6.0x" formatting, axis top with no outliers / no cut.
- `src/components/Common/Charts/outlierPlot.mutation.test.ts`: stale answer rejection, tick filtering, marker keys and text, reserved marker rows at the axis boundary, row key and date.
- `src/components/Common/Charts/widgetHeaderSlot.mutation.test.tsx`: ready outside / inside a frame, cleanup on unmount.
- `src/components/Common/Charts/OffScaleLineMarkers.mutation.test.tsx`: position, stacking, aria-hidden, empty case.
- `src/components/Common/Charts/OutlierChip.mutation.test.tsx`: phone count vs words, opened-once, closes when disabled, tooltip, popover above-axis and "more" lines, visually hidden spoken text.

## Survivors left, with reasons

- Equivalent: `outliers.ts:19` (`length + 1` inside `Math.min`; the index never exceeds `length/2 - 1`), `outliers.ts:42` (`median === null` to `false`: `null <= 0` is true, same branch), `outliers.ts:102` (`<=` vs `<` at exactly 10 times: both give 10).
- `outliers.ts:15` (`.sort` removed on a `.filter` copy): a `values` mutation test with unsorted input passes only if the order matters; the new test covers it, so this one is probably stale from the incremental reuse (not re-verified).
- `outlierPlot.ts:19` (`NO_PERCENTILES` non-empty): equivalent in effect only when callers never read it; not pinned. Low value.
- Style-only: `sx` objects and strings in `OutlierChip` (79, 88) and `OutlierPopoverContent` (15-23, 59, 66), tooltip title string, `PHONE_WIDTH_QUERY`. Presentation constants no test can see in jsdom, except `visuallyHidden`, which the new test asserts through computed style.
- Suspect incremental staleness: `outlierWording.ts` 45-49, 60-65 and `OutlierPopoverContent.tsx` visuallyHidden entries survive although the new tests assert exactly those strings and styles. StrykerJS incremental mode may have reused the old "Survived" result. A plain re-run would show; not done, per the one-round rule.
- Not killed, real gaps: `OutlierChip.tsx:47` (`if (disabled)` to `true`; closing is already covered by `open = anchor && !disabled`, so near-equivalent), `outlierHeaderChip.tsx:34` (`median === null` / `cut === null` to `false`; both nulls only occur together with an empty outlier list), `OutlierPopoverContent.tsx:65` (`aboveTheAxis && ...` to `||`).
