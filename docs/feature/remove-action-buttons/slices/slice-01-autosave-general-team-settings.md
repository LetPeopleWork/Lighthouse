# Slice 01 — Auto-save general team settings (LINCHPIN)

Story: US-5077-01 | job: job-commit-intent-no-button | Must Have

> **LINCHPIN — review carefully before Slices 2-4 build on this mechanism.** It introduces
> the reusable auto-save mechanism (debounced auto-fire of `handleSave` when `formValid`,
> save-state machine, inline-error-as-primary-feedback) in `useModifySettings`. Three
> settings slices depend on its design. Validate it on the simplest surface first.

## Goal
Editing a Team's general settings persists automatically the moment the form is valid, with a
calm save-state indicator and inline errors replacing the Save click.

## IN scope
- Auto-save mechanism in `useModifySettings.ts` (debounce on `formValid`; save-state machine
  idle/saving/saved/failed; inline error as primary channel).
- General-settings fields on `/teams/{teamId}/settings`; remove the Save button.
- RBAC parity: no auto-save where `disableSave` is true (read-only for non-admins).
- Invalid input held + inline error; failed save → Retry, edits kept; stale-edit guard.

## OUT of scope
- State Mappings refresh (Slice 2), Forecast Filter (Slice 3), portfolio (Slice 4), forecasts (5/6).

## Learning hypothesis
**Confirms** that a debounced auto-save gated on `formValid`, with a save-state indicator and
inline-error-as-primary-feedback, fully replaces the Save button without the user feeling unsure
"did it save?" — validated by dogfooding the real Team settings form.
**Disproves** the mechanism design if users still feel uncertain or half-typed state persists —
in which case Slices 2-4 must not proceed on it.

## Acceptance criteria
- [ ] Valid edit auto-persists (debounced), no Save button; indicator shows "Saving…" → "All changes saved".
- [ ] Invalid edit fires no save; inline error shown; last valid value stays persisted.
- [ ] Failed save shows "Couldn't save" + Retry; edits not lost; Retry re-fires save.
- [ ] No auto-save where `disableSave` true; fields read-only for non-admins.
- [ ] Rapid edits debounce; only latest valid state persists.

## Dependencies
None (first slice).

## Effort estimate
~1 day (mechanism + simplest surface).

## Reference class
Mechanism analog: `TeamForecastView` auto-run (debounce + stale-guard) — same shape, applied to
save instead of run. Surface: `useModifySettings.handleSave`, `ModifyTeamSettings.tsx`,
`ValidationActions.tsx` (Save removed; icons replaced by save-state indicator).

## Cross-cutting
- RBAC: suppress auto-save where `disableSave` (ModifyTeamSettings:253) via `useRbac()` / `IRbacAdministrationService`. No new authz.
- Clients: N/A (no API change; reuses `PUT /api/teams/{id}`).
- Website: N/A (UX polish on marketed flow).

## Dogfood moment
Edit the Atlas team's name + throughput history on a local instance; confirm persistence across
refresh with no button.
