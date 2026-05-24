# ADR-022: Cumulative State-Time — Full-Duration Attribution Algorithm, D12 Inclusion Rule, and Stacked Completed-vs-Ongoing Segment Computation

**Status**: Accepted (2026-05-24 — Morgan, interaction mode PROPOSE; bundled with ADR-023/024/025 as the four DESIGN decisions for `state-time-cumulative-view`)
**Date**: 2026-05-24
**Feature**: state-time-cumulative-view (Epic 4144 MVP bundle, slice B3)
**Decider**: Morgan (Solution Architect)

---

## Context

DISCUSS revised D5 on 2026-05-24 (user redirect) from "clip-to-window" to **full-duration attribution with frame-based item selection**. D11 mandates that in-flight items contribute their FULL `now - currentStateEnteredAt` time. D12 specifies the item-inclusion rule: an item is included iff (a) any `(stateEnter, stateExit)` interval intersects the window OR (b) it is currently in-flight with `currentStateEnteredAt ≤ windowEnd`. D6 splits each bar into a solid `completedContribution` segment and a hatched `ongoingContribution` segment.

DESIGN must pin down precisely:

1. **What counts as a "contribution"** — what `(workItem, state)` pair contributes how many days to which segment?
2. **What is the item-membership rule** — concretely how to evaluate D12 in SQL/LINQ against `WorkItemStateTransition` + `WorkItem.CurrentStateEnteredAt`?
3. **What is the duration formula** — per visit, per item, per segment?
4. **What is the segment-split rule** — when does a contribution land in `completedContribution` vs `ongoingContribution`?
5. **What is the per-item attribution for the US-04 drill-down** — `daysContributed` per item per state?
6. **What is the empty / zero-state behaviour** at the API boundary?

Available primitives from sibling ADR-015 / ADR-016:

- `WorkItemStateTransition { Id, WorkItemId, FromState, ToState, TransitionedAt }` with index `(WorkItemId, TransitionedAt)`.
- `WorkItem.CurrentStateEnteredAt: DateTime?` (sync-time persisted; updated only by `WorkItemService.RefreshWorkItems`).
- `WorkItem.State: string`, `WorkItem.StateCategory: StateCategories` (existing).
- `IWorkItemStateTransitionRepository.GetAllByPredicate(Expression<Func<WorkItemStateTransition, bool>>)` (sibling 1).
- Existing `WorkItemBase.GetDateDifference` day-counting convention (date-only diff, ceiling).

Sibling F's ADR-019 commits to a fundamentally different aggregation: **completed-items-only** with **ClosedDate-in-window** membership and **visit-level** sampling for a percentile distribution. The DISCUSS Cross-MVP coordination note 3+4 and ADR-019's "Cross-feature impact" section both flag the divergence explicitly. DESIGN must keep the two computations from sharing a helper that would silently conflate their rules.

---

## Decision

### 1. Item-membership rule (D12 concretised)

A work item `W` is **included** in the cumulative-state-time computation for window `[windowStart, windowEnd]` iff EITHER of the following holds:

```
(a) ∃ transition pair (entry_i, exit_i) for W, ordered by TransitionedAt,
    such that entry_i.TransitionedAt ≤ windowEnd
            AND exit_i.TransitionedAt   ≥ windowStart
    (i.e. some completed visit's [stateEnter, stateExit] interval intersects the window)

OR

(b) W is currently in-flight (W.StateCategory != StateCategories.Done)
    AND W.CurrentStateEnteredAt is non-null
    AND W.CurrentStateEnteredAt ≤ windowEnd
    (i.e. the item is still in-flight at windowEnd in the state it entered ≤ windowEnd)
```

Concretely, the EF-friendly query composition (illustrative — software-crafter selects the exact LINQ shape at GREEN):

```
// Step 1: candidate items by transition intersection (a)
var candidatesByTransitionIntersection =
    workItemRepository.GetAllByPredicate(w => w.TeamId == team.Id)
        .Where(w => transitionRepository
                       .GetAllByPredicate(t => t.WorkItemId == w.Id)
                       .Any(t => t.TransitionedAt <= windowEnd)
              && transitionRepository
                       .GetAllByPredicate(t => t.WorkItemId == w.Id)
                       .Any(t => t.TransitionedAt >= windowStart));

// Step 2: candidate items by in-flight rule (b)
var candidatesInFlight =
    workItemRepository.GetAllByPredicate(w =>
        w.TeamId == team.Id
        && w.StateCategory != StateCategories.Done
        && w.CurrentStateEnteredAt != null
        && w.CurrentStateEnteredAt <= windowEnd);

// Step 3: union by Id
var includedItemIds = candidatesByTransitionIntersection
    .Select(w => w.Id)
    .Union(candidatesInFlight.Select(w => w.Id))
    .ToHashSet();
```

The OR-composition deliberately admits items that satisfy ONLY (b) — newly-started items that have NO completed transitions in the window are still counted via their current state's `now - currentStateEnteredAt` contribution. This matches DISCUSS US-01 AC line 2 verbatim ("includes time-in-current-state for in-flight items").

### 2. Per-visit duration formula (completed visits)

For every included item `W` and every **completed visit** of `W` through state `S` (i.e. an `(entryTransition, exitTransition)` pair where `entryTransition.ToState == S`, the next transition by `TransitionedAt` has `FromState == S`, and that exit transition exists in `W.Transitions`), the visit's contribution to `S` is:

```
visitDuration(W, S, visit_i) = exitTransition_i.TransitionedAt - entryTransition_i.TransitionedAt
```

Day-counting convention: project's existing `WorkItemBase.GetDateDifference` (date-only diff, ceiling — same as `WorkItemAge` and same as ADR-019's per-state percentile). Software-crafter selects the exact call shape at GREEN.

**Crucially, the visit duration is NOT clipped to `[windowStart, windowEnd]`.** Per D5 full-duration attribution, the visit's FULL duration counts regardless of whether the `(entry, exit)` interval falls inside or outside the window. The window is a membership filter, not a duration clamp.

**Re-entries to the same state contribute multiple observations.** An item that bounced into Review three times contributes three independent visit-durations to the Review bar. This matches sibling F's visit-level semantics (ADR-019) and the DISCUSS "rework is part of the bar's height" expectation.

### 3. Per-item in-flight duration formula (US-01 ongoing segment)

For every included item `W` that is currently in-flight (`W.StateCategory != StateCategories.Done`), the in-flight contribution to `W.State` is:

```
inFlightDuration(W) = now - W.CurrentStateEnteredAt
```

where `now` is the request handler's `DateTime.UtcNow` snapshot taken at the start of the computation (single snapshot per request — passed into the helper, NOT read inside the loop, to keep the computation deterministic within a single response).

Day-counting convention: same `GetDateDifference` rule. Same NOT-clipped semantics per D11+D5.

The in-flight contribution lands in the `ongoingContribution` segment for `W.State` (D6).

### 4. Segment-split rule (D6 — completed vs ongoing)

For each state `S`:

```
completedContribution[S] = sum over included items W of:
    sum over completed visits visit_i of W through S of:
        visitDuration(W, S, visit_i)

ongoingContribution[S] = sum over included items W where W.State == S AND W is in-flight of:
    inFlightDuration(W)

totalDays[S] = completedContribution[S] + ongoingContribution[S]
```

**Invariant**: the two segments are disjoint by construction — a completed visit contributes only to `completedContribution`; an in-flight item's current-state contributes only to `ongoingContribution`. An item can contribute to both segments of the SAME state (e.g. an item that bounced into Review, back out, and is now back in Review — two completed-visit contributions + one ongoing-visit contribution).

### 5. Per-item attribution for US-04 drill-down (`daysContributed` per item per state)

For the drill-down endpoint, for each included item `W` and a SELECTED state `S`:

```
daysContributed(W, S) =
    sum over completed visits visit_i of W through S of:
        visitDuration(W, S, visit_i)
    + (inFlightDuration(W) if W.State == S AND W is in-flight else 0)
```

i.e. per-item `daysContributed` is the per-item, per-state portion of the bar height. The sanity-check invariant from US-04 AC ("sum of per-item rows equals the bar's totalDays within ±0.1d tolerance") holds by construction — both sides of the sum are computed by the SAME formula, just summed in different orders.

The drill-down endpoint returns one row per item that has `daysContributed > 0` for the selected state. Items with zero contribution to the selected state (e.g. an included item whose only completed visits are through OTHER states) are omitted.

### 6. Counts feeding US-03 tooltip

For each state `S`:

```
completedItemCount[S] = count of distinct included items W that have ≥1 completed visit through S
ongoingItemCount[S]   = count of included items W where W.State == S AND W is in-flight
itemCount[S]          = count of distinct included items W contributing to S
                       (i.e. items with completedContribution[S, W] > 0 OR ongoingContribution[S, W] > 0)
```

Per DISCUSS Cross-MVP note 3, `completedItemCount[S] + ongoingItemCount[S]` MAY exceed `itemCount[S]` because an item can be BOTH (re-entered Review and is now in Review) — the tooltip formula uses `itemCount[S]` (distinct) as the headline number with `completedItemCount` and `ongoingItemCount` shown as the breakdown counts. The US-03 line `Included items: A closed in window, B still in flight (C total — full durations counted)` uses A = `completedItemCount[S]`, B = `ongoingItemCount[S]`, C = `itemCount[S]` (distinct). The tooltip explicitly documents the non-additive case via the wording "X closed AND Y still in flight" rather than "X + Y total".

### 7. Mean and median per item

For each state `S`, given the list `perItemContributions[S] = [daysContributed(W, S) for W in includedItems with daysContributed > 0]`:

```
meanDays[S]   = perItemContributions[S].Average()           (or 0 when the list is empty)
medianDays[S] = the median of perItemContributions[S]       (or 0 when the list is empty)
```

Median uses the standard 50th-percentile-with-clamp rule via reuse of `PercentileCalculator.CalculatePercentile(values, 50)` — algorithmic parity with sibling F (ADR-019 DDD-3) and with the existing `cycleTimePercentiles`.

### 8. Workflow state ordering

States are returned in the team's (or portfolio's) workflow `doingStates` order — the same order the existing `WorkItemAgingChart` X-axis uses for its state columns. The frontend renders the bars left-to-right in that order (DISCUSS D3).

`StateCategories.Done` is INCLUDED in the bar chart only if an item has a completed visit through a state mapped to `Done` within the window's transitions — in practice this corresponds to terminal-state contributions and is uncommon. For the MVP, the bar chart renders bars for `Doing`-category states; `Done`-category contributions are aggregated into the bar for the team's `doneStates.First()` if any (effectively "exit to Done" is one bar). This matches the existing chart's X-axis composition.

Alternatives considered: rendering each `Done`-state as a separate bar (rejected — DISCUSS D3's "matching the team's kanban" ordering treats Done as a single column; multiple Done columns would not match the team's mental model). Omitting `Done` entirely (rejected — items spending time in a Done state before being archived contribute legitimately).

### 9. Empty / zero-contributing-state behaviour at the API

- **Zero included items** (filter yields nothing): return `states: []`. The chart's "no items match the filter" empty-state message (DISCUSS US-01 AC line 5) is driven by the empty array.
- **Zero contributions for a state** (a workflow state with no contributing item — e.g. a brand-new state nobody has entered yet): the state appears in the response with `totalDays: 0`, `completedContributionDays: 0`, `ongoingContributionDays: 0`, all counts `0`, `meanDays: 0`, `medianDays: 0`. The chart renders a labelled placeholder with bar height 0 (DISCUSS US-01 AC line 6).
- **Zero contributions for a state in the drill-down endpoint**: the items endpoint returns `items: []` with the state name echoed. The drill-down panel renders the US-04 empty-case message ("No items contributed to this state in the selected window.").

No backend threshold suppresses returned states. Suppression / annotation is purely presentational on the FE — same rule sibling F established (ADR-019 #5).

### 10. Caching policy

Reuse the existing `BaseMetricsService.GetFromCacheIfExists` pattern with cache keys:

```
CumulativeStateTime_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}        // bar data
CumulativeStateTime_Items_{state}_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}   // per-state drill-down
```

Scoped per-Team / per-Portfolio matching the existing per-entity cache key namespace (same scoping convention as `cycleTimePercentiles`, `AgeInStatePercentiles_…`). Cache is invalidated by the existing post-sync invalidation hook (the same one that invalidates other per-state metric keys).

The drill-down cache key includes the selected state name so a user opening then re-opening the same state's drill-down panel hits the cache. State names are sanitised in the cache key (whitespace and special characters replaced) — software-crafter chooses the exact sanitisation at GREEN; the architectural requirement is "the cache key is a stable function of the request inputs".

### 11. Determinism note for in-flight contributions

`now` is captured ONCE per request at the start of the helper (passed as a parameter, NOT read inside the loop). Within the same request, all in-flight contributions are computed against the same `now` snapshot. Across requests, `now` advances — so a refresh 5 minutes later legitimately shows ongoing segments 5 minutes taller for in-flight items, which is the user-correct behaviour. The cache TTL (existing `refreshRateInMinutes` from `BaseMetricsService`) bounds the staleness; cache invalidation on sync wipes the bars immediately when transitions change.

---

## Alternatives Considered

**Option A — Clip durations to the window (reverted DISCUSS draft).**

- Pros: bars describe "time spent in this state DURING the window"; never larger than the window's day count × itemCount; matches the original DISCUSS framing.
- Cons: explicitly rejected by user on 2026-05-24. Under-counts the real cycle time of items relevant to the period; conceals the actual cost of slow states. Verbatim user redirect: *"we pick the items that were relevant within the frame, but then look at the full time. so even if an item was closed on the 1st day of the window, if it was in progress 40 days before that, we should count the full time."*
- **Rejected** by DISCUSS revision. Not in scope to re-litigate at DESIGN.

**Option B — Item-level (not visit-level) attribution: one contribution per (item, state) summed across all visits.**

- Pros: simpler conceptual model; per-item totals are easier to relate to per-item drill-downs.
- Cons: hides re-work. An item that bounced into Review 3× (2 + 1 + 5 days = 8 days) would contribute one summed number to Review — visit-level sums to the same 8 days, so the bar-height arithmetic is identical. BUT the visit-level computation is the natural intermediate representation for the segment-split (each completed visit contributes to `completedContribution`; only the current-state-not-yet-exited visit contributes to `ongoingContribution`). The per-visit walk is needed for the segment-split regardless; item-level summation would still emerge as the outer fold.
- **Accepted as the outer presentation**, **visit-level as the inner computation.** The bar's `totalDays[S]` IS effectively item-level (sum across items of per-item contributions); the segment-split requires visit-level (to distinguish completed visits from the current in-flight visit). The drill-down's `daysContributed(W, S)` is per-item per-state. Visit-level is the right primitive; item-level totals are derived from it.

**Option C — Cap in-flight contributions at the window's day-count (e.g. an item in flight for 100 days, window is 30 days → cap at 30).**

- Pros: bars never exceed the window's possible "calendar time"; intuitive upper bound.
- Cons: violates D5 full-duration attribution. The whole point of D5 is to SURFACE that an item has been in a state for 100 days even within a 30-day window. Capping reintroduces the clip-to-window semantics under a different name.
- **Rejected** as a re-litigation of the DISCUSS revision.

**Option D — Exclude in-flight items from contribution; render only completed visits.**

- Pros: sibling F's exact rule; consistent across the two MVP features.
- Cons: violates D11 ("in-flight items MUST contribute" — verbatim user requirement) and contradicts the user's "cumulative times for all items in the filter, INCLUDING ongoing items" framing.
- **Rejected** as inconsistent with DISCUSS lock.

**Option E — Render `Done`-state contributions as separate bars per terminal state.**

- Pros: explicit visibility of "time spent in Done before archival".
- Cons: DISCUSS D3 picks workflow ordering matching the team's kanban; teams typically render `Done` as a single column. Multiple Done bars break the chart's correspondence with the kanban's mental model. Done-state contributions are uncommon and small at MVP scale (items typically close on entry to a terminal state).
- **Rejected** in favour of aggregating Done contributions into the team's `doneStates.First()` bar. If post-release telemetry shows users want per-terminal-Done breakdown, fold into a follow-up.

**Option F — `now` read inside the loop per in-flight item (rather than a single snapshot).**

- Pros: per-item `now` would be marginally more "accurate" in a millisecond sense.
- Cons: non-deterministic. Two in-flight items captured 1ms apart would show different `inFlightDuration` values; the per-item drill-down's sum-equals-bar-height invariant would fail under sub-second jitter. Day-resolution rounding via `GetDateDifference` masks this in practice but the determinism guarantee is cheap and worth keeping.
- **Rejected** for determinism. Single `now` snapshot per request.

---

## Consequences

**Positive**:

- Item-membership (D12), per-visit / per-item duration (D5), segment-split (D6), in-flight attribution (D11), per-item drill-down (US-04), counts (US-03), and empty/zero-state behaviour are explicit and aligned with the DISCUSS revision.
- The bar-height arithmetic and the drill-down arithmetic are explicitly the SAME formula summed in different orders — the US-04 AC "sum of per-item rows = bar height ±0.1d" invariant holds by construction, not by coincidence.
- The visit-level/item-level computation reuses the same day-counting convention (`GetDateDifference`) and the same percentile primitive (`PercentileCalculator`) as sibling F (ADR-019). Different aggregation semantics, same arithmetic vocabulary.
- The endpoint design surfaces ALL six tooltip fields (`totalDays`, `completedContributionDays`, `ongoingContributionDays`, `itemCount`, `completedItemCount`, `ongoingItemCount`, `meanDays`, `medianDays`) in one response — no extra round-trip for the US-03 tooltip enrichment.

**Negative**:

- The per-visit walk is O(transitions × included-items). For a team with 200 included items × 12 transitions each, ~2400 row-level operations per request. Trivially manageable at MVP scale; cache via the existing `GetFromCacheIfExists` deduplicates repeat requests.
- The D12 inclusion rule's transition-intersection query is a sub-query inside an EF predicate — software-crafter at GREEN may choose to materialise the candidate item-IDs in two steps (one query for transitions, one for in-flight items) and union in C# rather than express the OR-composition as a single LINQ expression. Either shape is acceptable; the architectural requirement is correctness against the rule above, not the specific SQL shape.
- The in-flight `now` snapshot is fresh per request — a 5-minute-apart re-render shifts ongoing segments by ~5 minutes (day-rounded to 0 days unless a midnight boundary crosses). This is the correct user-visible behaviour ("the bar grows in real time"); not a bug.

**Neutral**:

- The day-counting convention is `GetDateDifference`-style (date-only diff, ceiling). Sub-day visits round up to `1`. Identical to `CycleTime` and `WorkItemAge` and to sibling F's `ageAtStateExit` (ADR-019). Cross-feature comparability of day counts is preserved.
- The per-state cache key namespace is parallel to sibling F's (`AgeInStatePercentiles_…` vs `CumulativeStateTime_…`). Two distinct namespaces matching two distinct endpoints — the deliberate naming divergence is preserved at the cache layer.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| Item-inclusion follows D12 (union of transition-intersection AND in-flight-at-windowEnd) — items entirely outside the window are excluded | NUnit fixture test in `BaseMetricsServiceTests.cs` asserts each of the four edge cases: (closed-pre-window-no-transition-in-window) excluded, (closed-in-window) included, (started-in-window) included, (in-flight-throughout) included |
| Per-visit duration is the FULL `(exitTransition.TransitionedAt - entryTransition.TransitionedAt)` regardless of window boundaries | NUnit fixture test: an item that entered Review 30 days before window and exited 10 days into window contributes 40 days to Review, not 10 |
| In-flight contribution uses a SINGLE `now` snapshot per request (deterministic across the response) | NUnit test injects a fixed `IClock` (or equivalent test seam — software-crafter selects at GREEN) and asserts identical in-flight values across two contributing items |
| Day-counting convention: `GetDateDifference`-style (date-only diff, ceiling) — same convention as `WorkItemAge` and `cycleTimePercentiles` | NUnit test against a fixture with sub-day transitions; assert the same value the equivalent `WorkItemAge` test produces |
| Per-state cache keys use `CumulativeStateTime_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}` and `CumulativeStateTime_Items_{state}_{startDate}_{endDate}` shapes — distinct from sibling F's `AgeInStatePercentiles_…` namespace | NUnit test inspects the key passed to `GetFromCacheIfExists` matches the expected pattern; ArchUnitNET test confirms no shared cache-key constant is reused across the two endpoint families |
| Drill-down endpoint's `Σ daysContributed(W, S)` over rows equals the bar endpoint's `totalDays[S]` within ±0.1d tolerance | Integration test in `TeamMetricsControllerTests.cs` calls both endpoints with the same window+state and asserts equality |

---

## Cross-feature impact

- `time-in-state-and-staleness` (sibling 1, foundation): UNCHANGED. This ADR consumes ADR-015/016/017 primitives exactly as specified. No upstream schema change. D9 of DISCUSS held.
- `aging-pace-percentiles` (sibling F): SEMANTIC DIVERGENCE DOCUMENTED. This ADR's rules — D12 inclusion (frame intersection + in-flight-at-windowEnd) and D5 full-duration attribution (NOT clipped, INCLUDING in-flight) — are explicitly different from ADR-019's rules (ClosedDate-in-window membership; visit-level unclipped duration for COMPLETED visits only). The two endpoints (`cumulativeStateTime` vs `ageInStatePercentiles`) carry distinct names precisely because they answer distinct questions. ADR-024 (this DESIGN's ADR-018/021 disposition) prevents silent helper consolidation across the boundary.
- Future blocked-time epic (Epic #5074): orthogonal. Blocked-time attribution would slice the bars further (blocked-vs-not within each state); this ADR's bar arithmetic is the substrate that blocked-time would later subdivide.

