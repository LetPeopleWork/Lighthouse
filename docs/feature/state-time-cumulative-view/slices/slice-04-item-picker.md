# Slice 04 — In-chart item picker (scope the bars to a selected item or set)

**Feature**: `state-time-cumulative-view` (Epic 4144 MVP-bundle, slice B3)
**Stories**: US-05
**Effort estimate**: 1–2 crafter days (one new candidate endpoint pair + an `itemIds` param on the existing bar + drill-down endpoints + one new picker component + chart wiring)
**Reference class**: existing multi-select / autocomplete controls in `Lighthouse.Frontend` (work-item-type chips, team/portfolio selectors). Verify the established pattern during slice work and match it so the picker feels native.

## Goal (one sentence)

Let the user scope the cumulative-time-per-state chart from the systemic "all items" default down to a single item or a chosen set — searchable by Reference ID or Name, with parent-expand to grab a feature's child stories in one action — so they can see where THOSE items' time went (a feature retro lens for the delivery lead, an outlier deep-dive for the PO — the absorbed-B2 distribution lens, D15).

## IN scope

- New endpoint `GET /api/teams/{teamId}/metrics/cumulativeStateTime/candidates?startDate&endDate` returning `{ items: [{ workItemId, referenceId, title, workItemType, parentReferenceId? }] }` — the D12-included items selectable for the window (D17). `referenceId` + `title` are the search keys (D14); `parentReferenceId` drives parent-expand.
- New endpoint `GET /api/portfolios/{portfolioId}/metrics/cumulativeStateTime/candidates?startDate&endDate` (same shape, portfolio scope).
- Optional `itemIds` query param added to the slice-01 bar-data endpoints AND the slice-03 drill-down endpoints (both scopes); absent = all D12-included items, present = restrict to that subset. Selection does NOT bypass D12 — candidates are already the windowed set (D17).
- New service methods `GetCumulativeStateTimeCandidatesFor{Team|Portfolio}`, plus the `itemIds` subset filter threaded through the existing `GetCumulativeStateTimeFor{Team|Portfolio}` and `GetCumulativeStateTimeItemsFor{Team|Portfolio}` methods.
- New `MetricsService.ts` client methods for the candidate endpoints; `itemIds?` added to the existing bar + drill-down client methods.
- New component `CumulativeStateTimeItemPicker.tsx` — multi-select with type-to-search matching Reference ID or Name only (D14), a parent-expand action ("select all children of {parent ref}") when the search matches a parent, clear-all control. Emits the selected item-id set.
- Wiring on `CumulativeStateTimeChart.tsx`: cleared picker = systemic view (default); a selection passes `itemIds` to the bar (and, when open, the drill-down) endpoints and recomputes the bars. The adaptive unit (D16) re-chooses for the narrowed magnitude.
- RAG unchanged by selection (D18): the widget RAG continues to reflect the whole in-scope set; assert this explicitly.
- Single-item rendering (the absorbed-B2 distribution lens) verified.

## OUT scope (follow-ups, not this slice)

- Per-item state CHRONOLOGY / ordered transition timeline (D15 — dropped; revisit on demand only).
- Search by attributes other than Reference ID / Name (D14 — user-scoped the search keys deliberately).
- Saving / sharing a selection as a named view — out of scope.
- Cross-scope selection (items spanning teams/portfolios) — out of scope; the picker draws only from the current scope's candidates.

## Learning hypothesis

- **Disproves if it fails**: the B2-absorption bet (D15) — that the per-item/subset *distribution* lens is what users wanted from B2. If the picker is built but rarely used (KPI `OUT-cumulative-state-time-item-filter-usage` < 15% at week 8), the signal is that the *chronology* lens we dropped may have been the real need, and a follow-up should reconsider the timeline view.
- **Confirms if it succeeds**: that scoping the existing systemic chart (rather than building a separate per-item view) is enough to serve both the feature-retro lens and the outlier deep-dive — one chart, two zoom levels.

## Acceptance criteria

- All US-05 AC items from `feature-delta.md` apply unchanged.
- Integration test (NUnit + EF InMemory + WebApplicationFactory): the candidates endpoint returns exactly the D12-included items for the window (an item outside the window is absent — D17); the bar endpoint with `itemIds=[a,b]` returns bars summed over only a and b with full durations (D5); RAG output is identical with and without `itemIds` (D18).
- Vitest + RTL: picker filters candidates by Reference ID and by Name; parent-expand selects all children present in the candidate set; cleared picker renders the systemic view; a selection narrows the bars; the displayed unit adapts to a single short-lived item (hours, not `0.0d`); keyboard + screen-reader accessibility per AC.
- No regression in slices 01–03 acceptance: systemic chart, portfolio parity, drill-down panel, tooltip, RAG, empty/zero-state behaviour unchanged when no selection is active.
- `pnpm build` clean; `dotnet build` zero warnings; SonarCloud quality gate passes on PR; mutation testing ≥80% kill rate on new code.

## Dependencies

- **HARD**: slice 01 merged (the chart the picker scopes, and the per-state aggregation the candidate/subset queries reuse).
- **SOFT**: slice 03 (drill-down) — if slice 03 has shipped, also thread `itemIds` through its endpoints so the drill-down respects the selection; if not yet shipped, slice 03 picks up the `itemIds` param when it lands.
- **HARD**: same sibling-1 dependency — `WorkItemStateTransition` data accumulated.

## Production data requirement

Dogfood against the dev Lighthouse instance: pick a feature with several child stories, use parent-expand to select them, confirm the bars recompute to that feature's distribution; then select a single long-running item and confirm the unit adapts. Acceptance: screenshot of a parent-expanded selection + one-line caption in the PR.

## Dogfood moment (same-day)

After merge, live-demo to whoever is at the user's desk: "pick a feature, see where its stories' time went", then "now just this one slow item". The demo validates the absorbed-B2 deep-dive framing — the picker should make a single item's distribution obvious without any separate screen.

## Pre-slice SPIKE

OPTIONAL (~30 min): confirm the established multi-select/autocomplete pattern in the codebase (type-chips vs a dedicated picker) and the cheapest way to source `parentReferenceId` for candidates from the existing work-item model, so the picker matches convention and the candidate endpoint needs no schema work. Skip if precedent is clear.
