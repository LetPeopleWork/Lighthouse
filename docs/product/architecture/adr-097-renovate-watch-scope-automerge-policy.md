# ADR-097: Renovate Hosting = Mend Renovate GitHub App; Watch Scope = the Published Lighthouse Chart + Tracked Platform-Component Versions; Automerge ONLY the Tenant-Zero `chartVersion` Override, NEVER the Fleet `promotedVersion`

**Status**: **PROPOSED** (2026-06-30, Titan — PROPOSE mode, awaiting Benjamin)
**Date**: 2026-06-30
**Feature**: epic-5306-productization-platform (ADO Epic #5306, story #5205 RESCOPE slice-08a, US-08a-1/2/3) — resolves open question **O-08-4** and is the policy through which ADR-094 (auto-canary) is expressed
**Decider**: Benjamin (product owner) + Titan (System Designer)
**Relationship to prior work**: drives the slice-08 substrate's `chartVersion` (canary) + `promotedVersion` (fleet) knobs via PRs; gated by the slice-07 `validate-tenants` CI check; the Helm chart is published at `https://docs.lighthouse.letpeople.work/charts` (ADR-083).

---

## Context

US-08a wants new releases surfaced as **reviewable PRs** on the private gitops repo, with **Tenant Zero auto-canaried hands-off** and the **fleet promoted by one merge**. O-08-4 asks: which Renovate hosting, and exactly what is in scope. Hosting is **user-locked to the Mend Renovate GitHub App** (hosted; the operator installs the app + grants repo access — a one-time operator action). This ADR fixes the **watch scope** and the **automerge policy** (the `renovate.json` shape).

## Decision

**Hosting**: the **Mend Renovate GitHub App** (hosted) on `LetPeopleWork/lighthouse-platform`. No self-hosted CI cron (O-08-4 locked). Operator one-time setup: install the app, grant repo access, ensure branch protection on `main` requires the `validate-tenants` status check (so automerge gates on it — ADR-094).

**Watch scope** (two classes, kept as distinct PRs so they are reviewed independently — US-08a-1 example 2):
1. **The Lighthouse application** — the published `lighthouse` Helm chart, datasource `helm`, registry `https://docs.lighthouse.letpeople.work/charts`. Tracked in **two** places via custom managers:
   - `gitops/tenants/lpw/tenant.yaml` → the `chartVersion` **canary anchor** (Tenant Zero).
   - `gitops/tenants/_generator/applicationset.yaml` → the fleet `promotedVersion`.
2. **Tracked platform/chart-component versions** — the pinned ArgoCD Application chart/image versions the platform itself runs: CNPG, cert-manager, ingress-nginx, External Secrets Operator, external-dns, reloader (and the OpenBao/kube-prometheus charts as they land). Each raises **its own** PR, **no automerge** (operator reviews infra bumps).

**Automerge policy** (the crux of ADR-094):

| Target | Manager match | `automerge` | Rationale |
|--------|---------------|-------------|-----------|
| Tenant-Zero `chartVersion` (`gitops/tenants/lpw/tenant.yaml`) | the lpw canary anchor only | **`true`** | hands-off auto-canary — TZ takes every release first with zero operator action (US-08a-2) |
| Fleet `promotedVersion` (`applicationset.yaml`) | the fleet default only | **`false`** | the one-click human promote gate; nothing auto-promotes off a failed canary (US-08a-3, guardrail = 0) |
| Platform components (CNPG/cert-manager/ingress-nginx/ESO/external-dns/reloader/…) | their ArgoCD Application sources | **`false`** | infra bumps are operator-reviewed |

**`renovate.json` shape** (intent; DELIVER pins exact regex/syntax):
- `customManagers` (regex) for `chartVersion:` in the lpw record and `promotedVersion:` in the appset, each with a `# renovate: datasource=helm depName=lighthouse registryUrl=…` marker comment; `packageRules` apply the automerge split above (matched by `paths`/`matchFileNames`).
- `automergeType: pr`, `automergeStrategy: squash`; automerge waits on required status checks (the `validate-tenants` workflow) → ADR-094's gate.
- A `schedule` tightened from the Mend default (~hourly) toward KPI-2 (≤30 min publish→canary) **only if** the first measured roll shows the default too slow — start with the default, measure, adjust.
- Distinct PRs per dependency (`separateMinorPatch` / default grouping off for the app vs components) so the app bump and a component bump never share a PR (US-08a-1 example 2).
- No-op weeks raise no PR (Renovate default — US-08a-1 example 3 / no false churn).

## Consequences

- **Positive**: releases stop being self-discovered chores — 100% of new chart versions + tracked component versions raise a reviewable PR within one scan interval (KPI-3); the automerge split is the entire mechanism behind "TZ hands-off, fleet one-click"; no infrastructure to host (Mend App); the `validate-tenants` check is the safety gate on automerge.
- **Negative / cost**: dependence on a hosted third-party bot (acceptable — it only opens PRs; merges are gated by our CI + branch protection); a fine-grained list of tracked component versions must be maintained (drift risk if a new component is added without a manager — mitigated by optionally adding a `renovate-config-validator` lint to `validate-tenants.yml`); the ~hourly default scan caps KPI-2 unless tightened.
- **Standalone gate**: untouched — Renovate config + a hosted bot on the private repo; public chart byte-unchanged.

### Earned-Trust note

Automerge is gated on the `validate-tenants` probe (uniqueness/naming/lint) so a malformed record cannot auto-canary; the auto-merged canary is then probed live by the ADR-096 smoke-test — the chain never trusts a version it has not validated (pre-merge) and demonstrated healthy (post-sync). Self-application: a `renovate-config-validator` step keeps the watch-scope config itself honest after edits.

## Open questions for DELIVER

- Exact custom-manager regex + `matchFileNames` for the two single-line knobs (validate the lpw automerge truly cannot match the fleet `promotedVersion`).
- The full pinned component list actually in scope at DELIVER (CNPG/cert-manager/ingress-nginx/ESO/external-dns/reloader confirmed; OpenBao/kube-prometheus as they are added).
- Whether to add `renovate-config-validator` to `validate-tenants.yml` (recommended — keeps the config honest).
- Final `schedule` after measuring publish→canary latency against KPI-2.
