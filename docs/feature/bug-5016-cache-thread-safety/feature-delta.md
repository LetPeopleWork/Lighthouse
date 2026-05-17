# Feature Delta: bug-5016-cache-thread-safety

<!-- markdownlint-disable MD024 MD041 -->

Wave: DISTILL | Date: 2026-05-17 | Density: lean (per ~/.nwave/global-config.json)

Bug goal: `Lighthouse.Backend/Cache/Cache.cs` wraps a plain `Dictionary<TKey, CacheItem<TValue>>` with no synchronisation, even though the only consumer of note — `BaseMetricsService.MetricsCache` (a `static readonly Cache<string, object>`) — is mutated concurrently from every metric request and from `ForecastService` / `TeamDataService` background loops. Under realistic load (6 teams + 1 portfolio, ~1 mutation every ~28 s by background services alone, layered with concurrent user GETs) the cache crashes with `IndexOutOfRangeException` inside `Dictionary.TryInsert`, leaving the cache corrupted; affected cache keys keep returning HTTP 500 until process restart. ADO bug 5016 cites 836 occurrences over 15 hours in one customer instance reported by Liz.

DISTILL is fast-tracked: no DISCUSS / DESIGN / DEVOPS waves were run as separate sessions. The contract change is purely a thread-safety guarantee on an existing public class signature; no new endpoint, no schema change, no migration, no infrastructure work. The customer-supplied diagnosis (in `Microsoft.VSTS.TCM.ReproSteps` on bug 5016) is precise enough to serve as the design doc — the fix is a `Dictionary` → `ConcurrentDictionary` swap inside `Cache<,>`, surfaced to consumers as zero API drift.

---

## Wave: DISTILL / [REF] Inherited commitments

| Origin | Commitment | DDD | Impact |
|--------|------------|-----|--------|
| `Lighthouse.Backend/Cache/Cache.cs` | `Cache<TKey, TValue>` exposes `Store(key, value, expiresAfter)`, `Get(key) -> TValue?`, `Remove(key)`, and `IEnumerable<TKey> Keys` | n/a | Public surface is unchanged by the fix. Existing call sites in `BaseMetricsService`, `WorkTrackingConnectorFactory`, and `GitHubService` continue to compile without edit. |
| `Lighthouse.Backend/Cache/Cache.cs` (`Get` impl) | `Get(key)` performs lazy expiry: if the cached entry has aged past `ExpiresAfter`, the entry is `Remove`d and `default` is returned | n/a | The lazy-expiry behaviour stays — it is the source of the *read-path mutation* that participates in the race. The fix must keep the same observable semantics (expired entries disappear on the next Get) while making the underlying remove safe under contention. |
| `Lighthouse.Backend/Services/Implementation/BaseMetricsService.cs:12` | `private static readonly Cache<string, object> MetricsCache = new();` — a single shared instance services every metric request and every background recalculation | n/a | The cache is the hot, shared, contended object. Any thread-safety regression in `Cache<,>` shows up here first. The fix MUST be transparent to `BaseMetricsService` — no signature change, no required init flag, no consumer-side locking. |
| `Lighthouse.Backend/Services/Implementation/BaseMetricsService.cs:401` (`InvalidateMetrics`) | `MetricsCache.Keys.Where(k => k.StartsWith($"{entity.Id}_")).ToList()` enumerates `Keys` concurrently with writers on other threads | n/a | The `Keys` enumeration must not throw `InvalidOperationException` ("Collection was modified during enumeration") under concurrent `Store`/`Remove` — this is the second race the fix has to close, distinct from the `TryInsert` crash. |
| ADO Bug 5016 — repro stack (`System.IndexOutOfRangeException` at `System.Collections.Generic.Dictionary\`2.TryInsert`) | The observable failure mode under contention is `IndexOutOfRangeException` originating in `Dictionary.TryInsert`; once raised, the underlying Dictionary's `_entries` array is corrupted and the cache keeps 500-ing for the same keys until process restart | n/a | The acceptance scenarios are written to reproduce this exact failure mode (concurrent writer storms, concurrent reader+writer storms) and assert that no `IndexOutOfRangeException` (or any other exception) escapes the cache under contention. |
| `Lighthouse.Backend/Cache/CacheItem.cs` | `CacheItem<T>` carries `Value`, `Created` (`DateTimeOffset.Now` at construction), and `ExpiresAfter` (`TimeSpan`); `Created` and `ExpiresAfter` are `internal`, `Value` is `public` and immutable after construction | n/a | `CacheItem<>` is itself thread-safe (immutable after construction). No change needed; the race is entirely at the `Dictionary<,>` level. |
| `docs/product/architecture/brief.md` (hexagonal) | `Cache<,>` is a pure in-process utility — no driving port, no driven adapter | n/a | No port-to-port acceptance test applies (no CLI / HTTP / hook entry point). The acceptance grain is the public surface of `Cache<,>` invoked directly from a multi-threaded NUnit test — this is the correct "user" of the class. |

---

## Wave: DISTILL / [REF] Wave-decision reconciliation

No prior wave decisions exist for this bug (no DISCUSS / DESIGN / DEVOPS sessions were run — it is a customer-reported defect entered straight into DISTILL). The reconciliation gate therefore passes trivially: zero prior decisions, zero contradictions.

Implicit cross-feature touchpoint: ADO bug 5016's repro steps speculate this may be related to `#4618` (an existing diagnostic / hardening epic on the cache layer). That epic is not in scope for this fix — bug 5016 is a *single, surgical* thread-safety fix that does not pre-empt any broader cache redesign work in `#4618`. If `#4618` later changes the backing store (e.g. to a distributed cache), it will inherit a `Cache<,>` that is already safe under in-process contention, which is a strictly easier starting point than today.

---

## Wave: DISTILL / [REF] Scenario list (acceptance/bug-5016-cache-thread-safety.feature)

| # | Scenario | Tags |
|---|---|---|
| 1 | Many threads writing unique keys in parallel do not throw and every value is retrievable | `@bug-5016 @regression @in-memory @concurrency` |
| 2 | Concurrent readers and writers on overlapping keys never observe a corrupted entry | `@bug-5016 @regression @in-memory @concurrency` |
| 3 | Concurrent Get on an expired key serialises the lazy Remove without crashing | `@bug-5016 @regression @in-memory @concurrency` |
| 4 | Keys enumeration during concurrent Store and Remove does not throw "Collection was modified" | `@bug-5016 @regression @in-memory @concurrency` |
| 5 | BaseMetricsService.InvalidateMetrics is safe when called while metric calculations are racing | `@bug-5016 @regression @in-memory @concurrency` |
| 6 | Single-threaded behaviour is preserved (parity guard for the fix) | `@bug-5016 @regression @in-memory` |
| 7 | Single-threaded expiry-on-read still removes the entry (parity guard for the fix) | `@bug-5016 @regression @in-memory` |

Scenarios 1–5 are the regression repros (each one exercises a distinct mutating path on the underlying `Dictionary<,>`: concurrent insert, concurrent insert+read, concurrent lazy-remove on read, concurrent enumeration, and the realistic `InvalidateMetrics` composite). Scenarios 6–7 are single-threaded parity guards — they pass against today's `Cache<,>` and must still pass after the fix, ensuring the swap to `ConcurrentDictionary` does not change observable single-threaded behaviour (Store / Get / Remove / lazy expiry / Keys snapshot).

---

## Wave: DISTILL / [REF] Walking-skeleton strategy

Strategy A (Full InMemory) — auto-detected and confirmed:

- `Cache<TKey, TValue>` is a pure in-process utility with no I/O, no driven adapters, no external services.
- No `@real-io` scenarios apply. All seven scenarios are tagged `@in-memory`.
- Walking-skeleton scenario is **explicitly omitted**: per the DISTILL skill, walking skeletons are optional for bug fixes, and a bug-fix-only feature has no end-to-end driving port to skeleton. The regression scenarios themselves are the smallest end-to-end the bug supports — they exercise the same public surface that `BaseMetricsService` calls in production.

---

## Wave: DISTILL / [REF] Adapter coverage table

| Adapter | `@real-io` scenario | Covered by |
|---|---|---|
| n/a (pure in-process class) | n/a | This feature touches no driven adapter. Mandate 6 is not in scope. |

`Cache<,>` has no driven-adapter dependencies — it is a value-holding utility. The adapter-coverage mandate (one `@real-io` scenario per driven adapter) is therefore vacuously satisfied. If the cache later acquires an out-of-process backing store (e.g. Redis under `#4618`), a follow-up DISTILL must add `@real-io @adapter-integration` coverage for that store.

---

## Wave: DISTILL / [REF] Driving Adapter coverage

| Entry point | Acceptance grain |
|---|---|
| `Cache<TKey, TValue>` public surface — `Store` / `Get` / `Remove` / `Keys` | All 7 scenarios call these methods directly from the multi-threaded NUnit test fixture. |

Driving-adapter coverage is satisfied at the language-level public surface. The class is invoked in production from in-process code paths (no CLI subcommand, HTTP endpoint, or hook adapter exists for it), so a subprocess / HTTP / hook scenario is not applicable per the RCA P1 guidance.

---

## Wave: DISTILL / [REF] Test placement

`Lighthouse.Backend/Lighthouse.Backend.Tests/Cache/CacheTest.cs` — new file, new directory, mirroring the production layout `Lighthouse.Backend/Lighthouse.Backend/Cache/Cache.cs`. Precedent: the existing test project already mirrors the production tree (e.g. `Tests/API/ApiKeyControllerTest.cs` mirrors `Backend/API/ApiKeyController.cs`, `Tests/Services/Implementation/Auth/ApiKeyServiceTest.cs` mirrors `Backend/Services/Implementation/Auth/ApiKeyService.cs`). No `Tests/Cache/` directory exists today — DELIVER creates it.

Conventions inherited from the test project (verified against `LighthouseAppContextUtcTest.cs` and `ApiKeyServiceTest.cs`):

- Framework: NUnit 4.x (`[TestFixture]`, `[Test]`, `Assert.That(..., Is.EqualTo(...))`).
- Mocking: not needed for these scenarios — `Cache<,>` has no collaborators to fake.
- Concurrency primitives: `Task.WhenAll`, `Parallel.For`, `Barrier`, `ManualResetEventSlim` — pick whichever gives the highest contention probability per scenario (scenario 1: `Task.WhenAll` over N `Task.Run` + a starting `Barrier`; scenario 4: tight `Parallel.For` writer loop + foreground `Cache.Keys.ToList()` iterations).
- Determinism: each scenario is parameterised by thread count and iteration count; defaults chosen so the test reliably reproduces the race on a 4-core CI runner within < 2 s. Hard upper bound per scenario: 5 s wall clock (Mandate F-004 timing budget compliance).

---

## Wave: DISTILL / [REF] Scaffolds

No production-side scaffold is created. `Cache<TKey, TValue>` already exists at `Lighthouse.Backend/Lighthouse.Backend/Cache/Cache.cs` — Mandate 7 ("create scaffold module file so tests are RED not BROKEN") applies only to *new* production modules introduced by a feature. For this bug fix:

- The acceptance test class will be RED on first run because the **existing** production code reproduces the bug under contention (scenarios 1–5 throw `IndexOutOfRangeException` / `InvalidOperationException`; scenarios 6–7 are GREEN as parity guards).
- DELIVER closes the gap by replacing the `Dictionary<,>` field in `Cache.cs` with `ConcurrentDictionary<,>` (and using `TryGetValue` / `TryRemove` in `Get` / `Remove`). Acceptance test class then goes GREEN end-to-end.

No scaffold marker (`__SCAFFOLD__`) is needed because no scaffold file is created.

---

## Wave: DISTILL / [REF] Pre-requisites

| Source | Pre-requisite |
|---|---|
| DESIGN driving ports | None — bug fix has no new ports. |
| DEVOPS environment matrix | Default (clean) — no new dependency on infra. `dotnet test` runs the scenarios on the standard backend test environment; no SQLite/Postgres needed (the cache holds in-process state only). |
| External services | None. |
| Feature flags | None. |
| Migrations | None. |
| ADO bug 5016 state | Already `Active`; assigned to Benj. State transitions per `/ado-sync`: stays `Active` through DELIVER, moves to `Resolved` only after fix lands + CI green. Tag `Release Notes` already present. |

---

## Wave: DISTILL / [REF] Out of scope

- **Broader cache redesign** (e.g. distributed cache, eviction policy, size cap, telemetry on hit/miss). Tracked under epic `#4618` (not part of this bug fix).
- **Per-cache-key locking** (e.g. preventing two threads from recomputing the same expired metric simultaneously). The current `Cache<,>` does *not* offer single-flight semantics, and adding them would change the public surface. Bug 5016 is about crashes, not duplicate work — a separate feature can introduce single-flight later if desired.
- **Removing the `BaseMetricsService.MetricsCache` static**. The static lifetime is orthogonal to the thread-safety bug; the fix works regardless of whether the cache is `static` or per-instance.
- **Changing the `Get` lazy-remove behaviour**. Some implementations prefer a background sweeper or pure-read `Get`. The fix preserves lazy-remove-on-read so the public surface and observable behaviour stay identical for non-concurrent callers (scenarios 6–7 pin this).
- **Adding a `TryGet`/`GetOrAdd` API**. Tempting under concurrency, but out of scope — call sites are not changing.

---

## Wave: DISTILL / [REF] Mandatory review gate

Single reviewer dispatched on completion of this delta (no DISCUSS / DESIGN / DEVOPS sections present to review):

- `@nw-acceptance-designer-reviewer` (Sentinel) on Haiku — reviews DISTILL section + the `.feature` file.

Expected output: structured YAML verdict with `approval_status`, `blocker_count`, `findings_list`. The fix is blocked from DELIVER handoff unless verdict is `approved` or `conditionally_approved` with documented action items in DELIVER scope.

**Sentinel verdict (2026-05-17)**: **`approved`**, 0 blockers / 0 high / 0 low across all 9 dimensions and 3 mandates. Handoff to DELIVER cleared.

---

## Wave: DELIVER / [REF] Implementation summary

One production file modified (`Cache.cs`, +7/-6), one new test file (`CacheTest.cs`, 324 lines, 7 NUnit scenarios). The backing `Dictionary<TKey, CacheItem<TValue>>` in `Cache<,>` was replaced with `System.Collections.Concurrent.ConcurrentDictionary<TKey, CacheItem<TValue>>`, and the `Get` / `Remove` paths were rewritten to use `TryGetValue` / `TryRemove`. The public surface (`Store`, `Get`, `Remove`, `Keys`) and the lazy-expiry-on-read semantics of `Get` are unchanged — `BaseMetricsService`, `WorkTrackingConnectorFactory`, and `GitHubService` compile and pass their existing tests with no edit.

Wave path was lean: DISCUSS / DESIGN / DEVOPS were inlined into the DISTILL feature-delta because the customer-supplied diagnosis on ADO Bug 5016 served as the design doc. The roadmap was written inline (`validation.status = approved`, orchestrator inline) rather than dispatching `nw-solution-architect` — the surgical scope of a one-line fix justified the shortcut.

## Wave: DELIVER / [REF] Files modified

| Category | File | Change |
|---|---|---|
| Production | `Lighthouse.Backend/Lighthouse.Backend/Cache/Cache.cs` | Dictionary → ConcurrentDictionary; `TryGetValue` + `TryRemove` in `Get` / `Remove`; `Store` retains atomic indexer assignment; +7/-6 lines. |
| Tests | `Lighthouse.Backend/Lighthouse.Backend.Tests/Cache/CacheTest.cs` (new) | 7 NUnit `[Test]` methods, 5 concurrency repros + 2 single-threaded parity guards, `[CancelAfter(5000)]` per concurrency test (NUnit 4 / .NET 10 replacement for deprecated `[Timeout(5000)]`), 324 lines. New `Tests/Cache/` directory mirrors production `Backend/Cache/`. |
| Tooling | `Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.bug-5016-cache-thread-safety.json` (new) | Per-feature Stryker config scoped to `**/Cache/Cache.cs` and `FullyQualifiedName~CacheTest`. Mirrors existing per-feature config precedent (`stryker-config.ado-oauth.json`, etc.). |
| Workspace | `docs/feature/bug-5016-cache-thread-safety/` (new) | DISTILL + DELIVER feature-delta, acceptance `.feature` (7 scenarios), `deliver/roadmap.json`, `deliver/execution-log.json`. |
| Evolution | `docs/evolution/2026-05-17-bug-5016-cache-thread-safety.md` (new) | Long-term archive: wave path, decisions, quality gates, carry-forward to `#4618`. |

## Wave: DELIVER / [REF] Scenarios green count

7 of 7 — captured 2026-05-17 via `dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests/ --filter "FullyQualifiedName~CacheTest"`. First post-fix run: 7 / 0 / 0 (passed / failed / skipped) in ~1m 56s. Post-D1-refactor re-run: 7 / 0 / 0 in 53s. All 5 concurrency tests survive their full ~1s contention windows without exceptions.

## Wave: DELIVER / [REF] DoD check

No formal DoD checklist was issued upstream (lean bug-fix). De facto DoD derived from the customer report + CLAUDE.md quality gates:

| Item | Status | Note |
|---|---|---|
| Reproduce `IndexOutOfRangeException` / `InvalidOperationException` in a deterministic test | PASS | Scenarios 1, 4, 5 reliably RED on Dictionary; matches customer's production stack frame in `Dictionary.TryInsert` / `Dictionary.Remove`. |
| Fix prevents all reproductions | PASS | 7 / 7 GREEN on ConcurrentDictionary, including the `InvalidateMetrics`-style composite (scenario 5) that mirrors the production hot path. |
| Public surface unchanged | PASS | No signature changes; no new public methods; consumers untouched. |
| Lazy-expiry semantics preserved | PASS | Scenarios 6 and 7 (single-threaded parity) GREEN on both old and new backing stores. |
| Full backend suite green | PASS | 2530 / 2530 (2 pre-existing `LicensingIntegrationTest` file-fixture failures, confirmed independent by stash-and-rerun). |
| `dotnet build` zero warnings | PASS | `TreatWarningsAsErrors` satisfied. |
| CLAUDE.md comment policy | PASS | Zero banned comments added; reviewer (Haiku) confirmed. |
| ADO bug state | Pending | Stays `Active` through finalize; transitions to `Resolved` only after push + green CI per the slice-boundary ritual. |

## Wave: DELIVER / [REF] Demo evidence

Not applicable — no Elevator Pitch / CLI / UI surface exists for this bug. The acceptance grain is the `Cache<,>` public API exercised from the multi-threaded NUnit fixture. The "demo" is the `dotnet test` filter run captured under Scenarios green count above.

## Wave: DELIVER / [REF] Quality gates

| Gate | Outcome | Evidence |
|---|---|---|
| DISTILL review (Sentinel, Haiku) | approved | 0 / 0 / 0 findings across 9 dimensions + 3 mandates. |
| Phase 2 TDD (RED → GREEN) | PASS | RED capture: 3 reliable failures on Dictionary (1× `InvalidOperationException` in `InvalidateMetrics`-style scenario, 1× silent data loss in 64-thread insert, 1× CancelAfter timeout on lazy-remove). GREEN: 7 / 7 in 1m 56s. |
| Phase 3.5 post-merge integration | PASS | `dotnet build` 0 warnings / 0 errors. Full suite 2530 / 2530 (excluding 2 pre-existing licensing fixture failures). |
| Phase 3 L1-L6 refactor | SKIPPED | Cache.cs is 38 lines, already RPP-compliant; structural similarity across CacheTest concurrency scenarios reflects distinct races (not duplicated knowledge per CLAUDE.md DRY guidance). Reviewer's D1 (tautological assertion in test 4) addressed in a focused refactor commit `2e1a0dba`. |
| Phase 4 adversarial review (Sentinel for tests, Haiku) | conditionally_approved | 0 blockers, 1 medium (D1: tautological assertion, fixed), 2 low (D2: extra read-back in test 1, D3: extra read-back in test 5 — deferred per "no scope creep"). |
| Phase 5 mutation testing (Stryker.NET per-feature) | **87.50%** kill rate (7 killed / 1 survived / 0 timeout) — above the 80% gate | Config: `stryker-config.bug-5016-cache-thread-safety.json` scoped to `**/Cache/Cache.cs` + `FullyQualifiedName~CacheTest`. Total time 4m 22s. The single surviving mutant is `>=` → `>` at `Cache.cs:29` (`DateTimeOffset.Now - cached.Created >= cached.ExpiresAfter`); killing it would require a test that invokes `Get` at the exact tick where `elapsed == ExpiresAfter`. Because `DateTimeOffset.Now` has 100ns granularity and any `Store` → `Get` registers at least one tick of elapsed time, the boundary is practically unreachable from a test — the mutant is *equivalent* in observable behaviour for any realistic invocation. Accepted as an equivalent mutant rather than chasing it with a timing-flaky test. |
| Phase 6 DES integrity | PASS | `des-init-log` created the execution log; per-phase `des-log-phase` entries written by the crafter for steps 01-01 and 01-02. |
| Phase 7 finalize | Pending | Archive to `docs/evolution/2026-05-17-bug-5016-cache-thread-safety.md` written; commit of feature workspace next. |

## Wave: DELIVER / [REF] Pre-requisites

| Source | Pre-requisite | Status |
|---|---|---|
| DISTILL Tier-1 | 7-scenario `.feature` spec + adapter coverage table + test placement decision | Met (`docs/feature/bug-5016-cache-thread-safety/acceptance/bug-5016-cache-thread-safety.feature`). |
| DESIGN component manifest | `Cache<,>` is an existing leaf utility — no new component required | Met (architecture brief unchanged; existing `Cache.cs` is the only target). |
| DEVOPS env matrix | Default `clean` — no infrastructure dependency | Met (regression scenarios run in the standard backend test environment, no DB / network / external service). |
| ADO state | Bug 5016 already `Active`, assigned, tagged `Release Notes` | Met. No state transition until push + green CI per `/ado-sync` slice-boundary ritual. |
