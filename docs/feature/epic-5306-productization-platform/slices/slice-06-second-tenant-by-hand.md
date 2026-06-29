# Slice 06 — second-tenant-by-hand

- **ADO story**: #5207 (Tenant provisioning) — *de-risk slice, before automation*
- **job_id**: `job-saas-operator-onboard-tenant`
- **Band**: Tenancy-model validation

## Learning hypothesis

> The Tenant Zero pattern (namespace + DB + secret + route + app) **generalizes to a second,
> independent tenant** that is fully isolated — by copying and parameterising the same artifacts by
> hand. If true, the tenancy model holds (two tenants coexist with no cross-tenant bleed) and we have
> proven exactly what the automation in slice-07 must template. This de-risks the central tenancy
> decision *before* investing in the generator.

## Elevator Pitch

- **Before**: We have one bespoke tenant (zero); we don't know if the pattern generalizes or leaks.
- **After**: Benjamin hand-provisions `acme.lighthouse.letpeople.work` → sees Acme serving its own isolated instance, unable to read Tenant Zero's data.
- **Decision enabled**: "Does our tenancy model isolate two tenants?" — proven by hand before automating it.

## In / Out

- **IN**: A second tenant "Acme" provisioned by hand from the tenant-zero artifacts (own namespace, own DB, own store-sourced secret, own wildcard subdomain route); an explicit isolation check.
- **OUT**: Any automation/templating (that is slice-07); customer real data (Acme is a demo tenant).

## Dogfood moment

Tenant Zero stays the production canary; Acme is the first *other* tenant, proving isolation against real production next door.

## Thin end-to-end path

Copy the `tenant-lpw` artifacts → parameterise to `tenant-acme` → commit → ArgoCD syncs → Acme serves at its subdomain → attempt to read Tenant Zero's DB from Acme → fails (isolation proven).

## Done = observable

- `acme.lighthouse.letpeople.work` serves an isolated Acme instance.
- Acme cannot reach Tenant Zero's database or secrets (isolation check passes).
- The diff between `tenant-lpw` and `tenant-acme` artifacts = exactly the parameters the slice-07 generator must template.

## Depends on

- slice-04 (managed secrets), slice-05 (wildcard routing).
