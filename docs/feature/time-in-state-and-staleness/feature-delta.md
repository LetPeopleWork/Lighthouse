# Feature: time-in-state-and-staleness

Epic 4144 (More Detailed State Info) — first feature, covers slices A + B1 + D from the Epic catalog.

ADO Epic: https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/4144

## Wave: DISCUSS / [REF] Persona ID

**Primary**: `flow-coach` — team lead, agile coach, scrum master, or RTE running a team's flow review. Uses Lighthouse as a flow diagnostic surface (not just a forecasting tool).

**Secondary**: `team-admin`, `portfolio-admin` — existing RBAC personas, scoped to configuring the staleness threshold for the team or portfolio they own.

## Wave: DISCUSS / [REF] JTBD one-liner

When I'm running a team's flow review, I want to spot stuck items at a glance, so I can start the right "what's blocking this?" conversation before the team's cycle-time distribution gets worse.

Job-id: `job-flow-coach-spot-stuck-items` (in `docs/product/jobs.yaml`).

## Wave: DISCUSS / [REF] Locked decisions

| ID | Decision | Verdict |
|---|---|---|
| D1 | State transitions are pulled from source-of-truth where available (Jira, ADO confirmed; Linear investigated at DESIGN). For CSV or any connector that can't expose history, fall back to sync-side delta capture (compare current State to last-known on every sync). | Locked |
| D2 | Blocked transitions are out of scope for this feature; tracked under Epic #5074 (`docs/feature/epic-5074-blocked-items/`) with a different mechanism. | Locked (deferred) |
| D3 | Slice ordering across Epic 4144: A+B1+D (this feature) → F → B2 → B3 → C. | Locked |
| D7 | Staleness threshold: one integer per Team, one per Portfolio. No per-state thresholds. | Locked |
| D8 | Default staleness threshold: 7 days for newly-created teams; 14 days for newly-created portfolios. Revisable based on `OUT-staleness-threshold-tuning` KPI. | Locked (revisable post-release) |
| D9 | No changes to existing Cycle Time or Work Item Age semantics. Time-in-state is a new measurement that coexists with them. | Locked |

## Wave: DISCUSS / [REF] User stories with elevator pitches

### US-01 — See how long each work item has been in its current state

**Story**: As a `flow-coach`, I want to see how many days each work item has spent in its current state on the team view, so that I can spot stuck items without opening each one in Jira/ADO.

**Job-id**: `job-flow-coach-spot-stuck-items`

### Elevator Pitch
Before: I have to open each work item in Jira/ADO and read the history tab to know how long it has been in its current state — friction prevents me from doing it for every item every day.
After: open `/teams/{teamId}` → see a "Time in State" column populated for every row, format `11d in Review`.
Decision enabled: which items to raise at the next standup, retro, or 1:1 with the owner.

**AC**:
- Given a team backed by a connector with transition history (Jira, ADO, Linear), when I open `/teams/{teamId}`, then every row shows `<integer>d in <currentStateName>`.
- The integer matches the source-system history within 1 day for Jira and ADO teams (verified by integration test against recorded API responses).
- For CSV teams or connectors without history, the integer is the time since the most-recent sync that detected a state change; the badge tooltip reads `"Approximate — based on sync cadence"`.
- For items whose current state was first observed in this sync (no prior data), the badge renders `—` until the next sync establishes a baseline.
- The column is sortable (longest first).

### US-02 — Highlight items exceeding the team's staleness threshold

**Story**: As a `flow-coach`, I want stuck items to stand out visually, so that I don't have to mentally check each badge against a threshold I might forget.

**Job-id**: `job-flow-coach-spot-stuck-items`

### Elevator Pitch
Before: the badge shows `11d in Review` but I have to know in my head whether 11 days is bad for my team's cadence.
After: with the team's threshold set to 7, any item at `8d+` in any state shows the badge in red; others stay neutral.
Decision enabled: which items demand immediate intervention vs. which are still within normal pace.

**AC**:
- Given a team with `stalenessThresholdDays = 7`, when an item has been in its current state ≥8 days, the badge has the warning visual treatment (red colour matching existing blocked-style emphasis).
- When the threshold is 0, no items are highlighted (staleness disabled).
- Threshold changes take effect on the next page render — no sync required.

### US-03 — Configure the team's staleness threshold

**Story**: As a `team-admin`, I want to configure the staleness threshold for my team, so that the visual signal matches my team's expected cadence.

**Job-id**: `job-team-admin-tune-staleness`

### Elevator Pitch
Before: there's no way to tell Lighthouse what "stale" means for my team — the badge is a number with no opinion.
After: in Team Settings → flow-signals section, set `Staleness Threshold (days)` → save → US-02 highlighting in the work-item view updates on next render.
Decision enabled: how aggressive the alert should be for this team's cycle-time normal.

**AC**:
- The setting field is visible only to users whose `useRbac()` returns `isTeamAdmin(teamId) === true`.
- Accepts integers in `[0, 365]`. 0 disables highlighting.
- Default for newly-created teams is 7.
- Persisted on the Team entity; included in the existing `GET /api/teams/{teamId}/settings` payload.

### US-04 — Configure the portfolio's staleness threshold

**Story**: As a `portfolio-admin`, I want to configure the staleness threshold for my portfolio, so that the visual signal works for portfolio-level work items the same way it does for team work items.

**Job-id**: `job-team-admin-tune-staleness`

### Elevator Pitch
Before: portfolio-level items have no staleness signal even after teams get one.
After: in Portfolio Settings → flow-signals section, set `Staleness Threshold (days)` → save → highlighting in the portfolio-level work-item view updates.
Decision enabled: how aggressive the portfolio-level signal should be (typically higher than team threshold).

**AC**:
- The setting field is visible only to users whose `useRbac()` returns `isPortfolioAdmin(portfolioId) === true`.
- Accepts integers in `[0, 365]`. 0 disables highlighting.
- Default for newly-created portfolios is 14.
- Persisted on the Portfolio entity; included in the existing `GET /api/portfolios/{portfolioId}/settings` payload.

## Wave: DISCUSS / [REF] Definition of Done

1. All 4 stories pass their ACs via integration tests (NUnit, EF InMemory, WebApplicationFactory).
2. Time-in-state correctness verified against fixtures from real Jira + ADO API responses.
3. CSV fallback verified with at least 2 synthetic file reloads showing transition detection.
4. RBAC gating on team/portfolio settings verified with `viewer` and `non-admin` test users.
5. No regression in existing Cycle Time / Work Item Age tests.
6. `dotnet build` zero warnings; `pnpm build` clean (CI parity per CLAUDE.md).
7. SonarCloud quality gate passes on PR.
8. Mutation testing (Stryker.NET) for new Backend code: ≥80% kill rate.
9. Docs updated: screenshot of the badge + settings UI regenerated via `/update-docs`.

## Wave: DISCUSS / [REF] Out of scope

- B2 — per-item historical state breakdown (separate future feature)
- B3 — cumulative time-per-state across timeframe (separate future feature)
- C — detailed CFD using actual states (separate future feature)
- F — pace-percentiles bands on the Work Item Aging chart (separate future feature)
- Blocked-time history (Epic #5074, separate mechanism — see `docs/feature/epic-5074-blocked-items/`)
- Per-state thresholds (locked D7)
- Changes to existing CycleTime / WorkItemAge computation
- Backfilling historical state transitions from before this feature ships (data accumulates forward-only)

## Wave: DISCUSS / [REF] WS strategy

**Type A (additive walking skeleton).** No contract changes; only extensions. Thinnest end-to-end slice: US-01 against a single connector (ADO), single team — proves the path from connector → persistence → API → UI column in one cycle.

## Wave: DISCUSS / [REF] Driving ports

| Method | Route | Auth | Status | Change |
|---|---|---|---|---|
| GET | `/api/teams/{teamId}/work-items` | Authenticated | Existing | Add `currentStateEnteredAt: ISO8601` per work item |
| GET | `/api/portfolios/{portfolioId}/work-items` | Authenticated | Existing | Same addition |
| GET | `/api/teams/{teamId}/settings` | Authenticated | Existing | Add `stalenessThresholdDays: int` |
| PUT | `/api/teams/{teamId}/settings` | `isTeamAdmin(teamId)` | Existing | Accept `stalenessThresholdDays` |
| GET | `/api/portfolios/{portfolioId}/settings` | Authenticated | Existing | Add `stalenessThresholdDays: int` |
| PUT | `/api/portfolios/{portfolioId}/settings` | `isPortfolioAdmin(portfolioId)` | Existing | Accept `stalenessThresholdDays` |

No new top-level routes. UI surfaces: `TeamDetail.tsx` work-item table column; `PortfolioDetail.tsx` work-item table column; Team Settings form field; Portfolio Settings form field.

## Wave: DISCUSS / [REF] Pre-requisites

- `bug-5016-cache-thread-safety` (merged 2026-05-17) — the metrics cache must be thread-safe before adding a new derived field that flows through it.
- `rbac-enhancements` (merged) — team-admin / portfolio-admin gating already in place for the settings UX.

No DISCOVER or DIVERGE artifacts exist for Epic 4144; this DISCUSS run is the entry point. JTBD is constructed from the Epic description plus the `Community` and `Productboard` tags on the ADO item — these signal that the Epic is customer-validated even though no formal discovery artifact exists.

## Wave: DISCUSS / [REF] Outcome KPIs

| ID | Target | Scope | Measurement |
|---|---|---|---|
| `OUT-time-in-state-adoption` | ≥40% of teams open the team detail view within 2 weeks of release | per_instance | Existing page-view telemetry on the team detail route, filtered to "time-in-state column rendered" event |
| `OUT-staleness-threshold-tuning` | ≥20% of teams change the default 7-day threshold within 4 weeks | per_instance | `Team.StalenessThresholdDays != 7` count / total teams; sample at week 4 |
| `OUT-time-in-state-trust` | <5% of teams report Lighthouse days-in-state disagreeing with source by ≥2 days | vendor_demo_only + community reports | Issue tracker label `bug-time-in-state-drift`; community Slack channel mentions |

KPIs will be appended to `docs/product/kpi-contracts.yaml` at the DEVOPS handoff.

## Wave: DISCUSS / [REF] Definition of Ready — validation

| # | DoR item | Verdict | Evidence |
|---|---|---|---|
| 1 | Every story traces to a `job_id` | Pass | US-01, US-02 → `job-flow-coach-spot-stuck-items`; US-03, US-04 → `job-team-admin-tune-staleness` |
| 2 | Persona named & scoped | Pass | `flow-coach` primary; `team-admin`, `portfolio-admin` secondary |
| 3 | Elevator pitch per non-`@infrastructure` story | Pass | Each US-NN has Before/After/Decision triplet |
| 4 | AC testable, no ambiguous outcomes | Pass | Quantified thresholds; explicit RBAC predicates; deterministic edge cases (no prior data → `—`) |
| 5 | Out-of-scope explicit | Pass | Section above lists 8 items |
| 6 | Outcome KPIs measurable with targets | Pass | 3 KPIs, each with numeric target and measurement method |
| 7 | Pre-requisites resolved | Pass | bug-5016 merged; RBAC merged |
| 8 | Slice composition: each slice contains ≥1 user-visible story | Pass | Slice 01 ships US-01; Slice 02 ships US-02 + US-03 + US-04 |
| 9 | Handoff target identified | Pass | nw-solution-architect (DESIGN, full artifacts); nw-platform-architect (DEVOPS, outcome-kpis only) |

## Wave: DISCUSS / [REF] Wave decisions summary

**Primary user need**: enable flow coaches to spot stuck items at a glance via per-item time-in-state badges, with a per-Team / per-Portfolio configurable staleness threshold that turns the badge red when exceeded.

**Foundation investment**: a new `WorkItemStateTransition` data model and sync-time capture mechanism. This investment unlocks the rest of Epic 4144 (B2, B3, C, F all reuse it; only E needs its own mechanism).

**Walking skeleton scope**: slice 01 (US-01) against Jira + ADO connectors and the Team detail view.

**Feature type**: cross-cutting (sync layer + persistence + 2 UI surfaces + 2 settings forms).

**Upstream changes**: none. DISCOVER and DIVERGE were skipped because the Epic is community-validated by the Productboard + Community tags. No prior assumption is contradicted.

## Wave: DESIGN / [REF] DDD list

| ID | Decision | Verdict | One-line rationale |
|---|---|---|---|
| DDD-1 | `WorkItemStateTransition` is a standalone entity with FK→WorkItem (not an owned collection, not a JSON column) | Locked | Consumer aggregate-query friendliness + read-path performance for the work-item table; see ADR-015 |
| DDD-2 | `WorkItem.CurrentStateEnteredAt` is a sync-time-persisted column (not a query-time MAX over transitions, not a materialised view) | Locked | Work-item table renders the badge with one SELECT, zero subqueries; see ADR-016 |
| DDD-3 | Transition capture dispatch is a per-connector `SupportsTransitionHistory` boolean; sync-delta fallback lives in `WorkItemService.RefreshWorkItems` | Locked | One seam, explicit capability declaration, observable Linear runtime downgrade; see ADR-017 |
| DDD-4 | Shared `IPerStateAggregationService` is NOT introduced by this DESIGN | Locked (deferred to consumer DESIGNs) | Sibling consumer semantics diverge (completed-only-window-distribution vs full-duration-frame-inclusion); helper would conflate. Sibling DESIGNs consume `IWorkItemStateTransitionRepository` directly; ADR-018 |
| DDD-5 | `StalenessThresholdDays` lives on `WorkTrackingSystemOptionsOwner` (inherited by both `Team` and `Portfolio`); defaults applied via entity initialiser (`= 7` for Team, `= 14` for Portfolio) | Locked | DISCUSS D8 mandates per-Team and per-Portfolio with no per-state breakdown; shared base class avoids two parallel migrations |
| DDD-6 | `WorkItemService.RefreshWorkItems` is the ONLY writer of `WorkItemStateTransition` rows AND the ONLY mutator of `WorkItem.CurrentStateEnteredAt`. Both writes flush in one `SaveChangesAsync` | Locked | Aggregate-consistency invariant by-construction; ArchUnitNET-enforced |
| DDD-7 | Transition idempotency: dedup by `(WorkItemId, ToState, TransitionedAt)` on every sync | Locked | Re-running the same sync (Jira/ADO re-fetch with full `expand=changelog`) must not produce duplicate rows; integration test asserts |
| DDD-8 | Per-render staleness comparison is client-side, driven by `currentStateEnteredAt` + `stalenessThresholdDays` on the FE | Locked | DISCUSS US-02 AC line 3 ("Threshold changes take effect on the next page render — no sync required"); no backend round-trip on threshold edit |
| DDD-9 | The `WorkItemDto.Approximate: bool` flag carries the badge tooltip distinction. Sibling consumers (`aging-pace-percentiles`, `state-time-cumulative-view`) consume transition rows uniformly — only the badge UX surfaces capability |  Locked | Sibling DESIGNs do NOT need to branch on transition origin; the distinction lives only at the FE badge layer |
| DDD-10 | Linear connector extends GraphQL query with `history` connection; runtime downgrade to sync-delta if the field is unsupported per connection (logged once, badge tooltip surfaces "Approximate") | Locked | DISCUSS D1 mandated investigating Linear at DESIGN. Public Linear GraphQL schema documents `Issue.history.nodes { fromState toState createdAt }`; some Linear plans / quotas may restrict — graceful per-connection downgrade is the right failure mode |
| DDD-11 | The work-item-list API endpoint is `GET /api/v1/teams/{teamId}/metrics/wip` and `/cycleTimeData` (NOT a flat `/work-items` route as DISCUSS shorthand implied) — `WorkItemDto` is the carrier | Locked (DISCUSS-shorthand correction) | Code-reality check: routes live on `TeamMetricsController` at `/api/v1/teams/{teamId}/metrics/*`. Surface is the same; the DISCUSS shorthand is preserved with this DESIGN-side correction |

## Wave: DESIGN / [REF] Component decomposition

| Component | File | Change Type | Change Summary |
|---|---|---|---|
| `WorkItemStateTransition` (entity) | `Lighthouse.Backend/Lighthouse.Backend/Models/WorkItemStateTransition.cs` | NEW | 4 properties per DISCUSS: `WorkItemId` (FK), `FromState`, `ToState`, `TransitionedAt`. Implements `IEntity`. |
| `IWorkItemStateTransitionRepository` (port) | `Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Repositories/IWorkItemStateTransitionRepository.cs` | NEW | Extends `IRepository<WorkItemStateTransition>`. No additional methods at MVP (sibling consumer DESIGNs may extend). |
| `WorkItemStateTransitionRepository` (adapter) | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Repositories/WorkItemStateTransitionRepository.cs` | NEW | Extends `RepositoryBase<WorkItemStateTransition>` following the existing pattern. |
| EF migration `AddWorkItemStateTransitions` | `Lighthouse.Backend/Lighthouse.Migrations.Sqlite/Migrations/*_AddWorkItemStateTransitions.cs` + `Lighthouse.Backend/Lighthouse.Migrations.Postgres/Migrations/*_AddWorkItemStateTransitions.cs` | NEW | Adds `WorkItemStateTransitions` table (FK→WorkItems cascade delete; composite index `(WorkItemId, TransitionedAt)`), adds `WorkItems.CurrentStateEnteredAt` nullable, adds `WorkTrackingSystemOptionsOwner.StalenessThresholdDays` non-null with default values seeded by EF. Generated via `Create-Migration.ps1` (CLAUDE.md). |
| `LighthouseAppContext` | `Lighthouse.Backend/Lighthouse.Backend/Data/LighthouseAppContext.cs` | EXTEND | Adds `DbSet<WorkItemStateTransition>`. Adds `OnModelCreating` config block for the entity (FK + cascade + index). |
| `WorkItemBase` | `Lighthouse.Backend/Lighthouse.Backend/Models/WorkItemBase.cs` | EXTEND | Adds `DateTime? CurrentStateEnteredAt { get; set; }`. Adds transient `[NotMapped] IReadOnlyList<WorkItemStateTransition> SyncedTransitions { get; init; }` (sync-transport data; consumed and detached by `WorkItemService.RefreshWorkItems`). |
| `WorkTrackingSystemOptionsOwner` | `Lighthouse.Backend/Lighthouse.Backend/Models/WorkTrackingSystemOptionsOwner.cs` | EXTEND | Adds `int StalenessThresholdDays { get; set; }`. Default values applied via `Team` and `Portfolio` entity initialisers (7 and 14 respectively per DISCUSS D8). |
| `IWorkTrackingConnector` | `Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/WorkTrackingConnectors/IWorkTrackingConnector.cs` | EXTEND | Adds `bool SupportsTransitionHistory { get; }`. |
| `IssueFactory` (Jira) | `Lighthouse.Backend/Lighthouse.Backend/Factories/IssueFactory.cs` | EXTEND | Add `GetAllStateTransitions(JsonElement json) → IReadOnlyList<(string fromState, string toState, DateTime transitionedAt)>` that walks the SAME `changelog.histories` array as today's `GetTransitionDate`, but emits every status transition. Existing `GetStartedAndClosedDate` unchanged (DISCUSS D9 invariant). |
| `JiraWorkTrackingConnector` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs` | EXTEND | `SupportsTransitionHistory => true`. In `CreateWorkItemFromJiraIssue`, populates `WorkItemBase.SyncedTransitions` from the new `IssueFactory.GetAllStateTransitions`. |
| `AzureDevOpsWorkTrackingConnector` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs` | EXTEND | `SupportsTransitionHistory => true`. Add `GetAllStateTransitionsThrottled(witClient, workItemId)` next to today's `GetStateTransitionDateThrottled`; in `ConvertAdoWorkItemToLighthouseWorkItem`, populate `SyncedTransitions`. |
| `LinearWorkTrackingConnector` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Linear/LinearWorkTrackingConnector.cs` + `LinearResponses.cs` | EXTEND | `SupportsTransitionHistory => true` (with per-connection runtime downgrade). Extend `IssueNode` GraphQL query with `history { nodes { fromState { name } toState { name } createdAt } }`. Map to `SyncedTransitions`. On GraphQL error indicating history-field unsupported, log once + flip a per-connection in-memory flag + return `SyncedTransitions = []` for that connection thereafter; `WorkItemService` falls through to sync-delta for that connection's items. |
| `CsvWorkTrackingConnector` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Csv/CsvWorkTrackingConnector.cs` | EXTEND | `SupportsTransitionHistory => false`. `SyncedTransitions = []` for every item. No other changes. |
| `WorkItemService.RefreshWorkItems` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkItems/WorkItemService.cs` | EXTEND | Single seam for transition persistence + `CurrentStateEnteredAt` derivation. Capability-flag branch per ADR-017. All writes flush in one `SaveChangesAsync`. |
| `WorkItemDto` | `Lighthouse.Backend/Lighthouse.Backend/API/DTO/WorkItemDto.cs` | EXTEND | Adds `CurrentStateEnteredAt: DateTime?` and `Approximate: bool` (true when latest transition was synthesised via sync-delta — connector did not provide history). |
| `SettingsOwnerDtoBase` | `Lighthouse.Backend/Lighthouse.Backend/API/DTO/SettingsOwnerDtoBase.cs` | EXTEND | Adds `int StalenessThresholdDays { get; set; }`. Bubbles up to both `TeamSettingDto` and `PortfolioSettingDto` via the existing base-class constructor pattern. |
| `TeamController.UpdateTeam` | `Lighthouse.Backend/Lighthouse.Backend/API/TeamController.cs` | EXTEND | Validates `stalenessThresholdDays ∈ [0,365]`; returns 400 on out-of-range. Existing `[RbacGuard(TeamWrite)]` gates write. |
| `PortfolioController.UpdatePortfolio` | `Lighthouse.Backend/Lighthouse.Backend/API/PortfolioController.cs` | EXTEND | Same validation as TeamController. |
| ArchUnitNET tests | `Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/` (existing suite) | EXTEND | Adds the rules from ADR-015/016/017/018: WorkItem has no transition navigation; `WorkItemService.RefreshWorkItems` is sole writer; no `*PerStateAggregation*` class introduced. |
| Per-connector integration tests | `Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/WorkItemServiceTest.cs` + per-connector fixtures | EXTEND | One test per connector × representative fixture asserts: (a) transition rows persisted with expected timestamps; (b) `CurrentStateEnteredAt == MAX(transitions.TransitionedAt WHERE ToState = State)` invariant; (c) idempotency on re-sync. |
| `IWorkItem` model (TS) | `Lighthouse.Frontend/src/models/WorkItem.ts` | EXTEND | Adds `currentStateEnteredAt: Date \| null` and `approximate: boolean`. |
| `TimeInStateBadge` component | `Lighthouse.Frontend/src/components/Common/TimeInStateBadge/TimeInStateBadge.tsx` (+ test) | NEW | Renders `<integer>d in <stateName>` with optional red emphasis. Tooltip `"Approximate — based on sync cadence"` when `approximate === true`. Renders `—` when `currentStateEnteredAt === null`. Pure component; takes `(currentStateEnteredAt, currentStateName, stalenessThresholdDays, approximate, now)` props (with `now` defaulting to a clock service for testability). |
| `WorkItemsDialog` | `Lighthouse.Frontend/src/components/Common/WorkItemsDialog/WorkItemsDialog.tsx` | EXTEND | Adds optional `timeInStateColumn?: { stalenessThresholdDays: number }` prop (parallel structure to existing `highlightColumn`). When provided, the dialog renders a "Time in State" sortable column using `TimeInStateBadge`. |
| `ItemsInProgress` + Team/Portfolio metrics views | `Lighthouse.Frontend/src/pages/Teams/Detail/ItemsInProgress.tsx`, `TeamMetricsView.tsx`, equivalent portfolio file | EXTEND | Pass `timeInStateColumn={{ stalenessThresholdDays: team.stalenessThresholdDays }}` (and portfolio equivalent) through to `WorkItemsDialog`. |
| `ITeamSettings` model (TS) | `Lighthouse.Frontend/src/models/Team/TeamSettings.ts` | EXTEND | Adds `stalenessThresholdDays: number`. |
| `IPortfolioSettings` model (TS) | `Lighthouse.Frontend/src/models/Portfolio/PortfolioSettings.ts` | EXTEND | Adds `stalenessThresholdDays: number`. |
| `ForecastSettingsComponent` (team) | `Lighthouse.Frontend/src/pages/Teams/Edit/ForecastSettingsComponent.tsx` | EXTEND | Adds a new `InputGroup` titled "Flow Signals" containing a numeric `TextField` for `Staleness Threshold (days)` ([0,365], default 7). Field visible only when `useRbac().isTeamAdmin(teamId)` returns true (gated via parent page's `useRbacGate` or equivalent — matches existing pattern). |
| Portfolio edit settings form | `Lighthouse.Frontend/src/pages/Portfolios/Edit/EditPortfolio.tsx` (+ existing portfolio settings sub-components) | EXTEND | Same "Flow Signals" `InputGroup` (default 14, gated by `useRbac().isPortfolioAdmin(portfolioId)`). |
| Vitest + RTL tests for the new badge / column / settings field | colocated `.test.tsx` files | NEW / EXTEND | RBAC-gating, threshold-change-on-render, approximate-tooltip behaviour, badge red-emphasis above threshold. |
| E2E spec | `Lighthouse.EndToEndTests/tests/specs/flow/TimeInStateAndStaleness.spec.ts` (new file) | NEW | 1 happy-path scenario per skill DoD #5: open team → see badge → flip threshold → badges re-colour. NUnit integration tests carry the per-connector correctness work (faster than driving four connectors through Playwright). |

### Reuse / CREATE NEW summary

**EXTEND: 19** (every backend touchpoint is an extension of an existing class / method / DTO / settings round-trip / migration mechanism)
**NEW: 9** (1 entity + 1 repo port + 1 repo impl + 1 migration pair + 1 frontend component + 1 frontend test file group + 1 e2e spec + ArchUnit additions to existing suite, per-connector integration test additions to existing suite — the per-connector integration test extensions are counted under EXTEND on the table above for accuracy)

## Wave: DESIGN / [REF] Driving ports

| Method | Route | Auth | Status | Implementation pointer |
|---|---|---|---|---|
| GET | `/api/v1/teams/{teamId:int}/metrics/wip?asOfDate=…` | `[RbacGuard(TeamRead, ScopeIdRouteKey="teamId")]` | EXTEND | `TeamMetricsController.GetCurrentWipForTeam` line ~102 — payload via `WorkItemDto`; new fields auto-flow |
| GET | `/api/v1/teams/{teamId:int}/metrics/cycleTimeData?startDate&endDate` | `[RbacGuard(TeamRead)]` | EXTEND | `TeamMetricsController.GetCycleTimeDataForTeam` line ~131 — same |
| GET | `/api/v1/teams/{teamId:int}` | `[RbacGuard(TeamRead)]` | EXTEND (settings carrier — `TeamSettingDto` gains `stalenessThresholdDays`) | `TeamsController.GetTeamSettings` (parallel to `TeamController.GetTeam`); confirm exact route at GREEN time |
| PUT | `/api/v1/teams/{teamId:int}` | `[RbacGuard(TeamWrite)]` | EXTEND — validates `stalenessThresholdDays ∈ [0,365]` | `TeamController.UpdateTeam` (existing PUT handler — extend with validation) |
| GET | `/api/v1/portfolios/{portfolioId:int}` (settings GET) | `[RbacGuard(PortfolioRead)]` | EXTEND (settings carrier — `PortfolioSettingDto` gains `stalenessThresholdDays`) | `PortfolioController.GetPortfolio` (existing) |
| PUT | `/api/v1/portfolios/{portfolioId:int}` | `[RbacGuard(PortfolioWrite)]` | EXTEND — validates `stalenessThresholdDays ∈ [0,365]` | `PortfolioController.UpdatePortfolio` line ~85 |

No new top-level routes; no premium gate; no contract removal — Type A additive throughout, per DISCUSS WS.

## Wave: DESIGN / [REF] Driven ports + adapters

| Port | Adapter | Status |
|---|---|---|
| `IWorkItemStateTransitionRepository` (extends `IRepository<WorkItemStateTransition>`) | `WorkItemStateTransitionRepository` (EF Core via `LighthouseAppContext`) | NEW |
| `IWorkTrackingConnector.SupportsTransitionHistory` | `JiraWorkTrackingConnector` (true), `AzureDevOpsWorkTrackingConnector` (true), `LinearWorkTrackingConnector` (true with runtime downgrade), `CsvWorkTrackingConnector` (false) | EXTEND |
| `WorkItem.CurrentStateEnteredAt` persistence | `LighthouseAppContext` (EF Core, Sqlite + Postgres lockstep) | EXTEND (additive nullable column) |
| `WorkTrackingSystemOptionsOwner.StalenessThresholdDays` persistence | `LighthouseAppContext` (EF Core, Sqlite + Postgres lockstep) | EXTEND (additive non-null column with entity-initialiser defaults) |
| `WorkItemStateTransitions` table persistence | `LighthouseAppContext` (EF Core, Sqlite + Postgres lockstep) | NEW (new `DbSet<>`; FK + cascade + composite index) |

External integrations REUSED: Jira REST (`expand=changelog` already requested), Azure DevOps Work Item Tracking API (revisions already fetched), Linear GraphQL (new `history` field added to the existing query), CSV file system (no new contract). No new external integration; no contract test required (sibling features may add contract tests for the new endpoints they introduce, but THIS DESIGN does not introduce any external integration the project does not already have).

## Wave: DESIGN / [REF] Technology choices

| Component | Pin |
|---|---|
| Backend | C# .NET 8, ASP.NET Core, EF Core 8.x (existing) |
| Backend tests | NUnit 4.6, Moq, EF InMemory, `Microsoft.AspNetCore.Mvc.Testing` (WebApplicationFactory) — per the project's actual stack, NOT per CLAUDE.md's xUnit claim (project_test_stack memory) |
| Backend mutation | Stryker.NET (≥80% kill rate gate) |
| Backend migration tool | `Lighthouse.Backend/Create-Migration.ps1` (CLAUDE.md hard rule) |
| Backend ArchUnit | ArchUnitNET (existing suite extended) |
| Frontend | React 18 + TypeScript 5.x (strict), MUI 5.x |
| Frontend tests | Vitest + React Testing Library |
| Frontend mutation | Stryker (TS) (≥80% kill rate gate) |
| Frontend linter | Biome (zero errors / zero warnings on `./src` per CLAUDE.md) |
| E2E | Playwright with Page Object Model |

NO new technology introduced.

## Wave: DESIGN / [REF] Decisions table

| ID | Decision | Source / ADR |
|---|---|---|
| DDD-1 | `WorkItemStateTransition` placement: standalone entity with FK | ADR-015 |
| DDD-2 | `CurrentStateEnteredAt` derivation: sync-time persisted column | ADR-016 |
| DDD-3 | Transition capture dispatch: per-connector capability flag, sync-delta fallback in `WorkItemService` | ADR-017 |
| DDD-4 | Shared `IPerStateAggregationService`: NOT introduced (deferred to consumer DESIGNs) | ADR-018 |
| DDD-5 | `StalenessThresholdDays` placement: shared via `WorkTrackingSystemOptionsOwner` base class | DESIGN, no ADR (DISCUSS D7/D8 fixes the shape; placement is mechanical) |
| DDD-6 | Single seam (`WorkItemService.RefreshWorkItems` only writer/mutator); ArchUnitNET-enforced | ADR-016 / ADR-017 |
| DDD-7 | Transition idempotency: dedup by `(WorkItemId, ToState, TransitionedAt)` | ADR-017 |
| DDD-8 | Client-side staleness comparison (US-02 AC line 3) | DISCUSS US-02 (DESIGN locks the placement) |
| DDD-9 | `WorkItemDto.Approximate: bool` — only UX-layer distinguisher of capability path | DESIGN, no ADR (DISCUSS US-01 AC mandates the tooltip; placement is mechanical) |
| DDD-10 | Linear connector: extend GraphQL query with `history` connection; per-connection runtime downgrade | ADR-017 |
| DDD-11 | DISCUSS shorthand `/api/teams/{teamId}/work-items` is corrected to actual routes on `TeamMetricsController` (`/metrics/wip`, `/metrics/cycleTimeData`) | DESIGN, code-reality correction surfaced under Driving Ports |

## Wave: DESIGN / [REF] Reuse Analysis

| Existing Component | File | Overlap | Decision | Justification |
|---|---|---|---|---|
| `WorkItemService.RefreshWorkItems` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkItems/WorkItemService.cs:51-86` | Already does the diff between stored and incoming work items; already iterates per item; already calls `SaveChangesAsync` per sync | EXTEND | Adding transition-persistence + sync-delta synthesis inside the SAME loop is the natural extension; the diff that decides "new vs updated vs removed" already implicitly knows whether state changed. Net add: ~30 LOC vs ~150 LOC for a parallel sync-layer class. |
| `IssueFactory.GetTransitionDate` / `ExtractDateOfStateTransitionFromHistory` (Jira) | `Lighthouse.Backend/Lighthouse.Backend/Factories/IssueFactory.cs:171-223` | Already walks every changelog `histories[].items[]` looking for `field == "status"` transitions; already extracts `(createdDate, fromString, toString)` per transition; today keeps only the LAST one matching a target-state predicate | EXTEND | The walker function ALREADY sees every transition; adding `GetAllStateTransitions` next to it reuses the parsing investment. CREATE NEW would duplicate the changelog-pagination + json-extraction logic. |
| `AzureDevOpsWorkTrackingConnector.GetStateTransitionDateThrottled` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs:758-778` | Already calls `witClient.GetRevisionsAsync(workItemId)`; already iterates revisions; already detects state changes via `RevisionWasChangingState`; today keeps only the LAST boundary transition | EXTEND | Same reasoning as Jira: the revision walker already pages and parses; adding `GetAllStateTransitionsThrottled` reuses the throttled fetch and revision-iteration code. |
| `LinearWorkTrackingConnector` (GraphQL query for `IssueNode`) | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Linear/LinearWorkTrackingConnector.cs` + `LinearResponses.cs:171-200` | Existing query fetches `IssueNode` fields including `state { name }`. Linear's GraphQL exposes `Issue.history.nodes { fromState toState createdAt }` per their public schema | EXTEND | Adding the `history` connection to the existing query is a 1-block GraphQL change + a new `LinearResponses.IssueHistoryNode` mapping. CREATE NEW Linear-only history service would re-implement the GraphQL transport that the existing connector already operates. |
| `CsvWorkTrackingConnector` | `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Csv/CsvWorkTrackingConnector.cs:17-188` | No history at the source — CSV files are snapshots | EXTEND (single property: `SupportsTransitionHistory => false`) | No new code needed beyond the capability declaration; `WorkItemService` handles the rest. |
| `WorkItemBase` | `Lighthouse.Backend/Lighthouse.Backend/Models/WorkItemBase.cs` | Already owns the per-item state semantics: `State`, `StateCategory`, `StartedDate`, `ClosedDate`, computed `CycleTime` and `WorkItemAge` | EXTEND | `CurrentStateEnteredAt` is the third member of the same family (derived from state-history). Plus transient `SyncedTransitions` for sync-transport. Net add: 2 properties. |
| `WorkTrackingSystemOptionsOwner` | `Lighthouse.Backend/Lighthouse.Backend/Models/WorkTrackingSystemOptionsOwner.cs` | Already houses settings shared between `Team` and `Portfolio` (e.g. `ThroughputHistory` on Team-specific, `StateMappings` / `BlockedStates` / `BlockedTags` / `SystemWIPLimit` shared) | EXTEND | `StalenessThresholdDays` belongs in the same base — same lifecycle, same RBAC scope (admin of the owner can edit). Single column, two entity initialisers seed the defaults (Team=7, Portfolio=14). |
| `WorkItemDto` | `Lighthouse.Backend/Lighthouse.Backend/API/DTO/WorkItemDto.cs` | Already projects `WorkItemBase` for the work-item-table API | EXTEND | Add `CurrentStateEnteredAt`, `Approximate`. Net add: 2 properties. CREATE NEW DTO would duplicate the projection of 12 existing fields. |
| `SettingsOwnerDtoBase` / `TeamSettingDto` / `PortfolioSettingDto` | `Lighthouse.Backend/Lighthouse.Backend/API/DTO/SettingsOwnerDtoBase.cs` (+ Team/Portfolio specifics) | Already round-trips shared settings via the base class; existing `bug-5016-cache-thread-safety` pre-req cleared the cache invariant | EXTEND | Add `StalenessThresholdDays` to the base; both Team and Portfolio inherit automatically. |
| `LighthouseAppContext` / `OnModelCreating` | `Lighthouse.Backend/Lighthouse.Backend/Data/LighthouseAppContext.cs` | Already maps every entity, already configures FKs and cascade-deletes for related entities | EXTEND | Add `DbSet<WorkItemStateTransition>` + one `entity =>` config block. Migration via `Create-Migration.ps1`. CREATE NEW DbContext is absurd. |
| `Create-Migration.ps1` | `Lighthouse.Backend/Create-Migration.ps1` | Already generates lockstep Sqlite + Postgres migrations | REUSE AS-IS | CLAUDE.md hard rule. |
| `WorkItemsDialog` | `Lighthouse.Frontend/src/components/Common/WorkItemsDialog/WorkItemsDialog.tsx` | Already renders a sortable work-item DataGrid with optional `highlightColumn` extension slot for per-item numeric values | EXTEND | The `timeInStateColumn` prop is structurally parallel to the existing `highlightColumn`. CREATE NEW table component would duplicate the DataGrid setup, sort persistence, export, and link rendering. |
| `useRbac` hook + `useRbacGate` | `Lighthouse.Frontend/src/hooks/useRbac.ts`, `useRbacGate.ts` | Already exposes `isTeamAdmin(teamId)`, `isPortfolioAdmin(portfolioId)`, hidden-on-no-access render gating | REUSE AS-IS | Architectural invariant from rbac-enhancements (brief.md §RBAC). No new permissions; no new hook needed. |
| `ForecastSettingsComponent` / `EditPortfolio` settings page | `Lighthouse.Frontend/src/pages/Teams/Edit/ForecastSettingsComponent.tsx`, `Portfolios/Edit/EditPortfolio.tsx` | Already host setting `InputGroup` sections with numeric fields and RBAC gating | EXTEND | New `Flow Signals` group adjacent to existing `Forecast Configuration` group. Pattern-parallel to existing groups; CREATE NEW page would split the team settings UX. |
| `WorkItemAge` computation on `WorkItemBase` | `Lighthouse.Backend/Lighthouse.Backend/Models/WorkItemBase.cs:66-84` | Computes "days since item started" for in-progress items; uses `GetDateDifference` (date-only diff + 1) | REUSE the same `GetDateDifference` rounding convention for `daysInState` | DISCUSS US-01 AC mandates "matches source within 1 day"; using the project's existing day-diff convention keeps the badge consistent with `WorkItemAge` semantics. (Implementation detail handed to software-crafter.) |

**Totals: 15 rows. 9 EXTEND + 4 REUSE-AS-IS (no change) + 0 CREATE NEW at the OVERLAP level.** (The NEW entries — `WorkItemStateTransition` entity, the new repository, the new EF migration, the new `TimeInStateBadge` component, the new E2E spec — appear in the Component decomposition table; they have ZERO existing overlap in the codebase per the Reuse Analysis grep below, so they are correctly NEW.)

CODEBASE GREP FOR OVERLAP CANDIDATES THAT MIGHT JUSTIFY CREATE NEW BUT WERE REJECTED:

- Searched for `Transition|StateChange|StateHistory|state.*log|StateAudit` across `Lighthouse.Backend` — only matches are in connector code (changelog walkers) already in the Reuse table above. No existing entity / table / service named anything resembling `WorkItemStateTransition`.
- Searched for `staleness|stuckItem|stale.*item|TimeInState` across `Lighthouse.Backend` + `Lighthouse.Frontend/src` — zero production matches. No existing component duplicates the badge concern.
- Searched for `PerState|StateAggregation|PerStateMetric` — zero production matches. Confirms ADR-018's "no existing service to extend" position.

## Wave: DESIGN / [REF] Open questions

- **Linear `history` GraphQL plan/quota validation**: assumed available based on Linear's public GraphQL schema documentation. If a customer's plan denies the field at runtime, the per-connection runtime downgrade (ADR-017 / DDD-10) handles it gracefully. **Validation deferred to DELIVER** — first Linear-connected fixture exercises the path; if the field fails for the test workspace, the downgrade fires and the integration test asserts the fallback. No DESIGN blocker.
- **Backfill of pre-feature transitions**: explicitly OUT OF SCOPE per DISCUSS. First-observation items render `—`. Sibling consumers (`aging-pace-percentiles`, `state-time-cumulative-view`) inherit the same forward-only constraint — both explicitly noted this in their DISCUSS "out of scope" lists. **Resolved by DISCUSS; no DESIGN action.**
- **Cross-MVP coordination — shared aggregation primitive**: DEFERRED to consumer DESIGNs (ADR-018). Sibling DESIGN authors may choose to extract a helper inside their own DESIGN with a new superseding ADR. **Resolved by ADR-018; tracked in the consumer DESIGNs.**
- **`Team.PortfolioId`-scoped vs portfolio-team-aggregate staleness threshold semantics**: portfolios contain teams; if a portfolio has threshold 14 but its constituent teams have threshold 7, which threshold applies to portfolio-scope WORK ITEMS (which are different entities — portfolio work items are `Feature`s, not the constituent teams' `WorkItem`s)? Per DISCUSS US-04 the portfolio threshold applies to PORTFOLIO-level items (the `Feature` entities under the portfolio's `Features` collection — NOT a recursive aggregation of constituent teams' WorkItems). **Resolved**: portfolio threshold applies to the portfolio-level item table only; team thresholds apply to team-level item tables only. No cross-scope aggregation. (This was implicit in DISCUSS; making it explicit here so DELIVER does not invent surprising semantics.)
- **`Feature` entity transitions**: this DESIGN ships `WorkItemStateTransition` (FK→`WorkItem`). Portfolio item tables show `Feature`s (different entity). The DISCUSS feature-delta's "Add `currentStateEnteredAt` per work item" in the portfolio endpoint refers to the FEATURE-equivalent entity shown on the portfolio detail page. **Resolution for DELIVER**: mirror the same shape on `Feature` — add `Feature.CurrentStateEnteredAt` column + capture `Feature`-level transitions via the existing `WorkItemService.UpdateFeaturesForPortfolio` path. The architectural mechanism is identical (`FeatureStateTransition` entity OR generalising `WorkItemStateTransition` to accept either via a polymorphism; software-crafter chooses at GREEN — both are valid implementations of the same architectural rule). **Flagged as DELIVER decision, not a DESIGN blocker** because the architectural shape is the same; only the entity is different.

## Wave: DESIGN / [REF] Cross-MVP coordination outcomes

- **`WorkItemStateTransition` 4-field schema is sufficient for both sibling consumers** — confirmed against sibling DISCUSS D11 (aging-pace-percentiles) and D9 (state-time-cumulative-view). No upstream change requested by either sibling. NO FIELD ADDITION IS SURFACED by this DESIGN. The baseline assumption holds.
- **`WorkItem.CurrentStateEnteredAt` is the derived-but-persisted column both siblings explicitly rely on** — confirmed against sibling state-time-cumulative-view D9 and D11. ADR-016 ships exactly that shape.
- **Shared `IPerStateAggregationService` decision** — RESOLVED: NOT introduced by this DESIGN. ADR-018 records the decision and the rationale (semantic divergence between consumers; premature abstraction risk). Sibling DESIGNs are free to extract a helper later via a superseding ADR. ONE-LINE rationale: *"Helper would conflate inclusion-rule semantics that sibling DISCUSSes deliberately distinguished; ship the primitive (repository), defer the abstraction to known concrete callers."*

## Wave: DESIGN / [REF] Wave decisions summary

**Primary architectural commitment**: introduce a minimum viable persistence + capture pipeline for `WorkItemStateTransition` (1 new entity, 1 new column on `WorkItem`, 1 new column on `WorkTrackingSystemOptionsOwner`, 1 new capability boolean on `IWorkTrackingConnector`). Every other touchpoint is an additive extension of an existing class / DTO / route / migration mechanism. NO new top-level routes. NO new external integration. NO premium gate. NO breaking change.

**Architecture pattern**: ports-and-adapters / hexagonal (unchanged). The transition-capture dispatch is a single-property capability flag on the existing port; the dispatch seam is `WorkItemService.RefreshWorkItems` and it is the ONLY mutator of the new column / the ONLY writer of the new table.

**Walking skeleton path** (validates the architecture in one slice end-to-end): Jira (or ADO) connector → IssueFactory extension → WorkItemBase.SyncedTransitions → WorkItemService.RefreshWorkItems → `WorkItemStateTransition` row + `WorkItem.CurrentStateEnteredAt` update → existing `WorkItemDto` carries the new field → existing `/metrics/wip` route serves it → new `TimeInStateBadge` component renders it on the Team detail view. One slice exercises every architectural seam.

**Sibling MVP coordination**: both consumers' "no schema additions required" claims hold. The cross-MVP shared-aggregation question (D10 of both siblings) is answered: DEFER to consumer DESIGNs (ADR-018).

**Downstream changes propagated upstream**: NONE. DISCUSS assumptions (D1-D9) all hold without revision. The only DISCUSS-shorthand clarification surfaced (DDD-11: route is `/metrics/wip` not `/work-items`) does not change the contract or the persona experience.

**Handoff readiness**: ready for nw-platform-architect (DEVOPS) handoff. The outcome KPIs in DISCUSS feed into `kpi-contracts.yaml` at the handoff per DISCUSS note. No CI workflow changes are required (the existing `ci_verifysqlite.yml` + `ci_verifypostgres.yml` cover the EF migration; the existing test gates cover the new code).

## Wave: DESIGN / [REF] Outcome Collision Check

`nwave-ai outcomes check-delta docs/feature/time-in-state-and-staleness/feature-delta.md` was NOT executed: the tool (`nwave-ai`) is not installed in this repository and the canonical outcomes registry at `docs/product/outcomes/registry.yaml` does not exist. Per skill ("skip-and-document"): documented here, deferred to DEVOPS handoff for KPI / outcomes registration alongside the DISCUSS-defined `OUT-time-in-state-adoption`, `OUT-staleness-threshold-tuning`, `OUT-time-in-state-trust`.

## Wave: DISTILL / [REF] Inherited commitments

Scope: **slice 01 / US-01 only** (Jira + ADO, source-of-truth history path). Staleness threshold/red colour, Linear, CSV fallback, portfolio column, and settings forms are slice 02 — explicitly NOT covered by DISTILL this pass.

| Origin | Commitment | DDD | Impact |
|--------|------------|-----|--------|
| DISCUSS#US-01 | Every in-progress row on the team view shows `<integer>d in <currentStateName>` | n/a | Walking-skeleton E2E + black-box read-API ATs assert the badge data flows to the work-item table |
| DISCUSS#US-01 | Jira/ADO derived integer matches source history within 1 day | n/a | Two correctness ATs assert `currentStateEnteredAt` is within 1 day of the last transition timestamp |
| DISCUSS#US-01 | First-observed item (no prior data) renders `—` | n/a | Edge AT asserts `currentStateEnteredAt` is JSON `null` (FE maps null → `—` per DDD-9) |
| DISCUSS#US-01 | "Time in State" column is sortable (longest first) | n/a | Walking-skeleton spec exercises the sortable column header through the POM |
| DESIGN#DDD-11 | Read endpoint is `GET /api/v1/teams/{teamId}/metrics/wip` on `TeamMetricsController`, `WorkItemDto` is the carrier | DDD-11 | All backend ATs drive `/api/latest/teams/{teamId}/metrics/wip` (DISCUSS-shorthand `/work-items` corrected) |
| DESIGN#DDD-7 | Re-running the same sync must not change derived `currentStateEnteredAt` nor duplicate rows | DDD-7 | Idempotency AT asserts a stable `currentStateEnteredAt` across two reads of the same synced team |
| DESIGN#DDD-2 | `WorkItem.CurrentStateEnteredAt` is a sync-time-persisted column carried unchanged through the read DTO | DDD-2 | ATs assert the field on the response JSON, not a query-time recomputation |

## Wave: DISTILL / [REF] Scenario list with tags

| # | Scenario | Tier / Layer | Tags | US-01 AC covered |
|---|---|---|---|---|
| 1 | flow coach sees how long each in-progress item has been in its current state (E2E, Playwright `test.fixme`) | Tier A — E2E (layer 6) | `@walking_skeleton` `@driving_port` `@real-io` `@US-01` | Badge `<N>d in <state>` rendered on team view + sortable column (the demo proof) |
| 2 | `GetWip_JiraTeamWithInProgressItem_ExposesCurrentStateEnteredAtPerItem` | Tier A — integration (layer 4) | `@driving_port` `@US-01` | Read endpoint exposes `currentStateEnteredAt` per item |
| 3 | `GetWip_TeamSyncedWithTransitionHistory_CurrentStateEnteredAtMatchesLastTransitionWithinOneDay` | Tier A — integration (layer 4) | `@driving_port` `@US-01` | Read-API renders the derived value within 1 day of the last transition (connector-agnostic at this boundary) |
| 4 | `GetWip_SameTeamReadTwice_CurrentStateEnteredAtIsStableAcrossReads` | Tier A — integration (layer 4) | `@driving_port` `@US-01` `@error` (idempotency edge) | DDD-7 idempotency — value stable across reads |
| 5 | `GetWip_ItemFirstObservedThisSync_CurrentStateEnteredAtIsNull` | Tier A — integration (layer 4) | `@driving_port` `@US-01` `@error` (first-observation edge) | First-observed item surfaces `null` → FE `—` |

Error/edge ratio: 2 of 5 scenarios (#4 idempotency, #5 first-observation) are edge/negative = **40%**, on target. Tier B (state-machine PBT) is correctly SKIPPED: US-01 is a 1-story read-projection with a 1-2 scenario journey and no rich chained state machine — Tier A examples cover the space (Mandate 10 "skip when" clauses 2 and 3).

**Per-connector parsing correctness is deferred to DELIVER (not authored here).** The read-API ATs are connector-agnostic — at the `/metrics/wip` boundary the value renders identically regardless of which connector derived it, so a Jira-vs-ADO split there is duplication. The genuine Jira `changelog.histories` and ADO revisions parsing correctness (US-01 AC line 2, "verified against recorded API responses") becomes connector-layer tests authored in DELIVER: Jira via the existing `IssueFactoryTest` changelog-template style; ADO from a captured `GetRevisionsAsync` payload (no template helper exists yet — DELIVER captures one).

## Wave: DISTILL / [REF] WS strategy

Inherits DISCUSS "Type A (additive walking skeleton)". Under the project Architecture of Reference: **Driving port** = real HTTP via `WebApplicationFactory<Program>` (backend) and the production React app via Playwright (E2E); **Driven internal** (EF `LighthouseAppContext`, repositories) = real adapter through the test factory (`EnsureCreated`/`EnsureDeleted` per the `ForecastFilter*IntegrationTest` precedent); **Driven external** (Jira/ADO connector APIs) = real sync in E2E (`testWithUpdatedTeams` triggers a live update against the configured demo instances), seeded persisted state in the backend ATs. One walking-skeleton scenario (#1) closes the loop end-to-end through the production composition root.

## Wave: DISTILL / [REF] Adapter coverage

| Driven adapter | Covered by | Real-IO scenario? |
|---|---|---|
| `JiraWorkTrackingConnector` transition capture | E2E #1 (Jira-backed variant via `testWithUpdatedTeams`); connector-layer correctness AT authored in DELIVER (`IssueFactoryTest` changelog-template style) | DELIVER — read-API ATs here are connector-agnostic |
| `AzureDevOpsWorkTrackingConnector` transition capture | E2E #1 (ADO-backed `testData.teams[0]`); connector-layer correctness AT authored in DELIVER (captured `GetRevisionsAsync` payload) | DELIVER — read-API ATs here are connector-agnostic |
| `WorkItemStateTransitionRepository` / `WorkItem.CurrentStateEnteredAt` persistence (EF) | All backend ATs read the persisted column through the real EF context via the read endpoint | YES — real EF round-trip through `WebApplicationFactory` |

Per the DESIGN E2E note ("NUnit integration tests carry the per-connector correctness work, faster than driving four connectors through Playwright"), the Jira/ADO parsing correctness lands as connector-layer tests in DELIVER — see the per-connector deferral above and the fixture gap note.

## Wave: DISTILL / [REF] Test placement + precedent justification

| Artifact | Path | Precedent |
|---|---|---|
| Backend read-API ATs (4) | `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/TimeInStateReadApiIntegrationTest.cs` | Mirrors `ForecastFilterThroughputChartIntegrationTest` (seed Team + WorkItem via repositories, drive `/metrics/*` over `WebApplicationFactory`, parse response JSON) and `ForecastFilterTeamSettingsIntegrationTest` (`JsonDocument.Parse` dynamic assertions, `client.AsTeamAdmin`) |
| E2E walking-skeleton spec (1) | `Lighthouse.EndToEndTests/tests/specs/flow/TimeInStateAndStaleness.spec.ts` (new `flow/` dir per DESIGN component table) | Mirrors `teams/ForecastFilter.spec.ts` (`goToMetrics` → `switchCategory` → `getWidgetByName` → `openDialog`) and `TeamsDetail.spec.ts` (team-data update wait) |
| E2E POM extension | `Lighthouse.EndToEndTests/tests/models/metrics/WorkItemsDialog.ts` (extended, not duplicated) | DESIGN locates the "Time in State" column on `WorkItemsDialog`; POM methods (`timeInStateColumnHeader`, `getTimeInStateBadges`, `sortByTimeInState`) added per the project's POM-only rule |

**Black-box / port-to-port note**: the backend ATs reference NO not-yet-existing C# symbol (`WorkItemDto.CurrentStateEnteredAt`, the new entity, new connector members). `currentStateEnteredAt` is read from the response JSON dynamically; the seed helper sets the future column via reflection (`property?.SetValue`, a no-op today). The suite therefore compiles against today's code (`dotnet build -warnaserror` clean) and each test fails at RUNTIME with the field absent — the right RED reason — once un-ignored. This is what keeps `dotnet build` green and the CI suite green in this DISTILL pass.

## Wave: DISTILL / [REF] AT files created

- `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/TimeInStateReadApiIntegrationTest.cs` — 4 tests, all `[Ignore("pending DELIVER: US-01 …")]` (NUnit skip marker per the polyglot matrix C# row).
- `Lighthouse.EndToEndTests/tests/specs/flow/TimeInStateAndStaleness.spec.ts` — 1 walking-skeleton scenario, `test.fixme` (Playwright skip marker).
- `Lighthouse.EndToEndTests/tests/models/metrics/WorkItemsDialog.ts` — extended POM (3 new members for the Time-in-State column).

No production code, no scaffold stubs, no `.feature`/Python artifacts. DELIVER is the sole author of production code.

## Wave: DISTILL / [REF] Pre-DELIVER fail-for-the-right-reason gate

Before writing any production code, DELIVER MUST un-ignore each AT one at a time (per ADR-025: RED phase only unskips DISTILL scaffolds) and confirm it fails because functionality is missing — not because of a setup, compile, or fixture error:

1. **Un-ignore backend AT one at a time.** Remove a single `[Ignore]`, run `dotnet test --filter`, and classify the failure. Correct RED for #2/#3: `currentStateEnteredAt` absent from the JSON → `TryGetDateTime` returns false → assertion fires. Correct RED for #5 (first-observation): property present-but-not-null OR absent. Correct RED for #4 (idempotency): values differ or absent. WRONG RED (BLOCK and fix the test first): `JsonDocument.Parse` throws, the wip array is empty (seed/StateCategory mismatch), a 403/500 status, or any compile error.
2. **The reflection seed becomes a real setter at GREEN.** `ApplyCurrentStateEnteredAt` uses reflection only so the test compiles before the column exists. Once DELIVER adds `WorkItem.CurrentStateEnteredAt`, replace the reflection call with the real property setter (or seed `WorkItemStateTransition` rows + let `WorkItemService.RefreshWorkItems` derive it — preferred, as it exercises the real seam). Confirm `git diff --stat` shows production files changed when the AT flips to GREEN (no Fixture Theater).
3. **Un-fixme the E2E spec last**, start a local app, run it once, and confirm it fails because the column/badge does not render — then drive it green. Per the project rule, never commit a Playwright spec that has not been RUN locally against a started app.

## Wave: DISTILL / [REF] Reconciliation + infrastructure policy notes

- **Wave-decision reconciliation: PASSED — 0 contradictions.** DISCUSS and DESIGN agree across US-01. DDD-11 (`/work-items` → `/metrics/wip`) is a flagged DISCUSS-shorthand correction, not a contradiction. No `docs/feature/time-in-state-and-staleness/devops/` delta exists — default environment assumptions apply (warn, not block).
- **Infrastructure policy:** the canonical Python pilot artifacts (`docs/architecture/atdd-infrastructure-policy.md`, `tests/common/state_delta.<ext>`, `assert_state_delta` Universe assertions, Hypothesis/PBT harnesses, `__SCAFFOLD__` stubs) are NOT bootstrapped — this is a C#/.NET + React/Playwright project, not the Python pilot. The project's equivalent policy is already encoded in precedent: Driving = `WebApplicationFactory<Program>` (backend) / production React app (E2E); Driven internal = EF context via the test factory; Driven external = live connector sync (E2E) or persisted-state seeding (backend ATs). Mandates 8-9 (state-delta Universe, layer-dependent PBT) are Python-pilot-specific and do not transfer; the C# row of the polyglot matrix governs (NUnit skip marker, example-based integration tests, no PBT at layer 4+ per Mandate 11).

## Wave: DISTILL / [REF] Inherited commitments (slice 02 — US-02 / US-03 / US-04 + Linear/CSV capture + Portfolio column)

Scope: **slice 02 only** (staleness threshold, visual highlight, portfolio parity, Linear source-of-truth + CSV sync-delta fallback). US-01 (slice 01) is shipped and unchanged.

| Origin | Commitment | DDD | Impact |
|--------|------------|-----|--------|
| DISCUSS#US-03 | Team settings payload exposes `stalenessThresholdDays`, default 7; PUT accepts it; `[0,365]` enforced; write gated by `isTeamAdmin` | DDD-5 | GET/PUT round-trip ATs assert default + persistence; out-of-range → 400; non-admin → 403 |
| DISCUSS#US-04 | Portfolio settings payload exposes `stalenessThresholdDays`, default 14; same `[0,365]` + `isPortfolioAdmin` gating | DDD-5 | Mirror of US-03 ATs against the portfolio settings carrier |
| DISCUSS#US-04 | Portfolio (Feature) work-item read view carries `currentStateEnteredAt` for the Time-in-State column | DDD-9 / DESIGN open-q (Feature transitions) | Portfolio `/metrics/wip` read AT pins the contract; Feature-level capture is the DELIVER decision (FeatureStateTransition or polymorphic reuse) |
| DISCUSS#US-02 | Threshold comparison is client-side; red treatment takes effect on next render, no sync | DDD-8 | Asserted in the E2E walking-skeleton only (FE behaviour); backend just persists/serves the threshold |
| DISCUSS#D1 / slice-02 | CSV reload that detects a State change captures a transition + derives `currentStateEnteredAt`; connector without history → fallback (`approximate=true`); Linear with history → real (`approximate=false`) | DDD-1 / DDD-3 / DDD-9 | Service-seam ATs assert the OBSERVABLE contract (`CurrentStateEnteredAt` + captured transition). **No `source` field** — slice-doc `source="csv-fallback"` is superseded by `WorkItemDto.Approximate: bool` |
| DESIGN#DDD-7 | Re-syncing an unchanged CSV item must not synthesise duplicate sync-delta transitions | DDD-7 | Idempotency seam AT asserts ≤1 transition into the mapped state across two unchanged syncs |

## Wave: DISTILL / [REF] Scenario list with tags (slice 02)

| # | Scenario | Tier / Layer | Tags | AC covered |
|---|---|---|---|---|
| 1 | `GetTeamSettings_NewlyCreatedTeam_ExposesDefaultStalenessThresholdOfSevenDays` | Tier A — integration (layer 4) | `@driving_port` `@US-03` | Team default 7 + payload exposure |
| 2 | `PutTeam_TeamAdminSetsThresholdToFourteen_SettingsRoundTripsTheNewThreshold` | Tier A — integration (layer 4) | `@driving_port` `@US-03` | PUT accepts + GET round-trips |
| 3 | `PutTeam_TeamAdminSetsThresholdToZero_PersistsZeroToDisableHighlighting` | Tier A — integration (layer 4) | `@driving_port` `@US-03` (boundary) | 0 disables highlighting (lower bound) |
| 4 | `PutTeam_ThresholdBelowRange_ReturnsBadRequest` | Tier A — integration (layer 4) | `@driving_port` `@US-03` `@error` | `[0,365]` lower-bound rejection |
| 5 | `PutTeam_ThresholdAboveRange_ReturnsBadRequest` | Tier A — integration (layer 4) | `@driving_port` `@US-03` `@error` | `[0,365]` upper-bound rejection |
| 6 | `PutTeam_NonTeamAdminSetsThreshold_ReturnsForbidden` | Tier A — integration (layer 4) | `@driving_port` `@US-03` `@error` `@rbac` | write gated by `isTeamAdmin` |
| 7 | `GetPortfolioSettings_NewlyCreatedPortfolio_ExposesDefaultStalenessThresholdOfFourteenDays` | Tier A — integration (layer 4) | `@driving_port` `@US-04` | Portfolio default 14 + exposure |
| 8 | `PutPortfolio_PortfolioAdminSetsThresholdToThirty_SettingsRoundTripsTheNewThreshold` | Tier A — integration (layer 4) | `@driving_port` `@US-04` | PUT accepts + GET round-trips |
| 9 | `PutPortfolio_PortfolioAdminSetsThresholdToZero_PersistsZeroToDisableHighlighting` | Tier A — integration (layer 4) | `@driving_port` `@US-04` (boundary) | 0 disables highlighting |
| 10 | `PutPortfolio_ThresholdBelowRange_ReturnsBadRequest` | Tier A — integration (layer 4) | `@driving_port` `@US-04` `@error` | lower-bound rejection |
| 11 | `PutPortfolio_ThresholdAboveRange_ReturnsBadRequest` | Tier A — integration (layer 4) | `@driving_port` `@US-04` `@error` | upper-bound rejection |
| 12 | `PutPortfolio_NonPortfolioAdminSetsThreshold_ReturnsForbidden` | Tier A — integration (layer 4) | `@driving_port` `@US-04` `@error` `@rbac` | write gated by `isPortfolioAdmin` |
| 13 | `GetWip_PortfolioFeatureWithStateHistory_ExposesCurrentStateEnteredAtWithinOneDay` | Tier A — integration (layer 4) | `@driving_port` `@US-04` | Portfolio Feature read exposes `currentStateEnteredAt`, rendering the persisted value within 1 day (read projection; Feature-derivation correctness deferred to the DELIVER capture layer) |
| 14 | `GetWip_FeatureFirstObservedThisSync_CurrentStateEnteredAtIsNull` | Tier A — integration (layer 4) | `@driving_port` `@US-04` `@error` | first-observed Feature → `null` → FE `—` |
| 15 | `UpdateWorkItemsForTeam_CsvConnectorWithoutHistory_StateChangesAcrossSyncs_DerivesCurrentStateEnteredAtFromSyncDelta` | Tier A — service seam (layer 4, real EF + real service) | `@real-io` `@adapter-integration` `@US-02` | CSV fallback synthesises transition + derives `CurrentStateEnteredAt` |
| 16 | `UpdateWorkItemsForTeam_CsvConnectorWithoutHistory_StateUnchangedAcrossSyncs_DoesNotSynthesiseTransition` | Tier A — service seam (layer 4) | `@real-io` `@adapter-integration` `@US-02` `@error` (idempotency) | DDD-7 — no duplicate sync-delta transition |
| 17 | `UpdateWorkItemsForTeam_LinearConnectorWithHistory_PersistsRealTransitionsAndDerivesCurrentStateEnteredAt` | Tier A — service seam (layer 4) | `@real-io` `@adapter-integration` `@US-02` | Linear real-history path derives from real timestamp (`approximate=false` semantics) |
| 18 | E2E — stale items turn red after lowering threshold, no re-sync (`test.fixme`) | Tier A — E2E (layer 6) | `@walking_skeleton` `@driving_port` `@real-io` `@US-02` | US-02 red treatment on next render |
| 19 | E2E — team admin configures threshold from team settings (`test.fixme`) | Tier A — E2E (layer 6) | `@driving_port` `@real-io` `@US-03` `@rbac` | US-03 settings field, RBAC-gated visibility |
| 20 | E2E — portfolio admin configures threshold from portfolio settings (`test.fixme`) | Tier A — E2E (layer 6) | `@driving_port` `@real-io` `@US-04` `@rbac` | US-04 settings field |
| 21 | E2E — Time in State column on the portfolio work-item view (`test.fixme`) | Tier A — E2E (layer 6) | `@walking_skeleton` `@driving_port` `@real-io` `@US-04` | portfolio column demo proof |

Error/edge ratio (backend ATs #1-17): 10 of 17 (#3,4,5,6,9,10,11,12,14,16 — boundary + error + idempotency) = **>40%**, on target. Tier B (state-machine PBT) is correctly SKIPPED: per Mandate 10 "skip when" — the threshold round-trip is a config-shaped setting (single integer, no chained ≥3-scenario state machine); the CSV/Linear seam is example-pinned at layer 4 (Mandate 11 — integration sad paths stay example-based, never PBT-generated).

## Wave: DISTILL / [REF] source → Approximate reconciliation note (slice 02 HARD reconciliation)

The slice doc (`slices/slice-02-staleness-threshold.md`, "Additional slice-level acceptance") states CSV reload "generates a new `WorkItemStateTransition` row with `source = "csv-fallback"`." This is **superseded by DESIGN** and NOT asserted:

- **DDD-1**: `WorkItemStateTransition` has exactly `FromState` / `ToState` / `TransitionedAt` + FK — no `source` column.
- **DDD-9**: the source-of-truth-vs-sync-delta distinction is surfaced solely by `WorkItemDto.Approximate: bool` at the FE badge layer; sibling consumers read transition rows uniformly.

Therefore the CSV/Linear seam ATs (#16-18) assert the **observable** contract: after a change-detecting sync, the item's `CurrentStateEnteredAt` updates and a transition into the new mapped state is captured. The fallback-vs-real distinction is expressed by which path derives the value (sync-delta timestamp → `approximate=true`; real-history timestamp → `approximate=false`), surfaced at `WorkItemDto.Approximate` (asserted in DELIVER FE unit tests + the read-API DTO, not in the seam test). No `source` field is referenced anywhere. **This is an applied reconciliation, not a live cross-wave contradiction** — DESIGN is the authority and is internally consistent; the slice-doc line is a stale shorthand from before DESIGN locked DDD-1/DDD-9.

## Wave: DISTILL / [REF] Test placement + precedent justification (slice 02)

| Artifact | Path | Precedent |
|---|---|---|
| Team threshold settings ATs (6) | `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/TeamStalenessThresholdSettingsIntegrationTest.cs` | Mirrors `ForecastFilterTeamSettingsIntegrationTest` (mock `ILicenseService`, `client.AsTeamAdmin`/`AsViewer`, `JsonDocument.Parse` settings assertions). PUT body built as `JsonNode` from a valid `TeamSettingDto` + injected `stalenessThresholdDays` so the suite compiles before the DTO property exists. |
| Portfolio threshold settings ATs (6) | `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/PortfolioStalenessThresholdSettingsIntegrationTest.cs` | Same precedent; `client.AsPortfolioAdmin`; `PortfolioController.UpdatePortfolio` carries `[LicenseGuard]` so `ILicenseService` is mocked premium as in the team file. |
| Portfolio Time-in-State read ATs (2) | `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/PortfolioTimeInStateReadApiIntegrationTest.cs` | Mirrors slice-01's `TimeInStateReadApiIntegrationTest` shape against the portfolio `/metrics/wip` (Feature) endpoint; seeds a Portfolio + in-progress `Feature`; `FeatureDto : WorkItemDto` inherits the field, but Feature-level capture is unbuilt → genuine RED. |
| CSV/Linear capture seam ATs (3) | `Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/WorkItems/WorkItemServiceTransitionFallbackIntegrationTest.cs` | Mirrors `WorkItemServiceTransitionSyncIntegrationTest` (the real connector→service→derive seam added during slice-01 bugfixing) — `IntegrationTestBase`, real `WorkItemRepository` / `WorkItemStateTransitionRepository`, mocked `IWorkTrackingConnector` (NOT mocked-in-isolation; drives the real `WorkItemService.UpdateWorkItemsForTeam`). |
| E2E walking-skeleton specs (4) | `Lighthouse.EndToEndTests/tests/specs/flow/TimeInStateAndStaleness.spec.ts` (extended) | Extends the slice-01 flow spec; `test.fixme` for all slice-02 scenarios (held for the user's live run, never committed unrun). |
| E2E POM extensions | `WorkItemsDialog.ts` (stale-badge readers), `TeamEditPage.ts` / `PortfolioEditPage.ts` (threshold field) | POM-only rule; threshold field labelled `Staleness Threshold (days)` per DESIGN; stale badge via `data-testid="time-in-state-stale"` parallel to `getFeatureHasWarning`'s testid convention. |

**Black-box / compile-safety note**: NO not-yet-existing C# symbol is referenced. `stalenessThresholdDays` is read from / injected into response JSON dynamically (`JsonDocument` / `JsonNode`); the portfolio read AT relies on the already-shipped `WorkItemDto.CurrentStateEnteredAt` (inherited by `FeatureDto`) but seeds a `Feature` whose value is never written → null today; the seam ATs reference only existing symbols (`WorkItemService`, `IWorkTrackingConnector.SupportsTransitionHistory`, `WorkItemStateTransition`, `WorkItemStateTransitionMapper`). `dotnet build -warnaserror` is clean (verified: 0 warnings) and all 17 backend ATs report **Skipped** (verified), so `dotnet test` stays green this DISTILL pass.

## Wave: DISTILL / [REF] AT files created / extended (slice 02)

- `Lighthouse.Backend.Tests/API/Integration/TeamStalenessThresholdSettingsIntegrationTest.cs` — NEW, 6 tests, `[Ignore("pending DELIVER: US-03 …")]`.
- `Lighthouse.Backend.Tests/API/Integration/PortfolioStalenessThresholdSettingsIntegrationTest.cs` — NEW, 6 tests, `[Ignore("pending DELIVER: US-04 …")]`.
- `Lighthouse.Backend.Tests/API/Integration/PortfolioTimeInStateReadApiIntegrationTest.cs` — NEW, 2 tests, `[Ignore("pending DELIVER: US-04 portfolio Time-in-State column …")]`.
- `Lighthouse.Backend.Tests/Services/Implementation/WorkItems/WorkItemServiceTransitionFallbackIntegrationTest.cs` — NEW, 3 tests, class-level `[Ignore("pending DELIVER: slice 02 CSV sync-delta fallback + Linear capture …")]`.
- `Lighthouse.EndToEndTests/tests/specs/flow/TimeInStateAndStaleness.spec.ts` — EXTENDED, 4 new `test.fixme` scenarios (slice-01 scenario unchanged).
- `Lighthouse.EndToEndTests/tests/models/metrics/WorkItemsDialog.ts` — EXTENDED (`staleTimeInStateBadgeFor`, `countStaleTimeInStateBadges`).
- `Lighthouse.EndToEndTests/tests/models/teams/TeamEditPage.ts` + `Lighthouse.EndToEndTests/tests/models/portfolios/PortfolioEditPage.ts` — EXTENDED (`stalenessThresholdField`, `setStalenessThreshold`, `getStalenessThreshold`).

DEFERRED to DELIVER (would red the build now): FE component unit tests (`TimeInStateBadge` red treatment, the two settings-form components — reference props/threshold that don't exist yet), the EF migration for `StalenessThresholdDays` (via `Create-Migration.ps1`), the `WorkItemDto.Approximate` true/false unit assertions, and the per-connector Linear GraphQL `history` parsing correctness test (DESIGN open-question — first Linear fixture in DELIVER).

## Wave: DISTILL / [REF] Pre-DELIVER fail-for-the-right-reason gate (slice 02)

Un-ignore each AT one at a time; confirm the failure is MISSING_FUNCTIONALITY, not a setup/compile/fixture error (BLOCK and fix the test otherwise):

1. **Settings ATs (#1-12).** Correct RED for GET defaults: `stalenessThresholdDays` absent from the settings JSON → `TryGetInt` returns false → assertion fires. Correct RED for PUT round-trip: GET-after-PUT still lacks the field, or the PUT silently drops the injected property. Correct RED for `@error` 400 cases: the controller has no `[0,365]` validation yet → returns 200 instead of 400. Correct RED for `@rbac` 403: already wired by the existing `[RbacGuard(TeamWrite/PortfolioWrite)]` — if these PASS while un-ignored, that is EXPECTED (RBAC is inherited, not new); keep them as regression guards. WRONG RED: `JsonNode.Parse` throws, 500 from a malformed PUT body, or a compile error.
2. **Portfolio read ATs (#13-15).** Correct RED: `currentStateEnteredAt` present-but-null on the seeded in-progress Feature (Feature capture unbuilt) → #13/#14 assertions fire; #15 (first-observed null) may PASS by accident today (value is null because capture is unbuilt) — at GREEN, confirm #15 still passes for the RIGHT reason (no transition rows for a first-observed Feature), not because capture is simply absent. WRONG RED: wip array empty (seed/`StateCategory`/`asOfDate` mismatch), 403/500.
3. **Seam ATs (#16-18).** Correct RED for #16: after the change-detecting CSV sync, `CurrentStateEnteredAt` stays null (no fallback branch) → `Is.Not.Null` fires. Correct RED for #18: Linear real-history path may already derive correctly via existing `SyncStateTransitions` — if it PASSES un-ignored, that confirms the source-of-truth path works; keep as a guard and focus DELIVER on the #16 fallback branch. #17 (idempotency) guards against the DELIVER fallback double-counting. WRONG RED: connector mock returns null, EF context not created, `MapRawStateToMappedName` throws on an unmapped state.
4. **E2E (#19-22) un-fixme LAST**, start a local app, run live, confirm RED (field/column/red-badge absent), then drive green. Per project rule + memory, never commit a Playwright spec not RUN locally against a started app. The premium-license dev seed (memory `reference_premium_license_dev_seed`) may be needed if any portfolio PUT path hits the `[LicenseGuard]` in the running app.
5. **Fixture Theater check**: when each AT flips to GREEN, `git diff --stat` must show production files changed (DTO/base-class/controller/service/migration). Settings ATs that go green with only test-file changes mean the PUT-body injection is being echoed by the seed, not persisted — BLOCK.

## Wave: DISTILL / [REF] Reconciliation + infrastructure policy notes (slice 02)

- **Wave-decision reconciliation: PASSED — 0 live contradictions.** DISCUSS (US-02/03/04 + D1/D7/D8) and DESIGN (DDD-1/3/5/7/8/9) agree. The slice-doc `source="csv-fallback"` line is a stale pre-DESIGN shorthand, superseded by DDD-1/DDD-9 (applied as a HARD reconciliation above), not a cross-wave contradiction. No `devops/` delta exists — default environment assumptions apply (warn, not block).
- **Adapter coverage (Mandate 6):** CSV sync-delta fallback and Linear source-of-truth capture each get a real-service-seam AT (#16-18) driving the real `WorkItemService` + real EF repositories (the level slice-01 bugfixing proved necessary — mocked-connector-in-isolation missed the `WorkItemBase→WorkItem` + derive wiring). Per-connector GraphQL/CSV-parser correctness (Linear `history` field availability) is the DESIGN-flagged DELIVER open question.
- **Infrastructure policy:** unchanged from slice 01 — C#/.NET + React/Playwright, not the Python pilot. Backend ATs use `WebApplicationFactory<Program>` (driving) + real EF context (driven internal) + mocked `IWorkTrackingConnector`/`ILicenseService` (driven external). E2E uses the production React app + live connector sync. NUnit `[Ignore]` / Playwright `test.fixme` are the C#-row / TS-row skip markers per the polyglot matrix.

