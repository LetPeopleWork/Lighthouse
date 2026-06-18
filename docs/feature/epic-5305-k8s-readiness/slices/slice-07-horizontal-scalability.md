# Slice 07: Horizontal scalability — SignalR backplane + cluster-aware update work

**Feature**: epic-5305-k8s-readiness
**Story**: US-07 (ADO #5304) → job-operator-survive-multiple-replicas
**Estimate**: ~4–6 crafter days **after a required SPIKE** (highest uncertainty in the epic)
**Reference class**: distributed-coordination work; closest analog is Epic 5121 (domain-events + concurrency), but larger — this makes a singleton app multi-replica-safe

## Goal
Make Lighthouse genuinely safe to run with N API replicas: a notification raised on any replica reaches clients on all replicas; external syncs + the update queue run once across the fleet (no N× syncs, no racing Postgres writes); and `GetUpdateStatus` is consistent across pods. Config-gated: no Redis / one replica ⇒ exactly today's single-instance behaviour (standalone gate, D4).

## The three coupled breakages (from story-07-research.md §1)
1. **(B) SignalR fan-out** is in-memory per process → a notification raised on pod A never reaches pod B's clients.
2. **(C) Background updaters** (`TeamUpdater`, `PortfolioUpdater`, `UpdateQueueService`) run in *every* replica → N× external syncs + racing writes.
3. **(C) Status cache** — the in-memory `ConcurrentDictionary<UpdateKey,UpdateStatus>` in `UpdateNotificationHub` answers differently per replica.

## IN scope
- **SignalR Redis backplane**, config-gated: Redis configured ⇒ cross-pod fan-out; no Redis ⇒ current in-memory behaviour.
- **Cluster-aware update path** — the unit that must become cluster-aware is the **update queue itself**, not just a timer leader. Both trigger paths must be covered: the periodic timer loop AND request-triggered manual refresh (`TeamController`/`PortfolioController` → `UpdateQueueService.EnqueueAndAwaitAsync` inline on whatever replica serves the request). Fix space (DESIGN decides — do NOT pre-pick): a shared/distributed queue with a single consumer, or a cluster-wide per-entity lock, plus a shared status store so dedup + the awaited completion + `GetUpdateStatus` are consistent across replicas.
- **Shared status store** backing `GetUpdateStatus` (Redis or sourced from Postgres).

## OUT scope
- HPA / `sessionAffinity` / load-test manifests — those were the **learning** story 07 (#5197) k8s-layer spike (throwaway), not this production slice.
- Per-tenant isolation / namespace model → Productization #5306.

## Learning hypothesis
**Confirms if it succeeds**: with Redis + 3 replicas, a manual refresh served by pod B notifies a client on pod A; the external system is synced once per cycle across the fleet; `GetUpdateStatus` agrees across pods.
**Disproves if it fails**: leader election alone is insufficient (a manual refresh handled by a follower still double-works), proving the queue itself must be the cluster-aware unit — which is exactly why this is one coupled slice, not three.

## Acceptance criteria
See US-07 in `../feature-delta.md`. Key: a multi-host integration/e2e test asserts (a) single sync per entity across N hosts under concurrent timer + manual-refresh load; (b) cross-pod notification delivery via the backplane; (c) consistent `GetUpdateStatus`; (d) with no Redis / 1 host, behaviour and code path are identical to today.

## Dependencies
Soft-depends on slice 03 (clean drain) and slice 04 (migration safety) being in place so multi-replica operation is tested on a safe base. This is the LAST slice to ship.

## Production data requirement
**Required.** Real Postgres + Redis, ≥3 replicas on k3s, real work-tracking connector driving syncs. InMemory/mock tests cannot reproduce the cross-replica races (recurring lesson).

## Dogfood moment
The dev/stage deployment runs 3 replicas with Redis; operator triggers concurrent refreshes and a node drain and observes single syncs, consistent status, and no lost notifications.

## Cross-cutting checklist (confirmed in feature-delta)
RBAC: N/A — no authorization surface. Clients: likely N/A — internal infra, no API contract change (confirm in DESIGN). Website: N/A — infra, not a marketed feature.

## Pre-slice SPIKE (REQUIRED — high uncertainty; do BEFORE committing the slice)
- **Probe the cluster-aware-queue design** (`nw-spike`, ~1–2 days): prototype the two candidate shapes — (i) distributed queue with a single consumer, (ii) cluster-wide per-entity lock + shared status store — against real Postgres+Redis with 3 hosts driving both timer and manual-refresh paths. Goal: disprove "leader election is enough" and pick the unit of coordination. Output feeds DESIGN; do NOT pre-pick a solution in DISCUSS.
- Confirm `UpdateQueueService` singleton-per-process semantics (Channel queue, consumer, awaiters, `updateStatuses` dedup dict) match the research doc before designing the replacement. (~2 hr)
