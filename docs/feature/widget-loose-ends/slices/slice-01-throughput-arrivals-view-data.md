# Slice 01 — View Data on Throughput/Arrivals + aging percentile label

**Story**: 5508 Cleanup Widget lose Ends | **Group**: A (chrome wiring) | **Jobs**: `job-flow-coach-drill-into-throughput-and-arrivals`, `job-flow-coach-read-every-widget-the-same-way`

## Goal (one sentence)
Register the two missing `viewData` payloads so Total Throughput and Total Arrivals gain the drill-through every neighbouring widget already has, and strip the redundant term prefix from the Work Item Age percentile line labels on the aging chart.

### Elevator Pitch
Before: "Total Throughput 23" with no way to ask which 23, and the aging chart's WIA lines repeat "Work Item Age" on all four labels.
After: click the table icon on Total Throughput → `WorkItemsDialog` lists the completed items; switch the aging chart to Work Item Age → lines read `95% / 85% / 70% / 50%`.
Decision enabled: judge whether a throughput number came from real work or a batch of trivia, and read a dot's band without parsing a label that repeats the selected mode.

### Domain examples
1. Range with 23 completions → View Data on Total Throughput lists 23 items with the cycle-time highlight column.
2. Range with 17 arrivals → View Data on Total Arrivals lists those 17.
3. Empty range → icon renders, dialog opens empty, no crash.
4. Aging chart, source `workItemAge` → label `85%`.
5. Aging chart, source `cycleTime` → label unchanged from today.

### Outcome KPI
Overview widget chrome parity: every Flow Overview widget backed by an item set exposes View Data (asserted as a test over `getWidgetsForCategory("flow-overview")`, not a manual audit).

## IN scope
- Add `totalThroughput` and `totalArrivals` keys to `buildViewData` (`BaseMetricsView.tsx:466`), reusing the item sources the sibling chart widgets already build: `throughputItems` and `extractWorkItems(inputs.arrivalsData?.workItemsPerUnitOfTime)` (see the existing `throughput` and `arrivals` entries at lines ~630-650).
- Titles and highlight columns matching the sibling entries (`${title} Completed` / cycle-time highlight for throughput; the arrivals equivalent for arrivals).
- `WorkItemAgingChart.tsx:653-654` — render `` `${p.percentile}%` `` instead of `` `${workItemAgeTerm} ${p.percentile}%` ``.
- Vitest coverage for both `buildViewData` keys (present, correct items, empty case) and the label change.
- Team and Portfolio scope (CI2).

## OUT of scope
- Any new endpoint, query, or item source. If the arrivals item set is not already available at the `buildViewData` call site, thread the existing prop — do not fetch.
- RAG or trend for these widgets (`totalThroughput`/`totalArrivals` already have `previous-period` trend policies and working comparisons).
- Any other widget's chrome.

## Learning hypothesis
- **Disproves if it fails**: that the missing chrome is pure registration. If wiring `buildViewData` requires a new item source or a new fetch, then the five "just wiring" gaps are not uniform and slices 03-05 need re-estimating.
- **Confirms if it succeeds**: `WidgetShell` chrome gaps are registration gaps, and the remaining widget parity work is bounded by the data already in `BaseMetricsView`.

## Acceptance criteria
1. Given a team with completed items in the selected range, clicking View Data on Total Throughput opens `WorkItemsDialog` with exactly those items and the cycle-time highlight column.
2. Given a team with arrivals in the selected range, clicking View Data on Total Arrivals opens the dialog with exactly those items.
3. Given zero items in scope, the icon renders and the dialog opens empty without error.
4. Given the aging chart with percentile source `workItemAge`, each reference line label is exactly `{percentile}%`.
5. Given percentile source `cycleTime`, labels are unchanged from current behaviour.
6. Both View Data behaviours hold at Portfolio scope as well as Team scope.
7. E2E (demo data, POM-mediated): the Flow Overview View Data icon on Total Throughput opens a populated dialog for a demo team.

## Dependencies
None. `WidgetShell` already renders the View Data icon whenever a `viewData` payload is supplied; both item sources already exist in `BaseMetricsView`.

## Effort / reference class
~0.5 day. Reference class: the existing `throughput` / `arrivals` `buildViewData` entries — this slice is the same edit twice, plus a one-line label change. Frontend + Vitest only; no backend.

## Pre-slice SPIKE
None. Watch-item at DESIGN: confirm `arrivalsData` is in scope at the `buildViewData` call site (the `arrivals` entry at line ~643 suggests yes); if the throughput item extraction is local to the chart entry, hoist it rather than duplicating.
