# Slice 02 — Likelihood & How-Many "by a date" count working days

**Story:** US-02 · **Surface:** `forecast/manual/{id}` + `forecast/itemprediction/{id}` · **Direction:** date→days

## Goal
One sentence: when a forecast is asked "by `<target date>`", count only working (non-blackout) days between today and the target so likelihood and how-many aren't distorted by known non-working days.

## IN scope
- A shared **working-day counter**: working days in `(today, target]` excluding `BlackoutPeriod` days (reuse `GetBlackoutDayIndices`).
- Replace `(input.TargetDate - DateTime.Today).Days` at `ForecastController` likelihood (line ~80/93) and how-many-by-date (lines ~57/103) with the working-day count.
- Preserve the existing `timeToTargetDate <= 0` guard semantics (D5/AC3).

## OUT scope
- days→date projection (Slice 01).
- Feature/Delivery likelihood (`Feature.GetLikelhoodForDate`) → Slice 03.

## Learning hypothesis
**Disproves** "working-day counting composes cleanly with the existing `timeToTargetDate<=0` guard and the shipped throughput filter" **if** a target spanning a blackout mis-scores or a past/today target regresses.
**Confirms** the counter is the reusable inverse primitive for Slice 03.

## Acceptance criteria
US-02 AC1 (12 cal / 2 blackout → likelihood `GetLikelihood(10)`, how-many `HowMany(throughput,10)`), AC2 (no-blackout equal), AC3 (today/past unchanged). Production-data: real team + target date spanning a configured blackout.

## Dependencies
Slice 01 projector merged (shared blackout-source seam). `BlackoutDaysExtensions` (locked).

## Effort / reference class
~0.5–1 day. Reference class: ForecastController date-math edits.

## D8 gate
Forecasting code touched (`ForecastController`, counter) → brief + manual review BEFORE commit.
