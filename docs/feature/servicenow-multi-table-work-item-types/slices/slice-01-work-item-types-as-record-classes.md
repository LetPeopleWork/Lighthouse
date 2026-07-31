# Slice 01 — one team, several kinds of work

**Story**: B (feature-delta.md) · **ADO**: #5611 · **Effort**: ~1 day, no migration
**Order**: first. Carries every open call, needs no migration, and is the slice the dogfood asked for (D7).

## Goal

A flow coach whose team handles incidents and changes gets one Lighthouse team covering both, each
item labelled with its own kind.

## Learning hypothesis

**Confirms** D2 — that `sys_class_name` filtering on a hierarchy-rooted read is the right model for
"several kinds of work", and that Lighthouse's existing Work Item Types field is the exact right home
for it (a ServiceNow class name *is* a work item type, not an analogy for one).
**Disproves** D2 if either OC-1 (`^OR` class filtering does not bind as expected against `task`) or
OC-2 (ACL-filtered reads are indistinguishable from correct ones) fails on a real instance. In that
case the model falls back to per-team (table, query) pairs and the whole feature is re-scoped.

## IN scope

- The class filter: `Team.WorkItemTypes` becomes a `sys_class_name` clause prepended to the team's own
  encoded query, for a ServiceNow team rooted at a table with descendants.
- `Type` on a mapped item becomes `sys_class_name` (D4) — `ServiceNowWorkItemMapper.MapRecord`.
- Empty types on a hierarchy-rooted team reads nothing and says why (D3, AC-B3).
- `isWorkItemTypesRequired` becomes conditional on the configured table, in **both** schema twins
  (D6) — `DataRetrievalSchemaDto.cs` and `DataRetrievalSchemaDefaults.ts`, with the #5613
  exhaustiveness guard still passing.
- Docs: the `task`-root recipe and what to type into Work Item Types.

## OUT of scope

- Tables outside one hierarchy (`incident` + `rm_story`) — no common ancestor to root at.
- A class picker or dropdown; the field stays hand-typed as it is for every other connector.
- Changing the shipped default. A leaf-rooted team behaves exactly as it does today (AC-B5).

## Acceptance criteria

AC-B1..AC-B6 in `../feature-delta.md`.

## Dependencies

- Story 5577 landed and pushed.
- Bug #5613's schema-twin guard in place (shipped, `cb5f0efb0`).
- **OC-1 and OC-2 closed against a live instance.** This slice is not buildable until they are.

## Reference class

- `ServiceNowWorkTrackingConnector.cs:114-143` — `GetWorkItemsForTeam`, including the
  no-query-reads-nothing precedent that AC-B3 mirrors for the class dimension.
- `ServiceNowWorkItemMapper.MapRecord(record, owner, table)` — `Type = table` at `:89`.
- `useCreateWizard.ts:128`, `ModifyTeamSettings.tsx:76` — the existing
  `isWorkItemTypesRequired === false` gates; the components do not change, only what the schema says.

## Pre-slice SPIKE

**Yes — timeboxed, against the PDI or the on-prem instance.** Close OC-1 (does the `^OR` class filter
combine correctly with a team's own encoded query on `task`?) and OC-2 (what does a restricted account
see through `task`, and does the widening probe still mean anything?). Both are one-request
experiments; neither needs a build.
