# Slice 01: Rule-engine generalisation + Forecast-Throughput Filter applied to Feature Forecasts (Premium)

**Feature**: filter-forecast-throughput
**Stories shipped**: US-01, US-02, US-03 (chip on Feature Forecast surface only), US-07
**Estimate**: ~1.5 crafter days (refactor commit + feature commit). Pushes the slice slightly past the ≤1 day taste test; recorded with a reason — the refactor (rule-engine generalisation, D7) cannot be deferred without creating duplication-of-knowledge risk.
**Reference class**: similar to existing premium-gated EXTEND features that also touch a shared service (RBAC enhancements US-08 endpoint refactor + frontend extension).

## Goal
Ship the rule-based filter end-to-end against Feature Forecasts. Validates Epic 4896 demand (Liz / JLP) AND the "rule engine reuse" architectural bet — if the existing rule-editor component lands cleanly on WorkItem fields, every downstream slice is small.

## IN scope
- **Refactor commit** (separate from feature commit, per CLAUDE.md): generalise `IDeliveryRuleService` evaluator. Recommended path (DESIGN to confirm): extract `IRuleEvaluator<T>` + `IFieldProvider<T>`; existing `IDeliveryRuleService` becomes a thin Feature-scoped delegator; tests for existing rule-based deliveries remain green with zero changes.
- New persisted JSON column on `Team`: `ForecastFilterRuleSetJson` (nullable; nullable + empty conditions list both mean "no filter").
- EF migration via the existing `CreateMigration` PowerShell script (Sqlite + Postgres).
- New service `IForecastFilterRuleService` (WorkItem-scoped) backed by the generic evaluator. Exposes `GetSchema(team) → RuleSchema` (D9 fields) and `Filter(workItems, ruleSet) → workItems` (match = exclude, per D8).
- Premium gate: `ILicenseService.CanUsePremiumFeatures()` checked at the service layer; non-premium → empty filter (no-op).
- API:
  - `PUT /api/teams/{teamId}` payload extended with `forecastFilterRuleSet` (WorkItemRuleSet-compatible JSON shape; validation: subset of D9 schema).
  - `GET /api/teams/{teamId}/forecast-filter/schema` returns the field schema for the rule editor.
  - Feature Forecast response extended with `filterApplied: bool` and `excludedSummary: string`.
- Frontend Team Settings (`Lighthouse.Frontend/src/pages/Teams/Edit/ForecastSettingsComponent.tsx`): extend the existing "Forecast Configuration" InputGroup — the one that already holds Throughput History (days) and the fixed-dates toggle — with a "Forecast Filter (Premium)" sub-section rendering the **existing** rule-editor component (the one used today for rule-based deliveries). Same component, different field schema + the `semantics: "exclude"` configuration so labels read "Exclude items where…" (per D8). Hidden if `!isPremium`; editable only by team-admin; read-only for viewers if filter is set (so they understand why their forecasts are filtered).
- Frontend Feature Forecast widget: chip "Filtered throughput" with rule-list tooltip (per US-03), rendered when forecast response has `filterApplied: true`.
- Docs page: new section on premium-feature catalogue page.

## OUT scope
- Team Forecast per-run toggle → Slice 02
- Throughput chart per-view toggle → Slice 03
- Backtest per-run toggle → Slice 04
- Portfolio-level filter → out of feature
- Persistent per-user toggle preferences → out of feature

## Learning hypothesis
**Confirms if it succeeds**: ≥3 distinct customer teams configure a non-empty rule set within 30 days of release (validates Epic 4896 demand AND rule-editor reuse — if users complain "this is confusing," the reuse hypothesis is wrong); rule-engine refactor lands with zero regressions in existing rule-based deliveries (Stryker kill rate ≥ baseline).
**Disproves if it fails**: zero adoption AND/OR customer feedback "I want dedicated controls, the rule editor is overkill for this" → revisit D7. Cheap to revert the UI; refactor commit stays since it's a quality improvement regardless.

## Acceptance criteria
See US-01, US-02, US-03, US-07 in `../feature-delta.md`. Plus the rule-engine-reuse canary (cross-cutting invariant #6) and license-downgrade non-destruction (invariant #7).

## Dependencies
None for the slice itself. Slices 02, 03, 04 all depend on Slice 01's persisted column, schema endpoint, filter service, and chip pattern.

## Production data requirement
**Required.** Smoke test against the project's own Lighthouse self-hosted instance configured against an ADO team with a mixed closed history (User Stories + Bugs + orphan items). Verify:
1. Existing rule-based deliveries (on a Portfolio with such deliveries) continue to work unchanged after the refactor.
2. A new throughput-filter rule set (e.g. `Type = Bug` OR `Parent Reference ID = (empty)`) reduces the Monte Carlo throughput sample observably.
3. Default throughput charts unchanged.

## Dogfood moment
Project's Lighthouse team configures `Type = Bug` and `Parent Reference ID = (empty)` rules on a representative team. Refreshes a known feature forecast and confirms the P85 shifts later (Bugs and orphans were inflating throughput). Confirms US-03 chip enumerates the active rules in the tooltip.

## Pre-slice spike candidates
- **SPIKE-1** (~2 hr): confirm the rule-editor component is exportable as a stand-alone composite that accepts arbitrary field schema + `semantics: "include" | "exclude"` prop. If today's component is too tightly coupled to "feature deliveries", scope of slice 01 grows — flag immediately.
- **SPIKE-2** (~1 hr): verify Monte Carlo throughput sample is sourced from a single method in `teamMetricsService.GetCurrentThroughputForTeamForecast(team)`. If there are multiple call sites that build the vector independently, the filter must wrap all of them — flag scope.
- **SPIKE-3** (~30 min): confirm `IDeliveryRuleService.GetRuleSchema(Portfolio)` works given a Team's WorkTrackingSystemConnection in the same shape — the AdditionalField pattern needs to be portable from Portfolio to Team.
