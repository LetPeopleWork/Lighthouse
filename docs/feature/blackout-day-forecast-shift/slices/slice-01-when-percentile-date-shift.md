# Slice 01 — "When" percentile dates step over future blackout days

**Story:** US-01 · **Surface:** manual "When" forecast (`POST /api/.../forecast/manual/{id}`) · **Direction:** days→date

## Goal
One sentence: turn the "When" forecast's percentile *days* into a calendar date that skips configured future blackout periods and never lands on one — without touching the Monte Carlo.

## IN scope
- A shared working-day **date projector**: given a start date + a working-day count N, advance calendar days, skip days inside any `BlackoutPeriod`, stop after N working days; if the result lands on a blackout day, roll forward (D3).
- Wire it into `WhenForecastDto.GetFutureDate` (currently `Today.AddDays(days)`).
- Source blackout periods for the team's scope (reuse `BlackoutDaysExtensions` / repository; DESIGN decides the seam).

## OUT scope
- Date→days conversion (likelihood / how-many-by-date) → Slice 02.
- Feature/Delivery/write-back surfaces → Slices 03/04.
- Any Monte Carlo / `GetProbability` change (D4).

## Learning hypothesis
**Disproves** "a single shared working-day projector can serve the date path without altering the Monte Carlo days" **if** the percentile dates don't move by exactly the count of intervening blackout days, or the `GetProbability` day-values change.
**Confirms** the projector is the reusable primitive for Slices 03/04.

## Acceptance criteria
- US-01 AC1 (10 days + 2 blackout → 12 calendar days), AC2 (roll-forward on landing), AC3 (no-blackout unchanged), AC4 (`GetProbability` identical). Production-data: a real team with a configured `BlackoutPeriod` spanning a near-future weekend.

## Dependencies
Shipped `BlackoutPeriod` + `BlackoutDaysExtensions` (locked). No new endpoint.

## Effort / reference class
~0.5–1 day. Reference class: prior thin forecast-DTO changes (e.g. forecast-minimum-data-guard signal plumbing).

## D8 gate
Forecast-result code touched (`WhenForecastDto`, new projector) → brief the user + manual review BEFORE commit.
