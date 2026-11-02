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
| D3 | Where the allow-list lives, its key, how it shrinks | (a) A TypeScript data module beside the reporter. (b) JSON in `Scripts/test-timings/`. (c) Inline in an enforcement test like `ALLOWED_LEAKS`. | (a): `Lighthouse.Frontend/src/tests/timeBudget/slowTests.ts`; key = `relative file path :: full test name`; each entry has a one-line reason and a **ceiling** (quiet-local seconds) | The reporter runs in the frontend project and imports it. Every `it.each` row is its own test with its own name, listed and checked individually. **Failure directions:** (1) an unlisted test over the threshold fails, named. (2) A listed test over its own ceiling fails, named (a slow test may not get slower). (3) Stale entry, defined **against what ran, not against the source** (recommended; a source scan cannot expand `it.each` titles and template strings): for an entry whose file was part of this run, the test must have run, and not in under a quarter of the budget; an entry whose file no longer exists on disk is stale; entries for files outside a filtered run are ignored; a bailed or incomplete run skips the check. "Under a quarter" is safe because load only makes tests slower, never faster. The alternative, running the stale check only on full runs, was rejected: it needs the reporter to know a run was unfiltered, which Vitest does not tell it reliably. **How a reviewer sees growth:** an added entry is a line in `slowTests.ts`, with its reason and ceiling, in the PR diff; nothing else can add one. **Departure from the ADRs, recorded:** ADR-268 says "file or type" and fails on a stale entry; here the key is a test, the stale rule is the one above, and an entry has a ceiling. ADR-269 puts the list in an enforcement test over a tool's output; here a reporter reads the run itself. Slice 1 writes this as a status note on ADR-268 and ADR-269 (or one short new ADR, next free number, if the note grows past a paragraph). |
| D4 | The budget model and its value | 1 s / 2 s / 3 s / 5 s | **Rule fixed now, value derived in slice 1; provisionally 2 s.** B is the budget in seconds on a quiet machine under `pnpm test`. The enforced threshold in a run is `T = B x F (+ A for the first test in its file)`. F = 1 for local `pnpm test`; for the CI coverage run F is the 90th percentile of the per-test ratio (CI coverage-run duration over local plain-run duration) over tests that took at least 0.1 s locally, rounded up to the next 0.5. A is the measured extra cost of the first test of a file (cold module and first render), taken as the 90th percentile over files. Slice 1 measures: the ratio of local `--coverage` to local plain on the same machine (separates the coverage effect from the machine effect), the CI ratio from at least 5 CI artifacts, and A. **Value of B:** the smallest of {1, 2, 3} s for which every test over T in the calibration samples (the quiet local run, 3 oversubscribed local runs with workers = cores, the 5 CI artifacts) sits in a file that slices 4-11 name, so each entry has a slice that retires it; entries outside those families carry their own reason. (The earlier cap of 60 entries is dropped: it had no reason; the rule above ties the list's length to the work planned.) 5 s is excluded (it is the timeout). **Flake bound:** T is raised until the calibration samples show **zero** unlisted tests over it; a residual single-sample false failure on an unmeasured machine is possible, and is answered by a re-run, not by a retry in the reporter; the raised value and the observed spread are written into the guard's comment. **If the CI artifacts are missing or fewer than 5, slice 1 stops and asks for them; there is no fallback value.** | Local data: 4 tests over 2 s, 50 over 1 s, 252 over 0.5 s. The old "CI is 3-9x slower" was an **upper bound read off the budget file's ceilings** (they carry margin and are CI coverage-run seconds), not a measurement; slice 1 measures the ratio. CI artifacts come from the coverage run, so they calibrate the CI run kind directly. The value in this row is replaced by the measured one when slice 1 lands. |
| D5 | Environment and pool options | (a) `projects` split: a `node`-environment project for tests that touch no DOM (candidates: at most the 184 `.test.ts` files, an **upper bound**: some use `renderHook`, `localStorage` or the DOM). (b) Stop inlining MUI (`server.deps.inline`), or let `deps.optimizer` pre-bundle it. (c) happy-dom in place of jsdom for the DOM project (new dependency, MIT). (d) `isolate: false` for the node project only. (e) `pool: forks` or `vmThreads`. | **All five are measured in slice 2, none adopted before.** Order of expected payoff against risk: (a), (b), (d), (e), (c). | Each is a configuration change measured against the unchanged baseline. **A/B rule, decided now:** alternating A and B runs, at least 5 pairs, same machine, quiet, same worker count; adopt a candidate only if (1) median wall falls at least **10 %** (the two baseline runs differ by 3.3 %) and the ranges do not overlap, (2) the sorted test-id list is identical and every test passes, (3) peak memory is not more than 10 % higher than baseline, (4) 3 consecutive green full runs, (5) **for `isolate: false` (d), also 3 green runs with `sequence.shuffle` and different seeds**, because shared module state shows only when order changes, (6) **a CI arm**: one CI run with the candidate (the push or workflow dispatch is a lead step, not an AFK one) is green and not slower than the baseline CI run before the candidate is adopted. **Node project:** `setupTests.ts` also patches DOM things (`Element.prototype.scrollIntoView`, `ResizeObserver`, a `localStorage` mock) that do not exist in a node environment, so the node project needs its own trimmed setup file (locale pins and console mocks only), written and measured in slice 2. **Unknowns slice 2 reads from the workflow and records, not guesses:** the CI runner's vCPU count and memory (`runs-on` in `ci_frontend.yml`) and how many workers CI's default gives, since CI sets no cap. A candidate that needs any test edited is adopted per file or glob, and each edit is listed. Fidelity gate for (c): DataGrid, dialog and user-event tests must pass unmodified; run last, and only if (a)+(b) leave environment cost above 10 % of worker time. The reason `@mui/` is inlined is not in the repo; the slice reads the commit that added it first. |
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
| Function-level tests already present | `workItemsDialogContexts.test.ts`, `useModifySettings*.test.ts`, `reportOwner.test.ts` | REUSE as the destination for moved assertions |
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
| 1 | **Time-budget guard + failing per-file budget + profile report** | In order: (1) the four spike questions of D2; (2) calibration: local plain against local `--coverage` on the same machine, 3 oversubscribed local runs, CI artifacts (at least 5; stop if missing), first-test-in-file allowance A, CI/local ratio F; (3) reporter (options, not environment), pure compare step with planted-list tests for all three directions of D3, allow-list seeded from the calibration with reasons and ceilings; (4) **proof**: a planted slow test fails through `pnpm test` and through `CI=true pnpm run sonarreport`, and not under the Stryker marker or in watch; (5) `--warn-only` removed from the frontend call in `ci_frontend.yml` (ceilings re-derived if the check does not pass on the 5 runs); (6) `summarise.py` extended (top-N shares, counts over thresholds); (7) `docs/ci-learnings.md` entry "where to test"; (8) status notes on ADR-268 and ADR-269 (D3); (9) a standing check for skipped and focused tests: Biome has `suspicious/noSkippedTests` and `noFocusedTests`, neither enabled in `biome.json` today; the slice counts current offenders and enables them (offenders fixed or listed by name), which makes the per-slice grep a second line rather than the only one | none (no speed change) | 0; the guard is the product. If calibration does not fit one day, split: 1a spike + calibration, 1b guard + CI wiring |
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
