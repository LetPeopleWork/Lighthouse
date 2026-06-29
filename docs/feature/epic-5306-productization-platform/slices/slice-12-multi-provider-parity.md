# Slice 12 — multi-provider-parity  (DEFERRED / on-demand)

- **ADO story**: #5320 (Multi-provider cluster substrate) — Part B
- **job_id**: `job-saas-operator-provision-substrate`
- **Band**: Vendor-neutrality proof (deferred until a second provider is actually needed)

## Learning hypothesis

> The same OpenTofu substrate module stands up a conformant cluster on a **second provider** (AWS
> EKS) with only provider-boundary parameters changed, and the platform layer (ArgoCD, tenants,
> routing, secrets) installs unchanged on top. If true, vendor-neutrality is demonstrated, not just
> claimed — the substrate abstraction does not leak.

## Elevator Pitch

- **Before**: The substrate is proven on one provider (Infomaniak OpenStack); "vendor-neutral" is an assertion.
- **After**: Benjamin runs `tofu apply` with a second provider profile (e.g. AWS EKS, or any OpenStack cloud) → sees a conformant cluster, then installs the unchanged platform layer onto it.
- **Decision enabled**: "Is our hosting genuinely portable across providers?" — proven on two.

## In / Out

- **IN**: A second provider profile (AWS EKS) for the substrate module; the platform layer installed unchanged on it; a documented list of the *only* provider-specific parameters.
- **OUT**: Running production tenants on the second provider; multi-cloud active/active; provider cost optimization.

## Dogfood moment

Optional — a demo tenant on the second provider proves the path; production stays on the primary provider until there's a real reason to move.

## Why deferred

Vendor-neutrality is a *constraint* enforced from slice-01 (the platform layer only ever touches
conformant-cluster primitives). Actually standing up a second provider is only worth the cost when a
real need (a customer requirement, a cost/region driver) appears. Pulling it forward would burn
effort proving portability nobody is asking for yet. **Pull this slice in on demand.**

## Done = observable

- The same substrate module produces a conformant cluster on AWS EKS.
- The platform layer installs unchanged on the second provider.
- The provider-specific parameter list is documented and small.

## Depends on

- slice-01 (the substrate module to generalize).
