# Bug 5285 — Blackout Forecast Bug — RCA & Fix

**Reporter:** Chris · **ADO:** #5285 (Active, Severity 3-Medium) · **Date:** 2026-06-14

## Symptom

With weekends excluded (a recurring blackout rule) but no blackout days actually
falling in the relevant window, a forecast should be ~identical to having no
blackout configuration at all — weekends already carry zero throughput and the
projection already steps over them. Instead the forecast diverged noticeably.
Reproduced with Team Zenith data in both fixed-throughput-date and rolling-30-day
modes.

## Expected semantics (confirmed with reporter)

- **Past:** a blackout day is excluded from throughput as if it never existed —
  whatever its value (normally 0, but ignored even if > 0).
- **Future:** a forecast must never land on a blackout day — weekend, recurring
  rule, or one-off alike.
- Excluding weekends from the input *and* jumping over them in the future must
  yield ~the same result as keeping weekends in and not jumping — independent of
  implementation.

The throughput-stripping math (`FilterBlackoutDaysFromRunChart`) and the
forward projection (`ProjectWorkingDays`) were both verified correct against
these semantics on a clean cache.

## Root cause — stale throughput cache

The blackout-aware throughput is memoized in the shared static metrics cache
under the key `BlackoutAwareThroughput_{start}_{end}` (TTL = team refresh
interval). The key encodes only the window — **not the blackout configuration**.

Separately, **changing blackout configuration never invalidated any metrics
cache.** `BlackoutPeriodService` and `RecurringBlackoutRuleService`
Create/Update/Delete persisted and returned without touching the cache.

Consequence: after a user adds or changes blackout config, the previously cached
*unfiltered* throughput is served, while the forward projection
(`ProjectWorkingDays`, never cached) uses the *fresh* blackout days. Input and
projection disagree, skewing the forecast by ~11–15 days. A cold cache produces
the correct, near-identical result — which is exactly why it was intermittent and
hard to trust.

Empirically confirmed: distinct team ids (clean cache) strip identically across
fixed and rolling modes; reusing a team id (warm cache) returns byte-identical
stale results after a config change.

## Fix — event-driven invalidation

Blackout configuration is global, so a change must invalidate **every** team's
and portfolio's metrics.

- New domain event `BlackoutConfigurationChanged` (`Models/Events`).
- Published from `BlackoutPeriodService` and `RecurringBlackoutRuleService` on
  Create / Update / Delete (after `Save`).
- `BlackoutConfigurationChangedMetricsInvalidationHandler` fans out:
  `ITeamMetricsService.InvalidateTeamMetrics` for every team and
  `IPortfolioMetricsService.InvalidatePortfolioMetrics` for every portfolio.
- `InvalidateTeamMetrics` promoted onto `ITeamMetricsService` (the implementation
  already existed).
- Handler registered in `Program.cs`, mirroring the existing
  `PortfolioFeaturesRefreshed` invalidation handler.

Invalidation is a best-effort side effect: the dispatcher already isolates
handler failures so a metrics-cache miss can never abort the committed blackout
write.

## Tests

- **Symptom regression** (`TeamMetricsServiceTests`):
  `GetBlackoutAwareThroughputForTeam_BlackoutConfigChangesAfterCaching_StaysStaleUntilInvalidated`
  — caches unfiltered throughput, changes config, proves it stays stale, then
  proves `InvalidateTeamMetrics` strips the now-blacked-out day.
- **Handler** (`BlackoutConfigurationChangedMetricsInvalidationHandlerTest`):
  invalidates every team and every portfolio; no-op when none exist.
- **Dispatch** (`BlackoutPeriodServiceTest`, `RecurringBlackoutRuleServiceTest`):
  Create/Update/Delete publish the event; validation failures publish nothing.

Full backend suite: 3194 passed / 0 failed / 2 pre-existing skips. Zero-warning
build.

## Cross-cutting impact

- **RBAC** — N/A. Blackout endpoints stay gated (`LicenseGuard` premium +
  `RbacGuard` SystemAdmin); the event and handler are server-internal.
- **Lighthouse-Clients (CLI + MCP)** — N/A. No new or changed API contract; the
  fix is internal cache invalidation behind existing endpoints.
- **Website** — N/A. No user-visible feature change; forecasts simply become
  correct after a config change.
- **EF migration** — N/A. No schema change.
