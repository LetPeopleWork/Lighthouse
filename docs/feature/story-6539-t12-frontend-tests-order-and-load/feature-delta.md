# Feature: story-6539-t12-frontend-tests-order-and-load

Story #6539 · T12 Frontend tests that do not depend on order or a quiet machine · part of Epic #6506 "Fast, reliable test
suites". Catalogue: [Epic workspace](../epic-6264-modular-monolith/feature-delta.md) ·
Brief: [slice-47](../epic-6264-modular-monolith/slices/slice-47-t12-frontend-tests-order-and-load.md).

DISCUSS: the brief, settled with the maintainer on 2026-11-07. Its final section, "Maintainer decisions", overrides the text
above it. Waves start at DESIGN.

## Wave: DISCUSS / [REF] Inherited

- The behaviour-free contract: `docs/evolution/story-6457-fe7-fast-frontend-tests/evolution.md`. Epic decision D-6264-5.
- job_id: infrastructure-only. Contexts touched: none. RBAC, Lighthouse-Clients, website, usage-data, UI sketch, Terminology,
  context map: N/A, because only test files and one CI line change, as the brief answers item by item.

## Baseline re-count

Read at DESIGN from `measurement-seed-sweep.md` (trunk 57176a17c, Vitest 5.0.5, 618 files, 9500 tests, about 235 s a run).
DESIGN ran no new suite: the design agent has no shell, so these are the sweep's numbers, read and cross-checked against the
sources (the `vitest.config.ts` and `ActionButton.test.tsx` lines the sweep cites match trunk).

| Brief said | Today |
|---|---|
| 2 `PortfolioDetail` tests fail under seeds 11 and 33 | Confirmed, same 2 tests, also seed 3. Cause is a missing mock default, not a leaked implementation (the brief's hypothesis was wrong). Reproduces with no shuffle |
| Other order dependence, to be found | 2 more files in 5 seeds: `GeneralSettingsComponent.test.tsx` (seeds 1, 2), `ActionButton.test.tsx` (seed 1, an unhandled error with all tests green). 3 files, 3 causes, none shared. Extra files against the cap of 5: 2 |
| Reports 5 s timeouts and `pointer-events: none` (2026-11-04) | **Not reproduced.** 0 Reports failures in 7 runs, including 24 workers (load 28) and a starved run. The only load failures seen (2 files, `useModifySettings.validation`, `DeliveryCreateModal.rulebased`) came from a run so starved that 195 files never started; indicative, not the 2026-11-04 failures |
| Slice hypotheses on `createWizard` | Untested by any failing run; nothing to fix on evidence |

The Maintainer decision "known load failures are fixed" does not match this count: nothing was fixed, because nothing reproduced.
DECIDE D-6539-7 records what the Story says instead.

## Checklist

- [ ] Behaviour-free: production code unchanged
- [ ] No longer timeouts or retries as the fix
- [ ] Ratchet in the same change: shuffle in the CI frontend run
- [ ] Docs in the same change where a rule changes (`docs/ci-learnings.md`)
- [ ] Mutation N/A while only test files change, accepted by the maintainer

## Wave: DESIGN / [REF] Scope, mode and context

Application scope, propose mode, AFK: the maintainer is away, so each engineering choice below takes the recommended option
and is recorded as a decision. Rigor `standard`. Read: slice-47 brief with its "Maintainer decisions", the sweep,
`ci-learnings.md` Tests section, `ci_frontend.yml`, the `build-frontend` action, `vitest.config.ts`, `setupTests.ts`,
`src/tests/timeBudget/`. No SPIKE, no DISCUSS wave-decisions file (the brief is the DISCUSS output). No contradiction with
DISCUSS except the "load failures are fixed" premise (D-6539-7).

Context map (ADR-270): N/A, because no context, concept, event or edge changes. `brief.md`, ADR-270, `ARCHITECTURE.md` §4 and
the Outcome registry: unchanged (a test-set-up Story adds no typed contract). Loading and failure looks: N/A, because no screen
is added or changed.

## Wave: DESIGN / [REF] Decisions

| ID | Decision | Why (one line) |
|---|---|---|
| D-6539-1 | Three local fixes, test files only (below) | Each cause is reproduced and local; none needs a timeout, a retry or a production change |
| D-6539-2 | **No** config-wide reset rule in `setupTests.ts` or `vitest.config.ts` | See "Shared set-up rule weighed" |
| D-6539-3 | After the fixes: per-file runs of the 3 files on 10 seeds, then 7 full runs (seeds 1, 11, and fresh 4, 5, 6, 7, 8) | See "Local sweep size". Superseded: four full runs on seeds 11, 33, 4, 5 with CI=true (see "DISTILL / [REF] Review revision") |
| D-6539-4 | CI guard: `--sequence.shuffle.tests` (tests, not files) in the existing frontend run, seed = `github.run_id`, printed | See "CI guard" |
| D-6539-5 | Budget reporter and CSV scripts stay unchanged; verified at B1, adjusted only on evidence | See "Time budget and CSVs" |
| D-6539-6 | One replay line in `ci-learnings.md` Tests section | See "Replaying a red seed" |
| D-6539-7 | Load: record as "not reproduced, no fix claimed"; no change to any Reports file or the two starved-run files | See "Load" |
| D-6539-8 | Two slices: A (fixes and sweep), B (guard and docs) | Each at most a day |
| D-6539-9 | Close: 5 consecutive green `main` runs of the frontend job under the guard, distinct seeds | See "Closing" |
| D-6539-10 | ActionButton: drop the `window` stub, assert the unmounted case by the absence of rejections and console errors; prove the event first by temporary instrumentation | The stub is the hazard; see fix 3. Withdrawn: the stub stays (see "DISTILL / [REF] Review revision") |
| D-6539-11 | Peer review (`nw-solution-architect-reviewer`, `reviewer_model` of `standard`) is left to the orchestrator: this design agent was dispatched without the means to call it | Rigor profile still owes one review before DELIVER |

## Wave: DESIGN / [REF] Fix per file (D-6539-1, D-6539-10)

All three in `Lighthouse.Frontend/src`; behaviour-free, no skipped or deleted test, test count unchanged.

1. `pages/Portfolios/Detail/PortfolioDetail.test.tsx`: add one file-level `beforeEach` (outside every describe) that sets every
   mock default the first describe's `beforeEach` sets today (`mockGetPortfolio` and its siblings). Nested `beforeEach` hooks run
   after it, so each describe still overrides what it needs. The first describe keeps its own set-up only where it differs.
   The "a refresh that failed" tests that assert absence (`stops saying so ...`, `leaves the button alone ...`) pass vacuously
   in the broken order; give each a positive anchor (the portfolio is rendered) before the absence check, per the
   "invariance assertions need an anchor" rule. Check the update-deferral `beforeEach` for a second leak, which the sweep left untested.
2. `pages/Settings/General/GeneralSettingsComponent.test.tsx` (exact path to confirm at A2): reset `rbac.isSystemAdmin = true` in the
   top-level `beforeEach`; the one test that needs `false` keeps setting it itself. Grep the file for any other field of the hoisted
   `rbac` object that a test writes, and reset those in the same place.
3. `components/Common/ActionButton/ActionButton.test.tsx`: remove `vi.stubGlobal("window", undefined)` and its unstub calls. The
> [Superseded: see "DISTILL / [REF] Review revision"] this ActionButton fix is withdrawn; the revised fix is in the Review revision and DISTILL 01-03.
   unmounted case keeps its test: click, unmount, advance fake timers past the minimum wait, then assert no unhandled rejection
   and no `console.error` call (`setupTests.ts` makes it a `vi.fn`). Honest loss: the stub made a later `window` use in the
   delayed callback throw; the component has none today (only `isMounted`), and React 19 ignores a state set after unmount, so the
   remaining assertion is weaker. Recorded, accepted: the stub cannot be kept safely, because it deliberately removes the global every
   other listener in the worker needs while timers run.
   **How the event is proven** (it is inferred today): at A3, before editing, add temporary (uncommitted) capture-phase listeners on
   `document` for `focus`, `focusin`, `selectionchange` and `click` that log `event.type` and the target while `window` is stubbed;
   run `-t "takes the focus when asked to|leaves the page alone" --sequence.shuffle --sequence.seed=1`. The logged type names
   the stray event. Control: seed 3 (focus test second) logs nothing. Then remove the instrumentation and the stub; the proof of the
   fix is the same pair clean on seeds 1, 2, 4, 5, 6 and the whole file clean on seeds 1 and 7. If the logged event is something
   other than the focus test's, write the true cause in the delta before fixing.

## Wave: DESIGN / [REF] Shared set-up rule weighed (D-6539-2)

Candidates: `unstubGlobals: true`, `restoreMocks`, `mockReset` or `clearMocks` in `vitest.config.ts`, or an `afterEach` in
`setupTests.ts`. Measured against the three causes: cause 1 is a missing default, so a reset rule hides nothing and fixes nothing
(it would make the block fail in every order, which detects but does not repair); cause 2 is a plain object a test mutates, not a
mock, so no mock rule reaches it; cause 3's test already calls `vi.unstubAllGlobals()` itself, and the hazard is inside the gap. Coverage
of the known class: 0 of 3. Risk: `mockReset` and `restoreMocks` wipe implementations that module-level `vi.fn(impl)` and
`vi.hoisted` mocks rely on across 618 files, and `unstubGlobals` breaks any file that stubs a global once in `beforeAll`
(`DataGridBase.paging.test.tsx` stubs one). A config-wide change is riskier than three local fixes and buys nothing against the
known causes. Rejected. The net for the unknown remainder is the shuffled CI run, which is the reason to ship it.

## Wave: DESIGN / [REF] Local sweep size (D-6539-3)

Five seeds found three causes: seed 1 found two, seeds 2 and 3 found none new, so the discovery rate fell fast.
After the fixes: (a) the three files alone on seeds 11, 33, 1, 2, 3, 4, 5, 6, 7, 8 (seconds each; proves each fix on the seeds
that were red); (b) full runs on seed 1 (the seed that found two causes), seed 11, and fresh seeds 4, 5, 6, 7, 8: 7 runs at about
4 minutes, near 30 minutes of an idle machine. Fresh seeds are what can still surface a cause; CI then supplies a new seed per
push. Stop rules: a red full run is diagnosed and fixed if local (it counts against the cap of 5 extra files; 2 used so far);
at 5, pause T12 and ask. Not run: 24 workers or busy loops (the sweep showed neither reproduces the load failures). Order:
all seven full runs green before B starts, so CI does not go red at once.
> [Superseded: see "DISTILL / [REF] Review revision"] four full runs, not seven.

## Wave: DESIGN / [REF] CI guard (D-6539-4, D-6539-5, D-6539-6)

**Where.** `build-frontend/action.yml` is the only place `pnpm test` runs in CI (`package-app` passes `run-tests: 'false'`).
Add an optional input `shuffle-seed` (default empty) to the action. When set, its test step runs
`pnpm test --sequence.shuffle.tests --sequence.seed=$SEED` and first prints `Test order seed: $SEED` (plus a `::notice::`).
`ci_frontend.yml` passes `shuffle-seed: ${{ github.run_id }}`. Empty means the plain run, so a local `pnpm test` and every other
caller are unchanged. Re-running a red job reuses the run id, so the same seed comes back: a re-run cannot hide an order
dependence by luck, and the seed is readable from the URL. Rejected: a random seed inside Vitest (printed, but not in the
step title or the URL, and a re-run changes it); a config-level default (would shuffle local runs and Stryker runs, whose timings and
budget are meaningless anyway); a second workflow run (adds a full run, 10 to 18 minutes).
**Probe of the guard** (does it bite?): the sweep already ran this exact flag form against trunk and went red on 4 of 5 seeds;
> [Superseded: see "DISTILL / [REF] Review revision"] all five seeds went red (the sweep used the bare flag); the proof for `.tests` is the pre-fix table of DISTILL.
B1 runs the final action command line once locally with a run-id-sized seed (confirms an 11-digit seed is accepted).

**Files, tests or both: tests only.** All three causes are inside one file. Files run isolated (`pool: threads`, `isolate: true`),
so a file's order cannot change its result in jsdom; shuffling files only moves the load pattern and would add timeout noise
unrelated to order dependence. The bare `--sequence.shuffle` the sweep used shuffles both; its per-file test orders depend on the
seed alone, so the sweep's seeds stay valid. B1 confirms this: `.tests` plus seed 1 must reproduce the old ActionButton error
(run on a throwaway copy of the pre-fix test, or by recording the order); if not, fall back to the bare flag and say so.

**Time budget and CSVs.** The reporter derives "first test of a file" from the file's collected test list
(`observedRunFromVitest.ts`, index in `allTests()`), not from what ran first. Under shuffle the test that pays the cold first
render may then be another one that has no allowance. Seeds 11 and 1 each showed one offender, which the sweep did not name.
> [Superseded: see "DISTILL / [REF] Review revision"] only seed 11 showed an offender.
Decision: change nothing now; B1 names the offenders of the 7 full runs. If a shuffled run's offender is a test that moved into the
> [Superseded: see "DISTILL / [REF] Review revision"] the sweep used the bare flag and all five seeds went red; "7 full runs" is four, on seeds 11, 33, 4, 5.
first position, the fix is in the reporter (take the first test by start time), a separate step with its own test, not a loosened
limit. The per-file budget sums test time, so reordering does not change it, and the CSV is keyed by test name; B1 checks that
`vitest_to_csv.py` does not depend on test order and `check_budget.py` gives the same verdict on a shuffled run's CSV.
`bail: 1` stays (one red shows one test; the seed replays the rest).

**Replaying a red seed** (one line for `docs/ci-learnings.md`, Tests section): *A frontend CI run is shuffled with seed =
the run id (printed as "Test order seed" and in the Vitest header); replay with
`cd Lighthouse.Frontend && pnpm test --sequence.shuffle.tests --sequence.seed=<run id> <the red file>`, and bisect a pair with `-t "A|B"`.*
Also record there, as the one new rule: tests may not depend on another test's set-up or leave shared state changed; the guard
checks it. **Recognising a new order dependence**: the red test is in a file the commit did not touch; it passes alone and
on the previous seed; the replay with the same seed reds on the commit before as well (so the commit did not cause it); or the run
ends with all tests green and `Errors 1 error` / an unhandled error naming a file (cause 3's signature). Fix the test; do not
raise a timeout.

## Wave: DESIGN / [REF] Load (D-6539-7)

Honest position: the sweep did not reproduce the 2026-11-04 Reports timeouts or the `pointer-events` failure; oversubscription alone
(24 workers, load 28) gave 0 failures at the same 230 s, and the starved run's failures were a pool that could not start workers
plus two first-render or timer waits. No fix exists. The Story does: (a) record in the delta and as an ADO #6539 comment that the
load failures were not reproduced and nothing was changed for them; (b) keep the rule "no longer timeout or retry as the fix";
(c) watch: if a CI or local red shows `Test timed out in 5000ms` or `pointer-events: none`, capture file, test, seed and load
average in the ADO item and treat it as a new Story with that evidence. Rejected: rewriting `createWizard` or the two starved-run
files on a guess (slice hypotheses, untested; a production-like risk of churn T13 already owns); counting them toward the 5 extra
files. The existing `ci-learnings.md` line 118 (give marginal tests an explicit timeout) conflicts with this Story's rule only
for T12's own work; leave it, and revisit after the 5 green runs.
Slice B of the brief (Reports load fixes) is dropped; T12 stays before T6; T13's parked Reports slice is not blocked.

## Wave: DESIGN / [REF] Slices and steps (D-6539-8)

| Slice | Step | Delivers | Size |
|---|---|---|---|
| A fixes and sweep | A1 | PortfolioDetail: file-level defaults, anchored absence tests; the failing pair proven green with and without shuffle | 1 to 2 h |
| | A2 | GeneralSettingsComponent: reset in the top-level `beforeEach` | under 1 h |
| | A3 | ActionButton: event proven by instrumentation, stub removed | 1 to 2 h |
| | A4 | The sweep of D-6539-3, extra causes fixed or paused at the cap | 1 to 3 h (mostly waiting) |
| B guard | B1 | Action input and workflow line; local run of the exact command; budget and CSV checks; offenders named | 1 to 2 h |
| | B2 | `ci-learnings.md` replay line and rule; delta updated; then the Story waits on the 5 green `main` runs | under 1 h |

`pnpm test` and `pnpm build` clean at each step; a focused commit per step; no mutation testing (N/A: no production file, as in T13).
Every run uses `TZ=Europe/Zurich` via `pnpm test`; `LIGHTHOUSE_TIME_BUDGET` unset.

## Wave: DESIGN / [REF] Closing (D-6539-9)

Done when 5 consecutive `main` runs of the frontend job, started after B is pushed, are green with 5 different run ids
(so 5 seeds), including the time-budget step, each recorded in the delta (run id, seed, result). A docs-only push starts no
frontend run, so only code pushes count. A red caused by an order dependence is fixed (counts against the cap of 5) and the count
restarts; a red from infrastructure (runner, network, Sonar) is noted and does not reset it. Then: delta updated, finalize
checks (`ci-learnings.md` rule present), ADO #6539 moved by the usual story rule, the one-week review of the guard recorded:
revert it if reds need a seed to replay and that proves a nuisance (maintainer rule).

## Wave: DESIGN / [REF] Reuse Analysis

| Existing | File | Overlap | Decision | Justification |
|---|---|---|---|---|
| `build-frontend` action | `.github/actions/build-frontend/action.yml` | The one place CI runs `pnpm test` | EXTEND (optional input) | A second step or workflow would add a full run |
| Frontend workflow | `.github/workflows/ci_frontend.yml` | Caller of the action | EXTEND (one input) | Maintainer decision |
| Time budget reporter and CSV scripts | `src/tests/timeBudget/`, `Scripts/test-timings/` | Sees test order | Unchanged, verified at B1 | Change only on evidence (D-6539-5) |
| `setupTests.ts`, `vitest.config.ts` | `Lighthouse.Frontend/` | Where a reset rule would live | Unchanged | D-6539-2 |
| `docs/ci-learnings.md` Tests section | `docs/ci-learnings.md` | Home of the replay line and rule | EXTEND | A rule no tool can enforce on its own; the guard is the tool |
| CREATE NEW | none | | | |

Effect-isolation contract shapes and Earned Trust probes for adapters: N/A, because no component, port or adapter is designed;
the shuffled run is itself the probe for test isolation.

## Wave: DESIGN / [REF] Open questions

1. Whether a shuffled run's budget offenders include a test moved into the first position (answered at B1).
2. Whether `.tests` alone reproduces the bare-flag orders (answered at B1; fallback stated).
3. Exact path of `GeneralSettingsComponent.test.tsx` and the full list of mock defaults of the first `PortfolioDetail` describe (A1, A2).
4. Action pin tests (`.github/scripts/action-pins.test.mjs`, `toolchain-pins.test.mjs`) read `action.yml`; B1 runs them after the edit.
5. Peer review of this design (D-6539-11).

## Wave: DESIGN / [REF] Architecture summary

No hexagonal element changes: no driving adapter, application service, port, domain model piece or driven adapter is added or
modified, no external call is new, and everything in `Lighthouse.Backend` and production frontend code stays as is.

```
 Changed:   [3 test files] --fixed--> order-independent
            [setupTests.ts, vitest.config.ts] -- unchanged
            [ci_frontend.yml] --seed=run_id--> [build-frontend action] --> pnpm test --sequence.shuffle.tests --seed
            [docs/ci-learnings.md] + replay line, + isolation rule
```

Patterns weighed (one line each): test isolation by explicit per-test set-up, chosen (each test, not a neighbour, owns its
defaults); global reset in set-up (`restoreMocks`/`mockReset`/`unstubGlobals`), rejected (covers 0 of 3 causes, breaks
shared fixtures); retry or longer timeout, rejected by the maintainer (hides the order); seeded randomised ordering as a CI
ratchet, chosen (finds the class, replays by seed); file shuffle, rejected (files are isolated, adds load noise only);
random in-Vitest seed, rejected (a re-run changes it); second CI run, rejected (10 to 18 minutes more); stub-the-global
teardown simulation (`window = undefined`), rejected (removes what other listeners need).

## Wave: DESIGN / [REF] Orchestrator amendments (2026-11-07, maintainer away)

- **D-6539-3, the sweep is shorter.** The maintainer asked for a short local sweep. Four full runs after the fixes, about 16
  minutes, with the same flags CI will use: seeds 1, 4, 5, 6. The three files alone on the ten seeds stay.
- **D-6539-1, `ActionButton`.** The Epic's non-goal says no test is deleted without its assertion living on elsewhere, so the
  "accepted loss" is not accepted by default. DELIVER first tries to keep what the stub proved in a narrower form. If that is
  not possible, it stays recorded as a loss and is named at the hold.

## Wave: DEVOPS / [REF] Platform readiness

Existing platform; nothing new is deployed. One optional input and one line change in CI. Written 2026-11-07 under AFK: the
recommended option is taken everywhere and recorded. No `environments.yaml`: the matrix is two rows and fits the table.
Read at DEVOPS, fresh: `.github/actions/build-frontend/action.yml`, `ci_frontend.yml`, `package-app/action.yml`,
`action-pins.test.mjs`, `toolchain-pins.test.mjs`, `observedRunFromVitest.ts`, `vitest_to_csv.py`, `test-time-budget.json`.

| Topic | Answer |
|---|---|
| Environments | Local (`pnpm test`, plain order, unchanged) and CI ("Verify Frontend" job on `main`, shuffled). No staging, no deploy. |
| Usage-data event | N/A, because no user of the product reaches test files or a CI input, so no event could show use. |
| Rollback | One commit. The B1 commit (the `shuffle-seed` input plus the `ci_frontend.yml` line) is reverted alone with `git revert`; or, as a hot fix, deleting the one `shuffle-seed:` line in `ci_frontend.yml` already restores the plain run, since an empty input is the plain run. The three test fixes (A1 to A3) stay either way: they are order-independence fixes that hold without the guard and need no revert. **Adjustment to DESIGN:** B2 (the `ci-learnings.md` replay line) is a second commit, so "one commit" needs the replay line to come off with it. The rollback is therefore `git revert --no-commit <B2> <B1>` followed by one commit. The isolation rule stays in the ledger, the replay line goes. Decision point: the one-week review of D-6539-9 (revert if reds need a seed to replay and that proves a nuisance). |
| What the first push is judged against | A green "Verify Frontend" job with every step green, **including "Check test-time budget"** against the budget in force (`test-time-budget.json`: frontend 56 s per file, empty allow-list), under one printed seed (the run id). Judged on result, not duration: CI is bimodal (about 650 s or 1,100 s for the same tests), so one run's time says nothing. A red is read by the "Recognising a new order dependence" test of the CI-guard section: replay with the printed seed, and a red the commit did not cause is fixed as a new cause (cap of 5 extra files, 2 used). The Story closes on 5 distinct green `main` runs (D-6539-9), not on the first. |
| Time budget under shuffle | **Per-file budget: works unchanged.** `check_budget.py` sums test seconds per file from the CSV; a sum does not depend on order. **CSV and artifact: work unchanged.** `vitest_to_csv.py` reads `assertionResults` per file, keys rows by `fullName`, and sorts by duration, so order is not an input; `test-timings-frontend` still uploads (the step runs `if: always()`). **Per-test guard: one real risk, not yet shown to be safe.** `observeFile` marks `firstInFile` by position in the collected test list (`findIndex` over `file.tests`, from `allTests()`) and grants it `firstTestAllowanceSeconds` (0.31 s, `vitest.config.ts`). If Vitest leaves the collected list in file order while running shuffled, the test that pays the cold first render is not the one with the allowance, and a test over its limit fails the run on timing, not on order. The sweep saw one unnamed offender on each of seeds 1 and 11. This is DESIGN open question 1; B1 answers it with evidence from the full runs. If an offender is a moved first test, the fix is in the reporter (first by start time, its own test), never a looser limit. `bail: 1` is unaffected. |
> [Superseded: see "DISTILL / [REF] Review revision"] only seed 11 had an offender (not 1 and 11); the first-test risk is checked directly at 02-01, not by offenders.
| Action input and other callers | `shuffle-seed`: optional, `default: ''`, and the test step branches on empty, so empty runs exactly `pnpm test`. Callers of `build-frontend`, all of them: (1) `.github/workflows/ci_frontend.yml`: the only one that sets it, `shuffle-seed: ${{ github.run_id }}`. (2) `.github/actions/package-app/action.yml`: passes `run-tests: 'false'` and `sbom-version`, no `shuffle-seed`; its test step is skipped by `inputs.run-tests == 'true'`, so the input is never read. `ci_packageapp.yml` reaches the action only through (2). `ci.yml` calls `ci_frontend.yml`, not the action. No other workflow references it. Local `pnpm test` and Stryker never see the input. Build the step the way the other inputs are read: the seed goes through `env: SHUFFLE_SEED: ${{ inputs.shuffle-seed }}` and `"$SHUFFLE_SEED"`, never interpolated inside the `run:` text. |
| Tests that pin the files | `action-pins.test.mjs` and `toolchain-pins.test.mjs` read the real `action.yml` and `ci_frontend.yml`; they check `uses:` lines (commit pin plus release comment), a Node-version literal and the pnpm and `.nvmrc` rules. Run now, before the edit: 132 of 132 pass. The edit adds no `uses:`, no Node version and no pnpm step, so they should stay green; B1 reruns them (`node --test .github/scripts/*.test.mjs`) after the edit, and a red there is a B1 defect. `.github/actions/README.md` lists the action but not its inputs, so it needs no line; B1 may add one for `shuffle-seed` (optional). |
| Monitoring | The printed "Test order seed" line and notice in the job log, plus the existing `test-timings-frontend` artifact. No dashboard, no alert. |
| Mutation | N/A while only test files, a CI input and a docs line change, because Stryker mutates production code; accepted by the maintainer in DISCUSS. |
| Terminology, release notes, RBAC, Clients, Website, UI sketch | N/A, because no screen, word, route, DTO or marketing claim changes; release notes are written at `/release`. |
| Improvement backlog (`/continuous-improvement before`) | Does not apply: #6506 is an improvement Epic, not a new feature Epic. |

**Contradictions with DESIGN (soft, none blocks):**
1. Rollback in one commit does not hold as DESIGN slices it (B1 and B2 are two commits); resolved above by reverting both as one revert.
2. DESIGN says "7 full runs" in D-6539-3, the CI-guard order line ("all seven full runs green before B starts") and the Slices note; the Orchestrator amendment makes it 4 (seeds 1, 4, 5, 6). The amendment wins; read those lines as four.
3. DESIGN says the budget reporter and CSV scripts "stay unchanged; verified at B1" and treats them as one question. They are two: the CSV and per-file budget are order-independent (read above), the per-test first-test allowance is order-dependent and is the open risk.
4. DESIGN writes the seed as `$SEED` in the step; use an `env:` variable, not inline interpolation (above).

## Wave: DESIGN / [REF] Review revision and maintainer word (2026-11-07)

The design review returned NEEDS_REVISION. This section overrides anything above that differs, the first amendment
included.

**Maintainer, asked after the review:**

- **Load:** recorded as not reproduced, no fix claimed, watched. A future 5 s or `pointer-events` red becomes a new Story with
  its evidence. This replaces "the known load failures are fixed" in the brief.
- **Guard:** `--sequence.shuffle.tests`, tests only, as DESIGN proposed. A deliberate change from the brief's bare flag. "Files
  are isolated" rests on the config and is not measured.

**Fixed by the orchestrator:**

- **Sweep.** Full runs on seeds 11, 33, 4, 5, with `CI=true` and the flags CI will use, because `CI=true` changes `bail` and the
  budget factor. The earlier "same flags" claim was false without it. The three files alone on the ten seeds stay. All four
  green before the CI change.
- **`ActionButton`, the design's fix is withdrawn.** Dropping the stub would leave `if (isMounted.current)` untested:
  a state set from the delayed callback goes through React's own `window.event` read, so the stub is what makes the guard
  observable, and "React 19 ignores it" was inferred. Instead, first remove that guard and see the stubbed test go red. Then
  keep the stub and remove the stray event's source: prove which event it is as D-6539-1 says, and let it finish before the
  stub goes on. If the stub truly cannot stay, name the coverage loss at the hold.
- **`console.error` assertion:** not added. It accumulates across a file's tests, so it would be order-dependent itself.
- **`PortfolioDetail`:** the helper `renderAndWaitForTheUpdateFeed` also waits for the portfolio heading, so presence tests do
  not lean on the flush either.
- **`GeneralSettingsComponent.test.tsx`** is at `src/components/Common/BaseSettings/`.
- **Recognising a red:** a time-budget failure at the Vitest step, on a slow test moved out of first place, is a third kind.
  The per-test guard's first-test allowance goes by declaration order, so B1 changes the reporter to first by start time if
  that happens; a looser limit is not the fix.
- **Corrections:** only seed 11 had a budget offender. The sweep used the bare flag and all five seeds went red, so B1 has to
  show `.tests` reproduces the failures on the pre-fix commit. The busy-loop run did produce two timeouts, so "neither
  reproduces" is overstated; that run was too saturated to judge.
- **Mutation:** the maintainer accepted N/A on 2026-11-07 in chat, for Stories where only test files change; the reporter is
  a test tool, and if B1 changes it, it is measured.

## Wave: DISTILL / [REF] Scope, reconciliation and baseline (2026-11-07, maintainer AFK)

- Shape: three order-independence fixes in test files, then one CI guard (a shuffled run) and one ledger rule. No product
  behaviour is specified, so there is no new acceptance scenario to write. The executable specification is Vitest: the existing
  tests of the three files are the behaviour, and the checks below are what can fail. No Gherkin, no scaffolds, no skipped
  tests (Biome forbids `.skip`); Tier B, the domain-types module and the state-delta port are N/A because the project has no
  Gherkin, state machine or generator at this layer. An untagged test is correct here: no nWave scenario tag goes into any test
  title, comment or file; classification stays in this workspace.
- UI sketches: N/A, because nothing a user sees changes. Loading and failure looks: N/A, no screen is added or changed.
- Reconciliation: DISCUSS (brief, Checklist), DESIGN with its two amendments and the "Review revision and maintainer word", and
  DEVOPS read. **0 hard contradictions.** Soft points, resolved as recorded: (1) the first Orchestrator amendment says full runs on
  seeds 1, 4, 5, 6 and the review revision says 11, 33, 4, 5 with `CI=true`; **the revision wins** (it is the last section).
  (2) DESIGN and DEVOPS still read "seven full runs" in places; read four. (3) The brief says load failures are fixed; the maintainer
  word says not reproduced, no fix claimed (D-6539-7). (4) The ActionButton fix of DESIGN (drop the stub) is withdrawn; this wave
  uses the revised fix. (5) Step ids: DESIGN's A1 to A4 and B1 to B2 are 01-01 to 01-04 and 02-01 to 02-02 in the roadmap, with the
  action input, workflow line and reporter check in 02-01.
- Baseline: trunk `93dc95b80` (frontend sources unchanged since the sweep's `57176a17c`; 01-01 confirms with
  `git diff --stat 57176a17c HEAD -- Lighthouse.Frontend`, which must print nothing, and re-runs the pre-fix reproductions below
  on the commit it starts from). Vitest 5.0.5, 618 files, 9500 tests, about 235 s a run. Test counts per file are taken from a run
  at 01-01 (`pnpm test <file>`) and must be equal after each fix: no test added, removed, skipped or made `todo`, except where a
  step says an anchor is added inside an existing test.
- Mechanism: from `Lighthouse.Frontend`, `pnpm test <file> [flags]` (`pnpm test` is `cross-env TZ=Europe/Zurich vitest run`).
  `test-results.json` is deleted before every run that is read for ids. Local runs use `LIGHTHOUSE_TEST_WORKERS` unset.

## Wave: DISTILL / [REF] The guarantee

Stated as checks that can fail. Each is a command and an exit status or a named test.

**Every order-checking run below sets `LIGHTHOUSE_TIME_BUDGET=warn`** (read in `src/tests/timeBudget/`: unset or empty is `fail`
mode in `timeBudgetOptions.ts`; `timeBudgetReporter.ts` sets `process.exitCode = 1` only when `verdict.failing`, and
`compareToAllowList.ts` makes that `options.mode === "fail" && findings.length > 0`; so `warn` never sets a failing exit code).
The exit status of an order run then means tests and unhandled errors only, and the budget offenders go in a separate column of
the results table. `bail: 1` and the budget factor follow `CI=true` as before; `warn` does not change them.

1. **Order independence of the three files.** After the fixes, for each file `F` in {`PortfolioDetail.test.tsx`,
   `GeneralSettingsComponent.test.tsx`, `ActionButton.test.tsx`} and each seed `N` in {11, 33, 1, 2, 3, 4, 5, 6, 7, 8}:
   `LIGHTHOUSE_TIME_BUDGET=warn pnpm test <F> --sequence.shuffle.tests --sequence.seed=N` exits 0 with no `Errors` line (an unhandled error fails the run
   with every test green, so exit status, not the pass count, is the check). 30 runs, seconds each.
2. **Order independence of the suite on the CI flags.** `LIGHTHOUSE_TIME_BUDGET=warn CI=true pnpm test --sequence.shuffle.tests --sequence.seed=N` exits 0 for
   N in {11, 33, 4, 5}, with `test-results.json` deleted before each, 618 files and the same test count as the plain run.
3. **Behaviour-free.** Before-commit is **`93dc95b80`**. `git diff --name-only 93dc95b80 HEAD -- Lighthouse.Frontend` lists only: the three
   Story test files (`PortfolioDetail.test.tsx`, `GeneralSettingsComponent.test.tsx`, `ActionButton.test.tsx`), any further test
   file the 01-04 sweep adds (each named in the delta), and, only if 02-01 changes it, files under `src/tests/timeBudget/`; nothing
   else under `Lighthouse.Frontend/src`. Re-run at the end of 01-04, 02-01 and 02-02; any other name is a failure. (Older wording:
   `git diff --stat <before-commit> HEAD -- Lighthouse.Frontend/src` names only the three test files while
   slice 01 runs, and nothing else under `Lighthouse.Frontend` (the temporary edit of `ActionButton.tsx` in 01-03 is restored and
   leaves no diff). Per file the test count is unchanged and the id list from a run equals the before list.
4. **No weakening (mechanical).** Run `git diff -U0 93dc95b80 HEAD -- <the changed test files> | grep -E '^[-+][^-+].*(\.skip|\.only|\.todo|skipIf|runIf|timeout|retry)'` and
   reconcile: every hit is quoted in the step's note with a reason, and **a hit on an added line is a failure** unless the same
   text is on a removed line (a moved line). Removed `expect(` lines: `git diff -U0 ... | grep -cE '^-.*\bexpect\('` minus the same
   count of added lines must be 0, or each difference is quoted with where the assertion went. (Prose of the same check:) The diff of the three files adds no `.skip`, `.only`, `.todo`, `skipIf`, `runIf`, `{ timeout: ... }`,
   `testTimeout`, `retry`, `waitFor` option with a larger `timeout`, and removes no `expect(` without the same assertion living on
   elsewhere (the Epic's non-goal): `git diff <before-commit> -- <file> | grep -E '^[-+].*(\.skip|\.only|\.todo|skipIf|runIf|timeout|retry)'`
   prints nothing and each removed `expect(` is quoted in the step's note with where it went.
5. **The guard bites.** The pre-fix reproductions with the guard's flag go red (see "How the shuffled flag is proven"), and at 02-01 the
   final action command line run locally exits 0 on the fixed tree and exits 1 on a throwaway copy of the pre-fix PortfolioDetail test.
   **The same 11-digit seed run twice gives the same test order** (record one file's test order from both runs and compare).
6. **Nothing else moves. Pass condition:** `node --test .github/scripts/*.test.mjs` reports 132 passed before the 02-01 edit and 132 plus
   the new pin tests of 02-01, 0 failed, after it; `pnpm build` exits 0 with zero errors and zero warnings in its output; `git status`
   after it lists no file outside the step's diff; plain `pnpm test` exits 0. Details: `node --test .github/scripts/*.test.mjs` stays 132 of 132; `pnpm build` is clean (zero errors, zero
   warnings); `git status` is checked after it for files Biome rewrote; `pnpm test` plain (no shuffle) is green.
7. **Mutation: a waiver, not a check.** N/A while only test files, a CI input and a docs line change; accepted by the maintainer on 2026-11-07. If 02-01
   has to change the budget reporter (a test tool), that change is measured: its own test, and `pnpm test <reporter test files>`.

## Wave: DISTILL / [REF] What each fix must show (red before, green after)

All commands from `Lighthouse.Frontend`. "Red" means exit non-zero for the reason named, not for a set-up error: the failing
test names and the message are pasted into the step's commit body or this delta.

**01-01 `PortfolioDetail.test.tsx` (cause 1, a missing mock default; reproduces with no shuffle).**
- Red before: `pnpm test src/pages/Portfolios/Detail/PortfolioDetail.test.tsx -t "a refresh that failed"` gives 2 failed, 5 passed
  (`says so on the refresh button`, `keeps saying so when a forecast run finishes afterwards`; the DOM shows
  `portfolio-no-access-alert`). Also red on the shuffled pair `... --sequence.shuffle.tests --sequence.seed=11` (and 33, 3).
- Anchors first: add the positive anchor (the portfolio heading is rendered) to `stops saying so ...` and `leaves the button alone ...`
  and run `-t "a refresh that failed"` again **before** the defaults are added: those two must now also go red (they passed
  vacuously). If they stay green the anchor does not anchor; fix the anchor. `renderAndWaitForTheUpdateFeed` also waits for the
  portfolio heading.
- Then add the file-level `beforeEach` with every default the first describe sets; the first describe keeps only what differs.
  Check the update-deferral `beforeEach` (line 463 block) for a second leak with `-t "settings tab update deferral"` alone and on a
  shuffled seed. This is a **read-only check**: it has no red criterion, because no failure of that block was ever reproduced; a
  failure found is written in the delta and fixed, and a clean result is recorded as clean.
- Green after: `-t "a refresh that failed"` gives 7 passed; the whole file passes plain and on the ten seeds with
  `--sequence.shuffle.tests`; test count equals before.

**01-02 `GeneralSettingsComponent.test.tsx` (cause 2, `rbac.isSystemAdmin` left `false`).**
- Red before: `pnpm test src/components/Common/BaseSettings/GeneralSettingsComponent.test.tsx -t "does not offer it to someone who cannot open it|shows wizard button when selected work tracking system is CSV" --sequence.shuffle.tests --sequence.seed=1`
  fails the CSV test (`Unable to find ... /Select CSV File/i`); seeds 2 and 4 the same; seed 3 passes (the control).
  The whole file on seeds 1 and 2 is red as well.
- Fix: `rbac.isSystemAdmin = true` in the top-level `beforeEach`; the one test that needs `false` sets it itself. Grep the file for
  every other field of the hoisted `rbac` object that a test writes and reset those in the same place.
- Green after: the same pair on seeds 1, 2, 3, 4 passes; the whole file passes on the ten seeds.

**01-03 `ActionButton.test.tsx` (cause 3, a stray event while `window` is stubbed).**
- **Guard check before any edit (D-6539-10 revised).** The stub is the only thing that makes the `isMounted` guard observable. So
  first show that the test pins the guard: temporarily (uncommitted) delete the line `if (isMounted.current)` in
  `ActionButton.tsx` (keep `setInternalIsWaiting(false)`), run
  `pnpm test src/components/Common/ActionButton/ActionButton.test.tsx -t "leaves the page alone"`, and expect it **red** (the
  state set after unmount reaches React with no `window`). Restore with `git checkout -- src/components/Common/ActionButton/ActionButton.tsx`
  and confirm `git diff --stat` shows no production file. If it stays green with the guard removed, the test does not pin the
  guard today: write that in the delta first and stop at the hold with the finding, do not proceed on the DESIGN assumption.
- **Prove the event** (inferred until now): add temporary uncommitted capture-phase listeners on `document` for `focus`, `focusin`,
  `selectionchange` and `click` that log `event.type` and target, run
  `pnpm test <file> -t "takes the focus when asked to|leaves the page alone" --sequence.shuffle.tests --sequence.seed=1`; the log
  names the stray event. Control: seed 3 (focus test second) logs nothing. If the event is not the focus test's, write the true
  cause in the delta before fixing.
- Red before: that pair on seeds 1, 2, 4, 5, 6 gives `TypeError: Cannot read properties of undefined (reading 'nodeName')` as an
  unhandled error with all tests green (exit 1); the whole file on seed 1 the same, twice in a row.
- Fix: keep the stub; remove the stray event's source, by letting it finish before the stub goes on (an awaited tick or the
  event's own settle). No `console.error` assertion is added (it accumulates across a file and would be order-dependent).
- Green after: remove the instrumentation (`git diff` shows none); the pair on seeds 1, 2, 4, 5, 6 exits 0 with no `Errors`; the
  whole file on seeds 1 and 7 exits 0; **repeat the guard check** with the fix in place (guard removed, test red; restored): the
  fixed test still pins the guard. If the stub cannot stay, the coverage loss is named at the hold; nothing is deleted
  without its assertion living elsewhere.

**01-04 sweep.** The 30 per-file runs of guarantee 1 and the four full runs of guarantee 2, all green before slice 02 starts. A
red full run is diagnosed and fixed if local; each new file counts against the cap of 5 extra files (2 used); at 5, pause and ask.
Results (file, seed, exit, errors, load average) go in a table in this delta. Time-budget offenders are counted, not failures;
name each (they feed the 02-01 reporter check).

## Wave: DISTILL / [REF] How the shuffled flag is proven on the pre-fix commit

The sweep ran the bare `--sequence.shuffle` (files and tests). The guard uses `--sequence.shuffle.tests`, a deliberate change
that rests on "files are isolated", which is not measured. So the proof is the same failures with the narrower flag, on a tree
with the three test files at their pre-fix content (the commit 01-01 starts from, before any edit):

| Pre-fix reproduction | Command (from `Lighthouse.Frontend`) | Must be |
|---|---|---|
| PortfolioDetail | `LIGHTHOUSE_TIME_BUDGET=warn pnpm test src/pages/Portfolios/Detail/PortfolioDetail.test.tsx --sequence.shuffle.tests --sequence.seed=11` (also 33, 3) | exit 1, the 2 refresh-failed tests |
| GeneralSettings | `pnpm test src/components/Common/BaseSettings/GeneralSettingsComponent.test.tsx --sequence.shuffle.tests --sequence.seed=1` (also 2) | exit 1, one CSV or wizard test |
| ActionButton | `pnpm test src/components/Common/ActionButton/ActionButton.test.tsx --sequence.shuffle.tests --sequence.seed=1` | exit 1, unhandled `nodeName` error, all tests green |
| Seed accepted at run-id size | `pnpm test src/components/Common/ActionButton/ActionButton.test.tsx --sequence.shuffle.tests --sequence.seed=26000000000` | runs, prints `Running tests with seed "26000000000"` (no 32-bit truncation; a different order from seed 1 is fine) |
| Full run, CI flags, pre-fix | `LIGHTHOUSE_TIME_BUDGET=warn CI=true pnpm test --sequence.shuffle.tests --sequence.seed=11` (once) | exit 1, names `PortfolioDetail`; shows `.tests` alone is enough in the whole suite |

If a row does not reproduce with `.tests`, fall back to the bare `--sequence.shuffle` for the guard, say so in the delta, and
re-run the row; the decision of the maintainer was "tests only", so a fallback is named at the hold, not taken silently.
At 02-01 the final action command line (`pnpm test --sequence.shuffle.tests --sequence.seed="$SHUFFLE_SEED"` with an 11-digit id)
is run once locally on the **fixed** tree and must exit 0, and the same line on a scratch copy of the pre-fix PortfolioDetail test
must exit 1 (the throwaway copy is deleted, never committed).

## Wave: DISTILL / [REF] The guard (slice 02): what it must show

- **02-01 action input and workflow line.** `.github/actions/build-frontend/action.yml` is confirmed as the only place `pnpm test`
  runs in CI (read this wave: step "Test Frontend and Sonar report", `if: inputs.run-tests == 'true'`, `run: pnpm test`, `env: CI: true`;
  `package-app` passes `run-tests: 'false'`). Edit: optional input `shuffle-seed`, `default: ''`; the test step receives it as
  `env: SHUFFLE_SEED: ${{ inputs.shuffle-seed }}`, branches on empty in the shell (empty runs exactly `pnpm test`; set prints
  `Test order seed: $SHUFFLE_SEED`, emits `::notice::Test order seed: $SHUFFLE_SEED`, and runs
  `pnpm test --sequence.shuffle.tests --sequence.seed="$SHUFFLE_SEED"`). Never interpolate the input inside the `run:` text.
  `.github/workflows/ci_frontend.yml` passes `shuffle-seed: ${{ github.run_id }}` in the existing `with:` block, nothing else.
  Checks: `node --test .github/scripts/*.test.mjs` stays 132 of 132; the empty branch run locally prints no seed line and runs the
  plain command; the set branch run locally (shell snippet, `SHUFFLE_SEED=26000000000`) exits per above; `pnpm build` clean.
- **Reporter check (open questions 1 and 2).** Over the four full runs of 01-04, name the budget offenders. A shuffled run whose
  offender is a test that moved out of first place, or an offender that appears only under shuffle, means the per-test guard's
  first-test allowance (declaration order in `observedRunFromVitest.ts`) needs "first by start time"; that is a change to the reporter
  with its own test, in its own commit inside this step, never a looser limit. Also check that `vitest_to_csv.py` output and
  `check_budget.py`'s verdict are the same for a shuffled run's CSV as for a plain one (`Scripts/test-timings/`): same rows, same
  verdict. Result written in the delta either way ("no offender moved" is a result).
- **02-02 `docs/ci-learnings.md`, Tests section.** The replay line of DESIGN, verbatim in meaning: *A frontend CI run is shuffled
  with seed = the run id (printed as "Test order seed" and in the Vitest header); replay with
  `cd Lighthouse.Frontend && pnpm test --sequence.shuffle.tests --sequence.seed=<run id> <the red file>`, and bisect a pair with
  `-t "A|B"`.* Plus the one rule (tests may not depend on another test's set-up or leave shared state changed; the guard checks it),
  the three signatures of a new order dependence (red test in an untouched file that passes alone; same seed red on the previous
  commit; run ends all green with `Errors 1 error`), and the third kind: a time-budget failure on a slow test moved out of first place.
  Check: `grep -n "sequence.shuffle.tests" docs/ci-learnings.md` finds the line, and the file keeps its heading order. Line 118
  (explicit timeout for marginal tests) stays, and the new text says the rule "no longer timeout as the fix" covers order and load work.
  No release notes (written at `/release`).

## Wave: DISTILL / [REF] What closes the Story

1. Roadmap steps 01-01 to 02-02 done, each with its focused commit, `pnpm test` plain and `pnpm build` clean at each. The Story is
   **pushed at its end** (after 02-02), not per step; the push is to `main` per the trunk rule, and the maintainer's go-ahead rules
   as usual; a docs-only push starts no frontend run, so the push that carries 02-01 is the one that counts.
2. After the push: 5 consecutive `main` runs of the frontend job, with 5 different run ids (so 5 seeds), all green including
   "Check test-time budget", each recorded here (run id, seed, result). A red from an order dependence is fixed, counts against the
   cap of 5 extra files and restarts the count; a red from infrastructure is noted and does not reset it.
3. Finalize checks: the `ci-learnings.md` rule present; the load position recorded as an ADO #6539 comment ("not reproduced, no
   fix claimed; watch for `Test timed out in 5000ms` or `pointer-events: none`"); ADO #6539 moved by the usual story rule; the
   one-week review of the guard recorded (revert if reds need a seed to replay and that proves a nuisance), rollback per DEVOPS:
   `git revert --no-commit <02-02> <02-01>` and one commit; the three fixes stay.
4. The DISCUSS Checklist boxes are ticked by evidence: production unchanged (guarantee 3), no longer timeouts or retries
   (guarantee 4), ratchet in the same change (02-01), docs (02-02), mutation N/A (guarantee 7).

## Wave: DISTILL / [REF] Pre-requisites and hold

- Before 01-01: nothing external. Agree the rigor profile `standard` with the maintainer (recorded in the roadmap); pass `model`
  explicitly on every dispatch (the profile's `agent_model` for the crafter, `reviewer_model` for reviewers).
- DESIGN's peer review (D-6539-11) is still owed by the orchestrator; it is not satisfied by this wave.
- A conflict with trunk stops the step and reports; nothing is rebased.
- Self-completeness audit: C2 and C5 to C7 N/A (no state machine, mode flag, error contract or concurrency matrix is added).

## Wave: DISTILL / [REF] Review revision (2026-11-07)

The DISTILL review returned NEEDS_REVISION with no blocker. This section overrides anything above that differs. Both the design
review and this review have now run, so "design peer review owed" (D-6539-11) is closed.

- **Time budget and exit status (H1).** Read in `src/tests/timeBudget/`: `timeBudgetOptions.ts` maps unset or empty
  `LIGHTHOUSE_TIME_BUDGET` to `fail`; `timeBudgetReporter.ts` sets `process.exitCode = 1` only when `verdict.failing`;
  `compareToAllowList.ts` sets `failing: options.mode === "fail" && findings.length > 0`. So **`warn` never sets a failing exit
  code**, and every order-checking run (guarantees 1 and 2, the pre-fix table, 01-04) carries `LIGHTHOUSE_TIME_BUDGET=warn`; the
  results table has a separate offenders column. The CI run itself stays in fail mode (the variable is unset), which is what the
  "third kind" of red in the ledger covers.
- **First-test allowance, checked directly (H2).** `observeFile` marks `firstInFile` as the first non-skipped entry of `file.tests`,
  the collected list. In one shuffled run (a file with at least 5 tests, a seed that visibly reorders it, `LIGHTHOUSE_TIME_BUDGET=warn`),
  02-01 records (a) the order of `file.tests` as `observedRunFromVitest.ts` receives it (temporary, uncommitted log in
  `vitestRunOutcome`, or a probe test over the same function with the run's `testModules`) and (b) each test's start time from
  `test-results.json` (`startTime` of each assertion result, or the sum of preceding durations when absent), and states in the delta
  whether the first in the list is the first to start. **If not, the reporter change (first by start time, its own test, its own
  commit) is required in 02-01 whether or not an offender appears.** If yes, no reporter change, and the evidence is recorded.
  Offenders from 01-04 are then a second, weaker signal only.
- **Guarantee 3 (H3).** Before-commit is `93dc95b80`. Allowed under `Lighthouse.Frontend`: the three Story test files, any further
  test file the sweep adds (named in the delta), `src/tests/timeBudget/` only if the reporter changes; nothing else under
  `Lighthouse.Frontend/src`. Re-run at the end of 01-04, 02-01 and 02-02 (`git diff --name-only 93dc95b80 HEAD -- Lighthouse.Frontend`).
- **01-03 guard check (M1).** The expected red is a rejection from the `TypeError` on `event` of undefined (React reads `window.event`
  when the state is set after unmount with `window` stubbed), landing in the `rejections` array, failing the
  `expect(rejections).toEqual([])` line. **Any other red** (another assertion, a different error, a timeout) **stops at the hold**
  with the output pasted; a green stops at the hold too.
- **01-03 event proof (M2, M3).** The temporary listeners are on `window` and on `document`, installed **before** the stub goes on
  (so `window` listeners exist when the stub hides the global), capture phase, for `focus`, `focusin`, `selectionchange`, `click`.
  An **empty log** on the failing seeds stops at the hold: the event is not one of these, and the cause is unproven. The 300 ms
  timer must run **inside the stub window** (the test advances fake timers while `window` is undefined), and the log must show the
  event fired inside that window; the event source is fixed by the logged event type and target, and a **mismatch** (event outside
  the window, or an event other than the focus test's) stops at the hold, no fix is written on a guess.
- **02-01 pin (M4).** `.github/scripts/action-pins.test.mjs` gains a pin: in `build-frontend/action.yml` the test step reads
  `SHUFFLE_SEED` from `env`, and the text `inputs.shuffle-seed` never appears in a `run:` body. Written first and run **red** before
  the edit (the input does not exist yet, so the step has no `SHUFFLE_SEED` env), green after. This file is added to the allowed
  list of guarantee 3 for 02-01.
- **02-01 CSV order (M5).** Take one `test-results.json`, make a copy whose `assertionResults` are reordered inside each file
  (reversed), feed both through `Scripts/test-timings/vitest_to_csv.py`, and compare the rows **as a set** (sorted rows equal). The
  script sorts by duration, so equal-duration ties may order differently; hence the set comparison. `check_budget.py` gives the
  same verdict on both CSVs.
- **02-02 replay (M6).** Take one file from a 01-04 full run with its seed, run it alone with
  `pnpm test <file> --sequence.shuffle.tests --sequence.seed=<seed>`, and compare the order of its tests with the same file's order in
  the full run's `test-results.json`. The ledger line says replay "reproduces the order" only if they match; if they differ, the
  line says to replay the whole run and the delta records why.
- **Guarantees 4 to 7 (M7).** Guarantee 4 is the mechanical grep, reconciled against added lines; guarantee 5 matches what 02-01
  does and adds the same-seed-twice check; guarantee 6 has a pass condition; guarantee 7 is a waiver, not a check (all edited in
  place above).
- **Stale text (low).** The places that still say seven full runs, "seeds 11 and 1 each showed one offender", "4 of 5 seeds" and the
  dropped stub carry a one-line superseded marker pointing here. The facts: four full runs on 11, 33, 4, 5; only seed 11 had a
  budget offender; the sweep's five bare-flag seeds all went red; the stub stays.
- **01-01 (low).** The update-deferral check is a read-only check, with no red criterion (edited in place above).
- **Roadmap (M8).** `validation.status` is `approved` after this revision, with the review cited in `validation.note`.

## Wave: DELIVER / [REF] Step 01-01 pre-fix reproductions (2026-11-07)

Tree check: `git diff --stat 93dc95b80 HEAD -- Lighthouse.Frontend` printed nothing before the first edit. All runs from
`Lighthouse.Frontend`, `LIGHTHOUSE_TIME_BUDGET=warn`, `test-results.json` removed before each run read for ids. Every row
reproduced with `--sequence.shuffle.tests`; no fallback to the bare flag is needed.

| Pre-fix reproduction | Command (flags after the file) | Exit | Result | Budget offenders |
|---|---|---|---|---|
| PortfolioDetail, `-t "a refresh that failed"` | `-t "a refresh that failed"` (no shuffle) | 1 | 2 failed, 5 passed, 29 skipped: `says so on the refresh button`, `keeps saying so when a forecast run finishes afterwards`; `portfolio-no-access-alert` in the DOM | 0 |
| PortfolioDetail seed 11 | `--sequence.shuffle.tests --sequence.seed=11` | 1 | 2 failed, 34 passed (36): the same two tests | 0 |
| PortfolioDetail seed 33 | `--sequence.shuffle.tests --sequence.seed=33` | 1 | 2 failed, 34 passed (36): the same two tests | 0 |
| PortfolioDetail seed 3 | `--sequence.shuffle.tests --sequence.seed=3` | 1 | 2 failed, 34 passed (36): the same two tests | 0 |
| GeneralSettings seed 1 | `--sequence.shuffle.tests --sequence.seed=1` | 1 | 1 failed, 38 passed: `shows wizard button when selected work tracking system is CSV` | 0 |
| GeneralSettings seed 2 | `--sequence.shuffle.tests --sequence.seed=2` | 1 | 1 failed, 38 passed: `updates all fields when CSV wizard completes with populated data` | 0 |
| ActionButton seed 1 | `--sequence.shuffle.tests --sequence.seed=1` | 1 | 13 passed, `Errors 1 error`: unhandled `TypeError: Cannot read properties of undefined (reading 'nodeName')` | 0 |
| Seed at run-id size | `ActionButton ... --sequence.seed=26000000000` | 0 | prints `Running tests with seed "26000000000"`, 13 passed | 0 |
| Full run, CI flags | `LIGHTHOUSE_TIME_BUDGET=warn CI=true pnpm test --sequence.shuffle.tests --sequence.seed=11` | 1 | names `PortfolioDetail ... keeps saying so when a forecast run finishes afterwards`; `bail` under `CI=true` stopped the run at 1 failed, 109 passed, 358 skipped (526); budget line: 0 offenders | 0 |

PortfolioDetail test count before: 36.

### Step 01-01 results

- **Anchors first.** The positive anchor (the portfolio name is on the page) went into the three absence tests (`stops saying so`
  twice, `leaves the button alone`). With the defaults not yet added, `-t "a refresh that failed"` gave 5 failed, 2 passed: the
  anchored absence tests went red, so they had passed vacuously. `renderAndWaitForTheUpdateFeed` now also waits for the heading.
- **Fix.** One file-level `beforeEach` holds the portfolio and settings defaults the first describe used to set; that describe's own
  `beforeEach` is gone because it set nothing else. `-t "a refresh that failed"` gives 7 passed.
- **Whole file.** 36 tests (same as before): plain and `--sequence.shuffle.tests` on seeds 11, 33, 1, 2, 3, 4, 5, 6, 7, 8 all pass.
- **Update-deferral describe (read-only check).** Alone and shuffled on seeds 11, 33, 7: 2 passed each. Clean, no second leak.
- **Gates.** `pnpm build` exit 0, no errors or warnings, `git status` shows only the two step files. Plain `pnpm test`: 618 files,
  9500 tests passed, 0 budget offenders.

## Wave: DELIVER / [REF] Step 01-03 ActionButton stray event (2026-11-07)

- **Guard check, before any edit.** With `if (isMounted.current)` removed (uncommitted), `-t "leaves the page alone"` failed on
  `expect(rejections).toEqual([])` with `TypeError: Cannot read properties of undefined (reading 'event')`. File restored with
  `git checkout`; no production diff.
- **Pair before, seeds 1, 2, 4, 5, 6.** All tests green, `Errors  2 errors` (`reading 'nodeName'` and `reading 'event'`), exit 1.
  Seed 3: clean.
- **Stray event.** Temporary capture-phase listeners on `window` and `document`, installed before the stub, logged on seed 1:
  `selectionchange` on `#document`, on both, with the 300 ms timer running inside the stub window. Seed 3 logged only the click.
  Not a `focus` event itself: it is the selection change the focus test's `autoFocus` leaves queued, which React's document
  listener handles while `window` is gone. Listeners removed again before the fix.
- **Fix.** The test waits one real tick (`setImmediate`) before `vi.useFakeTimers()`, so the queued event is delivered first.
  The stub stays, no console.error assertion, no timeout.
- **After.** Pair on seeds 1, 2, 4, 5, 6: exit 0, no Errors line. Whole file on seeds 1 and 7: 13 passed.
- **Repeat guard check with the fix.** Guard removed: `-t "leaves the page alone"` red, and the pair on seed 1 red; restored.
- **Gates.** `pnpm build` clean. Plain `pnpm test`: 618 files, 9500 tests passed.

## Step 01-04: local sweep

Run from `Lighthouse.Frontend` with `LIGHTHOUSE_TIME_BUDGET=warn`. Per-file runs add `--sequence.shuffle.tests --sequence.seed=N`;
full runs add `CI=true` and delete `test-results.json` first. Load average is the one-minute figure when the run started.

### Per-file runs (30, all exit 0, no Errors line)

| File | Seeds | Tests | Load average |
|---|---|---|---|
| PortfolioDetail | 11, 33, 1, 2, 3, 4, 5, 6, 7, 8 | 36 passed | 0.43 to 0.89 |
| GeneralSettingsComponent | 11, 33, 1, 2, 3, 4, 5, 6, 7, 8 | 39 passed | 1.22 to 1.71 |
| ActionButton | 11, 33, 1, 2, 3, 4, 5, 6, 7, 8 | 13 passed | 1.41 to 1.70 |

### Full runs, `CI=true`

| Run | Seed | Exit | Result | Errors | Time-budget offenders | Load |
|---|---|---|---|---|---|---|
| 1 | 11 | 0 | 618 files, 9500 tests passed | none | 0 | 1.67 |
| 2 | 33 | 1 | 618 files, 9500 tests passed | 5 x `EnvironmentTeardownError: Closing rpc while "onUserConsoleLog" was pending` | 0 | 16.57 |
| 3 | 4, first attempt | 1 | bail 1 stopped it at 48 of 618 files: `ReportPrintPage.test.tsx` "is still preparing, and not ready, while the report has not arrived", line 148, expected `"false"`, received `undefined` | none | 0 | 14.86 |
| 4 | 5 | 0 | 618 files, 9500 tests passed | none | 0 | 14.39 |
| 3b | 4, second attempt, no fix | 0 | 618 files, 9500 tests passed | none | 0 | n/a |
| 3c | 4, third attempt, with the ReportPrintPage fix | 0 | 618 files, 9500 tests passed | none | 0 | n/a |

No run named a time-budget offender, so the reporter check of 02-01 has none to look for.

### Finding A: ReportPrintPage, seed 4 (fixed, one test file, 3 of 5 extra files used)

- The file alone, `--sequence.shuffle.tests --sequence.seed=4`, failed the same test (31 passed, 1 failed). Seeds 11, 1 passed.
  So the order within the file decides it, and it is not only load.
- Cause as far as the evidence goes: the print route is `lazy(...)`. When this test is the first to need the page, five
  microtask turns in `aMomentLater` pass before the lazy chunk has been drawn, so `[data-render-ready]` is absent and
  `renderReadiness()` is `undefined`. Run after any test that already loaded the chunk, the page is there. That accounts for
  the alone-and-seed-4 failure. It does not by itself explain why the same seed, same flags, passed in the second full run:
  there the chunk load can have been finished by another test in the same worker, which depends on how files are spread
  over workers, so the full-run result is load- and scheduling-dependent. I did not prove that part.
- Fix: `await waitFor(() => thePrintPage())` before `aMomentLater()` in the `it.each` test. After it, the file passed alone
  on seeds 4, 11, 33, 1, 2, 3, 5, 6, 7, 8 (32 passed each) and the full run on seed 4 was green.
- The fix was committed, withdrawn on the coordinator's instruction, then restored on the maintainer's decision.

### Finding B: seed 33 teardown errors (recorded, not fixed)

- All five errors are attributed to `FeatureSizeComponent.test.tsx`; vitest says that only means the file was running when
  the worker closed. The log does not show which test files were still logging. Every test passed.
- Not reproduced: no other full run showed it, and nothing was changed for it. A re-run of seed 33 was not done.
- The run started at load 16.57, so contention is a possible factor, unproven.
