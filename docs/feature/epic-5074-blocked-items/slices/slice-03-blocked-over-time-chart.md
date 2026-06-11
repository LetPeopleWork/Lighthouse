# Slice 03 — Blocked-items-over-time chart

**Job**: `job-delivery-lead-see-blocked-trend` | **Persona**: delivery-lead-rte
**MoSCoW**: Should | **Est**: ~1 day | **Premium**: No

## Goal (one line)
Plot the in-scope blocked COUNT as a forward-only daily time series (delivery-metrics history pattern)
in the **Flow Metrics chart area** (alongside the other flow-metrics charts, NOT folded into the
overview widget) so a lead can see whether blocked count is trending up or down.

## Learning hypothesis
**Disproves "a forward-only blocked-count snapshot answers 'are we clearing blockers faster than they
arrive?'" if**: the daily count is too noisy/coarse to read a trend, OR users expect retroactive
history the forward-only model can't provide and read the empty start as a defect.

## In scope
- Forward-only daily snapshot of the in-scope blocked count per Team/Portfolio, recorded going forward
  (reuse the delivery-metrics recorder pattern; NO per-item reconstruction).
- Blocked Items over-time chart placed in the **Flow Metrics chart area** on team/portfolio surfaces
  (alongside the other flow-metrics charts), composing with the existing team/portfolio/date-range filter.
- Honest empty state: "blocked trend builds forward from today — no snapshots yet".

## Out of scope
- Per-item drill-down on the chart. Blocked→stale (slice 04). Retroactive backfill (forward-only by
  design, mirroring delivery-metrics forecast-history).

## Production-data AC (drive via demo data + real connector)
- AC1: With snapshots recorded over a window, the chart plots the daily in-scope blocked count; a window
  where the count rose from 3 to 9 renders an upward trend.
- AC2: With no snapshots yet, the chart shows the forward-only empty state, never a flat zero line read
  as "never blocked".
- AC3: The chart respects the active team/portfolio/type/date-range filter (count reflects the filtered
  scope).
- AC4 (@property): the snapshot blocked count equals the count of items that are `IsBlocked` per the
  slice-01 `BlockedRuleSet` at snapshot time — one blocked definition.

## Dogfood moment
Let the recorder run for several days on a real team; open the chart and read the actual blocked-count
trend (not a synthetic series).

## Cross-cutting
- **RBAC**: read surface — inherits existing metric read gating; no new write surface.
- **Clients**: a new over-time metrics read endpoint — version-gate the client wrapper (pre-check
  server version, `FEATURE_REQUIRES_SERVER_NEWER_THAN`); see feature-delta.
- **Website**: N/A (non-premium).

## Dependencies
Slice 01 (`IsBlocked`). Independent of slice 02's per-item capture (counts current blocked, not spell
history), but ships after it by priority.
