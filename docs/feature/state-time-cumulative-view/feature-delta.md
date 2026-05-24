<!-- markdownlint-disable MD024 -->
# Feature: state-time-cumulative-view

Epic 4144 (More Detailed State Info) — third and final MVP-bundle feature, covers slice B3 from the Epic catalog.

ADO Epic: <https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/4144>
ADO Story (already created): <https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5076>

## Wave: DISCUSS / [REF] Pre-DISCUSS code reality check

Before drafting this delta, the existing Lighthouse charting and metrics surfaces were inspected to confirm or refute the carry-over README's gap claim ("no chart in Lighthouse today answers 'cumulative time per state across items in a window'"). Following the lesson from sibling `aging-pace-percentiles` (where pre-DISCUSS inspection revealed an inaccurate premise), the same rigor was applied here.

**Files inspected**:

- `Lighthouse.Frontend/src/components/Common/Charts/*` — full chart inventory: `BarRunChart`, `BaseRunChart`, `CycleTimePercentiles`, `CycleTimeScatterPlotChart`, `EstimationVsCycleTimeChart`, `FeatureSizeScatterPlotChart`, `LineRunChart`, `LoadBalanceMatrixChart`, `PercentileLegend`, `ProcessBehaviourChart`, `StackedAreaChart`, `TotalWorkItemAgeRunChart`, `TotalWorkItemAgeWidget`, `WorkDistributionChart`, `WorkItemAgingChart`. Plus widgets in `Lighthouse.Frontend/src/pages/Common/MetricsView/`.
- `Lighthouse.Frontend/src/pages/Common/MetricsView/widgetInfoMetadata.ts` — complete widget catalog with descriptions, RAG rules, and learn-more URLs.
- `Lighthouse.Frontend/src/pages/Common/MetricsView/categoryMetadata.ts` — widget placement per category (`flow-overview`, `flow-metrics`, `predictability`, `portfolio`).
- Codebase grep: `timeInState|TimeInState|cumulativeTime|stateBreakdown|cycleTimeByState|timePerState|stateAggregation` — zero production matches in `Lighthouse.Backend` or `Lighthouse.Frontend/src` (only the new feature-delta and journey files for sibling features mention `TimeInState`).
- `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/TeamMetricsService.cs` and `PortfolioMetricsService.cs` — no per-state aggregation method exists.

**Finding**: the carry-over README's framing is **accurate**. No existing widget or service computes cumulative time-per-state across items in a window. The closest existing widgets and their distinct purposes:

- `stacked` widget — "Simplified Cumulative Flow Diagram showing Doing and Done areas". This is a CFD: areas-OVER-TIME for two `StateCategory` buckets (Doing / Done), not cumulative time-PER-STATE across items. Different chart shape, different question.
- `WorkDistributionChart` — aggregates by parent feature/epic, not by workflow state.
- `WorkItemAgingChart` — per-item dots on a per-state X axis, not aggregated per state.
- `cycleScatter` and `percentiles` widgets — end-to-end cycle time of completed items, not per-state.

**No premise correction required**. The stub README's gap claim stands. The carry-over README is annotated below for reader trust (per Document Update contract: README NOT modified directly; this `feature-delta.md` is the modification record, and a note has been added to the stub README pointing here for clarity).

This contrasts with sibling `aging-pace-percentiles` where the README's premise was wrong (the chart DID have end-to-end CT bands already, gap was only per-state). Surfacing this contrast explicitly so the next DISCUSS reader can trust that BOTH directions (premise-confirmed and premise-corrected) get the same code-reality-check rigor.

## Wave: DISCUSS / [REF] Persona ID

**Primary**: `delivery-lead-rte` — Delivery Lead, Release Train Engineer, Engineering Manager, or anyone owning the team's or release train's quarterly improvement agenda. NEW persona created in this DISCUSS run (`docs/product/personas/delivery-lead-rte.yaml`). Differentiated from `flow-coach`: the flow coach answers "which ITEM is stuck?" (per-item triage, daily/weekly cadence); the delivery lead answers "which STATE is the constraint?" (systemic analysis, quarterly cadence).

**Secondary**: `flow-coach` — same persona file as sibling features; reuses the chart in the retro-facilitation context (a flow coach often co-presents the retro). Decision shape there is also "where to invest improvement effort", but the coach typically frames it per-team and on a sprint cadence, where the delivery lead frames it per-release-train and per-quarter.

## Wave: DISCUSS / [REF] JTBD one-liner

When I am preparing a retrospective, an improvement-planning meeting, or a leadership review and the conversation is about WHERE to invest our process-improvement effort next quarter, I want to see at a glance which workflow state has consumed the most of our collective time during the period — including time that in-flight items are STILL consuming — so I can name the constraint with evidence ("Review took 38% of our cumulative time this quarter") rather than guessing from anecdotes or per-item charts that answer a different question.

**Job-id**: `job-delivery-lead-spot-workflow-constraint` (NEW — added to `docs/product/jobs.yaml` in this DISCUSS run).

Differentiated from the two sibling jobs:

- Sibling A+B1+D (`job-flow-coach-spot-stuck-items`): per-item, "which item is stuck right now?" Triage cadence (daily / weekly). Answer: red badge on a specific work item.
- Sibling F (`job-flow-coach-spot-pace-outliers`): per-item-on-aggregate-bands, "which in-flight item is dragging vs historical pace for its state?" Glance cadence (per chart visit, typically per standup or flow review). Answer: dot above a per-state percentile band.
- THIS feature (`job-delivery-lead-spot-workflow-constraint`): per-state-on-aggregate, "which workflow STATE took the most of our collective time?" Systemic cadence (retro, quarterly planning, leadership review). Answer: the tallest bar on a bar chart of cumulative time per state.

The three jobs all consume the same `WorkItemStateTransition` data foundation but answer materially different questions. None is a substitute for the others — they are complementary lenses on the same underlying flow data.

## Wave: DISCUSS / [REF] Locked decisions

| ID | Decision | Verdict |
|---|---|---|
| D1 | NO existing cumulative time-per-state chart in Lighthouse. Verified via pre-DISCUSS code reality check above. No premise correction needed; the carry-over README's gap claim stands. | Locked — confirmed via inspection of `Charts/*`, `widgetInfoMetadata.ts`, `categoryMetadata.ts`, and backend metrics services. |
| D2 | **Default chart placement**: team detail page AND portfolio detail page, both under the existing `flow-metrics` category. Widget key: `stateTimeCumulative`. Size: `large`. Same placement reasoning as sibling F (D8): scope parity avoids confusion. Shipping a new top-level route would fragment the user's mental model. | Locked. |
| D3 | **Bar ordering**: workflow order (left-to-right matching the team's kanban board, same ordering as `WorkItemAgingChart` X-axis state columns). Alternatives considered: descending-by-cumulative-time (rejected — breaks pipeline-shape mental model); by `StateCategory` (rejected — hides per-state detail which is the whole point). | Locked. |
| D4 | **Unit displayed**: total time (in days) as the primary bar height. Tooltip discloses BOTH total and mean-per-item plus the item count. No user-facing total/mean toggle in MVP — one chart, one story. If post-release telemetry shows the mean view as a recurring user need, fold into a follow-up. | Locked. |
| D5 | **Date-range semantics: full-duration attribution with frame-based item selection** (revised 2026-05-24 per user redirect). The filter window selects WHICH ITEMS are included (per D12 inclusion rule); for every included item, the bar counts the FULL time the item spent in that state — regardless of whether that time falls inside or outside the window. An item that entered "Review" Dec 1 2025 and exited Jan 20 2026 contributes its FULL 50 days to the Review bar, even when the window is Q1 2026. Rationale: the chart's job is to surface the *real cycle pattern* of items relevant to the period, not to clip duration accounting to the window. Clipping would hide actual time items spent in slow states and mislead leadership about the true cost of those states. User verbatim (2026-05-24): *"we pick the items that were relevant within the frame, but then look at the full time. so even if an item was closed on the 1st day of the window, if it was in progress 40 days before that, we should count the full time."* **Alternative considered and rejected**: clip-to-window (was the initial DISCUSS default; rejected on user redirect because it under-counts real cycle time and conceals the constraint). A future toggle (full vs clip) is out-of-scope for MVP unless post-release telemetry shows demand. | Locked. |
| D6 | **Ongoing-items visual treatment**: each bar is stacked into TWO segments — a solid base segment for "completed contribution" (items that exited the state during the window) and a diagonally-hatched top segment for "ongoing contribution" (items still in the state at window end). User-stated requirement verbatim 2026-05-24: *"cumulative times for all items in the filter, including ongoing items"*. Hatching is the familiar Lighthouse "in progress" visual idiom (consistent with how blocked items are emphasized today). Alternatives considered: different shade (rejected — colour-blind users would lose the distinction); separate bar per segment (rejected — doubles the chart's bar count and breaks the one-bar-per-state mental model). | Locked. |
| D7 | **Filter composition**: composes UNCHANGED with existing Team/Portfolio scope, work-item-type chips, and date-range selector. No new filter primitives. Chart recomputes when any of those change. Matches sibling F's D7. | Locked. |
| D8 | **WS strategy**: Type A (additive). No contract change to existing endpoints. ONE new endpoint per scope (team + portfolio); ONE new chart widget; ONE new entry in `widgetInfoMetadata` + `categoryMetadata`. If the endpoint is absent, the chart slot is empty — no regression to existing widgets. | Locked. |
| D9 | **Sibling-data dependency on `WorkItemStateTransition`**: this DISCUSS does NOT request any schema addition to the transition table. The four fields shipped in sibling slice 01 (`workItemId`, `fromState`, `toState`, `transitionedAt`) PLUS the derived `currentStateEnteredAt` field on `WorkItem` are sufficient. The cumulative-time-per-state computation is fully expressible from those alone (formula in `journey-state-time-cumulative-view.yaml` D9 entry). **No upstream coordination with sibling 1 DESIGN required.** | Locked — explicit no-impact on sibling DESIGN. |
| D10 | **Cross-feature alignment with sibling F**: both this feature and `aging-pace-percentiles` compute per-state aggregations from `WorkItemStateTransition`. A shared per-state aggregation service / materialised cache is a candidate DESIGN-time optimization but NOT required for MVP correctness. MVP proceeds assuming independent computation per feature; DESIGN may consolidate if profiling demands it. | Locked (DESIGN-flagged, not DISCUSS-blocking). |
| D11 | **In-flight items MUST contribute**: user-stated requirement verbatim 2026-05-24. The bar MUST include time-in-current-state for items still in flight at window end (provided they meet D12's inclusion rule). Per D5 full-duration semantics, an in-flight item's contribution to the ongoing segment is its FULL `now - currentStateEnteredAt` time (not clipped to windowStart). D6 covers the visual treatment. | Locked (verbatim user requirement). |
| D12 | **Item-inclusion rule**: a work item is included in the chart if (a) its timeline has at least one `(state-enter, state-exit)` interval whose `[stateEnter, stateExit]` window intersects the filter window, OR (b) it is currently in-flight with `currentStateEnteredAt ≤ windowEnd`. Concretely covers: items closed within the window, items started within the window, items in any non-Done state at any point during the window, items still in-flight today whose current state was entered before windowEnd. Once included, the item's FULL state-durations contribute (per D5). Items entirely outside the window (e.g. closed before windowStart with no state-time overlap) are excluded. | Locked (user-confirmed 2026-05-24). |

## Wave: DISCUSS / [REF] User stories with elevator pitches

### US-01 — Cumulative time-per-state bar chart for filtered items (team scope, walking skeleton)

**Story**: As a `delivery-lead-rte`, I want a horizontal bar chart on my team's detail page (under Flow Metrics) showing total time spent in each workflow state across all items currently in my filter — INCLUDING time-in-current-state for in-flight items — so I can see at a glance which workflow state is the constraint on my team's throughput this quarter.

**Job-id**: `job-delivery-lead-spot-workflow-constraint`

#### Elevator Pitch

Before: I want to know "where did our team's time go last quarter?" but no chart in Lighthouse answers it. The Simplified CFD shows volume-over-time by `StateCategory` (Doing/Done only), not per workflow state, not aggregated. To answer the question I either guess (anecdote from the retro), or I export raw history into a spreadsheet and pivot it myself.
After: open `/teams/{teamId}` → Flow Metrics tab → see a new "Cumulative Time per State" widget. Bars are one-per-state in workflow order (matching the team's kanban). Each bar is split into a solid base (time contributed by items completed during the window) and a diagonally-hatched top segment (time contributed by items still in that state at window end). Hover a bar → tooltip shows `State: Review | Total: 184d | Items: 47 (38 completed, 9 ongoing) | Mean: 3.9d`.
Decision enabled: which workflow state to target for next quarter's process-improvement investment — "Review consumed 38% of our time last quarter; that's where the improvement budget goes".

**AC**:

- Given a team with `WorkItemStateTransition` data accumulated and at least one item matching the active filter AND the D12 inclusion rule, when I open `/teams/{teamId}` and switch to the Flow Metrics category, then the `stateTimeCumulative` widget renders with one bar per workflow state in the team's workflow order.
- Each bar's height equals the sum, over all D12-included items, of (a) the FULL duration `stateExit - stateEnter` of every completed `(enter, exit)` interval that item had in this state PLUS (b) `now - currentStateEnteredAt` if the item is currently in this state — per D5 full-duration attribution. The window does NOT clip durations; it selects which items are summed.
- Each bar is visually stacked into a solid base segment (`completedContribution`: time from D12-included items that have exited the state by now) and a diagonally-hatched top segment (`ongoingContribution`: time from items still in the state at now) — per D6.
- Bar tooltip on hover shows: state name, total days, item count, completed-contribution days, ongoing-contribution days, mean days per item, median days per item, and partial-window-item count.
- Given the filter changes (date range, work-item-type chip, blocked toggle), the chart refetches and re-renders with the recomputed bars — no manual refresh; same lifecycle as existing Flow Metrics widgets.
- Given an empty filter (no items match), the widget renders an empty-state message identical in tone to existing `WorkDistributionChart` and `WorkItemAgingChart` empty states.
- Given a workflow state with zero contributing items, that state's bar renders as a labeled placeholder with height 0 (so users see which states are part of the workflow definition, not "did the chart drop a state?").
- The widget's RAG rule (computed via a new `computeCumulativeStateTimeRag` function in `ragRules.ts`) maps to status guidance in `widgetInfoMetadata.ts`: `sustain` when one state has ≤ 40% of total cumulative time (balanced flow); `observe` when one state has 40-60%; `act` when one state has > 60% (single dominant constraint).
- No regression in the rest of the Flow Metrics category: existing widgets render unchanged when the new widget is absent.

### US-02 — Cumulative time-per-state bar chart on the portfolio detail page

**Story**: As a `delivery-lead-rte` running a portfolio-level improvement conversation, I want the same Cumulative Time per State chart on the portfolio detail page, so I can name constraints at the portfolio level the same way I name them at team level — without per-scope parity holes.

**Job-id**: `job-delivery-lead-spot-workflow-constraint`

#### Elevator Pitch

Before: even if the chart ships at team scope, portfolio-level retros and leadership reviews would have to derive the answer from per-team data and add it up manually — defeating the integrated-tool advantage and breaking parity with how the existing Flow Metrics category treats both scopes uniformly.
After: open `/portfolios/{portfolioId}` → Flow Metrics tab → see the same `stateTimeCumulative` widget, computed across all work items in the portfolio's scope (subject to the portfolio's existing filter primitives). Bar geometry and tooltip identical to the team-scope chart, only the scope differs.
Decision enabled: portfolio-level improvement priorities — "across all teams in this portfolio, Review is the bottleneck; let's prioritise Review-process work train-wide" — rather than "per-team Reviews are messy, let me eyeball it".

**AC**:

- Given a portfolio with `WorkItemStateTransition` data accumulated for its constituent work items, when I open `/portfolios/{portfolioId}` and switch to the Flow Metrics category, then the `stateTimeCumulative` widget renders with the same shape and behavior as the team-scope chart (US-01), scoped to the portfolio's work items.
- The portfolio-scope endpoint shares the request/response contract with the team-scope endpoint (same JSON shape, only the route prefix differs).
- All US-01 AC items (empty state, zero-contributing state, tooltip content, RAG rule, filter composition) hold for the portfolio scope unchanged.
- No regression in the rest of the portfolio's Flow Metrics category.

### US-03 — Tooltip surfaces included-items breakdown so users know which items the bar represents

**Story**: As a `delivery-lead-rte` looking at the chart for the first time and wondering "wait, which items are actually in this bar — the ones we closed this quarter, the ones still in flight, or both?", I want the tooltip to disclose the included-items breakdown by status, so I can recognise the chart's inclusion rule without leaving the chart or reading documentation.

**Job-id**: `job-delivery-lead-spot-workflow-constraint`

#### Elevator Pitch

Before: even with the chart visible, a sophisticated user immediately asks "wait, an item closed in Q1 had been in Review since December — does its FULL 50 days count, or just the bit inside Q1?" Without an in-chart answer, they distrust silently or open documentation. Either way, friction breaks the chart-glance flow.
After: hover any bar → tooltip includes one extra line `Included items: A closed in window, B still in flight (C contributors total — full durations counted per D5).` One glance answers the inclusion + attribution question.
Decision enabled: confidence in the chart's number. The user knows exactly which items were folded into the bar and that full durations were used, so they can defend the constraint claim in a leadership conversation.

**AC**:

- Given the bar tooltip from US-01 renders, then it includes an additional line of the form `Included items: A closed in window, B still in flight (C total — full durations counted)` where A is the count of D12-included items that have exited the state, B is the count of D12-included items still in this state, and C = A + B.
- When A = 0 (every contributing item is still in flight) or B = 0 (no in-flight contributors), the line still renders for consistency, showing `0` for the empty bucket.
- The counts are computed server-side and returned in the same response payload as the bar totals — no extra round-trip.
- The line is accessible to screen readers (aria-label includes the inclusion breakdown in plain language and clarifies the full-duration attribution rule).

### US-04 — Per-item drill-down on bar click

**Story**: As a `delivery-lead-rte`, when I identify a state as the constraint from the bar chart, I want to click that state's bar to see a table of the items that contributed and exactly how many days each contributed, so I can name the specific items driving the constraint when I raise it in a retrospective or improvement-planning meeting.

**Job-id**: `job-delivery-lead-spot-workflow-constraint`

#### Elevator Pitch

Before: the bar tells me "Review took 38% of our cumulative time this quarter", but I can't see *which items* drove that. To name names I would have to open each item by hand, read its state history, sum the days, and pivot in my head — friction that makes the chart a conversation-starter but not a conversation-driver.
After: click on the "Review" bar → a per-item panel opens listing each contributing item with `ID · Title · Type · Current State · Days contributed to Review`, sorted by days descending by default. The top 3 rows usually account for most of the bar; that's the retro agenda.
Decision enabled: which specific items to raise in the retro / improvement meeting ("Items #1234, #1289, and #1301 account for 70% of Review time this quarter — let's discuss those three").

**AC**:

- Given the chart is rendered (US-01 or US-02), when I click on a state's bar (or its labelled placeholder for zero-contributing states), then a per-item panel opens (implementation choice between modal dialog and expandable side-panel deferred to DESIGN).
- The panel's table has columns: Work Item ID (linkable to the existing work-item detail view), Title, Work-Item Type, Current State, Days Contributed To Selected State (the per-item portion of the bar height — full-duration per D5).
- Default sort: Days Contributed To Selected State, descending.
- Sorting by any column is supported (click column header to toggle asc/desc).
- The sum of "Days Contributed" across all rows in the panel equals the bar's height for that state (within ±0.1d rounding tolerance — sanity-check assertion in integration test).
- Closing the panel (Escape key, outside click, or explicit close control) returns to the chart view with the previous filter state intact.
- Keyboard accessible: Escape closes; Tab/Shift+Tab navigates rows; Enter on a Work Item ID row activates the work-item link.
- ARIA: panel has `role="dialog"` (or appropriate landmark for an expandable side-panel), labelled with "Items contributing to {state name}".
- Filter respect: the table reflects the active filter (type chips, date range, scope). Changing the filter while the panel is open re-fetches the per-item data (consistent with the chart's live-recompute lifecycle in US-01 AC).
- Zero-contributing case: clicking a zero-contributing-state bar opens an empty panel with the message "No items contributed to this state in the selected window."
- No regression: existing chart click handlers (other widgets in the Flow Metrics category) are unaffected.

## Wave: DISCUSS / [REF] Definition of Done

1. All 4 stories pass their ACs via integration tests (NUnit + EF InMemory + WebApplicationFactory for the new endpoints; Vitest + React Testing Library for the chart widget, tooltip, and drill-down panel).
2. Cumulative time-per-state math (D5 full-duration) verified against a fixture: a known set of items with known (state-enter, state-exit) intervals AND known in-flight items, asserting exact bar heights AND exact completed/ongoing segment heights for each state; verifying that the FULL state durations contribute regardless of whether they fall inside or outside the window.
3. Date-range INCLUSION semantics (D5, D12) verified against a fixture: items closed inside the window, items closed on day 1 of the window with pre-window state-time, items in-flight throughout the window, items still in flight today, items fully outside the window (must NOT be included). Assert exact contribution per included item.
4. Ongoing-item attribution (D6, D11) verified: in-flight items contribute their FULL `now - currentStateEnteredAt` time to the ongoing segment for their current state (no clipping to windowStart).
5. Filter composition (D7) verified: changing a type chip, date range, or scope re-fetches and re-renders correctly; no stale-cache bugs (this is why `bug-5016-cache-thread-safety` is a pre-req — same reason sibling 1 listed it).
6. Empty-state, zero-contributing-state, single-item, and state-removed-from-workflow edge cases all render gracefully per AC — for both the chart and the drill-down panel.
7. Portfolio-scope parity (US-02) verified by integration test asserting both scopes return shape-identical responses for the same underlying items.
7a. Per-item drill-down (US-04) verified: integration test asserts the panel's per-item rows for a clicked state sum (within ±0.1d) to that state's bar height; Vitest + RTL test asserts keyboard accessibility (Escape closes, Tab navigates), ARIA labelling, default sort, column-sort toggling, and filter-respect re-fetch behaviour.
8. No regression in existing Flow Metrics widgets (`computeSimplifiedCfdRag`, `computeWorkItemAgeChartRag`, etc.); existing `ragRules.test.ts` passes unchanged.
9. `dotnet build` zero warnings; `pnpm build` clean (CI parity per CLAUDE.md).
10. SonarCloud quality gate passes on PR.
11. Mutation testing (Stryker.NET for Backend; Stryker for Frontend): ≥80% kill rate for new code.
12. Docs updated: screenshot of the chart with both segments visible and a callout for the new widget; `widgetInfoMetadata.ts` learn-more URL points to a documentation page added in the same wave.

## Wave: DISCUSS / [REF] Out of scope

- **Total/mean per-item toggle in the widget** — deferred (D4). MVP ships total only; mean is in the tooltip. Fold into a follow-up if telemetry warrants.
- **Runtime toggle between full-duration and clip-to-window semantics** — out of scope. D5 locks full-duration per user redirect on 2026-05-24; a toggle would only ship if post-release telemetry shows demand from leadership-review users who want both lenses.
- **Comparing two windows side-by-side (e.g. this quarter vs last quarter on one chart)** — out of scope. The user is expected to flip the date range OR open two browser tabs. A dedicated comparison view would be a separate feature.
- **Per-item historical state breakdown (per-item → list of states)** — that is feature B2 (`work-item-state-history-view`), post-MVP. US-04's drill-down is the inverse (per-state → list of items contributing to that state); the two are complementary lenses on the same underlying transition data.
- **Cross-team or cross-portfolio aggregation** — out of scope; chart is scoped to one team or one portfolio at a time. Multi-scope is a future feature.
- **Configurable bar ordering** — out of scope. D3 picks workflow order; if users ask for descending-by-time, fold into a follow-up.
- **Configurable RAG thresholds for the new widget** — out of scope. The 40%/60% thresholds in the RAG rule are baseline defaults; if customer feedback warrants tunability, fold into a follow-up alongside the existing per-widget RAG-config feature work.
- **Changes to existing `WorkItemStateTransition` schema** — D9 explicitly preserves it. Sibling 1's DESIGN is unaffected.
- **Backfill of historical transitions before sibling slice 01 shipped** — sibling locked forward-only; this feature inherits the same constraint. The chart will be sparse for the first weeks post-release until enough transitions accumulate.
- **Time-in-blocked attribution** — blocked-time history is Epic #5074, separate mechanism. The bar chart shows time-in-state regardless of blocked status; when Epic #5074 ships, a blocked-vs-non-blocked breakdown of each bar could be added.
- **Shared per-state aggregation service with sibling F** — D10 deferred to DESIGN-time coordination, not DISCUSS scope.

## Wave: DISCUSS / [REF] WS strategy

**Type A (additive walking skeleton).** No contract change to existing endpoints. Two new endpoints per scope (one for the bar data, one for the per-state drill-down items), one new chart widget, one new drill-down panel component, one new widget-metadata entry. Walking skeleton = US-01 against the team scope only, with the new bar-data endpoint, the new widget rendering in the Flow Metrics category, and the empty / zero-state edge cases handled. US-02 = portfolio-scope parity (same chart + same endpoint shape, scope-swap). US-03 = tooltip enrichment (no new endpoint; counts piggy-back on US-01 response). US-04 = drill-down panel + new per-state items endpoint (kept separate from the bar-data endpoint to keep the bar-chart payload small for the common case where users only glance at the chart).

## Wave: DISCUSS / [REF] Driving ports

| Method | Route | Auth | Status | Change |
|---|---|---|---|---|
| GET | `/api/teams/{teamId}/metrics/cumulativeStateTime?startDate&endDate` | Authenticated | **New** | Returns `{ states: [{ state: string, workflowOrder: int, totalDays: double, completedContributionDays: double, ongoingContributionDays: double, itemCount: int, completedItemCount: int, ongoingItemCount: int, meanDays: double, medianDays: double }] }` — one entry per workflow state, ordered by `workflowOrder`. Item counts (`completedItemCount`, `ongoingItemCount`) feed US-03's tooltip line. Empty `states` array when no items match. |
| GET | `/api/portfolios/{portfolioId}/metrics/cumulativeStateTime?startDate&endDate` | Authenticated | **New** | Same shape as the team route, scoped to the portfolio (D2). |
| GET | `/api/teams/{teamId}/metrics/cumulativeStateTime/items?state=X&startDate&endDate` | Authenticated | **New (US-04)** | Returns `{ state: string, items: [{ workItemId: string, title: string, workItemType: string, currentState: string, daysContributed: double }] }` — per-item breakdown for ONE state. Ordered by `daysContributed` descending (default sort matches the panel's default; client may re-sort). Empty `items` array when no items contributed. Kept separate from the bar endpoint to keep the chart payload small for users who never drill down. |
| GET | `/api/portfolios/{portfolioId}/metrics/cumulativeStateTime/items?state=X&startDate&endDate` | Authenticated | **New (US-04)** | Same shape as the team route, portfolio-scoped. |
| GET | `/api/teams/{teamId}/metrics/cycleTimePercentiles` | Authenticated | Existing | **Unchanged** (D9). |
| GET | `/api/teams/{teamId}/metrics/ageInStatePercentiles?startDate&endDate` | Authenticated | New-via-sibling-F | **Unchanged** by THIS feature; sibling F ships it independently. |

UI surfaces touched:

- `Lighthouse.Frontend/src/components/Common/Charts/CumulativeStateTimeChart.tsx` (NEW): horizontal-bar chart widget with stacked completed/ongoing segments per bar, tooltip per D5/D6/US-03, click-to-drill-down handler per US-04.
- `Lighthouse.Frontend/src/components/Common/Charts/CumulativeStateTimeDrillDownPanel.tsx` (NEW): per-item table opened from a bar click; columns / sort / accessibility per US-04 AC. Implementation choice (modal vs side panel) deferred to DESIGN.
- `Lighthouse.Frontend/src/pages/Common/MetricsView/categoryMetadata.ts`: add `{ widgetKey: "stateTimeCumulative", size: "large" }` to `flow-metrics`.
- `Lighthouse.Frontend/src/pages/Common/MetricsView/widgetInfoMetadata.ts`: add `stateTimeCumulative` entry with description, learn-more URL, and RAG status guidance per US-01 AC.
- `Lighthouse.Frontend/src/pages/Common/MetricsView/ragRules.ts`: add `computeCumulativeStateTimeRag` function returning sustain/observe/act per the AC's % thresholds.
- `Lighthouse.Frontend/src/pages/Common/MetricsView/BaseMetricsView.tsx`: import and dispatch the new widget by `widgetKey` (mirror existing `WorkDistributionChart` / `WorkItemAgingChart` dispatch pattern).
- `Lighthouse.Frontend/src/services/Api/MetricsService.ts` (and the matching interface): add `getCumulativeStateTimeForTeam(teamId, startDate, endDate)`, `getCumulativeStateTimeForPortfolio(portfolioId, startDate, endDate)`, `getCumulativeStateTimeItemsForTeam(teamId, state, startDate, endDate)`, and `getCumulativeStateTimeItemsForPortfolio(portfolioId, state, startDate, endDate)` methods.
- `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/TeamMetricsService.cs` and `PortfolioMetricsService.cs`: add `GetCumulativeStateTimeForTeam` / `GetCumulativeStateTimeForPortfolio` methods (read `WorkItemStateTransition` rows + `WorkItem.currentStateEnteredAt`, apply D12 inclusion rule, sum FULL durations per D5, return the response shape above) plus `GetCumulativeStateTimeItemsFor{Team|Portfolio}` methods (same inclusion rule scoped to one state, returns per-item rows with `daysContributed` summed per item).
- `Lighthouse.Backend/Lighthouse.Backend/API/TeamMetricsController.cs` and `PortfolioMetricsController.cs`: add the four new endpoints.

No new top-level routes; no schema additions.

## Wave: DISCUSS / [REF] Pre-requisites

- **HARD**: `time-in-state-and-staleness` slice 01 must be **merged and shipped** so `WorkItemStateTransition` exists and connector capture is running for transitions. (Sibling 1 DISCUSS is complete; DESIGN/DELIVER not yet started — same MVP-bundle dependency note as sibling F.)
- **HARD**: `bug-5016-cache-thread-safety` (merged 2026-05-17) — the metrics cache must be thread-safe before adding a new derived metric that flows through it (same reasoning as sibling 1).
- **MEDIUM**: sibling F (`aging-pace-percentiles`) DESIGN — only relevant for the cross-feature coordination decision (D10). If F's DESIGN decides to ship a shared per-state aggregation service, this feature's DESIGN should consume it; if F's DESIGN computes independently, this feature does the same.
- The existing Flow Metrics category infrastructure (`BaseMetricsView`, `categoryMetadata`, `widgetInfoMetadata`, `ragRules`) is in place — confirmed in pre-DISCUSS code reality check.

**MVP bundle dependency note**: this feature cannot enter DELIVER until sibling 1 (`time-in-state-and-staleness`) is at least merged to main (the transitions table must exist and connector capture must be running). DESIGN of this feature CAN run in parallel with sibling 1's DELIVER and sibling F's DESIGN. This sequencing is consistent with sibling 1's locked D3 (`Slice ordering across Epic 4144: A+B1+D → F → B2 → B3 → C`) — though in practice the MVP bundle ships all three together at the end of B3.

No DISCOVER or DIVERGE artifacts exist for Epic 4144; this DISCUSS run extends the same "community-validated via Productboard + Community tags on ADO #4144" reasoning the two sibling DISCUSSes used. JTBD differentiation from siblings is the substantive new content of this feature (`job-delivery-lead-spot-workflow-constraint` is materially distinct from both sibling jobs).

## Wave: DISCUSS / [REF] Outcome KPIs

| ID | Target | Scope | Measurement |
|---|---|---|---|
| `OUT-cumulative-state-time-adoption` | ≥30% of team / portfolio detail page sessions include a view of the Cumulative Time per State widget within 6 weeks of release (giving sibling 1's transitions table 6 weeks to populate) | per_instance | Backend counter on the new endpoint: increment per call from the Flow Metrics view; sample at week 6 against total Flow Metrics page-views. |
| `OUT-cumulative-state-time-leadership-share` | Within 8 weeks of release, ≥3 community / customer mentions of "we used the Cumulative Time per State chart in our retro / leadership review" (signal of the chart's adoption in its intended high-stakes contexts, not just casual browsing) | vendor_demo_only + community reports | Slack / forum / issue-tracker label `feature-use-cumulative-state-time`; sample at week 8. |
| `OUT-cumulative-state-time-constraint-naming` | Within 12 weeks of release, ≥10% of teams with the chart adoption signal also have at least one configuration change (e.g. WIP limit adjustment, blocked-indicator change, throughput-filter rule) within 30 days of viewing the chart — proxies the "chart leads to a process change" causal chain | per_instance | Cross-correlate the `cumulativeStateTime` endpoint hits with subsequent Team/Portfolio settings PUT events on the same team; lag of ≤30 days. |

KPIs will be appended to `docs/product/kpi-contracts.yaml` at the DEVOPS handoff.

## Wave: DISCUSS / [REF] Definition of Ready — validation

| # | DoR item | Verdict | Evidence |
|---|---|---|---|
| 1 | Every story traces to a `job_id` | Pass | US-01, US-02, US-03, US-04 → `job-delivery-lead-spot-workflow-constraint` (new entry added to `docs/product/jobs.yaml` in this run, differentiated from sibling jobs). |
| 2 | Persona named & scoped | Pass | `delivery-lead-rte` primary (NEW persona file created at `docs/product/personas/delivery-lead-rte.yaml` with explicit differentiation from sibling personas — `flow-coach` per-item triage, `delivery-forecaster` forecast honesty); `flow-coach` secondary (existing persona, retro facilitation context). |
| 3 | Elevator pitch per non-`@infrastructure` story | Pass | Each US-NN has a Before/After/Decision triplet. After lines reference real entry points (`/teams/{teamId}` and `/portfolios/{portfolioId}` Flow Metrics category, bar click for US-04); sees-portions describe concrete observable output (bars, tooltips with named fields, segment shapes, RAG colours, per-item table rows for US-04); decision-enabled lines name the systemic-improvement decision the chart enables. |
| 4 | AC testable, no ambiguous outcomes | Pass | Quantified bar-height formula (D5 full-duration with D12 inclusion); explicit empty-state / zero-contributing-state / single-item / state-removed behaviour; explicit no-regression guarantee for existing Flow Metrics widgets; quantified RAG-rule thresholds (40% / 60%); explicit tooltip field list (US-01) and inclusion-breakdown field shape (US-03); explicit drill-down panel column list, sort behaviour, keyboard / ARIA, and sum-equals-bar-height sanity check (US-04). |
| 5 | Out-of-scope explicit | Pass | 11 items listed (total/mean toggle, runtime full-vs-clip toggle, comparison view, per-item historical state breakdown [feature B2], cross-scope aggregation, configurable bar ordering, configurable RAG thresholds, transition-schema changes, backfill, blocked attribution, shared aggregation service with sibling F). |
| 6 | Outcome KPIs measurable with targets | Pass | 3 KPIs, each with numeric target, scope, and measurement method. KPI 3 (`constraint-naming`) explicitly tests the causal chain "chart → process change", going beyond adoption-vanity to outcome. Drill-down click-through (US-04) is a secondary signal feeding KPI 2 (leadership-share) — not a separate KPI to avoid over-instrumentation. |
| 7 | Pre-requisites resolved | Pass (with sequencing note) | bug-5016 merged. Sibling 1 DISCUSS complete; sibling 1 DELIVER is the actual DELIVER-time blocker, NOT a DISCUSS or DESIGN blocker for this feature. Sibling F (medium pre-req) is in DISCUSS-complete state. Existing Flow Metrics category infrastructure confirmed present via code reality check. |
| 8 | Slice composition: each slice contains ≥1 user-visible story | Pass | Slice 01 ships US-01 (chart visible at team scope — value-bearing). Slice 02 ships US-02 + US-03 (portfolio parity + tooltip enrichment — both value-bearing). Slice 03 ships US-04 (per-item drill-down panel — value-bearing). No `@infrastructure`-only slices. |
| 9 | Handoff target identified | Pass | `nw-solution-architect` (DESIGN, full artifacts including cross-feature coordination note D10 and the drill-down modal-vs-side-panel implementation choice deferred from US-04); `nw-platform-architect` (DEVOPS, `outcome-kpis` only). |

**DoR overall verdict: PASSED (9/9 with evidence).**

## Wave: DISCUSS / [REF] Wave decisions summary

**Primary user need**: enable Delivery Leads / RTEs to identify the team's or portfolio's systemic workflow constraint from a single chart-glance, with evidence sufficient to defend the constraint claim in a leadership review or retrospective and to direct the next improvement-planning budget. Differentiated from siblings 1 (per-item triage badge) and F (per-item-on-aggregate-bands pace recognition): this feature answers the per-STATE-on-aggregate question that neither sibling addresses.

**Foundation investment**: zero new persistence; reuses `WorkItemStateTransition` (sibling 1) and existing `WorkItem.currentStateEnteredAt` (sibling 1). One new endpoint per scope (team + portfolio); one new chart widget; three new metadata entries (`categoryMetadata`, `widgetInfoMetadata`, `ragRules`). Backwards-compatible throughout — no schema changes, no existing-endpoint changes.

**Walking skeleton scope**: slice 01 (US-01) — team scope only, the new bar-data endpoint, the new widget rendering in the Flow Metrics category, empty/zero-state edge cases handled, no portfolio scope, no tooltip enrichment, no drill-down. Proves the data-foundation → endpoint → widget path in one slice. Slice 02 adds US-02 + US-03 (portfolio parity + tooltip). Slice 03 adds US-04 (per-item drill-down panel + per-state items endpoint).

**Feature type**: user-facing (Flow Metrics widget extension).

**Upstream changes**: **NONE for sibling 1 DESIGN**. D9 explicitly preserves `WorkItemStateTransition` schema as shipped by sibling 1 slice 01. **NONE for sibling F**. D10 flags an OPTIONAL DESIGN-time coordination opportunity (shared per-state aggregation service) but neither feature depends on the other to ship MVP-correct.

**Downstream coordination**: this is the third and final MVP-bundle feature. The MVP gate lifts when all three reach Done. Three-way DESIGN-time coordination on the per-state aggregation primitive is the only cross-feature design touchpoint identified.

## Wave: DISCUSS / [REF] Cross-MVP DESIGN coordination notes

The three Epic 4144 MVP-bundle features all consume `WorkItemStateTransition`. The DESIGN wave for the three should explicitly coordinate on the following:

1. **Shared per-state aggregation primitive (D10)**: sibling F computes per-state percentiles of age-at-state-exit for completed items; this feature computes per-state cumulative time across all items (completed + in-flight). Both walk the same transition data and group by state. A shared `IPerStateAggregationService` with two methods (`GetPerStatePercentiles(team, window)` and `GetPerStateCumulativeTime(team, window)`) would deduplicate the per-state loop and could share a materialised cache. NOT required for MVP correctness; flagged for DESIGN consideration.
2. **Materialised cache key strategy**: if a per-state cache materialises, the cache key must include (teamId-or-portfolioId, scope-type, startDate, endDate, type-filter-hash). Sibling 1 already touches the metrics cache; bug-5016 made it thread-safe; the new endpoints inherit that cache infrastructure.
3. **Date-range semantics are intentionally different across the bundle (sharper after D5 redirect)**: sibling F uses "history window" with COMPLETED items only (matches `cycleTimePercentiles` — the distribution is over historical age-at-state-exit of items that finished in the window). This feature uses "full-duration attribution with frame-based item selection" (D5 revised 2026-05-24; D12 inclusion rule) — items that touched the window during their life are included and their FULL state-durations are counted regardless of when those durations happened. DESIGN must NOT share a helper that conflates the two; the two endpoints should be named and documented to make the distinction unmistakable, since a sophisticated user will ask "why are the bar-chart numbers higher than the aging-chart bands suggest?" and the correct answer is "different question, different inclusion + attribution rule".
4. **In-flight contribution treatment**: this feature's D11 mandates in-flight contribution (their FULL current-state time, per revised D5); sibling F's per-state bands are derived from COMPLETED items only. DESIGN should ensure the two endpoints don't accidentally share a query helper that has different in-flight semantics for each — particularly important now that this feature uses full-duration attribution rather than the previously-locked clip-to-window math.
5. **Endpoint naming consistency**: sibling F's new endpoint is `/metrics/ageInStatePercentiles`; this feature's is `/metrics/cumulativeStateTime`. DESIGN should confirm both names are stable before either ships (renaming after either is consumed externally is expensive).

## Wave: DISCUSS / [REF] Changed Assumptions

Per the `Document Update (Back-Propagation)` contract in `nw-discuss/SKILL.md`, contradictions between DISCUSS findings and prior artifacts must be flagged. Unlike sibling F's DISCUSS (which corrected the README's "no bands exist today" premise), this feature's pre-DISCUSS code reality check **confirmed** the README's premise (no cumulative-time-per-state chart exists in Lighthouse today). The README does not require contradictory annotation, but for next-reader trust the README has been annotated with a quoted-block "verified" note pointing to this feature-delta — mirroring the pattern applied to `aging-pace-percentiles`'s README. The annotation is informational, not corrective: it documents that the gap claim was verified by code inspection, so a future reader does not have to repeat the inspection.

### Additions (informational, not contradictory)

**New persona**: `delivery-lead-rte` did not exist as a persona file before this DISCUSS run. The carry-over README referenced "Delivery Lead / RTE" as a persona candidate; this DISCUSS formalises it as a SSOT persona with file at `docs/product/personas/delivery-lead-rte.yaml`. Differentiated from `flow-coach` (per-item daily triage) and `delivery-forecaster` (forward-looking forecast honesty); no conflict with either.

**New job**: `job-delivery-lead-spot-workflow-constraint` did not exist as a SSOT job before this DISCUSS run. The carry-over README implied the job ("where does the team spend its time?") but didn't formalise it. This DISCUSS adds it to `docs/product/jobs.yaml` with full JTBD analysis (functional/emotional/social dimensions, four forces, opportunity score), explicitly differentiated from the two sibling jobs.

**New journey**: `docs/product/journeys/state-time-cumulative-view.yaml` created in this DISCUSS run with the full emotional-arc + steps + error-paths + design-decisions structure used by sibling F.

No DISCOVER document exists for Epic 4144 (per sibling 1's note); this DISCUSS extends the same community-validation reasoning, which has not been contradicted by any subsequent finding.

### Post-DISCUSS revision (2026-05-24) — D5 flipped, US-04 added

**Source**: user redirect after reviewing the DISCUSS output.

**Original D5 (pre-redirect)**: clip-to-window — bars showed only the time-in-state actually spent during the filter window; an item that entered Review 30 days before the window and exited 10 days in contributed 10 days to the Review bar. Rationale at the time: matched the "throughput in last quarter" mental model; avoided surprise-stretch from historical drag.

**Revised D5 (post-redirect, current)**: full-duration attribution with frame-based item selection. The window selects WHICH items count (per the new D12 inclusion rule); for each included item, the FULL state-durations contribute regardless of when they happened. Same item in the example now contributes its full 50 days. **User verbatim (2026-05-24)**: *"we pick the items that were relevant within the frame, but then look at the full time. so even if an item was closed on the 1st day of the window, if it was in progress 40 days before that, we should count the full time."*

**Rationale for the redirect**: clip-to-window under-counts the *real* cycle time of items relevant to the period and conceals the actual cost of slow states. For a leadership-review / retro audience the "real cycle pattern" framing is more actionable and harder to mislead with. The downside (bars become larger and sometimes much larger when items are stuck across window boundaries) is intentional — that's the signal the audience needs.

**New US-04 added**: per-item drill-down on bar click. Motivated by the corollary user requirement: with full-duration attribution, "which items drove this bar?" becomes the natural follow-up question. The drill-down panel lists contributing items with per-item day counts, sortable, accessible. Implementation choice (modal vs side panel) deferred to DESIGN. The drill-down also acts as the chart's "show your work" affordance — leadership can sanity-check the bar height by sum-of-rows.

**Items moved from out-of-scope to in-scope**: per-state drill-down (previously deferred to feature B2). Note: B2 (`work-item-state-history-view`) remains post-MVP for the inverse view (per-item → list of states); the two are complementary.

**Items moved from in-scope to out-of-scope**: runtime full-vs-clip toggle (added to out-of-scope as a deliberate "if telemetry demands" follow-up rather than a hidden assumption).

**New slice added**: slice-03 covers US-04 (per-item drill-down). Effort estimate: ~1–2 crafter days. Total feature effort grows from ~2 to ~3–4 crafter days. Slice ordering: 01 → 02 → 03 unchanged in dependency; all three remain within one MVP cycle.

**Cross-MVP impact of the redirect**: the date-range-semantics divergence between this feature and sibling F is now SHARPER (this feature: full-duration + frame-based inclusion; sibling F: completed-items-only with history window for the distribution). DESIGN coordination note 3 has been updated to reflect this — the two endpoints must NOT share a helper that conflates the rules.

**ADO follow-up flagged (not yet executed)**: ADO Story #5076 ("Cumulative time-per-state bar chart for filtered items") still describes US-01 well, so no rename needed. A new ADO Story for US-04 (per-item drill-down) should be added under Epic #4144 — flagged for the next /ado-sync invocation rather than auto-created.
