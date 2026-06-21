# Slice-07 DELIVER roadmap — horizontal scalability (Option B)

**Feature**: epic-5305-k8s-readiness · **Story**: US-07 (ADO #5304) · **ADR**: ADR-076 (Accepted — Option B)
**Substrate**: per-entity Postgres `pg_advisory_lock` (hard mutual-exclusion / INV-4) + shared Redis status store (soft dedup + monotonic-CAS reads / INV-1·2) + SignalR Redis backplane (AC2) + Redis pub/sub cross-pod awaiter. All gated on `ConnectionStrings:Redis`; absent ⇒ in-process verbatim (D1 / AC4).

## State at roadmap time
- **Port extracted (DONE, committed):** `IUpdateStatusStore` + `InProcessUpdateStatusStore`; `UpdateQueueService`, `UpdateNotificationHub`, `DatabaseMaintenanceGate` all read/write through the port. Scenario **#38** (`UpdateStatusStoreTest.Advance_RegressingProgress_NeverObservesRegressedProgress`) GREEN.
- **RED scaffolds present (`[Ignore]`, `Assert.Fail` carrying GWT):** #39 (`ScalabilityTests.NoRedisOneHost…`), #40 (`UpdateStatusStoreContainerTests.SharedAdvance…`), #41/#42/#43 (`ScalabilityTests`).
- **Harness reuse:** `PostgresContainerFixture` + `RedisContainerFixture` (`StartFreshAsync`), multi-host `ServiceProvider`/`Barrier` pattern from `ConcurrentStartupMigrationTests` (slice-04, shipped).
- **Packages to add (production csproj):** `Microsoft.AspNetCore.SignalR.StackExchangeRedis`, `StackExchange.Redis`. Npgsql already transitively present for the advisory lock.

## Step 0 — Earned-Trust probe (ADR-076 mandate, FIRST) · `@requires-docker`
Empirically prove Option B's substrate against real Postgres + Redis BEFORE building on it. A `@requires-docker` test (`SubstrateProbeTests`) that exercises the three lies the ADR names:
- **(a) mutual exclusion** — acquire the per-entity advisory lock from two Npgsql connections on the same `UpdateKey`-derived `bigint`; assert exactly one wins (`pg_try_advisory_lock`).
- **(b) exactly-once effect / dedup** — admit the same `UpdateKey` twice through the shared store; assert the effect happens once (`HSETNX`-style `TryAdmit`).
- **(c) reclaim on holder death** — drop the lock-holding connection; assert the lock auto-releases and a second holder acquires.

Output → the production `probe()` contract for step 6. **Gate:** all three demonstrated GREEN, or Option A fallback is escalated to the user (per ADR liveness caveat). No production-path build proceeds on a red probe.

## Step 1 — `RedisUpdateStatusStore` shared adapter · drives **#40**, half of **#43**
New driven adapter implementing `IUpdateStatusStore` over a Redis hash keyed by `UpdateKey`:
- `Advance` = **monotonic compare-and-set on the `UpdateProgress` ordinal** via a Lua script (atomic read-compare-write), never blind LWW (INV-1).
- `TryAdmit` = `HSETNX`-style soft dedup. `TryGet` = bounded-stale read (INV-2, no distributed lock on the hot path). `Remove` = `HDEL`. `HasActiveWork` = scan hash for `Queued`/`InProgress` (the DMG cross-pod question).
- RED: un-skip **#40** (`SharedAdvance_ConcurrentWriters_OrdinalNeverRegresses`) → concurrent writers + stale lower-ordinal writes, assert no observed regression.

## Step 2 — per-entity advisory-lock admission · drives **#41**
Parameterize the proven slice-04 `MigrateUnderAdvisoryLock` pattern into a cluster-wide per-entity admission boundary, invoked **at execution time inside the consumer** (not at enqueue — avoids holding a session lock across the thread hop):
- Key = `(long)(int)updateType << 32 | (uint)id`; dedicated Npgsql connection; `pg_try_advisory_lock` for the non-blocking admission test, `pg_advisory_lock` as the serialization backstop; released in `finally`; auto-released on connection drop (liveness).
- The shared `UpdateQueueService` impl acquires the lock around the update body when the Redis gate is on.
- RED: un-skip **#41** (`RedisThreeHosts_SingleSyncPerEntity_TimerAndManualRefresh`) → 3 hosts, concurrent timer + manual refresh on the same entity, assert connector invoked once per cycle (INV-4). Multi-host = N `WebApplicationFactory<Program>` sharing both connection strings; count connector calls via a fake connector.

## Step 3 — SignalR Redis backplane · drives **#42**
Append `.AddStackExchangeRedis(conn)` to the existing `AddSignalR()` registration (`Program.cs:279`) only when `ConnectionStrings:Redis` is present. Hub, groups, fan-out, `[Authorize]` unchanged.
- RED: un-skip **#42** (`RedisBackplane_NotificationOnPodA_ReachesClientOnPodB`) → 2 hosts on one Redis backplane, SignalR client on pod A, notification raised on pod B, assert delivery.

## Step 4 — cross-pod awaiter release · drives second half of **#43**
`EnqueueAndAwaitAsync` keeps the in-process `awaiters` TCS dict. When the awaiting caller is on a different pod than the runner, the runner publishes a terminal-advance over a **dedicated Redis pub/sub channel** (distinct from the SignalR client backplane); each pod's `UpdateQueueService` subscribes and releases any local awaiter for that `UpdateKey`. In-process adapter unchanged.
- RED: un-skip **#43** (`GetUpdateStatus_ConsistentAcrossPods`) → admit on pod A, query/await on pod B, assert consistent status + awaiter released on terminal advance.

## Step 5 — composition-root wiring + standalone gate · drives **#39**
`Program.cs:975-978` selects shared vs in-process adapters on the `ConnectionStrings:Redis` gate (the same single gate as ADR-075/076). Redis-set-but-SQLite-provider = misconfiguration surfaced by the probe (advisory lock needs Npgsql). Absent the gate ⇒ `InProcessUpdateStatusStore` + in-process `Channel` + in-process awaiters, **byte-identical to today**.
- RED: un-skip **#39** (`NoRedisOneHost_BehaviourAndCodePathIdenticalToToday`, `@standalone`) → no Redis ⇒ in-process adapters resolved, advisory lock a no-op.

## Step 6 — startup Earned-Trust probe wired into the host · production form of step 0
Composition root runs the substrate `probe()` at startup (wire → **probe** → use) BEFORE the cluster-aware path serves. A failing probe refuses to start the cluster-aware path with a structured **`health.startup.refused`** event naming the lie (e.g. "advisory lock not mutually exclusive — pooler likely in transaction mode; use session-mode pooling"), driving `/health/startup` Unhealthy. Pairs with the ADR-077 migration-lock probe (shared advisory-lock-on-real-Postgres concern).

## Close-out
- **Mutation** ≥80% on the introduced surface (`RedisUpdateStatusStore` CAS/dedup, advisory-lock admission, awaiter release, probe). `@requires-docker` excluded from the in-memory mutation run; assert the in-process adapter + admission logic there.
- **Live `@production-data` dogfood:** k3s 3 replicas + Redis + real Postgres; concurrent refreshes + node drain; observe single syncs, consistent `GetUpdateStatus`, no lost notifications (Claude runs live).
- **ArchUnit:** `IUpdateQueueService` retains `EnqueueUpdate` + `EnqueueAndAwaitAsync` (signature unchanged); callers unchanged.
- **Cross-cutting:** RBAC N/A (no auth surface); Clients N/A (internal infra, no API contract change — confirmed DESIGN); Website N/A (infra, not marketed).
- **ADO #5304** Active → Resolved at slice end; push after CI green (paused for review).

## Scenario → step ledger
| # | Scenario | Tags | Step |
|---|---|---|---|
| 38 | `Advance_RegressingProgress_NeverObservesRegressedProgress` | `@in-memory @invariant` | DONE (committed) |
| 39 | `NoRedisOneHost_BehaviourAndCodePathIdenticalToToday` | `@real-io @standalone` | 5 |
| 40 | `SharedAdvance_ConcurrentWriters_OrdinalNeverRegresses` | `@requires-docker @invariant` | 1 |
| 41 | `RedisThreeHosts_SingleSyncPerEntity_TimerAndManualRefresh` | `@requires-docker` | 2 |
| 42 | `RedisBackplane_NotificationOnPodA_ReachesClientOnPodB` | `@requires-docker` | 3 |
| 43 | `GetUpdateStatus_ConsistentAcrossPods` | `@requires-docker` | 1 + 4 |

## Sequencing rationale
Step 0 de-risks the substrate first (ADR mandate). Steps 1→2 build the two coordination primitives (soft store, hard lock) the rest depends on. Steps 3→4 add cross-pod delivery + await. Step 5 proves the standalone gate stayed byte-identical. Step 6 hardens startup refusal. Each step un-skips exactly one scenario (Outside-In), implements, stays GREEN; `@requires-docker` steps gate on Docker on the CI runner.
