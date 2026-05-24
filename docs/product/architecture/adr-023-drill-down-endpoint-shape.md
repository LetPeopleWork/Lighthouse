# ADR-023: Per-State Drill-Down — Separate Endpoint (not expand-param on the bar endpoint), Mirrors `cycleTimePercentiles` Shape, MUI `Dialog` Following `WorkItemsDialog` Precedent

**Status**: Accepted (2026-05-24 — Morgan, interaction mode PROPOSE; bundled with ADR-022/024/025)
**Date**: 2026-05-24
**Feature**: state-time-cumulative-view (Epic 4144 MVP bundle, slice B3 — US-04 per-item drill-down)
**Decider**: Morgan (Solution Architect)

---

## Context

US-04 (added during the 2026-05-24 DISCUSS revision) introduces a per-item drill-down: clicking a bar opens a panel listing each contributing item with `daysContributed`. The DISCUSS feature-delta locks the panel's columns, default sort, keyboard accessibility, ARIA contract, and filter-respect behaviour, but explicitly defers two implementation choices to DESIGN:

1. **Endpoint shape**: a SEPARATE endpoint `GET /metrics/cumulativeStateTime/items?state=X` versus an `?expand=items` parameter on the existing `GET /metrics/cumulativeStateTime` endpoint that conditionally includes per-item rows in the response.
2. **Panel UI primitive**: MUI `Dialog` (modal) versus expandable side-panel/drawer. DISCUSS slice-03 spec mentions an "OPTIONAL ~30 min spike" to identify the established Lighthouse pattern; DESIGN resolves this now so DELIVER does not stall on a discovery moment.

ADR-022 already specifies the per-item attribution formula (`daysContributed(W, S) = Σ visitDuration(W, S, visit_i) + (inFlightDuration(W) if W.State == S AND W in-flight else 0)`) and the sanity-check invariant (`Σ per-item rows == bar's totalDays ±0.1d`). This ADR pins the surface that exposes that formula and the UI primitive that hosts it.

---

## Decision

### 1. Endpoint shape — SEPARATE endpoint

Introduce two new endpoints (one per scope), parallel to the existing bar endpoints:

```
GET /api/teams/{teamId:int}/metrics/cumulativeStateTime/items?state={stateName}&startDate&endDate
GET /api/portfolios/{portfolioId:int}/metrics/cumulativeStateTime/items?state={stateName}&startDate&endDate
```

Response:

```
{
  "state": "Review",
  "items": [
    { "workItemId": "JIRA-1234", "title": "...", "workItemType": "User Story",
      "currentState": "Review", "daysContributed": 42.0 },
    ...
  ]
}
```

`items[]` is ordered by `daysContributed` descending (matching US-04 default sort). Empty `items: []` when the selected state has zero contributors.

Authentication: `[RbacGuard(TeamRead, ScopeIdRouteKey="teamId")]` / `[RbacGuard(PortfolioRead, ScopeIdRouteKey="portfolioId")]` (existing class-level guards on the metrics controllers).

Validation: `startDate.Date <= endDate.Date` with HTTP 400 returning the existing `StartDateMustBeBeforeEndDateErrorMessage` — mirrors the existing `cycleTimePercentiles` validation pattern. Additionally, `state` is required (HTTP 400 if missing or empty). `state` is matched case-insensitively against the team's / portfolio's workflow state names; a state name not in the workflow returns HTTP 200 with empty `items: []` (graceful — covers the case where a user opens the drill-down for a zero-contributing-state placeholder bar).

### 2. Panel UI primitive — MUI `Dialog` modal, following `WorkItemsDialog` precedent

The drill-down panel is a MUI `Dialog` component named `CumulativeStateTimeDrillDownDialog.tsx`, following the established Lighthouse pattern for "table opened from a chart click":

- **Precedent**: `Lighthouse.Frontend/src/components/Common/WorkItemsDialog/WorkItemsDialog.tsx` is the in-codebase canonical pattern: opens from chart-dot clicks on `WorkItemAgingChart`, renders an MUI `Dialog` with a `DataGridBase` table inside, supports sorting via `@mui/x-data-grid` defaults, closes via Escape / explicit close button / outside click. Used by the team-detail and portfolio-detail Flow Metrics surfaces today.
- **In-codebase search confirms**: zero `Drawer*` files exist under `Lighthouse.Frontend/src/`; `Dialog` is the universal pattern across `WorkItemsDialog`, `FeedbackDialog`, `DeleteConfirmationDialog`, `WipSettingDialog`, `LicenseStatusDialog`, `ColumnOrderDialog`, `LatestReleaseInformationDialog`. The "click-on-chart-element opens an item list" UX is specifically already established by `WorkItemsDialog`.
- **Reuse leverage**: `CumulativeStateTimeDrillDownDialog` consumes `DataGridBase` (`Lighthouse.Frontend/src/components/Common/DataGrid/DataGridBase.tsx`) for the table — providing column sorting, keyboard navigation, ARIA roles, default visual style, all for free.

The dialog's contract:

```
interface ICumulativeStateTimeDrillDownDialogProps {
    state: string;
    items: ICumulativeStateTimeItemRow[];
    open: boolean;
    onClose: () => void;
}

interface ICumulativeStateTimeItemRow {
    workItemId: string;
    title: string;
    workItemType: string;
    currentState: string;
    daysContributed: number;
}
```

The dialog's responsibility is presentation only — it does NOT fetch data. The bar chart's container fetches via `MetricsService.getCumulativeStateTimeItemsForTeam` (or `…ForPortfolio`) on bar click and passes the resolved items into the dialog. This mirrors the `WorkItemsDialog` pattern (items passed in, dialog dumb).

Title format: `"Items contributing to {state} ({itemCount} items)"`. Sub-heading text shows the active window and the full-duration-attribution clarification: `"Window: {startDate} – {endDate}. Full state durations counted per item, not clipped."` — this satisfies US-04's "show your work" affordance and lifts the same explanation into the drill-down view that the bar tooltip carries (US-03).

Columns (matching US-04 AC):

| Column | Width | Sort | Cell |
|---|---|---|---|
| Work Item ID | small | yes | `<Link>` to the existing work-item detail view (`WorkItemsDialog` already does this; reuse the same link factory) |
| Title | flex | yes | text |
| Type | small | yes | text |
| Current State | small | yes | text |
| Days Contributed | small | yes (DEFAULT sort, descending) | numeric, 1-decimal |

Filter respect (US-04 AC line 9): when the active filter changes while the dialog is open, the parent re-fetches via `getCumulativeStateTimeItems…` and passes new `items[]` into the dialog. The dialog re-renders. The dialog itself does NOT subscribe to filter state — the filter coupling lives in the parent component (mirrors `WorkItemsDialog`'s data-flow).

Empty case (US-04 AC line 9): when `items.length === 0`, the dialog renders the message `"No items contributed to this state in the selected window."` in place of the table. No skeleton loader is needed because the fetch is synchronous from the user's perspective (cache typically hits; even uncached the network round-trip is sub-100ms at MVP scale).

ARIA / keyboard (US-04 AC lines 7, 8): MUI `Dialog` supplies `role="dialog"`, focus trap, Escape-closes, outside-click-closes, and Tab navigation by default. The dialog adds `aria-labelledby={titleId}` pointing at the `DialogTitle` element (matching `WorkItemsDialog`'s pattern at lines 124-127). Per-row Work Item ID is a `<Link>` element so Enter activates it.

### 3. Bar-endpoint payload — NO expand parameter

The existing `GET /api/teams/{teamId}/metrics/cumulativeStateTime` bar endpoint is NOT extended with an `?expand=items` parameter. The per-bar payload remains slim (state, totalDays, completedContributionDays, ongoingContributionDays, three counts, mean, median per ADR-022 §6). The per-item details are ONLY available via the separate endpoint, called lazily on bar click.

---

## Alternatives Considered

**Option A — `?expand=items` parameter on the bar endpoint, returning per-item rows in the same payload.**

- Pros: one endpoint instead of two; one round-trip to fetch both the bar data and (when requested) the drill-down rows.
- Cons:
  1. **Payload bloat for the 80% case.** Most users glance at the chart and never drill down. Including per-item rows in every response inflates the payload by `items_per_state × states_count` even when nobody asked for it. For a team with 50 items × 5 states = 250 rows of JSON per response, vs ~5 small state objects.
  2. **Cache key conflation.** The bar endpoint's cache key (`CumulativeStateTime_{start}_{end}`) is shared across all viewers of the team for the same window. Conditionally including `items[]` makes the cache key dependent on an extra query parameter, fragmenting cache hits between glance-users and drill-down-users.
  3. **Response shape becomes conditional.** Adding a "sometimes-present array of arrays" field to the response complicates the FE typing — the TS model would have `states[].items?: ICumulativeStateTimeItemRow[]` everywhere, with most cases `undefined`. A separate endpoint produces a separate, clean TS model.
  4. **Selective-state drill-down requires filtering on the FE.** With `?expand=items` returning items per state, clicking ONE state means iterating the response payload to extract that state's items — vs a state-scoped endpoint returning ONLY the relevant rows.
- **Rejected** for payload economy, cache cleanliness, type clarity, and selective-state efficiency.

**Option B — Separate endpoint but reuse the bar endpoint's cache key with a wider payload.**

- Pros: single cache entry per (scope, window) for both the bar and drill-down data.
- Cons: same payload-bloat objection as Option A; the cache stores the larger payload even when only the bar is needed. The two endpoints SHOULD be cached separately (different invalidation windows on bar-only vs items-detailed re-renders is acceptable; both invalidate together on sync because the post-sync hook wipes the entity's whole cache namespace).
- **Rejected** in favour of two separate cache keys (`CumulativeStateTime_…` and `CumulativeStateTime_Items_{state}_…`).

**Option C — Side-panel / drawer pattern instead of modal `Dialog`.**

- Pros: a drawer slides in from the side, leaves the chart partially visible behind it; user can compare a bar to the rows without losing chart context.
- Cons:
  1. **Zero in-codebase precedent.** Glob search confirms no `*Drawer*.tsx` exists under `Lighthouse.Frontend/src/`. Every "open a panel from a click" interaction in Lighthouse today is a `Dialog`. Introducing a one-off drawer for this feature would diverge from established convention and force the user to learn a new affordance.
  2. **Filter-respect / responsive layout complexity.** A drawer that needs to coexist with the chart's MUI grid layout has more responsive-design edge cases than a modal that occupies a known z-index above the page.
  3. **`WorkItemsDialog` solves the same problem.** It is literally a modal table-list opened by a chart dot click on the same Flow Metrics page. Re-using its visual language means the drill-down panel feels native ("oh, this is the table-from-chart pattern I already know") rather than novel.
- **Rejected** in favour of `Dialog` for in-codebase consistency.

**Option D — Inline expansion (e.g. accordion below the chart, expanding the clicked state's row list inline).**

- Pros: no separate panel; the rows appear in the same scroll context.
- Cons: pushes the chart up off-screen when the row list is tall (50+ rows). Breaks the "chart is the anchor; drill-down is the discovery" mental model. No in-codebase precedent.
- **Rejected** for layout disruption.

**Option E — Reuse `WorkItemsDialog` directly (i.e. don't create a new dialog component; pass `IWorkItem[]` and a `highlightColumn` for `daysContributed`).**

- Pros: maximum reuse; one dialog component used by `WorkItemAgingChart` (existing) and `CumulativeStateTimeChart` (new).
- Cons:
  1. `WorkItemsDialog` accepts `items: IWorkItem[]` (existing model with ~15 fields including `cycleTime`, `workItemAge`, `state`, `blockedHistory`, etc.). The drill-down rows from `getCumulativeStateTimeItems…` carry only 5 fields and the augmented `daysContributed` — mapping into the rich `IWorkItem` shape would require either pulling all `IWorkItem` fields from the backend (defeating the slim-payload rationale of separate endpoints) OR fabricating defaults for missing fields (defeating type safety).
  2. The `highlightColumn` field in `WorkItemsDialogProps` (`{ title, description, valueGetter }`) is designed for SLE-coloured numeric columns on `IWorkItem`. The drill-down's `daysContributed` is a pure numeric column without SLE colouring — different semantics.
  3. `WorkItemsDialog`'s dependencies (`IFeature` discrimination, `useTerminology`, `getStateColor`, `realisticColor`/`riskyColor`/etc.) bring SLE-trafficked styling into the drill-down panel that isn't relevant. The drill-down's table is functionally simpler.
- **Rejected** in favour of a new sibling component (`CumulativeStateTimeDrillDownDialog`) that mirrors `WorkItemsDialog`'s STRUCTURE (Dialog + DataGridBase) without inheriting its rich-item model. Code-wise, the new dialog is ~80 LOC; the duplication is trivial; the type cleanliness is worth it.

---

## Consequences

**Positive**:

- Bar-endpoint payload stays slim — no per-item rows in the 80%-glance case.
- Drill-down has its own cache namespace and its own per-state granularity, hitting cache for repeat-views of the same bar.
- TS models for the bar endpoint and the drill-down endpoint are independent; neither is conditional on a query parameter.
- UI primitive (`Dialog`) is the established codebase pattern — zero novel affordances; user learns the chart and immediately knows the drill-down without reading docs.
- Reuses `DataGridBase` for table mechanics — sorting, keyboard, ARIA, visual style all inherited.
- The component file `CumulativeStateTimeDrillDownDialog.tsx` mirrors the file-naming convention `WorkItemsDialog.tsx` already establishes (`{Domain}Dialog.tsx`).

**Negative**:

- Two new endpoints per scope (bar + items × team + portfolio = 4 new endpoints) instead of one. Trivially mitigated by reusing the existing controller scaffolding pattern (same shape, same validation, same `GetEntityByIdAnExecuteAction` dispatch).
- One additional cache key namespace (`CumulativeStateTime_Items_{state}_…`). Quantified: at MVP scale, ~5–10 cached items per team per window × ~50–200 teams instance-wide = a few thousand cache entries. Within established cache footprint.
- ~80 LOC of new dialog component that DOES share structural shape with `WorkItemsDialog`. The duplication is structural, not semantic — the two dialogs solve different problems with different data models and would diverge further over time (e.g. if `WorkItemsDialog` adds blocked-history columns and the drill-down doesn't). Per CLAUDE.md "DRY = don't repeat *knowledge*, not code", the structural similarity is not a DRY violation.

**Neutral**:

- The new dialog is testable in isolation via Vitest + RTL (open/close behaviour, table content rendering, ARIA, keyboard) — no integration with the chart needed for component-level tests. The chart-plus-dialog integration is tested separately via the chart's own test suite (click handler → dialog open → resolved items rendered).
- Future "drill-down from a different bar chart" features can adopt the same naming pattern (`{Domain}DrillDownDialog.tsx`) and consume `DataGridBase` similarly.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| The bar endpoint (`GET /metrics/cumulativeStateTime`) does NOT accept an `expand` query parameter and does NOT return per-item rows | Integration test asserts the response shape contains only the per-state aggregate fields; no `items` field |
| The drill-down endpoint requires a non-empty `state` query parameter | Integration test asserts HTTP 400 on empty/missing `state`; HTTP 200 with empty `items` on unknown state |
| The drill-down endpoint's `Σ daysContributed` over rows equals the bar endpoint's `totalDays` for the same state within ±0.1d | Integration test calls both endpoints for the same state and asserts equality |
| The drill-down dialog uses MUI `Dialog` (not a custom drawer / popover / accordion) | Vitest RTL test asserts the rendered element has `role="dialog"` (the MUI default ARIA role) |
| The drill-down dialog consumes `DataGridBase` for the table (not a hand-rolled table) | Vitest test asserts the dialog renders a `DataGridBase`-produced grid container (queryable via the existing `DataGridBase` test-id) |
| The drill-down panel does not subscribe to global filter state — it accepts `items` as a prop | Vitest test mocks the parent and asserts that mounting the dialog with `open: true` does NOT trigger any data-fetch hooks |
| ARIA: `role="dialog"`, `aria-labelledby` points at the title element, Escape closes, focus trap active | Vitest RTL + axe-style assertion against the rendered subtree |

---

## Cross-feature impact

- `time-in-state-and-staleness` (sibling 1): UNCHANGED. The drill-down endpoint reads ADR-015's transition table and ADR-016's `CurrentStateEnteredAt` field via the established repository seam; no schema or sync-path coupling.
- `aging-pace-percentiles` (sibling F): UNCHANGED. Sibling F has no drill-down endpoint (its chart's `WorkItemAgingChart` already opens `WorkItemsDialog` on dot click — different semantic). This feature's drill-down is a NEW dialog component, not a modification to `WorkItemsDialog`.
- Future "blocked-time per state" feature (Epic #5074, post-MVP): could reuse the per-state drill-down endpoint shape (`/cumulativeStateTime/items?state=X&...`) as a precedent for `/blockedTime/items?state=X&...`. The endpoint convention this ADR establishes is a reusable pattern for any per-state aggregate's drill-down.

