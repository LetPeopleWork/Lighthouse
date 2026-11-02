# Slice 12 — Every Team and Portfolio refreshes to the same numbers through one refresh

Epic #6474 · invisible · `job_id: infrastructure-only` · ≤ 1 day (**may split at DESIGN**: 12a leaf steps,
12b Feature-level steps) · depends on: 07, 10

`infrastructure_rationale`: `TeamUpdater` and `PortfolioUpdater` are two pipelines for two kinds. With one unit
type, one refresh runs per unit and picks its steps by level (leaf: Work Items and throughput; Feature level:
Features, roll-up, then forecasts). #6455 then adds level-3 steps instead of a third updater. Prepares
`job-forecaster-know-when-each-promise-level-lands`.

## Problem

Jonas Weber's "Checkout Revamp" forecasts depend on two refresh pipelines staying in step. Every refresh change
today is made twice, once per updater, and a fix in one is easily missed in the other; the Portfolio refresh
also triggers the forecast directly instead of through an event (ADR-270 ratchet candidate 4).

## Learning hypothesis

One refresh with level-chosen steps reproduces both pipelines exactly. **Disproved if** the pinned numbers or
the order of published events differ (then the steps stay two pipelines behind one entry point).

## IN

- One refresh entry per unit; steps by leaf or Feature level; the update queue keys stay as they are.
- Published events keep today's names (`TeamDataRefreshed`, `PortfolioFeaturesRefreshed`,
  `PortfolioForecastsUpdated`) unless DESIGN replaces them with one `UnitDataRefreshed` and moves every handler.
- The forecast after a Feature-level refresh is triggered by the refresh event, not a direct call (if cheap;
  else recorded as left).

## OUT

- Moving the update queue (ADR-270 left open). New refresh behaviour.

## Domain examples

1. "Payments Crew" refreshes: same Work Items, same throughput, `TeamDataRefreshed` once.
2. "Checkout Revamp" refreshes: same Features, roll-up, forecasts; events in the same order.
3. A forced refresh and a scheduled one for the same Team coalesce as today.

## UAT scenarios

### Scenario: A Team's refresh reads the same work

Given the pinned numbers for "Payments Crew"
When it refreshes through the one refresh
Then its numbers equal the pinned numbers

### Scenario: A Portfolio's refresh ends in the same forecasts

Given the pinned forecasts for "Checkout Revamp"
When it refreshes through the one refresh
Then its forecasts equal the pinned ones

### Scenario: Two refresh requests for one unit still run once more, not twice

Given a refresh of "Search" is running
When another is requested
Then one more refresh runs after it, as today

## Acceptance criteria

- [ ] Teams and Portfolios refresh to the same numbers, forecasts and Delivery likelihoods as before.
- [ ] Everything that reacts to a refresh today still reacts, in the same order.
- [ ] A second request during a refresh still leads to exactly one more refresh.
- [ ] Slice 01 check green.

## Outcome KPI

OUT-1, OUT-4.

## Technical notes

- ci-learnings: a dedup queue must coalesce, not drop; no whole-table rebuilds under concurrency.
- Run the demo-data E2E; only it sees some refresh races.
