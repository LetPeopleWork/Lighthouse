# fix-work-tracking-system-switch — ADO Bug #5499

Defect fix routed through `/nw-bugfix` (RCA → user review → regression test + fix). No
DISCOVER/DISCUSS/DESIGN/DISTILL waves ran — the RCA is the authoritative design input.

## Wave: DELIVER / [REF] Implementation Summary

Switching a team's or portfolio's work tracking system never persisted: the dropdown showed
the new selection, no error appeared, and the change was silently dropped. Chris Graves'
workaround was to nudge the query field (add/remove a space) to trigger auto-save, which then
carried the connection change through as a passenger.

`handleWorkTrackingSystemChange` never set `hasInteractedRef.current = true`, so the
debounced auto-save effect hit its dirty-flag guard at `useModifySettings.ts:268` and
returned before scheduling the timer. The connection id was already in the effect's dep array
(`:288`) and already spread into the save payload (`:274`) — the effect re-ran correctly on
every dropdown change; the guard one line up silently negated it. One line restores the
intended behaviour.

## Wave: DELIVER / [REF] Files Modified

**Production (1 line):**
- `Lighthouse.Frontend/src/hooks/useModifySettings.ts` — `hasInteractedRef.current = true` added inside the existing `if (system)` guard at `:339`, marking the settings dirty so auto-save fires.

**Tests:**
- `Lighthouse.Frontend/src/hooks/useModifySettings.autosave.test.ts` — new `describe` block: the #5499 regression (switch-only → saves new connection id) plus two negative guards (silent on mount/conflict-reload; unknown system name never auto-saves connection id 0).
- `Lighthouse.Frontend/src/components/Common/Team/ModifyTeamSettings.test.tsx` — "handles work tracking system change" now asserts `mockSaveTeamSettings` is called with the new connection id, not just that a component rendered.
- `Lighthouse.Frontend/src/components/Common/ProjectSettings/ModifyProjectSettings.test.tsx` — same treatment for the portfolio path.

**Docs:**
- `docs/feature/fix-work-tracking-system-switch/deliver/rca-context.md` — full 5-Whys RCA with file:line evidence.
- `docs/feature/fix-work-tracking-system-switch/deliver/mutation/mutation-report-frontend.md` — Stryker run.

**Not tracked** (project convention): `stryker.config.*.mjs` and `vitest.stryker.*.ts` are gitignored (`**/stryker.config*.mjs`); `execution-log.json` is excluded from the workspace commit.

## Wave: DELIVER / [REF] Scenarios Green

3 of 3 new scenarios green (2026-07-16). Full frontend suite: 269 files / 3552 tests, 0 failures.

No `.feature` file — this bugfix had no DISTILL wave; the Vitest hook suite is the executable spec.

## Wave: DELIVER / [REF] DoD Check

| Item | Result |
|---|---|
| Root cause identified with evidence at each causal level | PASS — 5 Whys × 3 branches (mechanism / escape / silence), file:line evidence throughout |
| User reviewed and approved fix direction | PASS — minimal-fix scope approved 2026-07-16 |
| Regression test fails with the bug | PASS — verified by deleting the fix line; see Quality Gates |
| Fix makes the regression test pass | PASS |
| All existing tests still pass | PASS — 3552/3552 |
| Conventional commit `fix(scope): …` | PASS — `fix(settings): auto-save work tracking system switch` |

## Wave: DELIVER / [REF] Demo Evidence

N/A, because this bugfix has no DISCUSS wave and therefore no Elevator Pitch demo command.
The behavioural proof is the RED verification below: the defect is reproduced by a test that
fails on the pre-fix code and passes on the post-fix code.

## Wave: DELIVER / [REF] Quality Gates

| Gate | Outcome |
|---|---|
| RED verified (orchestrator, independent of crafter) | PASS — fix line deleted → `AssertionError: expected "vi.fn()" to be called 1 times, but got 0 times`; both component tests also went red (team + portfolio). Restore byte-identical (md5 match, empty `git diff`). |
| `pnpm test` | PASS — 269 files / 3552 tests |
| `pnpm build` | PASS — exit 0; zero warnings attributable to our source (`INVALID_ANNOTATION` noise is pre-existing `@microsoft/signalr` in node_modules) |
| Biome | PASS — runs as `prebuild`; no `src/docs` symlink present |
| Refactor pass (L1–L6) | N/A, because the change is one statement inside an existing guard — there is no structure to restructure. |
| Mutation (per-feature, ≥80%) | PASS — 100.00%, 12/12 killed, 0 survivors |
| DES integrity | PASS — `All 1 steps have complete DES traces`, exit 0; RED→GREEN→COMMIT all EXECUTED/PASS |
| Backend gates (`dotnet build` / `dotnet test`) | N/A, because no backend file changed — the defect is pure frontend change-detection. |
| E2E (Playwright) | N/A, because the RTL hook test reproduces the defect deterministically at 300ms; a live run would add minutes for no additional signal. |

## Wave: DELIVER / [REF] Pre-requisites

None. No DISTILL scenarios or DESIGN component manifest exist for this bugfix; the RCA
(`deliver/rca-context.md`) served as the design input and the existing
`useModifySettings.autosave.test.ts` suite supplied the test patterns.

## Wave: DELIVER / [WHY] Deferred Follow-ups

Deliberately out of scope per the user's minimal-fix decision (2026-07-16):

1. **Work-item purge on connection switch.** `TeamExtensions.cs:68` sets `connectionChanged`,
   which purges work items. Now that auto-save works, a stray dropdown click purges within
   300ms with no confirmation. Not a new capability — the space-trick workaround already did
   this — but the path is far easier to trigger accidentally. A confirm dialog is the obvious
   mitigation.
2. **Dirty tracking is coupled to a setter, not to the data.** `hasInteractedRef` is set only
   inside `updateSettings` and the list handlers, so any field modelled outside the `settings`
   object (as the connection is, at `:128-129`) is invisible to change detection by
   construction. This defect recurs for the next such field. Fix: fold the connection into
   `settings`, or replace the ref with a baseline-snapshot comparison.
3. **No observable "unsaved changes" state.** A missed dirty flag is indistinguishable from a
   completed save, which is why #5499 failed silently for users rather than erroring.

Per the user's instruction, these were **not** filed as ADO work items.

## Wave: DELIVER / [WHY] Upstream Issues

**`des-commit` cannot satisfy the DES stop hook.** `des-commit` appends only a `Step-Id`
trailer (`_with_step_id_trailer()` in `~/.claude/lib/python/des/cli/commit.py`), but the stop
hook requires both `Step-Id` and `Task-Id`, so the step-01-01 commit was rejected and needed
an amend to land. This is a contract mismatch in the tooling, not in this feature — it will
hit every `des-commit` in this repo until fixed upstream. Workaround: embed the trailer in
the message body (`--message "subject\n\nTask-Id: <project-id>"`), which survives because the
`Step-Id` guard is a no-op on other trailers.
