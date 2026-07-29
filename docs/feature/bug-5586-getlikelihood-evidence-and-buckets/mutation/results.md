# Bug #5586 — mutation testing results (2026-07-29)

Strategy `per-feature` (CLAUDE.md), gate ≥ 80 % kill rate. **Both stacks pass.**

| stack | score | killed | survived | timeout | config |
|---|---|---|---|---|---|
| backend (Stryker.NET 4.16) | **91.43 %** | 128 | 10 | 0 | `stryker.bug5586.json` |
| frontend (StrykerJS 9.6.1) | **91.30 %** | 42 | 4 | 0 | `stryker.bug5586.frontend.json` |

## The two claims this run existed to settle

**`GetLikelihood` is pinned in every direction it could regress** — 9 mutants on
`ForecastBase.cs:72-94`, all killed:

| line | mutant | outcome |
|---|---|---|
| 74 | `TotalTrials != 0` | Killed — the zero-evidence guard is load-bearing |
| 85 | `Compare(key, threshold) < 0` | Killed |
| 85 | `Compare(key, threshold) >= 0` | Killed |
| 85 | `!(Compare(key, threshold) > 0)` | Killed |
| 87 | break removed | Killed |
| 90 | `trialCounter -= simulation.Value` | Killed |
| 93 | `100 / TotalTrials / trialCounter` | Killed |
| 93 | `100 * TotalTrials` | Killed |
| 75 | block removal | Killed |

**The removed Stryker annotation was removed correctly.** `Delivery.cs:72`
`!HasRemainingWork()` → `HasRemainingWork()` is **Killed**, and the block removal at :73 with it.
That guard carried `// Stryker disable once all: equivalent while Bug #5586 stands`, whose own
comment predicted it would become observable once the bug was fixed. It did. The sibling annotation
at `Delivery.cs:64-67` (guard 2) was deliberately KEPT and its 4 mutants remain Ignored — guard 2 is
still subsumed by guard 5, so it is not #5586-dependent.

## Survivors — none in this feature's changed lines

Backend (10): `Delivery.cs` :28 name string, :96 `DayZeroMarker` collection initializer, :154/:155
`Sum()`→`Max()` in the feature breakdown · `Feature.cs` :62 string, :165 `FirstOrDefault()`→`First()`
· `ForecastBase.cs` :13 protected-ctor block, :38 `Count < 1` in the `SimulationResult` getter, :105
in `SetSimulationResult` · `HowManyForecast.cs` :13 ctor block. All pre-existing code the whole-file
scope drags in.

Frontend (4): `cannotForecast.ts:23` `Intl.ListFormat` options — a **genuine equivalent mutant**,
`style: "long"` and `type: "conjunction"` are the defaults · `ManualForecast.ts` :24 and :32
pre-existing constructor defaults · `ForecastLikelihood.tsx:35` `precision: "fixed2"` → `""`, a
display-precision gap rather than a correctness one, and the only survivor inside a line this feature
touched.

## Two traps this run walked into — read before re-running

1. **Stryker.NET silently ignores line-span patterns.** A first run used
   `"**/Models/Forecast/ForecastBase.cs{72..94}"` to scope mutation to the decision code. It reported
   a clean **100 %** — and every one of the 19 tested mutants was in `AggregatedWhenForecast.cs`, the
   one entry with no span. The spans matched nothing and those files contributed zero mutants; the
   score was real but described none of the fix. **Whole-file `mutate` entries plus survivor triage
   is the only trustworthy shape here.** Same failure mode as the test-case-filter trap recorded for
   Epic 5459: the config excludes quietly and the number still looks good.
2. **A run whose mutants are 100 % `Timeout` is not a result.** One backend run overlapped with the
   frontend Stryker (8 GB node heap) at concurrency 6 and every mutant timed out. Run the two stacks
   sequentially.

## Frontend tooling constraint (why `inPlace` is not optional here)

StrykerJS cannot build a sandbox in this repo: `TSConfigPreprocessor` calls
`ts.parseConfigFileTextToJson`, which the installed TypeScript no longer exports, so any
sandbox-mode run dies at instrumentation. `"inPlace": true` skips that preprocessor, which is why
the sibling bug-5571 config uses it too.

In-place mode mutates the **real working tree**. Its default `disableTypeChecks` prepends
`// @ts-nocheck` to every matched source file, and a run that dies mid-flight (this one OOMed on
`coverageAnalysis: "perTest"` across all 282 specs) leaves 661 files modified. Recovery is
`git checkout -- Lighthouse.Frontend/`, which is only safe because the feature work was already
committed. Set **`"disableTypeChecks": false`** — it removes the damage vector entirely, and vitest
transpiles through esbuild without typechecking so it buys nothing anyway. Keep
`coverageAnalysis: "off"` and `concurrency: 2`.

`vitest.stryker.mutation.ts` in `Lighthouse.Frontend/` is required by the frontend config and is
gitignored per the repo convention that Stryker configs are local tooling. It narrows vitest to the
11 specs covering the mutated files; sweeping all 282 is what OOMs the heap.
