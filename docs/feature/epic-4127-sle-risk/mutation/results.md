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

## Slice 01b — the minimum-sample guard

Re-run 2026-09-17 after `OUT-4127-risk-stability` forced the guard in.

| Stack | Mutants tested | Killed | Survived | Score |
|---|---|---|---|---|
| Backend | 34 | 34 | 0 | **100.00%** |
| Frontend | 54 | 49 | 5 | **90.74%** |

### The frontend run found a test that could not fail, and it took two passes to see it

The first run of the guard scored 83.64% with nine survivors, and one of them was not what it looked
like. `answers.map((answer) => [answer.referenceId, answer])` mutated to `() => undefined` was
reported **Survived** — and when the same mutation was applied by hand, it threw
`TypeError: Iterator value undefined is not an entry object` and the suite printed `Tests  no tests`.

The cause was in the test file, not the product: a descriptor was built at `describe` scope, so
anything that throws while building it kills **collection** rather than a test. A file that never
collects runs zero tests, and StrykerJS reads zero failures as a survivor. The fix was to move the
construction inside the tests. This is the same shape as the ledger's warning about an `include:`
list naming a spec that does not exist — a mutant that stops the tests from running is
indistinguishable from a mutant nothing covers.

The second real survivor was the ledger's most common one, arriving exactly as it describes:
blanking `SLE_RISK_NOT_ENOUGH_HISTORY_LABEL` to `""` survived every assertion, because every
assertion compared a label to the constant it came from. Both sentences are now pinned against their
literals once.

Two more fell to a test each: `riskFor` on an item the answer never mentioned (the optional chain
was load-bearing and nothing proved it), and an `answer?.risk` whose optional chain was **not**
load-bearing — the conjunct before it already short-circuits on a missing answer — which was
simplified away rather than tested.

### What survives, and why it is left alone

All five remaining survivors are one equivalent mutant: the two sentinel comparisons in
`sleRiskSortValue`. Removing them leaves `Number.parseInt("Beyond history")` returning `NaN`, which
the next line already turns into `undefined`. Unkillable as written, and kept deliberately — the
parse rejects both sentinels only because their wording starts with a letter, and one reworded to
start with a digit would be read as a risk on a column whose whole job is ordering.

### A fixture note the review raised

Two rescaled scenarios had landed on exactly ten comparable items, which is the guard's own
boundary. They are about the arithmetic, not the threshold, so they were given one more copy each —
twelve items, same proportions, same pinned percentages. The threshold has its own two scenarios at
nine and ten and does not need to borrow theirs.

## Slice 02 / Story #6017 — the at-risk count on the WIP card

Run 2026-09-17. Frontend only; slice 02 adds no backend code.

| File | Killed | Survived | Score |
|---|---|---|---|
| `utils/charts/sleRisk.ts` | 71 | 5 | **93.42%** |
| `pages/Common/MetricsView/WipOverviewWidget.tsx` | 23 | 17 | 57.50% |
| Combined | 94 | 22 | **81.03%** |

### The run found the counting rule's one real weakness

Two survivors sat on the first conjunct of each filter — `answer.risk === null` and
`answer.risk !== null`. Replacing either with `true` made an answer with **both** a number and zero
comparable items fall into both sets and be counted twice. The backend cannot emit that shape, so
the conjuncts were defending against an impossible payload, which is why no test could reach them.

Rather than write a test for an impossible answer, the two filters became one with two exclusive
arms:

```ts
answers.filter((answer) =>
    answer.risk === null ? answer.comparableItems === 0 : answer.risk >= AT_RISK_FROM,
)
```

Double counting is now impossible by construction rather than by a guard nobody could exercise, the
colour falls out of `answer.risk ?? 100` instead of a second branch, and both mutants are gone.

### The widget's 57.5% is presentation, and is left alone

`WipOverviewWidget.tsx` had never been in a mutation run before this slice. Sixteen of its seventeen
survivors are pre-existing: `sx` object literals and the strings inside them (`"flex"`, `"center"`,
`"100%"`), the default `title = "In Progress"` that production never uses, and `hasLimit`'s
`systemWipLimit != null` conjunct — which is genuinely equivalent, because `undefined > 0` is already
false, so the null check changes no outcome.

The seventeenth is on this slice's own line, and it is the `sx={{ mt: 0.5, fontWeight: 600 }}` of the
risk line. Every **behavioural** mutant on the new line was killed: whether it renders, what it says,
what colour it is given, and that it stays away at a count of zero.

Chasing the rest would mean asserting on margins and font weights, which is the implementation
detail the ledger says not to invent tests for. The number is recorded as it is rather than
massaged by narrowing the config to the file that scores well.
