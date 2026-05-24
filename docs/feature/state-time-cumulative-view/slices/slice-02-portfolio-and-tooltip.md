# Slice 02 — Portfolio-scope parity + tooltip attribution-count enrichment

**Feature**: `state-time-cumulative-view` (Epic 4144 MVP-bundle, slice B3)
**Stories**: US-02, US-03
**Effort estimate**: 1 day (both stories piggyback on slice 01's chart + endpoint plumbing)
**Reference class**: sibling F slice-02 (`aging-pace-percentiles/slice-02-legend-tooltip-portfolio.md`) — portfolio-scope parity + tooltip enrichment landing together. Same shape applies here.

## Goal (one sentence)

Ship the two value-bearing extensions on top of slice 01's walking skeleton: (a) the portfolio-scope endpoint and chart-widget registration so the same chart works at portfolio detail, and (b) the tooltip's `Window attribution: X partial, Y full` line so users can immediately see how many items contributed partial vs full-window time without leaving the chart.

## IN scope

- New endpoint `GET /api/portfolios/{portfolioId}/metrics/cumulativeStateTime?startDate&endDate` (shape-identical to the team endpoint from slice 01)
- New service method `PortfolioMetricsService.GetCumulativeStateTimeForPortfolio(portfolio, startDate, endDate)` (mirrors the team-scope method)
- `widgetInfoMetadata.ts` and `categoryMetadata.ts` already include the widget from slice 01; the portfolio dispatch in `BaseMetricsView.tsx` picks it up automatically because the widget has no `ownerFilter`
- Backend response payload extended with `partialWindowItemCount` and `fullWindowItemCount` per state (these were specified in slice 01's endpoint contract; this slice WIRES them through the chart tooltip)
- Chart tooltip extended with the `Window attribution: X partial, Y full` line (US-03)
- ARIA label on the tooltip includes the attribution counts in plain language (US-03 AC)
- Integration test for the portfolio endpoint asserting shape parity with the team endpoint on a comparable fixture
- Vitest test asserting the tooltip's attribution-line renders correctly and is screen-reader accessible

## OUT scope (post-MVP follow-ups, not this slice)

- Total/mean per-item toggle (D4 — deferred to follow-up)
- Per-state drill-down (feature B2, post-MVP)
- Configurable bar ordering (D3 picks workflow order; reconsider only on telemetry signal)
- Shared per-state aggregation service with sibling F (D10 — DESIGN-time decision, may land in DESIGN of either feature independently)

## Learning hypothesis

- **Disproves if it fails**: that the team-scope service method generalises cleanly to portfolio scope without leaking team-specific assumptions (e.g. about which transitions to consider, which workflow definition to walk). If portfolio scope surfaces a structural mismatch, it may indicate D2's "same widget at both scopes" decision needs revisiting.
- **Confirms if it succeeds**: that the slice-01 chart + endpoint contract was correctly designed to be scope-agnostic; AND that the tooltip's attribution-count line resolves the date-range-semantics confusion that sophisticated users were predicted to raise (US-03's elevator pitch motivation).

## Acceptance criteria

- AC items from US-02 and US-03 in `feature-delta.md` apply unchanged.
- Integration test: portfolio fixture with items mirroring the team fixture; assert endpoint returns shape-identical response with scope-appropriate values.
- Vitest test: tooltip renders the `Window attribution: X partial, Y full` line with correct counts derived from the fixture response; screen-reader ARIA-label includes the same information.
- No regression in slice-01 acceptance: team-scope chart, RAG rule, and empty/zero-state behavior unchanged.
- `pnpm build` clean; `dotnet build` zero warnings; SonarCloud quality gate passes on PR; mutation testing ≥80% kill rate on new code (both slices combined).

## Dependencies

- **HARD**: slice 01 of THIS feature merged (provides the chart widget, the metric service base, and the widget-metadata registration this slice extends).
- **HARD**: same sibling-1 dependency as slice 01 — `WorkItemStateTransition` data accumulated for at least one portfolio's items.

## Production data requirement

Dogfood the portfolio-scope chart against the dev Lighthouse instance's most-populated portfolio. Acceptance: screenshot + one-line caption in PR description, same pattern as slice 01's dogfood moment.

## Dogfood moment (same-day)

After merge, take a screenshot of BOTH the team-scope chart (slice 01 confirmation that the tooltip enrichment doesn't regress) AND the portfolio-scope chart (slice 02 confirmation), paste both into the PR description. The two screenshots side-by-side validate D2's "same widget, both scopes, identical experience" claim against real data.

## Pre-slice SPIKE

NONE. Slice 01's reference class plus the explicit "ship by mirroring the team-scope method" pattern (well-trod in the existing codebase — see `PortfolioMetricsService` vs `TeamMetricsService`) cover all the uncertainty.
