# Mutation testing — Epic #4127 (SLE Risk)

## Slice 01 / Story #6016 — the risk read and the dialog column

Run 2026-09-17. Gate: ≥ 80% kill rate on the new code, both stacks.

| Stack | Config | Mutants tested | Killed | Survived | Score |
|---|---|---|---|---|---|
| Backend | `stryker.6016.backend.json` | 29 | 29 | 0 | **100.00%** |
| Frontend | `stryker.6016.frontend.json` | 34 | 32 | 2 | **94.12%** |

Backend scope: `SleRiskCalculator.cs` whole, plus `TeamMetricsService.GetSleRiskForTeam` and
`TeamMetricsController.GetSleRiskForTeam` by byte range. Frontend scope: `utils/charts/sleRisk.ts`.
Both byte ranges were recomputed against the final files — the ledger records that a stale offset
mutates the wrong code and reports a meaningless score.

### What the first run found (70.59%, 9 survivors) and what each one was worth

The first backend run is the reason this slice grew four scenarios and two unit tests. Three
survivors were real gaps, three were diagnostics, and one was equivalent.

**Real, and each now has a test that fails without it:**

1. **`T >= a` weakened to `T > a` in the numerator** survived every scenario. It only shows when an
   item is past the target *and* something finished at exactly the item's age — then the mutant
   reports 50% where the truth is 100%, on the one item that can no longer make the target.
   Closed by `Risk_FinishedItemThatTookExactlyAsLongAsAnItemPastTheTarget_IsOneOfTheMisses`.
2. **The whole cache key blanked to `$""`** survived, because no scenario asked the same team two
   questions. That is the key's entire job. Closed by
   `Two_windows_asked_one_after_the_other_get_their_own_answers`, and by the target-change scenario
   below.
3. **The null guard deleted** survived, because LINQ throws the same exception one line later — so
   the test asserting it proved nothing. It is only a real guard when the range is also unusable,
   where the mutant returns "no answer" instead of refusing. Closed by
   `Risk_NoHistorySupplied_RefusesEvenWhenThereIsNoTargetToCompareAgainst`.

A fourth gap came out of the same run as `NoCoverage` rather than `Survived`: nothing asked this
route for a **backwards window**, so the 400 guard was never executed. Closed by
`A_window_that_ends_before_it_starts_is_refused`, with
`A_window_of_one_day_is_a_question_like_any_other` beside it, because the guard is one character
away from rejecting a single-day window too.

**Diagnostics, marked `// Stryker disable once all`:** the two `LogDebug` lines and the
`LogDateBoundaries` call. Their text is not behaviour and pinning it would only make the log
un-editable.

**Equivalent, and kept:** `Where(ct => ct > 0)` on the closed cycle times. Admitting a zero changes
no answer — an item of age 1 or more is never compared against one, and an age below that has no
answer at all. The filter stays because it is the same selection the cycle-time percentiles read,
and dropping it would let the two disagree about which work counts.

### The frontend's two survivors are one equivalent mutant

Both sit on `sleRiskSortValue`'s early return for the beyond-history label: removing it leaves
`Number.parseInt("Beyond history")` returning `NaN`, which the next line already turns into
`undefined`. Unkillable as written.

It is kept deliberately, and the code now says why: the parse rejects the sentinel only because the
wording happens to start with a letter. A sentinel reworded to start with a digit would be read as a
risk, silently, on a column whose whole job is ordering.

### Running it again

```pwsh
# Backend (ARM64 machine — Stryker forces TargetPlatform=X64 and finds 0 tests otherwise)
pwsh -NoProfile -File docs/feature/epic-4127-sle-risk/mutation/run-backend-x64.ps1 `
  -Config docs/feature/epic-4127-sle-risk/mutation/stryker.6016.backend.json

# Frontend — the vitest config must be copied to Lighthouse.Frontend/ first and named bare there,
# because `vitest.configFile` resolves against the working directory and `vitest/config` resolves
# upward from the config file's own directory.
cp docs/feature/epic-4127-sle-risk/mutation/vitest.stryker.6016.ts Lighthouse.Frontend/
cd Lighthouse.Frontend
npx stryker run ../docs/feature/epic-4127-sle-risk/mutation/stryker.6016.frontend.json
```

Re-anchor both byte ranges in the backend config against the current files before trusting a score.
