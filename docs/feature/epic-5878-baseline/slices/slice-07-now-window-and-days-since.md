# Slice 07 — Choose the Now window; days since Then ended

**Feature**: epic-5878-baseline · **Epic**: #5878 · **Story**: US-07 · **Estimate**: ~½d · **Tier**: Community ·
`job_id: job-flow-coach-show-whether-flow-changed`

## Goal

Anyone viewing a report picks how long Now is, and the header says how long ago Then ended.

## IN

- Now preset picker in the report header: Team 14 / 30 / 90, Portfolio 30 / 90 / 180 days; default = Then length;
  kept in the page address like the Metrics tab, not saved on the report (D22, Q6).
- Clipping from slice 03 applies to every preset (D23); header shows the clipped window.
- Header "63 days since Then ended" (D35).

## OUT

A "since Then ended" Now window as its own option (would reopen D11), saving the choice per report.

## Learning hypothesis

**This disproves "the rolling Now answers the 4-week shape"** (D14) if consultant #2's assessment, or a dogfood
coach, still asks for a window "since the start". Then D11 is reopened with the maintainer.

## Data and dogfood moment

- Demo: Team Lightspeed report; switch 30 → 14 days and read the header.
- Dogfood: ask consultant #2 (interview protocol Q6) which Now he would show at the end of his 4 weeks.

## Acceptance criteria

AC-7.1 … AC-7.3 in `feature-delta.md` (US-07).

## Dependencies

Slice 03.
