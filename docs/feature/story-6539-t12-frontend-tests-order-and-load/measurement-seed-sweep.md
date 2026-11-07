# T12 (#6539) measurement: seed sweep and load run

Measured 2026-11-07 on the 12-core dev machine, Vitest 5.0.5, trunk at 57176a17c, no file changed. Nothing here is a fix.

## Commands

All from `Lighthouse.Frontend`, one run at a time, `test-results.json` deleted before each, `LIGHTHOUSE_TIME_BUDGET` unset
(default 6 workers = half the cores, time budget in "fail mode" with limit factor 1):

```
rm -f test-results.json
TZ=Europe/Zurich npx vitest run --sequence.shuffle --sequence.seed=<N>     # N = 11, 33, 1, 2, 3
rm -f test-results.json
LIGHTHOUSE_TEST_WORKERS=24 TZ=Europe/Zurich npx vitest run                 # oversubscribed, unshuffled
```

(`pnpm test` is the same command with `cross-env TZ=Europe/Zurich`.)

### The Vitest flags (read from the installed 5.0.5 source)

- Seed: `--sequence.seed=<n>`. Vitest prints `Running tests with seed "11"` near the top of the log. It has no effect unless shuffle is on.
- Shuffle: `--sequence.shuffle` has two scopes, `--sequence.shuffle.files` and `--sequence.shuffle.tests`.
- The bare `--sequence.shuffle` used here is boolean `true`. In `index.1TqnEahn.js` that sets the sequencer to `RandomSequencer`
  (files) and leaves `sequence.shuffle` truthy (tests and suites inside a file). **So it shuffles both files and tests.**
  Shuffled test order inside a file was seen directly (the ActionButton order below changes with the seed).
- A shuffled file order only changes which file a worker starts first. With 6 workers, files are not isolated from each
  other anyway (`isolate: true`, `pool: threads`), so the failures found are all inside one file: order between tests, not between files.

## Runs

| Run | Files failed | Tests failed | Duration | Load avg at start / end | Notes |
|---|---|---|---|---|---|
| seed 11 | 1 | 2 | 235 s | 1.5 / 6.9 | PortfolioDetail; 1 time-budget offender, not a failure |
| seed 33 | 1 | 2 | 239 s | 6.9 / 8.2 | PortfolioDetail |
| seed 1 | 1 | 1 | 235 s | 8.2 / 8.3 | GeneralSettingsComponent, plus 1 unhandled error in ActionButton.test.tsx (the file counts as passed, the run exits 1) |
| seed 2 | 1 | 1 | 230 s | 8.3 / 7.7 | GeneralSettingsComponent |
| seed 3 | 1 | 2 | 233 s | 7.7 / 7.9 | PortfolioDetail |
| 24 workers, unshuffled | 0 | 0 | 230 s | 7.9 / 27.8 | 618 files, 9500 tests green; exit 1 only because of 114 time-budget offenders |
| extra: 24 workers + 12 busy loops | 2 of 423 | 3 | 912 s | 5 / 26.6 (peak 43) | not a usable run, see "Load" |

Each full run is 618 files, 9500 tests. No run failed on Reports. Time-budget offenders (seeds: 1, 0, 0, 0, 0; load run: 114) are not counted as failures.

## Failures, grouped by cause

### Cause 1: `PortfolioDetail.test.tsx` reads a mock default that only another describe installs (seeds 11, 33, 3)

- Failing: `... Access Tab and Write Controls Visibility > a refresh that failed > says so on the refresh button` and `> keeps saying so when a forecast run finishes afterwards`.
  Same two tests in seeds 11, 33 and 3; seeds 1 and 2 are green.
- Symptom: DOM shows `portfolio-no-access-alert`, the page the component draws when `getPortfolio` returns nothing.
- Cause: the `a refresh that failed` block renders with `renderWithMockApiProvider()` and the module-level `mockGetPortfolio`. The only
  `mockGetPortfolio.mockResolvedValue(portfolio)` is the `beforeEach` of the first describe (`PortfolioDetail component`, line 177).
  The failed-refresh block sits in another describe and has no such setup. `vi.clearAllMocks()` keeps implementations, so the value survives
  from whichever test ran earlier. Run that block before any test of the first describe and `getPortfolio` returns `undefined`.
- Proof by reproduction, no shuffle at all (the leaking state is the missing implementation, not a left-over):
  - `npx vitest run src/pages/Portfolios/Detail/PortfolioDetail.test.tsx -t "a refresh that failed"` gives 2 failed, 5 passed.
  - Adding one first-describe test, `-t "should render portfolio details after loading|a refresh that failed"`, gives 8 passed.
  - Smallest failing order: that block with nothing before it that calls the first describe's `beforeEach`.
- The slice's hypothesis (leaked `mockSubscribeToFeatureUpdates` implementation, queued `...Once` values) is **not** the cause of these two failures. The
  subscribe mock is reset in the block's own `afterEach`. Untested: whether the update-deferral `beforeEach` leaks something else under another seed.
- The other tests of the block (`stops saying so ...`, `leaves the button alone ...`) assert absence, so they pass vacuously in the broken order.
  Fixing the setup may reveal them as weaker than they look.
- Fix: give the block (or the whole file) one `beforeEach` that sets every mock default it needs, instead of borrowing the first describe's.

### Cause 2: `GeneralSettingsComponent.test.tsx` shares a mutable `rbac.isSystemAdmin` (seeds 1, 2)

- Failing: seed 1 `shows wizard button when selected work tracking system is CSV`; seed 2 `handleWizardComplete integration > updates all fields when CSV wizard completes with populated data`.
- Symptom: `Unable to find ... button ... /Select CSV File/i`; only a heading is in the DOM.
- Cause: the `vi.hoisted` object `rbac` is read by the `useRbac` mock. The test `who is offered a board picker > does not offer it to someone who cannot open it`
  sets `rbac.isSystemAdmin = false` and nothing sets it back. Two other describes reset it in their own `beforeEach`; the CSV and wizard tests do not and rely on the initial `true`.
  Opening a wizard button needs a system administrator.
- Proof by reproduction: `-t "does not offer it to someone who cannot open it|shows wizard button when selected work tracking system is CSV" --sequence.shuffle --sequence.seed=N`:
  seeds 1, 2, 4 run the "cannot open" test first and the CSV test fails; seed 3 runs them the other way round and both pass.
- Fix: reset `rbac.isSystemAdmin = true` in the top-level `beforeEach`, and have the one test that needs `false` set it locally.

### Cause 3: `ActionButton.test.tsx` stubs `window` to `undefined` (seed 1, unhandled error)

- Failing: no test fails; Vitest reports `TypeError: Cannot read properties of undefined (reading 'nodeName')` from react-dom as an unhandled error, "originated in
  ActionButton.test.tsx", last test `leaves the page alone when its minimum wait ends after it is gone`. The run exits 1 with 9499 green.
- Cause: that test does `vi.stubGlobal("window", undefined)` and then advances timers. If a DOM event reaches React's document-level listener during that gap with a target React
  does not own, React falls back to `window.nodeName` (react-dom-client line 21264) and throws. Which event arrives depends on what the test before it left behind.
- Proof by reproduction, single file: `npx vitest run src/components/Common/ActionButton/ActionButton.test.tsx --sequence.shuffle --sequence.seed=1` gives the error, twice in a row.
  Seeds 7 and 8 are clean. Smallest pair: `-t "takes the focus when asked to|leaves the page alone"` fails with errors whenever the focus test (`autoFocus`) runs first (seeds 1, 2, 4, 5, 6) and is clean when it runs second (seed 3).
- Not proven: which exact event (focus or selection) arrives. The focus test is the trigger, the mechanism is inferred.
- Fix: do not stub `window`. Drop the stub and let the test prove the "page is gone" path another way, or flush pending focus events (one awaited tick) before stubbing.

## Load

- Oversubscribed run as specified (24 workers, unshuffled): load average 7.9 at start, 27.8 at end, and no test failed. The slowest test was 9.99 s
  (`RefinementView.paging`, which has its own timeout); no Reports test took more than 3.9 s (`BulkReportWizard`, `ReportsPage.combinedPdf`, `ReportsPage.removal`). Wall time was the
  same 230 s as at 6 workers, so the machine was already saturated. **This run did not reproduce the Reports 5 s timeouts or the `pointer-events` failure from 2026-11-04.**
  What those two need is not measured here. The 114 budget offenders are slow tests, not failures.
- Extra run, 12 busy-loop processes plus 24 workers (about 36 runnable processes per core-pair, load up to 43): 912 s, and 195 test files never ran: `[vitest-pool-runner]: Timeout waiting for worker to respond`, "Failed to start threads worker".
  That is the pool failing to start under starvation, not a test fault, so the run is not comparable. Of the 423 files that did run, 2 failed:
  - `src/hooks/useModifySettings.validation.test.ts > warns the coach when the settings the auto-save just persisted do not validate`, 5 s timeout, 9.7 s wall. It sits in a `waitFor` chain on
    `saveState === "saved"` then `validationError`, with fake timers on `shouldAdvanceTime` (real wall time drives them).
  - `src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryCreateModal.rulebased.test.tsx`, first test of the file, 5 s timeout, 53 s wall: it waits for `delivery-rule-builder` to appear (schema load plus first render);
    the second failure there (`validateRules` never called) is a default 1 s `waitFor` expiring.
  Both are first-render or timer-driven waits that starve. Treat as indicative only, not as the 2026-11-04 failures.
- Reports files: zero failed in all seven runs.

## Extra files against the maintainer's limit of 5

Known: `PortfolioDetail.test.tsx`, and the Reports files (none failed in this sweep).
Extra files found by the 5-seed sweep: **2** (`GeneralSettingsComponent.test.tsx`, `ActionButton.test.tsx`). Both from seed 1; GeneralSettings also from seed 2.
The CPU-hog run adds 2 more files (`useModifySettings.validation.test.ts`, `DeliveryCreateModal.rulebased.test.tsx`) but under an unrepresentative load; counting them gives 4, still under 5.
Three causes in total, and no cause crosses files: none of the three is a shared setup fault (`setupTests.ts` is clean here).

## Recommended fix per cause (none is a timeout or a retry)

| Cause | Fix |
|---|---|
| 1 PortfolioDetail | One `beforeEach` at the top that sets all mock defaults for the file |
| 2 GeneralSettingsComponent | Reset `rbac.isSystemAdmin = true` in the top-level `beforeEach` |
| 3 ActionButton | Remove `vi.stubGlobal("window", undefined)`; prove the unmounted case without it |
| Load (indicative only) | Wait on the thing itself (rule builder, saved state) with a state-based `findBy`, not on elapsed time; decide after the 5 green `main` runs |

## Surprising

- `--sequence.shuffle` found three causes in five seeds, but only one of the two files the slice named; the Reports files never failed.
- The user-visible slice hypothesis (queued `Once` values, an unreset subscribe implementation) was wrong for the PortfolioDetail failures: it is a missing default, reproducible with no shuffle.
- An unhandled error fails the run with every test green (seed 1), so a shuffled CI run can go red with "9500 passed".
- At 24 workers the machine did the work in the same 230 s, so oversubscribing alone did not reproduce the load failures.
