# Bug 6458 (a): category lists cannot tell a raw state from a mapping name

ADO Bug #6458 (Active). Part (b) of the same item, the autosave of an unfinished mapping, shipped on its own
as `docs/feature/bug-6458-empty-mapping-autosave/` (commits `bfd39ec97`, `be3adace6`). This workspace is
part (a) only.

## Where we stopped (2026-11-01)

- **RCA done**, in `rca.md` here. The maintainer agreed both root causes and the split:
  part (a) is a design flaw (public API values, a data rewrite of stored lists, connector expansion sites,
  the settings editor), so it goes through **DESIGN** before any fix. No further frontend guard.
- **DESIGN is done (2026-11-01).** Shape (ii-b) was chosen; see "Wave: DESIGN" below and ADR-270.
  **DEVOPS is done (2026-11-01, AFK):** usage-data event is N/A, the seeder is skip-and-log, no clients
  version bump. **DISTILL is done (2026-11-01, AFK):** the scenarios, scaffolds and the red classification are in
  `distill/red-classification.md` and the "Wave: DISTILL" sections below; the UI decisions UI1-UI6 are pinned as
  written. **Next: the DISTILL review gate**, which should start with the five points in "Reconciliation
  result" (first of all: a skipped seeder owner against reads that no longer expand), then DELIVER (agree the
  rigor profile first; slice order in the last DISTILL section, seeder before the read switch).
- **The as-of projections do land under `Unknown`** for a mapping whose name is not one of its own states
  (`TeamMetricsService.cs:743`, `PortfolioMetricsService.cs:251`): now confirmed by a failing probe, written as
  skipped tests in `MetricsAsOfGroupNameTest` (both fail with `Unknown` where `Doing` is expected).
- **Regression rows are committed:** probes P1 and P2 as write-path, validation and display tests (P3 and P4 were
  already pinned by part (b)'s tests).
- **Carried over from (b)'s review:** the frontend folds names and states with `toLowerCase`/`trim`
  (`normalise` in `stateMappingReconciliation.ts`), the backend with `OrdinalIgnoreCase` and
  `IsNullOrWhiteSpace`, so `ı`/`I`, `ſ`/`s` or a U+0085 name still reach a 400. Fix it where (a) replaces
  the reconciliation. DISTILL corrected the examples: `ı`/`I` and `ſ`/`s` already agree (different on both
  sides); the real disagreements are the Kelvin and Angstrom signs and U+0085 (see "Reconciliation result", point 4).
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
| D4 | Shadowing payload from an API client | 400 naming the fix. Shadowing means the entry equals a mapping's name, that name is not one of the mapping's own states, AND at least one of the mapping's states is listed alongside it in the same list. The message reuses the copy the frontend already shows (shipped with Bug 6454): `Mapping "{name}" has the same name as the {list label} state "{state}". Add "{name}" to this mapping to group it, or choose another name.` | Today it silently loses a raw state; only API clients can send it |
| D5 | Data rewrite | Idempotent `ISeeder` (precedent `ApiKeyOwnerReconciliationSeeder`) | Provider-neutral; no marker needed; testable on both providers |
| D6 | Group position | First member's index; the seeder writes members at the old chip's index | Existing aging-column order is preserved |
| D7 | Stored chained mappings | Legacy one-level expansion plus a warning log; the next save reports the chain | Keeps today's meaning, no data loss on upgrade |
| D8 | Frontend rules | Mirror of the backend rules against shared fixtures; names and states folded the same way on both sides | Closes the `ı`/`ſ`/U+0085 carry-over from part (b) |
| D9 | As-of `Unknown` | Part of (a): failing probe first, fixed by splitting the category lookup into `CategoryOfRawState` and `CategoryOfDisplayName` | One method over two namespaces is the bug class this feature removes |
| D10 | One entry classifier (DESIGN correction after review) | One classifier on the owner (Models), called by the write path, the validator and the seeder. For a list entry that equals a mapping's name: (a) the name is one of that mapping's own states: a raw state, kept, and the members not yet listed are added after it; (b) collapsed chip: no state of that mapping is listed anywhere in that list: replace the entry by the members, in place, never a 400; (c) shadowing: the name is listed and at least one of the mapping's states is listed alongside: 400 on the write path, legacy expansion plus a Warning in the seeder. The normaliser's output holds a mapping name only under (a) | The rca.md draft refused case (b), but (b) is the shape the current editor saves on every mapping edit; three documents gave three verdicts. One classifier removes the divergence and keeps services free of an API dependency |
| D11 | Single write entry for the three lists | `SyncStates` in `TeamExtensions.cs` and `PortfolioExtensions.cs` (the create paths go through the same sync) and the seeder are the only code that assigns `ToDoStates`/`DoingStates`/`DoneStates`; both call the owner's normaliser. An architecture test pins it | A third writer would skip the classifier |

## Wave: DESIGN / [REF] Component decomposition

| Component | Path (under `Lighthouse.Backend/Lighthouse.Backend/` unless noted) | Change |
|---|---|---|
| Owner model | `Models/WorkTrackingSystemOptionsOwner.cs` | Modified: `AllStates`/`OpenStates` read lists as-is; adds `DisplayStatesFor`, `CategoryOfRawState`, `CategoryOfDisplayName` and the one entry classifier / normaliser (D10; the validator and the seeder call it, so no Services-to-API dependency); `GetRawStatesForCategory` kept for mapped-name references only |
| Owner contract | `Models/IWorkItemQueryOwner.cs` | Modified: the category lookup split |
| Settings sync | `API/Helpers/TeamExtensions.cs`, `PortfolioExtensions.cs` | Modified: `SyncStates` syncs mappings before lists and calls the owner's normaliser; refinement candidates from display states. `SyncStates` and the seeder are the only writers of the three lists (D11) |
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
  gains the three display lists and returns raw lists. Write returns 400 for shadowing (D10 case c) and chained
  mappings, and normalises everything else, collapsed chips included (D10 case b). Read-only driving ports expose no write method.
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
| Owner's entry classifier / normaliser | same | The same name-to-members walk as `GetRawStatesForCategory` | EXTEND (shares that walk; adds the three-way classification of D10) | Lives in Models so the API validator and the Services seeder both call it with no Services-to-API dependency |
| `MapRawStateToMappedName` | same | Raw to display name | EXTEND (reuse inside `DisplayStatesFor`) | The view is a single pass over it |
| `MapStateToStateCategory` | same | Categorise a state | EXTEND (split) | Two namespaces need two names |
| `StateMappingValidator` | `API/Helpers/` | Mapping rules | EXTEND | Add list-aware rules |
| `ApiKeyOwnerReconciliationSeeder` / `ISeeder` | `Services/Implementation/Seeding/` | Idempotent startup reconciliation | EXTEND (new seeder in the same pattern) | Precedent; avoids per-provider SQL |
| `WidgetShell` status | frontend | Loading and failure look | REUSE | No new look |
| `normalise` / `reconcileDoingStates` | `utils/stateMappingReconciliation.ts` | Fold and reconcile | DELETE reconcile; keep one shared fold | Reconciliation is the source of the bug |
| `DisplayStatesFor` | owner | None existing computes the view | CREATE NEW | The one place the view is derived; no alternative |
| List normaliser seeder | `Services/Implementation/Seeding/` | Follows the `ApiKeyOwnerReconciliationSeeder` precedent (same `ISeeder` pattern, per-owner skip-and-log) | CREATE NEW (class only) | The existing seeders reconcile other data; none rewrites list values, and extending one would mix concerns. It adds no logic of its own: it calls the owner's normaliser |

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
- Property test: the expansion of the normalised list equals the expansion of the original; normalising twice changes nothing. The generator includes the collapsed chip (`[Analysis, Review]` with `Review = [Code Review, Testing]`, case b), the self-named mapping (case a), and the shadowing shape (case c, which the write path refuses and the seeder expands the legacy way).
- Architecture test: only `SyncStates` (Team and Portfolio) and the list normaliser seeder assign `ToDoStates`, `DoingStates` or `DoneStates`.
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
  legacy-expanded (shadowing row, D10 case c) or chained (D7): `Warning`, owner type and id plus the rule that fired. One
  `Information` summary: owners scanned, rewritten, legacy-expanded, skipped. No owner names, no state names,
  no customer identifiers; matches `ApiKeyOwnerReconciliationSeeder`, which logs ids. Nothing is logged for
  an owner with no mapping.
- **Failure mode: skip and log.** `SeedDatabase` has no try/catch, so an exception from any seeder aborts
  startup; that is why the catch lives inside the seeder, per owner. A throwing owner is logged at `Warning`
  and the rest are still rewritten. Why not abort: a refused startup is an outage for a self-hosted customer
  with no operator able to fix a row. The next start retries.
  **Correction after review:** the earlier text said a skipped row "still works under the legacy expansion". It
  does not once reads are as-is (slice 4): an owner that keeps a collapsed list loses the grouped states from
  its queries until it is repaired. The repair is an ordinary save of that owner's settings (the write path runs
  the same normaliser; a collapsed chip is normalised, never refused, D10 case b) or the next start. The
  Warning is the only signal; the maintainer declined a UI signal.
- **Transactionality.** The seeder shares one scoped `DbContext`, so one owner's failure must not flush or lose
  another's tracked changes. Save per owner (`SaveChanges` after each rewritten owner); when an owner's save
  fails, detach that owner's entries before moving on. A failed save is logged at `Warning`, not rethrown.
- **Other callers.** The seeder also runs in-process, not only at process start, after `RestoreBackup` and
  `ClearDatabase` (`DatabaseManagementService.cs:116` and `:174`, `MigrateAndSeedDatabase`). A restored
  pre-upgrade backup is therefore normalised on restore, and the same skip-and-log rules apply there.
- **Concurrency.** Two replicas starting together compute and write the same value. The chart ships
  `replicaCount: 1` by default, but allows more with Redis (`chart/templates/_helpers.tpl:236`) and the
  Deployment has no `strategy` block, so a default rolling update overlaps old and new pods; see Q2.
- **`HistoricalSchemaPatch`: N/A, because no column or table is added.** The `LighthouseAppContext` mapping
  and both migration projects are unchanged, so the `CreateMigration` rule is not triggered.

## Wave: DEVOPS / [REF] Q2 - rollback

The backend rolls back safely for every list that passed the new checks: raw lists are what the old
`GetRawStatesForCategory` expansion already reads (a raw member in a list is a no-op there), so the old
backend gives the same metrics. A row the seeder skipped was never touched. The old frontend, on its next
mapping edit, re-collapses members into the mapping name, which is today's behaviour and what the old backend
expects. Nothing is persisted that the old version cannot read, so rollback is the artifact rollback (re-pin
the image, `helm rollback`, previous standalone) and is atomic, because one artifact carries bundle and API.

**Rolling-update overlap (correction after review).** The earlier text called the overlap harmless. The chart
allows `replicaCount` above 1 with Redis (`chart/templates/_helpers.tpl:236`) and the Deployment has no
`strategy` block, so a default rolling update runs old and new pods together. An old-version pod can save a
settings page after the new pod has seeded and re-collapse a list. With the single classifier that list is
repaired by the next save of that owner or the next start (a collapsed chip is normalised, never refused).
It is not lost data, but it is a window in which grouped states drop out of the new pod's queries. Owed: a
release-note line (listed under "Owed at finalize" in the slice notes).

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
  `ci_verifypostgres` (Playwright runs, twice). A docs-only PUSH to main starts no CI run, only `pages.yml` for
  `docs/**`; a docs-only `pull_request` does start `ci.yml` (`ci.yml:6`). No workflow file is edited.
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
- the summary line on the first start after upgrade, and after every `RestoreBackup` or `ClearDatabase`
  (`DatabaseManagementService.cs:116`, `:174`): owners scanned, rewritten, legacy-expanded, skipped;
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
| Stored data from the previous release | yes | Seeder rewrites once; shadowing and chained rows are legacy-expanded with a Warning (D10, D7); a skipped owner is repaired by a save or the next start |
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

## Wave: DISTILL / [REF] Prior Wave Consultation

AFK, 2026-11-01. Nothing was asked; every open point took the reading recorded here.

| Artifact | Read |
|---|---|
| This `feature-delta.md` (DESIGN D1-D9, components, loading/failure looks, open questions; UI1-UI6; DEVOPS Q1-Q7, environment matrix) | yes |
| `rca.md` (section 1 probes P1/P2, section 6.2 shape (ii-b), V1-V5, property test) | yes |
| `environments.yaml` (axes: provider, stored-list shape, run count) | yes |
| `docs/product/architecture/adr-270-...md` | yes |
| `docs/ci-learnings.md` (Vitest/RTL, NUnit/integration, E2E sections) | yes, applied (per-test mocks, anchored copy assertions, no wall-clock timing) |
| `docs/feature/bug-6458-typed-category-entries/discuss/` | **missing: warning logged.** This bug went RCA to DESIGN by maintainer agreement. Acceptance criteria were derived from DESIGN D1-D9, the RCA and UI1-UI6; there is no story-to-scenario traceability |
| `docs/product/journeys/`, `docs/product/kpi-contracts.yaml` | not applicable (no journey SSOT for settings; DEVOPS says no outcome KPI) |
| Existing tests next to the code (owner, validators, sync, seeders, connectors, StatesList, editor, metrics views) | yes, used as the layout precedent |

## Wave: DISTILL / [REF] Reconciliation result

Reconciliation passed: 0 blocking contradictions. DISCUSS is absent, so there was nothing upstream of DESIGN to
contradict. DESIGN against DEVOPS against the UI decisions:

- UI6 (no message for a mapping state listed in none of the lists) overrides the design's informational warning
  on V1. Resolved by the maintainer, not a contradiction: the backend accepts silently (a test pins that the
  response carries no warning) and the editor and the frontend validation say nothing (green today).
- DEVOPS Q5 says no spec sets a state mapping, so none needs a rewrite: consistent with DESIGN. Checked: the
  aging-chart page object (`WorkItemAgingChart.ts`) already goes through `MetricsWidget.waitUntilLoaded`, and
  `MetricsPage` waits for `data-widget-status="ready"` (:62-67, :429-434). No change needed.

Points the review gate should look at (found while writing the tests; each took the reading given, none blocks):

1. **A skipped owner is read as-is.** DEVOPS Q1 says an owner the seeder skips "still works under the legacy
   one-level expansion". DESIGN D1/ADR-270 make `AllStates`, `OpenStates` and the category lookup read the lists
   as they are. Both cannot hold: a skipped owner keeps its collapsed list and, once reads are as-is, items in
   the grouped states fall out of the queries. The tests pin the DEVOPS behaviour that is decidable (skip, log a
   Warning with the owner id, still rewrite the others, count it) and nothing about reading a skipped owner.
   Needs a decision before slice 4 ships: keep the skip rare (only a row that cannot be read at all), or add a
   read-side fallback for owners that are logged as skipped.
   **Maintainer decision (2026-11-01): skip, warn, retry. No read-side fallback.** The condition was "only if
   a user can fix it", and a user can: every save of the owner's settings sends the stored lists through the
   same normalisation, so an ordinary save repairs the row. The seeder also retries on every start, so a
   one-off failure clears itself. The normalisation is a pure function, so a failure that repeats is a
   database-level failure for that row, and the user's save then fails visibly instead of silently.
   Owed: a release-note line saying that an owner named in the seeder's Warning is repaired by saving its
   settings. DELIVER pins the repair: a save of a skipped owner's unchanged settings writes raw lists.
   **Why the decision now holds (revision round 1):** the single entry classifier (D10) makes a collapsed chip
   a normalised shape on the write path, never a 400. Before, the write path would have refused the very list
   a skipped owner still holds, so "a save repairs it" was false. DEVOPS Q1's claim that a skipped row still
   works under the legacy expansion is corrected there: it does not once reads are as-is; the repair is a save
   or the next start. No UI signal, as the maintainer decided.
2. **Collapsed chip and shadowing row: resolved by one classifier (D10, revision round 1).** The earlier
   reading, "one shape by the V3 definition, both refused with a 400", is superseded. A stored `[Analysis,
   Review, Waiting]` with `Review = [Code Review, Testing]` is a collapsed chip: no state of the mapping is
   listed, so the normaliser replaces the entry by the members in place. The write path and the seeder both do
   this and never refuse it (it is what the current editor saves on every mapping edit). Only the 6454
   shadowing shape, where the name is listed and a state of the mapping is listed alongside, is a 400 on the
   write path and a legacy expansion plus one Warning per owner in the seeder.
3. **Stored chained mappings and idempotence.** D7 expands a chained row one level and warns. A second start
   would expand the entry that is left, so the tests pin: an entry whose name is itself a state of another
   mapping is treated as a state and left alone. First start `[B, Analysis]` (with `A=[Fix]`, `B=[A]`) becomes
   `[A, Analysis]`; second start changes nothing.
4. **The case-folding premise was only half right.** `ı`/`I` and `ſ`/`s` are different on the server
   (`OrdinalIgnoreCase`) and on the page (`toLowerCase`), so they already agree. The names the two sides
   actually disagree on are the Kelvin sign (U+212A) against `k`, the Angstrom sign (U+212B) against `å`
   (the page folds them together, the server does not), and `U+0085` as whitespace (the server trims it and
   calls it blank, the page does not). The shared cases pin all of these, and the plain `ı`/`ſ` pairs as
   "different", so a fold built on `toUpperCase` alone (which turns `ı` into `I`) fails. The rule the page must
   follow: trim as the server trims, then upper-case one code point at a time, keeping a character whose upper
   case is more than one code point (`ß`) and also `ı` and `ſ`.
5. **`CategoryOfDisplayName` with a state listed nowhere.** UI6 allows it. The tests pin: judge a group by its
   listed states only; if none is listed, `Unknown`. During slices 1 and 2 the lists may still hold a collapsed
   group name, so the lookup must also answer for a name that is itself an entry of a list.

## Wave: DISTILL / [REF] Scenario inventory

No tags appear in the code (project rule); the classification lives in this table only. State: `pending` is
skipped with a reason that says what it waits for (`[Ignore("pending: ...")]`, `it.skip`); `green` runs today.
Counts per case (a parameterised method counts once per case): **backend 114 cases in these classes, 10 green,
104 pending; frontend 45 cases in these files, 10 green, 35 pending.** Error, edge, probe and property
scenarios are 66 of the 119 methods below (55%, above the 40% target). The classification column is a
reading of the test name, not a tag.

| Test file | Test | Pins | State | Classification |
|---|---|---|---|---|
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | DisplayStatesFor_MembersNextToEachOther_ShowsOneGroupInTheirPlace | owner view, split category lookup, raw reads | pending | happy path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | DisplayStatesFor_MembersApart_ShowsTheGroupWhereItsFirstMemberIs | owner view, split category lookup, raw reads | pending | edge |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | DisplayStatesFor_LaterMemberBeforeEarlierOne_ShowsTheGroupAtTheFirstListedMember | owner view, split category lookup, raw reads | pending | happy path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | DisplayStatesFor_SameStateListedTwiceInDifferentCasingAndSpacing_ShowsItOnce | owner view, split category lookup, raw reads | pending | edge |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | DisplayStatesFor_GroupNamedAfterOneOfItsOwnStates_ShowsTheGroupOnce | owner view, split category lookup, raw reads | pending | happy path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | DisplayStatesFor_GroupWhoseStatesAreListedNowhere_LeavesTheListAlone | owner view, split category lookup, raw reads | pending | edge |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | DisplayStatesFor_NoMappings_ReturnsTheListAsItIs | owner view, split category lookup, raw reads | pending | edge |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | DisplayStatesFor_AnyCategory_GroupsThatCategorysListOnly | owner view, split category lookup, raw reads | pending | happy path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | DisplayStatesFor_AfterTheMappingThatGroupedAStateIsDeleted_LeavesNoGhostName | owner view, split category lookup, raw reads | pending | regression probe |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | DisplayStatesFor_AnyGeneratedList_ShowsEachGroupOnceAtItsFirstMemberAndLosesNoState | owner view, split category lookup, raw reads | pending | property |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | CategoryOfRawState_ReadsTheListsAsTheyAre | owner view, split category lookup, raw reads | pending | happy path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | CategoryOfRawState_MappingNameThatIsNoStateOfTheTracker_IsUnknown | owner view, split category lookup, raw reads | pending | error path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | CategoryOfRawState_RawStateThatSharesAMappingsName_KeepsItsOwnCategory | owner view, split category lookup, raw reads | pending | happy path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | CategoryOfDisplayName_MappingNameOrRawState_ReadsTheCategoryOfWhatItStandsFor | owner view, split category lookup, raw reads | pending | happy path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | CategoryOfDisplayName_MappingWhoseStatesSitInDifferentCategories_IsUnknown | owner view, split category lookup, raw reads | pending | error path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | CategoryOfDisplayName_MappingWithOneStateListedNowhere_FollowsTheStatesThatAreListed | owner view, split category lookup, raw reads | pending | edge |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | CategoryOfDisplayName_MappingWhoseStatesAreAllListedNowhere_IsUnknown | owner view, split category lookup, raw reads | pending | error path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | CategoryOfDisplayName_GroupNamedAfterOneOfItsOwnStates_IsTheCategoryOfItsStates | owner view, split category lookup, raw reads | pending | happy path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | AllStates_ListHoldsAStateNamedLikeAMapping_KeepsThatStateAndDoesNotAddTheMappingsStates | owner view, split category lookup, raw reads | pending | happy path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | OpenStates_ListHoldsAStateNamedLikeAMapping_ReadsToDoAndDoingAsTheyAre | owner view, split category lookup, raw reads | pending | happy path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | AllStates_RawListWithAMappingOverIt_HoldsEveryRawStateOnce | owner view, split category lookup, raw reads | pending | happy path |
| `Tests/Models/CategoryListNormalisationTest.cs` | NormalisedStatesFor_GroupNamedAfterItsOwnState_ListsTheOtherStatesRightAfterIt | normalisation: set-equivalence, idempotence, rollback | pending | happy path |
| `Tests/Models/CategoryListNormalisationTest.cs` | NormalisedStatesFor_ListThatAlreadyHoldsEveryState_PassesThroughInTheSameOrder | normalisation: set-equivalence, idempotence, rollback | pending | happy path |
| `Tests/Models/CategoryListNormalisationTest.cs` | NormalisedStatesFor_SameStateInDifferentCasing_KeepsTheFirstSpelling | normalisation: set-equivalence, idempotence, rollback | pending | edge |
| `Tests/Models/CategoryListNormalisationTest.cs` | NormalisedStatesFor_NoMappings_ReturnsTheListWithoutRepeats | normalisation: set-equivalence, idempotence, rollback | pending | edge |
| `Tests/Models/CategoryListNormalisationTest.cs` | NormalisedStatesFor_EmptyList_StaysEmpty | normalisation: set-equivalence, idempotence, rollback | pending | edge |
| `Tests/Models/CategoryListNormalisationTest.cs` | NormalisedStatesFor_AnyGeneratedList_MeansTheSameStatesAsTheListBeforeIt | normalisation: set-equivalence, idempotence, rollback | pending | property |
| `Tests/Models/CategoryListNormalisationTest.cs` | NormalisedStatesFor_AnyGeneratedList_ChangesNothingTheSecondTime | normalisation: set-equivalence, idempotence, rollback | pending | property |
| `Tests/Models/CategoryListNormalisationTest.cs` | NormalisedStatesFor_AnyGeneratedList_NeverNamesAGroupThatIsNotOneOfItsOwnStates | normalisation: set-equivalence, idempotence, rollback | pending | property |
| `Tests/Models/CategoryListNormalisationTest.cs` | AnOlderBackend_ReadingANormalisedList_FindsTheSameStatesAsBeforeTheUpgrade | normalisation: set-equivalence, idempotence, rollback | pending | edge |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingRawStatesWithAGroup_ReadsBackTheSameStatesAndTheGroupOnDisplay | settings HTTP: write path and read model (Team and Portfolio) | pending | happy path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingGroupStatesThatAreApart_ShowsTheGroupWhereItsFirstStateIs | settings HTTP: write path and read model (Team and Portfolio) | pending | edge |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingAGroupInToDoAndDone_ShowsItThereToo | settings HTTP: write path and read model (Team and Portfolio) | pending | happy path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingAGroupNamedAfterOneOfItsStatesWithoutTheOthers_AddsTheOthersRightAfterIt | settings HTTP: write path and read model (Team and Portfolio) | pending | happy path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingTheGroupAndTheListInOneSave_ReadsTheListAgainstTheGroupJustSent | settings HTTP: write path and read model (Team and Portfolio) | pending | happy path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingTheListWithoutTheGroupThatStoodBehindAState_DoesNotBringTheGroupsOtherStatesBack | settings HTTP: write path and read model (Team and Portfolio) | green | happy path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingTheSameSettingsTwice_ChangesNothingTheSecondTime | settings HTTP: write path and read model (Team and Portfolio) | pending | edge |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingAListWhoseEntryIsAGroupNameAndStillListsTheGroupsState_IsRefusedWithTheFix | settings HTTP: write path and read model (Team and Portfolio) | pending | error path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingAListWhoseOnlyEntryIsTheGroupName_IsRefusedInsteadOfLosingTheTrackersState | settings HTTP: write path and read model (Team and Portfolio) | pending | regression probe |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingAGroupOverAState_WhoseEarlierGroupWasDeleted_LeavesNoLeftoverName | settings HTTP: write path and read model (Team and Portfolio) | pending | regression probe |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingAGroupThatListsAnotherGroupsName_IsRefusedNamingBothGroups | settings HTTP: write path and read model (Team and Portfolio) | pending | error path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingAGroupWithAStateListedNowhere_IsAcceptedWithoutAWarning | settings HTTP: write path and read model (Team and Portfolio) | pending | edge |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingANewGroupWhoseListedStatesSitInDifferentCategories_IsRefused | settings HTTP: write path and read model (Team and Portfolio) | pending | error path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingAnUnrelatedChange_WhileAnOlderGroupSpansCategories_StillSaves | settings HTTP: write path and read model (Team and Portfolio) | green | edge |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | ReadingAnOwnerSavedBeforeTheUpgrade_ShowsRawStatesAndTheGroupOnDisplay | settings HTTP: write path and read model (Team and Portfolio) | pending | happy path |
| `Tests/API/Helpers/StateMappingValidatorRawListsTest.cs` | EntryNamedLikeAGroupThatIsNotOneOfItsStates_IsRefusedNamingTheFix | validator rules over raw lists | pending | regression probe |
| `Tests/API/Helpers/StateMappingValidatorRawListsTest.cs` | EntryNamedLikeAGroupThatIsNotOneOfItsStates_IsRefusedWhicheverListHoldsIt | validator rules over raw lists | pending | regression probe |
| `Tests/API/Helpers/StateMappingValidatorRawListsTest.cs` | EntryNamedLikeAGroupAndAlsoOneOfItsStates_IsNotShadowing | validator rules over raw lists | green | regression probe |
| `Tests/API/Helpers/StateMappingValidatorRawListsTest.cs` | GroupThatListsAnotherGroupsName_IsRefusedNamingBothGroups | validator rules over raw lists | pending | error path |
| `Tests/API/Helpers/StateMappingValidatorRawListsTest.cs` | GroupWithAStateListedNowhere_IsValid | validator rules over raw lists | green | edge |
| `Tests/API/Helpers/StateMappingValidatorRawListsTest.cs` | GroupWhoseListedStatesSitInDifferentCategories_IsRefused | validator rules over raw lists | pending | error path |
| `Tests/API/Helpers/StateMappingValidatorRawListsTest.cs` | TwoGroupsOverTheSameState_AreRefusedWithoutBlamingAListEntry | validator rules over raw lists | green | error path |
| `Tests/API/Helpers/TeamListSyncRawStatesTest.cs` | SyncTeamWithTeamSettings_GroupNamedAfterItsOwnState_StoresTheOtherStatesRightAfterIt | sync order, normalisation, refinement on a group | pending | happy path |
| `Tests/API/Helpers/TeamListSyncRawStatesTest.cs` | SyncTeamWithTeamSettings_GroupDeletedInTheSameSave_ReadsTheListWithoutIt | sync order, normalisation, refinement on a group | pending | regression probe |
| `Tests/API/Helpers/TeamListSyncRawStatesTest.cs` | SyncTeamWithTeamSettings_RawStatesWithAGroup_KeepsTheListAsSent | sync order, normalisation, refinement on a group | green | happy path |
| `Tests/API/Helpers/TeamListSyncRawStatesTest.cs` | SyncTeamWithTeamSettings_RefinementChosenOnAGroup_KeepsTheGroup | sync order, normalisation, refinement on a group | pending | happy path |
| `Tests/API/Helpers/StateMappingRuleCasesTest.cs` | TwoMappingNames_AreTheSameExactlyWhenTheSharedCasesSayTheyAre | shared fold/blank cases, backend half | green | happy path |
| `Tests/API/Helpers/StateMappingRuleCasesTest.cs` | TwoMappingNames_DifferingOnlyInWhitespaceAtTheEnds_AreTheSame | shared fold/blank cases, backend half | pending | edge |
| `Tests/API/Helpers/StateMappingRuleCasesTest.cs` | ANameOfNothingButWhitespace_IsBlank | shared fold/blank cases, backend half | green | edge |
| `Tests/Services/Implementation/MetricsAsOfGroupNameTest.cs` | TeamLookingBack_ItemWasInAGroupWhoseNameIsNoStateOfTheTracker_WasStillInDoing | as-of category of a group name | pending | regression probe |
| `Tests/Services/Implementation/MetricsAsOfGroupNameTest.cs` | PortfolioLookingBack_FeatureWasInAGroupWhoseNameIsNoStateOfTheTracker_WasStillInDoing | as-of category of a group name | pending | regression probe |
| `Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowStateSpanMapperRawListsTest.cs` | WhenWorkStarted_StateNamedLikeAGroup_IsTheArrivalIntoThatState | connector reads lists as they are (ServiceNow) | pending | happy path |
| `Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowStateSpanMapperRawListsTest.cs` | WhenWorkFinished_DoneStateNamedLikeAGroup_IsTheArrivalIntoThatState | connector reads lists as they are (ServiceNow) | pending | happy path |
| `Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowStateSpanMapperRawListsTest.cs` | WhenWorkWasQueued_ToDoStateNamedLikeAGroup_IsTheLastReturnIntoThatState | connector reads lists as they are (ServiceNow) | pending | happy path |
| `Tests/Factories/IssueFactoryRawListsTest.cs` | CreateIssueFromJson_DoingStateNamedLikeAGroup_DatesTheStartByTheArrivalIntoThatState | connector reads lists as they are (Jira) | pending | happy path |
| `Tests/Factories/IssueFactoryRawListsTest.cs` | CreateIssueFromJson_DoneStateNamedLikeAGroup_DatesTheCloseByTheArrivalIntoThatState | connector reads lists as they are (Jira) | pending | happy path |
| `Tests/Services/Implementation/WorkTrackingConnectors/Csv/CsvDemoJourneyWithStateMappingTest.cs` | DemoJourney_TeamGroupsTwoOfItsDoingStates_NeverMovesAnItemFromTheGroupToItself | CSV demo journey over display states | pending | error path |
| `Tests/Services/Implementation/WorkTrackingConnectors/Csv/CsvDemoJourneyWithStateMappingTest.cs` | DemoJourney_GroupSitsFirstInDoing_StartsTheJourneyInTheGroup | CSV demo journey over display states | pending | happy path |
| `Tests/Architecture/CategoryListsAreReadAsTheyAreGuardTest.cs` | NoCodeExpandsACategoryList | no list expansion outside mapped-name readers (covers ADO) | pending | happy path |
| `Tests/Architecture/CategoryListsAreReadAsTheyAreGuardTest.cs` | OnlyCodeThatReadsSettingsNamingAGroupCallsTheExpansion | no list expansion outside mapped-name readers (covers ADO) | pending | happy path |
| `Tests/Services/Implementation/Seeding/CategoryListNormalisationSeederTests.cs` | Seed_SaveFails_LogsAWarningAndLetsTheStartContinue | seeder: save failure, nothing to rewrite | pending | error path |
| `Tests/Services/Implementation/Seeding/CategoryListNormalisationSeederTests.cs` | Seed_NothingToRewrite_SavesNothing | seeder: save failure, nothing to rewrite | pending | error path |
| `Tests/Integration/Containers/CategoryListNormalisationSeederOnBothProvidersTests.cs` | EveryStoredShape_IsRewrittenOnceAndLeftAloneTheSecondTime | seeder on SQLite and Postgres: shapes, idempotence, unreadable owner | pending | happy path |
| `Tests/Integration/Containers/CategoryListNormalisationSeederOnBothProvidersTests.cs` | AnOwnerWhoseGroupsCannotBeRead_IsSkippedWithAWarning_AndTheOthersAreStillRewritten | seeder on SQLite and Postgres: shapes, idempotence, unreadable owner | pending | error path |
| `src/utils/stateMappingView.test.ts` | shows one group in the place of states that sit next to each other | frontend display view and shared rule cases | pending | happy path |
| `src/utils/stateMappingView.test.ts` | puts a group where its first listed state is when its states are apart | frontend display view and shared rule cases | pending | edge |
| `src/utils/stateMappingView.test.ts` | treats the same state in another casing or with spaces around it as one | frontend display view and shared rule cases | pending | edge |
| `src/utils/stateMappingView.test.ts` | shows a group named after one of its own states once | frontend display view and shared rule cases | pending | happy path |
| `src/utils/stateMappingView.test.ts` | leaves a list alone when none of a group's states are in it | frontend display view and shared rule cases | pending | happy path |
| `src/utils/stateMappingView.test.ts` | leaves no name behind for a group that was deleted | frontend display view and shared rule cases | pending | happy path |
| `src/utils/stateMappingView.test.ts` | shows the list as it is when there are no groups | frontend display view and shared rule cases | pending | happy path |
| `src/utils/stateMappingView.test.ts` | tells the same names apart in plain letters | frontend display view and shared rule cases | green | edge |
| `src/utils/stateMappingView.test.ts` | tells the same names apart in every case of the shared list | frontend display view and shared rule cases | pending | edge |
| `src/utils/stateMappingView.test.ts` | folds the names of the shared list to the same key exactly when the server does | frontend display view and shared rule cases | pending | happy path |
| `src/utils/stateMappingView.test.ts` | ignores whitespace at the ends the way the server does | frontend display view and shared rule cases | pending | edge |
| `src/utils/stateMappingView.test.ts` | calls a name of nothing but whitespace blank, whichever whitespace it is | frontend display view and shared rule cases | pending | edge |
| `src/utils/stateMappingView.test.ts` | calls an empty name and a name of ordinary spaces blank | frontend display view and shared rule cases | green | edge |
| `src/utils/stateMappingValidation.rawLists.test.ts` | refuses a list that still has the mapping's state next to the mapping's name, and says to rename the mapping | frontend validation mirror | pending | error path |
| `src/utils/stateMappingValidation.rawLists.test.ts` | still refuses the name once the mapping's state has been taken out of the list by hand | frontend validation mirror | pending | error path |
| `src/utils/stateMappingValidation.rawLists.test.ts` | holds back the autosave for a name that shadows a state | frontend validation mirror | pending | regression probe |
| `src/utils/stateMappingValidation.rawLists.test.ts` | refuses a name that shadows a state in To Do or Done as well | frontend validation mirror | pending | regression probe |
| `src/utils/stateMappingValidation.rawLists.test.ts` | refuses a mapping that groups another mapping's name, naming both | frontend validation mirror | pending | error path |
| `src/utils/stateMappingValidation.rawLists.test.ts` | refuses a mapping whose listed states sit in different categories | frontend validation mirror | pending | error path |
| `src/utils/stateMappingValidation.rawLists.test.ts` | says nothing about a mapping state that is in none of the lists | frontend validation mirror | green | error path |
| `src/utils/stateMappingValidation.rawLists.test.ts` | accepts a mapping named after one of its own states | frontend validation mirror | green | happy path |
| `src/utils/stateMappingValidation.rawLists.test.ts` | reports two mappings over the same state, whatever the Doing list holds | frontend validation mirror | green | happy path |
| `src/components/Common/StatesList/StatesList.stateMappings.test.tsx` | shows a mapping as one chip with a group icon where its first state is | UI1, UI2, UI4, suggestions | pending | edge |
| `src/components/Common/StatesList/StatesList.stateMappings.test.tsx` | shows a mapping whose states are apart at the position of its first state | UI1, UI2, UI4, suggestions | pending | edge |
| `src/components/Common/StatesList/StatesList.stateMappings.test.tsx` | names the mapping's states in a tooltip that says how to remove it | UI1, UI2, UI4, suggestions | pending | happy path |
| `src/components/Common/StatesList/StatesList.stateMappings.test.tsx` | offers no way to remove a mapping chip, and still one for a plain state | UI1, UI2, UI4, suggestions | pending | happy path |
| `src/components/Common/StatesList/StatesList.stateMappings.test.tsx` | removes a plain state by its own name | UI1, UI2, UI4, suggestions | green | happy path |
| `src/components/Common/StatesList/StatesList.stateMappings.test.tsx` | moves all of a mapping's states together when the chip is dragged ahead of another | UI1, UI2, UI4, suggestions | pending | happy path |
| `src/components/Common/StatesList/StatesList.stateMappings.test.tsx` | lands a dragged mapping where it is dropped, even when its states were apart | UI1, UI2, UI4, suggestions | pending | edge |
| `src/components/Common/StatesList/StatesList.stateMappings.test.tsx` | drags a plain state ahead of a mapping without splitting the mapping | UI1, UI2, UI4, suggestions | pending | happy path |
| `src/components/Common/StatesList/StatesList.stateMappings.test.tsx` | shows an empty mapping list as plain states | UI1, UI2, UI4, suggestions | green | edge |
| `src/components/Common/StatesList/StatesList.stateMappings.test.tsx` | does not offer a state named like one of the mappings as a suggestion | UI1, UI2, UI4, suggestions | pending | regression probe |
| `src/components/Common/StateMappings/StateMappingsEditor.rawLists.test.tsx` | explains that a group is one chip in Doing and the charts, and that its states stay read from the tracker | UI3, UI6 | pending | happy path |
| `src/components/Common/StateMappings/StateMappingsEditor.rawLists.test.tsx` | says nothing about a mapping state that is in none of the lists | UI3, UI6 | green | error path |
| `src/pages/Teams/Detail/TeamMetricsView.agingColumns.test.tsx` | says the columns are loading, and shows none, while the settings are on their way | UI5 on the Team page | pending | happy path |
| `src/pages/Teams/Detail/TeamMetricsView.agingColumns.test.tsx` | says the columns could not be loaded when the settings fetch fails | UI5 on the Team page | pending | error path |
| `src/pages/Teams/Detail/TeamMetricsView.agingColumns.test.tsx` | shows the columns as the settings display them, groups in place of their states | UI5 on the Team page | pending | happy path |
| `src/pages/Teams/Detail/TeamMetricsView.agingColumns.test.tsx` | does not let a late answer for the team it left replace the columns of the team it shows | UI5 on the Team page | pending | error path |
| `src/pages/Portfolios/Detail/PortfolioMetricsView.agingColumns.test.tsx` | says the columns are loading, and shows none, while the settings are on their way | UI5 on the Portfolio page | pending | happy path |
| `src/pages/Portfolios/Detail/PortfolioMetricsView.agingColumns.test.tsx` | says the columns could not be loaded when the settings fetch fails | UI5 on the Portfolio page | pending | error path |
| `src/pages/Portfolios/Detail/PortfolioMetricsView.agingColumns.test.tsx` | shows the columns as the settings display them, groups in place of their states | UI5 on the Portfolio page | pending | happy path |
| `src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx` | shows a lone spinner and no chart while the columns load | UI5 on the aging widget frame | pending | happy path |
| `src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx` | says it couldn't be loaded when the columns could not be fetched, and draws no empty chart | UI5 on the aging widget frame | pending | error path |
| `src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx` | shows the chart once the columns have arrived | UI5 on the aging widget frame | green | happy path |
| `src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx` | leaves every other chart to load on its own while the columns are missing | UI5 on the aging widget frame | green | happy path |

Green at hand-off (they pin what must keep holding): the sync keeps a raw list as sent; saving without the
group that stood behind a state does not bring the group's other states back; an unrelated save succeeds while
an older group spans categories; two groups over one state are refused; a group named after its own state is
valid; a group with a state listed nowhere is valid and silent; the page and the server agree on plain-letter
duplicate names and on ordinary blank names; plain-state removal and an empty mapping list in `StatesList`; the
aging frame reads ready when the columns have arrived and leaves other charts alone.

## Wave: DISTILL / [REF] Walking skeleton

None. This is a bug fix (a walking skeleton is optional for bugs) and there is no user journey to demonstrate
end to end that the unit, HTTP and database tests below do not already prove. The closest end-to-end scenario is
`SavingRawStatesWithAGroup_ReadsBackTheSameStatesAndTheGroupOnDisplay` (Team and Portfolio), pending until the
write path ships. The green tests at hand-off are the regression guards listed above.

E2E: no new spec. Checked first: `BaseSettingsPage.getDoingStates` reads chips (display states after this
change) and no spec sets a state mapping, so none needs a rewrite; the aging chart page object already waits on
the widget status. A mapping spec would only repeat what the Vitest and NUnit scenarios pin.

## Wave: DISTILL / [REF] Adapter coverage

| Adapter | Real-I/O scenario | Covered by |
|---|---|---|
| Settings HTTP API, Team and Portfolio (driving) | yes | `CategoryListsHoldRawStatesSettingsIntegrationTest`, run for both owner kinds through `WebApplicationFactory` |
| Persistence, SQLite | yes | seeder cases on a migrated file database, every stored shape, first and second start, unreadable owner |
| Persistence, Postgres | yes | the same cases through `SharedPostgresContainer` (one container, a created database per case) |
| Jira issue factory | yes (no network) | `IssueFactoryRawListsTest` |
| Azure DevOps connector | **no unit seam**: the site is private and reachable only through live-ADO tests | `CategoryListsAreReadAsTheyAreGuardTest` (no list expansion outside mapped-name readers); the live `AdoIntegration` category stays the behavioural check and must be run once in DELIVER slice 4 |
| ServiceNow span mapper | yes | `ServiceNowStateSpanMapperRawListsTest` |
| CSV connector (demo journey) | yes | `CsvDemoJourneyWithStateMappingTest` through the production container |
| Jira and Linear connectors (read `AllStates` only) | indirect | owner `AllStates`/`OpenStates` tests; unchanged files |
| Metrics services (as-of, columns) | yes (in-memory repositories) | `MetricsAsOfGroupNameTest` for Team and Portfolio; the aging columns are pinned on the pages that feed them |
| Frontend driving adapters | yes | `StatesList`, `StateMappingsEditor`, `TeamMetricsView`, `PortfolioMetricsView`, aging widget frame |

## Wave: DISTILL / [REF] Driving adapter coverage

| Driving port (DESIGN) | Scenario |
|---|---|
| Team settings PUT and GET (`v1` and `latest` share the controller) | HTTP fixture, owner kind Team |
| Portfolio settings PUT and GET | the same fixture, owner kind Portfolio |
| Suggestions endpoint | no new scenario: it returns the stored lists, which are raw once the write path and the seeder have run; the frontend scenario "does not offer a state named like a mapping" covers what the editor offers |
| Metrics endpoints | no HTTP scenario: the shape is unchanged; the as-of lookup and the column order are pinned at the service, the page and the widget frame |
| Settings editor, States list, metrics pages | Vitest scenarios (UI1-UI6) |

## Wave: DISTILL / [REF] Scaffolds

Every stub compiles into the solution with zero warnings; production behaviour is unchanged by all of them.

| Where | Scaffold | Marker |
|---|---|---|
| `Models/WorkTrackingSystemOptionsOwner.cs` | `DisplayStatesFor(StateCategories)`, `CategoryOfRawState(string)`, `CategoryOfDisplayName(string)`, `NormalisedStatesFor(IReadOnlyList<string>)`: throw `InvalidOperationException("Not yet implemented -- RED scaffold ...")` | `// SCAFFOLD: true` |
| `Services/Implementation/Seeding/CategoryListNormalisationSeeder.cs` | class implementing `ISeeder`, constructor `(IRepository<Team>, IRepository<Portfolio>, ILogger<...>)`, `Seed()` throws. **Not registered in `Program.cs`** (DELIVER slice 3) | `// SCAFFOLD: true` |
| `API/DTO/SettingsOwnerDtoBase.cs` | `ToDoDisplayStates`, `DoingDisplayStates`, `DoneDisplayStates`: get-only, `[JsonIgnore]`, empty. They do **not** throw: a throwing getter would break every existing settings serialisation, and a public setter fails the existing round-trip guard (`SettingsOwnerDtoRoundTripTest`) | `// SCAFFOLD: true` |
| `Lighthouse.Frontend/src/utils/stateMappingView.ts` | new module: `foldKey`, `isBlankName`, `displayStatesFor` throw `Error("Not yet implemented -- RED scaffold ...")` | `export const __SCAFFOLD__ = true` |
| `models/Common/BaseSettings.ts` | optional `toDoDisplayStates`, `doingDisplayStates`, `doneDisplayStates` | doc comment `SCAFFOLD: true` |
| `components/Common/StatesList/StatesList.tsx` | optional prop `stateMappings` (not read yet) | doc comment `SCAFFOLD: true` |
| `pages/Common/MetricsView/BaseMetricsView.tsx` | optional prop `doingStatesStatus` (not read yet) | doc comment `SCAFFOLD: true` |

`grep -rn "SCAFFOLD: true\|__SCAFFOLD__" Lighthouse.Backend Lighthouse.Frontend/src` finds all of them. After DELIVER
none should remain. The pinned copy fragments DELIVER must produce: `Rename the mapping` (shadowing, in the 400 body and
the page error), `another mapping` (chain), the exact UI1 tooltip, the exact UI3 help text. Pinned test hooks:
`data-testid="state-mapping-chip"` on a mapping chip and `data-testid="state-mapping-chip-icon"` on its group icon;
the seeder's summary line carries the named values `Scanned`, `Rewritten`, `LegacyExpanded`, `Skipped`, and its
Warning for a skipped owner carries the owner id.

Shared data: `Lighthouse.Frontend/src/utils/stateMappingRuleCases.json` is read by the backend test
(`StateMappingRuleCasesTest`, through the repository root) and by the frontend test, like the other source-text
enforcement tests.

## Wave: DISTILL / [REF] Test placement

Next to the code each test covers, following the existing layout (no new `tests/acceptance` tree):
`Lighthouse.Backend.Tests/Models` (owner, normalisation, `StateListCases` generator), `API/Helpers` (sync,
validator, shared cases), `API/Integration` (settings over HTTP), `Services/Implementation/...` (metrics as-of,
seeder unit, connectors), `Factories`, `Architecture` (source-scan guard), `Integration/Containers` (seeder on
both providers, like the other upgrade tests). Frontend tests sit beside their component or util
(`*.rawLists.test.*`, `*.stateMappings.test.*`, `*.agingColumns.test.*`); the aging-frame scenarios extend the
existing `BaseMetricsView.loading.test.tsx` harness (one option added to its `openTheDashboard`).

No FsCheck in the backend test project, so the property scenarios use `StateListCases.Generate(300)`, a seeded
generator (seed 6458) inside one test per property, with the failing case printed. No package was added.

Existing tests DELIVER retires or rewrites as the code changes (they pin the old behaviour):
`stateMappingReconciliation.test.ts`, the doing-clash cases in `stateMappingValidation.test.ts`, the
`state-mapping-names` cases in `ModifyTeamSettings.test.tsx` and its Portfolio twin, the expansion cases of
`WorkTrackingSystemOptionsOwnerTest`, and the approved
`slice02-portfolio-northwind-platform-settings.approved.json` when the DTO gains the display lists.

## Wave: DISTILL / [REF] Pre-requisites

- DESIGN driving ports: Team and Portfolio settings endpoints, metrics views; DEVOPS matrix: SQLite and Postgres,
  first and second start, upgrade-from-previous-release state (the seeder fixture builds it with EF and, for the
  unreadable owner, one raw `UPDATE`).
- Docker for the Postgres cases; the SQLite cases need none (the fixture starts the container lazily, like the
  other upgrade tests).
- Local runs: backend `dotnet build`, then `dotnet test --filter` on the class names in the inventory; never the
  unfiltered suite (live connectors). Frontend `pnpm install` once per worktree, then `pnpm vitest run <files>`.
- No new package, environment, key or migration.

## Wave: DISTILL / [REF] Proposed slice order for DELIVER

The dependency that changes the first guess: **reads can only switch to "as they are" after the stored data is
raw**, so the seeder comes before the connector and metrics read switch, not after it. Each slice ships alone,
green, and leaves the old behaviour correct for the data still in the database.

| # | Slice | Turns on | Why it is safe alone |
|---|---|---|---|
| 1 | **Domain view and the as-of fix.** `DisplayStatesFor`, `CategoryOfRawState`, `CategoryOfDisplayName`, `NormalisedStatesFor`; the two as-of sites use `CategoryOfDisplayName` | owner view/lookup tests, normalisation and rollback property tests, `MetricsAsOfGroupNameTest` | adds members and changes only the as-of lookup (a bug fix: `Unknown` becomes the right category); no list is read differently |
| 2 | **Write path, validation, DTO, refinement.** mappings synced before lists, normalised lists, shadowing/chain/category rules, three display lists on the DTO (drop the `JsonIgnore`, update the approved payload), `SyncRefinement` candidates from display states | HTTP fixture, validator, sync, shared-case backend half | new saves store raw states; reads still expand, which the equivalence property proves gives the same set |
| 3 | **Seeder.** the rewrite, `Program.cs` registration (this makes CI run the full Integration suite), logging contract | seeder cases on both providers, seeder unit cases | idempotent, skip-and-log; stored data becomes raw |
| 4 | **Read switch.** `AllStates`/`OpenStates`/lookup as-is, ADO/Jira/ServiceNow sites, CSV demo journey, metrics columns from display states, architecture guard; run the live `AdoIntegration` category once | owner raw-read tests, connector tests, CSV, guard | only after 3; resolve review point 1 first |
| 5 | **Frontend.** `stateMappingView`, validation mirror and shared fold, `StatesList` chips/tooltip/drag/suggestions, editor help text, aging status on both pages and the frame; delete the reconciliation module | all Vitest scenarios | needs 2 for the display lists; deleting the reconciliation is safe once the server owns the rules; Biome, `tsc`, then `pnpm build` last |

**Slice 2 and the unchanged frontend.** Between slices 2 and 5 the editor still collapses members into the
mapping name on every mapping edit. A slice-2 save from that editor is normalised (D10 case b), not refused.
The GET then returns raw `doingStates`, which the old editor re-collapses on its next mapping edit until
slice 5. That is harmless: the next save normalises it again. Only an API payload of the shadowing shape
(case c) gets the 400.

**Single write entry.** Slice 2 routes `SyncStates` (Team and Portfolio; the create paths use the same sync)
through the owner's normaliser; slice 3's seeder calls the same normaliser. The architecture test (only those
two assign the three lists) lands with slice 2 and covers the seeder when slice 3 adds it.

**Owed at finalize** (none of it is a DELIVER step):
- Release-note lines, written at `/release`: the v1 meaning change of `doingStates` and the shadowing 400;
  the rollback re-collapse by the old settings page; an owner named in the seeder Warning is repaired by
  saving its settings; the rolling-update overlap with more than one replica.
- `docs/concepts/api-versioning.md` (or wherever the v1 API is documented), at feature finalization.
- Lighthouse-Clients skill text (`coaching-patterns.md`): `doingStates` is raw states, `doingDisplayStates` is
  what the charts show; rides on the next clients changeset.

Mutation testing runs last on frozen code (seeder, `DisplayStatesFor`, the lookups, the classifier, the validator, the frontend
mirror), as DEVOPS already says. DELIVER starts by agreeing the rigor profile.
