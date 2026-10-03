# Slice 05 — {WIP} and {Work Item Age} panels

**Feature**: epic-5878-baseline · **Epic**: #5878 · **Story**: US-05 (ADO #6163) · **Estimate**: ~1d · **Tier**: Community ·
`job_id: job-flow-coach-show-whether-flow-changed`

> Re-cut 2026-10-03: was "{Cycle Time} predictable; 85th percentile lower by 10% or more". Predictability was a
> stability judgement and is dropped (D39); the familiar 85th percentile and its % change already sit in the {Cycle
> Time} panel (slice 01). This slice completes the v1 catalog instead. The file name keeps its old slug so existing
> links still resolve.
>
> Revised 2026-10-03 after DESIGN: average {Work Item Age} has two values per side like the total (D48); every value
> shows its sample size (D45).

## Goal

The last two v1 panels, so a report shows all four metrics by default: "{WIP}: Then & Now" and "{Work Item Age}: Then
& Now".

## IN

- {WIP} panel: average {WIP} Then and Now with its range, change and % change on the average; good = down.
- {Work Item Age} panel: total and average {Work Item Age} Then and Now, change and % change on each; good = down.
  Total {Work Item Age} shows both the average of the daily totals over the window and the actual total on the
  window's last day, each with change and % change (Q10, confirmed 2026-10-03). Average {Work Item Age} likewise
  shows the window average (mean over the window's days of that day's total age ÷ that day's {WIP}; days without
  {WIP} left out) and the last day's value (last day's total age ÷ that day's {WIP}), each with change and % change
  (D48).
- Days of Now beyond Then's frozen limits, by side, for {WIP} and total {Work Item Age} (D40): above red, below green.
- All four metrics ticked by default. Every value with its sample size; "—" with a reason only when truly empty, e.g. no
  {WIP} for an average age (D45).
- Reports created before this slice show both as "Not captured for this report" (D27).

## OUT

Nice-to-have metrics ({SLE} breaches, arrivals, {WIP} streaks, {Feature} size, forecast lens); {Cycle Time}
predictability (dropped, D39).

## Learning hypothesis

**This disproves "coaches keep the default of all four"** if dogfood users untick two or more of the four metrics on
most reports. Then the template's default selection is wrong.

## Data and dogfood moment

- Demo: Team Gravity for {WIP}; Team Lightspeed for {Work Item Age}.
- Dogfood: one report per dev-instance Team; note what gets unticked.

## Acceptance criteria

AC-5.1 … AC-5.5 in `feature-delta.md` (US-05).

## Dependencies

Slice 04 (picker, panel registrations).
