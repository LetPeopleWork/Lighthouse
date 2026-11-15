# Mutation testing — Story 6648

Run once, on frozen code, after the whole-candidate review was accepted (HEAD `docs(test-infra): story 6648 —
whole-candidate review accepted`), 2026-11-15.

| Stack | Command | Mutated | Score | Killed | Survived | No coverage | Timeout |
|---|---|---|---|---|---|---|---|
| Frontend | `pnpm --dir Lighthouse.Frontend exec stryker run stryker.6648.frontend.json --concurrency 2` | `src/pages/Reports/printFailures.ts` | **100.00 %** (break 80) | 28 | 0 | 0 | 0 |

The tests run against the mutants were `printFailures.test.ts` and `ReportsPage.combinedPdf.test.tsx`
(`vitest.mutation.6648.config.ts`). The full JSON report is written to `stryker-6648-frontend.json` beside this file and
is gitignored.

**Only production file mutated.** `printFailures.ts` is the one new production module. `useReportsPdfExport.ts` changed
only to call it, and every other change in the story is a test.

**First attempt failed.** The first run, with the configured concurrency of 4, stopped in Stryker's initial test run:
"Leave it out unpicks every report that failed" did not find the "No PDF was made" dialog in time. The machine load was
12 from parallel work in other checkouts. The same two files passed 21 of 21 without Stryker right afterwards, and the
retry with `--concurrency 2` passed its initial run and gave the score above. No test or production line changed between
the two attempts.

No survivors, so there was no kill round.
