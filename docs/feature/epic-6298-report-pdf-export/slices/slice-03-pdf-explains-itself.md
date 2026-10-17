# Slice 03 — The report PDF explains itself

**Feature**: epic-6298-report-pdf-export · **Epic**: #6298 · **Story**: US-03 (ADO: to be created) · **Estimate**: ~½d ·
**Tier**: Community · **Ships alone**: yes · `job_id: job-share-numbers-outside-lighthouse`

## Goal

A reader who has never opened Lighthouse — Sabine Roth, the client's sponsor, three weeks after the export — can tell
from the PDF alone which side is frozen and since when, what Now covers, and what the colours, signs and "—" mean.

## IN

- One "how to read this" line under the PDF header (D13, D21), in the instance's terms: Then is frozen (its window,
  as captured then); Now is the window up to the produced date; green better, red worse, the sign and word always
  shown; "—" means no data in that window.
- Shown in the PDF only; the screen is unchanged.
- Copy confirmed with the maintainer at the start of DISTILL (sketch).

## OUT

A legend per panel · explanations for report types that do not exist yet · a confidentiality line (D13).

## Learning hypothesis

**Disproves "one line explains the report to a cold reader"** if someone who has never opened Lighthouse, handed
the PDF by the maintainer, cannot say which side is frozen and whether {Cycle Time} got better (K3: target ≥ 2 of
3 people).

## Data and dogfood moment

- Dev instance (`:5169`): export the busiest Team's report; the maintainer hands it to up to three people who do not
  use Lighthouse and logs their answers (K3).
- An instance with renamed terms (e.g. Cycle Time → "Lead Time", Work Items → "Tickets") checked once.
- E2E: none added.

## Acceptance criteria

AC-3.1 … AC-3.4 in `feature-delta.md` (US-03).

## Dependencies

Slice 01 (the PDF header it sits under).
