# Slice 00: A Team never shows a Portfolio's numbers

**Goal:** the metrics cache keeps a Team's entries and a Portfolio's entries apart, so a Team and a Portfolio with
the same id never serve each other's cached metrics.

## IN scope
- A failing reproduction test first: cache a metric for Team N, ask Portfolio N for the same metric and window, and
  get the Team's value. `ThroughputInfo` is a good candidate.
- The cache key and the invalidation prefix name the owner kind (`typeof(TEntity).Name`: Team or Portfolio) as well
  as the id. One function (`MetricsCacheKey.For<TEntity>(TEntity owner, string metricIdentifier)` / `PrefixFor<TEntity>(TEntity owner)`, kind from `typeof(TEntity).Name`, replacing the DISTILL scaffold in this slice; amended: generic signature, review M-R3-1, 2026-10-13) builds both, so a key and the
  prefix that clears it cannot disagree (DDD-6). (amended: derivation decided, 2026-10-13)
- Every one of the 20 colliding key families listed under "The colliding cache keys" in `../feature-delta.md` is
  covered by the change. The change is in the key builder, not at each call site.
- Invalidation still reaches its own owner's entries from every caller: `InvalidateTeamMetrics`,
  `InvalidatePortfolioMetrics`, and the handlers and fill target that call them.

## OUT of scope
- Any change to what a metric computes, how long it is cached, or when it is invalidated.
- The Feature Size routes (slices 01 and 02). They are not cached at all (DDD-5).
- The Jira connector's entries in the same cache, which already use their own namespace.

## Learning hypothesis
- **Disproves** "the collision is live" if the reproduction test cannot make a Team and a Portfolio share an entry,
  for example because ids never overlap in practice. In that case the slice shrinks to a guard test only.
- **Confirms** it if the test returns the other owner's value, as the key format predicts.
- **Result at DISTILL: confirmed.** Gravity and Ocean Explorer (both id 1) were served each other's Total Throughput
  (3 against 5) and Cycle Time 50th (2 against 21). See `../red-classification.md`. (added 2026-10-13)

## Acceptance criteria
AC-0.1 … AC-0.4 in `../feature-delta.md` (US-00, now in the DISCUSS User Stories). AC-0.3 is an outcome: no figure
crosses owners for any metric in the colliding list. That keys never overlap is a unit-test detail.
(amended: moved and AC-0.3 reworded, 2026-10-13)

## Dependencies
None. Ordered ahead of slice 01 and releasable alone. Slice 01 does not depend on it.

## Effort
About 2–3 hours: the reproduction test, the key builder, a key-uniqueness test and an invalidation test.
Reference class: a focused bug fix in `BaseMetricsService`.

## Dogfood
Teams and Portfolios are numbered in separate tables, so demo data should hold a Team and a Portfolio with the same
id (check the ids first). Open that Team's Flow Overview, then the Portfolio's, within one refresh interval. Before the fix, cards such as Total Throughput can show the same
numbers on both. After it, each shows its own.
