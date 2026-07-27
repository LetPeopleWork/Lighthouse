# Category-Scoped Metrics Fetching — Evolution

**Feature:** fix-widget-eager-fetch-by-category | **ADO:** Bug #5571 | **Shipped:** 2026-07-27 | **Commits:** `35f3524e1..34edb9d11`

## What shipped

Opening the metrics page fired ~34 `IMetricsService` calls on mount regardless of which widget category was on screen, then switching category fired almost nothing. Users on slower instances waited noticeably before any data appeared. The fix gates every fetch on the set of categories the user has actually *visited*.

Measured at the `IMetricsService` port, flow-overview mount: **team 34 → 19, portfolio 34 → 21.** Switching to an already-visited category still costs zero requests. Confirmed by the reporter in a real environment as "noticeably faster".

| Step | Outcome |
|---|---|
| 01-01 | `categoryMetadata.ts` gains `MetricsFetchKey`, `widgetFetchRequirements`, `getFetchKeysForCategories` — the widget↔data edge becomes a first-class artefact. |
| 01-02 | `useVisitedCategories(selectedCategory, resetToken)` — grow-only within a window, identity-stable. |
| 01-03 | `useMetricsData` takes a 5th `activeFetchKeys` param; 16 fetching effects gated; 4 batches re-split by *consumer*; 2 serial `await` chains converted to `Promise.all`. |
| 01-04 | `BaseMetricsView` hoists `ownerType`/`useCategorySelection` above the hook and wires the gate; 18-assertion regression block pins the bug. |
| 01-05 | Duplicate `getTotalWorkItemAge` removed — the widget takes a `totalAge` prop instead of self-fetching (Root Cause D). |
| 01-06 | Stale widget mock and a false code comment corrected. |
| 01-07 | Mutation gaps closed: empty-key-set gate test + owner-type discriminator tests. |

## Root cause

**Rendering was partitioned by category; fetching never was.** Commit `116d8b39a` (2026-04-05) introduced categories — and all 13 lines it touched containing a `metricsService.<method>(` call were *pure re-indentation*. One day later `5b4561934` lifted the un-gated set into `useMetricsData` under the message "consolidate metrics data fetching logic". A consolidation, not a re-scoping: it froze "fetch everything" into a hook signature with no category parameter, structurally incapable of knowing what was on screen.

Three factors kept it invisible for four months:

- **No test counted first-open requests.** `useMetricsData.test.ts` asserted *"should call all core metrics service methods on mount"* — the bug was pinned as intended behaviour.
- **The codified rule rewarded it.** The "one shared data path" comment invited moving fetches *into* the eager parent; `93eafd3a8` moved flow-efficiency lazy→eager and was merged as an improvement.
- **The widget↔data edge was not an artefact.** `categoryMetadata.ts` mapped category→widget and widget→trend but had no data dimension, so cross-category dependencies were invisible and batches were grouped by *dependency signature* rather than by consumer.

## Key decisions & design anchors

- **There is no cache in the metrics path.** Zero `cache|memo` matches in `src/services/Api/`; react-query is a dependency but used in exactly two non-metrics components. Results live in ~35 `useState` slots. The reporter's "switching barely loads anything" was therefore a *side effect of over-fetching*, not caching — a fact that inverts the obvious fix.
- **The gate is monotonic within an `(entity, startDate, endDate)` window.** Because there is no cache, gating on the *current* category would turn a one-time over-fetch into a refetch on every switch — strictly worse than the original bug. A grow-only visited set makes each gate flip false→true at most once, giving exactly one fetch per window with no refs, latches or cache layer. A plain boolean in the dependency array then suffices.
- **R3 — `predictability` keeps `workItemLookup`'s full input set.** `buildWorkItemLookup` feeds every PBC node, so honouring it forces that category to pull most of flow-metrics' data. Maintainer chose to accept it: the reported pain is first-open latency on the *default* view, and predictability is rarely opened first. PBC drill-through keeps full work-item names.
- **The requirement map is derived from four sources, not from widget bodies.** A widget's RAG chip, trend arrow and drill-in table are genuine data dependencies. Deriving from bodies alone would silently blank chips on the default view — the single most likely regression.
- **Reset token uses `formatLocalDate`, never `toISOString`.** At a negative UTC offset the UTC day flips first, which would reset the visited set and refetch everything for no reason (Bug #5566's failure mode).

## Gotchas worth remembering

- **A new metrics widget MUST declare a `widgetFetchRequirements` entry** or `categoryMetadata.test.ts` fails. That test is the standing counterweight to the root cause.
- **Non-obvious cross-category dependencies** that must stay in the map: `percentiles`←`cycleTimeData` (the RAG needs raw cycle times; `ICycleTimePercentilesInfo` carries none); `featureSizePercentiles`←`featureSizeData`; `blockedOverview`←`blockedCountHistory`; `workItemAgePercentiles`←`inProgressItems`; `totalThroughput`/`totalArrivals`←`throughput`+`arrivals` (`startedTotal`/`closedTotal` come from the *chart data*, not the `*Info` objects — an adversarial review got this backwards and would have blanked two chips); `estimationVsCycleTime`←`workItemLookup` inputs.
- **The cycle-time batch is gated on the disjunction of its four keys.** Any one active fires all five calls. Free on the default view where all four are needed; costs 1 extra call for flow-metrics-only and 3 for predictability-only. If a future category needs a strict subset, split the batch rather than widening the disjunction.
- **Still self-fetching off the shared data path** (deliberately out of scope): `PredictabilityScoreDetailsWidget` and `ThroughputRunChartCard`. The former is the easy one — the parent already holds `predictabilityData`.
- **`DashboardHeader.test.tsx > shows label and formatted date range`** runs ~3200 ms against a 5000 ms limit and flaked once during this work. Untouched; it will bite CI eventually.

## Quality gates

| Gate | Result |
|---|---|
| `pnpm test` | 276 files / 3753 tests green |
| `pnpm biome check ./src` | 648 files, zero errors, zero warnings |
| `pnpm build` | `tsc -b` + vite, zero warnings |
| `des-verify-integrity` | all 7 steps, complete DES traces |
| Mutation (feature-scoped Stryker) | **80.35%** — see `docs/feature/fix-widget-eager-fetch-by-category/mutation/` |
| CI on `34edb9d11` | frontend, sonar-gates, docker, postgres/sqlite/auth all green |

Mutation detail: `useCategorySelection.ts` 88.64%, `categoryMetadata.ts` 85.71%, `useMetricsData.ts` 79.61%, `TotalWorkItemAgeWidget.tsx` 30.56%. The last two are not coverage gaps — every surviving mutant in the widget is a MUI `sx` object or style string (all 11 behavioural mutants killed), and `useMetricsData`'s remainder is `console.error` message text, `.catch` arrow bodies, and dependency-array-to-`[]` mutants that are equivalent for single-render tests.

## DELIVER checklist

- **Public docs prose** — N/A. No behaviour, API or UI change; the page renders exactly as before, only sooner. No documented claim became false.
- **Per-feature screenshots** — N/A. No visual change; regenerating would produce byte-identical images.
- **Demo data** — N/A. No new entity, field or seeding path.
- **Lighthouse-Clients CLI/MCP versioning** — N/A. Frontend-only; no endpoint, DTO or contract touched.
- **Website marketing surface** — N/A. Performance fix to an existing feature, not a new capability.
- **RBAC impact** — N/A. No new endpoint or permission-gated surface; gating is client-side rendering economics.
- **EF migrations** — N/A. No backend change.

## Retrospective note

The bug's shape — *a check that cannot fail* — recurred inside the fix itself, twice, and both times mechanical verification caught what review would not have. The `TotalWorkItemAgeWidget` mock in `BaseMetricsView.test.tsx` had drifted to destructure props that no longer existed, rendering testids nothing asserted on; the suite stayed green throughout. And an assertion intended to prove the duplicate-fetch removal was vacuous, because the widget it depended on was mocked out — it would have passed before the fix too.

Sabotage verification is what surfaced both. Every step deliberately broke its own production code and confirmed the *specific* expected tests failed. One crafter reported honestly that a planned sabotage did **not** fail — the `toISOString` reset-token swap is unprovable at `Europe/Zurich`, since only viewers west of UTC break — rather than claiming a pass. That gap is documented in the code and remains untested by construction.

The adversarial review, by contrast, produced one finding labelled BLOCKER whose own body contradicted it, and one claiming code was absent that sits at `BaseMetricsView.tsx:1246`. Both were checked against the source and rejected. Worth remembering that a reviewer's severity label is a hypothesis, not evidence.
