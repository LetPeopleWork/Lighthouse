# Slice 05 — Fever chart (STRETCH)

**Feature**: `delivery-metrics` (Epic 3993 Delivery Metrics)
**Stories**: US-05 (STRETCH — gated behind Slices 2-4 shipping)
**Effort estimate**: 2-3 days (animated bubble visualization is the largest UI unknown in the epic)
**Reference class**: NONE in the existing Lighthouse chart inventory — this is a net-new visualization idiom (Tendon / TameFlow fever chart). Highest UI uncertainty; lowest priority.

## STRETCH gate

This slice is **explicitly a stretch goal**. It does not ship unless Slices 2-4 have shipped and the unified snapshot store (Slice 1) is proven. If the forward-fed cadence/quality (Slice 2 inferred-estimate learning) or the trend reads (Slices 3-4 learning) disappoint, this slice is dropped, not forced. It is OUT of the committed MVP.

## Goal (one sentence)

Render a TameFlow-style fever chart — an animated bubble that plots the delivery's buffer-consumption against schedule progress, moving through green/amber/red zones over the accumulated snapshots — giving leadership a single at-a-glance "how worried should we be?" signal per delivery (ref the epic's wayback link).

## IN scope (if reached)

- A fever-chart widget reading `DeliveryMetricSnapshot` history: x = schedule/progress consumed, y = buffer consumed, bubble moving through zones over snapshot time.
- Zone thresholds (green/amber/red) derived from delivery target date and remaining work; defaults baselined, tunability out of scope.
- Animation of the bubble's path across the recorded snapshots (the "fever" trail).
- Empty/sparse-data handling (no bubble until enough snapshots).

## OUT scope

- Configurable zone thresholds — baseline defaults only.
- Multi-delivery fever board (all deliveries on one chart) — future.
- Any retroactive bubble history (impossible — the forward-only rationale, D6/D11).

## Learning hypothesis

- **Disproves if it fails**: that the fever-chart idiom adds decision value beyond the Slice-3 on-track read and the Slice-4 predictability trend. If leadership finds the bubble decorative rather than decision-driving, the stretch was correctly deprioritized and stays dropped.
- **Confirms if it succeeds**: that a single buffer-vs-schedule fever signal is the leadership-glance artifact that the line charts (Slices 3-4) inform but don't replace.

## Acceptance criteria

- US-05 AC items from `feature-delta.md` apply unchanged.
- Integration test: with known snapshots the buffer/schedule coordinates per date are computed correctly; an on-track delivery traces through green, an at-risk one into red.
- Vitest + RTL: the bubble renders at the correct zone for a given fixture; the trail animates across snapshot dates; empty-state renders with no snapshots.
- `pnpm build` clean; `dotnet build` zero warnings; SonarCloud gate passes; mutation ≥80% on new code.

## Dependencies

- **HARD**: Slices 2, 3, AND 4 shipped (unified snapshot store from Slice 1 proven + forward-fed trend reads validated).
- **HARD**: the stretch gate above — explicit go decision after Slice 4.

## Production data requirement

Requires substantial accumulated snapshot history (weeks) for a meaningful fever trail. Dogfood only if the slice is greenlit post-Slice-4.

## Cross-cutting (DoR item 7)

- **RBAC**: read view; existing portfolio read path; `useRbac()` gating; `IRbacAdministrationService`. No new write surface. No `/my-summary` fetch.
- **Lighthouse-Clients**: N/A — a visualization-only slice over Slice 3's snapshot data; no new contract for clients to follow unless a fever-status endpoint is added, in which case version-gate it.
- **Website**: OPTIONAL marketing flourish under the delivery-tracking launch; decide at greenlight.

## Pre-slice SPIKE

REQUIRED (~half day) if greenlit: prototype the bubble/zone rendering against real snapshot data to de-risk the net-new animated visualization before committing the full slice. This is the only slice in the epic with no internal reference class.
