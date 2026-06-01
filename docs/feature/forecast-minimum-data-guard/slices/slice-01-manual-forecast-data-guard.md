# Slice 01 — Manual team forecast suppressed below the data threshold

**Feature:** forecast-minimum-data-guard · **ADO** #5125 · **Story** 1 · **Persona** delivery-forecaster

## Goal
A team with completions on fewer than 5 distinct days shows no manual When/How-Many forecast — instead an explicit "not enough throughput data yet (need ≥5 days with completed items)" state — end-to-end from the backend sufficiency computation to the `ManualForecaster` view.

## IN scope
- Backend: derive the **active-day count** (distinct days in the team's throughput window with ≥1 completed item) from the throughput series the forecast already uses (`RunChartData` / `WorkItemsPerUnitOfTime`), evaluated **after** the empty-filtered-sample fallback (D6).
- Backend: carry a sufficiency signal on `ManualForecastDto` (carrier shape = DESIGN's D7 call — boolean flag vs. count + message).
- Backend: the guard fires only when remaining work > 0 (D4); completed/no-work selections are untouched.
- Frontend: `ManualForecaster` / `ForecastLikelihood` suppress the likelihood + percentile output and render the rule-naming message when the signal says insufficient.

## OUT scope
- Portfolio delivery & per-feature surfaces (slice 02).
- Per-team configurable threshold (D5 — fixed 5).
- Any change to the Monte Carlo math or the `>95%` cap.

## Learning hypothesis
**Disproves** "a suppressed *not-enough-data* state reads as honest and helpful" **if** users/the reporter report the empty forecast as a bug or a broken screen rather than the intended, actionable answer. **Confirms** the suppress-and-explain treatment (D2) and the active-days rule basis (D1) before it is propagated to the portfolio surfaces.

## Acceptance criteria
Story 1 AC1–AC6 (see feature-delta). Key: When **and** How-Many both suppressed below 5 active days; renders normally at ≥5; boundary 5 = sufficient; no-remaining-work selection shows no message; message names the rule.

## Dependencies
None blocking. Reads the throughput series produced by the existing forecast pipeline; composes with the shipped empty-filtered-sample fallback (D6).

## Effort estimate
~0.5–1 day (one backend sufficiency computation + signal field + one FE suppressed state). Reference class: sibling `forecast-confidence-cap` slice 01 (FE-only, <1 day) plus a backend signal field — comparable to one filter-forecast-throughput surface slice.

## Pre-slice SPIKE
Low uncertainty, but confirm in the first RED: does `RunChartData.WorkItemsPerUnitOfTime` enumerate **only** days with completions, or include zero-days as empty buckets? The active-day count must be "days with ≥1 item" regardless — verify the derivation against both possibilities. (Carry the finding to DESIGN's D7.)
