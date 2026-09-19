# Mutation testing — slice 01 (ADO #6034, remove the SLE Risk background zones)

Run 2026-09-19, both stacks, sequentially (an overlapping run is 100 % `Timeout`, which is not a result).

| Stack | File | Tested | Killed | Raw score | Killable score |
| --- | --- | ---:| ---:| ---:| ---:|
| Backend | `SleRiskCalculator.cs` | 21 | 21 | **100 %** | 100 % |
| Frontend | `useAgingBackground.ts` | 35 | 28 | **80.00 %** | **100 %** — all 7 survivors verified equivalent |

Configs are archived beside this file. The frontend pair is normally gitignored as local tooling; these copies are the reproducible record.

## Why the scope is this narrow

The slice is a deletion. It adds almost no production logic, so there is almost nothing to mutate — and mutating the files it merely *shrank* would score the pre-existing suite rather than the change.

Two files were worth the run, for different reasons.

**`useAgingBackground.ts`** is the only file that gained a branch: a retired stored value resolving to Off.

**`SleRiskCalculator.cs` is the one that could have regressed without anyone noticing.** `Zones()` called `For()` inside its age walk, so the twelve deleted `Zones_*` tests were incidentally exercising `For()` across a spread of ages. `For()` itself is untouched, but its *effective* coverage could have fallen when they went. It did not: the surviving `For_*` tests kill all 21 mutants on their own.

## The seven frontend survivors, each checked

DDD-6 predicted one equivalent mutant before the run, so the report could be read against a stated expectation instead of argued about afterwards. The prediction was right about the branch and understated the count — the same reasoning covers four mutants, not one.

| # | Location | Mutation | Verdict |
| --- | --- | --- | --- |
| 1 | 38:6 `stored === "risk"` | → `false` | Equivalent. `"risk"` falls through to `return null`, and the hook's initial state is already `"off"`. |
| 2 | 38:17 `"risk"` | → `""` | Equivalent, same path as 1. |
| 3 | 40:6 `stored === "off"` | → `false` | Equivalent. `"off"` falls through to `return null` → `"off"`. |
| 4 | 40:17 `"off"` | → `""` | Equivalent, same path as 3. |
| 5, 6 | 60:5 and 69:5 `[]` | → `["Stryker was here"]` | Equivalent. Both are dependency arrays; a constant array of any content preserves mount-once semantics. |
| 7 | 51:11 the `catch` block | → `{}` | Equivalent, and **hand-verified**: with the early `return` gone, `stored` is still `null`, so `storedBackground(null)` returns `null` and the background stays `"off"`. All 13 tests pass against it. |

**Survivors 3 and 4 are equivalent only because the hook's initial state is `"off"`.** If that default ever changes, both become killable and the suite will not say so. Worth knowing; not worth a test today.

## One real gap, found and closed

The run reported the `catch` around `localStorage.getItem` as `NoCoverage`. There was a test for `setItem` throwing and none for `getItem` throwing — the read side of the same private-window case. `still draws a chart when the browser refuses to say what was stored` now covers it.

**It does not move the score**, because the mutant it covers is equivalent. It is here because a throw escaping that effect takes the whole chart down, and nothing tested that.

## A trap worth adding to the CI ledger

**A StrykerJS frontend run can fail three different ways here and exit 0 every time.** Three consecutive runs reported success to the shell having tested zero mutants:

1. `Cannot find TestRunner plugin "vitest"` — the config needs an explicit `"plugins": ["@stryker-mutator/vitest-runner"]`.
2. `Vitest failed to find test files related to mutated files` — `vitest.related` defaults on and cannot reconcile with a narrowed `include`. Set `"related": false`.
3. `No tests were found` — the scratch vitest config pointed at a setup file that does not exist (`src/tests/testSetup.ts`; the real one is `./setupTests.ts`).

This is the same family as the three config traps already in the ledger — the config matches nothing and the number still looks fine — except here there is no number at all, just a green exit. **Run `pnpm exec vitest run --config <the stryker vitest config>` standalone first and confirm it reports a test count.** That single check would have caught all three before the first Stryker invocation.

## A probe that was wrong, and why

Survivors 3 and 4 initially looked like real gaps: a test seeds `"pace"` and asserts it, so how does anything on line 40 survive? Hand-applying `if (false) return stored;` killed two tests, which looked like Stryker mis-reporting.

It was not. **The mutant covers only `stored === "off"`, not the whole `||` expression.** Replacing the wider expression is a different, stronger mutation, and the conclusion drawn from it was wrong. Read `location.start.column` / `end.column` from the report and mutate exactly that span — a line number alone is not the mutant.
