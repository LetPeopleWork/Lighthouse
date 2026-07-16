# RCA Context — ADO Bug #5499

**Title**: Switching Work Tracking System does not work
**Reported by**: Chris Graves | **Severity**: 3 - Medium | **State**: Active
**Approved fix scope**: minimal one-line fix + regression tests (user decision, 2026-07-16)

## Defect

With two work tracking system connections configured, switching an existing team or
portfolio from the first to the second never persists. No error surfaces; the dropdown
shows the new selection. Workaround: edit the query (add/remove a space) to trigger
auto-save, which then carries the connection change through.

## Root cause

`handleWorkTrackingSystemChange` (`Lighthouse.Frontend/src/hooks/useModifySettings.ts:333-350`)
never sets `hasInteractedRef.current = true`. The auto-save effect guards on that flag at
`:268` and returns early, so the debounce timer is never scheduled.

`hasInteractedRef.current = true` is set in exactly four places — `updateSettings` (`:327`)
and the list handlers `onAdd`/`onRemove`/`onReorder` (`:390`/`:394`/`:398`). None are
reachable from the dropdown.

Evidence the auto-save was intended to fire on this change:

- `selectedConnectionId` is in the effect dep array (`:288`) — the effect *does* re-run.
- The payload spreads `workTrackingSystemConnectionId: selectedConnectionId` (`:274`).

The guard at `:268` silently negates both.

**Workaround explained**: a query edit routes through `updateSettings` → flag true → effect
runs → payload at `:274` picks up the already-updated `selectedConnectionId`. `:209` resets
the flag on each save success, so the trick is needed per switch.

**Structural cause**: the connection lives in separate React state (`:128-129`), outside the
`settings` object, so it cannot flow through `updateSettings` — the only function that marks
dirty. Dirty tracking is coupled to a setter function rather than to the data.

## Fix

Single line in `useModifySettings.ts`, inside the existing `if (system)` block at `:338`:

```ts
const handleWorkTrackingSystemChange = (name: string) => {
	setValidationError(null);
	setValidationTechnicalDetails(null);
	const system = workTrackingSystems.find((s) => s.name === name) ?? null;
	setSelectedWorkTrackingSystem(system);
	if (system) {
		hasInteractedRef.current = true; // <-- ADD
		setSettings((prev) =>
			prev
				? { ...prev, dataRetrievalSchema: getSchemaForSystem(system.workTrackingSystem) }
				: prev,
		);
	}
};
```

Inside `if (system)`, not at the top of the function: `selectedConnectionId` is
`selectedWorkTrackingSystem?.id ?? 0` (`:170`). Flagging dirty on the null branch would
auto-save `workTrackingSystemConnectionId: 0` → FK violation at
`LighthouseAppContext.cs:270-291`. Guarding on `system` keeps the null path inert.

No save-on-load regression: the mount fetch (`:302-306`) and `reloadAfterConflict`
(`:248-254`) call `setSelectedWorkTrackingSystem` directly, bypassing the handler; `:248`
explicitly sets the flag `false` first. Only genuine user interaction reaches the new line.

## Scope

- **Team AND portfolio both affected — same line, one fix.** Both funnel through the same
  hook: `EditTeam.tsx:164` → `ModifyTeamSettings.tsx:76` (`autoSave.enabled: true` at `:84`)
  and `EditPortfolio.tsx:174` → `ModifyProjectSettings.tsx:90` (`enabled: true` at `:97-101`).
- **Create wizards NOT affected.** `CreateTeamWizard.tsx` / `CreatePortfolioWizard.tsx` use a
  disjoint `useCreateWizard` hook — no auto-save surface, no dirty flag; explicit Create
  button → `handleCreate` → `saveSettings(dto)`, connection id always in the DTO.
- **Backend NOT implicated.** `TeamController.cs:183` → `TeamExtensions.cs:43` and
  `PortfolioController.cs:129` → `PortfolioExtensions.cs:25` both map the field correctly;
  DTO field is `[JsonRequired]` (`SettingsOwnerDtoBase.cs:69`) so a missing field is a 400,
  not a silent zero. The backend never receives a request.

## Why it escaped

Test suites are organized per implementation unit, so the seam between WTS and auto-save is
where no test lives:

- `useModifySettings.test.ts:249` `describe("handleWorkTrackingSystemChange")` — its tests
  (`:250`, `:259`, `:271`) assert state only, never `saveSettings`.
- `useModifySettings.autosave.test.ts` — all changes driven via `updateSettings`
  (`:104`/`:185`/`:264`).
- `ModifyTeamSettings.test.tsx:332-339` fires the dropdown change but asserts a rendering
  (`expect(screen.getByText("GeneralInputsComponent"))`) while `mockSaveTeamSettings` sits
  wired at `:240`. Same shape at `ModifyProjectSettings.test.tsx:485`.

## Regression tests

**Primary — `Lighthouse.Frontend/src/hooks/useModifySettings.autosave.test.ts`** (owns the
canonical pattern: `vi.useFakeTimers({ shouldAdvanceTime: true })` `:96`, `DEBOUNCE_MS = 300`
`:15`, `advanceTimersByTime` → `waitFor(...toHaveBeenCalledTimes(1))`):

1. Switching WTS with **no other edit** auto-saves with the new
   `workTrackingSystemConnectionId` — the direct #5499 regression. MUST fail before the fix.
2. Negative: no save on mount / after `reloadAfterConflict` (locks `:248-254`, `:302-306`).
3. Negative: unknown system name (`:271` path) does not auto-save `connectionId: 0`.

**Secondary — strengthen existing component tests** (guards the portfolio path against
divergence + wrapper wiring): `ModifyTeamSettings.test.tsx:332` and
`ModifyProjectSettings.test.tsx:485` — add an assertion that `mockSaveTeamSettings` /
`mockSaveProjectSettings` is called with the new connection id. Follow the timer-free style
local to those files.

**Backend (NUnit + Moq + EF InMemory) — N/A, explicitly**: the backend path is already
correct and already covered; a new test would assert an unchanged, non-defective mapping for
zero regression value.

**E2E — N/A, explicitly**: the RTL hook test reproduces the defect deterministically at
300ms; a Playwright run would add minutes of suite time for no additional signal.

## Deferred (NOT in this change — user decision)

- **Work-item purge on switch**: `TeamExtensions.cs:68` sets `connectionChanged` → purges
  work items. Post-fix a stray dropdown click purges within 300ms with no confirm. Not a new
  capability (the workaround already does this) but the path gets much easier to trigger.
  → propose as a separate ADO bug/story with its own DISCUSS.
- **Structural (root cause A recurs for the next field added outside `settings`)**: fold the
  connection into `settings` so it flows through `updateSettings`, or replace
  `hasInteractedRef` with a baseline snapshot compare.
- **No observable "unsaved changes" state (root cause C)**: a missed dirty flag is
  indistinguishable from a completed save. A dirty indicator would make future misses loud.

## Quality gates (CI parity)

- `pnpm test` green
- `pnpm build` zero errors + zero warnings (Biome runs as `prebuild`)
- Conventional commit: `fix(settings): ...`
- Consult `docs/ci-learnings.md` before writing code
