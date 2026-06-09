# Slice 03 — Portfolio scope (card + toggle)

**Type:** vertical | **Est:** ~1 day | **Stories:** US-03

## Learning hypothesis

The WIP-age signal carries to release-train (Portfolio) scope. Disproved if portfolio-level aggregation of in-progress feature/item ages produces noise rather than an actionable spread.

## What ships

- The "Work Item Age Percentiles" card on the Portfolio metrics overview, computed over the portfolio's current in-progress population.
- The CT↔WIA toggle on the Portfolio aging chart, identical semantics to Team (D2).
- Mirror the Team compute/read path into `PortfolioMetricsController` / `PortfolioMetricsService` (same pattern as the existing `cycleTimePercentiles` portfolio sibling) — new `GET /api/portfolios/{portfolioId}/metrics/workItemAgePercentiles` endpoint.
- **Lighthouse-Clients (separate repo):** version-gated wrapper for the new portfolio endpoint.

## IN scope

- Portfolio scope card + toggle.
- Empty-WIP behaviour matching Team (D6).

## OUT of scope

- Per-state WIA, premium gating, any Team-scope change.

## Production-data AC

- Given a portfolio with in-progress features/items, when the coach opens the Portfolio metrics overview, then the WIA percentile card shows the 50/70/85/95 of the portfolio's current in-progress ages.
- Given the Portfolio aging chart, when the coach flips CT↔WIA, then behaviour matches Team scope exactly.
- Given an empty portfolio WIP, the card shows the graceful empty state.

## Taste tests

- Identical-except-scale to Team slices? No — it's the portfolio aggregation path, a distinct surface with its own controller/service; not a merge candidate. PASS.
- No new abstraction first: mirrors the existing portfolio percentile pattern. PASS.
- Disproves a pre-commitment (signal survives train-scope aggregation). PASS.
- Value-bearing. PASS.
