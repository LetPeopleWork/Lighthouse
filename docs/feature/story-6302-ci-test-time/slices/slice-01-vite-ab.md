# Slice 01 — Settle the 13 Oct dependency-bump suspicion

**Goal**: Find out by A/B whether vite 8.3.4 (or the MUI / pnpm 12.10 bumps of the same day) slowed every frontend test by ~9 %.

**Owner story**: US-01. **Effort**: ½ day. **Reference class**: dependency bisect.

**Learning hypothesis**
- Disproves "a dependency bump slowed every test" if both arms' medians sit within each other's spread.
- Confirms it if one arm is slower beyond the spread, and the fix shows on the A/B.

## IN scope
1. Same machine, same test set (pin to a commit), ≥3 `pnpm vitest run` runs per arm: vite 8.3.2 vs 8.3.4.
2. If vite is clean: the same A/B for the MUI bump (641ac9c6f), then pnpm 12.10 (1bbde5105).
3. Record medians and spreads in `feature-delta.md`; fix (pin / config / upstream issue) if one is guilty.

## OUT scope
- Judging the effect from CI runs alone (runner variance 0.55–1.13).
- Any test-file change.

## Acceptance criteria
AC-01.1, AC-01.2 in `feature-delta.md`.

## Dependencies
None. Runs first: its result shifts the baseline the other frontend slices measure against.

## Taste tests
Thin (no new component); production test suite, not synthetic; dogfood = the next `pnpm test` run.
