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

"One or two dominant roots, most likely the EF cache": **two roots hold, and neither is the EF cache.** The retained object is
the whole test host, and a static timer keeps it alive even after the factory is disposed. Two roots each pin every host on their
own: (1) the HTTP client a singleton captured, which keeps the client factory's cleanup timer looping; (2) the rate limiters'
timer loops, which nobody stops when the host is disposed. On top of both sits the per-host payload (3.8-5.6 MB), which is what
makes each pinned host expensive. The first pass below found only root 1; the fix experiment further down corrected that. The
EF cache is ruled out below.

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

**1. `IHttpClientFactory` cleanup timer pins every host that has a singleton holding a factory client (root 1 of 2; the fix experiment below shows it is not alone).**
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
- Handler lifetime is set at `Program.cs:340-343` (2 minutes); it only decides when the loop starts.
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
| Peak RSS | 15.3 GB (monitor sample); 17.7 GB in the gcdump run | **4.2 GB** (3 s sampler, at 360 s); predates the final design (rate limiting was off and there was no seam) |
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

**Spike result: decisions were written before the spike; the spike ran after `ea982758e`, see the Rate-limiter spike section.** The
design session had no shell tool, so nothing in this table was built or run when it was written. D2 part (b) was reasoned from the
measured root chains and the Rate-limiter spike then measured it. D1, D1b and D2(a) were written from the earlier fix experiment,
before the spike ran; the spike confirmed D1 + D2(a) as the base the seam sits on.

| # | Decision | Chosen | Why / trade-off |
|---|---|---|---|
| D1 | OAuth providers stop holding a factory client | The two providers take `IHttpClientFactory` (the client name is already a public constant on each class) and ask for a client inside each token request. Clients are not disposed by the provider; the factory owns the handler. The `AddHttpClient(name, timeout 30 s)` registrations at `Program.cs` 356-364 stay untouched and are honoured on every `CreateClient`. The two singleton registrations become plain type registrations. | The documented factory usage. A typed client would be registered transient or scoped, so a singleton provider would capture a stale one (the same trap in other clothes); a hand-built `SocketsHttpHandler` with `PooledConnectionLifetime` would leave the factory's handler management and the 30 s timeout registration, a behaviour surface with no memory gain. Same endpoint, same handler pipeline, same timeout, same API behaviour. One change in connection lifetime: before, the captured handler was never rotated; after, it rotates on the documented 2-minute handler lifetime (DNS is re-read). Accepted as alignment with the framework's documented use of `IHttpClientFactory`. Cost: one cheap `CreateClient` per token request (token requests are rare). |
| D1b | Test hosts shorten the handler lifetime | `TestWebApplicationFactory` re-registers `ConfigureAll<HttpClientFactoryOptions>` with a handler lifetime of 1 second (settled in DISTILL, guard case C), after the product's two minutes. Test-only. | Any host that did make one factory request stays pinned until lifetime plus the factory's fixed 10 s cleanup tick. With 2 minutes that is the 155 late hosts; with seconds it is a window of about 15 s. The product's value is unchanged. Handler lifetime only decides connection-pool reuse, which no test relies on (no test references it). |
| D2 | Rate limiting stays ON; its two timer loops stop pinning hosts | (a) The one-address global limiter becomes a DI singleton owned by the container (so it is disposed with the host) and the options pick it up through `IConfigureOptions<RateLimiterOptions>`. The limiter body, partitions and numbers are moved, not edited. (b) The framework-built endpoint limiter: see below. | (a) is certain and public-API only. Candidates rejected for the global limiter: dispose on `ApplicationStopping` (works, but a second lifetime story next to DI for the same object) and `ExecutionContext.SuppressFlow` (cuts only the context path, which is sufficient for this limiter because its partitioner is static, but leaves the object undisposed). |
| D2(b) | The endpoint limiter | **No public API disposes it or cuts its graph.** It is created inside `RateLimitingMiddleware` and never exposed. `SuppressFlow` is not enough: its timer loop is an instance method whose partitioner is a closure over the middleware, and the middleware holds the rest of the pipeline and the service provider, so the static timer reaches the host by a second path even with the context cut. Maintainer decision (option 1, no condition; see "Maintainer decision - endpoint rate limiter"): the spike showed that (i), a release after (a) alone, does not happen. Build the middleware at a seam the product owns (`app.Use` with a factory that creates the framework middleware exactly as `UseMiddleware` does), register the instance with a small disposable owner in DI, and on dispose dispose the middleware's endpoint limiter by reading its private field. Unconditional, not test-gated: one code path, and disposing limiters at graceful shutdown is correct in production too. | The reflection in (ii) is the price of the framework not exposing ownership. It fails loudly: if the field moves, a disposed host stays alive and the guard test goes red. Rejected here: re-implementing endpoint policies inside our own limiter (rule-change risk and a second enforcement path), a test-only switch that builds limiters differently (tests would stop proving production), and turning limiting off in tests (the two rate-limit tests assert it). |
| D3 | The guard | A fast NUnit test in `Lighthouse.Backend.Tests/Architecture/`. After a warm-up host, create a `TestWebApplicationFactory`, start it (a plain test request, so the pipeline and both limiters are built; rate limiting is on by default in test hosts), dispose it, drop every reference, then poll with a full GC until its weak reference is dead, up to 15 s. Three cases: (A) a host that served a request and never asked the factory for a client; (B) a host that additionally resolved every `IOAuthProvider`; (C) a host that created one factory client. The factory is created in a separate non-inlined method so a Release build keeps no hidden local alive. The fixture is `[NonParallelizable]` and goes on the allowlist in `BackendTestParallelizationGuardTest` with the reason. | Case B needs no 2-minute wait: once the seam step has landed (neither the provider change nor the limiter change frees a host alone, measured), resolving the providers must not create a client, so the host dies at once; the regression it catches is a provider (or any singleton) taking a client again. A host that actually used a factory client is bounded by the handler lifetime by design (D1b); with the test host's one-second handler lifetime (D1b) such a host is gone within about 12 s, so case C covers it (about 12 s on a serial fixture); the late-host check also lives in D4. The test is its own probe in the Earned Trust sense: it exercises the real environment lie (timers rooted outside the host). Serial so a loaded neighbour cannot slow the polling or add allocation noise; other fixtures' hosts cannot keep ours alive. No CI reporting step is needed, so `ci_backend.yml` is untouched. |
| D4 | Payoff measurement | Lead-owned precondition and closing step; protocol below. | Peak, shape and duration, not only peak. |
| D5 | Behaviour-free proof, with disclosed changes | Existing tests, listed below; nothing deleted, skipped or weakened. Three changes are disclosed rather than called invisible: (1) the OAuth token clients' connection lifetime (before, the captured handler was never rotated; after, it rotates on the documented 2-minute handler lifetime and DNS is re-read; API behaviour unchanged; accepted as alignment with the framework's documented use); (2) option 1 adds a production boot-time failure mode: if the framework's rate-limiting internals are missing, the application refuses to start instead of serving without limits (accepted by the maintainer); (3) parity with the framework's `UseRateLimiter`, see below. | Routes, DTOs, responses and rate-limit rules are unchanged. The first two changes are real, small and accepted, so "behaviour-free" holds for what a caller of the API sees. |

### D2 spike and what it decides (written before the spike; the outcome is recorded below)

One short, uncommitted probe, run through `dotnet test --no-build --filter "FullyQualifiedName~<probe>"` only, never the full
suite: a warm-up host, then a host that served one request and never asked the factory for a client, disposed, full GC, weak
reference alive or dead. This is the guard's case A, so the guard test written first in step 1 is the probe; no throwaway code
is needed.
- After D1 + D2(a) the probe dies: D2(b)(i) holds and the seam step is not needed. This did not happen: the probe stayed alive (Rate-limiter spike, variant 1).
- It stays alive (the expected result, and what happened): apply D2(b)(ii), re-run, and the probe must die. It did (variant 5); the combination "seam + provider resolution" is confirmed when case B is un-ignored in the seam step.
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

**Recommendation (taken by the maintainer as option 1).** Take variant 5 for the seam step, with these edges stated rather than hidden:
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
   - app.UseRateLimiter() REPLACED by our       endpoint limiter: disposed with the container [via the seam]
     construction of the middleware [NEW,                ^
     unconditional]                                      | reflection edge (driven side):
       |                                                 | owner -> internal RateLimitingMiddleware / _endpointLimiter
       +--> rate-limiter owner [NEW] ---------------------+  fails loud at startup if the member is missing
                                          
 test host
   TestWebApplicationFactory [MODIFIED: short handler lifetime]
   TestHostLeakGuard [NEW]: build, serve, dispose, GC, assert dead (cases A, B, C)
```

Unchanged: every route, DTO, response, rate-limit rule and number, the 30 s timeout, `IOAuthProvider` and the registry, and
`OAuthService`.

## Wave: DESIGN / [REF] Component decomposition

| Component | Change | Notes |
|---|---|---|
| `JiraOAuthProvider`, `AdoOAuthProvider` | MODIFY | Constructor takes the factory instead of a client. Contract shape: bounded-change (no state kept besides the factory, time provider and logger). |
| `Program.cs` OAuth registration | MODIFY | Two singletons become type registrations. |
| `Program.cs` rate-limiting registration | MODIFY | The global limiter body moves into an owned class; the registration reads it via `IConfigureOptions<RateLimiterOptions>`. Contract shape: unchanged limits, universe = this host's limiter. |
| Rate-limiter owner (class + disposal of the endpoint limiter) | CREATE NEW | No existing alternative: nothing in the product owns a limiter's lifetime. Disposes on container disposal: the global limiter, and the middleware's endpoint limiter by reflection. |
| `app.UseRateLimiter()` (Program.cs:216) | REPLACE | With our construction of the internal middleware; unconditional; same `RateLimits:Enabled` gate and pipeline position. |
| `TestWebApplicationFactory` | MODIFY | Handler lifetime of 1 second. |
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
| `app.UseRateLimiter()` (Program.cs:216) | REPLACE | No public API exists to release the endpoint limiter: `RateLimitingMiddleware` is internal, not `IDisposable`, and `RateLimiterOptions` exposes no hook (Rate-limiter spike, finding 3c). Maintainer option 1. |
| Rate-limiter owner | CREATE NEW | No existing alternative: nothing in the product owns a limiter's lifetime. |
| Leak guard fixture | CREATE NEW | `ctx_search` over `Lighthouse.Backend.Tests` (the `Architecture/` folder included) for `WeakReference`, `GC.Collect` and `IsAlive` found only this fixture: no host-lifetime test exists to extend. |

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
- Parity with the framework's `UseRateLimiter` (the lead's reading: it checks that `AddRateLimiter`'s services are registered, throwing
  otherwise, and it adds the middleware). DELIVER confirms this against the framework source for the pinned version and keeps both in the
  replacement: the registration check and the pipeline position.
- Seam-step exit criteria: the four rate-limit classes `S6_RateLimitingTests`, `UsageDataConsentRateLimitTests`,
  `Slice11CastAVoteScenarios` (holds `A_voter_sending_more_than_thirty_entries_a_minute_is_told_to_slow_down`) and `UsageDataEventCeilingTests`
  (holds `OneAddressInventingANewBrowserEachTime_IsStillBounded`) plus the three OAuth classes `JiraOAuthProviderTest`, `AdoOAuthProviderTest`
  and `OAuthServiceTest` are green with the counts recorded; and a full standard-filter run with rate limits on is green apart from the known
  2 publish-output failures.
- OAuth: `JiraOAuthProviderTest`, `AdoOAuthProviderTest`, `OAuthServiceTest` (including the real-identity-provider refresh arrange),
  and the OAuth controller and registry tests. Assertions are untouched; only the arranged client source changes.
- `dotnet build` with zero warnings; the standard-filter `dotnet test`.
- Mutation (last, on frozen code): the two provider files. `Program.cs` wiring is covered by the host tests above.

## Wave: DESIGN / [REF] Payoff measurement (D4)

- Protocol: same machine, quiet, one suite at a time, `LIGHTHOUSE_TEST_WORKERS=6`, standard filter, the existing sampler and
  weak-reference monitor. Before = main immediately before step 2 (the 2026-11-01 numbers, 15.3-17.7 GB peak and 8 m 11 s, are the reference
  if the machine and day match; otherwise re-measure once). After = the branch with all steps. Report peak RSS, duration, and live
  factories after GC at the same marks. Both arms use the same sampler (story 6461's `measure.sh`) plus the weak-reference tail monitor.
- Target: peak under 3 GB, flat shape (live factories bounded by the hosts created inside one cleanup window, not by the total
  created). It is the ONLY target. The 4.2 GB figure of the fix experiment predates the final design (rate limiting was off and there
  was no seam); D1b is expected to remove the 155 late hosts it left. If DELIVER's measurement misses 3 GB, the miss is recorded
  honestly and left to the maintainer at the hold; nobody changes the target.
- Late hosts: keep the monitor alive for three minutes after the last test (temporary probe, removed) and expect the live count to
  fall to single digits by about 140 s; or re-run with D1b only and see them disappear. A host still alive after that gets the root
  walk from the retention-roots section.

## Wave: DESIGN / [REF] ADR-270 and ARCHITECTURE.md

ADR-270: no change. ADR-005 (`docs/product/architecture/adr-005-rate-limiting-middleware.md`): a short addendum in DELIVER's seam step (own construction, container-owned disposal, reflection with fail-loud startup, the .NET-upgrade check); a DELIVER task. ARCHITECTURE.md: check the rate-limiting section (today it names rate limiting only as an API-edge concern, §4 line 297) and update it in the same step if it describes construction. The feature touches no bounded context, adds no concept, term, edge or domain event: it changes only how long
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
| 1 | The guard fixture written first (done in DISTILL: all three cases `[Ignore]`d, RED recorded for each), plus the allowlist entry. |
| 2 | OAuth providers ask the factory per request (D1) and the test factory shortens the handler lifetime (D1b); case B green; three OAuth test classes re-arranged. Measured: neither fix alone frees any host, so this step turns no guard green. |
| 3 | The rate-limiter seam, one step (merges the old 3a and 3b): the global limiter owned by the container (D2a) and our construction of the middleware with the endpoint limiter disposed by the owner (D2b). All `[Ignore]`s on the guard come off here, together; this is the first step that frees any host. |
| 4 (lead) | Payoff measurement and the late-host check (D4); mutation on the two provider files, last. |

The guard is already committed, `[Ignore]`d (DISTILL). It is un-ignored in the seam step, the first change that turns it green, so no commit on the way is red.

## Wave: DESIGN / [REF] Maintainer decision — endpoint rate limiter (2026-11-01, 18:2x, in chat)

The rate-limiter spike showed the framework's endpoint limiter (inside `UseRateLimiter()`) is the last holder and that
no public API releases it. Options put to the maintainer: (1) build the internal `RateLimitingMiddleware` ourselves in
`Program.cs` and let a container-owned singleton dispose the global limiter and the middleware's private endpoint
limiter at shutdown, by reflection; (2) accept the pin and leave the payoff to fewer hosts; (3) measure (2) first.

**Decision: option 1.** Rate limiting stays on with the same rules. The reflection fails at startup if the internal
type, constructor, `Invoke` method or field is not found, never by silently skipping the limiter; the in-suite leak
guard catches a regression. This supersedes D2(b)'s conditional wording: the seam is no longer gated on the spike, it
is part of the fix (one step, with the global limiter). Re-check the reflection on every .NET major upgrade.

## Wave: DEVOPS / [REF] Platform decisions (2026-11-01)

AFK mode: the lead took the recommended option on each row; the maintainer reviews before anything is pushed. Nothing deploys.

| Decision | Answer |
|---|---|
| Deployment target, orchestration, strategy | N/A, because nothing deploys separately: no artifact, image or runtime behaviour changes. Production runs one host per process, so the ownership changes cost nothing there; two changes are visible and accepted (OAuth handler rotation; a boot-time failure mode), see D5. |
| CI/CD platform | GitHub Actions, `ci_backend.yml` read and **UNCHANGED**; see the CI/CD section. |
| Branching | Trunk-based on `main`, local commits, the maintainer reviews before push. Nothing is pushed by the waves. |
| Observability, alerting, SLOs | The existing rate-limit logging and metrics are unchanged (the middleware is the framework's own, only constructed by us). The one runtime-visible change: the OAuth token clients' connection lifetime. The handler now rotates on the documented 2-minute lifetime (DNS is re-read); before, it was never rotated. API behaviour is unchanged. No new metric or alert. |
| Usage-data event | **N/A, because it is behaviour-free test infrastructure with no user-facing feature**; nothing a user does changes, so there is no use to count. No `UsageDataEventName` entry, no line in `docs/settings/usagedata.md`. |
| Security | No secret, permission or scope changes. The reflection touches only a private field of a framework type in our own process. |
| Cross-cutting (RBAC, Lighthouse-Clients, Website, UI sketch, Terminology, ADR-270) | All N/A as answered in PICK-UP and DESIGN: no route, DTO, screen, word, context or event changes. |
| Rollback | Revert the commits; nothing is persisted. |

## Wave: DEVOPS / [REF] Environment matrix

Also written as `environments.yaml` in this workspace.

| Environment | Workers | Configuration | Role |
|---|---|---|---|
| Local, quiet machine (12 cores, 31 GB, one suite at a time) | `LIGHTHOUSE_TEST_WORKERS=6` | Debug (`dotnet test` default), standard filter | The only place the payoff (peak RSS) is measured. |
| Local, any session | Story 6461 cap (half the cores when unset and not CI) | Debug | The guard runs as part of the normal backend run. |
| CI (`ubuntu-latest`, 4 cores, `CI=true`) | Each tool's own default: NUnit 4 workers; the 6461 cap is off in CI | **Release**, `dotnet test -c Release --no-build`; both steps (with coverage when `run_sonar`, without otherwise; one runs per job) | Runs the guard and the rate-limit and OAuth tests. |

## Wave: DEVOPS / [REF] CI/CD

**`ci_backend.yml` is unchanged (read-only check).** Its two test steps already run the whole test project, so the guard (an NUnit test in
`Lighthouse.Backend.Tests/Architecture/`) runs inside them. No reporting step, no memory ceiling (rejected in DESIGN: noisy proxy).

- **Release build.** The guard creates the factory in a `[MethodImpl(MethodImplOptions.NoInlining)]` method and returns only a `WeakReference`, so
  the JIT cannot keep the factory in a local of the test method; Release enregistration and inlining cannot extend its lifetime. The polling loop
  runs `GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect()` and checks the weak reference for up to 15 s. Release does not change what the timers
  root, so the guard measures the same thing as Debug. DELIVER runs the guard once under `-c Release` locally (cheap, one filtered run) and states the result.
- **4-core runner.** The fixture is `[NonParallelizable]` and sits in `AllowedSerialFixtures` of `BackendTestParallelizationGuardTest` with its reason; NUnit
  runs it alone, so no neighbour's allocations slow the polling and no other fixture's host can be mistaken for ours. The cost is one short serial
  window (sub-second when it passes; at most 15 s per case when it fails). The 15 s limit is the open question 3 from DESIGN: if the first CI run
  flakes, raise the limit, never drop the assertion.
- **`-- RunConfiguration.MaxCpuCount=0` (both steps).** This is a VSTest argument that sets how many test hosts VSTest may start (0 = all cores). It
  does not change NUnit's worker count inside a host, does not alter the guard's GC polling, and does not remove `[NonParallelizable]`. Not affected.
  (It does replace the story 6461 csproj property in CI; that is already the 6461 design.)
- **Coverage step.** Under `--collect:"XPlat Code Coverage"` the profiler adds allocation and slows the run, which only makes polling slower, not the
  result different; the 15 s limit has headroom. Checked on the first CI run.

## Wave: DEVOPS / [REF] Startup failure mode

The container-owned disposal finds the framework's internals by reflection: the internal `RateLimitingMiddleware` type (by name in the RateLimiting
assembly), its public constructor, its `Invoke` method and the private `_endpointLimiter` field. **If any is missing, the code throws at
startup with a message naming the missing member; it never skips the limiter.** (Maintainer decision, option 1.)

- Where it surfaces: the middleware is built when the pipeline is built, so **every `WebApplicationFactory` test and every app start** fails at once. In CI the
  first backend test step goes red immediately, not as a slow leak. A production start with a mismatching runtime also fails at boot rather than serving
  without rate limiting.
- Where it does not surface: a field that still exists but changes meaning (renamed semantics, different owner). The leak guard (cases A and B) is the
  backstop: the host would stay alive and the test goes red.
- **.NET-upgrade check.** On every .NET major upgrade (and a minor that touches `Microsoft.AspNetCore.RateLimiting`), run the backend tests before anything
  else; a startup failure naming the type, constructor, `Invoke` or `_endpointLimiter` means re-reading the new `RateLimitingMiddleware` source and
  updating the owner class. Renovate PRs bumping the SDK or the shared framework are the trigger; CI is the detector. Also noted in the owner class's
  failure message so the failing run explains itself.

## Wave: DEVOPS / [REF] Mutation strategy

Strategy stays `per-feature`, Stryker.NET, kill rate >= 80%, once at the end on frozen code (CLAUDE.md, not edited).

- **Scope:** `JiraOAuthProvider.cs`, `AdoOAuthProvider.cs` and the new rate-limiter owner class (final file name set in DELIVER; the config glob
  `**/*RateLimiterOwner*.cs` is adjusted to match).
- **Glob and filter are edited in the owner's commit** to the real owner file and test class names (today they hold placeholders), verified by a Stryker run whose report lists mutants per file.
- **Not `Program.cs`**, because it is a composition root: mutants there are wiring (registrations, middleware order, config keys) with no unit to
  assert against, the host tests that exercise it boot a `WebApplicationFactory` per test (18 min for coverage capture alone, see the repo memory), and the
  behaviour it wires is already proved by the rate-limit tests and the leak guard. Mutating it would cost hours for survivors that only an
  integration run could kill.
- **Acceptance suite excluded** (repo memory "Stryker must skip the acceptance suite"): `test-case-filter` selects the provider and owner test
  classes and subtracts `API.Integration`, `Integration.Containers` and `Acceptance`. The leak guard itself is not run by Stryker (it boots hosts and polls);
  the owner class is covered by its unit tests plus the reflection-failure tests, if DELIVER writes them.
- **Config:** `mutation/stryker-config.story-6459.json` (copied from the bug-5586 backend precedent; `break: 80`). Read the per-file table, not the total.

## Wave: DEVOPS / [REF] Payoff measurement protocol (owned by the lead, quiet machine)

- **Instrument:** `docs/feature/story-6461-t3-test-runs-leave-room/measurement/measure.sh`, called with `bash`:
  `bash measure.sh <label> <workdir> <command...>` prints `label,exit,wall_s,peak_tree_rss_mb,min_mem_available_mb,max_threads` (`OUT_DIR` sets the log folder).
- **Same sampler, both arms:** `measure.sh` plus the weak-reference tail monitor.
- **Command (both arms):** `LIGHTHOUSE_TEST_WORKERS=6 dotnet test` in `Lighthouse.Backend` with the standard filter
  (`TestCategory!=Integration&TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration`),
  after `dotnet build-server shutdown`, nothing else running.
- **Before** = `main` at the commit just before the first product change (checked out as a separate worktree so the arms do not share build output;
  the gitignored premium licence fixture is copied in). **After** = the final commit.
- **Two arms, n=1 each**, same machine, same day, run one at a time. Repeat rule, fixed in advance: repeat an arm if its peak differs from the reference by more than 10 %
  (reference for Before: 18.8-19.0 GB at 6 workers, 2026-11-01).
- **Record:** the SHAs of both arms, `nproc`, `dotnet --version`, the exact filter, and that `valid_not_expired_license.json` is present.
- **Report:** peak summed RSS, lowest `MemAvailable`, wall time, and the shape (RSS plateaus rather than climbs). Target from DESIGN D4: under 3 GB and flat, the only target. If the
  measurement misses 3 GB, record the miss honestly and leave it to the maintainer at the hold; nobody changes the target.
- **Late-host tail:** after the last test finishes, keep a temporary weak-reference monitor alive for **3 minutes** and expect the count of live factories
  to fall to single digits by about 140 s (handler lifetime plus the fixed 10 s cleanup tick). A host still alive after that gets the root walk from the retention-roots
  section. The monitor is committed as a script/patch under `measurement/` in this workspace (DELIVER writes it), so the protocol can be re-run.

## Wave: DEVOPS / [REF] Coexistence and push-time consequences

- **Story 6461 worker cap** (committed locally, not pushed): orthogonal. It changes how many hosts are alive at once; this story changes how long each lives.
  Every measurement states the worker count.
- **Program.cs edit forces the full backend Integration suite in CI** (repo note `program_cs_forces_full_integration_suite`): the path filter widens the run to the live
  Jira, Linear, Azure DevOps and ServiceNow connector tests. State this at push time as a CI consequence, not a defect of the change: the run is longer, and the Linear key is
  shared with CI, so avoid exploring the Linear API by hand and avoid other full runs shortly before the push.
- **ci_backend.yml, Stryker, IDE runners, `BackendTestParallelizationGuardTest`:** see `environments.yaml`; the only edit to an existing guard is one
  allowlist entry.

## Wave: DEVOPS / [REF] Risks and carry-forward for DISTILL and DELIVER

1. Guard flake on a loaded runner: raise the 15 s limit, never drop the assertion.
2. Reflection drift on a .NET upgrade: loud at startup by design; see the upgrade check.
3. Full Integration suite on the Program.cs push: announce it.
4. Case B must resolve the `IOAuthProvider`s and see no client taken; D1b (short handler lifetime in `TestWebApplicationFactory`) bounds hosts that really use a client.
5. Mutation last, after the payoff measurement, on frozen code; any edit after it shifts the survivors.

## Wave: DISTILL / [REF] Reconciliation

Reconciliation passed -- 0 contradictions. DISCUSS is covered at Epic level. DESIGN and DEVOPS agree on every point that matters
here: guard shape (D3), the maintainer decision (option 1, the endpoint limiter is disposed through a container-owned owner, the seam step
not gated on the spike), the fail-loud startup, and an unchanged `ci_backend.yml`. DESIGN's open questions are settled below
(Q1 route, Q5 the one `new` in `OAuthServiceTest`); Q4 (spike outcome of D2(b)): settled by maintainer option 1. Q2 (handler lifetime) is settled at 1 second for the test host (guard case C); Q3 (polling limit on CI) stays with DELIVER.
Maintainer AFK: the recommended option was taken for every choice. UI sketch review: N/A, because there is no UI.

## Wave: DISTILL / [REF] Scenario list

Classification lives here only; none of it is written into the code (project rule). Test names in the file are plain language.

| # | Scenario | Class | Where | Status in file |
|---|---|---|---|---|
| 1 | A host that served a request is freed once disposed (guard case A) | guard, error-shaped (the leak) | `Architecture/TestHostIsReleasedAfterDisposeTest.AHostThatServedARequestIsFreedOnceDisposed` | `[Ignore]` until the fix lands |
| 2 | A host that resolved every OAuth provider is freed once disposed (guard case B) | guard, error-shaped | `...AHostThatResolvedEveryOAuthProviderIsFreedOnceDisposed` | `[Ignore]` until the fix lands |
| 3 | Startup fails loudly when the framework's rate-limiting internals are missing | error, boundary | DELIVER writes it with the owner (see below); no scaffold | **not covered until DELIVER**; lands in the same commit as the owner class; exit criterion of the seam step |
| 4 | Behaviour preserved: rate limiting still on, same rules | preservation | existing `Slice11CastAVoteScenarios.A_voter_sending_more_than_thirty_entries_a_minute_is_told_to_slow_down`, `UsageDataEventCeilingTests.OneAddressInventingANewBrowserEachTime_IsStillBounded`, `S6_RateLimitingTests`, `UsageDataConsentRateLimitTests` | existing, green, unchanged |
| 6 | A host that created one factory client is freed once disposed (guard case C, the late-host pattern) | guard, error-shaped | `...AHostThatCreatedOneHttpClientIsFreedOnceDisposed` | `[Ignore]` until the seam step; poll limit 30 s, about 12 s on a serial fixture |
| 5 | Behaviour preserved: OAuth token exchange, refresh, errors | preservation | existing `JiraOAuthProviderTest`, `AdoOAuthProviderTest`, `OAuthServiceTest` (incl. refresh through a real identity provider), OAuth controller and registry tests | existing, arrange-only edit |

Counts: 3 new tests (all guard, all about a failure shape), 1 described (not covered until DELIVER), 2 preservation groups. Walking skeleton: N/A, because nothing a
user does changes; the guard is the end-to-end proof through the real composition root (the production `Program` via the test host).
Driven adapters: none new with real I/O. The guard uses the real host, the real timers and the real container; nothing is faked.

Guard design notes (all from the spike): a warm-up host is built and dropped first (the first host of every process is pinned by shared
one-shot timers); the host is created in a synchronous `[MethodImpl(NoInlining)]` method that returns only a `WeakReference` (an async
method's hoisted local keeps the host alive); one request to `/api/latest/zz-nothing-here` (a 404 that still passes the rate-limit
middleware, which answers DESIGN Q1) builds the limiters; then `GC.Collect`, `WaitForPendingFinalizers`, `GC.Collect` polled for up to 15 s.
`GC.Collect` needs a narrow `S1215` pragma with its reason (the build promotes it to an error). The failure message shows the elapsed seconds and the number of collection rounds. The warm-up discards the first host of the fixture's process run; that it is enough in full-suite order is not proven. Case C sets the expectation on the test host's 1-second handler lifetime (D1b) and the factory's 10 s cleanup tick: dead within about 12 s, poll limit 30 s.

## Wave: DISTILL / [REF] Acceptance criteria (Given / When / Then)

Driving port for all of them: the production `Program` composition root, entered through `TestWebApplicationFactory<Program>`, a real test host
(the driving adapter of the Architecture of Reference for this project). Contract shape of the guard: unbounded-preservation
in spirit (the whole host must be releasable), so the assertion is on the host object itself, not on a chosen field.

1. **Case A.** Given a test host that has served one request (so the rate limiters exist) and never asked the client factory for a client;
   when the host is disposed and every reference to it is dropped; then, within 15 seconds of full collections, the host can no longer be
   reached (its weak reference is dead). Via the test host.
2. **Case B.** Given a test host that has additionally resolved every `IOAuthProvider`; when it is disposed and dropped; then, within 15 s,
   it is gone. A provider that holds a factory client makes this fail. Via the test host.
3. **Fail loud (not covered until DELIVER; lands in the same commit as the owner class, and the owner's message names the missing member and the .NET-upgrade step).** Given a framework whose rate-limiting middleware lacks the expected type, constructor, `Invoke` method or `_endpointLimiter`
   field; when the application starts; then startup fails with a message that names the missing member and says to re-read the framework
   source after a .NET upgrade, and rate limiting is never silently skipped. Via the application start (every host test).
4. **Rate limiting unchanged.** Given the voting and usage-data scenarios that exceed their limits; when a caller exceeds the limit; then
   the caller is told to slow down exactly as before. Via HTTP, in the existing tests.
5. **OAuth unchanged.** Given the same token endpoint answers; when a token is exchanged or refreshed; then the same request goes out and the
   same result or error comes back. Via `IOAuthProvider` and `OAuthService`, in the existing tests.
6. **Case C.** Given a test host that has served one request and created one client from the client factory; when it is disposed and
   dropped; then, within 30 seconds of full collections (the test host gives client handlers a 1-second lifetime and the factory
   cleans up every 10 s), it is gone. Via the test host.

## Wave: DISTILL / [REF] Brief-AC traceability

| Brief AC | Scenario(s) |
|---|---|
| AC1 the retention root(s) are named | Delivered by DESIGN (retention roots section); guards 1, 2 and 6 pin the roots (the rate limiters' timer loops; a singleton holding a factory client; a host that really used a client, bounded by the handler lifetime) so they cannot return silently |
| AC2 peak memory falls and the shape is flat | **Owned by the lead's measurement** (DEVOPS payoff protocol, the only target is under 3 GB flat). No test asserts a memory ceiling (rejected in DESIGN). Guards 1, 2 and 6 are its structural cause-check |
| AC3 behaviour-free; a guard stops it coming back | Scenarios 4 and 5 (behaviour unchanged), 1, 2 and 6 (the guard). Scenario 3 (loud failure on drift) is not covered until DELIVER and is not counted here |

## Wave: DISTILL / [REF] Test placement and scaffolds

- Placement: `Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/TestHostIsReleasedAfterDisposeTest.cs`. Precedent: the other
  guard fixtures there (`PostgresContainerSharingGuardTest`, `PublishOutputOutsideProjectGuardTest`, `BackendTestParallelizationGuardTest`).
- Allowlist: `BackendTestParallelizationGuardTest.AllowedSerialFixtures` gained `TestHostIsReleasedAfterDisposeTest` with a plain-language reason.
  Both parallelization-guard tests stay green (run: 2 passed, 3 skipped with the new fixture).
- Scaffold pattern: no production stub is needed (the guard compiles against code that exists). The tests carry
  `[Ignore("Enabled when the test-host leak fix lands")]`, following NUnit's own mechanism (the project has `[Ignore(...)]` precedent in
  `Story6055ActivityNamesTheWorkScenarios`). No `__SCAFFOLD__` marker applies; DELIVER removes the two `[Ignore]` lines.
- DELIVER order: measured, neither fix alone frees any host, so the `[Ignore]`s on cases A, B and C come off together, in ONE step: the
  rate-limiter seam step (the old 3a and 3b merged). The OAuth step (D1, D1b) turns no guard green. The build stays green throughout. The
  combination "seam + provider resolution" is confirmed when case B is un-ignored.
- Fail-loud startup scenario (3), not scaffolded because it needs the owner type, which does not exist yet and a stub that does not compile
  is not allowed. DELIVER writes, with the owner class, one test per missing member, each as a plain unit test on the owner's lookup (give
  the lookup the assembly or a type to inspect, so a test can hand it a type lacking the constructor, `Invoke` or `_endpointLimiter`, and
  assert the exception message names that member and mentions the .NET upgrade). These tests also serve the mutation run on the owner. They are not covered until DELIVER: they land in the same commit as the owner class and are an exit criterion of the seam step.

## Wave: DISTILL / [REF] Arrange-only edits when the provider constructors take `IHttpClientFactory`

Assertions stay untouched; only where the client comes from changes. Each place builds its provider with `new XOAuthProvider(new HttpClient(handler), timeProvider, logger)`:

| File | Line (today) | Edit |
|---|---|---|
| `Services/Implementation/OAuth/Providers/JiraOAuthProviderTest.cs` | `CreateProvider`, 223-228 | Build a `Mock<IHttpClientFactory>`; `Setup(f => f.CreateClient(JiraOAuthProvider.HttpClientName)).Returns(() => new HttpClient(handler, disposeHandler: false))`; pass `factory.Object`. A function returning a fresh client (not a shared instance) mirrors the per-request ask; `disposeHandler: false` stops a client disposed by one request's `using` from disposing the shared stub handler the next request still needs. |
| `Services/Implementation/OAuth/Providers/AdoOAuthProviderTest.cs` | `CreateProvider`, 291-296 | Same with `AdoOAuthProvider.HttpClientName`. |
| `Services/Implementation/OAuth/OAuthServiceTest.cs` | `ArrangeRefreshThroughARealIdentityProvider`, 796-807 | Same, around `new HttpClient(identityProvider, disposeHandler: false)`; this is the one `new JiraOAuthProvider` (DESIGN Q5: yes, one). The `RequestCountingHandler` assertions stay. |

If the tests call the provider's `HttpClientName` constant, it is already public on each class. Do not add a shared helper (DESIGN reuse analysis).

## Wave: DISTILL / [REF] RED classification (run 2026-11-01)

Run: both `[Ignore]`s removed temporarily, `dotnet build` (0 warnings), `dotnet test --no-build --filter "FullyQualifiedName~TestHostIsReleasedAfterDisposeTest"`, then the `[Ignore]`s restored.

| Scenario | Result | Classification |
|---|---|---|
| Case A | `Failed AHostThatServedARequestIsFreedOnceDisposed [15 s]` ... `Assert.That(WaitUntilFreed(host), Is.True)  Expected: True  But was: False` | MISSING_FUNCTIONALITY (correct RED): the assertion fires on a live weak reference after 15 s of collections; no import, setup or compile error |
| Case B | `Failed AHostThatResolvedEveryOAuthProviderIsFreedOnceDisposed [15 s]` ... same assertion, `Expected: True  But was: False` | MISSING_FUNCTIONALITY (correct RED) |
| Case C | `Failed AHostThatCreatedOneHttpClientIsFreedOnceDisposed [30 s]` (message and assertion printed below) | MISSING_FUNCTIONALITY (correct RED): the test host still has the 2-minute handler lifetime, so the host is alive at 30 s |
| Fail-loud startup | not in file | Not applicable until the owner exists |
| Preservation (4, 5) | green today | Existing tests; they must stay green, the OAuth ones need the arrange edit above when the constructors change |

Case C, run on its own after revision 1 (`[Ignore]` removed temporarily, only this test run, then restored):

```
Failed AHostThatCreatedOneHttpClientIsFreedOnceDisposed [30 s]
  Error Message:
     A disposed test host that created one HTTP client is still reachable after 30.3 seconds (104 rounds of collections). The test host should give HTTP client handlers a lifetime of one second; check that it does.
Assert.That(outcome.Freed, Is.True)
  Expected: True
  But was:  False
Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 30 s
```

Honest limit: a live reference at 15 s is the same signal for "still pinned by a timer" and for "pinned by something new", so the message names the two
known causes and the fix section of DESIGN names how to walk the timers if a third appears.

## Wave: DISTILL / [REF] Driving-port coverage

| Driving port | Coverage |
|---|---|
| Application start via `TestWebApplicationFactory<Program>` | Guard cases A and B (disposal), and scenario 3 (startup failure) |
| HTTP rate-limited routes | Existing rate-limit tests (scenario 4) |
| `IOAuthProvider` / `OAuthService` | Existing OAuth tests (scenario 5) |

## Wave: DISTILL / [REF] Definition of Ready (DISTILL)

| Item | Status | Evidence |
|---|---|---|
| Reconciliation, 0 contradictions | PASS | above |
| 2 Persona | PASS | the maintainer running the suite (infrastructure exception) |
| 4 UAT G/W/T | PASS | the five G/W/T in "Acceptance criteria" (cases A and B, fail loud, rate limiting unchanged, OAuth unchanged), plus the sixth for case C |
| 8 Dependencies | PASS | Story 6461 (worker cap, measurement script) is a tracked soft dependency: committed locally, not pushed |
| 9 Outcome measurable | PASS (additional lead check) | peak memory of a full standard run, before and after, owned by the lead; target under 3 GB flat, the only target |
| Every brief AC maps to a scenario or an owner | PASS | traceability; AC2 owned by the lead |
| Error-path share | PASS | both new tests are leak (failure-shape) guards; fail-loud described |
| Test placement with precedent | PASS | Architecture/ guard fixtures |
| Scaffolds green at hand-off | PASS | build 0 warnings; fixture ignored; parallelization guard 2 passed |
| RED classified as the right reason | PASS | both cases fail on the live reference, run printed above |
| Arrange-only edits listed | PASS | table above |
| No scenario tags, no internal refs in code | PASS | checked in the new file |
| Open for DELIVER | NOTE | polling limit on CI (Q3); the exit criteria listed under "Gate revision 1"; fail-loud tests with the owner |

## Wave: DISTILL / [REF] Gate revision 1 (2026-11-01)

Applied after the four-reviewer gate, on the lead's decisions. Code: the leak guard gained case C, a result tuple for the wait, and the
message wording; `BackendTestParallelizationGuardTest` has the new reason text. Nothing else in code changed.

| Decision | Finding it resolves | Where it changed |
|---|---|---|
| L1. Neither fix alone frees any host (measured), so both `[Ignore]`s come off together in ONE step, the rate-limiter seam step (old 3a and 3b merged). The OAuth step turns no guard green. "Seam + provider resolution" is confirmed when case B is un-ignored. | The slicing implied a guard turning green at the OAuth step and a spike-gated step 3b | Slicing table; D3 "dies at once" sentence; D2 spike bullets (the "step 3b is dropped" text); Maintainer decision section; DISTILL reconciliation; DELIVER-order bullet in Test placement |
| L2. Every pre-decision part of the design says option 1, unconditionally | Design text still read as conditional on the spike | Hexagonal sketch (unconditional seam, reflection edge on the driven side); Component table row; Reuse Analysis (REPLACE row citing "no public API", guard row citing the `ctx_search` result: no other `WeakReference` or `GC.Collect` host-lifetime test in the test project); Spike header and D1/D2(a) wording; DISTILL reconciliation Q4; Retention roots verdict rewritten as two roots that both pin every host, plus the per-host payload |
| L3. Payoff target is under 3 GB and flat, the only target; the 4.5 GB fallback is deleted. A miss is recorded and left to the maintainer at the hold | A movable target; the 4.2 GB figure came from a design without rate limiting or a seam | D4 (design), DEVOPS payoff protocol, AC2 row; the 4.2 GB cell says it predates the final design. Same sampler in both arms, monitor committed under `measurement/` (DELIVER writes it), repeat rule fixed in advance (more than 10 % off the reference), SHAs, `nproc`, `dotnet --version`, exact filter and licence-file presence recorded |
| L4. Behaviour-free, disclosed honestly: OAuth token clients' connection lifetime changes (rotation every 2 minutes, DNS re-read; API unchanged; accepted); option 1 adds a production boot-time failure mode (accepted); parity with `UseRateLimiter` | "identical" and "invisible" overstated | D1, D5, the D5 proof bullets (parity, four rate-limit classes plus three OAuth classes with counts, full standard run green apart from the known 2 publish-output failures), DEVOPS platform rows |
| L5. Fail-loud tests (scenario 3) are not coverage yet | AC3 counted tests that do not exist | Scenario list, AC3, traceability AC3 row (removed from coverage), Test placement, Driving-port coverage; Stryker glob and filter edited in the owner's commit (DEVOPS mutation) |
| L6. Guard case C for the late-host pattern | AC3 not fully met without it | Code: `AHostThatCreatedOneHttpClientIsFreedOnceDisposed`, `[Ignore]`d, 30 s poll limit; D1b row (1 second); D3; scenario 6; AC 6; RED classification with the printed output |
| L7. Smaller fixes | Review nits | Arrange table (`disposeHandler: false`); allowlist reason; case A message; `WaitUntilFreed` returns false after the loop and the message shows seconds and rounds; warm-up comment; DoR rows; the garbled driving-port phrase; `Program.cs:340-343`; ADR section |

### DELIVER exit criteria and tasks added by this revision

- Seam step: all `[Ignore]`s off together; the fail-loud tests (one per missing member: type, constructor, `Invoke`, `_endpointLimiter`) land in the
  same commit as the owner class, and the owner's message names the missing member and the .NET-upgrade step.
- Stryker `mutate` glob and `test-case-filter` edited to the real owner file and test class names in that commit, verified by a run that reports
  mutants per file.
- Guard passes 5 consecutive runs under `-c Release` alone, and once inside a full standard run. Confirm the `[NonParallelizable]` exclusivity from
  that run's log. At the final commit a grep shows no `[Ignore]` left in the fixture. An abandoned fix removes the scaffold rather than leaving it skipped.
- ADR-005 addendum in the seam step (own construction, container-owned disposal, reflection with fail-loud startup, the .NET-upgrade check); check
  ARCHITECTURE.md's rate-limiting section and update it in the same step if it describes construction. ADR-270: no change.
- Renovate risk: framework bumps arrive as Renovate PRs that CI verifies before merge. DELIVER confirms, read-only, with
  `gh api repos/{owner}/{repo}/branches/main/protection` or the ruleset API, whether the backend tests are required checks. If they are not,
  record that as a risk for the maintainer; change nothing.
- Case C costs about 12 s on a serial fixture (1 s handler lifetime plus the factory's 10 s cleanup tick).
