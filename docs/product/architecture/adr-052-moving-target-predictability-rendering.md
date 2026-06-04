# ADR-052: Moving-Target Rendering on the Delivery Predictability Charts

**Status**: Proposed (2026-06-04 — Morgan, interaction mode PROPOSE; user-steered)
**Date**: 2026-06-04
**Feature**: delivery-target-date-tracking (Epic 3993 follow-up)
**Decider**: Morgan (Solution Architect), rendering choices set by the user (2026-06-04)
**Relates to**: ADR-051 (the `targetDateAtSnapshot` data it renders), ADR-050 (metrics-history FE
contract). Supersedes the DISCUSS US-03 burnup treatment and the "vertical markers on both
predictability views + fever trail" treatment.

ADO: https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/3993 (stories #5174, #5175;
#5176 Removed)

---

## Context

With `targetDateAtSnapshot` available per point (ADR-051), the predictability charts can show a moved
target honestly. Three surfaces were candidates: the burnup, the predictability **When?** view
(y-axis = forecast completion date — a time scale), and the predictability **How Likely?** view
(y-axis = likelihood %). The DISCUSS draft proposed vertical change-markers on both predictability
views, a fever-trail annotation, and a burnup "replanning history" (whose rendering — a date on a
count-axis chart — was the open question D8).

User direction (2026-06-04): *"In the burnup it's not needed — we don't need to see the delivery date
there. We should have some markers (like dots?) in the How Likely, and just a step line in the When.
No need to add anything in the delivery progress."*

## Decision

Render the moving target in the idiom that fits each chart's axes; leave the burnup alone.

1. **When? view — stepped target series (replaces the flat reference line).** The When? y-axis is
   already a time scale (forecast completion date). Replace the single flat
   `ChartsReferenceLine y={deliveryDate}` with a `LineChart` series whose data is each point's
   `targetDateAtSnapshot` rendered with `curve:"stepAfter"`, on the same y-axis as the forecast
   percentiles. The step itself shows where and to what the target moved, so **no** separate change
   marker is drawn on this view. **Fallback**: when every point's `targetDateAtSnapshot` is `null`
   (pre-migration history), keep the existing flat `ChartsReferenceLine y={deliveryDate}` (current
   behaviour, no crash).

2. **How Likely? view — change-dot overlay.** The y-axis is a percentage, so a date can't be plotted
   there. At each snapshot where `targetDateAtSnapshot` changed from the previous point, draw an
   emphasized dot **on the likelihood line** (a marks-only overlay series whose value equals the
   likelihood at that point, non-null only at change indices). The neutral date-pair
   (`Target moved: {old} → {new}`, D4) is shown in the dot's hover/tooltip — no chart-face text
   clutter. **Fallback**: no changes (or all-null) → no change dots; the line's normal marks are
   unaffected.

3. **Derivation in a pure helper.** A new `models/Delivery/deliveryTargetHistory.ts` exposes
   `targetChanges(points)` (→ change indices with old/new dates) and `steppedTargetData(points)`
   (→ the step series data, or `null` signalling the flat fallback). No React, no chart imports;
   unit-tested in isolation. The chart components consume these — keeping projection logic out of the
   presentational component (the UI-1 testability lesson from delivery-metrics).

4. **Burnup — untouched.** No target marker. The existing `ChartsReferenceLine x={deliveryDate}` in
   `DeliveryBurnupChart.tsx` renders off the right edge (the target date is beyond the snapshot data
   range) and is effectively invisible; flagged as **optional** dead-code removal, not committed
   scope.

## Alternatives Considered

### Vertical change-markers on BOTH predictability views (DISCUSS draft)
- **Rejected**: double-marks the same change — the When? step line already shows it. One idiom per
  axis (step where y is a date, dot where y is a percentage) is cleaner and avoids redundancy.

### Burnup replanning-history (faded prior lines / dual-axis step line / reuse markers — D8 options)
- **Rejected by the user**: the delivery date is not wanted on the burnup. Removes the only real viz
  uncertainty (the dual-axis option) and a whole slice.

### Fever-trail annotation (DISCUSS in-scope-stretch)
- **Rejected**: the fever chart's axes are completion-rate × chance-of-being-late, with no time axis
  to place a change marker cleanly; not requested. Can be revisited as a follow-up if wanted.

### Vertical `ChartsReferenceLine` instead of a dot on the How Likely? view
- **Rejected by the user** in favour of dots: a dot sits on the data and reads as "this point is the
  replan" without a full-height line dividing the chart.

## Consequences

**Positive**: stock MUI-X features only (`curve:"stepAfter"`, a marks-only series) — no new chart, no
new dependency; each view marks the replan in its natural idiom; pure-helper derivation is cheaply
and thoroughly unit-testable; two slices instead of three.

**Negative**: the How Likely? change is carried by a hover tooltip, so the date-pair isn't visible
without interaction (accepted — keeps the chart face clean; the dot itself signals "something changed
here"). The two views express the same change differently (step vs dot) — intentional, matched to
their axes.

## Earned Trust — probing the contract boundary

- **Step-fallback probe**: a Vitest test asserts the When? view renders the stepped series for mixed
  targets and the flat `ChartsReferenceLine` when all `targetDateAtSnapshot` are null.
- **Change-derivation probe**: unit tests on `targetChanges` — N changes → N indices, zero changes →
  empty, nulls handled — independent of the chart (DDD-4).
- **Dot-placement probe**: a Vitest test asserts a change dot appears at the changed snapshot on the
  How Likely? line and none when the target is constant.
- **MUI-X selector caution**: the step series and overlay dots use the same `data-series`-keyed
  styling that bit the delivery-metrics dashed estimated line (MUI-X 9.0.1) — run the live chart;
  the type-check does not validate selector/series wiring.
