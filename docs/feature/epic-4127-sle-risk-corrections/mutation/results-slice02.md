# Mutation testing — slice 02 (ADO #6037, one window, one anchor, one number)

Run 2026-09-19, both stacks, sequentially.

| Stack | File | Tested | Killed | Raw score | Killable score |
| --- | --- | ---:| ---:| ---:| ---:|
| Backend | `SleRiskCalculator.cs` | 15 | 13 | **86.67 %** | 100 % — both survivors verified equivalent |
| Frontend | `utils/charts/sleRisk.ts` | 48 | 48 | **100 %** | 100 % |

Configs archived beside this file. Scope verified per file from each report rather than taken from the headline — a config matching nothing prints a clean score in both stacks.

## The run earned its keep: a dead condition

The first backend run scored 83.33 % with three survivors, one of which was not equivalent:

```
[Survived] Equality mutation @ 54:97  'cycleTime >= ageInDays' → 'cycleTime > ageInDays'
```

That clause is **dead code**. The certainty rule above it guarantees `age <= target` by the time the counting happens, so for any breach `T > target >= age`, hence `T >= age` always holds. The second clause could never change the count.

DESIGN had already proved this in the abstract — *"`B` does not depend on `a`, for every `a ≤ R` … the numerator's second clause is free below the target"* — and the code still carried it. The mutation run is what turned a proof in a document into a change in the source.

Removing it: 80/80 suites still green, and the score moved **83.33 % → 86.67 %** with the mutant count dropping **18 → 15**. Three mutable positions disappeared along with a condition that read as a second constraint where there is only one.

## The two backend survivors, each checked

| Location | Mutation | Verdict |
| --- | --- | --- |
| `ArgumentNullException.ThrowIfNull(closedCycleTimes)` | removed | **Equivalent, and a repeat.** `Enumerable.Count` throws `ArgumentNullException` itself one line later, so no test can tell the guard's presence from its absence. Round 1's archive records the identical finding — *"a null guard whose test proved nothing because LINQ throws the same exception one line later."* Second occurrence; it belongs in the CI ledger rather than only here. |
| the `comparableItems == 0` block | `{}` | **Equivalent on this platform, and worth a caveat.** Without the guard, `0/0` is `NaN` and `(int)double.NaN` happens to be `0` here — so the tests cannot distinguish the explicit branch from an accidental coercion. `(int)NaN` is *unspecified* for unchecked conversions in the C# spec. The guard is not redundant; it is the reason the answer is zero, rather than the platform's. |

## The frontend result, and why 100 % is not the interesting part

48/48, including every mutation of the falsy-zero surface this slice created: `labelFor`'s `risk === undefined` comparison, `sleRiskSortValue`'s parse, `sleRiskColorFor`'s rank, and `sleRiskAtRiskSummary`'s threshold.

That surface exists because the slice retired two sentinels, which makes **`0` the commonest answer on a thin history** where a label used to be. A `labelFor` written as a truthiness check type-checks, reads naturally, passes review, and blanks exactly those cells. The tests were written against that specific trap before the run, so the score confirms rather than discovers.

## A correction to make in the open

**The first version of the cache-key test was vacuous, and it failed in the way this slice exists to prevent.**

It placed an item at age 10 against a 10-day target. At that age the comparable set and the breach set are identical, so the answer is 100 both today and tomorrow, and the test could not distinguish a correct cache key from one that had forgotten the day. It passed, and proved nothing.

That is precisely the defect DISTILL found in the *old* `a = R` test — a population containing no work that finished exactly on the target, so the assertion held whichever comparison the code used. It was reproduced one layer up, while writing the test intended to guard that class.

The replacement uses ages 5 → 6 over a population where the evidence genuinely moves, and it was verified in both directions:

- with `asOfDay` in the key: **passes**
- with `asOfDay` removed: **fails, "Expected 100, but was 50"** — yesterday's answer, served from a key that forgot the day

**A cache test that has not been run against the broken key is not evidence.** Neither is a boundary test whose population cannot tell the two sides of the boundary apart.

## What is not covered, stated plainly

**The cache key itself is not mutation-covered.** Stryker.NET silently ignores line-span `mutate` patterns in this repository, so a run cannot be scoped to `GetSleRiskForTeam` inside the 950-line `TeamMetricsService.cs`, and mutating that file whole would score the pre-existing suite rather than this slice. The key's four components are covered by acceptance tests against a live cache instead — target, history window, and as-of day each have one, and each was checked to fail when its component is dropped.
