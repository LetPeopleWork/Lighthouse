# Feature Delta — story-6302-ci-test-time

ADO source: [User Story #6302 — Investigate why the backend and frontend CI stages got slower](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/6302) (state: `Active`, no parent Epic, as of 2026-10-14)
Density: `lean` (DISCUSS hard default). No Tier-2 trigger fired: one persona, one context (CI test suites), no AC ambiguity, no compliance terms, no walking skeleton.
Waves complete: DISCUSS; DESIGN (all choices locked 2026-10-14 under the maintainer's AFK recommended-option rule; ADR-237); DEVOPS (2026-10-14, same rule; DO-1..DO-7, usage-data event N/A); DISTILL (2026-10-14, same rule; DI-1..DI-13; 42 pending guard tests + evidence checks E-01..E-06e). Final-wave review findings applied 2026-10-14 across all four waves (maintainer away; decisions taken by the reviewers' recommendations).
Next wave: DELIVER (slice 01 first).

**Lineage:** follows `backend-test-speed` (#5258), which parallelised the backend suite and published per-test timing CSVs from both CI jobs (`Scripts/test-timings/`). This story uses those CSVs as its evidence and builds its guard on them.

**Prior waves:** DISCOVER and DIVERGE have no artifacts. For an investigation story the measurement below *is* the discovery; DIVERGE is not needed because the evidence points to one fix per cause rather than competing directions.

---

## Wave: DISCUSS / [REF] Persona ID

`lighthouse-developer`: the maintainer or a contributor pushing to `main` and waiting on `Build And Deploy Lighthouse`. In two weeks the wait went from ~11 to ~17 minutes for the frontend verify job and from ~13 to ~15 for the backend one, long enough to switch context, then come back to a result that is stale.

---

## Wave: DISCUSS / [REF] JTBD one-liner

**When** I push a change to `main`, **I want** the frontend and backend verify jobs to answer in about the time they took at the start of October, and to keep answering that fast as tests are added, **so I can** stay on the change instead of batching commits behind a slow signal.

Traces to the existing job `job-dev-test-feedback-velocity` in `docs/product/jobs.yaml`. No new job.

---

## Wave: DISCUSS / [REF] Evidence base (measured 2026-10-14)

Raw data: 157 runs of `Build And Deploy Lighthouse` on `main` since 24 Sep, 225 job logs and the `test-timings-{frontend,backend}` artifacts (analysis scratch, not committed). Jobs: `Verify Frontend / frontend` and `Verify Backend / backend`; only success and failure runs counted.

**Job duration, median minutes per day**

| Day | Frontend | Backend |
|---|---|---|
| 1–3 Oct | 11.2–11.4 | 13.1–13.2 |
| 5–6 Oct | 12.2–13.2 | 14.5–14.7 |
| 10–12 Oct | 12.1–14.0 | 14.6–15.8 |
| 13 Oct | 16.2 | 13.8 |
| 14 Oct | 16.8 | 15.3 |

**Where the time went**

| Fact | Evidence |
|---|---|
| Frontend growth is the test step and nothing else | `pnpm run sonarreport` (`vitest run --coverage`): ~9.0 min on 2 Oct, 13.9–14.7 min on 13–14 Oct. Sonar 1.5→1.7 min, build ~0.2, install ≤0.2. Vitest `Duration`: 541 s (run 36977323096, 403 files, 5,999 tests) → 832–882 s (38385736908, 38465699915, 38517425784; 459–478 files, 7,204–7,540 tests). The "tests" phase share rose 41 %→56 %; import and environment fell. |
| ~90 % of the frontend growth is 75 new test files | Summed per-file time 618 s → 1,376 s between 36977323096 and 38517425784. The 403 files that existed on 2 Oct: 618 → 696 s (+13 %). The 75 new files: **680 s**. |
| Eight new frontend files carry ~456 s | `RefinementView.paging` 115 s (11 tests, ~10 s each), `WorkItemsDialog.contexts` 105 s (108 tests; 0.7 s before story 6248), `BaseMetricsView.loading` 53 s, `ModifyTeamSettings.refinementNeed` 49 s, `ModifyTeamSettings.readiness` 39 s, `RefinementView.comments` 34 s, `DataGridBase.paging` 32 s, `RefinementView.votes` 29 s. From stories 6202, 6248, 6251 and the refinement work. |
| A further ~9 % uniform frontend slowdown from 13 Oct, cause open | Unchanged files ran at speed index 0.99–1.08 on 8–12 Oct, 1.05–1.13 after. Two runs with identical test sets (459 files, 7,204 tests): fc633d9dd 630 s vs 3377a2015 832 s. 3377a2015 is vite 8.3.2→8.3.4; MUI (641ac9c6f) and pnpm 12.10 (1bbde5105) bumps landed the same day. Runner variance (0.55–1.13 for the same commit) confounds it. **Inferred, not proven.** |
| Backend growth is the test step; +24–28 % tests | `Test Backend (with Coverage)` ~9.0 → 10.2–10.8 min; dotnet 8m46s / 7,959 tests (36977323096) → 11m46s / 10,198 tests (38570239004). Build ~2.0, Sonar end ~1.2, NuGet 0.4 min: flat. |
| Backend: 52 % of the summed growth is new test classes, and 288 s of it is 34 new integration classes | Summed class time 1,991 → 2,653 s (+662 s). All 88 new backend classes add 346 s of that growth (52 %). The 34 new classes under `API.Integration.Refinement.`, `.Reports.` and `.TeamFeatureSize.` sum to 288 s in run 38570239004. The largest: `Refinement.Slice13Readiness` 31 s, `TeamFeatureSize.Slice01` 25 s, `Refinement.Slice11CastAVote` 19 s, `Slice03StageRules` 19 s, `Slice04RefinementCadence` 18 s, `Reports.Slice01TeamReportScenarios` 18 s, `Slice12Comments` 18 s, … (these classes do not use `IntegrationTestBase`: their harness bases — `RefinementAcceptanceTest`, `ReportsAcceptanceTest`, `TeamFeatureSizeAcceptanceTest` — build a new `TestWebApplicationFactory` host in `[SetUp]` for every test; corrected in DESIGN. Whether each test also gets a fresh database was not measured). |
| Backend: the other 48 % is in old classes, where a few tests now take 40–60 s alone | 38570239004: `S8_EmbedEntryPointTests…ReturnPathPointsOffHost("https://evil.example/steal")` 62 s, `CacheTest.ConcurrentReadersAndWriters…` 61 s, `BlockedCountSnapshotMigrationTests…OnPostgres` 51 s, `DataProtectionKeyRingTest…Redis` 43 s. Which of these are waits/timeouts vs container start-up is not yet known; the agent judged the per-run growers in old classes noisy. |
| Test-count baselines (14 Oct) | Frontend **7,630**, from run 38553818860 (14 Oct 11:02, commit b73a10fad): Vitest `Tests 7585 passed \| 44 skipped \| 1 todo (7630)`, `Test Files 479 passed \| 2 skipped (481)`. The 7,204–7,540 counts quoted above come from earlier 13–14 Oct runs. Backend **10,198**, from run 38570239004. |
| Ruled out (measured) | Runner image (ubuntu-24.04, 20260927 → 20261004; unchanged files ran at index ~1.0 on the new image 8–12 Oct). NuGet and Sonar caches always hit; pnpm cache misses on lockfile changes but install stays ≤0.2 min. Vitest pool/worker/coverage config unchanged in the window. |

---

## Wave: DISCUSS / [REF] Locked decisions

| ID | Decision | Verdict | Rationale |
|---|---|---|---|
| D1 | Feature type | Cross-cutting (developer experience) | Test suites of both stacks plus the CI timing scripts. No product surface. |
| D2 | Walking skeleton | No | Brownfield optimisation. |
| D3 | UX research depth | Lightweight | One persona, the developer. |
| D4 | JTBD | Yes, reuse `job-dev-test-feedback-velocity` | The developer is the user; not the infrastructure-only escape valve. |
| D5 | Scope | **Fix both stacks** | Maintainer decision 2026-10-14 (offered: fix frontend + A/B vite only; fix both stacks; name the cause and file follow-ups). |
| D6 | Regression guard | **Per-file time budget read from the existing timing CSVs** | Maintainer decision 2026-10-14. A slow file is caught the day it lands, not two weeks later as a trend. Applies to both stacks (frontend per file, backend per class), because the CI jobs already write both CSVs (`vitest_to_csv.py` → `test-timings-frontend.csv`, `trx_to_csv.py` → `test-timings-backend.csv`; `summarise.py` only prints a top-N table from the raw reports — corrected in DESIGN). The frontend fails the build from the start. The backend warns until its budget is calibrated, then fails: its set of classes changes from run to run and coverage slows it unevenly, so a limit set before the fixes land would trip on noise. |
| D7 | Behaviour preserved | Same tests, same assertions | A faster test must still fail on the bug it guards. No test deleted, skipped or weakened; frontend and backend test counts do not drop; coverage on the touched areas does not drop. |
| D8 | The vite question is settled by an A/B, not by reverting on suspicion | Local A/B of vite 8.3.2 vs 8.3.4 on an unchanged test set, ≥3 runs each | CI runner variance (0.55–1.13 for one commit) is too large to judge a ~9 % effect from CI runs. |
| D9 | Measuring a win | Median of ≥5 `main` runs after a slice lands | Single runs swing by several minutes with the runner. |
| D10 | ADO shape | No child Stories | Mirrors #5258: slices tracked here and in slice briefs; #6302 is the one item. A cause we decide not to fix gets its own follow-up item (the story's "done when"). |

---

## Wave: DISCUSS / [REF] Cross-cutting impact checklist

- **RBAC**: N/A, because only tests and CI scripts change; no endpoint, role or `IRbacAdministrationService` path is touched.
- **Lighthouse-Clients (CLI + MCP)**: N/A, because no API contract or DTO changes.
- **Website**: N/A, because nothing user-visible or marketed changes.
- **Sketch UI before building**: N/A, because no screen changes.
- **Loading and failure looks (DESIGN)**: N/A, because no view fetches data differently.
- **Usage-data event (DEVOPS)**: expected N/A, because nothing a Lighthouse user does changes; DEVOPS answers it explicitly.
- **Configurable terminology**: N/A, because no docs or UI copy.
- **Each Epic ships on its own**: N/A, because this is a single Story.

---

## Wave: DISCUSS / [REF] User stories with elevator pitches

### US-01 — Settle whether the 13 Oct dependency bumps slowed every frontend test

**As** a Lighthouse developer **I want** to know whether vite 8.3.4 (or MUI / pnpm 12.10 from the same day) slowed the whole Vitest run **so that** I either reclaim ~9 % for free or stop suspecting it.
**job_id**: `job-dev-test-feedback-velocity`

#### Elevator Pitch
Before: a ~9 % slowdown of unchanged frontend tests from 13 Oct is visible in CI but buried in runner variance; nobody knows whether a dependency caused it.
After: run `pnpm vitest run` ≥3× on each side of the A/B → sees the median Vitest `Duration` for vite 8.3.2 and 8.3.4 (and, if vite is clean, the MUI and pnpm bumps) recorded in this delta.
Decision enabled: pin or report upstream if one bump costs time; otherwise drop the hypothesis and record it.

#### Acceptance criteria
- AC-01.1: Median Vitest `Duration` for each A/B arm, ≥3 runs each on the same machine and the same test set, is recorded with the commit it ran on.
- AC-01.2: An arm's spread is the min–max of its ≥3 runs. A difference counts only when the slower arm's minimum exceeds the faster arm's maximum (the two spreads do not overlap). If it counts, the cause is fixed (pin, config, or upstream issue linked) and the fix is shown on the A/B; otherwise the hypothesis is recorded as disproved.

### US-02 — The two slowest new frontend files run in a fraction of their time

**As** a Lighthouse developer **I want** `RefinementView.paging.test.tsx` and `WorkItemsDialog.contexts.test.tsx` to stop costing 220 s of test time **so that** the frontend job sheds most of its new weight in one step.
**job_id**: `job-dev-test-feedback-velocity`

#### Elevator Pitch
Before: two files added this month take 115 s and 105 s; one paging test runs ~10 s on its own.
After: run `pnpm vitest run RefinementView.paging WorkItemsDialog.contexts` → sees both files pass with the same test names, together in ≤40 s locally, and the next CI `test-timings-frontend.csv` shows each file down against its 14 Oct time (115 s and 105 s).
Decision enabled: whether the cause (large row counts through the real DataGrid, per-test full renders, `waitFor` timeouts) is the pattern to apply to the next six files.

#### Acceptance criteria
- AC-02.1: Both files keep every test name and assertion (diff reviewed for D7); test count per file unchanged or higher.
- AC-02.2: Summed time of the two files in CI drops from ~220 s to ≤40 s (median of ≥5 runs, D9).
- AC-02.3: The reason each was slow is written down (one line each) so slice 03 can reuse it.

### US-03 — The next six slow frontend files follow

**As** a Lighthouse developer **I want** the remaining slow new files sped up the same way **so that** the frontend test step is back near its early-October time.
**job_id**: `job-dev-test-feedback-velocity`

#### Elevator Pitch
Before: `BaseMetricsView.loading`, `ModifyTeamSettings.refinementNeed`, `ModifyTeamSettings.readiness`, `RefinementView.comments`, `DataGridBase.paging` and `RefinementView.votes` take ~236 s together.
After: run `pnpm test` → all green; the CI `Verify Frontend / frontend` job median is ≤12 min, and `test-timings-frontend.csv` shows each of the six files down against its 14 Oct time.
Decision enabled: whether the frontend is fixed or a further cause remains (then named in a follow-up item).

#### Acceptance criteria
- AC-03.1: D7 holds for all six files.
- AC-03.2: Summed CI time of the six drops from ~236 s to ≤70 s.
- AC-03.3: After US-01..03, `Verify Frontend / frontend` median job time over ≥5 `main` runs is ≤12 min.

### US-04 — New backend integration classes stop paying full set-up per test

**As** a Lighthouse developer **I want** the new Refinement, Reports and TeamFeatureSize integration classes to share what they safely can **so that** backend growth stays proportional to the coverage added, not to the set-up repeated.
**job_id**: `job-dev-test-feedback-velocity`

#### Elevator Pitch
Before: 34 new `API.Integration` classes under Refinement, Reports and TeamFeatureSize add 288 s of summed time (run 38570239004, 14 Oct); their harness bases (not `IntegrationTestBase`) build a whole new `TestWebApplicationFactory` host for every test, and nobody knows how much of that is set-up.
After: run `dotnet test --filter "FullyQualifiedName~API.Integration.Refinement|FullyQualifiedName~API.Integration.Reports|FullyQualifiedName~API.Integration.TeamFeatureSize"` → sees the same tests green, and the `FixtureSetupTimer` output plus `test-timings-backend.csv` show set-up share and summed time for those classes, down against the 14 Oct baseline.
Decision enabled: whether the fixture-reuse pattern from `backend-test-speed` slice 06 should become the default for new integration classes.

#### Acceptance criteria
- AC-04.1: Set-up time and body time are reported per harness, for every class in scope, before any change. The instrument is `FixtureSetupTimer`, first added to the three harness bases, which do not call it today.
- AC-04.2: In classes where set-up dominates, the set-up cost per test drops (the mechanism is DESIGN's choice); parallel-safety rules from `docs/ci-learnings.md` and the `[NonParallelizable]` allowlist guard still hold.
- AC-04.3: D7 holds; summed time of the in-scope classes drops by at least a third, or the slice records why set-up is not the cost.

### US-05 — The 30–60 s single backend tests are explained, and fixed where they wait for nothing

**As** a Lighthouse developer **I want** to know why a handful of old backend tests each take 40–60 s **so that** time spent waiting on a timeout or a container is either removed or knowingly accepted.
**job_id**: `job-dev-test-feedback-velocity`

#### Elevator Pitch
Before: `S8_EmbedEntryPointTests` (one case 62 s), `CacheTest.ConcurrentReadersAndWriters…` (61 s), `BlockedCountSnapshotMigrationTests…OnPostgres` (51 s), `DataProtectionKeyRingTest…Redis` (43 s) dominate the slowest-test list, and it is unknown whether that is a timeout, a retry, or a container starting.
After: run `dotnet test --filter "FullyQualifiedName~S8_EmbedEntryPointTests"`, and the same with `CacheTest`, `BlockedCountSnapshotMigrationTests` and `DataProtectionKeyRingTest` → sees each class's time with the cause noted in this delta, and every timeout- or wait-bound test, once fixed, taking under 10 s locally.
Decision enabled: fix, or accept with a reason (container start-up is real work), recorded so the budget guard can allow it.

#### Acceptance criteria
- AC-05.1: Each of the four tests has a one-line cause, measured locally and confirmed against ≥3 CI timing CSVs.
- AC-05.2: Any test whose time is a wait or timeout that does not test the behaviour is fixed with D7 holding; the rest go on the guard's allowlist with that reason.

### US-06 — A slow test file fails the build the day it lands

**As** a Lighthouse developer **I want** CI to fail the build on any frontend test file over its time budget, and to warn on any backend test class over its budget until that budget is calibrated, then fail **so that** this kind of drift is caught on the commit that causes it.
**job_id**: `job-dev-test-feedback-velocity`

#### Elevator Pitch
Before: a 115 s test file merged on 12 Oct and was noticed only as a two-week trend in job durations.
After: push a commit adding a frontend file over budget → sees `Verify Frontend` fail with a line naming the file, its time and the budget, and pointing to the allowlist. A backend class over budget shows the same line as a warning on a green `Verify Backend` until the backend budget is calibrated and switched to failing; after that it fails too. A commit under budget passes as today.
Decision enabled: speed the file up, or add it to the allowlist with a reason, before it merges.

#### Acceptance criteria
- AC-06.1: The check judges the run that just finished without a second test run: it reads the `test-timings-*.csv` the jobs already produce.
- AC-06.2: Per stack, the budget follows the DS-Q1 rule (at least 2.1 × the largest non-allowlisted unit, at most 0.55 × the smallest regression it must catch) and its derivation is recorded. Pass when zero non-allowlisted units are over the budget across the ≥5 `main` runs after the budget commit; any over-budget unit there fails the criterion. The runs the budget was derived from do not count as evidence, because they sit under it by construction.
- AC-06.3: A planted over-budget frontend file turns the check red. A planted over-budget backend class prints a warning and leaves the job green while the backend is in warn mode, and turns the check red once it is switched to failing. An allowlisted unit stays green; the allowlist carries a reason per entry.
- AC-06.4: `docs/ci-learnings.md` gains the rule: a new test file over the budget fails CI; speed it up or allowlist it with a reason.

---

## Wave: DISCUSS / [REF] Definition of Done (feature)

1. The cause of each stack's slowdown is named with evidence in this delta (done for the measured part; US-01 and US-05 settle the two open ones).
2. US-01..US-06 acceptance criteria met.
3. D7 held: no test deleted, skipped or weakened; per-stack test counts do not drop.
4. `pnpm test`, `pnpm build`, `dotnet build` and the filtered `dotnet test` all green; no new SonarCloud issues.
5. Mutation ≥80 % on any production file touched (expected: none; test and script changes only).
6. KPI medians below met over ≥5 `main` runs, or a follow-up ADO item records the remaining cause and why it is not fixed here.
7. `docs/ci-learnings.md` updated (US-06).
8. #6302 reaches `Resolved`; no child Stories (D10).

---

## Wave: DISCUSS / [REF] Out-of-scope

- Runner type, larger runners or sharding the Vitest run across jobs: the cost is in the tests, and runner variance is not the trend.
- Changing the Vitest pool, worker count or coverage provider: unchanged in the window, not the cause.
- The live-connector categories and the E2E / Playwright jobs.
- Rewriting older frontend files that did not grow (+13 % on the 2 Oct set is within the uniform slowdown US-01 checks).
- Splitting the backend test project or changing the database provider for tests.

---

## Wave: DISCUSS / [REF] WS strategy

Not applicable: D2 = no walking skeleton.

---

## Wave: DISCUSS / [REF] Driving ports

| Surface | Form | Stories |
|---|---|---|
| `pnpm test` / `pnpm vitest run <file>` | Local CLI | US-01, US-02, US-03 |
| `dotnet test --filter …` | Local CLI | US-04, US-05 |
| `Build And Deploy Lighthouse` → `Verify Frontend` / `Verify Backend` | GitHub Actions | all (KPIs), US-06 (guard) |
| `Scripts/test-timings/*.py` + `test-timings-*.csv` artifacts | CI scripts | US-06 |

No HTTP, UI or client surface changes.

---

## Wave: DISCUSS / [REF] Pre-requisites

- Timing CSVs already published by `ci_frontend.yml` and `ci_backend.yml` (verified).
- `FixtureSetupTimer` and the `[NonParallelizable]` allowlist guard from `backend-test-speed` are in place; US-04 must keep the guard green.
- Prior art: `backend-test-speed` slice 06 (per-fixture WAF reuse, read-API cluster).
- In a worktree, copy `valid_not_expired_license.json` from the main checkout before judging backend results.

---

## Wave: DISCUSS / [REF] Scope assessment

| Heuristic | Threshold | Observed | Verdict |
|---|---|---|---|
| User stories | >10 | 6 | Pass |
| Bounded contexts / modules | >3 | 2 test suites + CI scripts | Pass |
| Effort | >2 weeks | ~4 days | Pass |
| Independent outcomes | several → split | one outcome: CI verify time back down and kept down | Pass |

**Scope Assessment: PASS.**

---

## Wave: DISCUSS / [REF] Story map and slice plan

```
 settle the open cause → slim frontend tests → slim backend tests → guard against drift
      slice 01             slices 02, 03          slices 04, 05          slice 06
```

| Slice | Story | Effort | Disproves if it fails |
|---|---|---|---|
| 01 vite-ab | US-01 | ½ d | "A dependency bump slowed every test" |
| 02 frontend-two-worst | US-02 | 1 d | "The new files are slow for a fixable reason, not because their coverage is inherently expensive" |
| 03 frontend-next-six | US-03 | 1 d | "One pattern explains the slow new frontend files" |
| 04 backend-new-integration | US-04 | 1 d | "Set-up, not test bodies, is what the new integration classes pay for" |
| 05 backend-outliers | US-05 | ½ d | "The 40–60 s tests are waiting on nothing" |
| 06 budget-guard | US-06 | ½ d of work, plus elapsed time for ≥5 post-fix `main` runs before calibrating and ≥5 more before the backend flip | "A budget can be set that catches a slow file without tripping on runner variance" |

**Order:** 01 first (cheapest, highest uncertainty, and its result changes the baseline the other slices are measured against); 02 then 03 (biggest win, and 02's finding sets 03's pattern); 04, 05; 06 last, because the budget is calibrated on the post-fix distribution.

Every slice has developer-visible output (a faster command, a recorded measurement, a red build); none is purely `@infrastructure`. Slices 01 and 05 are investigation slices: their output is a recorded measurement and a verdict, plus whatever fix the verdict calls for. All use production CI data, not synthetic timings.

---

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI ID | Measure | Baseline (14 Oct) | Target | Method |
|---|---|---|---|---|
| OUT-6302-fe-job | `Verify Frontend / frontend` median job minutes | 16.8 | ≤12 | ≥5 success/failure runs on `main` after slice 03 |
| OUT-6302-be-job | `Verify Backend / backend` median job minutes | 15.3 | ≤13.5 | ≥5 runs on `main` after slice 05 |
| OUT-6302-fe-slow-files | Summed CI time of the eight named frontend files | ~456 s | ≤110 s | Median over ≥5 `main` runs, from `test-timings-frontend.csv` |
| OUT-6302-be-new-integration | Summed CI time of the 34 in-scope classes under `API.Integration.Refinement.`, `.Reports.` and `.TeamFeatureSize.` | 288 s (run 38570239004) | ≤192 s (a third less) | Median over ≥5 `main` runs, from `test-timings-backend.csv` |
| OUT-6302-coverage-kept | Passed + skipped + todo test total per stack | Frontend 7,630 (run 38553818860: 7,585 passed, 44 skipped, 1 todo); backend 10,198 (run 38570239004) | not lower than the named run, per stack | CI logs (Vitest and `dotnet test` summaries); backend compared only on runs with the same test filter |
| OUT-6302-guard | Over-budget file fails the build | none | red on a planted file | US-06 AC-06.3 |

---

## Wave: DISCUSS / [REF] Definition of Ready (9-item gate)

1. Persona: ✓ `lighthouse-developer` — the maintainer or a contributor who pushes to `main` and waits on both verify jobs before moving on; at ~17 min the wait is long enough to switch context and lose the thread.
2. JTBD: ✓ `job-dev-test-feedback-velocity` (existing).
3. Stories sized: ✓ 6 stories, each ≤1 day.
4. ACs testable: ✓ each has a timing threshold, a recorded measurement or a binary gate.
5. Out-of-scope explicit: ✓ 5 items.
6. Pre-requisites: ✓ listed and verified.
7. KPIs measurable: ✓ baselines measured, targets numeric, method named.
8. Cross-cutting concerns: ✓ checklist above, each with a reason.
9. Handoff: ✓ DESIGN (light-touch).

---

## Wave: DISCUSS / [REF] Changed assumptions

The story assumed tooling might be the cause ("Dependency or tooling changes from Renovate … around 10–13 Oct", "Cache hits … runner type"). Measured: runner image, caches, install, build and Sonar are flat; the growth is test bodies. One tooling suspect remains (vite 8.3.4 and same-day bumps, ~9 %), carried as US-01.

## Wave: DESIGN / [REF] Changed assumptions

Corrected in place above (evidence table, D6, US-04 pitch, AC-04.1) so DISTILL does not inherit them.

| Original sentence (verbatim) | Corrected fact |
|---|---|
| "because `Scripts/test-timings/summarise.py` already produces both" (D6) | `summarise.py` does not produce the CSVs. `vitest_to_csv.py` writes `test-timings-frontend.csv` and `trx_to_csv.py` writes `test-timings-backend.csv`; `summarise.py` reads the raw TRX / Vitest JSON and prints a top-N table. |
| "each fixture builds its own `TestWebApplicationFactory` and gets a fresh database per test via `IntegrationTestBase`" (evidence table) and "each builds its own `TestWebApplicationFactory` and a fresh database per test" (US-04 pitch) | The new Refinement, Reports and TeamFeatureSize classes do not use `IntegrationTestBase`. Their harness bases build a whole new host (`TestWebApplicationFactory` + `WithWebHostBuilder`) in `[SetUp]` for every test. `IntegrationTestBase` itself already reuses one host per fixture. |
| "set-up vs body time is measured before any change (`FixtureSetupTimer`)" (AC-04.1) | `FixtureSetupTimer` is called only from `IntegrationTestBase`; the three harness bases need its calls added first (test-only change) before AC-04.1 can be met. |

---

## Wave: DISCUSS / [REF] Wave-decisions summary

- **Primary job:** `job-dev-test-feedback-velocity`.
- **Feature type:** cross-cutting, developer experience.
- **Decided:** fix both stacks (D5); per-file/per-class budget guard on the existing timing CSVs (D6); behaviour preserved (D7); A/B before acting on vite (D8); medians of ≥5 runs (D9); no child Stories (D10).
- **Handoff:** DESIGN, light-touch: the guard's shape (inside `summarise.py` or its own script; budget derivation; allowlist file) and whether fixture reuse becomes the default base for new integration classes.

---
---

# DESIGN

Density: `lean`, Tier-1 [REF] only. Scope: application / components (solution-architect, PROPOSE mode). The contested choices were first written as proposals; with the maintainer AFK, each was **locked under the recommended-option rule (none changes what a user sees); the maintainer may revisit**. ADR-237 records the budget guard.

Prior-wave reading: ✓ this DISCUSS delta + six slice briefs, ✓ journey `story-6302-ci-test-time.yaml`, ✓ `brief.md` backend-test-speed delta + ADR-074, ✓ `backend-test-speed` DESIGN, slices 04 and 06, ✓ `Scripts/test-timings/` (3 scripts, 3 test modules) and the two workflow steps that call them, ✓ `IntegrationTestBase`, `TestWebApplicationFactory`, `FixtureSetupTimer`/`FixtureTimingReporter`, `BackendTestParallelizationGuardTest`, ✓ the Refinement, Reports and TeamFeatureSize harness bases, ✓ `vitest.config.ts`, the two slowest frontend files and their test kits, ✓ `docs/ci-learnings.md` (timing, `[NonParallelizable]`, budget, timeout entries).

Outcome Collision Check: **skipped** — no product typed-contract surface (no endpoint, DTO or aggregate); the OUT-6302-* KPIs are internal developer-velocity measures, as for `backend-test-speed`.

## Wave: DESIGN / [REF] Code facts that shape the design

| # | Fact | Consequence |
|---|---|---|
| F1 | The CSVs come from `trx_to_csv.py` and `vitest_to_csv.py`; `summarise.py` reads the **raw** TRX / Vitest JSON and prints a top-N table for people. | The guard reads CSVs (AC-06.1). `summarise.py` is not the natural home for it. |
| F2 | `vitest.config.ts` sets `bail: 1` on CI. A failing run stops early and leaves a partial `test-results.json`. | The guard runs only after a green test run; a red one is already red, and its timings are partial. |
| F3 | Backend CI runs `Category!=Integration` plus whichever live-connector categories (`JiraIntegration`, `AdoIntegration`, …) the diff selected. The CSV's `category` column knows only `Integration`/`Unit`, so connector classes show as `Unit` on the runs that include them. | The set of backend classes differs from run to run; a backend allowlist cannot check for stale entries against one run, and network-bound connector classes appear only sometimes. |
| F4 | Backend `fully_qualified_name` = `class.method(args)`, and arguments can contain dots (`"https://evil.example/steal"`). | Class = the part before the last `.` once the `(…)` tail is removed. A fixture row pins this. |
| F5 | `IntegrationTestBase` already builds **one** host per fixture (constructor) and gives each test a fresh SQLite file. The Refinement, Reports and TeamFeatureSize classes do **not** use it: their harness bases build a new `TestWebApplicationFactory` + `WithWebHostBuilder` host in `[SetUp]` for **every test**, with a licence mock, a fake clock or a command interceptor captured per test. | "Adopt slice-06 reuse" here means moving host build to `[OneTimeSetUp]` in harnesses of exactly the kind slice 06 left alone as risky (per-test mocks). |
| F6 | `FixtureSetupTimer` is called only from `IntegrationTestBase`. | AC-04.1 needs the three harness bases instrumented first (test-only change). |
| F7 | `Scripts/test-timings/tests/` are stdlib `unittest` modules; no workflow runs them. | Whatever the guard adds there is not exercised in CI unless a step runs it. |
| F8 | Both slow frontend files already use a kit (`RefinementTabTestKit`, `WorkItemsDialogTestKit`) and a `RENDER_HEAVY = 20000` timeout. Paging: 143 rows through the real DataGrid per test, and `theRowsOnThisPage` runs `getAllByRole("row")` then `within(row)` per row. Contexts: 3 rows per test, 108 tests, each mounting the full dialog + DataGrid, and every kit helper starts from `screen.getByRole("dialog")`. | Two cost shapes share a DataGrid mount and repeated `*ByRole` scans over the grid's DOM. To be confirmed by profiling in slice 02, not assumed. |

## Wave: DESIGN / [REF] DDD list

| ID | Decision | Verdict | Status |
|---|---|---|---|
| DDD-1 | Guard input | The `test-timings-*.csv` files the jobs already write; column names come from the converters' `CSV_COLUMNS` (one source for the column contract) | Locked (AC-06.1, F1) |
| DDD-2 | Guard runs after a green test run only, as the **last** step of each verify job | Default `success()` condition. Placed after the Sonar end + metadata steps, so a budget breach never skips Sonar; the CSV upload keeps `if: always()` | Locked (F2; must not mask or pre-empt test failures) |
| DDD-3 | Guard shape: where it lives, the unit, the budget rule, the allowlist file | New `check_budget.py`; summed time per FE file / BE class; fixed seconds per stack, B ≥ 2.1 × L and B ≤ 0.55 × the smallest regression to catch (DS-Q1); JSON allowlist with reason + ceiling; fail closed; self-tests in the step | **LOCKED** (AFK recommended-option rule; ADR-237) |
| DDD-4 | Backend stack in the guard | Both wired; frontend fails, backend warns until ≥5 backend `main` runs after the budget commit show zero budget warnings, then fails | **LOCKED** (AFK recommended-option rule; ADR-237) |
| DDD-5 | Backend harness host lifetime | Option A: host once per fixture, per measured harness | **LOCKED** (AFK recommended-option rule) |
| DDD-6 | No new `[NonParallelizable]`; the allowlist guard and its seven entries stay as they are | Host reuse keeps fixture-level parallelism and a host per fixture | Locked (AC-04.2, ADR-074) |
| DDD-7 | Measure before changing: `FixtureSetupTimer` added to the three harness bases' set-up/tear-down | Test-only, reuses the existing timer | Locked (AC-04.1, F6) |
| DDD-8 | Frontend slow-file fixes | Option B: inside the existing kits, otherwise per file | **LOCKED** (AFK recommended-option rule) |
| DDD-9 | Behaviour kept (D7): no assertion moves to a cheaper level, no `*ByRole` used *as the assertion* becomes a CSS selector | A role query that is only *finding* an element may change; one that *is* the check stays | Locked (D7) |
| DDD-10 | Guard core is a pure function (rows + budget + allowlist → breaches); the script shell only reads files, prints, and sets the exit code | Functional core / imperative shell inside one script | Locked (testability; no side effects in the decision) |

## Wave: DESIGN / [REF] Locked choices (DS-Q1..Q3)

All three were written as proposals; the maintainer was AFK. **Locked under AFK by the recommended-option rule; maintainer may revisit.** None of them changes what a Lighthouse user sees.

### DS-Q1 — Budget guard (US-06, D6) — LOCKED, ADR-237

- **Where**: a new `Scripts/test-timings/check_budget.py` that reads the CSVs and takes its column names from the converters. Local and CI run the same command.
- **Unit**: summed test time per frontend file, and per backend class (class = the full name before the last `.` once the `(…)` argument tail is removed, F4).
- **Budget**: fixed seconds per stack, one formula everywhere. Let L be the largest non-allowlisted unit observed over ≥5 post-fix `main` runs. Then:
  - **B ≥ 2.1 × L.** The same commit runs anywhere from 0.55× to 1.13× its typical time, a ratio of about 2.05 between the fastest and the slowest runner, so a unit seen at L on a fast runner can take about 2.05 × L on a slow one. A 1.25× headroom would trip on an ordinary slow runner.
  - **B ≤ 0.55 × the smallest regression the guard must catch,** so that regression still trips the guard on the fastest runner. For the frontend that regression is the 115 s file that started this story, so B ≤ about 63 s, which in turn means L must be ≤ about 30 s.
  - **If both cannot hold,** the units that break the lower bound (larger than B / 2.1) go on the allowlist, each with a reason and its own ceiling, and L is recomputed without them.
  - The derivation (the runs used, L, the regression anchor, B, and any unit moved to the allowlist) is recorded here, next to the number, and in `ci-learnings.md`, and re-derived if the runner population changes.
- **Allowlist**: one checked-in JSON file beside the script, with a section per stack, each entry with a reason and its own ceiling in seconds. An entry's ceiling is at least 2.1 × that unit's largest observed time over the derivation runs, for the same runner-spread reason as the budget. Frontend fails on an entry whose file is absent from the CSV; backend checks only that the class still exists in source (F3).
- **Stacks**: both wired. Frontend fails the job; backend prints the same breach as a warning, then a one-line switch makes it fail. The switch needs ≥5 backend `main` runs **after the budget commit** with zero budget warnings, measured as the largest backend unit time on each of those runs against B. The runs the budget was calibrated on cannot serve: B is derived from them, so they are under it by construction.
- **Missing or malformed CSV after a green run**: fail closed, with a message naming the file.
- **Self-proof**: the step runs `python3 -m unittest discover Scripts/test-timings/tests` before the check, which also brings the existing converter tests into CI (F7).

Rejected:
- Extend `summarise.py` — it reads raw reports, not the CSV, and is a display tool, not a gate.
- A check inside each suite (Vitest reporter / NUnit hook) — two mechanisms in two languages; NUnit has no clean point after all timings exist.
- Frontend file wall time from the Vitest JSON — needs a new column on a shared artifact and adds shared import/environment noise.
- Percentile of the run's own distribution — rises with uniform drift, and the offender inflates it.
- Speed index against the run's median file — the median is milliseconds, so the ratio is noisy and the message is hard to read.
- Inline allowlist in the script — every entry would edit the gate's code.
- Marker in the test file — needs a source scan and a comment that only cites a rule.
- Both stacks failing from day one — backend class sets vary per run and coverage slows CPU-bound classes ~7×; flake risk before calibration.
- Frontend only — leaves the backend outliers that started this story unwatched.
- Skip-with-warning on a missing CSV — a guard that passes on no data proves nothing.

### DS-Q2 — Host lifetime for the new backend harnesses (US-04) — LOCKED, option A

Per harness, after measuring: where `FixtureSetupTimer` shows set-up dominates, the harness base builds its host once per fixture in `[OneTimeSetUp]` and disposes it in `[OneTimeTearDown]`. Per-test inputs (licence answer, clock position, command counter) are reset in `[SetUp]` through objects the fixture owns, never through a DI registration that captures a per-test field. A harness where the body dominates is recorded and left alone. No new `[NonParallelizable]`; a host per fixture; parallel safety shown with 3 consecutive full-assembly runs. A reusable default base is considered later, only if these numbers show reuse pays.

Rejected:
- B, reusable host as the default base now — touches ~20 harnesses before any evidence, carries the host-outlives-test bleed risk, and a hook in `IntegrationTestBase`'s constructor is a virtual call in a constructor (Sonar S1699).
- C, keep a host per test and cut only database cost — changes `TestWebApplicationFactory` (~70 fixtures) for a share nobody has measured.

### DS-Q3 — Frontend slow-file pattern (US-02/03) — LOCKED, option B

Fix inside the existing kits (`RefinementTabTestKit`, `WorkItemsDialogTestKit`) where profiling shows the cost sits in a kit helper; per file otherwise. Causes to profile, not assume (F8): DataGrid mount per test, `*ByRole` scans over grid DOM, `user.type` keystroke delays, a `waitFor` running to its timeout.

Rejected:
- A, per-file only — sibling files on the same kit (`RefinementView.comments`, `.votes`) would repeat the fix.
- C, a new shared DataGrid helper across kits — only if slices 02 and 03 both find the same hotspot, in both kits and outside them.

## Wave: DESIGN / [REF] Component decomposition

| Component | Location | Change type | Slice |
|---|---|---|---|
| Budget check | `Scripts/test-timings/check_budget.py` (per DS-Q1a B) | **CREATE NEW** (justified in Reuse Analysis) | 06 |
| Budget + allowlist data | `Scripts/test-timings/` JSON (per DS-Q1d A) | **CREATE NEW** | 06 |
| Guard tests | `Scripts/test-timings/tests/` + fixtures (planted over-budget unit, allowlisted unit, dotted-argument backend row, missing/malformed CSV) | **EXTEND** | 06 |
| Verify Frontend job | `.github/workflows/ci_frontend.yml` — one last step | **EXTEND** | 06 |
| Verify Backend job | `.github/workflows/ci_backend.yml` — one last step | **EXTEND** | 06 |
| Refinement / Reports / TeamFeatureSize harness bases | `Lighthouse.Backend.Tests/API/Integration/{Refinement,Reports,TeamFeatureSize}/*AcceptanceTest.cs` | **EXTEND** — timer calls (DDD-7); host lifetime per DS-Q2 | 04 |
| Old outlier tests (four named) | their own files | **EXTEND** per cause, or allowlist entry with reason | 05 |
| Frontend kits / slow files | `src/tests/RefinementTabTestKit.tsx`, `src/tests/WorkItemsDialogTestKit.tsx`, the eight files | **EXTEND** per DS-Q3 | 02, 03 |
| `docs/ci-learnings.md` | — | **EXTEND** (AC-06.4; plus the US-02 "why it was slow" lines if a rule falls out) | 02, 06 |
| Vite A/B | none | measurement only, recorded here | 01 |

## Wave: DESIGN / [REF] Driving ports

| Surface | Form | Change |
|---|---|---|
| `Verify Frontend` / `Verify Backend` jobs | GitHub Actions | New last step: "Check test-time budget" — fails naming unit, time, budget and the allowlist file |
| `python3 Scripts/test-timings/check_budget.py --stack <frontend\|backend> --csv <file> [--allowlist <json>] [--source-root <backend test dir>] [--warn-only]` | Local CLI | Same command against a downloaded `test-timings-*` artifact (`gh run download`). Defaults, exit codes and annotation format: DISTILL DI-1..DI-9, DI-13 |
| `pnpm vitest run <file>`, `dotnet test --filter …` with `LIGHTHOUSE_FIXTURE_TIMING=1` | Local CLI | Unchanged; the timer now covers the three harness bases |

## Wave: DESIGN / [REF] Driven ports + adapters

| Port | Adapter | Probe (what it must survive) |
|---|---|---|
| Timing rows | CSV file written by the converters | Missing file, empty file, header not matching `CSV_COLUMNS` → named error, non-zero exit (DS-Q1f) |
| Budget + allowlist | checked-in JSON | Unparseable file, entry without a reason, entry without a ceiling → named error, non-zero exit |
| Verdict | stdout + exit code | Exercised by the guard's own tests run in the step (DS-Q1g) |

No production port or adapter changes.

## Wave: DESIGN / [REF] Technology choices

| Choice | Pin | Licence | Rationale |
|---|---|---|---|
| Guard language | Python 3 stdlib (`csv`, `json`, `argparse`), as the sibling scripts | PSF | Already on the runner and in the folder; no new dependency (no PyYAML) |
| Guard tests | stdlib `unittest`, as the sibling tests | PSF | Existing convention |
| Backend measurement | `FixtureSetupTimer` (existing) | in-repo | Already writes the set-up/tear-down CSV |
| Frontend profiling | Vitest's own per-test durations + `--reporter=verbose` locally | MIT | No new tool |
| Guard placement | existing workflow files | — | Extend, no new workflow |

## Wave: DESIGN / [REF] Decisions table

| ID | Decision | Status |
|---|---|---|
| DDD-1 | Guard reads the CSVs, column names from the converters | Locked |
| DDD-2 | Guard is the last step, runs only after green tests | Locked |
| DDD-3 | New `check_budget.py`; summed time per FE file / BE class; fixed seconds per stack, 2.1 × L ≤ B ≤ 0.55 × the smallest regression to catch; JSON allowlist with reason + ceiling; fail closed; self-tests in the step | LOCKED (AFK rule) — ADR-237 |
| DDD-4 | Frontend fails; backend warns, then fails after ≥5 backend `main` runs after the budget commit show zero budget warnings | LOCKED (AFK rule) — ADR-237 |
| DDD-5 | Host once per fixture, per measured harness (option A) | LOCKED (AFK rule) |
| DDD-6 | No new `[NonParallelizable]`; guard untouched | Locked |
| DDD-7 | Timer in the three harness bases before changing them | Locked |
| DDD-8 | Frontend fixes inside the existing kits, otherwise per file (option B) | LOCKED (AFK rule) |
| DDD-9 | D7 at query level: role queries that *are* the check stay | Locked |
| DDD-10 | Guard decision is a pure function inside the script | Locked |

## Wave: DESIGN / [REF] Reuse Analysis

| Existing component | File | Overlap | Decision | Contract shape / assertion | Justification |
|---|---|---|---|---|---|
| `summarise.py` | `Scripts/test-timings/` | Reads timing data, ranks slow tests | **REUSE AS-IS** | n/a | Its input is raw TRX/JSON and its job is a table for people; the guard's input is the CSV and its job is a gate (F1). Extending it would mix a display tool and a gate |
| `trx_to_csv.py`, `vitest_to_csv.py` | `Scripts/test-timings/` | Define the CSV columns | **REUSE AS-IS** — guard imports `CSV_COLUMNS` | pure | One source for the column contract; no new column needed |
| `Scripts/test-timings/tests/` | — | Fixtures + unittest layout | **EXTEND** | pure-function tests on the guard core | Same convention, same folder |
| `BackendTestParallelizationGuardTest` (the `[NonParallelizable]` allowlist guard) | `Lighthouse.Backend.Tests/Architecture/` | "Allowlist with a reason per entry + ratchet" | **REUSE AS-IS** (pattern borrowed, code untouched) | unchanged | It reflects over the test assembly at build time; the time budget needs post-run data, so it cannot live there. Its name→reason shape is copied into the JSON allowlist |
| `IntegrationTestBase` | `…/TestHelpers/` | Host + DB per test lifecycle | **REUSE AS-IS** | unchanged | Already reuses its host per fixture (F5); the slow classes do not derive from it |
| Refinement / Reports / TeamFeatureSize harness bases | `…/API/Integration/*/` | Host build per test | **EXTEND** | per-test state reset in `[SetUp]`; universe = the fixture's own host | Where the cost is (F5) |
| slice-06 per-fixture reuse | 11 read-API fixtures | `[OneTimeSetUp]` host + per-test DB reset | **REUSE (pattern)** | as slice 06 | Precedent for DS-Q2 A; no shared code exists to call — each fixture inlined it |
| `FixtureSetupTimer` / `FixtureTimingReporter` | `…/TestHelpers/` | Set-up vs body timing | **REUSE AS-IS** (new call sites only) | read-only measurement | Exactly the instrument AC-04.1 names (F6) |
| `TestWebApplicationFactory` | `…/TestHelpers/` | Unique SQLite file per host | **REUSE AS-IS** | unchanged | Already safe for a reused host (`UseFreshDatabase`) |
| `RefinementTabTestKit`, `WorkItemsDialogTestKit` | `src/tests/` | Render + read helpers for the slow files | **EXTEND** (DS-Q3 B) | helpers stay read-only over the DOM | Fix where the repeated cost lives |

CREATE NEW, with evidence: (1) `check_budget.py` — nothing in the repo compares timings to a limit; the only timing consumer (`summarise.py`) has different input and purpose (F1). (2) The budget/allowlist JSON — no existing data file holds per-file limits; the NUnit guard's allowlist is compiled into a test and only sees attributes.

## Wave: DESIGN / [REF] C4 — CI test-timing topology (Mermaid)

Product System Context and Container: **unchanged** (test suites and CI scripts only). The backend source tree is scanned only to tell whether a backend allowlist entry still names a class that exists.

```mermaid
flowchart TD
    dev["Developer<br/>pushes to main"] -->|triggers| fe["Verify Frontend job"]
    dev -->|triggers| be["Verify Backend job"]
    fe -->|runs| vitest["Vitest run<br/>(bail 1 on CI)"] -->|writes| fjson[("test-results.json")]
    be -->|runs| nunit["dotnet test<br/>(Category!=Integration + selected connectors)"] -->|writes| trx[("*.trx")]
    fjson -->|converted by| v2c["vitest_to_csv.py"] -->|writes| fcsv[("test-timings-frontend.csv")]
    trx -->|converted by| t2c["trx_to_csv.py"] -->|writes| bcsv[("test-timings-backend.csv")]
    fcsv -->|uploaded as artifact by| up["upload-artifact (always)"]
    bcsv -->|uploaded as artifact by| up
    gtests["guard self-tests<br/>(unittest)"] -.->|prove red/green before| guard
    fcsv -->|read by| guard{{"check_budget.py<br/>last step, after green tests"}}
    bcsv -->|read by| guard
    allow[("budget + allowlist JSON<br/>unit → reason, ceiling")] -->|read by| guard
    bsrc[("backend test source tree<br/>Lighthouse.Backend.Tests/**/*.cs")] -->|scanned by| guard
    guard -->|fails job naming unit, time, budget<br/>(backend: warns until switched)| dev
    nunit -.->|LIGHTHOUSE_FIXTURE_TIMING=1, local| fst["FixtureSetupTimer CSV<br/>(set-up vs body per fixture)"]
```

## Wave: DESIGN / [REF] Open questions (deferred to DISTILL / DELIVER)

- **The budget numbers** — derived in slice 06 from ≥5 post-fix `main` runs by the DS-Q1c rule; recorded here and in `ci-learnings.md`.
- **When the backend flips from warn to fail** — after ≥5 backend `main` runs after the budget commit show zero budget warnings; recorded with the numbers.
- **Which harnesses are set-up-bound** — slice 04 measures first (DDD-7); a harness where the body dominates is recorded and left alone.
- **The four outliers' causes** — slice 05; any accepted one becomes the first backend allowlist entries.
- **Whether DS-Q3 C is earned** — only if slice 02 finds the same hotspot in both kits and slice 03 finds it outside them.
- **Vite A/B outcome** — slice 01; it shifts the baseline every later slice is measured against.
- **DISTILL shape** — mostly script tests (guard core) plus "same test names, same counts" checks for D7; no UI, so no sketches.

## Wave: DESIGN / [REF] ADRs

- **[ADR-237](../../product/architecture/adr-237-test-time-budget-guard.md) — test-time budget guard** (DS-Q1). Written 2026-10-14.
- No ADR for DS-Q2: option A keeps the change inside each harness; a `ci-learnings.md` entry carries the rule. An ADR amending ADR-074 is due only if a reusable host later becomes the default.
- No ADR for DS-Q3 (test-kit internals).

## Wave: DESIGN / [REF] Wave-decisions summary (DESIGN)

- **Pattern**: product architecture unchanged; a small functional-core CI script beside the existing converters, plus test-harness changes (OOP, NUnit; Vitest kits).
- **Locked from DISCUSS / the code**: CSV input with the converters' column names (DDD-1); last step after green tests (DDD-2); no new `[NonParallelizable]` (DDD-6); measure before changing (DDD-7); D7 at query level (DDD-9); pure guard core (DDD-10).
- **Locked under AFK by the recommended-option rule (maintainer may revisit)**: DS-Q1 — new `check_budget.py`; summed time per frontend file / backend class; fixed seconds per stack, 2.1 × L ≤ B ≤ 0.55 × the smallest regression to catch; JSON allowlist with reason + ceiling; frontend fails, backend warns until ≥5 runs after the budget commit show zero warnings, then fails; fail closed; self-tests in the step (ADR-237). DS-Q2 — option A, per measured harness. DS-Q3 — option B, inside the existing kits.
- **Reuse**: two CREATE NEW (script, data file), everything else EXTEND or REUSE AS-IS.
- **Corrections to DISCUSS**: F1, F5, F6 back-propagated (see Changed assumptions, DESIGN); F3 is why the backend budget starts in warn mode.
- **Handoff**: DEVOPS (usage-data event expected N/A) → DISTILL.

---
---

# DEVOPS

Density: `lean`, Tier-1 [REF] only. This feature deploys nothing and adds no infrastructure: one new last step in each of the two existing verify jobs, plus test-only changes. The maintainer was AFK; every engineering choice below took the recommended option and is recorded as a DO-* decision (none changes what a Lighthouse user sees; the maintainer may revisit). Machine artifact: `docs/feature/story-6302-ci-test-time/environments.yaml`. KPI contracts appended to `docs/product/kpi-contracts.yaml` (`OUT-6302-*`).

Prior-wave reading: ✓ this delta (DISCUSS KPIs, DESIGN DDD-1..10, DS-Q1..Q3), ✓ ADR-237, ✓ `ci.yml`, `ci_frontend.yml`, `ci_backend.yml`, `ci_changes.yml`, `ci_workflow-scripts.yml`, `.github/actions/build-frontend`, ✓ `Scripts/test-timings/` (3 scripts, 3 test modules, fixtures), ✓ `backend-test-speed` DEVOPS + `environments.yaml` (format precedent), ✓ `kpi-contracts.yaml` (format), ✓ `docs/ci-learnings.md` (timing CSVs, pipefail, piping a gate's output, Python under backend Sonar, Stryker temp dirs, pnpm).

## Wave: DEVOPS / [REF] Environment matrix

| Environment | Platform | Python | Preconditions | Role |
|---|---|---|---|---|
| `local-dev` | linux (the maintainer's box; macOS/WSL work but are not the reference) | `python3` 3.14.8 (observed 2026-10-14) | a `test-timings-*.csv` downloaded with `gh run download <run> -n test-timings-frontend` (or `-backend`), or produced locally by the converters | Same command as CI: `python3 Scripts/test-timings/check_budget.py --stack <frontend\|backend> --csv <file>`; self-tests `python3 -m unittest discover -s Scripts/test-timings/tests` (33 tests, OK, 0.006 s today) |
| `ci-runner` | GitHub Actions `ubuntu-latest` = ubuntu-24.04, image 20261004 | the image's system `python3` (3.12 line expected; the step prints `python3 --version` so the first run confirms it) | green test step; CSV written by the always-run extract step; repository checked out at the run's SHA | Gate: frontend fails the job, backend warns (DDD-4) |

**DO-1 — stdlib floor is the runner's Python, not the developer's.** The script and its tests use nothing newer than Python 3.12 (local dev is 3.14, so a 3.13+ stdlib API would pass locally and break on CI). No `actions/setup-python`, no `pip install`: the check is `csv`, `json`, `argparse`, `pathlib`, `unittest` only (DESIGN technology choices). The script checks `sys.version_info < (3, 12)` before anything else and, on an older interpreter, exits non-zero with a message naming the version it needs and the one it found, rather than failing later on a syntax or library error that hides the cause.

## Wave: DEVOPS / [REF] CI/CD pipeline outline

No new workflow, job or stage; one appended step per verify job (DESIGN component table; `feedback-ci-and-e2e-minimalism`). Callers unchanged: `Build And Deploy Lighthouse` (`ci.yml`) → `Verify Frontend` (`ci_frontend.yml`, job `frontend`) and `Verify Backend` (`ci_backend.yml`, job `backend`, called with `run_sonar: true` on every run). No other workflow calls either file, so the guard runs on `main` pushes, `features/**` pushes and PRs alike.

**Verify Frontend / frontend** (step order after the change):

| # | Step | Condition | Note |
|---|---|---|---|
| 1 | checkout | — | |
| 2 | Build Frontend (composite): install → `pnpm run build` → `pnpm run sonarreport` (`vitest run --coverage`, `bail: 1` on CI) → Sonar cache → Sonar scan | success | tests and the Sonar scan both live inside the composite, before the guard |
| 3 | Extract frontend per-test timings (`vitest_to_csv.py`, `summarise.py --top 20`) | `always()` | unchanged |
| 4 | Upload frontend per-test timings | `always()` | unchanged; the CSV the guard judged is published even when the guard is red |
| 5 | Upload Sonar report metadata | success | unchanged |
| **6** | **Check test-time budget** (new, last) | default `success()` | runs only if 1–5 succeeded, so only after a green, complete Vitest run (F2) |

**Verify Backend / backend**:

| # | Step | Condition | Note |
|---|---|---|---|
| 1–9 | checkout … Begin SonarCloud … Build … Compute filter | as today | unchanged |
| 10 | Test Backend (with Coverage) / (without Coverage) | `run_sonar` / `!run_sonar` | `ci.yml` always passes `run_sonar: true`, so coverage is always on; the budget is calibrated under coverage |
| 11 | Extract backend per-test timings (`trx_to_csv.py`, `summarise.py`) | `always()` | unchanged |
| 12 | Upload backend per-test timings | `always()` | unchanged |
| 13 | End SonarCloud analysis | `run_sonar` | unchanged; runs before the guard, so a breach never skips Sonar (DDD-2) |
| 14 | Upload Sonar report metadata | `run_sonar` | unchanged |
| **15** | **Check test-time budget** (new, last) | default `success()` | only after a green test step; skipped Sonar steps do not block it |

**Step body (both jobs, same three commands; DO-2):**

1. `python3 --version` — records the interpreter on the log (DO-1).
2. `python3 -m unittest discover -s Scripts/test-timings/tests` — the guard's own tests plus the existing converter and `summarise` tests, which no workflow runs today (F7). Any failure fails the step in **both** stacks; warn mode never softens it.
3. `python3 Scripts/test-timings/check_budget.py --stack frontend --csv test-timings-frontend.csv` / `--stack backend --csv test-timings-backend.csv --source-root Lighthouse.Backend/Lighthouse.Backend.Tests --warn-only`.

**DO-3 — frontend fails, backend warns, and what "warn" covers.** Frontend: a breach exits non-zero and fails the job. Backend: `--warn-only` turns a *budget breach* into a warning and exit 0; the one-line switch to failing (DDD-4) is deleting that flag in `ci_backend.yml` once ≥5 backend `main` runs after the budget commit show zero budget warnings. Warn mode never softens a missing, empty or malformed CSV, an unparseable allowlist, a stale allowlist entry or a failing self-test: those fail in both stacks (ADR-237 §6, §7; DI-7). A frontend budget breach has the same release reach as a red frontend test has today: the release job does not need the frontend job directly, and reaches it only through `sonar-gates` (via the standalone packages), so a breach holds a release exactly where a red frontend test already does, no further.

**DO-4 — how the verdict reaches the reader: annotations, not a piped summary.** Each breach is printed as a GitHub workflow-command line, `::error::` for the frontend and `::warning::` for the backend, naming the unit, its seconds, the budget and the allowlist file to edit. GitHub lifts these onto the run's summary page, so the job summary shows the verdict with no `$GITHUB_STEP_SUMMARY` write and no `| tee`. The message is escaped as GitHub's workflow-command parser expects, so a unit name cannot break the line or forge a second command: `%` becomes `%25`, CR `%0D` and LF `%0A`. Rejected: teeing the output into the step summary — a pipe on a gate's output is exactly what `ci-learnings.md` warns about, and under the default `bash -e` shell (no `pipefail`) `tee` would swallow the guard's exit code.

**DO-5 — commands run from the repository root,** with paths as the existing extract steps write them (`test-timings-*.csv` at the root; backend source root `Lighthouse.Backend/Lighthouse.Backend.Tests`). No `working-directory`, no new env var, no secret.

Cost: the guard reads one CSV of ~8–11 k rows and the self-tests take milliseconds; the step adds well under 5 s to each job.

## Wave: DEVOPS / [REF] Monitoring contracts (KPI → instrument)

All are `vendor_repository` measurements, read with `gh` by the maintainer at named check points (D9: median of ≥5 `main` runs after the slice lands). No dashboard, no scheduled job, no alert beyond the job turning red. Contracts in `docs/product/kpi-contracts.yaml`.

| KPI | Instrument | How it is read |
|---|---|---|
| OUT-6302-fe-job (16.8 → ≤12 min) | GitHub Actions job timing for `Verify Frontend / frontend` | `gh run list -R LetPeopleWork/Lighthouse -w "Build And Deploy Lighthouse" -b main -e push --status completed --limit 30 --json databaseId,headSha`, then per run `gh api repos/LetPeopleWork/Lighthouse/actions/runs/<id>/jobs --paginate`, keep the job of that name with conclusion `success` or `failure` (skipped/cancelled out), minutes = `completed_at − started_at`; median over ≥5 runs after slice 03. Cross-check the run ids against `git log` SHAs (the run-list API has served stale pages). |
| OUT-6302-be-job (15.3 → ≤13.5 min) | same, job `Verify Backend / backend` | as above, after slice 05. Also report the median over runs whose "Compute Test Backend filter" log shows `filter=Category!=Integration` alone, because runs that pull in connector categories are slower for reasons outside this story (F3). |
| OUT-6302-fe-slow-files (~456 → ≤110 s) | `test-timings-frontend.csv` artifact | `gh run download <id> -n test-timings-frontend`; sum `duration_ms` grouped by `file` for the eight named files; median of the per-run sum over ≥5 `main` runs. |
| OUT-6302-be-new-integration (288 → ≤192 s) | `test-timings-backend.csv` artifact | `gh run download <id> -n test-timings-backend`; sum duration for rows under `API.Integration.Refinement.`, `.Reports.` and `.TeamFeatureSize.`; median of the per-run sum over ≥5 `main` runs after slice 04. |
| OUT-6302-coverage-kept (passed + skipped + todo per stack, not lower than 7,630 / 10,198) | CI log test counts | Frontend: the Vitest summary line (`Tests  N passed \| M skipped \| K todo (T)`) in the `pnpm run sonarreport` log, compared against run 38553818860 (7,630); backend: the `dotnet test` summary, passed + skipped + todo, compared against run 38570239004 (10,198). Backend counts compared only between runs with the same filter (F3). The CSV row count is the cross-check. |
| OUT-6302-guard (red on a planted file) | the guard step itself | Every run: the self-tests carry a planted over-budget unit (red), an allowlisted one (green) and a missing CSV (red), so the step proves itself whenever it runs. At slice 06: one local run of `check_budget.py` against a real downloaded CSV with a planted over-budget row, output recorded in this delta. `main` is never pushed red to show it (green-before-push). |

## Wave: DEVOPS / [REF] Deployment strategy

**N/A**, because there is no deployable artifact: the change is CI steps, a stdlib Python script with its JSON data, and test code. Nothing reaches a Lighthouse instance, the Docker image or the Helm chart.

Rollback, designed first: (a) a misfiring frontend guard is relieved in one line by adding `--warn-only` to its step, or (b) by an allowlist entry with a reason and ceiling, or (c) by `git revert` of the step commit. The backend flip to failing is a one-line change, so its rollback is one line too. Test-harness changes (DS-Q2, DS-Q3) roll back with `git revert`; the suites are their own safety net (3 consecutive green full-assembly runs, DS-Q2).

## Wave: DEVOPS / [REF] Mutation testing strategy

**per-feature** (CLAUDE.md, unchanged; ≥80 % kill rate). Applied as follows:

- **Production C# and TS**: expected untouched (DESIGN: test kits, harness bases, CI script only). Stryker.NET and StrykerJS mutate production code, so with no production file in the diff there is nothing in scope. If DELIVER does touch a production file after all, Stryker runs on that file and the 80 % gate applies (DoD 5); mutation runs last, on frozen code.
- **Test code** (`RefinementTabTestKit`, `WorkItemsDialogTestKit`, the eight files, the three harness bases, the four outlier tests): not mutated, because it is the mutation oracle, not its target. D7 ("same names, same assertions, counts do not drop") is the guard on it.
- **`check_budget.py` (Python)**: **no mutation run**, because the repository has no Python mutation tool (no mutmut, cosmic-ray or equivalent; no Python requirements file; Stryker covers C#, JS and TS only), and adding one plus its install step for one stdlib script would bring a dependency into a step designed to need none (DO-1). Substitute, pinned by DISTILL: the pure core's unit tests probe both sides of every boundary a mutant would flip — a unit exactly at the budget and just over it, an allowlisted unit at and over its ceiling, the backend class name from a dotted argument (`"https://evil.example/steal"`), a stale frontend entry, an entry with no reason or no ceiling, and a missing, empty or wrong-header CSV.

No CLAUDE.md change.

## Wave: DEVOPS / [REF] Observability stack

Reuse, nothing added: the `test-timings-{frontend,backend}` artifacts (#5020, `backend-test-speed`), the guard's annotations on the run summary page (DO-4), the existing `summarise.py --top 20` log lines, and GitHub's own job timings read with `gh`. No metrics, log or trace stack, and nothing a running Lighthouse emits (self-hosted, no phone-home posture).

## Wave: DEVOPS / [REF] Branching strategy

**Trunk-based on `main`** (project standard): each slice is a focused local commit set, pushed at the slice boundary, CI awaited green, then ADO transitioned. No feature branch, no PR. `ci.yml` also triggers on `features/**` pushes and PRs. Once slice 06 adds `Scripts/test-timings/**` to the trigger paths (DO-7), the guard runs there unchanged, which keeps a contributor PR honest without extra config; until then a PR touching only the guard starts no verify job.

## Wave: DEVOPS / [REF] Coexistence matrix

| Existing thing | Why it keeps working |
|---|---|
| Timing extract + upload steps (`always()`) in both jobs | Unchanged. The guard only reads the CSV they write and runs after them; the upload still happens on a red test run and on a red guard. |
| `summarise.py --top 20` log output | Unchanged; the guard does not import or replace it (DESIGN reuse: REUSE AS-IS). |
| Frontend Sonar (inside the build composite) and backend Sonar begin/end + report metadata | All run before the guard, so a breach never skips a scan. A red frontend guard makes the job `failure`, which skips `sonar-gates` for that run, exactly as a red test does today; the analysis itself has already been submitted. Backend warn mode leaves the job green. |
| SonarCloud on the new Python | `Scripts/**` is already in the backend scanner's `sonar.exclusions` and `sonar.coverage.exclusions` (the 2026-08-31 learning), and the frontend scan's base dir is `Lighthouse.Frontend`, so the script raises no Sonar issue. |
| Coverage upload (Vitest `--coverage` lcov and backend opencover, both read by Sonar) | The guard reads neither and changes no test command; coverage collection is untouched. |
| `[NonParallelizable]` allowlist guard (`BackendTestParallelizationGuardTest`) | Runs inside `dotnet test`, code untouched; DDD-6 adds no `[NonParallelizable]`. Its name → reason shape is copied, not shared, so the two allowlists cannot collide. |
| Stryker temp dirs (`.stryker-tmp*`, `StrykerOutput/`, gitignored) | Never present on CI. Locally, the backend allowlist's "class still exists in source" scan must skip them along with `bin/` and `obj/`, or a deleted class surviving in a sandbox copy keeps a stale entry green on the developer's box and red on CI (DO-6; DISTILL pins it). |
| `bail: 1` in `vitest.config.ts` | The guard runs on `success()` only, so it never reads the partial JSON a failing run leaves (F2). |
| Backend filter / connector selection (`ci_changes.yml`, "Compute Test Backend filter") | Unchanged. The guard copes with a varying class set by checking backend allowlist entries against source, not against the run (F3). |
| `Verify Workflow Scripts` (`node --test .github/scripts/*.test.mjs`) | Untouched; the Python self-tests live in the guard step per DS-Q1, not in that Node job. |
| E2E and verify-sqlite/postgres/auth workflows | Do not call `ci_frontend.yml` or `ci_backend.yml`; unaffected. |

**DO-6 — the backend source scan skips build and mutation output** (`bin/`, `obj/`, `.stryker-tmp*/`, `StrykerOutput/`), so local and CI answers agree.

## Wave: DEVOPS / [REF] Pre-requisites

- Timing CSVs written and uploaded by both jobs (verified in `ci_frontend.yml` and `ci_backend.yml`).
- `Scripts/test-timings/tests` green with plain `python3 -m unittest discover` today: 33 tests, OK (verified locally, Python 3.14.8). The modules put their parent folder on `sys.path` themselves, so no `__init__.py` or package install is needed.
- `Scripts/**` excluded from the backend Sonar project (verified).
- **DO-7 — the trigger paths must include `Scripts/test-timings/**`** (DELIVER slice 06, same commit as the guard steps). Today `ci.yml`'s `push` paths filter has no `Scripts/**` entry, and `ci_changes.yml` sets `frontend`/`backend` only for `Lighthouse.Frontend/`, `Lighthouse.Backend*/` or `ci*.yml` changes. A push that touches only the guard, its allowlist or a converter would therefore run neither verify job, and the change would first be checked by whichever unrelated push comes next. Recommended: add `Scripts/test-timings/**` to the `ci.yml` push paths and to both the frontend and backend patterns in `ci_changes.yml`. (Normally an allowlist entry rides with the slow test it excuses and so triggers anyway; the gap is a script-only or allowlist-only commit.) Cost: a script-only or allowlist-only push then runs both full verify jobs, about 15 min each, because the path filter cannot tell a guard change from a test change.
- The budget numbers need ≥5 post-fix `main` runs (slice 06 comes last for this reason).
- `gh` authenticated against `LetPeopleWork/Lighthouse` for the monitoring reads.
- In a worktree, copy `valid_not_expired_license.json` from the main checkout before judging backend results.

## Wave: DEVOPS / [REF] Usage-data event

**N/A, because the feature changes CI and tests only; no Lighthouse user action is new or changed, so there is nothing for the opt-in usage-data catalogue (`UsageDataEventName`) to count.** Confirmed against the stories: US-01 is a local A/B measurement, US-02/03 speed up frontend test files, US-04/05 change backend test harnesses and tests, US-06 adds a CI step; the DISCUSS driving ports are `pnpm`/`dotnet test` CLIs, the GitHub Actions verify jobs and the timing scripts, with "no HTTP, UI or client surface changes". No `UsageDataEventName` entry, no `docs/settings/usagedata.md` change.

## Wave: DEVOPS / [REF] Contradictions with DESIGN

1. **Trigger gap (DO-7).** ADR-237 §8 says the step "proves itself on every run", but a commit touching only `Scripts/test-timings/**` starts no verify run, so the proof waits for an unrelated push. Not a change to the design; a missing piece of wiring, assigned to slice 06.
2. **Allowlist file count, wording only.** DS-Q1 says "one checked-in JSON beside the script, per stack"; ADR-237 §4 says one JSON file with one section per stack. The ADR wording is the decision; DISTILL and DELIVER follow it.
3. **Python version split (DO-1).** DESIGN says "already on the runner"; true, but the runner's system Python is older than the developer's, so the script is held to the 3.12 stdlib.

## Wave: DEVOPS / [REF] Wave-decisions summary (DEVOPS)

- **Deployment**: none, N/A with reason. **CI/CD**: existing GitHub Actions; one last step per verify job; no new workflow, job or stage. **Branching**: trunk-based. **Observability**: the timing artifacts, guard annotations and `gh` job timings. **Mutation**: per-feature, unchanged; nothing in scope unless a production file is touched; no Python mutation tool, substitute boundary tests named.
- **Decided (AFK recommended option, maintainer may revisit)**: DO-1 runner-Python stdlib floor, no setup-python, no pip; DO-2 step = `--version`, self-tests, check; DO-3 backend `--warn-only` softens breaches only, never fail-closed errors; DO-4 annotations, no tee into the step summary; DO-5 repo-root paths, no env or secret; DO-6 source scan skips `bin`/`obj`/Stryker output; DO-7 `Scripts/test-timings/**` added to the CI trigger paths in slice 06.
- **Usage-data event**: N/A (section above).
- **Upstream changes**: none to DISCUSS or DESIGN decisions; contradictions 1–3 are wiring and wording, carried forward.
- **Handoff**: DISTILL. `environments.yaml` gives the local-vs-CI axis (Python version, presence of Stryker sandboxes, varying backend class set); the KPI contracts name the instruments.

---
---

# DISTILL

Density: `lean`, Tier-1 [REF] only. The maintainer was AFK; every engineering choice below took the recommended option and is recorded as a DI-* decision (none changes what a Lighthouse user sees; the maintainer may revisit). Stack for the authored tests: Python 3.12 stdlib `unittest`, the convention `Scripts/test-timings/tests/` already uses, not pytest-bdd. The project policy (`docs/architecture/atdd-infrastructure-policy.md`) records that the Python-pilot machinery (`tests/common/state_delta`, Hypothesis, pytest-bdd step modules, `domain_types.py`) does not apply to this repository, so none of it is bootstrapped.

Prior-wave reading:
+ `docs/feature/story-6302-ci-test-time/feature-delta.md` (DISCUSS US-01..06, ACs, KPIs, D1-D10; DESIGN F1-F8, DDD-1..10, DS-Q1..Q3, Changed assumptions; DEVOPS DO-1..7, Contradictions)
+ `docs/feature/story-6302-ci-test-time/environments.yaml`
+ `docs/feature/story-6302-ci-test-time/slices/slice-01..06-*.md`
+ `docs/product/architecture/adr-237-test-time-budget-guard.md`
+ `docs/product/journeys/story-6302-ci-test-time.yaml`
+ `docs/product/kpi-contracts.yaml` (OUT-6302-fe-job, -be-job, -fe-slow-files, -be-new-integration, -coverage-kept, -guard)
+ `docs/feature/backend-test-speed/feature-delta.md` (DISTILL: acceptance shape, scenario list, red-classification)
+ `Scripts/test-timings/` (`vitest_to_csv.py`, `trx_to_csv.py`, `summarise.py`, tests and fixtures)
+ `docs/ci-learnings.md` (timing CSVs entry 2026-05-17; Python under the backend Sonar scanner 2026-08-31; `python3 -c` blocked in the agent sandbox)
+ `docs/architecture/atdd-infrastructure-policy.md`
+ `.github/workflows/ci_frontend.yml`, `ci_backend.yml` (extract steps; no step runs the Python tests)
- `docs/feature/story-6302-ci-test-time/{discuss,design,devops}/wave-decisions.md` (not present: lean single-file model, decisions read inline)
- `docs/product/architecture/brief.md` `## For Acceptance Designer` (not needed: DESIGN names the driving ports in this delta)
- `.nwave/des-config.json` (absent; deliverable type resolves to `application`)
- `spike/` (no spike ran)

## Wave: DISTILL / [REF] Reconciliation result

**Reconciliation passed: 0 contradictions.** DISCUSS D1-D10 were checked against DESIGN and DEVOPS. The differences DESIGN and DEVOPS recorded are corrections of fact or wording, each with a resolution already written down, and none changes what the developer gets:

| Item | DISCUSS said | Resolution (upstream) | DISTILL reading |
|---|---|---|---|
| Who produces the CSVs (D6) | `summarise.py` | DESIGN F1: the converters write them; `summarise.py` only prints a table | The check reads the CSVs and imports `CSV_COLUMNS` from the converters |
| Slow backend harnesses (US-04) | `IntegrationTestBase` per test | DESIGN F5/F6: three harness bases build a host per test; the timer must be added first | Evidence check E-04 measures set-up first |
| Allowlist file count | "one JSON per stack" (DS-Q1 wording) | DEVOPS contradiction 2: ADR-237 §4 stands, one file with a section per stack | Tests write one file with `frontend` and `backend` sections |
| CI trigger paths | not addressed | DEVOPS DO-7: add `Scripts/test-timings/**` to `ci.yml` and `ci_changes.yml` in slice 06 | Evidence check E-06d |
| Python version | not addressed | DEVOPS DO-1: Python 3.12 stdlib floor; the script refuses an older interpreter with a clear message | Script and tests verified on 3.12.13 and 3.14.8; the refusal is DI-13 |
| Backend gate strength (D6 "both stacks") | frontend fails; backend warns until calibrated, then fails | DESIGN DDD-4 / DEVOPS DO-3: backend warns until ≥5 runs after the budget commit show zero warnings | Warn-only is pinned by tests; the flip is evidence check E-06b |

**Existing CI does not run `Scripts/test-timings/tests`** (verified: no `unittest` or `discover` anywhere under `.github/`). DELIVER slice 06 wires `python3 -m unittest discover -s Scripts/test-timings/tests` into both verify jobs (DO-2), which brings the existing converter tests and the new guard tests into CI together.

## Wave: DISTILL / [REF] Acceptance shape (why this feature is non-standard)

Following `backend-test-speed`: this is a test-infrastructure feature, and most of it changes how fast existing tests run rather than what anything does. Acceptance therefore has two kinds of oracle.

1. **One new behaviour, the budget guard (US-06), gets executable acceptance tests.** Its driving port is the command CI runs, so the tests invoke `Scripts/test-timings/check_budget.py` as a **subprocess** against real CSV, JSON and C# files in a temporary folder, and assert the exit code and the `::error::` / `::warning::` lines on stdout. That covers argument parsing, file reading and the exit code, which is where a gate fails open; calling the core function directly would not. Two seam tests run the real converters first. All are layer-3 tests, so they are example-based (sad paths named one by one); the one property that fits, "the verdict does not depend on how a file's time is split across its tests", is example-pinned with `subTest`. Hypothesis is not a dependency and is not added.
2. **The speed slices (US-01..05) are measurement and refactoring.** Their acceptance is evidence that DELIVER runs and records: an A/B protocol for US-01; for US-02..05, the sorted list of test names before and after (no name lost, no new skip), plus timing thresholds read from local runs and the CI CSVs. Writing new behavioural tests for them would duplicate the tests they speed up, which are themselves the oracle for behaviour (D7). They are specified as commands in **Acceptance checks (evidence-based)** below, with no Gherkin.
3. **Job durations (KPIs)** are measured with `gh`, not asserted in a test; a duration assertion would be flaky under the 0.55-1.13 runner spread.

No `.feature` file is written: there is no Gherkin runner for Python in this repository, and the project policy row for CI scripts (appended this wave) names `unittest` test names as the scenario statements, as the `node:test` precedent does for `.github/scripts`.

## Wave: DISTILL / [REF] Scenario list with tags

File: `Scripts/test-timings/tests/test_check_budget.py`. The tags below live in this table, keyed by class and test name; the test file carries none, because a tag comment would only cite a story id. Every test is `@US-06 @real-io @driving_adapter` (subprocess against real files) and pending (`@unittest.skip("pending — DELIVER slice 06")`). `@contract-shape:pure-function` unless stated: the verdict is a function of the files read, and the check writes nothing. Rows 37-41 were added after the acceptance review; row 42 after the DEVOPS final-wave review.

| # | Test (class → test) | Extra tags | Path |
|---|---|---|---|
| 1 | AFrontendFileOverTheBudgetFailsTheBuild → a run with every file under the budget passes without annotations | `@kpi:OUT-6302-guard` | happy |
| 2 | … → a file exactly at the budget passes | `@boundary` | happy |
| 3 | … → a file over the budget fails naming the file, its time, the budget and the allowlist | `@kpi:OUT-6302-guard @kpi:OUT-6302-fe-slow-files` | verdict |
| 4 | … → a file a millisecond over the budget fails | `@boundary @error` | edge |
| 5 | … → a file is judged on the sum of its tests, not on its slowest test | | verdict |
| 6 | … → the verdict does not depend on how a file's time is split across its tests (8 subtests) | `@property` (example-pinned) | invariant |
| 7 | … → only the file over the budget is named when others are under it | | verdict |
| 8 | … → judging a run leaves every file in the folder as it was | `@contract-shape:unbounded-preservation` | invariant |
| 9 | AnAllowlistedUnitIsHeldToItsOwnCeiling → an allowlisted file under its own ceiling passes although it is over the budget | `@kpi:OUT-6302-guard` | happy |
| 10 | … → an allowlisted file exactly at its ceiling passes | `@boundary` | happy |
| 11 | … → an allowlisted file over its own ceiling fails naming the file, its time and its ceiling | | verdict |
| 12 | … → a frontend allowlist entry for a file no longer in the run fails | `@error` | edge |
| 13 | … → a backend allowlist entry for a class still in the test source passes when the class did not run | | happy |
| 14 | … → a backend allowlist entry for a class no longer in the test source fails | `@error` | edge |
| 15 | … → a class left only in build or mutation output does not count as still in the source (`bin`, `obj`, `.stryker-tmp*`, `StrykerOutput`) | `@error @env:local-dev` | edge |
| 16 | AnAllowlistThatCannotBeTrustedIsRefused → a backend test source folder that does not exist fails closed | `@error @fail-closed` | error |
| 17 | … → an allowlist entry without a reason is rejected (missing, empty, blank) | `@error @fail-closed` | error |
| 18 | … → an allowlist entry without a usable ceiling is rejected (missing, text, negative) | `@error @fail-closed` | error |
| 19 | … → an allowlist that is not valid JSON fails closed | `@error @fail-closed` | error |
| 20 | … → an allowlist without a section for the stack fails closed | `@error @fail-closed` | error |
| 21 | … → an allowlist section without a budget fails closed | `@error @fail-closed` | error |
| 22 | … → a missing allowlist file fails closed | `@error @fail-closed` | error |
| 23 | ATimingFileThatCannotBeReadFailsClosed → a missing timing file fails closed naming the file | `@error @fail-closed @kpi:OUT-6302-guard` | error |
| 24 | … → an empty timing file fails closed | `@error @fail-closed` | error |
| 25 | … → a timing file with a header and no tests fails closed | `@error @fail-closed` | error |
| 26 | … → a timing file with the other stack's columns fails closed | `@error @fail-closed` | error |
| 27 | … → a timing file with an unreadable duration fails closed | `@error @fail-closed` | error |
| 28 | WarnOnlySoftensABreachButNothingElse → warn-only reports a class over the budget as a warning and passes | `@warn-only @kpi:OUT-6302-be-job` | happy |
| 29 | … → warn-only reports an allowlisted class over its ceiling as a warning and passes | `@warn-only` | happy |
| 30 | … → warn-only still fails on a missing timing file | `@warn-only @error @fail-closed` | error |
| 31 | … → warn-only still fails on a malformed timing file | `@warn-only @error @fail-closed` | error |
| 32 | … → warn-only still fails on a malformed allowlist | `@warn-only @error @fail-closed` | error |
| 33 | ABackendClassIsJudgedOnTheSumOfItsTests → backend tests are summed per class | | verdict |
| 34 | … → a class is named without its method or arguments, even when the arguments contain dots | `@boundary` | verdict |
| 35 | TheCheckJudgesWhatTheConvertersWrite → a frontend timing file written by the Vitest converter is judged end to end | `@walking_skeleton` | seam |
| 36 | … → a backend timing file written by the TRX converter is judged per class | `@walking_skeleton` | seam |
| 37 | AFrontendFileOverTheBudgetFailsTheBuild → each stack is held to the budget its own section sets (budget 30 s; 40 s fails, 25 s passes; both stacks) | `@boundary` | verdict |
| 38 | AnAllowlistedUnitIsHeldToItsOwnCeiling → an allowlisted backend class is held to its own ceiling (at it passes, over it fails, no warn-only) | `@boundary` | verdict |
| 39 | … → a backend class counts as in the source whatever the file it is declared in is called | | happy |
| 40 | … → a class of the same name in another namespace does not keep an entry alive | `@error` | edge |
| 41 | WarnOnlySoftensABreachButNothingElse → warn-only still fails on an allowlist entry for a class no longer in the source | `@warn-only @error` | error |
| 42 | AFrontendFileOverTheBudgetFailsTheBuild → a file name containing a percent sign and a line break is reported on one escaped annotation line | `@boundary @error` | edge |

**Error and edge paths: 22 of 42 (52 %)** — 16 failures and refusals (16-27, 30-32, 41) plus 6 edges (4, 12, 14, 15, 40, 42). Test 18 also refuses a ceiling of 0 and of `true`; test 30 runs for both stacks; test 34 includes arguments containing `(` and `)`. Every refusal and every verdict also asserts that no traceback escapes (a closed error set: exit 0, 1 or 2, each with an annotation).

Contract pinned by these tests (DI-1..DI-9 below): exit **0** pass (or warn-only), **1** a unit over its limit or a stale allowlist entry, **2** an input that cannot be read or trusted; one annotation per finding on stdout, naming the unit, its seconds to one decimal, the limit, and the allowlist file name, with `%`, CR and LF in the message percent-encoded.

## Wave: DISTILL / [REF] Acceptance checks (evidence-based)

DELIVER runs these at the slice boundary and records the result under that slice's `## Wave: DELIVER` heading in this delta. Behaviour preservation is the invariant across all of them: **no test is deleted, skipped or weakened.** Commands are bash, run from the repository root; `$D` is a scratch folder outside the repository (`D=$(mktemp -d)`).

| ID | Story / AC | What is checked | Threshold | Recorded in |
|---|---|---|---|---|
| E-01 | US-01 / AC-01.1, AC-01.2 | Vite 8.3.2 vs 8.3.4 A/B on an unchanged test set, same machine | ≥3 runs per arm, alternated A B A B A B. Each arm's spread is the min–max of its runs; an arm is slower only if its fastest run is slower than the other arm's slowest run, as AC-01.2 words it. If so, fix and show the fix on the A/B; if not, record "disproved". If vite is clean, repeat once each for the MUI bump (641ac9c6f) and pnpm 12.10 (1bbde5105) | DELIVER slice 01: arm, version, commit, each `Duration`, median, spread, machine |
| E-names | all D7 checks | Sorted list of test names before and after a slice (command below) | `comm -23 before after` prints nothing (no name lost); no non-`Passed` name that was `Passed` before; per-file / per-class counts not lower | DELIVER slice table: before/after counts per file or class, and "0 names lost" |
| E-weaken | all D7 checks | The slice's diff adds no skip and drops no assertion without a same-meaning replacement (command below) | Skip grep prints nothing; every removed `expect(` / `Assert.` line is paired with its replacement in the review note; a `*ByRole` that *is* the assertion stays a role query (DDD-9) | DELIVER slice review note |
| E-02a | US-02 / AC-02.1 | E-names + E-weaken for `RefinementView.paging.test.tsx`, `WorkItemsDialog.contexts.test.tsx` | as above | slice 02 |
| E-02b | US-02 / AC-02.2, pitch | Summed time of the two files | Local: ≤40 s summed from the run's JSON. CI: median over ≥5 `main` runs of the per-run sum from `test-timings-frontend.csv` ≤40 s | slice 02, plus OUT-6302-fe-slow-files |
| E-02c | US-02 / AC-02.3 | Why each file was slow | One line each, naming the measured cost (DESIGN F8 candidates) | slice 02; `ci-learnings.md` if a rule falls out |
| E-03a | US-03 / AC-03.1 | E-names + E-weaken for the six files | as above | slice 03 |
| E-03b | US-03 / AC-03.2 | Summed CI time of the six files | Median over ≥5 `main` runs ≤70 s; all eight named files together ≤110 s (OUT-6302-fe-slow-files) | slice 03 |
| E-04a | US-04 / AC-04.3 (D7) | E-names + E-weaken for `API.Integration.{Refinement,Reports,TeamFeatureSize}` | as above | slice 04 |
| E-04b | US-04 / AC-04.1 | Set-up vs body time per in-scope class, **before** any change | `FixtureSetupTimer` output exists for all three harness bases; set-up share recorded per harness; in `git log --oneline` the commit adding the timer calls precedes the first harness change | slice 04 |
| E-04c | US-04 / AC-04.2 | Parallel safety | 3 consecutive green full-assembly runs with the standard filter; `BackendTestParallelizationGuardTest` green; no `[NonParallelizable]` added | slice 04 |
| E-04d | US-04 / AC-04.3 | Summed CI time of the in-scope classes | Median over ≥5 `main` runs drops by ≥⅓ against the 14 Oct baseline (288 s in run 38570239004 → ≤192 s, OUT-6302-be-new-integration), or the slice records why set-up is not the cost | slice 04 |
| E-05a | US-05 / AC-05.2 (D7) | E-names + E-weaken for the classes holding the four outlier tests | as above | slice 05 |
| E-05b | US-05 / AC-05.1 | Cause of each of the four outliers | One line each, measured locally (`dotnet test --filter "FullyQualifiedName~<Class>"`) and confirmed on ≥3 CI CSVs | slice 05 |
| E-05c | US-05 / AC-05.2 | Fixed outliers finish in seconds; accepted ones are allowlisted | A fixed test under 10 s locally (the US-05 threshold), its CI CSV time over ≥3 runs recorded beside it; an accepted one has a proposed entry (class, reason, ceiling) that slice 06 seeds into `test-time-budget.json` | slice 05 → slice 06 |
| E-06a | US-06 / AC-06.3 | The 42 guard tests, unskipped one at a time, plus the DI-13 interpreter check | All green; `python3 -m unittest discover -s Scripts/test-timings/tests` reports 0 skipped, on Python 3.12 and 3.14 | slice 06 |
| E-06b | US-06 / AC-06.2 | Budget derivation per stack | From ≥5 post-fix `main` runs: largest non-allowlisted unit L; budget B with 2.1·L ≤ B ≤ 0.55 × the smallest regression to catch (frontend: 0.55 × 115 s ≈ 63 s, so L ≤ ~30 s). Units that break the lower bound go on the allowlist with a reason and their own ceiling, and L is recomputed. Pass: zero over-budget non-allowlisted units across the ≥5 `main` runs after the budget commit (the derivation runs are input, not evidence). Backend: same rule, with its regression anchor named next to the number; it flips from warn to fail only when the ≥5 backend runs after the budget commit show zero budget warnings (largest backend unit ≤ B on each) | slice 06, `test-time-budget.json` neighbourhood, `ci-learnings.md` |
| E-06c | US-06 / AC-06.3 | Planted file on real data | One local run against a downloaded CSV with a planted 120 s row: exit 1 and an `::error::` naming it; output pasted | slice 06 (`main` never pushed red) |
| E-06d | US-06 / DO-7, AC-06.1 | A guard-only commit starts both verify jobs; the guard step starts no second test run | `Scripts/test-timings/**` present in `ci.yml` push paths and in both the frontend and backend patterns of `ci_changes.yml`; the guard step's `run:` block contains no `dotnet test`, `vitest` or `pnpm` | slice 06 |
| E-06e | US-06 / AC-06.4 | The rule in `docs/ci-learnings.md` | Entry present: a new test file over the budget fails CI; speed it up or allowlist it with a reason and a ceiling | slice 06 |
| E-KPI | OUT-6302-fe-job, -be-job, -coverage-kept | Job medians and test counts | Frontend ≤12 min, backend ≤13.5 min (median ≥5 `main` runs, query in `kpi-contracts.yaml`); passed + skipped + todo per stack not below 7,630 (run 38553818860) / 10,198 (run 38570239004) | DoD check point |

**E-names** (frontend; run once on the slice's base commit as `before`, once on its last commit as `after`):

```bash
( cd Lighthouse.Frontend && pnpm vitest run <files…> --reporter=json --outputFile="$D/fe-$WHEN.json" )
python3 Scripts/test-timings/vitest_to_csv.py --input "$D/fe-$WHEN.json" --source-root "$PWD/Lighthouse.Frontend" --output "$D/fe-$WHEN.csv"
export LC_ALL=C
tail -n +2 "$D/fe-$WHEN.csv" | sed -E 's/,[0-9.]+,[A-Za-z]+$//' | sort > "$D/fe-$WHEN.names"
tail -n +2 "$D/fe-$WHEN.csv" | grep -v ',Passed$' | sed -E 's/,[0-9.]+,[A-Za-z]+$//' | sort > "$D/fe-$WHEN.notpassed"
comm -23 "$D/fe-before.names" "$D/fe-after.names"          # must print nothing: no name lost
comm -13 "$D/fe-before.notpassed" "$D/fe-after.notpassed"  # must print nothing: no newly skipped or failing test
cut -d, -f1 "$D/fe-before.names" | uniq -c > "$D/fe-before.counts"   # same for after; after >= before per file
```

**E-names** (backend; same shape, the converter that CI uses):

```bash
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "<scope>" --logger "trx;LogFileName=be-$WHEN.trx" --results-directory "$D"
python3 Scripts/test-timings/trx_to_csv.py --trx "$D/be-$WHEN.trx" --output "$D/be-$WHEN.csv"
export LC_ALL=C
tail -n +2 "$D/be-$WHEN.csv" | sed -E 's/,[A-Za-z]+,[0-9.]+,[A-Za-z]+$//' | sort > "$D/be-$WHEN.names"
comm -23 "$D/be-before.names" "$D/be-after.names"          # must print nothing
```

The `sed` drops the trailing duration and outcome columns (never quoted), so a name containing commas or quotes compares as the converter wrote it.

**E-weaken**:

```bash
git diff <base>..HEAD -- <files…> | grep -E '^\+.*\b(it|test|describe)\.(skip|todo|only)\b|^\+.*\bx(it|describe)\(|^\+.*\[(Ignore|Explicit)\b|^\+.*Assert\.Ignore\('   # must print nothing
git diff <base>..HEAD -- <files…> | grep -cE '^-.*\b(expect\(|Assert\.)'   # every removed line paired with its replacement in the review note
```

**Per-file / per-class sums from a CI CSV** (E-02b, E-03b, E-04d; duration is the second-to-last column in both CSVs):

```bash
gh run download <runId> -R LetPeopleWork/Lighthouse -n test-timings-frontend -D "$D/<runId>"
awk -F, 'NR>1 && $1 ~ /(RefinementView\.paging|WorkItemsDialog\.contexts)\.test\.tsx$/ {s+=$(NF-1)} END {printf "%.1f\n", s/1000}' "$D/<runId>/test-timings-frontend.csv"
grep -E '^"?Lighthouse\.Backend\.Tests\.API\.Integration\.(Refinement|Reports|TeamFeatureSize)\.' "$D/<runId>/test-timings-backend.csv" | awk -F, '{s+=$(NF-1)} END {printf "%.1f\n", s/1000}'
```

**E-01 A/B** (in `Lighthouse.Frontend`; the version change is never committed):

```bash
pnpm vitest run 2>&1 | grep -E '^ *Duration'                 # arm B, vite 8.3.4 as committed
pnpm add -D vite@8.3.2 --save-exact && pnpm vitest run 2>&1 | grep -E '^ *Duration'   # arm A
git checkout -- package.json pnpm-lock.yaml && pnpm install      # back to arm B; alternate until 3 runs each
```

## Wave: DISTILL / [REF] WS strategy

The feature has no walking skeleton (D2), but the guard's entry point gets two **seam tests tagged `@walking_skeleton`** (35, 36): the real converter writes the CSV, the real check judges it. They prove the column contract between the two scripts, the thing a converter change would silently break. They cannot be green at hand-off, because the check is a scaffold, so they are pending like the rest (an exception to "one green walking skeleton", recorded as DI-10). Per the Architecture of Reference: the driving port (the command line) is real, the driven ports (CSV, JSON, C# source tree) are real files, and there is no external or non-deterministic port to fake.

## Wave: DISTILL / [REF] Adapter coverage

| Driven adapter | `@real-io` | Covered by |
|---|---|---|
| Timing CSV reader (`test-timings-{frontend,backend}.csv`) | YES | every test (CSV written with the converters' `CSV_COLUMNS`); 35-36 read CSVs the converters themselves wrote; 23-27, 30-31 the failure modes |
| Budget + allowlist reader (`test-time-budget.json`) | YES | every test (JSON in the temp folder); 16-22, 32 the failure modes |
| Backend test source scan (`--source-root`) | YES | 13-16 (real `.cs` files, including `bin`/`obj`/Stryker leftovers) |
| Verdict output (stdout annotations + exit code) | YES | every test asserts both; GitHub reads `::error::` / `::warning::` lines from stdout (DO-4) |

No in-memory doubles, so there is nothing they cannot model.

## Wave: DISTILL / [REF] Driving adapter coverage

| Entry point (DESIGN / DEVOPS) | Covered by |
|---|---|
| `python3 Scripts/test-timings/check_budget.py --stack frontend --csv …` | 1-27, 35 (subprocess with `sys.executable`) |
| `… --stack backend --csv … --source-root … --warn-only` (the CI backend command) | 28-32, 41 |
| `… --stack backend --csv … --source-root …` (after the backend flips to failing) | 13-16, 33-34, 36-40 |
| `python3 -m unittest discover -s Scripts/test-timings/tests` (the step's self-proof) | the suite itself; E-06a |

Zero uncovered entry points. Every test passes `--allowlist` explicitly; the default (the file beside the script) is exercised by E-06c.

## Wave: DISTILL / [REF] Scaffolds

| Scaffold | Path | Marker | State |
|---|---|---|---|
| Budget check | `Scripts/test-timings/check_budget.py` | `__SCAFFOLD__ = True`; `main` parses the final argument surface (`--stack`, `--csv`, `--allowlist`, `--source-root`, `--warn-only`) then raises `AssertionError("Not yet implemented -- RED scaffold")` | Imports clean on 3.12 and 3.14; RED when unskipped |
| Budget + allowlist data | `Scripts/test-timings/test-time-budget.json` | not created | DELIVER slice 06 creates it with the calibrated numbers (E-06b); a placeholder budget would be a number nobody derived |

## Wave: DISTILL / [REF] Red-classification

Run with the pending marker temporarily turned off (then restored, verified by diff), on Python 3.14.8 and 3.12.13:

| Tests | Classification | Evidence |
|---|---|---|
| All 42 (63 failures counting subtests) | **RED, MISSING_FUNCTIONALITY** | 0 `ERROR`, 63 `FAIL`; every subprocess run ends in `AssertionError: Not yet implemented -- RED scaffold` on stderr. 48 fail first on the missing annotation (checked before the exit code, because the scaffold's uncaught error also exits 1), 15 passing-run tests on exit 1 instead of 0; none on set-up, import or fixture |

No test passes against the scaffold: tests that expect exit 1 also require an annotation on stdout and no traceback, which the scaffold cannot produce. Satisfiability was checked too: a throwaway reference implementation (scratch only, not committed, not handed over) turned the first 41 green on both Python versions, so no assertion is unsatisfiable or self-contradictory. Test 42 was added later and confirmed RED (not BROKEN) against the scaffold on Python 3.14.8.

## Wave: DISTILL / [REF] Test placement

`Scripts/test-timings/tests/test_check_budget.py`, beside `test_vitest_to_csv.py`, `test_trx_to_csv.py` and `test_summarise.py`: same folder, same `sys.path` insert of the parent folder, same `unittest` idiom, discovered by the same command. One helper class (`CheckWorkspace`) is the single vocabulary for "a timing file with these files and seconds", "an allowlist with these entries", "a class declared in the test source" and "run the check", so test bodies stay three to five lines.

## Wave: DISTILL / [REF] Outcome registration

**Registered `OUT-5`** (`kind: operation`, artifact `Scripts/test-timings/check_budget.py`) in `docs/product/outcomes/registry.yaml` with `nwave-ai outcomes register`. The CLI reflowed the four existing entries' text; that churn was reverted so the diff is the new entry alone.

## Wave: DISTILL / [REF] Pre-requisites

- DESIGN driving port: the `check_budget.py` command line (DESIGN Driving ports, DEVOPS step body).
- DEVOPS environments: `local-dev` (Python 3.14.8) and `ci-runner` (system Python, 3.12 line). Locally, 3.12 is reachable with `uv run --no-project --python 3.12 python -m unittest discover -s Scripts/test-timings/tests`.
- Converters unchanged: the fixtures take their headers from the converters' `CSV_COLUMNS`, so a renamed column moves fixtures and check together; only the seam tests 35-36 catch a converter and the check drifting apart.
- DELIVER order unchanged: slices 01-05 (evidence checks) before slice 06 (unskip the 42 tests, write the script, create `test-time-budget.json`, wire the step, DO-7 paths, `ci-learnings.md`).
- Project policy: one Driving row appended to `docs/architecture/atdd-infrastructure-policy.md` (CI timing scripts → `unittest` + subprocess against temp files).

## Wave: DISTILL / [REF] Self-completeness audit

15-item checklist: **14/15, COMPLETE.** C1a empty input (24, 25) ✓; C1b boundaries 59 / 60 / 60.001 s and ceiling at/over (2, 4, 7, 10, 11) ✓; C2a/C2b N/A, the check is stateless ✓; C3 0 / 1 / many units and allowlist entries ✓; C4a/C4b N/A, the check mutates nothing (8 pins it) ✓; C5a `--warn-only` × {breach, ceiling breach, missing CSV, malformed CSV, malformed allowlist} and both stacks ✓; C5b warn-only changes only the annotation kind and the exit code (28-29 assert both) ✓; C6a malformed value for every input (CSV, allowlist, source root) ✓; C6b every declared failure triggered ✓; C6c closed error set (exit 0/1/2, no traceback) ✓; C7b/C7c N/A, read-only and single invocation ✓. **Gap, documented, in DELIVER scope if wanted:** C7a, an unreadable (permission-denied) CSV; expected to fall under "cannot be read" (exit 2). No specification ambiguity routed upstream.

## Wave: DISTILL / [REF] Wave-decisions summary (DISTILL)

Locked under the AFK recommended-option rule (maintainer may revisit); none changes what a Lighthouse user sees.

| ID | Decision | Why |
|---|---|---|
| DI-1 | CLI = the DEVOPS command plus `--allowlist <file>` (default `Scripts/test-timings/test-time-budget.json`) and a default `--source-root` of the repository's `Lighthouse.Backend/Lighthouse.Backend.Tests` | Tests must point at a temp file; CI and the local command stay exactly as DEVOPS wrote them |
| DI-2 | One JSON file, a section per stack: `{"frontend": {"budget_seconds": N, "allowlist": [{"unit", "reason", "ceiling_seconds"}]}, "backend": {…}}` | ADR-237 §4; the budget lives with the allowlist so one reviewed file holds every limit |
| DI-3 | Exit 0 pass, 1 verdict (over a limit, stale entry), 2 cannot judge (missing/malformed input) | A reader of the log can tell "your test is slow" from "the check had nothing to read" |
| DI-4 | Limits are inclusive: at the budget or ceiling passes, a millisecond over fails | Pins the boundary a mutant would flip, standing in for the mutation run DEVOPS ruled out |
| DI-5 | One annotation per finding on stdout, naming unit, seconds to one decimal, the limit and the allowlist file name; passing runs print no annotation. In the message, `%` is written `%25`, CR `%0D` and LF `%0A` (test 42) | DO-4; the message is the fix instruction, and GitHub's parser would otherwise end the annotation at a line break and read the rest as plain log or as another command |
| DI-6 | A header with no rows counts as empty and fails closed | A green run always has tests; zero rows means the CSV is wrong |
| DI-7 | A stale allowlist entry is a verdict (exit 1, `::error::`), not an input error, and `--warn-only` does **not** soften it (test 41) | DO-3 and ADR-237 §6 soften budget breaches only, and §4 says a stale entry fails the check; the allowlist is the reviewed file, so it must stay true while the backend still warns |
| DI-8 | Reason must be non-blank text; ceiling a positive number; the stack section must have a budget | "With a reason" means a reason a reviewer can read |
| DI-9 | A backend class counts as in source when a `.cs` file outside `bin`, `obj`, `.stryker-tmp*` and `StrykerOutput` declares that class name under that namespace, whatever the file is called (tests 39-40); a missing source folder fails closed | DO-6; a missing folder would otherwise make every entry look stale |
| DI-10 | The two seam tests are pending, not green, at hand-off | Their subject is the check, which is a scaffold; green would need a second implementation |
| DI-11 | Example-based only; the split-invariance property is example-pinned | Subprocess layer; Hypothesis is not a dependency and DO-1 forbids adding one |
| DI-12 | No `.feature` file, no state-delta port, no `domain_types.py`, no step-reuse ratio | Project policy: the Python-pilot machinery does not apply here; `CheckWorkspace` is the single vocabulary |
| DI-13 | The script refuses to run on Python older than 3.12: it checks `sys.version_info < (3, 12)` first and exits non-zero with a message naming the version needed and the one found | DO-1. Not an acceptance test, because a subprocess test runs under whatever interpreter runs the suite and cannot get an older one without faking `sys.version_info`; DELIVER covers it with a unit-level check of the guard function |

**Handoff to DELIVER:** slices 01-05 run and record the evidence checks; slice 06 unskips the 42 tests one at a time (suggested order: 1, 3, 2, 4, 37, 5, 7, 42, 6, 8, 33-34, 9-15, 38-40, 23-27, 16-22, 28-32, 41, 35-36), adds the DI-13 interpreter check, replaces the scaffold, creates `test-time-budget.json` from E-06b, adds the guard step to both verify jobs and the DO-7 trigger paths, and writes the `ci-learnings.md` rule.

## Wave: DISTILL / [REF] Acceptance review verdict + action items

Sentinel (`nw-acceptance-designer-reviewer`, 2026-10-14) on the DISTILL sections, the tests and the scaffold: **conditionally approved, 0 blockers, 2 high, 4 medium, 9 low.** All mandates passed (driving port only, plain-language names, full journey per test). Applied the same day:

- **High, budget value never varied** → test 37 sets a 30 s budget per stack.
- **High, warn-only vs a stale entry contradicted DO-3 / ADR-237** → DI-7 corrected to the upstream rule (warn-only softens breaches only); test 41 pins it.
- **Medium**: backend ceiling in fail mode (test 38); warn-only fail-closed on both stacks (test 30); tag mapping stated as living in the table; E-01 restated in AC-01.2's words.
- **Low**: namespace and file-name cases (39, 40); ceiling 0 and `true` refused (18); arguments containing parentheses (34); annotation asserted before the exit code so the first failure names the missing verdict; red-classification and converter-seam wording; E-names compares not-passed names before vs after under `LC_ALL=C`; E-04b checks the timer commit comes first; E-06d covers AC-06.1 (no second test run); E-05a names the classes holding the four outlier tests.

The three other final-gate reviewers (DISCUSS, DESIGN, DEVOPS) were not dispatched in this run.

---
---

# DELIVER

## Wave: DELIVER / [WHY] Upstream Issues

**Deviation from DS-Q1 / DDD-4 / DO-3 ("frontend fails from the start"), 2026-10-14.** The maintainer is away and nothing may be pushed in this DELIVER run, so no post-fix `main` CI runs will exist to calibrate the budget against, and DS-Q1 derives L from ≥5 of them. Shipping a failing frontend gate on a number nobody could check against CI would let the first push go red on calibration error rather than on a slow file. Recorded as the [ADR-237 amendment of 2026-10-14](../../product/architecture/adr-237-test-time-budget-guard.md#amendment-2026-10-14-provisional-pending-maintainer-ratification) (provisional, pending maintainer ratification). So, in roadmap step 06-06:

- **Provisional budget per stack**, same formula (B ≥ 2.1 × L, B ≤ 0.55 × the smallest regression to catch), with L taken from the post-fix **local** timing CSVs of the full suites (produced by the existing converters from a local `pnpm vitest run --coverage --reporter=json` and a `dotnet test` TRX), plus the most recent CI CSVs for units the fixes did not touch, taking the larger time per unit because the local machine is not the CI runner. The derivation records which source set L.
- **Both stacks wired with `--warn-only`** until real post-fix `main` runs exist. Warn mode still fails on unreadable input, a stale allowlist entry or a failing self-test (DO-3, DI-7), so the guard is live; only a budget breach is softened.
- **Recorded follow-up for the maintainer:** after ≥5 post-push `main` runs, re-derive both budgets on them (E-06b) and delete `--warn-only` from the frontend step; the backend flips by its own DDD-4 rule (≥5 backend `main` runs after the budget commit with zero budget warnings).
- **Owed CI medians (follow-ups, each needing runs on `main` after the push):**
  - E-02b CI: median of ≥5 `main` runs, the two slice-02 files summed ≤40 s.
  - E-03b: median of ≥5 `main` runs, the six slice-03 files ≤70 s and all eight named files ≤110 s.
  - E-04d: median of ≥5 `main` runs, the in-scope Refinement / Reports / TeamFeatureSize classes ≤192 s.
  - E-KPI: median of ≥5 `main` runs, `Verify Frontend` ≤12 min and `Verify Backend` ≤13.5 min; test totals not below 7,630 / 10,198.

Also carried: the slice-06 brief and the journey named the budget file `Scripts/test-time-budget.json`, while DI-1 and the DO-7 trigger glob put it at `Scripts/test-timings/test-time-budget.json`. DELIVER follows DI-1; the brief and journey were corrected to match.


## Wave: DELIVER / [REF] Slice 02 record — step 02-01 `RefinementView.paging.test.tsx`

**E-02c cause (measured):** two whole-page role lookups in the test file itself, not the kit and not the DataGrid render: `getByRole("button", { name: "Go to next page" })` computes an accessible name for every button on a 100-row page (≈650 ms per call), and the page reader ran three role queries inside each of 100 rows (`within(row)…ByRole("link" | "gridcell")`), costing 0.3–0.5 s per read and up to four reads per test.

**Fix (test file only; `RefinementTabTestKit.tsx` unchanged, so comments and votes are untouched):**

- The page reader still finds rows with `screen.getAllByRole("row")`. Inside each row it now takes the link as `a[href]` and the first cell as `[role="gridcell"]`. These are the same elements, and the same `toEqual` assertions on reference ids and numbers read them.
- `goToNextPage` finds the pager button with `getByLabelText("Go to next page")`, the button's own `aria-label`. That lookup only sets up a click. The page change is still proven by `findByText("101–143 of 143")` and the rows asserted after it. The assertion `queryByRole("button", { name: "Go to next page" })).not.toBeInTheDocument()` in the 38-row test stays a role query.
- **Removed or shortened waits:** none. No `waitFor` or `findBy` was replaced or given a lower timeout. The filter test's `waitFor(…, { timeout: 5000 })` is unchanged; it simply polls a cheaper reader now.
- **Removed `expect(` lines:** 0.

**Local timing** (summed per-test durations from the run's Vitest JSON; paging, comments and votes run together, never two Vitest processes at once; another session's Stryker job was loading the machine):

| Run | Load avg (1 min) | paging | comments | votes |
|---|---|---|---|---|
| before1 | 5.54 | 34.3 s | 13.0 s | 11.2 s |
| before2 | 5.37 | 33.9 s | 12.5 s | 11.6 s |
| before3 | 5.45 | 34.7 s | 13.4 s | 12.4 s |
| after1 | 5.53 | 16.0 s | 12.8 s | 11.8 s |
| after2 | 6.33 | 15.9 s | 12.7 s | 11.5 s |
| after3 | 7.23 | 16.7 s | 13.3 s | 12.5 s |
| before4 (bracket, base-commit worktree) | 8.06 | 34.7 s | 13.6 s | 12.2 s |

The before median is 34.3 s and the after median is 16.0 s, a ratio of 0.47 (2.1× faster). Load rose during the after runs and the bracket before run still read 34.7 s, so load drift does not explain the gain. The remaining ~1.2 s per test is rendering 100 to 143 rows through the real DataGrid and re-rendering on the page change, which is production render time and outside this step.

**E-names** (base `0c9e489f9` against the step's change; paging, comments, votes): `comm -23` prints nothing against before1 or before4, and `comm -13` on not-passed prints nothing. Counts are unchanged at paging 11, comments 24 and votes 33. **E-weaken:** the skip grep prints nothing and the removed-`expect(` count is 0.

## Wave: DELIVER / [REF] Slice 02 record — step 02-02 `WorkItemsDialog.contexts.test.tsx`

**E-02c cause (measured, CPU profile of the whole file):** about 40 % of the file's CPU time is jsdom's `getComputedStyle`, which matches every one of the ~311 emotion style rules the dialog and DataGrid inject. Opening Manage columns triggers it through Popper's scroll-parent walk and the focus moves inside the grid (production render, ≈250 ms per opening, about 70 openings in the file), and the kit's set-up role queries trigger it as well (`getByRole("dialog")`, `queryAllByRole("columnheader")`, `getAllByRole("checkbox")`, `findByRole("menuitem", { name })`, ≈5 s of CPU across the file).

**Same pattern as 02-01?** Partly. The role-query half is the same: a `*ByRole` used only to find something to click or read pays a per-element style or accessible-name computation on a page full of DataGrid markup. The larger half is not the same. It is production render cost (Popper and focus in jsdom), so it is cut only by opening the Manage columns panel fewer times. For slice 03 and the shared-helper decision, the shared part is "don't use role queries for set-up on DataGrid pages". Each kit fixes that in a few lines with `querySelectorAll('[role=…]')` or `getByLabelText`, so no shared helper is warranted on this evidence (DS-Q3 option C not triggered).

**Fix (kit and test file; production untouched):**

- `WorkItemsDialogTestKit.tsx`: `theDialog` reads `[role="dialog"]` and still fails unless there is exactly one. The column headers are `[role="columnheader"]` inside the dialog. The Manage columns entries are `input[type="checkbox"]:not([role])`, the same set the implicit-checkbox role query found (MUI `Switch` carries `role="switch"` and stays excluded), still filtered by `name`. "Manage columns" in the column menu is found among `[role="menuitem"]` by its text, inside `waitFor`. New `turnColumns(user, changes)` turns several columns in one Manage columns visit, in the order given. `turnColumn` delegates to it, so the other nine importers are unchanged.
- `WorkItemsDialog.contexts.test.tsx`: the 13 `…comes back with the reader's own layout…` cases, `a column turned on appears in its place in the fixed order`, and the `every column named here sorts on a header click` cases now make their column changes in one Manage columns visit instead of one visit per column. The changes and their order are the same, and the assertions are untouched.
- **Removed or shortened waits:** one wait was replaced. `screen.findByRole("menuitem", { name: "Manage columns" })` in `openManageColumns` became `waitFor(…)`, which polls for a `[role="menuitem"]` whose text is "Manage columns". The default timeout is the same 1000 ms, and the wait is still a wait, not a one-shot query. It only finds the entry to click. Every caller's behaviour is still proven by the assertions that follow, such as `catalogueOffered()` equalling the expected list and `contextColumnsShown()` after a change. No timeout was lowered.
- **Removed `expect(` lines:** 0. No role query that is itself an assertion was touched. All the removed `*ByRole` calls were set-up lookups inside kit helpers.

**Local timing** (summed per-test durations from each run's Vitest JSON; the 9 other kit importers plus `RefinementView.paging` run together, one Vitest at a time. Another session's backend tests and Vitest run were loading the machine, and load climbed from ~5 to ~14 during the series). `RefinementView.paging` is untouched by this step and serves as the in-run control:

| Run | Load avg (1 min, start) | contexts | RefinementView.paging (control) | contexts ÷ control |
|---|---|---|---|---|
| before1 | 4.76 | 34.3 s | 18.9 s | 1.81 |
| before2 | 6.31 | 45.1 s | 25.9 s | 1.74 |
| before3 | 8.38 | 45.4 s | 26.8 s | 1.69 |
| after1 | 10.44 | 46.3 s | 32.7 s | 1.42 |
| after2 | 14.36 | 40.4 s | 28.1 s | 1.44 |
| after3 | 13.21 | 40.4 s | 28.2 s | 1.43 |
| before4 (bracket, base-commit worktree) | 13.91 | 50.3 s | 28.1 s | 1.79 |

The raw medians are 45.1 s before and 40.4 s after, a ratio of 0.90. The load rose by ~2× between the series, though, and the raw ratio understates the gain. At matched load the bracket before4 read 50.3 s against after2/after3 at 40.4 s, a ratio of 0.80, with the control flat at 28.1–28.2 s. The control-normalised medians are 1.74 → 1.43, also 0.82. Single-file runs on a quieter machine agree: 27.9 s at load 2.2 before, 22.4 s at load 6.4 after (0.80). The file runs about 1.25× faster, not a large fraction. The remaining ~200 ms per test is DataGrid render, plus the ~250 ms Popper/focus cost of each Manage columns opening that a test needs, both production render time.

**Two-file sum (RefinementView.paging + contexts, one run's JSON, these two files only):** pafter1 45.1 s (load 12.4), pafter2 49.9 s (load 14.3), pbefore1 53.6 s (load 10.9). **The 40 s criterion is not shown under this load.** On the quieter readings the parts add to about 38 s: contexts 22.4 s at load 6.4, and paging 16.0 s, 02-01's after median at load 5.5–7. That sum is not from one run, so it does not satisfy the criterion's wording. The run needs repeating on an idle machine.

**E-names** (base `219acea1f` against the step's change; all ten `WorkItemsDialogTestKit` importers plus `RefinementView.paging`): `comm -23` prints nothing against before1 and before4 for after1, after3 and after4 (after4 = final kit). `comm -13` on not-passed prints nothing. Counts are unchanged: contexts 108, `BaseMetricsView` 192, `featureChildItemsDialog` 15, `DataGridBase.defaults` 13, `RefinementView.paging` 11, `WorkItemsDialog.loading` 16, `layout` 7, `cumulativeStateTimeDialog` 6, `WorkItemsDialog.paging` 1, `LineRunChart.dialog` 1. **E-weaken:** the skip grep prints nothing and the removed-`expect(` count is 0. `pnpm test`: 480 files passed, 1 skipped; 7594 tests passed, 37 skipped, 1 todo.
