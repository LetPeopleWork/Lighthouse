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
| D5 | **Date-range semantics**: bars show ONLY time-in-state ACTUALLY SPENT during the filter window (clip-to-window). An item that entered "Review" 30 days before the window and exited 10 days into the window contributes 10 days to the Review bar, not 40. An item still in "Review" at window end contributes `(windowEnd - max(stateEnteredAt, windowStart))`. Matches the existing mental model for Lighthouse "throughput in last quarter" (only items closed in the window count) and avoids surprising-stretch behavior. Tooltip discloses partial-window vs full-window contribution counts. **Alternative considered**: full-duration attribution (item's entire time in state, even if most of it falls outside the window). REJECTED because it inflates bars with historical drag that current-window actions cannot influence; defeats the retro-actionable framing of the persona's job. The user's brief flagged this as the trickiest decision and asked us to make a reasonable default and let them redirect — clip-to-window is the default; if leadership-review users push for full-duration, fold into a follow-up toggle. | Locked (revisable post-release). |
| D6 | **Ongoing-items visual treatment**: each bar is stacked into TWO segments — a solid base segment for "completed contribution" (items that exited the state during the window) and a diagonally-hatched top segment for "ongoing contribution" (items still in the state at window end). User-stated requirement verbatim 2026-05-24: *"cumulative times for all items in the filter, including ongoing items"*. Hatching is the familiar Lighthouse "in progress" visual idiom (consistent with how blocked items are emphasized today). Alternatives considered: different shade (rejected — colour-blind users would lose the distinction); separate bar per segment (rejected — doubles the chart's bar count and breaks the one-bar-per-state mental model). | Locked. |
| D7 | **Filter composition**: composes UNCHANGED with existing Team/Portfolio scope, work-item-type chips, and date-range selector. No new filter primitives. Chart recomputes when any of those change. Matches sibling F's D7. | Locked. |
| D8 | **WS strategy**: Type A (additive). No contract change to existing endpoints. ONE new endpoint per scope (team + portfolio); ONE new chart widget; ONE new entry in `widgetInfoMetadata` + `categoryMetadata`. If the endpoint is absent, the chart slot is empty — no regression to existing widgets. | Locked. |
| D9 | **Sibling-data dependency on `WorkItemStateTransition`**: this DISCUSS does NOT request any schema addition to the transition table. The four fields shipped in sibling slice 01 (`workItemId`, `fromState`, `toState`, `transitionedAt`) PLUS the derived `currentStateEnteredAt` field on `WorkItem` are sufficient. The cumulative-time-per-state computation is fully expressible from those alone (formula in `journey-state-time-cumulative-view.yaml` D9 entry). **No upstream coordination with sibling 1 DESIGN required.** | Locked — explicit no-impact on sibling DESIGN. |
| D10 | **Cross-feature alignment with sibling F**: both this feature and `aging-pace-percentiles` compute per-state aggregations from `WorkItemStateTransition`. A shared per-state aggregation service / materialised cache is a candidate DESIGN-time optimization but NOT required for MVP correctness. MVP proceeds assuming independent computation per feature; DESIGN may consolidate if profiling demands it. | Locked (DESIGN-flagged, not DISCUSS-blocking). |
| D11 | **In-flight items MUST contribute**: user-stated requirement verbatim 2026-05-24. The bar height MUST include time-in-current-state for items still in flight at window end. D6 covers the visual treatment; D5 covers the date-range arithmetic for ongoing contribution. | Locked (verbatim user requirement). |

## Wave: DISCUSS / [REF] User stories with elevator pitches

### US-01 — Cumulative time-per-state bar chart for filtered items (team scope, walking skeleton)

**Story**: As a `delivery-lead-rte`, I want a horizontal bar chart on my team's detail page (under Flow Metrics) showing total time spent in each workflow state across all items currently in my filter — INCLUDING time-in-current-state for in-flight items — so I can see at a glance which workflow state is the constraint on my team's throughput this quarter.

**Job-id**: `job-delivery-lead-spot-workflow-constraint`

#### Elevator Pitch

Before: I want to know "where did our team's time go last quarter?" but no chart in Lighthouse answers it. The Simplified CFD shows volume-over-time by `StateCategory` (Doing/Done only), not per workflow state, not aggregated. To answer the question I either guess (anecdote from the retro), or I export raw history into a spreadsheet and pivot it myself.
After: open `/teams/{teamId}` → Flow Metrics tab → see a new "Cumulative Time per State" widget. Bars are one-per-state in workflow order (matching the team's kanban). Each bar is split into a solid base (time contributed by items completed during the window) and a diagonally-hatched top segment (time contributed by items still in that state at window end). Hover a bar → tooltip shows `State: Review | Total: 184d | Items: 47 (38 completed, 9 ongoing) | Mean: 3.9d`.
Decision enabled: which workflow state to target for next quarter's process-improvement investment — "Review consumed 38% of our time last quarter; that's where the improvement budget goes".

**AC**:

- Given a team with `WorkItemStateTransition` data accumulated and at least one item matching the active filter, when I open `/teams/{teamId}` and switch to the Flow Metrics category, then the `stateTimeCumulative` widget renders with one bar per workflow state in the team's workflow order.
- Each bar's height equals the sum, over all items matching the active filter, of `clip(stateExitTime, windowEnd) - clip(stateEnterTime, windowStart)` for every (enter, exit) interval of that item in that state — per D5 clip-to-window semantics.
- Each bar is visually stacked into a solid base segment (`completedContribution`: time from items that exited the state during the window) and a diagonally-hatched top segment (`ongoingContribution`: time from items still in the state at window end) — per D6.
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

### US-03 — Tooltip surfaces partial-window-attribution count to disambiguate edge cases

**Story**: As a `delivery-lead-rte` looking at the chart for the first time and wondering "wait, an item that entered Review BEFORE my window starts — how much of its Review time is in this bar?", I want the tooltip to disclose the count of items contributing PARTIAL-window time vs FULL-window time, so I can recognise edge effects without leaving the chart or reading documentation.

**Job-id**: `job-delivery-lead-spot-workflow-constraint`

#### Elevator Pitch

Before: even with the chart visible, a sophisticated user immediately asks "do you mean the FULL time the item spent in Review, or only the part that falls in my window?" Without an in-chart answer, they either trust silently (and may distrust later) or open documentation. Either way, friction breaks the chart-glance flow.
After: hover any bar → tooltip includes one extra line `Window attribution: X items partial-window, Y items full-window` (where X = items whose state-time intersects but is not fully contained in the window; Y = items whose state-time is fully inside the window). One glance answers the date-range-semantics question.
Decision enabled: confidence in the chart's number. The user trusts the bar represents what the window claims, not surprise-stretch from years past.

**AC**:

- Given the bar tooltip from US-01 renders, then it includes an additional line of the form `Window attribution: X partial, Y full` where X and Y are the integer counts of partial-window-attribution and full-window-attribution items respectively for that state.
- When X = 0 (every contributing item is fully inside the window), the line still renders for consistency, with `0 partial, Y full`.
- The counts are computed server-side and returned in the same response payload as the bar totals — no extra round-trip.
- The line is accessible to screen readers (aria-label includes the attribution counts in plain language).

## Wave: DISCUSS / [REF] Definition of Done

1. All 3 stories pass their ACs via integration tests (NUnit + EF InMemory + WebApplicationFactory for the new endpoints; Vitest + React Testing Library for the chart widget and tooltip).
2. Cumulative time-per-state math verified against a fixture: a known set of items with known (state-enter, state-exit) intervals AND known in-flight items at a known windowEnd; assert exact bar heights AND exact completed/ongoing segment heights for each state.
3. Date-range-clip semantics (D5) verified against a fixture: items entering before windowStart, items exiting after windowEnd, items fully inside, items fully outside (must contribute 0). Assert exact contributions.
4. Ongoing-item attribution (D6, D11) verified: in-flight items at windowEnd contribute exactly `windowEnd - max(currentStateEnteredAt, windowStart)` to the ongoing segment for their current state.
5. Filter composition (D7) verified: changing a type chip, date range, or scope re-fetches and re-renders correctly; no stale-cache bugs (this is why `bug-5016-cache-thread-safety` is a pre-req — same reason sibling 1 listed it).
6. Empty-state, zero-contributing-state, single-item, and state-removed-from-workflow edge cases all render gracefully per AC.
7. Portfolio-scope parity (US-02) verified by integration test asserting both scopes return shape-identical responses for the same underlying items.
8. No regression in existing Flow Metrics widgets (`computeSimplifiedCfdRag`, `computeWorkItemAgeChartRag`, etc.); existing `ragRules.test.ts` passes unchanged.
9. `dotnet build` zero warnings; `pnpm build` clean (CI parity per CLAUDE.md).
10. SonarCloud quality gate passes on PR.
11. Mutation testing (Stryker.NET for Backend; Stryker for Frontend): ≥80% kill rate for new code.
12. Docs updated: screenshot of the chart with both segments visible and a callout for the new widget; `widgetInfoMetadata.ts` learn-more URL points to a documentation page added in the same wave.

## Wave: DISCUSS / [REF] Out of scope

- **Total/mean per-item toggle in the widget** — deferred (D4). MVP ships total only; mean is in the tooltip. Fold into a follow-up if telemetry warrants.
- **Full-duration attribution semantics override** — out of scope (D5 picks clip-to-window; full-duration alternative is explicitly rejected for MVP).
- **Comparing two windows side-by-side (e.g. this quarter vs last quarter on one chart)** — out of scope. The user is expected to flip the date range OR open two browser tabs. A dedicated comparison view would be a separate feature.
- **Per-state drill-down (e.g. click "Review" bar → see which items contributed)** — that is feature B2 (`work-item-state-history-view`), post-MVP. The bar tooltip's count + mean is the deepest MVP exposure.
- **Cross-team or cross-portfolio aggregation** — out of scope; chart is scoped to one team or one portfolio at a time. Multi-scope is a future feature.
- **Configurable bar ordering** — out of scope. D3 picks workflow order; if users ask for descending-by-time, fold into a follow-up.
- **Configurable RAG thresholds for the new widget** — out of scope. The 40%/60% thresholds in the RAG rule are baseline defaults; if customer feedback warrants tunability, fold into a follow-up alongside the existing per-widget RAG-config feature work.
- **Changes to existing `WorkItemStateTransition` schema** — D9 explicitly preserves it. Sibling 1's DESIGN is unaffected.
- **Backfill of historical transitions before sibling slice 01 shipped** — sibling locked forward-only; this feature inherits the same constraint. The chart will be sparse for the first weeks post-release until enough transitions accumulate.
- **Time-in-blocked attribution** — blocked-time history is Epic #5074, separate mechanism. The bar chart shows time-in-state regardless of blocked status; when Epic #5074 ships, a blocked-vs-non-blocked breakdown of each bar could be added.
- **Shared per-state aggregation service with sibling F** — D10 deferred to DESIGN-time coordination, not DISCUSS scope.

## Wave: DISCUSS / [REF] WS strategy

**Type A (additive walking skeleton).** No contract change to existing endpoints. One new endpoint (per scope), one new chart widget, one new widget-metadata entry. Walking skeleton = US-01 against the team scope only, with the new endpoint, the new widget rendering in the Flow Metrics category, and the empty / zero-state edge cases handled. US-02 = portfolio-scope parity (same chart, scope-swap). US-03 = tooltip enrichment (no new endpoint).

## Wave: DISCUSS / [REF] Driving ports

| Method | Route | Auth | Status | Change |
|---|---|---|---|---|
| GET | `/api/teams/{teamId}/metrics/cumulativeStateTime?startDate&endDate` | Authenticated | **New** | Returns `{ states: [{ state: string, workflowOrder: int, totalDays: double, completedContributionDays: double, ongoingContributionDays: double, itemCount: int, completedItemCount: int, ongoingItemCount: int, partialWindowItemCount: int, fullWindowItemCount: int, meanDays: double, medianDays: double }] }` — one entry per workflow state, ordered by `workflowOrder`. Empty `states` array when no items match. |
| GET | `/api/portfolios/{portfolioId}/metrics/cumulativeStateTime?startDate&endDate` | Authenticated | **New** | Same shape as the team route, scoped to the portfolio (D2). |
| GET | `/api/teams/{teamId}/metrics/cycleTimePercentiles` | Authenticated | Existing | **Unchanged** (D9). |
| GET | `/api/teams/{teamId}/metrics/ageInStatePercentiles?startDate&endDate` | Authenticated | New-via-sibling-F | **Unchanged** by THIS feature; sibling F ships it independently. |

UI surfaces touched:

- `Lighthouse.Frontend/src/components/Common/Charts/CumulativeStateTimeChart.tsx` (NEW): horizontal-bar chart widget with stacked completed/ongoing segments per bar, tooltip per D5/D6/US-03.
- `Lighthouse.Frontend/src/pages/Common/MetricsView/categoryMetadata.ts`: add `{ widgetKey: "stateTimeCumulative", size: "large" }` to `flow-metrics`.
- `Lighthouse.Frontend/src/pages/Common/MetricsView/widgetInfoMetadata.ts`: add `stateTimeCumulative` entry with description, learn-more URL, and RAG status guidance per US-01 AC.
- `Lighthouse.Frontend/src/pages/Common/MetricsView/ragRules.ts`: add `computeCumulativeStateTimeRag` function returning sustain/observe/act per the AC's % thresholds.
- `Lighthouse.Frontend/src/pages/Common/MetricsView/BaseMetricsView.tsx`: import and dispatch the new widget by `widgetKey` (mirror existing `WorkDistributionChart` / `WorkItemAgingChart` dispatch pattern).
- `Lighthouse.Frontend/src/services/Api/MetricsService.ts` (and the matching interface): add `getCumulativeStateTimeForTeam(teamId, startDate, endDate)` and `getCumulativeStateTimeForPortfolio(portfolioId, startDate, endDate)` methods.
- `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/TeamMetricsService.cs` and `PortfolioMetricsService.cs`: add `GetCumulativeStateTimeForTeam` / `GetCumulativeStateTimeForPortfolio` methods (implementation reads `WorkItemStateTransition` rows + `WorkItem.currentStateEnteredAt`, applies D5 clip semantics, returns the response shape above).
- `Lighthouse.Backend/Lighthouse.Backend/API/TeamMetricsController.cs` and `PortfolioMetricsController.cs`: add the new endpoints.

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
| 1 | Every story traces to a `job_id` | Pass | US-01, US-02, US-03 → `job-delivery-lead-spot-workflow-constraint` (new entry added to `docs/product/jobs.yaml` in this run, differentiated from sibling jobs). |
| 2 | Persona named & scoped | Pass | `delivery-lead-rte` primary (NEW persona file created at `docs/product/personas/delivery-lead-rte.yaml` with explicit differentiation from sibling personas — `flow-coach` per-item triage, `delivery-forecaster` forecast honesty); `flow-coach` secondary (existing persona, retro facilitation context). |
| 3 | Elevator pitch per non-`@infrastructure` story | Pass | Each US-NN has a Before/After/Decision triplet. After lines reference real entry points (`/teams/{teamId}` and `/portfolios/{portfolioId}` Flow Metrics category); sees-portions describe concrete observable output (bars, tooltips with named fields, segment shapes, RAG colours); decision-enabled lines name the systemic-improvement decision the chart enables. |
| 4 | AC testable, no ambiguous outcomes | Pass | Quantified bar-height formula (D5 clip math); explicit empty-state / zero-contributing-state / single-item / state-removed behaviour; explicit no-regression guarantee for existing Flow Metrics widgets; quantified RAG-rule thresholds (40% / 60%); explicit tooltip field list (US-01) and attribution-count field shape (US-03). |
| 5 | Out-of-scope explicit | Pass | 11 items listed (total/mean toggle, full-duration semantics, comparison view, per-state drill-down, cross-scope aggregation, configurable bar ordering, configurable RAG thresholds, transition-schema changes, backfill, blocked attribution, shared aggregation service with sibling F). |
| 6 | Outcome KPIs measurable with targets | Pass | 3 KPIs, each with numeric target, scope, and measurement method. KPI 3 (`constraint-naming`) explicitly tests the causal chain "chart → process change", going beyond adoption-vanity to outcome. |
| 7 | Pre-requisites resolved | Pass (with sequencing note) | bug-5016 merged. Sibling 1 DISCUSS complete; sibling 1 DELIVER is the actual DELIVER-time blocker, NOT a DISCUSS or DESIGN blocker for this feature. Sibling F (medium pre-req) is in DISCUSS-complete state. Existing Flow Metrics category infrastructure confirmed present via code reality check. |
| 8 | Slice composition: each slice contains ≥1 user-visible story | Pass | Slice 01 ships US-01 (chart visible at team scope — value-bearing). Slice 02 ships US-02 + US-03 (portfolio parity + tooltip enrichment — both value-bearing). No `@infrastructure`-only slices. |
| 9 | Handoff target identified | Pass | `nw-solution-architect` (DESIGN, full artifacts including cross-feature coordination note D10); `nw-platform-architect` (DEVOPS, `outcome-kpis` only). |

**DoR overall verdict: PASSED (9/9 with evidence).**

## Wave: DISCUSS / [REF] Wave decisions summary

**Primary user need**: enable Delivery Leads / RTEs to identify the team's or portfolio's systemic workflow constraint from a single chart-glance, with evidence sufficient to defend the constraint claim in a leadership review or retrospective and to direct the next improvement-planning budget. Differentiated from siblings 1 (per-item triage badge) and F (per-item-on-aggregate-bands pace recognition): this feature answers the per-STATE-on-aggregate question that neither sibling addresses.

**Foundation investment**: zero new persistence; reuses `WorkItemStateTransition` (sibling 1) and existing `WorkItem.currentStateEnteredAt` (sibling 1). One new endpoint per scope (team + portfolio); one new chart widget; three new metadata entries (`categoryMetadata`, `widgetInfoMetadata`, `ragRules`). Backwards-compatible throughout — no schema changes, no existing-endpoint changes.

**Walking skeleton scope**: slice 01 (US-01) — team scope only, the new endpoint, the new widget rendering in the Flow Metrics category, empty/zero-state edge cases handled, no portfolio scope, no tooltip enrichment. Proves the data-foundation → endpoint → widget path in one slice.

**Feature type**: user-facing (Flow Metrics widget extension).

**Upstream changes**: **NONE for sibling 1 DESIGN**. D9 explicitly preserves `WorkItemStateTransition` schema as shipped by sibling 1 slice 01. **NONE for sibling F**. D10 flags an OPTIONAL DESIGN-time coordination opportunity (shared per-state aggregation service) but neither feature depends on the other to ship MVP-correct.

**Downstream coordination**: this is the third and final MVP-bundle feature. The MVP gate lifts when all three reach Done. Three-way DESIGN-time coordination on the per-state aggregation primitive is the only cross-feature design touchpoint identified.

## Wave: DISCUSS / [REF] Cross-MVP DESIGN coordination notes

The three Epic 4144 MVP-bundle features all consume `WorkItemStateTransition`. The DESIGN wave for the three should explicitly coordinate on the following:

1. **Shared per-state aggregation primitive (D10)**: sibling F computes per-state percentiles of age-at-state-exit for completed items; this feature computes per-state cumulative time across all items (completed + in-flight). Both walk the same transition data and group by state. A shared `IPerStateAggregationService` with two methods (`GetPerStatePercentiles(team, window)` and `GetPerStateCumulativeTime(team, window)`) would deduplicate the per-state loop and could share a materialised cache. NOT required for MVP correctness; flagged for DESIGN consideration.
2. **Materialised cache key strategy**: if a per-state cache materialises, the cache key must include (teamId-or-portfolioId, scope-type, startDate, endDate, type-filter-hash). Sibling 1 already touches the metrics cache; bug-5016 made it thread-safe; the new endpoints inherit that cache infrastructure.
3. **Date-range semantics consistency**: sibling F uses "history window" (matches `cycleTimePercentiles`). This feature uses "clip-to-window" (D5). Both are correct for their respective questions, but a DESIGN-time review should confirm the two semantics are clearly named and documented so users don't conflate them ("why does the aging chart's bands include items from before the window but the cumulative bar chart doesn't?" — answer: different questions, both correct).
4. **In-flight contribution treatment**: this feature's D11 mandates in-flight contribution; sibling F's per-state bands are derived from COMPLETED items only. DESIGN should ensure the two endpoints don't accidentally share a query helper that has different in-flight semantics for each.
5. **Endpoint naming consistency**: sibling F's new endpoint is `/metrics/ageInStatePercentiles`; this feature's is `/metrics/cumulativeStateTime`. DESIGN should confirm both names are stable before either ships (renaming after either is consumed externally is expensive).

## Wave: DISCUSS / [REF] Changed Assumptions

Per the `Document Update (Back-Propagation)` contract in `nw-discuss/SKILL.md`, contradictions between DISCUSS findings and prior artifacts must be flagged. Unlike sibling F's DISCUSS (which corrected the README's "no bands exist today" premise), this feature's pre-DISCUSS code reality check **confirmed** the README's premise (no cumulative-time-per-state chart exists in Lighthouse today). The README does not require contradictory annotation, but for next-reader trust the README has been annotated with a quoted-block "verified" note pointing to this feature-delta — mirroring the pattern applied to `aging-pace-percentiles`'s README. The annotation is informational, not corrective: it documents that the gap claim was verified by code inspection, so a future reader does not have to repeat the inspection.

### Additions (informational, not contradictory)

**New persona**: `delivery-lead-rte` did not exist as a persona file before this DISCUSS run. The carry-over README referenced "Delivery Lead / RTE" as a persona candidate; this DISCUSS formalises it as a SSOT persona with file at `docs/product/personas/delivery-lead-rte.yaml`. Differentiated from `flow-coach` (per-item daily triage) and `delivery-forecaster` (forward-looking forecast honesty); no conflict with either.

**New job**: `job-delivery-lead-spot-workflow-constraint` did not exist as a SSOT job before this DISCUSS run. The carry-over README implied the job ("where does the team spend its time?") but didn't formalise it. This DISCUSS adds it to `docs/product/jobs.yaml` with full JTBD analysis (functional/emotional/social dimensions, four forces, opportunity score), explicitly differentiated from the two sibling jobs.

**New journey**: `docs/product/journeys/state-time-cumulative-view.yaml` created in this DISCUSS run with the full emotional-arc + steps + error-paths + design-decisions structure used by sibling F.

No DISCOVER document exists for Epic 4144 (per sibling 1's note); this DISCUSS extends the same community-validation reasoning, which has not been contradicted by any subsequent finding.
