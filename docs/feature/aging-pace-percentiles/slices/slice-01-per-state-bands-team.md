# Slice 01: Per-State Pace-Percentile Bands — Team + Portfolio

> **Filename note**: kept as `slice-01-per-state-bands-team.md` because ADR-019, ADR-020 and
> the sibling `state-time-cumulative-view` slices link to it. After the 2026-05-25
> simplification this is the **single delivery slice** for the whole feature — team **and**
> portfolio — not team-only. Former slice 02 was folded in (see `slice-02-*` tombstone).

**Feature**: aging-pace-percentiles
**Stories shipped**: US-01 + US-02 (the whole feature). US-03 removed.
**Estimate**: ~3 crafter days

## Goal
Ship per-state **cumulative total-age-at-state-exit** percentile bands (50/70/85/95) on the
Work Item Aging chart, as **filled colored background zones** (green below 50th → red above
95th) behind the dots, toggled by a single **Pace percentiles** chip that is **off by
default**. Computed from `WorkItemStateTransition` data (sibling feature) for completed items
in the configured history window. Renders on **both** the team and portfolio Work Item Aging
charts (the chart is shared via `BaseMetricsView`).

## IN scope
- New backend endpoints (team + portfolio):
  - `GET /api/teams/{teamId}/metrics/ageInStatePercentiles?startDate&endDate`
  - `GET /api/portfolios/{portfolioId}/metrics/ageInStatePercentiles?startDate&endDate`
  - Both return `[{ state, percentiles: [{ percentile, value }] }]` in workflow order. States with zero observations are omitted. **No `sampleSize`** (DDD-4).
- Backend computation (shared `protected BaseMetricsService.ComputeAgeInStatePercentiles`, per DDD-9): for each completed item in `[startDate, endDate]` (membership = `ClosedDate ∈ window`, mirroring `cycleTimePercentiles`), walk its `WorkItemStateTransition` rows; for each `Doing`-category state it exited, record the **cumulative total age at exit** `exitTransition.TransitionedAt − item.StartedDate` (per D12 / ADR-019 amended). Bucket by state name. Compute 50/70/85/95 per bucket via the existing `PercentileCalculator`. Values rise left→right across states.
- Frontend: `WorkItemAgingChart` accepts new optional `perStatePercentileValues?: IPerStatePercentileValues[]` prop. When the **Pace percentiles** chip is on, render filled `<rect>` zones per state column inside the existing `<ChartsContainer>`, behind the dots — `x ∈ [stateIndex−0.4, stateIndex+0.4]`, Y boundaries at the consecutive percentile values, filled from the `ForecastLevel` green→red palette (DDD-6 / ADR-020 amended).
- Single toggle: a `showPaceBands` boolean (`useState` in the chart, off by default) gated by one new **Pace percentiles** chip in `PercentileLegend`. The existing percentile / SLE chips are untouched (DDD-8).
- Frontend service: `MetricsService.getAgeInStatePercentiles(id, startDate, endDate)`.
- `useMetricsData` hook: fetch the new endpoint alongside `cycleTimePercentiles`, stash as `ctx.perStatePercentileValues`.
- `BaseMetricsView`: pass `perStatePercentileValues={ctx.perStatePercentileValues}` to the `<WorkItemAgingChart>` instance (covers both team and portfolio, since the view is shared).

## OUT scope (cut 2026-05-25 — see feature-delta "Out of scope")
- In-flight dot tooltip pace annotation (old US-03).
- Per-band hover tooltip.
- Low-sample messaging + `sampleSize` field.
- Per-percentile sub-toggles / a second chip group.
- Configurable percentiles per team/portfolio (deferred, D4).

## Learning hypothesis
**Confirms if it succeeds**: bands derived from `WorkItemStateTransition` are accurate to a
known fixture, rise left→right, and render as colored zones behind the dots without
restructuring `WorkItemAgingChart`'s axis system. A flow coach toggles the chip and instantly
sees which dots sit in a state column's red zone.
**Disproves if it fails**: either (a) the transitions data is too sparse in the first weeks
post-sibling-ship to give meaningful bands for most states (those columns simply show no
band — acceptable, but low value until data accumulates), or (b) anchoring filled `<rect>`s
to per-column X ranges inside `<ChartsContainer>` is not supported by the existing MUI-X
setup and we need the `getBoundingClientRect()` fallback (ADR-020 open question).

## Acceptance criteria
See US-01 + US-02 in `../feature-delta.md`. Slice specifics:
- Backend fixture: a synthetic team (and a portfolio) with 20 completed items, each with known `StartedDate` + per-state transition timestamps across `In Progress`, `Review`, `Test`. Integration test asserts the endpoint returns exact 50/70/85/95 **cumulative total-age-at-exit** values per state, and that the values rise left→right (D12).
- Chart test: render with a non-empty `perStatePercentileValues` and the overlay on → assert the expected `<rect>` band elements at the expected x-spans / y-boundaries with green→red fills, behind the dots.
- Toggle test: chip off by default (no band rects) → click → rects appear → click → gone; existing percentile / SLE chips unaffected throughout.
- No-regression test: with the overlay off (default), the chart DOM matches the existing snapshot.

## Dependencies
**Hard**: `time-in-state-and-staleness` slice 01 merged to main (transitions table exists, ADO + Jira connectors emit rows; `WorkItem.StartedDate` available). D11 preserves the `WorkItemStateTransition` schema — formal coordination point with the sibling.

## Production data requirement
**Required.** The Lighthouse project's own ADO team + its portfolio must be observable through the new bands after sibling-slice-01 has been running for ≥2 weeks (real completed items with transitions in the window). DEVOPS smoke against the project's own production instance.

## Dogfood moment
After deploy, on the project's own dev Lighthouse instance, open `/teams/{ownTeamId}` (and a `/portfolios/{id}`) → Work Item Aging chart → toggle **Pace percentiles** on → see colored zones behind the dots, rising left→right. Confirm the band heights look plausible against a manually-checked sample of recently-completed items.

## Pre-slice spike candidates
- 30 min: verify a filled SVG `<rect>` rendered as a child of `<ChartsContainer>` lands in the chart's data coordinate space (x in state-index units, y in days) — same assumption today's `ChartsReferenceLine` relies on. If not, use the `getBoundingClientRect()` overlay fallback (ADR-020 open question) and re-estimate.
- 30 min: profile the new endpoint with a team that has 6 months of transitions (~thousands of rows) to confirm `GetFromCacheIfExists` is sufficient and no materialisation is needed.
- 15 min: confirm `PercentileCalculator.CalculatePercentile` accepts the input shape we produce (list of integer day counts from `exit − StartedDate`).
