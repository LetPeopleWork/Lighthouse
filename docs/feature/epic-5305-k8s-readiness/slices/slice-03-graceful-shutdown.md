# Slice 03: Graceful shutdown (SIGTERM) + connection draining

**Feature**: epic-5305-k8s-readiness
**Story**: US-03 (ADO #5309) → job-operator-zero-downtime-rollout
**Estimate**: ~1–1.5 crafter days
**Reference class**: `IHostedService` / `IHostApplicationLifetime` lifecycle wiring; touches the same update-queue hosted services as Epic 5121 / #5304

## Goal
Handle SIGTERM cleanly so a terminating pod stops accepting new work, drains in-flight HTTP + SignalR connections, flushes/awaits the in-memory update queue, and finishes within `terminationGracePeriodSeconds` — enabling zero-downtime rolling updates.

## IN scope
- Wire `IHostApplicationLifetime` `ApplicationStopping`/`ApplicationStopped` and/or `IHostedService.StopAsync` to:
  - stop accepting new HTTP requests and new SignalR negotiations,
  - drain in-flight HTTP requests within a bounded window,
  - flush/await the in-memory `UpdateQueueService` Channel so queued/in-flight updates complete (or are safely abandoned) before exit,
  - close SignalR connections so clients reconnect to a surviving pod.
- Configurable shutdown timeout aligned to `terminationGracePeriodSeconds`.

## OUT scope
- The cluster-wide single-consumer queue redesign → slice 07 (#5304). This slice drains the *current per-process* queue cleanly; it does not make the queue distributed.
- SignalR Redis backplane → slice 07.
- Probe manifests → Productization #5306.

## Learning hypothesis
**Confirms if it succeeds**: under a rolling update, a load test driving requests + an active SignalR client sees zero failed requests and a clean client reconnect as pods cycle.
**Disproves if it fails**: the in-memory update queue can't be drained deterministically within a sane grace period (e.g. a long external sync mid-flight), forcing the queue-redesign (slice 07) to land *before* true zero-downtime is claimable.

## Acceptance criteria
See US-03 in `../feature-delta.md`. Key: an integration test issues SIGTERM/`StopAsync` while an HTTP request and a queued update are in flight and asserts both complete (or the update is safely re-enqueued) before the host reports stopped; a single-container Ctrl-C behaves exactly as today (standalone gate).

## Dependencies
Pairs with slice 02 (readiness must flip to NotReady on `ApplicationStopping` so the LB stops routing before drain). Soft-precedes slice 07.

## Production data requirement
**Required.** Drive the dev instance under a small load generator + live SignalR client through a simulated rolling restart; assert no dropped requests.

## Dogfood moment
Operator triggers a rolling restart of the dev deployment during active use and observes no user-visible error and a seamless SignalR reconnect.

## Cross-cutting checklist (confirmed in feature-delta)
RBAC: N/A. Clients: N/A — server-side lifecycle only; CLI/MCP callers just reconnect. Website: N/A.

## Pre-slice spike candidates
- Measure worst-case in-flight update duration (external sync) to size the grace period. (~1 hr)
- Confirm Kestrel/ASP.NET shutdown ordering vs. our hosted services so drain runs before the server socket closes. (~1 hr)
