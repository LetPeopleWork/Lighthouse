# ADR-020: Per-State Bands — Extend Existing `WorkItemAgingChart` via Custom SVG Overlay (not new widget; not `ChartsReferenceLine` per-state)

**Status**: Accepted (2026-05-24 — Morgan, interaction mode PROPOSE; bundled with ADR-019/021)
**Date**: 2026-05-24
**Feature**: aging-pace-percentiles (Epic 4144 MVP bundle, slice F)
**Decider**: Morgan (Solution Architect)

---

## Amendment (2026-05-25) — feature simplification

The feature was scoped down with the user to "the coloring with the toggle, nothing more". This ADR is amended; the body below is **superseded where it conflicts** (see `aging-pace-percentiles/feature-delta.md` D6, DDD-6, DDD-8):

1. **Bands are filled colored `<rect>` zones, not dashed `<line>` segments.** Per state column with data, render a stacked set of `<rect>`s spanning `x ∈ [stateIndex−0.4, stateIndex+0.4]` and the consecutive percentile boundaries in Y (`0→p50→p70→p85→p95→top`), filled from the `ForecastLevel` green→red palette, rendered **before the dots** so they sit behind them. The §Decision "Option B / custom SVG `<line>`" and Option E discussion are superseded on the *primitive* (rect vs line) — but the **core decision is unchanged**: extend `WorkItemAgingChart` via a custom SVG overlay inside the existing `<ChartsContainer>` coordinate system; sibling widget (C) and chart replacement (D) remain rejected for the same reasons.
2. **One on/off toggle, not two chip groups.** A single `showPaceBands` boolean (one **Pace percentiles** chip in `PercentileLegend`, **off by default**) gates the whole overlay. §3's `visiblePerStatePercentiles` map and the `useChartVisibility` extension are **dropped** — `useChartVisibility` is unchanged; `showPaceBands` is a trivial local `useState`. §4's "second chip group with sub-header" becomes one chip. The existing percentile/SLE chips are untouched.
3. **No low-sample tooltip, no per-segment hover, no `sampleSize`.** §6's low-sample handling and `IPerStatePercentileValues.sampleSize` are removed; a state with no data simply renders no band.

Unchanged: extend-not-replace (reject C/D), the optional `perStatePercentileValues` prop with absent/empty ⇒ today-identical rendering, in-`ChartsContainer` coordinate system (no absolute positioning), and the data plumbing through `useMetricsData` / `BaseMetricsView` (now for both team and portfolio).

---

## Context

DISCUSS D6 locked the visual treatment: per-state bands render as **short horizontal segments anchored to each state column** (not full-width lines), at the same dashed style as today's full-width CT bands. The legend has TWO chip groups: `Cycle Time %iles (overall)` (existing full-width) and `Age-in-State %iles (per state)` (new per-column segments).

The existing chart (`WorkItemAgingChart.tsx:349-388`) renders today's full-width CT bands as MUI-X `ChartsReferenceLine` elements with `y={value}` — these draw a horizontal line across the entire X axis. MUI-X `ChartsReferenceLine` accepts `x` OR `y`, not a partial `x-range`. There is no built-in "segment from `x = i - 0.5` to `x = i + 0.5` at `y = v`" primitive in MUI-X-charts.

The pre-slice spike candidate in `slice-01-per-state-bands-team.md` flagged exactly this: *"verify `ChartsReferenceLine` can be constrained to a sub-range of the X axis. If not, identify the alternative primitive (custom SVG layer over the chart canvas) and re-estimate."* DESIGN must pick the rendering approach now so that DELIVER does not stall on a discovery moment.

Two further axes of decision:

- **Where the rendering lives**: extend the existing `WorkItemAgingChart` component vs. introduce a new sibling widget vs. compose a new chart.
- **How the rendering is wired through state**: extend `WorkItemAgingChart`'s prop interface with `perStatePercentileValues?: ...` (the DISCUSS feature-delta's stated approach) vs. extend `BaseMetricsView`'s context vs. introduce a parallel widget instance.

Plausible rendering strategies:

- **A. Multiple `ChartsReferenceLine` per state column**: render four `ChartsReferenceLine` per state (one per percentile), each with `y = percentileValue`. Not supported: the line spans full width and there is no per-column clip. **Not technically feasible.**
- **B. Custom SVG `<line>` overlay rendered as a child of the `ChartsContainer`**: MUI-X `ChartsContainer` accepts arbitrary SVG children that participate in the same coordinate system. The chart already passes data through `xAxis.data = doingStates.map((_, index) => index)` — so a `<line x1={i - 0.4} x2={i + 0.4} y1={v} y2={v}>` rendered inside the same `<ChartsContainer>` lands in the correct chart coordinates, with the existing X scale's `valueMin = -0.5` / `valueMax = doingStates.length - 0.5` (per the existing chart's xAxis config). Stroke-dasharray reuses the same `"5 5"` style as today's CT bands. Color reuses the same `ForecastLevel(percentile).color` palette.
- **C. New sibling widget rendered next to `WorkItemAgingChart`** (e.g. `PerStatePercentileBandsChart`): a separate chart with the same X-axis layout as `WorkItemAgingChart` but showing only the bands.
- **D. Replace the chart entirely with a custom-rendered chart** (e.g. plain SVG, drop MUI-X for this widget): full control over per-state bands, but discards the existing chart's tooltip / dot rendering / state-column gridlines / SLE line / type-color legend.

The DISCUSS-locked outcome — *both* CT lines and per-state bands visible **alongside** each other on the **same** chart with **independent** toggles (D6, US-01 AC line 2, US-02 AC) — narrows this: a sibling widget (C) loses the "alongside" semantics; a replacement chart (D) breaks every test in `WorkItemAgingChart.test.tsx`. Both leave the user staring at twice the visual furniture for the same conversation.

---

## Decision

**Option B — Custom SVG `<line>` segments rendered as children of the existing `WorkItemAgingChart`'s `<ChartsContainer>`, anchored to each state column's X index. Extend the existing chart component; do NOT introduce a sibling widget.**

The architectural contract:

1. **Chart component interface** — `WorkItemAgingChart` accepts a new optional prop `perStatePercentileValues?: IPerStatePercentileValues[]` where:

    ```
    // AMENDED 2026-05-25 (DDD-4): sampleSize dropped
    interface IPerStatePercentileValues {
        state: string;
        percentiles: IPercentileValue[];
    }
    ```

    When the prop is absent OR empty, the chart renders exactly as today (no regression — guarded by an existing-snapshot test per slice-01-per-state-bands-team.md acceptance criteria). Backwards-compatible by construction.

2. **Rendering** — inside the existing `<ChartsContainer>`, after the existing `ChartsReferenceLine` mappings (lines 349-388 of `WorkItemAgingChart.tsx`), render one SVG `<line>` per `(state, percentile)` pair where both the percentile is visible (per the new chip group's toggle state) and the state has a non-empty entry in `perStatePercentileValues`. The `<line>` uses the chart's coordinate system (the same one `ChartsReferenceLine` uses) — `x1 = stateIndex - 0.4`, `x2 = stateIndex + 0.4`, `y1 = y2 = percentileValue`. Stroke = `forecastLevel.color`, `strokeDasharray = "5 5"` (matching today's CT band style), `strokeWidth = 1`. The 0.4 offset (vs the 0.5 used for the state-divider vertical lines on line 378-388) leaves a small visual gap between adjacent state columns' bands — a familiar visual idiom for "this band belongs to THIS column".

    Software-crafter implements the exact SVG container shape at GREEN; the architectural commitment is "stay inside the existing `ChartsContainer` coordinate system; do not absolute-position over the chart's bounding rect" (absolute positioning would break on container resize and on responsive layouts).

3. **Visibility wiring** — the existing `useChartVisibility` hook already maintains a `visiblePercentiles: Record<number, boolean>` keyed on the percentile integer (50/70/85/95). DESIGN extends this with a parallel `visiblePerStatePercentiles: Record<number, boolean>` (a second independent map) returned by an extended invocation of `useChartVisibility`. Sibling chip group `Age-in-State %iles` reads/writes this second map; the existing CT chip group continues to read/write `visiblePercentiles` unchanged.

    Alternative wiring (introducing `useChartVisibility` with a second percentile-set parameter) is identical in behavior; the choice between "two invocations of the same hook" vs "one invocation with two parameters" is left to software-crafter at GREEN (it is implementation shape, not architectural contract).

4. **Legend** — `PercentileLegend` is extended to accept a second percentile set rendered as a parallel chip group with its own sub-header. The component already accepts `(percentiles, visiblePercentiles, onTogglePercentile)` — DESIGN extends with optional `(perStatePercentiles, visiblePerStatePercentiles, onTogglePerStatePercentile, perStateGroupLabel)`. When `perStatePercentiles` is undefined / empty, the legend renders exactly as today (parallel to the chart's behaviour).

5. **Data plumbing** — `BaseMetricsView` passes `perStatePercentileValues={ctx.perStatePercentileValues}` to the `<WorkItemAgingChart>` rendering (lines 788-794). `ctx.perStatePercentileValues` is populated by `useMetricsData` via a new `metricsService.getAgeInStatePercentiles(entity.id, startDate, endDate)` call, in the same `useEffect` block that today populates `ctx.percentileValues` via `getCycleTimePercentiles`. The two calls run in parallel (both depend only on `entity.id, startDate, endDate`); they share the existing window-period state in `useMetricsData`.

6. **Empty / low-sample on the chart** — a state with no entry in `perStatePercentileValues` renders NO per-state segments above that column (US-01 AC line 4). The existing full-width CT bands continue to render across every column, so the chart is never empty of guidance. A state with `sampleSize < 10` renders segments normally; the low-sample tooltip lives on the legend chip and on the per-segment hover (US-02, slice 02).

---

## Alternatives Considered

**Option A — Multiple `ChartsReferenceLine` per state column.**

- Pros: would reuse the same MUI-X primitive the existing CT bands use.
- Cons: NOT TECHNICALLY FEASIBLE. `ChartsReferenceLine` does not accept an X-range; the `y={value}` line spans the entire X axis. Any attempt to clip it (CSS `clip-path`, SVG `<clipPath>` over the line element) would fight the chart's responsive sizing.
- **Rejected** as infeasible.

**Option C — New sibling widget `PerStatePercentileBandsChart` rendered next to `WorkItemAgingChart`.**

- Pros: zero changes to `WorkItemAgingChart`; no risk of regressing existing tests.
- Cons:
  1. The user is looking at the same chart conceptually (dots = in-flight items; bands = historical pace). Splitting into two widgets means the user must visually map dots from one widget to bands in another — defeating the user-story rationale ("at-a-glance pace recognition").
  2. The new widget would need to duplicate the X-axis state-column layout (`doingStates`, the state labels, the type-color filter chips that drive `inProgressItems` membership). Duplication invites drift.
  3. DISCUSS D6 explicitly says "alongside the existing full-width CT lines". A sibling widget renders them physically apart, not alongside.
- **Rejected** because it breaks the user-facing "alongside" semantics and duplicates layout machinery.

**Option D — Replace `WorkItemAgingChart` with a custom SVG-only chart that natively supports per-state segments.**

- Pros: full control over per-state bands; no MUI-X "doesn't support X-range" limitation.
- Cons:
  1. Discards every existing feature of `WorkItemAgingChart`: the scatter dot rendering, the tooltip, the SLE line, the state-divider gridlines, the type filter chips, the WIP marker behavior, the `WorkItemsDialog` integration on dot click. Re-implementing every one of these in custom SVG is ~weeks of work for zero user-visible improvement beyond the per-state bands.
  2. Existing `WorkItemAgingChart.test.tsx` snapshot / behaviour tests would all need to be rewritten.
  3. Maintenance cost diverges from the rest of the chart library (every other chart in Lighthouse uses MUI-X).
- **Rejected** as massively disproportionate. Option B reuses 100% of the existing chart investment and adds the new layer as a thin SVG overlay inside the same coordinate system.

**Option E — Render the per-state bands as additional `ScatterPlot` data-points (i.e. invisible "bar-cap" markers at the percentile heights, per state column) instead of as SVG `<line>` elements.**

- Pros: stays entirely within MUI-X's primitive set; no manual SVG.
- Cons:
  1. Markers are points, not horizontal segments. Three markers across a column do not visually communicate "the 85th percentile of this state is THIS Y value" — the user expects a horizontal line, not a constellation of dots.
  2. Markers would inherit the chart's marker styling defaults (filled, sized for visibility); overriding to match the dashed-line aesthetic requires per-marker custom rendering — at which point we are right back at custom SVG, just with worse semantics.
- **Rejected** because the resulting visual would not match DISCUSS D6's "short dashed horizontal segments".

---

## Consequences

**Positive**:

- Single chart, two band families, alongside one another — matches the DISCUSS-locked user experience.
- 100% reuse of `WorkItemAgingChart`'s scatter / tooltip / X-axis / dot-click / `WorkItemsDialog` machinery. The new code path is the SVG overlay block (~30 LOC) plus the legend extension (~20 LOC) plus the prop wiring (~10 LOC across `BaseMetricsView` + `useMetricsData`).
- Backwards-compatible: chart with `perStatePercentileValues` absent renders byte-identical (or at minimum test-id-identical) to today. The existing snapshot tests guard this.
- The custom SVG overlay sits inside `ChartsContainer` and uses the chart's coordinate system — it survives responsive resize, chart-area padding changes, and font-size changes the same way the existing `ChartsReferenceLine` elements do.

**Negative**:

- One area of custom SVG inside the otherwise-declarative MUI-X chart structure. Mitigated by colocating the overlay inside the same component file (no separate file to maintain) and by an in-component documentation note (within the JSX, not as a banned per-line comment — the architectural intent is documented HERE, in this ADR; the JSX names the overlay block).
- The 0.4 X-axis offset is a visual-tuning constant. If a future ADR changes the chart's state-column spacing (e.g. by reordering `doingStates` rendering), the overlay's segment width may need re-tuning. Acceptable: the overlay's geometry is one component-internal constant, not a cross-file contract.

**Neutral**:

- The chart now reads from TWO data sources (the existing `percentileValues` for CT bands + the new `perStatePercentileValues` for per-state bands). Both are optional from the chart's perspective; both come from the same `useMetricsData` hook; both refresh together when the window-period state changes.
- Snapshot tests for the chart will need a new snapshot for the "with per-state bands" rendering case, in addition to the existing snapshot for the "without per-state bands" case. The acceptance criterion in slice-01 already requires both.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| `WorkItemAgingChart` with `perStatePercentileValues` undefined / empty renders identically to the chart without the prop | Vitest snapshot/behavioural test asserts test-id-identical DOM output with and without the prop empty |
| Per-state bands render inside the existing `<ChartsContainer>` (so they share the chart's coordinate system) and not as an absolute-positioned overlay | Vitest test asserts the band-overlay element is a descendant of the `<ChartsContainer>` DOM root (queried by data-testid on the container) |
| The two legend chip groups are visually distinguishable (different sub-header text) | Vitest RTL test asserts both group sub-headers are present with distinct text matching the DISCUSS D6 spec |
| The new `useChartVisibility` invocation for per-state percentiles is independent of the existing CT invocation (toggling one does not affect the other) | Vitest test toggles each chip group in isolation and asserts only the targeted bands hide/show |

---

## Cross-feature impact

- `time-in-state-and-staleness`: unchanged (different chart surface — work-item table, not aging chart).
- `state-time-cumulative-view`: unchanged (different chart entirely — bar chart, not aging chart). The per-state cumulative bar chart is its own widget per sibling B3's DESIGN to come.
- The custom SVG overlay technique established here is a reusable pattern for any future chart that needs per-state visual annotations inside an MUI-X chart. Documenting it in this ADR makes it discoverable.
