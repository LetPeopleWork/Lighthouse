# Slice 04 — Choose the metrics a report shows; {Throughput} panel

**Feature**: epic-5878-baseline · **Epic**: #5878 · **Story**: US-04 (ADO #6162) · **Estimate**: ~1d · **Tier**: Community ·
`job_id: job-flow-coach-show-whether-flow-changed`

> Re-cut 2026-10-03: was "Pick claims: {Throughput}, {WIP}, Total {Work Item Age}". Claims and verdicts are gone
> (D39); this slice brings the metric picker and the {Throughput} panel. {WIP} and {Work Item Age} move to slice 05.
> The file name keeps its old slug so existing links still resolve.
>
> Revised 2026-10-03 after DESIGN: {Throughput} is the total and the per-day average over the whole window, not a
> weekly median (D47); every value shows its sample size (D45).

## Goal

The coach chooses which metrics the report shows before looking at Now, and gets a second panel: "{Throughput}: Then
& Now". Every applicable metric is frozen whatever is ticked.

## IN

- Metric picker in Create Report; every metric in the catalog ticked by default; at least one required ("Choose at
  least one metric").
- {Throughput} panel (D47): total {Work Items} finished in the window and the average per day, Then and Now; change
  and % change on the per-day average (correct also when Now's length differs from Then's); good = up. Days of Now
  beyond Then's frozen limits, by side (D40): above green, below red.
- A Then of 0 shows no % change: "—" with "Then was 0" (Q9, confirmed 2026-10-03).
- Every value with its sample size; "—" with a reason only when truly empty (D45).
- One panel registration per metric. Every applicable metric frozen at creation (D26). Reports created before this
  slice show {Throughput} as "Not captured for this report" (D27).

## OUT

{WIP} and {Work Item Age} panels (05); changing the selection later (10).

## Learning hypothesis

**This disproves "a per-day average reads sensibly beside a count of days beyond"** if dogfood readers misread the
per-day average or ask what "days beyond" means. Then the {Throughput} panel needs different wording.

## Data and dogfood moment

- Demo: Team Gravity's daily series (≈35 days); check a single busy day appears as "1 above", nothing more.
- Dogfood: one report per dev-instance Team; show the {Throughput} panel to a reader who is not a flow expert.

## Acceptance criteria

AC-4.1 … AC-4.6 in `feature-delta.md` (US-04).

## Dependencies

Slice 03 (the count against frozen limits).
