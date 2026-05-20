# DISTILL wave decisions — feature-size-closed-date-toggle

ADO #5036 — *"Feature Size / View by Closed Date Toggle"*. User Story, state `Active`, tagged `Release Notes`.

The story adds a third option to the existing Y-axis toggle on the Feature Size Chart (`FeatureSizeScatterPlotChart`). Today the toggle is binary — `<Estimation Unit>` vs `Cycle Time` — shown only when a project has an estimation unit configured. The new mode `Closed Date` changes the chart's semantics:

- The axes swap. **X = Closed Date**, **Y = Size (Child Items)**. Today's chart is X = Size, Y = Cycle Time / Estimation.
- The percentile reference lines flip orientation. Today they are vertical (`ChartsReferenceLine x={p.value}`) over the Size axis. In Closed Date mode they become horizontal (`y={p.value}`), still indicating the size percentiles — only the orientation changes because Size moved from X to Y.
- Items that are not yet closed (state category `ToDo` or `Doing`) have no closed date. They are pinned at the **"today" marker** on the X axis so they remain visible to the user.

---

## DWD-01: Walking-skeleton strategy = A (Full InMemory / component-level)

The slice is a pure front-end change to one React component (`FeatureSizeScatterPlotChart.tsx`) plus its consumer (`BaseMetricsView.tsx`) and the Page-Object-Model (`MetricsPage.ts`). There is **no** backend, persistence, queue, or external system involved — `closedDate` is already on `IFeature` (`Lighthouse.Frontend/src/models/Feature.ts:62`) and `stateCategory` is already on `IWorkItem`. The walking skeleton is therefore a **Vitest unit test** against the rendered component with InMemory `IFeature[]` test data.

**Tag (informal)**: `@walking_skeleton @in-memory`.

This matches the [[ws-strategy-decision]] tree: pure-domain (no I/O ports) → Strategy A.

## DWD-02: Test framework split = Vitest (chart logic) + Playwright (toggle visibility)

Per Lighthouse conventions (`CLAUDE.md` *Test Framework*), this slice uses:

- **Vitest + React Testing Library** for *all* chart-behavior assertions (axis labels, percentile orientation, data-point coordinates, the "today" pin). Co-located at `Lighthouse.Frontend/src/components/Common/Charts/FeatureSizeScatterPlotChart.test.tsx`.
- **Playwright** for *one minimal* E2E that verifies the toggle button is rendered with the `Closed Date` option when an estimation unit is configured *and* when it is not (per DWD-03 below). The existing `MetricsPage.ts` POM gains a helper for the size-chart toggle. The E2E does **not** re-assert axis labels or data shapes — those are pinned by Vitest. Per [[feedback_ci_and_e2e_minimalism]], implementation-invariant assertions stay in Vitest, not Playwright.

There is **no** NUnit / backend test in this slice. There is **no new API** — the closed-date data already flows through the existing feature-size endpoint.

## DWD-03: Toggle is always 3 options when there is data — even without an estimation unit

The original two-option toggle was conditionally rendered only when `showEstimationToggle` was true (i.e. when the project had an `estimationUnit` configured). For ADO #5036 we change the rule:

- If **estimation unit configured**: toggle shows **`<EstimationUnit>` / `Cycle Time` / `Closed Date`** (3 options).
- If **estimation unit NOT configured**: toggle still appears, with **`Cycle Time` / `Closed Date`** (2 options). Today no toggle is shown at all in this case — the chart simply renders cycle time on Y. After this slice the user gets the choice between cycle time and closed date, which is the lower of the two value-adds but still valuable.

Rationale: the closed-date view is independent of estimation — it depends only on `closedDate`, which every project has on its features. Hiding the new mode behind an estimation-unit gate would punish users who never set up estimation.

**Confirmed by user 2026-05-20**: 3-way toggle when an estimation unit is configured, 2-way (`Cycle Time` / `Closed Date`) when it is not.

## DWD-04: Default mode preserves status quo

When the chart first mounts:

- Estimation unit configured → default = `estimation` (unchanged).
- Estimation unit NOT configured → default = `cycleTime` (unchanged behaviour, no surprise re-default to Closed Date).

Users who land on the page see exactly what they see today; the new mode is opt-in via the toggle.

## DWD-05: Unclosed items pin to "today" — exact pixel position

For items in `Doing` / `ToDo` (no `closedDate`), the description says they "should show at the today marker". Concretely:

- The X value used for plotting is `Date.now()` (component-mount time, captured once in a `useMemo` so the chart is stable during interaction). **Confirmed by user 2026-05-20** — captured at mount, not re-captured on every render.
- A `ChartsReferenceLine x={today}` reference line is drawn at `today` with label `"Today"` and a distinct stroke so the user can see the pile-up.
- Items with `stateCategory === "ToDo"` and `size === 0 || size === null` are still filtered out — same rule the chart already enforces (`FeatureSizeScatterPlotChart.tsx:399-403`).

## DWD-06: Percentile values do NOT change between modes

The percentile values come from the backend (`sizePercentileValues: IPercentileValue[]`) and describe **size**, not cycle time, regardless of toggle position. What changes is:

- `cycleTime` / `estimation` mode → percentile is drawn as a vertical line over the X (size) axis: `<ChartsReferenceLine x={p.value} />`.
- `closedDate` mode → percentile is drawn as a horizontal line over the Y (size) axis: `<ChartsReferenceLine y={p.value} />`.

The percentile labels (e.g. `"50%"`, `"85%"`) and colors stay identical. This is a pure orientation flip.

## DWD-07: X-axis scale type for closed date

The closed-date X axis uses `scaleType: "time"` and accepts JavaScript `Date` objects (or epoch milliseconds — the chart picks based on the values supplied to `data[].x`). The X axis label becomes `"Closed Date"`.

The `valueFormatter` for the X axis in closed-date mode uses `Intl.DateTimeFormat` to render a short month/year label, matching the convention used by other date-axis charts in the codebase (verify against `BarRunChart` during DELIVER — if it uses a different format, align to that).

## DWD-08: Tooltip stays content-equivalent

The single-item tooltip currently shows `"{name} - {state} (Click for details)"`. In closed-date mode the tooltip stays semantically identical — it never claimed to be axis-aware. No change required. Group tooltip ("N Closed Features (Click for details)") likewise unchanged.

## DWD-09: Skip the 4-reviewer ceremony — single-component UI slice

Per [[feedback_ci_and_e2e_minimalism]] and the precedent set by `portfolio-delete-serialise` (DWD-03 there), the four-parallel-reviewer ceremony is overkill for a single-component front-end change with no API contract, no DB schema, and no infrastructure impact. This slice goes through:

- `@nw-software-crafter-reviewer` during DELIVER (per-step adversarial review).
- `pnpm test`, `pnpm build` (with Biome), `dotnet build` (no backend changes, but CI runs it anyway).
- SonarCloud quality gate on push.
- The single Playwright E2E running green in CI.

If the slice grows (e.g. backend changes to expose closed-date percentile data), escalate.

## DWD-10: Out of scope — Closed-date percentiles (separate metric)

The user description doesn't ask for *closed-date* percentiles (e.g. "p85 of items closed within N days"). The slice keeps **size percentiles** in all three modes. A separate ADO item would introduce a closed-date / age percentile if needed — out of scope for #5036.

## DWD-11: Out of scope — backend changes

`closedDate` is already serialised on `IFeature` from the existing feature-size endpoint. No new endpoint, no DTO change. If the DELIVER step discovers that `closedDate` is *not* populated for the rows reaching the frontend, that becomes a new bug ticket — do not silently extend the slice into the backend.

## DWD-12: Telemetry — DocumentationDensityEvent skipped

The `nw-distill` skill's telemetry hooks (`scripts/shared/telemetry.py:write_density_event`) target a Python-centric nWave runtime that is not installed in this repo. Per the precedent in this repo (no other feature folder writes density events), telemetry emission is skipped. Density resolution defaults to `lean` + `ask` (the global default per D12 cascade) and Tier-2 expansions are not auto-rendered.
