# Slice 50 — T9 A tight budget for fast tests, and a ceiling for the slow tiers that only goes down

Epic #6506 · ADO #6536 · `@infrastructure` · job_id: infrastructure-only · **Size S** (estimate: two slices of at most a day) · DRAFT
infrastructure_rationale: one budget for all tests is too loose for logic tests and says nothing about the total time of the slow tiers.

## Goal and why

A slow test in the fast tier fails the build, and a slow tier whose time per test creeps up fails the build, while a feature that adds
tests does not. ADO #6536: median 0.3 ms (frontend logic), 0.9 ms (backend unit), 204 ms (backend integration, 76 % of backend test time,
CI run 41691382136). Today `check_budget.py` judges one unit (a frontend file, a backend class) against 56 s and 34 s
(`test-time-budget.json`); the frontend reporter has one per-test limit, 2 s local and 6 s on CI (`LIMIT_FACTOR` 3, T4). As read in
`check_budget.py`, the backend has no per-test limit and no tier has a total.

## Settled elsewhere (not reopened)

Fast is decided by time (T6 decisions); T9's per-test budget enforces it. Regular backend tests carry no marker; one category
`Integration` covers hosted and live tests. T6 slice D writes the tier into the CSV `category` column; the frontend tier is the extension
(`.test.ts` fast, `.test.tsx` component). T4's rule stays: a limit is at least 2.1 x the largest time seen.

## Proposals (each with its why)

1. **Fast-tier per-test budget, both stacks: 2.1 x the largest fast-tier test over at least 5 `main` runs after T6, rounded up.** No
   figure yet: the only one on record is 2.95 s, the largest unlisted frontend test (T4, `.ts` and `.tsx` together). Why: T4's own rule,
   nothing new to defend. The reporter gets a second, lower limit for `.test.ts`; `check_budget.py` gains a per-test check on rows whose
   category is not `Integration`. A test over it is a test to rethink; an allow-list entry needs a reason, as today.
2. **The `Integration` ceiling is on seconds per test, not on the sum.** Summed `Integration` seconds / `Integration` count, from the CSV,
   must stay under a ceiling that only goes down. Why: a ceiling on the sum fights every feature that lands an honest host test; one on
   the mean lets the count grow and still fails when tests get slower. Weakness: moving the fastest tests out would lift the mean; T5 and
   T10 move the slowest, so the risk is small.
3. **Runner-speed headroom.** Backend whole-run totals vary 1.13 x, slow to fast (T4: 2,556 to 2,883 s), so the mean is steady. Ceiling =
   the largest mean seen over at least 5 runs x 1.25 (an estimate; DESIGN confirms it from the measured max/min of the mean; T4's 2.1
   would be looser than the noise). Frontend whole runs vary 1.62 x (649 to 1,054 s), too wide for a mean ceiling: no frontend total
   here; the per-file budget (T13) and per-test limits stay its guard.
4. **How a new feature's tests fit.** Fast tests, or host tests that keep the mean, change nothing; the count may rise. A feature whose
   host tests lift the mean past the ceiling moves them to the port (T5) or raises the ceiling in a reviewed diff with a `reason`, as T4 does.
5. **Publish count, sum and mean.** `check_budget.py` writes them to the job's step summary and log each run. Why: that is how the
   maintainer sees how fast integration tests grow; the CSVs are already artifacts. No dashboard. Touches `ci_backend.yml` for the summary.
6. **The guard names what went over** (unit, seconds, limit, file; T4's `::error::` form); its tests plant a slow fast-tier test per stack
   and an over-ceiling mean. Calibration: at least 5 complete plain `main` runs after T6, ids listed as in T4.

OUT: rewriting tests (T10, T13), the tiers (T6), sharding (T8), production code. Dependencies: hard T6 #6512 (tier column, 5 runs after
its push) and T4 #6462 (pushed 2026-11-06); soft T10 lowers the ceiling at its end. Slice-composition gate waived (D-6264-5).

## Acceptance criteria

- [ ] A planted slow fast-tier test (each stack) or an `Integration` mean over its ceiling fails the build and is named; more tests at the same mean pass.
- [ ] Both limits derive from at least 5 plain `main` runs after T6, run ids written down; each run shows count, sum and mean in its summary.

## Cross-cutting (no silent N/A)

RBAC: N/A, no authorization code changes. Lighthouse-Clients: N/A, no route or DTO changes. Website: N/A, nothing user-visible. Usage-data
event: N/A, nobody uses test tooling in the product. UI sketch: N/A, no screen (the step summary is CI text). Terminology: N/A, no
configurable term. Context map (ADR-270): N/A, no context or concept touched. Mutation: N/A, no product file.

## Open for the maintainer

1. **Shape of the `Integration` ceiling.** (a) Mean per test, count free; (b) sum, raised by reviewed diff per feature; (c) both. Recommend
   (a): (b) makes every feature a negotiation, and the count is printed anyway.
2. **Frontend slow-tier total.** (a) None; (b) normalised by the logic tier's time in the same run (unproven). Recommend (a).
3. **Order against T10.** (a) T9 first, T10 lowers the ceiling in its last commit; (b) T10 first. Recommend (a).

## Maintainer decisions (2026-11-07)

These override the proposals above where they differ.

- **`Integration` ceiling: both.** Mean seconds per test fails the build and only goes down. The total also has a ceiling,
  and a feature that adds integration tests raises it in a reviewed diff with a reason. That is overhead on purpose: more
  integration time should be an explicit, justified act. So the total is the one ceiling that may go up, and only this way.
- **Frontend:** no total ceiling, because whole runs swing 1.62 x. T9 comes before T10.

## Changes from T6's DESIGN and DEVOPS (2026-11-07)

- The `category` column comes from T6's slice F, not D. `Integration` also covers container tests.
- Both ceilings leave connector classes out, or every run that forces the live suites goes red: live tests reach the CSV only
  when a connector's path changed, and any `ci*.yml` edit causes that.
- `check_budget.py` can write to `GITHUB_STEP_SUMMARY` itself, so publishing needs no `ci_backend.yml` edit, which would run
  every live suite once more.

## Test pyramid vision (maintainer, 2026-11-07)

Direction, not a gate: about 90 % fast, 9.5 % integration, 0.5 % E2E, by test count, as upper limits on the two upper layers.

- **Layers:** fast is the fast tier (`pnpm test:fast`, `Test-Fast.ps1`). Integration is everything in the middle, including frontend `.test.tsx`. E2E is everything in Playwright.
- **Moving down beats the limit:** a test moved from E2E to integration is good, even if the integration share goes over for a while.
- **The hard rule stays time:** the time ceilings gate the build; the shares are only reported, because a share can be met by adding trivial tests.
- **Where it stood on 2026-11-07:** frontend 3,648 fast and 5,857 `.test.tsx` of 9,505; about 56 Playwright tests, 0.25 % of about 22,600. The backend split is known only once T6 has labelled it.

T9 prints the three shares in its report, with no gate on them.

**Changes from T5's DESIGN and DEVOPS (2026-11-07).** The frontend budget is now 54 s, not 56 s. T5 keeps the slowest tests at the host, so proposal 2's line that it moves them is wrong. Calibrate after T5's last move, and read a host-to-port saving by class, not from the tier column. T5 raises the fast tier's mean by an estimated 3 to 4 %, up to about 9 %.
