# Slice 03 — Where the limits actually moved (PBC over time)

**Story**: US-03 | **ADO**: 6053 | **Estimate**: ~1 day | **Reference class**: slice 01/02 seam + `ProcessBehaviorRecordingHandler`'s existing honesty gates

## Goal

A delivery lead opening Predictability → PBC Over Time sees UNPL, Average and LNPL as three dated lines across
the capped window, for every metric type the toggle offers — not the three points the recorder happened to catch.

## IN scope

- Reconstruction of `ProcessBehaviorSnapshot` rows for all five team families
  (`Throughput`, `WorkItemAge`, `Wip`, `CycleTime`, `Arrivals`) and all six portfolio families (plus
  `FeatureSize`), matching `TeamReaders` / `PortfolioReaders` exactly.
- The existing honesty gates applied to reconstructed days unchanged:
  `Status != BaselineStatus.Ready` → no row; `Average == 0 && UpperNaturalProcessLimit == 0` → no row.
- The lookback span per day mirrors `LookbackDaysFor` — `ThroughputHistory - 1` for a rolling team,
  `FixedDatesTeamLookbackDays` (30) for a fixed-dates team, `PortfolioLookbackDays` (90) for a portfolio.
- An explicit decision, asserted in a test, on the today-anchored baseline problem below.

## OUT of scope

- Empty-state copy and docs → slice 04.
- Changing the baseline feature's **semantics** — what a pinned baseline means, how it is validated, or what
  a user sets. Unchanged.
- Reconciling `DemoPercentilesBackfillHandler`'s synthetic Throughput backdating with reconstruction —
  **answered at DESIGN by DDD-15** (fill-if-absent: the filler steps over backdated demo rows rather than
  correcting them), so this is no longer an open DoD item to settle at finalization.

> **Corrected 2026-09-22 after DESIGN (FLAG-1).** This brief originally read "Changing
> `BaselineValidationService` itself, or the baseline feature's semantics" — forbidding the very change the
> slice needs. DDD-12 threads an explicit as-of day through `Validate(...)` and the `DoneItemsCutoffDays`
> cutoff, defaulting to `Clock.Today` so the forward recorder and all six point-in-time PBC widgets stay
> byte-identical. That is an **additive signature change**, not a semantic one. The original line left only
> "accept a silent empty series", which AC3 below rules out — the brief was wrong, not the decision.
> DDD-12's named fallback if this over-runs the slice budget: refuse PBC reconstruction for pinned-baseline
> owners **with explicit copy**, never silently produce nothing.

## The hazard this slice exists to confront

`BaselineValidationService.Validate(baselineStart, baselineEnd, owner.DoneItemsCutoffDays, Clock.Today)` is
anchored to **today**, on all three PBC builders (`BuildDailyRunChartProcessBehaviourChart`,
`BuildTotalWorkItemAgeProcessBehaviourChart`, `BuildCycleTimeProcessBehaviourChart`) and on
`GetFeatureSizeProcessBehaviourChart`.

Reconstructing day D still validates against today. Two ways that goes wrong, both silent:

- An owner with a **pinned** `ProcessBehaviourChartBaselineStartDate`/`EndDate` may validate against today but
  not against D — or the reverse. A `BaselineInvalid` chart hits the `Status != Ready` gate and yields **no
  row**, so the whole reconstruction quietly produces nothing and looks like "the data didn't support it".
- An owner with **no** pinned baseline gets `baselineStart = startDate, baselineEnd = endDate` — the
  reconstruction window — which is correct per day, and is the case the happy path will accidentally prove.

The second case passing is not evidence the first works. Both need a test that says which behaviour is intended.

## Learning hypothesis

**Disproves if it fails**: "PBC reconstruction is the percentile seam with a different reader." If the pinned-
baseline owner produces an empty series where a populated one is expected, PBC needs its own as-of handling
rather than the shared walk, and slice 03 grows.

**Confirms if it succeeds**: one reconstruction seam covers both tables, and the existing recorder gates are
sufficient for honesty without a PBC-specific absence rule.

## Acceptance criteria

1. All five team families and all six portfolio families reconstruct; the family **set** per scope is asserted,
   so dropping one is a test failure rather than a silent capability loss.
2. A day whose chart is `Status != Ready` yields no row; a day whose chart has
   `Average == 0 && UpperNaturalProcessLimit == 0` yields no row.
3. An owner with a **pinned** baseline produces a defensible series, and a test states which — flat limits
   across every reconstructed day is a correct reading of a fixed baseline; an empty series is not.
4. An owner with **no** pinned baseline reconstructs per-day limits from the per-day window.
5. `FeatureSize` reconstructs at portfolio scope only, and is absent at team scope.
6. On the dev DB, portfolio 1's PBC Over Time widget spans the capped window for all six types; team 1 for all
   five.
7. `UpdateTime` floor, data floor and idempotency hold, as in slices 01–02.
8. Backend build zero warnings; backend tests green; frontend `pnpm test` green and `pnpm build` clean.

## Dependencies

Slices 01 and 02. `ProcessBehaviorSnapshot`, `IProcessBehaviorSeriesQuery`,
`GET .../metrics/process-behavior-over-time` — all shipped in Epic 5427 slices 03/04.

## Dogfood moment

Same day: on the restored dev DB, toggle every PBC type at both scopes and read three dated limit lines on each.
