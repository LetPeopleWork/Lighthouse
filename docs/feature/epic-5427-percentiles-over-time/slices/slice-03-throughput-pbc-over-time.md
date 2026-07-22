# Slice 03 — Throughput PBC natural process limits over time

**Goal**: A delivery lead opens a new "PBC Over Time" widget (Throughput selected) and sees
UNPL / Average / LNPL trending day by day.

**Stories**: US-04 (value).

## IN scope
- PBC-NPL daily snapshot for **Throughput** recorded through the US-02 pipeline.
- New "PBC Over Time" widget in Predictability with a metric-type toggle (Throughput active only).
- UNPL/Average/LNPL three-line rendering matching existing PBC styling; honest empty state.

## OUT of scope
- Remaining PBC types (slice 04). Percentile widgets (slices 01/02). Special-cause overlays.

## Learning hypothesis
**Disproves** "the percentile-series pipeline extends to PBC NPL triples (UNPL/Avg/LNPL)" **if** NPL
recording/serving needs a structurally different shape than the percentile series.
**Confirms** one over-time infrastructure covers both percentile lines and PBC limits.

## Acceptance criteria
See US-04 AC1–AC3 in `feature-delta.md`.

## Dependencies
- Slice 01 (pipeline + series contract). Existing Throughput PBC computation (`throughputPbc`).

## Effort / reference class
≤1 day (new widget shell + NPL series). Reference class: slice 01 widget + `ProcessBehaviourChart` model.

## Dogfood moment
On a real team/portfolio, confirm the Throughput NPL lines render and move only when the underlying
limits recompute — a stable process shows flat limit lines.
