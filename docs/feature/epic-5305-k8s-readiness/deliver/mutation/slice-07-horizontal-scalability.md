# Mutation Testing — Slice 07: Horizontal Scalability (in-process substrate)

Config: `Lighthouse.Backend.Tests/stryker-config.epic-5305-slice-07-scalability.json`
Output: `StrykerOutput-epic-5305-slice-07/reports/mutation-report.html`

## Scope

Mutation covers the **in-process substrate** introduced by this slice — the code path
exercised when `ConnectionStrings:Redis` is absent (the sacrosanct single-container default):

- `UpdateQueueService` — substrate seams: `ReleaseAwaiter`, `IsDistributed` cross-pod-awaiter
  gating, `PublishCompletionAsync` wiring, terminal-status notify, `Dispose` of the subscription.
- `InProcessUpdateExecutionLock` — no-op lock (single-container already serializes via the queue).
- `InProcessUpdateCompletionNotifier` — non-distributed notifier (no fan-out).

The distributed adapters — `RedisUpdateStatusStore`, `PostgresUpdateExecutionLock`,
`RedisUpdateCompletionNotifier`, `ClusterSubstrateHealthCheck` — are **excluded from the mutate
set**. They are covered by `@requires-docker` Testcontainers integration tests
(`Integration/Containers/*`) that run real Postgres + Redis to assert advisory-lock exclusion,
monotonic-CAS, cross-pod pub/sub delivery, and the startup substrate probe. Stryker's unit harness
cannot drive those (no broker in-process), so mutating them would report uniform NoCoverage rather
than a meaningful signal; their behaviour is gated by integration assertions instead.

## Result

- **Raw score: 62.0%** (49 detected / 30 survived+nocov on the three mutated files).
- **Introduced-surface adjusted: 96.1%** — exceeds the 80% per-feature gate.

The gap between raw and adjusted is structural: `UpdateQueueService` is a logging-heavy queue
orchestrator, and the whole file is mutated (the new seams are line-interleaved with pre-existing
code), so the denominator carries mutants that are not behavioural.

### Justified survivors (28)

| Category | Count | Justification |
|---|---|---|
| `logger.Log*` string / statement mutants | 20 | Log output is deliberately untested (house rule); asserting log text is an anti-pattern. |
| `TrySetResult(true→false)` ×3 | 3 | The `bool` result is never surfaced — the public API returns plain `Task`, so `true`/`false` complete it identically. |
| `?? updateStatus` null-coalescing | 1 | Equivalent in-process: `TryAdmit(key, updateStatus)` stores that exact reference, so `Advance(...)` returns the same object; the fallback is unreachable. Behavioural teeth (fresh-deserialized status) live only in `RedisUpdateStatusStore` → integration-covered. |
| `registration.Dispose()` / `GC.SuppressFinalize(this)` | 2 | Resource-cleanup with no observable behaviour (no finalizer; CT registration is short-lived). |
| `observer.TrySetCanceled()` (NoCoverage) | 1 | Dead defensive branch: the inner TCS is only ever `SetResult`/`SetException`, never cancelled, so `t.IsCanceled` is unreachable. |
| `Group("GlobalUpdates")` routing literal | 1 | Presentational SignalR group name; tests assert the send occurs, not the literal. |

### Accepted real survivors (2)

| Line | Mutant | Why accepted |
|---|---|---|
| `Advance(updateKey, InProgress)` (fire-and-forget) | statement removal | Transient state: the entry flips Queued→InProgress→Completed faster than a test can observe. Asserting it deterministically requires gating the work and racing the status read — flaky. The terminal Completed/Failed transition and removal are fully asserted. |
| `Advance(updateKey, InProgress)` (awaitable) | statement removal | Same transient-state rationale on the awaitable path. |

## Tests added

- `UpdateQueueServiceTests` — cross-pod awaiter gating (distributed vs non-distributed), `ReleaseAwaiter`
  via the captured subscription callback, `PublishCompletionAsync` on both paths, terminal-status notify
  payload identity, status removal after awaitable completion, `Dispose` of the subscription, and the
  cancellation-aware observer (token-cancelled / success / fault under a cancellable token).
- `InProcessUpdateAdaptersTest` — non-distributed notifier contract (no fan-out, disposable subscription)
  and the no-op execution lock (same-key concurrent acquire does not block).
- `ScalabilitySubstrateSeamArchUnitTest` — guards the `IUpdateQueueService` caller contract
  (`EnqueueUpdate` / `EnqueueAndAwaitAsync` signatures unchanged).
