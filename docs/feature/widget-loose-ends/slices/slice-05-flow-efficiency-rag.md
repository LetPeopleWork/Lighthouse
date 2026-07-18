# Slice 05 — Flow Efficiency RAG status via the shared data path

**Story**: 5508 Cleanup Widget lose Ends | **Group**: C (data) | **Job**: `job-flow-coach-read-every-widget-the-same-way`

## Goal (one sentence)
Move the flow-efficiency fetch out of the widget and into the `BaseMetricsView` data layer so the widget gains the standard Act/Observe/Sustain chip like every one of its neighbours.

### Elevator Pitch
Before: Flow Efficiency colours its own big number but has no status chip, so it is the one widget whose status I read by a different convention.
After: open **Team → Metrics → Flow Overview** → the **Flow Efficiency** widget shows the same status chip and hover tip as its neighbours.
Decision enabled: scan the row of widgets for red chips and act on what I find, with no blind spot.

### Why the chip is missing
`FlowEfficiencyOverviewWidget.tsx` self-fetches via `metricsService.getFlowEfficiencyInfoFor{Team,Portfolio}` in its own `useEffect` and computes `computeFlowEfficiencyRag` internally to colour its `<Typography>`. It is the only Flow Overview widget off the shared data path, and `buildWidgetFooters` therefore has no `flowEfficiency` key to emit — the `WidgetShell` header has nothing to render. The RAG rule already exists and is already correct; only the data location is wrong.

### Domain examples
1. Wait states configured, efficiency 72% → chip renders the status `computeFlowEfficiencyRag(72, terms)` returns, unchanged rule.
2. Wait states not configured → body keeps "Not configured — define wait states in settings", no status colour.
3. No data in scope → body keeps "No data in scope", no status colour.
4. Portfolio scope → identical behaviour via the portfolio fetch.
5. Fetch fails → widget degrades as today (no data), no status colour, no crash.

### Outcome KPI
Overview widget chrome parity: 100% of Flow Overview widgets expose a RAG status — asserted as a test over `getWidgetsForCategory("flow-overview")` vs the keys registered in `buildWidgetFooters`, which is exactly the assertion this slice makes pass.

## IN scope
- Move the flow-efficiency fetch into the `BaseMetricsView` data layer (alongside the other metrics reads in `useMetricsData` / the `BaseMetricsView` context), for both Team and Portfolio.
- `FlowEfficiencyOverviewWidget` becomes a presentational component rendering from props — no `useEffect`, no service call.
- Register `flowEfficiency: computeFlowEfficiencyRag(...)` in `buildWidgetFooters`, reusing the existing rule verbatim.
- Preserve both existing non-numeric bodies ("Not configured", "No data in scope") and ensure neither renders a misleading status colour.
- Widget status guidance entry in `widgetInfoMetadata.ts` if the widget lacks one.
- Vitest: widget renders from props with no service call; footer registration; both degraded bodies.

## OUT of scope
- Changing `computeFlowEfficiencyRag`'s thresholds or tip text (D7 reuses the rule as-is).
- Removing the in-number colouring, unless it becomes redundant against the chip — a presentation call for DESIGN, not a requirement here.
- Any other self-fetching widget outside Flow Overview.

## Learning hypothesis
- **Disproves if it fails**: that lifting the fetch is mechanical. If `BaseMetricsView`'s data layer cannot absorb the call without an extra round trip, a render loop, or a load-order problem, then "put every widget on the shared data path" is not a free refactor and future widgets may legitimately stay self-fetching.
- **Confirms if it succeeds**: the shared data path is the right home for every overview widget's reads, and the chrome parity KPI is structurally enforceable from here on.

## Acceptance criteria
1. Given a configured flow efficiency of X%, the widget header renders the status returned by `computeFlowEfficiencyRag(X, terms)` — the rule is unchanged.
2. Given wait states are not configured, the widget keeps its "Not configured" body and renders no status colour.
3. Given no data in scope, the widget keeps its "No data in scope" body and renders no status colour.
4. Flow efficiency data is fetched once through the `BaseMetricsView` data layer; the widget makes no service call of its own (asserted by rendering it from props with a service mock that must not be called).
5. Colour is not the only signal — the Act/Observe/Sustain label and tip carry the same information.
6. Holds at Portfolio scope as well as Team scope.
7. A test asserts every Flow Overview widget key has a `buildWidgetFooters` entry, so a future widget cannot silently ship without a status.
8. E2E (demo data, POM-mediated): the Flow Efficiency widget on a demo team shows a status chip; the POM asserts the status attribute, not a pixel.

## Dependencies
None — independent of slices 01-04. Scheduled last only because it can absorb schedule slack without stranding anything else.

## Effort / reference class
~0.5–1 day. Reference class: any of the existing overview widgets that already render from `BaseMetricsView` props with a `buildWidgetFooters` entry (`WipOverviewWidget`, `StaleOverviewWidget`) — this slice makes Flow Efficiency look like them.

## Pre-slice SPIKE
None. Watch-item at DESIGN: check whether the existing flow-efficiency fetch can join the batched read in `useMetricsData` (which already runs several metrics calls together) rather than becoming a separate sequential request — the point of the lift is one shared data path, not one more round trip.
