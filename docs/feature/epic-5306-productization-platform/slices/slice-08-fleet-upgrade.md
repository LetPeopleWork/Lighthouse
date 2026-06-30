# Slice 08 — fleet-upgrade

> **STATUS (2026-06-30): SUBSTRATE ONLY — superseded by the #5205 rescope.** This slice DELIVERED the
> staged-upgrade *mechanism* (matrix `promotedVersion` + per-record `chartVersion` canary override,
> expand-only guard, git-revert rollback) and proved it live. It did NOT deliver the merge-only
> automation ADO #5205 actually asks for (Renovate, auto-canary, one-click promote, sync-wave migration
> ordering, post-sync smoke-test + alert, broken-image rollback drill). #5205 was reopened (Active) and
> RESCOPED into three thin slices — **08a** renovate-merge-only-release, **08b**
> ordered-upgrade-smoketest-alert, **08c** broken-image-rollback-drill — see `slice-08a/08b/08c-*.md`
> and `discuss/slice-08-rescope-*`. This brief is retained as the substrate record those slices build on.

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
