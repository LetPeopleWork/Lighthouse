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
3. **DELIVER S03 (next)** — tenant-lpw is Unknown: chart fails-fast on missing DB password (ADR-082,
   proves slice-03 @error AC). Bring Tenant Zero up: hand-made Postgres/OIDC secret in tenant-lpw ns,
   confirm chart published at docs.lighthouse.letpeople.work/charts (v0.1.1), DNS for
   lpw.lighthouse.letpeople.work → ingress LB, cert. Completes the WS. NOTE: chart values reference
   secret — wire the hand-made secret name into the ApplicationSet helm values or chart.
   **DECIDED (2026-06-29):** add `postgresql.auth.existingSecret` to the PUBLIC chart (#5199) — chart
   reads password from a pre-existing k8s Secret via secretKeyRef, relax the ADR-082 render-time
   `required` when existingSecret set. DURABLE: slice-03 hand-creates that Secret; slice-04 ESO
   creates the SAME Secret from OpenBao — chart reference unchanged. Do NOT pass password as a helm
   value (CC-3 violation, ESO-incompatible). Chart change = secret.yaml + deployment secretKeyRef +
   values + values.schema.json + a render test. Then: ingress-nginx Application in platform/, DNS
   lpw.lighthouse.letpeople.work → ingress LB, cert-manager ClusterIssuer (Let's Encrypt).
4. Optional: substrate.probe (NetworkPolicy/LB/StorageClass, ADR-088); move tofu state to Infomaniak S3.
5. **DISTILL S04-S11 per-slice** as DELIVER reaches them; S12 deferred.

## Tooling note
OpenTofu v1.12.3 installed to ~/.local/bin (was absent); allowlisted via `lean-ctx allow tofu`.
helm/kubectl/kind/az already present. tflint NOT installed (used tofu fmt/validate instead).
