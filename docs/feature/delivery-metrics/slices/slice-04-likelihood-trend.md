# Slice 04 — Likelihood / when-distribution over time: predictability trend

**Feature**: `delivery-metrics` (Epic 3993 Delivery Metrics)
**Stories**: US-04
**Effort estimate**: 1-2 days
**Reference class**: Slice 3's forward-fed forecast field + the existing likelihood/when-distribution rendering already used on the current-snapshot delivery view.

## Goal (one sentence)

Add forward likelihood + when-distribution fields to the unified snapshot store and plot the trend of the delivery's `likelihoodPercentage` (and the spread of its when-distribution) over the accumulated snapshots, so a Delivery Forecaster answers a different question than Slice 3 — not "are we on track today?" but "is our predictability for this delivery improving or degrading?" — and can say so with evidence in a leadership review.

## IN scope

- Add forward likelihood + when-distribution fields to the `DeliveryMetricSnapshot` rows via the existing `DeliveryMetricSnapshotRecordingHandler` (one more column on the same `PortfolioForecastsUpdated` handler; forward-only, idempotent on date).
- A likelihood-trend series (and a when-distribution spread band) drawn from `DeliveryMetricSnapshot.likelihoodPercentage` and the serialized when-distribution per snapshot.
- A new `DeliveryPredictabilityChart.tsx` (the second of three charts per D12 — separate from the burnup because likelihood is a probability axis, not a count) with TWO first-class views, matching the customer Excel example: (a) **likelihood view** — likelihood-over-time line banded with the existing `getLikelihoodLevel` RAG thresholds (their "How Likely?" chart); (b) **when view** — a DATE y-axis plotting forecast completion-date percentile line(s) (default 70%; 50/70/85/95 spread available) per snapshot date against a dashed target-delivery-date reference line (their "When will it be done?" chart), where a narrowing spread converging under the target reads as firming predictability and a widening/crossing spread reads as slipping.
- The honest-uncertainty framing inherited from the forecaster persona: a flat-high or improving likelihood reads as "stabilising"; a degrading or widening spread reads as "predictability slipping — investigate."
- Sparse-data and empty-history handling consistent with Slices 2-3.

## OUT scope (deferred)

- Fever chart (Slice 5).
- Cross-delivery predictability comparison — one delivery at a time in MVP.
- Any retroactive likelihood points (impossible by design — the forward-only rationale, D6/D11).

## Learning hypothesis

- **Disproves if it fails**: that a likelihood/spread trend is a question forecasters actually act on. If the trend view sees little use or users can't connect "predictability is degrading" to an action, the second-question framing was wrong and Slice 3's on-track view alone may suffice.
- **Confirms if it succeeds**: that "is predictability improving or degrading?" is a distinct, valued lens on the same snapshots — complementary to the on-track read, not redundant.

## Acceptance criteria

- US-04 AC items from `feature-delta.md` apply unchanged.
- Integration test: with known snapshots of varying `likelihoodPercentage` and when-distribution spread, the trend endpoint returns the per-date series; an improving fixture and a degrading fixture produce distinguishable trend output.
- Vitest + RTL: the likelihood-over-time line renders; the spread band narrows/widens per fixture; sparse-data and empty-history states render.
- `pnpm build` clean; `dotnet build` zero warnings; SonarCloud gate passes; mutation ≥80% on new code.

## Dependencies

- **HARD**: Slice 1 merged (the unified `DeliveryMetricSnapshot` store + forward recorder) AND enough days elapsed since the forward likelihood field began recording for a trend.
- **SOFT**: Slice 3 (shares the forward-recorder + trend endpoint shape).

## Production data requirement

Requires accumulated snapshots spanning enough days that likelihood has moved. Dogfood against a real delivery whose likelihood visibly changed; screenshot the trend in the PR.

## Cross-cutting (DoR item 7)

- **RBAC**: read view; existing portfolio read path; `useRbac()` gating; `IRbacAdministrationService`. No new write surface. No `/my-summary` fetch.
- **Lighthouse-Clients**: if the likelihood-trend is a new endpoint, version-gate in clients (`FEATURE_REQUIRES_SERVER_NEWER_THAN`). Otherwise N/A if it extends Slice 3's trend endpoint with an extra series.
- **Website**: N/A as a standalone — folds under the Slice-3 flagship delivery-tracking marketing surface.

## Pre-slice SPIKE

NONE required — reuses Slice 3's snapshot-trend access pattern.
