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
- ✅ DELIVER S01 substrate (#5320): OpenTofu module in private repo, pushed to main. `@in-memory`
  band GREEN (`tofu fmt` + `tofu validate` vs real openstack provider v2.1). Dual adapter
  (managed-k8s Magnum primary + k3s-compute fallback) behind CC-4 contract per ADR-088.

## Next
1. **S01 @requires_external** — operator runs `tofu apply`/`destroy` with Infomaniak creds
   (OS_* env). Confirm Infomaniak managed-k8s resource shape vs the Magnum modelling (residual O-1)
   before first apply — boundary is contained to `modules/managed-k8s/`.
2. **DISTILL S02 is done** (slice-02-gitops.feature exists). **DELIVER S02** — `gitops/bootstrap/`
   + `gitops/platform/` ArgoCD app-of-apps (ADR-086) in the private repo; greens slice-02 @in-memory
   (kubeconform manifest lint) then @requires_external reconcile/drift.
3. **DELIVER S03** — `gitops/tenants/lpw/` Tenant Zero record; completes the WS line.
4. **DISTILL S04-S11 per-slice** as DELIVER reaches them; S12 deferred.

## Tooling note
OpenTofu v1.12.3 installed to ~/.local/bin (was absent); allowlisted via `lean-ctx allow tofu`.
helm/kubectl/kind/az already present. tflint NOT installed (used tofu fmt/validate instead).
