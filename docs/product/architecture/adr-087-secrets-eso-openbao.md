# ADR-087: Per-Tenant Secrets via External Secrets Operator (ESO) Backed by Self-Hosted OpenBao; Only `ExternalSecret` *References* Live in Git; Rotation = Update the Store, No Git Edit; Sealed Secrets and HashiCorp Vault Rejected

**Status**: **ACCEPTED** (2026-06-29, Benjamin) — O-5: single OpenBao + operator-held unseal for the walking skeleton; HA (3-node Raft) + auto-unseal before customer tenants
**Date**: 2026-06-29
**Feature**: epic-5306-productization-platform (ADO Epic #5306, story #5203 secrets, #5207 provisioning) — converges cross-cutting decision **CC-3**
**Decider**: Benjamin (product owner) + Titan (System Designer, PROPOSE)
**Relationship to prior work**: Builds ON the shipped #5199 chart (ADR-080..085) — the chart consumes a normal Kubernetes `Secret`; ESO *materialises* that Secret from the store. No chart template and no app code change. Composes with ADR-086 (the `ExternalSecret` is one of the objects the per-tenant app-of-apps renders) and ADR-091 (the CNPG `Cluster` consumes the materialised DB Secret).

---

## Context

CC-3 invariant: **no plaintext secret in the GitOps repo**, **per-tenant isolation**, **rotation supported**. The locked tenancy model (CC-1 namespace-per-tenant) makes the secret boundary the namespace. Per-tenant secret material = DB password (CNPG), OIDC client secret, license key, optional Redis/MCP credentials. The repo (ADR-086) is the single change-control surface, so secrets must be *referenced* there, never *contained* there. Three strategies were weighed; the backend must fit an OpenStack substrate without single-cloud lock-in (D0b vendor-neutral).

## Decision

**Install External Secrets Operator (ESO) as a cluster-singleton platform component (ADR-086 `platform/`), backed by a self-hosted OpenBao instance.** Each tenant namespace gets a `SecretStore` (namespace-scoped) bound to an OpenBao path `secret/tenants/<tenant-id>/*` derived from the CC-6 id; the per-tenant app-of-apps renders an `ExternalSecret` that names the keys to pull. ESO reconciles the actual `Secret` into the tenant namespace on a refresh interval.

- **No plaintext in git**: the repo holds only `ExternalSecret` (key names + store ref) — `git grep` for secret *values* = 0 (guardrail test).
- **Per-tenant isolation**: OpenBao policies scope a tenant's auth (Kubernetes auth method, ServiceAccount-bound) to *its own* `secret/tenants/<tenant-id>/*` path only; a namespace-scoped `SecretStore` (not a cluster-wide one) prevents one tenant's `ExternalSecret` from reading another's path.
- **Rotation without git edit**: update the value in OpenBao → ESO re-syncs within the refresh interval → the tenant picks up the new value, zero commits (US-05 AC).
- **Backend = OpenBao** (the Linux Foundation, MPL-2.0 fork of Vault). Vendor-neutral, runs on any conformant cluster, no OpenStack coupling. **HashiCorp Vault is rejected** for the platform default on its 2023 BSL licence change (a re-licensing/supply risk for an OSS-aligned vendor-neutral platform, same class of risk as the Bitnami rejection in ADR-080). OpenBao is the drop-in, freely-licensed equivalent.
- **OpenStack Barbican** is *not* the ESO backend: ESO has no mature first-class Barbican provider, and making the platform secrets layer OpenStack-specific would violate CC-4 (the platform layer stays provider-neutral; provider specifics live behind the substrate boundary). Barbican *may* optionally back OpenBao's auto-unseal/KMS as an OpenStack-specific detail behind the CC-4 boundary — not required, not the default.

### Earned-Trust probe (CC-3 honesty)

A `secrets.probe` runs as a platform health check at ESO/OpenBao bring-up and after every OpenBao upgrade: it (a) writes a canary value to `secret/_probe`, reads it back through an `ExternalSecret` into a probe namespace, asserts round-trip equality, then (b) attempts a cross-tenant read (tenant-A SA reading tenant-B path) and asserts it is **denied**. A failed probe emits `health.startup.refused{component=eso, lie=<round-trip-mismatch|isolation-breach>}` and blocks the platform from being marked Healthy — the substrate must *demonstrate* isolation, not be assumed to enforce it. (Self-application: the probe re-runs after each OpenBao/ESO version bump.)

| Quality attribute | Weight | (A) ESO + OpenBao ✅ | (B) Sealed Secrets | (C) HashiCorp Vault + ESO |
|---|---|---|---|---|
| No plaintext in git (invariant) | Highest | **Only refs in git** | Ciphertext in git (encrypted, but secret material is committed) | Only refs in git |
| Rotation without git edit (US-05 AC) | Highest | **Update store → auto-resync** | **Re-seal → new git commit** (fails the AC) | Update store → auto-resync |
| Per-tenant isolation | High | Namespace `SecretStore` + OpenBao path policy | Per-namespace sealing cert; weaker path-level policy | Vault namespaces/policies |
| Vendor-neutral licence (D0b) | High | **MPL-2.0, LF-governed** | OK (Apache-2.0) but ciphertext-in-git model loses on rotation | **BSL since 2023 — rejected** |
| Operability for small team | Medium | One operator + one OpenBao (HA optional) | Trivial (one controller) but rotation pain compounds at fleet scale | Heaviest to run (unseal, HA, audit) |
| Off-cluster store of record | Medium | **Yes (OpenBao is the SoR; cluster rebuildable)** | No — sealing key is the SoR; lose it = lose all secrets | Yes |

## Consequences

- **Positive**: rotation is a store update (no git churn, KPI-aligned); the GitOps repo provably holds zero secret values; OpenBao is the off-cluster source of record so a `tofu destroy`/rebuild (US-01) re-attaches secrets without re-sealing; vendor-neutral licence; one mechanism for every tenant (no per-tenant special-casing — fed by the same ADR-086 generator).
- **Negative / cost**: OpenBao is a stateful platform dependency that must itself be backed up and (in production) run HA + auto-unsealed; ESO refresh interval bounds rotation latency (seconds–minutes, acceptable). OpenBao's own root/unseal keys are the bootstrap secret — stored out-of-band (operator-held / KMS), the one secret that cannot live in the cluster it protects.
- **Standalone gate**: untouched — the standalone product and the #5199 chart consume a plain `Secret`; ESO is a hosted-platform-only overlay. The chart never requires ESO.

## Alternatives considered

1. **Sealed Secrets** — rejected: stores encrypted secret material *in* git, so rotation requires a re-seal + commit (fails the US-05 "rotate without git edit" AC); the sealing private key becomes a single un-backed-up source of truth.
2. **HashiCorp Vault + ESO** — rejected as the default on the 2023 BSL re-licensing (vendor-neutrality/supply risk); OpenBao is the freely-licensed drop-in with the same ESO integration. Vault remains a documented BYO option for operators who already run it.
3. **OpenStack Barbican as the ESO backend** — rejected: no mature ESO Barbican provider and it couples the platform secrets layer to OpenStack (violates CC-4). Allowed only as an optional OpenBao auto-unseal KMS behind the substrate boundary.
4. **`kubectl`-applied hand-made Secrets (slice-03 interim)** — explicitly the *before* state; replaced by this ADR at slice-04. Acceptable only for the Tenant-Zero walking skeleton before secrets are productized.
