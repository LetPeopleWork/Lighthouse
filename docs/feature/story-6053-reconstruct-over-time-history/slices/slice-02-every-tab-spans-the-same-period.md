# Slice 02 — Every tab spans the same period (CT-60/90, WIA, portfolio)

**Story**: US-02 | **ADO**: 6053 | **Estimate**: ~1 day | **Reference class**: slice 01's reconstruction seam

## Goal

Toggling `[ WIA | CT-30 | CT-60 | CT-90 ]` on team and portfolio shows every tab covering the same dated span,
so the cross-tab comparison the combined widget was built for actually works.

## IN scope

- CT-60 and CT-90 reconstruction, each with its own window.
- **Work-item-age reconstruction** at the `PercentilesOverTimeSnapshot.NoHorizon` sentinel — this is the
  genuinely different path, see below.
- Portfolio scope via `IPortfolioMetricsService`.
- Every slice-01 guarantee carried to each family and scope: absence gate, `UpdateTime` floor, data floor,
  idempotency, non-blocking read.

## OUT of scope

- PBC families → slice 03.
- Empty-state copy and docs → slice 04.
- Feature Size (a PBC family, not a percentile family) — belongs to slice 03 and stays portfolio-only.

## Why this is not slice 01 at a different scale

The carpaccio taste test flags "2+ slices identical except for scale". This one survives it on a specific
technical difference, and if that difference turns out to be cosmetic the slices should be merged at DELIVER:

- **CT-60/90 genuinely are scale** — the same windowed call with a different horizon.
- **WIA is not.** `GetWorkItemAgePercentilesForTeam(team, endDate)` takes **no start date**. Its as-of-day
  correctness comes from `GetWipSnapshotForTeam(team, endDate)` filtering through `WasItemProgressOnDay`, and
  the projection `AgeOnDay(zone, DateOnly.FromDateTime(endDate))` — the D15 fix. Reconstruction here depends on
  that path being genuinely as-of rather than today-anchored, which is the thing to assert.
- **Portfolio is a different service** with its own cache keys and its own family set.

## Learning hypothesis

**Disproves if it fails**: "the as-of-`endDate` WIA path reconstructs a past day correctly." If a reconstructed
WIA day disagrees with a recorded one while CT agrees, the D15 fix does not hold as far back as reconstruction
reaches, and WIA must be excluded from reconstruction rather than silently wrong.

**Confirms if it succeeds**: both percentile families reconstruct through one seam, and the only remaining
variation is the PBC table.

## Acceptance criteria

1. CT-60 and CT-90 reconstruct; each horizon's window is its own (`D - horizon`, `D`).
2. A reconstructed WIA day equals what `GetWipSnapshotForTeam(owner, D)` → `AgeOnDay(zone, D)` → `BuildPercentiles`
   returns for that day — asserted against one of the 4 genuinely recorded days on the dev DB.
3. Portfolio scope reconstructs for CT-30/60/90 and WIA.
4. Absence gate, `UpdateTime` floor, data floor and idempotency hold for every family and scope added here.
5. Switching tabs does not re-trigger reconstruction of a span already reconstructed.
6. On the dev DB, all four tabs on team 1 **and** on portfolio 1 span the same dated window.
7. Backend build zero warnings; backend tests green; frontend `pnpm test` green and `pnpm build` clean.

## Dependencies

Slice 01's reconstruction seam and its SPIKE-01 verdict on D6.

## Dogfood moment

Same day: on the restored dev DB, toggle all four tabs at team scope and all four at portfolio scope, and
confirm the x-axis span matches across every one.
