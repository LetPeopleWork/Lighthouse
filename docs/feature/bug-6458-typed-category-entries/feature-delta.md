# Bug 6458 (a): category lists cannot tell a raw state from a mapping name

ADO Bug #6458 (Active). Part (b) of the same item, the autosave of an unfinished mapping, shipped on its own
as `docs/feature/bug-6458-empty-mapping-autosave/` (commits `bfd39ec97`, `be3adace6`). This workspace is
part (a) only.

## Where we stopped (2026-10-31)

- **RCA done**, in `rca.md` here. The maintainer agreed both root causes and the split:
  part (a) is a design flaw (public API values, a data rewrite of stored lists, connector expansion sites,
  the settings editor), so it goes through **DESIGN** before any fix. No further frontend guard.
- **DESIGN is done (2026-11-01).** Shape (ii-b) was chosen; see "Wave: DESIGN" below and ADR-270.
  **DEVOPS is done (2026-11-01, AFK):** usage-data event is N/A, the seeder is skip-and-log, no clients
  version bump. **Next: DISTILL** (walk the UI sketches with the maintainer at its start, one decision at a
  time; they are already recorded in the "Maintainer decisions: UI sketches" block), then DELIVER (agree the
  rigor profile first).
- **The as-of projections do land under `Unknown`** for a mapping whose name is not one of its own members
  (`TeamMetricsService.cs:743`, `PortfolioMetricsService.cs:251`): confirmed by reading the code, not yet by a
  failing test. The probe comes first in DISTILL.
- **Regression rows to commit in DISTILL:** probes P1 and P2 in `rca.md` section 1 (the probe file itself
  was not kept). P3 and P4 are already pinned by part (b)'s tests.
- **Carried over from (b)'s review:** the frontend folds names and states with `toLowerCase`/`trim`
  (`normalise` in `stateMappingReconciliation.ts`), the backend with `OrdinalIgnoreCase` and
  `IsNullOrWhiteSpace`, so `ı`/`I`, `ſ`/`s` or a U+0085 name still reach a 400. Fix it where (a) replaces
  the reconciliation.
- **API note for DISCUSS-level checklists:** the `doingStates` values in `v1` GET would change meaning (raw
  states instead of mapping names), and a shadowing mapping becomes a 400. Lighthouse-Clients passes
  settings through as opaque JSON, but its docs and skills must say so.

## Wave: DESIGN / [REF] Design decisions

Application scope, propose mode, 2026-11-01. Convention: `docs/product/architecture/adr-270-category-lists-hold-raw-states-mappings-are-a-derived-view.md`.
Architecture delta: `docs/product/architecture/brief.md`, section "bug-6458-typed-category-entries (DESIGN delta)".
Invariant: the To Do, Doing and Done lists hold raw tracker states; mapping names never appear in them.

| # | Decision | Verdict | Why |
|---|---|---|---|
| D1 | Shape | Raw-only lists; a mapping is a derived display grouping (ii-b) | One source of truth (`StateMapping.States`); deletes the reconciliation; no schema change |
| D2 | Display columns in the DTO | Read-only `toDoDisplayStates`, `doingDisplayStates`, `doneDisplayStates`, computed in the domain (`DisplayStatesFor`) | One implementation of the ordering rule for backend and UI |
| D3 | Removing a mapping chip in Doing | Refused, with copy telling the user to delete the mapping instead | Removing members silently loses raw states and is hard to reverse |
| D4 | Shadowing payload from an API client | 400 naming the fix | Today it silently loses a raw state; only API clients can send it |
| D5 | Data rewrite | Idempotent `ISeeder` (precedent `ApiKeyOwnerReconciliationSeeder`) | Provider-neutral; no marker needed; testable on both providers |
| D6 | Group position | First member's index; the seeder writes members at the old chip's index | Existing aging-column order is preserved |
| D7 | Stored chained mappings | Legacy one-level expansion plus a warning log; the next save reports the chain | Keeps today's meaning, no data loss on upgrade |
| D8 | Frontend rules | Mirror of the backend rules against shared fixtures; names and states folded the same way on both sides | Closes the `ı`/`ſ`/U+0085 carry-over from part (b) |
| D9 | As-of `Unknown` | Part of (a): failing probe first, fixed by splitting the category lookup into `CategoryOfRawState` and `CategoryOfDisplayName` | One method over two namespaces is the bug class this feature removes |

## Wave: DESIGN / [REF] Component decomposition

| Component | Path (under `Lighthouse.Backend/Lighthouse.Backend/` unless noted) | Change |
|---|---|---|
| Owner model | `Models/WorkTrackingSystemOptionsOwner.cs` | Modified: `AllStates`/`OpenStates` read lists as-is; adds `DisplayStatesFor`, `CategoryOfRawState`, `CategoryOfDisplayName`; `GetRawStatesForCategory` kept for mapped-name references only |
| Owner contract | `Models/IWorkItemQueryOwner.cs` | Modified: the category lookup split |
| Settings sync | `API/Helpers/TeamExtensions.cs`, `PortfolioExtensions.cs` | Modified: mappings synced before lists; normalisation; refinement candidates from display states |
| Mapping validator | `API/Helpers/StateMappingValidator.cs` | Modified: sees the lists; shadowing and chain checks; category rules for changed mappings |
| Cycle-time and refinement validators | `API/Helpers/CycleTimeDefinitionValidator.cs`, `RefinementSettingsValidator.cs` | Modified: draw boundaries from display states |
| Settings DTO | `API/DTO/SettingsOwnerDtoBase.cs` | Modified: three read-only display lists |
| Suggestions | `API/SuggestionsController.cs` | Modified: stops returning mapping names as list suggestions |
| ADO, issue factory, ServiceNow span mapper | `Services/.../AzureDevOps/AzureDevOpsWorkTrackingConnector.cs` (StartedAndClosedDateFrom), `Factories/IssueFactory.cs` (GetStartedAndClosedDate), `Services/.../ServiceNow/ServiceNowStateSpanMapper.cs` (LastCrossingInto) | Modified: read the lists as they are |
| CSV demo journey | `Services/.../Csv/CsvWorkTrackingConnector.cs` (BuildInterpolatedDoneJourney) | Modified: iterate display states so members of one mapping make no self-transition |
| Metrics services | `Services/Implementation/TeamMetricsService.cs` (:463, :584, :743), `PortfolioMetricsService.cs` (:251, :319, :496) | Modified: display states for columns and order; display-name category for as-of |
| List normaliser | `Services/Implementation/Seeding/` (new seeder), registered next to the others in `Program.cs` | New |
| Jira, Linear connectors | `Services/.../Jira`, `Linear` | Unchanged |
| EF mapping and migrations | `Data/LighthouseAppContext.cs`, both migration projects | Unchanged |
| Frontend reconciliation | `Lighthouse.Frontend/src/utils/stateMappingReconciliation.ts` | Deleted (keep only a shared fold helper) |
| Frontend validation | `utils/stateMappingValidation.ts` | Modified: mirrors the backend rules; doing-clash rules deleted |
| Editor and chips | `components/Common/StateMappings/StateMappingsEditor.tsx`, `StatesList/StatesList.tsx`, `Team/ModifyTeamSettings.tsx`, `ProjectSettings/ModifyProjectSettings.tsx` | Modified: render display states with mapping chips, refuse mapping-chip removal, pick sources from raw Doing |
| Settings model | `models/Common/BaseSettings.ts` | Modified: three display lists |
| Metrics pages | `pages/Teams/Detail/TeamMetricsView.tsx`, `pages/Portfolios/Detail/PortfolioMetricsView.tsx` | Modified: read the display list |
| Wait-state, refinement, cycle-time pickers | `StateMappings/WaitStatesEditor.tsx`, `Team/RefinementSettingsSection.tsx`, `utils/isCycleTimeDefinitionValid.ts` | Modified: draw from display states |

## Wave: DESIGN / [REF] Driving ports

- Team and Portfolio settings read/write endpoints, `v1` and `latest` share one controller and DTO. Read
  gains the three display lists and returns raw lists. Write returns 400 for shadowing and chained mappings,
  and normalises legacy payloads. Read-only driving ports expose no write method.
- Suggestions endpoint: raw states only.
- Metrics endpoints: unchanged in shape; ordering of Age-in-State and cumulative columns follows display states.

## Wave: DESIGN / [REF] Driven ports and adapters

- Persistence (repositories, EF, SQLite and PostgreSQL): unchanged schema; the seeder rewrites stored list
  values through the repositories.
- Tracker connectors (Jira, ADO, Linear, CSV, ServiceNow): no new external call. Three sites stop expanding
  the lists; the CSV demo journey iterates display states.
- Earned-trust probe for the seeder: it must be idempotent (a second run changes nothing) and must survive a
  stored row that breaks the shadowing or chain rule (logged, not thrown). Both pinned by tests on both providers.

## Wave: DESIGN / [REF] Technology choices

No new technology, library or tool. Existing stack only: C# .NET 10, EF Core, NUnit, React 19, MUI, Vitest.

## Wave: DESIGN / [REF] Reuse Analysis

| Existing component | File | Overlap | Decision | Justification |
|---|---|---|---|---|
| `GetRawStatesForCategory` | `Models/WorkTrackingSystemOptionsOwner.cs` | Expands mapping names | EXTEND (narrow its callers) | Still correct for mapped-name references (wait, cycle-time, refinement) |
| `MapRawStateToMappedName` | same | Raw to display name | EXTEND (reuse inside `DisplayStatesFor`) | The view is a single pass over it |
| `MapStateToStateCategory` | same | Categorise a state | EXTEND (split) | Two namespaces need two names |
| `StateMappingValidator` | `API/Helpers/` | Mapping rules | EXTEND | Add list-aware rules |
| `ApiKeyOwnerReconciliationSeeder` / `ISeeder` | `Services/Implementation/Seeding/` | Idempotent startup reconciliation | EXTEND (new seeder in the same pattern) | Precedent; avoids per-provider SQL |
| `WidgetShell` status | frontend | Loading and failure look | REUSE | No new look |
| `normalise` / `reconcileDoingStates` | `utils/stateMappingReconciliation.ts` | Fold and reconcile | DELETE reconcile; keep one shared fold | Reconciliation is the source of the bug |
| `DisplayStatesFor` | owner | None existing computes the view | CREATE NEW | The one place the view is derived; no alternative |
| List normaliser seeder | `Services/Implementation/Seeding/` | None | CREATE NEW | One-off data rewrite; no existing component rewrites list values |

Contract shapes: `DisplayStatesFor`, `CategoryOfRawState`, `CategoryOfDisplayName` are pure functions of the owner.
Settings sync is bounded-change (declared mutation set: the three lists and the mappings of the owner being saved).
The seeder is bounded-change (the three lists of each owner, nothing else). Driving read ports expose no write.

## Wave: DESIGN / [REF] Loading and failure looks

- **Metrics views** (`TeamMetricsView`, `PortfolioMetricsView`) initialise their Doing columns to `[]` and fill
  them from the settings fetch. Today a slow or failed fetch can read as "no work in progress". Design: while
  the settings are loading, the aging widget shows `WidgetShell` status "loading" (a lone spinner, since there
  is nothing to dim); when the fetch fails it shows the could-not-load message. A pending or failed settings
  fetch must never render as an empty Doing list. Keyed queries so a late answer for an old request never lands.
  E2E page objects wait for `data-widget-status="ready"`. DISTILL pins both states; verify by hand with the
  browser throttled to a slow connection.
- **Settings editor:** while settings load the form shows its existing loading state; on failure the existing
  could-not-load state. Display lists come in the same payload as the raw lists, so there is no second fetch
  that could leave chips and raw states out of step. After a failed save the editor keeps the unsaved edit and
  shows the saved-failed indicator; it never shows the previous answer as current.

## Wave: DESIGN / [REF] Open questions / deferred to DISTILL

- Regression probes P1 and P2 from `rca.md` section 1, committed as tests (P3, P4 are pinned by part (b)).
- Failing probe for the as-of `Unknown` (Team :743, Portfolio :251) before the fix.
- Property test: the expansion of the normalised list equals the expansion of the original; normalising twice changes nothing.
- Non-adjacent-member ordering test (group at the first member's index).
- CSV demo-journey self-transition regression test.
- Seeder tests on both providers: idempotence, shadowing rows, chained rows, V1-breaking rows left alone.
- Rollback test with the older frontend, plus a release-note line.
- UI sketches at the start of DISTILL: mapping chip rendering, the refusal copy, the throttled loading/failure looks.
- DEVOPS usage-data event question: likely "N/A, because this is a correction, not a new feature"; confirm.
- Docs: API reference and the Lighthouse-Clients docs and skills (new meaning of `doingStates`, the 400); `ARCHITECTURE.md`.
- Lighthouse-Clients was outside the sandbox during DESIGN, so "opaque JSON, no field names" is the RCA's finding, not re-verified.

## Wave: DESIGN / [REF] Changed Assumptions

- RCA section 3 pairs `PortfolioMetricsService.cs:743` with the as-of projection. That line is the wait-state
  expansion; the Portfolio as-of site is only `:251`. Team `:743` is the as-of site.
- The RCA missed one frontend reader: `utils/isCycleTimeDefinitionValid.ts` (`resolveWorkflowStates`) flat-maps the lists through mappings.
- The as-of `Unknown` was a hypothesis in the RCA; the code read confirms it (stored transitions carry mapped
  names, categorised against lists that hold members only). Still owed: a failing test.

## Maintainer decisions: UI sketches (2026-11-01, collected before DISTILL, AFK entry)

Reuses `ItemListManager` chips and `WidgetShell` status; no new control.

| # | Decision | Chosen |
|---|---|---|
| UI1 | Mapping in the Doing list | One filled chip with a group icon, label = mapping name; tooltip: "State mapping: Code Review, Testing. Delete it under State Mappings to remove it." |
| UI2 | Refusing removal of a mapping chip | No delete icon on mapping chips; the tooltip above is the only explanation |
| UI3 | State Mappings help text | Replaced with: "Combine several Doing states into one named group. The group shows as one chip in your Doing list and in the charts; its states are still read from your tracker. Delete the group to see its states separately again." |
| UI4 | Dragging a mapping chip | Moves all its states together, so the group lands where it is dropped |
| UI5 | Aging chart while settings load / fail | `WidgetShell` status: lone spinner while loading, the existing could-not-load message on failure; never an empty chart |
| UI6 | Mapping state listed in none of To Do/Doing/Done | **No message** (overrides the informational warning in the design's V1 note); the backend allows it silently too |

```
Doing
 [Analysis ×] [⧉ In Progress] [Waiting for QA ×]
                 └ tooltip: "State mapping: Code Review, Testing.
                    Delete it under State Mappings to remove it."
 [ Add Doing state...                    ]
```

## Wave: DEVOPS / [REF] Prior Wave Consultation

AFK, 2026-11-01. Nothing was asked; every open point took the recommended option and is recorded below.

| Artifact | Read |
|---|---|
| This `feature-delta.md` (DESIGN D1-D9, component decomposition, loading/failure looks, UI decisions UI1-UI6) | yes |
| `docs/product/architecture/adr-270-category-lists-hold-raw-states-mappings-are-a-derived-view.md` | yes |
| `Program.cs` seeder registration (:1487-1493), `Data/DatabaseConfigurator.cs` (`ApplyMigrations`, `SeedDatabase`), `ApiKeyOwnerReconciliationSeeder.cs` | yes |
| `Models/UsageData/UsageDataEventName.cs` (values 0-24) | yes |
| `Services/Implementation/DemoDataService.cs`; `Lighthouse.EndToEndTests/tests` (helpers, POMs, specs) | yes |
| `/storage/repos/lighthouse-clients` (source, skills, changesets) | yes, grep only |
| `docs/feature/epic-4127-sle-risk-corrections/` DEVOPS sections and `environments.yaml` (format template) | yes |
| Dashboards, alert rules, runbooks | none exist in this repo, as in the template feature |

## Wave: DEVOPS / [REF] Q1 - the startup seeder

A new `ISeeder` (working name `CategoryListNormalisationSeeder`) rewrites each Team's and Portfolio's To Do,
Doing and Done lists once, in C#, through the repositories.

- **Rollout, both providers.** Startup runs `ApplyMigrations` (advisory lock on Postgres) and then
  `SeedDatabase` in the same scope. The seeder reads and writes through the repositories, so there is no
  provider-specific SQL and no migration. It runs on every start: SQLite, Postgres and the standalone builds.
- **Order.** Registered last in `Program.cs`, after `ForecastInputsForgottenAtStartSeeder` (registration order
  is run order). It needs only the migrated schema; none of the other seeders reads these lists.
- **Idempotent.** A list that already holds raw states is returned unchanged and not saved, so a second start
  rewrites nothing and logs a zero summary. That zero is the idempotence signal.
- **Logging.** Per owner rewritten: `Information`, owner type and id plus counts only. Per owner
  legacy-expanded (shadowing row, D7) or chained: `Warning`, owner type and id plus the rule that fired. One
  `Information` summary: owners scanned, rewritten, legacy-expanded, skipped. No owner names, no state names,
  no customer identifiers; matches `ApiKeyOwnerReconciliationSeeder`, which logs ids. Nothing is logged for
  an owner with no mapping.
- **Failure mode: skip and log.** `SeedDatabase` has no try/catch, so an exception from any seeder aborts
  startup; that is why the catch lives inside the seeder, per owner. A throwing owner is logged at `Warning`,
  its tracked changes discarded, and the rest are still rewritten. A failed final `Save` is logged at
  `Warning` and not rethrown. Why not abort: a row that breaks the new rules still works under the legacy
  one-level expansion (D7), so booting is safe, while a refused startup is an outage for a self-hosted customer
  with no operator able to fix a row. The next start retries. Cost: that owner keeps today's behaviour until
  fixed, and its Warning is the only signal.
- **Concurrency.** Two replicas starting together compute and write the same value; the chart ships
  `replicaCount: 1`, so this is a note, not a risk.
- **`HistoricalSchemaPatch`: N/A, because no column or table is added.** The `LighthouseAppContext` mapping
  and both migration projects are unchanged, so the `CreateMigration` rule is not triggered.

## Wave: DEVOPS / [REF] Q2 - rollback

The backend rolls back safely for every list that passed the new checks: raw lists are what the old
`GetRawStatesForCategory` expansion already reads (a raw member in a list is a no-op there), so the old
backend gives the same metrics. A row the seeder skipped was never touched. The old frontend, on its next
mapping edit, re-collapses members into the mapping name, which is today's behaviour and what the old backend
expects. Nothing is persisted that the old version cannot read, so rollback is the artifact rollback (re-pin
the image, `helm rollback`, previous standalone) and is atomic, because one artifact carries bundle and API.

Owed: (1) a **release-note line**, written at `/release` (not at finalize): lists now hold raw states, a
shadowing mapping payload is refused, and after a downgrade the old settings page may fold members into the
mapping name again on its next edit. (2) A **test**, already named in DESIGN: the expansion of the normalised
list equals the expansion of the original (property test); it is the backend-rollback safety proof.

## Wave: DEVOPS / [REF] Q3 - API v1 meaning change

`doingStates` (and the To Do and Done lists) now hold raw states; a shadowing entry is a 400; read-only
`toDoDisplayStates`, `doingDisplayStates`, `doneDisplayStates` are new. `docs/concepts/api-versioning.md`
names `/api/v1` the compatibility boundary for clients, so the break is stated there and in the release note.

Lighthouse-Clients, checked in `/storage/repos/lighthouse-clients`: no source file, test or changeset names
`doingStates`, `stateMapping` or the lists; settings pass through as opaque JSON. **Owed: docs and skill text
only, no code, no version bump.** Where the skill references describe Doing states
(`skills/lighthouse/references/coaching-patterns.md`), say that `doingStates` is raw tracker states and
`doingDisplayStates` is what the charts show. That text rides on the next clients changeset and does not
justify one on its own. This closes the DESIGN open question; it is now verified, not inferred.

## Wave: DEVOPS / [REF] Q4 - usage-data event

**N/A, because this is a correctness fix with no new user action.** Checked against `UsageDataEventName`
(0-24): `TeamRefinementConfigured` (12), `WorkTrackingSystemFieldsChanged` (19) and the rest name other
actions; none covers state mappings, and no outcome KPI of this bug needs a count. Nothing is appended to the
enum and `docs/settings/usagedata.md` is untouched.

## Wave: DEVOPS / [REF] Q5 - CI impact

- **`Program.cs` is edited** (seeder registration), so CI runs the full backend Integration suite, live
  connector categories included. Expected, not a regression; a connector 429 there is not caused by this change.
- **Workflows.** Code commits touch `Lighthouse.Backend/**` and `Lighthouse.Frontend/**`, so `ci.yml` fires:
  `ci_backend`, `ci_frontend`, `ci_e2e` (compile only), `ci_sonar_gates`, then `ci_verifysqlite` and
  `ci_verifypostgres` (Playwright runs, twice). A docs-only commit starts no CI run, only `pages.yml` for
  `docs/**`. No workflow file is edited.
- **E2E touching the area.** Settings editor: `tests/models/common/BaseSettingsPage.ts` (`getDoingStates`
  reads the chips, which now show display states). API helpers `tests/helpers/api/teams.ts` and
  `portfolios.ts` send `doingStates: states.doing` (raw, still valid, no mapping sent). Aging widget:
  `tests/models/metrics/WorkItemAgingChart.ts`, `MetricsPage.ts`, specs `flow/AgingPacePercentiles`,
  `flow/TimeInStateAndStaleness`, `flow/SleRiskColumnReachable`, `screenshots/Screenshots.spec.ts`. No spec
  sets a state mapping, so none needs a rewrite. `MetricsPage` already waits on `data-widget-status="ready"`
  (:62-67, :429-434). DISTILL should check that any POM method reading the aging chart goes through that wait,
  because the widget now shows `loading` while settings are fetched (UI5). No new E2E spec: E2E stays a thin
  sanity check and this is covered at unit and API level.

## Wave: DEVOPS / [REF] Q6 - demo data

`DemoDataService` contains no state-mapping configuration, and demo scenarios are loaded at runtime by the
user, after the seeder has run. So no demo team carries a mapping the seeder would rewrite, and no demo-data
JSON or CSV in the repo needs rewriting. **Answer: no demo-data change.** The one fixture carrying mappings
is `Lighthouse.Backend.Tests/API/Integration/FastWithManyFeatures/Approved/slice02-portfolio-northwind-platform-settings.approved.json`;
DELIVER updates it when the DTO gains the three display lists (a deliberate approval update, not a regression).

## Wave: DEVOPS / [REF] Q7 - monitoring after release

No metric, dashboard or alert is added (telemetry is off by default, nothing is phoned home). The signal is
the seeder's log, read by whoever runs the instance or by us on a support report:
- the summary line on the first start after upgrade: owners scanned, rewritten, legacy-expanded, skipped;
- a second start must report zero rewritten (idempotence);
- any seeder `Warning` is a stored row that needs a look; the legacy-expanded and skipped counts are the
  numbers to ask a reporter for.
Not instrumented, deliberately: how often users hit the new 400. It shows in the ordinary request log.

## Wave: DEVOPS / [REF] Environment matrix

Full inventory in `environments.yaml` (DISTILL parses it). Five existing environments: `local-dev`,
`ci-build`, `ci-verify-sqlite`, `ci-verify-postgres`, `customer-self-hosted`. No new environment, credential,
variable or key. The one named precondition is the **upgrade-from-previous-release state**: a database whose
Teams and Portfolios hold collapsed lists (mapping names in Doing), including shadowing and chained rows.

## Wave: DEVOPS / [REF] CI/CD pipeline outline

Unchanged; no workflow edited (stage list in Q5). Local gates unchanged (`.githooks` gitleaks and secrets
scan). Before push: `dotnet build`, `pnpm build`, and
`dotnet format analyzers Lighthouse.sln --severity info --verify-no-changes --no-restore`.

## Wave: DEVOPS / [REF] Monitoring contracts

No outcome KPI was defined for this bug. The contract is the seeder log in Q7; the acceptance contracts are
tests: idempotence, skip-and-log of a bad owner, expansion equality, on both providers.

## Wave: DEVOPS / [REF] Deployment strategy

Rolling/recreate, inherited and unchanged (`replicaCount: 1`, default `RollingUpdate`; Docker and standalone
are stop-and-replace). The rewrite runs at start inside the new artifact, before the API serves. Rollback as
in Q2: artifact rollback, no step to get in the wrong order.

## Wave: DEVOPS / [REF] Mutation testing strategy

`per-feature`, >=80% kill rate, Stryker.NET plus StrykerJS; already in `CLAUDE.md`, not touched. Runs last, on
frozen code. Targets: the new seeder, `DisplayStatesFor`, `CategoryOfRawState`/`CategoryOfDisplayName`,
`StateMappingValidator`, the frontend validation mirror. Traps: Stryker.NET must skip the acceptance suite;
StrykerJS can leave `@ts-nocheck` behind, so check the tree afterwards.

## Wave: DEVOPS / [REF] Observability stack

OpenTelemetry/Prometheus (off by default) and Serilog; unchanged. The only addition is the seeder log lines.

## Wave: DEVOPS / [REF] Branching strategy

Trunk-based, push to `origin/main`. One focused commit per step; refactor commits apart from feature commits.

## Wave: DEVOPS / [REF] Coexistence matrix

| Tool or surface | Must not break | Note |
|---|---|---|
| Stored data from the previous release | yes | Seeder rewrites once; rows breaking the new rules keep working via D7 |
| Older settings page after a downgrade | yes | Re-collapses on next edit; release-note line |
| `/api/v1` clients (CLI, MCP, scripts) | yes | Same field types; meaning change documented; shadowing is 400 |
| Metrics, forecasts, snapshots | yes | Expansion-equality property test |
| Existing E2E and screenshot suites | yes | No mappings in use; `ready` wait already exists |
| EF migrations, `HistoricalSchemaPatch` | yes | Untouched |
| Jira, Linear connectors | yes | Unchanged |

## Wave: DEVOPS / [REF] Pre-requisites

- None external. The premium licence fixture is needed only for `@screenshot`, which this change does not run.
- Seeder tests on Postgres use the shared Postgres container fixture (one instance, fresh database per test).

## Wave: DEVOPS / [REF] Changed Assumptions

None. DESIGN's open question about Lighthouse-Clients is now verified: no field names there.

## Wave: DEVOPS / [REF] Handoff to DISTILL

`environments.yaml` at the feature root. DISTILL must parametrise over: (1) provider, SQLite and Postgres,
for the seeder; (2) stored-list shape: already raw (no-op), collapsed with adjacent members, collapsed with
non-adjacent members (group at the first member's index), shadowing row, chained row, a row that throws;
(3) run count: first and second start (idempotence); (4) the upgrade-from-previous-release precondition;
(5) the aging widget's settings-fetch states: loading, ready, error. Seeder logging is pinned by level and
owner id only. No nWave scenario tags in code. No peer review was invoked; the end-of-DISTILL review applies.
