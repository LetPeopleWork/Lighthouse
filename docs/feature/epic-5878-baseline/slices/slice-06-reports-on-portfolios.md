# Slice 06 — Reports on Portfolios

**Feature**: epic-5878-baseline · **Epic**: #5878 · **Story**: US-06 (ADO #6164) · **Estimate**: ~1d · **Tier**: Community ·
`job_id: job-flow-coach-show-whether-flow-changed`

## Goal

Portfolio Admins create Then & Now reports on a Portfolio, with the same four panels on {Feature}-level series.

> Revised 2026-10-03: panels instead of claims (D39); Portfolio presets 90 / 180 days plus custom (D37, D43).

## IN

- Reports tab on Portfolio detail after Metrics (D18); Portfolio Write to create (`useRbac().isPortfolioAdmin`),
  Portfolio Read to view; Portfolio empty state.
- Same catalog and panels on the Portfolio series (`PortfolioMetricsService` PBCs and percentiles), including the
  points beyond Then's frozen limits.
- Then and Now lengths: presets 90 / 180 days plus a custom number of days, at least 14 (D37, D43).
- Words via Terminology: {Feature(s)}, {Portfolio}.
- Route key `PortfolioDetail_Reports` on `PortfolioTabOpened` (DEVOPS confirms).

## OUT

Portfolio-only metrics such as {Feature} size (nice-to-have), cross-owner reports.

## Learning hypothesis

**This disproves "panels say something at Portfolio level"** if every demo and dev-instance Portfolio shows "—" on
{Cycle Time} and 0 · 0 beyond Then's limits on the other panels. Then Portfolio reports need other metrics before
they are worth promoting.

## Data and dogfood moment

- Demo: Project Apollo / Orion have 7–10 finished {Features} over ≈85 days (S11), so the {Cycle Time} panel shows
  "—" with the reason — the honest too-little-data path, on purpose.
- Dogfood: the dev instance's largest Portfolio, 180-day Then.

## Acceptance criteria

AC-6.1 … AC-6.4 in `feature-delta.md` (US-06).

## Dependencies

Slice 05 (catalog complete).
