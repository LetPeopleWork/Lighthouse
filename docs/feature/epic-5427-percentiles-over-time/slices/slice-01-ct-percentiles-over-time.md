# Slice 01 — Cycle Time percentiles over time (walking skeleton)

**Goal**: A flow coach opens a new "Percentiles Over Time" widget and sees CT percentiles (50/70/85/95)
trending day by day for a chosen 30/60/90 horizon, fed by a forward-only daily snapshot recorded on refresh.

**Stories**: US-01 (value) + US-02 (`@infrastructure`, lands within this slice).

## IN scope
- New forward-only snapshot entity + DbSet + expand-only EF migration (via `CreateMigration`).
- Metrics-refresh domain-event handler that records CT percentiles for horizons 30/60/90, latest-write-wins per day.
- HTTP series endpoint (team + portfolio) returning CT-percentile-by-horizon daily series.
- Combined "Percentiles Over Time" widget in Predictability category with `[ CT-30 | CT-60 | CT-90 ]` toggle,
  50/70/85/95 red→green lines, honest empty state.

## OUT of scope
- WIA tab (slice 02), PBC widget (slice 03/04), demo backfill (finalization/DELIVER), export.

## Learning hypothesis
**Disproves** "the `DeliveryMetricSnapshot` forward-only pattern generalizes cheaply to percentile series
with a horizon dimension" **if** recording the 3-horizon CT set per day, latest-write-wins, or serving it
back as a chartable series proves to need a materially different persistence/handler shape.
**Confirms** the whole backbone (record → serve → render → read trend) on the hardest metric family first.

## Acceptance criteria
See US-01 AC1–AC5 and US-02 AC1–AC5 in `feature-delta.md`.

## Dependencies
- Epic 5121 domain-event bus (recording trigger).
- Existing CT-percentile computation (`percentiles` widget backend).

## Effort / reference class
≤1 day. Reference class: `DeliveryMetricSnapshot` (Epic 3993) + `BlockedCountSnapshot` (Epic 5074) —
both a snapshot entity + event recorder + series endpoint + over-time chart, already shipped.

## Pre-slice SPIKE
Optional: confirm the refresh event fires at a point where CT-per-horizon percentiles are already computed
(avoid recomputing in the handler). Low risk — computation already exists.

## Dogfood moment
Enable on a real Lighthouse team the same day; after two refreshes confirm exactly one row/day/horizon and
the widget renders (single-point chart is acceptable day one).
