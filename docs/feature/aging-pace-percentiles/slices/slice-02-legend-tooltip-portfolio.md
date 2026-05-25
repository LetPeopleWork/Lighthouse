# Slice 02 — REMOVED (folded into slice 01 on 2026-05-25)

> **This slice no longer exists as a separate delivery.** It is kept as a tombstone because
> the sibling `state-time-cumulative-view` slices link to it as a reference class.

## What happened

The feature was scoped down with the user to "the coloring with the toggle, nothing more".
Slice 02 originally carried US-02 (legend chip *group*), US-03 (in-flight dot tooltip
annotation), and the portfolio-scope half of US-01. After simplification:

- **US-02 collapsed** from two independent chip groups (each with per-percentile sub-toggles)
  to a **single on/off chip** for the whole band overlay — trivial, so it folds into slice 01.
- **US-03 removed** entirely (no dot tooltip annotation, no per-band hover tooltip, no
  low-sample messaging). The colored column background carries the signal on its own.
- **Portfolio scope is no longer a separate slice.** The Work Item Aging chart is not a
  team-only concern; it renders in both team and portfolio scope via the shared
  `BaseMetricsView`, so both are delivered together in slice 01.

Net result: the whole feature is a **single slice** — see
`slice-01-per-state-bands-team.md`. See `../feature-delta.md` (D5, D6, D12, DDD-4, DDD-6,
DDD-8, US-01, US-02, Out of scope) for the authoritative scope.

## Note for the sibling `state-time-cumulative-view`

If that feature mirrored this one's two-slice shape (slice 01 team / slice 02 portfolio +
tooltip), reconsider: per-state chart work that lives in a shared view need not be split by
scope, and tooltip/legend enrichment may be cuttable to the same "visual-only" bar the user
set here.
