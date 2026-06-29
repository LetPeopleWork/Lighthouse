# Slice 11 — restore-rehearsal

- **ADO story**: #5208 (Backup & disaster recovery) — Part B
- **job_id**: `job-saas-operator-recover-from-disaster`
- **Band**: Data durability (the slice that makes backups real)

## Learning hypothesis

> A documented restore runbook can bring a single tenant (and, in principle, the whole platform)
> back from the slice-10 backups **within a stated RTO**, verified against Tenant Zero — proving the
> backup is restorable, not just present. An untested backup is no backup; this slice is what turns
> RPO into an actual recoverability guarantee.

## Elevator Pitch

- **Before**: Backups exist but have never been restored; recoverability is assumed.
- **After**: Benjamin runs the restore runbook against a scratch namespace → sees Tenant Zero's data restored from backup and the instance serving, timed under the RTO target.
- **Decision enabled**: "Can we actually recover a tenant, and how fast?" — a rehearsed, timed RTO.

## In / Out

- **IN**: A restore runbook for one tenant (restore into a scratch/replacement namespace), rehearsed against Tenant Zero and a demo tenant; RTO measured and stated; an isolation check that a restore cannot land in the wrong tenant.
- **OUT**: Full-cluster DR failover automation (runbook documents it; automation deferred); automated periodic restore-testing (note as follow-up).

## Dogfood moment

We restore Tenant Zero — our own production — from backup in a rehearsal, so the runbook is proven before any customer relies on it.

## Thin end-to-end path

Take a Tenant Zero backup (slice-10) → follow the restore runbook into a scratch namespace → verify data integrity + the instance serves → time it → confirm the restore targeted only the intended tenant.

## Done = observable

- Tenant Zero is restored from backup and serves, within the stated RTO.
- The restore is verified (data integrity checked), not assumed.
- A restore cannot write into another tenant's namespace/DB (isolation check passes).

## Depends on

- slice-10 (backups to restore from).
