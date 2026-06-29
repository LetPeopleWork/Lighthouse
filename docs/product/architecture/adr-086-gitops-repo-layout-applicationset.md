# ADR-086: GitOps Repo Layout — a Tenant IS a Declarative Record Fanned by an ArgoCD ApplicationSet (Git Generator) in a Mono-Repo; Platform Components and Tenants Are Separated by Directory; No Bespoke Tenant Controller

**Status**: **ACCEPTED** (2026-06-29, Benjamin)
**Date**: 2026-06-29
**Feature**: epic-5306-productization-platform (ADO Epic #5306, stories #5201 GitOps, #5207 provisioning) — converges cross-cutting decision **CC-2**
**Decider**: Benjamin (product owner) + Titan (System Designer, PROPOSE)
**Relationship to prior work**: Builds ON the shipped #5199 chart (ADR-080..085) — the chart is the per-tenant workload the layout deploys; changes no chart template, no app code.

---

## Context

CC-2 asks: *what is a tenant as a declarative artifact, and how are platform components separated from tenants in the GitOps repo?* The answer is the carrier for the locked tenancy model (CC-1 namespace-per-tenant) and the single tenant identifier (CC-6). Three shapes were weighed.

## Decision

**A tenant IS a single declarative record** — `tenants/<tenant-id>/tenant.yaml` carrying the CC-6 identifier and a small set of knobs (`{ id, subdomain, plan/sizing, chartVersionOverride? }`). An **ArgoCD ApplicationSet with a Git-files generator** reads every `tenants/*/tenant.yaml` and fans each into one per-tenant **app-of-apps Application** that renders the tenant's full stack (namespace + quota + NetworkPolicy + CNPG `Cluster` (ADR-091) + `ExternalSecret` (ADR-087) + the #5199 chart release + route). All derived names key off the single `id`.

**Repo structure (mono-repo, directory-separated):**

```
bootstrap/            # ArgoCD installed + self-managed (app-of-apps root lives here)
platform/             # cluster-singletons: cert-manager, external-dns, ingress-nginx,
                      #   ESO, OpenBao, CNPG operator, kube-prometheus-stack, velero/backup
                      #   — each an Application under the platform app-of-apps
tenants/
  _generator/         # the ApplicationSet (Git-files generator over tenants/*/tenant.yaml)
  lpw/tenant.yaml     # Tenant Zero as an ordinary record (no production special-casing)
  acme/tenant.yaml
```

**Root = app-of-apps** (`bootstrap/root-app`) → syncs `platform/` (a platform app-of-apps) and `tenants/_generator` (the ApplicationSet). ArgoCD manages itself (the root app points at `bootstrap/`). **Mono-repo**, not a config-repo split — one reviewable change-control surface for a small operator team; the split is recorded as a future option if tenant churn or access-control demands it.

**Rejected: a `Tenant` Custom Resource + bespoke controller** — it is net-new application/controller code (forbidden in this infra-only scope) with ongoing maintenance, reconcile-loop bugs, and an upgrade burden, for power an ApplicationSet already provides at the ≥20-tenant density target.

| Quality attribute | Weight | (A) ApplicationSet Git generator ✅ | (B) Tenant CR + controller | (C) Per-tenant Application dir (no generator) |
|---|---|---|---|---|
| No bespoke code (infra-only constraint) | Highest | **Off-the-shelf ArgoCD** — zero code | **Violates** — net-new controller | Off-the-shelf, but verbose |
| One-record onboarding (North-Star KPI 1) | Highest | **One `tenant.yaml`** → full stack | One CR (also one file) | Whole hand-written dir per tenant |
| PR-time review + uniqueness gate | High | Record diff is reviewable; CI checks id uniqueness pre-merge | CR diff reviewable; admission webhook = more code | Reviewable but large diffs |
| Drift self-heal / prune-on-remove | High | Native (`syncPolicy.automated.prune`) | Native (controller owns finalizers) | Native, manual wiring |
| Operability at ≥20 tenants | High | Strong — generator scales linearly | Strong but controller is a new SPOF to run | Weak — copy-paste sprawl |
| Power (per-tenant conditional logic) | Low (not needed) | Generator + Helm values suffice | Highest (arbitrary logic) | Low |

## Consequences

- **Positive**: onboarding = one committed record (KPI 1); ArgoCD does all reconciliation, prune, self-heal natively; zero bespoke controller to maintain (honors the infra-only + no-app-code constraint); Tenant Zero and every customer are produced by the *same* generator (no production special-casing, slice-07 dogfood); the `tenant.yaml` record is the single CC-6 identity carrier.
- **Negative / cost**: per-tenant conditional logic is bounded by what Git-generator + Helm values express (sufficient here; a Tenant CR would be needed only for arbitrary per-tenant logic we do not have). The ApplicationSet controller becomes a control-plane dependency (it ships with ArgoCD — no extra install).
- **Standalone gate**: untouched — this governs only the hosted platform repo; the chart and standalone image are unchanged.

## Alternatives considered

1. **Tenant CR + controller** — rejected (bespoke code, infra-only scope, maintenance/SPOF).
2. **Per-tenant directory of raw Application manifests, no generator** — rejected (copy-paste sprawl, no DRY, weak at density).
3. **Config-repo split (platform repo + tenants repo)** — deferred; mono-repo is simpler for a small team; revisit if access-control or churn demands.
