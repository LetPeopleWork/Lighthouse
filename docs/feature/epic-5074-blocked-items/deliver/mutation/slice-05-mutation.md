# Slice-05 Mutation Report — Jira flagged via a predefined additional field (Story 5269)

Date: 2026-07-11
Tooling: Stryker.NET 4.16.0 (backend) · StrykerJS (frontend)
Configs: `Lighthouse.Backend.Tests/stryker-config.epic-5074-blocked-items-slice05.json`,
`Lighthouse.Frontend/stryker.config.epic-5074-blocked-items-slice05.mjs` (local tooling, untracked).

## Result — GATE PASSED (≥80% both stacks)

| Stack    | Scope                                   | Killed | Survived | NoCov | Feature kill rate |
|----------|-----------------------------------------|--------|----------|-------|-------------------|
| Backend  | slice-05 changed line ranges (below)    | 30     | 3        | 2     | **85.71%**        |
| Frontend | `AdditionalFieldsEditor.tsx:170-173`    | 10     | 0        | 0     | **100.00%**       |

Backend feature kill rate is computed over the slice-05 CHANGED line ranges only (post-filtered from
the whole-file Stryker report), not the whole-file score — the mutated files (JiraWorkTrackingConnector.cs
~1200 lines, WorkTrackingSystemConnectionController.cs) carry large pre-existing surfaces out of this
slice's scope. Line ranges scored:

- `WorkTrackingSystemConnectionController.cs`: 24-60 (GET auto-registration), 184-196 (write-back inbound-only drop), 234-250 (reconcile predefined exclusion)
- `AdditionalFieldsHelper.cs`: slot-count `!IsPredefined` split
- `AdditionalFieldDefinitionDto.cs`: 16 (ctor map), 34-40 (ToModel map)
- `JiraWorkTrackingConnector.cs`: 57-108 (GetPredefinedAdditionalFields + Resolve + sync-path Ensure), 123-124 (sync call site)

Test-case-filter widened beyond the `Slice05`/`PredefinedAdditionalField` suites to include the owning
suites (`JiraWorkTrackingConnectorTest`, `WorkTrackingSystemConnectionControllerTest`, `…WriteBackTest`) —
the slice-05 behaviour is also driven by those, and excluding them produced false NoCoverage.

## Tests added during hardening

1. `Slice05AdditionalFieldDtoSerializationTest.ToModel_round_trips_the_is_predefined_flag` — killed the
   ToModel `IsPredefined` mapping mutant (DTO→model direction was untested).
2. `JiraWorkTrackingConnectorTest.GetPredefinedAdditionalFields_JiraConnection_ContributesFlaggedFieldAsPredefined`
   — pure unit test (no HTTP); killed `IsPredefined=true→false`, DisplayName, and default-Reference mutants.
3. `Slice05PredefinedFieldTest.A_predefined_field_with_a_stale_reference_is_reconciled_in_place_not_duplicated`
   — pins the get-or-create UPDATE branch (drift reconcile).

## Production hardening (review finding fix)

The GET-time `EnsurePredefinedAdditionalFieldsRegistered` dedup was add-only, keyed on the resolved
Reference (`.All(!IsPredefined || Reference != …)`). A drifted Reference (stable default resolved before the
flagged field key was known, real one after) would have appended a SECOND predefined row, orphaning
write-back and rule `additionalField.{id}` references. Refactored to get-or-create keyed on
`(IsPredefined, DisplayName)` with in-place Reference update — mirrors the connector sync-path so both
writers share ONE dedup key. Test #3 above locks the fix.

## Residual survivors (accepted, low value)

- `WorkTrackingSystemConnectionController.cs:43` — `var changed = false` → `true`: forces a no-op
  `repository.Update/Save` when nothing changed. No observable state difference (idempotent) — effectively equivalent.
- `WorkTrackingSystemConnectionController.cs:47` & `JiraWorkTrackingConnector.cs:92` — dedup `&&` → `||`:
  only observable when a NON-predefined field shares the predefined field's DisplayName ("Flagged"), a
  collision the connection model does not produce. Contrived to kill.
- `JiraWorkTrackingConnector.cs:92-104` — the connector sync-path mirror of the controller get-or-create.
  Logic is identical to the now-well-tested controller path; killing these needs live-Jira `GetWorkItemsForTeam`
  assertions on predefined-field registration (disproportionate for the value). Documented gap.
