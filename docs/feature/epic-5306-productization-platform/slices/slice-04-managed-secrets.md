# Slice 04 — managed-secrets

- **ADO story**: #5203 (Secrets management)
- **job_id**: `job-saas-operator-isolate-tenant-secrets`
- **Band**: Isolation foundation

## Learning hypothesis

> A managed secret store (External Secrets Operator + a secret-manager backend — strategy named for
> DESIGN) can source Tenant Zero's secrets (DB creds, OIDC, license) into its namespace from a git
> *reference*, with **no plaintext secret ever committed**. If true, GitOps and secret-safety coexist,
> and per-tenant secret isolation + rotation become structural rather than manual.

## Elevator Pitch

- **Before**: Tenant Zero's secret is a hand-made `kubectl`-applied Secret outside GitOps; rotation is manual and it drifts.
- **After**: Benjamin commits an `ExternalSecret` reference → sees `kubectl get secret -n tenant-lpw` materialise from the store, with the git repo containing only the reference.
- **Decision enabled**: "Can we keep all state in git without leaking secrets?" — yes; rotation = update the store.

## In / Out

- **IN**: Secret store + operator installed (via ArgoCD); Tenant Zero's hand-made secret replaced by a store-sourced one; a rotation demonstrated.
- **OUT**: Per-tenant secret templating for arbitrary tenants (folds into slice-07 provisioning); Vault HA topology.

## Dogfood moment

Tenant Zero's real DB/OIDC credentials move into the managed store and out of any file.

## Thin end-to-end path

Install the secrets operator via ArgoCD → put Tenant Zero's secret values in the store → commit an `ExternalSecret` → operator materialises the k8s Secret → app picks it up → rotate the value in the store → app sees the new value.

## Done = observable

- `git grep` of the GitOps repo finds **zero** plaintext secret values.
- `kubectl get secret -n tenant-lpw` is materialised from the store.
- A rotation in the store propagates to the tenant without editing git.

## Depends on

- slice-03 (Tenant Zero exists with a hand-made secret to replace).
