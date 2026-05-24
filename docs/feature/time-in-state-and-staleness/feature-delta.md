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
