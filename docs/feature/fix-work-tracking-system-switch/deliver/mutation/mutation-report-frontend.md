# Frontend Mutation Testing — fix-work-tracking-system-switch (ADO Bug #5499)

Stryker (TypeScript/React) feature-scoped run via the command-runner pattern (vitest as a
subprocess, scoped `include`) to avoid the vitest-runner OOM.

- Config: `Lighthouse.Frontend/stryker.config.fix-work-tracking-system-switch.mjs`
- Scoped vitest config: `Lighthouse.Frontend/vitest.stryker.fix-work-tracking-system-switch.config.ts`
- Mutate scope: `src/hooks/useModifySettings.ts:333-351` — `handleWorkTrackingSystemChange` only.
  The dirty-flag line (`:339`) IS the entire production change; the surrounding handler is its
  blast radius. The rest of the 425-line hook predates this bugfix and is not this feature's
  mutation surface.
- Test surface (4 files): `useModifySettings.autosave.test.ts`, `useModifySettings.test.ts`,
  `ModifyTeamSettings.test.tsx`, `ModifyProjectSettings.test.tsx`.
- Baseline gate: full frontend suite green (269 files / 3552 tests) before and after.

## Verdict: PASS

**100.00%** — 12 killed / 12 total, 0 survivors, 0 timeouts, 0 errors. Threshold is 80%.

## Per-file kill rate

| File | Score | Killed / Total | Survivors | Note |
|------|-------|----------------|-----------|------|
| `useModifySettings.ts` (`handleWorkTrackingSystemChange`) | 100.00% | 12 / 12 | 0 | every mutant in the changed handler killed, including the dirty-flag line itself |

## Why this surface is small — and why that is honest

The fix is a single statement. A 12-mutant surface is the true size of the change; inflating
the `mutate` glob to the whole hook would have reported a bigger denominator made up of code
this bugfix never touched. Per-feature mutation gates the feature's own surface, so the
handler is the correct boundary.

The signal that matters: Stryker's mutators include statement removal and boolean-literal
replacement, so `hasInteractedRef.current = true` was mutated to both *deleted* and
`= false`. Both were killed — by the `@US-01` regression test asserting `saveSettings` is
called with `workTrackingSystemConnectionId: 2`. That is the same failure mode as the
original defect, which is exactly the property a #5499 regression test must hold.

The `if (system)` guard's mutants were also killed, confirming the placement decision: the
negative test ("ignores an unknown system name instead of auto-saving a missing connection")
constrains the null branch, so a mutant that hoists the flag out of the guard — reintroducing
the `workTrackingSystemConnectionId: 0` FK-violation path the RCA warned about — cannot
survive.

## Independent RED verification (orchestrator, not Stryker)

Beyond mutation, the fix line was manually deleted and the suites re-run:

- Hook regression test failed: `AssertionError: expected "vi.fn()" to be called 1 times, but got 0 times`
- Both component tests failed (team path AND portfolio path)
- Restoring the line returned all green; restore was byte-identical (md5 match, `git diff` empty)

This confirms the tests bite for the right reason and that both edit paths are genuinely
covered — not testing theater.
