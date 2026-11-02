# Slice 01 — Pin today's numbers per unit

Epic #6474 · invisible (test-only) · `job_id: infrastructure-only` · ≤ 1 day · depends on: nothing

`infrastructure_rationale`: every later slice re-shapes how Teams and Portfolios are stored and refreshed. D12
promises that nobody's throughput or forecasts change. This slice turns that promise into a check that runs on
every slice, before anything moves. Prepares `job-forecaster-know-when-each-promise-level-lands` (D8).

## Problem

Jonas Weber forecasts "Checkout Revamp" from four Teams' throughput. If the restructuring quietly drops a type
or a state mapping, his 85% date moves and nobody notices until a steering meeting. Today no test compares a
whole unit's numbers before and after a structural change.

## Learning hypothesis

The demo data plus one upgrade fixture capture every unit's numbers deterministically. **Disproved if** two runs
on unchanged code differ (then the harness pins the randomness or the clock first, and says which).

## IN

- A characterisation fixture per unit: item count by type, 90-day throughput series, Work Item Age set, and for
  every Feature its When-forecast percentiles (50/70/85/95) on pinned draws.
- Two inputs: the seeded demo data, and an upgrade fixture shaped like real installs: Jira Teams (Story, Bug),
  an Azure DevOps Team (User Story, Bug), a Linear Team (no types), a ServiceNow Team, a CSV Team, a Portfolio
  reading Epic and Change (Customer-A-shaped, D10), and custom Terminology ("Squad", "Initiative").
- Runs in the normal backend suite (fast: no live connectors).

## OUT

- Any production change. Live-connector categories. Performance numbers.

## Domain examples

1. Demo "Ocean Explorer" Portfolio: pinned percentiles for each Feature; re-run on unchanged code: identical.
2. Upgrade fixture, Linear Team "Mobile Squad" with no item types: pinned throughput 14 items in 90 days.
3. Fixture Portfolio "Checkout Revamp" reading Epic and Change: 6 Features, 2 using the default-size guess.

## UAT scenarios

### Scenario: Today's numbers are recorded for every unit

Given the upgrade fixture with seven Teams and two Portfolios
When the characterisation runs
Then a record exists for every Team and every Portfolio

### Scenario: The record is stable on unchanged code

Given the record from a first run
When the characterisation runs again on the same code
Then every unit's numbers equal the record

### Scenario: A change that moves a number is caught

Given the record
When "Payments Crew" stops reading Bug
Then the check fails naming "Payments Crew" and the throughput difference

## Acceptance criteria

- [ ] Every Team and Portfolio of both inputs has a record.
- [ ] Two runs on unchanged code agree.
- [ ] A deliberate type change fails the check, naming the unit and the number.

## Outcome KPI

OUT-1 (its measuring instrument).

## Technical notes

- Pinned randomness: `SeededRandomNumberService` + `DrawsFromAPinnedStartingNumber` (ci-learnings, Monte Carlo).
- Build the host once per fixture (`TestHostPerFixtureGuardTest`).
- Not an E2E test (memory: E2E is a thin sanity check).
