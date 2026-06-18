# Slice 04: Expand-only EF migrations + safe startup under N replicas

**Feature**: epic-5305-k8s-readiness
**Story**: US-04 (ADO #5308) → job-operator-zero-downtime-rollout + job-operator-survive-multiple-replicas
**Estimate**: ~2–2.5 crafter days
**Reference class**: EF migration mechanics (hit the stale-migration-DLL `--no-incremental` trap in `delivery-target-date-tracking`); concurrency coordination akin to Epic 5121

## Goal
Two coupled guarantees: (1) each release's migrations are additive-only (expand now; destructive cleanup deferred to a LATER release) so old pods never depend on a dropped column during a rollover; (2) when N replicas boot concurrently, exactly one applies migrations while the rest wait — no race on `Database.Migrate()`.

## IN scope
- **Expand-only discipline**: a guard/check (analyzer, test, or migration-review gate) that fails CI if a migration in this release is destructive (drop/rename column/table) — destructive ops must be a separate later release. Document the expand → contract two-release pattern.
- **Startup migration coordination**: a migration lock / dedicated init mechanism / leader so exactly one replica runs `Migrate()`; others wait until migrations are applied, then start serving.
- **Standalone gate**: a single SQLite or Postgres instance still auto-migrates on boot exactly as today (lock is a no-op / trivially-acquired with one instance).

## OUT scope
- The actual cluster-wide update-queue redesign → slice 07.
- Provider-matrix migration generation uses the existing `CreateMigration` PowerShell script (per CLAUDE.md) — not new tooling.

## Learning hypothesis
**Confirms if it succeeds**: 3 replicas started simultaneously against one fresh Postgres apply the migration exactly once (one applies, two wait), and a destructive migration is rejected by CI before merge.
**Disproves if it fails**: app-level migration coordination is too fragile under k8s and we must move migrations into a dedicated pre-deploy Job / ArgoCD sync-wave (decision pushed to Productization #5306) — in which case this slice delivers the expand-only guard + a documented "migrate via Job" path instead of an in-process lock.

## Acceptance criteria
See US-04 in `../feature-delta.md`. Key: an integration/concurrency test starts N hosts against one DB and asserts a single migration application (e.g. via a migration-history assertion / lock observation); a CI check rejects a destructive migration; single-instance boot auto-migrates unchanged.

## Dependencies
None hard. Feeds slice 02's "migrations applied" readiness signal. Precedes real multi-replica operation (slice 07).

## Production data requirement
**Required.** Reproduce concurrent startup against a real Postgres (k3s, 3 replicas) — InMemory tests will NOT catch the race (recurring lesson: persisted-model migration traps are invisible to InMemory).

## Dogfood moment
Operator scales a fresh deploy to 3 replicas against an empty Postgres and observes one migration application in the logs, all pods healthy.

## Cross-cutting checklist (confirmed in feature-delta)
RBAC: N/A. Clients: N/A — no API contract; possibly a CLI connection hint for Postgres, confirm in DESIGN. Website: N/A.

## Pre-slice spike candidates
- Evaluate `PostgreSQL advisory lock` vs. a migration-history sentinel vs. an init-Job approach for the boot lock. (~2 hr)
- Prototype the destructive-migration CI guard (parse generated migration for `DropColumn`/`DropTable`/`RenameColumn`). (~1 hr)
- Confirm the SQLite path degrades the lock to a no-op. (~30 min)
