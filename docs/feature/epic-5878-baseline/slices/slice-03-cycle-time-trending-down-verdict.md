# Slice 03 — {Cycle Time}: Now's {Work Items} beyond Then's frozen limits

**Feature**: epic-5878-baseline · **Epic**: #5878 · **Story**: US-03 (ADO #6161) · **Estimate**: ~1d · **Tier**: Community ·
`job_id: job-flow-coach-show-whether-flow-changed`

> Re-cut 2026-10-03: was "'{Cycle Time} is trending down': the first verdict". The report gives no verdicts any more
> (D39); this slice now adds the count of Now's points beyond Then's frozen limits (D40). The file name keeps its old
> slug so existing links still resolve.

## Goal

The {Cycle Time} panel counts how many of Now's finished {Work Items} fall beyond the limits frozen from Then, split
above and below and coloured by {Cycle Time}'s direction of good (down). No verdict; the reader interprets.

## IN

- Then's XmR average and limits for {Cycle Time}, frozen **unrounded** at creation (`XmRResult` rounds, S3); never the
  owner's PBC Baseline (S4), never Now's own data.
- Count of Now's {Work Items} beyond those limits, by side: "Work Items beyond Then's limits: 2 above · 0 below". Only
  points beyond the limits count, not runs or other special-cause rules (D40).
- Colour by direction of good, known only to the frontend panel: above red, below green, 0 neutral; the number is
  always shown.
- A lower limit at 0: below shows "—" with "Then's lower limit is 0" (Q8, confirmed 2026-10-03).
- Too few Then points for a process-behaviour chart: "—" with the reason the Metrics page gives (D41).
- Reports created before this slice show the count as "Not captured for this report" (D27).
- Demo data: check one demo Team gives a non-zero count; adjust Team Lightspeed's CSV only if none does (Checklist).

## OUT

Other panels (04, 05), any chart (dropped with slice 11), verdict words of any kind (D39), the Now length (07).

## Learning hypothesis

**This disproves "a count against frozen limits says something on real Teams"** if, on the dev instance, every Team
reads 0 above and "—" below over the 63 days since 31 Jul 2026. Then the count is mute on per-item data, and its
shape must change before panels 04 and 05 copy it.

## Data and dogfood moment

- Dogfood: the 31 Jul report from slice 02 on every dev-instance Team; tally the counts and check two {Work Items}
  above the limit against the Metrics tab's {Cycle Time} PBC for the same dates.

## Acceptance criteria

AC-3.1 … AC-3.6 in `feature-delta.md` (US-03).

## Dependencies

Slices 01, 02.
