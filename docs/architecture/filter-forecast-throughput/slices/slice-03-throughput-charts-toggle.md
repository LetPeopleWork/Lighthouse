# Slice 03: Throughput Charts — Filtered/Raw Per-View Toggle

**Feature**: filter-forecast-throughput
**Stories shipped**: US-05, US-03 chip (extended to the Throughput Run Chart + PBC surfaces)
**Estimate**: ~0.5–1 crafter day (depends on Slice 01 SPIKE-1 finding for the chart endpoint; see DEPENDS)
**Reference class**: chart header decoration + client-side dataset filter.

## Goal
Give the team's metrics view a way to flip between the team's TOTAL throughput (today's behaviour) and the team's FILTERED throughput — without changing pages, leaving the team's settings alone, or breaking the default-Raw invariant.

## IN scope
- Throughput Run Chart (`TotalThroughputWidget` and any related per-state throughput chart) and Throughput PBC chart get a new header control: `Show: [Raw] | [Filtered]`, rendered ONLY when:
  - tenant is premium, AND
  - the team has a non-empty filter configured.
- Toggle defaults to **Raw** on every render (D1 — non-breaking).
- When toggled to Filtered, the chart re-renders client-side: items that match the team's rule set are excluded from the throughput counts (and from the underlying chart series).
- US-03 chip ("Filtered throughput") is rendered alongside the chart title when Filtered is active; absent in Raw view.
- Empty-state for Filtered view when all items match the rule set: friendly message ("No items match the throughput filter in this window. Switch to Raw to see total throughput.").
- Toggle state is component-local (per-view), not persisted across navigations — explicit per-conversation intent (out of scope: persistent preference).

## OUT scope
- Charts other than the two throughput charts → out of scope (cycle time chart, work item age, predictability score are NOT toggled — D1 locks).
- Toggle persistence across navigations / sessions → out of feature.
- Backend changes to the chart-data endpoint, IF the endpoint already returns per-item granularity. If it only returns aggregated counts, this slice grows to include either a `?view=filtered` query param OR a parallel filtered endpoint — depending on the response from Slice 01's SPIKE-2 follow-on for the chart endpoint. DESIGN to settle definitively.

## Learning hypothesis
**Confirms if it succeeds**: at least one customer reports using the Filtered chart view in a conversation alongside the Raw view ("here's our total, here's our feature-bearing pace") within 60 days.
**Disproves if it fails**: the toggle is never observably flipped (zero community signal) → reconsider whether the chart toggle was needed at all, or whether a single "Raw" chart + a forecast-side chip is enough story-telling.

## Acceptance criteria
See US-05 in `../feature-delta.md` and the chip ACs in US-03.

## Dependencies
**Slice 01** — depends on the persisted rule set, the `IForecastFilterRuleService`, the chip pattern.
Independent of Slice 02. May be done in parallel with Slice 02 or Slice 04.

## Production data requirement
**Required.** Smoke against the project's own Lighthouse team Metrics view, both toggle positions, confirming counts differ when the filter matches items.

## Dogfood moment
Configure a rule set whose match-set is observably non-empty AND non-everything (e.g. ~30% of recent closes match). Flip toggle Raw → Filtered → confirm chart bars shrink predictably; chip appears. Flip back → bars restored, chip gone. No backend call observed in network panel (assuming client-side filter is feasible).

## Pre-slice spike candidates
- **SPIKE-3.1** (~1 hr): confirm `getThroughputPbc` and the throughput run-chart endpoint return per-item data (or that the dataset on the page is already item-granular). If aggregated-only, push back the slice scope to include a backend `?view=filtered` extension and another ~0.5 day.
