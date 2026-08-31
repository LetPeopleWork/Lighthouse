# Slice 01 — Configuration tab stops answering the key question

**Feature**: remove-encryption-frame-from-config
**ADO**: User Story #5875
**Story**: US-01 | **Job**: `job-operator-know-the-key-is-actually-in-effect`
**Effort**: ~1 hour | **Reference class**: single-component UI subtraction with a pinning test

## Goal

Delete the read-only "Secret Encryption Key" frame from `Settings → Configuration` so the Encryption tab
is the only Settings surface that states key source and active key id.

## IN scope

| File | Change |
|---|---|
| `Lighthouse.Frontend/src/pages/Settings/System/SystemSettingsTab.tsx` | Delete `EncryptionKeySection` (L31-54); delete `keyState` state (L68) and its explanatory comment (L65-67); delete `fetchKeyState` (L96-107) and its `useEffect` call (L130) and dep (L131); delete the render site (L209); drop `encryptionService` from the `useContext` destructure (L70-71); drop the now-unused `EncryptionKeyState` / `KEY_CUSTODY_WORDING` import (L18-21) and any MUI import left unused by the deletion. |
| `Lighthouse.Frontend/src/pages/Settings/System/SystemSettingsTab.test.tsx` | Delete the `describe("secret encryption key")` block (~L319-413). Remove the `createMockEncryptionService` / `mockGetKeyState` wiring (L79-80, L115) **only if** nothing else in the file uses it. **Add** one test asserting `encryption-key-state` is absent from the rendered Configuration tab. |
| `docs/assets/settings/configuration.png` | Regenerate via `@screenshot`. |

## OUT of scope

- `EncryptionPanel.tsx` and everything on the Encryption tab — untouched.
- `SystemInfoDisplay.tsx` "Encryption" row — deliberately kept; documented in
  `docs/settings/systeminfo.md:20` as the broad-audience custody check.
- `models/Encryption/EncryptionKeyState.ts`, `KEY_CUSTODY_WORDING` — still used by `EncryptionPanel`.
  **Not dead code. Do not delete.**
- Backend, API, DTOs, migrations — none.
- Any `data-testid` rename — deletion alone resolves the duplicate.

## Learning hypothesis

*The Encryption tab is a complete replacement for the Configuration frame.*

- **Confirms if it succeeds**: key custody has one owner in Settings; the operator reconciles nothing.
- **Disproves if it fails**: some operator-visible fact was reachable only from the Configuration tab —
  in which case that fact belongs on the Encryption tab, and this slice grows to move it there rather
  than to restore the frame.
- **Prior**: low risk. Read from code before the story was written — `EncryptionPanel.tsx:108,114`
  renders both values from the same `getKeyState()` call behind the same System-Admin gate
  (`Settings.tsx:89`), and adds the key ring, store path, custody explanation and actions on top.

## Acceptance criteria

- **AC-1** No `data-testid="encryption-key-state"` and no "Secret Encryption Key" heading on
  `Settings → Configuration` as System Administrator.
- **AC-2** `Settings → Encryption` still shows `encryption-custody` and `encryption-active-key-id`,
  unchanged.
- **AC-3** `encryption-active-key-id` resolves to exactly one element across Settings.
- **AC-4** `SystemSettingsTab` issues no `getKeyState()` call on render.
- **AC-5** All other Configuration groups render unchanged (Blackout, Optional Features, *Feature*
  Order, Terminology, *Team* Refresh, *Feature* Refresh).
- **AC-6** `docs/assets/settings/configuration.png` regenerated, no encryption frame.
- **AC-7** `pnpm test` green; `pnpm build` zero errors, zero warnings.

## Dependencies

- Epic #5775 shipped — met.
- **Premium licence fixture** required before the `@screenshot` run (AC-6). Gitignored, absent from
  every worktree; import it from the main checkout first.

## Gotchas

- **Delete `configuration.png` before the regen run.** The screenshot runner keeps the existing PNG when
  the diff falls under its threshold — and a frame disappearing from a long page is a small diff. Skip
  this and AC-6 silently passes against the stale image.
- **Do not "clean up" `EncryptionKeyState.ts`.** `EncryptionPanel` imports it.
- **The comment at `SystemSettingsTab.tsx:65-67` goes with the code.** It defends the admin-only data
  source, and the Encryption tab already reads from that same source behind that same gate, so there is
  nothing left for it to say once the frame is gone. Do not relocate it.
- **Pin the absence.** Without the AC-1 test the frame can be reintroduced with no test turning red —
  which is exactly how it arrived.

## Pre-slice SPIKE

None. Uncertainty resolved by reading the code during DISCUSS.

## Commit

`refactor(settings): drop duplicate encryption key frame from the configuration tab`
