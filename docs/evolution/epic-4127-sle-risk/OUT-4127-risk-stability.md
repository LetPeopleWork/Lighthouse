# OUT-4127-risk-stability — the measurement that gates slices 02–04

Taken 2026-09-17. **Verdict: the risk is not stable enough to be shown unguarded, and the fix is the
one slice 01's brief already named.** Slices 02, 03 and 04 should not start until it is in.

> **Amended 2026-09-19 — the measurement stands; the prescription was tried and reversed.**
>
> Everything measured below is still true and none of it is disputed: the bound is real, the
> 25-point overnight swing on 602 real closed items happened, and a share over a thin tail does move
> sharply. What changed is the answer. The minimum-sample guard recommended in *What to do about it*
> shipped, ran, and was removed by round 2 (`docs/feature/epic-4127-sle-risk-corrections/`, ADO
> #6037), because withholding the number turned out to cost more than the instability did.
>
> **The instability is now disclosed rather than used to withhold an answer.** Every in-flight item
> carries a number, and each risk cell says how much finished work it rests on — so a reader can see
> that a 100% resting on two items is not the same claim as a 100% resting on forty, which is the
> thing the guard was protecting them from being unable to tell.
>
> The sections below are unedited except where marked. *Consequence for the gated slices* describes
> surfaces that no longer exist and is corrected at the end.

## What was asked

Slice 01 ships alone because of one worry, in its own words: the denominator `count(T >= a)` shrinks
as the age grows, so one item entering or leaving the window can move a displayed risk by 15+
percentage points overnight — on precisely the items a coach is being told to prioritise. If that is
what real data does, the honest response is to reconsider the presentation *before* three more
surfaces inherit the instability.

## How it was taken, and what that costs in confidence

The brief asks for a dev instance restored from a production backup. That was not available, and the
maintainer cannot run the restore. Two substitutes were used instead, and between them they answer
the arithmetic half of the question but not the human half:

1. **An exact bound**, which needs no data at all. The displayed risk at age `a` is a proportion over
   `n(a) = count(T >= a)` survivors, so one item entering or leaving that set moves it by at most
   `100/n(a)` points. This is not an estimate.
2. **A day-by-day replay** over the 602 real closed items on the `Lighthouse Dev` board
   (Sep 2025 – Sep 2026, cycle times `1×488, 2×91, 3×6, 4×7, 5×5, 6×2, 8×2, 13×1`), sliding the
   window one day at a time and recording how far each age's answer moved from the day before.

**What this cannot establish:** whether a coach reading the column recognises the ordering as true of
their own team. That is the other half of slice 01's learning hypothesis, it needs a person, and it
is still open.

## (1) The bound

| survivors `n(a)` | most the number can move overnight |
|---|---|
| 100 | 1.0 pts |
| 50 | 2.0 pts |
| 30 | 3.3 pts |
| 20 | 5.0 pts |
| 13 | 7.7 pts |
| 10 | 10.0 pts |
| 7 | **14.3 pts** |
| 5 | **20.0 pts** |
| 3 | **33.3 pts** |

The brief's 15-point worry begins at `n(a) ≤ 7`. At `n(a) ≥ 20` the arithmetic cannot produce it.

So the question was never "is this metric unstable" — it is "at the ages where items actually sit, how
many survivors are there?" That is a property of the window and the team's tail, and it is knowable
before the number is drawn.

## (2) The replay

Ages 1–2 hold; from age 3 the tail runs out and the number starts jumping.

**90-day window, 2-day target**

| age | median `n(a)` | min `n(a)` | worst move | p95 move | days moving >15 pts |
|---|---|---|---|---|---|
| 1 | 170 | 60 | 2 pts | 1 pt | 0 |
| 2 | 26 | 10 | 15 pts | 3 pts | 0 |
| 3 | 6 | 2 | 0 pts | 0 pts | 0 |
| 5 | 3 | 0 | — | — | — |

**90-day window, 3-day target** — age 3 now has answers, and they move: worst 25 pts, 6 days over 15.

**30-day window, 2-day target** — the same shape one age earlier: age 2 sits at `n = 10`, swings 9 pts
at p95, and crossed 15 pts on 8 days.

Two things follow. **The window is part of the answer**: the same team on a 30-day picker is
materially less stable than on 90, and the picker is one click. And **this team is the easy case** —
602 items, median cycle time 1 day. A team with a genuine tail puts more items at ages where `n(a)`
is single digits, not fewer.

## What to do about it

**— SUPERSEDED 2026-09-19. This is what was done, and then undone. See the amendment at the top.**

**Add a minimum-sample guard to the read, and let slices 02–04 build on the guarded number.**

The precedent is in the repo and is exactly this shape: `ForecastDataSufficiencyPolicy` refuses a
forecast below `MinimumActiveDays = 5` rather than serving a confident-looking one. Here the same
policy reads: below some `n(a)`, answer with the no-answer sentinel instead of a number.

At `n(a) ≥ 20` the number cannot move more than 5 points overnight, which is the threshold that makes
the displayed value mean what it appears to mean. That is a starting proposal, not a decided value —
it is strict enough that on the replay above it would blank ages 3 and up on a 90-day window, which
is most of the interesting items on that particular board. A lower bar of 10 caps the move at 10
points and keeps more of the column populated. **Which of the two is right is a product judgement
about how wrong a number is allowed to look, and it has not been made.**

What is decided by the data: **something must gate it**, and `Beyond history` is not that gate — it
fires only at `n(a) = 0`, which is one item short of the `n(a) = 1` case that reports 0% or 100% with
total confidence on a single observation.

## Consequence for the gated slices — SUPERSEDED 2026-09-19

**Written 2026-09-17 against surfaces that round 2 removed. Kept for the reasoning; see below for
what actually happened to each.**

- **Slice 02 (at-risk count chip)** — counts items at ≥ 50%. A guarded item has no percentage, so the
  chip needs a rule for it. AC-02.6 already says `Beyond history` counts as at-risk; the same
  question now arrives for "not enough history", and the answers need not be the same.
- **Slice 03 (chart zones)** — the zone boundaries are the ages where risk crosses 25/50/75%. Those
  boundaries are computed from the same thinning tail, so an unguarded chart redraws its geometry
  overnight. This is the slice the instability hurts most, and D12's own note agrees: zone boundaries
  that move day to day are worse than no zones.
- **Slice 04 (write-back)** — writes the integer into someone else's tracker, where Lighthouse cannot
  take it back. A guarded item must produce **no write**, following the existing `=> null`
  convention, exactly as `Beyond history` does.

### What happened instead

- **The chip** became a widget with a status of its own, and the line moved from 50% to 70%. There is
  no guarded item to write a rule for, because there are no guarded items.
- **The chart zones** were deleted outright — and this measurement is part of why. The paragraph
  above is right that the instability hurts the zones most; round 2's conclusion was that a ladder
  whose geometry moves overnight, in a mode a reader explicitly chose, should not exist rather than
  be stabilised. The chart's SLE reference line already answered the question the zones answered.
- **The write-back** needed no change. It still writes no value for an item with no answer; what
  changed is that "no answer" now means only *closed* or *no target published*, never *too little
  evidence*.

The one thing this measurement asked for that round 2 did deliver, in a different shape: a reader can
now see the depth of evidence behind any number, in the cell, in words.

## Reproducing it

This measurement's script was a
throwaway against a copy of the dev database. To retake it on better data, point it at a restored
instance and re-run the two steps above: the bound needs nothing, and the replay needs only
`(ClosedDate, cycle time)` pairs for the team's finished work.
