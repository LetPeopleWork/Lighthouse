# ADR-065: Work Item Age Percentiles — Compute Server-Side on a New Read Endpoint (Team + Portfolio), Mirroring `cycleTimePercentiles`

## Status
Accepted (user-confirmed, 2026-06-09) — supersedes the prior provisional client-side recommendation.

## Context

Story #5257 (`work-item-age-percentiles`, non-premium, brownfield) adds a "Work Item Age Percentiles" overview card and a Cycle-Time↔Work-Item-Age toggle on the Work Item Aging chart. Both surfaces need the 50/70/85/95 percentiles of the **current in-progress population's `WorkItemAge`** — a snapshot of live WIP, explicitly **not** windowed by the selected date range (DISCUSS D4).

DISCUSS D8 deferred the compute location to DESIGN. A prior DESIGN pass recommended *client-side* derivation. **The user overrode that decision (2026-06-09):** *"WIA percentiles should be calculated in the BACKEND, with an extension to the API (and thus also the client packages). We want to do as little production work in the frontend."* This ADR records the corrected decision and demotes client-side to a rejected alternative.

Code reality verified before deciding:

- `GET …/metrics/cycleTimePercentiles?startDate&endDate[&definitionId]` returns `IEnumerable<PercentileValue>` (50/70/85/95) — class-level `[RbacGuard(TeamRead)]` (PortfolioRead twin), `startDate.Date > endDate.Date ⇒ 400`, cached per entity by a date-keyed string (`TeamMetricsController.cs:123`, `TeamMetricsService.GetCycleTimePercentilesForTeam` `:307`). This is the contract shape the new endpoint mirrors exactly.
- The current in-progress population already has a single canonical backend selection:
  - **Team**: `TeamMetricsService.GetWipSnapshotForTeam(team, asOfDate)` → `IEnumerable<WorkItem>` (`:563`; predicate `TeamId == team.Id && (StateCategory == Doing || Done)`, then `GenerateWorkInProgressByDay(asOf, asOf, …)[0]`). This is the same set surfaced by `GET …/metrics/wip` (`TeamMetricsController.cs:106`) that feeds the aging-chart dots.
  - **Portfolio**: `PortfolioMetricsService.GetInProgressFeaturesForPortfolio(portfolio, asOfDate)` → `IEnumerable<Feature>` (`:224`; analogous predicate over `Portfolios`).
- Every `WorkItem`/`Feature` extends `WorkItemBase`, which already exposes `int WorkItemAge` (`WorkItemBase.cs:73`) — the very value the aging chart plots per dot. No new age computation is needed.
- The 50/70/85/95 percentile set is already produced by the shared `BaseMetricsService.BuildPercentiles(List<int> values)` (`:304`), which calls `PercentileCalculator.CalculatePercentile` four times and emits `IEnumerable<PercentileValue>`. This is reused verbatim — no re-implementation.

## Decision

**Compute the Work Item Age percentiles server-side on a new read endpoint per scope, mirroring `cycleTimePercentiles` exactly.** Keep production work out of the frontend; achieve percentile-computation uniformity with `cycleTimePercentiles`/`ageInStatePercentiles` by construction (a single server-side algorithm — `BuildPercentiles` → `PercentileCalculator`).

### 1. New endpoints (additive, one per scope)

```
GET /api/teams/{teamId:int}/metrics/workItemAgePercentiles?startDate&endDate
    [RbacGuard(TeamRead)]      (existing class-level guard)   ⇒ IEnumerable<PercentileValue>

GET /api/portfolios/{portfolioId:int}/metrics/workItemAgePercentiles?startDate&endDate
    [RbacGuard(PortfolioRead)] (existing class-level guard)   ⇒ IEnumerable<PercentileValue>
```

- **REUSE `PercentileValue`** — the response is a flat 50/70/85/95 percentile list, identical in shape to `cycleTimePercentiles`. It is **NOT** per-state like `ageInStatePercentiles`, so it needs **no new DTO**.
- **`startDate`/`endDate` are kept on the signature for parity with the sibling endpoints, the shared `startDate.Date > endDate.Date ⇒ 400` guard, and a date-keyed cache key — but they MUST NOT filter the in-progress population.** WIA is "now": the population is the current WIP snapshot. The endpoint passes only `endDate` to the snapshot selection as `asOfDate` (the same convention `/wip` uses as its "as-of" date); `startDate` exists solely for signature/contract symmetry with `cycleTimePercentiles`. (Keeping the range params is the simplest *correct* signature — it avoids a bespoke single-param shape on an otherwise-uniform metrics-read surface, and the 400-guard + cache-key machinery come for free. The non-filtering invariant is enforced by an integration test asserting the percentiles are identical regardless of `startDate`.)

### 2. New service methods (compute = reuse two existing primitives)

- `TeamMetricsService.GetWorkItemAgePercentilesForTeam(Team team, DateTime startDate, DateTime endDate)`
- `PortfolioMetricsService.GetWorkItemAgePercentilesForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)`

Each is a thin composition over existing, tested primitives — **no new algorithm**:

```
ages   = <existing in-progress selection>(entity, endDate)   // GetWipSnapshotForTeam / GetInProgressFeaturesForPortfolio
           .Select(i => i.WorkItemAge).Where(a => a > 0).ToList()
result = BuildPercentiles(ages)                              // BaseMetricsService — PercentileCalculator ×4
```

- **In-progress selection is REUSED, not duplicated**: Team uses `GetWipSnapshotForTeam(team, endDate)`; Portfolio uses `GetInProgressFeaturesForPortfolio(portfolio, endDate)`. (The `> 0` age filter mirrors how `GetCycleTimePercentilesForTeam` drops non-positive cycle times before `BuildPercentiles`.)
- **Cache**: reuse `BaseMetricsService.GetFromCacheIfExists` with a new key namespace `WorkItemAgePercentiles_{endDate:yyyy-MM-dd}` (only `endDate` participates, because WIA is a single-moment snapshot — `startDate` does not affect the result, so it must not be in the key).

### 3. Empty / single-item WIP (D6) — stated invariant

- **Zero in-progress items** ⇒ `BuildPercentiles([])` returns the 50/70/85/95 entries with `0` values (the existing empty-fallback of `PercentileCalculator`). The frontend card renders its graceful empty state and the chart shows no WIA lines. Never a 500, never a crash.
- **One in-progress item** ⇒ percentiles over the single value (all four percentiles resolve to that value). **No special low-sample gate** — the endpoint behaves like the data it has, matching `cycleTimePercentiles`.

### 4. Lighthouse-Clients version-gate (this is the consequence of going server-side)

A **NEW endpoint** 404s opaquely on an old server, so the wrapping CLI + MCP client methods MUST be **version-gated** (per CLAUDE.md cross-cutting rule — same pattern as ADR-055 flow-efficiency and the ADR-062 family):

- The clients add a `getWorkItemAgePercentiles(id, scope, startDate, endDate)` wrapper that **pre-checks the server version** and fails with a clear "upgrade Lighthouse" error instead of surfacing the opaque 404.
- Pin the gate to **strictly newer than the *last released* Lighthouse version**; record that baseline in the clients' `FEATURE_REQUIRES_SERVER_NEWER_THAN` registry, bumping it to the current latest release at the moment the wrapper is added. **Dev / unparseable versions must never be blocked.**
- The clients live in a **separate repo** — this work is tracked there, not in this repo's slices, but is called out as a cross-cutting deliverable so DELIVER/finalize does not forget it.

### 5. No premium gate, no RBAC change (D3)

Reads ride the existing class-level `[RbacGuard(TeamRead)]` / `[RbacGuard(PortfolioRead)]`. No `ILicenseService` on the read path, no `useRbac()` UI gating, no new authorization surface.

## Alternatives Considered

### Option A — Compute client-side from already-loaded `inProgressItems` (the prior provisional recommendation — REJECTED by user)
Derive the WIA percentiles in a new canonical TS helper from `ctx.inProgressItems[].workItemAge`, parity-tested against `PercentileCalculator`; no new endpoint, no Clients change.
- **Upside (as previously argued)**: the FE already holds `inProgressItems` and `percentileValues` at the same `BaseMetricsView` call site; the toggle becomes a pure client-side re-render with no network round-trip; zero new contract.
- **Rejected because (user directive, 2026-06-09)**: it puts production percentile-computation *logic* in the frontend, which the user explicitly does not want ("as little production work in the frontend"). It also forks the percentile algorithm into a second language — the parity test mitigates but does not eliminate the divergence risk, and it makes the FE the only percentile surface NOT computed by the shared server-side `PercentileCalculator`. Server-side compute restores uniformity with `cycleTimePercentiles` and `ageInStatePercentiles` by construction: one algorithm, one place, no parity test owed. The cost the prior pass cited against this option (a new endpoint + the Clients version-gate) is small and well-trodden — the in-progress selection, `BuildPercentiles`, the controller pattern, and the version-gate machinery all already exist; the new code is two thin service methods, two controller actions, and a registered client wrapper.

### Option B-prime — New endpoint but with a bespoke single-`asOfDate` signature (no `startDate`/`endDate`)
Drop the range params since WIA is a snapshot.
- **Rejected because**: it makes `workItemAgePercentiles` the lone metrics-read with a different signature shape from its siblings, losing the shared `startDate>endDate ⇒ 400` guard and date-keyed cache idiom, and complicating the client wrapper (which mirrors `getCycleTimePercentiles`). Keeping the parity signature and simply not letting `startDate` filter the population is simpler and uniform. (Documented as the non-filtering invariant in §1.)

### Option C — New per-state DTO like `ageInStatePercentiles`
- **Rejected because**: WIA percentiles are a single flat distribution over the whole in-progress population's age, not a per-state breakdown (per-state is already served by `ageInStatePercentiles` / the pace bands). A flat `PercentileValue[]` is the correct, already-existing shape; inventing a per-state DTO would over-model and mislead consumers.

## Consequences

- **Positive (uniformity)**: WIA percentiles are computed by the same server-side `PercentileCalculator` / `BuildPercentiles` as every other percentile surface — one algorithm, no second-language fork, no parity test. The frontend consumes server-computed `PercentileValue[]` exactly as it already does for `cycleTimePercentiles`. Honours the user's "minimal production work in the frontend" directive.
- **Positive (reuse-maximal)**: no new DTO (`PercentileValue` reused), no new in-progress selection (existing `GetWipSnapshotForTeam` / `GetInProgressFeaturesForPortfolio` reused), no new percentile algorithm (`BuildPercentiles` reused), no new cache mechanism (`GetFromCacheIfExists` reused), no EF/migration (no persistence). The genuinely-new backend artifacts are two thin service methods + two controller actions.
- **Negative (new contract + Clients work)**: a NEW endpoint × 2 scopes means the CLI + MCP clients gain a **version-gated** wrapper (`FEATURE_REQUIRES_SERVER_NEWER_THAN` entry), tracked in the separate clients repo. This is the cost the prior pass weighed against; the user has accepted it in exchange for keeping computation server-side.
- **Negative (toggle cost)**: the chart's CT↔WIA toggle now swaps between two server-fetched arrays. The WIA array is fetched in parallel with the existing metrics reads (one extra request per scope load, cached), so flipping the toggle is still a pure client-side source swap with **no per-flip network round-trip** (KPI-2 <200 ms preserved) once both arrays are loaded.
- **Snapshot invariant**: the endpoint returns identical percentiles regardless of `startDate` (and regardless of any future date-range UI change) because the population is the current WIP snapshot keyed only on `endDate`. Enforced by an integration test.

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| `workItemAgePercentiles` returns a flat `IEnumerable<PercentileValue>` (50/70/85/95), reusing `PercentileValue` — no new DTO | `TeamMetricsControllerTest` / `PortfolioMetricsControllerTests` + integration: response shape equals `cycleTimePercentiles` shape |
| Percentiles computed via `BuildPercentiles` → `PercentileCalculator` over the in-progress selection's `WorkItemAge` | `TeamMetricsServiceTests` / `PortfolioMetricsServiceTests`: golden percentiles over a known in-progress fixture |
| In-progress selection is REUSED (`GetWipSnapshotForTeam` / `GetInProgressFeaturesForPortfolio`), not duplicated | Service test asserts the population equals the `/wip` (Team) / in-progress-features (Portfolio) set |
| `startDate` does NOT filter the population; percentiles identical across ranges (D4) | Integration test: two calls with different `startDate`, same `endDate` ⇒ identical percentiles |
| Zero in-progress ⇒ 50/70/85/95 with `0` values, never 500 (D6); one item ⇒ percentiles over the single value, no low-sample gate | Integration: empty WIP and single-item WIP fixtures |
| Read controllers do NOT reference `ILicenseService` (non-premium, D3) | Grep/ArchUnit: no `ILicenseService` on the `workItemAgePercentiles` path |
| NEW endpoint ⇒ version-gated client wrapper | Clients-repo handoff note: `getWorkItemAgePercentiles` pre-checks server version, `FEATURE_REQUIRES_SERVER_NEWER_THAN` entry pinned strictly-newer-than the last release; dev/unparseable versions never blocked |
