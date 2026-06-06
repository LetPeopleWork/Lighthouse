# Slice 03 — Recurring-rule days are indistinguishable across all forecasting & chart surfaces

**Story:** US-03 · **Surface:** feature/delivery forecast dates, likelihood-by-date, forecast write-back, chart blackout overlays · **Direction:** unified evaluation fans out everywhere

## Goal
One sentence: prove that every surface that already consumes one-off blackout days (feature & delivery percentile dates, likelihood/how-many-by-date, forecast write-back, and the chart overlays) treats a recurring-rule day identically — so the unified-evaluation guarantee (D4) holds across the whole product, not just the manual "When" forecast.

## IN scope
- Thread the recurring-rule-expanded days into the SAME global blackout-day set every shipped #4974 consumer reads: `WhenForecastDto`/`HowManyForecast` (covered in 01/02) PLUS `Feature.GetLikelhoodForDate`, `Delivery` expected dates, `DeliveryWithLikelihoodDto`, `WriteBackTriggerService`, and `BlackoutDaysExtensions.AnnotateBlackoutDays` (chart overlays).
- One assembly-layer fetch that unions one-off periods + recurring-rule days into the `IReadOnlyList<BlackoutPeriod>`-shaped set the consumers already accept (DESIGN decides the union seam; mirrors #4974 fetch-once pattern).

## OUT scope
- New recurrence semantics (done in 01/02).
- Settings management (edit/delete/validation) → Slice 04.

## Learning hypothesis
**Disproves** "the recurring-rule days drop into the existing union-of-blackout-days seam unchanged, so feature/delivery/write-back/chart surfaces need no per-surface recurring logic" **if** any surface shows a recurring-rule day differently from a one-off blackout day (e.g. a delivery date that steps over a one-off weekend but not a recurring weekend).
**Confirms** D4 (unified evaluation) is a true product-wide invariant.

## Acceptance criteria
- US-03 AC1: a Portfolio → Delivery whose feature window contains recurring-rule days shows feature percentile dates stepped over those days and rolled forward off them (identical to one-off behaviour).
- US-03 AC2: `Feature.GetLikelhoodForDate(date)` counts working days excluding recurring-rule days in the window.
- US-03 AC3: forecast write-back writes the recurring-blackout-shifted date to Jira/ADO.
- US-03 AC4: chart blackout overlays annotate recurring-rule days the same as one-off blackout days.
- US-03 AC5 (regression): with no recurring rules AND no one-off periods, every surface is byte-identical to pre-feature (inherits #4974 D6).
- Production-data: a delivery whose features span a weekend covered by the weekends-forever rule.

## Dependencies
Slices 01 + 02 (entity, expansion, unified evaluation seam). Shipped #4974 day↔date shift across all surfaces (LOCKED — only the day SOURCE widens to include recurring days).

## Effort / reference class
~1 day. Reference class: the #4974 Slice 03 fan-out (same consumers), now sourcing a wider day set.

## Slice value
Observable: a delivery on the portfolio surface (and a written-back Jira date) reflects the recurring weekend exactly as it would a one-off blackout — the forecaster's downstream benefit (`job-forecast-skip-known-nonworking-days`) is realised for recurring days too.
