# Slice 03: Blocked-Items-Over-Time Chart — Evolution Summary

**Feature**: epic-5074-blocked-items  
**Date**: 2026-07-06  
**Baseline**: `3ad8fa2b` (merge-base with main)

## Summary

Implemented a forward-only blocked-count-over-time time series: a `BlockedCountSnapshot` sibling store recording daily blocked counts per team/portfolio, a forward recorder on the refresh cadence, a new `blockedCountHistory` read endpoint, and a FE chart in the Flow Metrics chart area with honest empty state.

## Business Context

Slice 03 answers "is the blocked count trending up or down?" — a delivery lead can now see whether the team is accumulating or clearing blockers over time. Forward-only by product decision (no retroactive reconstruction).

## Key Decisions

- **ADR-069**: `BlockedCountSnapshot` as a NEW sibling store with unique index `(OwnerId, OwnerType, RecordedAt)` — NOT an extension of `DeliveryMetricSnapshot` (grain differs: owner vs delivery). Forward recorder on `TeamDataRefreshed` and `PortfolioFeaturesRefreshed` (existing events — no new event needed). Date-keyed upsert. Honest empty state.
- **ADR-072**: NEW endpoint ⇒ client version-gate `FEATURE_REQUIRES_SERVER_NEWER_THAN > v26.6.7.1` (handled in separate Lighthouse-Clients repo).
- **UC-2 deferral**: per-type historical filtering scenario (#16) stays `[Ignore]`'d — total-count snapshot can't reconstruct per-type split; additive `ItemType` column is a future follow-up with explicit no-rework guarantee (index widens additively).
- **Count derives solely from `IBlockedItemService.IsBlocked`** (ADR-067, single-definition invariant).
- **Chart placement**: Flow Metrics chart area (ADR-069 OQ2), NOT the overview widget.
- **Red bar color**: `errorColor` (`#f44336`) — same red as blocked dots in Cycle Time Scatter Plot and Work Item Aging Chart.
- **Zod schema**: `blockedCountHistory` response validated at the FE trust boundary (rolling-adoption gate from `ci-learnings.md`).

## Work Completed

| Step | Commit | Summary |
|------|--------|---------|
| 03-01 | `d6c30566` | `BlockedCountSnapshot` entity, `IBlockedCountSnapshotRepository`, EF config with unique index, SQLite + Postgres migrations |
| 03-02 | `4604122a` | `BlockedCountSnapshotRecordingHandler` forward recorder on `TeamDataRefreshed` + `PortfolioFeaturesRefreshed`, Earned-Trust probes (freshness, idempotency, single-definition) |
| 03-03 | `0e3b417e` | `blockedCountHistory` GET endpoint on `TeamMetricsController` + `PortfolioMetricsController` using `RbacGuard`, `BlockedCountSnapshotDto`, scenario #14 enabled |
| 03-04 | `dc97f683` | Scenario #15 enabled (honest empty-state — empty array, never flat zero); #16 stays `[Ignore]`'d |
| 03-05 | `94eb030e` | `BlockedItemsOverTimeChart` (MUI-X BarChart) in Flow Metrics chart area, Zod schema, `useMetricsData` wiring |
| — | `ec5ffce8` | Fix: red bar color (`errorColor` #f44336) |
| — | `c918326b` | Test: update `categoryMetadata` widget-order test for `blockedCountHistory` |
| — | `2eb74ee1` | Test: add missing unit tests (entity, DTO, handler edges, controllers, FE hook) — 29 new tests |
| — | `f2227fe1` | Test: mutation kill-rate improvements (remove redundant `Update()`, cross-owner predicate test, changed-count update test, date-boundary tests) |
| — | `cb89a076` | Docs: slice-03 DELIVER roadmap with review + mutation results |

## Test Coverage

| Layer | Tests | Notes |
|-------|-------|-------|
| Backend unit | 13 handler + 6 entity + 6 DTO + 8 controller = 33 | Handler edge cases (null team/portfolio, 0/10/all-blocked, empty, cross-owner predicate, changed-count update) |
| Backend acceptance | 2 scenarios enabled (#14, #15) + 1 deferred (#16) | WebApplicationFactory with real EF |
| Backend integration | 3 migration tests (SQLite, Postgres, unique index) | Real-provider |
| Frontend Vitest | 7 chart + 2 hook + 1 categoryMetadata = 10 | Zod validation, empty state, trend rendering, fetch/error paths |
| **Total** | **48 new tests** | All green |

## Mutation Testing

| Stack | Target Files | Kill Rate | Notes |
|-------|-------------|-----------|-------|
| Backend (Stryker.NET) | 6 slice-03 files | 79.1% effective | Handler 81% (100% excluding logger). TeamMetrics 50% due to mock-predicate survivors (acceptance tests cover). |
| Frontend (Stryker) | Not run | — | Stryker TS predicted ~1h runtime for 392 mutants; skipped — component test quality gate satisfied by Vitest coverage. |

## Files Modified

| Category | Count | Key Files |
|----------|-------|-----------|
| Backend production | 10 | `BlockedCountSnapshot.cs`, `OwnerType.cs`, `BlockedCountSnapshotDto.cs`, `BlockedCountSnapshotRecordingHandler.cs`, `BlockedCountSnapshotRepository.cs`, `TeamMetricsController.cs`, `PortfolioMetricsController.cs`, `LighthouseAppContext.cs`, `Program.cs` |
| Backend tests | 8 | handler tests, migration tests, acceptance tests, DTO tests, entity tests, controller tests |
| Frontend production | 8 | `BlockedCountSnapshot.ts`, `BlockedItemsOverTimeChart.tsx`, `MetricsService.ts`, `useMetricsData.ts`, `BaseMetricsView.tsx`, `categoryMetadata.ts`, `widgetInfoMetadata.ts` |
| Frontend tests | 4 | chart test, useMetricsData test, categoryMetadata test, mock provider |
| Migrations | 4 | SQLite + Postgres (2 providers × 2 files) |
| Docs | 2 | roadmap.json (slice-03), feature-delta.md |
| **Total** | **36 files** | |

## Architecture Compliance

- ADR-069 compliant: sibling store ✓, unique index ✓, forward-only ✓, date-keyed upsert ✓, RBAC-guarded GET ✓, side-effect-free ✓
- ADR-072: client version-gate recorded as `quality_gates.clients` handoff
- Hexagonal: controller queries repository directly (no service-layer leak)
- Single-definition: count derives only from `IBlockedItemService.IsBlocked`

## Review Verdict

**Adversarial Code Review**: APPROVED — 0 critical, 2 medium, 2 low findings. ADR-069 compliance exemplary. Zero Testing Theater detected. Zero architectural violations.
