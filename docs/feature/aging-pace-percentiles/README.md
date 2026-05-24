# Feature: aging-pace-percentiles — Initial Inputs

**Parent Epic**: #4144 (More Detailed State Info) — `docs/feature/epic-4144-more-detailed-state-info/README.md`
**ADO Epic**: https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/4144
**Status**: DISCUSS complete (2026-05-24). This stub is preserved as the origin record; the authoritative artifact is now `feature-delta.md`.
**MVP membership**: **Yes** — required for the Epic 4144 MVP release bundle alongside `time-in-state-and-staleness` and `state-time-cumulative-view`.
**Origin**: Promoted out of Epic #4144 slice F. Documented in the Epic catalog as "Pace-percentiles bands on Work Item Aging chart (ActionableAgile-style)".

> **Premise correction surfaced during DISCUSS (2026-05-24)**: the "Strategic framing" below overstated the gap. Lighthouse's Work Item Aging chart already renders end-to-end 50/70/85/95 cycle-time percentile bands as dashed lines (verified in `Lighthouse.Frontend/src/components/Common/Charts/WorkItemAgingChart.tsx:349-364` and `Lighthouse.Backend/Services/Implementation/TeamMetricsService.cs:303-318`). The ActionableAgile-parity differentiator is **per-state granularity** — bands showing how long items historically spent in *each* workflow state, not the end-to-end total. The feature has been reframed accordingly. See `feature-delta.md` for the corrected JTBD, AC, and the locked decision matrix.

This file captures what we figured out while scoping Epic #4144 — preserved for traceability. Read `feature-delta.md` for the current scope.

## Strategic framing (PRE-DISCUSS, superseded — see correction above)

This is **the** competitive-parity story for the MVP. ActionableAgile shows pace percentiles overlaid on the aging chart; Lighthouse currently does not. Closing this gap is the explicit value prop — the JTBD lives near "show me where my in-flight items sit relative to historically-finished items, at a glance."

## Carried-over scope notes

- **Persona candidates**: `flow-coach` (primary), Product Manager (secondary). Different decision shape from `time-in-state-and-staleness`'s per-item badge — chart-glance pace recognition vs per-item triage.
- **Data foundation reused, not rebuilt**: consumes the `WorkItemStateTransition` data shipped by `time-in-state-and-staleness`. No new sync-side capture.
- **Visual reference**: ActionableAgile's Work Item Aging chart with percentile bands (50th, 70th, 85th, 95th typically). Lighthouse equivalent surface is the existing Work Item Aging chart.

## What is NOT decided yet

- Which percentiles to show by default (50/70/85/95 is the ActionableAgile convention; may or may not match Lighthouse defaults).
- Whether percentiles are user-configurable per team/portfolio or fixed.
- Distribution computation: rolling window? all-time? same as the existing throughput/cycle-time history window?
- Whether bands render as filled regions, dotted lines, or both.
- Edge cases: empty distribution (new team, no completed items), bimodal distributions, very small samples.
- KPIs / AC / slice split.

These are open and intentionally left for the eventual `/nw-discuss` run.

## Initial story shape (for ADO seeding only — refine during DISCUSS)

- **US-A** — *Overlay pace-percentile bands on the Work Item Aging chart*: as a flow-coach, I want pace-percentile bands (e.g. 50th / 70th / 85th / 95th of historical age-at-done) overlaid on the Work Item Aging chart, so I can see at a glance which in-flight items are pacing slower than most historical items.
- **US-B** — *Configure displayed percentiles (deferred candidate)*: as a flow-coach, I want to choose which percentiles to display, so the chart matches the convention my team uses.

## Pre-requisites

- `time-in-state-and-staleness` slice 01 (data foundation) merged so `WorkItemStateTransition` exists.
- Existing Work Item Aging chart code path (verify location during DISCUSS).

## Pointers

- Parent Epic catalog: `docs/feature/epic-4144-more-detailed-state-info/README.md`
- Sibling MVP features: `docs/feature/time-in-state-and-staleness/`, `docs/feature/state-time-cumulative-view/`
