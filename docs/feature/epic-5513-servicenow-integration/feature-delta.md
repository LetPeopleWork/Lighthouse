# Feature: ServiceNow Integration (Epic 5513)

**Epic**: ADO 5513 — "ServiceNow Integration" (Community / Release Notes)
**Feature id**: `epic-5513-servicenow-integration`
**Wave**: DISCUSS complete (2026-07-27) → **SPIKE required before DESIGN**
**Density**: lean (Tier-1 [REF] only; expansions on demand via `--expand <id>`)

> **Read this first.** This is a *viability* epic, not a parity epic. The epic body states plainly:
> "We don't know SNOW at all, so we don't have much of an idea how it's normally used." Every
> technology statement below is marked as either **[verified]** (checked against the Lighthouse
> codebase, or against the maintainer's own ServiceNow instance) or **[hypothesis]** (desk research,
> to be proved or killed by the SPIKE). No slice is committed until the SPIKE closes its questions.
>
> **Two premises were closed by the maintainer on 2026-07-27, before the SPIKE ran:** basic auth works
> on the target on-prem instance (D3), and ITSM is the default data model to optimise for (D4). Both
> are first-hand evidence and outrank the desk research they replace.

---

## Wave: DISCUSS / [REF] Pre-requisites

- **A connector port already exists** [verified]. `IWorkTrackingConnector` (8 methods:
  `SupportsTransitionHistory`, `GetPredefinedAdditionalFields`, `GetWorkItemsForTeam`,
  `GetFeaturesForProject`, `GetParentFeaturesDetails`, `ValidateConnection`, `ValidateTeamSettings`,
  `ValidatePortfolioSettings`, `WriteFieldsToWorkItems`) is the whole contract a new work-tracking
  system must satisfy. Four implementations exist: `AzureDevOps`, `Jira`, `Linear`, `Csv`.
- **Adding a system is a known, bounded shape** [verified]. `WorkTrackingSystems` enum +
  `AuthenticationMethodKeys` entry + `IWorkTrackingAuthStrategy` + connector class +
  frontend `DataRetrievalSchemaDefaults` entry + optional `DataRetrievalWizardRegistry` wizard.
  The frontend connection/settings surface is **schema-driven** — a new system needs configuration,
  not new React screens, for the baseline case.
- **Linear is the reference class, not Jira** [verified]. Linear is the most recent addition, uses a
  non-REST API (GraphQL), has no work-item-types requirement (`isWorkItemTypesRequired=false`),
  declares `SupportsTransitionHistory` dynamically with a runtime `DowngradeHistorySupport()` path,
  and returns `WriteBackResult` unsupported. Every one of those traits is likely to recur for SNOW.
- **Demo-data generators are a per-system artifact** [verified]. `Scripts/DemoEnv/` holds
  `ADOSystemUpdater.py`, `JiraSystemUpdater.py`, `LinearSystemUpdater.py`. A
  `ServiceNowSystemUpdater.py` is the matching deliverable, and the user has confirmed a cloud
  instance can be self-provisioned for it.
- **OAuth infrastructure exists but is user-delegated** [verified]. ADR-007 (provider registry),
  ADR-008 (credential separation), ADR-009 (base-URL callback), ADR-010 (single-flight refresh),
  ADR-011 (popup flow) all assume an *authorization-code* flow with a human at a browser. A
  service-to-service **client-credentials** flow is not currently a registered pattern. Not a v1
  concern — v1 is basic auth (D3) — but it is the shape the successor OAuth work will need (D3a).
- **No prior wave artifacts.** No DISCOVER, no DIVERGE, no `docs/product/vision.md`, no
  `docs/project-brief.md`, no `docs/stakeholders.yaml`. jobs.yaml has **no** job for "connect a new
  work-tracking system" — this feature bootstraps that job family.

## Wave: DISCUSS / [REF] Personas

No new persona. Four existing personas carry this feature; a SNOW shop is the same set of humans on a
different toolchain.

| Persona ID | One-line identifier |
|---|---|
| `config-admin` | Owns the Connection. Creates it, validates it, and lives with whatever rights their SNOW admin will grant — **primary**. |
| `flow-coach` | The reason the connection exists: wants cycle time / throughput / age on SNOW-tracked work without exporting to a spreadsheet — **primary**. |
| `delivery-lead-rte` | Wants a Portfolio over SNOW parents — **conditional**, only if the SPIKE finds a usable hierarchy (US-04). |
| `lighthouse-maintainer` | Owns the *viability verdict*: does the SNOW market justify further investment? This epic's stated outcome is theirs — **primary for US-06**. |

## Wave: DISCUSS / [REF] JTBD one-liners

- **job-snow-admin-connect-servicenow** — *When my team's work lives in ServiceNow and I have only the
  rights my platform team will grant me, I want to point Lighthouse at my instance and be told plainly
  whether the credential actually works, so I can start without escalating for admin rights or
  guessing why a sync is empty.*
- **job-snow-flow-coach-see-flow-metrics** — *When my team's work is ServiceNow records rather than Jira
  issues, I want the same cycle time, throughput, age and forecast Lighthouse gives every other team,
  so my tool choice stops deciding whether I can measure flow.*
- **job-snow-delivery-lead-portfolio-over-servicenow** — *When my ServiceNow work rolls up to a parent
  record, I want a Lighthouse Portfolio over those parents, so I can forecast a delivery instead of
  only reading one team's throughput.* (**conditional on SPIKE finding a hierarchy**)
- **job-maintainer-learn-servicenow-viability** — *When I am deciding whether ServiceNow is a market
  worth building for, I want a real integration in front of real ServiceNow users and their feedback
  in my hands, so I invest based on evidence rather than on a hypothesis about an untapped market.*

The fourth job is deliberately a *maintainer* job, not a user job. This epic's stated expected outcome
is "we know more about the viability" — pretending that is a user feature would hide the actual bet.

## Wave: DISCUSS / [REF] Locked Decisions

| ID | Decision | Verdict | Source |
|---|---|---|---|
| D1 | Feature type | **Cross-cutting** — backend connector + auth strategy + FE schema/wizard config + docs + demo-data script + E2E. | User (AskUserQuestion, 2026-07-27) |
| D2 | Walking skeleton | **Yes — slice 01 is "connect and validate a ServiceNow connection"**, slice 02 is the first metric. WS strategy **A** (thinnest end-to-end vertical). | User |
| D3 | Auth method for v1 | **Basic auth — confirmed by direct evidence on the target on-prem instance.** The maintainer has verified basic auth works there today, whereas OAuth would need instance-side admin setup they do not have. OAuth is the long-term answer and a named successor item; it is **not** the v1 gate. | User (2026-07-27), verified against the real on-prem instance — outranks the desk research |
| D3a | OAuth deferral is a recorded risk, not an oversight | ServiceNow has been tightening inbound Basic Authentication platform-wide [hypothesis]. That is a *future* constraint, not a present blocker, and the mitigation is already named: OAuth 2.0 client credentials as the successor method. Two consequences: (a) the docs (US-05) state plainly that v1 is basic auth, so a customer whose instance already blocks it finds out before installing rather than after; (b) OAuth's own setup cost — an instance-side application-registry entry made by *someone else* — is an adoption barrier in its own right, which is exactly why it is not the v1 choice. | Desk research, downgraded from blocker to risk |
| D4 | What counts as a "work item" in SNOW | **ITSM is the default and the optimisation target** — `task`-derived tables (`incident`, `change_request`, `sc_task`, `sc_req_item`). The table name stays **configurable** so an Agile Development 2.0 shop (`rm_epic`→`rm_story`, plugin `com.snc.agile_dev`, not installed by default) is not locked out, but ITSM is what the field mapping, demo data, docs and worked examples are built around. | User (2026-07-27) |
| D5 | Query model | **Reuse the existing per-system "query" concept** — SNOW's Table API takes a `sysparm_query` encoded query [hypothesis], which is the same shape as WIQL (ADO) and JQL (Jira): an opaque, user-authored filter string Lighthouse passes through. No new UX concept. | Desk research + connector precedent |
| D6 | Transition history in v1 | **Declare `SupportsTransitionHistory = false` by default and degrade honestly.** The Linear `DowngradeHistorySupport()` precedent [verified] is the pattern. Upgrading to real history is slice 04, and only if the SPIKE finds an affordable, read-only-role-accessible source. | Precedent + SPIKE gate |
| D7 | Portfolio / hierarchy | **Conditional slice.** Epic body already sanctions the fallback: "If not, we may start with team only". The SPIKE decides; slice 03 is written but not committed until it does. | Epic body |
| D8 | Write-back | **Out of scope this epic.** `WriteFieldsToWorkItems` returns an explicit unsupported result, as Linear does [verified]. Read-only integration only. | Scope discipline |
| D9 | Licensing | **Free tier, no premium gate.** Existing connectors are not gated by system; gating a whole work-tracking system would contradict that and would also block the viability signal this epic exists to collect. | Precedent + epic outcome |
| D10 | Primary target environment | **Cloud (self-provisioned developer instance) is the build-and-test target.** On-prem is the *validation fallback*, exercised once the integration is in decent shape, via scripts the user can run under a restricted account. | User (AskUserQuestion, 2026-07-27) |
| D11 | Least privilege is a first-class requirement, not a nicety | The user's limited on-prem access is treated as **a feature of the design brief**: whatever role set the integration needs must be documented and proved sufficient with a restricted account. Discovering the minimum viable rights *is* part of the epic's value. | User |

## Wave: DISCUSS / [REF] Scope Assessment: SPLIT (oversized — 5 of 5 signals fired)

Oversized signals, all present: (1) >3 distinct unknowns each capable of invalidating the design;
(2) touches backend connector + auth + frontend schema + docs + demo tooling + E2E; (3) walking
skeleton needs a live external system that does not yet exist; (4) effort clearly >2 weeks;
(5) multiple independent outcomes that can ship separately (connect / team metrics / portfolio /
history / proof-on-customer-instance).

**Split into one mandatory SPIKE + 5 elephant-carpaccio slices.** The SPIKE is *not* a slice — it
ships no production code and therefore cannot satisfy the value gate. It is a gate in front of the
slices, per `docs/feature/epic-5513-servicenow-integration/spike-questions.md`.

### Carpaccio taste tests

| Test | Result |
|---|---|
| Any slice shipping 4+ new components? | Pass — slice 01 ships connector skeleton + auth strategy + enum/schema entries only; the connector's read methods arrive in 02. |
| Every slice depends on a new abstraction? | Pass — the abstraction (`IWorkTrackingConnector`) already exists and is shipped four times over. Nothing new to invent. |
| Does any slice disprove a pre-commitment? | Pass — 01 disproves "a non-admin ServiceNow account can validate a connection"; 02 disproves "ITSM records map onto our team+query+state-mapping concepts"; 03 disproves "ITSM work rolls up to something forecastable"; 04 disproves "state history is affordably readable"; 05 disproves "a restricted account is sufficient end to end". |
| Any slice on synthetic data only? | Pass — every slice acceptance runs against a live ServiceNow instance; `ServiceNowSystemUpdater.py` seeds *real records in a real instance*, not fixtures. |
| Any two slices identical except scale? | Pass — no duplicates. |

## Wave: DISCUSS / [REF] Story Map + WS strategy

**Backbone**: Connect an instance → Point a Team at SNOW work → Read flow metrics & forecast →
Roll up to a Portfolio → Trust the time-in-state numbers → Prove it on a customer's real instance.

**WS strategy = A (thinnest end-to-end vertical).** Slice 01 walks connection→validation→green tick;
slice 02 extends the same vertical to the first rendered metric.

| Slice | Story | Type | Ships | Gate |
|---|---|---|---|---|
| — | **SPIKE** | *no code* | Answers to 9 questions; kills or confirms D3/D4/D6/D7 | **blocks all slices** |
| 01 | US-01 Connect and validate a ServiceNow instance | value | enum + **basic-auth** strategy + connector skeleton + FE schema entry + `ValidateConnection` | SPIKE Q8 only (Q1 closed by D3) |
| 02 | US-02 A team's ServiceNow work becomes flow metrics and a forecast | value | `GetWorkItemsForTeam` over an **ITSM `task`-derived table** + `ValidateTeamSettings` + state mapping + `SupportsTransitionHistory=false` honest downgrade | 01 |
| 03 | US-03 A portfolio over ServiceNow parents | value | `GetFeaturesForProject` + `GetParentFeaturesDetails` + `ValidatePortfolioSettings` | 02 + SPIKE says hierarchy exists |
| 04 | US-04 Time-in-state on ServiceNow work | value | transition history source + `SupportsTransitionHistory=true` | 02 + SPIKE says history is affordable |
| 05 | US-05 A ServiceNow admin self-serves from the docs | value | docs page + `ServiceNowSystemUpdater.py` + screenshots + minimum-role documentation | 02 |
| 05 | US-06 The maintainer gets a viability verdict | value | on-prem restricted-account validation run + structured feedback from ≥3 SNOW users | 05 |

Slice briefs: `docs/feature/epic-5513-servicenow-integration/slices/slice-0{1..5}-*.md`.

**Prioritisation rationale (learning leverage first, not dependency-order-only):**
- **SPIKE first** — D3 (auth) and D4 (data model) are now closed by the maintainer's first-hand evidence, so the SPIKE is materially *shorter* than first scoped. D6/D7 and the ITSM field mapping still gate every downstream estimate.
- **01 before 02** — with the protocol settled, slice 01's remaining risk is **rights, not auth**: can a non-admin account validate and read? Still the cheapest place to find out, and still the likeliest blocker to adoption at a real customer.
- 02 before 03/04 — team-level metrics is the epic's minimum shippable value ("we may start with team only").
- 03 before 04 — hierarchy determines whether Lighthouse's headline capability (portfolio forecasting) is reachable at all on SNOW; transition history only affects secondary widgets, and D6 already gives an honest fallback.
- 05 last but **not deferred to `/release`** — docs, screenshots and the demo-data script are per-feature finalisation work, and US-06's feedback loop cannot start until a SNOW user can actually follow a page.

## Wave: DISCUSS / [REF] User Stories

### US-01 — Connect and validate a ServiceNow instance
`job_id: job-snow-admin-connect-servicenow` · slice 01 · **value**

As a configuration administrator in a ServiceNow shop, I want to create a ServiceNow connection in
Lighthouse using the credential my platform team will actually give me, and be told immediately
whether it works, so I find out about a permissions problem on the settings page instead of from an
empty team a week later.

#### Elevator Pitch
Before: ServiceNow is not in Lighthouse's connection list at all — a SNOW shop cannot start.
After: open **Settings → Work Tracking Systems → New Connection → ServiceNow**, enter instance URL and credential, click **Validate** → see a green "Connection valid" or a specific failure naming what the credential cannot do.
Decision enabled: decide whether the account I was granted is sufficient, or whether I need to go back to my platform team with a named missing right — before configuring any team.

#### Acceptance Criteria
- AC1: "ServiceNow" appears as a selectable system in the connection-creation wizard alongside Azure DevOps, Jira, Linear and CSV.
- AC2: The connection form renders the SNOW option set (instance base URL + basic-auth username/password, per D3) from the schema-driven configuration — no bespoke React screen.
- AC3: Clicking Validate against a reachable instance with a sufficient credential returns a success result.
- AC4: Validate against an unreachable host, a wrong credential, and a **reachable instance where the account lacks read access to the configured table** each return three *distinguishable* failure messages. A permissions failure must never be reported as a connection failure. *(This AC is the one D11 exists for.)*
- AC5: The credential is stored using the existing encrypted connection-option mechanism; it is never returned to the client in plaintext on reload.

---

### US-02 — A team's ServiceNow work becomes flow metrics and a forecast
`job_id: job-snow-flow-coach-see-flow-metrics` · slice 02 · **value**

As a flow coach whose team tracks work in ServiceNow, I want to point a Lighthouse team at a
ServiceNow query and see throughput, cycle time and a forecast, so my team gets the same flow
diagnosis as the Jira teams next door.

#### Elevator Pitch
Before: my team's work is in ServiceNow, so Lighthouse gives me nothing — I export to a spreadsheet or go without.
After: create a Team with a ServiceNow connection, paste a ServiceNow query into the team's query field, hit **Refresh** → the team's Throughput and Cycle Time charts render and the "How many items by date" forecast returns percentiles.
Decision enabled: decide what my team can commit to for the next two weeks from our own historical throughput, instead of from a guess.

#### Acceptance Criteria
- AC1: A Team bound to a ServiceNow connection accepts a query string and syncs matching records into Lighthouse work items. The work-item table defaults to an **ITSM `task`-derived table** (D4) and is configurable.
- AC2: Each synced record maps to a Lighthouse work item with id, title, type, state, and the dates needed for cycle time (started / closed), derived from the SNOW record's fields.
- AC3: The team's Doing/Done state mapping is configured through the **existing** team-settings state mapping UI. ServiceNow states are commonly **numeric choice values with a separate display label** — the mapping UI must show the label the user recognises ("In Progress"), never the raw integer.
- AC4: Throughput, Cycle Time and the "How many" / "When" forecasts render for the team from synced data.
- AC5: `SupportsTransitionHistory` returns **false** for ServiceNow connections (D6), and every widget that requires transition history degrades to its documented unsupported state — no blank chart, no crash, no silently-wrong number.
- AC6: `ValidateTeamSettings` reports a bad query or an unresolvable table as a specific, actionable message on the team settings page.
- AC7: Pagination is honoured — a query matching more records than one page returns all of them.

---

### US-03 — A portfolio over ServiceNow parents
`job_id: job-snow-delivery-lead-portfolio-over-servicenow` · slice 03 · **value** · **conditional on SPIKE**

As a delivery lead, I want a Lighthouse Portfolio whose features are ServiceNow parent records, so I
can forecast a delivery date rather than only reading one team's throughput.

#### Elevator Pitch
Before: even with teams syncing, ServiceNow work stops at team level — no portfolio, no delivery forecast.
After: create a Portfolio on a ServiceNow connection with a parent query → the portfolio lists features with their child-item counts and a forecasted completion date per feature.
Decision enabled: decide whether a delivery is on track, and which feature is the one at risk.

#### Acceptance Criteria
- AC1: A Portfolio bound to a ServiceNow connection syncs parent records as Lighthouse features.
- AC2: Child work items resolve to their parent via the hierarchy relationship the SPIKE identified, so each feature reports remaining/total work.
- AC3: Feature forecasts render from contributing teams' throughput.
- AC4: `ValidatePortfolioSettings` gives an actionable message when the parent query or the relationship field is wrong.
- AC5: **If the SPIKE finds no usable hierarchy**, this story is cancelled — not silently deferred — and the docs (US-05) state explicitly that ServiceNow support is team-scope only, so a prospect learns the limit before they install rather than after.

---

### US-04 — Time-in-state on ServiceNow work
`job_id: job-snow-flow-coach-see-flow-metrics` · slice 04 · **value** · **conditional on SPIKE**

As a flow coach, I want the time-in-state, staleness and percentile widgets to work on ServiceNow
work, so I get the same "where is time being spent?" diagnosis my Jira colleagues get.

#### Elevator Pitch
Before: ServiceNow teams see throughput and cycle time, but every time-in-state widget reads "not supported for this system".
After: open **Team → Metrics** on a ServiceNow team → the Cumulative State Time and per-state widgets render real per-state durations.
Decision enabled: decide which workflow state is actually eating the team's cycle time, instead of only knowing the total.

#### Acceptance Criteria
- AC1: `SupportsTransitionHistory` returns true for ServiceNow connections whose configuration supports it.
- AC2: State transitions are captured per work item and mapped through `WorkItemStateTransitionMapper` like every other system's.
- AC3: Cumulative State Time, per-state percentiles and staleness render on a ServiceNow team.
- AC4: If history is unavailable **at runtime** for a given instance (source not readable with the granted rights), the connector downgrades at runtime rather than erroring — the Linear `DowngradeHistorySupport()` precedent.
- AC5: The extra history queries do not push a normal team refresh beyond the existing refresh-duration expectations; if they do, the feature ships behind an opt-in team setting rather than on by default.

---

### US-05 — A ServiceNow admin self-serves from the docs
`job_id: job-snow-admin-connect-servicenow` · slice 05 · **value**

As a ServiceNow administrator evaluating Lighthouse, I want a documentation page that tells me exactly
which instance settings, roles and query to use, so I can connect without a call and without over-granting.

#### Elevator Pitch
Before: nothing in the docs mentions ServiceNow; a prospect has to ask, or guess and over-grant an admin role.
After: open **docs → Concepts → Work Tracking Systems → ServiceNow** → follow a page that names the exact minimum role set, the auth setup steps, and a worked example query, with screenshots.
Decision enabled: decide whether Lighthouse can work inside my organisation's security posture — before I ask anyone for access.

#### Acceptance Criteria
- AC1: A ServiceNow docs page exists under the work-tracking-systems docs, matching the structure of the Jira/ADO/Linear pages.
- AC2: The page names the **minimum** ServiceNow role/ACL set the integration needs, proved sufficient (US-06 AC2), not the roles that merely happened to work for an admin account.
- AC3: Screenshots are generated by an `@screenshot` E2E per theme (per project convention: `rm` the old PNG first).
- AC4: `Scripts/DemoEnv/ServiceNowSystemUpdater.py` — created minimally by the environment prereq — is brought to the same shape the ADO/Jira/Linear updaters produce, so demo and screenshot environments are reproducible.
- AC5: If US-03 was cancelled, the page states the team-only limitation prominently (US-03 AC5).

---

### US-06 — The maintainer gets a viability verdict
`job_id: job-maintainer-learn-servicenow-viability` · slice 05 · **value**

As the Lighthouse maintainer, I want the integration exercised on a real customer's on-prem instance
under a restricted account, and structured feedback from real ServiceNow users, so I decide whether to
invest further based on evidence.

#### Elevator Pitch
Before: "ServiceNow is a huge untapped market" is a hypothesis with no evidence attached to it.
After: run the documented validation script against the on-prem customer instance under a restricted account → get a pass/fail per capability, plus written feedback from ≥3 ServiceNow users on what works and what is missing.
Decision enabled: decide whether Epic 5513 gets a successor epic, a narrowing, or a stop.

#### Acceptance Criteria
- AC1: A repeatable validation script/checklist runs against an instance the maintainer does **not** administer, exercising connect → team sync → metrics, and reports pass/fail per capability. It must be runnable standalone by someone with limited rights and no Lighthouse build (D10 — building Lighthouse on the on-prem side is cumbersome).
- AC2: The minimum-role claim in the docs (US-05 AC2) is confirmed on that instance, or the docs are corrected to what actually worked.
- AC3: Cloud-vs-on-prem behavioural differences are recorded — API version, auth acceptance, table availability, plugin presence — as a written list, even if the list is empty.
- AC4: Structured feedback is collected from ≥3 ServiceNow users/prospects and written up as a go / narrow / stop recommendation.
- AC5: The recommendation is recorded on ADO 5513 and, if it is "go", the successor epic's scope is named.

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement |
|---|---|---|
| Time to first metric | ≤15 min from "create connection" to a rendered Throughput chart, for someone who has never connected Lighthouse before | Timed dogfood run on the cloud instance + repeated on the on-prem instance (US-06 AC1) |
| Least-privilege proof | The documented minimum role set is sufficient — **0** capabilities require an elevated/admin SNOW role | US-06 AC2, run under the restricted on-prem account |
| Honest capability reporting | **0** silent no-ops: each of the 8 `IWorkTrackingConnector` methods either works or reports a user-visible unsupported state | Backend integration tests + AC-level assertions (US-01 AC4, US-02 AC5, US-03 AC5) |
| Market signal | ≥3 ServiceNow users/prospects give structured feedback within 30 days of the release note | ADO 5513 `Community` tag + Slack/community thread; written up per US-06 AC4 |
| Verdict recorded | A go / narrow / stop recommendation exists on ADO 5513 within 30 days of shipping slice 05 | ADO work item |
| Mutation kill rate | ≥80% backend + frontend on new connector/auth code | Stryker.NET + Stryker, per-feature |

## Wave: DISCUSS / [REF] Definition of Done

1. All non-cancelled user stories' ACs pass. Cancelled conditional stories (US-03, US-04) are recorded as cancelled **with the SPIKE finding that cancelled them** — never silently dropped.
2. `dotnet build` zero warnings; `dotnet test` green; `pnpm test` / `pnpm build` / Biome clean.
3. Any EF migration is additive / expand-only, generated via the `CreateMigration` script across all providers.
4. Mutation testing ≥80% BE + FE on new code (per-feature, not deferred).
5. Every unsupported capability is *declared* and user-visible; no silent no-op (KPI 3).
6. E2E: one walking-skeleton spec covering connect → team sync → metric, driven from demo data, per the project's thin-sanity-check E2E principle. No team↔portfolio twin specs.
7. `Scripts/DemoEnv/ServiceNowSystemUpdater.py` exists and is documented alongside its three siblings.
8. Docs page + per-feature screenshots at feature finalisation (one `@screenshot` per theme; `rm` the old PNG first). Lighthouse-Clients CLI/MCP versioning: **N/A, because** this epic adds no client-facing contract change — connectors are server-side and the clients address teams/portfolios generically. Website marketing surface: **in scope** — a new supported system is a marketing claim; confirm with the maintainer before publishing.
9. SonarCloud gate: no new issues. ADO 5513 children mirrored + state-transitioned; the epic carries the `Release Notes` tag already.

## Wave: DISCUSS / [REF] Out of scope

- **Write-back to ServiceNow** (D8) — `WriteFieldsToWorkItems` reports unsupported.
- **OAuth 2.0** — v1 is basic auth (D3). OAuth is the named successor (D3a), not this epic.
- **Instance-side OAuth application registration** — a setup step performed by someone the Lighthouse user usually is not; part of why OAuth is deferred rather than a nicety we skipped.
- **ServiceNow as an inbound/webhook source** — Lighthouse polls; no SNOW-side business rules, flows or scripted REST endpoints are installed on the customer's instance. Requiring instance-side configuration would defeat D11.
- **Supporting both the Agile 2.0 and ITSM data models with bespoke logic** — D4 makes the table configurable; Lighthouse does not model SNOW's applications.
- **Premium gating** (D9).
- **Instance-side plugin installation** (e.g. asking a customer to activate `com.snc.agile_dev`) as a prerequisite for basic team metrics.
- **The successor epic** — this epic ends at a recommendation (US-06), not at parity.

## Wave: DISCUSS / [REF] Driving Ports (inbound surfaces)

- **UI actions**: Settings → Work Tracking Systems → New Connection → *ServiceNow* (wizard); Validate button on connection settings; Team settings query + state mapping; Portfolio settings parent query (US-03).
- **HTTP**: existing connection / team / portfolio settings and validation endpoints — no new routes. A new system is data, not a new controller surface.
- **Refresh pipeline (inbound trigger)**: the existing team/portfolio update queue drives `GetWorkItemsForTeam` / `GetFeaturesForProject` — no new scheduler.
- **Script (out-of-band)**: `Scripts/DemoEnv/ServiceNowSystemUpdater.py` (demo seeding) and the US-06 standalone validation script — the latter must run without a Lighthouse build (D10).

## Wave: DISCUSS / [REF] Pre-SPIKE gate

DESIGN **must not start** before the SPIKE closes. Question set:
`docs/feature/epic-5513-servicenow-integration/spike-questions.md` (9 questions, each with a named
decision it unblocks and an explicit "what we do if the answer is no").

**Environment prerequisite, ahead of the SPIKE**: provision a ServiceNow cloud Personal Developer
Instance and stand up a minimal `Scripts/DemoEnv/ServiceNowSystemUpdater.py` that seeds and churns
ITSM records against it. This is a *separate work item*, not part of the SPIKE timebox — PDI signup
carries no learning, and a signup problem is an account problem rather than a finding about the API.

It pays for itself three times over, which is why it is a story rather than a checklist line:
1. **Keeps the instance alive.** PDIs hibernate on inactivity and are eventually reclaimed. A
   scheduled seeder run is a cleaner guard than remembering to log in.
2. **Front-loads SPIKE evidence.** Creating and transitioning `incident` records over basic auth is
   the first real Table API traffic, and it answers part of Q2 (field mapping) and Q4 (which
   timestamps actually get populated when a record moves) as a by-product rather than a duplicate.
3. **It is the SPIKE's test data.** Every other question needs records to query.

**Ownership split with slice 05** (stated to avoid two work items claiming the same file): the prereq
creates the *minimal* seeder — enough ITSM records, in enough states, to answer the SPIKE. Slice 05
brings it to parity with `ADOSystemUpdater.py` / `JiraSystemUpdater.py` / `LinearSystemUpdater.py`
and documents it (US-05 AC4).

## Wave: DISCUSS / [REF] DoR Validation

| # | DoR item | Status |
|---|---|---|
| 1 | Job traceability | ✓ every story → real `job_id` (4 jobs added to `jobs.yaml`); no `infrastructure-only` escape used |
| 2 | Elevator pitch per value story | ✓ all 6 stories are value stories, all 6 have Before/After/Decision-enabled |
| 3 | Testable ACs | ⚠ **conditionally** — US-01 is now fully specified (D3 names the auth fields). US-03/US-04 ACs are testable but their *existence* is still gated on SPIKE Q5/Q6. Smaller residual than at first draft. |
| 4 | Personas defined | ✓ config-admin, flow-coach, delivery-lead-rte, lighthouse-maintainer — all pre-existing, extended with the new jobs |
| 5 | Journey mapped | ✓ `docs/product/journeys/epic-5513-servicenow-integration.yaml` |
| 6 | Slices ≤1 day, learning hypothesis each | ⚠ **5 briefs written, but slice 02 is the honest exception** — a first read-path against an unknown API is not a ≤1-day slice until the SPIKE has already made the calls by hand. The SPIKE deliberately absorbs that discovery so slice 02 becomes ≤1 day of *implementation*. Documented, not hidden. |
| 7 | Outcome KPIs numeric | ✓ 6 KPIs with targets and measurement methods |
| 8 | Out-of-scope explicit | ✓ 7 explicit non-goals |
| 9 | No silent N/A | ✓ RBAC impact: **N/A, because** connectors carry no new authorization surface — connection CRUD is already RBAC-gated and unchanged. Clients CLI/MCP versioning: **N/A, because** no client-facing contract changes (see DoD 8). Website marketing surface: **NOT N/A — in scope**, a new supported system is a public claim. |

**Requirements completeness: 0.93 — below the 0.95 gate, deliberately and transparently.** The residual
is not vagueness in what we want. Of the four originally-named unknowns, **two are now closed by the
maintainer's first-hand evidence** (D3 auth = basic; D4 table model = ITSM-first, configurable). Two
remain — D6 (history source) and D7 (hierarchy) — and no amount of requirements work closes them from
this side of the API. The SPIKE is the instrument. Expect >0.95 at SPIKE exit, and treat the SPIKE
report as a required amendment to this document before DESIGN.

## Wave: DISCUSS / [REF] Changed Assumptions

Two additions to the epic body, one of which reverses an earlier draft of this document.

### 1. Basic auth: challenged, then CONFIRMED by first-hand evidence

> **Original (ADO 5513 description, verbatim):** "We can build a test instance online (if available for
> free) and support one mean of auth (e.g. basic auth) - others like OAuth we could add later."

An earlier draft of this document put this premise **at risk**, on desk research showing ServiceNow
tightening inbound Basic Authentication in favour of OAuth 2.0 client credentials, and deferred the
auth choice to the SPIKE.

**That challenge is withdrawn.** The maintainer checked the actual target: basic auth works on the
on-prem customer instance today, and OAuth there would require instance-side admin setup they do not
have access to. First-hand evidence from the deciding environment beats desk research about the
platform in general, so **D3 locks basic auth for v1** and the epic body stands as written.

**What survives** is a smaller, real point, recorded as D3a rather than as a blocker: the restriction
trend is genuine, so OAuth is a named successor rather than an optional nicety, and US-05's docs must
say plainly that v1 is basic auth — a customer whose instance *already* blocks it should learn that
before installing. Note the second-order finding this surfaced: OAuth's setup cost (an instance-side
application-registry entry made by someone other than the Lighthouse user) is itself an adoption
barrier, which reinforces D11 rather than arguing against the deferral.

### 2. ITSM is the default data model — new, not in the epic body

> **Original (ADO 5513 description, verbatim):** "To be seen how we connect, how the 'query' works (if
> at all), if there is a concept like boards and states…"

**New assumption**: ServiceNow customers are assumed to be tracking work in **ITSM** (`task`-derived
tables: `incident`, `change_request`, `sc_task`, `sc_req_item`), not in Agile Development 2.0
(`rm_story`, a plugin that is not installed by default). Field mapping, demo data, docs and worked
examples optimise for ITSM; the table name stays configurable so an Agile 2.0 shop is not locked out.

**Rationale**: the epic left "what is a work item" entirely open. Optimising for the wrong model would
mean building the state mapping, demo generator and docs against tables most prospects do not use.
Consequence to carry into DESIGN: an ITSM-first read changes the vocabulary (tickets, not stories),
makes `On Hold` a natural blocked-state candidate, and weakens the hierarchy story — ITSM's rollup
concept is less obvious than `rm_epic`, so US-03 is now *more* likely to be cancelled, not less.

**The epic body is not edited.** This section is the record, per the back-propagation contract.

## Wave: DISCUSS / [REF] Wave Decisions Summary

- **Primary need**: let ServiceNow shops get the flow metrics and forecasts every other Lighthouse user gets — and, for the maintainer, find out whether that market is real.
- **Feature type**: cross-cutting (D1).
- **Walking skeleton**: connect + validate (D2), because auth is the likeliest thing to make the epic infeasible and the cheapest place to find that out.
- **Constraints**: read-only (D8); **basic auth** as the single v1 method, OAuth as named successor (D3/D3a); **ITSM-first** but configurable work-item table (D4); honest capability downgrade over silent gaps (D6); least privilege as a design requirement, not a nicety (D11); cloud primary / on-prem as the validation fallback (D10).
- **Upstream changes**: no DISCOVER/DIVERGE existed. Basic auth was challenged on desk research, then **confirmed** by the maintainer testing the actual on-prem instance; ITSM-as-default is a new assumption the epic body did not carry. Both recorded in Changed Assumptions.

## Next Wave

**SPIKE first** (`/nw-spike`) against `spike-questions.md` — mandatory gate, user-confirmed.
Then **DESIGN** (`nw-solution-architect`) + **DEVOPS** (`nw-platform-architect`, KPIs only).

Key DESIGN questions: whether the basic-auth strategy is a new class or reuses the shape of
`JiraCloudBasicAuthStrategy` (which is also username+token over Basic); whether the SNOW connector is
one class or splits cloud/on-prem; how the configurable ITSM table name (D4) threads through
connection vs team vs portfolio option scopes, and where its default lives; how numeric state choice
values are surfaced as readable labels in the existing state-mapping UI (US-02 AC3); and — downstream
of the SPIKE — where the transition-history source (D6/US-04) sits relative to
`WorkItemStateTransitionMapper`.

Deliberately **not** a DESIGN question this epic: the OAuth client-credentials shape (D3a). It is
successor work; designing it now would be speculative generality.
