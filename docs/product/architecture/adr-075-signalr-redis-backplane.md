# ADR-075: SignalR Cross-Pod Fan-Out — Redis Backplane via `Microsoft.AspNetCore.SignalR.StackExchangeRedis`, Config-Gated on `ConnectionStrings:Redis`; Degrades to In-Memory Fan-Out at One Replica

**Status**: Accepted (2026-06-16 — Morgan, Solution Architect; interaction mode PROPOSE. Inherits System Decision 1 / A2; third-architect application detail.)
**Date**: 2026-06-16
**Feature**: epic-5305-k8s-readiness (ADO Epic #5305 — make the Lighthouse app itself safe to run on Kubernetes)
**Decider**: Morgan (Solution Architect), confirming the system-designer's Decision 1
**Relationship to prior ADRs**: AMENDS ADR-027 (Q1/3A single-instance default stands; this adds a config-gated multi-replica branch behind the existing `AddSignalR()` seam). Pairs with ADR-076 (cluster-aware queue), ADR-077 (migration coordination), ADR-078 (observability). Honours the D1 standalone gate.

---

## Context

`UpdateNotificationHub` (`UpdateNotificationHub.cs`) fans server-raised update notifications out to clients via `Clients.Group(updateKey.ToString())` and `Clients.Group("GlobalUpdates")` (`UpdateQueueService.cs:199-204`). Registration today is `AddSignalR().AddJsonProtocol(...)` (`Program.cs:269`) with **no `.AddStackExchangeRedis`** and **no `SignalR.StackExchangeRedis` package** (grep: zero). The hub is mapped at `app.MapHub<UpdateNotificationHub>("api/updateNotificationHub")` (`Program.cs:212`) and is `[Authorize]` (`UpdateNotificationHub.cs:7`). The frontend `withUrl(...)` does **not** set `skipNegotiation` → the client negotiates a transport.

At one replica this is correct: groups live in that process, every connected client is on that process, fan-out reaches everyone. The moment a second replica exists, a notification raised on pod B reaches only the clients connected to pod B; clients on pod A never see it (US-07 AC2 — "a notification raised on any pod reaches clients connected to any other pod"). This silently fails — no error, just missing updates.

The D1 epic gate is sacrosanct: the single-container standalone product must be byte-identical, auto-degrading to the in-process path with no Redis required.

## Decision

**Append `.AddStackExchangeRedis(connectionString)` to the existing `AddSignalR()` registration (`Program.cs:269`) ONLY when `ConnectionStrings:Redis` is present in configuration.** Absent the connection string, the registration is left exactly as today and SignalR uses its in-memory group store.

```
var signalR = builder.Services.AddSignalR().AddJsonProtocol(/* unchanged converters */);

var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    signalR.AddStackExchangeRedis(redisConnection);
}
```

- **Package**: `Microsoft.AspNetCore.SignalR.StackExchangeRedis` (Microsoft, MIT, first-party for ASP.NET Core 8) — no third-party fan-out library, no managed-service SDK.
- **Config idiom**: `ConnectionStrings:Redis` follows the established `Configure<T>(...)`/`builder.Configuration[...]` convention (the same idiom `DatabaseConfigurator` uses to switch SQLite vs Postgres). `__` bridges colons for env vars (`ConnectionStrings__Redis`).
- **Hub, groups, fan-out code, `[Authorize]`, negotiation — all unchanged.** The backplane is transparent to `UpdateNotificationHub` and `UpdateQueueService.NotifyListeners`; group sends transparently publish to Redis pub/sub and every pod's SignalR re-delivers to its local group members.
- **Reuse verdict**: EXTEND the `AddSignalR()` registration; no new hub, no fan-out rewrite.

**Earned-Trust note**: the Redis backplane is a driven adapter on an external substrate. Its connectivity is proven by the ADR-078 / composition-root startup probe (the same probe that validates the ADR-076 substrate and the status store) — wire then probe then use: a `ConnectionStrings:Redis` that is set but unreachable causes a structured `health.startup.refused` event naming the unreachable Redis, rather than a silent degradation to broken cross-pod fan-out. The backplane is pub/sub only (no persistence) so there is no durability contract to probe beyond reachability + publish/subscribe round-trip.

## Standalone Degradation (D1 / US-07 AC4)

No `ConnectionStrings:Redis` ⇒ `.AddStackExchangeRedis` is never called ⇒ in-memory group fan-out, **identical code path and behaviour to today**. One replica needs no backplane (every client is local). The standalone single-container product pays zero Redis cost and ships exactly ADR-027's `replicas: 1` topology.

## Alternatives Considered

**Chosen: Redis backplane, config-gated.**
- Pros: first-party MS package; matches the north-star (planning §4 "API N replicas + Redis"); no managed-service lock-in; pub/sub round-trip is sub-millisecond on a cluster LAN and the fan-out volume is tiny (≤~150 SignalR connections *total* across the fleet, ADR-027 sizing); clean degradation to today.
- Cons: requires an operator-provided Redis for multi-replica; sticky sessions are still required (see below).

**Rejected: Azure SignalR Service.** Offloads fan-out entirely but is a managed Azure dependency — it couples the self-hostable, runs-anywhere product to a specific cloud service, contradicting the vendor-neutral posture (A2). Forbidden as proprietary without an explicit requirement.

**Rejected: sticky sessions only (`sessionAffinity: ClientIP`, in-memory fan-out).** Pinning a client to one pod makes in-memory fan-out "work" for that client — but it does NOT deliver *server-raised* notifications (the actual use case here) to clients on *other* pods, and it breaks on pod rebalancing. It was the *learning* spike (story 07), not a product answer (A2).

## Consequences

**Positive**:
- Cross-pod notification delivery (US-07 AC2) with a transparent, first-party backplane; no change to hub or fan-out code.
- Degrades cleanly to the standalone in-memory path (D1); the standalone product is unaffected.

**Negative**:
- **Sticky-session / affinity is required EVEN WITH the backplane** (MS docs: negotiation + the subsequent connection must land on the same pod). This is a **deploy concern (Productization epic #5306)**, NOT in-app code — flagged here so #5306 does not assume the backplane alone makes SignalR multi-replica-safe. The in-app surface is only the backplane wiring.
- Adds an operator-provided Redis dependency for the multi-replica topology (deployed by #5306).

**Neutral**:
- The same Redis instance backs the ADR-076 shared status store; one small Redis serves both (backplane pub/sub + a small `UpdateKey`-keyed hash).

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| `.AddStackExchangeRedis` is called iff `ConnectionStrings:Redis` is present | Integration test: with the connection string absent, registration matches today (no backplane service registered); with it set + reachable, the Redis backplane is wired |
| Standalone (no Redis) fan-out is byte-identical | Existing SignalR notification tests run unchanged with no Redis configured (US-07 AC4) |
| Unreachable-but-configured Redis refuses startup, not silent-degrades | Composition-root probe (shared with ADR-078) emits `health.startup.refused` naming the unreachable Redis |
| No managed-service SDK introduced | Grep/dependency check: no `Microsoft.Azure.SignalR` package |
