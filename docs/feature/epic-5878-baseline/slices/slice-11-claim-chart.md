# Slice 11 — Open the chart behind a verdict (cancellable)

**Feature**: epic-5878-baseline · **Epic**: #5878 · **Story**: US-11 (ADO #6169) · **Estimate**: ~1d · **Tier**: Community ·
`job_id: job-flow-coach-show-whether-flow-changed`

## Goal

A reader expands a claim and sees Now's points against the Then average and limits, with the points that decided the
verdict highlighted and traceable to {Work Items}.

## IN

- Expand a claim row → the existing process-behaviour chart component, fed with the frozen Then limits (never the
  owner's PBC Baseline) and Now's points.
- Signal points highlighted; per-{Work Item} claims name the {Work Items} (e.g. LS-412, LS-415).
- No chart for Not enough data; the reason only.

## OUT

Chart export, charts for ThresholdChange claims (C6 shows values only).

## Learning hypothesis

**This disproves "the verdict needs its chart to be trusted"** if readers in dogfood and early use never expand a
row. Then later templates can leave the chart out.

## Data and dogfood moment

- Demo: Team Lightspeed's Holds row from slice 03.
- Dogfood: show a report to a reader; note whether they expand before accepting a verdict.

## Acceptance criteria

AC-11.1 … AC-11.3 in `feature-delta.md` (US-11).

## Dependencies

Slice 03. Cancellable (Scope Assessment).
