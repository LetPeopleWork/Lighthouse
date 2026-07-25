# Evolution: epic-5427-percentiles-over-time (Slices 01-02 of 4)

- **Date finalized (slice-01)**: 2026-07-24 · **(slice-02)**: 2026-07-25
- **ADO**: Epic #5427 ("Show Percentiles over Time Charts", Community / Productboard). Non-premium, free-tier, brownfield. Slice-02 story **#5547 — Closed**.
- **Status**: **Slices 01-02 delivered on `main`**. Epic is **in progress** — slices 03-04 (the `ProcessBehaviorSnapshot` / "PBC Over Time" family) not started. DISCUSS → DESIGN → DEVOPS → DISTILL complete for the whole epic; DELIVER complete for slices 01 and 02. Both slices: backend suite green, mutation ≥80% on the new surface, adversarial review clean, integrity verify exit 0.
- **One archive per epic** — this file grows a section per slice. Slice-01 prose below is preserved as written; where slice-02 diverged from a slice-01 prediction it is marked **SUPERSEDED** in place and the correction lives in the Slice 02 section.
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
- **~~NULL-`Horizon` reserved for slice-02 WIA.~~ — SUPERSEDED by slice-02 (2026-07-25).** *Original prediction (kept for the record):* "The unique index includes `Horizon`; WIA rows (no horizon dimension, age is as-of-today) will write `Horizon = NULL`. Provider note for slice-02: NULLs are distinct under a plain unique index, so the WIA idempotency guard must key on `MetricType = WorkItemAge` with `Horizon IS NULL` explicitly — do not rely on the composite unique index alone to collapse same-day WIA re-writes on every provider."
  **What actually shipped**: slice-02 did **not** write `Horizon = NULL`. It introduced the sentinel `PercentilesOverTimeSnapshot.NoHorizon = 0` and persists WIA rows at horizon `0`. The slice-01 note diagnosed the NULL problem correctly but proposed working *around* it (an explicit `Horizon IS NULL` branch in the guard); the sentinel removes the problem instead — the unique index enforces one-row-per-day for WIA exactly as it does for CT, and the single `Horizon == horizon` upsert predicate serves both families with no branch. See the **Amendment (slice-02)** section of [ADR-106](../product/architecture/adr-106-percentiles-over-time-snapshot-table-shape.md) and "Slice 02 → Key decisions" below.

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

## Forward pointer — slices 02-04 (as scoped at slice-01 finalization; 02 has since shipped)

| Slice | Story | Ships | Deferred components |
|---|---|---|---|
| 02 | US-03 WIA percentiles over time | ~~planned~~ **DELIVERED 2026-07-25** — see "Slice 02" below. Shipped as predicted except `Horizon = NoHorizon (0)` rather than `NULL` | — |
| 03 | US-04 Throughput PBC NPLs over time | `ProcessBehaviorSnapshot` table + repo + `ProcessBehaviorRecordingHandler` + `IProcessBehaviorSeriesQuery` + `process-behavior-over-time?type=` endpoint + "PBC Over Time" widget (Throughput) | entire ProcessBehaviorSnapshot family |
| 04 | US-05 PBC remaining type toggles | WIA/WIP/CT/Arrivals/Feature-Size(portfolio-only) toggle options on the PBC widget | remaining `MetricType` values, Feature-Size portfolio-gating |

The **pipeline-reuse** KPI (one shared recording pipeline / snapshot-table family for CT+WIA, no per-metric bespoke recorder) is the load-bearing invariant across slices 01→02 — slice-02 WIA must join the existing handler/table, not fork a new one.

## Slice 02 — Work Item Age percentiles over time (US-03, ADO #5547) — SHIPPED 2026-07-25

Slice 02 adds the **Work Item Age** family to the pipeline slice-01 built. The load-bearing constraint
was the epic's `OUT-5427-pipeline-reuse` KPI: WIA had to **join** the existing handler, table, endpoint
and widget — not fork a parallel set. It did. No new table, no new repository, no new handler, no new
endpoint, **no EF migration**.

A flow coach opens **Team → Metrics → Predictability → "Percentiles Over Time"**, clicks the **"Age"**
chip (first in the toggle row; the default stays "30 days"), and reads four dated 50/70/85/95
age-percentile lines on the same red→green ramp — with no horizon choice, because age is measured
as-of-today.

### What shipped

1. **`MetricType.WorkItemAge`** — *appended* after `CycleTime` (`Models/MetricType.cs`). The enum
   persists as its integer ordinal, so appending is the only safe edit; the type now carries that as an
   XML-doc warning.
2. **`PercentilesOverTimeSnapshot.NoHorizon = 0`** — the horizon sentinel WIA rows persist at. The
   `Horizon` column stays `int?`; only the written value changed ⇒ **no schema change, no migration**,
   expand-only trivially satisfied.
3. **`PercentilesOverTimeRecordingHandler.RecordFamily(...)`** — one handler, one pass, both families:
   CT over `[30, 60, 90]`, WIA once under `[NoHorizon]`, on the same `TeamDataRefreshed` /
   `PortfolioFeaturesRefreshed` events. Each family runs in its **own inner `try/catch`** so a failing
   family never unwinds the rows the other one already staged for the shared `Save()`. The slice-01
   `finally { invalidateReadCache(); }` regression guard is preserved and now **test-pinned on both the
   success and the exception paths**. Failure logging is factored into `LogRecordingFailure(...)` with
   `MetricFamily` as a `private const string "Percentiles"` — a family, not a metric type, so operator
   alerting stays one alert rather than several.
4. **`DemoPercentilesBackfillHandler`** — backdates WIA across the same 14-day demo window; the
   idempotency guard is now evaluated **per metric family** (see Durable lessons).
5. **Read contract** — both `TeamMetricsController` and `PortfolioMetricsController` gain an additive
   `[FromQuery] MetricType metricType = MetricType.CycleTime`; `PercentilesOverTimeSeriesQuery.ResolveHorizon`
   maps `WorkItemAge` → `NoHorizon` so the sentinel never leaks past the query port. Existing CT calls
   are byte-identical on the wire.
6. **Frontend** — `PercentilesSelection = "age" | 30 | 60 | 90` replaces the horizon-only type;
   `usePercentilesOverTime` caches **per selection** (so Age↔30↔60↔90 re-plots without a refetch);
   `describeSelection` (module-level helper) supplies each chip's label/tooltip/test-id; the
   Tooltip-wrapped explicit-`selected` `ToggleButton` pattern from slice-01 is preserved. `MetricsService`
   builds `metricType=WorkItemAge` for the age tab and `horizon={n}` otherwise.
7. **E2E** — POM gains `ageToggle` / `isAgeSelected` / `selectAge` plus an exported
   `PERCENTILES_OVER_TIME_EMPTY_COPY` constant (verbatim, so a copy change fails loudly). Two new
   scenarios: a populated demo WIA tab, and a fresh **non-demo** team rendering the honest empty state.

Seven commits, all CI-green on `main`:
`51ec12870` recorder · `e3c583b98` series endpoint · `0dbe4d031` demo backfill · `93e8027f6` FE Age tab ·
`57f043dc4` E2E · `724099f32` review fix (metric-family log contract) · `0c074c456` Sonar fix (NUnit2045).

### Key decisions (slice-02)

- **`Horizon = 0` sentinel, not `NULL`** — reverses the slice-01 prediction. Two mechanical drivers:
  (a) SQL NULLs are distinct, so a NULL horizon defeats the unique index
  `(OwnerId, OwnerType, MetricType, Horizon, RecordedAt)` and WIA would accrue a duplicate row per
  refresh; (b) EF Core translates `s.Horizon == horizonParam` to `Horizon = @p`, which is never `TRUE`
  against NULL, so the upsert's find-existing predicate would miss and silently INSERT instead of
  UPDATE. The column stays nullable ⇒ expand-only, no migration.
  → **[ADR-106 Amendment (slice-02)]**.
- **Metric-family selection is explicit (`?metricType=`), not implicit ("no horizon ⇒ WIA")** — slice-01
  had already shipped `int? horizon`, so an *omitted* horizon is a legal cycle-time request. Re-reading
  it as "WIA" would have been a silent breaking change wearing a default's clothes. The new parameter
  defaults to `CycleTime`, so the contract is additive ⇒ **no CLI/MCP client version gate**.
  → **[ADR-108 Amendment (slice-02)]**.
- **One handler, per-family failure containment** — a second recorder would have doubled the refresh
  cost and drifted from the rows it is meant to sit beside; the inner `try/catch` is what makes sharing
  one `Save()` safe. → **[ADR-107 Amendment (slice-02)]**.
- **`MetricFamily` log property is a family, not a metric type** — reverted a first-pass implementation
  that logged `metricType.ToString()`. Caught by adversarial review as the slice's single **BLOCKER**
  (`724099f32`): per-type values would fragment the `OUT-5427-recording-failure-isolation` alert.
- **ADR-107's recording-failed template reconciled to the shipped code.** The DESIGN-wave observability
  note specified `"Percentile/PBC snapshot recording failed for …"`; slice-01 shipped
  `"Percentile snapshot recording failed for …"` and slice-02 keeps it. The shipped string is canonical —
  the two-handler decision means the PBC recorder (slices 03/04) emits its own message and never needed
  to share this one. Recorded in the ADR-107 amendment rather than silently edited into the DESIGN prose.

### Quality outcomes

| Gate | Result |
|---|---|
| `dotnet build` | 0 warnings (`TreatWarningsAsErrors`) |
| Backend suite | **3601 pass / 0 fail / 3 skipped** (the 3 skips pre-date this slice) |
| `pnpm test` | **3644 pass** |
| `pnpm build` | clean (implies a clean Biome `prebuild`) |
| Playwright `PercentilesOverTime.spec.ts` | **3/3** live against a locally-built instance |
| Mutation — Stryker.NET (BE) | **87.13%** (≥80% per-feature mandate) |
| Mutation — Stryker (FE) | **93.42%** (≥80%) |
| Adversarial review | **REJECTED → 1 BLOCKER fixed → clean** |
| `des-verify-integrity` | exit 0 — 5/5 steps, complete traces |
| CI on `main` | all jobs green: backend, frontend, E2E, SQLite + Postgres, auth, sonar-gates |

Two CI cycles were spent and both are now ledgered in `docs/ci-learnings.md`: a SQLite
`disk I/O error` runner flake in `IntegrationTestBase.Init`, and a `sonar-gates` failure on a single
INFO-severity `NUnit2045` that is invisible to a warning-clean local build.

### Slice-boundary decisions worth remembering

- **The E2E team-creation helper was already broken before this slice consumed it.**
  `helpers/api/teams.ts` omitted `blockedStalenessThresholdDays`, so `POST /api/latest/Teams` 400s. Every
  prior E2E drove demo data, so nothing had ever called it — the slice-02 "fresh non-demo team" empty-state
  scenario is its **first consumer**, and inherited the bug. A helper with no callers is not tested code;
  budget for fixing it when a scenario finally needs it.
- **The empty-state scenario needs a NON-demo team.** The demo backfill (item 4) deliberately populates
  every demo owner, so an honest-empty-state assertion on demo data would be structurally impossible.
  Empty-state scenarios and demo-data scenarios need opposite fixtures.
- **Screenshots deliberately deferred to epic completion.** Slices 03/04 add further tabs/toggles to this
  same widget, so per-slice PNGs would be regenerated twice and thrown away. Recorded as an explicit
  N/A-with-reason against DoD item 8, **not** a silent skip. Deferral note for whoever closes the epic:
  `rm` the old PNG before regenerating — the `@screenshot` comparator keeps the old file when the diff
  is under 0.5%.

### Durable lessons

- **The demo-backfill idempotency trap — a guard scoped to the owner makes every *new* family a
  permanent no-op.** Slice-01's guard read "any CT snapshot for this owner with `RecordedAt < today` ⇒
  skip the whole backfill". Left alone, every environment that slice-01 had already CT-backfilled — i.e.
  every demo instance and every screenshot environment — would have hit the skip and **never gained a
  single WIA row**. The failure mode is worse than a crash: unit tests seed *fresh* owners, so they take
  the first-run path and stay green, while the demo chart quietly renders the empty state and reads as a
  UI bug. The guard is now keyed per metric family. **Rule**: an "already ran?" guard on a backfill that
  grows new families over time must be keyed on the unit that can independently be missing (the family),
  never on the owner — and the regression test must seed an owner already backfilled with the *older*
  family and assert the new one still lands.
- **A persisted enum is an ordinal, so its member order is production data.** `MetricType` is stored as
  its integer value. `WorkItemAge` was appended after `CycleTime`; inserting or reordering a member
  silently re-maps every already-shipped snapshot row to a different metric family — corruption with no
  compiler error, no test failure and no migration to review. **Rule**: append only, and say so at the
  declaration site (the enum now carries the warning in its XML doc).
- **A nullable column reserved for "later" should be re-derived when later arrives, not honoured.**
  Slice-01 reserved `Horizon = NULL` and even predicted the NULL-distinctness problem — then proposed a
  workaround (an explicit `IS NULL` branch). Re-deriving the choice at implementation time produced a
  strictly better answer (a sentinel) that needed no branch and no migration. A DESIGN-time placeholder
  is a hypothesis, not a commitment.
- **`INFO`-severity Sonar rules fail a `new_violations = 0` gate while being invisible locally.**
  `NUnit2045` never reaches `dotnet build`, warning-clean or not — it only exists in SonarCloud's Roslyn
  pass. Wrap adjacent independent `Assert.That` calls in `using (Assert.EnterMultipleScope())` *before*
  pushing. Nesting caveat found here: if one assert's value comes from a helper that itself asserts and
  then parses, evaluate the helper **before** entering the scope, or a raw exception escapes mid-scope.

### Cross-cutting (slice-02)

- **RBAC** — **N/A**: free-tier (D3), reads inherit the existing class-level `MetricsController` read
  guard. No `useRbac()` change, no license path touched.
- **Lighthouse-Clients (CLI + MCP)** — **N/A**: the contract change is one optional query parameter with
  a default equal to the previous behaviour. No existing request or DTO changes shape ⇒ no client version
  gate.
- **Website / marketing surface** — **N/A**: free metric surface, no pricing or positioning change.
- **EF migration** — **N/A**: no schema change (the sentinel is a value written into an existing nullable
  column). `CreateMigration` not run, deliberately.
- **Docs prose** — the ADR amendments (106/107/108/109), this archive section, the `brief.md` component
  inventory and the `kpi-contracts.yaml` baselines **are** the slice-02 docs pass. No user-facing docs
  page changed because the widget's user-visible behaviour is "one more chip on an existing toggle".
- **Per-feature screenshots** — **N/A for slice-02, deferred to epic completion**: slices 03/04 change
  the same widget's toggle row, so a slice-02 PNG would be regenerated and discarded twice.
- **Demo data** — **covered**: `DemoPercentilesBackfillHandler` backdates WIA over the 14-day window, so
  demo/`@screenshot` environments render a populated Age tab.
- **ADO** — story **#5547 Closed** before finalization (no transition performed by this pass).
- **Outcomes registry** — no `docs/product/outcomes/registry.yaml` in this repo; collision check **N/A**
  (recorded, not silently skipped). Testable outcomes live in `kpi-contracts.yaml`.

## Remaining — slices 03-04 (NOT started)

The whole `ProcessBehaviorSnapshot` family is still deferred: the table + repository + EF migration (this
one **is** a real schema change), `ProcessBehaviorRecordingHandler`, `IProcessBehaviorSeriesQuery`, the
`process-behavior-over-time?type=` endpoint, and the "PBC Over Time" widget (slice 03, Throughput only;
slice 04 adds WIA/WIP/CT/Arrivals/Feature-Size, the last portfolio-only).

Carry-forward for slice 03:

- Its demo backfill must extend `DemoPercentilesBackfillHandler`'s **per-family** guard idiom to the new
  table's per-`MetricType` rows — same trap, different table.
- `ProcessBehaviorMetricType` will also persist as an ordinal ⇒ append-only from day one.
- The PBC recorder emits its **own** recording-failed message (`MetricFamily = "ProcessBehavior"`), per
  the ADR-107 amendment — it does not share the percentiles template.
- The "PBC Over Time" widget's empty-state honesty is the last open leg of `OUT-5427-empty-state-honesty`.

## Links

- ADRs: `docs/product/architecture/adr-106-percentiles-over-time-snapshot-table-shape.md` · `adr-107-percentiles-recording-handler-on-refresh-events.md` · `adr-108-percentiles-over-time-series-http-contract.md` · `adr-109-demo-percentiles-backfill-handler.md`
- Architecture: `docs/product/architecture/brief.md` → "Application Architecture — epic-5427-percentiles-over-time (Epic 5427)"
- KPI contracts: `docs/product/kpi-contracts.yaml` → 5 `OUT-5427-*` rows
- ADR amendments (slice-02): the **Amendment (slice-02, 2026-07-25)** section of each of ADR-106 / ADR-107 / ADR-108 / ADR-109
- CI learnings from slice-02: `docs/ci-learnings.md` → the two 2026-07-25 entries (SQLite `disk I/O error` runner flake; NUnit2045 INFO-severity gate failure)
- Feature workspace (full wave history): `docs/feature/epic-5427-percentiles-over-time/feature-delta.md` + `deliver/roadmap.json` (slice-02) + `deliver/roadmap-slice-01.json` / `deliver/execution-log-slice-01.json` (slice-01, archived)
