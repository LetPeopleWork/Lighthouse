# Evolution: epic-5427-percentiles-over-time (Slice 01 of 4)

- **Date finalized (slice-01)**: 2026-07-24
- **ADO**: Epic #5427 ("Show Percentiles over Time Charts", Community / Productboard). Non-premium, free-tier, brownfield.
- **Status**: **Slice 01 delivered on `main`** (walking skeleton). Epic is **in progress** — slices 02-04 not started. DISCUSS → DESIGN → DEVOPS → DISTILL complete for the whole epic; DELIVER complete for slice-01 only. Backend suite green; mutation ≥80% on the new surface (**BE 85.71% / FE 90.91%**); adversarial review APPROVED (0 blockers); integrity verify exit 0.
- **Workspace (history)**: `docs/feature/epic-5427-percentiles-over-time/`
- **Builds on**: the forward-only one-row-per-day snapshot precedent (`DeliveryMetricSnapshot` Epic 3993, `BlockedCountSnapshot` Epic 5074), the Epic 5121 domain-event bus (`TeamDataRefreshed` / `PortfolioFeaturesRefreshed`), and the existing point-in-time CT percentile widget + D7 red→green ramp it wraps.

## What shipped (slice-01)

Slice 01 walks the **full backbone** end-to-end for **Cycle Time percentiles over time** and lands the **shared forward-only recording pipeline** that all later slices reuse. A flow coach opens **Team → Metrics → Predictability → "Percentiles Over Time"**, keeps the default CT-30 toggle, and reads four dated 50/70/85/95 lines (red→green) trending across the horizon; switching 30↔60↔90 re-plots from already-persisted history without a backend recompute.

Components (all new unless noted), mirroring the sibling epic-5074 Slice-03 blocked-count-over-time idiom:

1. **`PercentilesOverTimeSnapshot`** entity + **`MetricType`** enum (`CycleTime` only this slice; nullable `Horizon` carries 30/60/90) — `Models/`.
2. **`IPercentilesOverTimeSnapshotRepository`** + thin `RepositoryBase<T>` impl; DbSet + unique natural-key index `(OwnerId, OwnerType, MetricType, Horizon, RecordedAt)` on `LighthouseAppContext`.
3. **EF migration** `AddPercentilesOverTimeSnapshot` on **both** providers (Sqlite `20260724065010`, Postgres `20260724065020`), additive/expand-only, generated via the `CreateMigration` script; Postgres enforces the unique constraint. Real Postgres-container migration test.
4. **`PercentilesOverTimeRecordingHandler`** — `IDomainEventHandler<TeamDataRefreshed>, <PortfolioFeaturesRefreshed>`. Records today's CT percentiles for horizons 30/60/90; idempotent-per-day upsert (latest-write-wins, one row per natural key); forward-only; self try/catch emitting a structured recording-failed Error event (dispatcher swallows handler errors, so the handler owns its own observability).
5. **`IPercentilesOverTimeSeriesQuery`** + impl (read-only port) and **`PercentilesOverTimeSnapshotDto`**; **`percentiles-over-time?horizon=`** GET added to **both** Team and Portfolio `MetricsController`s (side-effect-free, reads only persisted rows; empty owner → empty array → honest empty-state).
6. **`DemoPercentilesBackfillHandler`** — demo-gated CT backdater (14-day window × 3 horizons), mirroring `DemoBlockedHistoryBackfillHandler`; real tenants stay forward-only. Populates the walking-skeleton chart.
7. **Frontend "Percentiles Over Time" widget** (Predictability category, team + portfolio) + **`usePercentilesOverTime`** hook + `getPercentilesOverTime` service method + `categoryMetadata.ts` registration. CT-30/60/90 toggle, red→green lines, honest empty-state copy ("builds forward from today — no snapshots recorded yet").
8. **E2E walking-skeleton spec** `PercentilesOverTime.spec.ts` (POM, Team Zenith demo data) — Scenario 1 incl. horizon re-plot.

Eight steps, every commit CI-green: `2e4b4576b` recorder · `021c19317` series endpoint · `962bc4dc2` demo backfill · `25cb8799e` FE widget · `cd8fb07f6` E2E · `321ad3b54` UX polish (single legend, day-labelled chips, uniform markers) · `e7e6d861f` mutation ≥80% · `6f4861c28` SonarCloud new-code clean. (Substrate/migration steps 01-01/01-02 folded into the recorder commit.)

## Key decisions (ADR-106..109, realised in slice-01)

- **ADR-106** — **two purpose-shaped snapshot tables, not one god-table**. Slice-01 ships only the `PercentilesOverTimeSnapshot` table (CT+WIA shape, `MetricType` discriminator + nullable `Horizon`); the `ProcessBehaviorSnapshot` NPL table is **deferred to slices 03/04**. Wide-discriminator and per-family-three both rejected.
- **ADR-107** — **recording on the existing refresh events**, idempotent-per-day upsert, self-isolated failure log. Realised by `PercentilesOverTimeRecordingHandler` (CT only this slice); the second handler `ProcessBehaviorRecordingHandler` is **deferred to slices 03/04**. Inline-in-refresh and a separate scheduler rejected.
- **ADR-108** — **two typed read endpoints on `MetricsController`**. Slice-01 ships `percentiles-over-time?horizon=`; the `process-behavior-over-time?type=` endpoint is **deferred**. Polymorphic envelope + single all-series call rejected.
- **ADR-109** — **demo-gated backfill**, real tenants forward-only. Realised CT-only.

### Slice-boundary decisions worth remembering

- **Demo backfill was pulled INTO slice-01.** The slice-01 brief originally listed "demo backfill" as OUT-of-scope, but walking-skeleton Scenario 1 requires a **populated** chart and the DISTILL adapter-coverage matrix lists `DemoPercentilesBackfillHandler` as covered by Scenario 1. A **minimal CT-only** demo backfill is therefore an in-slice-01 dependency of the WS litmus; broader multi-metric demo polish stays deferred.
- **CT-per-horizon read source — open question resolved without a new port.** DESIGN flagged whether `ITeamMetricsService` could yield CT percentiles per 30/60/90 window as-of-today. It can: the recorder calls the existing `GetCycleTimePercentilesFor{Team,Portfolio}(owner, today.AddDays(-H), today)` once per horizon. No new service method / port required (EXTEND held).
- **NULL-`Horizon` reserved for slice-02 WIA.** The unique index includes `Horizon`; WIA rows (no horizon dimension, age is as-of-today) will write `Horizon = NULL`. Provider note for slice-02: NULLs are distinct under a plain unique index, so the WIA idempotency guard must key on `MetricType = WorkItemAge` with `Horizon IS NULL` explicitly — do not rely on the composite unique index alone to collapse same-day WIA re-writes on every provider. Verify this on both Sqlite and Postgres when WIA lands.

## Quality outcomes

- **Mutation**: BE **85.71%** (Stryker.NET) / FE **90.91%** (Stryker), both ≥80% per-feature gate, scoped to the new snapshot/recording/query/widget code.
- **Adversarial review**: APPROVED, 0 blockers. Integrity verify exit 0.
- **CI parity**: `dotnet build` zero warnings, `dotnet test` green, `pnpm test`/`pnpm build`/Biome clean, both Sonar gates new-violations = 0, migration clean on Sqlite + Postgres.

## Cross-cutting

- **RBAC** — N/A (free-tier, D3). The new GETs inherit the existing class-level `MetricsController` read guard; no `useRbac()` / license-path change.
- **Lighthouse-Clients (CLI + MCP)** — **N/A** for slice-01. Contract change is **additive** (a new GET action + a new DTO), so no client version gate is required (contrast the WIA-percentiles feature, where compute-on-backend forced a version-gated wrapper). If a later slice changes an existing contract shape, re-evaluate.
- **Website** — marketing N/A (free metric surface). Docs + per-feature screenshots handled at finalization per project convention.
- **Outcomes registry** — no `docs/product/outcomes/registry.yaml` in this repo; collision check N/A (recorded, not silently skipped). Testable outcomes live in `kpi-contracts.yaml` (5 OUT-5427 rows).

## Durable lessons

- **Walking-skeleton "populated chart" litmus can pull a nominally-deferred enabler into the first slice.** The demo-backfill handler was marked OUT in the slice brief but was a hard dependency of the WS E2E; the DISTILL adapter-coverage matrix is the tell — if a driven adapter is "covered by Scenario 1", it is in slice-01 whatever the brief's scope list says. Reconcile the brief's scope list against the adapter-coverage matrix before starting.
- **A nullable discriminator column reserved for a later slice needs an explicit provider-behaviour note now.** `Horizon = NULL` for slice-02 WIA interacts with the unique index's NULL semantics (NULLs distinct on most providers) — capturing the idempotency-guard implication at slice-01 finalization saves a same-day-double-write bug when WIA lands.

## Forward pointer — slices 02-04 (NOT started)

| Slice | Story | Ships | Deferred components |
|---|---|---|---|
| 02 | US-03 WIA percentiles over time | WIA tab on the combined widget; WIA rows in the **same** `PercentilesOverTimeSnapshot` table (`MetricType=WorkItemAge`, `Horizon=NULL`) via the **same** recording pipeline (no second recorder) | WIA `MetricType` value, WIA tab, WIA demo backfill |
| 03 | US-04 Throughput PBC NPLs over time | `ProcessBehaviorSnapshot` table + repo + `ProcessBehaviorRecordingHandler` + `IProcessBehaviorSeriesQuery` + `process-behavior-over-time?type=` endpoint + "PBC Over Time" widget (Throughput) | entire ProcessBehaviorSnapshot family |
| 04 | US-05 PBC remaining type toggles | WIA/WIP/CT/Arrivals/Feature-Size(portfolio-only) toggle options on the PBC widget | remaining `MetricType` values, Feature-Size portfolio-gating |

The **pipeline-reuse** KPI (one shared recording pipeline / snapshot-table family for CT+WIA, no per-metric bespoke recorder) is the load-bearing invariant across slices 01→02 — slice-02 WIA must join the existing handler/table, not fork a new one.

## Links

- ADRs: `docs/product/architecture/adr-106-percentiles-over-time-snapshot-table-shape.md` · `adr-107-percentiles-recording-handler-on-refresh-events.md` · `adr-108-percentiles-over-time-series-http-contract.md` · `adr-109-demo-percentiles-backfill-handler.md`
- Architecture: `docs/product/architecture/brief.md` → "Application Architecture — epic-5427-percentiles-over-time (Epic 5427)"
- KPI contracts: `docs/product/kpi-contracts.yaml` → 5 `OUT-5427-*` rows
- Feature workspace (full wave history): `docs/feature/epic-5427-percentiles-over-time/feature-delta.md` + `deliver/roadmap.json`
