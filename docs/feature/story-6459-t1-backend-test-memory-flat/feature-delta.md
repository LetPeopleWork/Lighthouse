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
