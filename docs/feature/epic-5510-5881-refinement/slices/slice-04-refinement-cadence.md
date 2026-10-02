# Slice 04 — Set the Refinement cadence; the tab names the next Refinement

**Feature**: epic-5510-5881-refinement · **Epic (proposed)**: E2 Refinement need (#5881) · **Story**: US-04 ·
**Estimate**: ~1d · **Tier**: Community

## Goal

A Team admin sets when the Team refines: one or more weekdays, every N weeks, and a starting week when N > 1. The
tab then reads "Next Refinement: Thu 8 Oct". Without a cadence, the tab still shows the list and votes, plus a hint (D29).

## IN

- A cadence setting in Settings → Refinement. Weekdays, N ≥ 1, and a starting week required when N > 1. Expand-only
  storage.
- A next-date rule (DD-6): the first cadence date strictly after today, in the instance time zone. On a Refinement
  day, it is the following one.
- The tab header shows the date, or the hint "Set a Refinement cadence to see how many Work Items are needed" (role-aware,
  as in DD-15).
- The API returns the date as a fact (ISO date), not as text.

## OUT

The need number (05). Blackout-aware dates (out of scope; a Refinement on a holiday is the Team's call).

## Learning hypothesis

**This disproves "weekdays + every N weeks expresses real cadences" (D27)** if Teams refine on "the first Tuesday of
the month" or ad hoc. Watch the dev instance Team and ask 3 known users. If it fails, the cadence model is re-planned
before slice 05 builds on it.

## Data and dogfood moment

- Demo: Team Gravity, Thursdays, every week. Unit tests use fixed dates: Fri 2 Oct 2026 → Thu 8 Oct; Thu 8 Oct →
  Thu 15 Oct; Tuesdays every 2 weeks from the week of 6 Oct → Tue 6 Oct, then Tue 20 Oct.
- Dogfood: set the dev Team's real cadence. Does the date shown match the calendar invite?

## Acceptance criteria

AC-4.1 … AC-4.4 (US-04).

## Dependencies

Slice 01.
