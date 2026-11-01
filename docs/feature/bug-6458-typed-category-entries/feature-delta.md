# Bug 6458 (a): category lists cannot tell a raw state from a mapping name

ADO Bug #6458 (Active). Part (b) of the same item, the autosave of an unfinished mapping, shipped on its own
as `docs/feature/bug-6458-empty-mapping-autosave/` (commits `bfd39ec97`, `be3adace6`). This workspace is
part (a) only.

## Where we stopped (2026-10-31)

- **RCA done**, in `rca.md` here. The maintainer agreed both root causes and the split:
  part (a) is a design flaw (public API values, a data rewrite of stored lists, connector expansion sites,
  the settings editor), so it goes through **DESIGN** before any fix. No further frontend guard.
- **DESIGN is done (2026-11-01).** Shape (ii-b) was chosen; see "Wave: DESIGN" below and ADR-270.
  **Next: DEVOPS** (answer the usage-data event question), then DISTILL (walk the UI sketches with the
  maintainer at its start, one decision at a time), then DELIVER (agree the rigor profile first).
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
