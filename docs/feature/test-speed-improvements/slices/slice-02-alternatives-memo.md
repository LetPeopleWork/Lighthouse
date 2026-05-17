# Slice 02 — Alternatives memo grounded in baseline data

**Goal (one sentence)**: Publish `docs/feature/test-speed-improvements/alternatives.md` — a one-page scoring of each candidate mechanism (CS-A…CS-F + combinations) against the Slice-01 timing data — and recommend the top-1 or top-2 picks for the next slice opening.

**Owner story**: US-02.

**Estimated effort**: ≤ 1 day. Reading + numerical analysis + writeup. No production / test code changes.

**Learning hypothesis**:
- Confirms: One candidate (or a stack of two) clearly dominates the others on the data — i.e. the recommendation is data-driven, not aesthetic.
- Disproves: If multiple candidates score similarly, the memo says so and recommends a cheap experiment (e.g. CS-E config tune as a half-day probe) before committing to a heavier candidate.

## IN scope

- Read the Slice-01 timing CSVs from a representative PR build and from a `main` build.
- For each candidate (CS-A, CS-B, CS-C, CS-D, CS-E, CS-F): fill the memo template with the hypothesis it disproves, an effort estimate, coverage-invariant impact, expected wall-clock impact CALIBRATED to the actual baseline numbers, and a recommendation (open as slice | hold | reject).
- For the combinations called out in `feature-delta.md` (CS-B+CS-A; CS-F+CS-D; CS-C alone): score the same way.
- Conclude with a ranked top-1 or top-2 picks.
- Update ADO #5020 with a comment linking to the published memo (no child stories; user-directed).

## OUT scope

- Implementing any candidate (those are separate, conditional slices).
- E2E candidates (out per D6).
- New candidate categories beyond CS-A…CS-F (the catalog is deliberately closed at DISCUSS time; if a missing category appears later, DISCUSS reopens via `/nw-update` or a follow-up DISCUSS pass).
- Vendor telemetry / phone-home (out per `feature-delta.md`).

## Acceptance criteria

- AC-02.1 … AC-02.4 from `feature-delta.md` US-02.

## Dependencies

- Slice 01 (US-01) — must publish at least one CI run worth of timing data before this slice can score the candidates honestly. Recommended: 3 PR builds + 1 `main` build of baseline data to smooth noise.

## Reference class

Architecture / decision memo with a numerical backbone. Similar in shape to the ADR set under `docs/product/architecture/adr-*.md` but feature-scoped, not project-scoped.

## Pre-slice SPIKE

Not required. The memo IS the spike for the candidate slices.

## Taste tests

- Ship 4+ new components? **No** — one markdown memo + an ADO comment.
- Depends on a new abstraction? **No**.
- Disproves something? **Yes** — the assumption that any single candidate is obviously right.
- Synthetic data only? **No** — driven by real Slice-01 CSVs.
- Identical-except-for-scale duplicate of another slice? **No**.

All taste tests pass.

## Note to the maintainer

This slice is the explicit "don't narrow too early" gate. If the memo finds that the data does not support any of CS-A…CS-F cleanly, the right outcome is to open a half-day probe (CS-E being the cheapest), feed those results back into the memo, and only then open a heavier candidate slice. Better to delay a week than ship the wrong fix.
