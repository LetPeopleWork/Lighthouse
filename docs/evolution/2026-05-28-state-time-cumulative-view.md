# Evolution: state-time-cumulative-view

- **Date finalized**: 2026-05-28
- **Parent Epic**: #4144 — More Detailed State Info (third and final MVP-bundle feature, slice B3)
- **ADO**: Epic 4144; stories US-01/02/04 delivered, US-03 withdrawn (D13), US-05 story #5103 Removed (D21)
- **Status**: Shipped to `main`, CI-green. MVP gate target 2026-06-03.
- **Workspace (history)**: `docs/feature/state-time-cumulative-view/`

## What shipped

A cumulative time-per-state bar chart on the team and portfolio detail pages
(Flow Metrics category, widget key `stateTimeCumulative`). It answers a
leadership/retro question the existing charts did not: *"where does the team or
portfolio spend its time across the workflow as a whole?"* — the systemic
complement to the per-item aging badge.

- **One bar per `Doing`-category workflow state**, in board order. Each bar is
  stacked into a solid "completed contribution" segment and a hatched "ongoing
  contribution" segment (in-flight items' time in their current state).
- **Full-duration attribution (D5)**: the date window selects *which items* are
  in scope; each included item then contributes its *full* time in a state, not
  the clipped-to-window portion — so a slow state's true cost is visible.
- **Per-state drill-down (US-04)**: clicking a bar opens the shared
  `WorkItemsDialog` listing the contributing work items with a "Days Contributed"
  column.
- **In-chart item picker (US-05)**: multi-select by Reference ID / Name narrows
  the bars to a chosen subset of the windowed candidate set; RAG always reflects
  the whole in-scope set, never the selection (D18).
- New read endpoints (all authenticated), team + portfolio scoped:
  `…/metrics/cumulativeStateTime`, `…/cumulativeStateTime/items?state=`,
  `…/cumulativeStateTime/candidates`.

Data foundation reused, not rebuilt: consumes the `WorkItemStateTransition` rows
+ derived `WorkItem.CurrentStateEnteredAt` from sibling `time-in-state-and-staleness`.
No sync-side or schema change (D9).

## Slices

| Slice | Scope |
|-------|-------|
| 01 | Team-scope bar chart walking skeleton (endpoint + widget + adaptive unit + empty/zero states) |
| 02 | Portfolio-scope parity |
| 03 | Per-item drill-down (items endpoint + dialog) |
| 04 | In-chart item picker (candidates endpoint + picker) |
| Review revision (2026-05-27) | D19–D24, see below |

## Key decisions

The full decision log (D1–D24, with verbatim user framing) lives in the workspace
`feature-delta.md`. The load-bearing ones:

- **D5 — full-duration attribution** over clip-to-window: the window picks items;
  bars count their entire time in each state. Clipping would conceal the real cost
  of slow states.
- **D12 / D20 — inclusion rule**: an item is included if a state interval
  intersects the window OR it is in-flight with `CurrentStateEnteredAt ≤ windowEnd`;
  items *currently* in a `To Do` category are excluded entirely (D20) — even if
  they had earlier `Doing` time (a bounce-back is intentionally dropped).
- **D6 — ongoing visual**: hatched top segment, the established Lighthouse
  in-progress idiom (colour-blind-safe vs a shade change).
- **D16 — adaptive uniform display unit**: full-precision compute; the displayed
  unit (min→hr→day→week) is chosen once per render from the largest bar so heights
  stay comparable.
- **D18 — RAG over the whole in-scope set**, never recomputed by the picker (mirrors
  hiding a work-item-type in the cycle-time chart).

### Review revision (2026-05-27) — D19–D24

The first shipped cut drew user review that reshaped the feature:

- **D19** — bars show only `Doing`-category states (a stray "Closed" bar came from
  `BuildCumulativeWorkflowStateOrder` appending `DoneStates[0]`; removed).
- **D20** — exclude items currently in a `To Do` category from the candidate set.
- **D21** — **parent-expand dropped**; the picker selects raw work items only
  ("scales cleanly on portfolio level"). The candidates DTO lost `parentReferenceId`;
  ADO story #5103 set to Removed. This retired the original 2026-05-26 DELIVER
  block: the parent-expand @US-05 E2E became impossible-by-design, so it was
  replaced with a raw-item-select scenario.
- **D22** — drill-down reuses the shared `WorkItemsDialog` (clickable name link,
  state chip, CSV export) instead of a bespoke dialog; the items endpoint gained
  `workItemId`, `url`, `stateCategory`, and `daysContributed` is fed to the
  highlight column via an id-keyed map (no cast, no shared-component contract
  change).
- **D23** — all "Items" wording uses the configured Work Items terminology term.
- **D24** — fixed-height picker header so selecting/searching/clearing never
  reflows or skews the chart.

## Mutation testing

Feature-scoped Stryker (whole-file mutate + report-time filter on the backend;
TS line-ranges on the frontend). Report: workspace `deliver/mutation/mutation-baseline.md`.

- **Backend feature surface: 82.8%** (96/116; baseline 59%). The gap-closing pass
  drove window-boundary inclusivity, prepend/any-overlap, in-flight conjunction,
  multi-portfolio membership, drill-down ordering, mean/median, and null-timestamp
  robustness through the public read endpoints. The 20 remaining survivors are all
  equivalent (cache-key strings, `LogDebug` removals, ordering inside monotonic
  data).
- **Frontend: 68.4% raw / real-surface complete** (154/225; baseline 60.9%).
  formatDuration 100%, ragRules 95%, drill-down map 8%→83%, picker 67%. The 71
  survivors are all presentational (MUI `sx`/colour/variant/testid/label/axis-config
  /render-props) or equivalent (redundant `length===0` guard, unused default fields,
  hook-dep arrays). The chart is a thin declarative MUI wrapper whose styling props
  cannot be asserted without coupling tests to MUI internals; its computation is
  unit-tested.

## Lessons learned

- **Live E2E catches what unit tests cannot.** A chart prop defaulting to inline
  `new Date()` fed a `useEffect` dep that re-fired every render → React #185 loop
  that blanked the metrics view; 26 passing Vitest tests missed it. Fix:
  `useMemo`-stabilize. (See `project_react185_loop_unstable_useeffect_dep`.)
- **MUI X band-axis auto-hides overlapping tick labels.** The x-axis state labels
  rendered as empty `<text>` until we forced `tickLabelInterval: () => true` +
  explicit axis `height`, matching the Work Item Aging chart.
- **Demo data needed a transition history.** Demo CSVs had no state-since data
  (drill-down "—"); slice-03 added an optional "Current State Since" CSV column so
  demo items get a derived value. (See `project_demo_data_time_in_state_via_csv_column`.)
- **Stryker.NET `{a..b}` line-range globs silently mute all mutants** — whole-file
  mutate + report-time filtering is the only reliable backend scope.
- **The execution log predated the design pivot.** The 2026-05-26 DELIVER run
  blocked at 04-03 on parent-expand + single-item adaptive-unit demo-data gaps;
  both were resolved not by more code but by the 2026-05-27 review revision (D21
  dropped parent-expand). The feature is complete; the log just isn't the
  authority — git history + this doc are.
- **CI-learnings recurrences still bite.** The gap-closing tests pre-applied the
  NUnit4002 (`Is.Zero`), NUnit2056 (`EnterMultipleScope`), and CA1861
  (hoist constant arrays to `static readonly`) rules from the ledger.

## Follow-ups (open)

- **RBAC viewer check**: the new endpoints are marked "Authenticated"; confirm
  team/portfolio *viewer* roles (not just admins, which the integration tests
  exercised) can read them.
- **Lighthouse-Clients**: extend CLI + MCP clients to fetch the new endpoints
  (and the sibling stale-items / time-in-state-for-items data).
- **Telemetry KPIs blocked**: `OUT-cumulative-state-time-item-filter-usage` and
  siblings need per-instance counters; self-hosted instances do not phone home
  (Epic 5015, no timeline). (See `project_self_hosted_telemetry_gap`.)

## Pointers

- Decision log + slices + ACs: `docs/feature/state-time-cumulative-view/feature-delta.md`, `slices/`
- Mutation report: `docs/feature/state-time-cumulative-view/deliver/mutation/mutation-baseline.md`
- Backend: `Services/Implementation/{BaseMetricsService,TeamMetricsService,PortfolioMetricsService}.cs`, `API/DTO/CumulativeStateTime*.cs`, `Architecture/CumulativeStateTimeSeamArchUnitTest.cs`
- Frontend: `components/Common/Charts/CumulativeStateTime{Chart,ItemPicker}.tsx`, `pages/Common/MetricsView/{BaseMetricsView,ragRules}.tsx`, `components/Common/WorkItemsDialog/`
