# ADR-096: Post-Sync Health Smoke-Test = a Version-Stamped ArgoCD PostSync Hook Job (Per Tenant) That Asserts the Served Version + Health and, on Failure, Opens/Updates a GitHub Issue in `LetPeopleWork/lighthouse-platform`

**Status**: **PROPOSED** (2026-06-30, Titan — PROPOSE mode, awaiting Benjamin)
**Date**: 2026-06-30
**Feature**: epic-5306-productization-platform (ADO Epic #5306, story #5205 RESCOPE slice-08b/08c, US-08b-2 + US-08c-1) — resolves open question **O-08-3**
**Decider**: Benjamin (product owner) + Titan (System Designer)
**Relationship to prior work**: REUSES the epic-5305 chart health/readiness probe (the served instance's endpoint), the tenant-runtime overlay (`gitops/_charts/tenant-runtime/`), ESO/OpenBao (ADR-087) for the GitHub token, and the matrix-with-`promotedVersion` pattern (slice-08). The alert channel is **GitHub issue** (user-locked O-08-3) — slice-09's Prometheus/Alertmanager does not exist yet, so this is the thin standalone path.

---

## Context

US-08b-2 requires that **after a tenant syncs a new version**, a smoke-test checks its served health endpoint and, **on failure, raises a per-tenant-attributed alert naming the tenant + version** within the detection target (KPI-4 ≤5 min); a healthy tenant raises nothing (no noise). US-08c-1 consumes the same alert as the *detect* half of the rehearsed broken-image rollback drill. The alert channel is **user-locked to a GitHub issue** in `LetPeopleWork/lighthouse-platform`.

Two design tensions:
- The **version roll** happens on the *chart* Application (the public #5199 chart, byte-unchanged — we cannot add a hook to it).
- The *runtime* overlay Application (private) and the chart Application sync **independently**, so a naive PostSync hook could fire mid-roll and judge the old/transitional pod.

## Decision

**A version-stamped per-tenant ArgoCD PostSync hook Job, rendered by the `tenant-runtime` overlay, that asserts the expected served version before judging health, and on failure calls the GitHub Issues API.**

1. **Version-stamp the runtime overlay.** Extend `gitops/tenants/_generator/applicationset-runtime.yaml` to the **same matrix-with-`promotedVersion`** pattern the workload ApplicationSet already uses (REUSE the proven shape), and pass the resolved version (`chartVersion` override else `promotedVersion`) into the tenant-runtime chart as a value. A version change therefore re-renders the Job manifest → ArgoCD re-syncs the runtime app → the **PostSync hook re-fires** on every roll. This is how a private overlay observes a roll of the public chart without touching the chart.
2. **The Job (annotated `argocd.argoproj.io/hook: PostSync`, `hook-delete-policy: HookSucceeded`)** polls the tenant's health endpoint (the epic-5305 chart probe on the served instance) with bounded retry/backoff and:
   - **waits until the endpoint reports the EXPECTED version** (the version-stamp) — this defeats the independent-app race: it ignores the old/transitional pod and only judges `vN+1`;
   - on a healthy 200 for the expected version → exits 0, hook is deleted, **no issue, no noise**;
   - if the expected version never serves healthy within the window → exits non-zero and **opens/updates a GitHub issue**.
3. **Alert = GitHub issue (curl → `api.github.com`).** The Job (minimal `curl` image) calls `POST /repos/LetPeopleWork/lighthouse-platform/issues` with a Bearer token, titled **`tenant <id> unhealthy after upgrade to <version> (health <code>)`**, labelled `upgrade-smoke-fail`. It first searches open issues by a stable title key so a re-fire **updates** (comments on) the existing issue rather than duplicating. Per-tenant attribution is inherent — one Job per tenant, stamped with `{{.id}}` + version.
4. **Token via ESO/OpenBao (REUSE ADR-087).** A fine-grained PAT scoped to **issues:write on the single repo** is stored in OpenBao and materialised by an ExternalSecret. **Recommended placement: a shared `platform` namespace** (the Job hits the tenant's *public* health URL, reachable from anywhere) so the token lives **once**, not in every tenant namespace — least secret spread. Trade-off vs running in the tenant namespace (token spread, larger blast radius) is resolved in favour of the shared placement; confirm at DELIVER.

## Rollback posture (O-08-5) — confirmed

The smoke-test is **detect + alert only; it does NOT auto-act.** Rollback stays the **operator-initiated `git revert`** path proven in the substrate (ADR-093). Auto-rollback-on-smoke-fail is explicitly **OUT** (flagged as a possible future controller, not built). US-08c-1 rehearses the manual path on a throwaway tenant and times detect→recover (KPI-5 ≤15 min).

## Consequences

- **Positive**: a bad upgrade is named and surfaced within minutes instead of festering until a customer reports it; per-tenant attribution is structural; the served-version assert makes the signal robust to the two-app race and gives true version attribution; reuses ESO/OpenBao + the chart probe + the matrix pattern; the GitHub-issue channel needs no observability stack (slice-09 can later supersede it with Alertmanager without changing the smoke-test contract).
- **Negative / cost**: a GitHub issue is a coarse channel (no paging/severity routing — acceptable for a single-operator fleet; slice-09 upgrades it later); the Job depends on the chart exposing a served-version field (confirm at DELIVER); the version-stamp on the runtime overlay couples runtime re-sync to the roll (intentional — that is the trigger).
- **Scale**: control-plane, not data-plane — tens of tenants × a few rolls/week = a handful of Jobs and at most a handful of issue-API calls; GitHub's authenticated rate limit (5000/h) is never approached. No capacity bottleneck; the binding constraints are the **latency budgets** (KPI-4 ≤5 min detect) and **correctness** (version-match before judging), not throughput.
- **Standalone gate**: untouched — private-repo overlay + a hosted API call; public chart byte-unchanged.

### Earned-Trust note

This Job IS the Earned-Trust probe for the upgrade substrate: it refuses to *assume* a synced tenant is healthy and instead **demonstrates empirically** that the expected version actually serves a healthy response in the real cluster, and emits a structured signal (the issue, naming the specific tenant+version+code) when the substrate lies (a tenant reports Synced but serves 503). Self-application: because the Job is version-stamped, it re-probes on every dependency/version bump, so the probe stays honest after upgrades.

## Open questions for DISTILL/DELIVER

- The exact chart health/version endpoint path + served-version field (epic-5305 probe) for the version-match assertion.
- Token type (fine-grained PAT vs GitHub App installation token) + its OpenBao path + the ExternalSecret placement (recommended: shared `platform` ns).
- Retry window / backoff (must exceed tenant cold-start; feeds KPI-4 ≤5 min) — tune against the first measured roll and the 08c drill.
- Issue de-duplication key + auto-close-on-recovery policy (nice-to-have; not required for 08b AC).
