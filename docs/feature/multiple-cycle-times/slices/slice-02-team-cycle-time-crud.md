# Slice 02 - Team Cycle Times CRUD + mapping-aware picker + end-after-start validation

**Type:** vertical | **Est:** ~1 day | **Stories:** US-02

## Learning hypothesis

A named cycle time persists cleanly as a `CycleTimeDefinitions` List on the settings aggregate riding
the EXISTING settings write endpoint (twin of WaitStates - no new write contract), and the mapping-
aware, workflow-ordered boundary picker plus end-after-start validation give admins a self-explanatory
"a cycle time is just two boundary states" model.

## What ships

- Backend: `CycleTimeDefinitions` List of `{ name, startState, endState }` on
  `WorkTrackingSystemOptionsOwner`; carried on the settings DTO; save-time validation that `endState`
  is strictly after `startState` in `AllStates` (D4) and the name is non-empty + unique per owner.
- Frontend: a "Cycle Times" config in the flow-metrics/state-config cluster (Team) - Default shown
  read-only, add/edit/delete named definitions, two mapping-aware pickers in workflow order (D3) with
  an inline boundary preview. Premium + config-admin (team-admin) gated via `useRbac()`.
- Slice 01's hard-coded definition is replaced by reading real saved definitions into the selector.

## IN scope

- Team scope CRUD; end-after-start + name validation; read-your-writes on reload.
- Concurrency inherited free from epic-5121 tokened settings aggregate (no new surface).

## OUT of scope

- Invalid-on-removal handling (Slice 03), cumulative scope switch (Slice 04), Portfolio (Slice 05).

## Production-data AC

- Given Carlos opens Team Phoenix settings, when he adds "Concept to Cash" with start "Planned" and
  end "Done" and saves, then on reload the definition is present and appears in the scatterplot selector.
- Given Carlos picks end "Planned" with start "Done", when he saves, then the save is rejected inline
  with "End state must come after the start state in the workflow" and nothing is persisted.
- Given Carlos picks a State-Mapping name as a boundary, when the picker lists candidates, then they
  appear in workflow (AllStates) order and the mapping resolves via GetRawStatesForCategory.

## Taste tests

- Value-bearing: an admin defines a real reusable named cycle time. PASS.
- Right-sized: single owner scope, one CRUD surface, 3-5 scenarios. PASS.
