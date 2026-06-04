# Slice 02 — Target-change dot markers on the "How Likely?" view

**Story:** US-02 (ADO #5175) | **job_id:** `job-honest-delivery-trend-when-target-moves` |
**Estimate:** ~0.5 day | **Reference class:** the existing marks-only / `showMark` series usage in
the delivery charts (same LineChart primitive).

## Goal
Mark each target change with an emphasized dot on the How Likely? likelihood line (neutral date-pair
on hover), so a likelihood jump is attributable to a replan, not read as progress.

## IN scope
- Pure FE helper `targetChanges(points)` in `models/Delivery/deliveryTargetHistory.ts` (null-safe;
  shared with Slice 1's `steppedTargetData`), unit-tested in isolation.
- A marks-only overlay series on the How Likely? view: non-null only at change snapshots, value =
  the likelihood there (so the dot sits on the line); emphasized mark.
- Neutral date-pair (`Target moved: {old} → {new}`, D4) in the dot's hover/tooltip.

## OUT of scope
- When? view markers — the Slice-1 step line already shows the change (no duplicate marker).
- Burnup (dropped). Fever-chart annotation (dropped — no clean time axis). Editorial replan copy.

## Learning hypothesis
Disproves "a date-pair dot is enough for a forecaster to attribute the jump to the replan" if dogfood
reviewers still misread the step as progress with the dot present.

## Acceptance criteria
US-02 AC1–AC4 (feature-delta). Key: N changes → N dots; zero changes → no change-dot; When? carries
no separate marker (its step line is the marker).

## Dependencies
Slice 1 (`targetDateAtSnapshot` on the history points + the shared `deliveryTargetHistory.ts` helper).

## Dogfood moment
On the replanned demo delivery, confirm one dot at the recorded change on the How Likely? line,
hover shows both dates.
