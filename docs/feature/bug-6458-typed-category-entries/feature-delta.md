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
| D10 | One entry classifier (DESIGN correction after review) | One classifier on the owner (Models), called by the write path, the validator and the seeder. For a list entry that equals a mapping's name: (a) the name is one of that mapping's own states: a raw state, kept, and the members not yet listed are added after it; (b) collapsed chip: no state of that mapping is listed anywhere in that list: replace the entry by the members, in place, never a 400; (c) shadowing: the name is listed and at least one of the mapping's states is listed alongside: 400 on the write path, legacy expansion plus a Warning in the seeder. Precedence: an entry that is one of its own mapping's states is case (a), even when other states of that mapping are listed beside it (the exclusion the scaffold's `ListEntryKind.cs` doc text omitted; DELIVER corrects that comment). A first step drops null, empty and whitespace-only entries; this leaves the expansion set unchanged, because such an entry expands to nothing a tracker can return. The normaliser's output holds a mapping name only under (a). A mapping with no states (null or empty) is not a group: an entry with its name stays a raw state; the seeder logs it as a Warning with the fixed rule value `EmptyGroup` and does not throw, and no read throws either | The rca.md draft refused case (b), but (b) is the shape the current editor saves on every mapping edit; three documents gave three verdicts. One classifier removes the divergence and keeps services free of an API dependency |
| D11 | Single write entry for the three lists | `SyncStates` in `TeamExtensions.cs` and `PortfolioExtensions.cs` (the create paths go through the same sync) and the seeder are the only code that assigns `ToDoStates`/`DoingStates`/`DoneStates`; both call the owner's normaliser. An architecture test pins it. Seed-data writer, allow-listed: `DemoDataFactory.cs:36-38` and `:55-57` builds new Teams and Portfolios with list initialisers; its demo lists are raw states and carry no mappings, so there is nothing to normalise | A third writer would skip the classifier |
| D12 | Normalise on read (maintainer rule: "skip, warn, retry if a user can fix it; if not, normalise") | Supersedes the "lists read as they are" wording of D1 and ADR-270. The stored value still holds raw states, but every read goes through the owner's one classifier / normaliser (D10): `AllStates`, `OpenStates`, both category lookups, `DisplayStatesFor`, the connector reads and the suggestions endpoint see the normalised view of the stored lists. On a list the seeder already rewrote the normaliser is the identity. A skipped, pre-seeder or old-pod-written owner still loads every item. Case (c), shadowing, reads with the legacy expansion, exactly as the seeder writes it. The seeder stays: it makes stored data match, keeps the API GET's raw lists honest, and retries on every start | The earlier claim "an ordinary save repairs a skipped owner" is false: the seeder skips an owner only when reading or writing that row fails, a persistent failure fails the user's save too, and for the pinned unreadable shape (a group with no states) the save is a 400. Cost: a small per-read cost, and reads depend on the mappings again, but without ambiguity because of the classifier |

## Wave: DESIGN / [REF] Component decomposition

| Component | Path (under `Lighthouse.Backend/Lighthouse.Backend/` unless noted) | Change |
|---|---|---|
| Owner model | `Models/WorkTrackingSystemOptionsOwner.cs` | Modified: `AllStates`/`OpenStates` and the lookups read the normalised view of the stored lists (D12; identity once the seeder has run); adds `DisplayStatesFor`, `CategoryOfRawState`, `CategoryOfDisplayName` and the one entry classifier / normaliser (D10; the validator and the seeder call it, so no Services-to-API dependency); `GetRawStatesForCategory` kept for mapped-name references only |
| Owner contract | `Models/IWorkItemQueryOwner.cs` | Modified: the category lookup split |
| Settings sync | `API/Helpers/TeamExtensions.cs`, `PortfolioExtensions.cs` | Modified: `SyncStates` syncs mappings before lists and calls the owner's normaliser; refinement candidates from display states. `SyncStates` and the seeder are the only writers of the three lists (D11) |
| Mapping validator | `API/Helpers/StateMappingValidator.cs` | Modified: sees the lists; shadowing and chain checks; category rules for changed mappings |
| Cycle-time and refinement validators | `API/Helpers/CycleTimeDefinitionValidator.cs`, `RefinementSettingsValidator.cs` | Modified: draw boundaries from display states |
| Settings DTO | `API/DTO/SettingsOwnerDtoBase.cs` | Modified: three read-only display lists |
| Suggestions | `API/SuggestionsController.cs` | Modified: `SuggestionsController.cs:71` (and the Portfolio twin) reads the stored lists directly; it must read the owners' normalised lists (D12) so a not-yet-seeded owner never offers a group name as a state |
| ADO, issue factory, ServiceNow span mapper | `Services/.../AzureDevOps/AzureDevOpsWorkTrackingConnector.cs` (StartedAndClosedDateFrom), `Factories/IssueFactory.cs` (GetStartedAndClosedDate), `Services/.../ServiceNow/ServiceNowStateSpanMapper.cs` (LastCrossingInto) | Modified: stop expanding the lists themselves and read the owner's normalised view (D12) |
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
- Suggestions endpoint: raw states only, taken from the owners' normalised lists (D12).
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
| UI7 | Copy for a mapping whose states sit in more than one category (**maintainer decision: default taken at 09:49 with no objection**) | Two states in different categories: `Mapping "Review" groups states from different categories: "Code Review" is in To Do and "Testing" is in Doing. Keep a mapping's states in one category.` One state in two categories: `Mapping "Review" groups "Testing", which is in both To Do and Done. Keep a mapping's states in one category.` |
| UI8 | Copy for a mapping named like a state beside its own states | Doing uses the shipped Doing sentence (`Mapping "{name}" has the same name as the Doing state "{state}". Add "{name}" to this mapping to group it, or choose another name.`); To Do and Done use the shipped sentence from `stateMappingValidation.ts` (`Mapping "{name}" has the same name as the {To Do\|Done} state "{state}". Choose another name.`). Both stacks pin these exactly. This refines D4's single sentence |

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
  **Correction after review (round 2, D12):** the seeder skips an owner only when reading or writing that row
  fails, so a persistent failure fails the user's save too and "a save repairs it" is false. Instead every
  read normalises the stored lists through the owner's classifier (D12), so a skipped owner still loads every
  item; on a rewritten list the normaliser is the identity. The seeder retries on every start. The Warning
  remains the only signal; the maintainer declined a UI signal.
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
settings page after the new pod has seeded and re-collapse a list. Because the new pod normalises on read
(D12), that list still loads every item; the next start rewrites it. Nothing is lost. Owed: a release-note line
(listed under "Owed at finalize" in the slice notes).

Owed: (1) a **release-note line**, written at `/release` (not at finalize): lists now hold raw states, a
shadowing mapping payload is refused, and after a downgrade the old settings page may fold members into the
mapping name again on its next edit. (2) A **test**, already named in DESIGN: the expansion of the normalised
list equals the expansion of the original (property test); it is the backend-rollback safety proof.

## Wave: DEVOPS / [REF] Q3 - API v1 meaning change

`doingStates` (and the To Do and Done lists) now hold raw states; a shadowing entry is a 400; read-only
`toDoDisplayStates`, `doingDisplayStates`, `doneDisplayStates` are new. `docs/concepts/api-versioning.md`
names `/api/v1` the compatibility boundary for clients, so the break will be stated at finalize in `docs/concepts/api-versioning.md` and the release note.

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
| Stored data from the previous release | yes | Seeder rewrites once; shadowing and chained rows are legacy-expanded with a Warning (D10, D7); a skipped or not-yet-seeded owner keeps working because reads normalise (D12), and the seeder retries on each start |
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
   one-level expansion". DESIGN D1/ADR-270 made `AllStates`, `OpenStates` and the category lookup read the lists
   as they are. Both cannot hold: a skipped owner keeps its collapsed list and, once reads are as-is, items in
   the grouped states fall out of the queries. The tests pin the DEVOPS behaviour that is decidable (skip, log a
   Warning with the owner id, still rewrite the others, count it) and nothing about reading a skipped owner.
   Needed a decision before slice 4 ships: keep the skip rare, or add a read-side fallback for skipped owners.
   **Maintainer decision (2026-11-01, superseded in DESIGN round 2): "skip, warn, retry if a user can fix it;
   if not, normalise."** The first answer was "skip, warn, retry, no read-side fallback" on the reasoning that
   a user can fix a skipped owner by saving its settings. Two reviewers showed that false: the seeder skips an
   owner only when reading or writing that row fails, a persistent failure makes the user's save fail too, and
   for the pinned unreadable shape (a group with no states) the save is refused with a 400. A user cannot fix
   it, so the conditional's second branch applies: **normalise on read (D12).** Reads go through the owner's
   one classifier; on a rewritten list it is the identity, and a skipped, pre-seeder or old-pod-written owner
   still loads every item. Case (c), shadowing, reads with the legacy expansion, as the seeder writes it. The
   seeder stays and retries on every start. DEVOPS Q1's claim that a skipped row still works under the legacy
   expansion is now true again, for that reason. No UI signal, as the maintainer decided. Owed: a release-note
   line, "an owner named in the seeder's Warning keeps working; the seeder retries on each start."
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
Counts per case (a parameterised method counts once per case): **backend 182 cases in these classes, 37 green,
145 pending; frontend 54 cases in these files, 19 green, 35 pending.** The table lists methods: 175 rows, 125
pending, 50 green.

**Error-path share.** Strict error paths (a refusal, an unreadable or refused write, a failed fetch, null or blank
input, a late answer) are 40 of 175 methods: **23%**, below the 40% target. Adding the edge, regression-probe
and property rows brings the share of non-happy-path rows to 97 of 175 (55%). The strict share is not
reachable honestly beyond this: the feature is a data-model correction, so most of its observable behaviour is
a successful reading of a different shape (raw lists, a group on display, a rewritten row) and the refusals it
adds are few (shadowing, chain, categories). The error scenarios that exist were all added; none is padding.
The classification column is a reading of the test name, not a tag.

| Test file | Test | Pins | State | Classification |
|---|---|---|---|---|
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | DisplayStatesFor_MembersNextToEachOther_ShowsOneGroupInTheirPlace | owner view, split category lookup, raw reads | pending | happy path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | DisplayStatesFor_MembersApart_ShowsTheGroupWhereItsFirstMemberIs | owner view, split category lookup, raw reads | pending | edge |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | DisplayStatesFor_LaterMemberBeforeEarlierOne_ShowsTheGroupAtTheFirstListedMember | owner view, split category lookup, raw reads | pending | happy path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | DisplayStatesFor_SameStateListedTwiceInDifferentCasingAndSpacing_ShowsItOnce | owner view, split category lookup, raw reads | pending | edge |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | DisplayStatesFor_StatesPaddedWithTabsAndNonBreakingSpaces_AreRecognisedAsTheirGroup | owner view, split category lookup, raw reads | pending | edge |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | DisplayStatesFor_GroupNamedAfterOneOfItsOwnStates_ShowsTheGroupOnce | owner view, split category lookup, raw reads | pending | happy path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | DisplayStatesFor_GroupWhoseStatesAreListedNowhere_LeavesTheListAlone | owner view, split category lookup, raw reads | pending | edge |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | DisplayStatesFor_NoMappings_ReturnsTheListAsItIs | owner view, split category lookup, raw reads | pending | edge |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | DisplayStatesFor_AnyCategory_GroupsThatCategorysListOnly | owner view, split category lookup, raw reads | pending | happy path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | DisplayStatesFor_AStateWhoseGroupWasDeleted_IsLeftExactlyAsStored | owner view, split category lookup, raw reads | pending | regression probe |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | DisplayStatesFor_AnyGeneratedList_ShowsEachGroupOnceInTheOrderOfItsFirstStateAndLosesNoState | owner view, split category lookup, raw reads | pending | property |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | CategoryOfRawState_ReadsTheListsAsTheyAre | owner view, split category lookup, raw reads | pending | happy path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | CategoryOfRawState_MappingNameThatIsNoStateOfTheTracker_IsUnknown | owner view, split category lookup, raw reads | pending | edge |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | CategoryOfRawState_ListStillHoldsTheGroupNameInPlaceOfItsStates_FindsTheStatesInThatList | owner view, split category lookup, raw reads | pending | happy path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | CategoryOfDisplayName_MappingNameOrRawState_ReadsTheCategoryOfWhatItStandsFor | owner view, split category lookup, raw reads | pending | happy path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | CategoryOfDisplayName_ListStillHoldsTheGroupNameInPlaceOfItsStates_IsTheCategoryOfThatList | owner view, split category lookup, raw reads | pending | happy path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | CategoryOfDisplayName_MappingWhoseStatesSitInDifferentCategories_IsUnknown | owner view, split category lookup, raw reads | pending | edge |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | CategoryOfDisplayName_MappingWithOneStateListedNowhere_FollowsTheStatesThatAreListed | owner view, split category lookup, raw reads | pending | edge |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | CategoryOfDisplayName_MappingWhoseStatesAreAllListedNowhere_IsUnknown | owner view, split category lookup, raw reads | pending | edge |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | CategoryOfDisplayName_GroupNamedAfterOneOfItsOwnStates_IsTheCategoryOfItsStates | owner view, split category lookup, raw reads | pending | happy path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | AllStates_StoredListHoldsTheGroupNameInPlaceOfItsStates_ReadsTheStatesThrough | owner view, split category lookup, raw reads | green | happy path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | OpenStates_StoredListHoldsTheGroupNameInPlaceOfItsStates_ReadsTheStatesThrough | owner view, split category lookup, raw reads | green | happy path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | AllStates_StoredListHoldsAGroupNameBesideOneOfItsStates_ReadsTheLegacyExpansion | owner view, split category lookup, raw reads | green | happy path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | Reads_MappingWithNoStates_KeepTheEntryAsAStateAndDoNotThrow | owner view, split category lookup, raw reads | pending | error path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | Reads_CollapsedListTheStartUpPassCouldNotRewrite_FindTheGroupsStatesEverywhere | owner view, split category lookup, raw reads | pending | error path |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | AllStates_ReadTwiceOverAListTheStartUpPassAlreadyRewrote_IsUnchanged | owner view, split category lookup, raw reads | green | edge |
| `Tests/Models/WorkTrackingSystemOptionsOwnerCategoryListsTest.cs` | AllStates_RawListWithAMappingOverIt_HoldsEveryRawStateOnce | owner view, split category lookup, raw reads | green | happy path |
| `Tests/Models/CategoryListNormalisationTest.cs` | ClassifyListEntry_NameOfAGroupThatIsOneOfItsOwnStates_IsAStateOfTheTracker | normalisation: set-equivalence, idempotence, rollback | pending | happy path |
| `Tests/Models/CategoryListNormalisationTest.cs` | ClassifyListEntry_NameOfAGroupWhoseStatesAreNotListed_IsACollapsedGroup | normalisation: set-equivalence, idempotence, rollback | pending | happy path |
| `Tests/Models/CategoryListNormalisationTest.cs` | ClassifyListEntry_NameOfAGroupBesideOneOfItsStates_IsShadowing | normalisation: set-equivalence, idempotence, rollback | pending | happy path |
| `Tests/Models/CategoryListNormalisationTest.cs` | ClassifyListEntry_StateOfTheGroupIsListedOnlyInAnotherList_IsStillACollapsedGroup | normalisation: set-equivalence, idempotence, rollback | pending | happy path |
| `Tests/Models/CategoryListNormalisationTest.cs` | ClassifyListEntry_NameWrittenInOtherCasingWithSpaces_IsRecognisedAsTheGroup | normalisation: set-equivalence, idempotence, rollback | pending | edge |
| `Tests/Models/CategoryListNormalisationTest.cs` | ClassifyListEntry_NameOfNoGroup_IsAStateOfTheTracker | normalisation: set-equivalence, idempotence, rollback | pending | happy path |
| `Tests/Models/CategoryListNormalisationTest.cs` | ClassifyListEntry_NameOfAMappingWithNoStates_IsAStateOfTheTracker | normalisation: set-equivalence, idempotence, rollback | pending | error path |
| `Tests/Models/CategoryListNormalisationTest.cs` | NormalisedStatesFor_NameOfAMappingWithNoStates_StaysInTheList | normalisation: set-equivalence, idempotence, rollback | pending | error path |
| `Tests/Models/CategoryListNormalisationTest.cs` | ClassifyListEntry_AnyGeneratedList_NamesEveryGroupEntryTheWayItWasBuilt | normalisation: set-equivalence, idempotence, rollback | pending | property |
| `Tests/Models/CategoryListNormalisationTest.cs` | NormalisedStatesFor_GroupNamedAfterItsOwnState_ListsTheOtherStatesRightAfterIt | normalisation: set-equivalence, idempotence, rollback | pending | happy path |
| `Tests/Models/CategoryListNormalisationTest.cs` | NormalisedStatesFor_CollapsedGroup_IsReplacedByItsStatesInPlace | normalisation: set-equivalence, idempotence, rollback | pending | happy path |
| `Tests/Models/CategoryListNormalisationTest.cs` | NormalisedStatesFor_ListThatAlreadyHoldsEveryState_PassesThroughInTheSameOrder | normalisation: set-equivalence, idempotence, rollback | pending | happy path |
| `Tests/Models/CategoryListNormalisationTest.cs` | NormalisedStatesFor_SameStateInDifferentCasing_KeepsTheFirstSpelling | normalisation: set-equivalence, idempotence, rollback | pending | edge |
| `Tests/Models/CategoryListNormalisationTest.cs` | NormalisedStatesFor_NoMappings_ReturnsTheListWithoutRepeats | normalisation: set-equivalence, idempotence, rollback | pending | edge |
| `Tests/Models/CategoryListNormalisationTest.cs` | NormalisedStatesFor_EmptyList_StaysEmpty | normalisation: set-equivalence, idempotence, rollback | pending | edge |
| `Tests/Models/CategoryListNormalisationTest.cs` | NormalisedStatesFor_EntriesThatAreNullEmptyOrOnlyWhitespace_AreLeftOut | normalisation: set-equivalence, idempotence, rollback | pending | edge |
| `Tests/Models/CategoryListNormalisationTest.cs` | NormalisedStatesFor_AnyGeneratedList_MeansTheSameStatesAsTheListBeforeIt | normalisation: set-equivalence, idempotence, rollback | pending | property |
| `Tests/Models/CategoryListNormalisationTest.cs` | NormalisedStatesFor_AnyGeneratedList_ChangesNothingTheSecondTime | normalisation: set-equivalence, idempotence, rollback | pending | property |
| `Tests/Models/CategoryListNormalisationTest.cs` | NormalisedStatesFor_AnyGeneratedList_KeepsAGroupNameOnlyWhereItIsOneOfTheGroupsOwnStates | normalisation: set-equivalence, idempotence, rollback | pending | property |
| `Tests/Models/CategoryListNormalisationTest.cs` | AnOlderBackend_ReadingANormalisedList_FindsTheSameStatesAsBeforeTheUpgrade | normalisation: set-equivalence, idempotence, rollback | pending | edge |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingRawStatesWithAGroup_ReadsBackTheSameStatesAndTheGroupOnDisplay | settings HTTP: write path and read model (Team and Portfolio) | pending | happy path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingGroupStatesThatAreApart_ShowsTheGroupWhereItsFirstStateIs | settings HTTP: write path and read model (Team and Portfolio) | pending | edge |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingAGroupOverToDoStates_ShowsItInToDo | settings HTTP: write path and read model (Team and Portfolio) | pending | happy path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingAGroupOverDoneStates_ShowsItInDone | settings HTTP: write path and read model (Team and Portfolio) | pending | happy path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingAGroupNamedAfterOneOfItsStatesWithoutTheOthers_AddsTheOthersRightAfterIt | settings HTTP: write path and read model (Team and Portfolio) | pending | happy path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingTheGroupsNameInPlaceOfItsStates_StoresTheStatesInPlace | settings HTTP: write path and read model (Team and Portfolio) | pending | happy path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingTheOnlyEntryAsTheGroupsName_StoresTheGroupsStateInstead | settings HTTP: write path and read model (Team and Portfolio) | pending | happy path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingAnOwnersUnchangedSettings_WhileItsStoredListHoldsACollapsedGroup_WritesRawStates | settings HTTP: write path and read model (Team and Portfolio) | pending | happy path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingTheGroupAndTheListInOneSave_ReadsTheListAgainstTheGroupJustSent | settings HTTP: write path and read model (Team and Portfolio) | pending | happy path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingTheListWithoutTheGroupThatStoodBehindAState_DoesNotBringTheGroupsOtherStatesBack | settings HTTP: write path and read model (Team and Portfolio) | green | happy path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingTheSameSettingsTwice_ChangesNothingTheSecondTime | settings HTTP: write path and read model (Team and Portfolio) | pending | edge |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingANameBesideOneOfItsGroupsStates_IsRefusedWithTheSentenceTheSettingsPageShows | settings HTTP: write path and read model (Team and Portfolio) | pending | error path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingAGroupOverAState_WhoseEarlierGroupWasDeleted_LeavesNoLeftoverName | settings HTTP: write path and read model (Team and Portfolio) | pending | regression probe |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingAGroupThatListsAnotherGroupsName_IsRefusedWithTheChainSentence | settings HTTP: write path and read model (Team and Portfolio) | pending | error path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingAGroupWithAStateListedNowhere_IsAcceptedWithoutAWarning | settings HTTP: write path and read model (Team and Portfolio) | pending | edge |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingANewGroupWhoseListedStatesSitInDifferentCategories_IsRefusedNamingTheTwoStates | settings HTTP: write path and read model (Team and Portfolio) | pending | error path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingAGroupWhoseOnlyStateIsListedInTwoCategories_IsRefused | settings HTTP: write path and read model (Team and Portfolio) | pending | error path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingAnUnrelatedChange_WhileAnOlderGroupSpansCategories_StillSaves | settings HTTP: write path and read model (Team and Portfolio) | green | edge |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingListsWithNullAndBlankEntries_LeavesThemOut | settings HTTP: write path and read model (Team and Portfolio) | pending | error path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingAGroupWithABlankNameAlongsideLists_IsRefused | settings HTTP: write path and read model (Team and Portfolio) | green | error path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingAGroupWithNoStatesAlongsideLists_IsRefused | settings HTTP: write path and read model (Team and Portfolio) | green | error path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SavingTwoGroupsWithTheSameName_IsRefused | settings HTTP: write path and read model (Team and Portfolio) | green | error path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | ReadingAnOwnerSavedBeforeTheUpgrade_AfterTheUpgradeRan_ShowsRawStatesAndTheGroupOnDisplay | settings HTTP: write path and read model (Team and Portfolio) | pending | happy path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SuggestingStatesAfterSavingRawStatesWithAGroup_OffersTheStatesAndNotTheGroupsName | settings HTTP: write path and read model (Team and Portfolio) | green | happy path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | ReadingAnOwnerWhoseStoredListStillHoldsAGroupName_ShowsTheGroupsStatesAndTheGroupOnDisplay | settings HTTP: write path and read model (Team and Portfolio) | pending | happy path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | ReadingAnOwnerWhoseGroupHasNoStates_KeepsTheEntryAsAStateAndDoesNotFail | settings HTTP: write path and read model (Team and Portfolio) | pending | error path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | ReadingAnOwnerWhoseStoredListHoldsAGroupNameBesideOneOfItsStates_ShowsTheGroupOnceAsTheOldExpansionDid | settings HTTP: write path and read model (Team and Portfolio) | pending | happy path |
| `Tests/API/Integration/CategoryListsHoldRawStatesSettingsIntegrationTest.cs` | SuggestingStatesWhileAnOwnersStoredListStillHoldsAGroupName_OffersTheStatesAndNotTheName | settings HTTP: write path and read model (Team and Portfolio) | pending | happy path |
| `Tests/API/Helpers/StateMappingValidatorRawListsTest.cs` | NameBesideOneOfItsGroupsStatesInDoing_IsRefusedWithTheSentenceTheSettingsPageShows | validator rules over raw lists | pending | error path |
| `Tests/API/Helpers/StateMappingValidatorRawListsTest.cs` | NameBesideOneOfItsGroupsStatesInToDo_IsRefusedNamingTheToDoList | validator rules over raw lists | pending | error path |
| `Tests/API/Helpers/StateMappingValidatorRawListsTest.cs` | NameBesideOneOfItsGroupsStatesInDone_IsRefusedNamingTheDoneList | validator rules over raw lists | pending | error path |
| `Tests/API/Helpers/StateMappingValidatorRawListsTest.cs` | NameStandingAloneInPlaceOfItsGroupsStates_IsNotRefused | validator rules over raw lists | green | edge |
| `Tests/API/Helpers/StateMappingValidatorRawListsTest.cs` | EntryNamedLikeAGroupAndAlsoOneOfItsStates_IsNotShadowing | validator rules over raw lists | green | regression probe |
| `Tests/API/Helpers/StateMappingValidatorRawListsTest.cs` | GroupThatListsAnotherGroupsName_IsRefusedWithTheChainSentence | validator rules over raw lists | pending | error path |
| `Tests/API/Helpers/StateMappingValidatorRawListsTest.cs` | GroupWithAStateListedNowhere_IsValid | validator rules over raw lists | green | edge |
| `Tests/API/Helpers/StateMappingValidatorRawListsTest.cs` | GroupWhoseListedStatesSitInDifferentCategories_IsRefusedNamingTheTwoStates | validator rules over raw lists | pending | error path |
| `Tests/API/Helpers/StateMappingValidatorRawListsTest.cs` | GroupWhoseOnlyStateIsListedInTwoCategories_IsRefused | validator rules over raw lists | pending | error path |
| `Tests/API/Helpers/StateMappingValidatorRawListsTest.cs` | TwoGroupsOverTheSameState_AreRefusedWithoutBlamingAListEntry | validator rules over raw lists | green | error path |
| `Tests/API/Helpers/StateMappingValidatorRawListsTest.cs` | GroupWithABlankName_IsRefusedWhateverTheListsHold | validator rules over raw lists | green | error path |
| `Tests/API/Helpers/StateMappingValidatorRawListsTest.cs` | GroupWithNoStates_IsRefusedWhateverTheListsHold | validator rules over raw lists | green | error path |
| `Tests/API/Helpers/StateMappingValidatorRawListsTest.cs` | TwoGroupsWithTheSameName_AreRefusedOnce | validator rules over raw lists | green | error path |
| `Tests/API/Helpers/StateMappingValidatorRawListsTest.cs` | ListsWithNullAndBlankEntries_DoNotMakeTheValidatorThrow | validator rules over raw lists | green | error path |
| `Tests/API/Helpers/TeamListSyncRawStatesTest.cs` | SyncTeamWithTeamSettings_GroupNamedAfterItsOwnState_StoresTheOtherStatesRightAfterIt | sync order, normalisation, refinement on a group | pending | happy path |
| `Tests/API/Helpers/TeamListSyncRawStatesTest.cs` | SyncTeamWithTeamSettings_CollapsedGroupFromTheCurrentEditor_StoresItsStatesInPlace | sync order, normalisation, refinement on a group | pending | happy path |
| `Tests/API/Helpers/TeamListSyncRawStatesTest.cs` | SyncTeamWithTeamSettings_GroupChangedInTheSameSave_ReadsTheListAgainstTheGroupJustSent | sync order, normalisation, refinement on a group | pending | happy path |
| `Tests/API/Helpers/TeamListSyncRawStatesTest.cs` | SyncTeamWithTeamSettings_GroupDeletedInTheSameSave_ReadsTheListWithoutIt | sync order, normalisation, refinement on a group | green | regression probe |
| `Tests/API/Helpers/TeamListSyncRawStatesTest.cs` | SyncTeamWithTeamSettings_RawStatesWithAGroup_KeepsTheListAsSent | sync order, normalisation, refinement on a group | green | happy path |
| `Tests/API/Helpers/TeamListSyncRawStatesTest.cs` | SyncTeamWithTeamSettings_EntriesThatAreNullOrOnlyWhitespace_AreLeftOut | sync order, normalisation, refinement on a group | pending | error path |
| `Tests/API/Helpers/TeamListSyncRawStatesTest.cs` | SyncTeamWithTeamSettings_RefinementChosenOnAGroup_KeepsTheGroup | sync order, normalisation, refinement on a group | pending | happy path |
| `Tests/API/Helpers/StateMappingRuleCasesTest.cs` | TwoMappingNames_AreTheSameExactlyWhenTheSharedCasesSayTheyAre | shared fold/blank cases, backend half | green | happy path |
| `Tests/API/Helpers/StateMappingRuleCasesTest.cs` | TwoMappingNames_DifferingOnlyInWhitespaceAtTheEnds_AreTheSame | shared fold/blank cases, backend half | pending | edge |
| `Tests/API/Helpers/StateMappingRuleCasesTest.cs` | ANameOfNothingButWhitespace_IsBlank | shared fold/blank cases, backend half | green | edge |
| `Tests/Services/Implementation/MetricsAsOfGroupNameTest.cs` | TeamLookingBack_ItemWasInAGroupWhoseNameIsNoStateOfTheTracker_WasStillInDoing | as-of category of a group name | pending | regression probe |
| `Tests/Services/Implementation/MetricsAsOfGroupNameTest.cs` | PortfolioLookingBack_FeatureWasInAGroupWhoseNameIsNoStateOfTheTracker_WasStillInDoing | as-of category of a group name | pending | regression probe |
| `Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowStateSpanMapperRawListsTest.cs` | WhenWorkStarted_StoredDoingListHoldsTheGroupName_IsTheArrivalIntoOneOfItsStates | connector reads lists as they are (ServiceNow) | green | happy path |
| `Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowStateSpanMapperRawListsTest.cs` | WhenWorkFinished_StoredDoneListHoldsTheGroupName_IsTheArrivalIntoOneOfItsStates | connector reads lists as they are (ServiceNow) | green | happy path |
| `Tests/Services/Implementation/WorkTrackingConnectors/ServiceNow/ServiceNowStateSpanMapperRawListsTest.cs` | WhenWorkWasQueued_StoredToDoListHoldsTheGroupName_IsTheLastReturnIntoOneOfItsStates | connector reads lists as they are (ServiceNow) | green | happy path |
| `Tests/Factories/IssueFactoryRawListsTest.cs` | CreateIssueFromJson_StoredDoingListHoldsTheGroupName_DatesTheStartByTheArrivalIntoOneOfItsStates | connector reads lists as they are (Jira) | green | happy path |
| `Tests/Factories/IssueFactoryRawListsTest.cs` | CreateIssueFromJson_StoredDoneListHoldsTheGroupName_DatesTheCloseByTheArrivalIntoOneOfItsStates | connector reads lists as they are (Jira) | green | happy path |
| `Tests/Services/Implementation/WorkTrackingConnectors/Csv/CsvDemoJourneyWithStateMappingTest.cs` | DemoJourney_TeamGroupsTwoOfItsDoingStates_NeverMovesAnItemFromTheGroupToItself | CSV demo journey over display states | pending | edge |
| `Tests/Services/Implementation/WorkTrackingConnectors/Csv/CsvDemoJourneyWithStateMappingTest.cs` | DemoJourney_GroupSitsFirstInDoing_StartsTheJourneyInTheGroup | CSV demo journey over display states | pending | happy path |
| `Tests/Architecture/CategoryListOwnershipGuardTest.cs` | NoCodeExpandsACategoryList | no list expansion outside mapped-name readers (covers ADO) | pending | happy path |
| `Tests/Architecture/CategoryListOwnershipGuardTest.cs` | OnlyTheSettingsSyncAndTheSeederPutAListOnAnOwner | no list expansion outside mapped-name readers (covers ADO) | green | happy path |
| `Tests/Architecture/CategoryListOwnershipGuardTest.cs` | OnlyCodeThatReadsSettingsNamingAGroupCallsTheExpansion | no list expansion outside mapped-name readers (covers ADO) | pending | happy path |
| `Tests/Services/Implementation/Seeding/CategoryListNormalisationSeederTests.cs` | Seed_SaveFails_LogsAWarningAndLetsTheStartContinue | seeder: save failure, nothing to rewrite | pending | error path |
| `Tests/Services/Implementation/Seeding/CategoryListNormalisationSeederTests.cs` | Seed_SaveFailsForTheFirstOwner_StillRewritesTheSecond | seeder: save failure, nothing to rewrite | pending | error path |
| `Tests/Services/Implementation/Seeding/CategoryListNormalisationSeederTests.cs` | Seed_MappingWithNoStates_LogsOneWarningWithAFixedRuleAndLeavesTheOwnerAlone | seeder: save failure, nothing to rewrite | pending | error path |
| `Tests/Services/Implementation/Seeding/CategoryListNormalisationSeederTests.cs` | Seed_NothingToRewrite_SavesNothing | seeder: save failure, nothing to rewrite | pending | edge |
| `Tests/Services/Implementation/Seeding/CategoryListNormalisationSeederTests.cs` | Seed_EveryLineItWrites_CarriesOnlyTheOwnerTypeIdCountsAndAFixedRule | seeder: save failure, nothing to rewrite | pending | happy path |
| `Tests/Integration/Containers/CategoryListNormalisationSeederOnBothProvidersTests.cs` | EveryStoredShape_IsRewrittenOnceAndLeftAloneTheSecondTime | seeder on SQLite and Postgres: shapes, idempotence, unreadable owner | pending | happy path |
| `Tests/Integration/Containers/CategoryListNormalisationSeederOnBothProvidersTests.cs` | AnOwnerWhoseGroupHasNoStates_IsLeftAloneWithAWarning_AndTheOthersAreStillRewritten | seeder on SQLite and Postgres: shapes, idempotence, unreadable owner | pending | error path |
| `Tests/Integration/Containers/CategoryListNormalisationSeederIsolationTests.cs` | AnOwnerWhoseWriteIsRefused_IsSkipped_AndTheOwnersAfterItAreStillWritten | seeder: one owner's refused write does not hold back the others (SQLite) | pending | error path |
| `Tests/Models/StateListCasesTest.cs` | Generate_ThreeHundredCases_HoldEveryKindOfEntryAndEveryKindOfPadding | the generator really produces the shapes the properties claim | green | happy path |
| `Tests/Models/StateListCasesTest.cs` | Generate_Twice_GivesTheSameCases | the generator really produces the shapes the properties claim | green | edge |
| `src/utils/stateMappingView.test.ts` | shows one group in the place of states that sit next to each other | frontend display view and shared rule cases | pending | happy path |
| `src/utils/stateMappingView.test.ts` | puts a group where its first listed state is when its states are apart | frontend display view and shared rule cases | pending | edge |
| `src/utils/stateMappingView.test.ts` | treats the same state in another casing or with spaces around it as one | frontend display view and shared rule cases | pending | edge |
| `src/utils/stateMappingView.test.ts` | shows a group named after one of its own states once | frontend display view and shared rule cases | pending | happy path |
| `src/utils/stateMappingView.test.ts` | leaves a list alone when none of a group's states are in it | frontend display view and shared rule cases | pending | happy path |
| `src/utils/stateMappingView.test.ts` | leaves a state exactly as stored once the group that held it is deleted | frontend display view and shared rule cases | pending | regression probe |
| `src/utils/stateMappingView.test.ts` | leaves out entries that are empty or only whitespace | frontend display view and shared rule cases | pending | edge |
| `src/utils/stateMappingView.test.ts` | recognises states padded with tabs and non-breaking spaces as their group | frontend display view and shared rule cases | pending | edge |
| `src/utils/stateMappingView.test.ts` | shows the list as it is when there are no groups | frontend display view and shared rule cases | pending | happy path |
| `src/utils/stateMappingView.test.ts` | tells the same names apart in plain letters | frontend display view and shared rule cases | green | edge |
| `src/utils/stateMappingView.test.ts` | tells the same names apart in every case of the shared list | frontend display view and shared rule cases | pending | edge |
| `src/utils/stateMappingView.test.ts` | folds the names of the shared list to the same key exactly when the server does | frontend display view and shared rule cases | pending | happy path |
| `src/utils/stateMappingView.test.ts` | ignores whitespace at the ends the way the server does | frontend display view and shared rule cases | pending | edge |
| `src/utils/stateMappingView.test.ts` | calls a name of nothing but whitespace blank, whichever whitespace it is | frontend display view and shared rule cases | pending | edge |
| `src/utils/stateMappingView.test.ts` | calls an empty name and a name of ordinary spaces blank | frontend display view and shared rule cases | green | edge |
| `src/utils/stateMappingValidation.rawLists.test.ts` | refuses a name that sits beside one of its mapping's states, with the sentence the server sends | frontend validation mirror | green | error path |
| `src/utils/stateMappingValidation.rawLists.test.ts` | holds back the autosave for that name with the same sentence | frontend validation mirror | green | happy path |
| `src/utils/stateMappingValidation.rawLists.test.ts` | does not refuse a name that stands alone in place of its mapping's states | frontend validation mirror | green | edge |
| `src/utils/stateMappingValidation.rawLists.test.ts` | names the To Do list when the shadowing sits there | frontend validation mirror | green | happy path |
| `src/utils/stateMappingValidation.rawLists.test.ts` | names the Done list when the shadowing sits there | frontend validation mirror | green | happy path |
| `src/utils/stateMappingValidation.rawLists.test.ts` | does not refuse a name that stands alone in To Do or Done in place of its mapping's states | frontend validation mirror | pending | edge |
| `src/utils/stateMappingValidation.rawLists.test.ts` | refuses a mapping that groups another mapping's name, with the sentence the server sends | frontend validation mirror | green | error path |
| `src/utils/stateMappingValidation.rawLists.test.ts` | refuses a mapping whose listed states sit in different categories, naming the two states | frontend validation mirror | pending | error path |
| `src/utils/stateMappingValidation.rawLists.test.ts` | refuses a mapping whose only state is listed in two categories | frontend validation mirror | pending | error path |
| `src/utils/stateMappingValidation.rawLists.test.ts` | says nothing about a mapping state that is in none of the lists | frontend validation mirror | green | edge |
| `src/utils/stateMappingValidation.rawLists.test.ts` | accepts a mapping named after one of its own states | frontend validation mirror | green | happy path |
| `src/utils/stateMappingValidation.rawLists.test.ts` | reports two mappings over the same state, whatever the Doing list holds | frontend validation mirror | green | happy path |
| `src/utils/stateMappingValidation.rawLists.test.ts` | reports two mappings with the same name once | frontend validation mirror | green | happy path |
| `src/utils/stateMappingValidation.rawLists.test.ts` | reports a mapping with no states, with the lists in view | frontend validation mirror | green | happy path |
| `src/utils/stateMappingValidation.rawLists.test.ts` | does not stumble on blank entries in the lists | frontend validation mirror | green | error path |
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
| `src/components/Common/StateMappings/StateMappingsEditor.rawLists.test.tsx` | says nothing about a mapping state that is in none of the lists | UI3, UI6 | green | edge |
| `src/pages/Teams/Detail/TeamMetricsView.agingColumns.test.tsx` | shows a loading frame, and no columns, while the settings are on their way | UI5 on the Team page | pending | happy path |
| `src/pages/Teams/Detail/TeamMetricsView.agingColumns.test.tsx` | says the columns could not be loaded when the settings fetch fails | UI5 on the Team page | pending | error path |
| `src/pages/Teams/Detail/TeamMetricsView.agingColumns.test.tsx` | shows the columns as the settings display them, groups in place of their states | UI5 on the Team page | pending | happy path |
| `src/pages/Teams/Detail/TeamMetricsView.agingColumns.test.tsx` | does not let a late answer for the team it left replace the columns of the team it shows | UI5 on the Team page | pending | error path |
| `src/pages/Portfolios/Detail/PortfolioMetricsView.agingColumns.test.tsx` | shows a loading frame, and no columns, while the settings are on their way | UI5 on the Portfolio page | pending | happy path |
| `src/pages/Portfolios/Detail/PortfolioMetricsView.agingColumns.test.tsx` | says the columns could not be loaded when the settings fetch fails | UI5 on the Portfolio page | pending | error path |
| `src/pages/Portfolios/Detail/PortfolioMetricsView.agingColumns.test.tsx` | shows the columns as the settings display them, groups in place of their states | UI5 on the Portfolio page | pending | happy path |
| `src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx` | shows a lone spinner and no chart while the columns load | UI5 on the aging widget frame | pending | happy path |
| `src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx` | says it couldn't be loaded when the columns could not be fetched, and draws no empty chart | UI5 on the aging widget frame | pending | error path |
| `src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx` | shows the chart once the columns have arrived | UI5 on the aging widget frame | green | happy path |
| `src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx` | leaves every other chart to load on its own while the columns are missing | UI5 on the aging widget frame | green | happy path |
| `src/components/Common/Team/ModifyTeamSettings.test.tsx` | shows the could-not-load words when the settings cannot be fetched | settings editor could-not-load words | pending | error path |

Green at hand-off (they pin what must keep holding): the sync keeps a raw list as sent and a group deleted in the
same save does not bring its other states back; an unrelated save succeeds while an older group spans categories;
two groups over one state, a blank group name, a group with no states and two groups of one name are refused
with the existing sentences; a name standing alone in place of its group's states is not refused by the validator;
a group named after its own state is valid; a group with a state listed nowhere is valid and silent; the
suggestions endpoint offers the states and not the group's name; the page and the server agree on plain-letter
duplicate names and ordinary blank names; the settings page already refuses a name beside one of its group's
states and a chain with the server's sentences; plain-state removal, an empty mapping list, and the aging frame
reading ready and leaving other charts alone; the source-scan guard that only the settings sync and the seeder
put a list on an owner.

**Cross-references (pinned elsewhere, not repeated).**

| Symptom (RCA section 2) | Pinned by |
|---|---|
| S3: P3 add-mapping autosave with an empty mapping, P3b/c a name with no states or states with no name, P4 two mappings over one state | Part (b), `docs/feature/bug-6458-empty-mapping-autosave/`: `useModifySettings.autosave.test.ts` ("autosaves without the empty mapping and keeps the row in the editor"), `stateMappingValidation.test.ts`, `StateMappingValidatorTest.cs`, and the controller `*_InvalidStateMappings_ReturnsBadRequest` cases. This feature adds, on the backend with lists in view, the blank-name, no-states, duplicate-name and shared-state refusals (rows marked error path above) |
| UI1 tooltip copy | `StatesList.stateMappings.test.tsx` > "names the mapping's states in a tooltip that says how to remove it", which asserts the tooltip text equals `State mapping: Code Review, Testing. Delete it under State Mappings to remove it.` |
| Settings editor loading look (DESIGN "Loading and failure looks") | Existing: `ModifyTeamSettings.test.tsx` (shows `Loading...` until the settings arrive, line 298) and `useModifySettings.test.ts` > "sets loading=true then false after fetch". Not duplicated |
| Settings editor could-not-load look | Existing: `useModifySettings.test.ts` > "handles fetch error gracefully (loading becomes false, settings stays null)". Not duplicated |
| Failed save keeps the unsaved edit and shows the saved-failed indicator | Existing: `useModifySettings.autosave.test.ts` > "keeps the edit and offers a retry when the save fails". Not duplicated |
| Aging widget loading / could-not-load / ready | New: the three `*.agingColumns.test.tsx` and `BaseMetricsView.loading.test.tsx` rows, with the real frame and `COULD_NOT_LOAD_MESSAGE` |

**Inner loop.** Unit tests on the validator, the sync and the pure helpers (`StateListCases` properties, display
and lookup) sit beside the code they cover, by the project's convention. The driving-port coverage is the HTTP
fixture (Team and Portfolio through `WebApplicationFactory`), the registered seeders run through the container,
and the Vitest component scenarios; the unit rows are the fast inner loop for the same behaviour, not a
substitute for them.

**Restore-backup and the seeder.** `DatabaseManagementService.RestoreBackup` and `ClearDatabase` both call the
private `MigrateAndSeedDatabase`, which runs every registered `ISeeder`. The existing
`DatabaseManagementServiceTest` (`ClearDatabase_Success_SeedsTheFreshDatabase`) pins that the generic seeding
runs, and a restore-side verify needs the same long mock setup for no new information. The scenario that adds
the information is `ReadingAnOwnerSavedBeforeTheUpgrade_AfterTheUpgradeRan_ShowsRawStatesAndTheGroupOnDisplay`:
it runs every registered seeder (`GetServices<ISeeder>()`, as the restore path does) over a collapsed owner, so
it also fails until the new seeder is registered in `Program.cs`. No separate restore test was written.

**Suggestions endpoint.** The RCA cited `SuggestionsController.cs:70-72` and `:87-89`; today they read each owner's
stored lists directly. Under D12 the endpoint reads the normalised owner lists, so the controller does change and
the design's component row ("stops returning mapping names") stands. Two scenarios:
`SuggestingStatesAfterSavingRawStatesWithAGroup_OffersTheStatesAndNotTheGroupsName` (raw stored lists, green) and
`SuggestingStatesWhileAnOwnersStoredListStillHoldsAGroupName_OffersTheStatesAndNotTheName` (collapsed stored list,
pending: today it offers the group name).

**Normalise on read (D12).** The tests that read a collapsed stored list are green today, because the old
expansion already reads the members through, and they stay green through every slice: `AllStates`, `OpenStates`,
the legacy expansion for a shadowing list, the Jira factory (start and close dates) and the three ServiceNow dates
(`WhenWorkStarted`, `WhenWorkFinished`, `WhenWorkWasQueued`). They are guards against a read that takes the stored
list as it stands, not pending scenarios. The pending ones are the reads that need new code: the category lookup on
a collapsed list, `DisplayStatesFor`, the HTTP display lists for a collapsed and a shadowing stored owner, the
suggestions for a collapsed stored owner, and the reads of an owner whose group has no states (null or empty;
today a null group throws inside `GetRawStatesForCategory` and an empty one drops the entry). The earlier "raw state named like a group" connector
scenarios are gone: an entry equal to a group's name with none of the group's states beside it is a collapsed
group (D10), so that premise no longer exists. No scenario claims that a save repairs a skipped owner; the one
that is true is kept: `SavingAnOwnersUnchangedSettings_WhileItsStoredListHoldsACollapsedGroup_WritesRawStates`
(Team and Portfolio). The normalised save from today's editor is
`SavingTheGroupsNameInPlaceOfItsStates_StoresTheStatesInPlace`, with its sync-level twin
`SyncTeamWithTeamSettings_CollapsedGroupFromTheCurrentEditor_StoresItsStatesInPlace`.

**A skipped owner's grouped states are pinned for both shapes.** (i) A collapsed chip the start-up pass could not
rewrite: `Reads_CollapsedListTheStartUpPassCouldNotRewrite_FindTheGroupsStatesEverywhere` (owner reads),
`ReadingAnOwnerWhoseStoredListStillHoldsAGroupName_ShowsTheGroupsStatesAndTheGroupOnDisplay` (HTTP, raw and display
lists, Team and Portfolio), `AnOwnerWhoseWriteIsRefused_IsSkipped_AndTheOwnersAfterItAreStillWritten` (a real refused
save on SQLite, then `AllStates` of the refused owner still holds the group's states), plus the green reads above.
(ii) A group with no states: a mapping with no states, null or empty, is not a group, so an entry with its name stays
a state; the seeder logs one Warning with the fixed rule `EmptyGroup`, leaves the owner alone and does not throw, and
no read throws. Pinned for null and for empty at the owner (`Reads_MappingWithNoStates_...`, `ClassifyListEntry`,
`NormalisedStatesFor`), at the seeder (unit, and on SQLite and Postgres), and over HTTP
(`ReadingAnOwnerWhoseGroupHasNoStates_KeepsTheEntryAsAStateAndDoesNotFail`).

**Settings editor, could-not-load words.** `ModifyTeamSettings` passes `hasError={false}` to its loading frame, so
a failed settings fetch shows no words today; the frame's own text ("Error loading data. Please try again
later.") is pinned only in `LoadingAnimation.test.tsx`, and the editor's test mock ignored `hasError`. The design
assumes an existing could-not-load state; it exists in the frame but is not wired in the editor. New pending
scenario in `ModifyTeamSettings.test.tsx`; its mock now renders the frame's words from `hasError`.

**Source-scan guard** (`CategoryListOwnershipGuardTest`: only the owner expands a mapping name, and only the sync,
the seeder and the allow-listed seed-data factory write the lists). The writer test catches any assignment to or
mutation of one of the three lists through a member access, whatever the receiver is called, and a list set inside
the initialiser of a `new Team` or `new Portfolio`; files are allow-listed with their reasons: the settings sync
(both owners), the seeder, `DemoDataFactory` (demo lists of plain states, no mappings), `CycleTimeDefinitionValidator`
(a throwaway Team built from a payload to ask one question, never stored) and the Jira connector (fills a board
description for the settings page, not an owner). Proof that it bites, run twice: a planted production class with
`new Team { DoingStates = ... }`, and later one with `existingTeam.DoingStates = ...`, each turned the test red
naming only the planted file; both plants were removed and the test is green again.

The call-shape regex of the expansion guard stays narrow on purpose: it catches an inline
`GetRawStatesForCategory(x.DoingStates)`. A call through a variable or a parameter (the ServiceNow mapper passes
the list down) is caught by the second guard test, which allows only the files that read settings naming a
group, whatever the call shape. Under D12 the connectors read the owner's normalised view, so they still call
no expansion on a list.

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
| Suggestions endpoint | `SuggestingStatesAfterSavingRawStatesWithAGroup_OffersTheStatesAndNotTheGroupsName` (Team and Portfolio route, raw stored lists, green) and `SuggestingStatesWhileAnOwnersStoredListStillHoldsAGroupName_OffersTheStatesAndNotTheName` (collapsed stored list, pending); under D12 the controller reads the normalised owner lists, see "Suggestions endpoint" under the inventory |
| Metrics endpoints | no HTTP scenario: the shape is unchanged; the as-of lookup and the column order are pinned at the service, the page and the widget frame |
| Settings editor, States list, metrics pages | Vitest scenarios (UI1-UI6) |

## Wave: DISTILL / [REF] Scaffolds

Every stub compiles into the solution with zero warnings; production behaviour is unchanged by all of them.

| Where | Scaffold | Marker |
|---|---|---|
| `Models/WorkTrackingSystemOptionsOwner.cs` | `DisplayStatesFor(StateCategories)`, `CategoryOfRawState(string)`, `CategoryOfDisplayName(string)`, `NormalisedStatesFor(IReadOnlyList<string>)`, `ClassifyListEntry(IReadOnlyList<string>, string)` (returns the new `ListEntryKind`: `NotAGroupName`, `StateOfItsOwnMapping`, `CollapsedGroup`, `Shadowing`): throw `InvalidOperationException("Not yet implemented -- RED scaffold ...")` | `// SCAFFOLD: true` |
| `Services/Implementation/Seeding/CategoryListNormalisationSeeder.cs` | class implementing `ISeeder`, constructor `(IRepository<Team>, IRepository<Portfolio>, ILogger<...>)`, `Seed()` throws. **Not registered in `Program.cs`** (DELIVER slice 3) | `// SCAFFOLD: true` |
| `API/DTO/SettingsOwnerDtoBase.cs` | `ToDoDisplayStates`, `DoingDisplayStates`, `DoneDisplayStates`: get-only, `[JsonIgnore]`, empty. They do **not** throw: a throwing getter would break every existing settings serialisation, and a public setter fails the existing round-trip guard (`SettingsOwnerDtoRoundTripTest`) | `// SCAFFOLD: true` |
| `Lighthouse.Frontend/src/utils/stateMappingView.ts` | new module: `foldKey`, `isBlankName`, `displayStatesFor` throw `Error("Not yet implemented -- RED scaffold ...")` | `export const __SCAFFOLD__ = true` |
| `models/Common/BaseSettings.ts` | optional `toDoDisplayStates`, `doingDisplayStates`, `doneDisplayStates` | doc comment `SCAFFOLD: true` |
| `components/Common/StatesList/StatesList.tsx` | optional prop `stateMappings` (not read yet) | doc comment `SCAFFOLD: true` |
| `pages/Common/MetricsView/BaseMetricsView.tsx` | optional prop `doingStatesStatus` (not read yet) | doc comment `SCAFFOLD: true` |

`grep -rn "SCAFFOLD: true\|__SCAFFOLD__" Lighthouse.Backend Lighthouse.Frontend/src` finds all of them. After DELIVER
none should remain. The pinned copy is the sentences the settings page already shows (shipped with Bug 6454),
which the server now sends too, with the list label varying (`To Do`, `Doing`, `Done`):
`Mapping "{name}" has the same name as the {list label} state "{state}". Add "{name}" to this mapping to group it, or choose another name.`
for a name beside one of its group's states, and
`Mapping "{name}" has the same name as a state grouped by mapping "{other}". Choose another name.` for a chain.
The earlier "Rename the mapping" and "another mapping" fragments were never approved and are gone. **The category sentences are UI7** (maintainer decision: default taken at 09:49 with no objection): `Mapping "Review" groups states from different categories: "Code Review" is
in To Do and "Testing" is in Doing. Keep a mapping's states in one category.` and, for one state in two categories,
`Mapping "Review" groups "Testing", which is in both To Do and Done. Keep a mapping's states in one category.`, both pinned
exactly on both stacks. Also pinned: the exact UI1 tooltip and UI3 help text, `data-testid="state-mapping-chip"` on
a mapping chip and `data-testid="state-mapping-chip-icon"` on its group icon; the seeder's summary line carries the
named values `Scanned`, `Rewritten`, `LegacyExpanded`, `Skipped`; every seeder line carries only `OwnerType`,
`OwnerId`, the counts and a fixed `Rule` (`Shadowing`, `Chained`, `Unreadable`, `SaveFailed`) and no exception text.

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

The dependency that changed the first guess: the seeder still comes before the connector and metrics read
switch, but since D12 the read switch no longer depends on the stored data being raw (reads normalise, and on a
rewritten list the normaliser is the identity). Each slice ships alone,
green, and leaves the old behaviour correct for the data still in the database.

| # | Slice | Turns on | Why it is safe alone |
|---|---|---|---|
| 1 | **Domain view and the as-of fix.** `DisplayStatesFor`, `CategoryOfRawState`, `CategoryOfDisplayName`, `NormalisedStatesFor`, `ClassifyListEntry` (D10); the two as-of sites use `CategoryOfDisplayName` | owner view/lookup tests, normalisation and rollback property tests, `MetricsAsOfGroupNameTest` | adds members and changes only the as-of lookup (a bug fix: `Unknown` becomes the right category); no list is read differently |
| 2 | **Write path, validation, DTO, refinement.** mappings synced before lists, normalised lists, shadowing/chain/category rules, three display lists on the DTO (drop the `JsonIgnore`, update the approved payload), `SyncRefinement` candidates from display states | HTTP fixture, validator, sync, shared-case backend half | new saves store raw states; reads still expand, which the equivalence property proves gives the same set |
| 3 | **Seeder.** the rewrite, `Program.cs` registration (this makes CI run the full Integration suite), logging contract | seeder cases on both providers, seeder unit cases | idempotent, skip-and-log; stored data becomes raw |
| 4 | **Read switch (normalise on read, D12).** `AllStates`/`OpenStates`/lookups read the owner's normalised view of the stored lists, ADO/Jira/ServiceNow sites, suggestions endpoint, CSV demo journey, metrics columns from display states, architecture guard; run the live `AdoIntegration` category once | owner read tests, connector tests, CSV, guard | a not-yet-seeded or skipped owner still loads every item, so it no longer has to wait for the data; review point 1 is resolved by D12 |
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
  the rollback re-collapse by the old settings page; "an owner named in the seeder's Warning keeps working;
  the seeder retries on each start"; the rolling-update overlap with more than one replica.
- `docs/concepts/api-versioning.md`, at feature finalization.
- Lighthouse-Clients skill text (`coaching-patterns.md`): `doingStates` is raw states, `doingDisplayStates` is
  what the charts show; rides on the next clients changeset.

Mutation testing runs last on frozen code (seeder, `DisplayStatesFor`, the lookups, the classifier, the validator, the frontend
mirror), as DEVOPS already says. DELIVER starts by agreeing the rigor profile.

## Wave: DISTILL / [REF] Revision round 1

Review-gate findings, answered. D10 and D11 are reflected in the tests: a list entry that equals a mapping's name
is classified once on the owner (`ClassifyListEntry`), a collapsed chip is normalised and never refused (HTTP, sync,
seeder), and only shadowing is a 400. The generator now covers padded, tabbed and non-breaking-space states,
other casing on names and states, collapsed chips and shadowing; the equivalence, idempotence and rollback
properties skip shadowing (the legacy expansion loses a state there by design), and the classifier has its own
property over every generated entry. The display property asserts invariants (each name once, in the order of its
first state, every state covered) instead of recomputing the grouping.

Decisions taken in the tests that the design did not state (for the next review):

1. Null, empty and whitespace-only list entries are dropped by the normaliser and the sync, never an error or a
   crash. Today a null entry crashes the save (`NullReferenceException`), which is the failure the tests show.
2. The category sentence is proposed copy (see "Scaffolds").
3. A group whose only state is listed in two categories is refused with the same category sentence naming the
   state twice.
4. The seeder's per-owner isolation is pinned two ways: mocked repositories (one `Save` per rewritten owner, the
   second owner still rewritten) and a real SQLite database where a trigger refuses one owner's update (the
   others are written, one skipped). Postgres is not repeated: what is tested is how the shared context is used.
5. The frontend still has no scenario for the could-not-load text of the settings editor itself; the existing
   hook test pins the state, not the words, and the design reuses the existing screen.

## Wave: DISTILL / [REF] Revision round 2

D12 (normalise on read), D10's drop step and precedence, the D11 allow-list and UI7 are reflected.

- **Reads.** The scenarios that said "read as they are" on a collapsed stored list are rewritten: a collapsed or
  shadowing stored list reads the members through `AllStates`, `OpenStates`, both category lookups, the Jira
  factory and the ServiceNow dates (green today, kept green as guards), and the pending ones need new code (the
  category lookup, the HTTP display lists, the suggestions, an unreadable owner's `AllStates`). The "raw state
  named like a group" connector scenarios are removed: under D10 that shape is a collapsed group. No text claims
  that a save repairs a skipped owner.
- **Guard (B1).** Widened to initialisers; `DemoDataFactory` and `CycleTimeDefinitionValidator` are allow-listed
  with reasons; shown red once with a planted writer, then removed.
- **Vacuous pass (B2).** The display property now builds the expected states from the stored input and the
  generator's own knowledge of what it built, and reads the owner as stored; it no longer overwrites the lists
  with the production normaliser's output.
- **Refusals leave nothing behind (N2).** After each of the seven refusals the lists and mappings are read back
  and must be as seeded. **Save status (N5)** is asserted wherever a save's result was dropped.
- **Generator (N4).** The comment describes the shape in words; a self-check test proves the leading-space, tab,
  non-breaking-space and upper-case variants are generated. `Trim()` in .NET and `trim()` in JavaScript both strip
  U+00A0 and U+2003, so the two agree; the shared cases now also pin a non-breaking-space pair and a tab pair.
- **Copy (UI7).** Both category sentences are pinned on both stacks.
- **Small items.** Pending reasons are comments above the `it.skip` rows; the editor's could-not-load words get a
  pending scenario (the editor never wires the frame's error state); the normalised save from today's editor is
  named in the inventory.
- **Red check.** Every pending test was run un-skipped again: 131 backend and 37 frontend fail on missing behaviour;
  `distill/red-classification.md` is regenerated, one short line per test (the Done-group row now says "no display
  lists").

## Wave: DISTILL / [REF] Revision round 3

- **Skipped or unreadable owner.** Pinned for both shapes (see the inventory): a collapsed chip the start-up pass
  could not rewrite still reads the group's states through `AllStates`, the category lookup, the display lists, the
  HTTP lists and, after a real refused save on SQLite, `AllStates` of the refused owner. A mapping with no states
  (null or empty) is not a group: the entry stays a state, the seeder logs a Warning with the rule `EmptyGroup`,
  nothing throws. The rule is in D10 and in `rca.md` section 6.2.
- **Wording that contradicted D12** is removed: the suggestions paragraph (the controller does change and reads the
  normalised lists), the driving-adapter row, the owner scaffold comment, the RCA note (now marks the as-is read as
  superseded), the precedence exclusion in `ListEntryKind` and in the RCA cases (a) to (c), the DemoDataFactory
  citation (`:36-38` and `:55-57`, checked against the file), and the guard test, renamed
  `CategoryListOwnershipGuardTest`.
- **Green, not pending.** `AllStates_RawListWithAMappingOverIt_HoldsEveryRawStateOnce` passes today and no longer
  carries a skip; the two frontend shadowing-sentence tests for To Do and Done also pass today (the shipped page
  already says it) and are live.
- **Non-breaking space.** The sources are now ASCII-only for this: a named constant `(char)160` or
  `String.fromCharCode(160)` replaces the literal character that editors had folded into a plain-looking space. The
  generator pads with a leading space, a trailing space, a tab, two spaces and U+00A0 on both sides; the owner test
  includes `" Testing"` and U+00A0-padded entries; the generator self-check asserts U+00A0 itself. A non-ASCII scan
  of the touched test sources finds only the shared JSON cases (which hold the character literally by design).
- **Copy.** UI8 records the shipped Doing and To Do/Done sentences; UI7 is recorded as taken by default at 09:49.
- **Red check.** All 145 backend and 35 frontend pending cases were run un-skipped again; `red-classification.md` is
  regenerated and the rows for the no-states reads and the suggestions are corrected.

