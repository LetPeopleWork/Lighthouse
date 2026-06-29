# Slice 10 — per-tenant-backups

- **ADO story**: #5208 (Backup & disaster recovery) — Part A
- **job_id**: `job-saas-operator-recover-from-disaster`
- **Band**: Data durability

## Learning hypothesis

> Scheduled per-tenant Postgres backups to **off-cluster** storage, with backup success itself
> monitored, give every tenant a recovery point within a stated RPO — automatically, for every
> tenant the generator produces. If true, we have a real (not hoped-for) data-protection posture and
> a measurable RPO.

## Elevator Pitch

- **Before**: There is no automated per-tenant backup; data loss would be unrecoverable.
- **After**: Benjamin runs `kubectl get cronjob,job -n tenant-lpw` (or the backup tool's status) → sees Tenant Zero's last backup timestamped within the RPO window, stored off-cluster.
- **Decision enabled**: "What is our recovery point per tenant?" — a concrete, monitored RPO.

## In / Out

- **IN**: A scheduled backup mechanism per tenant DB to off-cluster object storage; backup-success monitoring/alerting (wired into slice-09); RPO target stated and met for Tenant Zero + a demo tenant.
- **OUT**: The restore (slice-11); cross-region replication; PITR (state target/RPO, defer if heavy).

## Dogfood moment

Tenant Zero's real production database is the first thing backed up — we protect our own data first.

## Thin end-to-end path

Add a backup schedule to the tenant generator output → it runs per tenant → artifacts land in off-cluster storage → backup success is monitored → a missed backup alerts.

## Done = observable

- Every tenant has a backup artifact off-cluster, timestamped within the RPO.
- A failed/missed backup raises an alert (not silent).
- New tenants from the generator inherit backups automatically (no per-tenant setup).

## Depends on

- slice-07 (tenants to back up), slice-09 (monitoring to alert on backup failure).
