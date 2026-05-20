# DISTILL — Acceptance tests for `feature-size-closed-date-toggle` (ADO #5036)

Adapted for Lighthouse's actual test stack — Vitest + React Testing Library at the component level, Playwright (one minimal spec) at the E2E level. The `.feature` / pytest-bdd shape from the generic `nw-distill` methodology is not used here; each scenario maps to one Vitest `it()` block or one Playwright `test()` block. The Given/When/Then prose stays as the authoritative specification.

**Driving ports**:
- Component prop API: `FeatureSizeScatterPlotChart({ sizeDataPoints, sizePercentileValues, estimationData })` — the React component exposed at `Lighthouse.Frontend/src/components/Common/Charts/FeatureSizeScatterPlotChart.tsx`.
- User-visible toggle: the `ToggleButtonGroup` rendered with `aria-label="Y-axis mode"` inside the same component, plus a fresh `aria-label="X-axis mode"` once the chart has 3 dimensions (the existing label is misleading once the toggle controls more than just Y).

**Existing test file (extend, don't duplicate)**: `Lighthouse.Frontend/src/components/Common/Charts/FeatureSizeScatterPlotChart.test.tsx`.

**New test file**: none — all new Vitest scenarios go into the file above. The single new Playwright spec goes at `Lighthouse.EndToEndTests/tests/specs/metrics/FeatureSizeChartToggle.spec.ts` (new folder `metrics/` since none exists yet).

---

## Walking-skeleton scenario (drives the implementation)

### WS-01: Closed-Date mode swaps axes and pins unclosed items at "today"

**Driving port**: React component `FeatureSizeScatterPlotChart` props + the `ToggleButtonGroup` user interaction.

**Scenario**:
```
Given the chart is rendered with three features:
  - F1: closed, size = 4, closedDate = 2026-03-10
  - F2: closed, size = 7, closedDate = 2026-04-15
  - F3: in progress (stateCategory = "Doing"), size = 5, closedDate = unset/now
And  percentiles = [{ percentile: 50, value: 4 }, { percentile: 85, value: 7 }]
And  no estimation unit is configured
When the user clicks the "Closed Date" toggle button
Then the X-axis label reads "Closed Date"
And  the Y-axis label reads "Size (Child Items)"
And  the scatter series places F1 at (x = 2026-03-10, y = 4)
And  the scatter series places F2 at (x = 2026-04-15, y = 7)
And  the scatter series places F3 at (x = today, y = 5)
And  exactly one reference line is rendered at x = today with label "Today"
And  the size-percentile reference lines are drawn horizontally (y = 4 and y = 7)
And  no DbUpdateConcurrencyException nor unhandled error is logged in the test runner
```

**Test file**: `Lighthouse.Frontend/src/components/Common/Charts/FeatureSizeScatterPlotChart.test.tsx`.
**Test name**: `Closed Date mode swaps axes, pins unclosed items at today, and flips percentile orientation`.

**Why this is the walking skeleton**: it exercises every novel behaviour of the slice — toggle activation, axis swap, today-pin for unclosed items, percentile orientation flip — in a single component render. Failing this scenario means the slice has not shipped. Tag (informal): `@walking_skeleton @in-memory`.

---

## Milestone scenarios (one TDD cycle each in DELIVER)

### M-01: Toggle renders all three options when an estimation unit is configured

**Driving port**: React component props.

```
Given the chart is rendered with at least one feature
And  estimationData.status = "Ready"
And  estimationData.estimationUnit = "T-Shirt"
When the chart mounts
Then a ToggleButtonGroup is in the document
And  it contains exactly three buttons with the labels: "T-Shirt", "Cycle Time", "Closed Date"
And  the "T-Shirt" button has aria-pressed=true (default mode preserved per DWD-04)
```

**Test name**: `Toggle renders T-Shirt / Cycle Time / Closed Date when estimation unit configured`.

### M-02: Toggle renders two options when no estimation unit is configured

**Driving port**: React component props.

```
Given the chart is rendered with at least one feature
And  estimationData is undefined OR estimationData.status != "Ready" OR estimationData.estimationUnit is falsy
When the chart mounts
Then a ToggleButtonGroup is in the document
And  it contains exactly two buttons with the labels: "Cycle Time", "Closed Date"
And  the "Cycle Time" button has aria-pressed=true (default mode preserved per DWD-04)
```

**Test name**: `Toggle renders Cycle Time / Closed Date when estimation unit absent`.

### M-03: Cycle Time mode is unchanged (regression guard)

**Driving port**: React component props.

```
Given the chart is rendered with three closed features of varying cycle times and sizes
And  percentiles = [{ percentile: 50, value: 4 }, { percentile: 85, value: 7 }]
And  estimationData is undefined
When the chart mounts (default mode = cycleTime)
Then the X-axis label reads "Size (Child Items)"
And  the Y-axis label reads "Cycle Time (days)"
And  size-percentile reference lines are drawn vertically (x = 4 and x = 7)
And  no horizontal percentile line is drawn
And  no "Today" reference line is drawn
```

**Test name**: `Cycle Time mode keeps the existing X = Size / Y = Cycle Time layout`.

This scenario is an explicit regression guard for the existing behaviour: the slice must not silently mutate Cycle Time mode while shipping Closed Date mode.

### M-04: Estimation mode is unchanged (regression guard)

**Driving port**: React component props.

```
Given the chart is rendered with three closed features and matching estimationData (status = "Ready", unit = "T-Shirt", non-numeric category values)
And  the user has not interacted with the toggle
When the chart mounts (default mode = estimation)
Then the X-axis label reads "Size (Child Items)"
And  the Y-axis label reads "T-Shirt"
And  the Y-axis valueFormatter maps numeric values to category names (existing categoryValues lookup preserved)
And  no "Today" reference line is drawn
```

**Test name**: `Estimation mode keeps the existing X = Size / Y = Estimation layout including category formatter`.

### M-05: Switching modes preserves percentile values and data points (no data loss)

**Driving port**: User interaction on the `ToggleButtonGroup`.

```
Given the chart is rendered with three closed features and two percentile values
When the user clicks "Closed Date" then clicks "Cycle Time"
Then the rendered series in Cycle Time mode contains the same three group keys as before the round-trip
And  the percentile lines are once again vertical at the same x values
```

**Test name**: `Round-trip through Closed Date mode preserves Cycle Time mode data and percentile orientation`.

### M-06: Empty data set still renders an empty-state message (regression guard)

**Driving port**: React component props.

```
Given the chart is rendered with sizeDataPoints = []
When the chart mounts in any default mode
Then the empty-state Typography ("No data available") is rendered
And  no toggle button group is rendered
```

**Test name**: `Empty data set renders no toggle and shows No data available`.

### M-07: All-unclosed dataset shows every item at the "today" marker

**Driving port**: React component props + toggle interaction.

```
Given the chart is rendered with two features both in stateCategory = "Doing", sizes 3 and 6
And  percentiles = []
When the user clicks "Closed Date"
Then both features are plotted at x = today
And  exactly one "Today" reference line is drawn
And  the Y axis spans [0, max(size) * 1.1] (per existing getMaxYAxisHeight rule applied to Y instead of X)
```

**Test name**: `All-unclosed dataset stacks every point at the today marker`.

This scenario is intentionally written to fail loudly if a future refactor accidentally swaps `today` for a sentinel like `null` or `NaN` — both have caused production crashes in similar charts in this codebase.

### M-08: Closed-date X-axis valueFormatter renders month-year labels, not raw timestamps

**Driving port**: React component props + toggle interaction.

```
Given the chart is rendered with features closed in 2026-03 and 2026-04
When the user clicks "Closed Date"
And  the X axis is queried for its valueFormatter
Then the formatter applied to 2026-03-10 returns a string matching /^Mar 2026$|^03\/2026$|^Mar 10, 2026$/ (any short month-year-ish form — exact format pinned by reference to BarRunChart's formatter)
And  the formatter does NOT return the raw epoch number
```

**Test name**: `Closed Date X axis renders human-readable month/year labels`.

The regex stays permissive so the DELIVER step can align to whichever helper is reused — the assertion is that **a** human-readable formatter is wired, not which one.

---

## Playwright E2E scenarios (one spec only)

### E2E-01: Closed-Date toggle is visible on the metrics page and persists across category switches

**Driving port**: User clicks through `MetricsPage` → `PortfolioAndFeatures` category → Feature Size widget.

```
Given a seeded portfolio with at least one closed feature exists
And  the user navigates to the portfolio's Metrics page
When the user opens the "Portfolio and Features" category
And  the user clicks the "Feature Size" widget toggle "Closed Date"
Then the toggle button "Closed Date" is in the aria-pressed=true state
When the user navigates to another category and back
Then the chart remembers its mode within the lifetime of the page (no full reload) — toggle is still on "Closed Date"
```

**Test file**: `Lighthouse.EndToEndTests/tests/specs/metrics/FeatureSizeChartToggle.spec.ts` (new).
**POM extension**: `MetricsPage.ts` gains
- `getFeatureSizeChartToggle(): Locator` returning the `ToggleButtonGroup` aria-label query.
- `clickFeatureSizeMode(mode: "cycleTime" | "closedDate" | "estimation")` returning a `Promise<void>`.

The E2E intentionally does **not** assert axis labels or data shapes — those are pinned by Vitest. The E2E proves only that the toggle wiring reaches the user via the real React tree, the real metrics fetch, and the real POM the screenshot suites depend on.

Per [[feedback_run_playwright_before_commit]], this spec MUST be run locally against a started backend before being committed.

---

## Test-data factory contract

A factory `getMockFeatureForSizeChart(overrides?: Partial<IFeature>): IFeature` should be added (or reused if present) under `Lighthouse.Frontend/src/tests/`. It produces a `Feature` instance with sensible defaults:
- `id`: monotonic
- `stateCategory`: `"Done"`
- `closedDate`: a fixed date `new Date("2026-04-01T00:00:00Z")`
- `size`: 1
- `cycleTime`: 1
- `workItemAge`: 0
- `isBlocked`: false

The factory output must validate against the production `Feature` class (`Lighthouse.Frontend/src/models/Feature.ts`) — never `as`-cast in tests. Per CLAUDE.md *TDD & Tests*.

---

## Mandate 7: RED-ready scaffolding

The slice modifies an **existing** component (`FeatureSizeScatterPlotChart.tsx`). No new module is introduced — therefore no `__SCAFFOLD__` stub file is needed; the production module already imports cleanly and the new tests start RED purely because the toggle option, axis labels, and percentile orientation don't yet exist in the implementation.

**Verification of RED state for the DELIVER hand-off**: `pnpm test --run FeatureSizeScatterPlotChart` MUST show:
- M-01, M-02 failing because the toggle has 2 options (or none) instead of 3 (or 2) per DWD-03.
- WS-01, M-05, M-07, M-08 failing because no `closedDate` branch exists yet.
- M-03, M-04, M-06 PASSING — they are regression guards against existing behaviour.

If any of M-03 / M-04 / M-06 fail at this point, the existing implementation has already drifted and that needs investigating before the slice proceeds.

---

## Adapter coverage table (Mandate 6)

| Adapter | `@real-io` scenario | Covered by |
|---------|---------------------|------------|
| React component → DOM (MUI ToggleButton click) | YES | WS-01 + M-01 + M-02 (Vitest renders the real component into JSDOM) |
| React component → MUI X-Charts (`ChartsContainer`, `ChartsReferenceLine`) | YES — under the existing `vi.mock("@mui/x-charts", ...)` shim, the *consumer* of the shim is verified | WS-01 (props inspection of the shim) |
| Browser → React tree (real Vite-bundled chart in Playwright) | YES | E2E-01 |
| Closed-date data path on `IFeature` | YES (in-memory factory) | WS-01 + M-07 + M-08 |
| Backend feature-size endpoint | N/A — slice does not change it (DWD-11) | — |

No "MISSING" rows. The chart's only driven adapter is MUI X-Charts; the existing test file already shims it (`FeatureSizeScatterPlotChart.test.tsx:39`), so the contract verified by Vitest is "the component passes the right props into the shim", and the contract verified by Playwright E2E-01 is "the real MUI library accepts those props in a real browser".

---

## Pre-requisites (carried into DELIVER)

- `closedDate` is already wired through `Feature.ts` and the metrics serialiser. The DELIVER step should confirm with one console-log spike that real backend responses populate `closedDate` for completed features. If they do not, that is a backend bug to file separately (out of scope per DWD-11).
- The 3-option toggle requires `ToggleButton aria-label` and `ToggleButton value` for `"closedDate"`. These strings are the contract that Vitest + Playwright both query against — pick them in M-01 / M-02 and keep them stable through DELIVER.
- The MUI X-Charts `ChartsReferenceLine` API accepts both `x={...}` and `y={...}` props. Verify against the installed version of `@mui/x-charts` (see `package.json`) before relying on `y={...}` — if missing, fall back to a horizontal annotation slot.

---

## Self-review checklist

- [x] WS strategy declared in `wave-decisions.md` (DWD-01: Strategy A).
- [x] WS scenarios tagged correctly (informal `@walking_skeleton @in-memory`).
- [x] Every driven adapter has at least one `@real-io` scenario (Playwright E2E-01 covers the real chart in a browser).
- [x] InMemory shim (`vi.mock("@mui/x-charts")`) — documented in adapter coverage table as a known limitation; the real browser path is covered by Playwright.
- [x] Driving Adapter: `ToggleButtonGroup` click via JSDOM (Vitest) and via real browser (Playwright) are both exercised; the component prop API is exercised by every Vitest scenario.
- [x] Mandate 7 N/A — no new modules introduced.
- [x] 40%+ error/edge case coverage — M-06 (empty data), M-07 (all-unclosed), M-03/M-04 (regression guards) = 4/8 milestone scenarios.
- [x] Round-trip / state-preservation scenario included (M-05).
- [x] Playwright spec is minimal and pushes implementation-invariant assertions to Vitest, per [[feedback_ci_and_e2e_minimalism]].

---

## Handoff to DELIVER

The roadmap.json for DELIVER should sequence the steps as:

1. M-03 (regression guard for Cycle Time mode) — add and confirm PASSING against the current code.
2. M-04 (regression guard for Estimation mode) — add and confirm PASSING.
3. M-06 (regression guard for empty data) — add and confirm PASSING.
4. M-02 (toggle visible without estimation) — RED → GREEN, smallest change to render the toggle group conditionally.
5. M-01 (toggle visible with estimation, 3 options) — RED → GREEN, extends M-02.
6. WS-01 (closed-date mode end-to-end at component level) — RED → GREEN, the meat of the slice.
7. M-05 (round-trip) — RED → GREEN, tightens the state machine.
8. M-07 (all-unclosed) — RED → GREEN, hardens the today-marker code.
9. M-08 (X-axis formatter) — RED → GREEN, polishes labels.
10. E2E-01 (Playwright spec) — RED → GREEN, run locally first per memory.

After all are green and `pnpm test && pnpm build` are clean, the slice is ready for `git push` (wait for CI green) and then ADO #5036 transitions `Active` → `Resolved`.
