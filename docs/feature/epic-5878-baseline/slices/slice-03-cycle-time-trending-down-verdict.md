# Slice 03 — "{Cycle Time} is trending down": the first verdict

**Feature**: epic-5878-baseline · **Epic**: #5878 · **Story**: US-03 · **Estimate**: ~1d · **Tier**: Community ·
`job_id: job-flow-coach-show-whether-flow-changed`

## Goal

The {Cycle Time} row becomes a claim with a verdict — Holds, Does not hold, No change yet or Not enough data — judged
against limits computed from the frozen Then series. Now starts no earlier than the day after Then ends.

## IN

- Rule kind `ShiftInDirection(down)` as its own class; C1 registered with it.
- Limits recomputed on each view from the frozen Then series (`XmRResult` rounds limits, S3), never from the
  owner's PBC Baseline (S4). `XmRCalculator.Calculate(frozenThen, now)` reused.
- Verdict rules per D24 (sustained signal for Holds; any bad-side signal = Does not hold; lone good-side point named,
  No change yet); colour only for Holds / Does not hold.
- Not enough data per D25 (≥ 8 finished {Work Items} in Then, ≥ 1 in Now), "—" with the reason.
- Now clipped to start after Then (D23); a report frozen today reads No change yet, "Now begins after 2 Oct 2026";
  the header names the clipped Now.
- Zero-clamp disclosure in the claim detail (D28).
- Demo data: adjust Team Lightspeed's CSV so a 30-day Then and the Now after it yield a Holds (Checklist).

## OUT

Other claims (04, 05), the chart behind the verdict (11), Now choice (07).

## Learning hypothesis

**This disproves "PBC rules give a usable verdict on real Teams"** if, on the dev instance, C1 reads No change yet
or Not enough data for every Team over the 63 days since 31 Jul 2026. Then the zero clamp and low counts make the
claim mute, and D24/D25 (Q3, Q4) must change before more claims are built on them.

## Data and dogfood moment

- Dogfood: the 31 Jul report from slice 02 on every dev-instance Team; tally verdicts and note any Holds that a
  look at the Metrics tab would not support.
- Confirm Q2–Q4 with the maintainer before accepting this slice.

## Acceptance criteria

AC-3.1 … AC-3.6 in `feature-delta.md` (US-03).

## Dependencies

Slices 01, 02. Maintainer answers to Q2, Q3, Q4 (provisional assumptions applied meanwhile).
