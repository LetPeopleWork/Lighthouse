# Slice 40 — FE7 Frontend tests run in milliseconds: fix the slow ones, keep them fast

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size L** (split per hotspot at DESIGN)

## Goal

Frontend tests run in milliseconds, not seconds. The slow ones that exist get fixed, and a check stops new slow
tests landing.

## Why

On the Stryker bump (PR #1977, run 41173737474) the frontend suite took 438 s, 71 % of it in the tests
themselves. One file, `ReportsPage.removal.test.tsx`, took 48 s for 22 tests, and its "Select all picks every
row, on every page" test timed out at 6.2 s against Vitest's 5 s default on a loaded runner. It passes on `main`,
so it is a flake now, and it fails a CI run at random. Many tests render a whole page (React Query, router, RBAC,
licensing and terminology providers, the MUI data grid with up to 120 rows) in jsdom, drive it with user-event
and query by accessible role. Most of what they check is plain logic (pick limits, permission gating, how a
result is counted) that would test in milliseconds as a pure function or hook.

**Update 2026-10-31.** FE7 is now the frontend half of the test-infrastructure track (T1–T4, picked as next up).
The baseline has moved on: the frontend build-and-test step on main went from about 11 min in early October to
16–21 min in the last week of October, and the test files grew from 403 to 577 in October alone. Two additions to
the scope:

- **Memory.** The analysis also records the peak memory of a full `pnpm test` at today's settings (`pool:
  "threads"`, `maxWorkers` unset, so 11 workers on the 12-core development machine, with MUI inlined through
  `server.deps.inline`). T3 chooses the local worker cap from that data.
- **Story #6302's frontend follow-ups, never filed:** the production render cost of the MUI DataGrid and dialogs
  in jsdom, the cost left in every slow frontend file after #6302; and `ModifyTeamSettings.refinement.test.tsx`,
  which still sleeps 600 ms of real time and should move to a fake clock like its siblings.

FE7's ratchet and T4 share the frontend budget. Whichever lands first owns it, and the other drops that half.

## IN

- **Analyse first.** Profile the suite (per-file and per-test durations from Vitest's JSON reporter, plus a
  profile of the slowest files) and say which costs dominate: render size, role queries, user-event, `waitFor`
  polling, import/transform of inlined MUI, or worker contention. Measure; do not assume.
- **Fix what exists.** Push the logic the slow tests check down to unit tests of functions or hooks. Keep one or
  two page-level tests per flow to prove the wiring, give those small fixtures, and use cheaper queries where they
  say the same thing.
- **Prevent it.** An automated check in `pnpm test` / CI that fails on a test over a time budget, with a
  ratcheting allow-list for the slow ones that remain, in the same style as FE1's import-boundary ratchet
  (ADR-268, ADR-269). Record the test-design rule in `docs/ci-learnings.md` only if no tool can enforce it.

## OUT

- Any production behaviour or pixel change. Extracting logic out of a component so it can be tested as a
  function is in; changing what it does is not.
- E2E (Playwright). Backend test speed and memory (T1 #6459, T2 #6460). Worker caps (T3 #6461). The backend
  half of budget enforcement (T4 #6462).

## Hypothesis

Most of the wall time sits in a small number of page-level files, and moving their logic checks down to function
or hook tests cuts it by an order of magnitude without losing coverage or mutation kills. **Disproved if** the
profile shows the cost is mostly fixed per-file overhead (transform, environment setup) rather than per-test work;
then DESIGN re-scopes towards configuration (pool, environment, inlining) instead of test rewrites.

## Acceptance criteria

- [ ] DESIGN records the measured baseline: suite wall time, per-file and per-test durations, the slowest files,
  and which cost dominates.
- [ ] The tests over the budget are fixed or on the allow-list by name; the allow-list only shrinks.
- [ ] Given a new test over the budget, when `pnpm test` runs, then it fails naming that test.
- [ ] Line/branch coverage and the Stryker kill rate of the touched area do not drop.
- [ ] Suite wall time measured before and after, on the same machine, alternating runs.

## Note on "behaviour-free"

Unlike the other items, FE7 changes tests by design: page-level assertions move down to function and hook
tests. The Epic's "existing tests stay green unmodified" rule therefore reads here as: production code behaves
identically, `@screenshot` and Playwright are untouched, and every assertion a removed test made is still made
somewhere (DESIGN keeps a list of moved assertions).

## Dependencies

- Soft: FE1 (the ratchet pattern to copy; done). FE5 (splitting wide units makes their logic easier to test
  directly), in either order.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN: the profile and the time-budget ratchet
first, then one hotspot per slice.
