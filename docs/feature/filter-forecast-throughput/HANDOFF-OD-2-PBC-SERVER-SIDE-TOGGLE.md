# Session handoff — OD-2 PBC server-side toggle

**Created**: 2026-05-23 end of session
**Purpose**: Single-task handoff so the next Claude session can land OD-2 PBC server-side toggle without re-deriving the whole context.
**Out of scope for the next session**: ADO sync, docs screenshots, mutation kill-rate uplift, anything else. The user will manually verify after OD-2 lands and give further feedback.

---

## Current repo state

- **HEAD on origin/main**: `fd800d3d` — `docs(forecast): update evolution archive — OD-1 RESOLVED, OD-2 partial, OD-3 closed-by-deletion + post-archive timeline`.
- **Branch**: `main`. Trunk-based development; no feature branches, no PRs (per `feedback_trunk_based_development` memory).
- **CI baseline**: 11 of 12 jobs green on the last `Build And Deploy Lighthouse` run for HEAD. The only red job is `sonar-gates / sonar-gates` failing on `pnpm audit --audit-level=low` for GHSA-q8mj-m7cp-5q26 (qs DoS, moderate, transitive via `@stryker-mutator/core`). Override attempts break `Verify Frontend`'s `--frozen-lockfile`; root cause unresolved; reverted and accepted. Dependabot PR #80 tracks upstream. **Not your problem; do not touch.**
- **Working tree at handoff**: clean except for untracked nWave artifacts (`docs/feature/*/deliver/`) and `test-results/` (gitignored). Local stack not running.

## Big-picture context (skim if curious; not required to land OD-2)

`filter-forecast-throughput` is the Epic 4896 feature (premium-gated per-team forecast-throughput filter). DELIVER wave is complete with one open follow-up — **this task** — and a few non-blocking items the user is handling separately.

Read for orientation if you want a fuller picture (in this order):

- `docs/evolution/filter-forecast-throughput-evolution.md` — the long-form archive. The "Follow-ups" section names this task as #2 and ranks others.
- `docs/feature/filter-forecast-throughput/feature-delta.md` — original DISCUSS / DESIGN / DISTILL artifacts (lean v3.14 single file). Specifically the **ADR-014** section: chart-toggle split (Run Chart client-side, PBC server-side).
- `docs/ci-learnings.md` — durable rules. Multiple recurrence patterns to apply pre-emptively to your test files.
- `~/.claude/projects/-storage-repos-Lighthouse/memory/MEMORY.md` — auto-memory index; the entries that matter for this task are flagged below.

If you only have time for one: read the **ADR-014** block in `feature-delta.md` and the **OD-2 entry** in the evolution archive. Everything else here distils what you need.

---

## What OD-2 is

The throughput-filter feature has TWO chart surfaces under Team detail → Metrics tab:

1. **Run Chart (Total Throughput widget)** — daily throughput timeseries.
2. **Throughput PBC (Process Behaviour Chart)** — control-chart variant of the same data.

Per **ADR-014** (`feature-delta.md`):

- The Run Chart payload contains per-item granular data (`RunChartData.WorkItemsPerUnitOfTime: Dictionary<int, List<WorkItemBase>>`), so the toggle filters **client-side** — no backend round-trip.
- The PBC payload is aggregated and only carries `WorkItemIds`, so the toggle filters **server-side** via a `?view=raw|filtered` query parameter the backend has already implemented (commit `21960c6a`).

**Status of the two halves as of `fd800d3d`**:

- ✓ **Run Chart client-side**: `ThroughputChartFilterToggle` wired into `TotalThroughputWidget` via `BaseMetricsView.buildWidgetNodes`'s `filterToggle` slot. The toggle's `excludedSummary` prop receives a human-readable summary built from the team's rule conditions (helper `formatConditions` in `evaluateCondition.ts`). Chip tooltip works.
- ✗ **PBC server-side**: **NOT WIRED**. `BaseMetricsView.buildPbcWidget` does not pass a `filterToggle` slot to `ProcessBehaviourChart`. The `useMetricsData` hook does not accept a `view` parameter. The `MetricsService.getThroughputPbc(id, startDate, endDate)` does not accept a `view` parameter. Nothing on the FE invokes the backend `?view=filtered` query.

A code-comment marker is already in place at `BaseMetricsView.buildPbcWidget` pointing here.

## What "done" looks like

A Premium tenant viewing the Throughput PBC chart on Team detail → Metrics sees the `ThroughputChartFilterToggle` (Raw / Filtered buttons) AND a `FilteredThroughputChip` adjacent to the PBC widget when the toggle is in `Filtered` mode AND the PBC re-renders with filtered counts after a click (network round-trip with `?view=filtered`).

Concretely:
- Click Filtered → network shows a `GET .../metrics/throughput/pbc?...&view=filtered` request → response renders with the filtered point list.
- Click Raw → network shows the same URL with `view=raw` (or no view; default is raw) → response renders the original counts.
- The default state is Raw (D1 invariant — non-breaking).
- The toggle and chip are hidden when the tenant is non-premium OR the team has no filter configured.

## Files to touch (precise plan)

### 1. `Lighthouse.Frontend/src/services/Api/MetricsService.ts`

Extend `getThroughputPbc` to accept an optional `view` parameter and forward it as a query string.

Current signature (line 300):

```ts
async getThroughputPbc(id: number, startDate: Date, endDate: Date): Promise<ProcessBehaviourChartData>
```

Target:

```ts
async getThroughputPbc(id: number, startDate: Date, endDate: Date, view?: "raw" | "filtered"): Promise<ProcessBehaviourChartData>
```

Build the URL by appending `&view=filtered` when `view === "filtered"`. Omit the parameter otherwise (backend default is raw per DDD-5).

The interface declaration at line 56 must also be updated. If `IProjectMetricsService` and `ITeamMetricsService` share this method, both interfaces must accept the same optional param.

### 2. `Lighthouse.Frontend/src/hooks/useMetricsData.ts`

The hook fetches PBC data at line 359:

```ts
metricsService.getThroughputPbc(entity.id, startDate, endDate)
```

Two reasonable shapes:

- **(A) Refetch on demand**: expose a `refetchThroughputPbc(view)` function from the hook. The toggle invokes it on user action. Simpler; matches React-Query idiom; only one extra symbol surfaces from the hook.
- **(B) Hoist `view` into hook state**: hook takes `view` as a parameter, refetches when it changes. Requires the parent component to thread `view` state through the hook arguments.

**Pick (A)** — smaller blast radius and the toggle is the only consumer that needs the refetch lever. The hook's return type already exposes many discriminated data items; one more setter/refetch function fits the pattern.

Hook return type change (extend `MetricsData<T>`):

```ts
export interface MetricsData<T> {
  // ... existing fields ...
  refetchThroughputPbc: (view?: "raw" | "filtered") => Promise<void>;
}
```

Implementation: inside the hook, expose `refetchThroughputPbc` that calls `metricsService.getThroughputPbc(entity.id, startDate, endDate, view)` and updates the `throughputPbcData` state. Make sure it respects the same `useEffect` cancellation logic the initial fetch uses (the hook has an in-flight cancellation pattern around line 359 — preserve it).

### 3. `Lighthouse.Frontend/src/pages/Common/MetricsView/BaseMetricsView.tsx`

Two changes:

**3a. Destructure the new hook return value** (around line 906 where `useMetricsData(...)` is called):

```ts
const { ..., refetchThroughputPbc } = useMetricsData(entity, metricsService, startDate, endDate);
```

**3b. Wire the toggle into `buildPbcWidget`** (the function around line 558). It currently receives `data` + `titleSuffix` + `workItemLookup` + `type`. Extend it to also receive `isPremium`, `hasForecastFilter`, `forecastFilterConditions`, and `refetchThroughputPbc`. Render the `<ProcessBehaviourChart>` with a `filterToggle` slot — only for the THROUGHPUT type, not Cycle Time or other PBC types:

```tsx
<ProcessBehaviourChart
  data={data}
  title={titleSuffix}
  workItemLookup={workItemLookup}
  type={type}
  filterToggle={
    type === ProcessBehaviourChartType.Throughput ? (
      <ThroughputChartFilterToggle
        isPremium={isPremium}
        hasFilter={hasForecastFilter}
        chartKind="pbc"
        conditions={forecastFilterConditions}
        excludedSummary={formatConditions(forecastFilterConditions)}
        onServerViewChange={(view) => { void refetchThroughputPbc(view); }}
      />
    ) : undefined
  }
/>
```

The `void` on the promise is intentional — the toggle is sync; the refetch fires-and-forgets.

Remove the comment marker that `498a02d9` added at `buildPbcWidget`. Or replace it with a one-liner pointing at this resolution.

### 4. Vitest cases

Add tests in `Lighthouse.Frontend/src/pages/Common/MetricsView/` (a new `BaseMetricsView.pbc-toggle.test.tsx` if no existing file fits, or extend the closest existing test file — check first):

- Premium tenant + team has filter + PBC widget rendered → toggle visible adjacent to PBC.
- Click Filtered → `refetchThroughputPbc("filtered")` called once (mock the hook return).
- Click Raw → `refetchThroughputPbc("raw")` called once.
- Non-premium → toggle not rendered on PBC.
- No filter → toggle not rendered on PBC.

Reuse the patterns from `TotalThroughputWidget.test.tsx` and `ThroughputChartFilterToggle.test.tsx`. Don't reinvent the mock plumbing.

### 5. Service-layer tests (likely existing already)

`MetricsService.test.ts` (or equivalent) — assert `getThroughputPbc(id, start, end, "filtered")` produces a URL containing `view=filtered`. Add 2 cases: filtered + raw + omitted.

## Pre-flight rules (apply, do not re-derive)

Apply these from MEMORY before any code lands:

1. **`feedback_trunk_based_development`** — push directly to `origin main`. No feature branches. No PRs. After local green tests + a commit, push.
2. **`feedback_classifier_main_push_and_ado_reason`** — `git push HEAD:main` is auto-classifier-blocked even after user said yes. Ask once per session for explicit re-authorisation if you hit it. The user has been authorising pushes this whole session, so they'll likely OK it again.
3. **`feedback_systemtextjson_case_insensitive`** — N/A here (we're touching FE only) but flag it if you find any new `JsonSerializer.Deserialize<T>` on FE-originated JSON without `PropertyNameCaseInsensitive=true`.
4. **`feedback_run_playwright_before_commit`** — N/A here (the Playwright spec was deleted at `35017162`; do not write a new one for this task). The user will manually verify in a browser after this lands.
5. **`reference_premium_license_dev_seed`** — if you want to manually verify in a browser before pushing: `dotnet run` in `Lighthouse.Backend/Lighthouse.Backend`, then `curl -X POST -F "file=@Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Licensing/valid_not_expired_license.json" http://localhost:5169/api/v1/license/import`, then open the FE. Not required — Vitest unit tests are enough; the user has explicitly said they'll manually verify after the work lands.
6. **`feedback_ci_and_e2e_minimalism`** — do not write a Playwright spec for this. Vitest at the component layer + a service-layer URL assertion is sufficient.
7. **`feedback_finalize_workspace_commit`** — N/A; this is a follow-up commit on a shipped feature, not a finalize step.
8. **`feedback_slice_boundary_ritual`** — N/A; not a slice boundary.

CI-learnings (`docs/ci-learnings.md`) to apply pre-emptively when writing the tests:

- **CA1861** does not apply (FE TypeScript not C#). But the equivalent FE rule is **typescript:S7770** (don't wrap pure single-arg callbacks). Don't write `.map((x) => Number(x))`; write `.map(Number)`.
- **typescript:S7735** — don't write `cond !== X ? a : b` ternaries; flip to `cond === X ? b : a` (positive condition). Applies anywhere you'd guard `view !== "filtered"`.
- **typescript:S7764** — use `globalThis.location` not `window.location`. N/A here unless you grab the URL anywhere.
- **typescript:S107** — TypeScript class constructors above 3 params or with any optional → options-object pattern. N/A here (likely no new classes), but if you create a helper that takes ≥4 params, use an options object.

Build/test gates:

- `pnpm build` in `Lighthouse.Frontend` — must succeed with zero TS errors and zero Biome warnings. Biome runs as `prebuild` automatically.
- `pnpm test --run` in `Lighthouse.Frontend` — must pass all 2936+ tests (current baseline). Don't break the existing tests.
- `dotnet build` + `dotnet test` in `Lighthouse.Backend` — should remain green; no backend changes expected unless I missed something.

## Backend confirmation (no work needed)

The backend `?view=filtered` query param has been in place since commit `21960c6a`. The endpoint `GET /api/{teams|projects}/{id}/metrics/throughput/pbc?view=filtered` returns filtered counts; `view=raw` or omitted returns the unfiltered series. No backend changes required for OD-2.

Quick sanity-check command if you want to confirm (with the local stack running):

```
curl -s "http://localhost:5169/api/v1/teams/{TEAM_ID}/metrics/throughput/pbc?startDate=2026-05-01&endDate=2026-05-23&view=filtered" | head -c 200
```

## Anti-patterns to avoid

- **Don't add a Playwright spec.** User directive at the end of this session: rewrite when ready, not opportunistically.
- **Don't touch the `pnpm.overrides` block.** That fight has been had; the current state is the workaround.
- **Don't write more than ~6 Vitest tests for this.** Per CLAUDE.md test discipline + the L2 / mandate-5 parametrisation guidance — equivalence classes, not every input variant.
- **Don't refactor `useMetricsData` beyond the minimum.** Adding one new symbol to the return type is the bound. The hook is shared and any broad restructure breaks multiple consumers (TeamMetricsView, ProjectMetricsView, etc.).
- **Don't write a backend integration test for the round-trip.** That's a separate follow-up (item 8 in the evolution archive).
- **Don't expand `formatConditions` to take a schema parameter.** It's deliberately simple — uses hardcoded field-name humanisation. If you want richer display names, that's a separate refactor.

## How to start tomorrow (exact prompt to give the next session)

In a fresh Claude Code session at `/storage/repos/Lighthouse`:

```
Pls land OD-2 PBC server-side toggle for filter-forecast-throughput. The plan is
in docs/feature/filter-forecast-throughput/HANDOFF-OD-2-PBC-SERVER-SIDE-TOGGLE.md
— read that file first, then proceed. After the code + tests + commit, push to
origin main. Do not run Playwright; do not start the backend; I will manually
verify in a browser after you commit.
```

That's enough — the handoff file has everything else.

When that's done, the user will:
1. Pull the commit on their dev machine.
2. Run the stack (`dotnet run` + premium license import per the reference memory).
3. Manually verify the PBC toggle + chip behaviour in a real browser.
4. Open a new chat with feedback / follow-ups.

## Definition of done for OD-2

- ✓ `getThroughputPbc` accepts an optional `view` param and appends `view=filtered` to the URL only when explicitly set.
- ✓ `useMetricsData` returns a `refetchThroughputPbc(view)` callback.
- ✓ `BaseMetricsView.buildPbcWidget` renders the toggle in the PBC widget's `filterToggle` slot **only for `Throughput` PBC type, not Cycle Time / Feature Size / others**.
- ✓ Toggle wiring: `chartKind="pbc"`, `onServerViewChange={(view) => refetchThroughputPbc(view)}`, `excludedSummary` passed through (same `formatConditions` helper as the Run Chart toggle).
- ✓ Default state Raw (D1 invariant — non-breaking).
- ✓ Toggle hidden when non-premium OR no filter.
- ✓ Vitest suite green (no regressions; ≤6 new tests).
- ✓ `pnpm build` clean; `dotnet build` clean.
- ✓ Single conventional commit `feat(forecast): wire PBC server-side toggle — OD-2 close-out` with a body explaining the change. Trailer not required.
- ✓ Pushed to `origin main`.
- ✓ Evolution archive (`docs/evolution/filter-forecast-throughput-evolution.md`) updated: flip OD-2 from "PARTIALLY RESOLVED" to "RESOLVED". Move item 2 in the Follow-ups list to a strikethrough entry referencing the commit.

That's it. Single focused task. Don't scope-creep into the other follow-ups; the user is taking those.
