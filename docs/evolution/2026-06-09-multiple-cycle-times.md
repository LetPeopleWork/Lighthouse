# Evolution: multiple-cycle-times

- **Date finalized**: 2026-06-09
- **ADO**: Epic #5251 ("Multiple Cycle Times"), Stories #5252–#5256. All delivered (#5252/#5255 Closed, #5253/#5254/#5256 Resolved).
- **Status**: Delivered on `main` (DISCUSS → DESIGN → DISTILL → DELIVER all done; CI green; both Sonar gates `new_violations = 0`; mutation ≥ 80% on the feature surface — FE 91.7%, BE named-cycle core 84.5% / validator 82.1%).
- **Workspace (history)**: `docs/feature/multiple-cycle-times/`
- **Builds on**: [state-time-cumulative-view](./../feature/state-time-cumulative-view/feature-delta.md) (the Cumulative Time per State chart) and the Cycle Time Scatterplot / Cycle Time Percentiles widgets it extends.

## What shipped

Lighthouse now lets a premium config-admin define **named cycle times** — additional
start→end measurements over the workflow alongside the built-in Cycle Time — and read
each one on the scatterplot, the percentiles, and the cumulative-time-per-state chart, at
both Team and Portfolio scope. A "Lead Time (Backlog→Done)" and an "Analysis to Done" can
live side by side, and you switch between them without leaving the chart.

A named cycle time is `{ name, startState, endState }` with **half-open
`[enter start … enter end)`** semantics over the owner's ordered `AllStates` (first-crossing
of the start boundary; the end state stops the clock and contributes no dwell). The built-in
`WorkItemBase.CycleTime` is untouched.

- **US-01 (walking skeleton)** — `NamedCycleTimeDays` on `BaseMetricsService` reusing the
  single transition-ordering primitive (`OrderedStateEntries`); Team `cycleTimeData` /
  `cycleTimePercentiles` extended with an additive `definitionId`; the scatterplot gained a
  **Default + named** selector that re-plots the dots and recomputes 50/70/85/95 when a named
  definition is chosen (premium-gated via `useRbac`, no read-side license gate — ADR-062 rev).
- **US-02 (config + persistence)** — a `CycleTimeDefinition` owned collection on
  `WorkTrackingSystemOptionsOwner`, JSON-serialized like `StateMappings`, with a stamped
  read-time `IsValid` (presence of both boundaries in `AllStates`). The `CycleTimesEditor`
  config UI (mapping-aware, workflow-ordered boundary pickers) sits in the Team settings form;
  save-time validation enforces only end-after-start ordering (D4), presence is read-time (D5).
- **US-03 (invalid-on-removal)** — when a boundary state is later removed/reordered, the
  definition persists as `IsValid:false`: the scatter selector and cumulative scope disable it
  with a warning, the reads refuse it (fall back to Default/unscoped, never 500), and a shared
  TS `isCycleTimeDefinitionValid` mirrors the backend predicate.
- **US-04 (cumulative scope)** — the Cumulative Time per State chart gained a
  **"Scope to cycle time"** selector. Choosing a definition switches the displayed states to
  the ordered span `[startState … endState)` of `AllStates` (so earlier states such as Backlog
  appear and the end state drops out), while keeping the completed-vs-ongoing split from the
  To Do/Doing/Done mapping (it is *not* completed-only). The shared bar tooltip also got a
  readable themed background panel as part of this.
- **US-05 (Portfolio parity)** — the full Team build at Portfolio scope, over Features
  (materialised to `WorkItem` with transitions). The owner-agnostic boundary helpers
  (`ResolveBoundaryState`, `ResolveValidNamedDefinitions`, `NamedValuesForItem`,
  `BuildPercentiles`, `ScopedCumulativeStateOrder`) were lifted into `BaseMetricsService` so
  Team and Portfolio share one implementation; persistence/validity were already shared via
  `SettingsOwnerDtoBase`, so read-your-writes needed no new wiring.

## Key decisions

- **ADR-061** — metrics-layer compute; the named ordered-boundary duration is a `protected`
  helper on `BaseMetricsService` that reuses the existing transition walk. `WorkItemBase.CycleTime`
  and the model stay untouched (no parallel engine — enforced by a seam ArchUnit test).
- **ADR-062 (revised)** — list-shaped `WorkItemDto.NamedCycleTimes`; the named branch is an
  additive `definitionId` on the *existing* endpoints (no new endpoint, so no client version gate).
  **No read-side premium gate** — the premium gate lives on definition create/update only; a viewer
  reads the named branch like the Default.
- **ADR-063** — `IsCycleTimeDefinitionValid` is the single backend presence predicate, stamped as
  `IsValid` on the projection; one SSOT, no recomputation elsewhere.
- **ADR-064** — `CycleTimeDefinitions` persisted as a JSON column (ValueConverter/ValueComparer)
  on the owner, mirroring `StateMappings`; one EF migration across both providers.

## Cross-cutting

- **RBAC** — definition create/update is premium + Team/Portfolio-admin (via the settings write
  path + the editor's own premium gate); reads are not gated (ADR-062 rev).
- **Lighthouse-Clients (CLI + MCP)** — no client change required: the named branch is an additive
  query parameter on endpoints the clients already wrap, not a new endpoint, so no
  `FEATURE_REQUIRES_SERVER_NEWER_THAN` entry.
- **Website** — premium feature; surface on the website at the next marketing pass (not done here).

## Durable lessons

- **Whole-file mutation is misleading on giant shared service files.** Stryker.NET mutates whole
  files; the named-cycle logic lives inside `BaseMetricsService` / `TeamMetricsService` /
  `PortfolioMetricsService` next to throughput/WIP/PBC/forecast. A named-cycle-only test filter
  leaves the unrelated methods' mutants surviving as false negatives (43% aggregate). Measure the
  **named-cycle line ranges** (84.5% core) + the focused `CycleTimeDefinitionValidator` (82.1%),
  and justify the rest rather than writing vacuous tests. See `deliver/mutation/mutation-report.md`.
- **Scoping the cumulative chart is about which *states* show, not just dwell clipping.** The first
  attempt clipped completed visits to the window and dropped the ongoing segment; the correct model
  is "the default per-state computation over the half-open span of states", preserving the
  completed/ongoing split. (User review caught this.)
- **A presence-valid definition can still be reversed.** `IsValid` checks boundary presence, not
  ordering; if states are reordered after save, a definition's end can sort before its start.
  `NamedCycleTimeWindow` now guards `endThreshold <= startThreshold` (symmetric with
  `ScopedCumulativeStateOrder`) so the scatter/percentiles return null rather than a 1-day artifact.
- **The pre-commit ledger hook earns its keep.** It blocked a CA1861 inline-assertion-array twice
  during this epic before it could reach the Sonar gate.
