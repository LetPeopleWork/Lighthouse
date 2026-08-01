# RCA context — Bug #5628

**Title.** Team settings auto-save bypasses connector validation, so a bad work item type saves
silently and yields zero items.

Found 2026-08-01 while dogfooding US 5612. Not a ServiceNow bug — every connector and both settings
pages are affected.

## Root cause

Auto-save was added as a second save path through `useModifySettings` without inheriting the
validation gate the button path had; removing the Save button then made the gated path unreachable.

## Evidence chain

| # | Claim | Evidence |
|---|---|---|
| 1 | Auto-save effect saves without validating | `Lighthouse.Frontend/src/hooks/useModifySettings.ts:249-264` — debounced `setTimeout` → `dispatchSave` → `saveSettingsRef.current(payload)`. `validateSettings` is never referenced. |
| 2 | The validating path exists but is dead | `useModifySettings.ts:336` `handleSave` awaits `validateSettings`, sets `validationError`, and returns early on failure. |
| 3 | Team page never calls it | `components/Common/Team/ModifyTeamSettings.tsx:118-148` destructures 20 hook keys; `handleSave` is not among them. No Save button in the JSX — only `SaveStateIndicator`. |
| 4 | …yet passes the validator in | `ModifyTeamSettings.tsx:141` `validateSettings: validateTeamSettings`, `:145` `autoSave: { enabled: true, canSave: !disableSave, refreshOnSave: true }`. |
| 5 | Portfolio page is identical | `components/Common/ProjectSettings/ModifyProjectSettings.tsx:159` + `:162-166`. Same omission of `handleSave`. |
| 6 | Only the wizards validate | `hooks/useCreateWizard.ts:193` (step validation) and `:231` (`handleWizardComplete`). |
| 7 | The display surface already exists and can never fire | `ModifyTeamSettings.tsx:316-322` and `ModifyProjectSettings.tsx:348-354` render `validationError` + a `validationTechnicalDetails` expander. Only `handleSave` populates them. |
| 8 | Validation is a real connector round-trip | `Lighthouse.Backend/Lighthouse.Backend/API/TeamsController.cs:138` `POST /teams/validate` → `workItemService.ValidateTeamSettings(team)`. For ServiceNow that is the ADR-124 probe ladder: one probe per named kind of work. |

`useModifySettings` has exactly two consumers (3, 5) — so `handleSave` and the entire
`validationError` path are dead code in production.

## Consequence

Editing an existing team or portfolio takes the unvalidated path every time. A mistyped work item
type reports "saved", passes no check, and the next sync selects zero rows with nothing anywhere
explaining why. For ServiceNow this bypasses the ADR-124 probe ladder — built precisely to catch
this — on the page where a coach is most likely to mistype.

## Chosen fix direction (maintainer decision, 2026-08-01)

**Option A — validate after the save settles, warn non-blockingly.**

- Auto-save keeps writing exactly as today; the write is never blocked. The auto-save contract
  ("your edits are persisted") is preserved.
- Once a save settles (`saveState === "saved"`), run `validateSettings` once against the payload
  that was just persisted, and surface the outcome through the **existing** `validationError` /
  `validationTechnicalDetails` alert — no new UI component.
- Fix lives in `useModifySettings`, so team and portfolio pages are both covered by one change.
- Cost budget: at most one connector probe per settled save, never one per debounce tick. Dedupe so
  an unchanged connector-relevant payload does not re-probe.

Rejected alternatives: validate on work-item-types blur (guards one field only — a mistyped query or
state stays silent); reintroduce an explicit Validate button (nothing forces a coach to press it).

## Scope guards

- `modifyDefaultSettings` pages must stay unvalidated — `handleSave` already skips validation there
  (`useModifySettings.ts:345`); the new path must skip it the same way.
- Do **not** treat unmapped states as a defect. An unmapped state legitimately means "this item does
  not exist for Lighthouse" — `Canceled` is the canonical case. Out of scope here.
- Keep the fix minimal; no refactor of the auto-save state machine.

## Regression net

Shipped as a new sibling suite, `Lighthouse.Frontend/src/hooks/useModifySettings.validation.test.ts`,
rather than an extension of `useModifySettings.autosave.test.ts` — the hook's tests are split one
file per concern (`.autosave`, `.conflict`), and this is a third concern. The RED cases show a
settled auto-save with a failing `validateSettings` producing a surfaced warning, failing against
`main` at `4f39b1219` where `validateSettings` was never invoked on that path.

## Files in play

- `Lighthouse.Frontend/src/hooks/useModifySettings.ts` (fix)
- `Lighthouse.Frontend/src/hooks/useModifySettings.validation.test.ts` (regression test, new)
- `Lighthouse.Frontend/src/components/Common/Team/ModifyTeamSettings.tsx` (alert wiring verified — unchanged)
- `Lighthouse.Frontend/src/components/Common/ProjectSettings/ModifyProjectSettings.tsx` (same — unchanged)
