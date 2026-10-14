# Slice 02 — The two slowest new frontend files

**Goal**: `RefinementView.paging.test.tsx` (115 s) and `WorkItemsDialog.contexts.test.tsx` (105 s) run in ≤40 s together on CI with every test and assertion kept.

**Owner story**: US-02. **Effort**: 1 day. **Reference class**: test-performance refactor.

**Learning hypothesis**
- Disproves "these files are slow for a fixable reason" if, after profiling, the time is in assertions that genuinely need a full render of many rows.
- Confirms it if the time sits in avoidable set-up (oversized fixtures, a full page render per `it.each` case, `waitFor` running to its timeout), and gives slice 03 its pattern.

## IN scope
1. Profile both files locally (per-test times; where a test waits).
2. Change how the tests set up and wait, not what they assert (D7).
3. One line per file in `feature-delta.md` naming why it was slow.

## OUT scope
- Production code changes unless a test exposes a real slowness a user would also feel (then raise it, don't fold it in).
- The other six files (slice 03).

## Acceptance criteria
AC-02.1..AC-02.3 in `feature-delta.md`.

## Dependencies
Slice 01 result recorded (baseline).

## Taste tests
Two files; no new abstraction unless both share one (then it ships here, used by both); production CI timings.
