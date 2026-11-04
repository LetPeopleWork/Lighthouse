# Story 6457 — Frontend tests run in milliseconds: fix the slow ones, keep them fast

**Finished:** 2026-11-04 · ADO User Story #6457 · Epic #6506 "Fast, reliable test suites"

## The problem

The frontend suite had grown slow and nothing stopped a new slow test. Hundreds of seconds sat in a handful of test
families that rendered whole pages to check rules a function could answer. CI's budgets only warned, so a doubling went
unflagged.

## What changed

- **A guard that fails the build.** A Vitest reporter fails a run when a test goes over its limit: 2 s on a local run,
  three times that on a coverage run, which CI is today. A named allow-list holds the exceptions, each with a reason and
  a ceiling, and an entry that is no longer needed fails the run until it is removed.
- **The per-file budget fails the build too**, at 63 s per file on CI.
- **Rules tested as functions, one render per wiring.** Seven families were rewritten under a behaviour-free contract:
  every removed assertion has a row in the slice's record, surviving test ids are unchanged, and coverage did not fall.
- **The allow-list went from 30 entries to 3.** What stays is three genuinely long paging walks.

## What it did and did not achieve

Local figures are indicative, because the machine was shared. Summed test seconds of the touched files fell by 12 to
63 %; two of the seven rewritten families reached the estimated 50 %.

**The CI run did not get visibly shorter.** Vitest `Duration` on `main` falls into two groups, about 650 s and about
1,100 s, for the same tests, which look like hosted runners of different speed. The two final runs landed in the slow
group, inside the range of the runs before this Story. The slowest file did fall, from about 65 s to about 42 s, and
tests over 6 s from 2-3 to 1. Tests are only about half of `Duration`; import, environment and transform are the rest.

No configuration change passed the A/B rule, so none was adopted.

## Key decisions

- Budget 2 s on a quiet machine, a factor of 3 for coverage runs, and an allowance of 0.31 s for the first test.
- A local failure caused only by machine load is answered by a re-run; the guard stays enforcing.
- The metrics-view slice was dropped on measured payoff, to be revisited after FE5 #6393.
- The final sweep took no rewrites, because the remaining tests held no real waits and little payoff.
- The final local wall measurement and the five-run CI wait were both dropped by the maintainer, so the Story has no
  before-and-after wall figure.

## Quality

- Mutation: 97.76 % on the extracted production files, on direct tests only. See `mutation-results.md`.
- Adversarial review per slice and of the whole feature, each rejection fixed and proven.
- All 44 steps carry complete DES traces.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A for product docs, because nothing a user sees changed. The guard's switches are in `docs/ci-learnings.md`. |
| Screenshots | N/A, because no screen changed. |
| Demo data | N/A, because no data shape changed. |
| Website assets | N/A, because nothing the website shows changed. |
| Clients (CLI, MCP) | N/A, because no route or DTO changed. |
| Release notes | N/A here; written at /release, and this Story has no user-visible change. |
| Context map, `ARCHITECTURE.md` | No change: test tooling adds no concept, context, edge or domain event, and the test-stack line is current. |
| Usage-data event | N/A, because nobody uses test tooling in the product. |

## Follow-ups

- T7 #6513 drops coverage from CI; it is the next lever on CI minutes.
- T4 #6462: the frontend half is done and comes out of its brief at pick-up; it keeps the backend half and one
  recalibration on the plain CI run.
- T8 #6514 sharding is undecided.
- T12 #6539 handles order- and load-dependent tests found on the way.
