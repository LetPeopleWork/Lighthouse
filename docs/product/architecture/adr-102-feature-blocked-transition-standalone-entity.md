# ADR-102: Feature Blocked Spells Live in a Standalone `FeatureBlockedTransition` Entity with FK → `Features`, Not in `WorkItemBlockedTransition`

**Status**: Accepted (2026-07-19 — Morgan, interaction mode PROPOSE)
**Date**: 2026-07-19
**Feature**: portfolio-blocked-history (Story #5524, slices 01–02)
**Decider**: Morgan (Solution Architect); pre-settled by the user at DISCUSS (D3) and confirmed against the EF configuration during DESIGN (feature-delta C5)
**Relationship to prior ADRs**: applies ADR-015 (Option C — standalone transition entity with FK + cascade, not owned-collection, not JSON column) to the `Feature` aggregate. Extends ADR-068 (`WorkItemBlockedTransition`) with a sibling rather than by generalisation. Upholds ADR-067 (`IBlockedItemService` is the single blocked authority). Resolves DISCUSS D3 / journey D10 and DESIGN DDD-1.

---

## Context

Story #5524 must record blocked enter/leave spells for `Feature` so a Portfolio can answer historic blocked reads. `WorkItemBlockedTransition` (ADR-068) already exists and is shaped for exactly this, so the obvious move is to reuse it. Three verified facts make that unsafe.

**1. The table is keyed by a bare `int` with no owner discriminator.**

```csharp
// Models/WorkItemBlockedTransition.cs:5-12
public class WorkItemBlockedTransition : IEntity
{
    public int Id { get; set; }
    public int WorkItemId { get; set; }
    public DateTime EnteredAt { get; set; }
    public DateTime? LeftAt { get; set; }
}
```

`Feature` and `WorkItem` are separate tables with independent identity sequences. Neither `WorkItemBlockedTransitionRepository.GetBlockedTransitionsAt` (`:26`) nor `GetBlockedWorkItemIdsAt` (`:10`) nor `GetWorkItemIdsWithBlockedHistory` (`:18`) applies any owner scoping.

**2. The table carries an enforced FK to `WorkItems` with cascade delete on both providers.**

```csharp
// Data/LighthouseAppContext.cs:228-232
modelBuilder.Entity<WorkItemBlockedTransition>()
    .HasOne<WorkItem>()
    .WithMany()
    .HasForeignKey(t => t.WorkItemId)
    .OnDelete(DeleteBehavior.Cascade);
```

Mirrored in `20260705130040_AddWorkItemBlockedTransition.cs:27-32`. So writing a `Feature.Id` into that column has two outcomes, neither acceptable:

| Branch | Behaviour |
|---|---|
| `Feature.Id` collides with an existing `WorkItem.Id` | Insert succeeds. `TeamMetricsController:148` (`openSpellByItem.GetValueOrDefault(w.Id)`) returns the **feature's** spell → a team work item reports blocked on a range it was never blocked in. Corrupts the Story #5508 historic read. |
| `Feature.Id` has no matching `WorkItem` | FK violation → `DbUpdateException` → **swallowed by the domain-event dispatcher** → the write path aborts silently. |

**3. This is not hypothetical — it ships today.** `DemoBlockedHistoryBackfillHandler:90` selects `feature.Id` on the portfolio path and passes it to `UpsertBackdatedTransition:191`, which writes it as `WorkItemId`. It shipped green because `DemoBlockedHistoryBackfillHandlerTests` uses `UseInMemoryDatabase`, and **EF InMemory does not enforce foreign keys**.

The precedent already exists in this codebase for exactly this problem: `FeatureStateTransition` (`Models/FeatureStateTransition.cs`) is a separate entity from `WorkItemStateTransition`, keyed `FeatureId`, with its own FK + index (`LighthouseAppContext.cs:245-252`), for the same identity reason.

---

## Decision

**A new standalone `FeatureBlockedTransition` entity, ADR-015 Option C shape, FK → `Features` with cascade delete.**

```csharp
public class FeatureBlockedTransition : IEntity
{
    public int Id { get; set; }
    public int FeatureId { get; set; }
    public int PortfolioId { get; set; }   // keyspace grain — see ADR-103
    public DateTime EnteredAt { get; set; }
    public DateTime? LeftAt { get; set; }  // null = open (current) spell
}
```

EF model:

- New `DbSet<FeatureBlockedTransition> FeatureBlockedTransitions` on `LighthouseAppContext`.
- FK `FeatureId → Features.Id`, `OnDelete(DeleteBehavior.Cascade)` — matching `FeatureStateTransition` (`:245-249`).
- FK `PortfolioId → Portfolios.Id`, `OnDelete(DeleteBehavior.Cascade)` — see ADR-103 for why the portfolio is part of the key, and ADR-104 §Lifecycle for what this buys.
- Index `(PortfolioId, EnteredAt)` — the at-date reconstruction scan, the hot read.
- Index `(FeatureId, PortfolioId)` — the open-spell lookup during capture.
- `Feature` does NOT gain a navigation collection (ADR-015's read-path rule: the feature table render must load zero transition rows).
- Migration generated via the existing `CreateMigration` PowerShell script across all providers, **expand-only** (additive; no destructive cleanup this release). Build with `--no-incremental` when regenerating (stale-migration-DLL trap).

New repository `IFeatureBlockedTransitionRepository : IRepository<FeatureBlockedTransition>` over `RepositoryBase<T>`. **Every query signature takes `portfolioId`.** The unscoped shape of `WorkItemBlockedTransitionRepository:10/18/26` is the defect this ADR exists to avoid — it is not the pattern to copy.

`WorkItemBlockedTransition` is **unchanged**. Its FK, cascade and team read paths keep working exactly as ADR-068/099 shipped them.

---

## Alternatives Considered

**Option A — `OwnerType` discriminator on the existing `WorkItemBlockedTransition`.**

Add `OwnerType { Team, Portfolio }` alongside `WorkItemId`, scope every repository query by it, migrate existing rows to `OwnerType.Team`.

- Pros: one table, one repository, one handler pair; the `BlockedCountSnapshot` precedent (`OwnerId` + `OwnerType`, `LighthouseAppContext.cs:237-243`) shows the codebase already accepts a discriminated owner store.
- Cons, decisive: the discriminator column cannot coexist with `FK_WorkItemBlockedTransitions_WorkItems_WorkItemId`. A row with `OwnerType = Portfolio` and `WorkItemId = <Feature.Id>` violates it. Adopting this option therefore requires **dropping** that FK — surrendering referential integrity and cascade delete on the team path, which works correctly today, in order to accommodate features. That is a strict regression on shipped behaviour, traded for saving one table. It also converts every existing team query into one that is correct only if a filter is remembered, which is precisely the failure mode (unscoped queries) that produced the current defect.
- **Rejected.** The FK is load-bearing; the discriminator is not worth it.

**Option B — reuse `FeatureStateTransition` with a synthetic "Blocked" pseudo-state.**

- Pros: no new entity, no migration.
- Cons: blocked is orthogonal to state (a feature can be blocked in any state); rule sets match on tags and additional fields, not only state, so a state stream cannot represent a tag-driven block. README L1 forbids it. ADR-068 already rejected the identical proposal for work items (its Option C).
- **Rejected**, on the same grounds ADR-068 rejected it.

**Option C — a JSON column of spells on `Feature`.**

- Pros: no new table.
- Cons: the primary read is an interval-overlap scan across *all* features in a portfolio at a date (`GetBlockedTransitionsAt`), which under a JSON column means loading every feature and filtering in C#, on both SQLite and Postgres. Unbounded growth on a read-heavy row. ADR-015 Option B rejected this shape for the same reasons.
- **Rejected.**

---

## Consequences

**Positive**:

- `WorkItemBlockedTransition`'s FK, cascade and team historic read (Story #5508) are untouched — the phantom-spell class is structurally eliminated rather than filtered at read time. Feature ids simply cannot enter the work-item keyspace: the type system and the FK both refuse.
- Cascade delete on `FeatureId` means feature deletion (including `orphan-feature-cleanup`) disposes of spells with no application code.
- Owner-scoped repository signatures make the "forgot the owner filter" bug non-representable at the call site.
- The read shape mirrors `TeamMetricsController:130-156` closely enough that the parity KPI (Team vs Portfolio agreement on the same range) is testable as a single matrix.

**Negative**:

- One more entity, repository, migration and handler pair to maintain — two near-identical capture mechanisms that must stay behaviourally aligned. Mitigated by the parity integration matrix; not mitigated by code sharing, which is what Option A tried and what the FK forbids.
- Two cascade paths into one table (`Features`, `Portfolios`). Fine on SQLite and Postgres, the only supported providers; SQL Server would reject multiple cascade paths. Verify at migration-generation time.

**Neutral**:

- No contract change. `blockedSince` already exists on `WorkItemDto` and is transmitted as `null` for features today; populating it is additive (ADR-062 rule, per ADR-072). No CLI/MCP client version gate.

---

## Earned Trust — probing the keyspace

Every probe below MUST run against **SQLite**, not `UseInMemoryDatabase`. InMemory does not enforce foreign keys, so an FK-dependent assertion passes identically for broken and fixed code — the reason the current defect shipped green.

- **Keyspace-isolation probe**: create a `Feature` with `Id = N` and a never-blocked `WorkItem` with `Id = N`; open a feature spell; assert `GET /teams/{id}/metrics/wip?asOfDate=<past>` reports the work item `isBlocked = false` and `blockedItemsAtDate` excludes it. (US-01 AC1/AC2.)
- **Referential-invariant probe**: repository-level scan asserting no `WorkItemBlockedTransition.WorkItemId` lacks a corresponding `WorkItem`. Use `GetAllByPredicate(...).Any()` — `GetByPredicate`/`Exists` use `SingleOrDefault` and throw on multiple matches. (US-01 AC3.)
- **FK-lie probe**: attempt to insert a `FeatureBlockedTransition` with a non-existent `FeatureId` against SQLite and assert the write is refused. This probe is the one that would have caught the current defect; it exists to demonstrate the test substrate enforces what the schema claims.
- **Cascade probe**: delete a `Feature` → its spells vanish. Delete a `Portfolio` → its spells vanish. Assert both against SQLite.
- **Outcome-not-exception probe**: the dispatcher swallows handler exceptions, so every capture assertion asserts **rows present/absent**, never absence-of-throw.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| `Feature.Id` never enters `WorkItemBlockedTransition` | Repository invariant test (above) + ArchUnitNET: `DemoBlockedHistoryBackfillHandler` and the feature capture handlers may not reference `IWorkItemBlockedTransitionRepository` |
| `Feature` holds no `FeatureBlockedTransition` navigation collection | NUnit reflection test, mirroring ADR-015's enforcement row |
| Every `IFeatureBlockedTransitionRepository` query is portfolio-scoped | NUnit: interface reflection asserts no query method omits a `portfolioId` parameter |
| Spell tables are reached only via their repositories | ArchUnitNET: classes outside `Services.Implementation.Repositories` may not reference `DbSet<FeatureBlockedTransition>` |
| FK-dependent tests do not run on InMemory | Test-fixture convention + a `ci-learnings.md` entry (this defect is not yet in the ledger) |

---

## Cross-feature impact

- ADR-068: sibling entity, same enter/leave spell semantics; `WorkItemBlockedTransition` unchanged.
- ADR-099: the portfolio branch of blocked-membership reconstruction becomes implementable; `PortfolioMetricsController:498-500`'s comment asserting it is impossible becomes false and is removed.
- ADR-015/016/017: same standalone-entity-with-FK idiom, third application.
- `orphan-feature-cleanup`: cascade delete covers orphan disposal; no coordination required.
- `docs/ci-learnings.md`: candidate entry — "EF InMemory does not enforce FKs; any test asserting FK-dependent behaviour must run on SQLite."
