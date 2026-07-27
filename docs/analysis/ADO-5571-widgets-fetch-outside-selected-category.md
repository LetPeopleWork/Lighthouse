# RCA — Bug #5571: Metrics widgets fetch data even when their category is not selected

**Analyst:** Rex (Toyota 5 Whys)
**Date:** 2026-07-27
**Scope:** `Lighthouse.Frontend/src` — metrics dashboard data-fetching layer (team + portfolio)
**Status:** FIXED and shipped 2026-07-27 in `35f3524e1..34edb9d11`. Root causes identified, validated
forward and backward, then closed. Outcome summary: `docs/evolution/2026-07-27-fix-widget-eager-fetch-by-category.md`.
Note two corrections found during delivery: §8.1(2)'s positive list is incomplete (it omits
`getThroughput`, `getArrivals` and `getTotalWorkItemAge`, contradicting §5.1 — §5.1 is right), and
`estimationVsCycleTime` also depends on `workItemLookup`, which §Q5's table omits. R3 was decided as
option (a); see §7.

All line references are to the worktree
`/storage/repos/Lighthouse/.claude/worktrees/cozy-hatching-sunset`.

---

## 1. Problem statement (scoped)

Opening the Metrics view (team or portfolio) issues ~30 backend requests before anything is
rendered, regardless of which of the four widget categories is selected. Subsequently switching
category issues 0–2 requests. On slow instances the first paint is visibly delayed.

**In scope:** `useMetricsData`, `BaseMetricsView`, `categoryMetadata`, the widget components that
self-fetch.
**Out of scope (verified unrelated):** backend endpoint performance, `react-query` (used in exactly
2 places, neither in the metrics path — `LicenseStatusIcon.tsx:27`, `TerminologyContext.tsx:28`),
service-layer caching (none exists: `ctx_search "cache|Cache|memo"` over
`src/services/Api/` → 0 matches).

### 1.1 Empirical evidence (measured, not inferred)

A throwaway Vitest probe rendered `useMetricsData` with a call-recording `IMetricsService` double
and no other changes. Result:

| Owner | Distinct service calls on mount, before any widget renders |
|---|---|
| Team | **29** |
| Portfolio | **33** |

Full team call list (sorted, verbatim from the probe run):

```
blackoutPeriodService.getAll        getInProgressItems
getAgeInStatePercentiles           getMultiItemForecastPredictabilityScore
getArrivals                        getPredictabilityScoreInfo
getArrivalsInfo                    getThroughput
getArrivalsPbc                     getThroughputInfo
getBlockedCountHistory             getThroughputPbc
getBlockedItemsAtDate              getTotalWorkItemAge
getCumulativeStateTimeForTeam      getTotalWorkItemAgeInfo
getCycleTimeData                   getTotalWorkItemAgePbc
getCycleTimePbc                    getWipOverviewInfo
getCycleTimePercentiles            getWipPbc
getCycleTimePercentilesInfo        getWorkInProgressOverTime
getEstimationVsCycleTimeData       getWorkItemAgePercentiles   (×2 — current + previous period)
getFeaturesWorkedOnInfo
getFlowEfficiencyInfoForTeam
```

Portfolio = the same minus `getFeaturesWorkedOnInfo` (only on `ITeamMetricsService`,
`MetricsService.ts:264`) plus `getSizePercentiles`, `getAllFeaturesForSizeChart`,
`getFeatureSizePbc`, `getFeatureSizeEstimation`, `getFeatureSizePercentilesInfo`.

Add one more: `TotalWorkItemAgeWidget.tsx:41` self-fetches `getTotalWorkItemAge` a **second** time
when the flow-overview widget mounts. Observed totals are therefore **30 (team) / 34 (portfolio)**.

The probe file was deleted after the run; nothing in the working tree was modified.

---

## 2. The five investigation questions, answered

### Q1 — Where does the fetching happen?

**In the parent, as one un-gated fetch-all.** `useMetricsData`
(`src/hooks/useMetricsData.ts:106-113`) owns 17 `useEffect` blocks that fire on mount. Its
signature is:

```ts
export function useMetricsData<T, E>(
  entity: E,
  metricsService: IMetricsService<T>,
  startDate: Date,
  endDate: Date,
): MetricsData<T>
```

There is **no category parameter**. The hook is structurally incapable of knowing which category is
selected. It is called once, unconditionally, at `BaseMetricsView.tsx:1268`. It has exactly one
call site in the codebase.

Only three widgets fetch for themselves, and only two of those do it correctly:

| Widget | Fetch site | Trigger | Verdict |
|---|---|---|---|
| `PercentilesOverTimeWidget` | `usePercentilesOverTime.ts:59` | on mount, per-selection cache | correct (lazy) |
| `PbcOverTimeWidget` | `usePbcOverTime.ts:61` | on mount, per-family cache | correct (lazy) |
| `TotalWorkItemAgeWidget` | `TotalWorkItemAgeWidget.tsx:41` | on mount | **duplicate** of `useMetricsData.ts:215` |
| `BlockedItemsOverTimeChart` | `BlockedItemsOverTimeChart.tsx:77` | on user click | correct (lazy) |
| `ThroughputRunChartCard` / `PredictabilityScoreDetailsWidget` | `:38` / `:44` | filter-toggle callback only | correct (no mount fetch) |

### Q2 — Which first-open requests belong to widgets outside the initially-selected category?

Default category is `flow-overview` (`categoryMetadata.ts:139`, `getDefaultCategoryKey`), overridable
from `localStorage` (`useCategorySelection.ts:33-45`).

**Purely wasted on a flow-overview first open (zero flow-overview consumer):**

| Fetch | `useMetricsData.ts` | Sole consumer(s) | Category |
|---|---|---|---|
| `blackoutPeriodService.getAll` | 195-202 | `cycleScatter` (`BaseMetricsView.tsx:1015`) | flow-metrics |
| `getWorkInProgressOverTime` | 241-245 | `wipOverTime`, `totalWorkItemAgeOverTime` (`:1045`, `:1054`) | flow-metrics |
| `getAgeInStatePercentiles` | 302 | `aging` (`:1039`) | flow-metrics |
| `getCumulativeStateTimeForTeam` | 303-307 | `stateTimeCumulative` (`:1141`) | flow-metrics |
| `getEstimationVsCycleTimeData` | 373-380 | `estimationVsCycleTime` (`:1086`) | portfolio |
| `getThroughputPbc` | 477 | `throughputPbc` (`:805`) | predictability |
| `getCycleTimePbc` | 480 | `cycleTimePbc` (`:826`) | predictability |
| `getArrivalsPbc` | 481 | `arrivalsPbc` (`:840`) | predictability |
| `getWipPbc` | 478 | `wipPbc` (`:812`) + `loadBalanceMatrix` (`:1600`) | predictability + flow-metrics |
| `getTotalWorkItemAgePbc` | 479 | `totalWorkItemAgePbc` (`:819`) + `loadBalanceMatrix` (`:1601`) + `totalWorkItemAgeOverTime` RAG (`:1746`) | predictability + flow-metrics |
| *(portfolio only)* `getFeatureSizePbc` | 334 | `featureSizePbc` (`:833`) | predictability |
| *(portfolio only)* `getFeatureSizeEstimation` | 337 | `featureSize` (`:1096`) | portfolio |

**≈ 10 of 29 (34%) on a team, 12 of 33 (36%) on a portfolio, are for widgets that cannot render.**

### Q3 — Why does switching category then fire almost nothing?

**Because the data was already fetched eagerly — NOT because of caching.** These two are
distinguishable and the evidence separates them cleanly:

1. There is no cache anywhere on the metrics path. `BaseMetricsService`
   (`MetricsService.ts:303-886`) issues a bare `axios`/`fetch` per call; no memoisation
   (`ctx_search` over `src/services/Api/` for `cache|Cache|memo` → **0 matches**). `react-query`
   is in `package.json:29` and provided at `App.tsx:169`, but `useQuery` is called in exactly
   two components, neither of them metrics.
2. The results live in ~35 `useState` slots inside `useMetricsData`
   (`useMetricsData.ts:118-194`). `selectedCategory` lives in a *sibling* state
   (`BaseMetricsView.tsx:1539`, via `useCategorySelection`). Changing it re-renders
   `BaseMetricsView` but changes none of `useMetricsData`'s effect dependencies
   (`entity, metricsService, startDate, endDate`), so no effect re-runs and no state is discarded.

So "switching is cheap" is a **side effect of over-fetching**, not a cache that a lazy fix would
lose. The residual 1–2 requests the reporter saw are precisely the two correctly-lazy widget hooks:
switching to **Predictability** mounts `PercentilesOverTimeWidget` → 1 × `getPercentilesOverTime`
and `PbcOverTimeWidget` → 1 × `getProcessBehaviorOverTime` = **exactly two**. Switching to
**Flow Metrics** or **Portfolio** mounts nothing that self-fetches → **zero**. This reproduces the
report verbatim ("it does one or two requests, but not more").

### Q4 — Are all widgets mounted at once (hidden), or only the selected ones?

**Only the selected category's widgets are mounted.** Mounted-but-hidden is **NOT** the cause.

- `BaseMetricsView.tsx:1844` — `const activeWidgets = getWidgetsForCategory(selectedCategory, ownerType);`
- `:1845-1862` — `dashboardItems` is built **only** from `activeWidgets`.
- `Dashboard.tsx:135` — renders `items.map(...)`, nothing else. No hidden tab panels, no
  `display:none` siblings.

`buildWidgetNodes` (`BaseMetricsView.tsx:861-1173`) does construct a `ReactNode` for *every* widget
key, but constructing an element is not mounting it — no effects run for elements never placed in
the tree. That is exactly why `usePercentilesOverTime` stays quiet until Predictability is opened,
which is the control that proves the mechanism.

**Consequence:** the render layer is already correctly lazy. Only the data layer is not. The fix
does not need to touch rendering.

### Q5 — Which eager fetches legitimately serve multiple categories?

This is the trap in a naive fix. Several **flow-overview** RAG chips, trend arrows and view-data
tables are computed in the parent from data whose *primary* consumer is a widget in a **different**
category. Removing those fetches would silently blank a chip on the default view.

| Flow-overview surface | Reads | Sourced from | Whose "own" category |
|---|---|---|---|
| `percentiles` RAG (`:360` → `computeCycleTimePercentilesRag`, `ragRules.ts:174`) | `inputs.cycleTimes` (`:1722`) | `getCycleTimeData` (`useMetricsData.ts:294`) | flow-metrics (`cycleScatter`) |
| `totalThroughput` RAG (`:446`) and `totalArrivals` RAG (`:460`) → `computeStartedVsClosedRag` (`ragRules.ts:220`) | `startedTotal` (`:1708`), `closedTotal` (`:1709`) | `getArrivals` (`:384`), `getThroughput` (`:224`) | flow-metrics |
| `featureSizePercentiles` RAG (`:469` → `computeFeatureSizeRag`, `ragRules.ts:619`) | `sizePercentileValues` (`:1762`), `featureSizes` (`:1725`) | `getSizePercentiles` (`:328`), `getAllFeaturesForSizeChart` (`:331`) | portfolio (`featureSize`) |
| `blockedOverview` trend (`:1828`) | `blockedCountHistory` | `getBlockedCountHistory` (`:451`) | flow-metrics (`blockedCountHistory` chart) |
| `totalWorkItemAge` RAG (`:365`) | `totalWorkItemAge` | `getTotalWorkItemAge` (`:215`) | also feeds `loadBalanceMatrix` (flow-metrics, `:1599`) |
| `workItemAgePercentiles` RAG (`:389`) | `agingItems` (`:1732`) | `getInProgressItems` (`:231`) | shared with `aging`, `workDistribution` |

Symmetrically, **flow-metrics'** `loadBalanceMatrix` (`:1594-1610`) and `totalWorkItemAgeOverTime`
RAG (`:1746-1750`) read `wipPbcData` / `totalWorkItemAgePbcData`, i.e. two members of the
**predictability** PBC batch.

Two mitigations exist for the cheapest of these:
`throughputInfo.total` and `arrivalsInfo.total` (`InfoWidgetData.ts:11-20`) already carry the exact
numbers `computeStartedVsClosedRag` needs, so `getThroughput`/`getArrivals` can be dropped from the
flow-overview requirement set by re-sourcing `startedTotal`/`closedTotal` from the two `*Info`
fetches that flow-overview makes anyway. `cycleTimes` has **no** such substitute —
`ICycleTimePercentilesInfo` (`InfoWidgetData.ts:45`) carries percentiles, not the raw per-item
cycle times that `calculateSLEStats` needs — so `getCycleTimeData` must stay in flow-overview's set.

---

## 3. Five Whys — branch analysis

```
PROBLEM: Opening the metrics view issues ~30 (team) / ~34 (portfolio) backend requests, of which
         ~1/3 are for widgets in categories that are not selected and cannot render.
```

### Branch A — the parent data layer is category-blind

> **WHY 1A** — On first open ~30 requests fire before any widget renders.
> *Evidence:* probe run, §1.1: `TEAM_TOTAL=29` from `useMetricsData` alone + 1 from
> `TotalWorkItemAgeWidget.tsx:41`.

> **WHY 2A** — Because all fetches live in one parent hook whose 17 effects are unconditional.
> *Evidence:* `useMetricsData.ts:195, 204, 213, 222, 229, 253, 323, 348, 373, 382, 389, 398, 405,
> 414, 423, 432, 441, 458, 468` — every one keyed on `[entity, metricsService, startDate, endDate]`
> only. The only conditional guards are owner-type shape checks
> (`isProjectMetricsService` `:324`, `isTeamMetricsService` `:459`), never a category check.

> **WHY 3A** — Because the hook's contract has no category input, so the gate is not expressible.
> *Evidence:* signature at `useMetricsData.ts:106-113` — four parameters, none of them a category.
> `CategoryKey` is not imported anywhere under `src/hooks/`
> (`ctx_search "CategoryKey"` → matches only in `pages/Common/MetricsView/`).

> **WHY 4A** — Because the categorisation feature partitioned **rendering** only, and left the
> fetch layer exactly as it was.
> *Evidence:* commit `116d8b39a` (2026-04-05, "Implement category selection and tips toggle
> functionality") introduced `categoryMetadata.ts`, `CategorySelector`, `useCategorySelection`,
> `WidgetShell`. Its diff against `BaseMetricsView.tsx` touches **13 lines containing a
> `metricsService.<method>(` call, and all 13 are pure re-indentation** (`-\t\t\t\t\tX` / `+\t\t\t\tX`
> pairs). Not one fetch was gated, moved or removed. Before that commit every widget was on one
> page (`git show 116d8b39a^:…/BaseMetricsView.tsx` → 8 `useEffect` fetch blocks feeding a single
> flat widget list), where fetch-everything was *correct*.

> **WHY 5A** — Because one day later the un-gated fetch set was lifted verbatim into a hook, which
> froze "fetch everything" into a reusable contract and hid it behind an abstraction boundary that
> no longer had any reason to mention categories.
> *Evidence:* commit `5b4561934` (2026-04-06), message line: *"refactor: consolidate metrics data
> fetching logic in useMetricsData hook"* — `BaseMetricsView.tsx −345`, `useMetricsData.ts +286`.
> A **consolidation**, not a re-scoping. The one-day gap means the extraction was designed against
> the pre-category mental model ("the view needs all of it") while the category commit had already
> landed.

> **→ ROOT CAUSE A:** A rendering-scope change (categories) was shipped without a matching
> data-scope change, and the very next refactor encapsulated the now-stale "fetch everything"
> assumption into a hook signature that cannot express category scope. Every widget added since has
> inherited the eager default for free, because adding a fetch to `useMetricsData` is the path of
> least resistance and nothing pushes back.

### Branch B — nothing detects or forbids over-fetching

> **WHY 1B** — 12 fetches for unrenderable widgets shipped and stayed shipped through 15+ commits
> touching this file.
> *Evidence:* `git log --oneline -- src/hooks/useMetricsData.ts` — 15 commits between 2026-04-06
> and 2026-07-19, each *adding* a fetch (`6421820d1` arrivals, `526b4e0cf` per-state percentiles,
> `69e863af2` cumulative state time, `5d8d4450f` WIA percentiles, `94eb030e0` blocked history,
> `93eafd3a8` flow efficiency). None removed or gated one.

> **WHY 2B** — Because no test asserts *which* fetches fire for a given selected category; the
> tests only assert that a fetch *did* fire.
> *Evidence:* `useMetricsData.test.ts:187-200` — `describe("Baseline fetch orchestration")` /
> `it("should call all core metrics service methods on mount")`. The suite's stated intent is the
> opposite of the desired invariant. `BaseMetricsView.test.tsx` sets the category via `localStorage`
> in several places (`:5335`, `:5481`) but never pairs it with a negative fetch assertion.

> **WHY 3B** — Because the *only* fetch-count assertion in the suite was written to police a
> different concern — duplicate fetching — and only for one metric.
> *Evidence:* `BaseMetricsView.test.tsx:5433-5447` — *"fetches flow efficiency exactly once, through
> the shared data layer (AC4)"*, `expect(svc.getFlowEfficiencyInfoForPortfolio).toHaveBeenCalledTimes(1)`.
> Its comment (`:5434`) — *"one shared data path, not one more round trip (D18)"* — shows the team
> was optimising *duplication* while treating *eagerness* as free.

> **WHY 4B** — Because the design rule the team actually adopted was "one shared data path", and
> that rule actively rewards centralising fetches in the parent — which is precisely what makes them
> eager.
> *Evidence:* `useMetricsData.ts:254-257` — *"Every call below shares the same dependency signature,
> so they all belong in one parallel batch"*; `93eafd3a8` moved flow-efficiency's fetch *out of* the
> widget and *into* the parent hook, i.e. from lazy to eager, and was reviewed and merged as an
> improvement. The correct pattern also exists in-repo (`usePercentilesOverTime.ts`,
> `usePbcOverTime.ts`, `BaseMetricsView.tsx:1447-1451` `candidatesRequestedRef`) but was never
> written down as the rule.

> **WHY 5B** — Because "how many requests does opening this page cost" is not a tracked property of
> the metrics view: no budget, no assertion, no CI signal.
> *Evidence:* `ctx_search` for `toHaveBeenCalledTimes` scoped to a *count of distinct endpoints* →
> none. `docs/ci-learnings.md` has no request-count/eager-fetch rule. The defect was found by a
> human reading the browser Network tab, which is the detection channel of last resort.

> **→ ROOT CAUSE B:** First-open request count is an untracked, unasserted property, and the one
> design rule the team *did* codify ("one shared data path") pushes fetches toward the eager parent
> without a counterweight. Eagerness was therefore free to accumulate one widget at a time.

### Branch C — batching hard-codes cross-category coupling

> **WHY 1C** — Even a per-effect category gate cannot remove ~7 of the wasted fetches.
> *Evidence:* `useMetricsData.ts:284-308` batches `getCycleTimeData` (flow-overview RAG),
> `getCycleTimePercentiles` (flow-overview), `getWorkItemAgePercentiles` ×2 (flow-overview),
> `getAgeInStatePercentiles` (**flow-metrics**), `getCumulativeStateTimeForTeam` (**flow-metrics**)
> and `getFlowEfficiencyInfo` (flow-overview) into a single `Promise.all`. Likewise `:476-482`
> batches 3 predictability-only PBCs with 2 that flow-metrics also needs, and `:326-342` chains
> 5 portfolio fetches spanning three categories.

> **WHY 2C** — Because the batch boundary was drawn on *dependency signature*, not on *consumer*.
> *Evidence:* the batch's own comment, `useMetricsData.ts:254-256`: *"Every call below shares the
> same dependency signature, so they all belong in one parallel batch"*.

> **WHY 3C** — Because at the time the only cost being optimised was wall-clock serialisation, and
> every call was needed anyway, so grouping by dependency signature was lossless.
> *Evidence:* `:255-257` — *"getCycleTimeData used to be awaited sequentially ahead of the batch,
> which needlessly gated the rest of the view (D18)"*. Under fetch-everything, consumer identity
> carried no information.

> **WHY 4C** — Because there is no artefact anywhere that records *which widget consumes which
> fetch*. `categoryMetadata.ts` maps category → widget (`:47-104`) and widget → trend policy
> (`:106-137`), but stops there; the widget → data edge exists only implicitly, spread across
> `buildWidgetNodes` (`:861-1173`), `buildWidgetFooters` (`:315-491`) and `buildViewData`
> (`:520-752`).
> *Evidence:* `categoryMetadata.ts` full file — no data/fetch dimension. The three builders each
> re-derive the mapping ad hoc from ~40 destructured parent variables (`BaseMetricsView.tsx:1231-1268`).

> **WHY 5C** — Because the widget↔data relationship was never modelled as data, only as code, so it
> cannot be queried, gated on, or tested for completeness — and any gating attempt must first
> reverse-engineer it by hand from three separate builder functions.
> *Evidence:* the derivation in §2/Q5 of this document required reading `buildWidgetNodes`,
> `buildWidgetFooters` **and** the `RagInputs` construction site (`:1691-1787`) to discover that a
> *flow-overview* chip depends on a *flow-metrics* fetch. Nothing surfaces that edge.

> **→ ROOT CAUSE C:** The widget → data-requirement mapping is not a first-class artefact. Batches
> were therefore grouped by dependency signature rather than by consumer, and cross-category data
> dependencies (Q5) are invisible — making them the most likely thing a lazy-loading fix breaks.

### Branch D — a duplicate fetch survives inside a widget

> **WHY 1D** — `getTotalWorkItemAge` is requested twice on every flow-overview open.
> *Evidence:* `useMetricsData.ts:213-220` and `TotalWorkItemAgeWidget.tsx:36-56`, same
> `(entityId, asOfDate)` arguments.

> **WHY 2D** — Because the widget kept its original self-fetch when the parent added its own.
> *Evidence:* `BaseMetricsView.tsx:991-997` passes `entityId` + `metricsService` + `asOfDate` into
> the widget rather than the already-fetched `totalWorkItemAge` value, even though the parent holds
> it (`:1253`) and uses it for the RAG footer (`:365-374`) and `loadBalanceData` (`:1599`).

> **WHY 3D** — Because the parent needed the *number* for a RAG chip and the shortest path to that
> was to add a second fetch rather than to lift the widget's existing one.
> *Evidence:* both consumers are RAG/derived (`:365`, `:1599`); the widget renders the number itself
> from its own state (`TotalWorkItemAgeWidget.tsx:28,45`).

> **WHY 4D** — Because the "lift the self-fetch into the shared path" cleanup was performed once, as
> a scoped acceptance criterion for a single widget, not as a sweep.
> *Evidence:* `93eafd3a8` "flow efficiency status via the shared data path (#5508)" +
> `BaseMetricsView.test.tsx:5322-5325` — *"Flow Efficiency is the only Flow Overview widget off the
> shared data path"*. That statement was already false when written: `TotalWorkItemAgeWidget`
> was also off it, in the *other* direction (self-fetch **and** parent fetch).

> **WHY 5D** — Same as Root Cause B: with no assertion on total request count, a duplicate is
> invisible; the one duplication test that exists (`:5433`) covers a single named metric.

> **→ ROOT CAUSE D:** A per-widget cleanup was scoped to one widget and closed with a single-metric
> assertion, leaving a duplicate `getTotalWorkItemAge` in place. Independent of the category gating,
> and independently fixable.

### Cross-validation

| Pair | Consistent? | Note |
|---|---|---|
| A + B | consistent | A creates the eagerness; B removes the feedback that would have caught it. |
| A + C | consistent | C explains why A is not fixable by a one-line gate per effect. |
| A + D | consistent, independent | D exists inside a widget, not in the hook; survives any fix to A. |
| B + C | consistent, mutually reinforcing | The untracked property (B) is untrackable precisely because the mapping is not data (C). |

**All observed symptoms explained:**

| Symptom | Explained by |
|---|---|
| ~30 requests on first open | A (+ D for 1 of them) |
| ~1/3 of them for invisible widgets | A |
| Switching category ≈ free | A — data already in parent `useState`; no cache involved (Q3) |
| Switching to Predictability = exactly 2 requests | The two correctly-lazy widget hooks (`usePercentilesOverTime`, `usePbcOverTime`) |
| Switching to Flow Metrics / Portfolio = 0 requests | Neither category mounts a self-fetching widget |
| Worse on slow instances | A — the ~10–12 wasted calls are on the critical path to first paint |

**Backwards validation** — *if root cause A holds, would we observe exactly this?* Yes: a
category-blind parent hook must fetch the union of all categories on mount (30 requests), leaving
nothing for a category switch to fetch except what genuinely lives inside a widget (0–2). Both
halves of the report follow. No competing hypothesis (caching, hidden mounts, react-query
prefetch) survives §2/Q3–Q4.

**Gaps / open items honestly flagged:**

- *Hypothesis — requires verification:* which of the wasted calls dominate wall-clock on a slow
  instance. Payload/compute reasoning points at `getCycleTimeData`, the 5 PBC recomputes,
  `getCumulativeStateTimeForTeam` and `getAllFeaturesForSizeChart`, but this RCA measured **call
  counts, not latency**. Before/after timing on a real instance should confirm the win. This does
  not change the diagnosis, only the size of the prize.
- *Hypothesis:* the sequential `await` chains at `useMetricsData.ts:326-342` (5 serial portfolio
  round-trips) and `:230-246` (3 serial round-trips) add latency **independently of** category
  gating. Cheap to fix in the same change; listed as a contributing factor, not a root cause.

---

## 4. Contributing factors (not root causes)

| # | Factor | Evidence |
|---|---|---|
| CF-1 | Serial `await` chains inside two effects — 5 serial portfolio round-trips, 3 serial WIP round-trips — while the rest of the hook uses `Promise.all` | `useMetricsData.ts:326-342`, `:230-246` vs `:284-308`, `:470-482` |
| CF-2 | The previous-period WIA percentile fetch doubles one endpoint's cost for a trend arrow on one flow-overview widget | `useMetricsData.ts:296-301`, consumed at `BaseMetricsView.tsx:1838` |
| CF-3 | `isTeamMetricsService` (`useMetricsData.ts:93`) keys off `getFeaturesWorkedOnInfo`, whose own comment (`:98-104`) warns the predicate answers the wrong question — brittle owner-type discrimination adjacent to the code being changed | `useMetricsData.ts:88-104` |
| CF-4 | `buildWidgetFooters` / `buildViewData` compute entries for **all** ~35 widget keys on every render, then index only the active ones | `BaseMetricsView.tsx:315-491`, `:520-752`, consumed at `:1854-1857`. CPU-only, no requests — but it is the same "compute for everything, use a slice" shape as the bug |
| CF-5 | `BaseMetricsView.tsx` is 1892 lines with a 78-field inline ctx type (`:861-934`); the widget↔data edge is unreadable at a glance | file length; `buildWidgetNodes` parameter type |

---

## 5. Proposed fix

Design constraints honoured: (i) switching category must stay ~free on re-visit; (ii) no RAG chip,
trend arrow or view-data table on the default view may regress; (iii) minimal — no re-architecture.

### Key insight that keeps it minimal

Because the existing "cheapness" of switching is **not** a cache but retained parent state
(§2/Q3), the fix does not need to add a cache. It needs a gate that is **monotonic within a
(entity, startDate, endDate) window**: once a category has been visited, its fetches stay
"required" until the window changes. A plain boolean per effect then guarantees at-most-one fetch
per window with no refs, latches or cache layer:

```ts
const needsX = activeFetchKeys.has("x");
useEffect(() => {
  if (!needsX) return;
  /* unchanged body */
}, [entity, metricsService, startDate, endDate, needsX]);
```

`needsX` is a primitive, so the effect re-runs only when it flips. Monotonic within a window ⇒
flips false→true at most once ⇒ exactly one fetch. Window/entity change resets the visited set,
which is exactly when a refetch is wanted anyway (and `startDate`/`endDate` are already in deps).

### Change 5.1 — make the widget → data edge a first-class artefact (Root Cause C)

**File:** `src/pages/Common/MetricsView/categoryMetadata.ts` (append, after `getTrendPolicy` at `:166`)

```ts
export type MetricsFetchKey =
  | "blackoutPeriods" | "predictability" | "totalWorkItemAge" | "throughput"
  | "inProgressItems" | "blockedItems" | "wipOverTime" | "cycleTimeData"
  | "cycleTimePercentiles" | "workItemAgePercentiles" | "ageInStatePercentiles"
  | "cumulativeStateTime" | "flowEfficiency" | "featureSizeData" | "featureSizePbc"
  | "featureSizeEstimation" | "featureSizePercentilesInfo" | "estimationVsCycleTime"
  | "arrivals" | "throughputInfo" | "arrivalsInfo" | "wipOverviewInfo"
  | "totalWorkItemAgeInfo" | "predictabilityScoreInfo" | "cycleTimePercentilesInfo"
  | "blockedCountHistory" | "featuresWorkedOnInfo" | "pbcCore" | "pbcCharts";

/**
 * What a widget needs to render *completely* — body AND RAG footer AND trend AND view-data.
 * The footer/trend entries are the load-bearing ones: several Flow Overview chips are computed
 * from data whose primary consumer sits in another category (Bug #5571 §Q5). Omitting one here
 * blanks a chip on the default view, which is why categoryMetadata.test.ts asserts every widget
 * in every category has an entry.
 */
const widgetFetchRequirements: Record<string, readonly MetricsFetchKey[]> = { /* … */ };

export function getFetchKeysForCategories(
  categoryKeys: readonly CategoryKey[],
  ownerType: "team" | "portfolio",
): ReadonlySet<MetricsFetchKey> { /* union over getWidgetsForCategory(k, ownerType) */ }
```

Requirement entries that are **not** obvious and must not be dropped (all from §2/Q5):

| widgetKey | category | must include | why |
|---|---|---|---|
| `percentiles` | flow-overview | `cycleTimeData` | RAG `ragRules.ts:174` needs raw cycle times; `ICycleTimePercentilesInfo` has none |
| `featureSizePercentiles` | flow-overview | `featureSizeData` | RAG `ragRules.ts:619` needs `sizePercentileValues` + active feature sizes |
| `blockedOverview` | flow-overview | `blockedCountHistory` | trend `BaseMetricsView.tsx:1828` |
| `totalWorkItemAge` | flow-overview | `totalWorkItemAge` | RAG `:365` |
| `loadBalanceMatrix` | flow-metrics | `pbcCore`, `totalWorkItemAge` | `:1594-1610` |
| `totalWorkItemAgeOverTime` | flow-metrics | `pbcCore`, `wipOverTime` | RAG `:1746-1750` |
| `workItemAgePercentiles` | flow-overview | `inProgressItems` | RAG via `agingItems` `:1732` |
| `stacked` | flow-metrics | `throughput`, `arrivals`, `wipOverTime` | `:1061-1082` |
| every widget | — | its `buildViewData` sources (`:1789-1800`) | else the drill-in table silently empties |

`pbcCore` = `{getWipPbc, getTotalWorkItemAgePbc}` (predictability **and** flow-metrics);
`pbcCharts` = `{getThroughputPbc, getCycleTimePbc, getArrivalsPbc}` (predictability only).

### Change 5.2 — accumulate visited categories (monotonic within a window)

**File:** `src/pages/Common/MetricsView/useCategorySelection.ts` (append; existing test file present)

```ts
/** Grows as the user visits categories; resets when the entity or date window changes, which is
 *  the only time a refetch is wanted. Keeps category switching free on re-visit (Bug #5571). */
export function useVisitedCategories(
  selectedCategory: CategoryKey,
  resetToken: string,
): readonly CategoryKey[] { /* useState<CategoryKey[]> + reset effect on resetToken */ }
```

### Change 5.3 — gate every effect (Root Causes A + C)

**File:** `src/hooks/useMetricsData.ts`

1. `:106-113` — add a 5th parameter `activeFetchKeys: ReadonlySet<MetricsFetchKey>`.
2. Derive one boolean per fetch key just above the effects.
3. Add `if (!needsX) return;` as the first line of each of the 17 effects and append `needsX` to
   its dependency array.
4. **Split three batches by consumer, not by dependency signature:**
   - `:284-308` → keep `{getCycleTimeData, getCycleTimePercentiles, getWorkItemAgePercentiles ×2,
     flowEfficiency}` in one gated batch; move `getAgeInStatePercentiles` and
     `getCumulativeStateTimeForTeam` into their own gated effects. *The D18 intent
     (`:254-257`) is preserved:* sibling effects still dispatch in the same commit, so the calls
     remain parallel — only the grouping changes.
   - `:476-482` → two batches: `pbcCore` `{getWipPbc, getTotalWorkItemAgePbc}` and `pbcCharts`
     `{getThroughputPbc, getCycleTimePbc, getArrivalsPbc}`.
   - `:326-342` → three gated groups (`featureSizeData` / `featureSizePbc` /
     `featureSizeEstimation` + `featureSizePercentilesInfo`) **and** convert the serial `await`
     chain to `Promise.all` (CF-1).
5. `:230-246` — split `getWorkInProgressOverTime` into its own gated effect and `Promise.all` the
   remaining two (CF-1).

### Change 5.4 — wire it up

**File:** `src/pages/Common/MetricsView/BaseMetricsView.tsx`

- Move `useCategorySelection` (currently `:1539`) **above** the `useMetricsData` call at `:1268`.
  `ownerType` (`:1379-1380`) must move up with it; it depends only on `metricsService`, so this is
  a pure reordering with no behavioural change.
- Insert:
  ```ts
  const visitedCategories = useVisitedCategories(
    selectedCategory,
    `${entity.id}:${formatLocalDate(startDate)}:${formatLocalDate(endDate)}`,
  );
  const activeFetchKeys = useMemo(
    () => getFetchKeysForCategories(visitedCategories, ownerType),
    [visitedCategories, ownerType],
  );
  ```
  (`formatLocalDate` is already imported — `:1214`.)
- `:1268` — pass `activeFetchKeys` as the 5th argument.

### Change 5.5 — remove the duplicate fetch (Root Cause D) — separate commit

**Files:** `src/components/Common/Charts/TotalWorkItemAgeWidget.tsx`, `BaseMetricsView.tsx:991-997`

Replace the widget's `entityId`/`metricsService`/`asOfDate` props with a `totalAge: number | null`
prop fed from the parent's existing `totalWorkItemAge` (`:1253`), and delete the `useEffect` at
`TotalWorkItemAgeWidget.tsx:36-56`. Exactly the D18 lift already applied to Flow Efficiency in
`93eafd3a8`. Ship separately so a bisect can separate it from the gating change.

### Optional, if the measured win justifies it (defer by default)

Re-source `startedTotal`/`closedTotal` (`BaseMetricsView.tsx:1708-1709`) from
`arrivalsInfo.total` / `throughputInfo.total` (`InfoWidgetData.ts:11-20`), which flow-overview
fetches anyway. That drops `getThroughput` and `getArrivals` from flow-overview's requirement set —
2 more requests off the default view. Behaviour-preserving **only if** the `*Info` totals are
computed over the identical window; verify against the backend before doing it.

### Expected result

| View | Before (measured) | After (projected) |
|---|---|---|
| Team, flow-overview | 30 | ~19 (~17 with the optional change) |
| Portfolio, flow-overview | 34 | ~21 |
| Switch to a visited category | 0–2 | 0–2 (unchanged) |
| Switch to an unvisited category | 0–2 | that category's own set, once |

---

## 6. Files affected

| File | Change |
|---|---|
| `Lighthouse.Frontend/src/pages/Common/MetricsView/categoryMetadata.ts` | **+** `MetricsFetchKey`, `widgetFetchRequirements`, `getFetchKeysForCategories` |
| `Lighthouse.Frontend/src/pages/Common/MetricsView/categoryMetadata.test.ts` | **+** completeness KPI test (§7.3) |
| `Lighthouse.Frontend/src/pages/Common/MetricsView/useCategorySelection.ts` | **+** `useVisitedCategories` |
| `Lighthouse.Frontend/src/pages/Common/MetricsView/useCategorySelection.test.ts` | **+** monotonic-growth + reset-on-token tests |
| `Lighthouse.Frontend/src/hooks/useMetricsData.ts` | 5th param; 17 effect gates; 3 batch splits; 2 serial→parallel |
| `Lighthouse.Frontend/src/hooks/useMetricsData.test.ts` | update all `renderHook` call sites (5th arg); **+** gating unit tests (§7.2) |
| `Lighthouse.Frontend/src/pages/Common/MetricsView/BaseMetricsView.tsx` | hoist `ownerType`/`useCategorySelection`; compute `activeFetchKeys`; pass to hook |
| `Lighthouse.Frontend/src/pages/Common/MetricsView/BaseMetricsView.test.tsx` | **+** `describe("category-scoped fetching (Bug #5571)")` (§7.1) |
| `Lighthouse.Frontend/src/components/Common/Charts/TotalWorkItemAgeWidget.tsx` | *(5.5)* drop self-fetch, take `totalAge` prop |
| `Lighthouse.Frontend/src/components/Common/Charts/TotalWorkItemAgeWidget.test.tsx` | *(5.5)* prop-driven instead of service-driven |
| `docs/ci-learnings.md` | **+** rule: a new metrics widget must declare `widgetFetchRequirements` |

No backend files. No E2E specs.

---

## 7. Risk assessment

| # | Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|---|
| R1 | A flow-overview **RAG chip** blanks because its input came from another category's fetch (`percentiles`←`cycleTimeData`, `featureSizePercentiles`←`featureSizeData`, `totalThroughput`/`totalArrivals`←`throughput`/`arrivals`, `blockedOverview` trend←`blockedCountHistory`) | **High** — this is the single most likely regression | High — silent, visual-only | Requirement map derived from footers+trends+viewData, not widget bodies. The existing AC7 KPI test (`BaseMetricsView.test.tsx:5456`) already asserts every flow-overview widget renders a `widget-header-*`; extend it to all four categories |
| R2 | A widget's **view-data drill-in table** silently empties — `buildViewData` (`:520-752`) reads `inProgressItems`, `cycleTimeData`, `throughputData`, `wipOverTimeData`, `allFeaturesForSizeChart` (`:1789-1800`) | **High**, and subtler than R1 (needs a click to notice) | Medium | Include `buildViewData` sources in every requirement entry; assert `widget-view-data-*` testids per category (the mock `WidgetShell` at `:565-616` already emits them) |
| R3 | **PBC drill-through loses work-item names** — `workItemLookup` (`:1545-1553`) is built from throughput + wipOverTime + cycleTime + inProgress + features and is passed to every PBC node (`:854`). Fully honouring it forces `predictability` to pull most of flow-metrics' data, erasing much of the saving | **Medium** | Medium — degraded drill-through, or no saving | **Decide explicitly, do not assume.** Either (a) accept the extra fetches for `predictability` and take the win only on `flow-overview`, or (b) let the lookup degrade to id-only on PBC points. Recommend (a) for this fix; (b) is a product decision |
| R4 | **Category switching becomes slow** — the monotonic invariant is broken (e.g. someone passes only the current category's keys) | Low | High — regresses the reported-good behaviour | Test §7.1(4): visit → leave → return, assert `toHaveBeenCalledTimes(1)` throughout |
| R5 | **Date-range change stops refetching** the visible category | Low | High | `resetToken` includes both dates; test §7.1(6) |
| R6 | Splitting `Promise.all` batches raises first-open **concurrency** — more parallel in-flight requests on a slow instance | Low | Low | Net in-flight count *drops* (~30→~19). Note only |
| R7 | `useMetricsData.test.ts` — all ~18 `renderHook` call sites need the new 5th argument | Certain | Low (mechanical) | Default the parameter to "all keys" so existing call sites compile unchanged, then add gating tests explicitly. Keeps the diff honest and small |
| R8 | Reordering `useCategorySelection`/`ownerType` above `useMetricsData` changes hook order | Low | Medium if wrong | `ownerType` depends only on `metricsService` (`:1379-1380`); `useCategorySelection` only on `ownerType` + `entity.id`. Neither reads anything from `useMetricsData`. Full suite + `pnpm build` |
| R9 | `TotalWorkItemAgeWidget` (5.5) loses its own loading/error states (`:29-30`), which the parent does not model | Medium | Low — cosmetic | Keep `totalAge: number \| null` and render the existing null/loading branch off `null`. Ship as its own commit |

**Explicitly NOT at risk:** rendering/mounting (already lazy, §2/Q4); the two correctly-lazy widget
hooks (untouched); any cache (none exists, §2/Q3); backend behaviour (no backend change).

### R3 — DECIDED (2026-07-27, by maintainer)

**Option (a) — accept the extra fetches for `predictability`; take the win on `flow-overview` only.**

Rationale given: *"predictability will usually not be the first category that people open."* The
reported pain is first-open latency on the default view, and `flow-overview` **is** the default
view — so gating it is where the whole prize sits. PBC drill-through keeps full work-item names.

Binding consequence for Change 5.1: `predictability`'s requirement set **must** include
`workItemLookup`'s full input set — `throughput`, `wipOverTime`, `cycleTimeData`, `inProgressItems`
and (portfolio) the features source — because every PBC node receives `workItemLookup`
(`BaseMetricsView.tsx:854`, built at `:1545-1553`). Do **not** trim these to make the
`predictability` numbers look better; that is option (b), which was rejected.

---

## 8. Regression test proposal

Frontend only, service-layer spies, no E2E — per the stated constraint.

### 8.1 Primary — `src/pages/Common/MetricsView/BaseMetricsView.test.tsx`

New `describe("category-scoped fetching (Bug #5571)")`. This file is the right home because it
exercises the **real** wiring end-to-end in jsdom: `localStorage` → `useCategorySelection` →
`BaseMetricsView` → `useMetricsData` → `IMetricsService`.

**Existing infrastructure reused, nothing new needed:**
`createMockMetricsService<T>()` (`:733`, every method already a `vi.fn()`), `renderWithRouter`
(`:624`), `mockTeam` (`:908`) / `mockProject` (`:898`), and the established
`localStorage.setItem("lighthouse:metrics:<ownerType>:<id>:category", …)` pattern (`:5335`, `:5481`).

**Spy target:** the mock `IMetricsService` instance's methods — the port boundary, not the HTTP
client. Plus `mockBlackoutPeriodService.getAll` for `blackoutPeriods`.

**Assertions:**

1. **Negative, default category.** Category `flow-overview`, render, `await` the flow-overview
   widgets, then:
   ```ts
   for (const method of [
     "getThroughputPbc", "getCycleTimePbc", "getArrivalsPbc",
     "getAgeInStatePercentiles", "getCumulativeStateTimeForTeam",
     "getWorkInProgressOverTime", "getEstimationVsCycleTimeData",
   ] as const) {
     expect(svc[method], `${method} must not fire for flow-overview`).not.toHaveBeenCalled();
   }
   expect(mockBlackoutPeriodService.getAll).not.toHaveBeenCalled();
   ```
2. **Positive, default category.** Every flow-overview fetch called **exactly once**:
   `getWipOverviewInfo`, `getThroughputInfo`, `getArrivalsInfo`, `getBlockedItemsAtDate`,
   `getInProgressItems`, `getFlowEfficiencyInfoForTeam`, `getCycleTimePercentiles`,
   `getCycleTimePercentilesInfo`, `getWorkItemAgePercentiles` (×2 — current + previous),
   `getMultiItemForecastPredictabilityScore`, `getPredictabilityScoreInfo`,
   `getTotalWorkItemAgeInfo`, `getBlockedCountHistory`, `getCycleTimeData`.
3. **Gate opens on switch.** `userEvent.click` the Predictability category button
   (`CategorySelector.tsx`), `await` `widget-header-throughputPbc`, then
   `expect(svc.getThroughputPbc).toHaveBeenCalledTimes(1)`.
4. **The load-bearing invariant — switching back stays free.** predictability →
   flow-overview → predictability, then assert **every** method from (2) and (3) is still
   `toHaveBeenCalledTimes(1)`. This is the test that fails if someone "fixes" eagerness with a
   naive per-render gate, and it directly encodes the reporter's "switching barely loads anything".
5. **No chip regresses (guards R1/R2).** For each of the four categories × both owner types, after
   switching: every `widgetKey` from `getWidgetsForCategory(category, ownerType)` renders a
   `widget-header-${widgetKey}`. Generalises the existing AC7 KPI test (`:5456`) from
   flow-overview/portfolio to all categories.
6. **Window change still refetches (guards R5).** Change the end date via the date picker; assert
   the currently-visible category's fetches went from 1 → 2 calls.

### 8.2 Unit — `src/hooks/useMetricsData.test.ts`

- `renderHook` with `activeFetchKeys = new Set(["throughput"])` → `getThroughput` called once, and
  `getWipPbc`/`getAgeInStatePercentiles`/`getCumulativeStateTimeForTeam` **never**.
- `rerender` with a **superset** → the newly-added key fetches once, and `getThroughput` is
  **still** `toHaveBeenCalledTimes(1)` (monotonic-boolean invariant, R4 at unit level).
- Change `endDate` with an unchanged key set → every gated fetch refetches (R5 at unit level).

### 8.3 Structural KPI — `src/pages/Common/MetricsView/categoryMetadata.test.ts`

Mirrors the AC7 pattern already in this repo (`BaseMetricsView.test.tsx:5456`):

```ts
it("gives every widget a declared data requirement, so none can ship eagerly (Bug #5571)", () => {
  for (const category of getCategories()) {
    for (const ownerType of ["team", "portfolio"] as const) {
      for (const { widgetKey } of getWidgetsForCategory(category.key, ownerType)) {
        expect(
          widgetFetchRequirements[widgetKey],
          `widget "${widgetKey}" has no entry in widgetFetchRequirements — it will render without data`,
        ).toBeDefined();
      }
    }
  }
});
```

This is the standing counterweight to Root Cause B: a new widget cannot ship without declaring what
it fetches, and the declaration is what the gate reads.

---

## 9. Prevention

| Root cause | Permanent fix | Early detection |
|---|---|---|
| A — category-blind data layer | 5.3 + 5.4: the hook takes a fetch-key set; no ungated effect remains | 8.1(1) — negative assertions per category |
| B — request count untracked | `docs/ci-learnings.md` rule: *"a metrics widget's data needs are declared in `widgetFetchRequirements`; a fetch added to `useMetricsData` without a key is a defect"* | 8.3 structural KPI + 8.1(2) exact-count assertions |
| C — widget↔data edge not modelled | 5.1: `widgetFetchRequirements` becomes the single queryable source for the edge | 8.3 completeness test fails the moment a widget is added without an entry |
| D — duplicate self-fetch | 5.5: lift into the shared path, same as flow efficiency (`93eafd3a8`) | 8.1(2) `toHaveBeenCalledTimes(1)` on `getTotalWorkItemAge` |

**Sequencing:** 5.1 + 8.3 first (make the edge explicit and provable — pure addition, no behaviour
change), then 5.2 + 5.3 + 5.4 + 8.1/8.2 (the gate), then 5.5 (the duplicate) as its own commit.
Decide R3 explicitly before starting 5.1, since it determines `predictability`'s requirement set.
