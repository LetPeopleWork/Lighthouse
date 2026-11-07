# Slice 47 — T12 Frontend tests that do not depend on order or a quiet machine

Epic #6506 · ADO #6539 · `@infrastructure` · job_id: infrastructure-only · **Size M** (about 2 days, an estimate; sliced at DESIGN)
DRAFT for review. Every "suspect" below is a hypothesis to prove by running at DESIGN, not a finding.

## Goal

The frontend suite gives the same answer in any test order and on a machine with no spare capacity, so parallel sessions stop producing false reds.

## Why

Two `PortfolioDetail.test.tsx` tests fail under `--sequence.shuffle` seeds 11 and 33 (ADO #6539). Full runs at load 11 to 20 on
2026-11-04 failed on 5-second timeouts in several files, mostly Reports, each green alone; one,
`ReportsTab.createWizard.test.tsx`, failed on `pointer-events: none` (ADO #6539). `vitest.config.ts` sets no `testTimeout`, so
Vitest's own 5 s applies. `docs/ci-learnings.md` (line 118) advises an explicit timeout for marginal tests; this Story forbids that
as the fix. FE7's record hands such tests to T12 (`story-6457-fe7-fast-frontend-tests/evolution.md`, line 67).

## IN

- Find, for each known case, why it depends on order or load, and fix the test (or the shared kit) so it does not.
- Sweep a fixed set of shuffle seeds over the whole suite, to find order dependence beyond the two known tests.
- Proposal: an automated guard that keeps order independence ("Guard", open question 1).

Suspects, from reading the code (hypotheses):
| Case | Suspect | Prove by |
|---|---|---|
| `PortfolioDetail` under seeds 11 and 33 | Module-level mocks shared across tests. The file's `afterEach` is `vi.clearAllMocks()`, which clears calls but, in my reading of Vitest, keeps implementations and queued `mockResolvedValueOnce` values. The "update deferral" `beforeEach` installs a `mockSubscribeToFeatureUpdates` implementation that is never reset, and `mockGetUpdateStatus` is fed `...Once` values that a test ending early would leave queued. Which test runs before then decides what the next one sees | Run seeds 11 and 33, print the order, bisect to the leaking pair |
| Reports timeouts at load 11 to 20 | Tests that are fast alone but sit near 5 s of wall time when starved: long `findBy`/`waitFor` chains through wizard steps, `userEvent` sequences, MUI transitions. `createWizard` has about 1,200 lines and several `await next()` chains | Per-test time of the failing tests on a quiet run against the 5 s limit; rerun at worker counts above the core count |
| `pointer-events: none` in `createWizard` | `userEvent` refuses a click on an element MUI marks `pointer-events: none` while a Dialog or transition is opening or closing. Under load the click lands before the transition ends. The reopen test clicks "Cancel" then waits for the dialog to go; a click during the close is the likeliest place | Reproduce at a high worker count; log the element and its ancestors' style at failure |

"No longer timeouts as the fix" is a hard rule: a raised `testTimeout`, a per-test timeout or a `retry` setting never counts as the
fix. A test may carry an explicit timeout only if the cause is shown to be work the test legitimately does, and the reason is written.

## OUT

- Production behaviour and pixels (if a cause is in production code, stop and ask); speeding files up and budget numbers (T4, T13);
  backend, container and E2E tests.

## Done criteria, made checkable (proposals)

- **Seeds.** 11 and 33 (the known failures) plus 1, 2, 3, 5 and 7: 7 seeds, one full run each, each green. The count is a proposal.
- **Oversubscribed.** `LIGHTHOUSE_TEST_WORKERS=24` on the 12-core machine (twice the cores; the local default cap is 6,
  `testWorkers.ts`), with the load average above 12 for the whole run; record `uptime` before and after, add a CPU hog if the
  machine is quieter. "2 x cores" is my proposal, not a measured threshold.
- **Three in a row.** Three consecutive full runs, each oversubscribed, each with a different shuffle seed, each green, with no
  re-run between them. A red resets the count. A full run took about 650 s or 1,100 s on CI (memory note `reference_ci_vitest_duration_is_bimodal`);
  the local time is measured at DESIGN; three runs cost an estimated 30 to 60 minutes of an idle machine.
- Test count and coverage of the touched files do not drop; no test is skipped or deleted to get green. Mutation testing is N/A
  (no production file changes), recorded as an exception, as in T13.

## Guard

Proposal: extend the existing `ci_frontend.yml` run (via the `build-frontend` action) with `--sequence.shuffle` and a per-run seed
printed in the log, so a red replays with `--sequence.seed`. No new workflow. Cost: no extra CI time if it replaces the plain run
(estimate); a second full run adds the full run's time (estimate 10 to 18 minutes, from the 650 to 1,100 s above). Every later red
then needs its seed to replay, so it needs the maintainer's call. An automated check beats a written rule in `ci-learnings.md`.

## Dependencies

Hard: none (ADO #6539). Soft: T13 #6561 (below), T4 #6462 (the per-file budget). Slice-composition gate waived (D-6264-5).

## Order and overlap

- T13 now ends with slice A (the three `ModifyTeamSettings` files); its Reports and Overview slices are parked (D-6561-14, Q2).
  T12 is not expected to touch `ModifyTeamSettings` unless a seed shows it. Rule from T13: re-read each file at slice start; a
  conflict with trunk stops the slice, no autonomous rebase.
- T12 likely touches `PortfolioDetail.test.tsx`, `ReportsTab.createWizard.test.tsx`, `ReportsTestKit.tsx`, the Reports files that timed
  out, maybe `setupTests.ts`. Proposal: T12 before the parked T13 Reports slice, so T13 rewrites stable files.
- T6: no T6 brief exists in the repository, so its files are unknown. Open question 3.

## Slices (proposal, each at most 1 day, an estimate)
- A: `PortfolioDetail` order dependence, then the seed sweep (size depends on what it finds).
- B: the Reports load timeouts and the `pointer-events` failure, with the three-in-a-row proof.
- C: the CI guard, only if the maintainer says yes.

## Acceptance criteria

- [ ] The 7 seeds each give a green full run; the cause of each earlier failure is written down.
- [ ] Three consecutive oversubscribed full runs are green, with load and seed recorded.
- [ ] No timeout or `retry` increase is the fix; any explicit timeout carries a stated reason.
- [ ] Tests, coverage and production files unchanged; `pnpm test` and `pnpm build` clean; guard in place, or the decision recorded.

## Cross-cutting checklist (no silent N/A)

- RBAC: N/A, because only tests change; no endpoint, role or `useRbac()` use. `PortfolioDetail` mocks RBAC only as test data.
- Lighthouse-Clients (CLI/MCP versioning): N/A, because no API or contract changes.
- Website marketing surface: N/A, because nothing user-visible changes.
- Usage-data event: N/A, because a test fix shows no feature use to count.
- UI sketch: N/A, because no screen is added or changed.
- Terminology: N/A, because no user-facing words change; test copy keeps the seeded defaults (the wizard test mocks `getTerm`).
- Context map (ADR-270): N/A, because no context, concept, event or edge is touched; the Reports and Portfolios tests are only the files at issue.

## Open for the maintainer

1. Guard in CI (`--sequence.shuffle` with a printed, per-run seed). Options: (a) none, rely on the Story's one-time proof; (b) replace
   the plain CI run with a shuffled one, no extra time; (c) keep both, a second full run (estimate 10 to 18 minutes). Recommend (b),
   trying it for a week and reverting if reds appear that need a seed to replay.
2. Seed and load definitions: 7 seeds and 24 workers as proposed, or 3 seeds for speed. Recommend the 7 and 24.
3. Which files T6 touches, and whether T12 or T6 goes first (no T6 brief in the repository). Recommend T12 first, re-reading files.
4. If the sweep finds many more order-dependent files: carry on, or split a follow-up Story. Recommend a stop at 5 extra files, then ask.

## Maintainer decisions (2026-11-07)

These override the proposals above where they differ.

- **Guard:** `--sequence.shuffle` in the existing CI frontend run, seed printed. Tried for a week, reverted if reds need a
  seed to replay and that proves a nuisance.
- **Proof, replacing the 7 seeds and the three oversubscribed runs:** a short local sweep (seeds 11 and 33 plus a few more)
  to find and fix failures, then shuffle in CI. The Story is done after 5 green `main` runs with different seeds.
- **Load:** the known load failures are fixed; new ones are watched for rather than proven absent, because CI does not
  oversubscribe.
- **Scope:** fix up to 5 extra files, then pause T12, move on to the next Story and ask.
- **Order:** T12 before T6. No timeout or retry increase as the fix.
