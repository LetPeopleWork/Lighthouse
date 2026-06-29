# Slice 01 — substrate-up

- **ADO story**: #5320 (Multi-provider cluster substrate, OpenTofu)
- **job_id**: `job-saas-operator-provision-substrate`
- **Band**: Foundation (gates everything)

## Learning hypothesis

> Our OpenTofu substrate module produces a *conformant* Kubernetes cluster (nodes + CNI + ingress
> controller + storage class) that the **already-shipped #5199 chart installs onto unchanged** — and
> `tofu destroy` removes it cleanly. If true, the substrate is reproducible code, not a hand-built
> snowflake, and vendor-neutrality is real because the platform layer only ever touches
> conformant-cluster primitives.

## Elevator Pitch

- **Before**: The hosting cluster is hand-clicked in a provider console; nobody dares rebuild it.
- **After**: Benjamin runs `tofu apply` → sees a ready cluster (`kubectl get nodes` lists the node pool, ingress controller pod Running) reproducible from code.
- **Decision enabled**: "Is our substrate portable and rebuildable?" — proven by `tofu destroy` + re-apply.

## In / Out

- **IN**: One provider (Infomaniak Public Cloud, OpenStack provider), one cluster, node pool, CNI, ingress-nginx, a default storageClass; `tofu apply` / `tofu destroy`.
- **OUT**: Multi-provider parity (slice-11), autoscaling, ArgoCD (slice-02), any tenant.

## Dogfood moment

This is the cluster that will host Tenant Zero (LPW production) — built first, as code.

## Thin end-to-end path

`tofu apply` → cluster + ingress controller up → `helm install` the shipped chart with default values into a scratch namespace → it reaches Running/Ready → uninstall → `tofu destroy`.

## Done = observable

- `kubectl get nodes` shows the declared node pool Ready.
- The shipped #5199 chart installs onto it with **zero chart changes** (standalone gate intact).
- `tofu destroy` leaves no orphaned cloud resources.

## Depends on

- Shipped #5199 chart (ADR-080..085). Nothing else.
