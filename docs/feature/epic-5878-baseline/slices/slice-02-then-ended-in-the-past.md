# Slice 02 — A Then window that ended in the past, of any length from 14 days

**Feature**: epic-5878-baseline · **Epic**: #5878 · **Story**: US-02 (ADO #6160) · **Estimate**: ~1d · **Tier**: Community ·
`job_id: job-flow-coach-show-whether-flow-changed`

## Goal

A coach whose engagement started two months ago freezes the 90 days before it: Then ends on a past date, rebuilt once
from history inside the data cutoff, then frozen. A coach whose history does not fit 30 or 90 days enters a custom
length.

> Revised 2026-10-03: the maintainer added a custom length (minimum 14 days) to the presets (D37).

## IN

- Then end-date picker in Create Report, default today; no future dates; window must lie inside
  `DoneItemsCutoffDays` (D21). Refusal copy names the earliest allowed start and never says "Baseline".
- Length: Team presets 30 / 90 days plus a custom number of days, at least 14 (D37); "Then needs at least 14 days"
  otherwise.
- Rebuild the {Cycle Time} panel's values (and every metric registered by then) for the past window with today's settings, using the same calls
  the Metrics tab uses (Story 6053 precedent). Calendar days, inclusive, instance time zone (Bug #5567).
- Default name reflects the end date: "Then & Now — 90 days to 31 Jul 2026".

## OUT

Points beyond Then's limits (03), choosing Now's length (07; Now stays = Then length here).

## Learning hypothesis

**This disproves "history rebuilds a past Then credibly"** if a Then of 3 May – 31 Jul 2026 rebuilt today differs
from the Metrics tab for the same dates on the dev instance. Then the rebuild path is wrong, and every panel would
rest on it.

## Data and dogfood moment

- Demo: Team Lightspeed, 30 days ending 45 days ago (demo history is short, S11).
- Dogfood: on the dev instance, freeze 90 days ending 31 Jul 2026 and compare each percentile with the Metrics tab
  set to the same dates. Try an end date that breaks the cutoff, and a custom 10 days, and read both messages.

## Acceptance criteria

AC-2.1 … AC-2.6 in `feature-delta.md` (US-02).

## Dependencies

Slice 01.
