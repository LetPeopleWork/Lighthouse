# Outcome KPIs — slice-08 RESCOPE (ADO #5205 Automated Upgrades)

## Feature: fleet-upgrade merge-only release (slices 08a/08b/08c)

### Objective
Make shipping a Lighthouse release to the whole hosted fleet a **single reviewed click** — safe by
construction, fast to detect when it goes wrong, and proven to recover — so a small team keeps any number
of tenants current without a maintenance window.

### Outcome KPIs

| # | Who | Does What | By How Much | Baseline | Measured By | Type |
|---|-----|-----------|-------------|----------|-------------|------|
| 1 | LPW operator (Benjamin) | releases a new version to the whole fleet | **1 operator action** (merge one Renovate PR) | ~3+ manual steps (notice release → hand-edit `promotedVersion` → commit → push), no review surface | count of operator actions per fleet release, from git/PR history | Leading (Outcome) |
| 2 | the platform | gets a new published release onto Tenant Zero (canary) | **≤ 30 min, 0 human actions** (hands-off auto-canary) | manual, only if the operator remembers to add a `chartVersion` override | timestamp: image publish → `tenant-lpw` serving the new revision | Leading (Outcome) |
| 3 | the platform | surfaces a new release as a reviewable PR | **100%** of new image tags + tracked chart-dep versions raise a PR within one scan interval | 0% (no automation; releases self-discovered) | Renovate PR history vs published releases | Leading (Secondary) |
| 4 | LPW operator | detects an unhealthy tenant after an upgrade | **≤ 5 min** after sync, via a named alert | unbounded (no post-upgrade health gate; found when a customer reports it) | timestamp: tenant unhealthy → alert in ops channel | Leading (Outcome) |
| 5 | LPW operator | recovers a tenant from a broken release | **detect→recover ≤ 15 min** on a rehearsed runbook | untested/unknown (rollback never exercised against a broken image) | the 08c drill: broken push → smoke-test alert → git-revert → HTTP 200, timed | Leading (Outcome) |

### Metric Hierarchy
- **North Star**: **release-to-fleet operator actions = 1** (KPI 1) — the #5205 exit criterion made measurable.
- **Leading indicators**: hands-off time-to-canary (KPI 2), release→PR coverage (KPI 3), time-to-detect-unhealthy (KPI 4), detect→recover time (KPI 5).
- **Guardrail metrics (must NOT degrade)**:
  - Tenant Zero availability through every release = **100% HTTP 200** (the canary never goes down to canary).
  - Fleet convergence after a merge = **100%** of tenants on the new revision (`argocd app list` equality).
  - Migration-before-API ordering correctness = **100%** (0 API-before-migration windows).
  - Destructive migrations reaching a tenant = **0** (ExpandOnlyMigrationGuard holds).
  - Auto-promotion off a failed canary = **0 incidents** (the human merge gate holds).

### Measurement Plan
| KPI | Data Source | Collection Method | Frequency | Owner |
|-----|------------|-------------------|-----------|-------|
| 1 release actions | git/PR history (lighthouse-platform) | count merges/edits per release | per release | operator |
| 2 time-to-canary | GHCR publish event + `argocd app` revision time | timestamp diff | per release | operator |
| 3 PR coverage | Renovate PR log vs GitHub releases | reconcile published vs PR'd | weekly | operator |
| 4 time-to-detect | post-sync smoke-test result + alert timestamp | timestamp diff | per upgrade | operator |
| 5 detect→recover | 08c drill record | timed rehearsal | per release (drill) | operator |

### Hypothesis
We believe that **Renovate-raised PRs + a hands-off Tenant-Zero auto-canary + a one-click fleet promote +
ordered upgrades with a post-sync smoke-test/alert + a rehearsed broken-image rollback** for the **LPW SaaS
operator** will achieve **"release to the whole fleet in one reviewed click, detected fast and proven to
recover."** We will know this is true when **the operator performs exactly 1 action per fleet release
(KPI 1), Tenant Zero is canaried hands-off within 30 min (KPI 2), and an unhealthy upgrade is alerted within
5 min (KPI 4)** — while the availability/convergence/ordering guardrails stay at 100%.

### Handoff to DEVOPS (instrumentation needs)
- Per-release operator-action count (PR/merge events).
- Image-publish → canary-serving latency (registry event + ArgoCD revision timestamp).
- Renovate PR-vs-release reconciliation (coverage).
- Post-sync smoke-test result stream + alert latency; the ops alert channel target (O-08-3).
- 08c drill timing record (detect→recover).
- Guardrail alerts: Tenant-Zero-down, fleet-not-converged, API-before-migration, destructive-migration-reached-tenant, canary-auto-promoted.

> Targets 2/4/5 (≤30/≤5/≤15 min) are aspirational stretch values for first delivery; confirm against the
> first measured drill and adjust. They depend on the unresolved RPO/RTO commitment (DESIGN O-3) and the
> Renovate scan interval (O-08-4).
