# Slice 2 facts: preparation for the configuration spike (2026-11-03)

Untimed preparation only. No A/B run, profile or full suite was run; the only runs were two small smoke checks.

## Worktrees

Both detached at `c98f49b05`, each with `pnpm install --frozen-lockfile --ignore-scripts` done in `Lighthouse.Frontend`.

| Arm | Path | State |
|---|---|---|
| Baseline | `/storage/repos/Lighthouse-fe7-arms/baseline` | unchanged `c98f49b05` |
| Candidate | `/storage/repos/Lighthouse-fe7-arms/candidate` | `c98f49b05` plus uncommitted draft (below) |

Smoke check of the baseline: `pnpm exec vitest run src/tests/timeBudget/` gave 4 files, 109 tests, all passing.
The main checkout's `Lighthouse.Frontend` is untouched.

## Scripts (`measurement/scripts/`)

| Script | Use |
|---|---|
| `measure.sh` | Copy of the T3 sampler (one CSV line: label, exit, wall s, peak RSS MB, min MemAvailable MB, max threads). |
| `ab.sh <out-dir> <pairs> <baselineFE> <candidateFE> [candidate vitest args...]` | Alternates A, B, A, B for N pairs at `LIGHTHOUSE_TEST_WORKERS=6`, `TZ=Europe/Zurich`, `LIGHTHOUSE_TIME_BUDGET=off`. Each run is `pnpm exec vitest run --reporter=default --reporter=json` through `measure.sh`; saves `<label>.json.gz` and `<label>.log`, appends the CSV line and the anchored `Duration` line to `runs.txt`. `SHUFFLE=<seed>` adds `--sequence.shuffle --sequence.seed=<seed>` to the candidate arm only. |
| `ab_report.py <out-dir>` | Run as `python3 -I`. Median and min-max wall per arm, peak RSS per arm, verdict items 1 (median -10 % and non-overlapping ranges), 2 (identical sorted `file :: fullName` lists, no failed test) and 3 (memory at most +10 %). Items 4-6 (three green runs, shuffled runs, CI arm) are the lead's. |
| `perFileSplitReporter.mjs`, `split.py` | Per-file time split, see below. |

`ab_report.py` was tried on `plain-1.json.gz` (as A) against `plain-2.json.gz` (as B) with their CSV lines: 9,049 ids in each, 0 failed, identical lists, memory -10.0 % (PASS), wall -2.4 % (FAIL, as it should be for two baseline runs).
The saved JSONs hold absolute paths; the report cuts them at `Lighthouse.Frontend/` so the two worktrees compare equal.

Example: `SHUFFLE=7 scripts/ab.sh /tmp/ab-node 5 /storage/repos/Lighthouse-fe7-arms/baseline/Lighthouse.Frontend /storage/repos/Lighthouse-fe7-arms/candidate/Lighthouse.Frontend`.
For the project split, the candidate arm needs no extra args; the config carries it.

### What Vitest 5.0.5 offers per file

- The stock JSON reporter has only `startTime` and `endTime` per file. That span covers import, environment, setup and tests together, so it cannot be split. `split.py <test-results.json>` prints the summed span and the top files by it, nothing more.
- Vitest's reporter API does carry the split: `testModule.diagnostic()` returns `environmentSetupDuration`, `prepareDuration`, `collectDuration` (import plus suite bodies), `setupDuration`, `duration` (tests and hooks), `importDurations` (per imported module) and `heap` (only with `logHeapUsage`). The stock reporters do not write them.
- `perFileSplitReporter.mjs` is a 20-line reporter that writes those five numbers per file to `$SPLIT_OUT`; `split.py per-file-split.json [top-n]` sums them and lists the top files. Use: `SPLIT_OUT=/tmp/split.json pnpm exec vitest run --reporter=default --reporter=/abs/path/perFileSplitReporter.mjs`. Tried on the 4-file `timeBudget` folder only: works (0.6 s per file there, 73 % environment).
- Not available: a per-file split of transform versus import (only the summary line has "transform", "import", "environment", "setup", "tests" percentages), and `importDurations` needs `experimental.importDurations` to be switched on.
- Vitest 5 also ships `vitest doctor`, which measures `pool: 'threads'`, `vmThreads`, `vmForks`, `happy-dom`, `isolate: false`, `threads + isolate: false`, `fsModuleCache: true` and a `maxWorkers` probe against a baseline. It is a ready-made screening tool for the candidates below (and shows `fsModuleCache` as one more candidate). Not run here.

## Candidate arm (candidate worktree only, uncommitted)

- `setupTests.node.ts`: the locale pins and the console mocks of `setupTests.ts`; no jest-dom, user-event, `ResizeObserver`, `scrollIntoView` or `localStorage` mock.
- `src/tests/domTsTestFiles.ts`: the `.test.ts` files that stay in jsdom.
- `vitest.config.ts`: two `projects` entries with `extends: true`. `node` (`environment: "node"`, `setupTests.node.ts`, include `src/**/*.test.ts`, exclude the DOM list) and `jsdom` (`setupTests.ts`, include `src/**/*.test.tsx` plus the DOM list). The root `setupFiles` was moved into the jsdom project because setup files are added together with the root's, not replaced (first attempt: all 125 node files failed on `Element is not defined` from `setupTests.ts`). The root `exclude` became a shared `baseExclude`.
- Note: reporters, `maxWorkers`, `pool`, `isolate` and the time-budget reporter stay at the root, so the guard still loads.

### File counts

| | `.test.ts` | `.test.tsx` | Total |
|---|---|---|---|
| All | 188 | 394 | 582 |
| Node project (no DOM word in the file) | 125 | 0 | 125 |
| jsdom project | 63 | 394 | 457 |

(The design's "184" was an earlier count; today it is 188.) A `.test.ts` file counts as DOM when it mentions any of: `@testing-library`, `renderHook`, `document`, `window`, `localStorage`, `sessionStorage`, `screen`, `navigator`, `HTMLElement`, `Element`, `jsdom`, `vitest-environment`, `Event(`, `MouseEvent`, `KeyboardEvent`, `location`, `ResizeObserver`, `IntersectionObserver`, `matchMedia`, `File(`, `FileReader`, `render(`. This is a word grep over the test file only; a node file may still reach DOM code through an import, which is what the smoke run checks. `vitest list --filesOnly` confirms 582 files in total and 457 in `--project jsdom`.

### Node project smoke: `pnpm exec vitest run --project node`

125 files, 1,786 tests, all passed, Duration 24.1 s (transform 54 %, import 32 %, tests 10 %) at 6 workers on a machine another session is using, so the time is not a measurement. No test edited. One thing to know: five files print `Error starting SignalR connection: Cannot resolve '/api/updateNotificationHub'` on stderr (`DataRetrievalWizardRegistry`, `RefinementCadenceSettings`, `PrintFrameHost.budget`, `printPageState`, `reportTypes`). A relative hub URL does not resolve without a `location`; it is caught and logged, does not fail a test, and jsdom (which has a location) hides it. Candidate for a sentence in the adoption notes, not a blocker.

## Why `@mui/` is inlined

Found. Two commits: `c6b16e527` ("inline @mui/material for vitest ESM resolution", 2026-06-18) and `0c0c11eba` (same day, widened to the regexes). Quote from `0c0c11eba`: "The string "@mui/material" inline was insufficient: the failing tests also vi.mock @mui/x-date-pickers, whose importActual pulls transitions that deep-import react-transition-group/TransitionGroupContext (a directory with a main/module redirect package.json). Node's native ESM resolver — used by Vitest for any non-inlined dep, including importActual — rejects directory imports with ERR_UNSUPPORTED_DIR_IMPORT. Vite/Rollup (the production build) resolve them fine, which is why pnpm build passed while the suite failed." (From `c6b16e527`: "MUI 9.1.1's Transition.mjs uses a directory deep-import of react-transition-group/TransitionGroupContext".) `@mui/x-data-grid` had been inlined earlier, from the commit that added the DataGrid (`a51ed8d8f`, no reason given).

Consequence for candidate (b): simply not inlining will fail with `ERR_UNSUPPORTED_DIR_IMPORT`. The variants worth measuring are: inline `react-transition-group` and `@mui/x-*` only, leaving `@mui/material` external; or keep them external and let `deps.optimizer` pre-bundle them; or alias the one deep import. Each needs the full suite green, since the failure shows only in tests that `vi.mock` MUI with `importActual`.

## CI runner facts

- `ci_frontend.yml:13`: `runs-on: ubuntu-latest`. The repository is public, so this is GitHub's standard Linux runner for public repositories: 4 vCPU, 16 GB RAM (GitHub docs, "GitHub-hosted runners" table; not re-fetched from the web here).
- Vitest 5.0.5 default for `vitest run`: `getDefaultMaxWorkers(watch)` = `max(availableParallelism() - 1, 1)` (watch: half). On 4 vCPU that is **3 workers**. CI sets no cap (`resolveTestWorkers` returns nothing in CI), so CI uses 3 workers; a developer machine with 12 cores uses 6 through the cap.
- Run 41619566770: still `in_progress` when checked; its frontend job (125861697719) was not finished, so the `Time budget: ... N workers, M processors` line is not fetched. Command for later: `gh api --allow-escape-sequences repos/LetPeopleWork/Lighthouse/actions/jobs/125861697719/logs | sed 's/\x1b\[[0-9;]*m//g' | grep -a "Time budget"`. It will confirm or correct the 3 workers above.
- Current pool: `pool: "threads"`, `isolate: true`, `fileParallelism: true`, `maxWorkers: workers`, `bail: 1` when `CI` is set.

## Candidates for the A/B

| | Candidate | State | Notes |
|---|---|---|---|
| (a) | `node` project for DOM-free `.test.ts` | drafted, node project smoke green | 125 of 582 files (1,786 tests). Expect a gain only on environment cost; the node project skips jsdom start-up and the heavy setup file. |
| (b) | MUI inlining off or pre-bundled | not drafted | Needs one of the three variants above because of `ERR_UNSUPPORTED_DIR_IMPORT`. |
| (d) | `isolate: false` for the node project only | not drafted (one line in the node project) | Shared module state shows only under `--sequence.shuffle`; run the three shuffled runs. |
| (e) | `pool: forks` / `vmThreads` / `vmForks` vs the current `threads` | not drafted | Current pool is `threads` with `isolate: true`. `vmThreads` is the memory-risk one (item 3 of the verdict). `vitest doctor` can screen them first. |
| (c) | happy-dom for the DOM project | not drafted, last | Only if (a)+(b) leave environment above 10 % of worker time; fidelity files per refinement m9. |
| (f) NEW | Worker count | not drafted | See below. Also `fsModuleCache: true` (Vitest 5; in `vitest doctor`'s list) is worth adding to the same screening. |

### (f) worker count

Calibration: 12 workers ran the full suite in about 221 s against about 250 s at 6 (-12 %), with each test 1.66x slower (median per test). Two places set the count:

- **Local**: `LIGHTHOUSE_TEST_WORKERS`, default half the cores (`testWorkers.ts`, `LOCAL_WORKER_FRACTION`, mirrored in the csproj). Changing the fraction changes every developer's run and both stacks' agreement, so (f) locally is a change to that constant in two places.
- **CI**: no cap, so Vitest's default (cores - 1, which is 3 on a 4 vCPU runner). A higher count there would need an explicit `maxWorkers` or `--maxWorkers` in `ci_frontend.yml`/the build action, and on 4 vCPU more workers than cores only adds contention; the gain in the calibration came from using all 12 cores on a 12-core machine, which CI does not have.

So (f) can show a local gain that does not carry to CI, and it extends the planned candidate set; it is measured with the same A/B rule, the arms differing only in `LIGHTHOUSE_TEST_WORKERS` (`ab.sh` defaults both arms to 6; for (f) run it with `WORKERS_B=12`).

## What blocks the timed runs

Nothing found. Open points for the lead: the candidate worktree is uncommitted (commit it or leave it, `git diff` shows the draft); run 41619566770's frontend job was still running; candidates (b), (d), (e) are not yet written into the candidate config.

## Timed part (quiet machine, 2026-11-03, 17:00)

Both worktrees were moved to `614772515` (main with slices 4 to 6) and reinstalled with `pnpm install --frozen-lockfile --ignore-scripts`; every candidate patch still applies there.
The new `.test.ts` files of slices 4 to 6 are all DOM-free (one only says "window" in its data), so the node-project candidate needs no change to its DOM list.

### Per-file split of one full baseline run

Command and output: `split-2026-11-03/summary.txt` (the split reporter's `per-file-split.json` and the run log are next to it).
6 workers, time budget off: 593 files, 9,191 tests, all passed, wall 279 s, peak tree memory 4.1 GB.

| Part (summed over all files) | Seconds | Share |
|---|---|---|
| Environment set-up (jsdom) | 297.5 | 18.5 % |
| Collect (import of the test file and everything it pulls in, plus suite bodies) | 487.7 | 30.3 % |
| Setup files | 72.6 | 4.5 % |
| Tests and hooks | 746.4 | 46.4 % |
| Prepare | 3.8 | 0.2 % |
| Total worker time | 1,608 | |

So more than half of all worker time (53 %) is fixed cost per file before a test runs. Environment set-up is about 0.5 s per jsdom file;
a file that needs no DOM pays it for nothing, which is what the node-project candidate removes. Import is the larger part and is what the
MUI-inlining, optimizer and module-cache candidates attack.

The slowest files split two ways: most are dominated by their tests (`WorkItemsDialog.contexts` 23.5 of 25.1 s, `ModifyProjectSettings` 21.1 of 22.4 s,
`RefinementView.paging` 19.2 of 21.0 s), which only the rewrite slices can shrink; a few are dominated by import (`GeneralSettingsComponent`
19.1 s of collect in a 22.5 s file, `EditConnection.defaults` 9.3 s).

### CPU profile of two slow files

Command per file in `profile-2026-11-03/<file>/command.txt`, one fork, one worker; summary per file in `summary.txt`, the gzipped `.cpuprofile` next to it.

- **`WorkItemsDialog.contexts.test.tsx`** (heavy in tests; 108 tests, 18.6 s alone): jsdom 27.8 % of self time, React and react-dom 23.5 %, `@asamuzakjp/dom-selector` 6.2 %,
  the MUI DataGrid 3.9 %. The top function is jsdom's `collectCandidateStylesFrom` (7.5 %), which is jsdom computing styles; that is what
  `getByRole` and visibility checks call for every element they test. Rendering fewer rows and asking fewer role queries is the lever,
  not configuration.
- **`GeneralSettingsComponent.test.tsx`** (heavy in import; 33 tests, 10.4 s alone, with transform 41 % and import 31 %): 43 % of samples are idle, i.e. the
  worker waits for Vite to transform modules; of the busy time most is module evaluation (`Script`, Vitest's `_runInlinedModule`, `wrapSafe`).
  Alone it collects in a few seconds; in the full run its collect took 19 s, so the cost is the transform queue it shares with the other workers.
  This is the case the inlining, optimizer and module-cache candidates are for.
