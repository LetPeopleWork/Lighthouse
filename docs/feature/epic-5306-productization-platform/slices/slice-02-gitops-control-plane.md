# Slice 02 — gitops-control-plane

- **ADO story**: #5201 (GitOps with ArgoCD)
- **job_id**: `job-saas-operator-declare-platform-as-code`
- **Band**: Foundation

## Learning hypothesis

> ArgoCD, installed via an app-of-apps root pointed at the platform git repo, makes the cluster
> *become whatever the repo says* — a merged PR changes the cluster and manual drift is self-healed.
> If true, every later capability (tenants, upgrades) is delivered by committing to git, and the
> repo layout we choose here is the carrier for the tenancy model.

## Elevator Pitch

- **Before**: The cluster is changed by hand with `kubectl apply`; nobody is sure it matches git.
- **After**: Benjamin merges a PR adding a platform component → runs `argocd app list` → sees the root app and child Synced/Healthy, the component live without a manual apply.
- **Decision enabled**: "Do we change the platform by PR?" — yes; and the repo layout that will carry tenants is chosen and exercised.

## In / Out

- **IN**: ArgoCD installed (itself reconciled), app-of-apps root, one platform component (e.g. cert-manager) reconciled from git; drift self-heal demonstrated.
- **OUT**: Tenant ApplicationSet/generator (slice-07), secrets backend (slice-04), wildcard routing (slice-05).

## Dogfood moment

The control plane that will manage LPW Tenant Zero and every customer tenant.

## Thin end-to-end path

`kubectl apply` ArgoCD bootstrap → root app points at repo → commit a component manifest → ArgoCD syncs it → delete the live resource by hand → ArgoCD recreates it (self-heal proof).

## Done = observable

- `argocd app list` shows the root + child apps Synced/Healthy.
- A hand-deleted managed resource is auto-restored (drift self-heal).
- The repo layout documents where a *tenant* will live (a directory / generator entry) — names the tenancy-model carrier for DESIGN.

## Depends on

- slice-01 (a cluster to install ArgoCD onto).
