# RCA — Bug #6089: Adding a new write-back mapping for a new additional field fails

## Symptom
Adding an additional field and, before saving, a write-back (data sync) mapping that references it, fails on save (500). Workaround today: save the field first, then add the mapping.

## Root cause
The frontend gives unsaved rows temporary negative ids (`AdditionalFieldsEditor.tsx:181`), and a mapping created against an unsaved field stores that negative id as `additionalFieldDefinitionId`. On save the backend resets a new field's own id to 0 (`AdditionalFieldDefinitionDto.cs:29-31`) so the database assigns a real one, but copies the mapping's `AdditionalFieldDefinitionId` (still negative) through unchanged (`WriteBackMappingDefinitionDto.cs:35-37`; update path `WorkTrackingSystemConnectionController.cs:~226-232`; create path `WorkTrackingSystemConnectionsController.cs:~238-243`). The FK to `AdditionalFieldDefinition` (`LighthouseAppContext.cs:459-464`) then fails on `SaveChanges`. `WriteBackMappingValidator.cs:20` rejects only `null or 0`, so a negative id is not caught.

Same defect: re-pointing an existing mapping at a new, unsaved field (update path).

## Fix direction (approved by the maintainer 2026-09-28)
Backend only, single save. One shared helper in `API/Helpers/`, used by both create and update:
- Before `ToModel()` resets ids, record `tempId -> new AdditionalFieldDefinition model` for each field DTO with `Id < 0`.
- For each new or updated mapping with `AdditionalFieldDefinitionId < 0`: if the temp id is known, set `mapping.AdditionalFieldDefinition = newField` and `AdditionalFieldDefinitionId = null` (EF fills the generated key in the same SaveChanges); otherwise return 400 "unknown additional field".
- Run `WriteBackMappingValidator.Validate` before the remap (or let line 20 accept `AdditionalFieldDefinition != null`).
- No frontend change: the payload already carries the matching negative id on field and mapping.

## Regression tests
- Backend integration (primary, `Lighthouse.Backend.Tests/API/Integration/`, SQLite with FKs on): PUT an existing connection with field `{id:-1}` + mapping `{id:-1, additionalFieldDefinitionId:-1}` → 200, persisted mapping FK == persisted field's generated id > 0. Twins: POST create; existing mapping moved to a new field. Fails today with 500.
- Backend unit (`API/WriteBack/WorkTrackingSystemConnectionControllerWriteBackTest.cs`): mapping handed to `repository.Update` references the same new field instance; unknown negative id → 400.
- Frontend Vitest (optional contract guard, `ModifyConnectionSettings.test.tsx`): payload carries mapping `additionalFieldDefinitionId ===` new field's negative id.
