# wait-states-flow-efficiency — Slice 05 Close-Out

Cross-cutting close-out for the wait-states-flow-efficiency feature (Epic / Story #5173).
Folds in the three DELIVER review action items and pins the standing invariants with guardrail tests.

## Guardrails added (step 05-01)

### 1. D9 overlay-only acceptance test (review action item #2)

`FlowEfficiencyReadApiIntegrationTest.GetFlowEfficiency_WithWaitStatesDefined_DoesNotChangeAnyOtherMetric`

Seeds a team with Doing-time and **no** wait states, captures the `throughput`, `cycleTimePercentiles`,
and `ageInStatePercentiles` endpoint bodies, then defines wait states through the **real settings PUT
endpoint** (read-modify-write of the settings JSON, mutating only `waitStates` so the sole configuration
delta is the overlay), and re-captures the same three bodies. Each other-metric body must be **byte-identical**
before and after — wait states are a labelling overlay and must not shift any other metric.

Falsifiability confirmed: corrupting the equality (comparing `throughputAfter` against a different body)
flips the test RED, proving the assertion is live, not vacuous.

### 2. ComputeFlowEfficiency seam ArchUnit guard (ADR-024)

`FlowEfficiencySeamArchUnitTest` (mirrors the sibling `CumulativeStateTimeSeamArchUnitTest`):

- `ComputeFlowEfficiency_IsProtected_AndNotExposedViaAnyInterface` — asserts `ComputeFlowEfficiency` is a
  **protected** member of `BaseMetricsService` (intra-inheritance only, never public) and that **no interface**
  in the production assembly declares a member of that name.
- `NoFlowEfficiencyAggregationService_WasIntroduced` — asserts no `IFlowEfficiencyService` /
  `FlowEfficiencyService` / per-state aggregation service type exists. Flow efficiency stays a fold on
  `BaseMetricsService` computed from the team's/portfolio's `WaitStates` overlay.

ADR-024 (no shared per-state-aggregation service) is the invariant this protects, upheld for the 5th time.
Falsifiability confirmed: making `ComputeFlowEfficiency` public flips the protected-member rule RED.

### 3. EF stale-migration guard (review action item #3)

The `WaitStates` column was added in slice 01-01 via the `CreateMigration` script across both providers:

- Sqlite: `Lighthouse.Migrations.Sqlite/Migrations/20260605065727_AddWaitStates.cs` — `WaitStates TEXT NOT NULL DEFAULT '[]'` on `Teams` and `Portfolios`.
- Postgres: `Lighthouse.Migrations.Postgres/Migrations/20260605065737_AddWaitStates.cs` — `WaitStates text[] NOT NULL` on `Teams` and `Portfolios`.

Verification performed in 05-01:

- `dotnet build --no-incremental` on both migration projects → 0 warnings, 0 errors. The `--no-incremental`
  rebuild is **mandatory** even for a fresh migration: the Epic-4144 slice-04 trap is that an incremental
  build can keep a stale migration DLL, so the new column is silently absent at `dotnet test` time. The
  full `dotnet test` was run **after** the `--no-incremental` rebuild.
- Sqlite apply is exercised live by `FlowEfficiencyReadApiIntegrationTest` (and the portfolio sibling), which
  call `EnsureCreated()` against a real SQLite file per fixture and then read/write the `WaitStates` column
  through the settings PUT/GET round-trip — green confirms the column applies and round-trips.
- The Postgres `text[]` migration is well-formed and rebuilds clean; the `verifypostgres` CI job applies it
  against a real Postgres container.

## Docs & learn-more (acceptance criterion #4)

The `flowEfficiency` tile already carries a `learnMoreUrl` of `${DOCS_BASE}#flow-efficiency` in
`Lighthouse.Frontend/src/pages/Common/MetricsView/widgetInfoMetadata.ts`; the cumulative chart's
wait-highlight surfaces the same flow-efficiency concept. The public docs-site anchor `#flow-efficiency`
is a **follow-up** — per the project rule that documentation changes wait for user confirmation in the live
environment, this close-out does not block on a website/docs-site edit. The in-app learn-more wiring is in
place and points at the canonical docs base.

### Screenshot (live walking-skeleton observation, 04-02)

The live walking-skeleton run in 04-02 observed the Flow Efficiency tile reading **75%** with **2 highlighted
wait bars** on the cumulative state-time chart (the highlighted bars correspond to the configured wait states).
This confirms the tile and the chart's wait-highlight render together against demo data. A static screenshot
capture is a docs-site follow-up alongside the `#flow-efficiency` anchor.

## Pre-existing architecture-seam leak — surfaced AND fixed

Running the Architecture suite during this close-out surfaced a **pre-existing RED** test, independent of
05-01 and confirmed red at HEAD (`1393cda0`) with the 05-01 guardrails stashed:

`ModuleBoundariesArchUnitTest.ServiceLayer_DoesNotDependOnApiLayer` failed because
`Lighthouse.Backend.API.DTO.FlowEfficiencyInfoDto` lived in the **API driving-adapter** namespace, yet the
Services core (`ITeamMetricsService`, `IPortfolioMetricsService`, `BaseMetricsService`,
`TeamMetricsService`, `PortfolioMetricsService`) returns it — violating the ADR-027 D3/D5 hexagonal seam
(core DTOs must live in `Models.*`, not `API.DTO`).

Root cause: slice 01-02 (`b7573642`) placed `FlowEfficiencyInfoDto` under `Lighthouse.Backend/API/DTO/`,
whereas **every** sibling Info DTO (`ThroughputInfoDto`, `ArrivalsInfoDto`, `CycleTimePercentilesInfoDto`,
`WipOverviewInfoDto`, …) correctly lives in `Lighthouse.Backend/Models/Metrics/InfoWidgetDtos.cs`. An
isolated deviation, not a systemic one.

This is the same hexagonal-seam invariant (ADR-024/ADR-027) that 05-01's `FlowEfficiencySeamArchUnitTest`
protects — shipping a close-out that pins the seam while the seam was actively violated would be incoherent,
and the fix is a small, mechanical relocation matching the established sibling pattern. So it was applied here
(a measured scope extension beyond the original `files_to_modify`, justified by the seam-protection purpose
of this very step):

1. Moved `FlowEfficiencyInfoDto` into `Lighthouse.Backend/Models/Metrics/InfoWidgetDtos.cs`
   (namespace `Lighthouse.Backend.Models.Metrics`), alongside its siblings; deleted the old
   `Lighthouse.Backend/API/DTO/FlowEfficiencyInfoDto.cs`.
2. Dropped the now-unused `using Lighthouse.Backend.API.DTO;` from the 5 Services files and from
   `BaseMetricsServiceTests` (they reference no other genuinely-`API.DTO` type — their remaining DTOs are in
   `Models.Metrics` / `Models.Forecast`). The 2 metrics controllers keep the `using` (they still use
   `FeatureDto` / `WorkItemDto`, which are genuinely in `API.DTO`).

Result: `ModuleBoundariesArchUnitTest.ServiceLayer_DoesNotDependOnApiLayer` is now GREEN alongside the new
`FlowEfficiencySeamArchUnitTest`.

## Quality gates (05-01)

- `dotnet build` (test project): 0 warnings, 0 errors.
- New tests green: 2 seam tests + 1 D9 AT (+ the 11 pre-existing FlowEfficiency ATs stay green).
- `dotnet build --no-incremental` on both migration projects: clean.
- No new SonarCloud violations expected — the new test code applies the recurring ledger rules
  (`Assert.EnterMultipleScope` for grouped asserts, concrete return types on private helpers, no inline
  constant-array args, `Is.Zero` where applicable).
