# Slice 01 — An update log that says what the update did

**Feature**: epic-5687-faster-updates · **ADO**: Epic #5687 → Story #5724 · **Story**: US-01 · **Estimate**: ~4h
**Reference class**: `RefreshLog` / `IRefreshLogService` — the duration and item count this slice reports
are already recorded there by `TeamUpdater`; this slice adds two fields and one log line, and turns the
existing noise down.

## Goal

Every completed update writes one Information line saying which entity, in which mode, how many records
it saw, how many it fetched, and how long it took — and stops writing the per-entity, per-item stream
that currently buries it.

## IN scope

- `RefreshLog` gains `Mode` (`full` | `delta`), `RecordsScanned`, `RecordsFetched`. Additive, expand-only
  migration via the `CreateMigration` script.
- One structured summary line emitted from `UpdateServiceBase` / the two updaters after an update
  completes, success or failure.
- `mode` is hard-coded `full` in this slice. The field exists before delta does, so later slices change
  the data, not the format — and slice 02's acceptance criteria have something to read.
- Log-level surgery on the update path. **Measured baseline (DISTILL, 2026-08-09): 153 Information-level
  lines for a 25-item team refresh, 416 for a 25-Feature portfolio refresh.** The list below is where to
  start, not the whole of it — AC-1.7's ≤ 2 lines per entity is the boundary, and the acceptance tests
  are what enforce it:
  - `UpdateServiceBase.UpdateAll` "Checking last update for {Entity}" → Debug.
  - `TeamUpdater.ShouldUpdateEntity` / `PortfolioUpdater` "Last Refresh of … was N minutes ago" → Debug.
  - The three copies of "Updating Work Items for Team {X}" (`WorkItemService.UpdateWorkItemsForTeam`,
    `.RefreshWorkItems`, `JiraWorkTrackingConnector:114` / `AzureDevOpsWorkTrackingConnector:50`)
    collapse to one, at the top of the update.
  - Per-item and per-feature lines (`Added/Updated/Removed Work Item`, `Feature … Extrapolating`,
    "Added N Items to Feature") → Debug.
  - The rest of `WorkItemService`: "Updating / Done Updating Features for Portfolio", "Updating / Done
    Updating Remaining Work for Portfolio", "Owning Team for Portfolio …", "Feature Owner Field …",
    "Found following teams …", "Added {n} Items for Feature {x} to Team {y}", "Using Percentile …",
    "Features had following number of child items", "{Percentile} Percentile Based on …" → Debug.
  - `TeamDataService`: "Updating / Finished updating Team Data for {TeamName}" → Debug.
  - Errors and warnings untouched. Nothing is deleted; noise is demoted, never dropped.
- A logger-capturing test asserting ≤2 Information lines per entity per update, and 0 for a skipped one.

### Coverage boundary on AC-1.5

The acceptance tests fake `IWorkTrackingConnector`, so they observe only the two copies of "Updating Work
Items for Team" that live in `WorkItemService`. Each connector's own copy is below the faked port — it is
covered by the code change and by the connectors' existing tests, not by the AT. Do not read a green AT
as proof the connector copy is gone.

### Shared-contract blast radius (measured)

DDD-7 changes `ITeamDataService.UpdateTeamData` to return a sync outcome instead of `Task`. Every call
site, counted:

- `ITeamDataService.UpdateTeamData` — one production caller, `TeamUpdater`.
- `IWorkItemService.UpdateFeaturesForPortfolio` — one production caller, `PortfolioUpdater`, plus two
  hand-written test fakes that implement the interface and must change in the same commit:
  `Lighthouse.Backend.Tests/API/Integration/PortfolioDeleteSerialisationTests.cs` and
  `…/TeamDeleteSerialisationTests.cs`.

## OUT of scope

- Any change to what is fetched. Not one remote call moves in this slice.
- Any UI. `/api/v1|latest/update/status` is untouched (Epic #5511 owns the view).
- Structured-logging framework changes, log sinks, correlation ids, OpenTelemetry.
- The `RefreshLog` retention policy.

## Learning hypothesis

**Disproves "an update knows what it fetched"** — the assumption every later slice rests on. If the
summary line cannot be assembled from what the update already has in hand, then the sync path does not
track its own scope, and reporting a delta (let alone proving one) needs a bigger change than this epic
budgets. Two ways it fails:

1. **The counts are not available at the point the log is written.** `itemCount` in `TeamUpdater` is read
   off `team.WorkItems.Count` after the fact, not from the fetch. If scanned-vs-fetched cannot be
   threaded out of `WorkItemService` without restructuring it, delta reporting is a redesign, not a field.
2. **The noise is load-bearing.** If demoting the per-entity lines makes a real diagnostic disappear —
   something the maintainer actually uses to debug a stuck update — then the log is not noise, it is a
   poor man's progress bar, and the answer is Epic #5511, not this slice.

Confirms, if it succeeds: the instrument exists, and KPI-1/KPI-2/KPI-5 become measurable.

## Acceptance criteria

AC-1.1 … AC-1.9 from `feature-delta.md` (US-01).

## Dependencies

None. This slice is unblocked and blocks nothing but the *measurement* of slices 02-08.

## Effort

~4.5h left. Migration **done** in DISTILL (`AddRefreshLogModeAndRecordCounts`, both providers,
expand-only), tests **done** in DISTILL. Remaining: summary line + outcome plumbing ~1.5h, log-level pass
~2.5h. The log-level pass was originally estimated at ~1.5h against the shorter demotion list above; the
measured 153/416 baseline is what moved it.

The migration could not be deferred: EF treats `PendingModelChangesWarning` as an error inside
`Database.Migrate()`, so the three `RefreshLog` columns red 55 host-booting tests until the migration
exists. The acceptance tests themselves would not have caught it — they build their schema with
`EnsureCreated`. Any further model change in this epic needs its migration in the same commit.

## Production data / dogfood moment

Restore a real backup onto the `:5169` dev instance and let one full refresh cycle run against real
recorded history. Read the log. If the cycle is not legible in under ten seconds of reading, the slice
is not done — that is the acceptance, and it happens the same day.

## Pre-slice SPIKE

Not needed. No unknown mechanism.

## Verdict

**Confirmed on the code, open on the dogfood read.**

The hypothesis was *"an update knows what it fetched"*. It does. Both counts are sourced from what the
connector returned, inside the method that fetched it, and travel back to the updater as a `SyncOutcome`
— no restructuring of `WorkItemService` was needed, and neither failure mode fired:

1. *The counts are not available where the log is written.* They were. `RefreshWorkItems` and
   `RefreshFeatures` each materialise the connector's return once and hand back
   `SyncOutcome.FullSync(recordsFromTracker.Count)`. Nothing reads `team.WorkItems.Count` after the fact.
2. *The noise is load-bearing.* It was not. Every demoted line still exists at Debug, and two of them are
   pinned there by acceptance tests as positive controls. Nothing was deleted.

**KPI-5, measured** (25-item team refresh / 25-Feature portfolio refresh / skipped cycle): **8 → 2**,
**10 → 2**, **2 → 0**. Target ≤ 2, and 0 for a skipped entity — met on all three.

The 153 / 416 baselines in this brief are **wrong and superseded**. They were measured through a capture
logger missing production's `MinimumLevel.Override` block, so ~95 % of what they counted was EF Core
`Executed DbCommand` SQL that no operator sees. The honest before-figures are 8 and 10.

**Dogfood read: passed** (2026-08-09, manual verification by the maintainer). The cycle is legible, which
was the slice's own bar. Both halves of the hypothesis are therefore answered — the mechanism reports
its own scope, and the result is readable by the person the log is for.
