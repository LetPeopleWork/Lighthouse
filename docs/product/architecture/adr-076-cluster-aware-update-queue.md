# ADR-076: Cluster-Aware Update Queue — the Queue Itself Is the Cluster-Aware Unit (Not a Timer Leader); **ACCEPTED — Option B (Per-Entity Postgres Advisory Lock + Shared Redis Status Store)** Behind the Existing `IUpdateQueueService` Port

**Status**: **Accepted — Option B chosen (2026-06-20 design SPIKE; live Earned-Trust probe scheduled as slice-07 DELIVER step 0)** — supersedes the Proposed/OPEN state of 2026-06-16. (Morgan, Solution Architect; interaction mode PROPOSE. Inherits System Decision 2 / A1 / D5 and the DDD invariants INV-1..4.)
**Date**: 2026-06-16 (proposed) · 2026-06-20 (SPIKE-resolved → Accepted)
**Feature**: epic-5305-k8s-readiness (ADO Epic #5305)
**Decider**: slice-07 design SPIKE (2026-06-20) — Option B, with Option A retained as the documented fallback if the DELIVER live probe shows advisory-lock liveness is fragile under the real pooler topology
**Relationship to prior ADRs**: AMENDS ADR-027 (single-instance default stands; this adds the config-gated multi-replica branch behind `IUpdateQueueService`). DEPENDS on ADR-075 (the Redis it can reuse). Pairs with ADR-077. Honours the D1 standalone gate and the DDD-architect's INV-1 (monotonic progress), INV-4 (single-active-lifecycle-per-`UpdateKey`), and the no-outbox / idempotency-on-`UpdateKey` confirmation.

---

## Context

`UpdateQueueService` (`UpdateQueueService.cs`) is `AddSingleton` (`Program.cs:934`) but singleton **per process**: each replica owns its own unbounded `Channel<Func<Task>>` (`:11`), its own single `Task.Run` consumer (`StartProcessingQueue`, `:181-197`), its own `awaiters` TCS dict (`:15`), and reads/writes the shared `updateStatuses` `ConcurrentDictionary` (`:14`, injected from the singleton at `Program.cs:932-933`). Dedup ("don't enqueue the same entity twice while one is in flight") is `updateStatuses.TryAdd` (`:46`/`:72`).

Updates fire from **two paths** (D5):
1. **Timer**: `UpdateServiceBase<T>.ExecuteAsync` loops `UpdateAll() → TriggerUpdate(id)` on a `Task.Delay(Interval)` cadence.
2. **Inline manual refresh**: `TeamController.UpdateTeamData → TriggerUpdate`, `PortfolioController.UpdateFeaturesForPortfolio → TriggerUpdate`, and the delete paths via `EnqueueAndAwaitAsync`.

At N replicas the naïve result is the story-07 (C) defect: N× connector calls and **racing Postgres writes for the same entity**, plus a per-pod `updateStatuses` that disagrees (`GetUpdateStatus` on pod A can't see pod B's in-flight work — US-07 AC3). A timer-leader fixes only path 1 and leaves the per-process dedup invisible across pods (necessary-not-sufficient, A1). **The cluster-aware unit must be the queue itself, because both trigger paths flow through the `IUpdateQueueService` port** (`EnqueueUpdate` + `EnqueueAndAwaitAsync`) — making the port cluster-aware covers both.

The DDD-architect has fixed the contract this port must honour:
- **INV-4 (aggregate invariant)**: across the fleet, admission (`TryAdd` / lock-acquire) for a given `UpdateKey` succeeds for **at most one active lifecycle at a time** — the chosen mechanism is the *transactional boundary of the update-lifecycle aggregate*, enforcing a real invariant, not a perf guard.
- **INV-1 (monotonic progress)**: the shared status store advances `UpdateProgress` only (compare-and-set on the ordinal), never regresses; **blind last-writer-wins is rejected**.
- **INV-2 (bounded-stale reads)**: reads may lag; strong consistency is NOT required.
- **No outbox**: the store is a non-authoritative coordination projection; recovery is the next re-sync (ADR-027 D2); the update task + after-commit handlers must stay **idempotent on `UpdateKey`** under at-least-once cluster execution.

## Decision

**Option B — per-entity Postgres advisory lock + shared Redis status store, behind the existing `IUpdateQueueService` port.** Both candidates were evaluated against the full contract (INV-1..4, D1, the two trigger paths, cross-pod awaited completion, liveness/reclaim) in the 2026-06-20 design SPIKE; the discriminator is recorded in **§SPIKE Outcome** below. Both candidates swap the implementation **behind the existing `IUpdateQueueService` port** (signature unchanged — EXTEND, not rewrite) and **both require a shared status store** (the separately-extracted `IUpdateStatusStore`, ADR application-layer detail) to satisfy US-07 AC3 + INV-1/INV-2.

### Option A — Distributed single-consumer queue

Replace the in-process `Channel<Func<Task>>` with a **shared queue** (Redis Stream with a consumer group, or a Postgres-backed work table) drained by **exactly one consumer across the fleet**. Manual refresh enqueues to the shared queue; `EnqueueAndAwaitAsync` awaits completion via the shared status store keyed by `UpdateKey` (the TCS-on-a-single-pod pattern becomes a store-observed terminal-status advance + ADR-075 backplane push). Single-consumer admission IS the INV-4 boundary.

### Option B — Cluster-wide per-entity lock + shared status store (leaning)

Keep each replica's in-process queue, but guard each `(UpdateType, id)` update with a **distributed per-entity lock** (Postgres `pg_advisory_lock` on a key derived from `UpdateKey`, or a Redis lock). The lock acquisition IS the INV-4 admission boundary. Back `GetUpdateStatus` / dedup with the shared status store so reads and dedup agree across pods.

### Quality-attribute trade-off

| Quality attribute | Weight | Option A — distributed queue | Option B — per-entity lock + shared store |
|---|---|---|---|
| Correctness: single sync per entity (US-07 AC1 / INV-4) | Highest | **Strong** — one consumer ⇒ no double-work by construction | Strong *if* the lock is held for the whole update; lock-holder-dies-mid-update needs a TTL + fencing |
| Correctness: awaited completion across pods (`EnqueueAndAwaitAsync`) | Highest | Natural — caller awaits via the shared store keyed by `UpdateKey` | Needs the shared store to signal completion to a *different* pod's awaiter — more wiring |
| Monotonic-progress store (INV-1) | Highest | CAS-on-ordinal in the store (Redis hash / PG row) — same either way | CAS-on-ordinal in the store — same either way |
| Standalone degradation (D1 / US-07 AC4) | Highest | Clean — no queue substrate ⇒ in-process `Channel` verbatim | Clean — no lock provider ⇒ lock is a no-op, in-process queue verbatim |
| Simplicity / operability | High | Lower — introduces queue technology + consumer-group semantics + a "who is the consumer" liveness story | Higher — no new queue; reuses Postgres (advisory lock) or the ADR-075 Redis; **directly extends the `DatabaseMaintenanceGate` mutual-exclusion pattern the codebase already proves** |
| Reuse of existing seams | High | EXTEND `IUpdateQueueService` impl; awaiters move to the store | EXTEND `IUpdateQueueService` impl; models on `DatabaseMaintenanceGate` |
| Failure modes — "what if the substrate lies" (Earned Trust) | High | Redis-Stream "exactly-once" is really at-least-once → consumer must be idempotent on `UpdateKey` (dedup already keys on it); a stuck consumer stalls the fleet | Advisory lock auto-releases on connection drop (good); a network partition can grant two holders → needs fencing; Redlock is contested under partition; **pgBouncer transaction-mode breaks advisory-lock session affinity** |
| Latency at our scale (~30 QPS, background concurrency 1) | Low | One queue round-trip per enqueue | One lock acquire/release per update (~sub-ms on the same PG) |

## Recommendation (PROPOSE — for the user to confirm, SPIKE-validated)

**Lean Option B (per-entity Postgres advisory lock + shared status store), with Option A held as the fallback if the SPIKE shows lock liveness is fragile.** Rationale:
1. It reuses substrate already present in the hosted topology (Postgres for the lock, the ADR-075 Redis for the shared store) and **directly extends the `DatabaseMaintenanceGate` mutual-exclusion pattern the codebase already proves** — smallest new surface, highest operability (ADR-027's highest-weighted attribute).
2. It avoids the queue-technology semantics (consumer groups, "who is the single consumer" election) Option A drags in.
3. At ~30 QPS / background-concurrency-1, lock contention is near-zero, so Option B's main cost is a non-issue here.
4. Postgres advisory locks **auto-release on connection loss**, giving a clean liveness story the SPIKE can verify.

**Do NOT pre-commit in DELIVER.** The slice-07 SPIKE prototypes **both** candidates against real Postgres + Redis with 3 hosts driving timer + manual-refresh concurrently; the one that disproves double-work (connector call count = 1 per entity per cycle, INV-4) **and** keeps awaited-completion consistent under a mid-update pod kill (INV-1/INV-2) wins.

## SPIKE Outcome (2026-06-20 — design probe; live empirical probe = slice-07 DELIVER step 0)

**Chosen: Option B.** The probe at this stage is a **design probe** grounded in the code at HEAD (`UpdateQueueService.cs`, `DatabaseMaintenanceGate.cs`, `DatabaseConfigurator.MigrateUnderAdvisoryLock`) plus the proven slice-04 advisory-lock pattern; the **empirical Earned-Trust probe (mutual-exclusion / dedup / reclaim against real Postgres+Redis with 3 hosts) is the FIRST DELIVER step** (`deliver/slice-07-roadmap.md` step 0), per the ADR's own "the probe implementation is a DELIVER detail" framing. The design probe is sufficient to pick the shape because the decisive discriminator is structural, not performance:

**Decisive discriminator — Option A's headline advantage evaporates under the two-trigger-path reality.** Option A ("natural awaited completion on the single consumer") is only natural for callers that happen to be served by the consumer pod. With two trigger paths (timer + inline manual refresh on *whatever* pod serves the request, D5), a manual refresh served by a *follower* pod must **always** await cross-pod via the shared status store + ADR-075 backplane push anyway — exactly the wiring Option B needs too. So A buys nothing on awaited-completion, while still dragging in queue-technology semantics (consumer groups), a "who is the single consumer" liveness/election story, and a second degrade branch at N=1. Option B gets single-sync (INV-4) from the per-entity lock without any of that.

**Why Option B wins on the weighted attributes (ADR-027 ranks operability highest):**
1. **Reuse of a proven primitive.** Option B's per-entity lock is the *parameterized* form of slice-04's shipped `MigrateUnderAdvisoryLock` (`DatabaseConfigurator.cs:108-136`): session-scoped `pg_advisory_lock` on a dedicated connection, released in `finally`, **auto-released on connection drop**. Smallest new surface; the codebase already proves the pattern + its test shape (`ConcurrentStartupMigrationTests`).
2. **Clean liveness/reclaim.** Postgres advisory locks auto-release when the holding connection dies → a pod killed mid-update releases the lock with no TTL/fencing machinery (Earned-Trust probe (c)). Redlock-style Redis locks are contested under partition (rejected as the lock substrate).
3. **No queue technology / no leader election.** Each replica keeps its in-process `Channel` queue verbatim; the lock is the only cross-pod addition. At N=1 the lock is a no-op → byte-identical standalone (D1) with a *single* degrade branch.
4. **Latency is a non-issue** at ~30 QPS / background-concurrency-1; uncontended manual refresh stays local (today's path), only contended work dedups to an awaited store-observation.

**Chosen substrate split (co-designed admission boundary, INV-4):**
- **Hard mutual-exclusion (INV-4 boundary) = Postgres per-entity advisory lock.** Key derived from `UpdateKey` as a stable `bigint` (`(long)(int)updateType << 32 | (uint)id`). Acquired **at execution time inside the consumer** (not at enqueue — avoids holding a session lock across the enqueue→consume thread hop), on a dedicated Npgsql connection held for the update duration and released in `finally` — the slice-04 pattern, parameterized by key. `pg_try_advisory_lock` for the non-blocking admission test; `pg_advisory_lock` (blocking) is the serialization backstop if the soft dedup races.
- **Soft cluster-wide dedup + read/await projection (INV-1/2, AC3) = shared Redis status store.** A Redis hash keyed by `UpdateKey`; `Advance` is a **monotonic compare-and-set on the `UpdateProgress` ordinal** (Lua/`WATCH`), never blind LWW (INV-1). `TryAdmit` is `HSETNX`-style soft dedup so two pods don't both enqueue+run in the common case; the advisory lock is the hard guarantee for the race window. Reads (`GetUpdateStatus`) are bounded-stale (INV-2) — no synchronous distributed read on the hot path.
- **Cross-pod awaiter release.** `EnqueueAndAwaitAsync` keeps an in-process `awaiters` TCS dict (`UpdateQueueService.cs:15`). For a caller awaiting on a *different* pod than the one that runs the update, the running pod publishes a terminal-advance signal over **Redis pub/sub** (a small dedicated channel, distinct from the SignalR client backplane which targets browser clients, not server-side TCS); each pod's `UpdateQueueService` subscribes and releases any local awaiter for that `UpdateKey`. In-process adapter does this in-process exactly as today.

**Both substrates gated on `ConnectionStrings:Redis` present** (the same single gate as ADR-075). The hosted multi-replica topology always has Redis **and** Postgres; absent the gate ⇒ `InProcessUpdateStatusStore` + in-process `Channel` + in-process awaiters, **byte-identical to today** (D1 / US-07 AC4). A `ConnectionStrings:Redis`-set-but-SQLite-provider combination is a misconfiguration the startup probe surfaces (advisory lock needs Npgsql).

**Scope correction surfaced by the SPIKE (DESIGN missed it): a THIRD consumer of the raw `updateStatuses` singleton.** `DatabaseMaintenanceGate` (`DatabaseMaintenanceGate.cs:11,103-107`) injects the same `ConcurrentDictionary<UpdateKey,UpdateStatus>` and its `HasActiveBackgroundWork()` enumerates `.Values` for `Queued`/`InProgress` before allowing a backup/restore. At N pods "is any background work active *anywhere*" is a cross-pod question, so `IUpdateStatusStore` MUST expose a `HasActiveWork()` (or enumerable) member and `DatabaseMaintenanceGate` must move onto the port. The shared adapter answers it from the Redis hash; the in-process adapter answers it from the dict (today). This is an explicit roadmap step (slice-07 step 2).

## Earned-Trust Probe (MANDATORY, BOTH options — first-class design responsibility)

This section fixes the probe *contract* (what it must empirically demonstrate); the probe *implementation* is a DELIVER detail, not a DESIGN one.

The chosen substrate is a driven adapter on an external dependency that is known to lie. It MUST expose a `probe()` that the composition root runs at startup (wire → **probe** → use) BEFORE the cluster-aware path is permitted to serve. The probe empirically exercises the specific lie:
- **(a) mutual exclusion**: acquire the per-entity lock (B) / claim a sentinel from the consumer group (A) from **two connections** and assert exactly one wins — catches a misconfigured advisory-lock scope or a pgBouncer transaction-mode proxy that silently breaks session affinity, and a Redis that buffers under partition.
- **(b) exactly-once *effect* given at-least-once *delivery*** (A) / **dedup** (B): enqueue/claim a sentinel `UpdateKey` twice and assert the *effect* happens once (idempotent dedup on `UpdateKey`).
- **(c) reclaim on holder death**: kill the holder/consumer connection and assert the lock/claim is reclaimed within the TTL.

A failing probe causes the cluster-aware path to **refuse to start with a structured `health.startup.refused` event naming the lie** (e.g. "advisory lock not mutually exclusive — connection pooler likely in transaction mode; use session-mode pooling"), and to fall back to single-instance only if explicitly configured. **The probe is part of the DELIVER slice, not a later hardening pass.** It is enforced (per the methodology's three orthogonal layers) by the adapter-probe contract that ADR-077's migration lock also satisfies — they share the same advisory-lock-on-real-Postgres concern.

## Standalone Degradation (D1 / US-07 AC4)

No Redis and no distributed-lock provider ⇒ the lock is a **no-op** (B) / the queue is the in-process `Channel` (A), and `IUpdateStatusStore` is the in-process `ConcurrentDictionary` — **behaviour AND code path identical to today**. The timer updaters run in the single process; no leader is needed at N=1. INV-1..4 are satisfied by the existing in-process code: `ConcurrentDictionary.TryAdd` is INV-4; the one consumer's in-place `updateStatus.Status =` mutation is INV-1 (monotone, single writer); a single-process read is trivially INV-2/INV-3 (DDD §standalone-degradation).

## Alternatives Considered

**Chosen framing: the queue itself is the cluster-aware unit (Option A or B behind `IUpdateQueueService`).**

**Rejected as sufficient — leader election for the timer only.** Elect one replica to run Team/Portfolio/ForecastUpdater. Does nothing for a manual refresh handled by a follower, and the per-process dedup stays invisible across replicas — the same entity can still be updated concurrently and race the same Postgres rows. Necessary-not-sufficient (story-07 §1, A1). Kept only as a *possible component* of a fuller design, not the design.

**Rejected — do nothing / let pods race.** Concurrent `TriggerUpdate` across pods produces N× connector calls and racing writes; correctness violation (US-07 AC1).

## Consequences

**Positive**:
- Single sync per entity (US-07 AC1 / INV-4) and consistent `GetUpdateStatus` across pods (US-07 AC3 / INV-1/2) regardless of N; both trigger paths covered (D5).
- The seam already exists (`IUpdateQueueService` + the shared `updateStatuses` singleton) — EXTEND, not rewrite.
- Degrades to the verbatim standalone path (D1).

**Negative**:
- The decision is OPEN until the SPIKE — the single highest-risk item in the epic; DELIVER must not commit the shape early.
- A new config-gated substrate adapter is unavoidable (no distributed primitive exists today), though it is thin and lives behind existing ports.

**Neutral**:
- The shared status store (`IUpdateStatusStore`) is needed by *both* options and is extracted independently; its consistency invariant (INV-1) is substrate-agnostic (CAS-on-ordinal works on a Redis hash or a Postgres row).

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| Both options swap the impl behind `IUpdateQueueService` — signature unchanged | ArchUnit / signature test: `IUpdateQueueService` retains `EnqueueUpdate` + `EnqueueAndAwaitAsync`; callers (controllers, `UpdateServiceBase`) unchanged |
| Single sync per entity at N=3 (INV-4) | SPIKE acceptance: 3 hosts, concurrent timer + manual refresh, connector call count = 1 per entity per cycle |
| Awaited completion consistent under mid-update pod kill (INV-1/2) | SPIKE acceptance: kill the holder mid-update; the awaiting pod's `EnqueueAndAwaitAsync` resolves via the store + backplane; no regressed `UpdateProgress` observed |
| Substrate probe runs before serving; refuses on lie | Composition-root probe emits `health.startup.refused`; the three-layer adapter-probe contract (subtype/structural/behavioural) verifies the probe exists and exercises (a)(b)(c) |
| Standalone (no Redis/no lock) path is byte-identical | Existing `UpdateQueueService` tests + `PortfolioDeleteSerialisationTests` run unchanged (US-07 AC4) |
| Idempotency on `UpdateKey` preserved end-to-end | Re-running a completed sync re-derives the same DB state (DDD Confirmation 3 caveat) — asserted by re-execution test |
