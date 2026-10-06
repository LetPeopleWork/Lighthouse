# ADR-215: The need band is the manual forecast's How Many for the next Refinement date, read at (100 − p), and everything on the Refinement tab is derived on read

- **Status**: Accepted for the need number and its verdict (DELIVER, 2026-10-05); the band setting is still Proposed.
  **Amended 2026-10-04**: decision 6 no longer holds for `lineAfterPosition` and `fewerListedThanHigh` — the maintainer
  moved the "enough for" line to the browser, which places it after the high end's count of rows in whatever order it
  shows them, so the response carries neither.
  **Amended 2026-10-06 (Story #6204)**: decision 1's window is superseded. See "Amendment 2026-10-06" below.
- **Date**: 2026-10-02
- **Feature**: epic-5510-5881-refinement (ADO Epic #5881 slices 02, 04, 05, 06, 07; Epic #5510 slice 10)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

The Refinement tab answers one question before the next Refinement: are enough Work Items ready — below, in or
above the range the Team is likely to pull? The maintainer settled the method (D20): the existing `HowMany` Monte
Carlo over the Team's total Throughput, from now until the next Refinement date, unchanged. The band is low = 50%
and high = 85% by default (DD-2), a Team setting from slice 07.

Facts from the code that the band must respect:

- `HowManyForecast` sorts its keys **descending** (`HowManyForecast.cs:7`), so `GetProbability(85)` is "at least this
  many with 85% likelihood" — the **conservative, low** count (ADR-210 recorded the same trap for the Reality Check).
  The band's high end is the count the Team exceeds with only 15% likelihood, which is `GetProbability(15)`.
- The manual forecast already computes How Many for a target date: throughput from
  `ITeamMetricsService.GetForecastThroughputStatus(team, mode)` and a horizon of **blackout-aware working days**
  from today to the date (`ForecastController.cs:104-139`). Its `HasSufficientData` is the minimum-data guard AC-5.6
  asks to reuse.
- The cadence (weekdays + every N weeks + starting week, D27) is the same calendar rule as recurring blackouts
  (`RecurringBlackoutRuleExtensions.Matches`, ADR-060), currently private there.
- Team forecasts do **not** order Work Items. The only order ladder is `FeatureComparer.CompareOrderValues`, which
  compares the tracker's `Order` value (`Models/FeatureComparer.cs:28`) and is equally defined for Work Items.
- There is no server-side "metrics window" per Team; the Metrics tab's window is a browser choice. The one per-Team
  history window the server owns is the Throughput window (`Team.GetThroughputSettings(today)`).

## Decision

1. **The band is the manual forecast's How Many for target date = next Refinement.** Same throughput call (with the
   Team's own filter setting respected, as forecasts do), same blackout-aware working-day horizon, same engine. The
   value at a user-facing percentile *p* ("the Team pulls at most this many with *p*% likelihood") is
   **`GetProbability(100 − p)`**: low (50) = `GetProbability(50)`, high (85) = `GetProbability(15)`.
2. **Verdict**: `Below` when ready < low, `In` when low ≤ ready ≤ high, `Above` when ready > high. When there is no
   number, there is no verdict but a closed reason: `NoCadence`, `InsufficientData` (the existing guard),
   `NoRefinementStates`.
3. **Next Refinement date** is a pure function of the cadence and `ILighthouseClock.Today` (instance zone): the first
   cadence date **strictly after** today (DD-6), using the week-modulo rule **extracted** from the blackout code into
   a shared pure `WeeklyRecurrence`. The date is not shifted by blackouts (US-04); the horizon in working days is,
   exactly as in the manual forecast.
4. **Backlog order** is `WorkItem.Order` through `FeatureComparer.CompareOrderValues`, tiebreak `ReferenceId`.
5. **SLE yardstick**: the Team SLE when both fields are set; else the 85th percentile of the Team's **default** cycle
   time (`GetCycleTimePercentilesForTeam`) over the **Throughput window**; else unavailable. Never a choice of
   cycle-time definition (D24).
6. **All of it is derived on read and never stored**, the pattern of the SLE risk and the Reality Check
   (`ARCHITECTURE.md` §6). The response carries facts only — counts, band, percentiles, date, horizon,
   `isRefinementDay`, verdict or reason, `lineAfterPosition` and `fewerListedThanHigh` — and every client composes
   its own sentence with the instance's Terminology.

## Amendment 2026-10-06 — the window is one Refinement cycle (Story #6204)

The maintainer redefined the need as a replenishment target: a Refinement must leave enough on the shelf to last
until the following one. **Decision 1's window "target date = next Refinement, from today" no longer holds.** The
How Many now runs over **one Refinement cycle**: the working days after the next Refinement up to and including the
Refinement after it, or from today to the next Refinement when today is a (not blacked-out) Refinement day. A
Refinement on a blackout day is skipped, so the cycle runs to the next one that happens; blackout days inside the
cycle are not working days. The engine, the throughput call, the reading at `GetProbability(100 − p)`, the verdict,
the reasons and decisions 2–6 are unchanged. `HowMany` takes a day count only, so moving the window's start changes
nothing but that count.

The cycle is a fact of the calendar (`RefinementCadenceCalendar`, pure), the blackout lookup is extended to the end
of the cycle so no unfetched day is ever judged, a cadence with no Refinement after the next one reads `NoCadence`,
and the response's `need` gains `cycleStart` / `cycleEnd` while `horizonWorkingDays` keeps its name and counts the
cycle. **Enforcement E9 is restated**: the band equals the manual How Many over the same throughput for the cycle's
working-day count. Full decisions: feature delta, DSN-23..DSN-30.

## Alternatives considered

- **A new simulation over Work Items leaving refinement.** Withdrawn by the maintainer (D20); would also have been a
  second Monte Carlo to keep honest. **Rejected.**
- **Reading the band at `GetProbability(p)`** (the obvious call). Puts the 85% "high end" *below* the median and
  inverts the verdict. **Rejected** — this ADR exists mostly to stop it.
- **Storing the band daily (a snapshot table).** Nothing asks for its history; K3/K6 are read from usage events.
  **Rejected** — a table with no reader.
- **Ignoring the Team's forecast filter** ("total" read literally). Makes the Refinement band disagree with the
  Forecasts tab for the same date on Teams that use the filter. Not chosen; left to the maintainer (MQ-1) because
  D20's "total" could be read either way.
- **A hard-coded 30- or 90-day window for the SLE fallback.** A second window beside the Team's own for no gain.
  **Rejected.**

## Consequences

- **Positive**: one engine, one horizon rule and one order ladder; the band for a date equals what the manual
  forecast shows for that date, which is testable (parity) and explainable.
- **Positive**: no stored state means no staleness, no backfill, no cache invalidation.
- **Negative**: every tab open runs one How Many (trials × days ≤ ~30) — milliseconds; throughput is already cached.
- **Negative**: `WeeklyRecurrence` extraction touches shipped blackout code; it is a behaviour-preserving refactor in
  its own commit, held green by the existing blackout tests.

## Enforcement

- Property test: `NeedBand.ValueAt(p) == forecast.GetProbability(100 − p)` and `low ≤ high` for 1 ≤ low < high ≤ 99.
- Parity test: band == manual-forecast How Many values for target date = next Refinement.
- `NeedBand`, `RefinementCadenceCalendar`, `WeeklyRecurrence` are static and pure (ArchUnitNET).
- No `DateTime.UtcNow`/`Today` in the feature (existing `CalendarDayAnchorSeamArchUnitTest`).

Cross-refs: [ADR-039](./adr-039-forecast-data-sufficiency-backend-signal.md),
[ADR-060](./adr-060-recurring-blackout-rule-entity-weekday-storage-and-expansion.md),
[ADR-192](./adr-192-sle-risk-as-a-pure-conditional-over-the-cycle-time-population.md),
[ADR-209](./adr-209-a-report-is-a-response-not-a-record.md),
[ADR-210](./adr-210-a-forecast-level-holds-or-it-does-not-and-its-nominal-rate-is-the-level.md),
[ADR-214](./adr-214-refinement-settings-are-one-json-valued-property-on-the-team.md).
