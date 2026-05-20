# RCA — verifypostgres flake (round 3): HTTP DELETE races queue updates

## Story so far

1. **Round 1** — `fix-portfolio-refresh-race` slice (`982017c6`/`55033d62`). Fixed `Active updates: 2` symptom via `ConcurrentDictionary.TryAdd` in `UpdateQueueService.EnqueueUpdate`. Closed the dedupe TOCTOU.
2. **Round 2** — `994ac67b` rolled back the `RemoveOrphanedFeatures` guard in `LighthouseAppContext.PreprocessDataBeforeSave` (regressed delivery tests), then `c7a45b09`/`ce4ce866` added `IOrphanedFeatureCleanupService` invoked at refresh tail + startup as the proper restoration of orphan cleanup.
3. **Round 3 (this slice)** — CI run [`26160018077`](https://github.com/LetPeopleWork/Lighthouse/actions/runs/26160018077) shows `verifypostgres` STILL flaking on `PortfolioDetail.spec.ts:50:37`. The `Active updates: 1` hangs for minutes again. **The race is still firing, just on a different code path.**

## Symptom

Same Playwright failure as before:
```
expect(refreshFeatureButton).toBeEnabled() failed — 30000ms
Locator: getByRole('button', { name: 'Refresh Features' })
unexpected value "disabled"
```

The button stays disabled because the in-flight update task does not transition to `Completed` or `Failed` within Playwright's wait window. The update task is hung in `SaveWithRetry` cycling through `DbUpdateConcurrencyException` retries.

## Stack trace from the latest failure

```
Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException:
The database operation was expected to affect 1 row(s), but actually affected 0 row(s)
    at Npgsql.EntityFrameworkCore.PostgreSQL.Update.Internal.NpgsqlModificationCommandBatch...
    at LighthouseAppContext.SaveWithRetry            :316
    at LighthouseAppContext.SaveChangesAsync         :303
    at RepositoryBase.Save                           :80
    at RefreshLogService.LogRefreshAsync             :18
    at PortfolioUpdater.Update                       :90
```

The exception bubbles from the update task's `SaveChanges` chain.

## Root cause — TWO contexts racing on overlapping rows

The exception fires because some EF Core tracked entity (in context A) had its DB row deleted by a different EF Core context (B) between A's load and A's save. Without an explicit concurrency token, EF detects the deletion via "0 rows affected" on the `UPDATE … WHERE Id = X`.

| | Context A (queue task) | Context B (HTTP request) |
|---|---|---|
| Origin | Periodic `PortfolioUpdater.UpdateAll()` → enqueued task → `UpdateQueueService` channel reader → `PortfolioUpdater.Update(id)` | `PortfolioController.DeletePortfolio(id)` (HTTP `DELETE /api/latest/portfolios/{id}`) |
| Scope | `IServiceScopeFactory.CreateScope()` in `UpdateQueueService.ExecuteUpdateTask` | Per-request ASP.NET scope |
| Mutations | `WorkItemService.UpdateFeaturesForPortfolio` loads → mutates → `featureRepository.Save()` (multiple calls across the update) | `portfolioRepository.Remove(id)` → `PortfolioRepository.Remove` → `RemoveOrphanedFeatures(id, portfolio)` → `Context.Features.RemoveRange(orphaned)` → `Save()` |
| Saves | ≥10 separate `SaveChanges` calls across the update | One `SaveChanges` |

The smoking gun in the failing log: at `11:51:58` two log lines fire in the same millisecond:
```
11:51:58 - INFORMATION - WorkItemService: Added 10 Items to Feature Integration Test Project
11:51:58 - INFORMATION - PortfolioRepository: Feature Service Level Expectation (3) is not related to any portfolio - removing.
11:51:58 - WARNING - LighthouseAppContext: Concurrency exception occurred, retrying 1/3
```
Two different services are mid-`SaveChanges` simultaneously — one is the queued `PortfolioUpdater` update task (context A), the other is the HTTP-driven `PortfolioController.DeletePortfolio` doing `PortfolioRepository.Remove` (context B). They share Postgres rows via cascade FKs and the many-to-many `Features ↔ Portfolios` join. Context B commits first; context A's next save finds rows missing → exception → 3 retries fail same way → bubbles → 2-3 minute hang → Playwright timeout.

## Why sqlite passes but postgres flakes

- **SQLite** holds a process-wide writer lock. Context A's transaction blocks B's (or vice versa); only one is in-flight at a time. Race surface ≈ zero.
- **Postgres** MVCC lets both transactions run concurrently. They commit independently; the loser sees "row not found." Race surface is large.

Same code, same SHA — different DB consistency model surfaces the race.

## Why the earlier fixes didn't catch this

- **TryAdd in `UpdateQueueService.EnqueueUpdate`** prevented the `Active updates: 2` double-queue symptom. Did not help here — the second actor isn't a queued task at all, it's a synchronous HTTP request.
- **Removing `RemoveOrphanedFeatures` from `PreprocessDataBeforeSave`** removed one of two orphan-sweep code paths. The OTHER path is still there: `PortfolioRepository.Remove` (lines 24-54) deletes orphan features as part of the portfolio-delete operation. That path runs in the HTTP request scope and is exactly what races the queue.
- **`IOrphanedFeatureCleanupService` at refresh tail** runs strictly after the update task's main work. It can't help with a race during the update.

## Proposed fix — Option B: route DELETE through the queue

Make `PortfolioController.DeletePortfolio` enqueue a delete task on the same `UpdateQueueService` channel that the periodic updates use, and await its completion. The channel's single-threaded reader serialises everything by construction.

### Why this and not the alternatives

- **Per-entity semaphore** (Option A): smallest diff (~50 LOC) but adds a SECOND coordination primitive parallel to the queue. Future code that adds new write-paths-on-Portfolio has to know to acquire the semaphore. Easy to forget. The queue is already the canonical "exclusive operation on entity X" primitive.
- **Make `SaveWithRetry` detach 'row gone' entities** (Option C): defence-in-depth, doesn't address root cause. The retry loop can still spin on each affected entity — and there are many in this scenario (the change tracker holds 100+ entities by the time the late saves fire). Keep as a follow-up resilience improvement.
- **Route DELETE through the queue** (Option B, chosen): single canonical primitive ("exclusive operation on portfolio X"). Future code that mutates a Portfolio's many-to-many or cascade graph either runs in the queue OR doesn't run at all if the portfolio is being deleted. Self-documenting via UpdateType.

### Implementation sketch

1. Add `UpdateType.PortfolioDelete` to the `UpdateType` enum.
2. Add `Task EnqueueDeleteAsync(int portfolioId, Func<IServiceProvider, Task> deleteWork)` to `IUpdateQueueService` (a synchronous-awaiting variant of `EnqueueUpdate`). Internally: TryAdd a new `UpdateStatus`, write to the channel, return a `Task` backed by a `TaskCompletionSource<bool>` that the reader's continuation completes.
3. `PortfolioController.DeletePortfolio` becomes `await updateQueueService.EnqueueDeleteAsync(id, sp => { sp.GetRequiredService<IRepository<Portfolio>>().Remove(id); ... })`. Returns 200 OK after the queue work completes. (If the channel is currently processing a long task, the controller awaits — same behaviour as if it had used a semaphore.)
4. Queue dedupe via `TryAdd`: if a delete for the same portfolio is already queued, the second caller awaits the same `TaskCompletionSource` instead of enqueuing twice.
5. Concurrent feature update on a portfolio being deleted: the queue processes the delete BEFORE or AFTER the update — never concurrently. After the delete commits, a subsequent update task sees `projectRepository.GetById(id) == null` and returns early (existing behaviour).

### What stays the same

- `IOrphanedFeatureCleanupService` at refresh tail + startup — unchanged. Still useful for orphan-Feature accumulation between portfolio operations.
- `TryAdd` in `EnqueueUpdate` — unchanged. Still closes the original double-queue TOCTOU.
- `RemoveOrphanedFeatures` in `PortfolioRepository.Remove` — unchanged. It now runs inside the queue so the orphan sweep no longer races user-facing updates.
- The existing E2E tests stay untouched. They were correct.

## Acceptance scope (handed off to DISTILL)

DISTILL designs the concrete acceptance-test scenarios. The fix must:

1. Survive 5 consecutive `verifypostgres` runs on the same commit with zero Playwright retries on `PortfolioDetail.spec.ts:50:37`.
2. Not regress any existing `DeliveriesControllerIntegrationTest` (10 tests) or `PortfolioUpdaterTest`.
3. Preserve the existing `DELETE /api/latest/portfolios/{id}` API contract: 200 OK on success, 404 on missing.
4. Add a backend integration test that proves a delete + concurrent update are serialised (no `DbUpdateConcurrencyException`).
5. Keep the cleanup service's invocation contract: cleanup still runs at refresh tail + startup, unaffected.

## Out of scope

- Routing TEAM DELETE through the queue (same race exists for `TeamController.Delete` calling `TeamRepository.Remove`). Same pattern but separate slice — flag in a follow-up note. The verifypostgres flake we're chasing is portfolio-driven; team-delete only races on portfolios that have already been deleted.
- Generalised concurrency tokens (xmin / RowVersion) on Feature/Portfolio. Larger refactor, separate slice.
- Option C (SaveWithRetry resilience). Belt-and-braces follow-up.
