# RE-DISCUSS — Epic 5306 productization-platform completion (2026-07-02)

**Wave**: DISCUSS (re-discussion / sanity-check, not a fresh feature). **PO**: Luna.
**Trigger**: user sanity-check on "what's pending" against four newly-articulated goals, after S01–S10
delivered live. **Verified against**: public repo commits through `cfe0bc0b` + the freshly-cloned
private `lighthouse-platform` repo (`C:\Users\benja\repos\lighthouse-platform`) tenant record, generator
ApplicationSet, `.gitignore`, and substrate `.tf` (no remote-backend block present).

---

## The four goals (user, 2026-07-02)

1. **Everything controlled by git** — so the operator can work from multiple machines and onboard other
   people to infra management (no local-only configs).
2. **Multi-provider support** — Infomaniak today; Hetzner / Oracle Cloud potentially in future.
3. **Documented in the `lighthouse-platform` repo** — a high-level top README + capability sub-pages,
   learning-oriented ("here's how ArgoCD does X and how I'd change it"; "here's how backups work and how
   to restore now"), for a tech-literate non-expert. Covers: OpenTofu, ArgoCD, secrets, tenant mgmt,
   wildcard DNS, auto-updates, observability, backup/recovery, and the customer-onboarding workflow.
4. **Customer-onboarding workflow** — decide host, decide setup (MCP server? auth? URL?), then provision
   with a small change → commit → push → the rest handled automagically. Works AND is documented.

## Decisions taken this session

- **[RD-1] Multi-provider = DOCUMENT-THE-PATH ONLY** (delivery of a 2nd provider stays deferred, S12).
  The readiness bar the user set: **be ready = everything reproducible from git, no local-only configs.**
  Re-target the S12 *reference* provider from AWS EKS → **Hetzner / Oracle Cloud** in the docs. Actual
  stand-up of a 2nd provider remains pull-on-demand (real customer/cost/region driver).
- **[RD-2] Onboarding knobs — CONFIRMED 2026-07-02.** Genuine *choices* per customer: **provider/placement**,
  **MCP server (y/n)**, **URL/subdomain**. **Auth is MANDATORY** for every instance — no `none` option; the
  decision is *which IdP config* (issuer/clientId), not *whether*. **Licence is MANDATORY** for every
  instance — every tenant must have a valid (premium) licence seeded (resolves the Tenant-Zero
  `AuthMode.Blocked` gap); it is a required onboarding *step*, not an optional field. `backupEnabled`
  stays a field (default on). Future extensions (not fields yet): BYO-domain, owner/contact + region.
- **[RD-3] Documentation** = ONE structured-backfill story (top README refresh + all capability sub-pages
  authored as a coherent set), backfilling S01–S11 doc debt. Not per-slice, not incremental.

## Verified current state (private repo, 2026-07-02)

- **Tenant record** (`gitops/tenants/lpw/tenant.yaml`) fields: `id`, `subdomain`, `plan`, `chartVersion`,
  `oidcEnabled`/`oidcIssuer`/`oidcClientId`, `runtime`. → URL/subdomain ✅ · auth ~partial (OIDC on/off) ·
  **MCP toggle ❌ absent** · **provider/placement ❌ absent** (single cluster).
- **Git-completeness gap CONFIRMED**: `.gitignore` excludes `*.tfstate`, `*.tfvars`, `clouds.yaml`,
  `kubeconfig`, `*.key`, `*.pem`; substrate `.tf` has **no remote-backend block** → tofu state is
  local-only. On a freshly-cloned machine the operator can edit git but **cannot `tofu apply` (no shared
  state) nor reach the cluster (no kubeconfig)** until a bootstrap path exists. This is #5374 + a runbook.
- **Docs skeleton exists but is thin + stale**: top `README.md` still says "slice-02 Next / slice-03
  walking-skeleton" while S01–S10 are live. Only `docs/infomaniak-setup.md`, `gitops/README.md`,
  `infra/substrate/README.md` exist.
- **Original 9-story scope is ~90% delivered + live-proven**; the only *original-scope* pending work is
  S10 backups live-proof + S11 restore-rehearsal DELIVER (Track D below).

---

## Wave: DISCUSS / [REF] New / refined user stories

### US-10 — Reproducible-from-git platform (operator onboarding, no local-only state)  *(Goal 1 + Goal 2 readiness)*
- **job_id**: `job-saas-operator-provision-substrate` (extended)
- **Problem**: platform state and operator credentials live only on Benjamin's original machine; a second
  machine or a second operator cannot safely operate the platform.
### Elevator Pitch
Before: a freshly-cloned machine can edit git but cannot `tofu apply` (no shared state) or reach the cluster.
After: run the documented bootstrap (`tofu init` against the S3 backend + regenerate `clouds.yaml`/kubeconfig from the store) → sees `tofu plan` show *no changes* and `kubectl get nodes` list the live cluster.
Decision enabled: "Can a new machine / new operator safely operate the platform?" — yes, from git + the shared store.
- **AC-1**: tofu state lives in an Infomaniak S3 remote backend (backend block committed); two machines
  running `tofu plan` off the same state agree (no divergence). *(= #5374)*
- **AC-2**: an operator-bootstrap runbook regenerates every CC-3-gitignored artifact (Infomaniak token →
  `clouds.yaml`/tfvars, kubeconfig, OpenBao unseal keys, ArgoCD deploy key) from a documented source; no
  secret is committed.
- **AC-3**: an audit confirms any local config that *could* safely live in git has been moved into git.
- **Done=observable**: on the notebook (2nd machine), following only the runbook + git, the operator
  reaches `tofu plan` = no-op and `kubectl get nodes` = live cluster.

### US-11 — Guided customer onboarding (decision workflow + record template + runbook)  *(Goal 4)*
- **job_id**: `job-saas-operator-onboard-tenant` (extended)
- **Problem**: provisioning is one-record automagic (S07 ✅) but the *decisions* (where to host, MCP
  server?, auth mode, URL, licence/backup tier) are not formalized on the record nor in a runbook.
### Elevator Pitch
Before: onboarding requires tacit knowledge of which record fields to set for each customer choice.
After: copy `tenants/_template/tenant.yaml`, answer the commented decisions, `git commit && push` → sees the tenant serving at its URL with the chosen MCP/auth setup, in minutes.
Decision enabled: "How do I set up *this* customer (host, MCP, auth, URL)?" — the template + runbook make every choice explicit and reviewable.
- **AC-1**: tenant-record schema exposes the decision set — add `mcpEnabled` (wires the chart's optional
  MCP workload) and a forward-looking `placement`/`provider` field; `backupEnabled` stays a field. Auth
  (OIDC) and a valid licence are MANDATORY for every tenant — no auth-off path, licence-seed is a required
  onboarding step (per RD-2).
- **AC-2**: a commented `tenants/_template/tenant.yaml` documents each decision inline.
- **AC-3**: an onboarding runbook walks *decide → copy template → fill → commit → push → verify* end-to-end.
- **AC-4**: `mcpEnabled: true` on a throwaway tenant deploys the MCP workload; `false` omits it — both reachable.
- **Done=observable**: a new tenant onboarded from the template alone (no tribal knowledge) serves at its
  URL with the chosen MCP + auth setup.

### US-12 — Platform operations documentation (learning-oriented)  *(Goal 3, one structured-backfill story)*
- **job_id**: `job-platform-operator-operate-and-learn` (new; operate/change/recover the platform)
- **Problem**: no operator-facing learning docs exist in the private repo; knowledge lives in
  `feature-delta.md` methodology artifacts + the operator's head.
### Elevator Pitch
Before: to change ArgoCD behaviour or restore a backup, the operator reverse-engineers it from manifests.
After: open `README.md` → follow the linked capability page → sees a plain-language explanation + the exact how-to ("this is how backups work; run this to restore now").
Decision enabled: "How does X work and how do I change / recover it?" — answerable from the repo docs alone.
- **AC-1**: top `README.md` refreshed to a truthful high-level overview (current, not slice-02-stale).
- **AC-2**: one learning-oriented sub-page per capability — substrate/OpenTofu, ArgoCD/GitOps, secrets
  (ESO+OpenBao), tenant mgmt/provisioning, wildcard DNS, auto-updates (Renovate), observability,
  backup/recovery (+restore runbook), **provider-addition how-to** (RD-1), operator bootstrap (US-10 AC-2),
  onboarding runbook (US-11 AC-3).
- **AC-3**: written for a tech-literate non-expert; deep detail may link out (Diataxis: explanation + how-to).
- **Done=observable**: a tech-literate reader who has never seen the platform can, from the docs alone,
  explain how ArgoCD reconciles a tenant and perform a restore.

---

## Wave: DISCUSS / [REF] Pending backlog — four tracks

| Track | Work | Maps to | ADO |
|---|---|---|---|
| **A — git-completeness / portability readiness** | A1 tofu remote state → Infomaniak S3 backend · A2 operator-bootstrap runbook + local-config audit | US-10, Goals 1+2 | A1 = **#5374 (exists, promote to blocker)**; A2 = new (or fold into #5374) |
| **B — onboarding decision workflow** | B1 extend tenant-record schema (`mcpEnabled`, `placement`, `backupEnabled`; auth+licence MANDATORY for all) + commented template + licence-seeding step · B2 onboarding runbook | US-11, Goal 4 | new story |
| **C — documentation backfill** | top README refresh + capability sub-pages (absorbs A2, B2, provider how-to) | US-12, Goal 3 | new story |
| **D — finish original core scope** | S10 backups live-proof · S11 restore-rehearsal DELIVER | #5208 (Active) | existing |

**Suggested sequence**: D can proceed independently (finishes the shipped mechanism). **A1 is the
multi-machine unblocker** (do early — it's what the notebook clone needs). Then B (schema + runbook). C
authored last as the coherent set (it documents A/B/D + all prior slices). Multi-provider *delivery*
(S12) stays deferred; its *documentation* is a C sub-page.

## Resolved (2026-07-02, user)
- **[A-1] ✅** Licence + auth are MANDATORY for every tenant (no opt-out path); `backupEnabled` stays a field.
- **[A-2] ✅** A2 (operator bootstrap) folds into **#5374** (not a separate story).
- **[A-3]** US-12's new `job-platform-operator-operate-and-learn` job to be added to `docs/product/jobs.yaml`.
- **Sequence ✅**: **C (docs) FIRST** (git-only, works from any machine). A1/#5374 runs from the machine
  holding the current local tfstate. ADO go-ahead GIVEN: promote #5374 to blocker + create B and C stories.
