# ADR-237: A Test File or Class Over Its Time Budget Fails the Verify Job

**Status**: Accepted (provisional — taken while the maintainer was away; pending ratification).
**Date**: 2026-10-14
**Feature**: ADO User Story #6302 (the frontend and backend CI verify stages got slower). Follows `backend-test-speed` (ADR-074).
**Decider**: Morgan (Solution Architect), interaction mode = PROPOSE; the recommended options were taken under the maintainer's standing rule for decisions that do not change what a user sees.

---

## Context

In the first two weeks of October the frontend and backend verify jobs went from about 11 and 13 minutes to about 17 and 15. Nearly all of it was test time. A few new test files and classes each cost 30 to 115 seconds, and nobody noticed until the job durations had drifted for two weeks. A single 115 s frontend file merged on 12 October and was only found by working back from the trend.

Both jobs already write per-test timing CSVs: `vitest_to_csv.py` writes `test-timings-frontend.csv` (file, test, duration, outcome) and `trx_to_csv.py` writes `test-timings-backend.csv` (full test name, category, duration, outcome). They are uploaded as artifacts and nothing reads them automatically.

Three facts constrain any check built on them:

- The same commit runs anywhere from 0.55× to 1.13× of its typical time depending on the runner, so a limit must have about 2× headroom and still catch a real regression.
- On CI, Vitest stops at the first failure, so a red run's timing file is partial.
- The backend run includes different test classes from push to push (live-connector suites join only when a connector changed), and coverage makes CPU-bound backend tests several times slower than locally. A backend limit therefore cannot be calibrated on a developer machine, and a limit set before the fixes land would be noisy.

## Decision

1. **One script reads the CSVs.** A new `Scripts/test-timings/check_budget.py`, beside the converters, takes its column names from them. Its core is a pure function (timing rows + budget + allowlist → breaches); the script around it only reads files, prints, and sets the exit code. Developers run the same command locally against a downloaded artifact.
2. **The unit is a frontend file or a backend class**, measured as the sum of its tests' durations. A backend class is the full test name up to the last dot, after removing the argument list (arguments can contain dots, such as URLs).
3. **The budget is a fixed number of seconds per stack**, set from at least five `main` runs after the slow files are fixed. With L the largest unit not on the allowlist over those runs, the budget B is at least 2.1 × L, because the fastest and slowest runners differ by a factor of about 2.05 (1.13 / 0.55) and a unit seen at L on a fast runner must not trip the budget on a slow one. B is also at most 0.55 × the smallest regression it must catch, so that regression still trips it on the fastest runner; for the frontend, a 115 s file gives B ≤ about 63 s. If both bounds cannot hold, the units above B / 2.1 go on the allowlist with a reason and their own ceiling. The calculation is written down next to the number and in `docs/ci-learnings.md`, and is redone if the runners change.
4. **The allowlist is a checked-in JSON file**, one section per stack. Every entry carries a reason and its own ceiling in seconds, set at least 2.1 × that unit's largest observed time for the same runner-spread reason as the budget, so an accepted slow test (a container start-up, say) cannot grow without limit. A frontend entry whose file no longer appears in the timings fails the check; a backend entry is only checked for still existing in source, because the backend set varies per run.
5. **It runs as the last step of each verify job, and only after a green test run.** It cannot hide a test failure, never reads a partial file, and never stops the Sonar steps from running.
6. **Frontend breaches fail the job from the start. Backend breaches print as warnings** until at least five backend `main` runs **after the budget commit** show zero budget warnings (the largest backend unit at or under the budget on each); then one line switches them to failing. The runs the budget was derived from cannot count, because they sit under it by construction. Warn mode softens only budget breaches; a stale allowlist entry fails in both stacks.
7. **It fails closed.** A missing, empty or malformed CSV after a green run fails the step with a message naming the file. On a Python older than 3.12 the script stops at once with a message naming the version it needs.
8. **It proves itself on every run.** The step first runs the script folder's unit tests, including a planted over-budget unit, an allowlisted one and a missing file. This also puts the existing converter tests in CI for the first time.

The failure message names the file or class, its time, the budget, and the allowlist file to edit. It is written as a GitHub workflow-command annotation, with `%`, CR and LF percent-encoded so that a unit name cannot break the line or start another command.

## Alternatives considered

- **Add a budget mode to `summarise.py`.** Rejected: it reads the raw TRX and Vitest reports, not the CSVs, and exists to print a table for people. A gate inside it would mix two jobs.
- **A check inside each test suite** (a Vitest reporter, an NUnit hook). Rejected: two mechanisms in two languages, and NUnit has no clean point at which every timing is known.
- **Frontend file wall time from the Vitest report.** Rejected: needs a new column on a shared artifact, and adds the file's import and environment time, which is shared noise rather than the file's own cost.
- **A percentile of the run's own distribution.** Rejected: the bar rises as the whole suite slows, and the offending file inflates the percentile it is compared against.
- **A speed index against the run's median file.** Rejected: the median file takes milliseconds, so the ratio is noisy and the message is hard to act on.
- **Allowlist inline in the script, or as markers in test files.** Rejected: the first makes every entry an edit to the gate's code; the second needs a source scan and comments that exist only to cite a rule.
- **Backend failing from the first day, or frontend only.** Rejected: the first risks failing pushes on run-to-run noise before calibration; the second leaves the backend slow tests that started this work unwatched.
- **Skip with a warning when the CSV is missing.** Rejected: a check that passes when it has nothing to check proves nothing.

## Consequences

- **Positive**: a slow file or class fails the build on the commit that adds it, with its name and the fix (speed it up or allowlist it with a reason). No second test run; the check costs well under a second. Accepted slow tests are visible in one reviewed file, each with a reason and a ceiling.
- **Positive**: the timing scripts' own tests start running in CI, so a converter change that breaks the CSV columns is caught where it happens.
- **Negative**: the budget is a number that must be revisited if the runner fleet changes speed; the derivation has to stay next to it.
- **Negative**: until the backend switches to failing, a slow backend class only warns, and a warning can be ignored.
- **Negative**: a legitimately slow new test now needs an allowlist entry in the same change, one more file to touch.
- **Neutral**: product behaviour, API contracts and authorization are unchanged; this governs the CI test jobs only. `docs/ci-learnings.md` gains the rule: a new test file over the budget fails CI; speed it up or allowlist it with a reason.

## Amendment 2026-10-14 (provisional, pending maintainer ratification)

**Change to decision 6.** Until at least five `main` runs after the fixes are pushed exist to calibrate on, **both** stacks run the check with `--warn-only`. The first budget is a provisional one: the same formula as decision 3, with L taken from timings measured on a developer machine and from the most recent CI timings for the tests the fixes did not touch.

- **Frontend:** once those five runs exist, the budget is worked out again on them and the frontend switches to failing by deleting `--warn-only` from its step.
- **Backend:** unchanged, it follows decision 6 and switches to failing once at least five backend `main` runs after the budget commit show no budget warning.

**Why.** The fixes this budget depends on were made while the maintainer was away and nothing could be pushed, so no CI run of the fixed suites exists yet. Decision 3 asks for the budget to be set from such runs, and a developer machine runs at a different speed from the CI runners. A failing check on a number nobody has checked against CI could turn the first push red because the number is wrong, not because a test got slow. That would teach people to distrust the check before it ever caught anything. Warn mode still fails on a missing or broken timing file, a broken allowlist, a stale allowlist entry or a failing self-test, so the check is live from the first push. Only the budget comparison is softened, and only until it can be calibrated.
