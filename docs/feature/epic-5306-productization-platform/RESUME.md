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
   - ✅ **DELIVER S08 done (LIVE 2026-06-30)** — automated upgrade (#5205), ADR-093. PRIVATE-repo GitOps only
     (public chart UNCHANGED; image tag already → Chart.appVersion). `tenants` ApplicationSet generator → **matrix**
     folding records (git) with one fleet `promotedVersion` (list); `targetRevision` hasKey-guarded so a record's
     `chartVersion` is its CANARY override, else it inherits `promotedVersion`. lpw dropped its pin → inherits 0.1.4
     (render-unchanged). Expand-only pre-flight = existing epic-5305 `ExpandOnlyMigrationGuard` (fails dotnet test on
     Drop/Rename) — already the release gate.
     Private commits: 1987835 mechanism → **7c72e3f FIX** (single-quote targetRevision; double-quote broke appset
     unmarshal → 0 apps, TZ preserved — see [[project_argocd_gotemplate_scalar_single_quote]]) → 89c095c provision
     canarytest@0.1.3 → 5f0c00b promote (drop override→0.1.4) → 7cad97f revert (rollback→0.1.3) → 684c36b teardown.
     **LIVE-PROVEN (throwaway-tenant path, user-chosen):** no-op (lpw 0.1.4 via promotedVersion, 200); canarytest
     0.1.3 serves 200; PROMOTE rolled 0.1.3→0.1.4 (deployRev 1→2, reloader pod-anno appeared = real chart roll),
     200 mid-roll; git-revert ROLLBACK 0.1.4→0.1.3 (deployRev→3, anno gone), 200; TEARDOWN pruned both apps + ns,
     zero orphans. **Tenant Zero 0.1.4/Healthy/200 throughout, never version-moved.** OpenBao canarytest kv/policy/role
     seeded + cleaned out-of-band (user). [SUPERSEDED — substrate only; #5205 reopened + rescoped into 08a/08b/08c.]
   - ✅ **DISTILL S08a/08b/08c done** — `tests/platform/epic-5306/acceptance/slice-08a-renovate-merge-only.feature`
     (+08b ordered-upgrade-smoketest, +08c rollback-drill). DESIGN ADR-094..097 (ADR-095 committed public).
   - ✅ **DELIVER S08a done — in-repo + CI GREEN (2026-06-30)** — merge-only release via Renovate (#5205,
     ADR-094/097). PRIVATE platform `4a7fa91` (public chart UNCHANGED): `renovate.json` (Mend App; 2 custom
     managers over the published `lighthouse` helm datasource — lpw `chartVersion` automerge **ON**, fleet
     `promotedVersion` automerge **OFF** — + argocd manager watching cert-manager/external-secrets/openbao/
     reloader/ingress-nginx, no automerge); re-added the lpw `chartVersion: "0.1.4"` canary anchor
     (==promotedVersion → render-unchanged); `scripts/validate-renovate-policy.sh` (jq assertion = executable
     form of the @in-memory US-08a-1/2/3); `validate-tenants.yml` +renovate-config-validator +policy check,
     triggers broadened.
   - ✅ **PLATFORM BUILD FIXED (red since slice-07)** — same commit pins helm-unittest **v1.0.3** so its
     plugin.yaml loads on CI helm v3.16.2 (v1.1.x `platformHooks` → `unknown command unittest` → exit 1; see
     [[project_epic5306_k8s_productization_design]]). `validate-tenants` run **28476255400 GREEN** (all 8 steps).
     ⚠️ CI renovate-config-validator must run renovate **latest** (43+) — `managerFilePatterns` is rejected by
     cached old 37.x; clean CI runners pull latest so it passes (local stale-cache trap only).
   - ⏳ **S08a @requires_external = operator one-time setup** (then live scenarios go live): install the **Mend
     Renovate App** on `LetPeopleWork/lighthouse-platform` + mark `validate-tenants` a **required status check**
     (branch protection on `main`) so the TZ auto-canary has its gate. ADO **#5205 stays Active** (08b/08c pending).
   - ▶ **NEXT: DELIVER S08b** (ordered-upgrade PostSync smoke-test Job + GitHub-issue alert, ADR-096 — needs a
     GitHub PAT in OpenBao), then **S08c** broken-image rollback drill.

## ✅ #5205 CLOSED — Resolved 2026-07-01 (supersedes the SLEEP HANDOFF below)

**08a/08b/08c all delivered + verified; ADO #5205 → Resolved.**
- **ESO incident CLEARED (not the predicted stale cache).** The batched Renovate PRs had taken ESO
  0.20.4→**2.7.0** (dropped `v1beta1`) + openbao 0.16.1→0.28.4. v2.7.0 controller CrashLoopBackOff'd — the
  big `secretstores`/`clustersecretstores` CRDs couldn't patch to v1 (client-side-apply `last-applied`
  annotation >262144B) so they stayed v1beta1-stored while the binary that converts them was gone.
  Fix-forward: revert to 0.20.4 → `conversion.strategy: None` relabel (v1beta1→v1 is schema-compatible) +
  re-encode SecretStores to v1 + prune CRD `storedVersions` to `[v1]` → re-upgrade to 2.7.0 with
  `ServerSideApply=true`. openbao STS immutable-field fixed via orphan-delete (OnDelete + selector re-adopt,
  no unseal). Private `76c45e3`,`fd4573b`,`040ddc5`. Tenant Zero stayed 200 throughout.
- **08b VERIFIED** after two latent smoke-test bugs found on first live run + fixed: (1) kube-apiserver
  egress blocked by the tenant default-deny (apiserver is an EXTERNAL non-443 endpoint on Infomaniak KaaS,
  Cilium drops it) → new smoke-test-scoped **CiliumNetworkPolicy** `toEntities: [kube-apiserver]` + bounded
  kubectl/curl timeouts (`a363b46`); (2) `alpine/k8s` kubectl doesn't auto-select in-cluster config
  (localhost:8080) → point it at the apiserver explicitly from the mounted SA (`c40905b`). chart
  `tenant-runtime` 0.1.2→0.1.4, 19/19 unittest.
- **08c @error PROVEN** — driltest (workload chart 9.9.9) → smoke-test opened the GitHub alert + exited
  non-zero (issue #15, closed) → torn down (`80c32b1`,`a19f9ca`). Alert proven via a controlled one-off Job
  because ArgoCD health-gates PostSync behind the tenant's DB ExternalSecret (needs an OpenBao seed the
  drill deliberately skips); zero-orphan prune already proven in slice-07.
- Records: feature-delta DELIVER S08a/08b/08c (public `a1242423`). Closed a false-alert issue #14 (pre-fix
  smoke-test misfire on healthy lpw).
- **Minor follow-up (cosmetic):** the smoke-test health code renders `000000` (curl `|| echo 000` doubles
  with `%{http_code}`) in the alert title — harmless, tidy on the next chart touch. openbao app shows a
  benign OutOfSync (agent-injector webhook caBundle self-injection, Healthy).

---

## SLEEP HANDOFF (2026-07-01) — [DONE — kept for the incident audit trail]

**#5205 status:** 08a ✅ DONE+live-proven · 08b ✅ code shipped (live happy-path proof BLOCKED on the ESO
incident below) · 08c ⬜ one drill left. Story NOT closed yet.

### S08a — DONE (auto-canary live-proven)
Renovate (Mend App) on `LetPeopleWork/lighthouse-platform`. `renovate.json`: 2 custom managers over the
published `lighthouse` helm datasource (lpw `chartVersion` automerge ON, fleet `promotedVersion` automerge
OFF) + argocd manager for the 5 platform components (no automerge). `mode:full` (Mend defaults silent).
Drill: dropped lpw anchor 0.1.4→0.1.3 → Renovate raised canary PR#1 → `validate-tenants` gated green →
**auto-merged hands-off** (`edc6d98`) → ArgoCD rolled TZ 0.1.3→0.1.4, **200 throughout**. Anchor DRILL
comment cleaned (`0300a50`). NOTE: free private-org plan ⇒ no GitHub branch protection/rulesets (403);
auto-merge gates via Renovate `ignoreTests:false` wait-for-tests (accepted). `config:recommended` also
pulled in github-actions/terraform(openstack)/helm managers ⇒ extra no-automerge PRs (noise; scope later).
Fleet manual-promote PR NOT demoed (only lpw exists; deferred to final e2e per user).

### ⚠️ ESO INCIDENT (must clear first tomorrow — blocks 08b/08c live)
User merged Renovate component PRs: cert-manager v1.16.2→v1.20.3 (fine) and **external-secrets
0.10.7→0.20.4** which **DROPPED the `external-secrets.io/v1beta1` API** (cluster now serves `v1` only,
storage=v1). FIXED in git: migrated all 5 ESO manifests v1beta1→v1 (`efc0490`/`df9ff38`) + chart
bump 0.1.1→0.1.2 to bust repo-server cache (`beda74e`). **Platform stayed UP** (lpw 200 throughout;
`lighthouse-db`/`oidc` ExternalSecrets SecretSynced True as v1). BUT `tenant-lpw-runtime` + `platform`
argo apps still show **OutOfSync** citing the old v1beta1 — this is ArgoCD's stale **cluster discovery /
REST-mapper cache** for the removed CRD version, NOT a data problem. `github-issues-token` Secret not yet
materialised because the sync can't complete.
**TOMORROW STEP 1 — clear it:** `KUBECONFIG=~/.kube/lpw-substrate.yaml`; hard-refresh
(`kubectl annotate app tenant-lpw-runtime platform -n argocd argocd.argoproj.io/refresh=hard --overwrite`);
if still OutOfSync with v1beta1 error after ~10min, the argocd-application-controller discovery cache is
stale → restart it (`kubectl rollout restart deploy argocd-application-controller -n argocd` — or statefulset;
shared control-plane, classifier-gated, get user OK). Then verify: both apps Synced/Healthy;
`kubectl get externalsecrets.v1.external-secrets.io -A` all True; `kubectl get secret github-issues-token -n
tenant-lpw` exists.

### S08b — code shipped (`16a0ad9`), VERIFY happy-path after recovery
chart `tenant-runtime` 0.1.2: `smoke-test-job.yaml` (PostSync hook; **rollout-based version attribution**
— Deployment image tag==expected + rollout-complete + `/health/ready` 200, because `/api/v1/version` is
**OIDC-gated→401**, only `/health/*` anonymous), `smoke-test-rbac.yaml` (SA+Role get deployments),
`smoke-test-secret.yaml` (github-issues-token via ClusterSecretStore `platform-store`). values `smokeTest`
(off by default). `applicationset-runtime.yaml` folded onto `promotedAppVersion: "26.6.21.1"` matrix
(version-stamp). `platform/external-secrets-platform-store.yaml` (ClusterSecretStore + SA
platform-token-reader, ns external-secrets). 17/17 helm-unittest. OpenBao prereqs SEEDED by user: policy
`platform-read` + k8s role `platform`(SA external-secrets/platform-token-reader) + PAT raw at
`secret/platform/github-issues-token` key `token`. **VERIFY after recovery:** the PostSync smoke-test on
tenant-lpw PASSES (lpw on 26.6.21.1, healthy → exit 0 → hook auto-deletes; no GitHub issue opened).

### S08c — fast broken-image-rollback drill (DO after 08b verified)
Throwaway tenant, NO OpenBao DB seed needed (the breakage IS the missing workload). Steps (fish):
1. create `gitops/tenants/driltest/tenant.yaml`: `id: driltest`, `subdomain: driltest`, `plan: standard`,
   `runtime: enabled`, `chartVersion: "9.9.9"` (non-existent chart → workload never deploys).
2. commit + push. ArgoCD provisions driltest runtime (ns/quota/netpol + github-issues-token ESO via
   platform-store); workload app errors (chart 9.9.9 not found) → no deploy → smoke-test Job waits
   maxAttempts (30×10s=5min) → **opens GitHub issue** "tenant driltest unhealthy after upgrade to
   26.6.21.1 (health 000)" (= @error US-08b-2 alert proven).
3. note detect time; ROLLBACK = `git rm -r gitops/tenants/driltest`, commit, push → ArgoCD prunes the
   app + namespace (zero orphans). note recover time; close the issue.
4. record runbook + detect→recover in feature-delta DELIVER S08c [REF].

### After 08c: close-out
- feature-delta `[REF]` DELIVER blocks for S08a/08b/08c; supersede the slice-08 substrate banner.
- ADO **#5205 → Resolved/Closed** (08a/08b/08c all delivered).
- Optional cleanups: scope `renovate.json` to drop github-actions/terraform/helm manager noise; revisit
  the open component PRs (#2 helm, dashboard #3); the `helm` binary PR + cert-manager already merged.
- Final full e2e (real release → canary auto + fleet MANUAL promote PR + smoke-test + broken-image
  rollback) deferred per user — run at epic wrap-up alongside ADO #5374 (tofu state→S3).

**Public docs (this repo) committed LOCAL, UNPUSHED:** `e8cf158d` (slice-08a record) + this handoff edit —
push both when convenient (platform-repo side already pushed).

## Tooling note
OpenTofu v1.12.3 installed to ~/.local/bin (was absent); allowlisted via `lean-ctx allow tofu`.
helm/kubectl/kind/az already present. tflint NOT installed (used tofu fmt/validate instead).

---

## ✅ S09 fleet-observability (#5206) — DISTILL + DELIVER + LIVE-PROVEN 2026-07-01 → #5206 RESOLVED

**ADO #5206 = Resolved** (live done=observable proven on Tenant Zero; induced-fault firing drill deferred per user).

### Live-proof close-out (2026-07-01) — two latent config bugs fixed live, then GREEN
- **Bug 1 — PodMonitor `namespaceSelector.matchLabels` invalid for prometheus-operator** (private
  `e70c940`, fleet-monitoring 0.1.0→0.1.1). Operator `NamespaceSelector` = `any`/`matchNames` ONLY (not a
  label selector); CRD structural schema pruned `matchLabels`→`{}` = "own namespace (monitoring)" → 0
  targets + perpetual ArgoCD OutOfSync. Fix: `namespaceSelector: {any: true}` (podSelector bounds it).
  +regression unittest, 14/14.
- **Bug 2 — default-deny blocked the scrape** (private `934beda`, tenant-runtime 0.1.4→0.1.5). New
  `allow-metrics-from-monitoring` NetworkPolicy: ingress from ns `monitoring` → api-component pods only,
  metrics port. 20/20 unittest.
- **Grafana OOM→503 fixed** (private `a338b0e`): 200Mi stack-default → 384Mi/160Mi (every other monitoring
  pod was already sized in the #5206 capacity pass; grafana was missed).
- **GREEN:** `up{tenant="lpw"}=1` healthy; metric name `http_server_request_duration_seconds` matches (no
  tune); recording series `tenant:http_requests:rate5m`/`tenant:http_latency:p95_5m`/`fleet:tenants_degraded:count`;
  4 alerts loaded+healthy; cardinality bounded (2211 clean series/tenant; only the 5 synthetic scrape-meta
  series carry pod/instance — unavoidable, bounded). Public docs record: feature-delta DELIVER S09 LIVE PROOF block.
- **Deferred (user):** induced-fault drill proving `TenantDegraded` *fires* (rule loaded, firing unproven);
  Grafana ESO-admin + SSO + dashboard hardening.

### (historical) NEXT: S09 @requires_external LIVE PROOF — DONE, see close-out above

- **DISTILL**: `tests/platform/epic-5306/acceptance/slice-09-fleet-observability.feature` (12 scenarios,
  5 @error=42%, @US-09; @in-memory×7 + @real-io @requires_external×5). Reconciliation PASSED (ADR-090).
  Sentinel reviewed; 2 findings reconciled as epic-convention false-positives (slice-number @US tag; no
  @contract-shape corpus-wide — see [[project_argocd_gotemplate_scalar_single_quote]] sibling epic notes).
  feature-delta DISTILL + DELIVER S09 [REF] blocks. Public `046cfe76`.
- **DELIVER (@in-memory, GREEN)** — PRIVATE `af7e961` (public chart byte-unchanged; +1 test-only
  standalone-gate assertion Telemetry__Enabled=false):
  - `gitops/_charts/fleet-monitoring` NEW: PodMonitor (one bounded `tenant` relabel from namespace +
    unbounded labeldrop at scrape, ADR-090); recording rules (tenant:* + fleet:* pre-aggregation); alert
    rules TenantDegraded/TenantDown/FleetUnhealthy/FleetMetricCardinalityBudgetExceeded; Grafana
    fleet-dashboard ConfigMap (sidecar-discovered, reads pre-aggregated). 13 helm-unittests.
  - `gitops/platform/monitoring.yaml` NEW (kube-prometheus-stack, selectors opened, ServerSideApply);
    `gitops/platform/fleet-monitoring.yaml` NEW (overlay app, sync-wave 1).
  - `applicationset.yaml`: `telemetry.enabled: true` per HOSTED tenant (hosted-only → D0 gate holds).
  - `validate-tenants.yml`: lint+unittest fleet-monitoring. **Private CI GREEN** (run 28504171391).

### ▶ NEXT: S09 @requires_external LIVE PROOF (on lpw-substrate; needs user at cluster)
1. Confirm ArgoCD synced `monitoring` (wave 0) then `fleet-monitoring` (wave 1); both Synced/Healthy.
   **PIN the exact kube-prometheus-stack patch** in `gitops/platform/monitoring.yaml` — I placeholdered
   `65.5.1` (DESIGN intent 65.x). If that tag 404s, Renovate/the operator sets the available patch.
2. Tenant Zero re-syncs on chart 0.1.4 with `telemetry.enabled` → `/metrics` exposed; confirm Prometheus
   scrapes `tenant="lpw"` (the PodMonitor podSelector is `app.kubernetes.io/name: lighthouse` +
   namespaceSelector `app.kubernetes.io/part-of: lighthouse-saas` — **verify the live pod label matches**;
   tune the selector if the chart labels pods differently).
3. `kubectl -n monitoring port-forward svc/<kps>-grafana 3000` → open **Lighthouse Fleet** dashboard →
   per-tenant tiles + fleet summary (dogfood: lpw first instance).
4. Induce a fault on a throwaway tenant → tile flags + `TenantDegraded` fires; confirm cardinality bounded
   (only `tenant`). Then record DELIVER S09 [REF] live done=observable + transition **#5206 → Resolved**.
- Grafana ingress + ESO-sourced admin + SSO = later hardening (internal via port-forward this slice).
- OTel metric name assumed `http_server_request_duration_seconds` (values `metricName`) — **tune vs the
  live /metrics output** if ASP.NET/OTel emits a different name; recording rules key off it.
