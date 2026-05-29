# Slice 02 — Auto-save + auto-refresh State Mappings (D-RELOAD)

Story: US-5077-02 | job: job-commit-intent-no-button | Must Have

## Goal
Editing state mappings auto-saves (reusing Slice 1's mechanism) AND triggers the dependent data
reload automatically; the "After saving, a data reload is needed" Alert is removed.

## IN scope
- State-mappings auto-save via Slice-1 mechanism.
- **D-RELOAD (cheap → auto):** dependent metrics auto-refresh after a successful mapping save.
- One-click "Reload now" fallback if auto-refresh fails (never "go elsewhere and refresh").
- Remove the `StateMappingsEditor` "After saving, a data reload is needed" Alert (lines 109-111).

## OUT of scope
- Forecast filter (Slice 3), portfolio (Slice 4), forecasts (5/6).

## Learning hypothesis
**Confirms** that auto-save can be coupled to an automatic dependent-data refresh so the two-step
ceremony collapses to zero steps, and users trust the data is current without the deleted reminder Alert.
**Disproves** the cheap-auto-refresh choice if the refresh is too expensive/janky in practice —
then fall back to the D-RELOAD one-click action as the primary affordance.

## Acceptance criteria
- [ ] Valid mapping edit auto-saves (Slice 1 mechanism), no Save button.
- [ ] Dependent metrics refresh automatically after a successful mapping save.
- [ ] "After saving, a data reload is needed" Alert removed (grep + test confirm absence).
- [ ] Invalid mapping (empty group name) fires no save; inline error shown.
- [ ] Failed auto-refresh → in-place one-click "Reload now" (never "go elsewhere").

## Dependencies
Slice 1 (auto-save mechanism). May reuse Slice 3's reload wiring.

## Effort estimate
~1 day.

## Reference class
`StateMappingsEditor.tsx` (Alert removed); same `useModifySettings` save path (mappings feed the
same form Save → same `disableSave` gate as Slice 1). D-RELOAD cheap-auto analog: any in-app
optimistic refresh after a successful mutation.

## Cross-cutting
- RBAC: same `disableSave` gate as Slice 1 (mappings feed the same form Save). No new authz.
- Clients: N/A (no API change).
- Website: N/A.

## Dogfood moment
Regroup two Doing states into a mapping on the Atlas team; confirm metrics reflect it without a
manual refresh and without the hint.
