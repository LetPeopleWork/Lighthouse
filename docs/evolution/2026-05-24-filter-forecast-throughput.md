<!-- DES-ENFORCEMENT : exempt -->

# Evolution Archive — filter-forecast-throughput (Finalize)

**Feature ID**: `filter-forecast-throughput`
**Epic**: ADO #4896 (https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/4896)
**Customer**: Liz / JLP at LetPeopleWork
**Waves shipped**: DISCUSS -> DESIGN -> DISTILL -> DELIVER (2026-05-20 -> 2026-05-24)
**HEAD at finalize**: `52513299` (`style(forecast): flatten DeliveryCreateModal.handleSave to clear typescript:S3776`)
**Commit range**: `34c4b0c3..52513299` (origin/main) — ~70 commits inclusive of refactor + feature delivery + CI/Sonar close-out + post-DELIVER UX iteration.
**Status**: Feature complete and shipped. All seven user stories live in production-equivalent (self-hosted) instances. All known open defects from the prior `c7824d0f` / `fd800d3d` / `b5e020fa` archive iterations are resolved. Subsequent session-level UX iteration (rename, chip removal, AND/OR mode, rules engine extension, e2e test, Quick Settings indicator, Predictability Score bug fix) is also captured below.
**Prior archive**: `docs/evolution/filter-forecast-throughput-evolution.md` (kept as the long-form pre-finalize history; this file is the close-out).

---

## Feature summary

Rule-based throughput exclusion for forecasting, premium-gated. The feature ships a filter UI inside the team's Forecast Configuration (settings) plus runtime "use filtered throughput" toggles on every throughput-derived surface — Feature Forecast (always applied, no toggle), Team Forecast (How-Many / When), Backtest, Throughput Run Chart, Throughput PBC, and the Predictability Score view. The same underlying rule engine that powers rule-based deliveries was generalised (`IRuleEvaluator<T>` + `IRuleFieldProvider<T>`) so the throughput-filter editor reuses the existing `DeliveryRuleBuilder` against a `WorkItem` field schema. Match semantics is decided at the caller (`RuleSetSemantics.MatchExcludes` for the throughput filter, `MatchIncludes` for delivery rules) and is NOT embedded in the persisted JSON.

## Business context

Customers needed a way to exclude "noise" work items — typically Bug records, recurring sprint hygiene tasks, orphaned items, items tagged `maintenance` — from the Monte Carlo throughput history so that feature forecasts model the real delivery rate against the upcoming feature backlog. Liz / JLP raised this on Epic 4896. Premium feature per pricing tier; non-premium tenants see a teaser block inside Forecast Configuration. Persisted state survives a license downgrade (the filter just becomes inactive until re-upgrade — invariant #7 / DDD-9).

## Key decisions

### Architectural (ADR-backed)

- **ADR-012 — Rule engine generalisation (hybrid)** — Shared value-objects (`WorkItemRuleSet`, `WorkItemRuleCondition`) + extracted generic port `IRuleEvaluator<T>` + per-T field providers (`FeatureFieldProvider`, `WorkItemFieldProvider`). The existing `IDeliveryRuleService` public surface is preserved as a thin delegate; existing rule-based deliveries are zero-regression. CI gate: `RuleEngineReuseCanaryTests`. Path: `docs/product/architecture/adr-012-rule-engine-generalisation.md`.
- **ADR-013 — Rule-match semantics at the caller, not in storage** — `RuleSetSemantics` enum (`MatchIncludes` / `MatchExcludes`) is passed at the application layer. Delivery rules use `MatchIncludes`; throughput filter uses `MatchExcludes`. The persisted `WorkItemRuleSet` JSON does NOT encode semantics; the same JSON can mean "include these" or "exclude these" depending on use site. Path: `docs/product/architecture/adr-013-rule-match-semantics.md`.
- **ADR-014 — Throughput chart toggle delivery splits by payload shape** — Run Chart payload is already per-item granular (`Dictionary<int, List<WorkItemBase>>`), so the toggle filters client-side and re-renders without a server round-trip. PBC payload carries only `WorkItemIds`, so the toggle requires a server-side `?view=raw|filtered` query on `/api/teamMetrics/{teamId}/throughput/pbc`. Different latency budgets justify the split. Path: `docs/product/architecture/adr-014-throughput-chart-toggle.md`.

### Domain / design invariants

- **DDD-4 — ArchUnit-enforced filter seam** — The throughput-filter step lives in `ITeamMetricsService` at exactly two methods: `GetCurrentThroughputForTeamForecast(team, mode)` and `GetBlackoutAwareThroughputForTeam(team, start, end, mode)`. An ArchUnit test (`ForecastFilterSeamArchUnitTest`) forbids any other class from invoking `IForecastFilterRuleService.Filter` directly. `ThroughputFilterMode` (default `RespectTeamSetting`) keeps the filter invisible to non-forecast callers.
- **DDD-9 — License gate on the read path** — `ForecastFilterRuleService.GetEffectiveRuleSet` is the single place that consults `ILicenseService.CanUsePremiumFeatures()`. Returns `null` on free tenants; persisted JSON is preserved verbatim across a license downgrade. The DB write path accepts the rule set regardless of license, so re-upgrade is non-destructive.
- **D1 — Charts default to Raw** — The Run Chart and PBC toggles default to `Raw` to preserve today's behaviour (no default-chart regression). The toggle is opt-in to view the filtered series.

### Late-session decisions (post-prior-archive)

- **Switched all filter toggles to a uniform `<Switch>` UI labelled `Use filtered {Throughput}`** — Previously the PBC used a segmented Raw/Filtered ToggleButtonGroup while the Run Chart used a Switch. Unified on the Switch across PBC, Run Chart, and Predictability Score for consistency. Commit `652835a6`.
- **Introduced `Models/WorkItemRules/RuleOperators.cs` as single source of truth** — Duplicated operator constant lists in `ForecastFilterRuleService.GetSchema` and `DeliveryRuleService.GetRuleSchema` had caused new operators to silently miss the delivery-rule schema. Single source closes that defect class. Commit `12e5ab94`.
- **Extended rules engine with `notContains` / `isEmpty` / `isNotEmpty` operators + group-level AND/OR `mode` field** — Backwards-compatible schema bump: `WorkItemRuleSet.mode` is optional and defaults to `"and"` when missing from stored JSON, so existing rule sets continue to evaluate identically. Commits `2fdfdba8` (engine), `e08d09ac` (UI), `ea3062bd` (schema endpoint fix).
- **Surfaced filter state in Team Quick Settings tooltip with an info-coloured icon** — Replaces an earlier per-feature warning-icon approach that the user discarded. Quick Settings tooltip now shows whether a forecast filter is active. Commit `f3a07610`.
- **Renamed "Forecast Filter" UI block to "Exclude Items for {Throughput}"** — Direct, user-language naming aligned with D8 semantics ("Exclude items where ..."). Commit `cff5eb51`.
- **Removed "Filtered throughput" chip from Forecaster, PBC, Total-Throughput widget, and feature views** — The chip was visual noise once the toggle itself became the canonical state indicator. Commits `19a29631`, `1dc3fdb3`, `c1674c5b`.
- **Surfaced save+refresh hint under the filter editor** — Inline note tells users the change takes effect after Save AND a Refresh; closes the "I changed the filter and nothing happened" support class. Commit `53e6287e`.
- **Cleared backtest result when filter toggle changes** — Prevents a stale backtest result from appearing alongside a changed toggle state. Commit `76c25876`.
- **Backtest historical + actual throughput now respect filter override** — Previously only the forward forecast respected the toggle; the backtest's historical-window throughput needed the same treatment. Commit `3c3deb77`.
- **Predictability Score bug fix — explicit `SkipFilter` on the raw branch** — `ForecastController` returning the raw predictability score was calling `GetCurrentThroughputForTeamForecast(team)` with the default `RespectTeamSetting`, which silently applied the team's configured filter — making the toggle visually active but data-identical to the filtered branch. Fix: pass `ThroughputFilterMode.SkipFilter` explicitly on the raw branch. Commit `18082ef8`.

## Steps completed

The DELIVER wave executed 29 logical steps (28 planned + one `04-03b` extension), all GREEN in `docs/feature/filter-forecast-throughput/deliver/execution-log.json` (schema 3.0). Slice structure:

- **Slice 01 — rule engine generalisation + first feature ship** (17 steps): 01-A refactor sub-phase (01-01 .. 01-05) extracts `IRuleEvaluator<T>` + `IRuleFieldProvider<T>` + `FeatureFieldProvider`, refactors `DeliveryRuleService` to delegate, lands ArchUnit/reflection enforcement, and turns the DDD-7 `RuleEngineReuseCanaryTests` GREEN — refactor commit gate. 01-B feature sub-phase (01-06 .. 01-17) lands `WorkItemFieldProvider`, real `ForecastFilterRuleService`, `ThroughputFilterMode` enum, `Team.ForecastFilterRuleSetJson` + EF migration (Sqlite + Postgres) via `CreateMigration`, schema endpoint, `TeamMetricsService` filter seam, Feature Forecast OQ-3 DTO settlement, real `ForecastFilterEditor`, `DeliveryRuleBuilder` additive props (DDD-6), settings integration with premium teaser, real `FilteredThroughputChip`, docs page text-only update.
- **Slice 02 — Team Forecast per-run toggle**: `TeamForecastForm` filter-override toggle, response carries `filterApplied` + `excludedSummary`, chip on Team Forecast result.
- **Slice 03 — Throughput charts toggle**: EF migration for `FilterApplied` + `ExcludedSummary` on `ForecastBase`, `TeamMetricsController` PBC `?view=filtered` query, TS evaluator port + operator-parity test, `ThroughputChartFilterToggle` wired into Run Chart + PBC.
- **Slice 04 — Backtest runtime override + walking skeleton**: Backtest `applyFilterOverride` plumbing + chip, `BacktestForm` toggle, walking-skeleton Playwright spec (later removed at `35017162`; rewritten in this session at `7c45f1dc`).
- **Slice 05 — mutation testing config**: backend Stryker `mutate` list extended (`52ed9d5b`), frontend Stryker harness `vitest.stryker.filter-forecast.config.ts` created (`af4893e1`).

### Post-DELIVER UX iteration (this session and prior)

Items below are NOT in the `execution-log.json` — they landed as direct commits to `main` after the DELIVER wave nominally closed, in response to the user iterating on UX and discovering defects:

1. **OD-1 close-out** — Predictability `GetEffectiveRuleSet` silent-zero-conditions bug fixed with `PropertyNameCaseInsensitive = true` on the read-path deserialise. Commit `8deba479`. Filed memory `feedback_systemtextjson_case_insensitive`.
2. **OD-2 close-out** — Chip-tooltip `excludedSummary` threaded through `ThroughputChartFilterToggle` (`498a02d9`); PBC server-side toggle wired through `MetricsService.getThroughputPbc(..., view?)` + `useMetricsData.refetchThroughputPbc(view)` + `BaseMetricsView.buildPbcNode` filterToggle slot (`7dbc16b7`).
3. **OD-3 close-out** — Walking-skeleton Playwright spec deleted (`35017162`), then rewritten end-to-end against demo data (`7c45f1dc`) once the chip/toggle wiring stabilised.
4. **Chip removal across surfaces** — Filtered-throughput chip dropped from Forecaster + PBC (`19a29631`), Total-Throughput widget (`1dc3fdb3`), and Feature views (`c1674c5b`) — the toggle itself is the canonical indicator.
5. **Rename + wording polish** — "Forecast Filter" -> "Exclude Items for {Throughput}" (`cff5eb51`); save+refresh hint surfaced under the editor (`53e6287e`); banned comments stripped + feature-shorthand constant renamed on FE (`79c5b8cc`).
6. **Backtest filter fidelity** — Toggle clears stale backtest result (`76c25876`); historical and actual throughput both respect the override (`3c3deb77`); PBC limit-line E2E assertion added (`f6f17a10`).
7. **Predictability Score raw-branch bug** — Explicit `SkipFilter` on the raw branch (`18082ef8`).
8. **Quick Settings indicator** — Filter state surfaced on Team Quick Settings tooltip (`f3a07610`).
9. **Rules engine extension** — `notContains` / `isEmpty` / `isNotEmpty` operators + group-level AND/OR mode (`2fdfdba8`), exposed in the UI (`e08d09ac`), exposed on the schema endpoints (`ea3062bd`).
10. **Single-source operators refactor** — `Models/WorkItemRules/RuleOperators.cs` becomes the SSOT; delivery rules + AND/OR plumbing migrate (`12e5ab94`).
11. **Uniform Switch UI** — PBC, Run Chart, and Predictability Score all use the same `<Switch>` labelled `Use filtered {Throughput}` (`652835a6`).
12. **E2E spec rewrite** — `Lighthouse.EndToEndTests/tests/specs/teams/ForecastFilter.spec.ts` rewritten to drive the full UX against demo data (`7c45f1dc`).
13. **SonarCloud close-out** — `style(forecast): flatten DeliveryCreateModal.handleSave` for `typescript:S3776` cognitive complexity (`52513299`); cognitive-complexity extraction on `BaseMetricsView.buildPbcNodes` (`9bb813a9`).

All commits above are on `origin/main` per trunk-based development (no feature branches per `feedback_trunk_based_development` memory).

## Lessons learned

- **Integer-rounded statistical values can collide between raw and filtered views even when inputs differ.** Average / UNPL / LNPL on the PBC and the percentile buckets on the Predictability Score round to integers; demo-data bug counts were a small enough fraction of throughput that the rounded limits matched between raw and filtered. Visible-data assertions in E2E are brittle for these surfaces. **Action**: lock such invariants at the unit / integration layer; in E2E, assert toggle-state flips and chip presence rather than re-asserting computed numbers.
- **Backwards-compatible schema bumps are free if the new field has a sensible default.** Adding the optional `mode` (`"and"` | `"or"`) field to `WorkItemRuleSet` JSON cost zero — System.Text.Json default-deserialises missing fields, and the engine reads `mode ?? "and"`. Existing stored rule sets evaluate identically. **Action**: when extending persisted JSON, prefer optional-with-default over a versioned schema bump.
- **Single-source-of-truth pattern locks out a defect class.** Operator constants were duplicated in two services; new operators silently missed the delivery-rule schema until tests on a customer instance caught it. Refactoring to `RuleOperators` as a single static class made the next miss impossible to write. **Action**: when adding a second copy of a constant list across two services, stop and extract — the second copy is the warning sign, not the third.
- **Read-path filter modes need explicit overrides, not defaults, when "skip" is a first-class branch.** The Predictability Score raw-branch bug was caused by the controller calling `GetCurrentThroughputForTeamForecast(team)` (default `RespectTeamSetting`) on what was meant to be the unfiltered branch. The default was correct for 95% of call sites but wrong for the one caller that explicitly wants raw data. **Action**: when a method has a "skip the cross-cutting concern" mode, callers that want skipping must pass it explicitly — code review should treat default-arg use on branching call sites as a smell.
- **Trunk-based development with a long UX feedback tail wants a finalize sweep that walks `git log`, not just the execution-log.** Roughly half the lasting decisions for this feature live in commits AFTER the execution-log closed (slice 05-02). The roadmap-driven view alone would have missed the rename, the rules engine extension, the AND/OR mode, the Quick Settings indicator, and the Predictability Score bug fix. **Action**: this finalize doc walks `git log --grep` to reconstruct the picture; future finalizes should do the same when work shipped across more than one session.

## Issues encountered

- **OD-1 — Silent-zero-conditions deserialisation** (pre-archive). FE writes camelCase JSON; `ForecastFilterRuleService.GetEffectiveRuleSet` called `JsonSerializer.Deserialize<WorkItemRuleSet>` without `PropertyNameCaseInsensitive = true`; `Conditions` bound to empty list; downstream guard returned `null`; toggle never rendered on the Metrics page. Fixed at `8deba479`.
- **OD-2 — PBC server-side toggle wiring gap** (pre-archive). `BaseMetricsView` didn't pass `excludedSummary` / `onServerViewChange` to `ThroughputChartFilterToggle`; the `?view=filtered` round-trip never fired. Fixed in two commits — chip-tooltip (`498a02d9`) and server-side wiring (`7dbc16b7`).
- **OD-3 — Playwright walking-skeleton flakiness** (pre-archive). Deleted at `35017162`; rewritten end-to-end at `7c45f1dc` once chip/toggle wiring stabilised.
- **Predictability Score raw-branch silently filtering** (this session). `ForecastController` raw branch was relying on a default `ThroughputFilterMode`; for teams with a configured filter, both branches returned identical data. Fixed at `18082ef8` by passing `SkipFilter` explicitly.
- **E2E integer-rounded value brittleness** (this session). PBC limit-line E2E assertion (`f6f17a10`) had to be tightened so it asserts a *difference* between raw and filtered limits, not specific numeric values, after demo-data bug-count proportions produced visually-equal rounded limits.
- **Duplicated operator lists across services** (this session). `ForecastFilterRuleService.GetSchema` and `DeliveryRuleService.GetRuleSchema` each carried their own operator-constant list; the new `notContains`/`isEmpty`/`isNotEmpty` operators landed on the throughput-filter schema but silently missed the delivery-rule schema. Caught when adding rules to a delivery-rule-using portfolio. Fixed by introducing `Models/WorkItemRules/RuleOperators.cs` as SSOT (`12e5ab94`) and surfacing on both schema endpoints (`ea3062bd`).

## Migrated permanent artifacts

| Source | Destination | Notes |
|---|---|---|
| `docs/feature/filter-forecast-throughput/feature-delta.md` | `docs/architecture/filter-forecast-throughput/feature-delta.md` | Single-narrative DISCUSS+DESIGN+DISTILL+DELIVER spec (755 lines); lasting architectural value. |
| `docs/feature/filter-forecast-throughput/slices/slice-01-rule-engine-and-feature-forecast.md` | `docs/architecture/filter-forecast-throughput/slices/slice-01-rule-engine-and-feature-forecast.md` | Slice scope / dependencies / dogfood plan. |
| `docs/feature/filter-forecast-throughput/slices/slice-02-team-forecast-toggle.md` | `docs/architecture/filter-forecast-throughput/slices/slice-02-team-forecast-toggle.md` | Slice scope / dependencies / dogfood plan. |
| `docs/feature/filter-forecast-throughput/slices/slice-03-throughput-charts-toggle.md` | `docs/architecture/filter-forecast-throughput/slices/slice-03-throughput-charts-toggle.md` | Slice scope / dependencies / dogfood plan. |
| `docs/feature/filter-forecast-throughput/slices/slice-04-backtest-runtime-override.md` | `docs/architecture/filter-forecast-throughput/slices/slice-04-backtest-runtime-override.md` | Slice scope / dependencies / dogfood plan. |

### Not migrated (already in canonical location from prior commits)

- **ADR-012, ADR-013, ADR-014** — already at `docs/product/architecture/adr-012-rule-engine-generalisation.md`, `adr-013-rule-match-semantics.md`, `adr-014-throughput-chart-toggle.md` (project's flat-ADR namespace lives under `docs/product/architecture/`, not `docs/adrs/`). Migrated at commit `50f8cfe3`.
- **Architecture brief delta** — `docs/product/architecture/brief.md` § "Application Architecture — filter-forecast-throughput" was added at `50f8cfe3` and updated at `c7824d0f`. No additional migration needed.
- **Component decomposition / data-model / technology-stack** — not produced as separate files; folded into `feature-delta.md` § "Wave: DESIGN" sections.
- **Walking skeleton / journey YAML** — not produced as separate files; the walking-skeleton Playwright spec lives at `Lighthouse.EndToEndTests/tests/specs/teams/ForecastFilter.spec.ts` (rewritten this session at `7c45f1dc`).

### Discarded (skip pattern per finalize skill)

- `docs/feature/filter-forecast-throughput/deliver/execution-log.json` — DELIVER wave audit log.
- `docs/feature/filter-forecast-throughput/deliver/roadmap.json` — DELIVER roadmap.
- `docs/feature/filter-forecast-throughput/deliver/.develop-progress.json` — session marker.
- `docs/feature/filter-forecast-throughput/HANDOFF-OD-2-PBC-SERVER-SIDE-TOGGLE.md` — single-task session handoff, OD-2 closed at `7dbc16b7`.
- `docs/feature/filter-forecast-throughput/slices/.nwave/des/logs/audit-2026-05-20.log` — gitignored nwave audit log; not in version control.

## Documentation update

Documentation update pending — defer to user-driven `/update-docs` invocation. The customer-facing docs page (`/docs/teams/edit.md`) received text-only updates at commit `34619ab4`; screenshot regeneration and any further copy updates for the late-session UX changes (rename, AND/OR mode, Quick Settings indicator, uniform Switch UI) are owned by the next `/update-docs` pass per `feedback_docs_wait_for_confirmation` memory.

## References

- Feature workspace (preserved as audit history): `docs/feature/filter-forecast-throughput/`
- Migrated architecture spec: `docs/architecture/filter-forecast-throughput/feature-delta.md`
- Migrated slice briefs: `docs/architecture/filter-forecast-throughput/slices/`
- Prior long-form archive: `docs/evolution/filter-forecast-throughput-evolution.md`
- ADRs: `docs/product/architecture/adr-012-rule-engine-generalisation.md`, `adr-013-rule-match-semantics.md`, `adr-014-throughput-chart-toggle.md`
- Architecture brief section: `docs/product/architecture/brief.md` § "Application Architecture — filter-forecast-throughput"
- ADO Epic: https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/4896
