# Feature: story-6459-t1-backend-test-memory-flat

Story #6459 · T1 Backend test run keeps its memory flat: find and fix the test-host leak · part of Epic #6264
([workspace](../epic-6264-modular-monolith/feature-delta.md)) ·
brief: [`slice-41-t1-backend-test-memory-flat.md`](../epic-6264-modular-monolith/slices/slice-41-t1-backend-test-memory-flat.md)

DISCUSS: covered at Epic level (see the Epic's Wave Decisions, D-6264-3); waves start at DESIGN.

## Wave: PICK-UP / [REF] Inherited

From the Epic's `feature-delta.md`, by heading. Linked, not copied:

- [REF] How to pick up a Story: the recurring per-Story checklist (copied below to tick).
- [REF] Stories → Test-infrastructure track → T1.
- [REF] Story Map → Pick-up groups → G0b (order T3 → T1 → T2 with FE7 → T4); dependency graph: `T1 --> T2` (soft:
  name the leak before reducing hosts). T3 (#6461) is delivered locally and not pushed yet; T1 does not depend on it,
  but its worker cap changes how many hosts are alive at once, so every measurement here states the worker count.
- [REF] Cross-Cutting Impact Checklist (answered below).
- [REF] Outcome KPIs + guardrails: no KPI row is touched. Guardrail: every backend test stays green; none is deleted,
  skipped or weakened.
- [REF] Improvement log: nothing since 2026-11-01 that changes T1.
- ADR-270: T1 touches no bounded context unless the retention root turns out to be in product code; DESIGN answers
  that once the root is named.
- Maintainer decisions for this item (2026-11-01, in chat, AFK entry for the test track): the retention root may be
  fixed in product code if the fix is behaviour-free; the guard prefers an in-suite test, and a small reporting step
  in `ci_backend.yml` is allowed if no test can catch the leak; rigor standard; refactor and review per slice,
  mutation once at the end; nothing is pushed.

## Wave: PICK-UP / [REF] DoR re-run

| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | One standard-filter backend run peaks at about 20 GB with the machine down to 2.4 GB free (re-count below). |
| 2 Persona | PASS | `lighthouse-maintainer` and the parallel sessions on the 12-core, 31 GB machine; CI as the thing that must not slow down. |
| 3 Domain examples | PASS | The 2026-10-31 sample (2 → 18.3 GB in 8 minutes); the 2026-11-01 runs below; the suspects named in the brief, re-counted. |
| 4 UAT G/W/T | DEFERRED | Epic catalogue rule: full Given/When/Then is written at each Story's DISTILL (Epic DoR item 4). |
| 5 AC derived from UAT | PASS | The brief's four AC. |
| 6 Right-sized | PASS | Size M; slicing at DESIGN once the roots are named. |
| 7 Technical notes + cross-cutting | PASS | Below. |
| 8 Dependencies | PASS | None hard (brief and graph agree). |
| 9 Outcome measurable | PASS | Peak memory of a full standard-filter run, before and after, same machine and worker count. |

Slice composition: waived at Epic level (D-6264-5). JTBD: infrastructure-only; rationale in the Epic T1 entry.

## Wave: PICK-UP / [REF] Baseline re-count (2026-11-01)

| Brief says | Today |
|---|---|
| 2.0 → 18.3 GB in 8 minutes, not levelling off (2026-10-31 sample) | **Confirmed and slightly worse.** Six standard-filter runs on 2026-11-01 (story 6461's measurement, quiet machine, `docs/feature/story-6461-t3-test-runs-leave-room/measurement/results/`): peak summed RSS 19.7–20.1 GB at NUnit's default 12 workers and 18.8–19.0 GB at 6 workers; lowest machine `MemAvailable` 2.4 GB. Halving the workers barely moves the peak, which supports retention over concurrency. |
| About 98 `= new TestWebApplicationFactory` outside a `using` | **136** `new TestWebApplicationFactory` in the test project, **34** of them in a `using` (`git grep`), so about **102** outside one. |
| Derived hosts from `WithWebHostBuilder` / `WithTestAuthentication` | **106** `WithWebHostBuilder` and **71** `WithTestAuthentication` call sites. |
| Tests adding a new interceptor instance per test | **15** `AddInterceptors` calls in **13** test files, including `TestHelpers/TestWebApplicationFactory.cs` itself and `TestHelpers/CapturedMaterialisations.cs`; product code adds interceptors in `Data/LighthouseAppContext.cs`. |
| EF logs `ManyServiceProvidersCreatedWarning` | Neither product nor tests configure or suppress it (`git grep`: 0 hits); whether it fires is for the snapshot run to show. |
| `dotnet-gcdump` needed | Installed 2026-11-01 (`dotnet-gcdump` 10.0.750501); `dotnet-counters` already present. |

## Wave: PICK-UP / [REF] Cross-cutting re-check

- RBAC: N/A, because no authorization rule changes; if the root is in product code, the fix releases a reference and
  changes no rule.
- Lighthouse-Clients: N/A, because no route, DTO or serialized response changes.
- Website: N/A, because nothing user-visible changes.
- Usage-data event: provisionally N/A, because it is behaviour-free test infrastructure; DEVOPS confirms.
- UI sketch: N/A, because nothing a user sees changes.
- Terminology: N/A, because no user-facing words change.

## Wave: PICK-UP / Checklist

- [ ] Behaviour-free: every test stays green, none deleted, skipped or weakened; if product code changes, routes,
  DTOs, responses and topologies are unchanged.
- [ ] Ratchet in the same change: the leak guard (test, or CI reporting step) lands with the fix.
- [ ] Moves separate from edits.
- [x] Re-count before designing (above).
- [ ] Mutation last, on frozen code, on any touched product or helper logic.
- [ ] Architecture docs in the same change (ARCHITECTURE.md / ADR only if a product-side decision changes).
- [ ] Payoff proved: peak memory before and after, same machine, same worker count.
- [ ] No user-facing release notes.
- [ ] Cross-cutting re-check answered (above).

## Wave: DESIGN / [REF] Retention roots (heap snapshots, 2026-11-01)

Measured, nothing fixed. Every number: standard filter, `LIGHTHOUSE_TEST_WORKERS=6`, quiet 12-core / 31 GB machine, one suite at
a time. Raw material: `snapshots/` (gzipped `dotnet-gcdump report` text for the three dumps, the in-suite monitor, the probe path).
The `.gcdump` binaries stay out of git. A temporary probe (a test file, a reflection root-walker, and a two-line hook in
`TestWebApplicationFactory`) was used and removed; `git status` shows no trace of it.

### Verdict on the hypothesis

"One or two dominant roots, most likely the EF cache": **one dominant root holds, but it is not the EF cache.** The retained
object is the whole test host, and a static timer keeps it alive even after the factory is disposed. The EF cache is ruled out
below.

### RSS over time (run 1: gcdumps at 60/240/420 s, 6 workers, 7 m 40 s)

| s | 36 | 76 | 106 | 137 | 167 | 197 | 227 | 275 | 305 | 335 | 365 | 395 | 421 | 458 | 463 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| RSS GB | 1.9 | 3.3 | 4.7 | 5.9 | 6.9 | 7.3 | 7.4 | 8.5 | 10.6 | 12.8 | 12.9 | 14.2 | **17.7 (peak)** | 15.6 | 14.8 |

No plateau before the end. Counters at the end of the run: managed gen2 11.7-12.5 GB, LOH 0.41 GB, committed 15-16 GB. So the
growth is managed heap, not native memory. A gcdump forces a full GC and RSS did not fall back, so it is not uncollected garbage.

Run 2 (same, plus a temporary monitor forcing two full GCs every 30 s; 8 m 11 s, peak RSS sample 15.3 GB at 479 s). After full
GCs, `TestWebApplicationFactory` instances still alive (weak references) against created:

| s | 31 | 98 | 169 | 241 | 317 | 395 | 479 |
|---|---|---|---|---|---|---|---|
| created | 209 | 595 | 997 | 1149 | 1590 | 1599 | 1962 |
| alive after full GC | 208 | 594 | 996 | 1148 | 1589 | 1598 | 1961 |
| of those, already disposed | 202 | 588 | 990 | 1142 | 1580 | 1589 | 1944 |
| live managed MB | 1169 | 3378 | 5482 | 6237 | 9226 | 9374 | 10968 |

99.9 % of the factories ever created are still reachable, 99 % of them after `Dispose()`. About 5.6 MB of live managed heap per
factory (an isolated probe measured 3.8 MB per host, both disposed and undisposed: disposal frees nothing). 1962 factories x 5.6 MB
is the 11 GB.

### The three dumps

`dotnet-gcdump` 10.0.750501 falls back to lossy buffering ("non-lossy needs .NET 11+") and **stops at 10,000,000 objects, about 585 MB**
in all three dumps. So the dumps are the same-sized slice of a process at 2.8, 7.7 and 16.9 GB RSS, and cannot show growth. They do
show what the slice is made of.

| Type (instances in the slice) | 60 s | 240 s | 420 s |
|---|---|---|---|
| GC heap bytes in dump / objects | 588.9 MB / 10.0 M | 586.5 MB / 10.0 M | 583.1 MB / 10.0 M |
| `Hosting.Internal.Host` | 102 | 102 | 103 |
| `DependencyInjection.ServiceProvider` | 106 | 104 | 104 |
| `TestHost.TestServer` | 99 | 99 | 99 |
| `TestWebApplicationFactory<Program>` | 100 | 99 | 99 |
| `DelegatedWebApplicationFactory<Program>` | 159 | 157 | 157 |
| `Http.DefaultHttpClientFactory` | 100 | 100 | 100 |
| `Http.ActiveHandlerTrackingEntry` | 207 | 0 | 0 |
| `Http.ExpiredHandlerTrackingEntry` | 0 | 3 | 8 |
| EF `RuntimeModel` | 2 | 2 | 2 |

The slice is dominated by about 100 whole hosts, each with its own MVC and routing metadata: `DefaultModelMetadata` 15.8 MB,
`RoutePatternPart[]` 13.7 MB, `DefaultModelBindingMessageProvider` 12.8 MB, `FilterDescriptor` 11.8 MB, `ControllerActionDescriptor`
7.9 MB, `string` 18.8 MB (sizes = count x per-object bytes from the report). The report columns are per-object bytes, count, type.
The shift from active to expired handler entries between 60 s and 240 s is the HttpClientFactory handler lifetime expiring.

### Named roots, ranked

**1. `IHttpClientFactory` cleanup timer pins every host that has a singleton holding a factory client (dominant, about all of it).**
- Chain, found by walking from static fields by reflection 230 s after a *disposed* factory
  (`snapshots/probe-disposed-host-root-path.txt`): `static TimerQueue.Instances` -> `TimerQueueTimer._state` ->
  `DefaultHttpClientFactory._services` -> `ServiceProviderEngineScope.RootProvider` -> `ServiceProvider` -> the factory delegates
  -> `TestWebApplicationFactory`. The disposed host was still alive after 30, 60, 90, 130, 150, 170, 200 and 230 s, so the
  default 2-minute handler lifetime is not what holds it.
- Why it never lets go: an expired handler entry is purged only once its handler can be collected. The handler is held by an
  `HttpClient` stored in a singleton of the same host: `JiraOAuthProvider.httpClient` and `AdoOAuthProvider.httpClient`, both
  created with `httpClientFactory.CreateClient(...)` inside `AddSingleton` (`Lighthouse.Backend/Program.cs:1526-1542`). Found by
  walking from the disposed host's `ServiceProvider` to every reachable `HttpClient` (`FactoryCallSite.Value` ->
  `JiraOAuthProvider` / `AdoOAuthProvider`). The host holds the client, the client keeps the handler alive, the expired entry
  cannot be purged, and the 10 s cleanup timer re-arms itself forever; the timer is rooted by the static timer queue. A loop,
  rooted outside the host.
- Handler lifetime is set at `Program.cs:341-344` (2 minutes); it only decides when the loop starts.
- Production runs one host per process, so this costs nothing there; the suite creates about 2000 hosts in one process.
- Test code that creates the hosts: `TestHelpers/TestWebApplicationFactory.cs` (and every fixture that news one up).

**2. Per-host payload size (about 3.8-5.6 MB each) is what makes root 1 expensive, not a root itself.** Each host builds its own
MVC metadata, route table, and DI graph; none is shared across hosts. This is the multiplier for ROOT 1 and the lever for T2
(fewer hosts), not a retention path.

**3. Undisposed factories: real, but small.** The brief counted about 102 `new TestWebApplicationFactory` outside a `using`, but at
most 17 factories were alive-and-undisposed at any 30 s sample; fixtures dispose in teardown. An undisposed host also keeps
its own live timers (a SignalR-style heartbeat loop, seen through `HostingListener` in the timer's execution context), but the
count is too small to matter next to root 1.

### Ruled out

- **EF `ServiceProviderCache` / options churn.** Every interceptor in the test project is a singleton reused across hosts:
  `NoWaitingForTheDisk.Instance`, `CapturedMaterialisations.Router.Instance` (documented as shared for exactly this reason), and the
  product's `SqliteLegacyDoubleQuotedStringInterceptor.Instance` (`Data/LighthouseAppContext.cs:105`). The three dumps hold 2
  `RuntimeModel` instances, constant. `ManyServiceProvidersCreatedWarning` does not appear in the console output of either full run
  (0 hits). Caveat: EF may log it to a sink the console run does not show; the model count is the stronger evidence.
- **SQLite connections, `byte[]`/`string`, logging providers, hosted services:** nothing growing in the slice; hosted services are
  removed by the factory.
- **Native memory:** gen2 plus LOH account for the RSS (see above).

### Not established

- A stub-OAuth run (`Lighthouse:OAuth:UseStubProvider=true` through `UseSetting`) still kept the disposed host alive after 150 s.
  I could not tell whether the setting reached Program's registration code (it reads configuration while building) or whether a
  third singleton also holds a factory client. So "the two OAuth providers are the only holders" is **not proven**; the walk found
  exactly those two besides the test client list, and it covered the disposed host's reachable graph.
- Exact retained bytes per root. `dotnet-gcdump` is lossy past 10 M objects and reports types, not retained sizes or root paths;
  `dotnet-dump` or PerfView would give a dominator tree. The 5.6 MB per factory above is live-heap growth divided by factories.
- Whether removing root 1 returns the heap to a plateau. Nothing was fixed or tried; the stub run above is the nearest evidence and
  was inconclusive.
- gen2 counters came from the last 15 s of run 1 only; I started `dotnet-counters` late.

### Fix experiment (temporary, reverted)

Ordered by the coordinator to confirm root 1 before DESIGN commits to a fix. Temporary and uncommitted: `JiraOAuthProvider` and
`AdoOAuthProvider` took a `Func<HttpClient>` (the old constructors kept, delegating), and `Program.cs` passed
`() => httpClientFactory.CreateClient(name)`, so no factory client is held. Same probe as before, 6 workers, one run at a time.
Everything was reverted afterwards (`git status` shows only the three epic-6033 files). Raw output: `snapshots/fix-experiment-*.txt`.

**Result: root 1 is necessary but not sufficient. The earlier ranking above was wrong to call it the only dominant root.**
With the per-call client alone, a disposed host was still alive at 200 s. A second, equally strong root exists, found below.

**Stub-mode puzzle (question 2): answered.** `UseSetting("Lighthouse:OAuth:UseStubProvider","true")` does reach Program: the host's
configuration read `true` and the providers resolved were three `StubOAuthProvider`, no Jira/Ado. The stub host was still retained, so a
third holder exists. Root walk from the static timer queue: `TimerQueueTimer._state` -> `System.Threading.RateLimiting.TimerAwaitable`
-> its callback's `AsyncStateMachineBox<DefaultPartitionedRateLimiter<HttpContext, ...>.<RunTimer>d__14>` -> `ExecutionContext` (the
`HostingListener` async-local) -> `Host` -> `ServiceProvider` -> `TestWebApplicationFactory`.

**Root 1b: rate limiter replenishment timers, never stopped when the host is disposed.** Walking every timer in the static queue and
asking which of them reaches a disposed host: with rate limiting at its default, two to three `TimerAwaitable` loops of
`DefaultPartitionedRateLimiter<HttpContext, string>` / `<HttpContext, DefaultKeyType>` (`snapshots/fix-experiment-timers-limiter-on.txt`).
They come from `options.GlobalLimiter = WhatOneAddressMayHandIn()` (`Program.cs`, end of the `AddRateLimiter` block, a
`PartitionedRateLimiter.Create` nobody disposes) and from the framework's own endpoint limiter that `app.UseRateLimiter()`
(`Program.cs:216`, only when `RateLimits:Enabled`) builds. Removing the global limiter left one loop (`DefaultKeyType`, the framework's);
`RateLimits__Enabled=false` removed the last (`snapshots/fix-experiment-timers-after-limiter-off.txt`: no timer reaches the host, host dead).
Note the first host of any test process is held by shared one-shot timers (Sqlite pool prune, the NUnit adapter's cache timer) that captured
its execution context; that is one host per process, not the leak, so the matrix below discards a warm-up host first.

**Matrix (questions 1a, 1b).** A = host never created a factory client. B = a factory client for the Jira name created once, then dropped
(what a per-call request does). Weak reference to the root factory and its `ServiceProvider` after forced full GCs, seconds after `Dispose()`:

| Setting | A at 0 s / 5 s | B at 0 / 30 / 60 / 130 s | B at 160 / 200 s |
|---|---|---|---|
| per-call client + rate limiting off (both fixes) | **dead / dead** | alive / alive / alive / alive | **dead / dead** |
| captured client (as shipped) + rate limiting off | alive / alive | alive / alive (stopped at 30 s) | not sampled |
| per-call client + rate limiting on | alive / alive | alive / alive (stopped at 30 s) | not sampled |
| as shipped | alive / alive | alive / alive (stopped at 30 s) | not sampled |

- **1b:** with both fixes, a host that never asked the factory for a client is collected immediately after `Dispose()` (dead at 0 s).
- **1a:** with both fixes, a host that created one client stays until the 2-minute handler lifetime plus the 10 s cleanup tick:
  alive at 130 s, dead at 160 s. Matches the prediction. Neither fix alone frees any host in either case.
- Rate limiting off here was `RateLimits__Enabled=false` plus `GlobalLimiter` left unset, both through temporary environment switches. It is a
  measurement device, not a proposed behaviour: two rate-limiting acceptance tests fail in the run below because of it.

**Before / after, full standard-filter run, 6 workers, weak-reference monitor with a forced full GC every 30 s**
(both fixes, rate limiting off; 6 m 20 s; 11,891 passed, 4 failed: the 2 known publish-output scans plus the 2 rate-limit tests
`A_voter_sending_more_than_thirty_entries_a_minute_is_told_to_slow_down` and `OneAddressInventingANewBrowserEachTime_IsStillBounded`):

| | Before (run 2, as shipped) | After (both fixes) |
|---|---|---|
| Peak RSS | 15.3 GB (monitor sample); 17.7 GB in the gcdump run | **4.2 GB** (3 s sampler, at 360 s) |
| Run duration | 8 m 11 s | 6 m 20 s |
| Live factories after GC at ~30 / 120 / 240 / 340 / 370 s | 208 / ~830 / 1148 / ~1600 / 1961 | 9 / 6 / 9 / 155 / 155 |
| Live managed MB at the same marks | 1169 / ~4700 / 6237 / ~9300 / 10968 | 185 / 309 / 277 / 1636 / 1617 |
| Factories created at the end | 1962 | 1962 |

Plateau: flat for the first 4 minutes (6-11 live factories, 173-309 MB live managed, RSS 0.7-1.0 GB). Late in the run it steps up:
from 274 s the live count is 13, then 155 from 338 s, live managed 0.9 -> 1.6 GB, RSS 2.1 -> 4.2 GB at the end. So the curve is no longer
a ramp tied to the number of hosts created (1962 created, about 150 alive), but it is not perfectly flat either.

**Not established by the experiment**
- What holds the 155 hosts at the end. They are disposed ones, and the run ended before a 2-minute lifetime could have expired for them, so
  the likeliest reading is hosts that did create a factory client and are still inside the 2 minutes plus 10 s window. This was not checked
  (no walk at that point; I did not extend the run).
- Whether a third class of holder exists in the part of the suite that runs last. The first 4 minutes show none.
- Whether the rate limiter can be kept on in test hosts with a different fix (disposing the limiters with the host, or building them from a
  registered, disposable service). Only "rate limiting off" was measured. The same goes for production-safe shapes of the OAuth change.
- The EF cache result above stands; neither fix touched it.

## Wave: DESIGN / [REF] Decisions (propose mode, maintainer AFK, 2026-11-01)

Shape of the work: a composition-root lifetime fix (who owns an object that has a timer) plus a guard test. Nothing a user sees
changes, so no sketch, no loading or failure look, no usage-data event. The recommended option was taken for every choice.

**Spike result: not run.** The design session had no shell tool, so nothing was built or run and no number exists. D2 part (b) is
reasoned from the measured root chains, not validated; the spike is the guard test of step 1 and is run in DELIVER, and the
section "D2 spike and what it decides" states what each outcome means. D2(a), D1 and D1b rest on the earlier fix experiment.

| # | Decision | Chosen | Why / trade-off |
|---|---|---|---|
| D1 | OAuth providers stop holding a factory client | The two providers take `IHttpClientFactory` (the client name is already a public constant on each class) and ask for a client inside each token request. Clients are not disposed by the provider; the factory owns the handler. The `AddHttpClient(name, timeout 30 s)` registrations at `Program.cs` 356-364 stay untouched and are honoured on every `CreateClient`. The two singleton registrations become plain type registrations. | The documented factory usage. A typed client would be registered transient or scoped, so a singleton provider would capture a stale one (the same trap in other clothes); a hand-built `SocketsHttpHandler` with `PooledConnectionLifetime` would leave the factory's handler management and the 30 s timeout registration, a behaviour surface with no memory gain. Behaviour is identical: same endpoint, same handler pipeline, same timeout. Cost: one cheap `CreateClient` per token request (token requests are rare). |
| D1b | Test hosts shorten the handler lifetime | `TestWebApplicationFactory` re-registers `ConfigureAll<HttpClientFactoryOptions>` with a handler lifetime of a few seconds, after the product's two minutes. Test-only. | Any host that did make one factory request stays pinned until lifetime plus the factory's fixed 10 s cleanup tick. With 2 minutes that is the 155 late hosts; with seconds it is a window of about 15 s. The product's value is unchanged. Handler lifetime only decides connection-pool reuse, which no test relies on (no test references it). |
| D2 | Rate limiting stays ON; its two timer loops stop pinning hosts | (a) The one-address global limiter becomes a DI singleton owned by the container (so it is disposed with the host) and the options pick it up through `IConfigureOptions<RateLimiterOptions>`. The limiter body, partitions and numbers are moved, not edited. (b) The framework-built endpoint limiter: see below. | (a) is certain and public-API only. Candidates rejected for the global limiter: dispose on `ApplicationStopping` (works, but a second lifetime story next to DI for the same object) and `ExecutionContext.SuppressFlow` (cuts only the context path, which is sufficient for this limiter because its partitioner is static, but leaves the object undisposed). |
| D2(b) | The endpoint limiter | **No public API disposes it or cuts its graph.** It is created inside `RateLimitingMiddleware` and never exposed. `SuppressFlow` is not enough: its timer loop is an instance method whose partitioner is a closure over the middleware, and the middleware holds the rest of the pipeline and the service provider, so the static timer reaches the host by a second path even with the context cut. Recommended: the spike decides between (i) and (ii). (i) If the framework limiter is released when the host is disposed after (a) (unlikely, given the measured loop), nothing more. (ii) Build the middleware at a seam the product owns (`app.Use` with a factory that creates the framework middleware exactly as `UseMiddleware` does), register the instance with a small disposable owner in DI, and on dispose dispose the middleware's endpoint limiter by reading its private field. Unconditional, not test-gated: one code path, and disposing limiters at graceful shutdown is correct in production too. | The reflection in (ii) is the price of the framework not exposing ownership. It fails loudly: if the field moves, a disposed host stays alive and the guard test goes red. Rejected here: re-implementing endpoint policies inside our own limiter (rule-change risk and a second enforcement path), a test-only switch that builds limiters differently (tests would stop proving production), and turning limiting off in tests (the two rate-limit tests assert it). |
| D3 | The guard | A fast NUnit test in `Lighthouse.Backend.Tests/Architecture/`. After a warm-up host, create a `TestWebApplicationFactory`, start it (a plain test request, so the pipeline and both limiters are built; rate limiting is on by default in test hosts), dispose it, drop every reference, then poll with a full GC until its weak reference is dead, up to 15 s. Two cases: (A) a host that served a request and never asked the factory for a client; (B) a host that additionally resolved every `IOAuthProvider`. The factory is created in a separate non-inlined method so a Release build keeps no hidden local alive. The fixture is `[NonParallelizable]` and goes on the allowlist in `BackendTestParallelizationGuardTest` with the reason. | Case B needs no 2-minute wait: after D1, resolving the providers must not create a client, so the host dies at once; the regression it catches is a provider (or any singleton) taking a client again. A host that actually used a factory client is bounded by the handler lifetime by design (D1b); a test waiting for it would cost 12 s or more, so it is left out, and the late-host check lives in D4. The test is its own probe in the Earned Trust sense: it exercises the real environment lie (timers rooted outside the host). Serial so a loaded neighbour cannot slow the polling or add allocation noise; other fixtures' hosts cannot keep ours alive. No CI reporting step is needed, so `ci_backend.yml` is untouched. |
| D4 | Payoff measurement | Lead-owned precondition and closing step; protocol below. | Peak, shape and duration, not only peak. |
| D5 | Behaviour-free proof | Existing tests, listed below; nothing deleted, skipped or weakened. | |

### D2 spike and what it decides (to be run before step 3b)

One short, uncommitted probe, run through `dotnet test --no-build --filter "FullyQualifiedName~<probe>"` only, never the full
suite: a warm-up host, then a host that served one request and never asked the factory for a client, disposed, full GC, weak
reference alive or dead. This is the guard's case A, so the guard test written first in step 1 is the probe; no throwaway code
is needed.
- After D1 + D2(a) the probe dies: D2(b)(i) holds, step 3b is dropped.
- It stays alive (the expected result): apply D2(b)(ii), re-run, and the probe must die.
- It still stays alive with (ii): a third holder exists. Walk the timers with the reflection root-walker recorded under
  `snapshots/`, name it, and fall back to the weaker option: keep limiting on, accept the endpoint limiter's pin per host that
  served a request, and ask T2 (fewer hosts) to carry the rest. Trade-off: the suite still holds one host per rate-limited
  request-serving host, so the peak falls to roughly the live-host count, not to the flat shape.

### Rate-limiter spike (temporary, reverted)

Run after the design commit `ea982758e`, at the coordinator's request. Temporary and uncommitted, reverted afterwards (`git status`: only the
three epic-6033 files). Focused probe runs only, no full suite, 6 workers, rate limiting ON. Each probe: a warm-up host first (discarded), then
a host that served one request (`GET /api/latest/zz-nothing-here`, a 404 that still passes the rate-limit middleware), never created a factory
client, was disposed, and then polled with full GCs for up to 15 s. The probe's `Build` must not be `async` (an async state machine keeps the
factory in a hoisted local and gave a false "alive" in a first pass; the numbers below are from the synchronous rewrite).
Raw output: `snapshots/limiter-spike-*.txt`.

Applied in every variant: D1 (providers take a per-call `Func<HttpClient>`) and D2(a) (the one-address limiter built by a DI singleton the
container disposes, fed in through `IConfigureOptions<RateLimiterOptions>`).

| Variant | Host after 15 s of polling GCs |
|---|---|
| 1. D1 + D2(a) only | **alive** |
| 2. + (3a) the `UseRateLimiter()` pipeline built under `ExecutionContext.SuppressFlow()` | **alive** |
| 3. + (3b) the middleware's endpoint limiter disposed (reflection, field `_endpointLimiter`) | **dead** |
| 4. (3a) + (3b) | **dead** |
| 5. Variant for tests, below: our own construction of the middleware, endpoint limiter disposed by the container | **dead** |

The first host of each probe process stayed alive in every variant; that is the one-host-per-process pin by shared one-shot timers noted
earlier, not the leak.

**Question 2, the holder after D1 + D2(a).** The framework's endpoint limiter, nothing else. Root walk from the static timer queue:
`TimerQueueTimer._state` -> `System.Threading.RateLimiting.TimerAwaitable` -> `_callback` -> `AsyncStateMachineBox<...DefaultPartitionedRateLimiter<HttpContext,
DefaultKeyType>.<RunTimer>d__14>` -> `ExecutionContext` (the `HostingListener` async-local) -> `Host` -> `ServiceProvider` -> `TestWebApplicationFactory`.
The key type `DefaultKeyType` is the framework's own; the product's global limiter (`string` keys) is gone from the walk after D2(a).

**3a, SuppressFlow: fails.** With the flow suppressed the `ExecutionContext` path disappears, but a second path remains, as the design predicted:
the timer's state machine `<>4__this` -> the limiter's `_partitioner` closure -> `RateLimitingMiddleware` -> `_next` -> `AuthenticationMiddleware` ->
`AuthorizationMiddlewareInternal` -> `AuthorizationPolicyCache` -> `CompositeEndpointDataSource` -> `RouteEndpointDataSource._applicationServices` ->
`ServiceProvider`. The closure over the middleware reaches the whole pipeline and the container, so cutting the execution context alone is not enough.

**3b, disposing the endpoint limiter: works.** Disposing `RateLimitingMiddleware._endpointLimiter` (type
`System.Threading.RateLimiting.PartitionedRateLimiter<Microsoft.AspNetCore.Http.HttpContext>`, a private instance field in .NET 10) stops its timer
loop and the host dies, with or without SuppressFlow. The middleware's fields in .NET 10: `_next`, `_defaultOnRejected`, `_logger`, `_metrics`,
`_globalLimiter` (same type), `_endpointLimiter` (same type), `_rejectionStatusCode`, `_policyMap`, `_defaultPolicyKey`.

**3c, public API: none.** From reflection on the .NET 10 assemblies: `RateLimitingMiddleware` is **not public** (internal, sealed, implements no
interface, so not `IDisposable`). `RateLimiterOptions` has only `GlobalLimiter`, `OnRejected`, `RejectionStatusCode` and `AddPolicy` and is not `IDisposable`.
The middleware's one public constructor takes `(RequestDelegate, ILogger<RateLimitingMiddleware>, IOptions<RateLimiterOptions>, IServiceProvider,
RateLimitingMetrics)` and it exposes `Invoke`. So no callback, option or registration hands the endpoint limiter to the container, and the type cannot be
named in our code. The policies registered with `AddPolicy` are the endpoint limiter's partitioner input; they do not change who owns the timer.

**What worked in a form that could be shipped (variant 5).** Replace `app.UseRateLimiter()` (inside the same `RateLimits:Enabled` check, same pipeline
position) with a call that builds the middleware ourselves and gives it to the same container-owned singleton that owns the global limiter:
`ActivatorUtilities.CreateInstance(app.Services, <the internal type looked up by name in the RateLimiting assembly>, next)`, hands it to the singleton, and returns
a `RequestDelegate` bound to its `Invoke`. The singleton's `Dispose()` disposes the global limiter and the middleware's `_endpointLimiter` through reflection.
The host died, and the dispose happens at container disposal, so this is the "at host shutdown" form (variant 3b disposed it earlier, while the host was
still up; liveness is the same).

**Question 4, the two rate-limit tests.** `A_voter_sending_more_than_thirty_entries_a_minute_is_told_to_slow_down` and
`OneAddressInventingANewBrowserEachTime_IsStillBounded`, run by filter: **both pass** with variant 5 (2 passed) and also with D1 + D2(a) alone
(2 passed), so moving the global limiter into a singleton changes neither limit.

**Recommendation.** Take variant 5 for step 3b, with these edges stated rather than hidden:
- It is the only variant measured to release the host with rate limiting on, and it keeps both rate-limit tests green.
- It reflects on one private field of an internal framework type (`_endpointLimiter`) and activates the internal type by name. A framework update that
  renames the field or the type fails safe in the guard test (the host stays alive, the test goes red), and the production behaviour is unchanged as
  long as the middleware's constructor and `Invoke` keep their shape; both are checked at startup in the spike only by the call succeeding, so the real change
  should fail loudly at startup if the type or method is not found rather than skip the limiter.
- Fallback if that coupling is judged too risky: keep D2(a) and let the endpoint limiter pin one host per host that served a request (variants 1 and 2),
  and rely on T2 (fewer hosts). It was not measured how far that lowers the peak.

**Not covered by this spike.** Whether `Invoke` as a method-group delegate on the internal type behaves identically for every request shape
(only the two rate-limit tests and a 404 were exercised); the effect on the full-run peak (the earlier full run turned rate limiting off); and a host
that also created a factory client (D1b).

## Wave: DESIGN / [REF] Hexagonal sketch

```
 driving side                         application / domain                 driven side
 ------------                         --------------------                 -----------
 HTTP request                          OAuthService (unchanged)             JiraOAuthProvider   [MODIFIED]
   |                                     |  uses IOAuthProvider port            AdoOAuthProvider    [MODIFIED]
   v                                     v                                       asks IHttpClientFactory per request
 Program.cs composition root  [MODIFIED]  IOAuthProviderRegistry (unchanged)           |
   - registers providers as plain types       |                                         v
   - AddHttpClient(name, 30 s) (unchanged)    +--> IOAuthProvider port (unchanged)   named client, pooled handler (unchanged)
   - AddRateLimiter: global limiter                                                  
       from a DI singleton [NEW owner]      RateLimitingMiddleware (framework)
   - UseRateLimiter seam (D2b ii) [NEW?]      endpoint limiter: disposed with the host [via the seam]
                                          
 test host
   TestWebApplicationFactory [MODIFIED: short handler lifetime]
   TestHostLeakGuard [NEW]: build, serve, dispose, GC, assert dead (cases A, B)
```

Unchanged: every route, DTO, response, rate-limit rule and number, the 30 s timeout, `IOAuthProvider` and the registry, and
`OAuthService`.

## Wave: DESIGN / [REF] Component decomposition

| Component | Change | Notes |
|---|---|---|
| `JiraOAuthProvider`, `AdoOAuthProvider` | MODIFY | Constructor takes the factory instead of a client. Contract shape: bounded-change (no state kept besides the factory, time provider and logger). |
| `Program.cs` OAuth registration | MODIFY | Two singletons become type registrations. |
| `Program.cs` rate-limiting registration | MODIFY | The global limiter body moves into an owned class; the registration reads it via `IConfigureOptions<RateLimiterOptions>`. Contract shape: unchanged limits, universe = this host's limiter. |
| Rate-limiter owner (class + disposal of the endpoint limiter) | CREATE NEW | No existing alternative: nothing in the product owns a limiter's lifetime. Disposes on container disposal. |
| `app.UseRateLimiter()` call site | MODIFY (only if D2(b)(ii)) | Same gate (`RateLimits:Enabled`), same position in the pipeline. |
| `TestWebApplicationFactory` | MODIFY | Handler lifetime of a few seconds. |
| Leak guard fixture | CREATE NEW | No existing test measures host lifetime. |
| `BackendTestParallelizationGuardTest` allowlist | MODIFY | One entry, with reason. |
| The three OAuth provider test classes | MODIFY | Arrange only: a mock factory returns the same stub-handler client; assertions untouched. |

## Wave: DESIGN / [REF] Reuse Analysis

| Existing | Decision | Justification |
|---|---|---|
| `AddHttpClient(JiraOAuth / AdoOAuth, timeout 30 s)` | EXTEND (reuse as is) | The named clients exist and stay; D1 only changes who asks for them and when. |
| `IHttpClientFactory` handler lifetime (`ConfigureAll`, 2 min) | EXTEND (test override) | One override in the test factory; product value kept. |
| `TestWebApplicationFactory` | EXTEND | Already the single place that configures test hosts. |
| `BackendTestParallelizationGuardTest` allowlist | EXTEND | The mandated route for a serial fixture. |
| `WhatOneAddressMayHandIn` | EXTEND | Same code, moved into the owned class; rules untouched. |
| Per-test Moq style used by the OAuth tests | EXTEND | `Mock<IHttpClientFactory>` returning the existing stub-handler client; no new shared helper. |
| Rate-limiter owner, leak guard fixture | CREATE NEW | No existing alternative. |

## Wave: DESIGN / [REF] Rejected alternatives

- **EF cache work**: ruled out by the snapshots (two `RuntimeModel`s, constant); nothing to fix.
- **Forced periodic GC in the suite**: collects nothing, the hosts are reachable from static timers.
- **Process per fixture**: slows the suite by far more than the leak costs and moves the problem to start-up time.
- **Dispose-only on `ApplicationStopping` for the global limiter**: works, but a second lifetime mechanism next to the container.
- **`ExecutionContext.SuppressFlow` alone**: cuts one path; the endpoint limiter has a second.
- **Typed client / hand-built `SocketsHttpHandler` for the OAuth providers**: see D1.
- **A weak-reference wrapper around the service provider**: the middleware also holds the next delegate, so the pipeline pins the host anyway.
- **Rate limiting off in tests**: two tests assert it; it was only a measuring device.
- **Static, process-wide limiters shared by all hosts**: parallel tests would share windows; a behaviour change.
- **A CI step reporting peak memory**: the in-suite test catches the cause; a ceiling would be a noisy proxy for it.
- **Lowering the production handler lifetime**: changes production behaviour for a test problem.
- **Re-implementing endpoint policies in our own limiter**: a second enforcement path.

## Wave: DESIGN / [REF] Behaviour-free proof (D5)

- Rate limiting still ON: `A_voter_sending_more_than_thirty_entries_a_minute_is_told_to_slow_down` (`Slice11CastAVoteScenarios`) and
  `OneAddressInventingANewBrowserEachTime_IsStillBounded` (`UsageDataEventCeilingTests`) must be green in the filtered run; both failed
  in the measuring run precisely because limiting was off, so they now prove it is on. Also `S6_RateLimitingTests` and
  `UsageDataConsentRateLimitTests`.
- OAuth: `JiraOAuthProviderTest`, `AdoOAuthProviderTest`, `OAuthServiceTest` (including the real-identity-provider refresh arrange),
  and the OAuth controller and registry tests. Assertions are untouched; only the arranged client source changes.
- `dotnet build` with zero warnings; the standard-filter `dotnet test`.
- Mutation (last, on frozen code): the two provider files. `Program.cs` wiring is covered by the host tests above.

## Wave: DESIGN / [REF] Payoff measurement (D4)

- Protocol: same machine, quiet, one suite at a time, `LIGHTHOUSE_TEST_WORKERS=6`, standard filter, the existing sampler and
  weak-reference monitor. Before = main immediately before step 2 (the 2026-11-01 numbers, 15.3-17.7 GB peak and 8 m 11 s, are the reference
  if the machine and day match; otherwise re-measure once). After = the branch with all steps. Report peak RSS, duration, and live
  factories after GC at the same marks.
- Target: peak under 3 GB, flat shape (live factories bounded by the hosts created inside one cleanup window, not by the total
  created). Honest revision: the experiment with per-call clients alone left 155 late hosts and 4.2 GB; D1b is expected to remove
  them. If D1b falls short, the target becomes under 4.5 GB with a flat shape, and the remainder is T2's.
- Late hosts: keep the monitor alive for three minutes after the last test (temporary probe, removed) and expect the live count to
  fall to single digits by about 140 s; or re-run with D1b only and see them disappear. A host still alive after that gets the root
  walk from the retention-roots section.

## Wave: DESIGN / [REF] ADR-270 and ARCHITECTURE.md

No change to either. The feature touches no bounded context, adds no concept, term, edge or domain event: it changes only how long
the composition root and the test host keep objects alive. The OAuth providers stay in the work-tracking context behind the same
port; rate limiting stays an API-edge concern (ARCHITECTURE.md already names it only as such). `docs/ci-learnings.md` gets no rule,
because the guard test enforces the lesson.

## Wave: DESIGN / [REF] Patterns weighed (for the maintainer)

Chosen: dependency inversion through the factory port (D1), single owner per disposable (the container), the test as probe.
Rejected: typed client, per-fixture process, static shared limiters, weak-reference wrapper, re-implemented endpoint policy.
One caution recorded for the future: a singleton must hold the factory, never a client it made; the guard now fails on it.

## Wave: DESIGN / [REF] Open questions for DISTILL

1. Which cheap anonymous route does the guard request in case A (any route that goes through the rate-limiting middleware)?
2. The exact handler lifetime in seconds for D1b (smaller means sooner release; the cleanup tick is a fixed 10 s).
3. Does the guard's polling limit (15 s) survive a loaded CI runner? Raise it if the first CI run shows flakes; do not drop the assertion.
4. Spike outcome of D2(b): which of (i), (ii) or the weaker fallback applies.
5. Whether `OAuthService` tests build the provider through `new` (yes, one) so the Moq arrange is the only edit.

## Wave: DESIGN / [REF] Proposed slicing

Rigor: standard. One slice, 3 steps, guard in the same change as the fix.

| Step | What it delivers |
|---|---|
| 0 (lead) | Quiet slot; baseline numbers re-confirmed. |
| 1 | The guard fixture written first, red for both cases, plus the allowlist entry (kept local until step 3 turns it green). |
| 2 | OAuth providers ask the factory per request (D1) and the test factory shortens the handler lifetime (D1b); case B green; three OAuth test classes re-arranged. |
| 3a | Global limiter owned by the container (D2a). |
| 3b | If the spike says so, the endpoint limiter's owner seam (D2b ii); case A green. |
| 4 (lead) | Payoff measurement and the late-host check (D4); mutation on the two provider files, last. |

Commit the guard together with the first change that turns it green, so no commit on the way is red.

## Wave: DESIGN / [REF] Maintainer decision — endpoint rate limiter (2026-11-01, 18:2x, in chat)

The rate-limiter spike showed the framework's endpoint limiter (inside `UseRateLimiter()`) is the last holder and that
no public API releases it. Options put to the maintainer: (1) build the internal `RateLimitingMiddleware` ourselves in
`Program.cs` and let a container-owned singleton dispose the global limiter and the middleware's private endpoint
limiter at shutdown, by reflection; (2) accept the pin and leave the payoff to fewer hosts; (3) measure (2) first.

**Decision: option 1.** Rate limiting stays on with the same rules. The reflection fails at startup if the internal
type, constructor, `Invoke` method or field is not found, never by silently skipping the limiter; the in-suite leak
guard catches a regression. This supersedes D2(b)'s conditional wording: step 3b is no longer gated on the spike, it
is part of the fix. Re-check the reflection on every .NET major upgrade.
