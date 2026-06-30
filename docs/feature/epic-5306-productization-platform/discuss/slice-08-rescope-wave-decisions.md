<!-- markdownlint-disable MD024 -->
# DISCUSS — slice-08 RESCOPE (ADO #5205 Automated Upgrades)

> **Wave**: DISCUSS (brownfield rescope of a single slice). **Mode**: lean (Tier-1). **Date**: 2026-06-30.
> **Analyst**: Luna. **Feature**: epic-5306-productization-platform. **Persona**: platform-operator
> (LPW SaaS-operator flavour). **Operator in examples**: Benjamin (LPW maintainer/operator).
>
> This is a RESCOPE, not a fresh feature. The epic's DISCUSS/DESIGN/journey/jobs already exist; this
> document refines ONLY the fleet-upgrade slice (#5205) against its REAL acceptance text. JTBD was NOT
> re-run — every story traces to the EXISTING job `job-saas-operator-upgrade-all-tenants-safely`
> (`docs/product/jobs.yaml`).

## Decisions carried in from the orchestrator (not re-litigated)

| # | Decision | Value |
|---|---|---|
| D1 | Feature type | Infrastructure (operator-facing GitOps/CI automation) — but operator-VISIBLE, so stories are NOT `@infrastructure`; each has a real operator entry point (the Renovate PR, the git merge, `argocd app list`, the alert channel). |
| D2 | Walking skeleton | No — the multi-tenant substrate (slices 01–08) already ships LIVE. |
| D3 | UX research depth | Lightweight (operator happy path = review Tenant Zero + merge one PR). |
| D4 | JTBD | Pre-validated; trace to `job-saas-operator-upgrade-all-tenants-safely`; refine its `functional` dimension via back-propagation (below), do NOT create a new job. |

## What already shipped (the SUBSTRATE — do not re-spec)

DELIVERED slice-08 (LIVE 2026-06-30, ADR-093). The private `tenants` ApplicationSet (repo
`LetPeopleWork/lighthouse-platform`) is a **matrix** folding each tenant record (git generator) with one
fleet-wide `promotedVersion` (list generator); a record's own `chartVersion` is a **canary override**, else
it inherits `promotedVersion`. Zero-downtime rolling update + graceful drain + expand-only-migration guard
(epic-5305 `ExpandOnlyMigrationGuard`, fails `dotnet test` on Drop/Rename) + git-revert rollback are all
proven LIVE on a throwaway tenant (`canarytest`); Tenant Zero (`lpw`, real LPW production) was render-unchanged.
Public chart UNCHANGED (image tag already defaults to `Chart.appVersion`, ADR-083).

## The gap (why #5205 went back to Active)

The substrate ships the staged-rollout MECHANISM but NOT the merge-only AUTOMATION the real #5205 asks for:
Renovate on the gitops repo (watching the Lighthouse image tag + chart-dependency versions); a Renovate PR
on a new image that the operator reviews + merges; ArgoCD **sync waves** so DB migrations run before the API
upgrade; a **post-sync smoke-test** that ALERTS if a tenant is unhealthy after upgrade; a **tested rollback**
(push a broken image → health-check fails → roll back). **Exit criterion: releasing a new version requires
only reviewing + merging a Renovate PR; everything else is automatic.**

## The operator's desired shape (captured as INTENT, mechanism deferred to DESIGN)

- **Tenant Zero (the canary) auto-updates with NO human ask** — it always takes a new release first, hands-off.
- **In parallel, Renovate opens a PR to update all OTHER tenants** (the fleet, pinned to the latest released version).
- The operator watches Tenant Zero, and when happy, **merges that one PR with a single click** → the whole fleet rolls.
- Operator's hint: "Tenant Zero tracks latest, the rest stay pinned." Captured as intent only — see open
  question O-08-1; the literal mutable-`latest`-tag mechanism has a known gotcha and is NOT locked here.

---

## Scope Assessment (Elephant Carpaccio gate) — SPLIT into 3 thin slices

The remaining #5205 scope is **right-sized only when split**. As one slice it bundles three independent
operator outcomes (release-by-merge / safe-ordered-upgrade-with-alert / proven-rollback) and touches
Renovate + ArgoCD sync-waves + a smoke-test hook + an alert channel + a rollback drill — > 7 scenarios,
mixed risk classes. Split by user outcome, each ≤1 day, end-to-end, each with a learning hypothesis and a
Tenant-Zero dogfood moment:

| Slice | Outcome (the verifiable working behavior) | Stories | Learning hypothesis (one line) |
|---|---|---|---|
| **08a** `renovate-merge-only-release` | Releasing = review Tenant Zero + merge ONE Renovate PR; TZ auto-canaries hands-off; the fleet rolls on the merge. | US-08a-1, US-08a-2, US-08a-3 | Renovate-raised PR + TZ auto-canary collapses a fleet release to one click. |
| **08b** `ordered-upgrade-smoketest-alert` | Migrations run before the API serves; a post-sync smoke-test alerts the operator when a tenant comes back unhealthy. | US-08b-1, US-08b-2 | Ordering + post-sync health alert detects a bad upgrade in minutes, named, not silent. |
| **08c** `broken-image-rollback-drill` | A deliberately broken release on a throwaway tenant is caught by the smoke-test and rolled back within the RTO on a rehearsed runbook. | US-08c-1 | A rehearsed broken-image drill proves the "safe" claim instead of asserting it. |

**Delivery sequence**: 08a → 08b → 08c. 08a delivers the headline exit criterion (one-click release); 08b
makes it safe (ordering + detection); 08c proves it safe (tested rollback). 08b can be built against a manual
`promotedVersion` bump independently of 08a; 08c consumes 08b.

**Gate**: scope assessed → OVERSIZED-as-one → **split confirmed** (3 slices, briefs at
`slices/slice-08a|08b|08c-*.md`). The DELIVERED `slice-08-fleet-upgrade.md` is the **substrate record**,
superseded by 08a/08b/08c (see its STATUS banner — and note: the in-place banner edit and the
`feature-delta.md` append were blocked this session by the lean-ctx shadow read-tracking conflict; this
discuss/ package is the authoritative rescope record. A follow-up should fold these `[REF]` sections into
`feature-delta.md` under `## Wave: DISCUSS / [REF] slice-08 RESCOPE …` for full convention parity).

## Story → job traceability (N:1)

All six stories trace to **`job-saas-operator-upgrade-all-tenants-safely`** (opportunity 4/1 → gap 3). No
new job; no `infrastructure-only` story (every story is operator-observable).

---

## Shared Artifacts Registry (slice-08 rescope additions)

| Artifact | Source of truth | Consumers | Risk | Validation |
|---|---|---|---|---|
| `released-image-tag` (e.g. `ghcr.io/letpeoplework/lighthouse:26.7.x`) | the published GHCR image / GitHub release | Renovate watch target → the fleet bump PR + TZ auto-canary | HIGH — a missed tag = a silently-skipped release | a new tag raises a Renovate PR within one scan interval |
| `promotedVersion` (fleet default) | private gitops `tenants` ApplicationSet values | every non-canary tenant's resolved `targetRevision` | HIGH — stale = fleet behind / mixed versions | `argocd app list` revision equality after the merge |
| `tenant-zero-canary-version` (TZ `chartVersion` override) | private gitops `tenants/lpw` record | Tenant Zero's resolved `targetRevision` (ahead of fleet) | HIGH — if it does NOT track latest, the canary is stale and the gate is meaningless | TZ runs the new version before the fleet PR is merged |
| `ops-alert-channel` (the smoke-test alert sink) | platform config (DESIGN/DEVOPS to name) | the post-sync smoke-test failure notification | MEDIUM — wrong/missing channel = silent unhealthy upgrade | a failed smoke-test produces a visible alert naming tenant + version |
| `tenant-health-endpoint` (per-tenant readiness/health URL) | the shipped chart's health/readiness probe (epic-5305) | the post-sync smoke-test | MEDIUM — must match the served instance, not just pod-Ready | smoke-test asserts a served HTTP 200 on the new version |

Coherence note: `released-image-tag`, `promotedVersion` and `tenant-zero-canary-version` are three views of
the same `chart-version` shared artifact already in the epic registry — the fleet upgrade has NOT converged
until all three agree (TZ first, then `promotedVersion` after merge, both equal to the latest released tag).

---

## Changed Assumptions — back-propagation to `job-saas-operator-upgrade-all-tenants-safely`

> Back-propagation note ONLY. The DISCOVER SSOT (`docs/product/jobs.yaml`) is NOT edited by this wave. A
> future DISCOVER refresh should fold this refinement into the job's `functional` dimension.

**Original `functional` text (quoted verbatim from `jobs.yaml`):**

> "A version bump in the GitOps repo propagated to all tenants by ArgoCD; each tenant upgrades via the
> epic-5305 rolling-update + graceful-drain + expand-only-migration path; staged/canaryable rollout;
> rollback = git revert + helm rollback (additive migrations, no schema rollback)"

**Refined `functional` (proposed — the merge-only shape #5205 actually asks for):**

> Renovate watches the published Lighthouse image tag + tracked chart-dependency versions on the GitOps repo
> and raises a version-bump PR; **Tenant Zero auto-canaries the latest release with no human ask**; the
> operator reviews Tenant Zero and **promotes the whole fleet by merging ONE Renovate PR** (which bumps the
> fleet `promotedVersion`); each tenant then upgrades via the epic-5305 rolling-update + graceful-drain path
> with **schema migrations applied before the new API serves**; a **post-sync smoke-test alerts the operator's
> ops channel** if a tenant returns unhealthy; rollback = git revert (+ helm rollback), **rehearsed against a
> deliberately broken image** so the recovery path is tested, not assumed. Net: **releasing a new version
> requires only reviewing + merging one PR; everything else is automatic.**

This is a refinement (narrowing toward the real driving ports), not a contradiction — the staged
canary→promote substrate and expand-only/git-revert remain exactly as the original stated.

---

## Open Design Questions (red cards for DESIGN — flagged, not locked)

- **O-08-1 — Tenant-Zero auto-canary mechanism (the headline question).** Capture INTENT only ("TZ takes
  every release first, hands-off; fleet stays pinned until one merge"). Candidates: (i) Renovate **auto-merge**
  of a TZ-record-only PR bumping its `chartVersion` override; (ii) a literal mutable `latest` tag the TZ record
  tracks — **KNOWN GOTCHA: mutable + registry-side, and ArgoCD tracks GIT not the registry, so a retag does
  not trigger a sync** (do not pick blindly); (iii) a Renovate branch/automerge rule scoped to the TZ record.
  DESIGN converges; DISCUSS does not lock the mechanism.
- **O-08-2 — Migration-before-API ordering mechanism.** The app runs `Database.Migrate()` on boot, so ordering
  may already be implicit. DESIGN decides whether the expand-only guarantee + rolling update already satisfies
  "migrations before API serves", or whether a dedicated **pre-upgrade migration Job as an earlier ArgoCD
  sync-wave** is warranted. AC stays solution-neutral.
- **O-08-3 — Smoke-test surface + alert channel.** Operator hint: an **ArgoCD PostSync hook Job**. Exact alert
  channel (Discord/Slack webhook, email, Alertmanager) is undecided — AND slice-09's Prometheus/Alertmanager
  stack does NOT exist yet, so 08b may need a thin standalone alert path before the full observability stack
  lands. Flag the ordering dependency.
- **O-08-4 — Renovate hosting + watch scope.** Self-hosted GH-Actions cron vs the Renovate GitHub App; and
  exactly which chart/platform-component versions (CNPG/Postgres, cert-manager, ingress-nginx, ESO, …) are in
  Renovate's watch scope vs left manual. DESIGN/DEVOPS decide.
- **O-08-5 — Rollback posture.** 08c rehearses the operator-initiated `git revert` path only. Whether a future
  **auto-rollback on smoke-test failure** is desired is flagged, not built.

## Constraints honored (inherited)

D0 standalone gate (chart byte-unchanged — all change is private-repo GitOps + CI); D0c expand-only migrations
(the CI guard is the pre-flight); D0d extend the existing GitHub Actions workflow, trunk-based; D0e built ON
the shipped chart (never forked). Vendor tools (Renovate, ArgoCD) are already the platform's chosen stack — no
vendor-neutrality concern (this is the LPW hosting topology, not a redistributable product surface).
