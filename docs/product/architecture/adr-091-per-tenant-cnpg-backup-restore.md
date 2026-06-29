# ADR-091: Per-Tenant Postgres = One CloudNativePG `Cluster` per Tenant Namespace (DB-per-Tenant, CC-5); Backups Are CNPG-Native WAL Archiving + Scheduled Base Backups to Off-Cluster S3-Compatible Object Storage, Keyed by Tenant id; Restore Is Namespace-Isolated and Rehearsed

**Status**: **ACCEPTED** (2026-06-29, Benjamin) — O-3: RPO 24h / RTO 30m now, tighten to RPO ≤1h once proven
**Date**: 2026-06-29
**Feature**: epic-5306-productization-platform (ADO Epic #5306, stories #5208 backup/DR, #5207 provisioning) — converges the **per-tenant DB + backup/restore topology** for the locked **CC-5 DB-per-tenant** decision
**Decider**: Benjamin (product owner) + Titan (System Designer, PROPOSE)
**Relationship to prior work**: Realises ADR-080's "CloudNativePG is the recommended production BYO target" — the hosted platform sets `postgresql.enabled=false` and points the #5199 chart's `externalDatabase.*` at a per-tenant CNPG `Cluster`. The DB password is materialised by ESO (ADR-087). The CNPG operator is a `platform/` singleton (ADR-086). No chart template or app code changes.

---

## Context

CC-5 is locked to **DB-per-tenant** (ties to backup scope + the Postgres-only chart). This ADR fixes *how* each per-tenant Postgres is run and *how* it is backed up and restored, meeting KPI-3 (RTO ≤ 30 min, rehearsed) and KPI-4 (RPO ≤ 24h initial, aspirational ≤ 1h), with a hard isolation guardrail (a restore cannot cross tenants — US-09 `@property`).

## Decision

### 1. One CNPG `Cluster` per tenant namespace

The CNPG operator (CloudNativePG, CNCF, official images — vendor-neutral, the ADR-080 production target) is a cluster-singleton. The per-tenant app-of-apps (ADR-086) renders **one `Cluster` CR per tenant** in the tenant namespace, named `<tenant-id>-db`, with credentials from the ESO-materialised Secret (ADR-087). DB-per-tenant = **physical isolation**: a tenant's data lives in its own Postgres instance + PVC inside its own namespace under NetworkPolicy (CC-1). Restore blast radius is exactly one tenant.

**Why CNPG (not the bundled chart StatefulSet, not Bitnami):** the bundled chart Postgres (ADR-080) is the single-replica convenience DB for self-hosters; the *hosted platform* needs operator-managed HA, automated failover and **native backup/PITR** — CNPG provides all three on official images. Bitnami is rejected (ADR-080 supply risk).

### 2. Backup = CNPG-native, off-cluster, keyed by tenant id

Each `Cluster` enables **continuous WAL archiving + scheduled base backups** to **off-cluster S3-compatible object storage** (Infomaniak Swift/S3 object storage — accessed via the S3 API, an OpenStack-specific endpoint configured *behind the CC-4 boundary*, credentials via ESO). The object-store prefix is `backups/<tenant-id>/` — the CC-6 id is the backup key, so a new tenant from the ADR-086 generator **inherits backups automatically** (the `Cluster` template carries the backup spec). WAL archiving makes RPO a function of the WAL flush/archive interval (minutes → aspirational ≤ 1h reachable), well inside the ≤ 24h initial target.

**Why CNPG-native backup over Velero:** Velero is namespace/volume-snapshot oriented (crash-consistent at best for a running DB) and would back up *all* namespace objects; CNPG backup is **Postgres-consistent**, supports **PITR**, and is scoped to exactly the per-tenant DB. Velero remains optional for non-DB namespace manifests, but the DB — the only stateful, irreplaceable data — is protected by CNPG. (The rest of a tenant is reconstructable from git via ADR-086.)

### 3. Restore = namespace-isolated, rehearsed, timed

Restore creates a **new `Cluster` with `bootstrap.recovery`** from the tenant's `backups/<tenant-id>/` prefix **into a scratch namespace** (`<tenant-id>-restore`). The restore can only read that tenant's prefix (object-store credentials + ESO policy are per-tenant, ADR-087) and writes only into the scratch namespace — satisfying the US-09 `@property` "a restore cannot cross tenants". The restore runbook is **rehearsed against Tenant Zero** and **timed** against the RTO target (KPI-3). A missed/failed backup raises an alert via the ADR-090 stack (backup age per tenant is a metric; staleness > RPO fires) — **no silent backup failure** (US-09 AC).

### Earned-Trust probe (CC-5/DR honesty — an untested backup is not a backup)

The restore rehearsal *is* the probe: a scheduled (per release) rehearsal restores Tenant Zero's latest backup into a scratch namespace, asserts the instance serves and a known row is present, records the elapsed time, and emits `dr.restore.rehearsed{tenant=lpw, rto_seconds=…, ok=true|false}`. A failed or over-RTO rehearsal alerts. Backups are thus *proven* recoverable continuously, not assumed. A standing `BackupStale` alert (`backup_age > RPO`) proves the schedule is actually running.

## Consequences

- **Positive**: physical per-tenant DB isolation (strongest CC-5 boundary); Postgres-consistent, off-cluster, PITR-capable backups inheriting automatically per new tenant; restore blast radius = one tenant, isolation enforced by per-tenant object-store prefix + namespace; RTO/RPO are measured (rehearsal timing + backup-age metric), not claimed.
- **Negative / cost**: one Postgres instance per tenant has higher baseline resource cost than a shared DB with schema-per-tenant — accepted as the price of isolation + clean restore, and bounded by tenant sizing (`plan` knob) and the ≥20/cluster density target; the object-store + its credentials are a per-tenant dependency (managed via ESO + the generator).
- **Standalone gate**: untouched — the bundled single-replica chart Postgres (ADR-080) is unchanged for self-hosters; CNPG is the hosted-platform production DB only.

## Alternatives considered

1. **Bundled chart StatefulSet Postgres per tenant** — rejected for the hosted platform: single-replica, no operator-managed HA/failover, no native PITR backup. Fine for self-host convenience (ADR-080), not for a fleet under an SLA.
2. **Schema-per-tenant or shared DB** — rejected by the locked CC-5 (DB-per-tenant); they widen restore blast radius and weaken isolation.
3. **Velero as the DB backup mechanism** — rejected for the DB (crash-consistent volume snapshots, whole-namespace scope); retained as an optional non-DB manifest backup only.
4. **Cross-region / active-active DB replication** — deferred (out of scope per feature-delta); WAL archive to off-cluster object storage is the initial durability posture.
