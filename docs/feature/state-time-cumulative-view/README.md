# Feature: state-time-cumulative-view — Initial Inputs

**Parent Epic**: #4144 (More Detailed State Info) — `docs/feature/epic-4144-more-detailed-state-info/README.md`
**ADO Epic**: https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/4144
**Status**: DISCUSS complete (2026-05-24). See `feature-delta.md` for the authoritative artifact.

> **DISCUSS verification note (2026-05-24)**: a pre-DISCUSS code reality check
> (inspecting `Lighthouse.Frontend/src/components/Common/Charts/*`,
> `Lighthouse.Frontend/src/pages/Common/MetricsView/widgetInfoMetadata.ts`,
> `Lighthouse.Frontend/src/pages/Common/MetricsView/categoryMetadata.ts`, and the
> backend metrics services) **CONFIRMED** the gap claim below: no chart in
> Lighthouse today aggregates cumulative time per workflow state across items in
> a window. The existing `stacked` widget is the Simplified CFD (areas-over-time
> by `StateCategory`, not per-state cumulative); `WorkDistributionChart`
> aggregates by parent feature, not by state; `WorkItemAgingChart` is per-item.
> No premise correction was required (unlike sibling `aging-pace-percentiles`,
> whose README premise had to be reframed at DISCUSS). The DISCUSS feature-delta
> (`docs/feature/state-time-cumulative-view/feature-delta.md` D1) records the
> inspection evidence so future readers can trust the gap claim without
> repeating the inspection.
**MVP membership**: **Yes** — required for the Epic 4144 MVP release bundle alongside `time-in-state-and-staleness` and `aging-pace-percentiles`.
**Origin**: Promoted out of Epic #4144 slice B3. Documented in the Epic catalog as "Cumulative time-per-state across timeframe".

This file captures what we already figured out while scoping Epic #4144. The eventual `/nw-discuss` run will produce the full `feature-delta.md` alongside this stub.

## Strategic framing

Where the per-item badge (`time-in-state-and-staleness` US-01) answers "is THIS item stuck?", this feature answers "where does the team / portfolio spend its time across the workflow as a whole?" — a leadership / retro / improvement-prioritisation view rather than a triage view.

User-provided framing (verbatim, 2026-05-24): *"a chart that shows where items spent time (think bar charts per state — cumulative times for all items in the filter, including ongoing items)"*.

## Carried-over scope notes

- **Persona candidates**: Delivery Lead / RTE (primary, per Epic catalog), `flow-coach` (secondary). Decision shape: which workflow states are the biggest constraints; where to focus improvement work next quarter.
- **Data foundation reused, not rebuilt**: consumes the `WorkItemStateTransition` data shipped by `time-in-state-and-staleness`. No new sync-side capture.
- **Ongoing items included**: cumulative bar must include time-in-current-state for items that are still in flight, not only completed items. (User-confirmed in the originating message.)
- **Filter-scoped**: cumulative across all items in the active filter — team, portfolio, work-item-type, date range, etc. Existing filter primitives should compose unchanged.

## What is NOT decided yet

- Default chart placement (team detail page? portfolio detail page? new dedicated route?).
- Bar ordering: by workflow order, by descending cumulative time, by state category (To Do / Doing / Done)?
- Unit: total person-days? mean per item? both views?
- How to render the "ongoing" slice visually (different shade? stacked segment?).
- Date-range semantics: does an item that entered the state before the window contribute the full duration, or only the portion within the window?
- KPIs / AC / slice split.

These are open and intentionally left for the eventual `/nw-discuss` run.

## Initial story shape (for ADO seeding only — refine during DISCUSS)

- **US-A** — *Cumulative time-per-state bar chart for filtered items*: as a Delivery Lead, I want a bar chart showing total time spent in each workflow state across all items currently in my filter (including in-flight items' time in current state), so I can see which states are the biggest constraints on throughput.
- **US-B** — *Filter composition with existing Team / Portfolio / date filters*: as a Delivery Lead, I want the cumulative chart to respect my current filter selection, so I can compare a sprint vs a quarter, or a team vs a portfolio, without leaving the view.

## Pre-requisites

- `time-in-state-and-staleness` slice 01 (data foundation) merged so `WorkItemStateTransition` exists.
- Existing filter / scope primitives (verify reuse path during DISCUSS).

## Pointers

- Parent Epic catalog: `docs/feature/epic-4144-more-detailed-state-info/README.md`
- Sibling MVP features: `docs/feature/time-in-state-and-staleness/`, `docs/feature/aging-pace-percentiles/`
