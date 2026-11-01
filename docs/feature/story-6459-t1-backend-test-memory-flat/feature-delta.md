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
