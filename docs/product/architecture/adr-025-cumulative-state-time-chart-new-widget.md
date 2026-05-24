# ADR-025: Cumulative State-Time Chart — New `CumulativeStateTimeChart` Widget (Not Extension of `WorkItemAgingChart`), Stacked Horizontal Bars via MUI-X `BarChart`, Single `flow-metrics` Widget Registration

**Status**: Accepted (2026-05-24 — Morgan, interaction mode PROPOSE; bundled with ADR-022/023/024)
**Date**: 2026-05-24
**Feature**: state-time-cumulative-view (Epic 4144 MVP bundle, slice B3)
**Decider**: Morgan (Solution Architect)

---

## Context

DISCUSS D2 places the new chart in the `flow-metrics` category on both the team detail and portfolio detail pages under widget key `stateTimeCumulative` at size `large`. D3 specifies workflow-order bar layout; D4 picks total-days as the unit with mean-per-item in the tooltip; D6 specifies stacked completed/ongoing segments with diagonal hatching for the ongoing segment. US-04 adds a click-to-drill-down handler.

DESIGN must pin:

1. **Component shape**: a NEW sibling widget vs an extension of an existing chart component. Sibling F (ADR-020) extended the existing `WorkItemAgingChart` because the dots and bands belong on the SAME canvas conceptually. This feature's chart is fundamentally different (per-state bars summed across items, not per-item dots), so the extension lever does not apply.
2. **Rendering primitive**: MUI-X `BarChart` (declarative, already used by `BarRunChart`) vs custom SVG (sibling F's approach for its overlay). For a NEW chart with the standard "stacked horizontal bars" semantic, the MUI-X primitive applies cleanly.
3. **Widget registration**: a SINGLE entry in `categoryMetadata.ts` + `widgetInfoMetadata.ts` + `ragRules.ts`, dispatched by `BaseMetricsView.tsx` to BOTH scopes (team + portfolio) via the existing `ownerFilter` mechanism (which here would be omitted, meaning "render in both"). Confirmed via codebase inspection of `categoryMetadata.ts:69-78` showing exactly this pattern for the existing `flow-metrics` widgets (`cycleScatter`, `aging`, `throughput`, etc. all have no `ownerFilter` and render in both scopes).

The frontend's component-level convention (verified via `WorkItemsDialog`, `BarRunChart`, `WorkItemAgingChart`, `CycleTimeScatterPlotChart`) is: one component per chart, MUI-X primitive when applicable, custom SVG only when MUI-X is insufficient.

The DISCUSS Slice 01 spec specifically calls out that the chart does NOT have a pre-slice SPIKE because "the reference class (sibling F slice 01) covers all the uncertainty about endpoint shape, chart-widget registration, and `ragRules` integration. The only new complexity is the segment-stacking arithmetic, which is well-specified in D5/D6 of the feature-delta." This ADR pins the segment-stacking rendering primitive so DELIVER does not stall.

---

## Decision

### 1. NEW widget component — `CumulativeStateTimeChart.tsx`

Create a NEW React component `Lighthouse.Frontend/src/components/Common/Charts/CumulativeStateTimeChart.tsx`. Do NOT extend `WorkItemAgingChart` (different chart type, different data shape, different click target).

Component contract:

```
interface ICumulativeStateTimeChartProps {
    states: ICumulativeStateTimeStateRow[];
    isLoading: boolean;
    onBarClick: (stateName: string) => void;
}

interface ICumulativeStateTimeStateRow {
    state: string;
    workflowOrder: number;
    totalDays: number;
    completedContributionDays: number;
    ongoingContributionDays: number;
    itemCount: number;
    completedItemCount: number;
    ongoingItemCount: number;
    meanDays: number;
    medianDays: number;
}
```

The component:

- Renders ONE bar per entry in `states`, ordered by `workflowOrder` ascending (DISCUSS D3).
- Each bar is stacked into two segments: a solid base segment of height `completedContributionDays` and a diagonally-hatched top segment of height `ongoingContributionDays` (DISCUSS D6).
- Tooltip on hover discloses ALL fields from `ICumulativeStateTimeStateRow` plus the US-03 inclusion-breakdown line.
- Click handler fires `onBarClick(stateName)` (US-04).
- Empty case: when `states.length === 0`, renders an empty-state message in tone matching `WorkDistributionChart`'s empty state.
- Zero-contributing-state: a row with `totalDays === 0` still renders a labelled bar with height 0 (DISCUSS US-01 AC line 6).

### 2. Rendering primitive — MUI-X `BarChart` with stacked series

Use MUI-X `<BarChart>` from `@mui/x-charts` (existing dependency; same library `WorkItemAgingChart` uses for its scatter and `BarRunChart` uses for its bars).

Configuration:

- `layout="horizontal"` — the bars run left-to-right matching the chart's value axis; the state column is the category axis on the Y axis (so per-DISCUSS D3 the workflow-ordered states cascade top-to-bottom; alternative `layout="vertical"` with X-axis state columns is equivalent under D3 — software-crafter selects at GREEN based on screen-real-estate after seeing the chart against real data; DISCUSS D3 is satisfied by either orientation as long as the order matches the team's workflow).
- Two stacked series (`stack="contribution"`):
  - Series 1 (bottom): `data = states.map(s => s.completedContributionDays)`, label `"Completed contribution"`, solid fill in the chart's theme primary colour.
  - Series 2 (top): `data = states.map(s => s.ongoingContributionDays)`, label `"Ongoing contribution"`, fill via an SVG `<pattern>` rendering diagonal hatching at 45°.
- Bar `onClick` wires to `onBarClick(state)`.

The diagonal hatching for the ongoing segment is implemented as an SVG `<defs><pattern id="cumulativeStateTimeOngoingHatch" .../></defs>` inside the chart's `<ChartsContainer>`, with the second series's `color` set to `url(#cumulativeStateTimeOngoingHatch)`. This is the standard SVG approach for fill-pattern styling and is supported by MUI-X 5+ chart series colour configuration. Software-crafter selects the exact pattern dimensions at GREEN; the architectural commitment is "diagonal hatching, visually distinct from the solid base segment, distinguishable for colour-blind users" (DISCUSS D6 rejected alternative shades for colour-blindness).

Tooltip: use MUI-X `<ChartsTooltip>` with a `slots`/`slotProps` override or a custom `slot.tooltip` component that renders ALL ICumulativeStateTimeStateRow fields per AC. The tooltip's structure:

```
{state}
─────────────────────────
Total: {totalDays}d
  Completed: {completedContributionDays}d
  Ongoing:   {ongoingContributionDays}d
Items: {itemCount} ({completedItemCount} closed in window, {ongoingItemCount} still in flight)
Mean per item:   {meanDays}d
Median per item: {medianDays}d
Full durations counted per item (not clipped to window).
```

The last line is the US-03 attribution clarification; the second-to-last sentence (`Items: ...`) is the US-03 inclusion-breakdown line. Both render server-side from fields the bar endpoint already returns (ADR-022 §6) — no extra round-trip. ARIA / screen-reader: the tooltip element carries an `aria-label` summarising the same content in plain language (US-03 AC line 4).

### 3. Drill-down dialog wiring (US-04)

`CumulativeStateTimeChart` does NOT own the drill-down dialog directly. The chart fires `onBarClick(stateName)`; the parent (a new wrapper component `CumulativeStateTimeWidget` or directly `BaseMetricsView`'s widget dispatch — software-crafter selects at GREEN) holds the dialog's open state and the resolved drill-down `items[]`. On click:

1. Parent fetches `MetricsService.getCumulativeStateTimeItemsForTeam(entity.id, stateName, startDate, endDate)` (or `…ForPortfolio`).
2. Parent opens `<CumulativeStateTimeDrillDownDialog state={stateName} items={resolvedItems} open={true} onClose={...} />` (per ADR-023).
3. While the fetch is pending, the dialog renders a loading spinner inside its content area (or the parent suppresses the dialog open until the fetch resolves — software-crafter selects at GREEN based on perceived UX).

This separation keeps the chart component focused on chart concerns and the dialog component focused on dialog concerns.

### 4. Widget registration — single entry

`categoryMetadata.ts`:

```
"flow-metrics": [
    { widgetKey: "cycleScatter", size: "large" },
    { widgetKey: "aging", size: "large" },
    // ... existing entries ...
    { widgetKey: "stateTimeCumulative", size: "large" },  // NEW — no ownerFilter, renders in both team and portfolio scope
],
```

`widgetInfoMetadata.ts`: new entry `stateTimeCumulative` with description, RAG status guidance, learn-more URL pointing to the documentation page added in the same wave (DISCUSS DoD #12).

`ragRules.ts`: new `computeCumulativeStateTimeRag(states: ICumulativeStateTimeStateRow[], terms: RagTerms): RagResult` function. RAG semantics per DISCUSS US-01 AC:

- `green` (sustain) when no single state has > 40% of total cumulative time across all states (balanced flow).
- `amber` (observe) when one state has 40-60% of total cumulative time.
- `red` (act) when one state has > 60% (single dominant constraint).
- Edge case: when `states.length === 0` (no items match the filter), return `red` with a sustain-style tip pointing the user at the filter ("No items match the filter. Adjust scope or date range to see the chart."). This matches the empty-state guidance pattern of `computeWipOverviewRag`.

`BaseMetricsView.tsx`: extend the existing widget-dispatch switch (the same place `WorkItemAgingChart`, `WorkDistributionChart`, etc. are dispatched by their `widgetKey`) to dispatch `stateTimeCumulative` to `<CumulativeStateTimeWidget …>` (or `<CumulativeStateTimeChart …>` directly — wrapper vs flat dispatch is a software-crafter choice at GREEN, both shapes preserve the architectural seam).

`MetricsService.ts`: two new methods (one per scope) for the bar endpoint, two for the items endpoint:

```
getCumulativeStateTimeForTeam(teamId: number, startDate: Date, endDate: Date): Promise<ICumulativeStateTimeResponse>
getCumulativeStateTimeForPortfolio(portfolioId: number, startDate: Date, endDate: Date): Promise<ICumulativeStateTimeResponse>
getCumulativeStateTimeItemsForTeam(teamId: number, state: string, startDate: Date, endDate: Date): Promise<ICumulativeStateTimeItemsResponse>
getCumulativeStateTimeItemsForPortfolio(portfolioId: number, state: string, startDate: Date, endDate: Date): Promise<ICumulativeStateTimeItemsResponse>
```

All four mirror the existing `getCycleTimePercentiles` HTTP shape (line 247 of `MetricsService.ts`). Added to the `IMetricsService` interface and the concrete `MetricsService` class.

`useMetricsData` extends ctx with `cumulativeStateTime: ICumulativeStateTimeResponse | null` (initial `null`) and a fetch in the same `useEffect` block that today fetches `cycleTimePercentiles`. Parallel-fetch alongside the existing window-scoped requests.

### 5. NO contract test recommendations (external integrations)

This feature reads only Lighthouse-internal persisted data (transitions table + work items table). There are no third-party APIs, no webhooks, no OAuth providers, no internal cross-team APIs introduced. **No contract tests recommended at the platform-architect handoff.**

---

## Alternatives Considered

**Option A — Extend `WorkItemAgingChart` to ALSO render the cumulative bars (single chart, multi-visualisation).**

- Pros: one chart on the page; user sees per-item dots, per-state bands (sibling F), and per-state cumulative bars (this feature) together.
- Cons:
  1. The three visualisations answer different questions (per-item triage vs per-state pace vs per-state cumulative time). Combining them into one chart overloads the visual surface; users glancing at a stuffed chart cannot identify which visual element answers which question.
  2. The data shapes are different: dots are per-item points, bands are per-state percentile heights, bars are per-state summed durations. Combining them requires either three Y axes (impossible in MUI-X out of the box) or three different chart layers sharing one Y axis (which would conflate units — "days in current state" for dots, "days at percentile" for bands, "summed days across items" for bars).
  3. DISCUSS D2 explicitly places this chart in `flow-metrics` as a SEPARATE widget. Combining would override DISCUSS.
- **Rejected** for conceptual overload and DISCUSS conflict.

**Option B — Custom SVG bars (sibling F's overlay technique applied to a new component).**

- Pros: full control over segment shapes, hatching, click targets.
- Cons:
  1. Stacked horizontal bars are a STANDARD MUI-X primitive. Reimplementing them in custom SVG discards weeks of investment that MUI-X already provides (axis labels, value-axis ticks, responsive resize, hover detection, click detection).
  2. Sibling F's custom SVG was needed because MUI-X `ChartsReferenceLine` does not support X-ranged segments. This feature has the opposite situation: MUI-X `BarChart` natively supports stacked bars; no missing primitive.
  3. Hatching is achievable via SVG `<pattern>` references on the MUI-X series colour, which is the standard SVG approach and works inside `BarChart`.
- **Rejected** because MUI-X `BarChart` is the right primitive; custom SVG would be net loss.

**Option C — Use `BarRunChart` (existing) with new props.**

- Pros: reuse an existing chart component.
- Cons: `BarRunChart` (`Lighthouse.Frontend/src/components/Common/Charts/BarRunChart.tsx`) is a single-series bar-over-time chart for run-chart-style data (throughput, arrivals). It does NOT support stacked segments, NOT support horizontal layout, NOT support per-bar click handlers for state-name dispatch. Extending it to support all three of these features would either (a) double its complexity and break its existing tests or (b) fork it. Either path is more work than a NEW component.
- **Rejected** for shape mismatch.

**Option D — Two separate widgets (one for the bar chart, one for the drill-down table) registered in `categoryMetadata`.**

- Pros: full separation; the drill-down could be browsed independently.
- Cons: the drill-down is a follow-up action ON the bar chart, not a standalone insight. Users would not browse the drill-down without first identifying a state to drill into; surfacing it as a sibling widget is a UX confusion.
- **Rejected** in favour of the click-from-chart dialog pattern (ADR-023).

**Option E — Use `StackedAreaChart` (existing) horizontally rotated.**

- Pros: leverages an existing chart for "stacked totals".
- Cons: `StackedAreaChart` is an area chart over time, not a bar chart over a categorical axis. Bars on categorical axes are bars; areas on continuous axes are areas. Type mismatch.
- **Rejected** for shape mismatch.

**Option F — Render the chart on a new top-level route (`/teams/{id}/cumulative-state-time`).**

- Pros: full page real estate; richer interactions possible.
- Cons: DISCUSS D2 explicitly locks placement inside the existing Flow Metrics category. A new top-level route fragments the user's mental model.
- **Rejected** for DISCUSS conflict.

---

## Consequences

**Positive**:

- A new, focused chart component renders the new metric without overloading existing charts.
- MUI-X `BarChart` provides axis labels, ticks, hover detection, click detection, responsive resize, accessibility primitives — all reused.
- The widget registration mechanism (`categoryMetadata.ts` + `widgetInfoMetadata.ts` + `ragRules.ts` + `BaseMetricsView.tsx` dispatch) is the established codebase pattern; this feature follows it exactly.
- Single widget entry renders in both team and portfolio scopes via the existing `ownerFilter`-omitted convention (matching `cycleScatter`, `aging`, etc.).
- The drill-down dialog (ADR-023) is decoupled from the chart — the chart fires a click event; the parent owns the dialog. Both components are testable in isolation.
- New `computeCumulativeStateTimeRag` function follows the existing `ragRules.ts` shape; no regression to existing RAG functions; passes the existing `ragRules.test.ts` unchanged (per DISCUSS DoD #8).

**Negative**:

- One new chart component (~150–200 LOC). Quantified as net-new because no existing component overlaps semantically.
- One new SVG `<pattern>` definition for hatching. Single-file scope; no cross-component drift risk.
- The MUI-X `BarChart` stacking + per-bar click interaction is exercised at slice 01; if any MUI-X version pin in the project doesn't expose the bar `onClick` API at the right level of granularity, software-crafter at GREEN may need a small adjustment (e.g. using the chart's `onItemClick` handler instead). **Validation deferred to DELIVER slice 01** — same risk profile as sibling F's `<line>` overlay interop assumption. If false, the alternative is a custom SVG bar layer (Option B above) — heavier but feasible. No DESIGN blocker.

**Neutral**:

- The component is testable in Vitest + RTL via MUI-X chart's data-testid attributes and the rendered SVG structure (per the existing `WorkItemAgingChart.test.tsx` and `BarRunChart.test.tsx` patterns).
- Mutation testing (Stryker for TS) ≥80% on new code per DoD covers the bar-height arithmetic, the tooltip field assembly, the RAG threshold logic, and the click handler dispatch.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| `CumulativeStateTimeChart` is a NEW component file under `Lighthouse.Frontend/src/components/Common/Charts/`; it does NOT extend `WorkItemAgingChart` | Code-review gate; ArchUnit-style assertion via Biome / import-linter is not available for TS in this codebase, so the rule is enforced via code review with this ADR as canonical reference |
| The chart uses MUI-X `<BarChart>` (NOT a custom SVG bar layer) for the primary bar rendering | Vitest test asserts a MUI-X `BarChart`-produced container is present in the rendered DOM (via the established data-testid pattern) |
| The hatching for the ongoing segment uses SVG `<pattern>` referenced by the series colour (not a different shade) | Vitest test queries for an SVG `<pattern id="cumulativeStateTimeOngoingHatch">` element in the rendered DOM |
| The bar `onClick` fires `onBarClick(stateName)` with the correct state name | Vitest test simulates a bar click and asserts the handler is called with the expected state name |
| `computeCumulativeStateTimeRag` thresholds: green ≤ 40%, amber 40-60%, red > 60% (per DISCUSS US-01 AC line 7) | Unit test in `ragRules.test.ts` asserts each threshold boundary |
| Widget registration in `categoryMetadata.ts` has NO `ownerFilter` (renders in both team and portfolio scope) | Vitest test asserts `getWidgetsForCategory("flow-metrics", "team")` and `getWidgetsForCategory("flow-metrics", "portfolio")` both include the `stateTimeCumulative` widget |
| The chart accepts `isLoading: boolean` and renders an appropriate skeleton/loader (consistent with the `WidgetShell` loading conventions) | Vitest test asserts the rendered output when `isLoading: true` |

---

## Cross-feature impact

- `time-in-state-and-staleness` (sibling 1): UNCHANGED. The chart reads only the response from this feature's new endpoints; no schema or sync-path coupling.
- `aging-pace-percentiles` (sibling F): UNCHANGED. Sibling F's chart (`WorkItemAgingChart`) and this feature's chart (`CumulativeStateTimeChart`) are sibling widgets in the same `flow-metrics` category — they coexist on the same page, render side-by-side at the user's chosen widget order, and share no component code. The two are conceptually related ("both per-state under flow-metrics") but technically independent.
- Future per-state visualisation features could adopt this pattern: a new MUI-X-`BarChart`-based widget + a new entry in `categoryMetadata` + a new RAG rule. The pattern this ADR establishes is reusable.

---

## Modal vs side-panel — recommendation

The drill-down dialog (US-04) uses MUI `Dialog` (modal), not a side-panel/drawer. This decision is made authoritatively in ADR-023; this ADR's chart contract (`onBarClick(stateName: string)`) is shaped to support either UI primitive equally well — the dialog vs panel choice lives one layer above the chart in the parent component. Per ADR-023, the modal is the chosen primitive for in-codebase consistency with `WorkItemsDialog`.

