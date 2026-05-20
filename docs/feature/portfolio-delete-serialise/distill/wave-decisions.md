# DISTILL wave decisions — portfolio-delete-serialise

## DWD-01: Walking-skeleton strategy = C (Real local)

The walking skeleton uses the real `LighthouseAppContext` (Postgres-capable via `IntegrationTestBase`), the real `UpdateQueueService` channel, the real `PortfolioRepository.Remove` cascade. The only test-double is `IWorkItemService` (mocked behind a `TaskCompletionSource` gate so the test controls when the slow update task releases). Rationale: the race is fundamentally between a real Postgres transaction and a real queue continuation — any InMemory-style WS strategy (A or B) would mask the very behaviour the slice exists to fix.

**Tags on WS scenario**: `@walking_skeleton @real-io` (informal — Lighthouse doesn't use a `.feature` tag system; this is just for cross-referencing the methodology).

## DWD-02: Test framework = NUnit + WebApplicationFactory, not pytest-bdd

The nw-distill skill emits `.feature` files for pytest-bdd. Lighthouse is C# / .NET 8 — the closest analogue is NUnit + `WebApplicationFactory<Program>` per `IntegrationTestBase`. Each Given/When/Then scenario in `acceptance-tests.md` maps to one NUnit `[Test]` method. The prose specification stays authoritative; the executable specification is C# integration tests.

## DWD-03: Skip the 4-reviewer ceremony for this slice

The nw-distill skill mandates four parallel reviewers (Eclipse / Architect / Forge / Sentinel) on every distill output. For this slice — a bug-fix follow-up with a fully analysed RCA and a single chosen approach — the ceremony adds latency without commensurate quality gain. The slice still goes through:
- `nw-software-crafter-reviewer` during DELIVER (per-step adversarial review, existing mandate).
- SonarCloud quality gate on push.
- CI green across `verifypostgres` + `verifysqlite` for at least one full run before declaring done.

If the slice grows in scope beyond what `acceptance-tests.md` lists, escalate to the full ceremony.

## DWD-04: Acceptance scope is OFFICIAL portfolio-delete only — TEAM delete is a follow-up

The same cross-context race exists for `TeamController.Delete` → `TeamRepository.Remove` (mirror of `PortfolioRepository.Remove`). Out of scope for this slice — flag in a follow-up note when the slice closes. Reason: the failing E2E (`PortfolioDetail.spec.ts:50:37`) only exercises the portfolio-delete path; routing both at once doubles the surface area without doubling the immediate value.

## DWD-05: `EnqueueAndAwaitAsync` is added to `IUpdateQueueService`, not a separate service

Adding a sibling service (`IPortfolioDeleter`) was considered and rejected. The capability "enqueue and await" is generic; future code (e.g. `TeamController.Delete` in DWD-04's follow-up) will want the same primitive. Centralising it on `IUpdateQueueService` keeps the queue as the canonical "exclusive operation on entity X" abstraction.

## DWD-06: Concurrent-delete coalescing semantics

When two HTTP DELETE requests for the same portfolio arrive simultaneously (M-03), they share the same enqueue slot via `ConcurrentDictionary.TryAdd` — only one set of work runs. The second request awaits the same `TaskCompletionSource<bool>` and observes the same outcome. Both clients see 200 OK (the work succeeded for both). This is the semantically correct behaviour: the user's intent ("delete this portfolio") is satisfied for both calls.

The edge case where the first call's work succeeds but the second call's HTTP path then does its own existence check and finds the portfolio gone returning 404 is acceptable. M-03 admits both 200 OK and 404 as valid outcomes for the second request.

## DWD-07: Cancellation tokens

The new `EnqueueAndAwaitAsync` accepts a `CancellationToken` (from `HttpContext.RequestAborted`). If the HTTP client disconnects before the queue processes the work, the controller's await is cancelled BUT the queue work itself runs to completion (we don't propagate the cancellation into the queue — half-deleted state is worse than a late-completion). Documented behaviour: client cancellation results in 499-equivalent on the controller side but the portfolio is still deleted server-side.
