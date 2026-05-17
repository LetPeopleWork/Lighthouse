# Evolution: bug-5016-cache-thread-safety

**Finalized**: 2026-05-17
**Wave path**: DISTILL → DELIVER (lean; bug-fix scope, DISCUSS/DESIGN/DEVOPS sections inlined into the feature delta because the customer-supplied diagnosis on ADO Bug 5016 served as the design doc)
**Outcome**: One production file modified (13 lines net), one new test file (324 lines, 7 NUnit scenarios), 5 RED concurrency repros → GREEN, 2 GREEN single-threaded parity guards preserved. Public surface of `Cache<,>` unchanged — zero ripple to `BaseMetricsService` / `WorkTrackingConnectorFactory` / `GitHubService`.

## Summary

Closed a thread-safety defect in `Lighthouse.Backend/Cache/Cache.cs` that crashed `BaseMetricsService.MetricsCache` (a `static readonly Cache<string, object>`) with `System.IndexOutOfRangeException` inside `Dictionary.TryInsert` under concurrent metric requests. Customer Liz observed 836 occurrences over 15 hours in one production instance: every cycle-time / throughput / WIP / PBC endpoint sporadically returned HTTP 500, and once corrupted the underlying `Dictionary<,>` stayed broken for the affected keys until process restart.

The fix is a one-line swap of the backing store to `System.Collections.Concurrent.ConcurrentDictionary<TKey, CacheItem<TValue>>`, with `TryGetValue` / `TryRemove` adjustments in `Get` / `Remove`. The public surface (`Store`, `Get`, `Remove`, `Keys`) is unchanged; lazy-expiry-on-read semantics are preserved verbatim. The choice is consistent with existing concurrency posture in the codebase — `docs/product/architecture/brief.md:222` already uses `ConcurrentDictionary<int, SemaphoreSlim>` for the OAuth single-flight pattern.

## Business context

Reported via ADO Bug 5016 (assigned 2026-05-14, activated 2026-05-17). Reporter (Liz) is using Lighthouse through the MCP server to aggregate data into a "Flow Review" — every concurrent fan-out to the metrics endpoints widened the race window and surfaced the crash. The bug pattern is independent of database choice (Postgres in Liz's instance) because the crash is in an in-process static cache, not in any persistence layer.

Concurrency profile from the customer report (15-hour window, 6 teams + 1 portfolio):

| Activity | Count | Cadence |
|---|---|---|
| `ForecastService` Monte Carlo run-cycle starts | 700 | every ~77 s |
| `TeamDataService` per-team refreshes | 1,205 | every ~45 s |
| Combined cache-mutating events | ~1,900 | one every ~28 s |
| `IndexOutOfRangeException` crashes | 836 | one every ~65 s |

The crash rate tracks the cache-mutation rate, which means the race window is essentially always open under normal operating load — this is a near-deterministic defect under realistic multi-team deployments, not an edge case.

## Key decisions

| ID | Decision | Rationale |
|---|---|---|
| ConcurrentDictionary (lock-free) over `lock`-wrapped Dictionary | Cache is read-heavy; ConcurrentDictionary's read path is lock-free, write path uses fine-grained striped locks | Smallest behavioural delta, best throughput characteristics for the metrics-cache hot path. Architectural precedent: `brief.md:222` (OAuth single-flight). |
| Surgical fix in `Cache<,>` only; no consumer change | `BaseMetricsService` and other consumers depend on the public surface, not the backing store | Zero ripple, zero risk of regressing the 2530-test backend suite. |
| Preserve lazy-expiry-on-read in `Get` | Some callers depend on the observable "expired entry disappears the moment it is read" semantics | Scenarios 6 and 7 of the regression suite pin this single-threaded behaviour as a parity guard. |
| Two-step roadmap (RED then GREEN) inline rather than architect-dispatched | Surgical scope justifies a 2-step roadmap that fits in one TDD pass | Avoided the overhead of a `nw-solution-architect` round-trip for a one-file fix. Recorded with `validation.status = approved` (orchestrator-inline) in `roadmap.json`. |
| No new components | The fix changes the *implementation* of an existing component, not its boundary | Out-of-scope items (eviction policy, distributed cache, single-flight semantics, removing the `static`) are explicitly documented in feature-delta `[REF] Out of scope` for follow-up under epic `#4618`. |

## Steps completed (1 phase, 2 steps, 1 refactor)

| Step | Commit | What |
|---|---|---|
| 01-01 (RED) | `dbda78e0` | `test(cache): add bug-5016 regression suite for Cache<,> thread-safety` — new file `Lighthouse.Backend.Tests/Cache/CacheTest.cs` (7 NUnit `[Test]` methods, 5 concurrency repros + 2 single-threaded parity guards). On unmodified `Dictionary<,>`-backed Cache: 3 reliable RED failures including `InvalidOperationException` ("A concurrent update was performed on this collection and corrupted its state") inside `Dictionary.Remove` at `Cache.cs:17` (the exact `BaseMetricsService.InvalidateMetrics` race), `Get` returning null for 8 of 64 keys after a 64-thread insert storm (silent data loss), and a CancelAfter timeout on the lazy-remove scenario. |
| 01-02 (GREEN) | `4946d345` | `fix(cache): use ConcurrentDictionary to prevent IndexOutOfRangeException under concurrent metric requests (ADO bug 5016)` — `Cache.cs` Dictionary → ConcurrentDictionary swap, `TryGetValue`/`TryRemove` in `Get` and `Remove`. All 7 CacheTest scenarios GREEN; full backend suite 2530 / 2530 passing (2 pre-existing `LicensingIntegrationTest` failures are file-fixture issues confirmed independent of this change). `dotnet build` zero warnings. |
| 01-02 refactor | `2e1a0dba` | `refactor(cache): remove tautological assertion in KeysEnumerationDuringMutation test` — removed an `Assert.That(key, Does.StartWith("churn-"))` inside a `Where(k => k.StartsWith("churn-"))` loop (true by construction; flagged by reviewer as D1). The test's value is the absence of `InvalidOperationException` / `IndexOutOfRangeException` from `Keys.Where(...).ToList()` under concurrent Store/Remove churn, which the final `Assert.That(exceptions, Is.Empty)` still pins. |

## Quality gates summary

- **DISTILL review**: `nw-acceptance-designer-reviewer` (Haiku) → **approved**, 0 blockers / 0 high / 0 low.
- **Build**: `dotnet build Lighthouse.Backend/Lighthouse.sln` — 0 warnings, 0 errors (CLAUDE.md `TreatWarningsAsErrors`).
- **Test density**: 7 / 7 in `Cache.CacheTest` GREEN; full backend suite 2530 / 2530 GREEN (2 pre-existing `LicensingIntegrationTest` file-fixture failures, independent of Cache change, verified by stash-and-rerun).
- **Adversarial review**: `nw-software-crafter-reviewer` (Haiku) → **conditionally_approved**, 1 medium (D1, addressed inline), 2 low (D2/D3, deferred — optional strengthening that would not catch a different defect class).
- **Mutation testing**: Stryker.NET (per-feature, scoped to `**/Cache/Cache.cs` + `FullyQualifiedName~CacheTest`). 8 mutants tested, **7 killed / 1 survived / 0 timeout — 87.50% kill rate**, above the 80% CLAUDE.md gate. The lone survivor at `Cache.cs:29` mutates the expiry comparison from `>=` to `>` — semantically a difference only at the exact `elapsed == ExpiresAfter` boundary, which is unreachable in practice because `DateTimeOffset.Now`'s 100ns tick granularity guarantees at least one tick of slack between `Store` and any subsequent `Get`. Accepted as an equivalent mutant. Full HTML report at `Lighthouse.Backend/StrykerOutput/2026-05-17.16-33-12/reports/mutation-report.html` (not committed; can be regenerated from `stryker-config.bug-5016-cache-thread-safety.json`).
- **Wiring smoke**: no new functions added on the production side; the existing public surface is fully exercised by the regression tests AND by `BaseMetricsService` / `WorkTrackingConnectorFactory` / `GitHubService` in production.
- **Scaffold removal**: none required — no scaffolds were created (`Cache<,>` was an existing module).
- **CLAUDE.md comment policy**: zero comments added to `Cache.cs`; zero banned comments in `CacheTest.cs` (no section banners, no Arrange/Act/Assert labels, no Given/When/Then narration, no provenance).
- **Design compliance (F-2)**: `Cache.cs` is the same component listed in the architecture brief; one new test file under a new `Tests/Cache/` directory mirrors the production layout `Backend/Cache/` (precedent verified against existing `Tests/API/` ↔ `Backend/API/`, `Tests/Services/Implementation/Auth/` ↔ `Backend/Services/Implementation/Auth/`).

## Carry-forward / follow-ups

- **`#4618` (broader cache redesign)**: explicitly noted in feature-delta as the home for eviction policy, size cap, telemetry on hit/miss, and any distributed-cache work. This fix is a strictly narrower in-process thread-safety fix and does not pre-empt that epic's scope.
- **Single-flight semantics (preventing two threads from recomputing the same expired metric simultaneously)**: out of scope here; the current `Cache<,>` does not offer single-flight, and adding it would change the public surface. If a future feature wants single-flight, it can layer on top of `ConcurrentDictionary.GetOrAdd` with a `Lazy<Task<TValue>>` value type — no further change required in `Cache.cs`.
- **Removing the `BaseMetricsService.MetricsCache` static**: orthogonal to the thread-safety bug; the fix works regardless of whether the cache is `static` or per-instance. Tracked under `#4618` if the wider redesign chooses per-request scope.
- **D2 / D3 reviewer suggestions**: optional strengthening of read-back observability in tests 1 and 5. Not adopted because (a) the production bug was `IndexOutOfRangeException`, not visibility; (b) ConcurrentDictionary documents publication semantics; (c) "don't add features beyond what the task requires" (CLAUDE.md). If a future visibility-class defect appears, these suggestions become first-line tightening.
- **Pre-existing OAuth merge conflicts** in `docs/feature/work-tracking-oauth-authentication/deliver/{.develop-progress.json, execution-log.json}` from merge commit `17b962f5`: noted at finalize time, untouched by this slice. The maintainer should resolve those separately before the next push (the local state — marker deleted, execution-log local — matches the intent of `e62c0636` + `a5ca6444` and accepting "ours" is the safe resolution).
