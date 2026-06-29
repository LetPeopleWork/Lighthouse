# ADR-092: Tenant Provisioning Data-Flow — One `tenant.yaml` Record Fans (via the ADR-086 ApplicationSet) into a Sync-Wave-Ordered App-of-Apps; All Resource Names Derive from the Single CC-6 id; Uniqueness Is Gated at PR Time; Removal Prunes Everything

**Status**: **ACCEPTED** (2026-06-29, Benjamin) — O-2: reserved-subdomain guard + scoped external-dns so tenant records coexist with the existing docs.lighthouse.letpeople.work Pages record
**Date**: 2026-06-29
**Feature**: epic-5306-productization-platform (ADO Epic #5306, story #5207 provisioning) — defines the **provisioning data-flow** for the North-Star KPI (one record → reachable ≤ 10 min)
**Decider**: Benjamin (product owner) + Titan (System Designer, PROPOSE)
**Relationship to prior work**: The concrete realisation of ADR-086 (the ApplicationSet + tenant record), composing ADR-087 (ESO secret), ADR-088 (substrate contract), ADR-091 (CNPG DB), the #5199 chart, cert-manager + external-dns. No bespoke controller (ADR-086); ordering is via ArgoCD sync-waves.

---

## Context

US-06 wants one declarative action to expand into a complete, isolated, reachable tenant — and one removal to prune it with no orphans. The pieces (namespace, quota, NetworkPolicy, DB, secret, chart release, route, DNS, cert) have ordering dependencies (e.g. the namespace must exist before the DB; the DB + secret before the app; the app before its ingress is useful). CC-6 demands every derived name flow from one identifier so nothing diverges or orphans.

## Decision

**The single `tenants/<id>/tenant.yaml` record `{ id, subdomain, plan, chartVersionOverride? }` is fanned by the ADR-086 ApplicationSet into one per-tenant app-of-apps whose children carry ArgoCD sync-wave annotations, applied in order:**

| Wave | Resources (rendered, namespaced to the tenant) | Derives from id |
|---|---|---|
| **0 — isolation shell** | `Namespace tenant-<id>` + `ResourceQuota` (from `plan`) + `NetworkPolicy` (default-deny + allow-ingress + allow-DNS) | namespace name = `tenant-<id>` |
| **1 — data + secrets** | CNPG `Cluster <id>-db` (ADR-091) + `ExternalSecret` → OpenBao `secret/tenants/<id>/*` (ADR-087) + backup spec to `backups/<id>/` | DB name, store path, backup prefix |
| **2 — workload + route** | #5199 chart release (`externalDatabase.*` → the CNPG service; secret from wave 1) + `Ingress` host `<subdomain>.lighthouse.letpeople.work` + cert-manager `Certificate` (or wildcard, ADR uses slice-05 wildcard) + external-dns annotation | ingress host, cert SAN, OIDC callback |

**All names key off the one `id`** (CC-6 single source — Shared Artifacts registry). **Uniqueness is gated at PR time**: a CI check rejects a record whose `id`/`subdomain`/DB name collides with an existing `tenants/*/tenant.yaml` — the duplicate is refused *before* it reaches the cluster (US-06 AC), never live.

**Reserved subdomains (O-2 resolution).** The base zone `lighthouse.letpeople.work` already carries non-tenant hosts — notably **`docs.lighthouse.letpeople.work`**, an explicit GitHub-Pages record (the public docs site, ADR-083). The same PR-time check therefore also rejects any `subdomain` in a **reserved-name list**: `docs`, `www`, `api`, `app`, `mcp`, `admin`, `argocd`, `grafana`, `auth`, `status` (extend as infra hosts are added). This prevents a tenant from claiming an infrastructure host. DNS resolution itself is safe by precedence — a **specific** record (`docs` → Pages) always wins over the **wildcard** (`*.lighthouse.letpeople.work` → cluster ingress LB), so the docs site keeps resolving to GitHub Pages unchanged while tenant hosts match the wildcard. **external-dns is scoped, not zone-wide**: it runs with a TXT-registry `--txt-owner-id` and a domain filter so it manages ONLY records it created for tenant Ingresses and **never deletes or overwrites the manually-managed `docs` (or other reserved) record**. The wildcard `*` record and the explicit `docs` record coexist in the same zone.

**De-provision = remove the record** → the ApplicationSet drops the Application → `syncPolicy.automated.prune` cascades the namespace and all namespaced children; the CNPG `Cluster` deletion removes the DB + PVC; the external-dns annotation removal retracts the DNS record; the ESO `ExternalSecret` deletion drops the materialised Secret (the *value* in OpenBao is retained or purged per a documented retention step). Result: no orphans (US-06 AC). The object-store backup prefix is retained per the DR retention policy (intentional — a deleted tenant's backup outlives the namespace until retention expires).

**Tenant Zero and Acme are re-expressed as generator records** (slice-07 dogfood) — no production special-casing; the same flow produces every tenant.

## Consequences

- **Positive**: onboarding = one reviewable commit (North-Star KPI 1, ≤10 min); ordering is declarative (sync-waves), no imperative script and no bespoke controller; every name traceable to one id (no cross-tenant bleed / orphans by construction); PR-time uniqueness keeps collisions off the live cluster; teardown is prune-driven and complete.
- **Negative / cost**: sync-wave ordering must be correct (a wave-2 app starting before its wave-1 DB would crash-loop until the DB is ready — mitigated by the chart's fail-fast + epic-5305 startup probe, which simply waits/retries rather than half-provisioning); backup retention deliberately outlives the namespace (documented, not an orphan).
- **Standalone gate**: untouched — provisioning is a hosted-platform-only flow over the unchanged chart.

### Earned-Trust probe (provisioning honesty)

After a tenant syncs Healthy, a `provision.probe` (synthetic) hits `https://<subdomain>.lighthouse.letpeople.work` and asserts a 200 over a valid cert, and asserts the tenant's pod **cannot** reach another tenant's namespace/DB (the CC-1 NetworkPolicy + ADR-087 secret isolation, exercised live). This is the slice-06 "validate the model by hand" automated for slice-07; it emits the onboarding-lead-time KPI timestamp (PR-merge → 200). Failure flags the tenant unhealthy rather than presenting a half-isolated tenant as ready.

## Alternatives considered

1. **Imperative provisioning script / Job** — rejected: not GitOps-reconciled, no self-heal/prune, drift-prone; sync-waves give declarative ordering for free.
2. **No ordering (all resources at once)** — rejected: the app would crash-loop awaiting its DB/secret; harmless but noisy and slows time-to-ready; sync-waves are cheap correctness.
3. **Uniqueness enforced by an admission webhook** — rejected: bespoke code (ADR-086 infra-only constraint); a PR-time CI check is sufficient and keeps the gate in the reviewable change-control surface.
