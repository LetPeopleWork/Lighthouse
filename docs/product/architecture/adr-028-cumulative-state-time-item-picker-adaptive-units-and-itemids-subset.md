# ADR-028: Cumulative State-Time — In-Chart Item Picker (US-05), Adaptive Display Units, `itemIds` Subset Filter + Candidate Endpoint, RAG-on-Whole-Set, and B2-Distribution Absorption

**Status**: Accepted (2026-05-26 — Morgan, interaction mode PROPOSE; AMEND pass reconciling the 2026-05-26 DISCUSS revision D13–D18)
**Date**: 2026-05-26
**Feature**: state-time-cumulative-view (Epic 4144 MVP bundle, slice B3 — slice-04 US-05 + adaptive-units across all slices)
**Decider**: Morgan (Solution Architect)
**Relationship to prior ADRs**: ADDS to (does not supersede) ADR-022 (algorithm), ADR-023 (drill-down), ADR-024 (no shared per-state service), ADR-025 (chart widget). Those four remain Accepted and authoritative. This ADR carries only the decisions introduced by the 2026-05-26 DISCUSS revision.

---

## Context

The DISCUSS wave for `state-time-cumulative-view` was revised on 2026-05-26 (decisions D13–D18), after the original DESIGN (ADR-022/023/024/025, 2026-05-24) was complete. The revision:

- **D13** withdrew US-03 (the standalone tooltip "included items" explanation line). The completed/ongoing COUNTS still ship in US-01's tooltip; the inclusion+attribution EXPLANATION moves to `widgetInfoMetadata.ts` learn-more text.
- **D14** added US-05: an in-chart multi-select item picker, searching by **Reference ID or Name only**, with **parent-expand** (match a feature/epic ref → "select all its children"). Default cleared = systemic all-items view.
- **D15** absorbed former post-MVP feature B2 (`work-item-state-history-view`): its per-item *distribution* lens lands here via US-05 single-item selection; its *chronology* lens is dropped. Adds secondary persona `product-owner` + job `job-po-deep-dive-item-state-time`.
- **D16** adopted adaptive display units: bar height computed at full calendar precision; the DISPLAY unit chosen once per render from the largest bar (minutes→hours→days→weeks), uniform across bars. Wall-clock, not business-hours.
- **D17** gated the picker candidate set by the date window: the picker selects ONLY from the D12-included items for the window.
- **D18** pinned RAG to the whole in-scope set: `computeCumulativeStateTimeRag` runs on all in-scope items and ignores the picker selection.

The original ADRs' math decisions (ADR-022 §1–§11 for D5 full-duration, D6 segment-split, D11 in-flight, D12 inclusion) are UNCHANGED. This ADR pins the new surfaces and the FE-vs-BE responsibility split the revision introduced, and re-litigates the D10 shared-aggregation question one more time now that the divergence is sharper.

---

## Decision

### 1. US-03 withdrawal — counts stay, explanation moves (D13)

The completed/ongoing item COUNTS remain in US-01's bar tooltip (they are already in the bar-endpoint payload per ADR-022 §6 — `completedItemCount`, `ongoingItemCount`, `itemCount`). The standalone "Included items: A closed, B still in flight" EXPLANATION line and the full-duration-attribution clarification are REMOVED from the per-hover tooltip and relocated to the `stateTimeCumulative` entry in `widgetInfoMetadata.ts` learn-more text — one discoverable, screen-reader-reachable place. No endpoint or contract change results; ADR-022 §6's counts computation is untouched. The chart's tooltip slot (ADR-025 §2) drops the explanation line; the `Items: {C} ({A} closed in window, {B} still in flight)` count line is retained.

### 2. US-05 item picker UI — chart-toolbar `Autocomplete`(`multiple`)+`Chip` (D14)

A NEW component `CumulativeStateTimeItemPicker.tsx` built on MUI `Autocomplete` with `multiple` and `Chip` selected-tags — the established item/feature-selection idiom already used by `ManualForecaster.tsx` (`Autocomplete` + `getOptionLabel`/`onChange`/`renderInput` + `Chip`). Placement: a chart-toolbar control rendered above the bars (passed into `CumulativeStateTimeChart` as a render-slot), visually bound to the chart it scopes.

- **Search keys**: a custom `filterOptions` matches `referenceId` OR `title` only (D14). No other attribute is a search key.
- **Parent-expand** (D14): a candidate whose `referenceId` is itself referenced by other candidates' `parentReferenceId` exposes an inline, focusable, labelled "Select all N children" action that adds those child ids (drawn from the SAME candidate set — only in-window children, per §3) in one click. The action shows the count it will add so the user is not surprised by out-of-window children being absent.
- **Default**: no selection = systemic all-items view (unchanged behaviour).
- **Accessibility** (US-05 AC): Autocomplete supplies combobox ARIA (open/close, type-to-search, multi-select with keyboard); the parent-expand action is a focusable, labelled control; empty candidate set renders a disabled/empty state consistent with the chart's own empty state.

The picker is a NEW component, not an extension of `ManualForecaster`'s Autocomplete (single-select, forecast-coupled) nor of `FilterBar` (free-text overview-name search). Reuse is of the IDIOM, not the literal component.

### 3. `candidates` endpoint + `itemIds` subset filter (D14/D17 + D17/D18 semantics)

**Candidate endpoint** (D17): a NEW `GET .../metrics/cumulativeStateTime/candidates?startDate&endDate` per scope returns exactly the D12-included items for the window + ambient filters:

```
{ "items": [ { "workItemId": 1234, "referenceId": "JIRA-1234", "title": "...",
               "workItemType": "Bug", "parentReferenceId": "JIRA-1200" }, ... ] }
```

It REUSES the same D12-inclusion query the bar endpoint runs (ADR-022 §1), then projects each item to the candidate row. `parentReferenceId` is read from the existing vendor-neutral `WorkItemBase.ParentReferenceId` (populated by every connector — Jira/ADO/Linear/CSV — and `DemoDataFactory`; already projected as `WorkItemDto.ParentWorkItemReference`), so parent-expand works uniformly with no schema change and no new driven port. Cache key `CumulativeStateTime_Candidates_{startDate}_{endDate}`.

**`itemIds` subset filter** (D17/D18): the bar and drill-down endpoints gain an OPTIONAL `itemIds` query param.

- Absent ⇒ all D12-included items (systemic view).
- Present ⇒ the SAME D12-inclusion query runs, then the result is **INTERSECTED** with `itemIds` before the per-state walk. The selection narrows the population; it NEVER bypasses inclusion. A selected id that is not in the D12 set is silently ignored (the intersection guarantees an out-of-window item cannot be smuggled in).
- Full-duration attribution (ADR-022 §2), segment-split (§4), counts (§6), mean/median (§7) all apply unchanged to the narrowed set.

The `itemIds` intersection and the candidate projection live in the derived `TeamMetricsService` / `PortfolioMetricsService` (which own the D12 query). The `BaseMetricsService` helpers (`ComputeCumulativeStateTime`, `ComputeCumulativeStateTimeItems`) stay subset-agnostic — they receive the already-narrowed `includedItems`. The `ITeamMetricsService` / `IPortfolioMetricsService` signatures gain an `IReadOnlyList<int>? itemIds` parameter on the bar + items methods and a new candidates method.

Cache: a non-empty `itemIds` selection adds a selection-hash suffix to the bar/items cache key so the systemic and narrowed responses cache independently (extends ADR-022 §10).

### 4. Adaptive display units — `formatDuration` util, FE-owned (D16)

Bar height is computed at full calendar-time precision on the backend; the backend contract stays a single canonical numeric — `totalDays` (double) — exactly as ADR-022 §2/§3 already specify. **The DISPLAY unit is a frontend concern.** A NEW pure util `Lighthouse.Frontend/src/utils/date/formatDuration.ts` chooses ONE display unit per chart render (ladder: minutes → hours → days → weeks) from the LARGEST bar's magnitude and formats every bar uniformly in that unit (so heights stay comparable). The util is used for bar labels, the value axis label, and the tooltip's primary value (the tooltip may show a finer secondary value).

Wall-clock / calendar time (NOT business-hours — matching the existing cycle-time / age charts; a working-hours calendar is a separate large feature, out of scope per D16).

**Rationale for FE-owned units**: keeping the contract numeric (`totalDays`) avoids a backend unit decision, preserves cross-endpoint comparability (sibling F and `cycleTimePercentiles` also speak days), and makes the adaptive choice a render-time presentation concern where it belongs. The exact contract numeric (precise days-double) is confirmed; seconds were considered and rejected (days is the established unit across the metrics surface).

### 5. `itemIds` transport — repeated GET query params (`int[]?`)

`itemIds` is transported as REPEATED query params (`?itemIds=12&itemIds=87&…`), bound `[FromQuery] int[]? itemIds`. The param is NULLABLE to avoid the ASP.NET model-binder required-by-default 400 (ci-learnings 2026-05-16). The endpoints STAY GET (cacheable, REST-consistent with the existing metrics endpoints). Practical selection sizes — a feature's in-window children, a handful of items, or a single item — keep the URL well under the ~2 KB safe limit. If a future bulk-select feature blows the URL budget, switching THAT endpoint to POST is a documented follow-up.

### 6. RAG reflects the whole in-scope set, never the selection (D18)

`computeCumulativeStateTimeRag` (ADR-025 §4) runs on the WHOLE in-scope set and is computed from a bar-endpoint response fetched WITHOUT `itemIds`. The frontend keeps the systemic (no-selection) bar data in `useMetricsData` ctx as the RAG source even while displaying a picker-narrowed set; the narrowed fetch drives ONLY the rendered bars, never the RAG. Locked analogy: hiding a work-item-type in the cycle-time chart does not change its RAG. The picker is a view-level lens, not a recompute of the systemic health signal.

### 7. B2 distribution absorption — single-item selection, no chronology (D15)

A single-item picker selection (`itemIds=[oneId]`) renders that one item's per-state DISTRIBUTION (one bar per state it visited) using the IDENTICAL bar arithmetic + segments + adaptive unit — no new rendering path, just the n=1 subset case of §3. This is the substantive value of former feature B2. The CHRONOLOGY lens (ordered transition timeline, re-entry sequence with dates) is explicitly NOT built (D15 out-of-scope). The feature gains secondary persona `product-owner` + job `job-po-deep-dive-item-state-time` for traceability; no extra component or endpoint is needed for the PO deep-dive.

### 8. Shared per-state aggregation (D10) — re-litigated a fourth time, STAY INDEPENDENT

The amend revisits Open-question D (shared `IPerStateAggregationService`) now that this feature has six endpoints and sibling F has shipped its per-state percentile endpoint. **Decision unchanged: uphold ADR-018 + ADR-021 + ADR-024.** The divergence is now MORE concrete, not less: this feature's `itemIds` subset narrowing has no analogue in sibling F (which has no per-item-selection notion); folding both behind one service would hide an even larger asymmetry. The cache-sharing win remains zero at MVP scale; supersession would still break three stable ADRs. The post-MVP extraction path (semantically-named methods embedding the inclusion rule) stays documented in ADR-024.

---

## Alternatives Considered

**US-05 picker placement — Option B1 (chosen): chart-toolbar `Autocomplete`+`Chip`.** Visually bound to the chart, reuses the in-codebase idiom, keyboard/SR-accessible via Autocomplete's combobox ARIA.

**Option B2: standalone dropdown menu.** Rejected — divorces the control from the chart it scopes and needs its own ARIA scaffolding that Autocomplete already provides.

**Option B3: dedicated side panel for selection.** Rejected — heavier, no codebase precedent (same Drawer-absence finding as ADR-023), over-engineered for a multi-select.

**Parent-expand as a tree control.** Rejected — over-engineered; the inline "Select all N children" row action covers "all stories of a feature" in one click without a tree widget.

**`itemIds` transport — Option C1 (chosen): repeated GET query params, `int[]?`.** Idiomatic ASP.NET array binding; GET stays cacheable + REST-consistent; small in practice.

**Option C2: comma-separated single param (`?itemIds=12,87,…`).** Rejected — requires manual parsing (no model-binder support), and gives no real URL-length advantage over repeated params for the selection sizes in play.

**Option C3: switch the bar + drill-down endpoints to POST with an `itemIds` body.** Rejected for MVP — breaks GET cacheability and the uniform `cycleTimePercentiles`-style controller pattern, for a payload that is small in practice. Reserved as a documented follow-up if a future bulk-select feature blows the URL budget on a specific endpoint.

**Adaptive units — Option E1 (chosen): FE-owned `formatDuration` util, contract stays `totalDays` double.** Keeps the contract numeric and comparable across endpoints; adaptive choice is a render concern.

**Option E2: backend returns a pre-formatted unit + value.** Rejected — pushes a presentation decision into the contract, breaks cross-endpoint numeric comparability, and would require the backend to know the full set of bars to pick the largest-magnitude unit (it does — but the FE is the right layer for a display choice; the backend would also have to re-decide on every `itemIds`-narrowed fetch).

**Option E3: extend `age.ts` or `chartAxisUtils.ts`.** Rejected — `age.ts` returns a day-COUNT with no unit string; `chartAxisUtils.ts` formats raw numbers/dates with no unit-laddering. Neither ladders units; extending either distorts its single responsibility. New `formatDuration.ts` co-located beside `age.ts`.

**Shared aggregation — Path B (`IPerStateAggregationService`).** Rejected for the fourth time; see ADR-024 Alternatives (the conflation risk, premature-abstraction, zero-cache-win, and supersession-cost arguments all hold and are reinforced by the `itemIds` asymmetry).

---

## Consequences

**Positive**:

- US-05 ships on the established `Autocomplete`+`Chip` idiom with zero new dependency; parent-expand is free across all connectors because `ParentReferenceId` already exists and is vendor-neutral.
- The `itemIds` filter is a strict post-inclusion intersection — by construction a selection can never bypass the D12 window rule (D17 enforced, not just documented).
- Adaptive units are isolated in one pure, mutation-testable util; the backend contract is untouched, so the bar/items/candidates endpoints stay numerically comparable with sibling F and `cycleTimePercentiles`.
- RAG-on-whole-set (D18) is enforced by the FE always holding the systemic response as the RAG source; the picker fetch is a separate, narrowed fetch that never feeds RAG.
- B2's distribution lens falls out for free (n=1 subset) — no new rendering path, no extra endpoint.
- The shared-aggregation decision is now backed by FOUR independent re-litigations converging on the same answer; the `itemIds` asymmetry makes the keep-separate case stronger.

**Negative**:

- Three endpoints per scope (six total) instead of two per scope; mitigated by the uniform `cycleTimePercentiles`-style controller scaffolding and the candidate endpoint reusing the existing D12 query.
- A non-empty `itemIds` selection fragments the bar/items cache (selection-hash suffix). Quantified: most sessions use the systemic view (shared cache); picker-narrowed responses are a smaller, shorter-lived cache population. Within the established footprint.
- One additional FE component (`CumulativeStateTimeItemPicker`) and one util (`formatDuration`). Both isolated, both Vitest + Stryker covered.

**Neutral**:

- The candidate endpoint and the bar endpoint share the D12 query but cache independently (no `itemIds` on candidates; it always returns the full candidate set for the window).
- The adaptive unit re-chooses per render, so a picker-narrowed view legitimately switches from days to hours for a single short-lived item — the intended D16 behaviour.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| The bar + drill-down endpoints accept an optional `itemIds` (`int[]?`, nullable) and return the systemic set when it is absent | Integration test: call without `itemIds` ⇒ systemic; call with `itemIds` ⇒ narrowed; missing param does NOT 400 |
| `itemIds` is a post-inclusion INTERSECTION — a selected id outside the D12 set is ignored, never smuggled in | NUnit test: pass an `itemIds` containing an out-of-window id; assert it does not appear in the bars |
| The `candidates` endpoint returns exactly the D12-included items for the window, projecting `parentReferenceId` | Integration test against an EF InMemory fixture with parented items across connectors |
| Parent-expand selects only in-window children (children drawn from the candidate set) | Vitest test on the picker: a parent whose children are partly out-of-window expands to the in-window subset; the shown count matches |
| `computeCumulativeStateTimeRag` is computed from the systemic (no-`itemIds`) response and does not change with a picker selection | Vitest test on `BaseMetricsView`: selection active ⇒ bars narrow, RAG unchanged |
| `formatDuration` chooses the unit from the largest bar and applies it uniformly; sub-day magnitude renders in hours/minutes | Vitest unit test on the util at each ladder boundary |
| The US-01 tooltip retains the completed/ongoing COUNTS but has NO standalone US-03 "included items" explanation line | Vitest test on the chart tooltip asserting counts present, explanation-line absent |
| No new `*PerStateAggregation*` class/interface (extends ADR-024 across the amend) | Existing ArchUnitNET test; canonical reference ADR-018 + ADR-021 + ADR-024 + ADR-028 |
| The `itemIds` intersection + candidate projection live in the derived services; the `BaseMetricsService` helpers stay subset-agnostic and `protected` | NUnit reflection test on the helper signatures (no `itemIds` param on the base helpers); ArchUnitNET test that no interface exposes them |

---

## Cross-feature impact

- `time-in-state-and-staleness` (sibling 1): UNCHANGED. The candidate projection reads `WorkItemBase.ParentReferenceId` (pre-existing, not a sibling-1 addition) and the sibling-1 transition/`CurrentStateEnteredAt` primitives exactly as ADR-022 already specified. D9 still holds — no schema change.
- `aging-pace-percentiles` (sibling F): UNCHANGED. ADR-021 stays Accepted; this ADR upholds it for the fourth time. Sibling F has no `itemIds`/picker/candidate notion; the asymmetry reinforces the keep-separate decision.
- `work-item-state-history-view` (former B2): RETIRED from Epic 4144 (D15). Its distribution lens is absorbed here via §7; its chronology lens is dropped. No B2 ADO item existed (catalog-level TBD), so no removal needed.
- Future "blocked-time per state" (Epic #5074): orthogonal; could reuse the candidate-endpoint + `itemIds` pattern this ADR establishes for its own per-state subset views.
