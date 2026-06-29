# Slice 05 — wildcard-routing

- **ADO story**: #5202 (Wildcard DNS + subdomain routing)
- **job_id**: `job-saas-operator-route-tenant-by-subdomain`
- **Band**: Routing generalization

## Learning hypothesis

> A wildcard DNS record (`*.lighthouse.letpeople.work`) + ingress host routing + automatic TLS
> (cert-manager, wildcard or per-host) means **any new subdomain serves the right tenant with a
> trusted cert and zero manual DNS/cert steps**. If true, routing is solved once and every future
> tenant inherits it for free — the precondition for one-action provisioning.

## Elevator Pitch

- **Before**: Tenant Zero has a single hand-made host route; a new tenant would need a new DNS record + cert by hand.
- **After**: Benjamin points a throwaway host `demo.lighthouse.letpeople.work` at the ingress → sees it resolve and serve with a valid auto-issued cert, no DNS edit per host.
- **Decision enabled**: "Can a brand-new tenant get a working HTTPS URL automatically?" — yes.

## In / Out

- **IN**: Wildcard DNS record; ingress host-based routing keyed on tenant identifier; automatic TLS issuance; verified with a temporary demo host pointing back at Tenant Zero or a stub.
- **OUT**: The tenant *record* that drives the host (slice-07); cross-tenant network policy hardening.

## Dogfood moment

Tenant Zero's route is migrated from the single-host slice-03 form onto the wildcard mechanism — same URL, now backed by the generalized routing.

## Thin end-to-end path

Add wildcard DNS → configure ingress to route `{tenant}.base` → namespace by host → cert-manager issues TLS automatically → hit a never-before-seen subdomain → it serves with a valid cert.

## Done = observable

- A subdomain that was never manually configured resolves and serves over HTTPS with a trusted cert.
- Host → namespace mapping is derived from the tenant identifier (single source — see shared-artifact registry).
- `lpw.lighthouse.letpeople.work` still serves (no regression to Tenant Zero).

## Depends on

- slice-03 (a reachable tenant to generalize from).
