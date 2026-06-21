# Evolution — Epic 5305: Kubernetes-readiness (production code)

**Date:** 2026-06-21
**Epic:** ADO #5305 · **Stories:** #5304, #5307–#5312, #5329, #5330 (all Closed/Resolved)
**Predecessor:** split out of learning epic #5189 on 2026-06-15
**Successor:** epic #5306 (k8s productization — Helm chart, docs, GitOps, dogfood)

## Summary

Make the Lighthouse application itself safe to run on Kubernetes — multiple replicas, rolling
updates, behind an Ingress / reverse proxy — through production C#/TS changes, **without changing
the sacrosanct single-container standalone product**. Seven capability slices plus two cross-repo
follow-ons (API JWT bearer + clients OAuth metadata). The app had to be cluster-*safe* before it
could be cluster-*packaged* (#5306).

## Business context

Six `platform-operator` jobs drove the slicing (`docs/product/jobs.yaml`). Highest opportunity:
`survive-multiple-replicas` (importance 5 / satisfaction 1 / gap 4) — running >1 replica without
N× external syncs, lost SignalR notifications, or migration races. The high-uncertainty multi-replica
work shipped **last** (#5304) but SPIKEd early; the small gap-3 "login behind proxy" work shipped
**first** (#5311) to unblock all cluster auth testing.

## The hard gate (D1): standalone stays byte-identical

Every slice had to degrade to the single-container standalone path. The entire cluster-aware surface
is gated on `ConnectionStrings:Redis`: absent → in-process adapters, identical to today; present →
distributed adapters. This held across all 7 slices and is the epic's defining constraint.

## Slices delivered

| Slice | Story | What shipped |
|---|---|---|
| 01 | #5311 | Reverse-proxy forwarded headers (`UseForwardedHeaders`) — correct HTTPS/cookies/OIDC/SignalR behind a proxy |
| 02 | #5310 | Health checks: liveness / readiness / startup endpoints |
| 03 | #5309 | Graceful shutdown (SIGTERM) + connection draining — `IReadinessState` + `DrainAsync` + `GracefulShutdownService` |
| 04 | #5308 | Expand-only EF migrations + once-only startup migration lock under N replicas; destructive-migration CI guard |
| 05 | #5312 | In-app observability hooks — off-by-default `/metrics` + structured logging |
| 06 | #5307 / #5330 | MCP HTTP server inbound auth (OAuth pass-through), clients advertise protected-resource metadata |
| 07 | #5304 | Horizontal scalability — SignalR Redis backplane + cluster-aware update queue |
| 08 | #5329 | API accepts IdP JWT bearer for hosted MCP OAuth |

## Key decisions (ADRs, in `docs/product/architecture/`)

- **ADR-075** SignalR Redis backplane — fan-out notifications across pods.
- **ADR-076** Cluster-aware update queue — Option B: per-entity `pg_advisory_lock` + shared Redis
  status store (monotonic-CAS) + SignalR backplane + Redis pub/sub awaiter. All gated on Redis;
  standalone path byte-identical. SPIKE-gated in design, confirmed in DELIVER.
- **ADR-077** Concurrent-startup migration coordination — once-only migration under N replicas;
  shares the Earned-Trust probe contract with ADR-076.
- **ADR-078** In-app observability hooks — `/metrics` off by default; "cluster-internal" is a
  deployment expectation (network policy = #5306), not a code-enforced boundary.
- **ADR-079** API JWT bearer for MCP OAuth.

## Lessons learned

- **Substrate seam, not a rewrite.** Cluster-awareness lives behind `IUpdateExecutionLock` /
  `IUpdateCompletionNotifier` / `IUpdateStatusStore`; the `IUpdateQueueService` caller contract
  never changed (ArchUnit-guarded). Callers are oblivious to the substrate.
- **Earned-Trust startup probe refuses on a lying substrate** — the cluster-aware path verifies its
  Postgres advisory-lock + Redis CAS primitives at startup before serving.
- **Stale-migration DLL** needs a `--no-incremental` rebuild even for a fresh migration (InMemory
  tests miss the real provider's destructive-migration shape).
- **Redis sync-API thread-pool starvation** — use the async StackExchange.Redis API throughout the
  update path; the sync API starves the pool under concurrent timer + manual-refresh load.
- **Testcontainers for races.** Cross-pod behaviour (advisory-lock exclusion, monotonic-CAS,
  pub/sub delivery, startup probe) is covered by `@requires-docker` integration tests with real
  Postgres + Redis — the only way to assert the distributed invariants. In-process unit mutation
  covers the standalone path; the two are complementary, not overlapping.

## Mutation testing

Per-slice reports in `docs/feature/epic-5305-k8s-readiness/deliver/mutation/`. Slice-07
introduced-surface adjusted 96.1% (≥80% gate); raw 62% reflects whole-file mutation of the
logging-heavy `UpdateQueueService` orchestrator (justified survivors documented).

## Cross-cutting

- **RBAC:** N/A — infra slices, no authorization surface; the update substrate is composition-root infra.
- **Clients:** the MCP OAuth slices (#5330) updated the clients repo; #5304 needed no client change
  (no API contract change).
- **Website:** N/A — no marketed UI surface changed.
- **Docs/screenshots:** N/A at the code level — user-facing k8s deployment docs belong to
  productization epic #5306.

## Status

All 9 stories Closed/Resolved; epic rolled up complete. Standalone single-container product
unchanged. Next: epic #5306 packages and hosts the now-cluster-safe app.
