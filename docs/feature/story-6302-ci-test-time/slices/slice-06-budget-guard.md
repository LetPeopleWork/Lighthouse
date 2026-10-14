# Slice 06 — A slow test file fails the build the day it lands

**Goal**: The verify jobs fail when a frontend test file or a backend test class exceeds a calibrated time budget and is not on a reasoned allowlist.

**Owner story**: US-06. **Effort**: ½ day. **Reference class**: `backend-test-speed` slice 04 (allowlist + regression guard).

**Learning hypothesis**
- Disproves "a budget can catch a slow file without tripping on runner variance" if, calibrated on the post-fix distribution, it either misses a 100 s file or fails normal runs.
- Confirms it if a planted slow file goes red and ≥5 normal runs stay green.

## IN scope
1. Read the `test-timings-*.csv` the jobs already produce (extend `Scripts/test-timings/`).
2. Budget derived from the post-fix distribution with headroom for the 0.55–1.13 runner spread; derivation recorded.
3. Allowlist with a reason per entry (seeded from slice 05).
4. `docs/ci-learnings.md` rule.

## OUT scope
- A job-duration alarm (the per-file signal is the earlier one).
- Budgets per individual test.

## Acceptance criteria
AC-06.1..AC-06.4 in `feature-delta.md`.

## Dependencies
Slices 02–05 landed (budget is calibrated on the fixed suite).

## Taste tests
One script change, one data file, one doc entry; real CI data.
