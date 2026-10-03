# Slice 07 — Set Now's length at creation; days since Then ended

**Feature**: epic-5878-baseline · **Epic**: #5878 · **Story**: US-07 (ADO #6165) · **Estimate**: ~½d · **Tier**: Community ·
`job_id: job-flow-coach-show-whether-flow-changed`

> Re-cut 2026-10-03: was "Choose the Now window; days since Then ended", with a per-view picker in the page address.
> The maintainer decided Now's length is saved on the report and only editors change it (D43), and Now may overlap
> Then (D38). The file name keeps its old slug so existing links still resolve.

## Goal

The coach sets how long Now is when creating the report; it is saved for every reader. The header shows both windows
and how long ago Then ended.

## IN

- Now length in Create Report: default = Then length; Team 30 / 90, Portfolio 90 / 180 days, or a custom number of
  days, at least 14 and inside `DoneItemsCutoffDays` (D43). Saved on the report (stored since slice 01 as the Then
  length); not in the page address.
- No Now control for anyone viewing; editors change it later through Edit report (slice 10).
- Now is the full rolling window ending today, never clipped, even where it overlaps Then (D38).
- Header: "Then: 90 days to 31 Jul 2026 — frozen · Now: last 90 days, 5 Jul – 2 Oct 2026 · 63 days since Then ended"
  (D35).

## OUT

A "since Then ended" Now as its own option (would reopen D11); a per-view or per-reader Now (D43); changing Now after
creation (10).

## Learning hypothesis

**This disproves "a saved rolling Now answers the 4-week shape"** (D14) if Dev Malhotra's assessment case, or a
dogfood coach, still asks for a window "since the start". Then D11 is reopened with the maintainer.

## Data and dogfood moment

- Demo: Team Lightspeed report with Then 30 days and Now 14 days; read the header.
- Dogfood: ask consultant #2 (interview protocol Q6) which Now length he would save at the end of his 4 weeks.

## Acceptance criteria

AC-7.1 … AC-7.4 in `feature-delta.md` (US-07).

## Dependencies

Slice 01 (the Now length is stored from the first report on).
