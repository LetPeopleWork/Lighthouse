# Evolution: epic-5427-percentiles-over-time (Slices 01, 02, 03, 03b, 04 — COMPLETE)

- **Date finalized (slice-01)**: 2026-07-24 · **(slice-02)**: 2026-07-25 · **(slices 03 / 03b / 04)**: 2026-07-26
- **ADO**: Epic #5427 ("Show Percentiles over Time Charts", Community / Productboard). Non-premium, free-tier, brownfield. Slice-02 story **#5547 — Closed**. Slice-04 story **#5549** — to be transitioned by the orchestrator.
- **Status**: **COMPLETE — all five slices delivered on `main`.** All six user stories (US-01..US-06) shipped; the epic's planned scope is closed. DISCUSS → DESIGN → DEVOPS → DISTILL complete for the whole epic; DELIVER complete for slices 01, 02, 03, 03b and 04. Every slice: backend suite green, mutation ≥80% on the new surface, integrity verify exit 0. Two evidence gaps are recorded rather than glossed — slice 03 published no mutation score of its own and its diff never received an adversarial review. Carried-forward defects and the one open DEVOPS action item are listed under "Epic status at slice-04 close".
- **One archive per epic** — this file grows a section per slice. Slice-01 prose below is preserved as written; where a later slice diverged from an earlier prediction it is marked **SUPERSEDED** in place and the correction lives in that slice's section.
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

## Forward pointer — slices 02-04 (as scoped at slice-01 finalization; all have since shipped)

| Slice | Story | Ships | Deferred components |
|---|---|---|---|
| 02 | US-03 WIA percentiles over time | ~~planned~~ **DELIVERED 2026-07-25** — see "Slice 02" below. Shipped as predicted except `Horizon = NoHorizon (0)` rather than `NULL` | — |
| 03 | US-04 Throughput PBC NPLs over time | ~~planned~~ **DELIVERED 2026-07-26** — see "Slice 03" below. `ProcessBehaviorSnapshot` table + repo + `ProcessBehaviorRecordingHandler` + `IProcessBehaviorSeriesQuery` + `process-behavior-over-time?type=` endpoint + "PBC Over Time" widget (Throughput). An unplanned **slice 03b** (US-06, date-range) was inserted after it | — |
| 04 | US-05 PBC remaining type toggles | ~~planned~~ **DELIVERED 2026-07-26** — see "Slice 04" below. Shipped exactly as scoped: WIA/WIP/CT/Arrivals/Feature-Size(portfolio-only) as appended enum values + a scope-aware toggle, no new component | — (the demo backfill was deliberately *not* extended — see slice-04 decision (b)) |

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

## Slice 03 — Throughput PBC limits over time (US-04, ADO #5548) — SHIPPED 2026-07-26

Recorded retroactively on 2026-07-26 during slice-03b's close-out: slice-03's own finalize never ran, so
this section is reconstructed from the shipped code, `roadmap-slice-03.json` and the commit history
(`2d6c73690..3377c038b`, 6 steps). The full reconstruction, including what could NOT be recovered, is in
`feature-delta.md` → "Wave: DELIVER / [REF] Implementation summary (slice-03…)".

The whole `ProcessBehaviorSnapshot` family landed: the table + repository + a **real** additive EF
migration on both providers (the only DDL in the epic since slice-01), `ProcessBehaviorRecordingHandler`
on the same two refresh events the percentiles recorder already subscribes,
`IProcessBehaviorSeriesQuery` + `ProcessBehaviorSnapshotDto`, the `process-behavior-over-time?type=` GET
on both controllers, the demo backfill extension, and the "PBC Over Time" widget (Throughput only).

Unlike slice-02 this was not a pure extension — second table, second handler, second read port, second
DTO, new widget. The pipeline *shape* was reused; the code is new because the `Unpl/Average/Lnpl` triple
is a different row shape from the four-percentile row (ADR-106). One deliberate deviation from D7: the
three limits **are** the series in an over-time chart, so they render solid in distinct colours instead
of the point-in-time chart's neutral dashes, which would collapse into three near-identical greys in
dark mode.

**Two gaps that cannot be closed after the fact and are recorded rather than glossed:** no mutation
score was ever published for slice-03 (step 03-06 ran a mutant-killing pass; the nearest hard evidence
is the slice-03b run, which mutates the whole epic surface and covers every slice-03 file above the
gate), and slice-03's diff never received an adversarial review.

## Slice 03b — Over-time widgets respect the dashboard date range (US-06, ADO #5564) — SHIPPED 2026-07-26

An unplanned slice, inserted between 03 and 04 after user review of the shipped widget found the
dashboard date pickers had no effect on either over-time chart — they took no date parameters anywhere
in the read path, so every recorded day was always plotted while every sibling widget on the same
dashboard respected the range.

Read-path only. Optional additive `startDate`/`endDate` on both series endpoints across both scopes,
filtered on `RecordedAt` inclusive at both ends and composed onto the existing `IQueryable` so the
database applies them; `IProcessBehaviorSnapshotRepository.GetSeries` created so both families place the
series query in the repository rather than one there and one in its query class (ahead of slice-04
adding five more metric types to that path); both hook caches re-keyed from selection-alone to
selection-plus-range; the empty state disambiguated **in the widget** by the range's end rather than via
a response envelope, which ADR-108 rejects. Mutation BE 89.86% / FE 92.76%.

Three durable lessons, all of them mistakes made and caught inside this slice:

1. **The brief's empty-state predicate was unimplementable.** It assumed a "default = unfiltered" state;
   the dashboard's default IS a bounded window (30 days team, 90 portfolio), so every request it makes is
   narrowed. The discriminator became the range's *end* instead. Worth checking that a "default" state
   actually exists before writing a decision that branches on it.
2. **The headline acceptance test could not fail.** A POM getter returning `0` for "chart not painted
   yet" satisfied `toBeLessThan`, so the assertion held with the date filter deleted from the backend.
   Found by adversarial review, not by the test suite, and fixed by polling for the render then bounding
   both sides. The general rule now in `ci-learnings.md`: after writing an assertion whose failure you
   have not observed, sabotage the production code and watch it go red.
3. **Two of our own artifacts asserted things that were false** — a coverage table claimed a scenario was
   tested when nothing tested it, and an ADR called a reachable defect an unreachable accepted edge. Both
   were written in good faith during DESIGN and only checked when someone went looking.

Carried forward, not fixed: the `startDate`/`endDate` URL params round-trip through UTC while requests
are built from local date parts, so a reloaded or shared link loses one day and can flip the empty-state
sentence outside UTC (pre-existing, affects every date-ranged widget, filed separately); a typed inverted
range now 400s and the hooks have no error state, so both cards render blank until corrected; and the
widget screenshots are stale for both slices — deliberately, by maintainer decision 2026-07-26: the whole
percentiles/PBC over-time set gets regenerated once after slice 04, which touches the same surface again.

## Slice 04 — PBC over time across every metric family (US-05, ADO #5549) — SHIPPED 2026-07-26

The last planned slice. The "PBC Over Time" widget stops being a Throughput chart with a one-button
toggle: a delivery lead now switches it across **Throughput, Work Item Age, WIP, Cycle Time and
Arrivals** on a team, plus **Feature Size on a portfolio**, each showing its own dated UNPL/Average/LNPL.

The slice brief's learning hypothesis was "adding PBC types is pure configuration over the slice-03
shell, *unless* any type needs a bespoke recorder or breaks the Throughput regression". It held. No new
table, no repository, no handler, no endpoint, no DTO, no EF migration, no new frontend component — five
appended enum members, two scope-specific reader arrays, a scope-aware toggle, and the read-port
coverage to prove each family carries its own series. The single-entry `readers` array slice 03 had
deliberately built "so that a later type failing cannot discard the rows an earlier type already
staged" was the seam this slice slotted into, exactly as intended: a design decision made one slice
early paid for itself.

Nine commits, `e61d0b47a..1cfe48ad0`, 15/15 CI checks green. Backend suite 3749 passing, frontend 3685,
Playwright `PbcOverTime.spec.ts` 6/6 against a live instance. Mutation **BE 90.14% / FE 94.08%** — the
epic's highest on both stacks. Adversarial review (`@nw-software-crafter-reviewer`): **APPROVED, zero
findings** across 10 checks — the only clean-first-pass review in the epic.

### Two maintainer decisions, both deliberate and both lossy in a stated way

- **The ready-but-zero honesty gate excludes `Lnpl == 0`.** `XmRCalculator` returns a fully collapsed
  band (`Average = UNPL = LNPL = 0`) for an empty or all-zero baseline, and every chart builder still
  stamps `Status = Ready` for it — so without a gate the recorder would persist a fake triple and the
  chart would plot three flat zero lines as if that were a process. The gate refuses
  `Average == 0 && Unpl == 0`. It deliberately does **not** include the lower limit, because the
  calculator clamps a negative LNPL to zero for zero-bounded data, so a real, busy process routinely
  reports `Lnpl == 0`. The tidier fix — stamping `NotReady` at the chart builders — was rejected as
  out-of-blast-radius: it would change the behaviour of six shipped point-in-time PBC widgets app-wide.
- **`DemoPercentilesBackfillHandler` stays Throughput-only.** The five new families therefore render the
  honest forward-only empty copy on demo data until a day of real recording accrues. The cost is named
  rather than hidden: milestone-4's outline scenario ("three dated lines are plotted for each type")
  cannot be asserted through the browser for any non-Throughput family, so its plotting assertion lives
  at the read port instead. The roadmap reviewer accepted this as intentional-but-lossy on condition the
  E2E carry an explicit comment **at the point the assertion is not made**, naming the read-port fixture
  and forbidding a future "fix" that weakens it or extends the backfill. That comment shipped. Users are
  told too — `docs/metrics/predictability.md` now says the demo backfill covers Throughput only.

### Durable lessons

- **Two guards that overlap on all *reachable* data are not one redundant guard — they encode different
  claims, and mutation testing will tell you they are the same.** Stryker survived a mutant removing the
  `return` from the `Status != Ready` check, because every not-ready path in production also zeroes the
  triple, so the new ready-but-zero gate catches it anyway. The tempting readings are "delete the
  redundant guard" or "mark it equivalent". Both are wrong: `Status` is authoritative *regardless of the
  numbers*, so a chart reporting not-ready while carrying a live band must still write nothing —
  otherwise a future builder that stamps a not-ready status beside computed values silently starts
  recording limits the owner was told were not ready. The survivor was converted into a real contract by
  constructing the input production cannot currently produce (`Ready`-shaped band + not-ready status) and
  asserting the guard on it. **Rule**: when a mutant survives on a guard that a *second* guard shadows,
  ask what each guard claims independently before calling it equivalent; if the claims differ, the
  surviving mutant is a missing test, not an equivalent mutant. Verify by reproducing the mutant and
  checking that *only* the new cases fail.
- **A pre-scan phrased over "test methods" misses assertion helpers, and that is how a 5×-recurring rule
  recurs again.** `NUnit2045` (adjacent `Assert.That` calls need `Assert.EnterMultipleScope()`) is an
  INFO-severity SonarCloud-only rule that a warning-clean `dotnet build` cannot see, and it has now cost
  five CI cycles across the codebase. This time the violation was not in a `[Test]` method at all — it
  was three asserts inside a `foreach` in a `private static void Assert…` helper. Every existing pre-scan
  habit ("check each new or edited test method") walks straight past that shape, and the fix wants the
  scope *inside* the loop (one grouped report per row, not one for the whole loop). **Rule extended in
  `ci-learnings.md`**: pre-scan every new or edited *assertion helper* too — anything whose body contains
  ≥2 adjacent standalone `Assert.That`, wherever it lives.
- **A "not a real value" sentinel in a test is a liability the moment the production enum can grow.**
  Slice 03's unknown-family rejection tests used the literal string `"CycleTime"` as the family that must
  400. Slice 04 promoted `CycleTime` into a real family — and those tests stayed **green**, now passing
  for entirely the wrong reason and no longer testing the 400 guard at all. Caught by inspection, not by
  the suite, because there is nothing for a suite to notice. Repaired with a name that can never become
  real (`"NotAProcessBehaviourFamily"`) **plus a test asserting it is genuinely not a declared member**,
  so a future repeat fails loudly. **Rule**: a negative-case sentinel drawn from the same namespace as
  the values under test must be pinned as non-member by its own assertion, or it silently decays.
- **`TerminologyContext` has no key for every noun a UI renders.** The toggle labels the six families
  through `useTerminology()` so a tenant that renamed "Work Item" or "Feature" sees the rename follow —
  but there is no `ARRIVALS` and no `FEATURE_SIZE` key. Arrivals is a hard-coded literal, and Feature
  Size is composed as `` `${getTerm(FEATURE)} Size` ``. Worth knowing before designing a label map:
  terminology coverage is per-noun and partial, so a "just use terminology for all of them" plan needs a
  key-existence check first, and composing a term with a fixed suffix is the established fallback.
- **Exposing a capability in the clients is a separate act from keeping the client contract compatible —
  and only the second one shows up in a per-slice checklist.** Every slice from 01 through 03b correctly
  answered "Lighthouse-Clients versioning: N/A" — each contract change genuinely was additive, so no
  version gate was required. All four were right, and the clients had nonetheless never exposed *either*
  over-time endpoint at all. The gap was only noticed at slice 04, and closing it took 4 client methods,
  2 CLI metrics and 4 MCP tools spanning the whole epic (`lighthouse-clients` `5bcb2a6`, gated on server
  `v26.7.11.4`). **Rule**: the clients checklist item needs two questions, not one — "does this break an
  existing client?" *and* "is this surface reachable from the CLI/MCP at all?".
- **A DTO that omits the dimension its request selected is a sharp edge for every non-browser consumer.**
  Found while wiring the clients: `PercentilesOverTimeSnapshotDto` carries `recordedAt`, `metricType` and
  the four percentiles — but **no `horizon`**. The widget never notices, because it always sends an
  explicit horizon and re-plots per selection. A client that omits `horizon` on a `CycleTime` request
  gets every recorded horizon interleaved in one array **with no field to tell them apart**. Documented
  as a SHARP EDGE in the client rather than changed, because adding the field is a response-shape change
  to a shipped contract. **Rule**: when a request parameter *filters* a series, the response row should
  carry that dimension — the UI's habit of always sending it hides the omission until a scripted consumer
  arrives.

### Cross-cutting (slice-04)

- **RBAC** — **N/A**: free-tier (D3), no new endpoint or gate; the six families ride an existing GET's
  `?type=` parameter under the class-level `MetricsController` read guard. Feature-Size-portfolio-only is
  a **scope** rule, not a permission rule: the toggle withholds the option and the wire stays permissive
  (a team asking for `FeatureSize` gets an empty 200, pinned at the read port).
- **Lighthouse-Clients (CLI + MCP)** — **DONE**, and it closed a gap spanning slices 01-04 (above).
- **Website / marketing surface** — **N/A**: free metric surface, no pricing or positioning change, no
  website asset references this widget.
- **EF migration** — **N/A**: the five new families are *values* in the existing ordinal column. No DDL.
  What had to be handled instead was the enum-ordinal hazard: members appended at the end, and all six
  ordinals now pinned member-by-member (the slice-03 one-member guard was retired *by design*, its
  invariant absorbed rather than dropped).
- **Demo data** — **PARTIAL by decision**: Throughput backfilled, five families forward-only. See above.
- **Docs prose** — three corrected claims in `docs/metrics/predictability.md` (the Flow-Metric row, the
  toggle sentence, and the empty-state note now disclosing the Throughput-only demo backfill), the
  `ci-learnings.md` recurrence-5 entry, the ADR-109 slice-04 amendment, and this section.
- **Per-feature screenshots** — **DONE**. The deferral opened at slice-02 and re-affirmed at slice-03b
  ("regenerate once, after slice 04") is discharged: `pbcOverTime.png` and
  `percentilesOverTimeWorkItemAge.png` changed; `percentilesOverTime.png` came back byte-identical, which
  is the *correct* result because slice 04 does not touch that widget — and it is a genuine re-capture,
  not the comparator keeping a stale file, because the old PNGs were removed first.
- **ADO** — story **#5549** to be transitioned by the orchestrator; this finalization pass does not touch
  the board.
- **Outcomes registry** — **N/A**: no `docs/product/outcomes/registry.yaml` in this repo. Testable
  outcomes live in `kpi-contracts.yaml`; all five `OUT-5427-*` rows carry slice-04 measurements.

### Epic status at slice-04 close

All six user stories delivered. Planned scope complete. Carried forward, none of it planned work: the
slice-03b UTC/local URL round-trip defect (affects every date-ranged widget, to be filed separately);
the typed-inverted-range blank cards (hooks have no error state); the known defect against
`OUT-5427-empty-state-honesty`'s second clause (an owner whose snapshots all predate a still-ending-today
window reads the forward-only copy); slice-03's two unrecoverable evidence gaps (no mutation score of its
own, no adversarial review of its diff); the collapsed-band `Status = Ready` question at the chart
builders; and, still open since the DISTILL gate and now the epic's single outstanding DEVOPS action
item, the **operator monitoring procedure** — the log-scan and alert-rule guidance for the
recording-failed event was deferred to "epic completion" at slice-02, and this is epic completion.

## Links

- ADRs: `docs/product/architecture/adr-106-percentiles-over-time-snapshot-table-shape.md` · `adr-107-percentiles-recording-handler-on-refresh-events.md` · `adr-108-percentiles-over-time-series-http-contract.md` · `adr-109-demo-percentiles-backfill-handler.md`
- Architecture: `docs/product/architecture/brief.md` → "Application Architecture — epic-5427-percentiles-over-time (Epic 5427)"
- KPI contracts: `docs/product/kpi-contracts.yaml` → 5 `OUT-5427-*` rows
- ADR amendments (slice-02): the **Amendment (slice-02, 2026-07-25)** section of each of ADR-106 / ADR-107 / ADR-108 / ADR-109
- ADR amendment (slice-04): the **Amendment (slice-04, 2026-07-26)** section of ADR-109 — the demo backfill was *not* extended to the five new process-behaviour families, correcting the slice-02 amendment's forward statement
- CI learnings from slice-02: `docs/ci-learnings.md` → the two 2026-07-25 entries (SQLite `disk I/O error` runner flake; NUnit2045 INFO-severity gate failure)
- CI learnings from slice-04: `docs/ci-learnings.md` → **NUnit2045 recurrence 5** (2026-07-26) — the violation lived in a private assertion helper, not a `[Test]` method, which is the shape a "check each test method" pre-scan misses
- User-facing docs: `docs/metrics/predictability.md` → "PBC Over Time" (all six families; Feature Size portfolio-only; demo backfill covers Throughput only)
- Clients parity: `lighthouse-clients` `5bcb2a6` — 4 client methods, 2 CLI metrics, 4 MCP tools for **both** over-time endpoints, gated on server `v26.7.11.4`; closes a gap spanning slices 01-04
- Feature workspace (full wave history, **preserved — the wave matrix derives status from it**): `docs/feature/epic-5427-percentiles-over-time/feature-delta.md` (one narrative file, lean v3.14 — sections tagged `## Wave: <NAME> / [REF] …`, no `discuss/`/`design/`/`distill/` subdirectories), the five `slices/slice-0N-*.md` briefs, the six `acceptance/*.feature` specs, and `deliver/` — where `roadmap.json` + `execution-log.json` are slice-04's and the earlier slices are archived alongside as `roadmap-slice-01/02/03/03b.json` and `execution-log-slice-01/02/03.json`
