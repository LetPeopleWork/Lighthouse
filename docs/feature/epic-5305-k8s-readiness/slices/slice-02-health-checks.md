# Slice 02: Health checks (liveness / readiness / startup)

**Feature**: epic-5305-k8s-readiness
**Story**: US-02 (ADO #5310) → job-operator-trust-pod-health
**Estimate**: ~1–1.5 crafter days
**Reference class**: new read endpoints + DI wiring; learning story 04 (#5194) exercised probes as a spike — this is the product implementation

## Goal
Add real ASP.NET Core health checks driving the three k8s probes so traffic reaches only serving pods and only genuinely-dead pods restart.

## IN scope
- `AddHealthChecks()` with distinct tagged checks mapped to three endpoints:
  - **readiness** (`/health/ready`): DB connectivity + migrations-applied → pod kept OUT of LB rotation until truly serving.
  - **liveness** (`/health/live`): shallow — restart only on genuine deadlock, NOT on a slow dependency.
  - **startup** (`/health/startup`): covers slow boot / migration window without tripping liveness.
- Endpoints harmless / no-op-friendly in single-container mode (standalone gate).

## OUT scope
- The k8s probe manifests (chart story 09 / Productization #5306).
- Migration-applied detection that requires the migration lock → coordinate with slice 04 (this slice checks "migrations applied", slice 04 owns "apply once across replicas").
- /metrics, tracing → slice 05.

## Learning hypothesis
**Confirms if it succeeds**: a pod with an unreachable DB drops out of rotation (readiness red) WITHOUT being restarted (liveness green) — no restart storm.
**Disproves if it fails**: a shallow liveness check can't distinguish deadlock from slow dependency cheaply, forcing a richer (and riskier) liveness signal.

## Acceptance criteria
See US-02 in `../feature-delta.md`. Key: integration tests assert (a) readiness returns unhealthy when DB is down but liveness stays healthy; (b) readiness returns healthy only when DB reachable AND migrations applied; (c) endpoints return 200 in single-container mode with no orchestrator.

## Dependencies
Soft on slice 04 for the precise "migrations applied" signal; can ship with a simpler "can open a DB connection" readiness first and tighten once slice 04 lands.

## Production data requirement
**Required.** Run the dev instance, kill the DB connection, observe readiness flip while the process is NOT restarted; restore and observe recovery.

## Dogfood moment
Dev instance deployed with the three probes wired; operator watches a clean rollout where a not-yet-migrated pod stays out of rotation until ready.

## Cross-cutting checklist (confirmed in feature-delta)
RBAC: N/A — health endpoints are unauthenticated operational surface (no business data). Clients: N/A. Website: N/A.

## Pre-slice spike candidates
- Decide whether health endpoints sit on the main port or a separate management port. (~30 min)
- Confirm a cheap, reliable "migrations applied" query against EF Core for both SQLite and Postgres. (~1 hr)
