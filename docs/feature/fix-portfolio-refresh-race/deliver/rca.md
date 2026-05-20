# RCA — E2E flake: portfolio refresh button never re-enables (Postgres ↔ SQLite alternation)

**Reporter**: Benjamin (CI/CD pipeline instability since ~2026-05-18)
**Reference CI run**: [GH Actions run 26116104462 / job 76898482119](https://github.com/LetPeopleWork/Lighthouse/actions/runs/26116104462/job/76898482119?pr=1424) — `Verify Postgres Linux Build / verifypostgres` failed; `verifysqlite` on the same SHA passed.

## Symptom

`Lighthouse.EndToEndTests/tests/specs/portfolios/PortfolioDetail.spec.ts:51` —
`should show correct Features for Jira portfolio on refresh` —
fails on the assertion at `:68`:

```ts
await expect(portfolioDetailPage.refreshFeatureButton).toBeEnabled();
```

The "Refresh Features" button stays `Mui-disabled` for the full 30 s timeout. All 3 Playwright retries hit the same wall. Pattern across recent runs is **alternating**: same SHA, same tests, sometimes only `verifypostgres` fails, sometimes only `verifysqlite`, occasionally both, occasionally neither.

## Root cause

Two interacting bugs in the portfolio-refresh path turn a benign race into a stuck background update:

### Bug A — Orphan-sweep deletes rows mid-update

`Lighthouse.Backend/Data/LighthouseAppContext.cs:368` runs `RemoveOrphanedFeatures()` inside `PreprocessDataBeforeSave()` on **every** `SaveChangesAsync`:

```csharp
var orphanedFeatures = ChangeTracker.Entries<Feature>()
    .Where(e => e.State != EntityState.Deleted && e.State != EntityState.Detached)
    .Select(e => e.Entity)
    .Where(f => !f.IsParentFeature && f.Portfolios.Count == 0)
    .ToList();

if (orphanedFeatures.Count != 0)
{
    Features.RemoveRange(orphanedFeatures);   // ← marks them Deleted
}
```

During `PortfolioUpdater.Update` (`Services/Implementation/BackgroundServices/Update/PortfolioUpdater.cs:35`) the sequence is:

1. `WorkItemService.UpdateFeaturesForPortfolio(project)` — mutates features, calls `featureRepository.Save()`.
2. `projectMetricsService.InvalidatePortfolioMetrics(project)` — touches change-tracked entities.
3. `deliveryRuleService.RecomputeRuleBasedDeliveries(...)` + `deliveryRepository.Save()`.
4. `writeBackTriggerService.TriggerFeatureWriteBackForPortfolio(project)`.
5. `forecastUpdateService.UpdateForecastsForPortfolio(project)` — another save.

A `Feature` whose `Portfolios` navigation collection isn't fully hydrated at step 2/3 reads `Portfolios.Count == 0`, gets queued for deletion by the orphan sweep, and a subsequent `Update(feature)` in step 4/5 issues an UPDATE for a row that's already been DELETEd in the same batch → **0 rows affected** → `DbUpdateConcurrencyException`.

### Bug B — `SaveWithRetry` can't recover from this case

`LighthouseAppContext.cs:306-329`:

```csharp
catch (DbUpdateConcurrencyException ex) when (retryCount < maxRetryCount)
{
    retryCount++;
    foreach (var entry in ex.Entries)
    {
        await entry.ReloadAsync(cancellationToken);   // ← reload from DB
    }
}
```

`ReloadAsync` on a row that *was* deleted in the same batch reloads to the post-DELETE state (or a tombstone, depending on EF Core version), but leaves the entry tracked. The next retry sees the same change tracker state and throws the same exception. After 3 retries the wrapper rethrows, `UpdateQueueService.ExecuteUpdateAsync` catches at the outer `try` and sets `Status = Failed`, but the **frontend disable-while-active gate (SignalR `GlobalUpdates`) only re-enables on `Completed`**, not `Failed`, until the next poll cycle — which is why the button still reads disabled at the 30 s mark in some runs.

### Bug C — TOCTOU window in queue dedupe (amplifier, not root cause)

`Services/Implementation/BackgroundServices/Update/UpdateQueueService.cs:44-52`:

```csharp
if (updateStatuses.ContainsKey(updateKey))   // check
{
    return;
}
// ... 4 lines later ...
updateStatuses[updateKey] = updateStatus;    // ... then set
```

If the background `UpdateAll()` tick (`UpdateServiceBase.cs:81`) and the HTTP `POST /api/v1/portfolios/{id}/refresh` (`PortfolioController.cs:47`) race the check, both pass `ContainsKey` and both enqueue. The log line `Active updates: 2` in the failing run is the smoking gun. Two queued tasks with the same `UpdateKey` get processed sequentially through the channel, but they share the same scoped `DbContext` lifetime semantics and the second one triggers the Bug A race more deterministically.

## Why now — and why postgres ↔ sqlite alternation

- **SQLite** serializes all writers at the file level (single global write lock). The sequence of operations in `PreprocessDataBeforeSave` → SaveChanges almost always interleaves the same way: orphan sweep is consistent because no other connection can be mid-flight.
- **Postgres** uses MVCC with row-level locks. Save batching from EF Core 10.0.8 (commits `10022082`, `b6839f50`, merged 2026-05-18) reorders the SQL within a batch, so the orphan sweep sees a different `Portfolios.Count` snapshot run-to-run.
- This is why same SHA flips between green/red on the two jobs: identical code, different DB-engine consistency model surfaces the same race differently.
- The build history on `main` confirms timing: green through 2026-05-17, cluster of 5 reds on 2026-05-18, intermittent since.

## Files affected by the proposed fix

1. **`Lighthouse.Backend/Lighthouse.Backend/Data/LighthouseAppContext.cs`**
   - `RemoveOrphanedFeatures` (lines 368-384): only consider `Modified` features whose `Portfolios` collection has actually been loaded (`Entry.Collection(f => f.Portfolios).IsLoaded`). Skip `Added` features — they haven't been linked to portfolios yet, deleting them is a guaranteed lost write.
   - `SaveWithRetry` (lines 306-329): if a reloaded entry's `State == Detached` (row deleted) **and** the same entity wasn't explicitly removed in this scope, surface the original exception rather than spinning retries against a tombstone.

2. **`Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateQueueService.cs`**
   - `EnqueueUpdate` (lines 34-57): replace the `ContainsKey` / indexer-set with an atomic `updateStatuses.TryAdd(updateKey, updateStatus)`; only proceed to write the channel if `TryAdd` succeeded.

3. **Regression test (new)** — `Lighthouse.Backend/Lighthouse.Backend.Tests/Services/BackgroundServices/Update/PortfolioRefreshConcurrencyTests.cs`
   - Postgres-backed integration test (use the existing `IntegrationTestBase` Postgres fixture). Two `TriggerUpdate(portfolioId)` calls in rapid succession; assert exactly one update task runs to `Completed`, no `DbUpdateConcurrencyException` is logged, and the portfolio's `UpdateTime` advances. This test must FAIL on `main` and PASS after the fix.

## Risk

- **Blast radius**: low–medium. The orphan-sweep guard is narrow (one extra predicate), the `TryAdd` is mechanical, the retry-loop change only adds a tombstone short-circuit.
- **No DB migration**, no frontend changes.
- **No E2E changes** — the failing spec is *correct* and stays untouched. The point of the bug fix is that the test was telling the truth.
- Existing `SaveChangesAsync` callers that *legitimately* delete an orphan still work, because the new predicate only excludes features whose `Portfolios` collection is **unloaded** (the race condition) — features whose collection is loaded and genuinely empty still get swept.

## Acceptance criteria

1. The new `PortfolioRefreshConcurrencyTests` fails on `main` (proves the bug) and passes after the fix.
2. `Lighthouse.EndToEndTests/tests/specs/portfolios/PortfolioDetail.spec.ts:51` passes on both `verifypostgres` and `verifysqlite` for **10 consecutive CI runs** (manual re-run of last 10 commits or a dedicated stress loop).
3. No `DbUpdateConcurrencyException` is logged during the refresh path under the regression test or the E2E suite.
4. All existing backend tests still pass with zero warnings (`TreatWarningsAsErrors`).
5. SonarCloud quality gate is green on the PR.

## Out of scope

- Generalised concurrency-token (`xmin` / `RowVersion`) wiring across `Feature` / `Portfolio`. That's a separate design decision (see Epic backlog).
- The frontend "Active updates: N" banner UX (it shows `2` because both queue entries pass the check pre-fix — once `TryAdd` closes the race, the banner will read `1` and the test will not even hit the slow path).
- The `test-speed-improvements` parallelization work (paused — see [[project_test_speed_improvements_paused]]).

## 2026-05-20 — Rollback note on the orphan-sweep guard

The orphan-sweep guard in `LighthouseAppContext.RemoveOrphanedFeatures` (commit `55033d62`, later `d0c0b299` pre-rebase) was **reverted** on the same day because it regressed five `DeliveriesControllerIntegrationTest` cases (run [26153057516](https://github.com/LetPeopleWork/Lighthouse/actions/runs/26153057516)).

### Why the guard backfired

The guard used `entry.Collection(f => f.Portfolios).IsLoaded` to distinguish "Portfolios is genuinely empty after fixup" from "Portfolios was never loaded." `IsLoaded` is the wrong signal:

- `IsLoaded` is set to `true` only by `Include`, `Load`, or `LoadAsync`. **Relationship fixup does NOT set it.**
- So a Feature whose link to Portfolio was removed via `Features.Clear()` + fixup ends up with `Portfolios.Count == 0` AND `IsLoaded == false` — indistinguishable from a Modified feature that was never loaded with Include.
- The test helpers `AddPortfolio` + `AddFeatures` create a Feature `F0` inline, then `portfolio.UpdateFeatures(features)` clears that link. With the guard, `F0` survives the sweep at AddFeatures save time. Later in the controller flow, `DeliveryRepository.GetFeaturesByIds(...).Include(f => f.Portfolios)` re-loads `F0` with `IsLoaded == true` and empty Portfolios → the guard now permits the sweep → `F0` gets deleted mid-Delivery-save → FK breaks → HTTP 404 on the next request.

### What survived the rollback

The `UpdateQueueService.EnqueueUpdate` TryAdd change (Bug C) stays. Its regression test `EnqueueUpdate_ConcurrentSameKey_OnlyOneQueued` stays. TryAdd alone removes the "Active updates: 2" symptom by preventing the second queue entry, which collapses the most common race window in practice (background periodic tick + HTTP refresh on same UpdateKey). The original E2E flake should occur much less often, though the latent orphan-sweep race in concurrent SaveChanges across separate scopes can still in principle fire.

### Path forward — deferred to a follow-up slice

The correct orphan-detection signal is **whether the join-table link to Portfolios was deleted in this scope**, not the in-memory state of the navigation collection. Implementation sketch:

```csharp
// Iterate Dictionary<string, object> entries (EF's representation of the implicit join entity
// for many-to-many in EF Core 5+) whose State == Deleted and whose Metadata is the
// Feature↔Portfolio join. Extract the FeatureId from the join row.
var unlinkedFeatureIds = ChangeTracker.Entries()
    .Where(e => e.State == EntityState.Deleted)
    .Where(e => e.Metadata.IsImplicitlyCreatedJoinEntityType /* and points at Feature↔Portfolio */)
    .Select(e => /* extract Feature FK column */)
    .ToHashSet();

var orphanedFeatures = ChangeTracker.Entries<Feature>()
    .Where(e => unlinkedFeatureIds.Contains(e.Entity.Id))
    .Select(e => e.Entity)
    .Where(f => !f.IsParentFeature && f.Portfolios.Count == 0)
    .ToList();
```

This precisely targets features whose link to a Portfolio was just removed (the intent of the sweep) without misfiring on features whose navigation collection happens to be empty for unrelated reasons. The implementation needs a small spike to confirm the EF Core join-entity API surface; that's the work of the next slice.

Until then, the production code keeps the original (heuristic, occasionally race-prone) orphan sweep.
