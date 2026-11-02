# Slice 10 — Feature-level settings move to an extension

Epic #6474 · invisible · `job_id: infrastructure-only` · ≤ 1 day · depends on: 03

`infrastructure_rationale`: what a Portfolio adds is tied to being the leaf's direct parent (default-size guess,
Size Estimate / Feature Owner / Dependency fields, owning Team, forecast fingerprint). Keyed by unit id, they
stop making the unit Portfolio-shaped, so #6455's level-3 units hold only the shared part. Prepares
`job-forecaster-know-when-each-promise-level-lands`.

## Problem

Jonas Weber's "Checkout Revamp" forecasts rely on its default Feature size and owning Team. Priya's future
programme board on level 3 has no default Feature size, no owning Team and no dependency field. If
those stay on the unit, every upper unit carries dead settings and code branches on "is this a Portfolio".

## Learning hypothesis

The Feature-level settings move with an expand-only change and payloads stay identical. **Disproved if** a field
turns out to be meaningful on every non-leaf level (then it stays on the unit and the inventory says so).

## IN

- Default-size guess settings (`DefaultAmountOfWorkItemsPerFeature`, percentile switch, history days,
  percentile, override real child count states); Size Estimate, Feature Owner and Dependency fields with their
  "follows the default" sources; `IgnoreDependencies`; `OwningTeamId`; `ForecastInputFingerprint`.
- One record per Feature-level unit, keyed by unit id; owners per ADR-270 (Work Items' roll-up, Organisation
  set-up mapping, Forecasting fingerprint).
- Portfolio settings payloads unchanged.

## OUT

- Deliveries (Promises, unchanged). Refresh (12).

## Domain examples

1. "Checkout Revamp": 25 Work Items per Feature, owning Team "Payments Crew" → same after the move.
2. "Ocean Explorer" (demo) with the 85th percentile over 90 days → same default-sized Features.
3. A Portfolio following the connection's Dependency field default → still follows it.

## UAT scenarios

### Scenario: A Portfolio's sizing defaults survive the move

Given "Checkout Revamp" assumes 25 Work Items for a Feature without children
When the install is upgraded
Then its Features without children are still sized 25

### Scenario: Following the connection's default is kept

Given a Portfolio follows the connection's Dependency field
When the install is upgraded
Then it still follows that default

### Scenario: Unchanged inputs still skip the forecast

Given a Portfolio whose inputs did not change since its last forecast
When it refreshes after the upgrade
Then the forecast is not recomputed

## Acceptance criteria

- [ ] Values and sources copied for every Portfolio; payloads unchanged.
- [ ] The forecast fingerprint keeps working (only the forecast run writes it).
- [ ] Slice 01 check green.

## Outcome KPI

OUT-1, OUT-4.

## Technical notes

- The fingerprint's save behaviour (`SetAfterSaveBehavior(Ignore)`) must carry over.
- Expand-only: old columns kept, unwritten.
