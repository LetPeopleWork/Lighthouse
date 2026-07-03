# Epic 5306 — Productization Platform (multi-tenant SaaS hosting)

**Finalized:** 2026-07-03
**ADO:** Epic #5306 (all children Closed/Resolved)
**Status:** SHIPPED + live-proven on production Tenant Zero (`lpw.lighthouse.letpeople.work`), full onboarding e2e verified end-to-end with a throwaway `demo` tenant on 2026-07-03.

---

## Feature summary

Turn Lighthouse from a single self-hostable app into a **multi-tenant SaaS platform** where a new customer instance is stood up from **one reviewable git file plus out-of-band secrets** — namespace, quota, network isolation, secrets, app, wildcard TLS, backups, observability and optional MCP server all follow automatically via GitOps (ArgoCD). The standalone single-tenant chart stays byte-clean throughout (no production special-casing — Tenant Zero is produced by the *same* generator every customer uses, ADR-086).

## Business context

LetPeopleWork needed to host Lighthouse for customers without hand-building each instance or forking the product for "hosted mode." The design constraint (**standalone-sacrosanct**): every capability the platform adds must leave the public Helm chart installable and byte-identical for self-hosters. The payoff is one-record onboarding: a customer instance in minutes, fully isolated, reproducible from git on any operator machine.

## Architecture (repo split, decided 2026-06-29)

- **Public `Lighthouse`** (this repo): product code, Helm chart (#5199), design ADRs (`docs/product/architecture/adr-086..097`), and the acceptance `.feature` SSOT (`tests/platform/epic-5306/acceptance/`). Methodology artifacts stay public.
- **Private `LetPeopleWork/lighthouse-platform`**: ALL infra/gitops — `infra/substrate/` (OpenTofu) + `gitops/` (ArgoCD ApplicationSets, per-tenant records, `_charts/tenant-runtime`, platform components). Private because it holds hosting topology + secret *references* (CC-3: no plaintext in git).

## Work completed — 13 slices

| Slice | ADO | What shipped | Live proof |
|---|---|---|---|
| S01 substrate-up | #5320 | OpenTofu module → real Infomaniak KaaS (`infomaniak_kaas`, NOT Magnum); k3s fallback behind same CC-4 contract | cluster `lpw-substrate` Active, 2 nodes Ready k8s 1.33 |
| S02 gitops-control-plane | #5201 | ArgoCD v3.4.4, app-of-apps from private repo over read-only SSH deploy key; drift self-heal proven | cert-manager delete → recreated |
| S03 tenant-zero-reachable | #5202/#5204 | chart 0.1.2 `postgresql.auth.existingSecret` (monolithic Npgsql connstr in the Secret); ingress-nginx LB + Let's Encrypt | `lpw.lighthouse…` trusted HTTPS 200 |
| S04 managed-secrets | #5203 | chart 0.1.3 `oidc.existingSecret`; ESO 0.10.7 + OpenBao 2.3.1 self-hosted; OIDC login store-sourced | rotation proven (store change → Secret sha → restart) |
| S05 wildcard-routing | #5202 | chart 0.1.4 host-mandatory guard + reloader auto-roll wiring; `*.lighthouse`→LB | reloader auto-roll proven; never-configured subdomain serves trusted HTTPS |
| S06 second-tenant-by-hand | #5207 | `acme` by hand from lpw's records on UNCHANGED chart — CC-1 tenancy de-risk | 2 isolated tenants coexist; torn down post-proof |
| S07 automated-provisioning | #5376 | `tenant-runtime` chart (ns+quota+5 default-deny NetworkPolicies+ESO) + `tenants-runtime` ApplicationSet + `validate-tenants.sh` CI — the SaaS payoff | `riverbank` from ONE record → 200, Cilium enforces cross-tenant block |
| S08a/b/c fleet-upgrade | #5205 | Renovate merge-only release (TZ auto-canary), PostSync smoke-test + GitHub-issue alert, broken-image rollback drill | auto-canary hands-off merge; alert issue opened on drill |
| S09 fleet-observability | #5206 | kube-prometheus-stack + PodMonitor + recording/alert rules + Grafana fleet dashboard | `up{tenant="lpw"}=1`, cardinality bounded |
| S10 per-tenant-backups | #5208 | `tenant-runtime` pg_dump→off-cluster Infomaniak S3 CronJob (id-keyed) + `BackupStale` alert | artifact off-cluster within RPO; alert fires + names tenant |
| S11 restore-rehearsal | #5208 | id-keyed restore Job + weekly rehearsal CronJob (scratch→verify→drop, timed vs RTO) | restore 32 tables ~8s ≪ 30-min RTO; failure → GitHub issue |
| S12 multi-provider-parity | #5320 | DOCUMENT-PATH-ONLY (provider-addition-howto.md); 2nd-provider stand-up deferred pull-on-demand | n/a (documented path) |
| S13 tenant-record Track B | #5387 | onboarding-decision fields (`mcpEnabled`/`placement`/audience) + `_TEMPLATE.tenant.yaml` + validator auth-mandatory/mcp-shape guards; MCP dogfooded on TZ | `POST /mcp` 401+WWW-Authenticate; metadata 200 |
| Track A remote-state | #5374 | tofu state local→Infomaniak S3 (partial backend config), ADR-098 | state migrated, live-proven |
| MCP OAuth pass-through | #5388 | Auth0 DCR setup (5 settings) so Claude connectors get an `aud`-scoped token | connector tool call 200 + data |

**Final onboarding e2e (2026-07-03):** a throwaway `demo` tenant walked the entire `onboarding-a-customer.md` runbook — record → OpenBao seed (db/oidc/policy/role) → push → ArgoCD reconcile → **200 + trusted LE cert + cross-tenant isolation + Auth0 login + premium licence** → clean de-provision (app+ns pruned, OpenBao cleaned, lpw untouched). Every step matched the documented runbook.

## Key decisions

- **ADR-086** GitOps repo layout + ApplicationSet; **no production special-casing** — Tenant Zero is an ordinary tenant record.
- **ADR-087** secrets via ESO + OpenBao; git holds references only.
- **ADR-088** substrate boundary (OpenStack/KaaS ⟷ k3s) behind one CC-4 contract.
- **ADR-089** break-glass GitOps path.
- **ADR-090** metric cardinality bounding (one bounded `tenant` relabel; labeldrop at scrape).
- **ADR-091** per-tenant CNPG backup/restore — **later superseded** at DISTILL by a pg_dump logical dump against the bundled Postgres (CNPG WAL deferred); RPO ≤24h / RTO ≤30min.
- **ADR-092** one-record provisioning data flow (the generator).
- **ADR-093** automated upgrade flow (canary → promote → expand-only → git-revert).
- **ADR-094** Tenant-Zero auto-canary via Renovate automerge.
- **ADR-095** migration-before-API, no pre-upgrade Job.
- **ADR-096** PostSync smoke-test → GitHub-issue alert.
- **ADR-097** Renovate watch scope + automerge policy.
- **ADR-079/085** MCP OAuth inbound-auth + MCP workload (chart already ships `mcp.enabled` + `mcp.auth.mode`).
- **Re-discuss 2026-07-02 (RD-2):** auth + licence are **MANDATORY for every tenant, no opt-out**; readiness = everything reproducible from git; multi-provider = document-path-only.

## Lessons learned (durable gotchas)

- **ArgoCD goTemplate scalars** with inner quotes (`hasKey . "x"`) must be **single-quoted** YAML or the ApplicationSet unmarshal fails → 0 apps generated (Tenant Zero preserved). Validate YAML before push.
- **prometheus-operator `NamespaceSelector`** takes `any`/`matchNames` only — a `matchLabels` gets pruned to own-namespace → 0 scrape targets. Use `{any: true}` bounded by `podSelector`.
- **Default-deny NetworkPolicy** blocks cross-namespace Prometheus scrape → needs an explicit `allow-metrics-from-monitoring` policy; on Infomaniak KaaS the kube-apiserver is an **external non-443 endpoint** Cilium drops → smoke-test needs a `toEntities: [kube-apiserver]` CiliumNetworkPolicy.
- **`minio/mc` image ships no coreutils** (no awk/grep/sort/tail) — select the newest backup with `mc` subcommands + shell builtins only.
- **ESO 2.7.0 drops `v1beta1`** — pre-stored SecretStores strand; recover via `conversion.strategy: None` relabel + `storedVersions` prune + `ServerSideApply`. Orphan-delete for OpenBao STS immutable fields (no unseal).
- **ArgoCD repo-server git cache ~2min** after every push before ApplicationSets regenerate — **don't restart** the shared repo-server; wait the poll. (Observed again in the demo onboarding.)
- **Removing an ArgoCD app's `resources-finalizer` does not stick** (controller re-adds it) → prune can cascade to ESO-owned Secrets. To truly orphan: annotate `sync-options: Delete=false` *before* removing the app, or migrate by editing the managing app's source.
- **Stale `asuid.<sub>` TXT** (or any leftover descendant record) silently shadows a working DNS wildcard (RFC 4592 empty-non-terminal → NODATA). Check for empty-non-terminals before suspecting the wildcard.
- **MCP OAuth audience MUST equal the MCP server URL** (`.../mcp` or origin, NOT `.../api`) — RFC 9728 resource-indicator; compliant clients reject a mismatched `resource`.
- **Auth0 DCR for Claude connectors** needs 5 settings (register `/mcp` API+permission, Resource-Parameter-Compatibility-Profile ON, third-party user-delegated Authorized, DCR ON, promote login connection to domain-level). Desktop relay-caches the DCR client per MCP-URL → delete the stale Auth0 app to break the cache.
- **`validate-tenants.sh` `field()`** returned nonzero on an absent key under `set -e`/`pipefail` → silently killed the script; `|| true` fixes it.
- **Infomaniak object storage has no state locking** (501 on conditional PUT / no DynamoDB) → `use_lockfile=false`; endpoint `dc4-a`→`pub2`.

## Issues encountered

- Tenant Zero node-capacity outage (pool drifted to 1× a1-ram2, 121% overcommit → CNI/kubelet flap NotReady). Fixed: a2-ram4 flavour + tofu validation guard (ram≥4/vCPU≥2/min≥2). Not the monitoring itself.
- A leaked `INFOMANIAK_TOKEN` in an early transcript — deleted at Infomaniak; mint fresh on demand.
- #5388 (MCP OAuth token audience) split out from #5387's live-proof correction — the workload+discovery handshake proved before end-to-end tool DATA access did; closed once the Auth0 DCR chain lined up.

## Links to permanent artifacts

- **ADRs:** `docs/product/architecture/adr-086..097-*.md` (+ ADR-079/085 MCP, ADR-098 remote-state).
- **Acceptance SSOT:** `tests/platform/epic-5306/acceptance/slice-*.feature` (13 slices).
- **Architecture brief:** `docs/product/architecture/brief.md`.
- **UX journey:** `docs/ux/epic-5306-productization-platform/journey-fleet-upgrade-merge-only.{yaml,-visual.md}` (migrated at finalize).
- **Infra + runbooks:** private `LetPeopleWork/lighthouse-platform` — `docs/onboarding-a-customer.md` is the operator runbook; `gitops/tenants/_TEMPLATE.tenant.yaml` the copy-me record.
- **Feature workspace (history):** `docs/feature/epic-5306-productization-platform/` (preserved — feeds the wave matrix).
