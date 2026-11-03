# Slice 1a proof: the time-budget guard through the real reporter (2026-11-03)

Quiet machine, `main` at `fb2747a6e`, `pnpm test` at its default 6 workers. Run by the lead with the scratchpad script
described below; every command, its exit code and the guard's lines are in `proof-2026-11-03/summary.txt`, the full
output per check in `proof-2026-11-03/<check>.log.gz`. Scratch files lived in `src/fe7scratch/` and two scratch
allow-list entries were added for WS7/WS8; all were removed afterwards (`git status` clean apart from these records).
The planted test sleeps 6.5 s (its own timeout 15 s), so it is over the plain limit (2 s) and the coverage limit
(2 s x 3 = 6 s).

## Confirming guard-on full run

`pnpm test`, guard enforcing, seeded list of 30: **exit 0, 581 files, 9,054 tests passed, 0 offenders.** The start line
reads `Time budget: fail mode, plain run, 6 workers, 12 processors`. `sonar-report.xml` and `test-results.json` were
both rewritten by the run (10:35:38), so plain `pnpm test` now writes the reports again (WS9).

## Walking-skeleton checks

| # | Check | Command | Result |
|---|---|---|---|
| WS1 | Planted slow test, plain | `pnpm test src/fe7scratch/slow.test.ts` | exit 1; `src/fe7scratch/slow.test.ts :: planted sleeps past the budget took 6.51 s, limit 2 s` |
| WS2 | Same, CI coverage run | `CI=true pnpm run sonarreport src/fe7scratch/slow.test.ts` | exit 1; `... took 6.51 s, limit 6 s`; start line says `coverage run`; both report files rewritten (10:35:53) |
| WS3 | Stryker marker | `STRYKER_MUTATOR_WORKER=0 pnpm test ...` | exit 0; no guard output |
| WS4 | Watch mode | `pnpm exec vitest src/fe7scratch/slow.test.ts` (no `run`, no TTY, so Vitest ran one cycle and exited) | exit 0; no guard output |
| WS5 | Warn, on GitHub Actions | `LIGHTHOUSE_TIME_BUDGET=warn GITHUB_ACTIONS=true pnpm test ...` | exit 0; `::warning::src/fe7scratch/slow.test.ts :: ... took 6.51 s, limit 2 s` |
| WS5 | Fail, on GitHub Actions | `GITHUB_ACTIONS=true pnpm test ...` | exit 1; `::error::src/fe7scratch/slow.test.ts :: ... took 6.51 s, limit 2 s` |
| WS6 | Off | `LIGHTHOUSE_TIME_BUDGET=off pnpm test ...` | exit 0; no guard output |
| WS7 | Listed test over its ceiling (4 s) | `pnpm test src/fe7scratch/slow.test.ts` | exit 1; `... took 6.51 s, limit 4 s` |
| WS8 | Listed test that no longer exists, filtered run | `pnpm test src/fe7scratch/slow.test.ts` (entry's file not collected) | no stale line (only the WS7 offender) |
| WS8 | Same, full run | `pnpm test` | exit 1; `src/fe7scratch/other.test.ts :: was deleted long ago stale allow-list entry (test-absent)`; 583 files, 9,057 tests passed |
| WS9 | Reports written by plain `pnpm test` | (full run above) | both files rewritten |
| WS10 | CI with bail, slow test then a failing test | `CI=true pnpm test src/fe7scratch/` | exit 1; `planted sleeps a long time took 6.52 s, limit 2.31 s` named; the absent listed test is **not** reported stale |
| WS10 counter-check | Same files, no bail | `pnpm test src/fe7scratch/` (CI unset) | exit 1; the same offender **and** `planted sleeps past the budget stale allow-list entry (test-absent)` |

The counter-check shows the bailed run skipped the stale check because the run was cut short, not because the stale
rule missed the entry.

## Found in passing

Every run now prints a Vite notice that the config pulls in modules imported without a file extension
(`src/tests/timeBudget/*.ts`), which the future native config loader will not accept. It is a notice, not a failure;
the slice 1a refactor pass removes it.
