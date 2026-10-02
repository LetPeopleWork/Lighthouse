# Slice 05 — {Cycle Time} predictable; 85th percentile lower by 10% or more

**Feature**: epic-5878-baseline · **Epic**: #5878 · **Story**: US-05 (ADO #6163) · **Estimate**: ~1d · **Tier**: Community ·
`job_id: job-flow-coach-show-whether-flow-changed`

## Goal

The two remaining v1 claims: C5 judges Now on its own limits; C6 gives the sponsor the familiar 85th-percentile
number with a 10% threshold.

## IN

- Rule kind `NoSignalsInWindow` (C5): Now on its own limits; Then's signal state frozen and shown for reference.
- Rule kind `ThresholdChange(down, 10%)` (C6): Then vs Now 85th percentile with the percentage ("21 → 12 days
  (−43%)"); {SLE} shown when the owner has one; within ±10% = No change yet.
- Both unticked by default; frozen like every claim.

## OUT

Nice-to-have claims (Out of scope list), percentage on other claims (the DIVERGE dissent trigger, not pulled).

## Learning hypothesis

**This disproves "a sponsor needs the familiar percentage beside the verdict"** (R-A) if C6 is ticked on fewer than
1 in 3 dogfood and early reports; then C6 can leave the default catalog in a later template.

## Data and dogfood moment

- Demo: Team Lightspeed has an {SLE} (85% / 7 days), so the reference shows.
- Dogfood: show a C6 row to a reader who is not a flow expert; note whether the verdict or the percentage is read first.

## Acceptance criteria

AC-5.1 … AC-5.3 in `feature-delta.md` (US-05).

## Dependencies

Slice 04 (claims picker).
