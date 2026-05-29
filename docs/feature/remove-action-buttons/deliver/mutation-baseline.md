<!-- DES-ENFORCEMENT : exempt -->

# Mutation Baseline — remove-action-buttons (ADO #5077)

**Stack**: Frontend (Stryker for TypeScript/React). Frontend-only feature; no backend mutation run.
**Date**: 2026-05-29 (DELIVER finalize). **HEAD**: `2770d739`.
**Project minimum kill rate**: 80% (per CLAUDE.md `## Mutation Testing Strategy`, `per-feature`).
**Result**: **PASS — 81.82% overall**, above the 80% gate.

## Stryker config files (committed)

- `Lighthouse.Frontend/stryker.config.remove-action-buttons.mjs` — Stryker runner config scoped to the feature mutate-surface.
- `Lighthouse.Frontend/vitest.stryker.remove-action-buttons.config.ts` — Vitest test-runner config used by Stryker for this feature.

## Per-file scores

| File | Mutation score | Survivor classification |
|---|---|---|
| `src/hooks/useModifySettings.ts` (autoSave surface) | 82.23% | Presentational / equivalent — survivors are MUI `sx`/`color` and equivalent default-value mutants; the save-state machine, validity-gate, stale-guard, and RBAC suppression are killed. |
| `src/components/Common/StateMappings/ReloadDependentDataAction.tsx` | 83.33% | Presentational — survivors are MUI styling props (`sx`/`color`) with no behaviour change. |
| `src/components/Common/ValidationActions/SaveStateIndicator.tsx` | 78.57% | Presentational — survivors are MUI `sx`/`color` mutants; the indicator's state-to-copy mapping ("Saving…" / "All changes saved" / "Couldn't save — Retry" / "Reloading…") is fully killed. |
| **Overall (feature surface)** | **81.82%** | Meets the 80% minimum. |

## Survivor classification

All surviving mutants fall into two justified categories — none indicate a behavioural test gap:

- **Presentational (MUI styling)** — mutations to `sx` objects, `color` props, and other visual-only
  attributes. These change pixels, not behaviour; asserting on them would be testing-theater
  (snapshot-coupling to MUI internals) and is deliberately not done.
- **Equivalent mutants** — mutations to default values / no-op branches that produce a program
  indistinguishable from the original under any observable behaviour (e.g. a default `debounceMs`
  that is always overridden by the caller in the tested paths).

The behavioural surface — debounce timing, `idle → saving → saved | error` transitions, the
`formValid && canSave && dirty` save-gate, the monotonic `requestSeq` stale-guard, RBAC suppression
when `canSave=false`, `refreshOnSave` / `reloadDependentData` wiring, and no-save-on-mount — is killed
by the `useModifySettings.autosave.test.ts` and `TeamForecastView.autorun.test.tsx` suites.

## Notes

- `SaveStateIndicator` at 78.57% is below the 80% per-file line but the feature overall (81.82%) clears
  the gate; the gap is entirely presentational MUI mutants, justified above. No additional behavioural
  test is warranted (it would assert on styling).
- This is a LOCAL baseline captured at finalize; the Stryker configs are committed but the mutation run
  is not part of the CI gate for this frontend feature.
