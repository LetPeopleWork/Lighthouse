# DEVOPS Decisions — epic-6350-bulk-reports

**Agent**: Apex (`nw-platform-architect`) · **Date**: 2026-10-27 · **Mode**: autonomous subagent, documents only,
lean. Per-wave review skipped (no trigger: no new target, CI framework, observability rewrite or security change).
Full text: `../feature-delta.md` → `## Wave: DEVOPS / …`. Machine artifact: `../environments.yaml`. Design notes:
`upstream-changes.md`. Contracts: `docs/product/kpi-contracts.yaml` → `OUT-6350-*`.

## Key Decisions

- [DVO-1] **Four name-only usage events**, provisional integers, re-read on `main` by each emitting slice:
  `AllReportsListed` 21 (01 browser, 06 clients), `ReportBulkCreationStarted` 22 (02a), `ReportBulkRemovalStarted`
  23 (05), `ReportCombinedPdfExportStarted` 24 (07). Tests pin names and append order, never integers. Each lands
  with its `docs/settings/usagedata.md` row in one commit (see: feature-delta "Usage-Data Event Design").
- [DVO-2] **D19 decided**: the bulk-creation event fires per sent run, also when nothing is created; no outcome
  property. K2 counts tries.
- [DVO-3] **Existing `ReportCreated` / `ReportDeleted` fire per outcome inside bulk runs**: once per `created`, once per
  report this run removed (never already-gone, DU-1). No `origin` property (OQ-3 closed), no `report_template` on the
  bulk events, no entry-point enum: no KPI needs any of them.
- [DVO-4] **Combined PDF**: its own event for ≥ 2 reports; one selected report counts as the single export (DU-3).
- [DVO-5] **Clients report `AllReportsListed`** (source `Cli` / `Mcp`) for the no-owner list only, on success, after
  consent: K5's only instrument.
- [DVO-6] **Monitoring**: K1, K2, K4, K5 and the slice-05 hypothesis per browser / client identifier in PostHog; K3,
  K6, G2 manual; G1, G3, G4 acceptance tests. `OUT-6350-*` appended.
- [DVO-7] **Logs**: `report-bulk-run` fields fixed, no names; Warning `report-bulk-run-slow` above 30 s (create) /
  10 s (remove); `report-list-slow` and `report-creation-options-slow` above 2 s (DU-4). No alerting (self-hosted).
- [DVO-8] **CI**: no workflow change in either repository. Slice 02a's `Program.cs` registration triggers the
  live-connector suite once (connector-shared path), expected.
- [DVO-9] **Rollback** = previous release; nothing stored, reports made in bulk stay ordinary rows.
- [DVO-10] **Clients release** follows a Lighthouse release carrying slice 01; one minor changeset for the five
  packages; manual `pnpm release:version` + commit + push first. Version-gate entry optional (404 relay suffices).

LOCKED (maintainer, 2026-10-27), each the recommendation: **DVQ-1 (a)** the Reports page counts as soon as its
list shows, no dwell · **DVQ-2 (a)** normal release cadence · **DVQ-3 (a)** no `report_template` on the bulk or
combined events. Options as first put (kept for the record): DVQ-1 no dwell vs 5 s dwell; DVQ-2 normal cadence vs
whole Epic vs after 02a; DVQ-3 name-only vs carrying `report_template`.

## Infrastructure Summary

- Deployment: existing artifacts (server zips, Docker, Helm chart, Standalone Linux / Windows / macOS); calver release
  with the gated `release` job; no new strategy, no chart or config change.
- CI/CD: GitHub Actions, trunk-based on `main`; E2E (one walking skeleton) in `ci_verifysqlite` / `ci_verifypostgres`.
- Observability: Serilog lines above + opt-in usage data (browser / clients → backend → PostHog).
- Mutation testing: per-feature, ≥ 80%; Stryker.NET after 05, StrykerJS after 07, clients StrykerJS after 06.

## Constraints Established

- No event carries a count, name, id, window, value, reason or duration; each new event is accepted by `Fits` only bare.
- The FK-race test (DDD-6) runs on a relational provider, never EF InMemory.
- N = 50 is checked by a 50-report print in the Linux desktop app in slice 07; failing lowers N to 25 everywhere.
- A bulk run longer than ~30 s risks a lost answer (proxy timeouts, a 30 s shutdown drain); D8's could-not-confirm
  look is the mitigation, no other.

## Upstream Changes

DU-1 already-gone marker on removal outcomes · DU-2 Reports page dwell (DVQ-1) · DU-3 one-report combined export ·
DU-4 log fields, slow thresholds, two slow-read lines. See `upstream-changes.md`. **All APPLIED 2026-10-27.**
