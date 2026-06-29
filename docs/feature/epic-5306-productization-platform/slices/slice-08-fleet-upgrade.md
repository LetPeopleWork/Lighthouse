# Slice 08 — fleet-upgrade

- **ADO story**: #5205 (Automated upgrades)
- **job_id**: `job-saas-operator-upgrade-all-tenants-safely`
- **Band**: Fleet operations

## Learning hypothesis

> Bumping one chart appVersion/image tag in git, with a **canary on Tenant Zero first** then
> promotion, rolls the new version across every tenant as a zero-downtime rolling update with
> expand-only migrations — no maintenance window, no tenant left behind, rollback = git revert. If
> true, a small team can keep an arbitrary number of tenants current and safe.

## Elevator Pitch

- **Before**: Upgrading tenants is manual and per-tenant; a bad version could hit everyone at once.
- **After**: Benjamin bumps `appVersion` in git → sees Tenant Zero canary the new version (zero dropped requests, health holds), then `argocd app list` shows the whole fleet Synced on the new revision.
- **Decision enabled**: "Can we ship a release to all tenants safely during the working day?" — yes.

## In / Out

- **IN**: Version bump propagated by ArgoCD; staged rollout (Tenant Zero canary → fleet promote); reuse of epic-5305 rolling-update + drain + expand-only-migration guard; git-revert + `helm rollback` rollback path.
- **OUT**: Blue-green/canary *traffic* splitting within a tenant (epic-5305 Band D); per-tenant version pinning policy.

## Dogfood moment

Tenant Zero is the permanent canary — LPW production takes every release first, so we feel any upgrade pain before a customer does.

## Thin end-to-end path

Bump appVersion in git → ArgoCD rolls Tenant Zero → verify zero-downtime + health → promote to fleet → all tenants converge → (rehearse) git revert + helm rollback restores prior version.

## Done = observable

- Tenant Zero upgrades with zero dropped requests (probes + drain hold).
- `argocd app list` shows every tenant Synced on the new revision after promotion.
- A destructive (non-expand-only) migration is blocked in CI before reaching any tenant.
- `git revert` rolls the fleet back (additive migrations need no schema rollback).

## Depends on

- slice-07 (a fleet of generated tenants to upgrade), epic-5305 upgrade primitives.
