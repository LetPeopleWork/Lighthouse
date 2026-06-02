# Slice 03 — Forecast-over-time (stacked on Done): the on-track read

**Feature**: `delivery-metrics` (Epic 3993 Delivery Metrics)
**Stories**: US-03
**Effort estimate**: 2 days
**Reference class**: Slice 1's burnup widget + the existing Monte-Carlo WhenForecast/HowMany engine. The forecast field is forward-fed into the unified forward-recorded `DeliveryMetricSnapshot` store (Slice 1) by the same `DeliveryMetricSnapshotRecordingHandler` (reacting to `PortfolioForecastsUpdated`) as Slice 1's backlog/done and Slice 2's inferred estimate — one more forward column on the existing handler; no new event or hook.

## Goal (one sentence)

Add a forecast band stacked on top of the Done line — the daily "how many will we get to by the delivery date" projection recorded FORWARD into the snapshot store — so a Delivery Forecaster reads the on-track signal directly off the chart: when Done + projected forecast meets or exceeds the backlog line at the target date, the delivery is on track.

## IN scope

- Add the forward forecast field ("how-many-to-delivery-date" projection recorded that day, pinned to an existing Monte-Carlo percentile — default **85%**, matching the customer Excel "to 85%" chart; reuses the existing `WhenForecast` percentile output, not a new computation) to the `DeliveryMetricSnapshot` rows via the existing `DeliveryMetricSnapshotRecordingHandler` (one more column on the same `PortfolioForecastsUpdated` handler); forward-only, idempotent on date.
- Extend the single `.../metrics-history` endpoint (one endpoint, per ADR-050) to return, per snapshot date, the recorded projected completion-by-target-date figure as an additional series.
- Extend `DeliveryBurnupChart.tsx`: stack a forecast segment on the Done area, and draw the backlog line so the "done + forecast ≥ backlog ⇒ on track" read is visible at a glance at the target date.
- The "on track / at risk" visual read derives purely from chart geometry (forecast meets backlog or not) — no new RAG endpoint needed in MVP, but a guardrail note if the team wants one later.
- Sparse-data handling: with few snapshots the forecast band is short; an annotation explains "forecast trend builds forward from {first snapshot date}."
- Empty/insufficient-snapshot edge cases (consistent with Slice 2's empty-history state).

## OUT scope (deferred)

- Likelihood / when-distribution trend (Slice 4 — separate question: is predictability improving?).
- Fever chart (Slice 5).
- Any retroactive forecast points (impossible by design — the forward-only rationale, D6/D11).

## Learning hypothesis

- **Disproves if it fails**: that stacking the daily forecast on Done produces a legible "on-track" read for a forecaster. If users can't tell on-track-vs-at-risk from the stacked geometry (e.g. the forecast band is too noisy day-to-day, or the backlog line crossing is ambiguous), the visualization needs rework (smoothing, a target-date marker, or an explicit on-track badge).
- **Confirms if it succeeds**: that snapshot-fed forecast-over-time, stacked on Done against the backlog line, is the single chart a forecaster uses to answer "are we on track for this delivery?" without opening the per-feature breakdown.

## Acceptance criteria

- US-03 AC items from `feature-delta.md` apply unchanged.
- Integration test: with a known set of `DeliveryMetricSnapshot` rows carrying the forward forecast field, the forecast endpoint returns the recorded projection per date; the stacked-on-track assertion (done + forecast vs backlog at target) holds for an on-track fixture and an at-risk fixture.
- Vitest + RTL: the forecast band stacks on the Done area; the backlog line is drawn; an on-track fixture reads visibly differently from an at-risk fixture; the sparse-data annotation appears with few snapshots.
- `pnpm build` clean; `dotnet build` zero warnings; SonarCloud gate passes; mutation ≥80% on new code.

## Dependencies

- **HARD**: Slice 1 merged (the unified `DeliveryMetricSnapshot` store + forward recorder + burnup host chart) AND enough days elapsed since the forward forecast field began recording for a usable band.
- **SOFT**: Slice 2 (shares the forward-recorder pattern); not a blocker.

## Production data requirement

Requires ≥1-2 weeks of accumulated snapshots on the dev instance for a real delivery to show a meaningful forecast band. Dogfood once the trend is non-trivial; screenshot an on-track and (if available) an at-risk delivery in the PR.

## Cross-cutting (DoR item 7)

- **RBAC**: read view; existing portfolio read path; `useRbac()` UI gating; `IRbacAdministrationService`. No new write surface. No `/my-summary` fetch.
- **Lighthouse-Clients**: N/A for a new gate — the forecast projection is an extra series on the single Slice-1 `metrics-history` endpoint (ADR-050), so the Slice-1 version-gate already covers it. Decide at this slice only whether CLI/MCP surface delivery-forecast-history as a command (likely, since this is the flagship forecasting read) — no new endpoint to gate.
- **Website**: LIKELY YES at this slice — delivery forecast-over-time is the flagship capability worth surfacing/marketing. Flag for the website repo; mark the concrete update in the launch checklist.

## Pre-slice SPIKE

NONE required — the unified snapshot store, forward recorder, and burnup widget (all Slice 1) cover the uncertainty; this slice is one more forward-fed column + a stacked series.
