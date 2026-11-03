# Slice 01a spike: answers before the time-budget guard is built

Run on 2026-11-03. Vitest 5.0.5, @stryker-mutator/core 10.1.1, TZ=Europe/Zurich. All experiments used
throwaway files under `Lighthouse.Frontend/spike-tmp/`, deleted afterwards. Another session was running
on the machine, so no timing conclusion is drawn here.

## 1. Which Vitest hooks deliver per-test duration and the end-of-run results

- Per-test duration: `onTestCaseResult(testCase)`, read as `testCase.diagnostic()?.duration`
  (milliseconds, fractional). Declared in
  `node_modules/vitest/dist/chunks/plugin.d.C8mmycim.d.ts` ("Called after the test and its hooks are
  finished running"). Observed: `tc t1-a state=passed dur=31.37...` for a test that sleeps 30 ms.
- End-of-run set: `onTestRunEnd(testModules, unhandledErrors, reason)` with
  `reason: "passed" | "interrupted" | "failed"`. Walk
  `testModule.children.allTests()` and read `testCase.result().state` and `testCase.diagnostic()`.
  Observed on a green run: `END modules=3 tests=6 states={"passed":6} reason=passed`.
- Failing exit code: a reporter sets `process.exitCode = 1` inside `onTestRunEnd`. Observed: a green
  run whose reporter set it exited with code 1; the same run without it exited 0. The value was
  `undefined` before the reporter ran on a green run, and `1` before it on a failing run, so the
  reporter must only ever raise it, never reset it. There is no "return an error" channel in this API.
- Hook order across files is not stable (t3 before t1 in one run), so the reporter must not depend on
  order; sort before comparing.

## 2. Does `--reporter=default` replace the reporters in vitest.config.ts

Yes. The `test` script is `cross-env TZ=Europe/Zurich vitest run --reporter=default`; the config lists
`default`, `vitest-sonar-reporter` and `json`. Evidence:

    $ ls -la --time-style=full-iso test-results.json sonar-report.xml     # both 2026-11-02 19:08:07
    $ TZ=Europe/Zurich pnpm exec vitest run src/tests/testWorkers.test.ts --reporter=default
      Test Files  1 passed (1)
    $ ls -la --time-style=full-iso test-results.json sonar-report.xml     # unchanged, 19:08:07

The two files were not rewritten, so the CLI flag drops the sonar and json reporters. A reporter added
to the config only runs under `pnpm test` if the flag stops overriding the list.

Package.json change step 01-07 makes: the `test` script becomes `cross-env TZ=Europe/Zurich vitest run`
(drop `--reporter=default`), so the configured reporters, including the time-budget reporter, run.
The cost is that `pnpm test` then also writes `sonar-report.xml` and `test-results.json` (both
gitignored). If that is unwanted, the alternative is to repeat the full list on the command line
(`--reporter=default --reporter=<budget reporter>`); the config route is preferred because there is one
list. Until then, local measurement uses `pnpm exec vitest run --reporter=default --reporter=json`.

## 3. The Stryker environment marker

Verified: `STRYKER_MUTATOR_WORKER` is set, with the worker id as a string, `"0"` for the first worker.

- Source: `node_modules/@stryker-mutator/core/dist/src/child-proxy/child-process-proxy.js:32`,
  `env: { STRYKER_MUTATOR_WORKER: workerId, ...process.env }`. The vitest runner itself reads it
  (`stryker-setup-${process.env.STRYKER_MUTATOR_WORKER ?? 0}.js`).
- Experiment: a one-test throwaway project, `stryker run` with the vitest runner, a test that appends
  `process.env.STRYKER_MUTATOR_WORKER` to a file. Output, once per run of the test:
  `worker=0 pid=181968` (three lines). The Stryker run reported both mutants killed.
- Consequences for the options function: test the marker with `!== undefined`, not truthiness, because
  the first worker's id is the string `"0"`. The marker is present in the process that hosts Vitest
  (same pid as the tests), so it is visible where `vitest.config.ts` is evaluated.
- Side finding: every `vitest.stryker.*.ts` config in `Lighthouse.Frontend/` is a standalone file and
  does not import `vitest.config.ts`. A guard configured only in `vitest.config.ts` therefore never
  reaches a Stryker run; the marker is a second line of defence, not the only one.
- Side finding: with `disableTypeChecks: true` and no `ignorePatterns`, Stryker walks
  `src-tauri/target` and spends over two minutes on binary files (logged as a WARN). Throwaway
  configs should set `ignorePatterns`.

## 4. Can the composite action read `vars`

Not confirmed for composite actions, so do not depend on it.

- GitHub's context documentation says of `secrets`: "not available for composite actions due to
  security reasons" and says nothing equivalent for `vars`
  (docs.github.com, "Contexts", checked 2026-11-03). Its availability table has rows for
  `jobs.<job_id>.steps.*` but none for a composite action's own `runs.steps`, so the docs neither
  promise nor deny `vars` there.
- No existing use in the repo to copy: `grep -rn 'vars\.' .github` finds nothing. The action reads
  `inputs.*`, `runner.os` and `env.SONAR_TOKEN` (`env` set by the calling job).
- Fallback the design uses: do not read `vars` in `.github/actions/build-frontend/action.yml`. The
  calling workflow (`.github/workflows/ci_frontend.yml` calls the action) sets
  `LIGHTHOUSE_TIME_BUDGET: ${{ vars.LIGHTHOUSE_TIME_BUDGET }}` in the job `env:`; composite steps
  inherit the job environment, as `SONAR_TOKEN` already does. An optional action input is the second
  choice. Either way, an unset variable must mean the default mode (`fail`).

## 5. Does the end-of-run hook fire with the full list on a bailed run

It fires, but the list is not full. Experiment: four test files (three passing, one failing), one
worker, `bail: 1`, a reporter logging `onTestRunEnd`. On the second run Vitest's cache ran the failing
file first:

    END modules=4 tests=1 states={"failed":1} reason=interrupted errs=0 exitCodeBefore=1

All four modules arrive, but `allTests()` yields only the test that ran; the other six tests are
absent. On an earlier bailed run where the failing file happened to run last, all seven tests were
present (`reason=interrupted`), so the content depends on how far the run got. Two consequences:

- `reason === "interrupted"` is the signal that the set is partial. The time-budget comparison must not
  report a test as "missing from the run" (or anything derived from a complete list) in that case, and
  the exit code is already 1 from the failure.
- A reporter that raises `process.exitCode` never needs to run when the suite already failed; it must
  still not throw.

## What changes for later steps

- 01-07: drop `--reporter=default` from the `test` script (question 2); `pnpm test` then writes the
  sonar and json files too.
- 01-06 (reporter): use `onTestCaseResult` plus `onTestRunEnd`; set `process.exitCode = 1`, never
  throw, never reset; treat `reason === "interrupted"` as a partial list.
- 02-01 (CI wiring): export `LIGHTHOUSE_TIME_BUDGET` from the calling workflow's job `env:`, not from
  `vars` inside the composite action.
- Options function: Stryker marker is `process.env.STRYKER_MUTATOR_WORKER !== undefined`.
