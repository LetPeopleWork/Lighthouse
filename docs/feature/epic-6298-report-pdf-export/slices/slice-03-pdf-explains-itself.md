# Slice 03 — The report PDF explains itself

**Feature**: epic-6298-report-pdf-export · **Epic**: #6298 · **Story**: US-03 (ADO: to be created) · **Estimate**: ~½d ·
**Tier**: Community · **Ships alone**: yes · `job_id: job-share-numbers-outside-lighthouse`

## Goal

A reader who has never opened Lighthouse — Sabine Roth, the client's sponsor, three weeks after the export — can tell
from the PDF alone which side is frozen and since when, what Now covers, and what the colours, signs and "—" mean.

## IN

- Three short lines under a "How to read this" label below the PDF header (D13, D21; NMD-5, locked 2026-10-19,
  superseding "one line"):
  - "Then is frozen: the {90} days to {2 Oct 2026}, exactly as captured on that day."
  - "Now is the {90} days up to {30 Oct 2026}, read when this PDF was produced."
  - "Each line compares Now with Then. ✔ Improved and ✘ Got worse say which way it moved (green better, red worse);
    — means there is no value, and the reason is printed beside it."
  The braces are values from the report; the lines name no renamable term.
- Shown in the PDF only; the screen is unchanged.

## OUT

A legend per panel · explanations for report types that do not exist yet · a confidentiality line (D13).

## Learning hypothesis

**Disproves "three short lines explain the report to a cold reader"** if someone who has never opened Lighthouse, handed
the PDF by the maintainer, cannot say which side is frozen and whether {Cycle Time} got better (K3: target ≥ 2 of
3 people).

## Data and dogfood moment

- Dev instance (`:5169`): export the busiest Team's report; the maintainer hands it to up to three people who do not
  use Lighthouse and logs their answers (K3).
- An instance with renamed terms (e.g. Cycle Time → "Lead Time", Work Items → "Tickets") checked once: the lines
  still read true and the panels below say "Lead Time".
- E2E: none added.

## Acceptance criteria

AC-3.1 … AC-3.4 in `feature-delta.md` (US-03).

## Dependencies

Slice 01 (the PDF header it sits under).
