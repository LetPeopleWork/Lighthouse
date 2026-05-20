# Design — Orphaned Feature Cleanup (restoration after refresh-race rollback)

## Why this exists

In `fix-portfolio-refresh-race` we removed `LighthouseAppContext.RemoveOrphanedFeatures` from `PreprocessDataBeforeSave` because it was the root cause of a cross-DB E2E flake (`DbUpdateConcurrencyException` racing concurrent saves). That fix sacrificed the existing — albeit broken — orphan-cleanup behaviour: Features whose `Portfolios` collection becomes empty in the DB now accumulate. This slice restores the cleanup with a race-free implementation.

## Options considered

| Option | Pros | Cons | Race-safe? |
|---|---|---|---|
| A. Startup-only cleanup | Dead simple. One call site. | Standalone-only viable; server uptime is days → orphans accumulate. | Yes (single-threaded boot). |
| B. Dedicated `OrphanedFeatureCleanupService` (BackgroundService) using `DatabaseMaintenanceGate` | Decoupled from refresh. Configurable cadence. | New BackgroundService to maintain. Gate semantics need care (currently *skips* enqueued updates, doesn't queue them — could lose a refresh tick). | Yes if gate held. |
| **C. Hook into `PortfolioUpdater.Update` tail + one-shot startup call** (CHOSEN) | Zero new services. Reuses single-threaded queue. Cleanup cadence = refresh cadence. Startup covers cold-boot orphans. | Tied to refresh — paused refreshes pause cleanup. | Yes, in a fresh scope with a focused bulk DELETE. |

## Chosen design

Introduce `IOrphanedFeatureCleanupService` with a single method `Task<int> CleanupAsync(CancellationToken)`. The implementation:

1. Creates a fresh `IServiceScope` (same pattern as `UpdateQueueService.ExecuteUpdateTask`).
2. Resolves a fresh `LighthouseAppContext` from that scope.
3. Issues a single bulk-DELETE:
   ```csharp
   await db.Features
       .Where(f => !f.IsParentFeature && !f.Portfolios.Any())
       .ExecuteDeleteAsync(cancellationToken);
   ```
4. Logs the count and returns it.

Two invocation points:
- **`PortfolioUpdater.Update` tail** — invoked after the `try`/`finally` block so cleanup failures never affect the refresh's success/failure log. Runs inside the queue's single-threaded loop → no concurrent cleanup.
- **`Program.cs` startup** — one-shot call during host start, before the web pipeline serves requests. Covers operators who restart the process; standalone-mode users especially benefit (their orphan accumulation window is much longer between refresh cycles).

### Why this is race-free

- **Fresh scope, fresh DbContext** — no shared `ChangeTracker` with the refresh's accumulated entities. The bug we just rolled back was triggered by `RemoveOrphanedFeatures` iterating a change tracker holding 100+ entities; here the change tracker is empty.
- **Bulk DELETE bypasses change tracking** — `ExecuteDeleteAsync` generates raw `DELETE FROM Features WHERE NOT IsParentFeature AND NOT EXISTS (SELECT 1 FROM PortfolioFeature WHERE FeatureId = Features.Id)`. No `SaveChanges` involved, no `PreprocessDataBeforeSave` chain.
- **Queue serialisation** — both invocation points run inside the `UpdateQueueService` channel-reader loop (refresh-tail case) or during host startup before any handler accepts requests (startup case). No parallel cleanup possible.
- **FK cascade verified** — `FeatureWork`, `Forecasts` (and their `SimulationResults`), `Portfolios` join-table rows, `Deliveries` join-table rows all have `OnDelete: Cascade` configured at the DB level (`20250209101823_InitialCreate.cs`). The bulk DELETE cleans up dependent rows without explicit handling.

### What about concurrent writes from HTTP / other contexts?

- `PortfolioController.Refresh` triggers a queued update (`portfolioUpdater.TriggerUpdate(portfolioId)`) — that update will be processed sequentially after the in-flight task (including its cleanup tail). No race.
- `DeliveriesController` POST creates a Delivery referencing Features. Those Features ARE linked to a Portfolio (the controller requires it), so the cleanup query's predicate excludes them. No race in practice.
- Feature mutation paths (e.g., `WorkItemService.UpdateFeaturesForPortfolio` orphaning a feature via `portfolio.UpdateFeatures([...])`) happen INSIDE the refresh task. The cleanup runs AFTER the refresh task's `SaveChanges` has already committed the orphaning, so by the time the cleanup query runs the orphan state is consistent.

## Acceptance criteria

1. **Orphan deletion**: A Feature whose `Portfolios` collection is empty AND `IsParentFeature == false` is deleted when `CleanupAsync` runs.
2. **Linked feature preserved**: A Feature linked to ≥1 Portfolio is NOT deleted.
3. **Parent feature preserved**: A Feature with `IsParentFeature == true` is NOT deleted, even if `Portfolios` is empty (parent features are intentionally portfolio-less).
4. **PortfolioUpdater triggers cleanup**: After `PortfolioUpdater.Update` completes successfully OR fails, `IOrphanedFeatureCleanupService.CleanupAsync` is invoked exactly once.
5. **Cascade-safe**: When an orphan Feature is deleted, its `FeatureWork`, `Forecasts`, `SimulationResults`, and join-table rows for `Portfolios`/`Deliveries` are cleaned up by the DB-level cascade (verified by an integration test using SQLite).
6. **No regression in `DeliveriesControllerIntegrationTest`** — the 5 tests that regressed against the rolled-back guard must remain green.
7. **No `DbUpdateConcurrencyException` in normal refresh flow** — confirmed by clean CI run with no Playwright retries on `verifypostgres` / `verifysqlite`.
8. **Build clean**: `dotnet build /warnaserror` passes with zero warnings.

## Out of scope

- A dedicated BackgroundService with its own timer (option B). If usage patterns later show option C's cadence is insufficient (e.g., a tenant pauses refreshes for days), revisit.
- An admin API endpoint to trigger cleanup on demand. Easy follow-up if ops requests it.
- A mechanism to prevent feature orphaning in the first place (i.e., fixing the data model so `Portfolios.Clear()` cascades to delete the orphan). That's a separate, larger refactor.

## File map

### Production
- NEW `Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IOrphanedFeatureCleanupService.cs`
- NEW `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/OrphanedFeatureCleanupService.cs`
- MODIFY `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/PortfolioUpdater.cs` — invoke cleanup at tail of `Update`
- MODIFY `Lighthouse.Backend/Lighthouse.Backend/Program.cs` — register service + one-shot startup call

### Tests
- NEW `Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/OrphanedFeatureCleanupServiceTests.cs` — unit tests (SQLite in-memory)
- MODIFY `Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/BackgroundServices/Update/PortfolioUpdaterTest.cs` — assert cleanup invoked
