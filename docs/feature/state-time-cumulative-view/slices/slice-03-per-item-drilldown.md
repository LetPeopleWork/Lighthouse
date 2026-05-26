# Slice 03 — Per-item drill-down panel on bar click

**Feature**: `state-time-cumulative-view` (Epic 4144 MVP-bundle, slice B3)
**Stories**: US-04
**Effort estimate**: 1–2 crafter days (one new endpoint pair + one new UI panel + click wiring on the existing chart from slice 01)
**Reference class**: existing modal/dialog patterns in `Lighthouse.Frontend` (verify the chosen pattern during slice work; the codebase already has dialog and side-panel examples in settings forms and work-item detail views).

## Goal (one sentence)

Make the bar chart from slice 01 *actionable* by letting a delivery lead click any state's bar to open a panel listing exactly which items contributed how many days to that state — so the chart becomes the entry point into the retro agenda rather than just a discussion-starter.

## IN scope

- New endpoint `GET /api/teams/{teamId}/metrics/cumulativeStateTime/items?state=X&startDate&endDate` returning `{ state, items: [{ workItemId, referenceId, title, workItemType, currentState, daysContributed }] }`, ordered `daysContributed` descending. Applies the same D12 inclusion rule and D5 full-duration attribution as slice 01's bar endpoint, scoped to ONE state. (Slice 04 adds an optional `itemIds` param so the drill-down composes with the US-05 picker selection.)
- New endpoint `GET /api/portfolios/{portfolioId}/metrics/cumulativeStateTime/items?state=X&startDate&endDate` (same shape, portfolio scope).
- New service methods `TeamMetricsService.GetCumulativeStateTimeItemsForTeam(team, state, startDate, endDate)` and `PortfolioMetricsService.GetCumulativeStateTimeItemsForPortfolio(portfolio, state, startDate, endDate)` (read `WorkItemStateTransition` rows for items meeting D12 inclusion, sum FULL durations per item in the selected state, return per-item rows).
- New `MetricsService.ts` client methods: `getCumulativeStateTimeItemsForTeam(teamId, state, startDate, endDate)` and `getCumulativeStateTimeItemsForPortfolio(portfolioId, state, startDate, endDate)`.
- New component `Lighthouse.Frontend/src/components/Common/Charts/CumulativeStateTimeDrillDownPanel.tsx` — per-item table with columns (Work Item ID linkable, Title, Type, Current State, Days Contributed), default sort by Days Contributed desc, per-column sort toggle, keyboard navigation, ARIA labelling. Implementation choice between modal dialog and expandable side-panel made during slice work (validate against existing codebase patterns; pick the one that matches established convention).
- Click handler wired on the existing `CumulativeStateTimeChart.tsx` bars (added in slice 01) to open the panel with the clicked state.
- Filter respect: the panel re-fetches when the active filter changes while open (matches the chart's live-recompute lifecycle).
- Sanity-check assertion in the integration test: the sum of `daysContributed` across rows for a state equals the bar's `totalDays` from slice 01's endpoint (within ±0.1d tolerance).
- Vitest test: panel opens on click, keyboard accessibility (Escape closes, Tab navigates), ARIA dialog role, default sort, column-sort toggling.
- Empty case: clicking a zero-contributing-state bar opens an empty panel with the message from US-04 AC ("No items contributed to this state in the selected window.").
- Integration test on both endpoints asserting they return shape-identical responses for the same underlying items at team vs portfolio scope.

## OUT scope (post-MVP follow-ups, not this slice)

- Inline editing of work items from the panel — out of scope, use the existing work-item detail page.
- Bulk actions on selected items — out of scope.
- Per-item state-time CHRONOLOGY (the ordered timeline / re-entry sequence for one item) — dropped from MVP (D15, former feature B2's chronology lens). Note the per-item DISTRIBUTION (where one item's time went, per state) is NOT out of scope — it ships via the US-05 picker single-item selection in slice 04. US-04's drill-down here is the inverse (one state → its items).
- Exporting the panel data to CSV — out of scope (the user can sort by column and screenshot).
- Cross-state comparison (e.g. select multiple bars at once) — out of scope.
- Configurable column visibility — out of scope (D-level decision for a follow-up).

## Learning hypothesis

- **Disproves if it fails**: that the chart-glance + drill-down combination genuinely makes the chart "retro-actionable" rather than just informative. If delivery leads view the chart but rarely drill down (telemetry signal: chart view count vs panel open count), the bar tooltip alone may be sufficient — meaning US-04 was over-engineered and slice 03 could have been deferred to a follow-up. The fix would be to ship the chart + tooltip (slices 01 + 02), measure adoption for 6 weeks, then decide on drill-down based on actual demand.
- **Confirms if it succeeds**: that "which items?" is the natural follow-up question to "which state?", and that surfacing the answer in the same view (rather than asking users to open each item by hand) is the difference between a chart that drives a retro agenda and a chart that merely informs one.

## Acceptance criteria

- All US-04 AC items from `feature-delta.md` apply unchanged.
- Integration test (NUnit + EF InMemory + WebApplicationFactory): given a known fixture of items with known state-transition intervals, when the per-state items endpoint is called for state X, then the response's `items[]` contains every D12-included item that has time in state X with the exact `daysContributed` per item, AND `sum(daysContributed) ≈ totalDays` from the bar endpoint for the same state (±0.1d tolerance).
- Vitest + React Testing Library tests: panel opens on bar click, closes on Escape, closes on outside click; keyboard Tab/Shift+Tab navigates rows; Enter on a work-item ID activates the link; ARIA role is `dialog` (or appropriate landmark) with the correct aria-label; default sort is `daysContributed` descending; clicking any column header toggles sort.
- No regression in slice 01 / 02 acceptance: existing bar tooltip and click behaviours on other Flow Metrics widgets unchanged.
- `pnpm build` clean; `dotnet build` zero warnings; SonarCloud quality gate passes on PR; mutation testing ≥80% kill rate on new code (whole-slice combined).

## Dependencies

- **HARD**: slice 01 of THIS feature merged (provides the chart that the panel attaches to, and the per-state aggregation infrastructure the new endpoints reuse).
- **NONE on slice 02**: slice 03 can ship before or after slice 02. The two slices are functionally independent (portfolio parity vs drill-down panel); they touch different components and different endpoints. The MVP gate waits for all three.

## Production data requirement

Dogfood against the dev Lighthouse instance: pick the most-populated team, open the chart, click the tallest bar, screenshot the resulting drill-down panel. Acceptance: screenshot + one-line caption in PR description confirming the top-3 items in the panel match the user's intuition for "items stuck in that state".

## Dogfood moment (same-day)

After merge, do a 5-minute live demo to whoever is at the user's desk: open the chart, ask them "guess which item is dragging Review the most?", click the Review bar, compare their guess to the top row. The demo's purpose is to validate the "retro-actionable" framing — the panel should make the answer obvious and discussable, not require interpretation.

## Pre-slice SPIKE

OPTIONAL (~30 min): if the modal-vs-side-panel choice isn't obvious after a quick scan of the existing codebase, run a half-hour spike to identify the established Lighthouse pattern for "opens from a chart, lists rows, can be dismissed". Pick whichever pattern has more in-codebase precedent so the panel feels native to the app rather than a bespoke component. If the codebase has clear precedent in either direction, skip the spike.
