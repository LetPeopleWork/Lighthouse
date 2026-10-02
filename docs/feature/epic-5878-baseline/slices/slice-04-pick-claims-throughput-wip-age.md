# Slice 04 — Pick claims: {Throughput}, {WIP}, Total {Work Item Age}

**Feature**: epic-5878-baseline · **Epic**: #5878 · **Story**: US-04 (ADO #6162) · **Estimate**: ~1d · **Tier**: Community ·
`job_id: job-flow-coach-show-whether-flow-changed`

## Goal

The coach chooses which claims the report makes before looking at Now. C1–C4 are preselected; every applicable claim
is frozen whatever is selected.

## IN

- Claims picker in Create Report; C1–C4 ticked; at least one claim required ("Choose at least one claim").
- C2 {Throughput} trending up (`ShiftInDirection(up)`; weekly median + total), C3 {WIP} stable or down and C4 Total
  {Work Item Age} stable or down (`NoShiftAgainst(down)`; average + range, average daily total + average {Work Item
  Age}). Rule kind `NoShiftAgainst` as a class; one registration per claim.
- Freeze all applicable claims at creation (D26). Reports created before this slice show C2–C4 as "Not captured for
  this report" (D27).

## OUT

C5, C6 (05), changing the selection later (10).

## Learning hypothesis

**This disproves "coaches keep the preselection"** if dogfood users untick two or more of C1–C4 on most reports;
then the template's default selection is wrong.

## Data and dogfood moment

- Demo: Team Gravity's daily series (≈35 days) for C2–C4; check a single busy day reads No change yet, not Holds.
- Dogfood: one report per dev-instance Team; note what gets unticked.

## Acceptance criteria

AC-4.1 … AC-4.5 in `feature-delta.md` (US-04).

## Dependencies

Slice 03 (verdict rules and the clipped Now).
