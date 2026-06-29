# ADR-093: Automated Fleet Upgrade — Tenant Zero Is a Permanent Canary on a `canaryVersion`, the Fleet Tracks a Separate `promotedVersion`; a Two-Step Git Bump (Canary → Promote) Rolls the Fleet; Expand-Only CI Guard Blocks Destructive Migrations *Before* Any Tenant Rolls; Rollback = git revert + helm rollback

**Status**: **ACCEPTED** (2026-06-29, Benjamin)
**Date**: 2026-06-29
**Feature**: epic-5306-productization-platform (ADO Epic #5306, story #5205 automated upgrades) — defines the **automated-upgrade flow**
**Decider**: Benjamin (product owner) + Titan (System Designer, PROPOSE)
**Relationship to prior work**: REUSES the epic-5305 rolling-update primitives wholesale — health probes (#5310), graceful drain (#5309), the Postgres advisory-lock migration coordination + **expand-only CI guard** (ADR-077/#5308), Redis backplane for `replicaCount>1` (ADR-075). Composes with ADR-086 (the version values live in the GitOps repo) and ADR-092 (tenants are generator records). Changes no app code; the guard already exists.

---

## Context

US-07 wants a single version bump to roll the whole fleet zero-downtime, staged through a canary, with destructive migrations blocked before any tenant is touched and a clean rollback. Tenant Zero is already the permanent canary (US-03). The shaping question is *how the canary/promote staging is expressed declaratively* so it stays GitOps-reconciled.

## Decision

**Two version values in the GitOps platform config drive a two-step promotion:**

- **`canaryVersion`** — Tenant Zero (`lpw`) tracks this directly (via its `chartVersionOverride`, ADR-086/092).
- **`promotedVersion`** — every other tenant tracks this (the generator default).

**Upgrade flow:**
1. **Bump `canaryVersion`** in git → only Tenant Zero rolls. The roll is zero-downtime via the epic-5305 primitives: startup/readiness probes gate the new pods, graceful drain bounds in-flight requests, the advisory-lock migration runs once, expand-only so old pods keep serving (D0c). The ADR-092 provision-probe + ADR-090 per-tenant error metric watch the canary.
2. **Promote**: once the canary is healthy (probe green, error ratio nominal for a soak window), **bump `promotedVersion` to match** → ArgoCD rolls every tenant; `argocd app list` shows the fleet converged on one revision (KPI-2: 100% tenants, ≤30 min, 0 dropped requests).

**Expand-only CI guard blocks destructive migrations before any tenant rolls** (ADR-077 guard, run in the existing GitHub Actions workflow per D0d): a non-expand-only migration fails CI → the version never reaches `canaryVersion`, so no tenant — not even the canary — rolls. The guard is the pre-flight gate, not a runtime check.

**Rollback** = `git revert` the version bump (canary or promoted) → ArgoCD reconciles back to the prior revision; because migrations are **expand-only/additive there is no schema rollback** — `helm rollback` (epic-5305) restores the prior image and the additive columns are simply unused by the old code. The fleet re-converges on the reverted revision.

## Consequences

- **Positive**: one (well, two-step) git bump rolls the fleet safely; the canary is a *permanent* real-production gate (Tenant Zero), not a synthetic one; a bad release is contained to one tenant before fleetwide promotion; destructive migrations cannot reach *any* tenant (CI pre-flight); rollback is git-native + additive-migration-safe; reuses 100% of epic-5305 — zero new app code.
- **Negative / cost**: the canary soak adds latency to a fleetwide roll (intentional safety tax); per-tenant version *pinning* (a tenant electing to stay behind) is out of scope (feature-delta) — all non-canary tenants track `promotedVersion`; a tenant needing a hold uses `chartVersionOverride` as an escape hatch (documented, discouraged).
- **Standalone gate**: untouched — self-hosters upgrade their own chart on their own cadence; this governs only the hosted fleet's promotion values.

### Earned-Trust note

The expand-only guard is itself an Earned-Trust probe on the migration substrate (it refuses to let a destructive change ship), and the canary is the empirical proof that the *new* version actually serves on real production data before the fleet trusts it — the fleet never assumes a release is safe; Tenant Zero demonstrates it.

## Alternatives considered

1. **Single `appVersion` for all tenants (no canary/promote split)** — rejected: a bad version hits the whole fleet at once (US-07 explicitly forbids); the two-value split is the minimal declarative canary.
2. **ArgoCD progressive-sync / rollout waves across tenants** — heavier; deferred. The Tenant-Zero-canary-then-promote split meets KPI-2 without per-tenant wave orchestration; revisit if the fleet grows enough to want staged % rollouts beyond a single canary.
3. **Argo Rollouts (in-tenant blue-green/canary traffic split)** — out of scope (epic-5305 Band D, feature-delta); this ADR stages *across* tenants, not *within* a tenant.
4. **Runtime destructive-migration check instead of CI guard** — rejected: too late (a tenant would already be mid-roll); the guard must be pre-flight (ADR-077).
