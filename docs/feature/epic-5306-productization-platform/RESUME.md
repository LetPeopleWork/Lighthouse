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
  Billing against CHF 300 credit. ⚠️ INFOMANIAK_TOKEN exposed in chat transcript — ROTATE.

## Next
1. **Rotate INFOMANIAK_TOKEN** (exposed in transcript). Re-export new one for any further tofu/API.
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
4. Optional: substrate.probe (NetworkPolicy/LB/StorageClass, ADR-088); move tofu state to Infomaniak S3.
5. **DISTILL S04-S11 per-slice** as DELIVER reaches them; S12 deferred.

## Tooling note
OpenTofu v1.12.3 installed to ~/.local/bin (was absent); allowlisted via `lean-ctx allow tofu`.
helm/kubectl/kind/az already present. tflint NOT installed (used tofu fmt/validate instead).
