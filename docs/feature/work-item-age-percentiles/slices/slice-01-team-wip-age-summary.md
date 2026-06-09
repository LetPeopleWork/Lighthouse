# Slice 01 — Team WIP-age percentile summary (thin end-to-end)

**Type:** vertical | **Est:** ~1 day | **Stories:** US-01

## Learning hypothesis

A percentile readout of the **current in-progress population's own ages** (50/70/85/95) is a meaningful at-a-glance flow signal — distinct from the historical cycle-time percentiles already shown. Disproved if coaches ignore the card or find it redundant with the aging-chart dots.

## What ships

- A new backend `GET /api/teams/{teamId}/metrics/workItemAgePercentiles` endpoint (D8 LOCKED backend; ADR-065) computing the 50/70/85/95 percentiles of the **current** in-progress items' `workItemAge` (snapshot, not windowed) — reuse the existing in-progress selection + `BuildPercentiles`/`PercentileCalculator` + `PercentileValue`, mirroring `cycleTimePercentiles`.
- `MetricsService.getWorkItemAgePercentiles` + `useMetricsData` ctx field (minimal FE wiring — no client-side percentile math).
- A new small **"Work Item Age Percentiles"** card in `OverviewCategory`, mirroring `CycleTimePercentiles.tsx` (descending rows, ForecastLevel colouring), Team scope.
- **Lighthouse-Clients (separate repo):** version-gated `getWorkItemAgePercentiles` wrapper for the new team endpoint (pin strictly-newer-than last released; record in `FEATURE_REQUIRES_SERVER_NEWER_THAN`).

## IN scope

- Team scope only.
- Overall WIP age (not per-state).
- Graceful empty state when zero in-progress items (D6).

## OUT of scope

- Aging-chart toggle (Slice 02), Portfolio (Slice 03), per-state WIA, premium gating.

## Production-data AC

- Given Team Phoenix has in-progress items aged d₁…dₙ, when the flow coach opens the metrics overview, then the card shows the 50/70/85/95 of those ages in descending order with ForecastLevel colours.
- Given Team Phoenix has zero in-progress items, when the overview opens, then the card shows a graceful empty state, not an error.

## Taste tests

- Not 4+ new components: one card mirroring an existing one. PASS.
- No new abstraction shipped first: reuses `PercentileCalculator`. PASS.
- Disproves a pre-commitment (WIP-age summary is meaningful). PASS.
- Production/demo data, not synthetic-only: demo data has in-progress items. PASS.
- Value-bearing (not @infrastructure): coach reads a real WIP-age summary. PASS.
