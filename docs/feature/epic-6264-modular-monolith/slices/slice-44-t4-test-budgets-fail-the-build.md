# Slice 44 — T4 Test-time budgets fail the build instead of warning

Epic #6264 · ADO #6462 · `@infrastructure` · job_id: infrastructure-only · **Size S** (plus elapsed time for calibration runs)

## Goal

The CI test-time budgets fail the build, so a slow test file or class is caught on the commit that adds it.

## Why

Story #6302 added per-file (frontend) and per-class (backend) budgets read from the timing CSVs
(`Scripts/test-timings/check_budget.py`, `test-time-budget.json`). Both CI steps still pass `--warn-only`, pending
a recalibration that never happened. Since #6302 closed in mid-October, the frontend build-and-test step on main
went from about 11 min to 16–21 min, and nothing went red.

## IN

- Re-derive both budgets, the large-file limit and every ceiling on at least 5 main runs, after T2 and FE7 have
  landed their first fixes, using #6302's DS-Q1 rule. Drop the allow-list entries that now fall under the budget.
- Remove `--warn-only` from `ci_frontend.yml` and `ci_backend.yml`.
- Show a planted slow file failing the build.

## OUT

- Making tests faster (T2, FE7).

## Dependencies

Soft: after T2 and FE7's first fixes, so the budget is calibrated on the faster distribution. FE7 plans its own
frontend ratchet; whichever lands first owns the frontend budget, and the other drops that half.

## Acceptance criteria

- [ ] Both stacks fail on an over-budget unit outside the allow-list and on a stale allow-list entry.
- [ ] At least 5 main runs after the change are green.
- [ ] The derivation of each budget is recorded.
