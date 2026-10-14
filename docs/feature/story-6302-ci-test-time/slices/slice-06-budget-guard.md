# Slice 06 — A slow test file fails the build the day it lands

**Goal**: The verify jobs fail when a frontend test file or a backend test class exceeds a calibrated time budget and is not on a reasoned allowlist.

**Owner story**: US-06. **Effort**: ½ day of work, spread over elapsed time: calibration needs ≥5 `main` runs after slices 02–05 land, and the backend flip from warn to fail needs ≥5 more `main` runs after the budget commit. The waiting is calendar time, not effort. **Reference class**: `backend-test-speed` slice 04 (allowlist + regression guard).

**Learning hypothesis**
- Disproves "a budget can catch a slow file without tripping on runner variance" if, calibrated on the post-fix distribution, it either misses a 100 s file or fails normal runs.
- Confirms it if a planted slow file goes red and zero non-allowlisted units are over the budget across the ≥5 `main` runs after the budget commit (the calibration runs sit under it by construction, so they prove nothing).

## IN scope
1. Read the `test-timings-*.csv` the jobs already produce (extend `Scripts/test-timings/`).
2. Budget per stack: B ≥ 2.1 × L (L = largest non-allowlisted unit over ≥5 post-fix `main` runs; the 0.55–1.13 runner spread is a ratio of about 2.05) and B ≤ 0.55 × the smallest regression to catch (frontend ≈ 63 s). Units breaking the lower bound go on the allowlist with a reason and their own ceiling. Derivation recorded.
3. Frontend fails from the start; backend warns, and flips to failing once ≥5 backend `main` runs after the budget commit show zero budget warnings.
4. Create `Scripts/test-timings/test-time-budget.json`: the budget per stack and the allowlist, each entry with a reason and its own ceiling (seeded from slice 05).
5. Implement `check_budget.py` so the pending acceptance tests in `Scripts/test-timings/tests/test_check_budget.py` pass.
6. Add the check as the last step of both verify jobs (`ci_frontend.yml`, `ci_backend.yml`), running the timing-script self-tests in the same step.
7. Add `Scripts/test-timings/**` and the budget file to the trigger paths in `ci.yml` and `ci_changes.yml`, so a change to the guard alone starts both verify jobs.
8. `docs/ci-learnings.md` rule.

## OUT scope
- A job-duration alarm (the per-file signal is the earlier one).
- Budgets per individual test.

## Acceptance criteria
AC-06.1..AC-06.4 in `feature-delta.md`.

## Dependencies
Slices 02–05 landed (budget is calibrated on the fixed suite). Then a wait: ≥5 post-fix `main` runs before the budget can be calibrated, and ≥5 more after the budget commit before the backend flips. Others' pushes produce these runs; the slice stays open until they exist.

## Taste tests
One script change, one data file, one doc entry; real CI data.
