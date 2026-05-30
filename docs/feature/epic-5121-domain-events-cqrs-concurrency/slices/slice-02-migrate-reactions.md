# Slice 02: Migrate remaining add/delete reactions to domain-event handlers

**Feature**: epic-5121-domain-events-cqrs-concurrency
**ADO child**: #5099
**Story shipped**: US-5099 (`@infrastructure`)
**Estimate**: ~1–1.5 crafter days — **CARPACCIO RISK** (see split valve below)
**ADR**: D1, D3; event families A, B, C (addendum)

## Goal

Once the dispatcher seam (#5098) has earned trust, migrate the rest of the hand-wired add/delete reactions onto domain-event handlers across three families, so constructors shrink and "new reaction to add/delete" becomes "add a handler" rather than "edit a controller". **No production behaviour change** — same reactions run, just published-then-subscribed.

## IN scope

Three reaction families (ADR-027 addendum):

- **Family A — pipeline decomposition**: the full 6-step `PortfolioUpdater.Update` + symmetric `TeamUpdater.Update` become handlers on `PortfolioFeaturesRefreshed` / `TeamDataRefreshed`.
- **Family B — lifecycle events**: the delete→cleanup→re-trigger pattern across SIX delete paths (Team / Portfolio / Delivery / Connection / User / ApiKey / BlackoutPeriod) + create paths, unified onto `*Deleted` / `*Created` handlers. Because only Team+Portfolio currently call `RemoveRefreshLogsForEntity`, a unified `*Deleted` handler ALSO fixes the refresh-log-cleanup gap on the other paths. `TeamController.DeleteTeam` (9 ctor injections) is the flagship — its ctor shrinks (target ≤5).
- **Family C — cross-aggregate trigger**: `TeamDataService` directly calling `forecastUpdater.TriggerUpdate(portfolio.Id)` across module lines is replaced by a `TeamDataRefreshed` handler living in Forecasting.

## OUT scope

- Work-item events (#5122), concurrency tokens (#5100), module-boundary rules (#5101).
- New behaviour. This is a pure decoupling refactor; the refresh-log-cleanup "fix" on the extra delete paths is an emergent consequence of unifying the handler, not a new feature being designed here.

## CARPACCIO split valve

If the whole migration exceeds ~1 day, split **Family B (lifecycle/cleanup)** into its own follow-up story, shipping Family A + C first (the `PortfolioUpdater`/`TeamUpdater` pipeline decomposition + the cross-aggregate trigger), then Family B (the six delete/create paths + the flagship `DeleteTeam` ctor shrink + the refresh-log-cleanup gap fix) as a clean second slice. Family B is the most independently severable because the delete paths share a single unified-handler shape.

## Learning hypothesis

**Confirms if it succeeds**: the seam scales beyond the first reaction — multiple families (pipeline, lifecycle, cross-aggregate) migrate cleanly; `TeamController.DeleteTeam`'s ctor drops from 9 to ≤5; the unified `*Deleted` handler closes the refresh-log-cleanup gap on the four delete paths that lacked it; no behaviour regresses.
**Disproves if it fails**: either (a) the symmetric `TeamUpdater`/cross-module Forecasting handler re-introduces a cross-module dependency the module rules (#5101) would forbid (surfacing an ordering conflict — may need #5101 first for those families), or (b) decomposing the 6-step ordered pipeline into independent handlers loses a required ordering guarantee that the imperative sequence implicitly provided (would need an in-transaction/ordered tier, reopening D2's after-commit default for those steps).

## Acceptance criteria

See US-5099 in `../feature-delta.md`. Slice specifics:

- `TeamController` constructor injection count drops from 9 to ≤5; `DeleteTeam` no longer hand-wires `RemoveRefreshLogsForEntity` + the `foreach TriggerUpdate` loop — it publishes `TeamDeleted` / `TeamDataRefreshed`.
- A unified `*Deleted` handler runs `RemoveRefreshLogsForEntity` for ALL six delete paths; integration tests assert refresh logs are cleaned up on Delivery / Connection / User / ApiKey / BlackoutPeriod deletes (the previously-missing paths), not just Team/Portfolio.
- `TeamDataService` no longer calls `forecastUpdater.TriggerUpdate` directly; a `TeamDataRefreshed` handler in Forecasting performs the trigger.
- No-regression: every existing add/delete reaction produces the same observable outcome; full suite green.

## Dependencies

**Hard**: slice 01 / #5098 (the dispatcher seam) merged to main. **Soft/ordering**: if Family A/C handlers would cross a module boundary that #5101 enforces, sequence #5101 (slice 04) before those families — flagged as an open question for the orchestrator.

## Production data requirement

**Not required.** Zero behaviour change; integration tests against fixtures cover the migrated reactions and the refresh-log-cleanup gap fix. Dogfood: deleting a Team / Connection on the project's own instance leaves no orphaned refresh logs and re-triggers forecasts exactly as before.

## Carpaccio taste tests

- **Independently shippable?** YES (precursor, test-observable) — but watch the >1-day risk; use the Family-B split valve if needed.
- **One day or less?** AT RISK — ~1–1.5 days; split valve defined.
- **End-to-end?** YES per family — publish→handler→reaction→commit.
- **`@infrastructure`?** YES — labelled `@infrastructure`; ctor-shrink and gap-fix are test-observable, not user-visible.
