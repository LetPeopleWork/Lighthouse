# RESUME — Epic 5306 productization platform

## Repo split (decided 2026-06-29)
- **Public Lighthouse** (this repo): product, chart #5199, design docs, acceptance `.feature` SSOT
  (`tests/platform/epic-5306/acceptance/`). Specs stay here (methodology artifacts).
- **Private `LetPeopleWork/lighthouse-platform`**: ALL infra/gitops — `infra/substrate/` (OpenTofu)
  + `gitops/` (ArgoCD). Private = holds LPW hosting topology + secret references (CC-3, no plaintext).
  Cloned at `/storage/repos/lighthouse-platform`.

## State
- ✅ DISCUSS + DESIGN (combined whole-platform): feature-delta.md, design/wave-decisions.md
  (ADR-086..093), 12 slice docs. O-1..O-5 resolved.
- ✅ DISTILL (WS-first S01-S03): `.feature` SSOT in public Lighthouse; Tier-1 [REF] wave-delta in
  feature-delta.md. Reconciliation gate PASSED. Committed c19c4eeb (Lighthouse, local).
- ✅ DELIVER S01 substrate (#5320): OpenTofu module (private repo). Managed adapter = REAL Infomaniak
  KaaS provider `Infomaniak/infomaniak` (`infomaniak_kaas` + `_instance_pool`), NOT Magnum (O-1
  fully resolved). k3s-compute fallback behind same CC-4 contract.
  **APPLIED + LIVE**: cluster `lpw-substrate` kaas_id=4323 (cloud 21349 / project 44008 PCP-6ROXJE3),
  status Active, 2 nodes Ready v1.33.8 (k8s 1.31 deprecated → 1.33). kubeconfig at
  `~/.kube/lpw-substrate.yaml` (local, 0600). tofu state LOCAL (move to Infomaniak S3 before CI/team).
  Billing against CHF 300 credit. ✅ INFOMANIAK_TOKEN: exposed token DELETED at Infomaniak 2026-06-30
  (leaked value now dead, no replacement created) — mint a fresh token on demand before any further
  tofu/API work.

## Next
1. ✅ **INFOMANIAK_TOKEN secured (2026-06-30)** — exposed token deleted at Infomaniak (leaked value
   dead). No active token exists; mint a fresh one + re-export before the next tofu/API run.
2. ✅ **DELIVER S02 done (live)** — ArgoCD v3.4.4 installed on lpw-substrate; root app-of-apps
   reconciling from the PRIVATE repo over a read-only SSH **deploy key** (`argocd-readonly`, key
   `~/.ssh/lpw-platform-argocd`; argocd repo secret `lighthouse-platform-repo`). platform-root/
   platform/tenants/cert-manager Synced/Healthy. ApplicationSet generated tenant-lpw. Manifests use
   SSH repoURLs. Drift self-heal PROVEN (deleted cert-manager deploy → ArgoCD recreated it).
   ADO #5201 = Resolved.
3. ✅ **DELIVER S03 done (LIVE 2026-06-29)** — Tenant Zero reachable at
   **https://lpw.lighthouse.letpeople.work** over a trusted Let's Encrypt cert. ALL three
   "done=observable" met (pods Ready, trusted HTTPS serves LPW, real secret out-of-band).
   - PUBLIC chart 0.1.2 (pushed 7f4da742): `postgresql.auth.existingSecret` — chart reads DB creds
     from a pre-existing Secret. CRUX: Npgsql connection string is monolithic, so the existing Secret
     supplies BOTH keys (`Database__ConnectionString` + `postgres-password`), not just the password.
     New helpers `lighthouse.db.secretName` + `lighthouse.renderDbKeys`; secret.yaml skips chart-owned
     DB keys (and renders no empty Secret) when set; deployment+statefulset DB refs → db.secretName.
     ADR-082 `required` relaxed by NOT calling connectionString helper (no helper edit). 44/44 unittest.
     Packaged+indexed into docs/charts; served via Pages.
   - PRIVATE platform (pushed b652934): tenant-lpw chartVersion→0.1.2 + `existingSecret: lighthouse-db`
     + ingress TLS/cert annotation in ApplicationSet values; new platform/ingress-nginx.yaml
     (LoadBalancer, default IngressClass nginx) + platform/cluster-issuer.yaml (letsencrypt-prod,
     HTTP-01, contact contact@letpeople.work).
   - LIVE: ingress-nginx LB **179.237.75.110** (Infomaniak Octavia). DNS A lpw.lighthouse→IP (GoDaddy,
     letpeople.work zone, user-made). cert-manager HTTP-01 → cert lpw-tls READY. tenant-lpw api+postgres
     1/1; API↔Postgres via hand-made `lighthouse-db` Secret (ns tenant-lpw, out-of-band, NOT in git).
   - NO OIDC on Tenant Zero yet (ApplicationSet sets only ingress.host; oidc defaults off = standalone
     parity). DECIDED: add OIDC in slice-04 so the client secret is ESO-sourced from day one (avoid
     hand-made-then-migrate). Chart keeps OIDC clientSecret in `lighthouse.secretName` (separate from
     the DB existingSecret) → enabling OIDC is an independent flip.
   - DNS long-term: slice-05 wildcard `*.lighthouse.letpeople.work`→LB (one record, all tenants) or
     external-dns controller; today's single explicit host is deliberate slice-03 thinness.
   - ⚠️ hand-made lighthouse-db Postgres password appeared in chat transcript (cluster-internal DB,
     slice-04 ESO rotates it — low stakes). ADO #5204 + thin #5202 → transition to Resolved.
4. ✅ **DELIVER S04 done (LIVE 2026-06-29)** — managed-secrets (#5203). Tenant Zero's secrets now
   store-sourced from self-hosted OpenBao via ESO. ALL "done=observable" met.
   - PUBLIC chart 0.1.3 (04e78058 + README 2cf70908): `oidc.existingSecret` escape hatch mirroring
     slice-03's `postgresql.auth.existingSecret`. New helpers `lighthouse.oidc.secretName` +
     `renderOidcKey`; secret.yaml skips chart-owned OIDC key (renders no empty Secret) + relaxes the
     clientSecret `required` when set; deployment OIDC env → `oidc.secretName`. 47/47 unittest +
     install-smoke green. Packaged+indexed; DISTILL `slice-04-managed-secrets.feature` SSOT.
   - PRIVATE platform (8fd4b53..eeca622): `platform/external-secrets.yaml` (ESO 0.10.7) +
     `platform/openbao.yaml` (OpenBao 2.3.1, single-node file storage, O-5 operator-held keys) +
     standalone `tenant-secrets` app over `gitops/tenant-secrets/lpw/secrets.yaml` (SecretStore via
     ESO vault provider + k8s auth, SA `openbao-auth`, `lighthouse-db` + `lighthouse-oidc`
     ExternalSecrets). tenant.yaml chartVersion 0.1.3 + oidcEnabled/oidcIssuer/oidcClientId;
     ApplicationSet renders oidc block + nginx proxy-buffer + app.proxy.trustedNetworks when
     oidcEnabled.
   - LIVE: OpenBao init/unsealed (keys at `~/.openbao-lpw-init.json`, 0600, NOT in git/transcript);
     kv-v2 `secret/`, k8s auth role `tenant-lpw` → policy read `secret/tenants/lpw/*`. DB creds seeded
     from the hand-made Secret; `lighthouse-db` now ESO-owned (hand-made deleted), API restarts clean.
     OIDC (Auth0 `dev-xlw0xiiyqjdtaaid.us.auth0.com`) login round-trips: 302 → /authorize with the
     store-sourced client secret, **https** redirect_uri. Rotation PROVEN: store change → Secret sha
     changed → restored, zero git edits.
   - ⚠️ FINDINGS (not blockers): (a) Tenant Zero is in **AuthMode.Blocked** — "auth configured
     correctly, but Premium licensing not valid"; import a premium license for full function.
     (b) chart has **no config-checksum** on the pod template → ConfigMap/Secret changes (incl. ESO
     rotation) need a manual `rollout restart` to reach the running app; consider a checksum annotation
     or stakater/reloader (slice-05+). ADO #5203 → Resolved (PENDING user confirm).
5. ✅ **DELIVER S05 — SHIPPED + 4/4 live-proven (demo-subdomain proof passed 2026-06-30)** —
   wildcard-routing (#5202). DISTILL: `slice-05-wildcard-routing.feature` (9 scenarios). Gate PASSED.
   - PUBLIC chart **0.1.4** PUSHED (4511b3b9 feat + 594b1f4b specs/docs + 3b173fb6 publish
     0.1.4.tgz→docs/charts): `ingress.yaml` host-mandatory guard; `_helpers.tpl`
     `reloadSecrets`/`reloadEnabled`; `deployment-api.yaml` pod-template annotations
     (`checksum/config` + `secret.reloader.stakater.com/reload`) gated on a managed `existingSecret`
     (standalone byte-clean). `helm unittest` 53/53; render-diff checksum proof; D0 0 matches; lint clean.
   - PRIVATE platform PUSHED (eeca622..0e96ee9): `gitops/platform/reloader.yaml` NEW (stakater/reloader
     2.2.12); `tenant.yaml` chartVersion→0.1.4; `applicationset.yaml` comment-only.
   - **subdomain→id defaulting DEFERRED to slice-07**: `dig`/`.subdomain` fallback unprovable under
     helm (`.Values` ≠ ArgoCD param map); wrong guess + `missingkey=error` breaks ALL tenants. Host
     still derives from id (subdomain==id by convention); wildcard mechanism unaffected.
   - **LIVE-PROVEN (2026-06-29):** ArgoCD synced both. Tenant Zero on chart 0.1.4 — API deploy carries
     `checksum/config: c7659a2f…` + `reload: lighthouse-db,lighthouse-oidc`. reloader controller
     Running, watching secrets all-namespaces. ✅ `lpw.lighthouse…` HTTP/2 200, Let's Encrypt cert
     (NO regression). ✅ **reloader auto-roll PROVEN** on a throwaway Secret (v1→v2 → "Changes
     detected… updated 'probe' Deployment", gen 1→2, new pod, NO manual restart / NO git commit) —
     the exact path that fires on an ESO/OpenBao rotation of the watched secrets. Tightens slice-04.
   - ✅ **never-configured `demo.lighthouse…` serves trusted HTTPS (2026-06-30)** — root cause was NOT
     a missing/wrong wildcard. The `*.lighthouse`→179.237.75.110 A record was correct all along (random
     labels synthesized fine). A stale `asuid.demo.lighthouse` TXT (Azure App Service custom-domain
     verification, leftover from an old Azure demo env) made `demo.lighthouse` an **empty non-terminal**,
     which per RFC 4592 **suppresses wildcard synthesis** for that exact name → NODATA on every type at
     `demo` while siblings resolved. Deleting the TXT (SOA serial bumped, ~600s negative-cache wait) let
     `demo.lighthouse` fall through to the wildcard. Scratch Ingress `demo-wildcard-proof` (host
     demo.lighthouse, cluster-issuer letsencrypt-prod, backend `tenant-lpw-lighthouse-api:80`) → HTTP-01
     auto-issued → `curl --resolve` = HTTP/2 200, CN=demo.lighthouse, issuer Let's Encrypt prod, TLS 1.3;
     `lpw.lighthouse` still 200 (no regression). Scratch ingress+cert+secret deleted.
     ⚠️ DURABLE LESSON: a stale `asuid.<sub>` TXT (or any leftover descendant record) silently shadows a
     working DNS wildcard — check for empty-non-terminals before suspecting the wildcard itself.
   - ADO #5202 → Resolved (all 4/4 done=observable met; 2026-06-30).
6. ✅ **DELIVER S06 done (LIVE 2026-06-30)** — second-tenant-by-hand (#5207), the CC-1 tenancy-model
   de-risk. Tenant `acme` stood up BY HAND from Tenant Zero's records onto the UNCHANGED chart 0.1.4
   (no public-repo change). DISTILL: `slice-06-second-tenant.feature` (5 scenarios) + feature-delta
   `[REF]` blocks (Lighthouse repo, local). PRIVATE platform `29c6f94`: `gitops/tenants/acme/tenant.yaml`
   (id/subdomain acme, chartVersion 0.1.4, **oidcEnabled:false** — present-but-false so the
   missingkey=error ApplicationSet guard renders empty) + `gitops/tenant-secrets/acme/secrets.yaml`
   (DB-only ESO bundle, SecretStore role tenant-acme). OpenBao out-of-band: `secret/tenants/acme/db`
   (acme's OWN password, never printed) + policy/role `tenant-acme` mirroring lpw.
   - **ArgoCD reconcile gotcha**: after push the `tenants` AppSet kept generating **1** app — argocd
     **repo-server git cache** served the pre-acme commit; restarting it is out-of-scope (shared
     control-plane, classifier-blocked). It self-resolved on the next repo-server poll (~2 min) →
     "generated 2 applications" → tenant-acme Application fanned out. Don't restart; just wait the poll.
   - **LIVE-PROVEN (4/4 done=observable):** `acme.lighthouse.letpeople.work` HTTP/2 200 + Let's Encrypt
     cert (own ns/DB/ingress, cert auto-issued via the slice-05 wildcard, **no new DNS record**); secret
     isolation (`auth can-i get secrets -n tenant-lpw` as both acme SAs = **no**); DB-credential isolation
     (acme connstr → `tenant-acme-lighthouse-postgres`, password DISTINCT from lpw); Tenant Zero no
     regression (`lpw.…` still 200, cert + record untouched). tenant-acme + tenant-lpw both Synced/Healthy.
   - **CC-1 tenancy model HOLDS** — two isolated tenants coexist; the lpw↔acme record diff IS the
     slice-07 generator's parameter set. User verified 2026-06-30. ADO **#5207 → Closed**.
   - ✅ **acme TORN DOWN post-verification (2026-06-30)** — learning banked, demo tenant removed.
     Private `f1952c8` rm'd `tenants/acme/` + `tenant-secrets/acme/` → ArgoCD pruned the tenant-acme
     Application + workloads (appset back to "generated 1 applications"); namespace `tenant-acme`
     deleted (ArgoCD does NOT prune CreateNamespace=true namespaces → delete by hand). OpenBao acme
     kv/policy/role deleted out-of-band. Tenant Zero (lpw) untouched, still HTTP/2 200 + Healthy.
   - ⚠️ OpenBao seed + private push were classifier-gated (secret-store write / shared-cluster); user
     authorized both. NetworkPolicy packet-isolation + per-tenant backups = later slices.
   - **Public Lighthouse pushed** (`685f5736`): slice-06 DISTILL spec + DELIVER record.
7. ✅ **DELIVER S07 done (LIVE 2026-06-30)** — automated-provisioning (#5376), the SaaS payoff. FULL
   ADR-092 one-record generator, all PRIVATE-repo GitOps + CI (no public chart change). DISTILL:
   `slice-07-automated-provisioning.feature` (9 scenarios) + feature-delta [REF] (Lighthouse, local).
   - PRIVATE platform: NEW `gitops/_charts/tenant-runtime` chart (tracked Namespace + plan ResourceQuota
     + LimitRange + 5 default-deny NetworkPolicies + ESO SecretStore/ExternalSecret(s); 12/12 helm-unittest)
     `c3ec493`; NEW `tenants-runtime` ApplicationSet gated by post-selector `runtime: enabled`; chart
     appset drops CreateNamespace + `hasKey` subdomain→id/oidc defaults; `scripts/validate-tenants.sh` +
     `.github/workflows/validate-tenants.yml` (first CI). riverbank `59781e7`→deprovision `76e37c1`; TZ
     migrate `c732396`.
   - **LIVE-PROVEN:** riverbank from ONE record → `riverbank.lighthouse` HTTP/2 200 + LE cert, ESO synced,
     **NetworkPolicy ENFORCES** (probe: own DB reachable, cross-ns lpw DB BLOCKED via Cilium). Remove
     record → both apps + the **tracked namespace** pruned, zero orphans cluster-wide (closes slice-06's
     ns-orphan gap). Tenant Zero dogfooded onto the generator (tenant-lpw + tenant-lpw-runtime
     Synced/Healthy, tenant-secrets app retired) — **API pod never restarted, 200 throughout**.
   - ⚠️ DURABLE: **orphan-then-adopt scare** — removing an ArgoCD app's `resources-finalizer` does NOT
     stick (controller re-adds it) → prune cascaded → ESO `Owner` deleted lpw's Secrets. Running pod kept
     its env (no downtime) but recovery was a fast manual `helm template -s templates/secrets.yaml | kubectl
     apply`. To truly orphan: annotate resources `argocd.argoproj.io/sync-options: Delete=false` BEFORE
     removing the app, OR migrate by editing the managing app's source rather than deleting it.
   - ⚠️ Recurring **repo-server git-cache ~2min lag** after every push before AppSets regenerate — don't
     restart the shared repo-server (out of scope); just wait the poll. ADO **#5376 → Resolved** (PENDING confirm).
8. Optional: substrate.probe (NetworkPolicy/LB/StorageClass, ADR-088). Tofu state local→Infomaniak S3
   backend = **ADO #5374** (child of 5306), scheduled for full-epic wrap-up.
9. **DISTILL S08-S11 per-slice** as DELIVER reaches them; S12 deferred.
   - ✅ **DISTILL S08 fleet-upgrade done (2026-06-30)** — `tests/platform/epic-5306/acceptance/slice-08-fleet-upgrade.feature`
     (10 scenarios: 3 `@in-memory` incl. reuse of epic-5305 `ExpandOnlyMigrationGuard` + helm single-source-version
     render; 7 `@requires_external` canary→promote→revert + failed-canary-not-promoted + partial-fleet + transient-retry)
     + feature-delta DISTILL S08 [REF] section. Reconciliation PASSED (ADR-093 canary/promote/expand-only/git-revert;
     ADR-086). Chart UNCHANGED (image tag already → `Chart.appVersion`, ADR-083).
     **Sentinel reviewed (conditionally_approved, 1 blocker + 3 high — ALL applied)**: declarative permanent-canary,
     atomic Then split, 4/10 `@error` (40%, +partial-fleet +transient-retry), zero-dropped reframed to observable.
     **NEXT for S08 = DELIVER**: private `tenants` ApplicationSet grows `canaryVersion` (Tenant Zero) +
     `promotedVersion` (fleet) staged params; release workflow wires `ExpandOnlyMigrationGuard` as a
     tenant-rollout gate; roll on Tenant Zero. S09 fleet-observability is the next DISTILL.

## Tooling note
OpenTofu v1.12.3 installed to ~/.local/bin (was absent); allowlisted via `lean-ctx allow tofu`.
helm/kubectl/kind/az already present. tflint NOT installed (used tofu fmt/validate instead).
