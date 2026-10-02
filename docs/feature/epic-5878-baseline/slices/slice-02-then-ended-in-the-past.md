# Slice 02 — A Then window that ended in the past

**Feature**: epic-5878-baseline · **Epic**: #5878 · **Story**: US-02 (ADO #6160) · **Estimate**: ~1d · **Tier**: Community ·
`job_id: job-flow-coach-show-whether-flow-changed`

## Goal

A coach whose engagement started two months ago freezes the 90 days before it: Then ends on a past date, rebuilt once
from history inside the data cutoff, then frozen.

## IN

- Then end-date picker in Create Report, default today; no future dates; window must lie inside
  `DoneItemsCutoffDays` (D21). Refusal copy names the earliest allowed start and never says "Baseline".
- Rebuild C1 (and every claim registered by then) for the past window with today's settings, using the same calls
  the Metrics tab uses (Story 6053 precedent). Calendar days, inclusive, instance time zone (Bug #5567).
- Default name reflects the end date: "Then & Now — 90 days to 31 Jul 2026".

## OUT

Verdicts (03), clipping Now after Then (03), Now choice (07).

## Learning hypothesis

**This disproves "history rebuilds a past Then credibly"** if a Then of 3 May – 31 Jul 2026 rebuilt today differs
from the Metrics tab for the same dates on the dev instance. Then the rebuild path is wrong, and slice 03's verdicts
would rest on it.

## Data and dogfood moment

- Demo: Team Lightspeed, 30 days ending 45 days ago (demo history is short, S11).
- Dogfood: on the dev instance, freeze 90 days ending 31 Jul 2026 and compare each percentile with the Metrics tab
  set to the same dates. Try an end date that breaks the cutoff and read the message.

## Acceptance criteria

AC-2.1 … AC-2.4 in `feature-delta.md` (US-02).

## Dependencies

Slice 01.
