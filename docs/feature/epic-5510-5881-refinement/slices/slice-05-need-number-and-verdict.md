# Slice 05 — Know whether to refine more or stop

> **Superseded in part on 2026-10-04 (maintainer): the ready count only.** The verdict uses the votes' ready count
> on a Team without stage rules and the stages' on a Team with them — never the two added together. Everything else
> here stands. See `../feature-delta.md` → "Maintainer decision — the E2 UI, and slice 08 folded into 03
> (2026-10-04)" and `../distill/upstream-issues.md`.

**Feature**: epic-5510-5881-refinement · **Epic (proposed)**: E2 Refinement need (#5881) · **Story**: US-05 ·
**Estimate**: ~1d · **Tier**: Community

## Goal

The tab opens on one verdict about the next Refinement: below, in or above range. "Stop refining" is as loud as
"refine more" (C5).

## IN

- One `HowMany` run over the Team's Throughput, covering the days from today to the next Refinement. It uses the same
  history window and settings as the Team's forecasts (D20, AC-5.1).
- The band: the low end is the 50% value; the high end is the count exceeded with only 15% likelihood (DD-2). Mind the
  `HowManyForecast` semantics: its "85%" value is the low, conservative count, not the high end.
- The verdict per DD-3, from the ready count: state stages (slice 03) plus the vote-ready Work Items from slice 13,
  which ships earlier (DD-22, AC-5.7). The API returns facts only: ready count,
  low, high, date and a verdict enum. The client composes the sentence with Terminology.
- One component with three states, all the same size and in the same position. Copy:
  - below: "3 ready — below the range of 5–8 … Refine 2 to 5 more."
  - in: "Nothing more needs refining before Thu 8 Oct."
  - above: "Stop refining: nothing more is needed before Thu 8 Oct."
- The existing minimum-data guard and its message are reused.

## OUT

The "enough for" line (06), percentile settings (07; defaults are hard-wired here), the SLE yardstick (10).

## Learning hypothesis

**This disproves "HowMany over total Throughput gives a band narrow enough to act on" (D20)** if, on the dev instance's
real Throughput, the one-week band is so wide (e.g. 2–14) that the verdict is nearly always "in range". If that
happens, revisit the default percentiles (07) or the horizon before E3 builds on the verdict.

## Data and dogfood moment

- Dev instance (`:5169`, real history): record the band for the next Refinement on three Teams, and how often each
  verdict appears over a week of daily looks.
- Demo: Team Gravity, with fixed-seed tests for each of below, in and above.

## Acceptance criteria

AC-5.1 … AC-5.6 (US-05).

## Dependencies

Slices 03, 04. Picks up slice 13's vote-ready count if it has shipped (it normally has, DD-22).
