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
| D-6539-3 | After the fixes: per-file runs of the 3 files on 10 seeds, then 7 full runs (seeds 1, 11, and fresh 4, 5, 6, 7, 8) | See "Local sweep size" |
| D-6539-4 | CI guard: `--sequence.shuffle.tests` (tests, not files) in the existing frontend run, seed = `github.run_id`, printed | See "CI guard" |
| D-6539-5 | Budget reporter and CSV scripts stay unchanged; verified at B1, adjusted only on evidence | See "Time budget and CSVs" |
| D-6539-6 | One replay line in `ci-learnings.md` Tests section | See "Replaying a red seed" |
| D-6539-7 | Load: record as "not reproduced, no fix claimed"; no change to any Reports file or the two starved-run files | See "Load" |
| D-6539-8 | Two slices: A (fixes and sweep), B (guard and docs) | Each at most a day |
| D-6539-9 | Close: 5 consecutive green `main` runs of the frontend job under the guard, distinct seeds | See "Closing" |
| D-6539-10 | ActionButton: drop the `window` stub, assert the unmounted case by the absence of rejections and console errors; prove the event first by temporary instrumentation | The stub is the hazard; see fix 3 |
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
B1 runs the final action command line once locally with a run-id-sized seed (confirms an 11-digit seed is accepted).

**Files, tests or both: tests only.** All three causes are inside one file. Files run isolated (`pool: threads`, `isolate: true`),
so a file's order cannot change its result in jsdom; shuffling files only moves the load pattern and would add timeout noise
unrelated to order dependence. The bare `--sequence.shuffle` the sweep used shuffles both; its per-file test orders depend on the
seed alone, so the sweep's seeds stay valid. B1 confirms this: `.tests` plus seed 1 must reproduce the old ActionButton error
(run on a throwaway copy of the pre-fix test, or by recording the order); if not, fall back to the bare flag and say so.

**Time budget and CSVs.** The reporter derives "first test of a file" from the file's collected test list
(`observedRunFromVitest.ts`, index in `allTests()`), not from what ran first. Under shuffle the test that pays the cold first
render may then be another one that has no allowance. Seeds 11 and 1 each showed one offender, which the sweep did not name.
Decision: change nothing now; B1 names the offenders of the 7 full runs. If a shuffled run's offender is a test that moved into the
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
