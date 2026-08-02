# Mutation testing — Epic 5585 slice 01

**Date**: 2026-08-02 · **Stack**: frontend (StrykerJS + vitest runner) · **Story**: ADO #5614

## Result

| | total | killed | survived | score |
|---|---|---|---|---|
| **All files** | 51 | 42 | 9 | **82.35%** |
| `DeliveryEpicSizeChart.tsx` | 33 | 25 | 8 | 75.76% |
| `DeliverySection.tsx` (MetricsTab, 577-641) | 18 | 17 | 1 | 94.44% |

Passes the project's 80% floor (`CLAUDE.md` — Mutation Testing Strategy: per-feature, minimum 80%).

## How to reproduce

```bash
cd Lighthouse.Frontend
cp ../docs/feature/epic-size-and-count-over-time/mutation/stryker.5585.frontend.json .
cp ../docs/feature/epic-size-and-count-over-time/mutation/vitest.stryker.mutation.ts .
pnpm exec stryker run stryker.5585.frontend.json
```

Both config files live in the frontend root at run time and are gitignored there; the copies here are
the archive. `inPlace: true` mutates sources directly — commit before running so a crash is recoverable
by `git checkout`.

The vitest config narrows `include` to the two acceptance files. Stryker runs the whole suite per
mutant, and sweeping all 289 spec files OOMs the node heap — the same constraint Bug #5628 hit.

## First run: 60.78%

Under the floor. The survivors split cleanly into two groups, and only the first was worth acting on.

**Real test gaps, now closed** (+17.6 points, 31 → 40 killed):

| Survivor | What it exposed | Killed by |
|---|---|---|
| `LABEL_DATA_KEY = "label"` → `""` | The x-axis assertion read `props.xAxis?.[0]?.dataKey ?? "label"`. The `??` fallback meant the test passed even with the axis key blanked — and, worse, even with the whole `xAxis` array emptied. A test that survives its own subject being deleted is not a test. | Dropped the fallback; assert the key is truthy and the dataset is addressed through it |
| `xAxis` → `[]` | Same fallback. No x-axis at all still passed. | Same |
| `scaleType: "band"` → `""` | ADR-122's band scale was never asserted | New scenario: "spaces the days evenly rather than by calendar distance" |
| `featuresTerm = "Epics"` → `""` | The default term was never exercised — only the value threaded from `DeliverySection` | New scenario: "still names itself when the caller supplies no term" |
| series `label` → `""` | Legend label unasserted | Extra assertion on the DDD-8 scenario |
| `ariaLabel` → `` `` `` | The enlarge control's accessible name was untested | New scenario asserting the button's name |
| `isLoading \|\| history === null` → `&&` / `false` | The loading guard was only ever exercised with BOTH conditions true, so the operator was free | New scenario: history resolves null → placeholder stays up |
| `display: "grid"` → `""` | AC-1.3 claims a grid and nothing asserted it — child count and DOM order pass just as well in a flex column | `getComputedStyle(grid).display` — jsdom does resolve emotion's injected styles |

**Cosmetic, deliberately left alive** (9 survivors, all `sx`):

`DeliveryEpicSizeChart.tsx:37,40` (Card/CardContent `sx` objects and their string values), `:59` (empty-state
`sx={{ mt: 2 }}`), `:88` (chart `margin`), `DeliverySection.tsx:592` (tab placeholder `sx`).

Killing these means asserting on emotion class names or inline style strings — brittle against any visual
tweak, and they encode no behaviour. The `display: "grid"` mutant was the one exception because the grid
*is* the acceptance criterion; padding, margins and border radii are not.

## Note for slice 02

`DeliveryEpicSizeChart.tsx` gains bars, a left axis and a widened `FeatureMetric` in slice 02. Re-run
this config then rather than treating 75.76% on that file as settled — the eight surviving `sx` mutants
will still be there, but the denominator changes.
