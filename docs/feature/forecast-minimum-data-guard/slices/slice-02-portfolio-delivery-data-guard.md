# Slice 02 — Portfolio delivery & per-feature likelihoods suppressed below the data threshold

**Feature:** forecast-minimum-data-guard · **ADO** #5125 · **Story** 2 · **Persona** delivery-forecaster

## Goal
A delivery (and its features) whose owning team has fewer than 5 distinct days with completed items shows a "not enough data" indicator instead of a likelihood percentage on every portfolio surface, reusing the sufficiency signal proven in slice 01.

## IN scope
- Backend: evaluate the slice-01 sufficiency rule **per owning team** and carry the signal on `DeliveryWithLikelihoodDto` / `FeatureLikelihoodDto`.
- Frontend: suppress the percentage and render the "not enough data" indicator at all three portfolio call sites — `DeliverySection` delivery header chip, `DeliverySection` per-feature likelihood column, and `DeliveriesChips` overview chip.
- Preserve composition: at ≥5 active days the sibling `>95%` cap still applies; completed items (no remaining work) still read `100%`/Done (D4).

## OUT scope
- Re-deriving the sufficiency computation (reuse slice 01's).
- Manual forecaster surface (slice 01).
- Per-team threshold config (D5).

## Learning hypothesis
**Disproves** "the validated sufficiency rule propagates cleanly to the aggregate portfolio surfaces" **if** the per-owning-team evaluation produces inconsistent results across the delivery chip, overview chip, and per-feature column (e.g. a delivery suppressed but one of its features still showing a number), revealing the signal isn't carried uniformly. **Confirms** cross-surface consistency (Story 2 AC4).

## Acceptance criteria
Story 2 AC1–AC4 (see feature-delta). Key: <5 active days + remaining work → indicator, no percentage, at all three sites; ≥5 → renders as today (cap still applies); completed → `100%`/Done; consistent across surfaces.

## Dependencies
**Slice 01** (the backend sufficiency computation + signal). This slice is pure propagation to the aggregate DTOs and views.

## Effort estimate
~0.5 day (additive signal on two DTOs + three FE render sites adopting the suppressed state). Reference class: sibling `forecast-confidence-cap` slice 02 (pure propagation of a validated rule to the portfolio surfaces).

## Pre-slice SPIKE
None — the rule and treatment are proven in slice 01; the only new work is per-owning-team evaluation and uniform carriage across the three portfolio render sites.
