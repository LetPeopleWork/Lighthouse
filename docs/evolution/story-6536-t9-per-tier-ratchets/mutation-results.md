# Mutation results, Story 6536

Run 2026-11-08 at `41a2d8bb3`, StrykerJS 10.1.1 with the Vitest runner, `perTest` coverage analysis and incremental
mode, config `Lighthouse.Frontend/stryker.6536.frontend.json`. The tests run are the direct ones in
`src/tests/timeBudget/` only (the runner's `dir`), never the whole frontend suite.

| File | Score | Killed | Survived | No coverage | Timeout |
|---|---|---|---|---|---|
| `compareToAllowList.ts` | 100 % | 12 | 0 | 0 | 0 |
| `timeBudgetReporter.ts` | 100 % | 5 | 0 | 0 | 0 |

17 mutants, 16 seconds. No survivors, so no test was added and no mutant is accepted as equivalent.

Only the lines T9's slice A changed are mutated, by line range: in `compareToAllowList.ts` the new `budgetOfFile`
(which budget a file answers to), the limit falling back to it, and the stale-entry check measured against it; in
`timeBudgetReporter.ts` the summary line that now names the fast budget. The rest of both files is older code that
T9 did not touch. The new `fastBudgetSeconds` field is a type and has no mutants. `vitest.config.ts` is
configuration and is left out. Stryker's JSON report and incremental file are written to the gitignored
`reports/mutation/`.
