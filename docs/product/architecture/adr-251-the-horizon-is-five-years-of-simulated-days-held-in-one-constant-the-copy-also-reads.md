# ADR-251: The horizon is five years of simulated days, held in one constant the copy also reads

**Status**: Accepted (maintainer, 2026-10-18, DESIGN; recommendation taken; day count revised to 1,827 at the review
gate).
**Feature**: `epic-6311-responsive-during-updates` (Story #6309, slice 04)
**Depends on**: [ADR-250](./adr-250-total-trials-counts-every-simulated-run-and-a-run-that-does-not-finish-is-beyond-the-horizon.md).

## Context

`ForecastSimulationLimits.Default` lets one run cover 100,000 simulated days (about 274 years). Its comment says no data
can reach that. A low-throughput Team with many items does reach it, and each run steps one day at a time, so the
slowest forecasts spend minutes simulating centuries. A simulated day is a non-blackout calendar day: dates are projected
over working days (`BlackoutDaysExtensions.ProjectWorkingDays`).

## Decision

1. A new domain constant, `ForecastHorizon`, holds **years = 5** and derives **simulated days = 5 × 365 + 2 = 1,827**.
   Five calendar years hold at most 1,827 days (at most two leap days), so the horizon always covers them.
2. `ForecastSimulationLimits.Default` takes its run cap from `ForecastHorizon`. Fixtures that construct their own
   limits keep theirs; most already read the cap from `Default`, so they follow the change.
3. The copy "More than 5 years" reads the years from the wire (`horizonYears`, ADR-252), which reads `ForecastHorizon`.
   No second literal exists in the web app or the clients.
4. Why the copy stays true: a run not finished by simulated day 1,827 cannot finish before calendar day 1,828 from
   today, which is more than 5 calendar years away.
5. The comments on `HowTheRunEnded.RanOutOfDays` and `ForecastSimulationLimits` are corrected: data can reach the
   horizon, and a run that does is counted, not an error.
6. Builds after ADR-250 (honest counting first). Both ship in one release under Story #6309 (NMD-5).

## Alternatives considered

- **1,826 days** (5 × 365 + 1): one day short of 5 calendar years when the window holds two leap days.
- **A horizon in calendar years computed per instance** from the blackout calendar: the cap would move with
  configuration and with today's date.
- **The day count written as a bare number**: the copy would need its own "5".
- **Keep 100,000**: the cost is the incident.

## Consequences

**Positive**
- A run costs at most 1,827 steps; the slowest forecast is expected to be at least 50% faster (KPI-3).
- One place to change the horizon.

**Negative / accepted**
- With weekends configured as blackouts, a percentile inside the horizon can show a real date up to about 7 years out.
  It is a true date.
- DISCUSS's AC-04.1 and the slice-04 UAT say 1,826. DESIGN's Changed Assumption 7 records the change; DISTILL's
  scaffolds that pin 1,826 follow it.

## Enforcement

- Unit: `Default`'s run cap equals `ForecastHorizon.SimulatedDays` (1,827), and no simulated run passes it.
- Seeded fixture baselines for Features that finish within 5 years are unchanged; any that move are listed in the
  commit body.
- Frontend and clients tests render the years from the payload, not from a literal.
