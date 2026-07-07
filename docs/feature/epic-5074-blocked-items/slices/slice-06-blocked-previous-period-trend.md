# Slice 06 — Previous-period trend on the Blocked overview widget

**Epic**: 5074 Blocked Items | **Batch**: enhancements (post slices 01-04) | **Job**: `job-delivery-lead-tell-blocked-trend-vs-last-period`

## Goal (one sentence)
Show a direction + delta on the Blocked overview widget comparing the current blocked count against the blocked count on the last day of the previous period.

## IN scope
- Trend indicator on `BlockedOverviewWidget` (`pages/Common/MetricsView/BlockedOverviewWidget.tsx`): direction (up/down/flat) + numeric delta.
- Baseline = the `BlockedCountSnapshot` value on the last day of the **previous period**, where "period" = the dashboard's currently-selected date range (same range that already drives the view).
- Computed **client-side** from `ctx.blockedCountHistory` already passed into `BaseMetricsView` — no new endpoint, no new capture.
- No-baseline state: render "—" when no snapshot exists at/before the previous-period boundary.
- Works for both Team and Portfolio (same widget, same `blockedCountHistory` shape).

## OUT of scope
- Any new backend endpoint or persisted field (data already exists in `BlockedCountSnapshot`).
- Sparkline / mini-chart on the widget (that is the existing over-time chart's job).
- RAG colour (slice 07) and chart drill-through (slice 08).

## Learning hypothesis
- **Disproves if it fails**: that "current count vs prior-period boundary" is a meaningful, computable comparison from the forward-only snapshot series — if the boundary snapshot is routinely missing (sparse history) the indicator is "—" so often it adds no value.
- **Confirms if it succeeds**: the widget becomes directionally interpretable in a review with zero new data capture.

## Acceptance criteria
1. Given a team with `BlockedCountSnapshot` history spanning the selected range and the prior period, when the metrics view loads, then the Blocked widget shows the current count and a delta+direction vs the blocked count on the last day of the previous period.
2. Given the current count exceeds the prior-period-boundary count, the indicator shows an "up/worse" direction and the positive delta; when lower, a "down/better" direction; when equal, "flat".
3. Given no snapshot exists at or before the previous-period boundary, the indicator renders "—" with a "no prior-period baseline yet" tooltip — never a delta of 0.
4. E2E (demo data): the Blocked widget on a team metrics page renders the trend indicator with a visible direction; POM asserts the indicator element and value.

## Dependencies
- `BlockedCountSnapshot` history endpoint (shipped slice 03) already surfaced as `blockedCountHistory` on `BaseMetricsView`.

## Effort / reference class
~0.5 day. Reference class: WIP/throughput trend widgets (same "count + delta" widget-enrichment shape). Pure FE + Vitest; no migration.

## Pre-slice SPIKE
None — data and plumbing already exist; lowest-risk slice, sequenced first as the confidence builder.
