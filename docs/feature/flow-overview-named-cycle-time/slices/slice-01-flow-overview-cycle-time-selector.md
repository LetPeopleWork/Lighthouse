# Slice 01 - Flow Overview cycle-time selector + named percentiles + neutral RAG + View Data

**Type:** vertical | **Est:** ~1 day | **Stories:** US-01, US-02

## Learning hypothesis

A named cycle time reads correctly on the Flow Overview `percentiles` widget by lifting the selection
to `BaseMetricsView` (the shipped `cumulativeScopeDefinitionId` pattern) and reusing the EXISTING
`cycleTimePercentiles?definitionId=` endpoint - no new read contract - while a `minWidth: 200` MUI
Select still fits a `size: "small"` widget above its percentile table.

**Disproves if it fails:** that the Overview widget can host the selector at all without a layout
redesign (its 3-col x 2-row footprint may not take a Select + 4 table rows). If this fails, the story
needs a widget-size or spotlight-only rethink BEFORE any backend work - which is exactly why this
slice runs first.

**Confirms if it succeeds:** the lifted-state pattern generalises from the cumulative chart to any
Overview widget, making US-03's trend wiring a pure add.

## What ships

- **Frontend only.** No backend change - `getCycleTimePercentiles(id, start, end, definitionId)`
  (`MetricsService.ts:332`) and `GetNamedCycleTimePercentilesForTeam` (`TeamMetricsService.cs:340`)
  already exist and are reused unchanged.
- Selection state lifted to `BaseMetricsView` (`percentilesScopeDefinitionId` +
  `onPercentilesScopeChange`), mirroring `cumulativeScopeDefinitionId` (`BaseMetricsView.tsx:1191, 1462`).
  Consumed ONLY by the `percentiles` widget - the scatterplot's own local state is NOT touched.
- A controlled selector on `CycleTimePercentiles`, shaped after `CumulativeStateTimeScopeControl.tsx`:
  `null` -> "Default"; early-return `null` when `namedCycleTimeDefinitions.length === 0`; `useEffect`
  self-reset when the selected definition disappears or goes `isValid === false` (D5).
- Named percentiles fetched on selection; RAG branches to `ragStatus: "none"` + the D11 tip (the
  `WidgetShell` header tip tooltip - hover-only, no card-body caption; DELIVER amendment [A1]).
- `buildViewData()` (`BaseMetricsView.tsx:466`) takes the selection: conditional `highlightColumn`
  (named title + `item.namedCycleTimes` lookup, D15), rows filtered to items carrying a named value
  (D16), no `sle` passed for a named selection.
- Team AND Portfolio (both already thread `cycleTimeDefinitions` through `BaseMetricsView`).

## IN scope

- Selector, named percentiles, neutral RAG + tip, View Data column + row filter.
- Premium gate (free - `namedCycleTimeDefinitions` is `[]` when `!isPremium`), empty-definitions case,
  invalid-definition reset, Portfolio scope.
- Vitest + RTL for the widget; E2E through the Page Object Model, driven from demo data.

## OUT of scope

- Trend under a named selection (slice 02 - the endpoint has no `definitionId` yet).
- Any scatterplot / `cycleTimePbc` / `workItemAgePercentiles` change.
- Persisting or sharing the selection across tabs or reloads.

## Production-data AC

Driven from real demo data (`DemoDataFactory.cs:74`). Demo workflow order: `Backlog` | `Next`,
`Analysing`, `Implementation`, `Waiting for Verification`, `Verification` | `Done`. The demo's default
cycle time therefore starts at `Next` (first Doing state), so `Lead Time (End to End)` (Backlog->Done)
is strictly wider by the whole Backlog wait - the point of the feature, and the reason it is the
E2E's named selection.

- Given the premium demo Team, when Priya opens Flow Overview, then the Cycle Time Percentiles widget
  shows a selector reading "Default" and listing `Lead Time (End to End)` and `Analysis to Done`.
- Given Priya selects a named definition, when the widget re-renders, then the 50/70/85/95 recompute
  over that definition's window and the RAG chip is suppressed (neutral, `ragStatus: "none"`) - NOT a
  red derived from the default SLE. The explanation ("SLE applies to the Default cycle time. Named
  cycle times have no SLE target.") lives in the widget-header tip tooltip, NOT as a caption in the
  card body - see DELIVER amendment [A1]; the small widget has no vertical room for a caption line.
  DELIVERED with two definitions: `Lead Time (End to End)` for the >= Default property (each named
  value >= the Default value for the same range - a wider window cannot be faster), and
  `Analysis to Done` for the genuine re-plot, because on demo data `Lead Time (End to End)` returns
  values identical to Default (zero Backlog dwell - see amendment [A2]).
- Given `Lead Time (End to End)` is selected, when Priya opens View Data, then the highlight column is
  titled `Lead Time (End to End)`, every listed item carries a value for that definition (no blanks,
  D16), the row count equals the population the percentiles were computed over, and no SLE line is
  drawn.
- Given the selector is on "Default", when Priya reads the widget, then percentiles, RAG and View Data
  are unchanged from today.
- Given `Lead Time (End to End)` is selected and an admin then removes `Backlog` from the demo Team's
  To Do states (invalidating that definition's start boundary, D5), when the definitions reload, then
  the selection resets to "Default", the definition is not selectable, and nothing crashes.
- Given Priya then opens the Flow Metrics tab, when the scatterplot renders, then its selector still
  reads "Default" (no cross-tab coupling).
- Given the premium demo Portfolio, when Priya opens its Flow Overview, then the selector behaves
  identically to Team scope.

## Dependencies

- Epic 5251 SHIPPED (definitions, named computation, premium gate, read endpoint) - satisfied.
- Premium seed for `@premium` E2E (`reference_premium_license_dev_seed`) - satisfied.
- Demo data named definitions - **satisfied, verified**: `DemoDataFactory.cs:74` seeds
  `Lead Time (End to End)` (Backlog->Done) and `Analysis to Done` (Analysing->Done) onto both the demo
  Team and Portfolio. Use the REAL demo names in fixtures; "Concept to Cash" is narrative shorthand only.
  **Superseded by DELIVER amendment [A2]:** this planned to drive the E2E from `Lead Time (End to End)`
  on the assumption its Backlog->Done span is much wider than the default started->finished window.
  Measured, it is not - demo items enter `Backlog` and `Next` the same day, so it returns values
  identical to Default and cannot exercise the false-red case. `Analysis to Done` is the definition that
  demonstrably re-plots; the E2E drives both.

## Effort estimate / reference class

~1 day. Reference class: Epic 5251 slice-04 (`cumulative-chart-scope-switch`) - the same shape
(controlled scope selector + lifted state + invalid reset) against an existing endpoint. This slice
adds the RAG branch and the ViewData conditional, and subtracts the backend work.

## Pre-slice SPIKE

**None needed for the logic** - the pattern is shipped and the endpoint exists. If the `size: "small"`
layout (Risk c) looks doubtful at DESIGN, timebox a 30-minute layout probe on the real widget before
committing; do not spike the data path.

## Taste tests

- **Value-bearing**: Priya reads a named window's percentiles on the page she already lands on and
  inspects the items behind them. PASS.
- **Not 4+ new components**: 1 new (the selector, likely a near-copy of the shipped scope control) +
  4 modified. PASS.
- **Abstraction-first**: no new abstraction - reuses the shipped lifted-state pattern and endpoint. PASS.
- **Disproves a pre-commitment**: yes - the layout assumption (Risk c), which gates the story. PASS.
- **Production data, not synthetic**: AC are driven by the real seeded demo definitions
  (`Lead Time (End to End)`, `Analysis to Done`) against the real demo workflow, and the
  wider-window-cannot-be-faster assertion is a genuine property of the data, not a plumbing check. PASS
  (Risk (a) closed - the definitions are already seeded).
- **Dogfood same day**: Flow Overview is the landing page; the switch is demoable the moment it ships. PASS.
- **Not identical-except-scale to slice 02**: different surface (percentiles vs trend), different
  stack (FE-only vs BE+FE). PASS.
