# Slice 04 — PBC over time: remaining metric-type toggles

**Goal**: The "PBC Over Time" widget toggles across all PBC metric types, each showing its own
UNPL/Average/LNPL over time.

**Stories**: US-05 (value).

## IN scope
- NPL daily snapshots for WIA, WIP, Cycle Time, Arrivals, and Feature Size (portfolio-only), via the US-02 pipeline.
- Type toggle exposing all six types; Feature Size stays `portfolio-only`.

## OUT of scope
- Percentile widgets. Combining PBC types into one chart (one type shown at a time). Alerting.

## Learning hypothesis
**Disproves** "adding PBC types is pure configuration over the slice-03 shell" **if** any type needs a
bespoke recorder or breaks the Throughput regression.
**Confirms** the PBC-over-time widget scales to the full type set by data, not new code paths.

## Acceptance criteria
See US-05 AC1–AC3 in `feature-delta.md`.

## Dependencies
- Slice 03 (PBC-over-time widget shell + NPL pipeline). Existing per-type PBC computations.

## Effort / reference class
≤1 day (data + toggle options, no new chart type). Reference class: slice 03.

## Dogfood moment
Toggle each type on a real portfolio; confirm Feature Size only appears at portfolio scope and Throughput
behaviour is unchanged.
