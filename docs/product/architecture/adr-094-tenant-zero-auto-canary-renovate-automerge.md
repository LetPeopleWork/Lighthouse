# ADR-094: Tenant-Zero Auto-Canary = Renovate Auto-Merges a Tenant-Zero-Scoped `chartVersion` Override PR (Everything Stays in Git); the Fleet `promotedVersion` PR Is Never Auto-Merged

**Status**: **ACCEPTED** (implemented + live-proven, epic-5306 slice-08; finalized 2026-07-03)
**Date**: 2026-06-30
**Feature**: epic-5306-productization-platform (ADO Epic #5306, story #5205 RESCOPE slice-08a) — resolves headline open question **O-08-1**
**Decider**: Benjamin (product owner) + Titan (System Designer)
**Relationship to prior work**: Builds ON the LIVE slice-08 substrate (matrix ApplicationSet folding each tenant record with one fleet `promotedVersion`; a record's own `chartVersion` is its canary override — `gitops/tenants/_generator/applicationset.yaml`). Composes with ADR-093 (canary→promote staging), ADR-086 (versions live in git), ADR-087 (ESO/OpenBao). Pairs with ADR-097 (the Renovate watch scope/automerge policy this decision is expressed through).

---

## Context

US-08a-2 requires that a newly published release land on **Tenant Zero (`lpw`) first, with zero operator action**, while the rest of the fleet waits for the operator's one-click merge (US-08a-3). The operator's stated *intent* is "Tenant Zero tracks latest, the rest stay pinned." That intent must be turned into a concrete mechanism without adding a new controller and without breaking the substrate's git-auditable / git-revertable rollback story.

The slice-08 DELIVER step deliberately *dropped* lpw's per-record `chartVersion` so Tenant Zero would render unchanged and inherit `promotedVersion`. This ADR re-introduces a per-record `chartVersion` on lpw — now as the **permanent, Renovate-tracked, always-ahead canary anchor** — which is the faithful expression of the "Tenant Zero first" promise.

## Options considered

| # | Option | Trade-off | Verdict |
|---|--------|-----------|---------|
| **(a)** | **Renovate auto-merges a Tenant-Zero-scoped PR** that bumps `gitops/tenants/lpw/tenant.yaml`'s `chartVersion`, while opening a **non-automerge** PR for the fleet `promotedVersion` | Everything stays in git (auditable, revertable, "what's running" = `git show`); reuses the substrate's existing `chartVersion` override verbatim; no new controller; zero operator action on TZ (Renovate merges once required checks pass); the fleet PR remains the one-click human gate. Cost: re-adds a per-record override on lpw (the thing slice-08 dropped) and depends on the hosted Mend App + branch protection. | **CHOSEN** |
| (b) | A literal mutable `latest`/floating tag the lpw record tracks | **GOTCHA (rejected)**: a mutable tag is **registry-side**, but ArgoCD reconciles **git**, not the registry — a retag does not trigger a sync; it would need argocd-image-updater to notice a new digest. Worse, it destroys rollback clarity: "what is running" and "revert to previous" are no longer answerable from git history. Also our version knob is the **Helm chart `targetRevision`**, not the image tag, so a floating image tag does not even move the chart. | Rejected |
| (c) | argocd-image-updater scoped to Tenant Zero | A new controller that watches the **registry** and writes back to git or overrides in-cluster — solves no bottleneck Renovate does not already solve, and targets container *images* while our knob is the *chart version* (image tag derives from `Chart.appVersion`, ADR-083), so it is a poor fit and adds an operational surface. | Rejected |

## Decision

**Tenant Zero auto-canaries via Renovate auto-merge of a Tenant-Zero-scoped `chartVersion`-override PR.**

1. `gitops/tenants/lpw/tenant.yaml` carries an **explicit `chartVersion: <current>`** (the canary anchor), marked for a Renovate custom manager. The substrate's `targetRevision` `hasKey` logic (unchanged) makes lpw run that override ahead of the fleet.
2. Renovate (ADR-097) runs a **custom manager** matching that `chartVersion` against the Helm `lighthouse` chart datasource (`https://docs.lighthouse.letpeople.work/charts`), with a package rule **`automerge: true`** scoped to that file only. On a new release Renovate raises the PR and **merges it itself** once the `validate-tenants` required check is green — **no operator action**. ArgoCD then reconciles git → `tenant-lpw` rolls to the new version, ahead of the fleet.
3. In the **same** scan Renovate raises a **separate, non-automerge** PR bumping the fleet `promotedVersion` in `applicationset.yaml`. That PR is the operator's one-click promote gate (US-08a-3) — it is never auto-merged; nothing auto-promotes the fleet off a failed canary (guardrail: canary-auto-promoted = 0).

## Consequences

- **Positive**: the "Tenant Zero first, hands-off" promise becomes a property of git + Renovate, not operator memory; the canary is always exactly one version ahead until a human merges; rollback stays `git revert` (ADR-093) because the canary move is a real commit; no new controller, no registry-mutable tag; reuses the substrate's `chartVersion` override and the slice-07 `validate-tenants` check as the automerge gate.
- **Negative / cost**: re-introduces a per-record `chartVersion` on lpw (slice-08 had dropped it) — lpw is no longer render-identical to a bare-inherit tenant, by design; depends on the hosted Mend App (ADR-097) and on **branch protection requiring the `validate-tenants` status check** so automerge cannot merge a record that fails validation (operator one-time setup); a bad release auto-lands on real LPW production first (that is the *point* of a permanent canary — pain is felt on our own tenant before any customer, US-08a-2 example 3).
- **Standalone gate**: untouched — this is private-repo GitOps + a hosted bot; the public chart is byte-unchanged.

### Earned-Trust note

The canary is the empirical proof that the new version actually serves on real production data before the fleet trusts it — the fleet never *assumes* a release is safe; Tenant Zero *demonstrates* it, and the post-sync smoke-test (ADR-096) is the probe that turns that demonstration into a signal. The auto-merge gate (required `validate-tenants` check) is itself a probe that the record is well-formed before it can reach ArgoCD.

## Open questions for DELIVER

- Confirm branch protection on `main` requires the `validate-tenants` check (else Renovate automerge has nothing to gate on).
- Renovate scan interval vs KPI-2 (≤30 min publish→canary): the Mend App default is ~hourly; a tighter `schedule` may be needed — tune against the first measured roll (ADR-097).
