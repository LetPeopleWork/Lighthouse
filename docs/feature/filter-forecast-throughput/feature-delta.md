# Feature: filter-forecast-throughput

Epic 4896 (Filter Forecast Throughput) — customer ask from Liz / JLP. Premium-gated.

ADO Epic: https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/4896

## Wave: DISCUSS / [REF] Persona ID

**Primary**: `delivery-forecaster` — team lead / delivery manager / RTE who owns the WHEN / HOW MUCH conversation with leadership. New persona introduced by this feature; profile at `docs/product/personas/delivery-forecaster.yaml`.

**Secondary**: `team-admin` — existing RBAC persona, scoped to configuring the team's filter rule set.

**Tertiary**: `viewer` — sees the forecast and the chip indicating a filter is active; can flip the chart / backtest / team-forecast toggles read-only on their own view (does not mutate persisted settings); cannot edit the filter rules themselves.

## Wave: DISCUSS / [REF] JTBD one-liner

When I am forecasting feature delivery dates and the team's historical throughput is inflated by ad-hoc work that won't repeat against the upcoming feature backlog, I want to express what counts as "noise" using a flexible rule set (the same one I already use for rule-based deliveries) and have all my forecast surfaces — feature forecasts, team forecasts, backtests, and the throughput charts — let me see both the filtered and the raw view, so I can give leadership honest delivery dates and still tell the team's total-work story when needed.

Job-id: `job-forecast-throughput-tune` (in `docs/product/jobs.yaml`).

## Wave: DISCUSS / [REF] Pre-requisites

- `ILicenseService.CanUsePremiumFeatures()` exists and is wired into the backend service container.
- Existing rule engine exists at `Lighthouse.Backend/Models/WorkItemRules/` (WorkItemRuleSet / WorkItemRuleCondition / WorkItemRuleSchema / WorkItemRuleFieldDefinition) and `IDeliveryRuleService`. Currently scoped to `Feature`. **This feature requires generalisation** of the rule engine to also operate on `WorkItem` — the exact extraction approach (generic `RuleSet<T>` + field-provider, or a parallel `WorkItemRuleService` reusing the DTOs) is a DESIGN-wave decision (see D7 + handoff notes).
- `WorkItemBase` exposes Type, State, Name, ReferenceId, ParentReferenceId, Tags, plus connector-defined `AdditionalFieldValues` — these become the field set the rule editor exposes for the throughput filter.
- Existing forecast service builds the Monte Carlo throughput sample from closed `WorkItem` rows in a date window via `teamMetricsService.GetCurrentThroughputForTeamForecast(team)`. The filter wraps that sample, NOT the chart-feeding queries — except where the user explicitly flips the chart toggle (see D1).
- `useRbac()` hook + `isTeamAdmin(teamId)` gating is established.
- Existing rule-editor UI component is the one users see today on rule-based deliveries (Portfolio settings → rule-based delivery configuration). The same component must be reused for the throughput-filter editor with a WorkItem field schema instead of a Feature field schema.

## Wave: DISCUSS / [REF] Locked decisions

| ID | Decision | Verdict |
|---|---|---|
| D1 | Throughput Run Chart and Throughput PBC chart support a "Show: Filtered / Raw" toggle. The toggle is rendered ONLY when the team has a non-empty filter configured (premium tenants only). Default state: **Raw** (preserves today's behaviour; the toggle is opt-in to view the filtered series). Switching the toggle re-renders the chart from the same dataset; no extra backend call. | Locked |
| D2 | Filter is scoped per Team. Portfolio-level filter is OUT of scope. Multi-team feature forecasts use each team's own filter independently. | Locked |
| D3 | Where the toggle applies (Premium tenants with a filter configured): Throughput Run Chart, Throughput PBC, Team Forecast (How Many / When), and Backtest each render a "Apply forecast-throughput filter" toggle. **Feature Forecasts do NOT show a toggle — they always use the filter** (Epic 4896 directive). All toggle-bearing surfaces default to the same value: ON for forecast surfaces (Team Forecast, Backtest); Raw=OFF for chart surfaces (D1). Default behaviour is deliberately asymmetric: charts default to today's behaviour (Raw); forecasts default to the new opinionated behaviour (Filtered). | Locked |
| D4 | Premium-only. Filter rule editor and all toggles are hidden on non-premium tenants. Backend silently no-ops the filter if license downgrades; persisted setting is preserved on downgrade for non-destructive re-upgrade. | Locked |
| D5 | If filter excludes ALL throughput in the window, the forecast / backtest falls back to UNFILTERED throughput AND surfaces a non-blocking warning chip ("Filter excluded all throughput; showing unfiltered forecast"). On charts, "Filtered" view with zero matches simply shows an empty chart with a friendly empty-state message — no fallback (the user explicitly chose to see the filtered view). | Locked |
| D6 | Slice ordering: 01 (rule-engine generalisation + rule-based filter + chip + apply to Feature Forecasts + premium gate) → 02 (Team Forecast per-run toggle) → 03 (Throughput charts toggle) → 04 (Backtest toggle). Slices 02/03/04 are independent of each other; either order acceptable as long as 01 ships first. | Locked |
| D7 | **Reuse and extend the existing rule engine** (`WorkItemRuleSet` / `WorkItemRuleCondition` / `IDeliveryRuleService`) for the forecast-throughput filter rather than building dedicated controls (work-item-type checkboxes, orphan bool, etc.). Generalisation path is a DESIGN-wave decision: either extract a generic `RuleSet<T>` evaluator with field providers (preferred — pays off architectural debt and unlocks future rule-based surfaces) OR introduce a parallel `WorkItemFilterRuleService` that reuses the DTO model but evaluates against WorkItem fields (faster, accepts some duplication). Either way, the JSON shape of `WorkItemRuleSet { Conditions: [{FieldKey, Operator, Value}] }` is reused verbatim. | Locked |
| D8 | Semantics: a rule that MATCHES a work item means "exclude this item from forecast throughput." This is the inverse of how delivery rules work today (match = include in delivery). Rationale: users naturally think "I want to filter out bugs," not "I want to include only non-bugs." UI labelling makes the semantics explicit ("Exclude items where…"). An empty rule set means "no filter" — identical to today's behaviour. | Locked |
| D9 | Field schema exposed to the rule editor for forecast-throughput filters: `workitem.type`, `workitem.state`, `workitem.name`, `workitem.referenceid`, `workitem.parentreferenceid`, `workitem.tags`, plus connector-defined `additionalField.{id}` entries (same pattern as delivery rules). Orphan detection is expressed as `workitem.parentreferenceid equals ""` — no dedicated bool needed (this is the whole point of reusing the engine per D7). | Locked |

## Wave: DISCUSS / [REF] User stories with elevator pitches

### US-01 — Configure a forecast-throughput filter as a rule set

**Story**: As a `team-admin`, I want to configure a rule set on my team that defines which work items should NOT count toward forecast throughput, using the same rule editor I already use for rule-based deliveries, so I can express any exclusion criterion my team needs without waiting for a dedicated checkbox or toggle to be added for every new case.

**Job-id**: `job-forecast-throughput-tune`

### Elevator Pitch
Before: Lighthouse has no way to tell its forecaster "ignore these items"; if I want a flexible filter (Bug + orphans + tag:maintenance + custom-field:non-feature) my only option is an offline spreadsheet that breaks the integrated tool.
After: open `/teams/{teamId}/settings` → Forecast Filter (Premium) → click "Add rule" → field `Type` operator `equals` value `Bug` → "Add rule" → field `Parent Reference ID` operator `equals` value `(empty)` → Save → next feature forecast for this team excludes both kinds of items from the throughput sample.
Decision enabled: which kind of work fuels the forecast vs which is "noise" — using the team's own vocabulary, expressible as data.

**AC**:
- Given a premium tenant, when I open `/teams/{teamId}/settings`, the "Forecast Filter (Premium)" section is visible and contains the same rule-editor component used by rule-based deliveries.
- The rule editor is wired to a WorkItem-field schema (D9): `workitem.type`, `workitem.state`, `workitem.name`, `workitem.referenceid`, `workitem.parentreferenceid`, `workitem.tags`, `additionalField.{id}` for each connector-defined field on the team's connection. Operators: `equals`, `notEquals`, `contains` (same as delivery rules).
- The section is editable only when `useRbac().isTeamAdmin(teamId)` is true; viewers see the rules read-only (or nothing — DESIGN to confirm; default proposal: read-only display so the viewer understands why their forecasts are filtered).
- Submitting `PUT /api/teams/{teamId}` with a `forecastFilterRuleSet` body field containing a valid ruleset returns 200; refreshing shows the rules persisted.
- Validation: same constraints as `WorkItemRuleSet` — max 20 conditions, max 500-char value, only known field keys; unknown field keys return 400 with a clear error message.
- Saving with zero conditions explicitly clears the filter (treated identically to "filter not configured"); no extra "delete filter" action needed.

### US-02 — Feature Forecasts use my team's filtered throughput

**Story**: As a `delivery-forecaster`, I want every Feature Forecast that involves my team to sample from filtered throughput automatically, so I never have to remember to apply the filter manually.

**Job-id**: `job-forecast-throughput-tune`

### Elevator Pitch
Before: feature forecast P85 dates pull from full team throughput; I have to mentally subtract noise every time I quote stakeholders.
After: open any Feature involving this team → see the forecast widget percentile dates → those dates are computed from throughput with items matching the team's filter rules removed.
Decision enabled: trust the forecast for the stakeholder conversation.

**AC**:
- Given a Team with a non-empty `forecastFilterRuleSet` and a closed history of 60 items (40 User Stories, 20 Bugs) in the throughput window, when a Feature Forecast involving this team is computed and the rule set matches Bug items, then the Monte Carlo throughput sample draws only from the 40 non-Bug closes.
- The two existing throughput charts (Throughput Run Chart, Throughput PBC) by default still show all 60 items (invariant per D1; the filtered view is opt-in via the D1 toggle and only on those two surfaces).
- The forecast response includes `filterApplied: true` and a small human-readable summary like `excludedSummary: "Type=Bug; ParentReferenceID=(empty)"` (DESIGN to settle precise shape).
- When `forecastFilterRuleSet` is empty (or missing entirely), the response has `filterApplied: false` and behaviour is identical to today (no regression).

### US-03 — See that any surface is running on filtered throughput

**Story**: As a `delivery-forecaster`, I want every surface that's running on the filtered throughput to clearly show a chip saying so, so I can communicate honestly with stakeholders ("this forecast / backtest / chart view excludes Bug and orphan items").

**Job-id**: `job-forecast-throughput-tune`

### Elevator Pitch
Before: I cannot tell from the UI whether what I'm looking at is filtered or raw; risk quoting filtered percentiles as if they were raw.
After: every surface that's currently computed against the filtered series (a Feature Forecast widget, a filtered Team Forecast run, a filtered Backtest, or the throughput chart in its Filtered view) shows a small chip "Filtered throughput" with a tooltip enumerating the active rules.
Decision enabled: caveat-or-not when quoting numbers to leadership.

**AC**:
- When a surface is computed using the filter, a chip "Filtered throughput" renders adjacent to the surface's key output (percentile dates for forecasts, chart title for charts, percentile dates for backtests).
- Chip tooltip lists the active rules in a human-readable form, e.g. `Type = Bug; Parent Reference ID = (empty); Tags contains "maintenance"`.
- When the surface is computed on raw throughput (no filter or filter explicitly disabled by toggle), the chip is absent.
- For charts (D1), the chip switches in/out as the user flips the toggle — no chart reload required.

### US-04 — Choose per run whether the Team Forecast applies the filter

**Story**: As a `delivery-forecaster`, I want the Team Forecast page (How Many / When) to let me opt the filter in or out per run, so I can ask both "honest feature-delivery pace" and "total team capacity if we did everything" from the same screen.

**Job-id**: `job-forecast-throughput-tune`

### Elevator Pitch
Before: Team Forecast always uses whatever the team settings say; if I want raw I have to temporarily delete the team's rules, run the forecast, then re-add them.
After: open Team Forecast (How Many or When) → see toggle "Apply forecast-throughput filter" (defaulted ON when team has a filter) → flip it → click Forecast → result reflects that single run's choice.
Decision enabled: which conversation I'm having — feature-delivery pace or total capacity.

**AC**:
- Given a premium tenant where the Team has a non-empty filter, the Team Forecast page (`/teams/{teamId}/forecast/howmany` and `/teams/{teamId}/forecast/when`) shows the toggle, defaulted ON.
- Given a Team without a filter (empty or missing rule set), the toggle is hidden.
- Given a non-premium tenant, the toggle is hidden.
- Submitting `POST /api/forecast/team/{teamId}/howmany` (or `/when`) with `applyFilterOverride: false` returns a forecast computed from UNFILTERED throughput; with `applyFilterOverride: true` (or omitted on a premium tenant with a filter) returns the filtered forecast.
- The forecast response includes `filterApplied: bool` so US-03's chip renders correctly.

### US-05 — Choose per view whether the throughput charts show the filtered or raw series

**Story**: As a `delivery-forecaster`, I want the Throughput Run Chart and Throughput PBC to let me flip between "Show: Filtered" and "Show: Raw" on demand, so I can have either conversation ("here's what we got done in total" vs. "here's what counts toward features") from the same chart without switching pages.

**Job-id**: `job-forecast-throughput-tune`

### Elevator Pitch
Before: the throughput charts always show the team's total throughput; if I want to see "throughput against the upcoming feature backlog only" I have nothing — that view doesn't exist as a chart anywhere in Lighthouse.
After: open the Team's Metrics view → throughput chart (Run Chart or PBC) → see a toggle "Show: Raw / Filtered" in the chart header (visible because the team has a filter configured) → flip → chart re-renders client-side from the same dataset, showing only items not matched by the team's filter rules.
Decision enabled: which throughput story I'm telling in the meeting — total work delivered, or feature-bearing throughput.

**AC**:
- Given a premium tenant where the Team has a non-empty filter, the Throughput Run Chart and Throughput PBC each show a toggle in their chart header: `Show: [Raw] | [Filtered]`.
- Given a Team without a filter (or a non-premium tenant), the toggle is hidden — chart renders today's behaviour unchanged.
- Default state of the toggle is **Raw** (D1 — non-breaking).
- Flipping the toggle to Filtered re-renders the chart from the same dataset, applying the team's rule set client-side (so no extra backend round-trip is required, assuming the chart endpoint returns per-item granularity already; DESIGN to confirm — if today's endpoint only returns aggregated counts, a backend pass for filtered counts may be needed). When Filtered is active, the US-03 chip renders on the chart.
- When the rule set excludes every item in the window, the Filtered view shows a friendly empty-state message ("No items match the throughput filter in this window"); the chart's RAW view remains the user's escape hatch (D5 chart half).
- Toggle state is per-view (component-local), not persisted across page navigations — the user's intent is per-conversation, not per-session.

### US-06 — Backtest respects the filter with a per-run toggle

**Story**: As a `delivery-forecaster`, I want the Backtest tool to honour the team's forecast-throughput filter (with a per-run toggle to opt out) so my backtests model the same throughput series my forward-looking forecasts use, making the validation honest.

**Job-id**: `job-forecast-throughput-tune`

### Elevator Pitch
Before: I run a backtest to validate that Lighthouse's forecast model would have predicted what we shipped, but the backtest uses unfiltered throughput while my forward forecasts use filtered — so the validation is comparing apples to oranges.
After: open Backtest → see "Apply forecast-throughput filter" toggle (defaulted ON when team has a filter, hidden otherwise) → flip → submit → backtest result is computed against the same throughput series the forward forecast would use.
Decision enabled: trust the backtest's accuracy claim as representative of the forecast my team will actually consume.

**AC**:
- Given a premium tenant where the Team has a non-empty filter, the Backtest input form shows the toggle, defaulted ON.
- Given a Team without a filter (or a non-premium tenant), the toggle is hidden.
- `POST /api/forecast/backtest/{teamId}` accepts an optional `applyFilterOverride: bool`; when `false`, the backtest uses unfiltered historical throughput; when `true` (or omitted on a premium tenant with a filter), it uses filtered historical throughput.
- Backtest result DTO includes `filterApplied: bool` so US-03's chip renders correctly on the backtest result view.

### US-07 — Premium gate + clear messaging when premium is missing

**Story**: As a `team-admin` on a non-premium tenant, I want to know that the filter exists as a Premium capability, so I can decide whether to upgrade.

**Job-id**: `job-forecast-throughput-tune`

### Elevator Pitch
Before: on a non-premium tenant the feature is invisible; admins don't know to ask for it.
After: open `/teams/edit/{teamId}` → inside the existing "Forecast Configuration" section (the one with Throughput History days), see a single short row: "Forecast Filter is a Premium feature. Learn more." → click → docs page explains it (with screenshots of the rule editor).
Decision enabled: whether to evaluate a Premium upgrade.

**AC**:
- On a non-premium tenant, the Forecast Filter rule-editor section is replaced by a one-line teaser with a docs link.
- On a premium tenant, the teaser is absent and the full rule editor renders.
- If a premium tenant downgrades to non-premium, any persisted rule set remains on the Team row in the database (no destructive deletion) but the backend forecast service silently ignores it AND all toggles disappear from forecast / backtest / chart surfaces; re-upgrading restores filtered behaviour without re-configuration.
- Integration test asserts the downgrade → no-op → re-upgrade path preserves the persisted rule set.

## Wave: DISCUSS / [REF] Acceptance criteria (cross-cutting invariants)

Beyond the per-story ACs, these invariants apply to the whole feature and MUST be verified by integration tests:

1. **Default chart behaviour invariant** (D1): on every team — filter or no filter, premium or not — the default rendering of Throughput Run Chart and Throughput PBC is identical to today (Raw view). The opt-in toggle changes ONLY the local view, never the persisted dataset.
2. **Feature-forecast no-toggle invariant** (D3): no Feature Forecast surface anywhere in the UI exposes the "apply filter" toggle — they always use the filter when one is configured.
3. **Multi-team feature forecasts**: a feature whose teams include `[T1, T2]` where T1 has a filter and T2 does not produces a forecast whose throughput sample is `T1-filtered + T2-unfiltered`. Each team's sample applies its own rule set independently.
4. **Empty-filter no-op**: a team with an empty or missing `forecastFilterRuleSet` produces identical forecasts to today; no chips on any surface; `filterApplied: false` on every response.
5. **RBAC**: non-team-admins cannot mutate the filter rules via API; 403 on PUT. Viewers can flip view-only toggles (charts / Team Forecast run override / Backtest run override) on their own session.
6. **Rule-engine reuse** (D7): the JSON shape persisted for `forecastFilterRuleSet` is `WorkItemRuleSet`-compatible. A canary test deserialises a stored rule set with the existing `WorkItemRuleSet` JSON deserialiser to prove shape compatibility.
7. **License-downgrade non-destruction** (D4 / US-07): persisted rule set is preserved across license downgrade → no-op → re-upgrade cycle.
8. **Forecast determinism**: identical inputs (team state + rule set + seed) produce identical Monte Carlo outputs across runs.

## Wave: DISCUSS / [REF] Definition of Done

1. All 7 stories pass their ACs via integration tests (NUnit, EF InMemory, WebApplicationFactory).
2. Cross-cutting invariants 1–8 above each have at least one dedicated test.
3. Rule-engine generalisation (D7) is shipped as Slice 01's refactor commit, separate from the feature commit (per CLAUDE.md TDD/refactor discipline).
4. End-to-end behaviour verified through ONE Playwright `@premium` E2E covering the full journey: configure a rule → see chip on feature forecast → flip throughput chart toggle → flip team forecast toggle → flip backtest toggle. (Per memory `feedback_ci_and_e2e_minimalism`: thin E2E, push invariant tests into backend integration.)
5. `dotnet build` zero warnings; `pnpm build` clean; SonarCloud quality gate passes.
6. Mutation testing (Stryker.NET) for new backend code: ≥80% kill rate, including the rule-engine generalisation.
7. EF migration generated via the existing `CreateMigration` PowerShell script — Sqlite + Postgres providers both updated.
8. Docs page: new "Filter Forecast Throughput (Premium)" entry under premium-features, with screenshots of the rule editor + each toggle, regenerated via `/update-docs` (note: screenshot only after user confirms the feature works locally, per memory `feedback_docs_wait_for_confirmation`).
9. ADO Epic 4896 transitions to Resolved on slice-completion; child stories (US-01..US-07) mirrored as ADO Stories per `/ado-sync`.
10. Feature ID referenced in `docs/product/jobs.yaml` `feature_context`; persona / journey SSOT updated.

## Wave: DISCUSS / [REF] Out of scope

- Portfolio-level forecast-throughput filter (D2; potential future if customers ask).
- Persisting the throughput-chart toggle state across navigations / sessions (D1 + US-05; potential future if users complain about re-toggling).
- Filter applies to historical metrics OTHER than throughput (cycle time, work item age, predictability score) — out of scope; filter affects ONLY Monte Carlo throughput sampling AND the optional chart filtered view.
- Per-state or per-time-window rules — out of scope; throughput-window selection is unchanged.
- Cross-team rules (e.g. "team X applies team Y's filter") — out of scope.
- Non-premium tenants configure the filter — out of scope (D4).
- Replacing the entire delivery-rules code path with the generalised engine — out of scope; generalisation must support delivery rules unchanged (Slice 01's refactor preserves the public surface of `IDeliveryRuleService` for Feature-scoped use).

## Wave: DISCUSS / [REF] WS strategy

**Strategy A** — additive thin slices on a mature walking skeleton. The forecast pipeline deploys end-to-end; this feature extends it at two narrow points (rule-evaluation step on the throughput sample + UI surfaces for editor / toggles / chip). Slice 01 includes the rule-engine generalisation as a refactor-commit-then-feature-commit pair (per CLAUDE.md), keeping the walking skeleton intact at every boundary.

## Wave: DISCUSS / [REF] Driving ports

| Method | Route | Auth | Purpose | Change |
|---|---|---|---|---|
| GET | `/api/teams/{teamId}` | authenticated team-viewer | Existing team payload — extended with `forecastFilterRuleSet` (D7 JSON shape) | EXTEND |
| PUT | `/api/teams/{teamId}` | team-admin | Existing team mutation — accepts `forecastFilterRuleSet`; subset-validated against schema | EXTEND |
| GET | `/api/teams/{teamId}/forecast-filter/schema` | authenticated | NEW — returns the WorkItem field schema (D9) for the rule editor (mirrors `IDeliveryRuleService.GetRuleSchema` but for WorkItem fields) | NEW |
| POST | `/api/forecast/feature/{featureId}/run` (or equivalent) | authenticated | Existing feature forecast — response carries `filterApplied: bool`, `excludedSummary: string` | EXTEND |
| POST | `/api/forecast/team/{teamId}/howmany` and `/when` | authenticated | Existing team forecast — request accepts `applyFilterOverride: bool`; response carries `filterApplied`, `excludedSummary` | EXTEND |
| POST | `/api/forecast/backtest/{teamId}` | authenticated | Existing backtest — request accepts `applyFilterOverride: bool`; response carries `filterApplied`, `excludedSummary` | EXTEND |
| GET | `/api/teams/{teamId}/metrics` | authenticated | Throughput chart data — confirm endpoint returns per-item granularity sufficient to apply rule set client-side for the D1 toggle. If today's payload is aggregated, a new query param `?view=filtered` may be needed (DESIGN to settle). | EXTEND or NO CHANGE depending on DESIGN |

## Wave: DISCUSS / [REF] Outcome KPIs

Lighthouse customer instances do not phone home (memory `project_self_hosted_telemetry_gap`, blocked on Epic 5015). KPIs rely on community feedback proxies (community Slack, GitHub, direct customer follow-up).

| ID | KPI | Target | Measurement method |
|---|---|---|---|
| OUT-1 | Customer teams configuring a non-empty filter rule set | ≥3 distinct teams within 30 days of release | Community feedback + direct follow-up with Liz/JLP; document confirmations in `docs/evolution/filter-forecast-throughput.md` |
| OUT-2 | Forecast-shift confirmation | At least one customer reports "the percentile dates shifted in the direction I expected and now match leadership conversation" within 60 days | Community / interview note |
| OUT-3 | Default chart behaviour regressions | Zero | CI integration tests (invariant #1); zero customer regression reports |
| OUT-4 | Per-run / per-view toggle usage diverges from default | At least one user reports flipping a toggle within the same session | Community / interview note (Slices 02/03/04 learning hypothesis) |
| OUT-5 | Rule-editor reuse satisfaction | No customer feedback reporting "the editor is confusing" or "I expected dedicated controls"; rule-editor pattern is recognised as the same one used for rule-based deliveries | Community / interview note |
| OUT-6 | Rule-engine generalisation regression | Zero regressions in existing rule-based deliveries behaviour (full existing test suite green; mutation kill rate ≥ pre-feature baseline for the rule-engine code) | CI / Stryker |

If OUT-1 is zero at 30 days AND OUT-2 has no signal at 60 days: open a follow-up to evaluate deprecation or pivot the JTBD.

## Wave: DISCUSS / [REF] Definition of Ready validation

| # | DoR item | Status | Evidence |
|---|---|---|---|
| 1 | User value clearly stated | ✓ | JTBD one-liner + Job-story in `docs/product/jobs.yaml`; direct customer ask (Liz / JLP, Epic 4896) |
| 2 | Acceptance criteria testable | ✓ | Each US has concrete-input AC; 8 cross-cutting invariants enumerated |
| 3 | Dependencies identified | ✓ | Pre-requisites section enumerates licensing, rule engine, WorkItem fields, forecast service hook, useRbac, rule-editor component |
| 4 | UX considered | ✓ | Journey schema with emotional arc + shared artifacts at `docs/product/journeys/filter-forecast-throughput.yaml`; error paths enumerated |
| 5 | Technical approach feasible | ✓ | Existing rule engine + rule editor component proven on delivery rules; throughput-sample hook is a single point in `teamMetricsService.GetCurrentThroughputForTeamForecast` |
| 6 | Sizing reasonable per slice | ✓ | 4 thin slices: 01 is ~1.5 crafter days (includes refactor commit + feature commit), 02/03/04 are ~0.5 day each; total ~3 crafter days |
| 7 | Metrics/KPIs defined | ✓ | OUT-1..OUT-6 with measurement method and disprove conditions |
| 8 | Out-of-scope explicit | ✓ | Out-of-scope section enumerates portfolio analog, persistent toggle state, other metrics, full delivery-rules replacement |
| 9 | RBAC / security implications considered | ✓ | Filter mutation = team-admin-only (403 otherwise); toggles are view-only / per-run, do not require admin; license downgrade is non-destructive; rule engine value-length cap (500) and rule-count cap (20) inherited from existing WorkItemRuleSet |

DoR PASS.

## Wave: DISCUSS / [REF] Pre-handoff notes for DESIGN (Morgan)

- **Rule-engine generalisation** (D7) is the architectural decision of this feature. Recommend extracting a `RuleSet<T>` evaluator with a `FieldProvider<T>` abstraction: existing delivery rules become `RuleSet<Feature>` with the current field set; the throughput filter is `RuleSet<WorkItem>` with the D9 field set. The Service split: `IRuleEvaluator<T>` (generic), `IDeliveryRuleService` (Feature-scoped, delegates), new `IForecastFilterRuleService` (WorkItem-scoped, delegates). Refactor commits the extraction; feature commits add the new service and wiring. Alternative (parallel `WorkItemFilterRuleService` duplicating the evaluator) is faster but creates duplication-of-knowledge risk — defer the extraction and you'll pay for it again on the next rule-based surface.
- **Throughput sample hookpoint**: the filter wraps the throughput sample inside `teamMetricsService.GetCurrentThroughputForTeamForecast(team)` (or an immediate caller). Verify this method is the SINGLE source of the Monte Carlo throughput vector; if there are multiple call sites that build the vector independently, the filter must wrap all of them, or those sites must be consolidated as part of the refactor.
- **Chart toggle delivery mechanism** (D1 / US-05): determine whether today's `getThroughputPbc` endpoint returns per-item data sufficient to apply the rule set client-side. If only aggregated counts come back, the toggle either needs a `?view=filtered` query param OR a parallel endpoint. Client-side is preferred because it makes the toggle instant and avoids round-trips.
- **Semantic inversion** (D8): a MATCH means EXCLUDE for throughput rules; in delivery rules a MATCH means INCLUDE. Make the inversion explicit in the UI labelling ("Exclude items where…"). Suggest the rule editor accept an optional `semantics: "include" | "exclude"` prop so the same component renders the right label for each use case.
- **Empty-filter equivalence**: persisting zero conditions MUST be treated identically to not persisting any rule set at all. Make this explicit in the type / nullable check at the service-layer entry point.
- **Backtest field**: confirm `BacktestInputDto` is the right DTO to extend with `applyFilterOverride`; if not, find the equivalent.

## Wave: DISCUSS / [REF] Wave-decisions summary

```markdown
# DISCUSS Decisions — filter-forecast-throughput

## Key Decisions
- [D1] Throughput charts get a per-view Filtered/Raw toggle, default Raw, visible only when filter configured.
- [D2] Per-Team scope only; portfolio analog out of scope.
- [D3] Toggle applies to Team Forecast, Backtest, and throughput charts. Feature Forecasts always use the filter (no toggle).
- [D4] Premium-only; non-destructive on license downgrade.
- [D5] Forecast surfaces fall back to unfiltered on empty-filtered-sample with warning; chart Filtered view shows empty-state instead.
- [D6] Slice order 01 → 02, 03, 04 (02/03/04 independent of each other).
- [D7] Reuse + generalise the existing WorkItemRuleSet rule engine for the throughput filter (refactor commit + feature commit).
- [D8] Rule-match semantics: match = EXCLUDE for throughput rules (inverse of delivery rules; UI labelling makes it explicit).
- [D9] WorkItem field schema for the editor: type, state, name, referenceId, parentReferenceId, tags, additionalField.{id}.

## Requirements Summary
- Primary need: team-admin configures a flexible rule-based filter; delivery-forecaster sees honest forecasts and chooses raw / filtered per chart view or per forecast run.
- Walking skeleton scope: N/A (Strategy A — additive on mature pipeline).
- Feature type: user-facing (Premium-gated team config + forecast/chart/backtest UI annotation).

## Constraints Established
- Premium-only (D4).
- Default chart behaviour unchanged (D1).
- Feature Forecasts always filtered (D3).
- Per-Team scope (D2).
- Empty-filter fallback / empty-state (D5).
- Reuse and generalise existing rule engine (D7).
- Match = exclude semantics (D8).
- Self-hosted telemetry gap → KPIs are community-feedback-based.

## Upstream Changes
- None. DISCOVER artifacts do not exist for this feature; JTBD evidence is the direct customer ask documented in Epic 4896 description.

## Changed Assumptions (from prior DISCUSS iteration on this feature)
> Original DISCUSS draft (this feature, earlier turn): "D1 — Filter affects forecast throughput only; throughput chart stays raw" and "US-01 / US-04 use dedicated controls for work-item-type exclusion and orphan-item bool."

New assumption: throughput charts and backtest gain per-view toggles (D1 generalised, D3 expanded). Dedicated controls (work-item-type checkboxes, orphan bool) are replaced by reuse of the existing `WorkItemRuleSet` rule engine (D7 + D8 + D9). Rationale: user feedback during DISCUSS — toggle on charts/backtest provides parity between the conversations users actually have, and rule-engine reuse unlocks orthogonal filter dimensions (tags, custom fields) without per-dimension UI changes.
```

## Wave: DISCUSS / [REF] Handoff to DESIGN

**Handoff to**: `nw-solution-architect` (DESIGN wave) — full artifact set.
**Handoff to**: `nw-platform-architect` (DEVOPS wave) — KPIs only (this `feature-delta.md` `Outcome KPIs` section). KPIs are community-feedback proxies (telemetry gap), DEVOPS observability for this feature is minimal — no new dashboards required.

Handoff accepted by Morgan (nw-solution-architect) on entry to DESIGN wave.

---

## Wave: DESIGN / [REF] Decisions

| ID | Decision | One-line rationale |
|---|---|---|
| DDD-1 | Hybrid generalisation: reuse `WorkItemRuleSet` / `WorkItemRuleCondition` / `WorkItemRuleFieldDefinition` / `WorkItemRuleSchema` value-objects verbatim; extract `IRuleEvaluator<T>` + `IRuleFieldProvider<T>` abstractions; existing `DeliveryRuleService` keeps its public surface (delegates internally to `RuleEvaluator<Feature>`); new `ForecastFilterRuleService` is the WorkItem-scoped delegator. ADR-012. | Pays the architectural debt without touching delivery-rules JSON shape or public API; unlocks future rule-based surfaces cheaply. Option C (parallel duplication) rejected (duplication-of-knowledge risk on next surface); pure Option A rejected (mutates the proven delivery-rules public surface unnecessarily). |
| DDD-2 | `RuleSetSemantics` enum (`Include` / `Exclude`) passed by the caller at the application layer, NOT embedded in `WorkItemRuleSet` JSON. The evaluator returns matched items; the caller decides include vs exclude. ADR-013. | Preserves D7's JSON-shape invariant (the persisted blob is identical to a delivery-rules blob); semantics is a property of *use*, not of storage. |
| DDD-3 | `Team.ForecastFilterRuleSetJson` (nullable string column, JSON-serialised `WorkItemRuleSet`). Mirrors `Delivery.RuleDefinitionJson`'s precedent. EF migration generated via the existing `CreateMigration` PowerShell script (Sqlite + Postgres). Null / empty / zero-conditions all map to "no filter" at the service-layer entry. | Consistency with the only other rule-set persistence in the codebase; no schema branch on additionalField IDs (foreign-key cascades stay simple). |
| DDD-4 | Filter applied INSIDE `ITeamMetricsService` at the two seams that produce the Monte Carlo throughput vector: `GetCurrentThroughputForTeamForecast(team, ThroughputFilterMode mode)` (used by `ForecastService.InitializeThroughputPerTeam` and `ForecastController.RunManualForecastAsync`) AND `GetBlackoutAwareThroughputForTeam(team, start, end, ThroughputFilterMode mode)` (used by `ForecastController.RunBacktest`). New optional second parameter; default `ThroughputFilterMode.RespectTeamSetting` preserves today's behaviour for all non-forecast call sites. | Single seam owns the filter step; the filter is invisible to non-forecast callers (chart endpoints, predictability, cycle-time — all unchanged). Caller-side wrapping rejected because two call sites would diverge. |
| DDD-5 | Throughput **Run Chart**: client-side filter (the existing `GET /teams/{teamId}/metrics/throughput` returns `RunChartData.WorkItemsPerUnitOfTime: Dictionary<int, List<WorkItemBase>>` — already per-item granular with Type/State/Tags/ParentReferenceId/AdditionalFieldValues). Throughput **PBC**: backend `?view=raw\|filtered` query parameter on `GET /teams/{teamId}/metrics/throughput/pbc` (PBC payload is `ProcessBehaviourChartDataPoint{ XValue, YValue, WorkItemIds: int[] }` — only IDs, no rule-evaluable shape). Default `view=raw` preserves today's behaviour. ADR-014. | Verified by reading `RunChartData.cs` and `ProcessBehaviourChart.cs`. Splits the toggle delivery on a structural property of each endpoint's payload — not an arbitrary preference. |
| DDD-6 | Reuse existing `DeliveryRuleBuilder` component (`Lighthouse.Frontend/src/components/Common/DeliveryRuleBuilder/DeliveryRuleBuilder.tsx`) with a minimal additive refactor: two new optional props (`title?: string` defaulting to "Define Rules (all conditions must match)" and `emptyStateMessage?: string` defaulting to today's "Add at least one rule to define which features to include.") so the throughput-filter editor can pass `title="Exclude items where…"` and `emptyStateMessage="Add at least one rule to exclude work items from forecast throughput."` for D8 semantics. No structural change, no new component. Refactor lands in Slice 01's refactor commit. | Component is 251 lines and already stateless; the two hard-coded strings are the only barrier to reuse. Zero risk to existing rule-based deliveries. |
| DDD-7 | Canary test for invariant #6: parameterised NUnit test at `Lighthouse.Backend/Lighthouse.Backend.Tests/Models/WorkItemRules/RuleEngineReuseCanaryTests.cs`. Each parameter is a representative rule-set JSON (Type=Bug; Tags-contains-maintenance; ParentReferenceId-equals-empty; multi-rule combinations; oversize at MaxRules+1; oversize at MaxValueLength+1; additionalField.{id}-equals-X). For each: (a) `JsonSerializer.Deserialize<WorkItemRuleSet>` against the same call delivery rules use; (b) validate against the `RuleSchema` returned by `IForecastFilterRuleService.GetSchema(team)` using the new evaluator's `IsValid(ruleSet, schema)` API; (c) assert the same fail/pass verdict as the existing delivery-rules validator on a delivery-context schema (positive cases pass, oversize cases fail). | One test class makes the "shape compatibility" invariant a CI gate; failing the canary means the generalisation has drifted and must be remediated before merge. |
| DDD-8 | Empty / null / zero-conditions equivalence is enforced at the **service-layer entry point** (`ForecastFilterRuleService.GetEffectiveRuleSet(team) → WorkItemRuleSet?` returns `null` for all three cases; downstream code checks `null` once and treats it as "no filter"). | Removes "null vs empty" branching from every caller; single point of normalisation. |
| DDD-9 | Premium gate is enforced at the **service layer** (`ForecastFilterRuleService.GetEffectiveRuleSet` returns `null` when `ILicenseService.CanUsePremiumFeatures() == false`, regardless of the persisted column). API-layer `LicenseGuardAttribute` is NOT applied — the column accepts writes on any tenant so US-07's "non-destructive on downgrade" invariant is preserved; the read path is the gate. UI hides the editor / toggles based on `useLicense()`. | Single point of license enforcement; persisted state preserved across downgrade → no-op → re-upgrade (US-07 invariant #7). |

---

## Wave: DESIGN / [REF] Component decomposition

| Component | File | Change | Why |
|---|---|---|---|
| `WorkItemRuleSet`, `WorkItemRuleCondition`, `WorkItemRuleFieldDefinition`, `WorkItemRuleSchema` | `Lighthouse.Backend/Lighthouse.Backend/Models/WorkItemRules/*.cs` | NO CHANGE | Value-objects reused verbatim (D7 invariant). |
| `IRuleEvaluator<T>` | `Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/WorkItemRules/IRuleEvaluator.cs` | NEW | Generic evaluator interface: `IEnumerable<T> Match(WorkItemRuleSet, IEnumerable<T>, IRuleFieldProvider<T>)` + `bool IsValid(WorkItemRuleSet, WorkItemRuleSchema)`. Pure function, no state. |
| `RuleEvaluator<T>` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DeliveryRules/RuleEvaluator.cs` | NEW | Implementation extracted from `DeliveryRuleService` (`FeatureMatchesAllConditions`, `EvaluateCondition`, `EvaluateTagsCondition`, `RuleSetHasError` move here, generalised). |
| `IRuleFieldProvider<T>` | `Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/WorkItemRules/IRuleFieldProvider.cs` | NEW | `string GetFieldValue(T item, string fieldKey)` + `List<string> GetTagsForField(T item, string fieldKey)` + `IReadOnlyList<WorkItemRuleFieldDefinition> GetFixedFields()`. |
| `FeatureFieldProvider` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DeliveryRules/FeatureFieldProvider.cs` | NEW | Implements `IRuleFieldProvider<Feature>`; receives the field-key constants (`feature.type`, `feature.state`, …) extracted from `DeliveryRuleService`. |
| `WorkItemFieldProvider` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DeliveryRules/WorkItemFieldProvider.cs` | NEW | Implements `IRuleFieldProvider<WorkItem>`; field keys `workitem.type` / `workitem.state` / `workitem.name` / `workitem.referenceid` / `workitem.parentreferenceid` / `workitem.tags` / `additionalField.{id}` per D9. |
| `DeliveryRuleService` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/DeliveryRuleService.cs` | EXTEND (refactor) | Public surface (`GetRuleSchema(Portfolio)`, `GetMatchingFeaturesForRuleset`, `RecomputeRuleBasedDeliveries`) unchanged. Internals now delegate to `RuleEvaluator<Feature>` with `FeatureFieldProvider`. Slice 01 refactor commit. |
| `IDeliveryRuleService` | `Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IDeliveryRuleService.cs` | NO CHANGE | Public interface unchanged. |
| `IForecastFilterRuleService` | `Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Forecast/IForecastFilterRuleService.cs` | NEW | `WorkItemRuleSchema GetSchema(Team team)`; `WorkItemRuleSet? GetEffectiveRuleSet(Team team)` (returns null on free tenant, no rule set, or zero conditions — DDD-8 + DDD-9); `IEnumerable<WorkItem> Filter(IEnumerable<WorkItem> items, WorkItemRuleSet ruleSet)` (D8 semantics: matched items are EXCLUDED — exclusion is built into this method, NOT into the generic evaluator per DDD-2); `bool ValidateRuleSet(WorkItemRuleSet, Team) → bool`. |
| `ForecastFilterRuleService` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/ForecastFilterRuleService.cs` | NEW | Implementation; depends on `IRuleEvaluator<WorkItem>` + `WorkItemFieldProvider` + `ILicenseService`. |
| `ITeamMetricsService` | `Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/ITeamMetricsService.cs` | EXTEND | Add optional `ThroughputFilterMode mode = ThroughputFilterMode.RespectTeamSetting` parameter to `GetCurrentThroughputForTeamForecast` and `GetBlackoutAwareThroughputForTeam` (DDD-4). |
| `TeamMetricsService` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/TeamMetricsService.cs` | EXTEND | Inject `IForecastFilterRuleService`; apply `Filter` step on the closed-items source list when `mode == ApplyFilter` OR `mode == RespectTeamSetting && effectiveRuleSet != null`. Cache key includes the mode. |
| `ThroughputFilterMode` | `Lighthouse.Backend/Lighthouse.Backend/Models/Metrics/ThroughputFilterMode.cs` | NEW | `enum { RespectTeamSetting, ApplyFilter, SkipFilter }`. |
| `Team` | `Lighthouse.Backend/Lighthouse.Backend/Models/Team.cs` | EXTEND | Add `string? ForecastFilterRuleSetJson { get; set; }` property. |
| `TeamSettingDto` | `Lighthouse.Backend/Lighthouse.Backend/API/DTO/TeamSettingDto.cs` | EXTEND | Add `string? ForecastFilterRuleSetJson { get; set; }` (round-trips the JSON blob through the existing `PUT /api/teams/{teamId}` endpoint). |
| `Team.SyncTeamWithTeamSettings` | (extension method) | EXTEND | Copy `ForecastFilterRuleSetJson` from DTO into entity during update. |
| EF migration `AddForecastFilterRuleSetJsonToTeam` | `Lighthouse.Backend/Lighthouse.Backend/Migrations/Sqlite/<timestamp>_AddForecastFilterRuleSetJsonToTeam.cs` + `…/Postgres/…` | NEW | Adds nullable `ForecastFilterRuleSetJson` column on `Teams`. Generated via the existing `CreateMigration` PowerShell script. |
| `TeamController` | `Lighthouse.Backend/Lighthouse.Backend/API/TeamController.cs` | EXTEND | `UpdateTeam` accepts the new DTO field (already via existing `TeamSettingDto` flow); validates by calling `IForecastFilterRuleService.ValidateRuleSet` when the JSON is non-null/non-empty and returns 400 on unknown field key or oversize. |
| `TeamMetricsController` | `Lighthouse.Backend/Lighthouse.Backend/API/TeamMetricsController.cs` | EXTEND | Add `[FromQuery] string? view = null` to `GetThroughputProcessBehaviourChart`; when `view == "filtered"`, pass `ThroughputFilterMode.ApplyFilter` through to the service. `GetThroughput` is unchanged (client-side filter per DDD-5). |
| `TeamMetricsController.ForecastFilterSchema` | `Lighthouse.Backend/Lighthouse.Backend/API/TeamController.cs` | NEW endpoint | `GET /api/teams/{teamId}/forecast-filter/schema → WorkItemRuleSchema` returning `IForecastFilterRuleService.GetSchema(team)`. Authorisation: `[RbacGuard(TeamRead)]` (viewers can fetch the schema so the read-only editor renders correctly for them). |
| `ForecastController` | `Lighthouse.Backend/Lighthouse.Backend/API/ForecastController.cs` | EXTEND | `RunManualForecastAsync` and `RunBacktest` accept `applyFilterOverride: bool?` (added to `ManualForecastInputDto` and `BacktestInputDto`); call `GetCurrentThroughputForTeamForecast` / `GetBlackoutAwareThroughputForTeam` with `ThroughputFilterMode.SkipFilter` when override == false. Response DTOs (`ManualForecastDto`, `BacktestResultDto`) gain `FilterApplied: bool` + `ExcludedSummary: string?` (DDD via existing DTO extension). |
| `ForecastService` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Forecast/ForecastService.cs` | EXTEND | `InitializeThroughputPerTeam` already calls `GetCurrentThroughputForTeamForecast(team)` (DDD-4 seam) — no signature change here; the default `mode = RespectTeamSetting` makes feature forecasts pick up the filter automatically (US-02 / D3). |
| `BacktestInputDto` | `Lighthouse.Backend/Lighthouse.Backend/API/DTO/BacktestInputDto.cs` | EXTEND | Add `bool? ApplyFilterOverride { get; set; }`. |
| `ManualForecastInputDto` | `Lighthouse.Backend/Lighthouse.Backend/API/ForecastController.cs` (inner class) | EXTEND | Add `bool? ApplyFilterOverride { get; set; }`. |
| `BacktestResultDto` | `Lighthouse.Backend/Lighthouse.Backend/API/DTO/BacktestResultDto.cs` | EXTEND | Add `bool FilterApplied`, `string? ExcludedSummary`. |
| `ManualForecastDto` | (existing manual forecast DTO) | EXTEND | Add `bool FilterApplied`, `string? ExcludedSummary`. |
| `DeliveryRuleBuilder` | `Lighthouse.Frontend/src/components/Common/DeliveryRuleBuilder/DeliveryRuleBuilder.tsx` | EXTEND | Add optional `title?: string` and `emptyStateMessage?: string` props (DDD-6); default to today's strings. No structural change. |
| `ForecastFilterEditor` | `Lighthouse.Frontend/src/components/Teams/ForecastFilterEditor/ForecastFilterEditor.tsx` | NEW | Thin wrapper around `DeliveryRuleBuilder` configured with the WorkItem field schema (fetched from the new schema endpoint), `title="Exclude items where…"`, `emptyStateMessage="Add at least one rule to exclude work items from forecast throughput."`. Read-only when `!isTeamAdmin(teamId)`. |
| `ForecastSettingsComponent` (existing "Forecast Configuration" InputGroup on the team Edit page) | `Lighthouse.Frontend/src/pages/Teams/Edit/ForecastSettingsComponent.tsx` (existing) | EXTEND | Render `ForecastFilterEditor` **inside the existing "Forecast Configuration" InputGroup**, directly below the `throughputHistory` / fixed-dates fields. Gated by `useLicense().isPremium`; non-premium variant shows the teaser per US-07. The InputGroup title stays "Forecast Configuration"; the editor introduces a sub-heading "Forecast Filter (Premium)" within it. Page is composed via `Lighthouse.Frontend/src/components/Common/Team/ModifyTeamSettings.tsx`; route is `/teams/edit/:id` (with redirect from `/teams/:id/settings`). |
| `FilteredThroughputChip` | `Lighthouse.Frontend/src/components/Common/Forecasting/FilteredThroughputChip.tsx` | NEW | Displays "Filtered throughput" chip with rule-list tooltip; consumed by Feature Forecast widget, Team Forecast result, Backtest result, throughput Run Chart, and throughput PBC chart (US-03). |
| Throughput Run Chart frontend widget | `Lighthouse.Frontend/src/components/Common/Charts/*Throughput*` (existing — to be located in DELIVER) | EXTEND | Add `Show: Raw \| Filtered` header toggle when `team.forecastFilterRuleSet != null` AND `isPremium`; client-side filter applies the rule set via a TypeScript port of `WorkItemFieldProvider` + `RuleEvaluator` minimal subset (DDD-5). |
| Throughput PBC chart frontend widget | (existing) | EXTEND | Add `Show: Raw \| Filtered` header toggle; flipping issues a network round-trip with `?view=filtered` (DDD-5). |
| Team Forecast page (How Many / When) | (existing) | EXTEND | Add `applyFilterOverride: boolean` form state; default `true` on premium tenant with filter; pass through to `forecastService` (US-04). |
| Backtest input form | (existing) | EXTEND | Add `applyFilterOverride: boolean` form state; same default semantics as Team Forecast (US-06). |
| `RuleEngineReuseCanaryTests` | `Lighthouse.Backend/Lighthouse.Backend.Tests/Models/WorkItemRules/RuleEngineReuseCanaryTests.cs` | NEW | DDD-7. |

---

## Wave: DESIGN / [REF] Driving ports (extends DISCUSS-table)

| Method | Route | Auth | Purpose | Change |
|---|---|---|---|---|
| PUT | `/api/team/{teamId}` | `[RbacGuard(TeamWrite)]` | Existing — TeamSettingDto extended with `forecastFilterRuleSetJson` (string, nullable, JSON-encoded `WorkItemRuleSet`); validation calls `IForecastFilterRuleService.ValidateRuleSet`. | EXTEND |
| GET | `/api/team/{teamId}/forecast-filter/schema` | `[RbacGuard(TeamRead)]` | NEW — returns `WorkItemRuleSchema` (D9 WorkItem field schema). Reused by the rule-editor on the team settings page. | NEW |
| POST | `/api/forecast/manual/{id}` | `[RbacGuard(TeamRead)]` | Existing — request gains optional `applyFilterOverride: bool?`; response gains `filterApplied: bool` + `excludedSummary: string?`. **Note**: Feature Forecasts (D3) do not surface `applyFilterOverride` in the UI — but the input DTO carries it for symmetry; the FE simply never sends it for Feature Forecast surfaces. | EXTEND |
| POST | `/api/forecast/backtest/{teamId}` | `[RbacGuard(TeamRead)]` | Existing — `BacktestInputDto` gains `applyFilterOverride: bool?`; `BacktestResultDto` gains `filterApplied` + `excludedSummary`. | EXTEND |
| GET | `/api/teamMetrics/{teamId}/throughput` | `[RbacGuard(TeamRead)]` | Existing — UNCHANGED. Per-item granularity already present (`RunChartData.WorkItemsPerUnitOfTime` carries `WorkItemBase` per day); client filters in-browser (DDD-5). | NO CHANGE |
| GET | `/api/teamMetrics/{teamId}/throughput/pbc` | `[RbacGuard(TeamRead)]` | Existing — gains `?view=raw\|filtered` query param (default `raw`). `filtered` triggers `ThroughputFilterMode.ApplyFilter` server-side (DDD-5). | EXTEND |

---

## Wave: DESIGN / [REF] Driven ports

| Port | Adapter | Technology | Purpose |
|---|---|---|---|
| `IRuleEvaluator<T>` (new inbound port for `DeliveryRuleService` + `ForecastFilterRuleService`) | `RuleEvaluator<T>` | C# generic class | Pure function: evaluate a `WorkItemRuleSet` against a collection. No I/O. |
| `IRuleFieldProvider<T>` (companion port) | `FeatureFieldProvider`, `WorkItemFieldProvider` | C# class | Maps field keys to typed field accessors. No I/O. |
| Forecast filter persistence | `LighthouseAppContext` | EF Core 8, Sqlite / Postgres | Stores `Team.ForecastFilterRuleSetJson` column. Reused — no new DbContext. |
| Premium license gate | `LicenseService` (implements existing `ILicenseService`) | C# — existing | `CanUsePremiumFeatures()` consulted by `ForecastFilterRuleService.GetEffectiveRuleSet` (DDD-9). |
| Throughput vector source | `ITeamMetricsService` | C# — existing, extended | Single seam where the filter is applied (DDD-4). |

No new technology dependencies introduced.

---

## Wave: DESIGN / [REF] Technology choices

| Component | Technology | Version | Status |
|---|---|---|---|
| Backend | C# .NET 8 ASP.NET Core | 8.x | Already locked (CLAUDE.md) |
| ORM | EF Core | 8.x | Already locked |
| Backend tests | NUnit 4.6 + Moq + Microsoft.EntityFrameworkCore.InMemory + WebApplicationFactory | — | Already locked (per `project_test_stack` memory; CLAUDE.md aligned) |
| Backend mutation | Stryker.NET, ≥80% kill rate target | — | Already locked |
| Frontend | React 18 + TypeScript 5.x | — | Already locked |
| Frontend tests | Vitest + React Testing Library | — | Already locked |
| E2E | Playwright | — | Already locked |
| Migration tooling | Existing `CreateMigration` PowerShell script (Sqlite + Postgres providers in lockstep) | — | Already locked (per CLAUDE.md backend conventions) |

No new technology choices. No proprietary additions.

---

## Wave: DESIGN / [REF] Reuse Analysis

Every component listed in the decomposition table is justified as EXTEND or NEW below.

| Existing Component | File | Overlap | Decision | Justification |
|---|---|---|---|---|
| `WorkItemRuleSet` family of value-objects | `Models/WorkItemRules/*.cs` | Persisted JSON shape for ALL rule-based features | NO CHANGE | D7 invariant: the persisted JSON shape on `Team.ForecastFilterRuleSetJson` must be `WorkItemRuleSet`-deserialisable. The canary test (DDD-7) makes this a CI gate. |
| `DeliveryRuleService` | `Services/Implementation/DeliveryRuleService.cs` | Feature-scoped rule evaluation + portfolio schema | EXTEND (refactor only) | Public interface unchanged (zero impact on rule-based deliveries callers); internals delegate to the new generic evaluator. Refactor commit separate from feature commit (CLAUDE.md TDD discipline). |
| `IDeliveryRuleService` | `Services/Interfaces/IDeliveryRuleService.cs` | Feature-scoped rule service port | NO CHANGE | Stable public surface; zero risk to consumers. |
| `RuleEvaluator<T>` + `IRuleFieldProvider<T>` | (new) | Generalised evaluator | NEW | No existing generic abstraction; required by DDD-1 to share evaluator logic across `Feature` and `WorkItem` typed evaluations. |
| `FeatureFieldProvider` | (new) | Field accessor for `Feature` | NEW | Extracted from `DeliveryRuleService.GetFieldValue` private method. |
| `WorkItemFieldProvider` | (new) | Field accessor for `WorkItem` (D9 schema) | NEW | Symmetric counterpart; no existing WorkItem-field accessor in the codebase. |
| `IForecastFilterRuleService` / `ForecastFilterRuleService` | (new) | WorkItem-scoped rule service for throughput filter | NEW | No existing WorkItem-scoped rule service; DDD-1 requires a separate inbound port symmetric to `IDeliveryRuleService`. |
| `Team` entity | `Models/Team.cs` | Team aggregate root | EXTEND | New nullable JSON column — additive, no risk to existing properties. |
| `TeamSettingDto` | `API/DTO/TeamSettingDto.cs` | Team settings DTO | EXTEND | Round-trips the new field through the existing PUT endpoint — single field added; no new DTO. |
| `TeamController` | `API/TeamController.cs` | Team CRUD controller | EXTEND | Adds the new schema endpoint + validates the new DTO field. No new controller. |
| `TeamMetricsController` | `API/TeamMetricsController.cs` | Team metrics endpoints | EXTEND | Adds `?view` query param to the PBC endpoint. Run chart endpoint unchanged. No new controller. |
| `ForecastController` | `API/ForecastController.cs` | Forecast + backtest endpoints | EXTEND | Adds the override field to existing DTOs; no new endpoints (other than the schema endpoint which logically belongs on `TeamController`). |
| `ITeamMetricsService` / `TeamMetricsService` | `Services/{Interfaces,Implementation}/TeamMetricsService.cs` | Throughput vector producer | EXTEND | Single seam owns the filter (DDD-4); adding two optional parameters is additive. |
| `ILicenseService` | `Services/Interfaces/Licensing/ILicenseService.cs` | License gate | NO CHANGE | `CanUsePremiumFeatures()` already exists. |
| `DeliveryRuleBuilder` (frontend) | `Lighthouse.Frontend/src/components/Common/DeliveryRuleBuilder/DeliveryRuleBuilder.tsx` | Rule editor UI | EXTEND | Two optional props added (DDD-6); zero structural change; existing delivery-rules consumers see no behavioural difference (defaults preserve today's strings). |
| `ForecastFilterEditor` (frontend) | (new) | Throughput-filter editor wrapper | NEW | Thin wrapper composing `DeliveryRuleBuilder` with the WorkItem schema + exclude semantics — the per-page integration point for the new section. |
| `FilteredThroughputChip` (frontend) | (new) | "Filtered throughput" chip + rule-list tooltip | NEW | No existing chip with this semantic; reused across five surfaces (Feature Forecast, Team Forecast result, Backtest result, Run Chart, PBC). |
| Throughput Run Chart / PBC frontend widgets | (existing) | Chart components | EXTEND | Single new header toggle + chip; no structural change. |
| Team Forecast / Backtest forms | (existing) | Form components | EXTEND | Single new toggle on the input form; passes through. |
| EF migration | `Migrations/Sqlite/…` + `Migrations/Postgres/…` | DB schema | NEW (one per provider) | Generated via the existing `CreateMigration` PowerShell script. Both providers in lockstep (CLAUDE.md). |

The Reuse Analysis is the hard gate: every NEW row is paired with an explicit "no existing alternative" justification; every EXTEND row links to an existing component path.

---

## Wave: DESIGN / [REF] Quality attribute strategies

**Maintainability** (primary driver): `IRuleEvaluator<T>` + `IRuleFieldProvider<T>` make the next rule-based surface (e.g. cycle-time exclusion, predictability scope) a ~half-day implementation — one new `IRuleFieldProvider<TNewEntity>` + one new service wrapping it. Rule operators, field-key validation, and value-length caps live in one place.

**Testability** (CLAUDE.md TDD non-negotiable): every new interface is mock-isolable via Moq. `RuleEvaluator<T>` is a pure function (no I/O); tested via NUnit parameterised tests against a fixed corpus per provider. `ForecastFilterRuleService` is tested against an in-memory team list and `LicenseService` mock. `TeamMetricsService` integration test asserts the filter step is applied iff the conditions in DDD-4 hold.

**Correctness regression safety** (D7 invariant): the canary test (DDD-7) is the regression gate for the rule-engine generalisation. If the refactor drifts the JSON shape or the validity verdict, the canary fails before merge.

**Performance**: filter step is `O(items × conditions)` where items is bounded by `ThroughputHistory` days × items-per-day (typically <1000 closed items in window for a normal-sized team). Negligible against the 10000-trial Monte Carlo cost. Caching: `TeamMetricsService.GetFromCacheIfExists` key includes the new `mode` enum so filtered and unfiltered series cache independently.

**Determinism** (invariant #8): the filter is a deterministic predicate over the closed-items list. Identical team state + identical rule set + identical seed → identical filtered throughput → identical Monte Carlo output.

**Forecast safety on empty filtered sample** (invariant #5 / US-05 chart half): when the filter excludes ALL items, `ForecastFilterRuleService.Filter` returns an empty `IEnumerable<WorkItem>`. The caller in `TeamMetricsService` checks `Total == 0` and, depending on call site, either:
- Forecast call site: returns the UNFILTERED `RunChartData` AND sets `FilterApplied = false` with `ExcludedSummary = "Filter excluded all throughput; showing unfiltered forecast"` for the chip. (D5 forecast half — non-blocking warning.)
- Chart-PBC call site (`?view=filtered`): returns the empty filtered chart — caller (FE) renders the empty-state message (D5 chart half).

**Security**: no new attack surface. The schema endpoint requires `TeamRead`; the JSON column mutation requires `TeamWrite` (existing gates on `PUT /api/team/{teamId}`). Value-length cap (500 chars) and rule-count cap (20) inherited from `WorkItemRuleSet`.

---

## Wave: DESIGN / [REF] Architectural enforcement

| Rule | Mechanism |
|---|---|
| `IRuleEvaluator<T>` implementations are pure (no I/O) | NUnit test asserts `RuleEvaluator<T>` does not depend on any `IRepository<>`, `IDbContext`, `HttpClient`, `ILogger` (constructor inspection). |
| `DeliveryRuleService` public API surface is preserved through the refactor | NUnit "API compatibility" test loads the assembly via reflection and asserts the three public methods exist with their original signatures (the refactor commit's safety net). |
| Forecast filter only applies inside `TeamMetricsService`'s two throughput seams (DDD-4 — single point of policy) | ArchUnitNET test (extends existing suite per rbac/oauth precedent): classes outside `Services.Implementation.TeamMetricsService` and `Services.Implementation.ForecastFilterRuleService` must not invoke `IForecastFilterRuleService.Filter` directly. |
| `ForecastFilterRuleService.GetEffectiveRuleSet` is the SINGLE point where the license premium gate is checked for the filter feature (DDD-9) | Unit test on `ForecastFilterRuleService` covers all three null-equivalent cases (free tenant, null JSON, zero conditions). ArchUnitNET test: `TeamMetricsService` must not call `ILicenseService` directly (must go through `IForecastFilterRuleService`). |
| Frontend rule-engine reuse: `ForecastFilterEditor` composes `DeliveryRuleBuilder` rather than reimplementing | Vitest test asserts `ForecastFilterEditor` renders `<DeliveryRuleBuilder>` with the throughput-filter title and emptyStateMessage props; structural snapshot of the JSX tree. |
| Canary test for D7 invariant (rule-engine JSON shape reuse) | DDD-7 test class is in the regular `dotnet test` run; CI gate. |

---

## Wave: DESIGN / [REF] Open questions (deferred to DISTILL/DELIVER)

1. **Throughput Run Chart frontend widget path** — exact path is `Lighthouse.Frontend/src/components/Common/Charts/*Throughput*` per Slice 03's wording ("`TotalThroughputWidget` and any related per-state throughput chart"). DISTILL to locate the precise file(s) and confirm the header-toggle insertion point. Not blocking — pattern is "add a toggle next to chart title."
2. **TypeScript port of the rule evaluator for client-side Run Chart filtering** — Slice 03 needs a minimal TS implementation of `evaluateCondition(workItem, condition)` for the Run Chart toggle. Two options at DELIVER: (a) hand-port the small evaluator (≈40 LOC); (b) extract a JSON-schema-defined operator semantics that backend and frontend both consume. (a) is simpler given the cap of three operators. DESIGN recommendation: (a). Mark with a small comment linking to the C# `RuleEvaluator<T>` so divergence is visible if it happens; the canary test (DDD-7) protects the JSON shape but not the operator semantics on the FE side — add a small Vitest "operator parity" test that constructs the same input both sides and asserts the same boolean.
3. **`/api/forecast/feature/{id}/run` shape** — feature-delta references this route in DISCUSS but the current `ForecastController` does not expose it (feature forecasts are computed via `IForecastUpdater.TriggerUpdate(portfolioId)` writing to `Feature.Forecast` properties, then read via `GET /api/features/{id}`). DESIGN recommendation: the chip data (`filterApplied`, `excludedSummary`) is computed at forecast-time and stored on `Feature.Forecast` (or an adjacent property bag on the Feature entity); DISTILL to confirm the read-path DTO and the persistence shape. Not blocking — additive to whatever the current Feature read DTO is.
4. **Outcome Collision Check** — `nwave-ai outcomes check-delta` CLI not available on this workstation (no nwave-ai binary in PATH). Skipped; reviewer to flag if available in CI.
5. **D5 forecast-fallback chip wording** — DISCUSS lock is "Filter excluded all throughput; showing unfiltered forecast" — confirm DISTILL's acceptance test asserts exactly this string (or formalises a localisation key).

---

## Wave: DESIGN / [REF] Handoff notes for DEVOPS (Platform Architect)

- **No new infrastructure**: feature is purely additive backend + frontend code + one EF migration (additive nullable column on `Teams`). No new dashboards, log streams, or external services.
- **CI parity unchanged**: existing `dotnet build` / `dotnet test` / `pnpm build` / `pnpm test` / Playwright `@premium` E2E / SonarCloud / `ci_verifysqlite.yml` + `ci_verifypostgres.yml` cover the change. The new EF migration is exercised by both DB verification workflows automatically.
- **Mutation testing target**: Stryker.NET ≥80% kill rate on:
  - `RuleEvaluator<T>` (new — pure function, easy mutation target)
  - `FeatureFieldProvider` and `WorkItemFieldProvider` (new)
  - `ForecastFilterRuleService` (new)
  - Refactored sections of `DeliveryRuleService` (regression-protected by both the existing rule-based-deliveries tests AND the new canary)
  - `TeamMetricsService` filter integration (new branches in `GetCurrentThroughputForTeamForecast` and `GetBlackoutAwareThroughputForTeam`)
- **KPIs**: community-feedback proxies only (per memory `project_self_hosted_telemetry_gap` — telemetry blocked on Epic 5015). No new metrics emitters wired.
- **External integrations**: none. This feature touches only internal services and the database.
- **ADO board mirror** (per memory `feedback_ado_workflow_rules`): Epic 4896 stays Active; child Stories US-01..US-07 mirrored at DELIVER-wave time, transitioned per `/ado-sync` ritual. DESIGN wave does not transition states.

---

## Wave: DESIGN / [REF] Wave-decisions summary

```markdown
# DESIGN Decisions — filter-forecast-throughput

## Key Decisions
- [DDD-1] Hybrid rule-engine generalisation: reuse value-objects verbatim; extract IRuleEvaluator<T> + IRuleFieldProvider<T>; existing DeliveryRuleService delegates internally (public surface unchanged); new ForecastFilterRuleService is the WorkItem-scoped delegator.
- [DDD-2] RuleSetSemantics (Include/Exclude) decided at the caller; not embedded in WorkItemRuleSet JSON.
- [DDD-3] Persistence: Team.ForecastFilterRuleSetJson nullable string column. Sqlite + Postgres migrations via the existing CreateMigration script.
- [DDD-4] Filter seam: inside ITeamMetricsService.GetCurrentThroughputForTeamForecast AND .GetBlackoutAwareThroughputForTeam, gated by a new ThroughputFilterMode parameter (default preserves today's behaviour).
- [DDD-5] Run Chart = client-side filter (RunChartData carries WorkItemBase per day). PBC = backend ?view=filtered (PBC payload exposes only WorkItemIds).
- [DDD-6] Reuse DeliveryRuleBuilder with two new optional props (title, emptyStateMessage).
- [DDD-7] Canary test class RuleEngineReuseCanaryTests proves shape compatibility — CI gate.
- [DDD-8] Null / empty / zero-condition rule sets normalised to "no filter" at the service-layer entry.
- [DDD-9] License premium gate enforced INSIDE ForecastFilterRuleService.GetEffectiveRuleSet (read-path gate); persisted JSON column accepts writes regardless of license to preserve invariant #7 (US-07 non-destructive downgrade).

## Architectural Pattern
Ports-and-adapters (hexagonal) — existing pattern, extended only. No new style introduced.

## ADRs
- ADR-012: Rule-engine generalisation strategy (Option C hybrid — value-objects shared, evaluator extracted, public surfaces preserved)
- ADR-013: Rule-match semantics decided at the caller via RuleSetSemantics enum (not embedded in WorkItemRuleSet)
- ADR-014: Throughput chart toggle delivery mechanism split by payload shape (Run Chart client-side, PBC backend ?view= query)

## Changed Assumptions
None. All DISCUSS-wave decisions (D1..D9) honoured.

## Cross-cutting invariants — design coverage
1. Default chart behaviour: enforced by DDD-5 (Run Chart unchanged, PBC default `view=raw`).
2. Feature-forecast no-toggle: enforced by DDD-4 (default `mode=RespectTeamSetting`) — FE never sends `applyFilterOverride` for Feature Forecasts.
3. Multi-team feature forecasts: enforced by `ForecastService.InitializeThroughputPerTeam` iterating per-team; each team's filter independently applied via the existing per-team `GetCurrentThroughputForTeamForecast(team)` call.
4. Empty-filter no-op: enforced by DDD-8 — `GetEffectiveRuleSet` returns null for all three null-equivalent cases.
5. RBAC: existing `[RbacGuard(TeamWrite)]` on `PUT /api/team/{teamId}`; new schema endpoint `[RbacGuard(TeamRead)]`.
6. Rule-engine reuse: enforced by DDD-7 canary test (CI gate).
7. License-downgrade non-destruction: enforced by DDD-9 (license is a read-path gate, not a write-path gate).
8. Forecast determinism: filter is a pure predicate over closed items; preserves determinism.
```

---

## Wave: DISTILL / [REF] WS strategy

Strategy B — Real local + faked WTS, per orchestrator decision 2026-05-22. Real WebApplicationFactory backend, real Sqlite, real Vitest. The work-tracking-system connector (Jira / ADO / Linear) is faked via the existing stub pattern at `Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/`. The faked connector returns a mixed closed history of User Stories and Bugs so the filter has observable effect on the throughput sample.

ONE Playwright `@walking_skeleton @premium @real-io` scenario drives the full user journey from team-admin configures a rule inside the existing Forecast Configuration InputGroup → flip throughput chart toggle → flip Team Forecast toggle → flip Backtest toggle. Implementation-invariant scenarios are routed to backend NUnit integration and frontend Vitest layers per the `feedback_ci_and_e2e_minimalism` memory.

## Wave: DISTILL / [REF] Test placement

Mirrors the `Lighthouse.EndToEndTests/tests/specs/oauth/` pattern: a `.feature` Gherkin documentation companion + a `.spec.ts` Playwright skeleton with `test.skip()` placeholders. Backend integration tests live under `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/` (HTTP-driven via WebApplicationFactory) and `…/Services/Implementation/Forecast/` (service-driven with real EF Sqlite + Moq for license boundaries). Frontend Vitest tests live next to their components under `Lighthouse.Frontend/src/`.

## Wave: DISTILL / [REF] Scenario list with tags

The walking skeleton is ONE end-to-end scenario covering US-01 / US-02 / US-03 / US-04 / US-05 / US-06. Implementation-invariant scenarios are re-layered to backend NUnit and frontend Vitest. Tag legend matches the OAuth pattern: `@walking_skeleton`, `@premium`, `@driving_adapter`, `@real-io`, `@US-N`, `@kpi-OUT-N`.

| # | Scenario title | Slice | Layer | Tags |
|---|---|---|---|---|
| 1 | Premium delivery-forecaster configures the filter and propagates it across every forecast surface | 01–04 | Playwright | `@walking_skeleton @premium @driving_adapter @real-io @US-01 @US-02 @US-03 @US-04 @US-05 @US-06 @kpi-OUT-filter-adoption` |
| 2 | PutTeam premium tenant team admin with valid rule set persists rule set and returns 200 | 01 | Backend integration (HTTP) | `@US-01` |
| 3 | GetTeam after rule set saved returns ForecastFilterRuleSetJson in payload | 01 | Backend integration (HTTP) | `@US-01` |
| 4 | GetForecastFilterSchema premium tenant team reader returns WorkItem field schema | 01 | Backend integration (HTTP) | `@US-01` |
| 5 | PutTeam premium tenant non-team-admin with rule set returns 403 | 01 | Backend integration (HTTP) | `@US-01 @error` |
| 6 | PutTeam premium tenant unknown field key returns 400 with error message | 01 | Backend integration (HTTP) | `@US-01 @error` |
| 7 | PutTeam premium tenant rule set exceeding MaxConditions returns 400 | 01 | Backend integration (HTTP) | `@US-01 @error` |
| 8 | PutTeam premium tenant rule value exceeding MaxLength returns 400 | 01 | Backend integration (HTTP) | `@US-01 @error` |
| 9 | PutTeam premium tenant zero conditions persists as cleared filter | 01 | Backend integration (HTTP) | `@US-01` |
| 10 | PutTeam non-premium tenant with rule set persists for later re-upgrade | 01 | Backend integration (HTTP) | `@US-07 @error` |
| 11 | GetEffectiveRuleSet free tenant with persisted rule set returns null | 01 | Backend integration (Service) | `@US-07 @real-io @adapter-integration` |
| 12 | GetEffectiveRuleSet premium tenant null JSON returns null | 01 | Backend integration (Service) | `@US-01 @real-io` |
| 13 | GetEffectiveRuleSet premium tenant zero conditions returns null | 01 | Backend integration (Service) | `@US-01 @real-io` |
| 14 | GetEffectiveRuleSet premium tenant non-empty rule set returns deserialised rule set | 01 | Backend integration (Service) | `@US-01 @real-io` |
| 15 | Filter matching rule excludes matched items | 01 | Backend integration (Service) | `@US-02 @real-io` |
| 16 | Filter no matching rule returns all items unchanged | 01 | Backend integration (Service) | `@US-02 @real-io` |
| 17 | Filter rule matches all items returns empty enumeration | 01 | Backend integration (Service) | `@US-02 @error` |
| 18 | License downgrade preserves persisted rule set GetEffective returns null | 01 | Backend integration (Service) | `@US-07 @error @real-io` |
| 19 | License re-upgrade after downgrade GetEffective returns original rule set | 01 | Backend integration (Service) | `@US-07 @real-io` |
| 20 | Feature forecast team with Bug exclusion rule draws throughput from non-Bug closes only | 01 | Backend integration (Service) | `@US-02 @real-io @adapter-integration` |
| 21 | Feature forecast response after filter applied includes filterApplied true and excludedSummary | 01 | Backend integration (Service) | `@US-02 @US-03 @real-io` |
| 22 | Feature forecast team without rule set returns filterApplied false and identical dates to today | 01 | Backend integration (Service) | `@US-02 @real-io` |
| 23 | Feature forecast multi-team feature applies each team's filter independently | 01 | Backend integration (Service) | `@US-02 @real-io @property` |
| 24 | Feature forecast rule set excludes all throughput falls back to unfiltered with warning summary | 01 | Backend integration (Service) | `@US-02 @error @real-io` |
| 25 | Feature forecast identical team state and rule set and seed produces identical percentiles | 01 | Backend integration (Service) | `@US-02 @property @real-io` |
| 26 | Feature forecast request without applyFilterOverride still respects team setting via default mode | 01 | Backend integration (Service) | `@US-02 @real-io` |
| 27 | HowMany premium tenant team with filter applyOverride=false returns unfiltered forecast | 02 | Backend integration (HTTP) | `@US-04` |
| 28 | HowMany premium tenant team with filter applyOverride=true returns filtered forecast | 02 | Backend integration (HTTP) | `@US-04` |
| 29 | HowMany premium tenant team with filter applyOverride omitted defaults to filtered via team setting | 02 | Backend integration (HTTP) | `@US-04` |
| 30 | HowMany premium tenant team without filter ignores override and returns unfiltered forecast | 02 | Backend integration (HTTP) | `@US-04` |
| 31 | HowMany response payload includes filterApplied and excludedSummary | 02 | Backend integration (HTTP) | `@US-03 @US-04` |
| 32 | When premium tenant team with filter applyOverride=false returns unfiltered forecast | 02 | Backend integration (HTTP) | `@US-04` |
| 33 | GetThroughputPbc premium tenant team with filter and view=filtered returns filtered counts | 03 | Backend integration (HTTP) | `@US-05` |
| 34 | GetThroughputPbc premium tenant team with filter and view=raw returns unfiltered counts | 03 | Backend integration (HTTP) | `@US-05` |
| 35 | GetThroughputPbc premium tenant team with filter and query param omitted defaults to raw | 03 | Backend integration (HTTP) | `@US-05 @kpi-OUT-filter-default-chart-regression` |
| 36 | GetThroughputPbc non-premium tenant team with view=filtered silently returns raw | 03 | Backend integration (HTTP) | `@US-05 @US-07 @error` |
| 37 | GetThroughput premium tenant team with filter returns per-item-granular payload for client-side filter | 03 | Backend integration (HTTP) | `@US-05 @kpi-OUT-filter-default-chart-regression` |
| 38 | Backtest premium tenant team with filter applyOverride=false runs against unfiltered historical throughput | 04 | Backend integration (HTTP) | `@US-06` |
| 39 | Backtest premium tenant team with filter applyOverride=true runs against filtered historical throughput | 04 | Backend integration (HTTP) | `@US-06` |
| 40 | Backtest premium tenant team with filter applyOverride omitted defaults to filtered via team setting | 04 | Backend integration (HTTP) | `@US-06` |
| 41 | Backtest result DTO includes filterApplied and excludedSummary for chip | 04 | Backend integration (HTTP) | `@US-03 @US-06` |
| 42 | Backtest premium tenant team without filter ignores override and returns unfiltered result | 04 | Backend integration (HTTP) | `@US-06` |
| 43 | RuleEngine canary: Type=Bug rule set deserialises identically across both consumers | 01 | Backend unit | `@kpi-OUT-filter-rule-engine-regression @property` |
| 44 | RuleEngine canary: Tags contains "maintenance" rule set deserialises identically across both consumers | 01 | Backend unit | `@kpi-OUT-filter-rule-engine-regression @property` |
| 45 | RuleEngine canary: ParentReferenceID=(empty) rule set deserialises identically across both consumers | 01 | Backend unit | `@kpi-OUT-filter-rule-engine-regression @property` |
| 46 | RuleEngine canary: multi-rule set deserialises identically across both consumers | 01 | Backend unit | `@kpi-OUT-filter-rule-engine-regression @property` |
| 47 | RuleEngine canary: additionalField rule set deserialises identically across both consumers | 01 | Backend unit | `@kpi-OUT-filter-rule-engine-regression @property` |
| 48 | RuleEngine canary: rule set exceeding MaxRules fails validation on both consumers | 01 | Backend unit | `@kpi-OUT-filter-rule-engine-regression @property @error` |
| 49 | RuleEngine canary: rule value exceeding MaxLength fails validation on both consumers | 01 | Backend unit | `@kpi-OUT-filter-rule-engine-regression @property @error` |
| 50 | ForecastFilterEditor renders DeliveryRuleBuilder configured for exclusion semantics | 01 | Frontend Vitest | `@US-01` |
| 51 | ForecastFilterEditor fetches WorkItem field schema on mount | 01 | Frontend Vitest | `@US-01` |
| 52 | ForecastFilterEditor renders read-only when current user is not team admin | 01 | Frontend Vitest | `@US-01 @error` |
| 53 | ForecastFilterEditor renders read-only when rule set is non-empty and user is viewer | 01 | Frontend Vitest | `@US-01 @error` |
| 54 | ForecastFilterEditor does not render when tenant is non-premium | 01 | Frontend Vitest | `@US-07 @error` |
| 55 | FilteredThroughputChip renders with "Filtered throughput" label when visible | 01 | Frontend Vitest | `@US-03` |
| 56 | FilteredThroughputChip does not render anything when visible=false | 01 | Frontend Vitest | `@US-03` |
| 57 | FilteredThroughputChip shows excluded summary in tooltip on hover | 01 | Frontend Vitest | `@US-03` |
| 58 | FilteredThroughputChip renders D5 warning copy when summary indicates fallback | 01 | Frontend Vitest | `@US-03 @error` |
| 59 | ForecastSettingsComponent renders ForecastFilterEditor inside the existing Forecast Configuration InputGroup on premium tenants | 01 | Frontend Vitest | `@US-01` |
| 60 | ForecastSettingsComponent renders the upgrade teaser instead of the editor on non-premium tenants | 01 | Frontend Vitest | `@US-07 @error` |
| 61 | ForecastSettingsComponent does not render Forecast Filter sub-section when team page is in default-settings mode | 01 | Frontend Vitest | `@US-01` |
| 62 | ForecastSettingsComponent preserves today's throughputHistory and fixed-dates fields above the new sub-section | 01 | Frontend Vitest | `@US-01` |
| 63 | ThroughputChartFilterToggle renders toggle only when team has filter on premium tenant | 03 | Frontend Vitest | `@US-05` |
| 64 | ThroughputChartFilterToggle defaults to Raw on every render | 03 | Frontend Vitest | `@US-05 @kpi-OUT-filter-default-chart-regression` |
| 65 | ThroughputChartFilterToggle Filtered re-renders Run Chart client-side without network round-trip | 03 | Frontend Vitest | `@US-05` |
| 66 | ThroughputChartFilterToggle Filtered on PBC issues request with ?view=filtered | 03 | Frontend Vitest | `@US-05` |
| 67 | ThroughputChartFilterToggle shows FilteredThroughputChip next to chart title when Filtered | 03 | Frontend Vitest | `@US-03 @US-05` |
| 68 | ThroughputChartFilterToggle shows empty-state when filter excludes every item in window | 03 | Frontend Vitest | `@US-05 @error` |
| 69 | ThroughputChartFilterToggle operator parity with C# evaluator on equals/notEquals/contains | 03 | Frontend Vitest | `@US-05 @property` |
| 70 | TeamForecastForm renders toggle only on premium tenants where team has filter | 02 | Frontend Vitest | `@US-04` |
| 71 | TeamForecastForm defaults toggle to On when visible | 02 | Frontend Vitest | `@US-04` |
| 72 | TeamForecastForm with toggle Off sends applyFilterOverride=false | 02 | Frontend Vitest | `@US-04` |
| 73 | TeamForecastForm with toggle On sends applyFilterOverride=true | 02 | Frontend Vitest | `@US-04` |
| 74 | TeamForecastForm shows FilteredThroughputChip on result panel when filterApplied=true | 02 | Frontend Vitest | `@US-03 @US-04` |
| 75 | BacktestForm renders toggle only on premium tenants where team has filter | 04 | Frontend Vitest | `@US-06` |
| 76 | BacktestForm defaults toggle to On when visible | 04 | Frontend Vitest | `@US-06` |
| 77 | BacktestForm with toggle Off sends applyFilterOverride=false | 04 | Frontend Vitest | `@US-06` |
| 78 | BacktestForm shows FilteredThroughputChip on backtest result view when filterApplied=true | 04 | Frontend Vitest | `@US-03 @US-06` |

Story coverage check (Dim 8 / Check A): US-01 → rows 2-10, 50-54, 59-62 | US-02 → rows 15-17, 20-26 | US-03 → rows 21, 31, 41, 55-58, 67, 74, 78 | US-04 → rows 27-32, 70-74 | US-05 → rows 33-37, 63-69 | US-06 → rows 38-42, 75-78 | US-07 → rows 10, 11, 18-19, 36, 54, 60. All 7 stories covered.

Total scenarios: 78. Error / edge / fallback scenarios: 32 (`@error` tag or expressing fallback / 403 / 400 / empty-state / boundary path) = 41% error ratio. Mandate 1 (error coverage ≥ 40%) PASS.

## Wave: DISTILL / [REF] Driving Adapter coverage

Every driving port from the DESIGN-wave table has at least one scenario exercising it.

| Driving port | Method + Route | Scenario IDs (from table above) | Layer |
|---|---|---|---|
| Team mutation w/ filter rule set | `PUT /api/team/{teamId}` | 1 (Playwright), 2, 5, 6, 7, 8, 9, 10 | Playwright + Backend HTTP |
| Team read w/ filter rule set | `GET /api/team/{teamId}` | 1, 3 | Playwright + Backend HTTP |
| Forecast filter schema | `GET /api/team/{teamId}/forecast-filter/schema` | 1, 4 | Playwright + Backend HTTP |
| Manual / feature forecast | `POST /api/forecast/manual/{id}` (Feature Forecast surface) | 1, 20-26 | Playwright + Backend service (sits behind the controller — feature forecasts are computed via the IForecastUpdater seam per DESIGN OQ #3; the HTTP read shape is exercised indirectly via the integration tests) |
| Team forecast How Many | `POST /api/forecast/team/{teamId}/howmany` | 1, 27-31 | Playwright + Backend HTTP |
| Team forecast When | `POST /api/forecast/team/{teamId}/when` | 32 | Backend HTTP |
| Backtest | `POST /api/forecast/backtest/{teamId}` | 1, 38-42 | Playwright + Backend HTTP |
| Throughput Run Chart | `GET /api/teamMetrics/{teamId}/throughput` | 1, 37, 63-69 | Playwright + Backend HTTP + Vitest |
| Throughput PBC chart | `GET /api/teamMetrics/{teamId}/throughput/pbc` | 1, 33-36, 66 | Playwright + Backend HTTP + Vitest |

## Wave: DISTILL / [REF] Adapter coverage table

Mandate 6 — every driven adapter has at least one real-I/O integration test.

| Adapter | Type | Real-I/O scenario | Notes |
|---|---|---|---|
| `TeamRepository` (real EF Sqlite) | Driven (persistence) | Rows 2-3, 9-14, 18-19 | Real EF Sqlite via WebApplicationFactory; round-trips `Team.ForecastFilterRuleSetJson`. |
| `LicenseService` (in-process, real) | Driven (license read gate) | Rows 11, 18-19, 36, 54, 60 | Real in-process check via `ILicenseService.CanUsePremiumFeatures()`; tenant license state mutated for downgrade scenarios. |
| `WorkTrackingConnector` (faked Jira/ADO/Linear) | Driven (external) | Rows 1 (Playwright), 20, 23, 24, 25 | **Faked** per WS Strategy B — the existing stub pattern at `Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/` is the contract smoke; full real-IdP traffic deferred to manual dogfood per Slice 01 production-data requirement. Acceptable because the connector returns closed-item history shape that the filter never inspects beyond `Type/State/Tags/ParentReferenceId/AdditionalFieldValues` — those properties are stable across real and faked connectors. |
| `TeamMetricsService` cache layer (in-process, real) | Driven (cache) | Rows 20-26, 33-37 | Real in-process cache; cache key includes the new `ThroughputFilterMode` enum (DDD-4 — filtered and unfiltered series cache independently). |
| `RuleEvaluator<WorkItem>` (pure function) | Driven (pure) | Rows 15-17, 43-49 | Pure function — no I/O; tested directly. Constructor-purity contract enforced by ArchUnitNET test. |
| EF migration (`Sqlite + Postgres`) | Driven (schema) | Rows 1 (Playwright runs against real Sqlite), 2-3 | Real Sqlite migration applied at WebApplicationFactory startup. Postgres counterpart verified by the existing `ci_verifypostgres.yml` workflow (no new test needed — both providers in lockstep per CLAUDE.md). |

## Wave: DISTILL / [REF] Scaffolds

All scaffold files tagged with `// SCAFFOLD: true` for the DELIVER agent to grep. Backend stubs throw `InvalidOperationException` so NUnit classifies them as RED test failures, not BROKEN builds. Frontend stubs `throw new Error` in test bodies and return `null` (or no-op) from component bodies. RED, not BROKEN.

**Production-code scaffolds** (backend):
- `Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Forecast/IForecastFilterRuleService.cs`
- `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Forecast/ForecastFilterRuleService.cs`

**Production-code scaffolds** (frontend):
- `Lighthouse.Frontend/src/components/Teams/ForecastFilterEditor/ForecastFilterEditor.tsx`
- `Lighthouse.Frontend/src/components/Common/Forecasting/FilteredThroughputChip.tsx`

**Test scaffolds** (Playwright):
- `Lighthouse.EndToEndTests/tests/specs/teams/ForecastFilter.feature`
- `Lighthouse.EndToEndTests/tests/specs/teams/ForecastFilter.spec.ts`

**Test scaffolds** (Backend NUnit):
- `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/ForecastFilterTeamSettingsIntegrationTest.cs`
- `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/ForecastFilterTeamForecastIntegrationTest.cs`
- `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/ForecastFilterThroughputChartIntegrationTest.cs`
- `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/ForecastFilterBacktestIntegrationTest.cs`
- `Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/ForecastFilterRuleServiceIntegrationTest.cs`
- `Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/ForecastFilterFeatureForecastIntegrationTest.cs`
- `Lighthouse.Backend/Lighthouse.Backend.Tests/Models/WorkItemRules/RuleEngineReuseCanaryTests.cs`

**Test scaffolds** (Frontend Vitest):
- `Lighthouse.Frontend/src/components/Teams/ForecastFilterEditor/ForecastFilterEditor.test.tsx`
- `Lighthouse.Frontend/src/components/Common/Forecasting/FilteredThroughputChip.test.tsx`
- `Lighthouse.Frontend/src/pages/Teams/Edit/ForecastSettingsComponent.test.tsx` (appended describe block — existing file extended)
- `Lighthouse.Frontend/src/components/Common/Charts/ThroughputChart/ThroughputChartFilterToggle.test.tsx`
- `Lighthouse.Frontend/src/components/Teams/TeamForecastForm/TeamForecastForm.test.tsx`
- `Lighthouse.Frontend/src/components/Teams/BacktestForm/BacktestForm.test.tsx`

**EF migration placeholder**: per CLAUDE.md DOD item 7, the migration `AddForecastFilterRuleSetJsonToTeam` is NOT generated here. DELIVER wave runs the existing `CreateMigration` PowerShell script (Sqlite + Postgres in lockstep).

## Wave: DISTILL / [REF] Pre-requisites

DESIGN / DEVOPS commitments the scenarios depend on:

- **DDD-1** — `IRuleEvaluator<T>` + `IRuleFieldProvider<T>` ports extracted in the refactor commit before the feature commit; `ForecastFilterRuleService` depends on `IRuleEvaluator<WorkItem>`.
- **DDD-3** — `Team.ForecastFilterRuleSetJson` nullable string column added via the existing `CreateMigration` PowerShell script (both Sqlite + Postgres in lockstep).
- **DDD-4** — `ITeamMetricsService.GetCurrentThroughputForTeamForecast(team, mode)` + `GetBlackoutAwareThroughputForTeam(team, start, end, mode)` extended with optional `ThroughputFilterMode mode = RespectTeamSetting`.
- **DDD-6** — `DeliveryRuleBuilder` extended with two optional props (`title`, `emptyStateMessage`); defaults preserve today's strings.
- **DDD-9** — License gate enforced INSIDE `ForecastFilterRuleService.GetEffectiveRuleSet` (read-path gate); persisted column accepts writes regardless of license.
- **DESIGN OQ #3** — Feature Forecast read-path DTO confirmed by DELIVER; `filterApplied` + `excludedSummary` carried on the projection returned by `GET /api/features/{id}` (current backing path; DESIGN open question left to DELIVER for exact placement).
- **DEVOPS** — No new infrastructure (architecture brief lines 489-490). Existing `ci_verifysqlite.yml` + `ci_verifypostgres.yml` workflows cover the EF migration; existing Playwright job runs the new `@walking_skeleton @premium` scenario.

## Wave: DISTILL / [REF] Outcomes registry note

Outcomes registry (`docs/product/outcomes/`) is not yet bootstrapped in this project; the `nwave-ai` CLI is not installed locally. OUT-IDs in `docs/product/kpi-contracts.yaml` (now updated with six entries `OUT-filter-adoption`, `OUT-filter-forecast-shift`, `OUT-filter-default-chart-regression`, `OUT-filter-toggle-divergence`, `OUT-filter-rule-editor-reuse`, `OUT-filter-rule-engine-regression`) serve as the local SSOT until the registry is introduced. The DISCUSS-wave OUT-1…OUT-6 numbering maps 1:1 to those six contract entries.

## Wave: DISTILL / [REF] Handoff to DELIVER

**Handoff to**: `nw-software-crafter` (DELIVER wave) — full artifact set + RED-ready scaffolds.

Slice ordering remains 01 → (02 / 03 / 04). Slice 01's refactor commit (rule-engine generalisation) lands FIRST per CLAUDE.md TDD discipline AND ADR-012; the feature commit follows. The walking-skeleton Playwright scenario stays `test.skip()` until end of Slice 04 wiring; intermediate slices unskip nothing in the Playwright spec but turn the corresponding backend / Vitest tests GREEN.

DOD items 5 (`dotnet build` zero warnings, `pnpm build` clean, SonarCloud quality gate), 6 (Stryker.NET ≥80% kill rate), 7 (EF migration via PowerShell script), 8 (docs page after user confirmation), 9 (ADO Epic 4896 + child stories per `/ado-sync`), 10 (jobs.yaml feature_context) are DELIVER-wave responsibilities.

### DELIVER pre-finalize checklist (consolidated from review gate)

Per the project's `per-feature` mutation testing strategy (CLAUDE.md), the Stryker configs are intentionally feature-scoped and must be updated *during DELIVER finalize* before the ≥80% kill-rate gate is meaningful. Folded in from the platform-architect reviewer (2026-05-22):

1. **Backend Stryker config** (`Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.json`): extend the `mutate` array to cover the new production paths before running Stryker.NET for this feature. Either explicit file list:
   - `Lighthouse.Backend/Services/Implementation/DeliveryRules/RuleEvaluator.cs`
   - `Lighthouse.Backend/Services/Implementation/DeliveryRules/FeatureFieldProvider.cs`
   - `Lighthouse.Backend/Services/Implementation/DeliveryRules/WorkItemFieldProvider.cs`
   - `Lighthouse.Backend/Services/Implementation/Forecast/ForecastFilterRuleService.cs`
   - `Lighthouse.Backend/Models/Metrics/ThroughputFilterMode.cs` (if introduced under that namespace)
   - plus the `IRuleEvaluator<T>` / `IRuleFieldProvider<T>` interface seams and any extensions to `TeamMetricsService`
   - or switch to inclusive glob (`**/Services/Implementation/{DeliveryRules,Forecast}/**/*.cs`) and document the convention as a one-liner in the config.
2. **Frontend Stryker harness**: create `Lighthouse.Frontend/vitest.stryker.filter-forecast.config.ts` mirroring the existing RBAC-scoped pattern, covering: `ForecastFilterEditor.test.tsx`, `FilteredThroughputChip.test.tsx`, `ThroughputChartFilterToggle.test.tsx`, `TeamForecastForm.test.tsx`, `BacktestForm.test.tsx`, and the appended describe block in `ForecastSettingsComponent.test.tsx`. Wire it into the per-feature mutation command parity with backend Stryker.NET.
3. **Glob hygiene** (low): if the backend `mutate` field stays a hard-coded file list, document in a one-line config comment that adding a new file under `DeliveryRules/` or `Forecast/` requires manually extending the list. Globs avoid the foot-gun; choose deliberately.

These three tasks are **not** DISTILL blockers — they relate to running mutation testing on production code that does not yet exist (RED scaffolds throw). They are pre-conditions for closing DOD item 6 at the end of DELIVER.

## Wave: DELIVER / [WHY] Decision: OQ-3 DTO shape

**Decision**: `FilterApplied: bool` + `ExcludedSummary: string?` ship as adjacent properties on the existing `WhenForecast` (`Feature.Forecast`) entity, projected through `WhenForecastDto`. NOT a new top-level `FeatureDto` field, NOT a separate property bag.

**Rationale**: cohesion. The chip data lives where the percentile dates live (`feature.forecasts[].filterApplied`, `feature.forecasts[].excludedSummary`); the FE chip component reads from the same per-forecast path that already drives the date display, no parallel lookup required. Multi-team aggregation collapses naturally on `AggregatedWhenForecast`: `FilterApplied = any(team.FilterApplied)`, `ExcludedSummary` = distinct non-null summaries joined by `"; "` (or `null` when no team contributes a summary).

**Alternative considered**: a new `feature.forecastFilterStatus: { applied, summary }` object hanging off `FeatureDto`. Rejected — duplicates the per-forecast/per-team locality already captured by `Forecasts[]`, forces the FE to cross-reference two paths, and breaks the cache-key symmetry of `WhenForecast` (which already keys filtered vs unfiltered Monte Carlo series at the TeamMetricsService seam — DDD-4 / scenario 27).

**Path**: `WhenForecast.FilterApplied` / `WhenForecast.ExcludedSummary` → populated in `ForecastService.UpdateFeatureForecasts` from the `ForecastThroughputStatus` map built by `InitializeThroughputPerTeam` → projected through `WhenForecastDto(forecast, probability)` → consumed by the FE chip at slice 01-16.
