# Slice 04 — New backend integration classes stop repeating set-up

**Goal**: The new `API.Integration.Refinement.*`, `Reports.*` and `TeamFeatureSize.*` classes (~300 s summed) drop by at least a third, with every test kept.

**Owner story**: US-04. **Effort**: 1 day. **Reference class**: `backend-test-speed` slice 06 (per-fixture WAF reuse, read-API cluster).

**Learning hypothesis**
- Disproves "set-up, not test bodies, is the cost" if `FixtureSetupTimer` shows set-up is a small share.
- Confirms it if set-up dominates and fixture reuse cuts it, which argues for reuse as the default for new integration classes (DESIGN decides).

## IN scope
1. Measure set-up vs body per class with `FixtureSetupTimer` before changing anything.
2. Adopt fixture reuse where set-up dominates and tests do not write shared state.
3. Keep the `[NonParallelizable]` allowlist guard and `docs/ci-learnings.md` isolation rules green.

## OUT scope
- Older integration classes that did not grow.
- The single slow old tests (slice 05).

## Acceptance criteria
AC-04.1..AC-04.3 in `feature-delta.md`.

## Dependencies
None on frontend slices. Copy the premium licence fixture into the worktree first.

## Taste tests
One pattern, already in the codebase; real CI timings.
