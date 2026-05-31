# Slice 01 — Portfolio delivery forecasts show ">95%"

**Goal:** A portfolio delivery (and its per-feature likelihoods) with remaining work that computes `> 95%` displays `">95%"` instead of `100%`/`96–99%`.

**Story:** Story 1 · `job_id: job-forecast-no-false-certainty`

## IN scope
- Shared likelihood-formatting rule: `likelihood > 95 && hasRemainingWork → ">95%"`, else existing format. Introduced here (first surface to use it).
- Apply at `DeliverySection.tsx` chip and `DeliveriesChips.tsx` (overview) — both currently `Math.round(likelihoodPercentage)%`.
- Per-feature likelihood within a delivery (`FeatureLikelihoodDto` render path).
- D4 exemption: completed delivery/feature (no remaining work) still shows `100%`/Done.

## OUT of scope
- Manual forecast surface → slice 02.
- Minimum-data guard → ADO #5125.
- Any change to the numeric `LikelihoodPercentage` DTO value (formatting only).

## Learning hypothesis
**Confirms** that a `">95%"` band reads as "very likely, not guaranteed" to a leader — removing the verbal caveat — if the reporter and a forecaster react positively on the live portfolio view.
**Disproves** the bucketing approach if users find `">95%"` confusing or demand the precise high number back (→ revisit D1 toward a 99.9% clamp).

## Acceptance criteria
Story 1 AC1–AC5 (see feature-delta.md). Verified with **production-shaped demo data** on a real Portfolio detail view, plus FE unit tests for the formatter at the 94.9 / 95.0 / 95.1 / 100 / completed-item boundaries.

## Dependencies
None. Existing deliveries pipeline + DTOs in place.

## Effort
~3–4 h (formatter + 2 call sites + per-feature path + tests + live E2E).

## Dogfood moment
Same day: open the local app's Portfolio detail with a high-likelihood demo delivery; confirm `">95%"` renders and a completed feature still shows `100%`.
