# Slice 02 — Manual forecast shows ">95%"

**Goal:** A manual "when" forecast with remaining items that computes `> 95%` displays `">95%"` instead of `100.00%`.

**Story:** Story 2 · `job_id: job-forecast-no-false-certainty`

## IN scope
- Apply the shared rule from slice 01 to `ForecastLikelihood.tsx` (currently `likelihood.toFixed(2)%`); preserve 2-decimal precision for `≤ 95%`.
- Keep the existing "Certain" `ForecastLevel` styling for the `">95%"` case (no re-banding).

## OUT of scope
- Portfolio surface (slice 01).
- The numeric `ManualForecastDto.Likelihood` value (formatting only).

## Learning hypothesis
**Confirms** the rule propagates cleanly to a second surface with different precision (`toFixed(2)` vs `Math.round`) via one shared formatter — no per-surface special-casing.
**Disproves** the shared-formatter design if the two surfaces need divergent rules (→ they don't; merge risk is the opposite).

## Acceptance criteria
Story 2 AC1–AC3 (see feature-delta.md). FE unit tests at the 95.0 / 95.01 / 100 boundary + `remainingItems === 0` exemption; live E2E of a manual forecast that lands `> 95%`.

## Dependencies
**Slice 01** (shared formatter must exist).

## Effort
~1–2 h (one call site + tests + live E2E).

## Dogfood moment
Same day: run a manual forecast locally against demo data tuned to exceed 95%; confirm the headline reads `">95%"`.
