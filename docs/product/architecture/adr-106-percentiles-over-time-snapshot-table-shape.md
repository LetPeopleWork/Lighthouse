# ADR-106: Percentiles-over-time uses two purpose-shaped snapshot tables, not one wide discriminator

- **Status**: Accepted
- **Date**: 2026-07-23
- **Feature**: epic-5427-percentiles-over-time (ADO Epic 5427)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

Epic 5427 records three metric families day by day so the UI can plot honest *trend* charts (DISCUSS D5, forward-only, latest-write-wins per calendar day): **cycle-time percentiles** over horizons 30/60/90, **work-item-age percentiles** (no horizon — age is as-of-today), and **process-behaviour (PBC) natural process limits** (UNPL/Average/LNPL) for up to six metric types (Throughput, WIA, WIP, CT, Arrivals, Feature-Size).

Two shapes are in play. The percentile families (CT, WIA) share an **identical** value shape — four percentiles `P50/P70/P85/P95` on the D7 red→green ramp. The PBC family has a **different** value shape — three natural-process-limit values (UNPL, Average, LNPL) — and no percentile meaning.

The existing snapshot precedent is **per-concern, not god-table**: `DeliveryMetricSnapshot` (Epic 3993) and `BlockedCountSnapshot` (Epic 5074) are separate tables, each with its own repository (`RepositoryBase<T>`, `GetByPredicate(OwnerId/OwnerType/RecordedAt)`), each keyed `(OwnerId, OwnerType, RecordedAt)` where `RecordedAt` is a `DateOnly`. ADR-090 bounds metric cardinality; every extra row-per-day multiplies against it.

## Decision

Two new snapshot entities, each a `RepositoryBase<T>` with an `IEntity` id, following the `BlockedCountSnapshot` idiom exactly:

1. **`PercentilesOverTimeSnapshot`** — CT + WIA percentile families collapse into one table (same value shape):
   - `OwnerId : int`, `OwnerType : OwnerType` (Team | Portfolio), `RecordedAt : DateOnly`
   - `MetricType : PercentileMetricType` (CycleTime | WorkItemAge) — discriminator
   - `Horizon : int?` — 30/60/90 for CycleTime; **null** for WorkItemAge (age has no lookback dimension, US-03 AC1)
   - `Percentile50, Percentile70, Percentile85, Percentile95 : double`
   - Natural key `(OwnerId, OwnerType, MetricType, Horizon, RecordedAt)` — unique index makes one-row-per-day structural.

2. **`ProcessBehaviorSnapshot`** — the PBC family (distinct NPL shape):
   - `OwnerId : int`, `OwnerType : OwnerType`, `RecordedAt : DateOnly`
   - `MetricType : ProcessBehaviorMetricType` (Throughput | WorkItemAge | Wip | CycleTime | Arrivals | FeatureSize)
   - `Unpl, Average, Lnpl : double`
   - Natural key `(OwnerId, OwnerType, MetricType, RecordedAt)`.

EF migration is **additive/expand-only** — two new tables, no change to existing schema — generated via the `CreateMigration` PowerShell script across all providers (never `dotnet ef migrations add` directly).

## Alternatives considered

- **One wide discriminator table** (metric-family + type + horizon + four generic value columns): forces PBC's `UNPL/Average/LNPL` into percentile-named columns — a semantic mismatch that leaves half the columns null per row and overloads the schema meaning by row. Every new metric type is another expand-`ALTER`, and row cardinality (CT×3 + WIA + PBC×6) all lands in one table against ADR-090. Diverges from the codebase's established per-concern precedent. **Rejected.**
- **Per-family, three tables** (`CTPercentileSnapshot` / `WIAPercentileSnapshot` / `ProcessBehaviorSnapshot`): clean natural keys, but CT and WIA have the *identical* four-percentile shape, so it is three tables where two suffice and duplicates the percentile columns and their EF config. **Rejected** — the `MetricType` discriminator with a nullable `Horizon` collapses CT+WIA without ambiguity.

## Consequences

- **Positive**: each entity's columns mean exactly one thing; the two-table split mirrors the D1 two-widget split (Percentiles-Over-Time vs PBC-Over-Time) and the two typed read endpoints (ADR-108); cardinality stays bounded and legible against ADR-090; migration is purely additive.
- **Accepted cost**: a nullable `Horizon` on `PercentilesOverTimeSnapshot` (null only for WorkItemAge rows) — the unique index includes it, so a null-horizon WIA row and a 30/60/90 CT row never collide.
- **Reuse, not clone**: both entities reuse the forward-only, `RecordedAt : DateOnly`, `(Owner, RecordedAt)` upsert *pattern* of `BlockedCountSnapshot`; they do **not** reuse its columns. New `IPercentilesOverTimeSnapshotRepository` / `IProcessBehaviorSnapshotRepository`, each a thin `RepositoryBase<T>` like `BlockedCountSnapshotRepository`.
- Cross-refs [ADR-069](./adr-069-blocked-count-snapshot-and-over-time-endpoint.md) (snapshot + over-time endpoint grain this follows), [ADR-090](./adr-090-metric-cardinality-bounding.md) (cardinality bound), [ADR-027](./adr-027-target-architecture-modular-monolith-domain-events-cqrs-lite.md) (domain-event bus + persistence idiom). Recording placement in [ADR-107](./adr-107-percentiles-recording-handler-on-refresh-events.md); read contract in [ADR-108](./adr-108-percentiles-over-time-series-http-contract.md); demo data in [ADR-109](./adr-109-demo-percentiles-backfill-handler.md).


## Amendment (slice-02, 2026-07-25) — WorkItemAge persists `Horizon = 0`, not `NULL`

**Status**: Accepted. Amends the Decision and the "Accepted cost" consequence above; the original
text is left intact as the record of what was decided at DESIGN time.

The decision above specifies `Horizon : int?` — "30/60/90 for CycleTime; **null** for WorkItemAge".
Slice-02, which actually shipped the WIA family, persists WIA rows at an explicit sentinel instead:

```csharp
// Models/PercentilesOverTimeSnapshot.cs
public const int NoHorizon = 0;
```

`MetricType = WorkItemAge` rows are written with `Horizon = PercentilesOverTimeSnapshot.NoHorizon`.
Two mechanical reasons, both discovered while making the WIA upsert idempotent-per-day:

1. **SQL NULLs are distinct, so a NULL horizon defeats the unique index.** The natural key is
   `(OwnerId, OwnerType, MetricType, Horizon, RecordedAt)`. On a plain unique index, two rows whose
   `Horizon` is NULL do **not** collide — every refresh would append another WIA row for the same
   day, and the "one row per key per day" invariant (ADR-107, US-02 AC2) would hold structurally for
   CT and not for WIA. `0` is a real value, so the index enforces the invariant for both families
   identically.
2. **EF Core's `Horizon == horizonParam` never matches NULL.** The upsert's find-existing predicate
   translates to `WHERE Horizon = @p`, which is `UNKNOWN` — never `TRUE` — when either side is NULL.
   A null-horizon predicate therefore never finds yesterday's/today's row and the upsert silently
   degrades to an INSERT on every refresh. Matching NULL would require a separate
   `Horizon == null` branch in the predicate, i.e. a second code path for one family — the sentinel
   removes the branch instead of guarding it.

**Migration impact: none.** The column stays `int?` (nullable); slice-02 only changes which value is
written into it. No schema change, no EF migration — expand-only holds trivially. Nullability is
retained rather than tightened to `int` so the change stays additive; a later cleanup could make it
non-nullable once no NULL rows exist anywhere, but that is a destructive migration and is **not**
part of this epic.

**Read side.** Because the sentinel is an implementation detail of the table, `PercentilesOverTimeSeriesQuery.ResolveHorizon`
maps `MetricType.WorkItemAge` → `NoHorizon` before hitting the repository, so no caller (controller,
frontend, CLI) ever has to know the number. See the ADR-108 amendment for the wire contract.

**Enum-ordinal hazard (recorded here because it is a property of this table).** `MetricType` is
persisted as its **integer ordinal**. `WorkItemAge` was therefore *appended* after `CycleTime`.
Reordering or renumbering the enum silently re-maps every already-shipped snapshot row to a different
metric family — a data-corruption bug with no compiler or test signal. New members are appended,
never inserted or reordered; the enum carries that note in its own XML doc.

Cross-refs [ADR-107](./adr-107-percentiles-recording-handler-on-refresh-events.md) (the upsert this
amendment protects), [ADR-108](./adr-108-percentiles-over-time-series-http-contract.md) (how a caller
selects the horizon-less family).
