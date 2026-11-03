# Feature: story-6457-fe7-fast-frontend-tests

Story #6457 · FE7 Frontend tests run in milliseconds: fix the slow ones, keep them fast · part of Epic #6264
([workspace](../epic-6264-modular-monolith/feature-delta.md)) ·
brief: [`slice-40-fe7-fast-frontend-tests.md`](../epic-6264-modular-monolith/slices/slice-40-fe7-fast-frontend-tests.md)

DISCUSS: covered at Epic level (D-6264-3, D-6264-4); waves start at DESIGN. This file holds PICK-UP, the measurement and
DESIGN (propose mode, maintainer AFK: the recommended option was taken for every engineering choice and is recorded).
Raw material: `measurement/baseline-2026-11-02/`: `runs.txt` (sampler lines and Vitest summary lines for both runs; the
figures below point at it), `profile-summary.txt`, `fe-base-1.log.gz`, `fe-base-2.log.gz`, `vitest-run2.json.gz`. The JSON of
run 1 was overwritten by run 2, so per-file and per-test figures come from run 2 only; run 1 contributes wall, memory and the
Vitest split.

## Wave: PICK-UP / [REF] Inherited

From the Epic's `feature-delta.md`, by heading. Linked, not copied:

- [REF] How to pick up a Story: the recurring per-Story checklist (copied below to tick).
- [REF] Stories → Frontend track → FE7, and its "Update 2026-10-31" bullet (FE7 absorbs #6302's two frontend follow-ups).
- [REF] Story Map → Pick-up groups → G0b (T3 → T1 → T2, FE7 alongside, T4 last); graph lines `FE1 --> FE7`,
  `FE5 <-> FE7` (soft, either order) and `T2, FE7 --> T4 (soft)`, `FE7 <-> T4 share the frontend budget`.
  FE1 is done (`190c68193`). T3 is done: the local worker cap that the measurement below ran under.
- [REF] Outcome KPIs + guardrails: KPI 14 (frontend test time: no test over the budget set at FE7 DESIGN, apart from a
  shrinking named allow-list) and KPI 16 (CI test step minutes on main). Guardrails: all tests green, mutation score per
  touched area, pixels unchanged (`@screenshot`), CI wall time not worse, zero new Sonar issues, Biome zero warnings.
- [REF] Definition of Done (Epic), frontend bullet. [REF] Wave Decisions: D-6264-3/4/5 (catalogue, carpaccio at DESIGN,
  composition gate waived), D-6264-8 (TanStack Query migration stays out).
- **T4 hand-off:** FE7 owns the whole frontend budget (D1). The T4 brief
  (`slices/slice-44-t4-test-budgets-fail-the-build.md`) must lose its frontend half (`ci_frontend.yml`, the frontend ceilings)
  at T4 pick-up and keep the backend.
- [REF] Cross-Cutting Impact Checklist (answered below). [REF] Improvement log: read at pick-up. Lessons that bind this
  Story: FE1 "list unit is the file pair, `pnpm test` not slower, alternating runs"; S11 "reproduce the CI sequence when
  claiming behaviour-free"; the T-track entries as they land.
- ADR-268 and ADR-269 (the ratchet convention; the frontend list lives in an enforcement test). Re-read in D2/D3.
- [REF] Story #6302 (`../story-6302-ci-test-time/feature-delta.md`): the first round, its frontend follow-ups folded into
  this Story, and its `Scripts/test-timings/` instrumentation, which this Story extends (see Reuse Analysis).
- [REF] `docs/ci-learnings.md` frontend, "Tests (Vitest, React Testing Library)": the cheap-query rule, "Test timed out in
  5000ms in files you did not touch" and the busy-button rule bind every rewrite below.
- Maintainer decisions for this item: the AFK entry for the test track (rigor `standard`, refactor and review per slice,
  mutation once at the end, nothing pushed) as recorded in the T1 and T2 workspaces. DELIVER still opens by proposing a
  profile per slice (CLAUDE.md).

## Wave: PICK-UP / [REF] DoR re-run

| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | 578 test files, 8,970 tests, 244-253 s wall on a quiet 12-core machine at the T3 default cap (6 workers); the CI frontend build+test step went from about 11 to 16-21 min in October (brief). One test in `ReportsPage.removal.test.tsx` flaked on a 5 s timeout. |
| 2 Persona | PASS | `lighthouse-maintainer` waiting on `pnpm test`/CI; parallel local sessions; the CI build. |
| 3 Domain examples | PASS | `ReportsPage.removal.test.tsx` "Select all picks every row, on every page" (1.97 s today); `WorkItemsDialog.contexts.test.tsx` (108 tests, 19.0 s); `ModifyTeamSettings.refinement.test.tsx` (600 ms real sleep); `RefinementView.paging.test.tsx` "Jonas pages to GR-0131" (2.48 s, the slowest test). |
| 4 UAT G/W/T | DEFERRED | Epic catalogue rule: written at each Story's DISTILL. |
| 5 AC derived from UAT | PASS | The brief's five AC. |
| 6 Right-sized | PASS | Size L; split into 12 slices of at most one day below. |
| 7 Technical notes + cross-cutting | PASS | Below. |
| 8 Dependencies | PASS | FE1 done (hard-ish, met). FE5 soft: see the slicing note for `BaseMetricsView`. T4 soft-depends on this Story. |
| 9 Outcome measurable | PASS | Suite wall time (alternating runs, same machine), summed seconds of the touched files, count of allow-listed tests, Vitest JSON per run. |

Slice composition: waived at Epic level (D-6264-5). JTBD: infrastructure-only.

## Wave: PICK-UP / [REF] Baseline re-count (2026-11-02, `main` at `208926f7b`)

Measured by the orchestrator, 12-core quiet machine, default local cap (half the cores = 6 workers), `pnpm test`, two
runs; sampler and Vitest lines in `measurement/baseline-2026-11-02/runs.txt` (sampler wall 267 s and 246 s, which includes
start-up; Vitest's own duration 252.50 s and 244.32 s; peak RSS 4,620 and 4,819 MB). Brief figures against today:

| Brief says | Today | Note |
|---|---|---|
| Suite 438 s on the Stryker-bump CI run (PR #1977), 71 % in tests | **252.5 s and 244.3 s** wall locally; Vitest's own split: tests 48-49 %, import 23-24 %, environment 18-19 %, transform 4-7 %, setup 4 % | Not the same machine, worker count or run kind, so the two are not comparable. The CI figure for today comes from the `test-timings-frontend` artifact of the last main runs, read in slice 1. The two local runs differ by 3.3 %: the noise floor for the A/B rule in D5. |
| `ReportsPage.removal.test.tsx` 48 s for 22 tests | **16.3 s for 24 tests** (0.68 s per test) | Faster, and two tests added. Still the 4th slowest file. |
| "Select all picks every row, on every page" timed out at 6.2 s against the 5 s default | **1.97 s**, 5th slowest test of 8,970 | Not a flake any more on a quiet machine, but 2.5x from the timeout; on a loaded runner it can come back. |
| Test files 403 → 577 in October | **578** | One more since the brief. 8,970 tests. |
| `ModifyTeamSettings.refinement.test.tsx` still sleeps 600 ms of real time | **Still true.** `AUTOSAVE_DELAY_AND_MARGIN_MS = 600` (line 175), awaited by `pastTheAutosaveDelay` (line 177), used once (line 298) | Cheap fix in one test; slice 4. |
| (not in the brief) other real sleeps in tests | `setTimeout` waits of 500 ms x5 (`ModifyProjectSettings.test.tsx` 590, 617, 722; `ModifyTeamSettings.test.tsx` 436, 471), 1,300 ms (`useLicenseRestrictions.status.test.tsx` 305), 350 ms (`BulkReportWizard.openings.test.tsx` 55), 300 ms (`BulkReportWizard.test.tsx` 919), 200 ms (`ProgressIndicator.test.tsx` 71), 50 ms x3 (`TeamDetail`, `PortfolioDetail`) | About 5.5 s of pure waiting in total across the suite: real, but small next to 691 s. They sit in slow files, so they go with those slices. |
| Local run uses 11 workers on 12 cores; `maxWorkers` unset | Half the cores locally (`resolveTestWorkers`, T3); CI untouched | The baseline below is at 6 workers. Memory: **peak 4,620 and 4,819 MB** (sampler, whole run). |
| (not in the brief) a per-file frontend budget already exists | `Scripts/test-timings/test-time-budget.json`: 63 s per file, nine allow-listed files with CI-second ceilings (77-182 s), checked by `check_budget.py --stack frontend --warn-only` in `ci_frontend.yml` | Built by #6302, warn-only until T4. See D1 and "Brief and Epic disagreements". The ceilings are CI seconds and sit up to 3-9 times above the local figures for the same files (`WorkItemsDialog.contexts`: ceiling 182 s CI, 19.0 s here). That is an **upper bound, not a measurement**: a ceiling carries margin, and CI runs the coverage command. Slice 1 measures the real CI/local ratio. |

## Wave: PICK-UP / [REF] Cross-cutting re-check

No silent N/A; each answer carries its reason.

- RBAC: N/A, because no authorization rule changes; only tests and test configuration. Production code may gain extracted
  pure functions/hooks, which keep calling `useRbac()` exactly as before. `useRbac()` stays the only gate.
- Lighthouse-Clients (CLI/MCP): N/A, because no route, DTO or serialized response changes.
- Website: N/A, because nothing user-visible changes.
- Usage-data event: N/A, because behaviour-free test infrastructure; nothing a user does is counted. DEVOPS confirms.
- UI sketch and the loading/failure look: N/A, because no screen, chart or widget is added or changed; tests that pin the
  loading and failure looks of existing views (for example `BaseMetricsView.loading.test.tsx`) must keep pinning them.
- Terminology: N/A, because no user-facing words change. Rewritten tests keep asserting the seeded default terms.
- Context map (ADR-270) and ARCHITECTURE.md: see "Context-map placement" below. No change expected; ARCHITECTURE.md line
  372 names the test stack and stays true unless the environment changes (slice 3 would then edit that line).
- Data display / visualization skill: N/A, because no data is shown to a user; no chart, widget, tile or report text changes.
- Docs/screenshots/demo data/release notes: N/A, because no user-facing behaviour; no release notes at finalize (rule).

## Wave: PICK-UP / Checklist

- [ ] Behaviour-free, as FE7 reads it: production code behaves identically; `@screenshot` and Playwright untouched; every
  assertion a removed or shrunk test made is still made somewhere (a moved-assertions list per slice, D7).
- [ ] Ratchet in the same change: the time-budget guard lands in slice 1; every later slice deletes the allow-list entries
  it fixes in that same change; the list never grows.
- [ ] Moves separate from edits: an extraction of a function or hook out of a component is a `refactor(...)` commit with
  no test change; the test rewrite is the next commit; a pure move of a shared test helper is its own commit.
- [x] Re-count before designing (above).
- [ ] Mutation: Stryker (frontend) once, at the end, on frozen code, over the production files touched by extractions
  (D9); kill rate of the touched area not lower; at least 80 % on new code.
- [ ] Coverage of the touched area (line and branch) not lower, before against after, per slice (D7).
- [ ] Architecture docs in the same change: ADR-270 and ARCHITECTURE.md §4: N/A (no context, concept or edge changes);
  ARCHITECTURE.md line 372 only if the test environment changes; `docs/ci-learnings.md` frontend Tests edited in
  slice 1 (new rule: where to test) and as entries become automated.
- [ ] Payoff proved: suite wall time before and after, same machine, alternating runs, median of at least 5; summed
  seconds of the touched files; allow-list length; CI step median after push (lead).
- [ ] No user-facing release notes.
- [x] Cross-cutting re-check answered (above).
- [ ] Epic per-Story checklist items: ratchet (done above); behaviour-free (above); mutation last (above); moves separate
  (above); ARCHITECTURE.md (above); "one context at a time" N/A, because no context is packaged here; terminology N/A
  (above); cross-cutting re-check (above).
- [ ] Project gates at the end of each slice's refactor pass: `pnpm test`, `pnpm build` (Biome + `tsc`) clean; touched
  test files also run once with `CI=true pnpm run sonarreport` (ci-learnings rule for fresh values in hooks).
- [ ] DISTILL (if run for this Story): no nWave scenario tags in code; tell the acceptance-designer and the reviewers.

## Wave: DESIGN / [REF] Context-map placement

Per CLAUDE.md and ADR-270, answered explicitly:

- Contexts touched: **none**. This is test infrastructure plus tests of existing code. Test files sit next to the code they
  test (several contexts' pages and components are visited in turn: Reports, Refinement, Org set-up forms, Tracker
  integration forms, Flow metrics views), but no production concept, term or edge is added or changed.
- New terms: none in the product glossary. Test-side words (time budget, slow-test allow-list) are not domain terms.
- Domain events published or reacted to: none.
- UI surface (ADR-271): none; nothing a user sees changes.
- ADR-270 / ARCHITECTURE.md §4: no update owed. The "nw-finalize" check should read "no concept added".

## Wave: DESIGN / [REF] Measurement (2026-11-02, two `pnpm test` runs, quiet machine, 6 workers)

Source: `measurement/baseline-2026-11-02/profile-summary.txt` and `vitest-run2.json`. Nothing here is estimated unless it
says "derived" or "estimate".

### Headline

| Figure | Value |
|---|---|
| Files / tests | 578 / 8,970 |
| Wall | 252.5 s and 244.3 s |
| Summed test time (all tests, all workers) | 691 s |
| Peak memory (sampler) | 4,620 MB and 4,819 MB |
| Vitest's split of worker time | tests 48-49 %, import 23-24 %, environment 18-19 %, transform 4-7 %, setup 4 % |
| Share of summed test time in the slowest files | top 10: 22 % (155 s); top 20: 37 % (253 s); top 50: 59 % (406 s) |
| Tests over 0.1 s / 0.5 s / 1 s / 2 s / 5 s | 1,893 (546 s) / 252 (197 s) / 50 (65 s) / 4 (9 s) / 0 |

### What dominates, said plainly

1. **Per-test work is about half of all worker time; per-file fixed overhead (import + environment + transform + setup,
   about 50-52 %) is the other half.** Derived: 6 workers x about 250 s wall is about 1,500 worker-seconds, of which tests
   are 691 s (46-49 %). The remaining roughly 800 worker-seconds spread over 578 files is about 1.4 s of fixed cost per
   file: import about 0.6 s, environment about 0.5 s, transform and setup the rest. Every file pays it, a 3-test file
   and a 129-test file alike.
2. **The brief's hypothesis is only partly confirmed.** It said most wall time sits in a small number of page-level
   files and that rewrites would cut it by an order of magnitude, "disproved if the profile shows the cost is mostly
   fixed per-file overhead". The profile shows both: the test half is concentrated (top 50 files hold 59 % of test time),
   but the other half is fixed overhead that no rewrite touches. The disproof condition is not met outright, but its
   spirit is: **the scope must cover both test rewrites and configuration**, as the brief's re-scope clause says.
3. **The test half is a long tail, not a few spikes.** Only 50 tests exceed 1 s (65 s, 9 % of test time); 1,893 tests
   exceed 0.1 s and make 79 % of it (546 of 691 s). The mean test is 77 ms. A per-test budget of 2 s guards almost
   nothing today (four tests); the saving lies in making the many 0.2-1 s page-level tests cheaper.
4. **Reachable by rewrites alone, estimate (not a promise):** the eleven hotspot families below hold about 288 summed
   seconds of the 691. If a rewrite saves half of a family's seconds, that is about 144 s summed, which at six workers is
   about 24 s of the 250 s wall, or **about 10 %**. An order of magnitude from rewrites is not reachable. The larger
   lever is configuration (about 800 worker-seconds of fixed cost), but its size is not measured yet: slice 2 produces it.

### Slowest files, and why (read: the first four, plus the pattern of the rest)

| File | s | Tests | s per test | What was read |
|---|---|---|---|---|
| `ModifyProjectSettings.test.tsx` | 19.7 | 44 | 0.45 | Renders the whole settings form per test; `waitFor` after nearly every action; three real 500 ms sleeps (lines 590, 617, 722), 1.5 s of waiting alone |
| `WorkItemsDialog.contexts.test.tsx` | 19.0 | 108 | 0.18 | `it.each` over contexts; each renders the dialog with the MUI DataGrid and opens the Manage columns panel (the budget file says about 70 openings, each paying Popper style lookups). `workItemsDialogContexts.test.ts` already tests the context logic as a function |
| `ModifyTeamSettings.test.tsx` | 17.7 | 33 | 0.54 | Whole form render per test; real 500 ms sleeps at lines 436 and 471 |
| `ReportsPage.removal.test.tsx` | 16.3 | 24 | 0.68 | `renderTheReportsPage` mounts the page with React Query, router, RBAC, licensing and terminology providers and the DataGrid; each test then picks rows with `user.click` on checkboxes and `findByRole("dialog")`. The pick limit (50) and the "count of picked" are plain logic. "Select all" renders 101 rows (1.97 s) to check a count |
| (next) `ModifyTeamSettings.refinementNeed` / `.readiness` | 15.4 / 14.4 | 52 / 47 | 0.30 | Same form; the budget file says every save check waits out the real 300 ms autosave debounce (not a fake clock) |
| (next) `RefinementView.paging` | 13.4 | 11 | 1.2 | 100-143 rows through the real DataGrid, re-rendered on each page change; holds the slowest test (2.48 s) |
| (next) `BaseMetricsView.loading` | 11.9 | 129 | 0.09 | The whole metrics dashboard per test; small per test, large by count |

The sampled cause families, in order of how much of the top-25 files they explain: (a) whole-page or whole-form render per
test, with the MUI DataGrid and dialogs in jsdom; (b) `waitFor` and `findBy*` polling after each action (polling has a
50 ms interval by default); (c) real timers: debounce waits and the few `setTimeout` sleeps; (d) `user-event` clicks and
typing, already with `delay: null` in `setupTests.ts`. **Not measured yet:** the share of each cause (a CPU profile of two
files is slice 2's second job) and the per-render cost of the provider stack the page kits build.

### What is not measured, and which slice measures it

| Missing number | Produced by |
|---|---|
| Per-file import, environment, transform cost split by file (Vitest prints only totals) | slice 2, from Vitest's per-module timing output and a node profile of 2 files |
| CI per-test durations (the machine the budget must hold on) | slice 1, from the `test-timings-frontend` artifacts of at least 5 main runs (needs `gh run download`, done by the orchestrator) |
| Effect of environment, pool, inlining options | slice 2, alternating A/B runs |
| Coverage and Stryker baseline of the touched production files | each slice records coverage before; Stryker once at the end (D9) |

## Wave: DESIGN / [REF] Decisions (propose mode, maintainer AFK, 2026-11-02)

Shape of the work: test infrastructure only. Recommended option taken for every engineering choice. Each A/B decision is
made by a rule written down now, so the result cannot be argued after the fact.

| # | Decision | Options | Chosen (recommended) | Why / trade-off |
|---|---|---|---|---|
| D1 | Who owns the frontend budget (FE7 against T4) | (a) FE7 owns the **whole** frontend budget: the new per-test ratchet **and** the existing per-file budget (`test-time-budget.json`, `check_budget.py --stack frontend`), made failing. (b) The per-test ratchet replaces the per-file budget. (c) FE7 owns only the per-test ratchet; T4 flips the per-file one. | (a) | The brief and the Epic say whichever lands first owns the frontend budget; FE7 lands first. Both halves stay because they guard different costs. A per-test duration does not include import, module-level code or `beforeAll` (they run outside any test), which is the other half of worker time; the per-file budget sees them, and cannot name the slow test in a 129-test file, which the per-test one does. (b) leaves the fixed-overhead half unguarded; (c) splits one budget across two Stories, against the brief. **How the per-file half becomes failing:** in slice 1, `--warn-only` is removed from the **frontend** call in `ci_frontend.yml` once `check_budget.py` passes on the CSVs of at least 5 main runs; if it does not, the nine ceilings are re-derived from those runs (highest value seen plus the noise margin slice 1 measures) in the same change. The backend call keeps `--warn-only` (T4). **T4 consequence:** T4 drops its frontend half and keeps the backend. The T4 brief (`slices/slice-44-t4-test-budgets-fail-the-build.md`, lines 12-30: it names `ci_frontend.yml` and says "whichever lands first owns") needs its frontend half removed at T4 pick-up; recorded here and in the hand-off. |
| D2 | How the budget is enforced, and where | (a) A **custom Vitest reporter** that reads each finished test's duration and fails the run naming the test, around a pure compare step. (b) A setup-file `afterEach` hook. (c) `slowTestThreshold` or a lowered `testTimeout`. (d) A post-run script over `test-results.json`. | (a) | The reporter sees the duration Vitest measured (hooks included), works for any invocation (full, filtered, IDE) and keeps the logic in one place. (b) times only part of a test; (c) `slowTestThreshold` only colours output, `testTimeout` aborts a test and cannot take a name list (it stays at 5 s as the backstop); (d) runs only through a script, not an IDE or filtered run. **Which CI step enforces:** CI's only Vitest run is the "Test Frontend and Sonar report" step of `.github/actions/build-frontend/action.yml`, which runs `pnpm run sonarreport` = `vitest run --coverage` with `CI=true`. So the guard is **on in `pnpm test` and in that coverage run**; it is **off only under Stryker and in watch mode**. (An earlier idea, off under coverage, would never have enforced anywhere in CI.) The per-file half is enforced by the "Check test-time budget" step in `ci_frontend.yml`, reading the CSV that the coverage run's JSON reporter wrote. **Options, not environment:** the reporter takes its mode as plain options (`enabled`, `runKind` plain or coverage, budget, factors); `vitest.config.ts` computes them with a small pure function of (environment, argv), unit-tested the way `resolveTestWorkers` is. **Slice-1 spike questions, answered before any other work:** (1) the Vitest 5 hook that delivers per-test duration and the end-of-run set (`onTestCaseResult` and `onTestRunEnd` are the candidates) and how the failing exit code is set (`process.exitCode` or an error handed back); (2) whether the `--reporter=default` that `pnpm test` passes **replaces** the reporters in the config (if so, the guard, and the Sonar and JSON reporters, are not loaded by `pnpm test`; the fix is to drop the flag from the script, or add the guard on the command line); (3) the marker Stryker's worker sets (candidate `STRYKER_MUTATOR_WORKER`, **unverified**); (4) `bail: 1` is on when `CI` is set, so a failing run is incomplete: the stale check (D3) is skipped for a bailed run. **Proof demanded of slice 1:** a planted slow test fails the run through `pnpm test` **and** through `CI=true pnpm run sonarreport`, and does not fail under the Stryker marker or in watch. |
| D3 | Where the allow-list lives, its key, how it shrinks | (a) A TypeScript data module beside the reporter. (b) JSON in `Scripts/test-timings/`. (c) Inline in an enforcement test like `ALLOWED_LEAKS`. | (a): `Lighthouse.Frontend/src/tests/timeBudget/slowTests.ts`; key = `relative file path :: full test name`; each entry has a one-line reason and a **ceiling** (quiet-local seconds) | The reporter runs in the frontend project and imports it. Every `it.each` row is its own test with its own name, listed and checked individually. **Failure directions:** (1) an unlisted test over the threshold fails, named. (2) A listed test over its own ceiling fails, named (a slow test may not get slower). (3) Stale entry, defined **against what ran, not against the source** (recommended; a source scan cannot expand `it.each` titles and template strings): for an entry whose file was part of this run, the test must have run, and not in under a quarter of the budget; an entry whose file no longer exists on disk is stale; entries for files outside a filtered run are ignored; a bailed or incomplete run skips the check. "Under a quarter" is safe because load only makes tests slower, never faster. The alternative, running the stale check only on full runs, was rejected: it needs the reporter to know a run was unfiltered, which Vitest does not tell it reliably. **How a reviewer sees growth:** an added entry is a line in `slowTests.ts`, with its reason and ceiling, in the PR diff; nothing else can add one. **Departure from the ADRs, recorded:** ADR-268 says "file or type" and fails on a stale entry; here the key is a test, the stale rule is the one above, and an entry has a ceiling. ADR-269 puts the list in an enforcement test over a tool's output; here a reporter reads the run itself. Slice 1 writes this as a status note on ADR-268 and ADR-269 (or one short new ADR, next free number, if the note grows past a paragraph). **Filtered and IDE runs (re-check):** a file "was part of this run" when Vitest collected it; a listed test that Vitest reports as skipped (by `-t`, `.only`, or an IDE single-test run) is ignored, not stale; only a listed test that was collected and ran, or is absent from a collected file, is judged. |
| D4 | The budget model and its value | 1 s / 2 s / 3 s / 5 s | **Rule fixed now, value derived in slice 1; provisionally 2 s.** B is the budget in seconds on a quiet machine under `pnpm test`. The enforced threshold in a run is `T = B x F (+ A for the first test in its file)`. F = 1 for local `pnpm test`; for the CI coverage run F is the 90th percentile of the per-test ratio (CI coverage-run duration over local plain-run duration) over tests that took at least 0.1 s locally, rounded up to the next 0.5. A is the measured extra cost of the first test of a file (cold module and first render), taken as the 90th percentile over files. Slice 1 measures: the ratio of local `--coverage` to local plain on the same machine (separates the coverage effect from the machine effect), the CI ratio from at least 5 CI artifacts, and A. **Value of B:** the smallest of {1, 2, 3} s for which every test over T in the calibration samples (the quiet local run, 3 oversubscribed local runs with workers = cores, the 5 CI artifacts) sits in a file that slices 4-11 name, so each entry has a slice that retires it; entries outside those families carry their own reason. (The earlier cap of 60 entries is dropped: it had no reason; the rule above ties the list's length to the work planned.) 5 s is excluded (it is the timeout). **Flake bound:** T is raised until the calibration samples show **zero** unlisted tests over it; a residual single-sample false failure on an unmeasured machine is possible, and is answered by a re-run, not by a retry in the reporter; the raised value and the observed spread are written into the guard's comment. **If the CI artifacts are missing or fewer than 5, slice 1 stops and asks for them; there is no fallback value.** | Local data: 4 tests over 2 s, 50 over 1 s, 252 over 0.5 s. The old "CI is 3-9x slower" was an **upper bound read off the budget file's ceilings** (they carry margin and are CI coverage-run seconds), not a measurement; slice 1 measures the ratio. CI artifacts come from the coverage run, so they calibrate the CI run kind directly. The value in this row is replaced by the measured one when slice 1 lands. **Listed tests use the same scaling (re-check):** a listed test's ceiling is in quiet-local seconds and is enforced as `ceiling x F (+ A if it is the first test in its file)`, exactly like `T` for unlisted tests; slice 1's proof covers a listed test over its scaled ceiling. **Value recorded at DESIGN for KPI 14: B = 2 s.** Slice 1 may move it to 1 s or 3 s only by the rule above, recorded as a dated re-check note here and in the Epic KPI row. |
| D5 | Environment and pool options | (a) `projects` split: a `node`-environment project for tests that touch no DOM (candidates: at most the 184 `.test.ts` files, an **upper bound**: some use `renderHook`, `localStorage` or the DOM). (b) Stop inlining MUI (`server.deps.inline`), or let `deps.optimizer` pre-bundle it. (c) happy-dom in place of jsdom for the DOM project (new dependency, MIT). (d) `isolate: false` for the node project only. (e) `pool: forks` or `vmThreads`. | **All five are measured in slice 2, none adopted before.** Order of expected payoff against risk: (a), (b), (d), (e), (c). | Each is a configuration change measured against the unchanged baseline. **A/B rule, decided now:** alternating A and B runs, at least 5 pairs, same machine, quiet, same worker count; adopt a candidate only if (1) median wall falls at least **10 %** (the two baseline runs differ by 3.3 %) and the ranges do not overlap, (2) the sorted test-id list is identical and every test passes, (3) peak memory is not more than 10 % higher than baseline, (4) 3 consecutive green full runs, (5) **for every candidate (not only `isolate: false`), also 3 green runs with `sequence.shuffle` and different seeds**, because shared module state shows only when order changes, (6) **a CI arm**: one CI run with the candidate (the push or workflow dispatch is a lead step, not an AFK one) is green and not slower than the baseline CI run before the candidate is adopted. **Node project:** `setupTests.ts` also patches DOM things (`Element.prototype.scrollIntoView`, `ResizeObserver`, a `localStorage` mock) that do not exist in a node environment, so the node project needs its own trimmed setup file (locale pins and console mocks only), written and measured in slice 2. **Unknowns slice 2 reads from the workflow and records, not guesses:** the CI runner's vCPU count and memory (`runs-on` in `ci_frontend.yml`) and how many workers CI's default gives, since CI sets no cap. A candidate that needs any test edited is adopted per file or glob, and each edit is listed. Fidelity gate for (c): DataGrid, dialog and user-event tests must pass unmodified; run last, and only if (a)+(b) leave environment cost above 10 % of worker time. The reason `@mui/` is inlined is not in the repo; the slice reads the commit that added it first. |
| D6 | Which hotspots go first | By measured summed seconds, after configuration | Reports (68 s), ModifyTeamSettings (59 s), RefinementView (31 s), Overview + BaseMetricsView (30 s), EditConnection + CreateConnectionWizard (29 s), WorkItemsDialog (27 s), ModifyProjectSettings (27 s) | Configuration first because it multiplies across every later slice and tells us how much of the 691 s is still worth rewriting. Within families, "estimated saving" is the assumption of half the summed seconds, to be replaced by the measured saving at each slice's close. `BaseMetricsView` goes last: FE5 will split it, and an extraction now could collide (soft dependency, either order). |
| D7 | How a rewrite is done | (a) Extract the logic the slow test checks into a function or hook (production refactor, no test change), test it directly in milliseconds, then shrink the page-level tests to one or two wiring tests with small fixtures. (b) Only make the page-level tests cheaper (smaller fixtures, cheaper queries, fake timers). (c) Delete low-value tests. | (a) where the logic is genuinely in the component; (b) where it is not (pure DataGrid or wiring cost); never (c) | The brief's own prescription. (c) violates "every assertion still made". The per-slice proof is in D8. Query choice follows the ci-learnings rule (cheap query unless the role is the assertion). Real sleeps go to `vi.useFakeTimers` as the siblings already do. |
| D8 | Behaviour-free proof, per slice | see the section below | | |
| D9 | Mutation | Stryker once at the end, over the production files touched by extractions | | Stryker mutates production code; rewritten tests are the thing that kills. Run last on frozen code. Config traps (memory): check the mutate list and the vitest runner's include before the run. Coverage per slice is the earlier signal. |
| D10 | Progress report to the maintainer | After each slice: summed seconds before and after for the touched files, allow-list length, wall median | | Keeps "payoff proved" visible. |
| D11 | Rigor profile (proposal) | `standard` for all slices (crafter/designer Sonnet, reviewers Haiku), as the AFK entry for the test track chose; `thorough` for slice 1 (the guard everyone then lives under) and slice 3 (central configuration) if the maintainer prefers | | Mechanical rewrites follow a pattern; the guard and the configuration have the largest blast radius. DELIVER asks anyway. |

### Brief and Epic disagreements found

1. **A per-file frontend budget already exists and is wired into CI** (`test-time-budget.json`, `check_budget.py`,
   `ci_frontend.yml`, warn-only). The brief and the Epic describe the ratchet as new. Resolved in D1: FE7 lands first and
   owns the whole frontend budget (per-test ratchet plus the per-file budget made failing); T4 drops its frontend half and
   keeps the backend. KPI 14 ("no test over the budget") is read as the per-test budget; KPI 16 stays the CI step median.
   The T4 brief needs its frontend half removed at T4 pick-up.
2. **The hypothesis's disproof condition is half met** (see Measurement); the brief itself says DESIGN then re-scopes
   towards configuration. Taken: both.
3. **The Epic text says FE7 "profiles the suite" and the AC says "which cost dominates"**; the answer is not one
   cost but two halves of similar size. Recorded plainly above.
4. **The brief's "11 workers" baseline is stale**: T3 made it 6 locally. All new numbers are at 6 workers.
5. **The ratchet's "stale entry" direction cannot be a plain duration rule** (noise), and the list is keyed by test with
   a ceiling; adapted in D3. Slice 1 writes the departure from ADR-268 and ADR-269 as status notes (or one short ADR).
6. **CI's only Vitest run is the coverage command**, so any guard that is off under coverage never enforces in CI, and
   the CI timing artifacts are coverage-slowed. The first design had both wrong; D2 and D4 are corrected.
7. **The Epic's checklist says "existing tests stay green unmodified"**; for FE7 that is replaced, per the brief, by the
   moved-assertions list. No conflict.

## Wave: DESIGN / [REF] Hexagonal sketch

There is no product hexagon to change. What is touched is on the test side of the system, plus small extractions in
production code.

```
 pnpm test / IDE / CI
       |
       v
 vitest.config.ts ----- projects (slice 3, if the spike says so) ---+--> node project  (DOM-free .test.ts)
   |   |                                                           +--> jsdom project (everything else)
   |   +-- setupTests.ts (locale pins, userEvent delay null, localStorage)       (unchanged)
   |
   +-- reporters: default | sonar | json  +  TimeBudgetReporter (NEW, slice 1)
                                                 |  reads each finished test's duration
                                                 +--> compareToAllowList(durations, slowTests) (NEW, pure)
                                                              |
                                                     src/tests/timeBudget/slowTests.ts (NEW, shrinks)

 test files  ---- page-level tests (shrink to 1-2 wiring tests per flow)
      \--------- unit tests of functions/hooks (NEW or grown)  <-- extracted from components (production, behaviour-free)

 Scripts/test-timings/summarise.py (EXTEND: top-N shares, counts over thresholds)
 ci_frontend.yml -> check_budget.py --stack frontend (per-file half, no longer --warn-only; EXTEND)
```

Touched: `Lighthouse.Frontend/vitest.config.ts` (modified), `src/tests/timeBudget/` (new: reporter, pure compare step,
allow-list, tests), `Scripts/test-timings/summarise.py` (modified), about 25 hotspot test files (modified), new unit-test
files beside the production code they cover, a few production components (modified only by extracting a function or hook
whose behaviour is identical), `docs/ci-learnings.md` (edited). Unchanged: all production behaviour, `@screenshot` and
Playwright, `setupTests.ts` (unless the spike finds setup cost, 4 %), the Python budget script and the backend half of
the CI budget step (T4; the frontend `--warn-only` is removed here, D1), the `MockApiServiceProvider` and test kits, except where a slice adds a cheaper render helper.

## Wave: DESIGN / [REF] Component decomposition

| Component | Location | Change |
|---|---|---|
| Time-budget reporter | `src/tests/timeBudget/` | CREATE NEW (thin shell around the pure step) |
| Compare step (both directions, pure, driven with made-up lists) | `src/tests/timeBudget/` | CREATE NEW; pattern copied from `layerBoundaries.enforcement.test.ts` and ADR-268 |
| Allow-list | `src/tests/timeBudget/slowTests.ts` | CREATE NEW, seeded in slice 1, shrinks |
| Profile report | `Scripts/test-timings/summarise.py` | EXTEND |
| Vitest config | `Lighthouse.Frontend/vitest.config.ts` | EXTEND (reporter; `projects` and options as slice 2 and 3 decide) |
| Hotspot test files | per slice | EXTEND (shrink, rewrite) |
| Extracted functions/hooks | beside their component | CREATE NEW only where the logic lives in a component; each justified in the slice |
| Per-file budget wiring | `.github/workflows/ci_frontend.yml`, `Scripts/test-timings/test-time-budget.json` | EXTEND (D1) |
| Standing skip/focus check | `Lighthouse.Frontend/biome.json` | EXTEND (slice 1) |
| ADR notes | ADR-268, ADR-269 | EXTEND (status notes, D3) |
| Ledger | `docs/ci-learnings.md` frontend Tests | EXTEND |

## Wave: DESIGN / [REF] Reuse Analysis

| Need | Existing thing | Decision / contract shape |
|---|---|---|
| Ratchet list, three directions, pure compare step | `layerBoundaries.enforcement.test.ts` (`ALLOWED_LEAKS`, counted, planted lists); ADR-268/269 | EXTEND the pattern. **Shape: pure function** (return-only: durations, run kind and a list in; offenders out; asserted by planted lists, no I/O). The reporter is the only impure part (reads Vitest, sets the exit code); it receives its mode as options |
| Options for the reporter from environment and argv | `src/tests/testWorkers.ts` (`resolveTestWorkers`) | EXTEND the pattern. **Shape: pure function** (environment and argv in, options out; unit-tested with fake environments) |
| Per-test durations and CSV | `Scripts/test-timings/vitest_to_csv.py`, `summarise.py`, the `test-timings-frontend` CI artifact, `test-results.json` from the existing json reporter | REUSE for calibration. EXTEND `summarise.py` for the repeatable profile (no second script). **Shape: pure function** (file in, text out) |
| Per-file budget | `test-time-budget.json`, `check_budget.py`, `ci_frontend.yml` | EXTEND (D1): `--warn-only` removed from the frontend call, ceilings re-derived if needed. **Shape: bounded change**: the universe is the nine frontend entries and one workflow line; the backend half is untouched (asserted by the existing `Scripts/test-timings/tests` and by the backend call keeping its flag) |
| Worker count | `src/tests/testWorkers.ts` | REUSE; A/B runs fix the worker count with `LIGHTHOUSE_TEST_WORKERS`. **Shape: pure function** |
| Cheap queries, fake timers | ci-learnings rules; siblings of `ModifyTeamSettings.refinement.test.tsx` already use a fake clock | REUSE. **Shape: bounded change** per rewrite slice (see "Universe and delta") |
| Test kits (`BulkReportsTestKit`, `ReportsTestKit`, `MockApiServiceProvider`) | `src/tests/` | EXTEND only to add a cheaper render helper where a slice proves the provider stack costs; no replacement. **Shape: bounded change** (additive helpers; existing exports unchanged) |
| Extracted functions and hooks in production code | the components of each hotspot | CREATE NEW only where the logic lives in a component. **Shape: pure function** for functions; hooks keep their existing effects (**bounded change**, same effects as before, asserted by the unmodified page tests passing at the extraction commit) |
| Function-level tests already present | `workItemsDialogContexts.test.ts`, `useModifySettings*.test.ts`, `reportOwner.test.ts` | REUSE as the destination for moved assertions Shape: bounded change; universe = the test files that receive moved assertions; delta = the assertions added, one row each in the slice's moved-assertions list. |
| The suite's global behaviour (config, setup) | `vitest.config.ts`, `setupTests.ts` | EXTEND (slices 1 and 3). **Shape: bounded change**: the universe is the reporter entry and, if slice 3 adopts it, the `projects` entry; asserted by an identical sorted test-id list before and after and 3 green full runs. Nothing in it may be unbounded-preservation: a config edit that silently drops a reporter (the `--reporter=default` question in D2) is exactly the bug class slice 1 proves absent |

## Wave: DESIGN / [REF] Rejected alternatives

- **Rewrite everything to unit tests and drop page-level tests.** Rejected: the brief keeps one or two wiring tests per
  flow; providers, routing and RBAC gating are only proven by a rendered page.
- **Raise `testTimeout` or retry flaky tests.** Rejected: hides the cost and the flake; the CI flake needs cheaper tests.
- **A budget on whole-file seconds only.** Rejected (D1): names a file, not the slow test. It stays as the per-file half,
  because per-test durations cannot see import or `beforeAll` cost.
- **A guard that is off under coverage.** Rejected (D2): CI's only Vitest run is the coverage command.
- **A fallback budget of 5 s when CI data is missing.** Rejected (D4): equals the timeout and guards nothing; slice 1 stops.
- **Fail on any test over 100 ms.** Rejected: 1,893 tests today; the list would be the suite.
- **Snapshot of durations as the baseline (a golden file of all timings).** Rejected: regenerated without review, noisy.
- **Run the budget only in CI.** Rejected: the AC is "when `pnpm test` runs"; a local run must also name the test.
- **Switch the whole suite to happy-dom or `isolate: false` first.** Rejected: the largest fidelity and state-leak risk
  for an unmeasured gain; both are measured in slice 2 behind the A/B rule, and the node-only variant is cheaper.
- **Parallelise further (more workers).** Rejected: T3 caps local workers on purpose; CI worker counts are not changed here.

## Wave: DESIGN / [REF] Behaviour-free proof

Per slice, written to `measurement/slice-NN-moved-assertions.md` and verified by the reviewer:

1. **Moved-assertions list.** For every page-level test removed or shrunk: old test name, the assertions it made, and the
   new test (file and name) that makes each of them. A removed `expect(` with no row in the list is a defect.
2. **Test-id diff.** The sorted list of `file :: test name` before and after; every id missing after must appear in the
   moved-assertions list; additions are listed.
3. **E-weaken on the diff.** No `.skip`, `.todo`, `.only`, `it.fails`, loosened matcher, or longer timeout added; checked
   by a grep over the slice diff.
4. **Coverage.** `pnpm test --coverage` over the touched directory before and after: line and branch not lower. (Run in
   the slice that edits; not combined with the timing runs, because coverage slows tests.)
5. **Production behaviour identical.** An extraction commit changes no test; its diff is a move of code into a named
   function or hook with the component calling it; the existing page tests still pass unmodified at that commit, before
   any test is rewritten. `@screenshot` and Playwright untouched (no spec in the diff).
6. **Mutation, once at the end** (D9): Stryker over the extracted production files, kill rate not lower than before
   the Story, at least 80 % for new code.
7. **Configuration slices (2, 3):** identical sorted test-id list, 8,970 tests, all passing, 3 consecutive green full runs.

## Wave: DESIGN / [REF] Proposed slicing

Each slice at most one day. Savings are **estimates** in summed test seconds (wall is about one sixth locally), at 50 % of
a family's current seconds unless stated; each slice replaces its estimate with the measured figure at close. Every slice
ends with `pnpm test` and `pnpm build` clean, a payoff note (D10) and the ledger/allow-list edits it owes.

| # | Slice | Delivers | Seconds it works on | Estimated saving |
|---|---|---|---|---|
| 1a / 1b | **Time-budget guard + failing per-file budget + profile report** (split by DEVOPS, 2026-11-02: **1a** = steps (1)-(4) and (6)-(9), no workflow-file edit, pushable with the other slices; **1b** = step (5), `--warn-only` becomes repository-variable-controlled in `ci_frontend.yml`, one full-suite push chosen by the lead) | In order: (1) the four spike questions of D2; (2) calibration: local plain against local `--coverage` on the same machine, 3 oversubscribed local runs, CI artifacts (at least 5; stop if missing), first-test-in-file allowance A, CI/local ratio F; (3) reporter (options, not environment), pure compare step with planted-list tests for all three directions of D3, allow-list seeded from the calibration with reasons and ceilings; (4) **proof**: a planted slow test fails through `pnpm test` and through `CI=true pnpm run sonarreport`, and not under the Stryker marker or in watch; (5) `--warn-only` removed from the frontend call in `ci_frontend.yml` (ceilings re-derived if the check does not pass on the 5 runs); (6) `summarise.py` extended (top-N shares, counts over thresholds); (7) `docs/ci-learnings.md` entry "where to test"; (8) status notes on ADR-268 and ADR-269 (D3); (9) a standing check for skipped and focused tests: Biome has `suspicious/noSkippedTests` and `noFocusedTests`, neither enabled in `biome.json` today; the slice counts current offenders and enables them (offenders fixed or listed by name), which makes the per-slice grep a second line rather than the only one | none (no speed change) | 0; the guard is the product. The DEVOPS split above replaces the earlier 1a/1b idea; if calibration does not fit one day, 1a splits further into spike + calibration and guard |
| 2 | **Configuration spike (measure only)** | One table: each D5 candidate, alternating A/B, the A/B rule applied (including the shuffled arm for `isolate: false` and the CI arm); a trimmed node-project setup file measured; per-file import/environment/transform split; a CPU profile of two slow files; CI runner facts read from the workflow. No adoption. Scripts in `measurement/scripts/` | about 800 worker-seconds of fixed overhead | 0 now; produces the number for slice 3 |
| 3 | **Adopt the winning configuration** | Only candidates that passed the A/B rule: `projects` with a node environment, MUI inlining change, etc., plus any per-file edits it needs, listed | the candidates that won | to be measured; derived upper guide for the node environment alone: at most 184 `.test.ts` files (an upper bound; some need the DOM) x about 0.5 s = up to 90 worker-seconds, about 15 s wall |
| 4 | **ModifyTeamSettings family** (`.test`, `.refinement`, `.refinementNeed`, `.readiness`, `.queryCommit`) | Autosave and rule-gating logic tested as hooks/functions; the 600 ms real sleep in `.refinement` and the 500 ms ones in `.test` move to a fake clock; 300 ms debounce waits fake-timed | at least 58.7 (`.refinement` not in the top 25) | about 30 s |
| 5 | **Reports pages**: `ReportsPage.removal`, `.combinedPdf` | Pick limit (50), selected count and "Select all" as functions over a row list; page keeps wiring tests with small fixtures | 31.2 | about 16 s |
| 6 | **Reports tab and wizards**: `ReportsTab`, `ReportsTab.createWizard`, `BulkReportWizard`, `ReportView` | Wizard step/validation logic as functions; fewer user-event walks; the 350 ms and 300 ms real waits to fake timers | 37.0 | about 18 s |
| 7 | **RefinementView**: `.paging`, `.comments`, `.votes` | Paging/filter arithmetic as functions; the 143-row render only in one test; holds the slowest test | 31.1 | about 16 s |
| 8 | **EditConnection and CreateConnectionWizard** | Field-change and default-resolution logic as functions | 29.2 | about 15 s |
| 9 | **WorkItemsDialog** (`.contexts`, `.test`, `.paging`) | The 108-case context table against `workItemsDialogContexts` (a function test exists); one render per distinct panel behaviour; paging arithmetic direct | at least 27.0 (`.paging` not in the top 25) | about 14 s |
| 10 | **ModifyProjectSettings** (`.test`, `.queryCommit`) | Shares the form pattern of slice 4: reuse its helpers; the three 500 ms sleeps to a fake clock | 26.8 | about 13 s |
| 11 | **OverviewDashboard and BaseMetricsView** (`.loading`, `.test`) | Loading/failure looks stay pinned; cost profiled first (the budget file says "not profiled yet"); `BaseMetricsView` waits for or coordinates with FE5 | 30.3 | about 15 s (low confidence) |
| 12 | **Sweep and close** | Allow-list review: delete what is fixed; tighten the budget if the list allows; DataGridBase.paging, featureChildItemsDialog, tail; final alternating before/after runs (median of at least 5); Stryker once (D9); finalize notes | the rest | measured |

Sums (derived): slices 4-11 work on about 271 of the 691 summed seconds; the estimates in the table add to about 136 s
summed (half, per row), about 23 s of wall, **about 9 %**. Adding slice 12's two named files (17 s more) gives the 288 s and the
10 % in the Measurement section. The wall effect of slice 3 is the unknown that decides whether this Story reaches
"minutes off CI"; hence it comes before the hotspot slices. The slices after 3 may be reordered or dropped by their
measured payoff; the maintainer's rule from the T-track holds: kill the survivors, do not chase hours of runs.

### Universe and delta of a rewrite slice (slices 4-11)

Every rewrite slice declares, at its start, in `measurement/slice-NN-moved-assertions.md`:

- **Universe (what may change):** the production functions or hooks it extracts (named then; each is a move with the
  component calling it, no behaviour change), the test files it edits, the allow-list entries it deletes. Nothing else:
  no other production file, no Playwright spec, no `setupTests.ts`.
- **Must stay green (the preserved set):** the sorted test ids of the touched files before the slice, except ids listed in
  the moved-assertions file as replaced by a named new test; the whole suite stays green.
- **Delta (what it changes):** summed seconds of the touched files, tests added and removed, entries deleted from
  `slowTests.ts`.

Extractions: slice 4 (autosave debounce and rule gating), 5 (pick limit and selected count), 6 (wizard step validation),
7 (paging and filter arithmetic), 8 (field-change and default resolution) are named from the files read above. Slice 9
needs none expected: `workItemsDialogContexts` already exists and is tested, so the work is to point the 108 cases at it;
slice 10 reuses slice 4's helpers; **slice 11's extractions are named at slice start after profiling**, as the budget file
says the cost "has not been profiled yet".

Soft ordering notes: slices 4 and 10 share helpers (do 4 first). Slice 11 follows FE5's split of `BaseMetricsView` if
FE5 has landed; otherwise it leaves that file and ratchets it. Slices 5-11 are independent of each other.

## Wave: DESIGN / [REF] Open questions

Genuine questions for the maintainer: **none.** Engineering choices took the recommended option. Inputs the orchestrator
must supply or confirm (not decisions):

- Slice 1 needs the `test-timings-frontend` artifacts of at least 5 recent main runs (`gh run download`; they come from the
  coverage run), to measure the CI/local ratio and calibrate D4. **If they are missing, slice 1 stops and asks for them;
  there is no fallback value.**
- Slice 2's CI arm (one CI run with a candidate configuration) needs a push or a workflow dispatch: a lead step, not an
  AFK one. CI runner vCPU, memory and worker count are unknowns slice 2 reads from `ci_frontend.yml`.
- The reason `@mui/` and `react-transition-group` are inlined is not written in the repo; slice 2 reads the commit that
  added it before testing its removal.
- Slice 1's spike answers (Vitest 5 reporter hook and exit code, the effect of `--reporter=default`, the Stryker marker)
  can change D2's mechanics but not its outcome (guard on in `pnpm test` and the CI coverage run, off under Stryker and
  watch); if (2) shows the flag drops config reporters, the script edit is part of slice 1.

## Architecture summary for the maintainer

**The feature's shape in hexagonal terms.** There is no product hexagon to change; FE7 works on the test side and on the
seams between tests and code.

```
  DRIVING SIDE (what runs the tests)         APPLICATION / DOMAIN (production, unchanged in behaviour)       DRIVEN SIDE
  ---------------------------------          -------------------------------------------------------         --------------------
  pnpm test | IDE | CI                       components / pages  --calls-->  extracted pure functions         API services (mocked
     |                                          (thin: render, wire)              and hooks  [NEW or grown]     with MockApiServiceProvider)
     v                                                                            ^
  vitest.config.ts  [MODIFIED]                tests, before:  page test ----------+  (logic checked through a whole render)
     |  projects: node | jsdom  [NEW, if       tests, after:   unit test of the function [NEW] + 1-2 page wiring tests
     |  slice 2 says so]
     |  reporters + TimeBudgetReporter [NEW] --reads durations--> compare step [NEW, pure] --> slowTests.ts [NEW, shrinks]
     +-- setupTests.ts  [unchanged]
```

New: the reporter, the pure compare step, the allow-list, unit tests of extracted functions. Modified: `vitest.config.ts`,
`summarise.py`, about 25 hotspot test files, a few components (extraction only). Unchanged: every driving adapter
(controllers, routes), application services and ports, the domain Models, the driven adapters (trackers, persistence),
Playwright, `@screenshot`. New external call: none.

**Patterns weighed.**

- Ratchet with a named allow-list (ADR-268/269): **chosen**, copied from the layer-boundary guard; a count was rejected
  because it hides which test was fixed and which was added.
- Functional core, imperative shell for the guard: **chosen** (pure compare step, thin reporter) so both failure
  directions are proven without breaking real tests.
- Single responsibility / extract function or hook out of a component: **chosen** where the logic is in the component, so
  it tests in milliseconds; "make the page test cheaper" is the fallback, deleting tests is rejected.
- Dependency inversion (already there): the ports in `MockApiServiceProvider` keep page tests honest; no change, and FE4's
  per-module ports will make doubles smaller later.
- Strategy-like configuration through Vitest `projects` (node versus jsdom per glob): **candidate**, adopted only if the
  A/B rule passes in slice 2.
- Fake clock instead of real sleeps (test double for time): **chosen**; real sleeps are about 5.5 s of the suite and the
  cause of the timing coupling.
- Stale-entry check by plain duration: **rejected** (a loaded runner makes tests look slow, never fast); replaced by "did
  not run in a file that ran, or ran in under a quarter of the budget", plus a per-entry ceiling so a listed test cannot
  get slower.
- One budget guarding both per-test and per-file cost: **rejected**; two halves (reporter and the failing per-file CI check)
  because import and `beforeAll` time belongs to no test.
- Domain events, outbox, unit of work, value objects, optimistic concurrency, JSON column against child table:
  **not applicable**; no domain or persistence change.

## Wave: DEVOPS / [REF] Platform decisions (2026-11-02)

AFK mode: the recommended option was taken on each row; nothing deploys and nothing is pushed by this wave.

| Decision | Answer |
|---|---|
| Deployment target, orchestration, strategy | N/A, because nothing deploys: test configuration, tests, Python scripts and one workflow line. |
| CI/CD platform | GitHub Actions. **No new workflow and no new step.** Files read: `ci.yml`, `ci_frontend.yml`, `ci_changes.yml`, `.github/actions/build-frontend/action.yml`. |
| Branching | Trunk-based on `main`; one push per slice, green first (`ci.yml:4-5` also runs `features/**` branches, used only for slice 2's CI arm). |
| Rigor | D11 as proposed: `standard`, `thorough` for slices 1 and 3 if the maintainer prefers; DELIVER asks and passes `model` on every dispatch. |
| Usage-data event | **N/A, because it is behaviour-free test infrastructure: nothing a user does changes, so there is no use to count.** No `UsageDataEventName` entry, no line in `docs/settings/usagedata.md`. |
| Observability, alerting, SLOs | N/A, no running service. The instruments already exist: the `test-timings-frontend` artifact (`ci_frontend.yml:41-47`), the JSON and Sonar reporters (`vitest.config.ts:31-35`), the run's Vitest `Duration` line. The CI "SLO" is KPI 16 (CI test step minutes, median of >= 5 main runs), measured, not alerted. |
| Security | No secret, permission or scope change. |
| Cross-cutting (RBAC, Clients, Website, UI sketch, Terminology, ADR-270) | N/A as answered in PICK-UP and DESIGN. |

## Wave: DEVOPS / [REF] CI facts that bear on the DESIGN (read from the files, not assumed)

1. **The composite action is ONE step in the jobs API.** `gh run view 41428581254 --json jobs` lists `Build Frontend (composite)` as a single step, 10:25:26 to 10:45:59 = 1,233 s (20.5 min), covering install, `pnpm run build`, the test run and the Sonar scan (`action.yml:42-81`). The sub-step "Test Frontend and Sonar report" has no entry of its own. **KPI 16's frontend half is therefore defined as the median of Vitest's own `Duration` line from the frontend job log, over at least 5 runs whose frontend job ran** (log via `gh api --allow-escape-sequences repos/LetPeopleWork/Lighthouse/actions/jobs/<job-id>/logs`, escape sequences stripped). Baseline from the 7 saved runs (`measurement/ci-artifacts-2026-11-02/ci-vitest-duration.txt`): 1092.25, 1084.89, 930.91, 677.03, 809.69, 834.69, 1069.85 s, **median 930.9 s (15.5 min)**; Vitest's split on CI is the same as local (tests 51-54 %, import 22-24 %, environment 14 %). The composite step (1253, 1247, 1076, 805, 952, 994, 1233 s, median 1076 s) stays as a secondary figure that includes install, build and the Sonar scan. CI against local wall is about 3.7x (931 s against about 250 s), a whole-run figure on a different machine, worker count and run kind, **not** the per-test F of D4.
2. **`bail: 1` is on in CI** (`vitest.config.ts:48-50`, `process.env.CI`); `CI: true` is set for the test step (`action.yml:65-66`). A failing test stops the run and the JSON and CSV are partial. A reporter that fails at **end of run** is not cut short by `bail` (it did not fail a test), so one run can name **every** offender. Decision: the reporter collects and reports at `onTestRunEnd`, never per test.
3. **Worker count in CI is Vitest's own default.** `maxWorkers` is `resolveTestWorkers(process.env, availableParallelism())` (`vitest.config.ts:44`); unset and CI, it returns nothing configured (CLAUDE.md "Local test worker cap"). The runner is `ubuntu-latest` (`ci_frontend.yml:13`), no size label; T2's DEVOPS recorded 4 cores for this repo. **Not read from the repo, so slice 1 and 2 print `nproc` and the effective worker count in the job log** (the reporter's start line). This machine has 12 cores and runs 6 workers, so the local/CI worker ratio is part of the F factor, not separable from it; D4 already measures F per test, which absorbs it.
4. **CI's only Vitest run is the coverage run**: `pnpm run sonarreport` = `vitest run --coverage` (`package.json:18`, `action.yml:60-66`). `pnpm test` passes `--reporter=default` (`package.json:17`); whether that replaces the config reporters is spike question 2 of D2, unchanged. The CI run has no `--reporter` flag, so a reporter in `vitest.config.ts` loads there for certain.
5. **Correction to D1's rationale.** D1 says the per-file budget "sees" import and module-level cost. It does not: `vitest_to_csv.py:41` and `:47` read each assertion's `duration`, the writer is `:56-66`, and `check_budget.py:107-112` sums those per file. Both guards see only test (and hook) time; **neither guards the fixed per-file half** (import, environment, transform). D1's decision stands (per-test names the slow test, per-file sums a 129-test file the per-test guard cannot flag), but its reason is "the sum over a file's tests", not "the other half of worker time". The fixed half stays guarded by nothing; slice 2/3 measure it, and a per-file wall guard from the JSON's file `startTime`/`endTime` is an option for slice 3 if that half proves large. Recorded, not decided here.
6. **Any edit to `.github/workflows/ci*.yml` forces everything on.** `ci_changes.yml:193-198`: a changed `ci*.yml` sets `github_changes=true`, which sets backend, e2e and every connector suite true. Slice 1 removes `--warn-only` from `ci_frontend.yml:60`, so **slice 1's push runs the full backend, E2E and live connector suites** (Jira, Linear, ADO, ServiceNow). Same consequence as T2 slice 1: the Linear key is shared with CI, so no hand-exploring of the Linear API and no full local backend run shortly before the push; 429s show up as several unrelated failures. Edits to `action.yml` alone do not trigger this (the classifier regex covers `.github/workflows/` only) and do not even start the frontend job unless a frontend path also changed. Only slice 1 touches a workflow file.
7. **Which main runs have a frontend artifact.** The frontend job runs only when the diff since the **last green main run** touches `Lighthouse.Frontend/`, `.nvmrc`, `Scripts/test-timings/` or a `ci*.yml` (`ci_changes.yml:198`; base chosen at `:140-170`). Backend-only pushes skip it (`ci_frontend.yml:12`); a red main run keeps the frontend flag on for the following runs. Count runs by artifact, not by run.
8. **Triggers for a CI arm, and what one costs.** `ci.yml:3-26`: push to `main` or `features/**` (path-filtered), `pull_request` to `main`, and `workflow_dispatch`. On a non-main ref the change base is `HEAD^` (`ci_changes.yml:180-183`), so the frontend job runs on a branch push or dispatch only if the tip commit itself touches a frontend path (the candidate configuration change does). **But the other jobs have no change filter**: `packageapp`, `sbom`, `docker`, `verifysqlite`, `verifypostgres` and `verifyauth` (`ci.yml:93-165`) run on the branch too, and `ci_verifysqlite.yml` carries the live-connector secrets (the Linear key among them, `:14-18`). **One candidate run is a full verify run with live connectors.** Accepted as the cost (changing CI gating is out of scope): the lead schedules it, runs at most one such candidate run, and not near another connector run, because the Linear key is shared with CI (rate limits).
9. **A reporter failure red-lights the job and skips the Sonar scan.** The test step has no `if: always()`, so the Sonar cache and scan steps (`action.yml:68-81`) are skipped and `sonar-gates` is skipped (`ci.yml:81-90`). That is how a failing test already behaves; a budget failure is a red frontend. The extract and upload steps are `if: always()` (`ci_frontend.yml:27, 42`), so the CSV is still published for a budget-red run, which is what calibration needs. The "Check test-time budget" step (`:56-60`) is **not** `always()`: after a red test step it does not run.

## Wave: DEVOPS / [REF] Pipeline changes per slice

Only slice 1 changes CI files. No new workflow, no new step, no new job.

| Slice | CI file changes | Detail |
|---|---|---|
| **1b** | `.github/workflows/ci_frontend.yml:56-60`: the frontend `check_budget.py` call no longer carries a fixed `--warn-only`; it is passed only when the repository variable `LIGHTHOUSE_BUDGET_WARN_ONLY` is `true` (an `env:` line on the step and a one-line shell test), and the comment at `:55` ("Warn only until the provisional budget is re-derived") is rewritten to say how the variable works. Unset = enforce. | The backend call lives in `ci_backend.yml` and keeps its flag (T4). If `check_budget.py` does not pass on the 7 saved artifacts (`measurement/ci-artifacts-2026-11-02/`, runs 41227946204 to 41428581254), the nine ceilings in `Scripts/test-timings/test-time-budget.json` are re-derived in the same change (D1) and `Scripts/test-timings/tests/test_check_budget.py` gets the new case. `check_budget.py:340` already fails on a **stale** entry even under `--warn-only`; today that is green. **Slice 1 is split:** 1a (everything else) pushes without touching a workflow file; 1b edits `ci_frontend.yml`, which sets `github_changes` and so runs the full backend, E2E and live connector suites (fact 6): the lead picks when. |
| **1a** | **The reporter runs inside the existing step**: it is listed in `vitest.config.ts` (`test.reporters`, lines 31-35, new entry with options from the pure options function), so `pnpm run sonarreport` (`action.yml:63`) runs it. The same commit wires the rollback variable: `env: LIGHTHOUSE_TIME_BUDGET: ${{ vars.LIGHTHOUSE_TIME_BUDGET }}` on the test step (`action.yml:65-66`; no `ci*.yml` edit, so no full-suite trigger). **Whether a composite action can read the `vars` context is checked first in 1a**; if it cannot, the value is passed as an action input from `ci_frontend.yml` and that wiring moves into 1b (until then 1a's options function defaults to `warn` when `CI` is set). | Local `pnpm test`: if `--reporter=default` drops the config reporters (D2 spike question 2, **slice 1a's first spike**), `package.json:17` changes (the script, not CI). Until that is answered, the measurement protocol runs Vitest with an explicit command (below). |
| 1a | `Scripts/test-timings/summarise.py` (extend), `check_budget.py` only if the ceilings change. | Both under `Scripts/test-timings/**`, which the frontend filter watches (`ci_changes.yml:198`). |
| 1a | `docs/ci-learnings.md`, ADR-268/269 notes, `biome.json` (`noSkippedTests`, `noFocusedTests`). | `biome.json` is checked by `pnpm build`'s prebuild (`package.json:15`); not a CI file. |
| 2 | None on `main`. The CI arm runs on a throwaway `features/**` branch whose tip changes `vitest.config.ts`. | Nothing from that branch lands on `main` except through slice 3. |
| 3 | None expected (`vitest.config.ts`, a node-project setup file). If a candidate needs `action.yml` (for example an env var), say so in that slice. | |
| 4-12 | None. | Tests and extracted code only. Each deletes its entries from `slowTests.ts`. |

**CSV artifact `test-timings-frontend`: no new columns.** The columns are fixed by `vitest_to_csv.py:12` (`file, test_name, duration_ms, outcome`) and `check_budget.py:199` refuses any other set (`tuple(columns) != expected_columns`), so a column is a coordinated change to both files and to every saved artifact. It is not needed: every CI artifact is a coverage-run artifact (the only Vitest run in CI, fact 4), so "run kind" is constant and carries no information; F is computed by joining a CI CSV to a local JSON on `file :: test name`; A (first test in file) is measured on the local JSON, whose `assertionResults` are in file order, and enters the threshold as a constant that F then scales. **Fallback**, only if slice 1 finds A cannot be calibrated that way: append one trailing column `position_in_file` in `vitest_to_csv.py`, relax `check_budget.py:199` to accept the old and the new header, and re-collect the artifacts.

**How a failing reporter surfaces in the job log.** One line per offender, printed at end of run, in the shape `check_budget.py` already uses (`breach_annotation`, `check_budget.py:172-176`) so the log reads the same from both guards, with the same escaping of `%`, CR and LF (`escaped`, `check_budget.py:167-169`):

```
::error::src/pages/X/Y.test.tsx :: Y does the thing took 3.4s, over the 3.0s budget (2s x 1.5 CI factor) set in slowTests.ts
::error::src/pages/X/Z.test.tsx :: Z does the other took 4.1s, over its 2.5s ceiling (1.0s x 1.5, plus 0.5s first test in file) set in slowTests.ts
::error::src/pages/X/W.test.tsx :: W is gone is allowlisted in slowTests.ts but did not run, or ran in under a quarter of the budget; remove its entry
```

Locally the same text without the `::error::` prefix (the prefix only when `GITHUB_ACTIONS` is set, decided by the pure options function, not by the reporter). The line is the test, its duration, the threshold and where to change it, nothing more; the exit code is set once at the end. The run's last line states the mode and numbers once (`time budget: B=2s, F=1.5, A=0.5s, workers=N, nproc=M, run kind coverage`), which is also the place the CI worker facts of fact 3 get recorded.

## Wave: DEVOPS / [REF] Measurement protocol

Owned by the lead; nothing here runs before a push except the local arm. The machine is reserved for it later.

### Local arm (quiet machine, slices 2, 3 and the closing runs of 12; per-slice summed seconds in rewrite slices)

1. Preconditions: nothing else running, `dotnet build-server shutdown` beforehand, `LIGHTHOUSE_TEST_WORKERS=6` exported in both arms (the baseline's worker count; the two baseline runs differ by 3.3 %, the noise floor for the D5 10 % rule).
2. Arms are two checkouts, never a toggled config: a baseline worktree at the pre-slice commit and a candidate worktree, each with `pnpm install --frozen-lockfile --ignore-scripts` (worktree hook caveat: memory note on pnpm 12 and worktree hooks). In a worktree copy the gitignored licence fixture only if a backend test is involved (not for the frontend run).
3. One run = `OUT_DIR=<dir> docs/feature/story-6461-t3-test-runs-leave-room/measurement/measure.sh <label> <worktree>/Lighthouse.Frontend pnpm test`; it prints `label,exit,wall_s,peak_tree_rss_mb,min_mem_available_mb,max_threads`. **Copy `test-results.json` out after every run** (run 1 of the baseline was overwritten by run 2; `vitest.config.ts:34` writes the same path each time).
   **Until slice 1a's spike answers whether `--reporter=default` replaces the config reporters, the protocol runs `pnpm exec vitest run --reporter=default --reporter=json` instead of `pnpm test`** (with `TZ=Europe/Zurich` as the script sets it), so the JSON is written regardless; it lands in `Lighthouse.Frontend/test-results.json` because the config sets that path (a CLI `--outputFile.json` was ignored in the baseline run). Copy it out after each run.
4. Alternate A, B, A, B ... for at least 5 pairs; report median wall, min-max range and peak memory per arm. D5 acceptance: median at least 10 % faster and ranges not overlapping; identical sorted `file :: test name` list (from the saved JSONs) and all passing; memory not more than 10 % up; 3 consecutive green full runs; for every candidate 3 more with `sequence.shuffle` and different seeds (the shuffle is a command-line option on the candidate arm, not a config edit).
5. Plain run and `--coverage` run on the same machine are separate pairs (the coverage run is not combined with timing runs, per DESIGN proof item 4); slice 1 uses them for F's machine-versus-coverage split.

### CI after-measurement (median of at least 5 main runs whose frontend job ran)

1. List completed `ci.yml` runs on `main` after the slice's commit (`gh run list --workflow ci.yml --branch main`, the list API serves a stale page about 1 time in 7: re-list before trusting a count; exclude the slice's own push run if it was red).
2. **Classify by job result, not by artifact presence.** For each candidate run: `gh run view <id> --json jobs`, find `Verify Frontend / frontend`. `skipped` = backend-only diff (fact 7), not counted; `failure` = does not count (partial CSV under `bail`); `success` = counts, then `gh run download <id> -n test-timings-frontend -D <dir>/ci<id>`. Cross-check that the CSV has no `Failed` rows and at least as many rows as the baseline (8,970 minus the tests the slice deliberately removed, listed in its moved-assertions file). Keep going until 5 qualifying runs are in hand; with backend-heavy traffic that may take more calendar days than runs.
3. Compute per-file medians of summed seconds and the total with the existing helper approach of T2 (`cistats.py`-style glob over `<dir>/ci*/test-timings-frontend.csv`; a frontend sibling is written in slice 1 under `measurement/scripts/` if `summarise.py` does not already do it) and compare with the saved baseline (`measurement/ci-artifacts-2026-11-02/`, 7 runs).
4. **KPI 16, frontend half** = the median Vitest `Duration` of the 5 qualifying runs' job logs (fact 1 for the command and the baseline, 930.9 s). Secondary: the composite step median (1076 s baseline), which includes install, build and the Sonar scan. State which one is meant. Overlapping ranges are not a verdict.
5. Recorded in a `Wave: DELIVER / [REF] CI after-measurement, slice N` block with run ids, table and verdict; the baseline files are never edited.

### CI arm for slice 2 (a candidate configuration on CI)

Both triggers exist (`ci.yml:5, 26`). Recommended: push a throwaway branch `features/fe7-config-<candidate>` whose tip commit changes `vitest.config.ts` (so the frontend job runs, fact 8), then read the artifact and the log `Duration` of that run; the dispatch is the alternative on an already-pushed branch (same tip rule). Compare against the 5-run median above (D5 rule 6: green and not slower). **Cost, stated plainly (fact 8): this is a full verify run with live connectors** (packageapp, sbom, docker, verifysqlite with the Linear key, verifypostgres, verifyauth all run on the branch, `ci.yml:93-165`). It is a lead step: at most one such run per candidate decision, scheduled away from any other connector run, branch deleted afterwards.

## Wave: DEVOPS / [REF] Rollback

Every slice is revertible alone; nothing is persisted.

- **Slice 1a:** `git revert` restores the previous config. **No commit is needed to switch the reporter off in CI:** set the repository variable `LIGHTHOUSE_TIME_BUDGET` (`off` | `warn` | unset = enforce; `gh variable set LIGHTHOUSE_TIME_BUDGET --body warn`, maintainer or lead). `action.yml:65-66` hands it to the test step, the reporter itself reads no environment, only the pure options function does (it maps variable, `CI` and argv to the reporter's options). `warn` prints every line as `::warning::` and exits 0, the role `--warn-only` plays for `check_budget.py`; `off` loads nothing. The CSV and per-file budget keep working.
- **Slice 1b:** set the variable `LIGHTHOUSE_BUDGET_WARN_ONLY=true` to restore warn-only with no commit; `git revert` of the slice commit restores the old workflow line.
- **Slice 2:** no change to `main`; delete the throwaway branch.
- **Slice 3:** `git revert`; the A/B table in `measurement/` says which candidate was adopted, so a revert is targeted (one option per commit).
- **Slices 4-11:** `git revert` of the rewrite commit; the extraction commit (a pure move, no test change) is its own commit and reverts separately; the revert re-adds that slice's `slowTests.ts` entries in the same commit, because the entries and the tests change together.
- **Slice 12:** nothing to roll back beyond the above.
- **Stop signals, not notes:** a flaky guard failure on an unlisted test in the first 5 main runs after slice 1 (switch to `warn`, re-calibrate B or F from the new artifacts); peak memory more than 10 % above the 4.6-4.8 GB baseline after slice 3.

## Wave: DEVOPS / [REF] Local checks per slice

Run on the quiet machine; frontend only (`Lighthouse.Frontend/`), no backend gate applies.

1. `pnpm test` green (plain run, guard on); **slices 1, 2 and 3 also run `CI=true pnpm run sonarreport` once** (the only CI-shaped run: coverage, `bail`, the reporter under both); slices 4-11 run it once on the touched test files (fresh-values-in-hooks rule).
2. `pnpm build` (Biome `check --write` then `tsc -b` then `vite build`): zero errors and warnings; then `git status` for files Biome rewrote (and the docs-symlink hazard in memory: remove `*/docs` symlinks first).
3. **Planted-test proof** (slice 1, and again after any change to the reporter or options): a planted slow test fails through `pnpm test` and through `CI=true pnpm run sonarreport`, and does not fail under the Stryker marker or in watch; a planted listed test over its scaled ceiling fails; the planted file is removed and `git status` shows it gone.
4. **Moved-assertions list** `measurement/slice-NN-moved-assertions.md` (slices 4-11): every removed or shrunk test with the new test making each assertion; the sorted `file :: test name` diff before and after, every missing id listed.
5. **E-weaken grep over the slice diff:** no `.skip`, `.todo`, `.only`, `it.fails`, loosened matcher, longer timeout; from slice 1 on Biome `noSkippedTests` and `noFocusedTests` back it up.
6. **Coverage not dropping:** `pnpm test --coverage` over the touched directory before and after (line and branch), run separately from timing runs.
7. **Allow-list only shrinks:** the slice deletes the `slowTests.ts` entries it fixes in the same commit; `check_budget.py` entries in `test-time-budget.json` likewise (stale entries fail even under `--warn-only`, `check_budget.py:340`).
8. Slice 1 only: `python3 -m unittest discover -s Scripts/test-timings/tests` green (the CI step runs it too, `ci_frontend.yml:59`); the options function and the compare step have their own planted-list unit tests.
9. Slices 2 and 3: identical sorted test-id list, 8,970 tests, 3 consecutive green full runs, plus 3 shuffled for `isolate: false` (D5).
10. No Playwright spec and no `@screenshot` file in any slice diff.

## Wave: DEVOPS / [REF] Mutation strategy

Unchanged from DESIGN D9 and the project's `per-feature` strategy: Stryker (frontend) once, at the end, on frozen code, over the production files touched by extractions in slices 4-8 and 11; kill rate not lower, at least 80 % for new code. Stryker's `vitest-runner` is a consumer of `vitest.config.ts`, so the reporter must be **off under Stryker** (D2, proved in slice 1); check the Stryker config's `mutate` list and the runner's include first (memory: Stryker config traps). The reporter and compare step are not mutated (they are test infrastructure); their planted-list tests are their equivalent.

## Wave: DEVOPS / [REF] Risks and carry-forward for DISTILL and DELIVER

1. Slice 1b's push edits a workflow file and so runs the full backend, E2E and live connector suites (fact 6): announce it, keep the Linear key quiet. Slice 1a does not.
2. D1's rationale correction (fact 5): DELIVER slice 1 words the ADR notes and the `ci-learnings` entry accordingly; the fixed per-file half stays unguarded.
3. KPI 16's frontend half is the median Vitest `Duration` from the job log (fact 1, baseline 930.9 s); slice 1a confirms the log fetch works from a CI run before the first "after" number is claimed.
4. CI runner size and effective worker count are unverified until slice 1's first CI run prints them.
5. Frontend artifacts exist only for runs whose diff touched the frontend; the "5 runs" can take days (measurement protocol, step 2).
6. A budget breach in CI skips the Sonar scan and `sonar-gates` (fact 9), as any red test step does.
7. Under `bail: 1` only offenders that ran before the bail are named; the reporter reports at `onTestRunEnd`, and whether that hook fires with the full list on a bailed run is slice 1a's spike question 1. The stale check is skipped for a bailed run (D3).
8. The slice-2 CI arm is a full verify run with live connectors (fact 8): one run, lead-scheduled.
9. DISTILL: no nWave scenario tags in code; reviewers treat an untagged scenario as correct.

## Wave: DISTILL / [REF] Scope and rules (2026-11-02, maintainer AFK)

- Shape: test infrastructure plus behaviour-free rewrites. The acceptance scenarios pin the **guard** and the **configuration adoption**; no product behaviour is specified.
- Reconciliation: DESIGN and DEVOPS agree on every decision read (D1-D11, the 1a/1b split, the `LIGHTHOUSE_TIME_BUDGET` rollback); 0 contradictions. DISCUSS decisions are the brief and Epic, already reconciled in PICK-UP.
- UI sketches: N/A, because nothing a user sees changes.
- nWave scenario tags: none in code (project rule); classification is kept in this section only.
- Mechanism: Vitest in-process for the pure steps; the reporter shell and the shell checks below are real-run checks owned by DELIVER.

## Wave: DISTILL / [REF] Driving ports and signatures chosen

DESIGN left the compare-step signature open (`compareToAllowList(durations, slowTests)`); chosen, minimal, in `Lighthouse.Frontend/src/tests/timeBudget/`:

| Port | File | Signature |
|---|---|---|
| Compare step (pure) | `compareToAllowList.ts` | `compareToAllowList(run: ObservedRun, allowList: readonly AllowListEntry[], options: TimeBudgetOptions): Verdict` |
| Options (pure) | `timeBudgetOptions.ts` | `resolveTimeBudgetOptions(env, argv): { mode: "off" \| "warn" \| "fail"; runKind: "plain" \| "coverage" }` |

- `ObservedRun = { tests: ObservedTest[]; complete: boolean; fileExists(file): boolean }`; `ObservedTest = { file, name, seconds, state: passed|failed|skipped, firstInFile }`. The reporter builds this from Vitest's per-test results and the collected-file set; `fileExists` is injected so the step stays pure.
- `AllowListEntry = { file, name, reason, ceilingSeconds }` (quiet-local seconds, as D3/D4).
- `TimeBudgetOptions = { mode, runKind, budgetSeconds (B), firstTestAllowanceSeconds (A), coverageFactor (F) }`. The real numbers come from slice 1a's calibration; the tests use made-up B=2, A=0.5, F=2.
- `Verdict = { findings, failing, lines }`; findings are `over-budget`, `over-ceiling` or `stale` (`file-gone`, `test-absent`, `ran-far-under-budget`). `failing` is true only in `fail` mode with a finding. `lines` is one `file :: test` line per finding with seconds and limit.
- Limit rule asserted: `B x F`, plus `A` (unscaled) for a file's first test; a listed ceiling is scaled the same way. "Under a quarter of the budget" is read as `B / 4` unscaled (conservative: only an unambiguously fast run retires an entry). If DELIVER finds a scaled quarter is needed it changes the test with a recorded reason.
- Options values: `LIGHTHOUSE_TIME_BUDGET` unset or empty = `fail`, `warn`, `off`, anything else throws naming variable and value. A Stryker marker (`STRYKER_MUTATOR_WORKER`, **unverified until the 1a spike**) or watch (`--watch`, or no `run`/`--run` argument) forces `off`. `--coverage` in argv = coverage kind. The DEVOPS contingency (default `warn` under `CI` until the `vars` context is confirmed in the composite action) would change the "CI with the switch unset" row: edit that one row then, with the reason.
- `compareToAllowList.ts` and `timeBudgetOptions.ts` are throwing stubs with a `__SCAFFOLD__` marker (the RED walking skeleton); DELIVER replaces both bodies and removes the marker.

## Wave: DISTILL / [REF] Scenario list (executable, all activate in slice 1a)

Files: `Lighthouse.Frontend/src/tests/timeBudget/compareToAllowList.test.ts` (30 tests at first writing) and `timeBudgetOptions.test.ts` (13); see the revision section below for the current counts. Both are wrapped in `describe.skip` with a reason; DELIVER removes the skip and works through the failures one test at a time. RED for the right reason, verified: unskipped, 30 of 30 compare tests fail with the stub's "Not yet implemented" (no import or setup errors).

| Group | Tests | Kind |
|---|---|---|
| Unlisted test over budget names file :: test, duration, limit; under passes; exactly at budget passes; every offender named; failed test judged alike | 5 | happy + error |
| First test of a file gets allowance A (3 rows) | 3 | boundary |
| Coverage run scales by F, first test plus A (4 rows) and the reported limit | 5 | boundary |
| Table rows judged individually | 1 | edge |
| Listed test within ceiling passes; over ceiling fails naming it; ceiling scaled in coverage; allowance on a first test; same title in another file judged on budget | 5 | happy + error |
| Stale: test absent, ran under a quarter, at a quarter stays, file gone from disk, file not in run ignored, skipped by filter ignored, bailed run not judged, bailed run still names a slow test | 8 | error + edge |
| Mode: off judges nothing, warn reports but does not fail, clean run in fail mode passes | 3 | mode |
| Options: variable name; unset/empty/CI-unset = fail (3); warn/off (2); unknown value throws; plain vs coverage (2); Stryker marker off; watch off (2); `--run` stays on | 13 | options |

Error/edge share is well above the 40 % target. The limit rule is a finite table (parametrised rows), not an unbounded domain, so no property test.

## Wave: DISTILL / [REF] Walking skeleton for slice 1a (shell checks run by DELIVER, not code)

The pure tests are the inner loop; the user-visible skeleton is the guard working through the real reporter. Record each run's output in `measurement/slice-01a-proof.md`.

| # | Check | Expected |
|---|---|---|
| WS1 | Plant a test that sleeps past T in a scratch file under `src/`; `pnpm test` | Run fails; output names `file :: test`, duration and limit; exit code non-zero |
| WS2 | Same planted test; `CI=true pnpm run sonarreport` | Fails the same way; `sonar-report.xml` and `test-results.json` still written (no reporter dropped) |
| WS3 | Planted test under the Stryker marker (the env var the spike verified) | Nothing judged, run green |
| WS4 | Planted test in watch mode (one cycle, then quit) | Nothing judged |
| WS5 | `LIGHTHOUSE_TIME_BUDGET=warn` with the planted test | Reported as `::warning::` lines, exit 0 |
| WS6 | `LIGHTHOUSE_TIME_BUDGET=off` | Nothing judged, exit 0 |
| WS7 | Planted test on the allow-list with a low ceiling, then exceeded | Fails naming it as over its ceiling |
| WS8 | Allow-list entry for a deleted test | Full run fails naming the stale entry; a filtered run (`-t`, one file) does not |
| WS9 | Plain `pnpm test` | `sonar-report.xml` and `test-results.json` both written (spike question 2, `--reporter=default`) |
| WS10 | `CI=true` with bail on and a planted failing test | Offenders that ran before the bail are named; stale check skipped |

The planted file is removed after each check; none of it is committed.

## Wave: DISTILL / [REF] Slice 1a remaining acceptance checks

- Calibration recorded (B, F, A, ratio, spread) with the D4 rule applied; stop and ask if fewer than 5 CI artifacts.
- Allow-list seeded from calibration: zero unlisted tests over T in the calibration samples; every entry has a reason and ceiling.
- `summarise.py`: top-N shares and counts over thresholds on the existing sample CSV; the existing `Scripts/test-timings/tests` still pass.
- Biome `noSkippedTests` and `noFocusedTests` enabled, offenders fixed or listed by name. The `describe.skip` in `src/tests/timeBudget/` must be gone first (it is the first thing 1a removes).
- `pnpm test` and `pnpm build` clean; ADR-268 and ADR-269 status notes; `docs/ci-learnings.md` entry.

## Wave: DISTILL / [REF] Slice 2 and 3 acceptance (checks, not tests)

Slice 2 (measure only): one table row per D5 candidate, each showing:
1. Alternating A/B, at least 5 pairs, same machine, quiet, fixed `LIGHTHOUSE_TEST_WORKERS`.
2. Median wall falls at least 10 % and the ranges do not overlap.
3. Sorted test-id list identical, every test passes (8,970 at last count).
4. Peak memory not more than 10 % above baseline.
5. 3 consecutive green full runs.
6. 3 green runs with `sequence.shuffle` and different seeds, for every candidate.
7. CI arm: one CI run green and not slower than the baseline CI run (lead-scheduled push).
8. happy-dom only: DataGrid, dialog and user-event tests pass unmodified.
9. A candidate needing test edits is adopted per file or glob and every edit is listed.
10. Nothing is adopted in slice 2.

Slice 3 (adopt): only candidates that passed 1-7 and 9; identical sorted test-id list before and after; all green; 3 green full runs plus 3 shuffled; the node project has its own trimmed setup file if `projects` is adopted; the guard still loads (WS2 and WS9 re-run, since a config edit that drops a reporter is the named bug class); allow-list entries still pass or are re-ceilinged with a reason; `@screenshot` and Playwright untouched.

## Wave: DISTILL / [REF] Slices 4-11 behaviour-free contract (per slice)

Each slice writes `measurement/slice-NN-moved-assertions.md` before its first rewrite commit:

```
Universe: extracted functions/hooks (named); test files edited; allow-list entries deleted. Nothing else.
Preserved set: sorted test ids of touched files before the slice, minus ids listed below as replaced.
Delta: summed seconds of touched files before/after; tests added/removed; entries deleted from slowTests.ts.

| Old test (file :: test) | Assertion text | New location (file) | New test (name) |
|---|---|---|---|
```

Rules for every slice 4-11:
1. Every removed or shrunk `expect(` has a row; an unlisted removal is a defect.
2. Test-id diff before/after: every missing id is in the list; additions are listed.
3. Preserved test ids are unchanged: a surviving test keeps its `file :: name`.
4. An extraction commit changes no test; the unmodified page tests pass at that commit; its diff is a move into a named function or hook.
5. A grep over the slice diff finds no added `.skip`, `.todo`, `.only`, `it.fails`, loosened matcher or longer timeout.
6. Coverage (`--coverage` over the touched directory): line and branch not lower than before; run apart from timing runs.
7. No new product behaviour: no Playwright spec or `@screenshot` file in the diff; no production file beyond the named extractions.
8. Slice close reports summed seconds before/after, allow-list length and wall median (D10).
9. Stryker once at the end (slice 12) over the extracted files: kill rate not lower than before, at least 80 % for new code; frozen code, mutate list and runner include checked first.

Slice pins: 4 and 10 move the real sleeps (600/500 ms) to a fake clock; 6 does the same for the 350 and 300 ms waits; 7 keeps the 143-row render in one test; 9 points the 108 cases at `workItemsDialogContexts`; 11 names its extractions after profiling and keeps the loading and failure looks pinned.

## Wave: DISTILL / [REF] Handoff

DELIVER removes the `describe.skip` of the compare tests and works through them, then the options tests. Pre-DELIVER RED classification: compare tests MISSING_FUNCTIONALITY (30/30); options tests 12 of 13 fail the same way (the variable-name constant test already passes) (superseded: see "Revision after review", 78 of 79). Tier B and the Mandate-12 domain-types module: N/A (no Gherkin or stateful journey in this project).

## Wave: DISTILL / [REF] Revision after review (2026-11-02) - supersedes earlier DISTILL counts and wording where they differ

Test files now in `Lighthouse.Frontend/src/tests/timeBudget/`: `compareToAllowList.test.ts` (47 tests), `timeBudgetOptions.test.ts` (21), `observedRunFromVitest.test.ts` (11): 79 tests, all under `describe.skip`, all activate in slice 1a. Stubs: `compareToAllowList.ts`, `timeBudgetOptions.ts`, `observedRunFromVitest.ts` (throw "Not yet implemented -- RED scaffold"). Unskipped copies: 78 fail on the stub, 1 passes on purpose (the `LIGHTHOUSE_TIME_BUDGET` variable-name constant test, because the constant exists in the stub; the unknown-value test fails with the stub's error rather than the expected message, which is the right RED).

Changes to the signatures and rules recorded above:

- **Vitest-result mapping is its own pure function** `observeVitestRun(outcome: VitestRunOutcome, fileExists): ObservedRun`, with `VitestRunOutcome = { files: { file, tests: { name, state: passed|failed|skipped|pending, durationMs }[] }[]; reason: passed|failed|interrupted }`. It maps `pending` to `skipped`, ms to seconds, marks `firstInFile` on the first test that ran (not skipped) per file, lists every collected file including ones with no tests, and sets `complete = false` only for `interrupted` (bail). The reporter is then a thin shell: Vitest data in, `observeVitestRun`, `compareToAllowList`, print, exit code.
- `ObservedRun` gained `collectedFiles`. A listed test whose file was collected but reported zero tests is stale (`test-absent`); a listed test whose file was not collected is ignored.
- **Matching is on the exact key** `file :: name`: a listed name that a run's name merely starts or ends with, or a listed path that a run's path merely extends, does not match (pinned with table rows, including a listed row beside an unlisted slow row of the same table).
- **Limit semantics:** a test offends only when it takes strictly more than its limit; exactly at the limit passes (pinned at budget, budget plus allowance, ceiling, scaled limit plus allowance, and a listed first test under coverage: ceiling x F + A). A failed test is judged like a passed one, listed or not.
- **Printed lines are pinned exactly:** `<file> :: <test> took <s> s, limit <s> s`; stale: `<file> :: <test> stale allow-list entry (<reason>)`.
- **Annotation lives in the options function:** `resolveTimeBudgetOptions` also returns `annotation`: `::error::` when `GITHUB_ACTIONS` is set and the mode is `fail`, `::warning::` when set and the mode is `warn`, otherwise `none` (also `none` under off, under the Stryker marker, and on a developer machine). All six combinations are pinned.
- **`off` means the reporter judges nothing** (DISTILL's reading). This **supersedes DEVOPS's "off loads nothing"**: `vitest.config.ts` cannot cheaply avoid listing a reporter whose mode is decided from the environment at config time without a second code path, and a reporter that judges nothing is equivalent for the run (no output, exit code untouched). WS6 below checks the equivalent: nothing reported, exit 0.
- **`CI` set means never watch** (safe reading): a CI command with no `run` argument still enforces; on a developer machine, no `run`/`--run` argument means watch and the guard is off. Pinned both ways.
- **Consequence of "under a quarter" = B/4 unscaled, also under coverage:** a test that takes 0.5 s plain and about 1.0 s under coverage is not retired by a coverage run (pinned: 0.8 s in a coverage run stays listed), so entries are retired by plain `pnpm test` runs only; the coverage run can only keep them, never delete them. This is the safe direction (load and coverage only make tests slower). **An incomplete run skips every stale check, including "file gone from disk"** (pinned).

### Slice 2 CI arm (decision, M5)

Adoption rests on the **local alternating A/B** (rule items 1-6). The CI arm is a sanity check plus a direction, not an adoption proof, because one CI run cannot separate a candidate from CI noise: its Vitest `Duration` is compared with the **930.9 s median of the 7 saved baseline runs** under the same 10 % rule (a candidate run more than 10 % above 930.9 s blocks adoption; a run at or below it is consistent; a single run in between is "direction only" and does not block). Adopting on CI evidence alone would need at least 3 candidate runs; this Story does not ask for that, and says so in the slice-2 table.

### Preserved-id lists (M6)

Preserved-id lists come **from a run, never from source**: after a run that writes `test-results.json`, the list is `file :: fullName` for every `assertionResults[].fullName` (file = the enclosing result's relative path), sorted, saved before and after the slice. Names of `it.each` rows and template-string titles exist only in a run, so a source scan is not accepted.

### Slice 2 and 3 refinements (m9)

- Item 8 (happy-dom fidelity): the list is every test file matching `**/*DataGrid*`, `**/*Dialog*` and the user-event suites (`grep -l "userEvent" src --include=*.test.tsx`), recorded by name in the slice-2 table, run unmodified. Per D5, happy-dom is measured last and only if (a)+(b) leave environment cost above 10 % of worker time.
- Peak memory (item 4) is sampled with `measurement/scripts/measure.sh` (the sampler of the DESIGN measurement), the same one for A and B.

### Slices 4-11 rules, sharpened (m9)

- Rule 1 counting method: per touched test file, `git diff -U0 <before> -- <file> | grep -c '^-.*expect('` must be matched by rows in the moved-assertions list for that file. The **"Assertion text" column is the removed `expect(...)` line verbatim**.
- Rule 5, "loosened matcher", is this set: `toEqual` to `toMatchObject` or `objectContaining`; an exact `toBe`/`toEqual` to `toContain` or a regex; an exact count to `toBeGreaterThan`/`toBeGreaterThanOrEqual`; a removed `not.`; plus any other weakening a reviewer judges so.

### Walking-skeleton note

WS6 (`LIGHTHOUSE_TIME_BUDGET=off`): nothing reported and exit 0; the reporter is loaded and judges nothing. WS5 adds: under `GITHUB_ACTIONS=true` the lines carry `::warning::`; in the enforcing case `::error::`.

## Wave: DELIVER / [REF] Roadmap notes (2026-11-03)

Roadmap: `deliver/roadmap.json`, 13 phases (one per slice: 01 = 1a, 02 = 1b, 03 = 2, 04 = 3, 05-12 = slices 4-11, 13 = 12),
41 steps. Rigor `standard` (model passed explicitly per dispatch). Steps named **QUIET MACHINE** (01-02, 01-08, 03-02, 03-03,
04-02, 13-02) are lead-run: the lead pauses and asks the maintainer for a quiet system before starting each. Other lead steps
(02-02, 03-04, 13-03, 13-04) need no quiet machine. Per-step detail, keyed by step id:

### Phase 01 - slice 1a
- **01-01 Spike.** Five answers, each with the command and output: (1) Vitest 5 per-test duration hook and end-of-run set, and
  how a failing exit code is set (candidates `onTestCaseResult`, `onTestRunEnd`); (2) whether `--reporter=default` in the
  `test` script replaces config reporters (WS9 is the check); (3) the Stryker worker marker (candidate
  `STRYKER_MUTATOR_WORKER`, unverified); (4) whether the composite action can read the `vars` context; (5) whether the end-of-run
  hook fires with the full list on a `bail` run. Answers can change the mechanics of 01-06, 01-07 and 02-01, not the outcome.
- **01-02 Calibration.** Local plain vs `--coverage` on the same machine separates machine effect from coverage effect; three
  oversubscribed runs (workers = cores) feed the flake bound; the 5+ CI artifacts give F per test (join on `file :: test name`);
  A from the local JSON in file order. B by the D4 rule (smallest of 1, 2, 3 s such that every offender sits in a file a slice names
  or carries its own reason). No fallback value if artifacts are missing. The Duration-from-log command is confirmed against
  a saved run, so KPI 16's first "after" number can be trusted later.
- **01-03/01-04 Compare (two parts), 01-05 Observe, 01-06 Options.** Stale rule is B/4 unscaled; a coverage run can keep an entry, never retire it. The CI-set-and-switch-unset row reads fail unless the reporter-wiring step moves the wiring to 1b. Pure steps: Each removes its file's `describe.skip` and the reason constant first, then goes green one
  test at a time. Order compare (47), observe (11, imports types from compare), options (21, needs the 01-01 marker answer).
  Test-file edits beyond removing the skip are allowed only where the spike shows a pinned row wrong, with a recorded reason
  (the DEVOPS contingency row for "CI with the switch unset" is the known candidate). Scaffold markers are removed from the stubs.
- **01-07 Reporter and wiring.** Prints a start line (runner facts) and a last line (verdict). Thin shell only; the options function decides everything from environment and argv; the
  calibrated B, A, F enter through the options. `slowTests.ts` is created empty here so the reporter compiles; 01-08 fills it.
  `package.json` changes only if 01-01 answer (2) says the flag drops reporters. `action.yml` gets the one env line (not a `ci*.yml`
  edit, so no forced full suite); if `vars` is unreadable in a composite action the wiring moves to 02-01.
- **01-08 Seed.** One entry per test over the threshold in the calibration samples; reason in plain words, ceiling in quiet-local
  seconds, no ids of stories or slices in reasons.
- **01-09 Proof (WS1-WS10).** The DISTILL table has ten checks, not six. Output recorded in `measurement/slice-01a-proof.md`;
  re-run WS2/WS9 after any reporter or options change (04-02 does).
- **01-10 summarise.py.** Extend, no second script; the sample CSV test gains cases; reproducing the DESIGN figures on the
  baseline CSV is the sanity check.
- **01-11 Docs.** Word per DEVOPS fact 5: the per-file half sums test time only; the import/environment/transform half is
  guarded by nothing.
- **01-12 Biome.** Count offenders of `noSkippedTests`/`noFocusedTests` first; fix, or suppress narrowly with a reason. Depends on
  the three pure steps because it needs the `describe.skip` gone.

### Phase 02 - slice 1b
- **02-01.** `--warn-only` becomes `LIGHTHOUSE_BUDGET_WARN_ONLY`-controlled (unset = enforce). Run `check_budget.py` on the 7
  saved artifacts first; re-derive the nine ceilings only if it fails (D1), with the new test case. Backend call untouched.
- **02-02.** Lead push. The `ci_frontend.yml` edit forces the full backend, E2E and live-connector suites (DEVOPS fact 6); the
  first CI run's reporter line records the real runner processor count and worker count.

### Phase 03 - slice 2 (measure only)
- **03-01.** Two worktrees, scripts under `measurement/scripts/`, the node-project trimmed setup in the candidate arm only, the
  commit that added MUI inlining read, CI runner facts read, per-file split and a two-file CPU profile.
- **03-02.** Candidates a, b, d, e of D5 per the A/B rule (median -10 %, non-overlapping ranges, identical id list from
  saved JSONs, memory +10 % at most, 3 green, 3 shuffled). **03-03** happy-dom only if environment cost stays above 10 % of
  worker time. **03-04** the CI arm: direction only (a run > 10 % above 930.9 s blocks, at or below is consistent, between is
  direction only); one full verify run with live connectors, lead-scheduled. **03-05** the verdict table; nothing adopted.

### Phase 04 - slice 3
- **04-01.** One option per commit; only candidates marked adopted. Bounded change: the reporter entry plus the adopted option
  entries. The WS2/WS9 re-run (a config edit that drops a reporter is the named bug class) is done in 04-02. `ARCHITECTURE.md` line on the test
  stack changes only if the environment changes. If nothing passed, the step records that and the phase ends.
- **04-02 (QUIET MACHINE).** WS2/WS9 re-run, 3 plain + 3 shuffled green, one `CI=true pnpm run sonarreport`, memory vs 4.6-4.8 GB, alternating before/after wall.

### Phases 05-12 - slices 4-11 (rewrite slices)
Common shape, two steps each: `NN-01` declares the universe (extractions, edited tests, allow-list entries), saves the before
id list from a results file, before seconds and before coverage; `NN-02` does the extraction as its own refactor commit (no test
change; unmodified page tests pass), then the test rewrite commit with the allow-list deletions. Rule 1 counting per file:
`git diff -U0 <before> -- <file> | grep -c '^-.*expect('` matched by rows. Order may change or a slice may be dropped by measured
payoff; slice 10 (`11-*`) follows slice 4 (`05-02`); slice 11 (`12-*`) follows the `BaseMetricsView` split if it landed.
- **05 (slice 4).** Autosave debounce and rule gating; 600 ms (`.refinement`) and 500 ms x2 (`.test`) sleeps and 300 ms
  debounce waits to a fake clock.
- **06 (slice 5).** 50-row pick limit, picked count, select-all as functions over a row list; the 101-row render becomes
  a direct check plus one wiring test. Measured (indicative, not quiet, 5 alternating pairs): summed test seconds of the
  touched files -22.7 %; allow-list 28 to 23.
- **07 (slice 6).** Wizard step and validation logic; 350 ms (`BulkReportWizard.openings`) and 300 ms (`BulkReportWizard`) waits
  and the report view's one-second retry delay to fake timers; three extraction files with 51 direct tests; one walk removed.
  Measured (indicative, not quiet, 5 alternating pairs): summed test seconds of the touched files -12.4 % (pairs 1 to 4,
  before the parallel load fell away: -4.4 %); allow-list 23 to 23.
- **08 (slice 7).** Line arithmetic and heading wording extracted (`refinementPaging.ts`, `refinementHeading.ts`) with 48 direct
  tests; exactly one test renders the 143-row list (extended with the page-2 line and numbers); nine large-list tests and the
  300-row test removed with rows. Measured (indicative, not quiet, 5 alternating pairs): summed test seconds of the four
  files -34.5 % (the two edited files -65.7 %); allow-list 23 to 13.
- **09 (slice 8).** Field-change and default resolution per tracker type; every tracker field set a test relied on is listed
  at `09-01` so none is lost.
- **10 (slice 9).** No extraction expected: each of the 108 cases is kept or has a row naming the function test making the same
  assertion; paging extraction only if `10-01` names it.
- **11 (slice 10).** Reuses slice 4's helpers; three 500 ms sleeps (`ModifyProjectSettings.test.tsx` lines 590, 617, 722 at
  the baseline) to a fake clock.
- **12 (slice 11).** Profile first (the budget file says "not profiled yet"); extractions named after profiling; loading and
  failure looks stay pinned by named tests (existing `BaseMetricsView.loading` tests keep their role).

### Phase 13 - slice 12
- **13-01.** Declaration: names the tail (every `slowTests.ts` entry still listed after the last shipped rewrite slice, plus `DataGridBase.paging` and `featureChildItemsDialog`), universe, before ids, seconds, coverage.
- **13-02.** Sweep. Allow-list review (delete fixed or no-longer-slow entries), the two named files (`DataGridBase.paging`,
  `featureChildItemsDialog`, about 17 s) and the tail, under the same contract; lower the budget only by the D4 rule.
- **13-03 (QUIET MACHINE).** Alternating baseline vs final, median of at least 5 pairs, on frozen code.
- **13-04 (QUIET MACHINE).** Stryker once, after 13-03 (it loads the machine and any edit shifts the staleness of earlier numbers); mutate list
  and runner include checked first; survivors killed with a follow-up test commit or listed, no hours of reruns.
- **13-05.** CI after-measurement by job result, Duration median from job logs (KPI 16), KPI 14 zero unlisted offenders.
- **13-06.** Closing report, T4 hand-off (frontend half leaves T4 at its pick-up), ARCHITECTURE.md/ADR-270 "no concept added".

### Roadmap build notes (contradictions found while reading the inputs)
1. The walking skeleton is **WS1-WS10** in DISTILL, not WS1-WS6; the roadmap uses ten.
2. DEVOPS "`off` loads nothing" is superseded by DISTILL's "`off` judges nothing" (kept as in the revision section).
3. DESIGN D1/slice table say `--warn-only` is **removed** from the frontend call; DEVOPS makes it variable-controlled
   (`LIGHTHOUSE_BUDGET_WARN_ONLY`, unset = enforce). The roadmap follows DEVOPS.
4. DESIGN D3/D4 read "under a quarter of the budget" as scaled; DISTILL pins `B/4` unscaled, so coverage runs can keep entries
   but never retire them; the roadmap follows DISTILL.
5. D2's guard-off conditions (Stryker, watch) vs DISTILL "`CI` set means never watch": compatible, roadmap follows DISTILL.
6. DEVOPS cites story 6461's `measure.sh` for the local arm; DISTILL cites `measurement/scripts/measure.sh`; 03-01 creates or
   copies the latter.
7. Slice 11's Stryker mention: DEVOPS lists slices 4-8 and 11 for Stryker; DISTILL lists "extracted files"; the roadmap uses the
   extracted production files of every shipped slice (13-04).

### Revision after review (2026-11-03)

Supersedes the step ids, step counts and QUIET MACHINE list above where they differ. The roadmap now has 13 phases and **44 steps**.

**Renumbering.** Phase 01: compare step split (01-03 part one, 01-04 part two), then observe 01-05, options 01-06, reporter 01-07, seed 01-08, proof 01-09, summarise 01-10, docs 01-11, Biome 01-12. Phase 04 gains 04-03. Phase 13: 13-01 declaration (new), 13-02 sweep, 13-03 final runs, 13-04 Stryker, 13-05 CI after-measurement, 13-06 finalize. Per-step notes above are re-keyed to the new ids (old 01-03..01-11 became 01-05..01-12, old 13-01..13-05 became 13-02..13-06).

**QUIET MACHINE steps now:** 01-02, 01-09, 03-01, 03-02, 03-03, 04-02, 13-03, 13-04. No rewrite slice (phases 05-12) needs one.

**What changed**
1. **Per-slice figures without a quiet machine (amends DISTILL rule 8, D10 and the DEVOPS local-arm line).** Slices 4-11 (and the 13-02 sweep) measure only the touched test files, before and after alternating (3 pairs, same minute, same worker count), so load from a parallel session hits both arms alike. The close report gives summed seconds before/after, relative change, allow-list length, tests added/removed, and replaces the slice's estimate with the measured relative figure. The NN-01 declarations record the "before" (pre-slice commit, ids, seconds, coverage). The suite wall median is a 13-03-only figure (quiet). 12-01's profiling uses the same method.
2. **03-01 is QUIET MACHINE** (its per-file split and CPU profile are part of the slice 2 measurement). The A/B and happy-dom steps stay QUIET MACHINE.
3. **CI reads (amends the DEVOPS per-slice CI line).** CI after-measurement is no longer per slice. A passive read is added after phase 04 (new step 04-03), because configuration is the change expected to move CI wall time; 13-05 reads the end state against both the baseline and the 04-03 read.
4. **02-02** depends also on 01-07, 01-08 and 01-09; it carries a flaky-guard window of the first 5 main runs after the push (switch to `warn` and re-calibrate, no retry in the reporter).
5. **Phase 13** depends on 12-02 (the last rewrite step); if later slices are dropped, re-point to the last shipped one. A declaration step 13-01 names the tail (every `slowTests.ts` entry still listed after the last shipped rewrite slice, plus `DataGridBase.paging` and `featureChildItemsDialog`) before the sweep rewrite 13-02.
6. **No slice ids in `slowTests.ts`:** entries carry a plain-language reason and a ceiling only; the retirement mapping (entry to slice) is a table in this file, written by 01-08 and kept current by each slice.
7. **Seeding (01-08)** uses 01-02's saved calibration data, with no fresh full run in the crafter step; the confirming guard-on full run moved into the quiet 01-09.
8. **04-01 no longer re-runs WS2/WS9;** that moved into the quiet 04-02.
9. **Compare step split** into two steps (01-03: listed/unlisted/limits/exact-key; 01-04: stale/bailed/modes). Part one keeps the stale and mode groups skipped by their own inner skip until part two; observe, options and the proof stay whole.
10. **01-02:** the oversubscribed runs override `LIGHTHOUSE_TEST_WORKERS=12` for those runs only.
11. **Every rewrite step's criteria** now copy rule 7 and the universe check: `git diff --name-only` is a subset of the declared universe, with no other production file, Playwright spec, screenshot file or `setupTests.ts`.
12. **01-12:** remaining Biome offenders get a narrow line suppression with a reason (Biome's way), never a wider one.
13. **New files** are marked `(create)` in `files_to_modify` (reporter, `slowTests.ts`, node setup file, measurement notes).
14. **12-01:** FE5 (the `BaseMetricsView` split) is named an external predecessor; if it has not landed, `BaseMetricsView` is left alone and ratcheted.
15. **13-04 (Stryker) is QUIET MACHINE.**
16. **03-04:** one CI arm per locally passing candidate, one run each; ties keep the simpler configuration.
17. **Rigor:** `standard` for the whole test track was agreed at its AFK entry (recorded in the T1 workspace); no per-slice raise.
18. **Contradictions list:** the DEVOPS rollback wording ("off loads nothing") is superseded by DISTILL ("off judges nothing"), as for the options step; resolution 3 (`--warn-only` becomes variable-controlled) follows DEVOPS's revised-after-review text.
19. `validation.status` is `pending_review`.

Second revision (minor fixes): the stale rule in 01-03/01-04 is pinned to B/4 unscaled (a coverage run keeps but never retires an entry); 01-06 and 01-07 state the same conditional for CI set with the switch unset (fail, unless the vars contingency moves the wiring to 1b, with the recorded reason); 01-07 prints a start line (runner facts) and a last line (verdict) and 02-02 checks both; 03-02 runs `CI=true pnpm run sonarreport` once (slice 1 has it in 01-09, slice 3 in 04-02); 11-02 gets the extraction-commit rule and 10-02 says why slice 9 needs none (it points at the existing `workItemsDialogContexts`) and applies the rule if 10-01 names a paging extraction; contradiction added: DESIGN says re-derive the per-file ceilings from 5 runs, DEVOPS and the roadmap use the 7 saved runs; resolution: use all 7 (more evidence, the floor of 5 is met).

Previously listed as not acted on (now acted on, see above): m2, m3, m5 (read as the M5 allow-list wording), m7 and m8, and the "(a) list it" part of m16.

**Second re-check fixes (2026-11-03).**
1. **Crafter runs are not timing runs.** Once the reporter is wired (from 01-07 on), every crafter step's full `pnpm test` runs with `LIGHTHOUSE_TIME_BUDGET=warn`: a parallel session's load can produce false offenders, and crafter runs prove correctness and test ids only. Timing verdicts come only from QUIET MACHINE steps. Written as P-crafter-warn and added to the criteria of every step; 04-01's id-list and all-pass runs use `warn` with no timing judgement, and the enforcing run stays in 04-02.
2. **Rewrite-slice figures are an explicit exception to the quiet rule.** Close figures (every NN-02, the 12-01 profile, the 13-02 sweep) are not quiet-machine measurements: alternating before/after runs of the same touched files within minutes put the same load on both arms, so the relative change is meaningful while absolute seconds are not. Every close report labels the figure "indicative (not quiet)", and the pairs are raised from 3 to 5 everywhere (this also corrects "3 pairs" in revision item 1 above).
3. **03-04 now also depends on 03-03.**
4. **Reference correction:** resolution 3 (`--warn-only` becomes variable-controlled) follows the DEVOPS "Pipeline changes per slice" table (about line 474, revised after review in commit a637162de). Open item: **maintainer to confirm: reverses DESIGN D1's removal of `--warn-only`.**
5. **Rigor citation:** the T1 record (the T1 feature-delta, about line 819) covers the whole test track, FE7 included; no change.
6. **@sizing-review-needed: 01-05, 01-06, 01-09 (kept whole by lead decision).**

## Wave: DELIVER / [REF] Calibration (step 01-02, 2026-11-03, quiet machine)

B = **2 s** (unchanged; DESIGN's provisional value holds by the D4 rule), F = **3.0** (per-test CI/local p90 2.61),
A = **0.31 s**. At B = 2 s, 30 tests are over the threshold in some sample: 27 in files a rewrite slice names, 3 with
their own reason (DashboardHeader popover, ReportsPage.usageData Retry, ThirdPartyPackagesSection.paging sort/filter).
Local coverage alone is 1.35x per test (median); an oversubscribed machine adds 1.66x. Seed for 01-08:
`measurement/calibration-2026-11-03/seed-allow-list.json` (ceilings 2.5-5.0 s). Detail and the CI log Duration command:
`measurement/slice-01a-calibration.md`.

## Wave: DELIVER / [REF] Allow-list retirement map (seeded 2026-11-03)

The thirty tests the time budget lets through today, each with its ceiling in seconds on a quiet machine and what is expected to retire it.

| file | test | ceiling (s) | expected to retire it |
|---|---|---|---|
| src/components/Common/DataGrid/DataGridBase.paging.test.tsx | Turning a page brings the top of the list into view never scrolls for a page change the... | 3.0 | sweep |
| src/components/Common/ProjectSettings/ModifyProjectSettings.queryCommit.test.tsx | ModifyProjectSettings query field saves the edited query once, after the field is left | 3.0 | slice 10 |
| src/components/Common/Team/ModifyTeamSettings.queryCommit.test.tsx | ModifyTeamSettings query field saves a changed name after the debounce without anyone l... | 2.5 | slice 4 |
| src/components/Common/Team/ModifyTeamSettings.queryCommit.test.tsx | ModifyTeamSettings query field saves the edited query once, after the field is left | 3.0 | slice 4 |
| src/components/Common/Team/ModifyTeamSettings.queryCommit.test.tsx | ModifyTeamSettings query field saves the latest edit after the save already on its way,... | 3.0 | slice 4 |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx | Readiness in the Refinement section of a Team's settings switches a rule back on at the... | 2.5 | slice 4 |
| src/components/Common/Team/ModifyTeamSettings.readiness.test.tsx | Readiness in the Refinement section of a Team's settings switches a rule that was refus... | 3.0 | slice 4 |
| src/components/Common/Team/ModifyTeamSettings.refinementNeed.test.tsx | Stage rules in the Refinement section of a Team's settings keeps an edited Ready rule w... | 2.5 | slice 4 |
| src/components/Common/Team/ModifyTeamSettings.refinementNeed.test.tsx | Stage rules in the Refinement section of a Team's settings saves a changed Ready rule a... | 2.5 | slice 4 |
| src/components/Common/WorkItemsDialog/WorkItemsDialog.paging.test.tsx | A Work Items dialog with more than 100 Work Items Priya, on the second page of 243, fil... | 3.5 | slice 9 |
| src/pages/Common/MetricsView/DashboardHeader.test.tsx | DashboardHeader shows label and formatted date range and opens popover which calls hand... | 2.5 | stays: renders the dashboard header and opens its date popover, which loads the date picker the first time |
| src/pages/Reports/ReportsPage.combinedPdf.test.tsx | One PDF of several reports above 50 picked, Export PDF is disabled and says at most 50 | 3.0 | slice 5 |
| src/pages/Reports/ReportsPage.combinedPdf.test.tsx | One PDF of several reports exactly 50 picked may still be exported as one PDF | 2.5 | slice 5 |
| src/pages/Reports/ReportsPage.removal.test.tsx | Picking reports Select all picks every row, on every page | 4.5 | slice 5 |
| src/pages/Reports/ReportsPage.removal.test.tsx | Picking reports above 50 picked, Remove is disabled and says at most 50 | 3.0 | slice 5 |
| src/pages/Reports/ReportsPage.removal.test.tsx | Picking reports exactly 50 picked may still be removed | 2.5 | slice 5 |
| src/pages/Reports/ReportsPage.removal.test.tsx | Remove several reports at once a coach removes three trial reports on two Teams and a P... | 2.5 | slice 5 |
| src/pages/Reports/ReportsPage.usageData.test.tsx | What the Reports page tells the usage data a Retry and a Leave it out on one PDF of sev... | 2.5 | stays: renders the Reports page, picks several reports and walks through a failed export, a retry and leaving a report out |
| src/pages/Settings/SystemInfo/ThirdPartyPackagesSection.paging.test.tsx | Third-party packages pages like every other list keeps the reader's sort and column fil... | 2.5 | stays: renders a long list of packages and walks through sorting, a column filter and a search that returns to the first page |
| src/pages/Teams/Detail/Refinement/RefinementView.paging.test.tsx | Team Gravity's 143 Work Items in refinement can all be reached Jonas pages to GR-0131, ... | 5.0 | slice 7 |
| src/pages/Teams/Detail/Refinement/RefinementView.paging.test.tsx | Team Gravity's 143 Work Items in refinement can all be reached with 0 needed, the line ... | 2.5 | slice 7 |
| src/pages/Teams/Detail/Refinement/RefinementView.paging.test.tsx | Team Gravity's 143 Work Items in refinement can all be reached with 100 needed, the lin... | 2.5 | slice 7 |
| src/pages/Teams/Detail/Refinement/RefinementView.paging.test.tsx | Team Gravity's 143 Work Items in refinement can all be reached with 101 needed, the lin... | 2.5 | slice 7 |
| src/pages/Teams/Detail/Refinement/RefinementView.paging.test.tsx | Team Gravity's 143 Work Items in refinement can all be reached with 120 needed, a State... | 4.5 | slice 7 |
| src/pages/Teams/Detail/Refinement/RefinementView.paging.test.tsx | Team Gravity's 143 Work Items in refinement can all be reached with 120 needed, numbers... | 2.5 | slice 7 |
| src/pages/Teams/Detail/Refinement/RefinementView.paging.test.tsx | Team Gravity's 143 Work Items in refinement can all be reached with 120 needed, the lin... | 2.5 | slice 7 |
| src/pages/Teams/Detail/Refinement/RefinementView.paging.test.tsx | Team Gravity's 143 Work Items in refinement can all be reached with 143 needed, the lin... | 2.5 | slice 7 |
| src/pages/Teams/Detail/Refinement/RefinementView.paging.test.tsx | Team Gravity's 143 Work Items in refinement can all be reached with 150 needed, the lin... | 2.5 | slice 7 |
| src/pages/Teams/Detail/Refinement/RefinementView.paging.test.tsx | Team Gravity's 143 Work Items in refinement can all be reached with 20 needed, the line... | 2.5 | slice 7 |
| src/pages/Teams/Detail/Refinement/RefinementView.test.tsx | The Refinement tab lists the Work Items in refinement handles three hundred Work Items,... | 3.0 | slice 7 |

## Wave: DELIVER / [REF] Per-file budget made failing (2026-11-03)

The frontend per-file time budget now fails the build; repository variable `LIGHTHOUSE_BUDGET_WARN_ONLY=true` switches it back to a warning.

Headroom evidence. Method: for each of the 7 saved CI coverage-run timing CSVs of 2026-11-02, sum `duration_ms` per test file, take the worst of the 7 runs per file, and compare with `budget_seconds` (63 s default) or the file's `ceiling_seconds` in `Scripts/test-timings/test-time-budget.json`. The budget's seconds are CI coverage-run seconds (derived from those same CI artifacts), so no coverage factor applies.

- Worst unlisted file: `src/pages/Reports/ReportsPage.removal.test.tsx` at 48.0 s of 63 s (76 %).
- Every listed file is at most 50 % of its ceiling.

## Wave: DELIVER / [REF] First CI run with both frontend budgets enforcing (step 02-02, 2026-11-03)

Pushed `45db5ed30..c98f49b05` (slices 1a and 1b). CI run 41619566770, frontend job 125861697719: **green**.

- Reporter start line: `Time budget: fail mode, coverage run, default workers, 4 processors` (the runner has 4 processors; Vitest's default
  gives it 3 workers).
- Reporter last line: `Time budget: fail mode, budget 2 s, coverage factor 3, first-test allowance 0.31 s, 0 offenders`.
- Vitest `Duration 672.85s` (baseline median of 7 runs 930.9 s, range 677-1,092 s: within the range, no verdict).
- The per-file budget step ran without `--warn-only` and passed.
- The run is red on one backend test, `JiraOriginalNamesLiveTest.The_demo_projects_work_items_have_the_same_states_types_and_dates_through_both_accounts`
  (live Jira: two newly created items seen through the English account but not yet through the German one). It failed the same way on main
  runs 41541784161, 41571118814 and 41571588500 before this push; unrelated to this Story, reported to the maintainer.
- Flaky-guard window: the next 5 main runs whose frontend job runs are watched for an unlisted test over the budget; none so far.
- Maintainer decision (2026-11-03): a local failure that only comes from machine load is answered by a re-run; the guard stays enforcing
  locally (no load scaling, no local warn default).
- Maintainer decision (2026-11-03): with no quiet window available, the rewrite slices start before the configuration spike; their
  figures are indicative, and the spike later measures on top of them.

## Wave: DELIVER / [REF] Slice 2 verdict: no configuration change passes (steps 03-01 to 03-05, 2026-11-03, quiet machine)

Measured on a quiet machine from 17:00 to 20:00, both arms at `614772515` (main with rewrite slices 4 to 6), 6 workers.
Full table, raw data and notes: `measurement/slice-02-ab-table.md`; facts, split and profiles: `measurement/slice-02-facts.md`.

**Fixed overhead, measured:** of 1,608 s summed worker time in one full baseline run (593 files, 9,191 tests, 279 s wall),
53 % is fixed cost per file before a test runs (collect, i.e. import, 30.3 %; jsdom set-up 18.5 %; setup files 4.5 %) and
46.4 % is the tests themselves.

| Candidate | Verdict | Failing rule item and the figure |
|---|---|---|
| a node project for DOM-free `.ts` files | Rejected | Wall: +2.0 % median over 5 pairs (ranges overlap). Memory -0.7 %, ids identical, 3 plain runs green |
| b0 MUI not inlined | Rejected | Wall: +10.5 % (first pass) |
| b1 only `@mui/x-*` inlined | Rejected | Wall: +10.1 % (first pass) |
| b2 MUI pre-bundled by the optimizer | Rejected | Wall: +13.3 % (first pass) |
| d node project with `isolate: false` | Rejected | Ids: 173 failing tests in its smoke run, never timed |
| e1 `pool: "forks"` | Rejected | Wall: +8.9 % (first pass) |
| e2 `pool: "vmThreads"` | Rejected | Ids: 22 missing, 68 failed; memory +298 % (17.1 GB). Wall was -41 % |
| f 12 workers | Rejected | Memory: +63 %; wall -5.7 %, short of -10 %, and CI has 4 cores |
| g `fsModuleCache` | Rejected | Wall: 0.0 % median over 3 pairs once the cache is warm |
| c happy-dom (03-03, measured because environment was 18.5 % > 10 %) | Rejected | Ids: 28 missing, 77 failed in 21 files; memory +30 %. Wall -7.9 % on one pair |

**Saving available from the winners: none.** Slice 3 adopts nothing: 04-01 records that and makes no change, 04-02 has no
configuration to verify, 04-03 has no CI payoff to read. The CI arm (03-04) was skipped for the same reason.
The speed of this suite is in what the tests do: in CI run 41691382136, `.tsx` component tests (median 74 ms) take 97 % of the
summed test time and `.ts` logic tests (median 0.3 ms) 3 %. The rewrite slices are where the remaining payoff is.

Found in passing (follow-ups, not in this Story's scope): two order-dependent tests in `PortfolioDetail.test.tsx` fail under
`--sequence.shuffle` seeds 11 and 33; four Reports and settings tests hit the 5 s test timeout only on a heavily loaded machine.
