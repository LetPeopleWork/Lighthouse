# ADR-077: Concurrent-Startup Migration Coordination — Wrap `Database.Migrate()` in a Postgres Advisory Lock (One Replica Applies, Others Wait) + a CI Guard Rejecting Destructive Migrations (Expand-Only / Expand→Contract); Degrades to a No-Op at One Instance

**Status**: Accepted (2026-06-16 — Morgan, Solution Architect; interaction mode PROPOSE. Inherits System Decision 3 / A3 / D4.)
**Date**: 2026-06-16
**Feature**: epic-5305-k8s-readiness (ADO Epic #5305)
**Decider**: Morgan (Solution Architect), confirming the system-designer's Decision 3
**Relationship to prior ADRs**: AMENDS ADR-027 (single-instance default stands; this adds a config-gated branch around the existing boot-time `Migrate()` call). Models on the existing `DatabaseMaintenanceGate` mutual-exclusion seam. Shares the advisory-lock-on-real-Postgres Earned-Trust concern with ADR-076 Option B. Honours D1 (standalone) and D4 (expand-only).

---

## Context

`DatabaseConfigurator.ApplyMigrations` calls `context.Database.Migrate()` (`DatabaseConfigurator.cs:85-92`), invoked once at boot from `Program.cs:973` (skipped only in the Testing environment, `:967-970`). There is **no concurrency guard**.

Two distinct problems appear at N replicas on one shared Postgres (US-04):
1. **Migration race** — every pod that boots runs `Migrate()` concurrently. `Migrate()` under concurrent start is **undefined** (two pods can both try to apply the same migration; EF's `__EFMigrationsHistory` insert is not a cluster-wide mutex by itself across separate connections starting simultaneously).
2. **Destructive migration breaks the old pods still serving** during a rolling update — old + new pods coexist on one Postgres, so a `DROP COLUMN`/`RENAME` applied by a new pod breaks the old pod mid-rollover (D4).

The existing `DatabaseMaintenanceGate` (`Program.cs:954`, a process-singleton that `UpdateQueueService.EnqueueUpdate` consults to refuse work during maintenance, proven serialized by `PortfolioDeleteSerialisationTests`) already establishes the **mutual-exclusion seam** to model a cluster lock on — this ADR extends that pattern rather than inventing one.

## Decision

### 1. Advisory-lock the migration (Postgres provider)

**Wrap the existing `Database.Migrate()` call (`DatabaseConfigurator.cs:85-92`) in a Postgres advisory lock (`pg_advisory_lock`) on a fixed, well-known migration key, for the Postgres provider only.** The first pod to acquire the lock runs `Migrate()`; the rest **block** on `pg_advisory_lock` until it releases, then re-check and see an up-to-date schema and no-op. The lock is acquired on the same session/connection that runs the migration and released when migration completes (and auto-released if the connection drops). This keeps "migrate on boot" — the self-hoster's current model (A3) — while making it cluster-safe.

```
// Postgres provider only; SQLite path is unchanged (no advisory locks, none needed)
await using var conn = /* a dedicated migration connection */;
await conn.OpenAsync();
await conn.ExecuteAsync("SELECT pg_advisory_lock(@key)", MigrationLockKey);
try
{
    context.Database.Migrate();   // one pod applies; the rest already see it applied and no-op
}
finally
{
    await conn.ExecuteAsync("SELECT pg_advisory_unlock(@key)", MigrationLockKey);
}
```

### 2. CI guard: reject destructive migrations in a release (expand-only / expand→contract)

A **CI check rejects a destructive migration** (drop column, drop table, rename column/table) in a release (US-04 AC2). The expand→contract two-release pattern is documented: a release may only ADD (new nullable columns, new tables, new indexes); a *later* release performs the destructive contract step once all old pods are gone. This is enforced at PR time, below build-warning severity, so it must be a dedicated check (a migration-SQL linter / a test that inspects the generated migration's `Up()` operations for `DropColumn`/`DropTable`/`RenameColumn`/`RenameTable`).

Migrations are generated via the existing `CreateMigration` PowerShell script across all providers (per CLAUDE.md), not `dotnet ef migrations add` directly.

## Earned-Trust Probe (MANDATORY)

A startup `probe()` asserting the advisory lock is **genuinely mutually exclusive on the *actual* Postgres** the operator wired — because some connection poolers / proxies break advisory-lock session affinity (**pgBouncer in transaction mode is the classic lie**: each statement may land on a different backend connection, so `pg_advisory_lock` and `pg_advisory_unlock` can hit different sessions and the lock silently does nothing). The probe acquires the lock from two connections and asserts exactly one wins. Probe failure ⇒ `health.startup.refused` naming "advisory lock not session-stable on this connection — use session-mode pooling," and the migration coordination refuses to proceed rather than racing silently. This probe is shared in spirit with ADR-076 Option B (same advisory-lock-on-real-Postgres concern); both are exercised by the same gold-test that catalogues the substrate lie.

## Standalone Degradation (D1 / US-04 AC3)

- **SQLite** has no advisory locks and needs none — at one instance the lock is a **no-op** and `Migrate()` runs exactly as today.
- A **single Postgres instance** likewise auto-migrates on boot: it acquires the lock uncontended and runs `Migrate()` — byte-identical observable behaviour to today, one extra (uncontended) lock acquire/release adding milliseconds to one boot.
- Steady-state cost is zero (the lock is only taken at boot).

## Alternatives Considered

**Chosen: in-process advisory-lock around the existing boot-time `Migrate()` + a CI destructive-migration guard.**
- Pros: keeps the self-hoster's "auto-migrate on boot" model; reuses the `DatabaseMaintenanceGate` mutual-exclusion pattern; degrades to a no-op at one instance (D1); advisory locks auto-release on connection loss (clean liveness); no new infrastructure.
- Cons: requires session-mode pooling (caught by the probe); the CI guard must be maintained as a check below build-warning severity.

**Rejected (deferred, not wrong): dedicated pre-deploy migration Job / ArgoCD sync-wave.** Cleaner migrate→deploy separation, but it is a *cluster/GitOps* mechanism → belongs to Productization #5306, and it would **break the single-container "auto-migrate on boot"** the self-hoster relies on. The slice-04 hypothesis explicitly allows falling back to this *if* the in-process lock proves fragile, recording the decision (A3).

**Rejected (wrong): do nothing / let pods race.** `Migrate()` under concurrent start is undefined behaviour (US-04 AC1 violation).

## Consequences

**Positive**:
- Migrations apply exactly once across the fleet (US-04 AC1); rolling updates are schema-safe (US-04 AC2 via expand-only); the self-hoster's boot-time model is preserved (D1 / US-04 AC3).
- The CI guard makes the expand→contract discipline enforceable, not aspirational.

**Negative**:
- Requires session-mode connection pooling for Postgres advisory locks (documented for operators; caught by the probe).
- The destructive-contract step is deferred to a *later* release — a process discipline operators must follow.

**Neutral**:
- N-1 pods add seconds to one boot waiting on the lock; zero steady-state cost.

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| N hosts started against one DB apply migrations exactly once (US-04 AC1) | Concurrency test (real Postgres): N processes boot together, assert `__EFMigrationsHistory` shows each migration applied once, no duplicate/failed application |
| Destructive migration rejected in a release (US-04 AC2) | CI check inspects generated migration `Up()` for `DropColumn`/`DropTable`/`RenameColumn`/`RenameTable` → fails the build; expand→contract documented |
| SQLite / single-instance auto-migrates as today (US-04 AC3) | Existing migration-on-boot tests run unchanged; lock is a no-op for SQLite |
| Advisory lock is mutually exclusive on the real Postgres | Startup probe (two connections) → `health.startup.refused` on a transaction-mode pooler; shared gold-test with ADR-076 Option B |
| Migrations generated via `CreateMigration` across all providers | Per CLAUDE.md; reviewed in PR |
