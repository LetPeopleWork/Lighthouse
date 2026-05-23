<!-- DES-ENFORCEMENT : exempt -->

# Evolution Archive — filter-forecast-throughput

**Feature ID**: `filter-forecast-throughput`
**Epic**: ADO #4896 (https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/4896)
**Customer**: Liz / JLP at LetPeopleWork
**Waves shipped**: DISCUSS → DESIGN → DISTILL → DELIVER (2026-05-20 → 2026-05-23)
**Commit range**: `34c4b0c3..498a02d9` (origin/main) — ~40 commits inclusive of CI infra fixes, post-archive defect resolution, and the H-1 chip-tooltip wiring.
**Status**: DELIVER complete. OD-1 (toggle render) **RESOLVED**. OD-2 (BaseMetricsView prop forwarding) **partially resolved** (chip tooltip done; PBC server-side toggle deferred). OD-3 (Playwright walking skeleton) **closed** by deletion — spec to be rewritten when remaining wiring lands. One quality follow-up remains: mutation kill rate 69.17%, below the 80% gate.

> UPDATE — This archive's earlier "WARNING" section described OD-1 as under active debug. Resolved in commit `8deba479` (2026-05-23): `ForecastFilterRuleService.GetEffectiveRuleSet` was calling `JsonSerializer.Deserialize<WorkItemRuleSet>(...)` without `PropertyNameCaseInsensitive = true`. The FE writes camelCase JSON; `Conditions` therefore bound to an empty list at the read path and `GetEffectiveRuleSet` returned `null`, so the Metrics-page toggle never rendered. Fix: shared `static readonly JsonSerializerOptions { PropertyNameCaseInsensitive = true }` passed to the Deserialize call. Test: `GetEffectiveRuleSet_PremiumTenantCamelCaseJson_ReturnsDeserialisedRuleSet`. New memory filed: `feedback_systemtextjson_case_insensitive`.

---

## Outcome

### What was delivered

The seven user stories scoped in DISCUSS all ship in this delivery wave under the Premium license gate:

- **US-01** — Configure a forecast-throughput filter as a rule set (team-admin, Premium). Editor is `ForecastFilterEditor` (`Lighthouse.Frontend/src/components/Common/Forecasting/ForecastFilterEditor.tsx`), which composes the existing `DeliveryRuleBuilder` against a `WorkItem` field schema. Persisted as nullable `Team.ForecastFilterRuleSetJson` column (Sqlite + Postgres EF migration).
- **US-02** — Filter is applied automatically to all Feature Forecasts. `TeamMetricsService.GetCurrentThroughputForTeamForecast(team, ThroughputFilterMode.RespectTeamSetting)` is the single seam. `Feature.Forecast` carries `filterApplied` + `excludedSummary` so the UI can render the chip.
- **US-03** — `FilteredThroughputChip` (with rule-list tooltip) renders on Feature Forecast, Team Forecast, Backtest, and the two Throughput charts.
- **US-04** — Per-run "Apply forecast-throughput filter" toggle on the Team Forecast How-Many / When forms. Defaults ON for Premium teams with a configured filter.
- **US-05** — Per-view "Raw / Filtered" toggle on the Throughput Run Chart (client-side filter via the new TS evaluator port) and the Throughput PBC chart (server-side via `?view=filtered` on `/api/teamMetrics/{teamId}/throughput/pbc`). Defaults to **Raw** (D1 invariant — no default-chart regression).
- **US-06** — Per-run toggle on the Backtest form; chip on the Backtest result.
- **US-07** — Premium-only gating; non-premium tenants see the teaser block in Forecast Configuration. License downgrade is non-destructive — the persisted rule set is preserved on downgrade and silently no-ops in the read path (`ForecastFilterRuleService.GetEffectiveRuleSet` returns `null` on free tenants).

### What is NOT yet realised (carries forward)

- **Throughput Metrics page (Team detail → Metrics tab)** — the chip + toggle never render in the browser because the FE re-fetch of team settings returns `forecastFilterRuleSetJson: null` even after a successful PUT. Suspected EF-tracking race in the background `TeamUpdater` path. **Under active interactive debug.**
- **PBC server-side branch wiring** — `BaseMetricsView` does not yet pass `excludedSummary` or `onServerViewChange` props to `ThroughputChartFilterToggle`, so the PBC `?view=filtered` round-trip never fires. Found by the adversarial reviewer (H-1).
- **Walking-skeleton Playwright spec** — the Features-tab block is skipped because the existing `testWithUpdatedTeams` fixture does not seed a Portfolio + Feature; the spec navigates back to Overview instead of asserting the chip on a Feature Forecast.

---

## Architectural deltas

| ID | Delta | Locked by |
|---|---|---|
| DDD-1 | Rule engine generalised. Existing `DeliveryRuleSet` / `DeliveryRuleCondition` value-objects renamed to `WorkItemRule*` (use-case-agnostic). A new generic port `IRuleEvaluator<T>` + concrete `RuleEvaluator<T>` (pure function — NUnit constructor-inspection test enforces no I/O dependencies). Two `IRuleFieldProvider<T>` implementations: `FeatureFieldProvider` (existing delivery-rule use) and `WorkItemFieldProvider` (new throughput-filter use). `DeliveryRuleService` retained as a thin delegate to `RuleEvaluator<Feature>` — public surface preserved. | ADR-012; canary test `RuleEngineReuseCanaryTests` (CI hard gate); commits `0cc35c08`, `93eed0ee`, `f51319cd`, `d78a39b2`, `f099f0f0`. |
| DDD-3 | `RuleSetSemantics` enum (`MatchIncludes` / `MatchExcludes`) decided at the caller, not embedded in the persisted JSON. Delivery rules → `MatchIncludes`; throughput filter → `MatchExcludes` (D8). | ADR-013; commit `07bd2e25`. |
| DDD-4 | The throughput-filter step lives inside `ITeamMetricsService` at exactly two seams: `GetCurrentThroughputForTeamForecast(team, mode)` and `GetBlackoutAwareThroughputForTeam(team, start, end, mode)`. A new `ThroughputFilterMode` enum (default `RespectTeamSetting`) keeps the filter invisible to non-forecast callers. ArchUnit test forbids any other class from invoking `IForecastFilterRuleService.Filter` directly. | ArchUnit test (`4777d3be`); commit `e9559811`. |
| DDD-6 | `DeliveryRuleBuilder` accepts two optional props (`title`, `emptyStateMessage`) so the same component renders both the delivery-rule editor and the throughput-filter editor with the right copy. Zero behavioural change for existing callers. | Commit `16cf1dbf`. |
| DDD-9 | Premium license check lives in exactly one place: `ForecastFilterRuleService.GetEffectiveRuleSet`. Read path returns `null` on free tenants; persisted JSON is preserved (non-destructive downgrade — US-07 / invariant #7). | Commit `68769869`. |
| Chart-toggle split | Run Chart filters client-side (per-item granular payload already); PBC uses a backend `?view=raw\|filtered` query param (payload carries only `WorkItemIds`). Split is enforced by payload shape, not preference. | ADR-014; commits `21960c6a`, `f62ca177`, `cde6108d`. |

See `docs/product/architecture/brief.md` § "Application Architecture — filter-forecast-throughput" for the full table (24 components: 8 NEW, 14 EXTEND, 2 NO CHANGE) and ADR references.

---

## Test count delta

| Suite | Pre-feature | Post-feature | Delta |
|---|---|---|---|
| Backend (NUnit) | ~2,433 | 2,663+ | +230 |
| Frontend (Vitest) | ~2,873 | 2,929+ | +56 |
| Playwright walking skeleton | 1 spec (skipped) | 1 spec (active, partial — see "Open defects") | +0 net |

Stryker configurations added but not yet run:

- Backend: extended `stryker-config.json` mutate-list (commit `52ed9d5b`) covering `RuleEvaluator<T>`, `ForecastFilterRuleService`, `TeamMetricsService` filter seams, and the new controller validation.
- Frontend: `vitest.stryker.filter-forecast.config.ts` harness (commit `af4893e1`) covering `ForecastFilterEditor`, `FilteredThroughputChip`, `ThroughputChartFilterToggle`, and the TS evaluator port.

Mutation testing per CLAUDE.md (`per-feature` strategy, ≥ 80% kill rate) is **deferred** — listed under follow-ups.

---

## Open defects (post-archive resolution status)

The three items below carried forward from the original archive write at `c7824d0f`. Status updates landed in commits `8deba479`, `16eecd70`, `35017162`, and `498a02d9`:

### OD-1 — RESOLVED (commit `8deba479`, 2026-05-23)

**Original symptom**: On a Premium tenant with a saved non-empty `ForecastFilterRuleSetJson`, opening Team detail → Metrics tab did not render `FilteredThroughputChip` or `ThroughputChartFilterToggle`. The browser-side fetch of `getTeamSettings(team.id)` returned `forecastFilterRuleSetJson: null` even though the FE PUT had saved a non-null rule set.

**Actual root cause** (different from the original 5-Whys hypothesis on EF tracking races): `ForecastFilterRuleService.GetEffectiveRuleSet` called `JsonSerializer.Deserialize<WorkItemRuleSet>(team.ForecastFilterRuleSetJson)` *without* `PropertyNameCaseInsensitive = true`. The FE serialises the rule set as camelCase (`{"version":1,"conditions":[{"fieldKey":"workitem.type","operator":"equals","value":"Bug"}]}`). The C# record exposes `Conditions` in PascalCase. System.Text.Json defaults to case-sensitive matching, so `Conditions` bound to an empty list. Downstream guard `if (ruleSet == null || ruleSet.Conditions.Count == 0) return null` returned `null`, so `BaseMetricsView`'s `hasForecastFilter` derivation was always false on the Metrics page → toggle never rendered. ASP.NET Core's controller binding (used by the PUT validation path) DOES apply case-insensitivity, so the validation step and the JSON column write looked correct; only the *read* path via direct `JsonSerializer.Deserialize` was silently broken.

**Fix**: shared `private static readonly JsonSerializerOptions JsonSerializerOptions = new() { PropertyNameCaseInsensitive = true }` on `ForecastFilterRuleService`, passed to the `Deserialize<WorkItemRuleSet>` call. The DbContext-race hypothesis turned out to be wrong: the DB row was always populated; only the in-process deserialization was empty.

**Test**: `ForecastFilterRuleServiceIntegrationTest.GetEffectiveRuleSet_PremiumTenantCamelCaseJson_ReturnsDeserialisedRuleSet` — fixture-JSON in camelCase, asserts `result` is non-null.

**Memory filed for future deliveries**: `feedback_systemtextjson_case_insensitive` — any non-controller-boundary `JsonSerializer.Deserialize` on FE-originated JSON in this .NET codebase MUST pass `PropertyNameCaseInsensitive = true`. Validation passing vacuously is the silent-defect class.

**Related cosmetic fix in the same commit**: collapsed a vacuous OR-operand in `TeamMetricsService` (`mode == Respect && rs == null || rs == null` was logically equivalent to `rs == null`).

### OD-2 — PARTIALLY RESOLVED (commit `498a02d9`, 2026-05-23)

**Resolved half**: `BaseMetricsView` now passes `excludedSummary` to `ThroughputChartFilterToggle` for the Run Chart slot. New helper `formatConditions(conditions): string` in `evaluateCondition.ts` produces a human-readable summary string per the existing `FilteredThroughputChip.test.tsx` expectation (`"Type = Bug; Tags contains \"maintenance\""`). 7 Vitest cases on the helper, including per-operator, multi-condition join, additionalField rendering, case-insensitivity, and empty-array edge case.

**Deferred half**: PBC server-side toggle wiring. `BaseMetricsView`'s `buildPbcWidget` does not yet:
- Render a `<ThroughputChartFilterToggle chartKind="pbc" onServerViewChange={...}>`
- Wire `onServerViewChange` to a refetch handler on the PBC endpoint with `?view=filtered`
- Extend `useMetricsData` to accept a `view` parameter and trigger a re-fetch

A code-comment marker exists at the `buildPbcWidget` function pointing at this archive entry. This is real but bounded work — requires touching the hook and the PBC widget; estimated ~1-2h focused crafter session.

### OD-3 — CLOSED-BY-DELETION (commit `35017162`, 2026-05-23)

**Resolution**: `Lighthouse.EndToEndTests/tests/specs/teams/ForecastFilter.spec.ts` and its companion `ForecastFilter.feature` were both deleted. The spec had been red on CI for slice-end wiring gaps and the minimal "save-and-persist" variant flaked on local back-to-back runs once the test fixture teardown started racing the schema endpoint. The user's directive was to remove both files and rewrite from scratch once the remaining wiring (OD-2 PBC half) lands. Backend integration tests + Vitest unit tests for the underlying components remain green and cover the per-component behaviour at the right layer.

**Re-write plan when picked back up**: extend the `testWithUpdatedTeams` fixture to seed a Portfolio + Feature; restore the configure → Team Forecast → Backtest → Metrics chain end-to-end; run locally against `dotnet run` + premium-licensed backend per the new memory `reference_premium_license_dev_seed`.

---

## Lessons learned (5 Whys retrospective)

### L-1 — Run Playwright locally before committing the spec

The walking-skeleton Playwright spec was committed and pushed before being run against a locally-started app. The `update-notification` modal then masked every subsequent locator, requiring two follow-up commits (`1259a719`, `1fed9c3e`) just to shepherd the spec into a runnable state, and a third (`c3a42601`) to scope it down to what the fixture actually supports. **Action**: reinforce the `feedback_run_playwright_before_commit` rule in MEMORY — never commit a Playwright spec or POM locator that hasn't been driven against `pnpm dev` + backend on :5169.

### L-2 — Premium dev-license seed is institutional knowledge — capture it

The E2E spec needs a Premium license loaded into the running backend to exercise the gated UI; this is done via `POST /api/v1/license/import` with `Lighthouse.Backend.Tests/Assets/valid_not_expired_license.json`. This is **not documented anywhere** outside the test source — the next agent attempting `@premium`-tagged Playwright work will spend hours rediscovering it. **Action**: file a memory note (`nW reference`) titled "Premium license dev seed for Lighthouse" linking the asset path + the POST endpoint.

### L-3 — Server-side round-trips need an integration test, not a unit test

OD-1 is invisible to the unit-test layer because every unit test stubs `getTeamSettings` to return whatever the test author wants. The defect manifests **only** when the BG update path collides with the user-initiated PUT in the real EF context. **Action**: add a backend integration test (`Lighthouse.Backend.Tests/API/Integration/ForecastFilterRoundTripIntegrationTest.cs`) that PUTs a rule set, triggers `TeamUpdater.Update`, then re-GETs and asserts the rule set survives. Existing test infrastructure (`WebApplicationFactory` + EF InMemory) can host this.

### L-4 — Adversarial review caught a real wiring gap

Phase 5's adversarial reviewer correctly identified H-1 (`BaseMetricsView` missing prop forwarding) as a blocker. **The reviewer was right; we shipped anyway under explicit user request** to lock in the architectural deliverables before the next session. **Action**: do not pattern-match this into "reviewers can be overridden" — this was an exception with a known-open defect documented. The default remains "reviewer-blockers ship after revision."

### L-5 — EF migration parity (Sqlite + Postgres) is now muscle memory

Three EF migrations shipped in this feature (`Team.ForecastFilterRuleSetJson`, then twice on `ForecastBase` for `FilterApplied` + `ExcludedSummary`). The third was caught only when Docker startup failed — the unit suite happily passed because EF InMemory ignores migrations. **Action (already in CLAUDE.md)**: continue using `CreateMigration` PowerShell across both providers; consider adding a smoke test that boots Sqlite + Postgres in CI before declaring the migration step complete. (Already covered by `ci_verifysqlite.yml` + `ci_verifypostgres.yml` — root cause was running the gate too late in the slice.)

---

## Commit timeline

30 commits between `34c4b0c3` (prior delivery's refactor-commit boundary) and `1fed9c3e` (current HEAD on origin/main).

### Slice 01 — rule-engine generalisation + filter + chip + Feature Forecast + premium gate

- `0cc35c08` feat(forecast): WorkItemFieldProvider + ThroughputFilterMode (slice 01 wrap-up)
- `68769869` feat(forecast): ForecastFilterRuleService GetSchema + GetEffectiveRuleSet
- `07bd2e25` feat(forecast): Filter + ValidateRuleSet (exclude semantics, ADR-013)
- `a7719865` feat(team): EF migration for Team.ForecastFilterRuleSetJson (Sqlite + Postgres)
- `e9559811` feat(forecast): TeamMetricsService DDD-4 filter seam
- `8fc4f587` feat(team): forecast-filter PUT validation + GET schema endpoint
- `118be66b` feat(forecast): Feature.Forecast carries filterApplied + excludedSummary (OQ-3)
- `16cf1dbf` refactor(rules): DeliveryRuleBuilder accepts optional title + emptyStateMessage (DDD-6)
- `a672422e` feat(forecast): ForecastFilterEditor wraps DeliveryRuleBuilder
- `69e681bf` feat(forecast): embed ForecastFilterEditor in Forecast Configuration + non-premium teaser (US-07)
- `34619ab4` feat(forecast): FilteredThroughputChip + Feature Forecast wiring
- `4777d3be` feat(forecast): slice-01 end — ArchUnit enforcement

### Slice 02 — Team Forecast per-run override

- `ce8058ff` feat(forecast): Team Forecast HowMany/When override + filterApplied/excludedSummary
- `a4b7250e` feat(forecast): TeamForecastForm filter-override toggle (US-04)
- `daec2279` feat(forecast): FilteredThroughputChip on Team Forecast result

### Slice 03 — Throughput charts toggle

- `d1a419e0` fix(forecast): EF migration for FilterApplied + ExcludedSummary on ForecastBase (caught by Docker startup)
- `21960c6a` feat(forecast): TeamMetricsController PBC `?view=filtered` (ADR-014)
- `f62ca177` feat(forecast): TS evaluator port + operator-parity test
- `cde6108d` feat(forecast): ThroughputChartFilterToggle wired into Run Chart + PBC

### Slice 04 — Backtest toggle + cleanup

- `ad363f15` feat(forecast): Backtest applyFilterOverride + result chip data
- `2d6f9b12` feat(forecast): BacktestForm filter-override toggle (US-06)
- `29deb363` test(forecast): unskip walking-skeleton Playwright spec
- `4d5f3979` refactor(forecast): inline toggle+chip into ManualForecaster/BacktestForecaster + delete orphan forms + team-settings round-trip

### Slice 05 — mutation harnesses + Playwright stabilisation

- `52ed9d5b` chore(forecast): backend Stryker mutate-list extended
- `af4893e1` chore(forecast): frontend Stryker harness for filter-forecast scope
- `1259a719` test(e2e): suppress update-notification dialog in LighthouseFixture
- `c3a42601` test(e2e): walking-skeleton navigates back to Overview + skips Feature Forecast block
- `1fed9c3e` test(e2e): networkidle wait after teamEditPage.save()

### CI infra fixes (start of session)

- `3069c3b0` fix(ci): remove stray `.claude/worktrees` gitlink that broke Detect Changes job
- `a9028034` fix(ci): actually remove the gitlink from HEAD

### Post-archive defect resolution + CI close-out (after `c7824d0f`)

- `f0482d56`..`3f7d2af9` (5 commits) refactor(forecast): L1-L6 pass on slice-01-05 production code (exclusions list honoured per Phase 5 review)
- `8deba479` fix(forecast): deserialise FE-camelCase JSON in ForecastFilterRuleService — **the OD-1 resolution**
- `16eecd70` test(forecast): activate US-07 license-downgrade scaffolds — DDD-9 read-path gate
- `3fb5feda` style(ci): clear 14 SonarCloud new-code violations from slice 01-04 (CA1861 x 8, NUnit2045 x 2, S1144, S2325, CA1806, S107 frontend)
- `757de810` docs(ci-learnings): record CA1806, S2325+S1144, frontend S107 rules + CA1861/NUnit2045 recurrence counts
- `35017162` test(forecast): remove ForecastFilter Playwright spec + Gherkin companion — **the OD-3 close-by-deletion**
- `1cf172f4` / `de47c594` / `a8a968ef` (range-form, then bare) attempted `pnpm.overrides` for `qs` security advisory — reverted at `3187d37f` because both formats trip `ERR_PNPM_LOCKFILE_CONFIG_MISMATCH` in CI's `--frozen-lockfile` despite local pnpm being happy (root cause unresolved; ledgered)
- `3187d37f` revert(ci): drop pnpm.overrides — restores Verify Frontend + Package App + verifysqlite + verifypostgres green; sonar-gates remains red on the qs audit
- `3bd3d4e0` docs(ci-learnings): correct pnpm.overrides entry — root cause unresolved, rollback workaround in place
- `498a02d9` fix(forecast): thread excludedSummary into ThroughputChartFilterToggle — **the OD-2 chip-tooltip half**

---

## Follow-ups (carry into next session)

1. ~~**OD-1**~~ — RESOLVED `8deba479`.
2. **OD-2 PBC server-side half** — Wire `chartKind="pbc"` toggle into `buildPbcWidget` in `BaseMetricsView.tsx`. Extend `useMetricsData` hook with a `view` parameter; expose a refetch handler the toggle's `onServerViewChange` invokes; pass it down via the `filterToggle` slot on `ProcessBehaviourChart`. Estimated: 1-2h focused crafter session.
3. ~~**OD-3**~~ — Closed by deletion at `35017162`. When picked back up: extend `testWithUpdatedTeams` fixture to seed a Portfolio + Feature; rewrite the spec from scratch; run locally per `reference_premium_license_dev_seed` memory.
4. **Mutation testing under 80% gate** — re-ran 2026-05-23 after the JSON-case fix + scaffold activations + CA1861 hoists: **69.17%** (was 69.09%; +0.08%). Worst slice files: `TeamMetricsService` 48.2% (110 surviving mutants, the biggest opportunity), `ForecastService` 57.5%, `ForecastFilterRuleService` 69.2%, `RuleEvaluator` 73.2%, `ForecastController` 79.2%, plus four small files (`WhenForecastDto`, `AggregatedWhenForecast`, `TeamSettingDto`) below 80%. ~99 more mutants need killing to clear the gate. Estimated: 2-4h focused crafter session targeting `TeamMetricsService` + `ForecastService`.
5. **L1-L6 refactor pass** — landed for slice 01-05 production code in `f0482d56..3f7d2af9` (5 commits, 10 files). The exclusions list (the toggle-render path) was honoured — that path can be revisited now that OD-1 + the OD-2 chip-tooltip half are landed.
6. ~~**Premium license dev seed memory**~~ — filed as `reference_premium_license_dev_seed`.
7. **CI sonar-gates audit gap (qs vulnerability)** — `pnpm audit --audit-level=low` flags GHSA-q8mj-m7cp-5q26 (qs DoS, moderate, transitive via `@stryker-mutator/core`). `pnpm.overrides` workaround attempted but breaks Verify Frontend's `--frozen-lockfile` (root cause unresolved). Currently accept the audit failure; Dependabot PR #80 tracks upstream resolution.
8. **Backend integration test for the round-trip** (per L-3) — `ForecastFilterRoundTripIntegrationTest.cs` using `WebApplicationFactory` + EF Sqlite would lock OD-1 in. The current `GetEffectiveRuleSet_PremiumTenantCamelCaseJson_ReturnsDeserialisedRuleSet` unit test exercises the deserializer at the service-layer entry point but does not exercise the full PUT-then-GET round-trip.
9. **ADO sync** — Epic #4896 + child stories US-01..US-07 transition Active → Resolved. Deferred per user; outlined in the `/clean-ci` session notes for the next dispatch.
10. **Docs page screenshot regen** — `/docs/teams/edit.md` got the Forecast Filter (Premium) text-only section at commit `34619ab4`. Per `feedback_docs_wait_for_confirmation`, regen screenshots via `/update-docs` once the OD-2 PBC half lands and the chip tooltip is visually verified end-to-end.

---

## KPI baseline note

The six OUT-* entries for this feature in `docs/product/kpi-contracts.yaml` (OUT-filter-adoption, OUT-filter-forecast-shift, OUT-filter-default-chart-regression, OUT-filter-toggle-divergence, OUT-filter-rule-editor-reuse, OUT-filter-rule-engine-regression) move from "deferred-pending-telemetry-feature" to **shipped 2026-05-23, customer feedback measurement starts now**. Two are CI-gated (default-chart-regression, rule-engine-regression — both green); the remaining four are community-feedback proxies blocked on Epic 5015 (opt-in telemetry).

This evolution file is the running notes target referenced from each OUT-* entry's `surfacing` list.

---

## References

- Feature workspace (preserved): `docs/feature/filter-forecast-throughput/`
- Feature delta (single-narrative spec): `docs/feature/filter-forecast-throughput/feature-delta.md`
- Execution log: `docs/feature/filter-forecast-throughput/deliver/execution-log.json`
- Roadmap: `docs/feature/filter-forecast-throughput/deliver/roadmap.json`
- Architecture: `docs/product/architecture/brief.md` § "Application Architecture — filter-forecast-throughput"
- ADRs: ADR-012 (rule-engine generalisation), ADR-013 (rule-match semantics), ADR-014 (chart-toggle delivery mechanism)
- ADO Epic: https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/4896
