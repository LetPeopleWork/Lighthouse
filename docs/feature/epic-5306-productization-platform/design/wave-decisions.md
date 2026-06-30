# DESIGN Wave Decisions — epic-5306-productization-platform

> **Wave**: DESIGN (combined, whole-platform). **Mode**: PROPOSE. **Date**: 2026-06-29. **Architect**: Titan (System Designer).
> **Scope**: system / infrastructure only (IaC + Helm + GitOps orchestrating Kubernetes). No application or domain code.
> **Authoritative architecture**: `docs/product/architecture/brief.md` → `## System Architecture — epic-5306-productization-platform`. **ADRs**: `adr-086..093`.

## Key Decisions

| # | Decision | Chosen option | ADR |
|---|---|---|---|
| CC-2 | GitOps repo layout | Tenant = `tenants/<id>/tenant.yaml`; ArgoCD **ApplicationSet** (Git-files generator); mono-repo `bootstrap/`+`platform/`+`tenants/`; **no bespoke controller** | 086 |
| CC-3 | Secrets strategy | **External Secrets Operator + self-hosted OpenBao**; only refs in git; rotate = update store. Sealed Secrets (ciphertext-in-git, rotation pain) + Vault (BSL licence) rejected | 087 |
| CC-4 | Substrate boundary + Infomaniak mode | Module emits a **conformant-cluster contract**; Kubernetes via **k3s on OpenStack compute (cloud-init)** + Calico; managed-k8s/CAPO are drop-ins behind the same boundary | 088 |
| Red card | Break-glass GitOps | Per-incident **auto-sync disable on the single affected Application**; standing `ArgoCDAutoSyncDisabled` alert = self-expiring | 089 |
| Red card | Metric cardinality | One bounded `tenant` label; drop unbounded labels at scrape; **recording rules** pre-aggregate the fleet dashboard; cardinality-budget alert | 090 |
| CC-5 | Per-tenant DB + DR topology | One **CloudNativePG `Cluster` per tenant**; CNPG WAL + scheduled backup to off-cluster S3-compatible storage keyed by id; namespace-isolated rehearsed restore | 091 |
| — | Provisioning data-flow | One record → **sync-wave-ordered** app-of-apps (ns/quota/netpol → DB/secret → chart/route/cert); all names from the CC-6 id; PR-time uniqueness; prune-on-remove | 092 |
| — | Automated upgrade | Tenant-Zero **canary (`canaryVersion`) → promote (`promotedVersion`)**; expand-only CI guard pre-flight; rollback = git revert + helm rollback | 093 |

## Architecture Summary

GitOps-reconciled, namespace-per-tenant multi-tenancy with declarative fan-out. OpenTofu (OpenStack provider) stands up a conformant k3s cluster; ArgoCD's app-of-apps reconciles the whole platform + every tenant from one mono-repo; a single `tenant.yaml` record is fanned by an ApplicationSet into a fully isolated tenant (namespace + NetworkPolicy + ResourceQuota + per-tenant CNPG Postgres + ESO-materialised secret + #5199 chart release + wildcard-routed TLS subdomain). The shipped #5199 chart is the per-tenant workload; epic-5305 runtime primitives are composed via chart values. Every capability is an additive overlay over off-the-shelf CNCF/official operators — no bespoke controller, no forked chart. C4 Context + Container + Component(provisioning) diagrams in the brief section.

## Reuse Analysis

**Zero unjustified CREATE NEW.** REUSE: #5199 chart, all epic-5305 primitives, ArgoCD / ingress-nginx / cert-manager / external-dns / ESO / OpenBao / CNPG / kube-prometheus-stack (all off-the-shelf, config only). EXTEND: the existing GitHub Actions workflow (PR-time uniqueness + expand-only guards). CREATE (justified): the OpenTofu substrate module (no prior substrate exists, standard provider resources) and GitOps configuration (tenant records, ApplicationSet, sync-wave manifests, recording rules — config-as-code, not application code).

## Technology Stack (pinned intent — DELIVER pins exact patch)

OpenTofu 1.8.x · terraform-provider-openstack ~>2.1 · k3s v1.31.x · Calico 3.28.x · ArgoCD 2.13.x · cert-manager 1.16.x · external-dns 0.15.x · External Secrets Operator 0.10.x · OpenBao 2.1.x · CloudNativePG 1.24.x · kube-prometheus-stack 65.x (Prometheus 2.55.x / Grafana 11.x). Vendor-neutral, official/CNCF images only.

## Constraints (honored)

- **D0 standalone gate sacrosanct** — entire platform is a hosted-only overlay; standalone product + chart standalone defaults byte-unchanged; everything auto-degrades.
- **D0b vendor-neutral** — official/CNCF images only; no Bitnami; Vault(BSL) and Bitnami-class supply risks rejected; provider specifics behind the CC-4 boundary.
- **D0c expand-only migrations** — CI guard pre-flight blocks destructive migrations before any tenant rolls (ADR-093).
- **D0d extend existing GH Actions, trunk-based** — guards added to the existing workflow, no parallel workflow.
- **D0e built ON the shipped chart** — chart parameterised, never forked.
- **Isolation guardrail (0 incidents)** — namespace + Calico NetworkPolicy + per-tenant OpenBao path + per-tenant CNPG + per-tenant backup prefix, all keyed off one id; probed live.

## Upstream Changes

**None.** No DISCUSS assumption was contradicted. CC-1/CC-5/substrate/provider were already locked. CC-3 (ESO + backend) was *narrowed* to OpenBao (consistent with the DISCUSS working assumption). CC-4's Infomaniak-k8s-mode was an *open question resolved* (k3s on compute), not an assumption changed. No `## Changed Assumptions` section and no `design/upstream-changes.md` required.

## Earned-Trust probes (per ADRs)

`substrate.probe` (NetworkPolicy/LoadBalancer/StorageClass proven empirically, ADR-088) · `secrets.probe` (round-trip + cross-tenant-deny, ADR-087) · `provision.probe` (200 + isolation, ADR-092) · `dr.restore.rehearsed` (timed Tenant-Zero restore, ADR-091) · standing `BackupStale` / `ArgoCDAutoSyncDisabled` / cardinality-budget alerts (ADR-089/090/091). Self-application: substrate/secrets probes re-run after each dependency version bump; restore rehearsal runs per release.

## Open Questions (flagged for the operator — not blocking)

- **O-1** Infomaniak managed-Kubernetes availability (assumed: stand up k3s on compute; managed-k8s is a drop-in if confirmed). *No WebSearch available to Titan — grounded in cutoff knowledge, needs operator confirmation.*
- **O-2** Confirm base domain `lighthouse.letpeople.work` + Tenant-Zero subdomain `lpw`.
- **O-3** Confirm RPO/RTO commitment (assumed RPO ≤24h / aspirational ≤1h, RTO ≤30 min).
- **O-4** Confirm tenant-density economic target (assumed ≥20/cluster, headroom ~200).
- **O-5** OpenBao operational posture (single + operator-held keys for WS; HA + auto-unseal at production; optional Barbican KMS behind CC-4).

## RESCOPE ADDENDUM — slice-08 (#5205 merge-only release)

> **Wave**: DESIGN (rescope, brownfield). **Mode**: PROPOSE. **Date**: 2026-06-30. **Architect**: Titan.
> **Scope**: PRIVATE-repo GitOps + CI only; public #5199 chart byte-unchanged. Designed ON the LIVE slice-08
> substrate (matrix appset + per-record `chartVersion` override + fleet `promotedVersion`). **ADRs**: 094..097.
> Full detail: `feature-delta.md` → `## Wave: DESIGN / [REF] slice-08 RESCOPE`.

### Key Decisions (resolved O-08-*)

| # | Decision | Chosen option | ADR |
|---|----------|---------------|-----|
| O-08-1 | Tenant-Zero auto-canary mechanism (headline) | **Renovate auto-merges a TZ-scoped `chartVersion`-override PR** (everything in git); fleet `promotedVersion` PR never auto-merged. Mutable `latest` tag rejected (registry-mutable; ArgoCD reconciles git; breaks rollback clarity); argocd-image-updater rejected (new controller; wrong target — knob is chart version, not image tag) | 094 |
| O-08-2 | Migration-before-API ordering | **No pre-upgrade Job / sync-wave.** Emergent from readiness-gated rolling update + on-boot advisory-lock `Database.Migrate()` + expand-only guard. A Job would break zero-downtime and add a failure mode | 095 |
| O-08-3 | Smoke-test surface + alert channel | **Version-stamped per-tenant ArgoCD PostSync hook Job** asserts served version + health; on failure opens/updates a **GitHub issue** (user-locked) naming tenant + version. Token via ESO/OpenBao (shared `platform` ns recommended) | 096 |
| O-08-4 | Renovate hosting + watch scope + automerge | **Mend Renovate GitHub App** (user-locked). Watch the published `lighthouse` chart (lpw `chartVersion` + fleet `promotedVersion`) + tracked platform components. Automerge ONLY the TZ `chartVersion`; fleet + components no-automerge | 097 |
| O-08-5 | Rollback posture | **Operator-initiated `git revert`** (proven in substrate). Smoke-test is detect+alert only. Auto-rollback OUT (flagged future) | 096 |

### Reuse Analysis (rescope)

**Zero unjustified CREATE NEW.** REUSE: the matrix appset + `promotedVersion`/`chartVersion` knobs, the
`ExpandOnlyMigrationGuard` (epic-5305 ADR-077), on-boot `Database.Migrate()` + advisory lock, readiness-gated
rolling update + drain, ESO/OpenBao (ADR-087), the epic-5305 chart health/version probe. EXTEND: the
`tenant-runtime` overlay (+ PostSync smoke-test Job + GitHub-token ESO), `applicationset-runtime.yaml` (fold
in `promotedVersion` to version-stamp the smoke-test), `validate-tenants.yml` (+ `renovate-config-validator`),
the lpw record (re-add the `chartVersion` canary anchor). CREATE (justified): `renovate.json` (no prior config;
merge-only entry point) + the PostSync smoke-test Job (no prior post-upgrade health gate; thin standalone
alert since slice-09 Alertmanager is absent). The workload `applicationset.yaml` is unchanged.

### Scale note

Control-plane, not data-plane: ~tens of tenants, ~weekly releases; a handful of Jobs + ≤1 issue-API call per
roll; GitHub 5000/h limit never approached. Binding constraints are latency budgets (KPI-2/4/5) and
correctness/ordering — not throughput. No new scaling component justified.

### Earned-Trust (rescope probes)

`renovate.validate` (automerge gated on the `validate-tenants` required check — a malformed record cannot
auto-canary) · `upgrade.smoketest` (the PostSync Job demonstrates the expected version actually serves healthy
in the real cluster; emits a structured GitHub issue naming tenant+version+code when a Synced tenant lies) ·
the smoke-test is version-stamped so it **re-probes on every version bump** (self-application) · optional
`renovate-config-validator` keeps the watch-scope config honest after edits.
