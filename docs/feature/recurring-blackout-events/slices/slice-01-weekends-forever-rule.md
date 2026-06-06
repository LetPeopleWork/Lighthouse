# Slice 01 — "Exclude weekends forever" recurring rule, end-to-end

**Story:** US-01 · **Surface:** new recurring-rules section (`POST /api/.../recurring-blackout-rules`) + recurrence expansion + unified evaluation · **Direction:** rule → concrete blackout days → forecast shift

## Goal
One sentence: let a config-admin create an open-ended weekly rule (Sat+Sun, every 1 week, start today, no end) that appears in the settings list AND immediately causes a "When" forecast to step its percentile dates over the upcoming weekend.

## IN scope
- New `RecurringBlackoutRule` entity (weekday set, interval=weeks, start `DateOnly`, optional null end) + repository + DTO, reusing the one-off `BlackoutPeriod` CRUD shape.
- `POST` + `GET` on a new `api/{v1|latest}/recurring-blackout-rules` controller; POST carries the same `[LicenseGuard(RequirePremium=true)]` + `[RbacGuard(SystemAdmin)]` pair; GET open.
- Recurrence **expansion**: rule → concrete non-working days, fed into the SAME `IsBlackoutDay`/`GetBlackoutDayIndices` evaluation the one-off periods feed (unified evaluation, D4). DESIGN decides materialise-vs-evaluate.
- Minimal FE: a "Recurring Blackout Rules" section in `BlackoutPeriodsSettings.tsx` neighbourhood with an Add dialog (weekday checkboxes, interval defaulting to 1, start date, empty end = forever) + a read-only list row with a human-readable summary.

## OUT scope
- Interval cadence > 1 / concrete end date as the primary path → Slice 02 (the field exists but the demoed path is weekly-forever).
- Edit / delete / validation error paths → Slice 04.
- Proving every downstream surface (feature/delivery/write-back/charts) → Slice 03.

## Learning hypothesis
**Disproves** "a recurring rule can expand to concrete days that feed the SAME unified blackout-day evaluation the one-off periods feed (D4), so the shipped #4974 day↔date shift consumes them for free" **if** a forecast for a team with the weekends rule does NOT step over the next Saturday/Sunday, OR if the recurring day is distinguishable from a one-off blackout day downstream.
**Confirms** the unified-evaluation seam is the reusable primitive for Slices 02/03.

## Acceptance criteria
- US-01 AC1: a "weekends forever" rule (Sat+Sun, every 1 week, start today, no end) created via POST appears in GET with a summary line.
- US-01 AC2: with that rule and no one-off periods, a "When" forecast's percentile dates step over the next Sat+Sun (each shifted by the count of weekend days in the window) and none land on a Sat/Sun.
- US-01 AC3: a recurring-rule day and a one-off `BlackoutPeriod` day produce identical `IsBlackoutDay`/index results (unified evaluation, D4).
- US-01 AC4: creation by a non-premium or non-SystemAdmin caller is rejected (403); GET succeeds for a viewer.
- Production-data: a real team with a near-future weekend and the weekends-forever rule configured.

## Dependencies
Shipped + LOCKED: one-off `BlackoutPeriod` CRUD pattern, `BlackoutPeriodsSettings.tsx`, premium+SystemAdmin guard pattern, `BlackoutDaysExtensions` evaluation seam, the #4974 day↔date forecast shift (`ProjectWorkingDays`). New endpoint → version-gate the client wrapper (see feature-delta cross-cutting).

## Effort / reference class
~1 day. Reference class: the shipped one-off `BlackoutPeriod` slice (entity + DTO + controller + service + settings table) plus the recurrence-expansion function (the only genuinely new logic).

## Slice value (no infrastructure-only slice)
Delivers an observable, user-verifiable behavior: admin creates ONE rule → sees it listed AND a forecast date visibly steps over the weekend. The unified-evaluation "infra" ships inside this value-producing slice, never alone.
