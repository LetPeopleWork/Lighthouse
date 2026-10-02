# Slice 09 — Settings-changed notice

**Feature**: epic-5878-baseline · **Epic**: #5878 · **Story**: US-09 (ADO #6167) · **Estimate**: ~1d · **Tier**: Community ·
`job_id: job-flow-coach-show-whether-flow-changed`

## Goal

When Now is computed under settings that differ from those frozen with Then, the report says so once, in the header.

## IN

- Compare today's settings with the snapshot stored since slice 01: {Work Item} types, To Do / Doing / Done mapping,
  the query, blackout days (D31).
- One header notice naming what changed: "Settings changed since this report was created (state mapping). Then
  stays as frozen on 14 Sep 2026; Now uses today's settings."
- No notice when nothing that shapes the claims changed.

## OUT

Recomputing Then under today's settings (never), a per-row notice, offering to rebuild the report.

## Learning hypothesis

**This disproves "users understand a frozen Then under changed settings"** if the dogfood coach deletes and rebuilds
the report after seeing the notice instead of reading on. Then the wording, not the mechanism, needs work.

## Data and dogfood moment

- Demo: add a Doing state to Team Gravity after creating a report.
- Dogfood: change a mapping on a dev-instance Team that has a week-old report.

## Acceptance criteria

AC-9.1 … AC-9.3 in `feature-delta.md` (US-09).

## Dependencies

Slice 01 (the snapshot is stored from the first report on).
