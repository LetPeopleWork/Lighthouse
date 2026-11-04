# Feature: story-6513-t7-drop-sonar-coverage

Story #6513 · T7 Drop Sonar coverage, keep Sonar's general analysis · part of Epic #6506 "Fast, reliable test suites"
(a Continuous Improvement Epic). Written 2026-11-04 from the maintainer's decisions of the same day; revised the same
day after the DISCUSS review. Facts below were read from the files on `main` (`e85f2ebc5`), not assumed.

## Wave: DISCUSS / [REF] Decisions

| Id | Decision (maintainer, 2026-11-04) |
|---|---|
| D-6513-1 | Remove code coverage collection from CI on both stacks; keep Sonar's general analysis. |
| D-6513-2 | The maintainer checked: the Sonar quality gate does not check coverage, so removing it is safe. |
| D-6513-3 | Local test runs ignore coverage unless there is a specific reason. `pnpm test` and `dotnet test` already collect none. |
| D-6513-4 | Keep `@vitest/coverage-v8` and an opt-in local coverage command; behaviour-free rewrites use it to check coverage did not fall. |
| D-6513-5 | Order: DISCUSS and DESIGN alongside FE7 (#6457). T7 lands only after FE7's CI after-measurement (at least 5 main runs after its last push), because that measurement is taken on the coverage run. T4 (#6462) follows T7, so the budgets are calibrated once, on the final CI run. |
| D-6513-6 | T8 (#6514) sharding is undecided and separate. Nothing here depends on it. |

FE7 status at writing: its last push is `e85f2ebc5` (2026-11-04); the CI after-measurement is not yet done, so T7 is
not pushed yet.

## Wave: DISCUSS / [REF] Problem and persona

Persona: the Lighthouse developer (today `lighthouse-maintainer`, plus the parallel Claude sessions that read CI
timings) who pushes to `main` and waits for CI to answer. Entry point: the push. Observable output: the minutes of the
"Test Frontend and Sonar report" and "Test Backend" steps in the CI run, and the Sonar analysis still reporting issues.

CI runs every test with coverage on, only so SonarCloud can show a coverage figure that no gate reads (D-6513-2).
The developer waits longer for the answer to a push, and the CI bill is longer, for a number nobody uses. Measured
coverage cost: locally, frontend `--coverage` took 326 and 319 s against 254 and 248 s plain on the same machine and
workers (about 71 s, +28 %; `story-6457-fe7-fast-frontend-tests/measurement/slice-01a-calibration.md`, runs cov-1,
cov-2, plain-1, plain-2); per test, median 1.35x, p90 1.78x. The CI cost of coverage is not measured yet: FE7's
after-measurement is taken on the coverage run, and a plain CI run is the first measurement of the saving. The backend
cost of the opencover collector is not measured anywhere.

## Wave: DISCUSS / [REF] Facts check

| Decision says | Files say | Verdict |
|---|---|---|
| Frontend CI runs `pnpm run sonarreport` = `vitest run --coverage`, CI's only Vitest run | `.github/actions/build-frontend/action.yml`, step "Test Frontend and Sonar report" (`run: pnpm run sonarreport`); `Lighthouse.Frontend/package.json:18` is `cross-env TZ=Europe/Zurich vitest run --coverage`; `package.json:17` `test` is the same without it. Only `.github` caller of `sonarreport`. | Confirmed. |
| Dropping coverage leaves the FE7 limits slack | **Differs, and the two checks behave differently.** (a) *Per-test guard*, in the test-run step: `timeBudgetOptions.ts` marks a run "coverage" only when argv has `--coverage`; `compareToAllowList.ts` (`limitFor`, line 70) multiplies the 2 s budget and every allow-list ceiling by `COVERAGE_FACTOR` (3, `vitest.config.ts`) only then. `timeBudgetReporter.ts:57` sets `process.exitCode = 1` on a failing verdict, so a plain CI run judges tests with the unscaled limits, **tighter**, and the "Test Frontend and Sonar report" step itself can go red on tests that pass today. (b) *Per-file check*, `Scripts/test-timings/check_budget.py` ("Check test-time budget" in `ci_frontend.yml`): `budget_seconds: 63` and the per-file ceilings in `Scripts/test-timings/test-time-budget.json` are in CI coverage-run seconds and carry **no factor**, so on a plain run the file sums fall and the limit is **looser**. | Differs. The per-test half must be designed in T7; the per-file half goes slack until T4. |
| FE7 guard: `COVERAGE_FACTOR = 3`, ceilings in CI coverage-run seconds | `vitest.config.ts` (`COVERAGE_FACTOR = 3`, comment above it: applies to any run with coverage on, local too); `ci_frontend.yml` comment above "Check test-time budget": "The budget is in CI coverage-run seconds". | Confirmed. |
| Backend steps "(with Coverage)" and "(without Coverage)", opencover settings | `ci_backend.yml:100` (`sonar.cs.opencover.reportsPaths`), `:103` (`sonar.coverage.exclusions`), `:148-151` with coverage, `:153-156` without; split by `if: ${{ inputs.run_sonar }}` / `if: ${{ !inputs.run_sonar }}`. The only caller, `ci.yml:63`, passes `run_sonar: true`, so the "without" step never runs today. | Confirmed, plus: the "without" step is already the target shape. |
| `coverlet.collector` in the test project | `Lighthouse.Backend.Tests.csproj:45`, 10.1.0; nothing else references it. | Confirmed. |
| Sonar frontend properties | `Lighthouse.Frontend/sonar-project.properties`: `sonar.javascript.lcov.reportPaths=coverage/lcov.info` (line 9) and `sonar.testExecutionReportPaths=sonar-report.xml` (line 8). Scan step: `build-frontend/action.yml` "Frontend SonarCloud". | Confirmed. |
| Reports Sonar needs that the coverage run also produced | `sonar-report.xml` (`vitest-sonar-reporter`) and `test-results.json` (`json` reporter) are config reporters in `vitest.config.ts`, independent of `--coverage`: they survive. Backend: `sonar.cs.vstest.reportsPaths=**/TestResults/*.trx` and `--logger trx` are in both steps: survive. Only `coverage/lcov.info` and `coverage.opencover.xml` go. | Confirmed. |
| Quality gate does not check coverage | Not verifiable from the repo (a SonarCloud setting); `ci_sonar_gates.yml` only waits on the gate through `report-task.txt`, which neither stack derives from coverage. Taken as given (D-6513-2). | Given. |
| Who else reads coverage output | No script reads it (`Scripts/`). Comments: `ci.yml:61` ("with coverage"), `ci_frontend.yml` budget comment. Docs: `docs/ci-learnings.md:114-116` tells developers to run touched test files with `CI=true pnpm run sonarreport` (the script goes, so this line changes to the new command); `:114` "Under `--coverage` it kills the worker" stays true for the opt-in command; `:332` is Stryker `NoCoverage`, unrelated. `.claude/commands/clean-ci.md:97,119,138,155-157` describe coverage-on-new-code failures. Project `CLAUDE.md`: no coverage mention. `.github/instructions/*` "100% coverage through behavior" is a design principle: stays. `.github/scripts/toolchain-pins.test.mjs` pins `ci_frontend.yml` and `build-frontend/action.yml` text (fixtures at lines 43 and 62; it does not mention coverage or `sonarreport` today). | Breaks nothing found; edits listed in the checklist. |

Also: `ci_backend.yml:204` runs the backend budget with `--warn-only` already, so the backend cannot go red from T7.

## Wave: DISCUSS / [REF] Story

### US-6513-1: A push to main gets its CI answer sooner, with the Sonar analysis intact

`job_id: infrastructure-only`
`infrastructure_rationale`: no end-user job changes; the work removes a cost the Lighthouse developer waits on at every
push (a coverage run feeding a figure no gate reads) and removes an uncalibrated dependency from the time-budget guard,
so T4 can set the budgets once on the final CI run.

Elevator pitch:
- **Before**: the developer pushes to `main`; CI runs every test with coverage on and the test steps take the longer
  coverage time (locally +28 % on the frontend); lcov and opencover files feed a Sonar figure nobody reads.
- **After**: the same push runs the plain test commands; the test steps show fewer minutes in the CI run; the Sonar
  analysis still reports issues and the gate still runs; only the coverage figure is gone.
- **Decision enabled**: T4 sets budgets from one final plain CI run; a behaviour-free rewrite checks "coverage did not
  fall" locally with `pnpm test:coverage`.

Domain examples:
1. Jonas pushes a small frontend change to `main`; "Test Frontend and Sonar report" runs plain and shows fewer minutes
   than FE7's after-measurement; Sonar reports 0 new issues and the gate passes.
2. The maintainer rewrites `ReportsPage.removal.test.tsx` into faster tests and runs `pnpm test:coverage` before and
   after; the covered percentage did not fall.
3. A test that takes 9 s is planted on a branch; the test-run step goes red naming the test (per-test guard;
   `LIGHTHOUSE_TIME_BUDGET` unset means fail).

### Slices (thin, each ships alone, in either order, both after the FE7 gate)

Estimated saving (excludes calendar waits: queueing, the Sonar gate wait, other jobs). Frontend: about 1 min of the
test step per run is the local figure (71 s at 6 workers, +28 %); the CI saving is **not measured** (CI has 3 workers
and a slower machine) and is read from the first five plain main runs against FE7's after-measurement. Backend: **not
measured**.

#### S1 Frontend

Delivers: the push's frontend test step runs without coverage, the guards stay meaningful, the opt-in command exists.

Acceptance criteria:
- AC1 `build-frontend/action.yml` runs the plain test script; `git grep -n -e "--coverage" -e sonarreport -- .github Lighthouse.Frontend/package.json` shows `--coverage` only in the new `test:coverage` script.
- AC2 `sonar-project.properties` has no `sonar.javascript.lcov.reportPaths`; `sonar.testExecutionReportPaths` stays, and `sonar-report.xml` and `test-results.json` are produced by the CI run.
- AC3 `pnpm test:coverage` runs `vitest run --coverage` (name settled: `test:coverage`, replacing `sonarreport`); `@vitest/coverage-v8` stays; `pnpm test` collects none.
- AC4 Per-test guard: on a plain CI run the limits are no tighter than today's effective ones (3 x the budget and 3 x each ceiling); a planted 9 s test turns the "Test Frontend and Sonar report" step red (through `process.exitCode`), and a test inside today's limits does not.
- AC5 Per-file check: `Scripts/test-timings/check_budget.py` and `test-time-budget.json` are unchanged by T7, the "Check test-time budget" step still runs, and the comment above it states that its limits are in coverage-run seconds and looser on a plain run until T4. Check: `git grep -n "coverage-run" -- .github Lighthouse.Frontend Scripts` returns only lines that say so.
- AC6 `docs/ci-learnings.md` no longer names the removed script: `git grep -n sonarreport` returns nothing outside `docs/feature/`.
- AC7 `.github/scripts/toolchain-pins.test.mjs` passes after the `action.yml` and `ci_frontend.yml` edits (fixtures updated if they pin the changed text).

Scenarios (infrastructure form):

1. Given the developer pushes a frontend change to `main`, when `Verify Frontend` runs, then the test step runs the plain script and its minutes are lower than FE7's after-measurement.
2. Given the plain run finished, when the Sonar scan runs, then it reads `sonar-report.xml`, reports issues for `LetPeopleWork_Lighthouse_Frontend`, and the quality gate job passes.
3. Given a test planted at 9 s, when the push's test step runs, then that step fails and names the test; and given a test at 5 s, which is inside today's 6 s limit, then the step stays green.
4. Given a file whose tests sum to 40 s on the plain run, when "Check test-time budget" runs, then it passes against the unchanged 63 s limit, and `LIGHTHOUSE_BUDGET_WARN_ONLY=true` still switches that check to warn.
5. Given the maintainer finished a behaviour-free rewrite, when they run `pnpm test:coverage`, then the coverage report prints, and `pnpm test` still prints none.
6. Given the toolchain pins test, when it runs after the edits, then it passes.

#### S2 Backend

Delivers: the push's backend test step runs without the coverage collector; the Sonar scan keeps its test results.

Acceptance criteria:
- AC1 `ci_backend.yml` has one "Test Backend" step: no `--collect:"XPlat Code Coverage"`, no opencover settings, `--logger trx` and `RunConfiguration.MaxCpuCount=0` stay; the `if: ${{ !inputs.run_sonar }}` line on the former "without" step goes (the `run_sonar` conditions on the Sonar steps stay and `ci.yml` still passes `run_sonar: true`).
- AC2 `dotnet sonarscanner begin` has no `sonar.cs.opencover.reportsPaths` and no `sonar.coverage.exclusions`; `sonar.cs.vstest.reportsPaths` stays. Check: `git grep -n -e opencover -e "XPlat Code Coverage" -e coverage.exclusions -- .github` returns nothing.
- AC3 The `ci.yml:61` comment no longer says "with coverage".
- AC4 `coverlet.collector` stays in the test project (recommendation; see Open for the maintainer, 2).

Scenarios (infrastructure form):

1. Given the developer pushes a backend change to `main`, when `Verify Backend` runs, then one "Test Backend" step runs with `--logger trx` and no coverage collector.
2. Given that step finished, when `dotnet sonarscanner end` runs, then the scan uploads, issues are reported for `LetPeopleWork_Lighthouse`, and the quality gate job passes.
3. Given the test step finished, when the timings steps run, then `test-timings-backend` is uploaded and the backend budget step runs (warn-only, as today).
4. Given five plain main runs, when their "Test Backend" minutes are compared with the five runs before the push, then they are lower.
5. Given a developer runs `dotnet test --collect:"XPlat Code Coverage"` locally, then coverage still works (coverlet stays).

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Measure | Baseline | Target |
|---|---|---|---|
| CI test step minutes, frontend | Duration of "Test Frontend and Sonar report", median of at least 5 main runs, read as FE7 reads it | FE7's CI after-measurement (taken on the coverage run) | Lower. Local coverage cost is 71 s; the CI saving is not measured |
| CI test step minutes, backend | Duration of "Test Backend", median of at least 5 main runs | The five main runs before the push ("Test Backend (with Coverage)") | Lower; cost not measured |
| Guardrail: Sonar analysis | Both projects still report issues | Today | Unchanged; only the coverage measure goes |
| Guardrail: quality gate | `sonar-gates` runs and passes on the first main run per stack | Passes | Passes |
| Guardrail: per-test guard | A planted 9 s test turns the test-run step red; no existing test newly fails | As today | Same |
| Guardrail: per-file check | `check_budget.py` still runs on the plain CSV and still fails a planted over-limit file | As today | Same (limit looser until T4) |

## Wave: DISCUSS / [REF] Scope and order

Right-sized: one Story, two slices, no bounded context, no integration point beyond SonarCloud, about 1 day (calendar
waits excluded). Carpaccio check: PASS. Epic ship-alone rule: S1 and S2 each shorten the push-to-answer time of their
own stack; neither waits on the other. Sequence: after FE7's after-measurement, before T4 (#6462). Nothing is pushed
before FE7's measurement. "New feature Epic checks the improvement backlog": N/A, because this item sits in the
Continuous Improvement Epic.

## Wave: DISCUSS / [REF] Cross-cutting checklist

| Item | Answer |
|---|---|
| RBAC impact | N/A, because no endpoint, role or permission is touched. |
| Lighthouse-Clients CLI/MCP versioning | N/A, because no route, DTO or response shape changes. |
| Website marketing surface | N/A, because nothing a user sees changes. |
| Docs and screenshots | Product docs and screenshots: N/A, because no user-visible behaviour changes. Developer docs in scope: `docs/ci-learnings.md:114-116`, `.claude/commands/clean-ci.md` coverage-on-new-code lines, the `ci.yml` and `ci_frontend.yml` comments, and a line in the `CLAUDE.md` quality gates naming `pnpm test:coverage` and when to use it (D-6513-3/4), written at finalize. No release notes (written at /release). |
| Context map (ADR-270) | Contexts touched: none (Platform-level CI and test tooling). New terms, concepts, edges: none. Domain events: none. ADR-270 and `ARCHITECTURE.md` §4 need no change. |
| UI surface (ADR-271) | None; no sketch, loading look or failure look. |
| Usage-data event (for DEVOPS) | Provisionally N/A, because it is CI tooling nobody uses in the product; DEVOPS confirms. |
| Terminology | N/A, because no user-facing word changes. |
| ADO | Continuous Improvement Epic #6506, Story #6513; transitions per `/ado-sync`; Epic state is the maintainer's. This wave creates and changes nothing in ADO. |
| Mutation testing | If S1 changes `compareToAllowList.ts` or `timeBudgetOptions.ts`, StrykerJS runs last on frozen code on those files, kill rate >= 80%. Workflow YAML, csproj, properties: N/A, because declarative. |

## Wave: DISCUSS / [REF] Definition of Ready

| # | Item | Status | Evidence |
|---|---|---|---|
| 1 | Problem statement | PASS | Problem and persona section, in the developer's terms. |
| 2 | Persona | PASS | Lighthouse developer pushing to `main`; entry point and observable output named. |
| 3 | 3+ domain examples with real data | PASS | Jonas's push, the `ReportsPage.removal.test.tsx` rewrite, a planted 9 s test; figures 326/319 s vs 254/248 s. |
| 4 | UAT in Given/When/Then, 3-7 | PASS | S1 six scenarios, S2 five, in infrastructure form. |
| 5 | AC derived from UAT | PASS | S1 AC1-7 and S2 AC1-4 map to the scenarios; the greps make S1 AC1, AC5, AC6 and S2 AC2 checkable. |
| 6 | Right-sized | PASS | Two slices, about 1 day, 5-6 scenarios each. |
| 7 | Technical notes and cross-cutting | PASS | Facts check, checklist, and the DESIGN exit criterion below. |
| 8 | Dependencies resolved or tracked | PASS (tracked) | Hard: FE7's after-measurement (pushed `e85f2ebc5`, measurement not yet done). T7 before T4. T8 independent. |
| 9 | Outcome KPIs measurable | PASS | KPI table: minutes by median of >= 5 main runs, plus guardrails. |
| JTBD | `job_id` | PASS | `infrastructure-only` with `infrastructure_rationale`. |

## Wave: DISCUSS / [REF] DESIGN exit criterion

DESIGN must choose and record the mechanism that keeps the per-test guard no tighter than today on a plain CI run;
otherwise S1 turns CI red. Recommendation: scale the limits for any CI run (`isCiRun`) by the same factor 3 until T4
measures a plain CI run, so the guard stays exactly as permissive as today.

Expected plain CI factor, from the calibration (not measured): CI coverage over local plain was median 1.98, p90 2.61
(F = 3.0); local coverage over plain was median 1.35, p90 1.78. Dividing out the coverage cost gives plain CI over local
plain of 1.98 / 1.35 = 1.47 (median) and 2.61 / 1.78 = 1.47 (p90), about 1.5. Two different runs make this figure, so
it is unmeasured and T4 measures it.

The two switches stay separate and named as they are: `LIGHTHOUSE_TIME_BUDGET` (fail, warn, off) controls the per-test
guard in the test-run step; `LIGHTHOUSE_BUDGET_WARN_ONLY` controls the per-file check.

## Wave: DISCUSS / [REF] Open for the maintainer

1. **Interim mechanism**: see the DESIGN exit criterion. Alternative: `LIGHTHOUSE_TIME_BUDGET=warn` between T7 and T4,
   not recommended because it silences the per-test guard.
2. **Keep `coverlet.collector`** in the backend test project. Recommendation: keep, to mirror the frontend's opt-in
   coverage. Alternative: remove it, one dependency fewer to patch.
3. **Sonar history**: new runs show no coverage and the coverage history ends. Recommendation: accept, per D-6513-2.

## Wave: DESIGN / [REF] Decisions (propose mode, maintainer AFK, 2026-11-04)

Shape: test infrastructure and CI only. Recommended option taken for every engineering choice and recorded here. Read
from the files on `main` (`e85f2ebc5`); Vitest behaviour read from the installed `vitest` sources, not from memory.

| # | Decision | Options | Chosen | Why / trade-off |
|---|---|---|---|---|
| D1 | What the per-test guard does on a plain CI run until T4 (the exit criterion) | (a) `runKind` gets a third value `"ci"`, derived from the CI environment (`isCiRun`) when there is no `--coverage`; it scales every limit like `"coverage"` and, like it, never retires an entry. (b) A scale factor handed in per run kind, with `runKind` staying plain/coverage (CI becomes `"plain"` with factor 3). (c) `LIGHTHOUSE_TIME_BUDGET=warn` until T4. (d) Do nothing (CI becomes `"plain"`, unscaled). | **(a)**, with its own constant: the options carry `coverageFactor` and `ciFactor`, both 3 in `vitest.config.ts`. | See the trace below. (b) fuses "scale the limits" with "may retire entries" the wrong way round: today those two are one switch, `runKind`, and (b) splits them by accident, so CI starts retiring entries. (c) turns the guard off in the one place it is enforced (a red run is the only thing that stops a slow test being pushed) and is a repository variable nobody remembers to unset. (d) is the S1 failure DISCUSS named: limits 3x tighter, CI red on tests that pass today. (a) keeps CI's behaviour **bit for bit** what it is now (limits 3x, no retirement) with the smallest change, and gives T4 one number to change (`ciFactor`) without touching the coverage factor that the opt-in local command still needs. |
| D2 | Precedence of the run kinds | coverage over ci, or ci over coverage | `--coverage` in argv wins: `"coverage"`; else CI set: `"ci"`; else `"plain"` | `CI=true` with `--coverage` (the run FE7's proof used, `CI=true pnpm run sonarreport`) stays exactly as it is. Both factors are 3 today, so the order changes no verdict now; it matters at T4. Watch mode and Stryker still turn the guard off first. |
| D3 | `first test allowance` | scale it with the factor, or leave it | Leave it: added after the scaling, unscaled, in every run kind (`limitFor`: `scaled + firstTestAllowanceSeconds`) | Unchanged from today's CI (limit = 3 x B + 0.31 s). On plain CI the cold-start cost inside the first test is smaller than under coverage, so the allowance is, if anything, generous. T4 may re-measure it on the plain run. |
| D4 | `retiresEntries` / stale check | allow retirement on CI, or keep it local | `"ci"` never retires (`retiresEntries = runKind === "plain" && passed`, unchanged text) | See trace. Retirement stays the job of the quiet local `pnpm test`. |
| D5 | Per-file check (`check_budget.py`, 63 s, nine ceilings) | scale by a factor in the script; re-derive now; leave | **Leave, comment only** | The limits are in CI coverage-run seconds and go looser on a plain run (file sums fall by roughly the coverage cost, about a third by the local figure 1.35, not measured on CI). Scaling needs a measured CI figure, which only T4 has; adding an unmeasured factor to the script is a second guess where the first is already slack. A planted file over the limit still fails while its sum stays above 63 s; looser means a file can grow by up to about a third before it trips. Accepted until T4. |
| D6 | Names | keep or rename the step "Test Frontend and Sonar report" | **Keep** | The step still runs the tests and still writes the Sonar test report (`sonar-report.xml`); renaming breaks the continuity of the KPI baseline and every earlier note that names it. Backend: the two steps collapse into one named "Test Backend". |
| D7 | Opt-in command | `test:coverage` name | `"test:coverage": "cross-env TZ=Europe/Zurich vitest run --coverage"`, `sonarreport` removed | Settled in DISCUSS (AC3). `coverage.reporter` stays `["text","lcov"]`: nothing reads lcov any more, but it is harmless and a local report viewer can use it; dropping it is a free follow-up, not worth a change here. |
| D8 | Backend collector | keep or remove `coverlet.collector` | **Keep** (DISCUSS open item 2) | Mirrors the frontend's opt-in; local `dotnet test --collect:"XPlat Code Coverage"` keeps working. Not what a user sees. |
| D9 | Rigor profile (proposal) | | `standard`; mutation run on `timeBudgetOptions.ts` and `compareToAllowList.ts` last on frozen code | The guard change is small but it is the guard everyone lives under; YAML, csproj and properties are declarative (no mutation). DELIVER asks anyway. |

### Trace: what a plain CI run does to the guard (the trap in this Story)

Facts read: `compareToAllowList.ts` `limitFor` scales iff `runKind === "coverage"`; `staleReason` retires iff
`runKind === "plain" && state === "passed" && seconds < budget / 4` (0.5 s); `timeBudgetOptions.ts` gives `"coverage"`
only for a `--coverage` argument; `vitest.config.ts` sets `bail: 1` iff `CI` is set, and CI's step sets `CI: true`.
Today CI is a coverage run, so it scales and never retires. The three effects are coupled in **one** switch, `runKind`:

| Effect | Today (CI = coverage run) | After S1 if nothing else changes (CI = `"plain"`, option d) | After S1 with D1 (CI = `"ci"`) |
|---|---|---|---|
| Limit for an unlisted test | 2 x 3 (+ 0.31 first) = 6 (6.31) s | 2 (2.31) s: **3x tighter**, step red on tests that pass today | 6 (6.31) s: unchanged |
| Limit for a listed test | ceiling x 3 (+ 0.31) | ceiling x 1 (+ 0.31): tighter, red | unchanged |
| Entry stale as `file-gone` / `test-absent` | judged on a completed run (they do not look at `runKind`; a bailed run skips the whole stale check) | same | same, unchanged |
| Entry stale as `ran-far-under-budget` (passed, under 0.5 s) | **never** (coverage runs cannot retire) | **starts to fire on CI**: a rewritten test that now runs fast turns the push red until its line leaves `slowTests.ts` | never, unchanged |
| `bail: 1` and the stale check | CI sets `bail`; the first failure cancels the run, Vitest reports reason `interrupted` (`index.CoUIxZd4.js:197-200` calls the cancel hook, `cli-api.De07GUeT.js:11787` maps a cancelling run to `interrupted`), `complete` is false, **the whole stale check is skipped**; offenders that ran before the bail are still named | same | same: bail keys on `CI`, not on coverage, so it is untouched by T7 |

Findings:

1. **Option (b) and option (d) both enable retirement on CI**, because retirement is keyed on `"plain"`. That is a
   second behaviour change hiding behind the factor question: `ran-far-under-budget` could redden a push. It would
   never be a false positive (load only makes tests slower, so a test under 0.5 s on CI is under it locally too), but
   a test sitting near 0.5 s on a noisy runner would flap between stale-red and green across pushes, for a check the
   quiet local run already makes. Only D1(a) keeps CI's contract: **scaled limits, never retires**.
2. **The stale check was never "off on CI"**: `file-gone` and `test-absent` run on CI today, on a completed run, and stay. Only the
   under-budget half is plain-only. DELIVER must not describe the change as "CI now skips stale".
3. **A bailed CI run still skips the whole stale check** (reason `interrupted`). Unchanged. The proof that this holds
   for the new kind is FE7's WS10 scenario, re-run with `CI=true` and no `--coverage`.
4. **Local `CI=true pnpm test`** (previously plain, strict) becomes `"ci"` (scaled, no retirement). Intended: that is
   the CI-shaped run. The strict plain run is `pnpm test` without `CI`.
5. **Observability**: the reporter's start line prints `<runKind> run`, so the CI log says `ci run`. Its summary line
   today reads "coverage factor N", which would mislead on a `ci` run. New text, exact, per kind: `plain` prints
   `limit factor 1`, `ci` prints `limit factor 3`, `coverage` prints `limit factor 3` (the values at the config's
   constants); the rest of the line is unchanged (`Time budget: <mode> mode, budget 2 s, limit factor <n>, first-test
   allowance 0.31 s, <k> offenders`). Pinned in `timeBudgetReporter.test.ts` line 77; that test changes.
7. **HAZARD (named by review): a two-way ternary.** `compareToAllowList.ts:70` is
   `runKind === "coverage" ? base * coverageFactor : base`. Adding `"ci"` to the `RunKind` union and leaving that line
   compiles and gives `"ci"` the **unscaled** limits, which is exactly the failure D1 rejects (option d). Required
   design: one function `factorFor(kind)` with an **exhaustive switch** over `plain | ci | coverage` and a `never`
   default (so a fourth kind fails to compile), used by `limitFor` **and** by the reporter's summary line (one source
   for the number the limit uses and the number the log prints). Tests: one per kind for the factor
   (plain 1, ci `ciFactor`, coverage `coverageFactor`) and one per kind for the resulting limit, using distinct
   factor values in the fixtures (e.g. ci 2, coverage 3) so a swapped branch fails. Same discipline for retirement:
   a test per kind that only `plain` retires.
6. **T4 contract**: after T7, T4 measures a plain CI run (at least 5 main runs) and sets `ciFactor`, the per-file
   `budget_seconds` and the nine ceilings once. `coverageFactor` then only serves the local opt-in command. If T4
   wants CI to retire entries it changes D4 on purpose, with the noise bound measured.

## Wave: DESIGN / [REF] Component decomposition and Reuse Analysis

| File | Change | Reuse / contract shape |
|---|---|---|
| `Lighthouse.Frontend/src/tests/timeBudget/timeBudgetOptions.ts` | MODIFY: `runKind` third value `"ci"` (D2) | Existing pure function `resolveTimeBudgetOptions(env, argv)`; `isCiRun` reused (already used for watch). **Pure function**; asserted by the existing fake-environment tests plus rows for `CI` set / unset / `false` / empty with and without `--coverage` |
| `.../timeBudget/compareToAllowList.ts` | MODIFY: `RunKind` gains `"ci"`; `limitFor` picks `coverageFactor` or `ciFactor` by kind; `staleReason` text unchanged | Existing pure compare step. **Pure function**, no I/O; asserted by planted-list tests (existing `plainRun` / `coverageRun` fixtures, new `ciRun`): scaled limit, first-test allowance unscaled, listed ceiling scaled, never retires, `file-gone` / `test-absent` still judged |
| `.../timeBudget/compareToAllowList.ts` (same file, extra edits) | MODIFY: `factorFor(kind)` exhaustive switch with `never` default (hazard, finding 7), exported for the reporter; reword the comment at `:11` ("Multiplier applied to every limit when the run is a coverage run") and the one at `:93` ("A coverage run slows tests down, so it can keep an entry but never retire it"), which describe the old CI | As above |
| `.../timeBudget/timeBudgetOptions.ts` (same file, extra edit) | `isCiRun`'s comment (`:30`, "CI counts as set unless it is empty or \"false\"") is an **unverified** claim about Vitest's rule for the value `FALSE`/`False`: `std-env` is not resolvable from `node_modules` here, so it could not be read. DELIVER check: run `CI=FALSE vitest run` and read what the start line says, then fix the comment or lowercase handling to match | Declarative check, no design change |
| Test fixtures that break when `ciFactor` is added to `TimeBudgetOptions` (typed, required field) | `compareToAllowList.test.ts:13-16` (`plainRun`, with `coverageFactor: 2`) and `:525-526`; `timeBudgetReporter.test.ts:12-20` (`reporterWith`, `coverageFactor: 2`) and `:77` (pinned summary text); give each a `ciFactor` distinct from `coverageFactor` | EXTEND fixtures only |
| `.../timeBudget/timeBudgetReporter.ts` | MODIFY: summary line prints the effective factor (`factorFor`) | Thin impure shell (Vitest in, exit code out); its mode and factors arrive as options. Asserted by the existing reporter test (line 77 updated) |
| `Lighthouse.Frontend/vitest.config.ts` | MODIFY: `CI_FACTOR = 3` beside `COVERAGE_FACTOR`; both passed in; comment says why CI is scaled: its limits were calibrated on a coverage run and nobody has measured a plain CI one, and one added sentence: `CI=true pnpm test` is judged as a CI run (scaled, no retirement). The existing comment at `:12` ("applies to any run with coverage switched on") describes the old CI and is reworded with it. `bail` untouched | Existing config. **Bounded change**: universe = the options object of the one reporter entry; asserted by `sonar-report.xml` and `test-results.json` still written under plain `CI=true` (WS9 / WS2 of FE7) |
| `Lighthouse.Frontend/package.json` | MODIFY: `sonarreport` becomes `test:coverage`; `@vitest/coverage-v8` stays | Existing scripts. Bounded: one script line |
| `.github/actions/build-frontend/action.yml` | MODIFY: `run: pnpm test` (the script already exists; no new script) | Step name and `env: CI: true` kept. Bounded: one line |
| `Lighthouse.Frontend/sonar-project.properties` | MODIFY: delete the `sonar.javascript.lcov.reportPaths` line; keep `sonar.testExecutionReportPaths` | Declarative. Frontend has no `sonar.coverage.exclusions` to remove |
| `.github/workflows/ci_frontend.yml` | MODIFY (comment only): the budget comment says the limits are in coverage-run seconds, are looser on a plain run, and T4 recalibrates | No logic change; `check_budget.py`, `test-time-budget.json` unchanged (D5) |
| `.github/workflows/ci_backend.yml` | MODIFY: delete step "Test Backend (with Coverage)" and the `if: !inputs.run_sonar` from the former "without" step, rename it "Test Backend"; delete `sonar.cs.opencover.reportsPaths` and `sonar.coverage.exclusions` from `sonarscanner begin` | The "without" step is already the target shape. `--logger trx`, `MaxCpuCount=0`, `sonar.cs.vstest.reportsPaths`, `sonar.exclusions` stay. **Behaviour note**: with `run_sonar` false (no caller does) the test step now still runs once, as before |
| `.github/workflows/ci.yml` | MODIFY (comment): line 61 drops "with coverage" | No logic change; `run_sonar: true` stays |
| `docs/ci-learnings.md`, `.claude/commands/clean-ci.md`, `CLAUDE.md` | MODIFY at finalize (see below) | Text only |
| Unchanged | `check_budget.py`, `test-time-budget.json`, `vitest_to_csv.py`, `ci_sonar_gates.yml`, `.trx` logger, `toolchain-pins.test.mjs` and its fixtures (they pin `pnpm/action-setup` and `setup-node` and never mention the test step or coverage), `slowTests.ts`, `coverlet.collector`, `setupTests.ts`, `bail`, `LIGHTHOUSE_TIME_BUDGET` and `LIGHTHOUSE_BUDGET_WARN_ONLY` semantics | |

### Does anything else depend on coverage output? (checked)

| Candidate | Finding |
|---|---|
| Sonar scan step order (frontend: test, cache, scan; backend: begin, build, test, end) | Unchanged. The scanner reads `sonar-report.xml` (written by the config reporter `vitest-sonar-reporter`, independent of `--coverage`) and the `.trx` files. Only `coverage/lcov.info` and `coverage.opencover.xml` stop existing, and the two properties that name them go with them, so Sonar never warns about a missing report path. |
| `test-results.json` (json reporter) and the CSV | Config reporters, independent of coverage. `vitest_to_csv.py` and the per-file check read the same file. Proof: WS2/WS9 re-run under plain `CI=true`. |
| `ci_sonar_gates.yml` | Reads `report-task.txt` artifacts only; unaffected. |
| `toolchain-pins.test.mjs` | Fixtures pin the pnpm and setup-node action lines only (lines 43-71); not affected. Run it anyway as a gate. |
| Backend timings and budget | `--logger trx` stays, so `trx_to_csv.py` and the warn-only backend budget keep working; the backend budget (34 s, also in coverage-run seconds) goes looser. It cannot go red **on a breach** (`--warn-only`), but `check_budget.py:340` still returns 1 for stale allow-list entries under `--warn-only`; the conclusion holds because T7 does not change stale detection (entries come from the units that ran, not from run kind). The comment at `ci_backend.yml:199` ("Warn only until the backend budget has held...") gets one sentence, same as the frontend: the backend limits are in coverage-run seconds and looser until T4. Open wording question for DELIVER: say "coverage-run seconds" or "seconds of the last coverage run"; either is acceptable, pick one for both files. |
| Stryker (both stacks) | Uses its own coverage analysis, independent of `--coverage`; the guard is off under Stryker. No change. |
| Docs naming the removed script | `docs/ci-learnings.md:114-116`; feature workspaces under `docs/feature/` keep their history (the AC6 grep excludes them). |

### Sonar settings removed, per stack

| Stack | Remove | Keep |
|---|---|---|
| Frontend | `sonar.javascript.lcov.reportPaths=coverage/lcov.info` | `sonar.testExecutionReportPaths`, `sonar.sources`, `sonar.exclusions` (test files stay excluded), `sonar.typescript.tsconfigPath`, `sonar.javascript.node.maxspace` |
| Backend (scanner `begin` arguments) | `sonar.cs.opencover.reportsPaths`, `sonar.coverage.exclusions` | `sonar.cs.vstest.reportsPaths`, `sonar.exclusions`, `sonar.sources`, `sonar.language` |

`sonar.coverage.exclusions` goes because nothing is measured; the broader `sonar.exclusions` already carries the
generated and non-product paths, so the general analysis is unchanged.

### Gate check (Earned Trust: the one claim the repo cannot show)

D-6513-2 (gate does not check coverage) is given. DELIVER step 0, read-only and before the first push: record the
gate conditions of both projects (`LetPeopleWork_Lighthouse`, `LetPeopleWork_Lighthouse_Frontend`) from the public
SonarCloud API (`/api/qualitygates/project_status`; the CLI is not installed) in the workspace. If either lists a
coverage condition (`new_coverage`, `coverage`), stop: that contradicts the decision and is the maintainer's. The first
main run per stack then confirms it (KPI guardrail: `sonar-gates` passes).

## Wave: DESIGN / [REF] Slices (unchanged)

S1 Frontend and S2 Backend stay as in DISCUSS, in either order, each after FE7's after-measurement. S1 AC4 is made
precise by D1: on a plain CI run the limits equal today's (3 x budget and 3 x ceiling, +0.31 s for a file's first
test); the planted 9 s test is red and a 5 s test is green; a planted fast listed test does not turn the step red on CI
(never retires) and does locally under plain `pnpm test`. S1 adds to the scenarios: a `CI=true` run with a planted
failing test names its offenders and skips the stale check (bail, finding 3). S1 AC5's grep stays.

**Push cost (carry-forward for DEVOPS):** any change to `.github/workflows/ci*.yml` sets `github_changes` and turns the
backend, E2E and live connector suites on (`ci_changes.yml:193-199`). S2 edits `ci_backend.yml` and `ci.yml`; S1 edits
`ci_frontend.yml` (comment). Each push therefore runs the full suites with live connectors (shared Linear key). Commit
the slices separately; DEVOPS may push S1 and S2 together to pay once.

## Wave: DESIGN / [REF] Docs to change at finalize (what /clean-ci and the ledger must say)

- `docs/ci-learnings.md:114-116`: the rule says the loop kills the worker under `--coverage` and, live, causes React
  #185, so the loop is **not** coverage-only; that stays. Only the command changes: "before pushing, run touched test
  files with `CI=true pnpm test <files>`" (plain CI-shaped run; `pnpm test:coverage` if the coverage worker kill is what
  is being checked). Add no new rule; if the plain-CI factor needs a sentence, it belongs in the `vitest.config.ts`
  comment, where the number lives.
- `.claude/commands/clean-ci.md:97,119,138,155-157`: coverage-on-new-code is no longer a failure source. Lines 97 and
  119 lose the coverage item; the `uncovered_lines,coverage,new_coverage` recipe (155-157) goes; lines 138 keeps the
  generic "conditions[] names each metric" and drops the coverage example. If a coverage condition ever returns,
  the recipe returns with it.
- `CLAUDE.md` quality gates: one line naming `pnpm test:coverage` (frontend) and `dotnet test --collect:"XPlat Code
  Coverage"` (backend) as opt-in checks for behaviour-free rewrites (D-6513-3/4). Not in CI.
- `ARCHITECTURE.md` / ADR-270 / context map: none (see checklist).

## Wave: DESIGN / [REF] Rejected alternatives

- **`LIGHTHOUSE_TIME_BUDGET=warn` until T4** (c): silences the guard in its only enforcing place; a repository variable
  nobody unsets; a slow test slips in for the weeks T4 takes.
- **Do nothing / plain CI unscaled** (d): limits 3x tighter, red on tests that pass today, retirement newly firing.
- **Factor handed in per run kind with CI staying `"plain"`** (b): fuses the wrong things; retirement starts on CI (trace).
- **Measure a plain CI run first, then set a tight factor in T7**: needs a coverage-free CI run before T7 lands, which
  is T7 itself; T4 follows T7 by design (D-6513-5).
- **Scale the per-file budget now** (D5): unmeasured second guess on a check that is only looser, not red.
- **Drop the stale check on CI altogether**: it also guards `file-gone` and `test-absent`, which are not noisy.
- **Remove `coverlet.collector`**: no user-visible gain; loses the symmetric opt-in.
- **Rename the step**: breaks baseline continuity (D6).

## Wave: DESIGN / [REF] Rollback

- **S1**: `git revert` of the S1 commits restores `sonarreport`, the lcov path and the old CI command in one step. The
  guard change reverts with it (a CI coverage run then maps to `"coverage"` again). Sonar coverage figure returns on the
  next main run. The per-file budget and ledger are untouched, so nothing else needs undoing.
- **S2**: revert restores both steps and the two scanner arguments; same effect for the backend.
- **Partial**: S1 and S2 are independent; either can be reverted alone. If the gate turns out to hold a coverage
  condition, revert the whole slice of the failing stack (without a report the condition fails, so keeping the plain
  run alone does not help) and tell the maintainer.
- **A flaky guard after S1** (a plain-CI test over a scaled limit): raise `ciFactor` (one constant) rather than
  switching the guard off; `LIGHTHOUSE_TIME_BUDGET=warn` stays the maintainer's emergency switch.

## Wave: DESIGN / [REF] Cross-cutting checklist (explicit answers)

| Item | Answer |
|---|---|
| Ports-and-adapters | Not touched. Test infrastructure and CI only; no domain, application service, port or adapter of the product. The shape inside the test tooling is the FE7 one: a pure compare step and options function, and a thin Vitest reporter shell. |
| Usage-data event | DEVOPS question, not asked here. Provisional answer stays N/A (CI tooling nobody uses in the product). |
| EF migrations | N/A, because no entity or table changes. |
| Context map (ADR-270) | N/A, because it is Platform-level CI and test tooling: no bounded context touched, no term, concept, edge or domain event. ADR-270 and `ARCHITECTURE.md` §4 need no change. |
| Loading and failure looks | N/A, because no screen, chart or widget fetches data. |
| UI surface (ADR-271) / sketch | N/A, because nothing a user sees changes. |
| Visualization skill | N/A, no chart or metric tile. |
| RBAC, Clients versioning, website, terminology, release notes | N/A as in DISCUSS (nothing user-visible; release notes are written at /release). |
| Improvement backlog check | N/A, because this item sits in the Continuous Improvement Epic. |
| New ADR | None: no architectural decision beyond FE7's guard design; the CI factor is recorded in the `vitest.config.ts` comment and here. |
| Mutation testing | StrykerJS on `timeBudgetOptions.ts` and `compareToAllowList.ts` last, on frozen code, kill rate >= 80%. Workflow YAML, csproj, properties, package.json: N/A, declarative. |
| Enforcement tooling | The guard itself is the enforcement; the planted-test scenarios are its proof. No architecture rule added or removed. |
| External integrations | SonarCloud only (already integrated, scan inputs shrink). Contract test recommendation: none; the gate-conditions read in "Gate check" is the substitute. |

## Wave: DESIGN / [REF] Open for the maintainer

1. **Sonar quality gate has no coverage condition (D-6513-2)** is taken as given. DELIVER step 0 records the gate
   conditions read-only; if one exists, T7 stops. Yours to confirm.
2. **`ciFactor` stays 3 until T4.** Recommendation taken. The alternative (a tighter measured factor now) needs a plain
   CI run that does not exist before T7.
3. **Per-file and backend budgets go looser until T4** (about a third by the local coverage figure, unmeasured on CI).
   Accepted; the alternative is an unmeasured factor in `check_budget.py`.
4. **Push cost**: workflow-file edits run the full suites with live connectors (Linear key shared with CI). Whether to
   push S1 and S2 together is DEVOPS's call and yours.
5. Carried over from DISCUSS: keep `coverlet.collector` (taken); Sonar coverage history ends (accepted).
6. **Spare change not made**: dropping `lcov` from `coverage.reporter`; say if you want it.

## Wave: DESIGN / [REF] Architecture summary for the maintainer

**Shape, in hexagonal terms.** There is no product hexagon here; this is test and CI tooling. The same vocabulary
maps onto it like this:

```
 push to main
   |  (driving adapter)
   v
 CI workflow YAML --- runs ---> pnpm test / dotnet test        [MODIFIED: plain, no coverage]
   |                                  |
   |                      Vitest reporters (driven adapters)
   |                        sonar-report.xml, test-results.json  [UNCHANGED]
   |                        TimeBudgetReporter (thin shell)      [MODIFIED: effective factor in the log]
   |                                  |
   |                       application: resolveTimeBudgetOptions  [MODIFIED: 3rd run kind "ci"]
   |                       domain: compareToAllowList (pure)      [MODIFIED: ciFactor; retirement rule unchanged]
   v
 SonarCloud scan (driven, external) --- reads .trx / sonar-report.xml ; lcov + opencover inputs [REMOVED]
 per-file budget (check_budget.py)                              [UNCHANGED, looser until T4]
```

- Driving: the push and the workflow files (modified: test command, one backend step, two scanner arguments).
- Application: `resolveTimeBudgetOptions` (modified); domain: `compareToAllowList` (modified, pure); driven: the Vitest
  reporter shell (log line only), the Sonar scan (two inputs removed), the report files (unchanged).
- New external calls: none. Unchanged: every test, the allow-list, bail, the Sonar test-result inputs, the quality gate.

**Patterns weighed.**
- *Separate the two things one switch decides* (scale the limits, may retire entries): chosen as a third run kind with
  its own factor; rejected a bare factor argument because it silently moves retirement onto CI.
- *Single responsibility / open for extension*: a run kind plus a factor per kind lets T4 recalibrate CI without
  touching the coverage factor; rejected a full rename of `coverageFactor` (churn for no behaviour).
- *Pure core, thin shell* (Functional Core / Imperative Shell, as FE7): kept; all verdicts stay in the pure compare step,
  asserted by planted lists.
- *Fail loud, not silent*: rejected `LIGHTHOUSE_TIME_BUDGET=warn` as an interim; it turns the guard off.
- *Do not guess a number twice*: rejected scaling the per-file budget now; T4 measures once.
- *Revert-ability*: two independent slices, each a plain revert.
- Not applicable here: domain events, outbox, unit of work, value objects, optimistic concurrency, JSON column versus
  child table (no product state is touched).
