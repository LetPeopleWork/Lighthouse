# DISTILL — Acceptance tests for `portfolio-delete-serialise`

Adapted for Lighthouse's actual test stack: NUnit 4.6 + `WebApplicationFactory<Program>` + Postgres-capable integration tests. The `.feature` / pytest-bdd shape from the generic nw-distill methodology is not used here — instead each scenario maps to one NUnit `[Test]` method in a named integration test class. The Given/When/Then prose stays as the authoritative specification.

## Walking-skeleton scenario (drives the implementation)

### WS-01: `PortfolioController.DeletePortfolio` runs through the update queue and is serialised against in-flight updates

**Driving port**: HTTP `DELETE /api/latest/portfolios/{portfolioId}` (controller method `PortfolioController.DeletePortfolio`).

**Scenario**:
```
Given a Postgres-backed test host with a Portfolio P seeded
And  the UpdateQueueService channel reader is artificially blocked on a slow update task
   (TaskCompletionSource gate inside a fake IWorkItemService that the test controls)
And  that slow task was enqueued first via portfolioUpdater.TriggerUpdate(P.Id)
When the test issues HTTP DELETE /api/latest/portfolios/{P.Id}
Then the DELETE request does NOT return until the slow task is released
And  no DbUpdateConcurrencyException is logged during the DELETE handling
And  after the test releases the gate, the DELETE completes with HTTP 200 OK
And  the Portfolio is gone from the DB
And  no Feature row owned by P remains
```

**Test class**: `PortfolioDeleteSerialisationTests` (new file at `Lighthouse.Backend.Tests/API/Integration/PortfolioDeleteSerialisationTests.cs`).
**Test method**: `DeletePortfolio_WhileQueueTaskInFlight_AwaitsQueueDrain_Returns200_NoConcurrencyException`.

**Why this is the walking skeleton**: it exercises the full path the user actually takes (HTTP DELETE → controller → queue → repository → SaveChanges) and proves the core property the slice exists for (serialisation against in-flight updates). Failing this scenario means the slice has not shipped.

---

## Milestone scenarios (one TDD cycle each in DELIVER)

### M-01: Delete request returns 200 OK on the happy path (no concurrent updates)

**Driving port**: HTTP `DELETE /api/latest/portfolios/{id}`.

```
Given Portfolio P seeded with one Feature linked to it
And  no update tasks queued or in-flight
When DELETE /api/latest/portfolios/{P.Id}
Then HTTP 200 OK is returned
And  the Portfolio row is gone
And  the Feature row that was linked only to P is also gone (existing orphan-removal behaviour preserved)
```

**Test class**: `PortfolioDeleteSerialisationTests`
**Test method**: `DeletePortfolio_NoOtherActivity_DeletesPortfolioAndOrphanFeatures`

### M-02: Delete request returns 404 when the portfolio does not exist

**Driving port**: HTTP `DELETE /api/latest/portfolios/{id}`.

```
Given no Portfolio with Id 99999 exists
When DELETE /api/latest/portfolios/99999
Then HTTP 404 Not Found is returned
And  no queue task is enqueued (no listener side-effect)
```

**Test class**: `PortfolioDeleteSerialisationTests`
**Test method**: `DeletePortfolio_NonExistentId_Returns404_NoQueueWork`

### M-03: Two concurrent DELETE requests for the same Portfolio coalesce — second one waits for the first

**Driving port**: HTTP `DELETE /api/latest/portfolios/{id}` (two concurrent HTTP calls).

```
Given Portfolio P seeded
When two concurrent HTTP DELETE /api/latest/portfolios/{P.Id} requests are fired
Then exactly one of them returns HTTP 200 OK
And  the other returns HTTP 200 OK or HTTP 404 (acceptable: it raced and lost — second saw "already gone")
And  the Portfolio is gone exactly once (no double-DELETE crash)
And  no DbUpdateConcurrencyException is logged
```

**Test class**: `PortfolioDeleteSerialisationTests`
**Test method**: `DeletePortfolio_TwoConcurrentRequestsSamePortfolio_BothSucceedOrSecondIs404_NoException`

### M-04: DELETE on Portfolio X does not block a queued update for Portfolio Y

**Driving port**: HTTP `DELETE /api/latest/portfolios/{X.Id}` + queue `TriggerUpdate(Y.Id)`.

```
Given Portfolios X and Y are seeded
And  a deliberately-slow update task is enqueued for Y (gate-held)
When DELETE /api/latest/portfolios/{X.Id} is invoked
Then the DELETE completes promptly (does NOT wait for Y's update — different UpdateKey)
And  Y's update remains running
And  after releasing the gate, Y's update completes normally
```

**Test class**: `PortfolioDeleteSerialisationTests`
**Test method**: `DeletePortfolio_DifferentPortfolioInFlight_DoesNotBlock`

This proves we serialise per-portfolio, not globally. Wrong implementation (single global semaphore) would fail this.

### M-05: An update task triggered AFTER a queued delete sees the portfolio is gone and short-circuits

**Driving port**: HTTP `DELETE` + HTTP `POST /api/latest/portfolios/{id}/refresh`.

```
Given Portfolio P seeded
When DELETE /api/latest/portfolios/{P.Id} is enqueued (delete task queued)
And  immediately after, POST /api/latest/portfolios/{P.Id}/refresh is invoked
Then both queue tasks run in order: delete first, then refresh
And  the refresh task sees projectRepository.GetById(id) returns null
And  the refresh task returns early without throwing
And  no DbUpdateConcurrencyException is logged
And  the channel reader continues processing subsequent tasks
```

**Test class**: `PortfolioDeleteSerialisationTests`
**Test method**: `RefreshAfterDelete_Serialised_RefreshShortCircuitsOnMissingPortfolio`

### M-06: The cleanup service still runs at refresh tail after a successful delete-then-refresh sequence

**Driving port**: Direct invocation of `PortfolioUpdater.Update` + assertion on `IOrphanedFeatureCleanupService` mock.

```
Given Portfolio P exists, cleanupService is a mock
When PortfolioUpdater.Update(P.Id) runs to completion
Then cleanupService.CleanupAsync was invoked exactly once
```

This already exists in `PortfolioUpdaterTest.Update_AfterRefreshCompletes_InvokesOrphanedFeatureCleanup` (commit `ce4ce866`). We re-assert it to guard against accidental regression while we modify PortfolioController.

**Test class**: `PortfolioUpdaterTest` (existing)
**Test method**: existing — verify still green after slice.

### M-07: DELETE through the queue does not break the existing DeliveriesControllerIntegrationTest path

```
Given the existing 10 DeliveriesControllerIntegrationTest cases
When the full DeliveriesControllerIntegrationTest suite runs after the slice lands
Then all 10 cases continue to pass
```

**Test class**: `DeliveriesControllerIntegrationTest` (existing, unchanged)
**Test method**: re-run as regression check.

### M-08 (regression for the original CI flake): No `Active updates: 1` hang in a delete-during-update scenario

This is a behavioural assertion that mirrors what the failing E2E run exhibited. We model it at the backend level so the assertion is fast and deterministic.

```
Given Portfolio P with seeded Features linked to it
And  a fake IWorkItemService that, when called for P, blocks on a gate for at most 5s
When the test (a) calls portfolioUpdater.TriggerUpdate(P.Id) and (b) issues DELETE /api/latest/portfolios/{P.Id} in parallel
And  after releasing the gate the test polls /api/latest/update/status
Then within 10 seconds of the gate release, /api/latest/update/status reports hasActiveUpdates == false and activeCount == 0
And  no DbUpdateConcurrencyException is logged at any retry level
```

**Test class**: `PortfolioDeleteSerialisationTests`
**Test method**: `DeleteDuringInFlightUpdate_QueueDrainsWithoutConcurrencyException_StatusEndpointReturnsClean`

---

## Adapter coverage

The slice touches three driven components. Mandate 6 calls for each to have at least one real-I/O acceptance scenario.

| Adapter | Real-I/O scenario | Covered by |
|---|---|---|
| `LighthouseAppContext` (Postgres) | YES | WS-01, M-01, M-08 (Postgres-backed `WebApplicationFactory`) |
| `UpdateQueueService` channel | YES | WS-01, M-04, M-05, M-08 (real channel reader, real continuation) |
| `PortfolioRepository.Remove` cascade | YES | M-01 (deletes Portfolio + cascaded Features) |

`IOrphanedFeatureCleanupService` is unchanged in this slice; M-06 keeps the existing mock-based assertion.

---

## Driving-adapter coverage (Mandate per RCA P1)

Every controller path / queue entry the change touches has at least one scenario that enters through the protocol (HTTP / queue dispatch), not through a service-layer shortcut.

| Driving adapter | Protocol scenario |
|---|---|
| `PortfolioController.DeletePortfolio` | WS-01, M-01, M-02, M-03, M-04, M-05, M-08 — all use HTTP `DELETE` via `Client` |
| `PortfolioUpdater.TriggerUpdate` (queue enqueue) | WS-01, M-04, M-08 (via the existing `portfolioUpdater.TriggerUpdate` driver in the test base) |
| `UpdateNotificationHub` / `/api/latest/update/status` | M-08 (queries the live status endpoint, not the in-memory dictionary directly) |

---

## Scaffold inventory (Mandate 7, adapted for C#)

Files to create/modify so the new tests compile and fail RED (not BROKEN) on the first run:

| File | Kind | Purpose |
|---|---|---|
| `Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateType.cs` | MODIFY | Add `PortfolioDelete` enum value. RED scaffold = the value exists but no consumer yet. |
| `Lighthouse.Backend/Services/Interfaces/Update/IUpdateQueueService.cs` | MODIFY | Add `Task EnqueueAndAwaitAsync(UpdateType updateType, int id, Func<IServiceProvider, Task> work, CancellationToken ct = default)`. RED scaffold = method declared but implementation throws `NotImplementedException` (treated as RED by NUnit). |
| `Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateQueueService.cs` | MODIFY | Implement `EnqueueAndAwaitAsync` — internal: TryAdd a status, register a `TaskCompletionSource<bool>` keyed by `UpdateKey`, write to channel, return the TCS task. Reader's continuation sets the TCS on the way out (success/failure both complete the task). |
| `Lighthouse.Backend/API/PortfolioController.cs` | MODIFY | `DeletePortfolio` now calls `await updateQueueService.EnqueueAndAwaitAsync(UpdateType.PortfolioDelete, portfolioId, sp => { sp.GetRequiredService<IRepository<Portfolio>>().Remove(portfolioId); ... })`. Keep the existing 404 short-circuit BEFORE enqueue (don't enqueue work for a non-existent portfolio). |
| `Lighthouse.Backend.Tests/API/Integration/PortfolioDeleteSerialisationTests.cs` | NEW | Hosts WS-01 + M-01..M-05 + M-08. Uses the existing Postgres-capable `IntegrationTestBase` pattern. |
| `Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/UpdateQueueServiceTests.cs` | MODIFY | Add a focused unit test for `EnqueueAndAwaitAsync` returning a completion task that fires after the inner work runs. Add a focused unit test for two enqueues with the same UpdateKey sharing the same returned `Task`. |

`NotImplementedException` in C# corresponds to the methodology's "AssertionError" — it surfaces as a test failure (RED), not an infrastructure error. The Lighthouse `dotnet test` runner treats it as a normal failed assertion. No need for a `__SCAFFOLD__` marker — `git grep "NotImplementedException"` in production code after the slice should return zero hits in the modified files.

---

## Test placement

`Lighthouse.Backend.Tests/API/Integration/` — follows the precedent of `DeliveriesControllerIntegrationTest`, `OAuthControllerIntegrationTest`, `ApiKeyControllerHttpSmokeTests`. The new file `PortfolioDeleteSerialisationTests.cs` lives there. The unit-level queue tests live in the existing `Services/Implementation/BackgroundServices/Update/UpdateQueueServiceTests.cs`.

---

## Pre-requisites this slice depends on

From prior waves / earlier slices:

- `IUpdateQueueService.EnqueueUpdate` with `TryAdd` dedupe (`982017c6`) — the dedupe mechanism we extend.
- `UpdateKey` value record with `(UpdateType, int Id)` — unchanged.
- `IOrphanedFeatureCleanupService` (`c7a45b09`) — unchanged; still runs at refresh tail + startup.
- `PortfolioRepository.Remove` with `RemoveOrphanedFeatures` (existing) — unchanged; now runs INSIDE the queued delete task instead of in the HTTP request scope.
- `IntegrationTestBase` Postgres support — already in place; existing integration tests run against Postgres in CI's `verifypostgres` job and against SQLite locally. The new tests use the same base; the race they exercise is reproducible on SQLite too with the right gate timing.

---

## Self-review checklist (adapted from the nw-distill skill)

- [x] Walking-skeleton scenario (WS-01) names the user's actual driving port (`DELETE /api/latest/portfolios/{id}`).
- [x] Every milestone scenario names a driving port (HTTP or queue) — none enter through a private helper.
- [x] At least one `@real-io @adapter-integration`-equivalent scenario per driven component (Postgres, queue, repository).
- [x] At least 40% error/edge-path scenarios: M-02 (404), M-03 (concurrent delete), M-05 (refresh after delete short-circuit), M-08 (no concurrency exception in race) — 4 of 8 = 50%.
- [x] Scaffolds listed; each maps to a real file or a real signature change. RED via `NotImplementedException`.
- [x] No `Assert.Multiple(() => …)` planned — all multi-assert scenarios will use `using (Assert.EnterMultipleScope()) { … }` per the 2026-05-12 ci-learnings entry. Numeric-zero assertions will use `Is.Zero` not `Is.EqualTo(0)` per the 2026-05-20 entry.
- [x] Container preference: existing `WebApplicationFactory<Program>` (no testcontainers needed). The CI `verifypostgres` job already runs a Postgres container — local repro is SQLite-backed.

---

## Definition of done for the slice

1. Walking-skeleton scenario WS-01 is GREEN locally on Postgres.
2. All 8 acceptance scenarios are GREEN locally on Postgres + SQLite.
3. Full backend suite (`dotnet test --filter "Category!=JiraIntegration&Category!=AdoIntegration&Category!=LinearIntegration"`) is GREEN with at least 2329 + 8 = 2337 tests passing.
4. `dotnet build /warnaserror` returns zero warnings.
5. CI run after push: `verifypostgres` and `verifysqlite` both report no Playwright retries on `PortfolioDetail.spec.ts:50:37` for 5 consecutive runs.
6. SonarCloud quality gate passes (`new_violations = 0`).
7. `docs/ci-learnings.md` gets a new entry under `## Tests` documenting the cross-context race + the route-through-queue fix.
