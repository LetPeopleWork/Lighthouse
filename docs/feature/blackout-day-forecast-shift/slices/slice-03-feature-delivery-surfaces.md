# Slice 03 — Feature & Delivery forecast dates and likelihood reflect blackouts

**Story:** US-03 · **Surface:** Portfolio → Delivery read surfaces · **Direction:** both

## Goal
One sentence: drop the proven projector (Slice 01) and counter (Slice 02) into the feature and delivery paths so portfolio percentile dates and delivery likelihood account for known non-working days too.

## IN scope
- `HowManyForecast.TargetDate` (`CreationTime.AddDays(days)`) → projector.
- `Delivery.cs:102` expected date + `DeliveryWithLikelihoodDto.ExpectedDate` → projector.
- `Feature.GetLikelhoodForDate(date)` `(date - Today).Days` → working-day counter.

> Blackout periods are **global** (`blackoutPeriodRepository.GetAll()`), so the projector/counter read the same global set everywhere — no team/scope resolution. A multi-team feature forecasts each team independently as today; the shift does not change that (each team's days→date projection uses the same global periods). (D9.)

## OUT scope
- Write-back date (Slice 04). Monte Carlo (D4). Per-team blackout scoping (periods are global).

## Learning hypothesis
**Disproves** "the same projector/counter drop into the feature & delivery paths unchanged" **if** portfolio dates diverge from the manual-forecast dates for the same inputs.
**Confirms** one translation layer over the global periods serves every read surface.

## Acceptance criteria
US-03 AC1 (feature/delivery dates step over + roll forward), AC2 (`GetLikelhoodForDate` working days), AC3 (no-blackout unchanged). Production-data: a real Portfolio → Delivery whose features' teams have a configured future blackout.

## Dependencies
Slices 01 + 02 merged. Multi-team blackout-source decision from DESIGN.

## Effort / reference class
~1 day. Reference class: delivery-metrics / delivery-target-date-tracking date-projection edits.

## D8 gate
Core forecast-result code (`Feature`, `Delivery`, `HowManyForecast`, delivery DTOs) → brief + manual review BEFORE commit.
