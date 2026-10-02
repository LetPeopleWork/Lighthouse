# Slice 06 — Reports on Portfolios

**Feature**: epic-5878-baseline · **Epic**: #5878 · **Story**: US-06 · **Estimate**: ~1d · **Tier**: Community ·
`job_id: job-flow-coach-show-whether-flow-changed`

## Goal

Portfolio Admins create Then & Now reports on a Portfolio, with the same claims judged on {Feature}-level series.

## IN

- Reports tab on Portfolio detail after Metrics (D18); Portfolio Write to create (`useRbac().isPortfolioAdmin`),
  Portfolio Read to view; Portfolio empty state.
- Same catalog and rules on the Portfolio series (`PortfolioMetricsService` PBCs and percentiles).
- Then and Now lengths 30 / 90 / 180 days (D20, D22).
- Words via Terminology: {Feature(s)}, {Portfolio}.
- Route key `PortfolioDetail_Reports` on `PortfolioTabOpened` (DEVOPS confirms).

## OUT

Portfolio-only claims such as {Feature} size (nice-to-have), cross-owner reports.

## Learning hypothesis

**This disproves "claims say something at Portfolio level"** if every demo and dev-instance Portfolio reads Not enough
data on C1 and No change yet on C2. Then Portfolio reports need other claims before they are worth promoting.

## Data and dogfood moment

- Demo: Project Apollo / Orion have 7–10 finished {Features} over ≈85 days (S11), so C1 shows Not enough data —
  the honesty path, on purpose.
- Dogfood: the dev instance's largest Portfolio, 180-day Then.

## Acceptance criteria

AC-6.1 … AC-6.4 in `feature-delta.md` (US-06).

## Dependencies

Slices 04, 05 (catalog complete).
