# Mutation testing: 6431 (Epic 6350 slice 03: Teams and Portfolios in one run, and the window for several owners)

Run 2026-10-30 against local `main` @ `93fb57443` (production code frozen) plus the kill-round tests below. Gate: 80 %
kill rate on the lines slice 03 changed (`git diff -U0 712651832..HEAD` on the nine production files, type-only and
import-only ranges left out).

## Backend: N/A

Slice 03's backend steps only un-ignored acceptance tests that the existing code already satisfied.
`git diff --stat 712651832~2..HEAD -- Lighthouse.Backend/Lighthouse.Backend` shows no file, so there is no backend
production change to mutate.

## Frontend (StrykerJS 10.0.0)

Config `stryker.6431.frontend.json` and `vitest.stryker.6431.mutation.ts` (the vitest config is gitignored in
`Lighthouse.Frontend/`). `mutate` lists the changed line ranges of `OwnerStep.tsx`, `BulkReportWizard.tsx`,
`ReportsPage.tsx`, `NowStep.tsx`, `ThenStep.tsx`, `WindowLengthControl.tsx`, `ActionButton.tsx` and
`WizardStepperFrame.tsx`, so the scores are changed-line scores. `reportTypes.ts` changed only an optional prop in an
interface, so it has nothing to mutate. Settings: `perTest`, `--incremental` (file
`reports/mutation/stryker-incremental-6431.json`, gitignored), concurrency 2, `inPlace`, `disableTypeChecks: false`,
`TZ=Europe/Zurich`. Command, from `Lighthouse.Frontend/`:

```
TZ=Europe/Zurich pnpm exec stryker run ../docs/feature/epic-6350-bulk-reports/mutation/stryker.6431.frontend.json --incremental
```

The vitest include is `src/pages/Reports/**/*.test.{ts,tsx}`, `src/pages/Common/Reports/wizard/**/*.test.{ts,tsx}`,
`src/pages/Common/Reports/ReportsTab.createWizard.test.tsx` (the single wizard's Then and Now steps) and
`src/components/Common/CreateWizards/CreateWizardShell.test.tsx`. `ActionButton.test.tsx` is left out on purpose: one of
its tests leaves a rejected promise unhandled, which crashes Stryker's dry run (vitest reports it as an unhandled
error). The wizards reach `ActionButton` and `WizardStepperFrame` in the included suites anyway.

Stryker produced no mutants for `ActionButton.tsx`, `WizardStepperFrame.tsx` and `ReportsPage.tsx`: their changed lines
are a destructured prop, a JSX attribute passing a prop through, and an `aria-describedby` attribute, none of which it
mutates. The attribute itself is covered by the over-the-ceiling tests of `BulkReportWizard.ownerLimits.test.tsx`.

| file | first run | after kill round |
| --- | --- | --- |
| `Common/Reports/wizard/thenAndNow/NowStep.tsx` | 23 / 28 | 27 / 28 (96.43 %) |
| `Common/Reports/wizard/thenAndNow/ThenStep.tsx` | 0 / 3 | 3 / 3 (100 %) |
| `Common/Reports/wizard/thenAndNow/WindowLengthControl.tsx` | 10 / 24 | 19 / 24 (79.17 %) |
| `pages/Reports/BulkReportWizard.tsx` | 11 / 12 | 11 / 12 (91.67 %) |
| `pages/Reports/OwnerStep.tsx` | 40 / 48 | 44 / 48 (91.67 %) |
| **total** | 84 / 115 = 73.04 % | **104 / 115 = 90.43 %** |

Run times: first run 16 m 15 s; the kill-round re-run (incremental, 35 of 115 results reused) 7 m 31 s.

### Kill round

Tests added to `BulkReportWizard.portfolios.test.tsx`:

- The Teams group lists only Teams and the Portfolios group only Portfolios.
- A group with nothing to pick is not shown (Teams-only and Portfolios-only options).
- Custom is pressed exactly while the length is not a preset; pressing it empties the field; no stray word sits beside
  the field.
- Now: Same as Then hides the length field; Custom shows it, is checked, and no stray label appears.

Tests added to `ReportsTab.createWizard.test.tsx` (the single wizard must not pick up the several-owner look):

- Then labels its custom length "Custom:" and has no Custom button.
- Now keeps "Length" and "or", and offers no Same as Then radio and no Custom button.

### Accepted survivors (11)

- `NowStep.tsx:45`, `""` to `"Stryker was here!"` for a custom Now. The field is a number input, so any non-numeric
  string is shown and read as empty. Equivalent.
- `WindowLengthControl.tsx:60` (two), `onChange("")` to a non-empty string, and the `if` made false (which falls through
  to `onChange("custom")`). Same reason: the number input reduces any non-numeric text to empty. Equivalent.
- `WindowLengthControl.tsx:13`, `CUSTOM = "custom"` to `""`. The constant is only compared to itself and used as the
  toggle's value; any distinct-from-presets value behaves the same. Equivalent.
- `WindowLengthControl.tsx:46` (two), `selected` always `CUSTOM`, and `customChoice || !isPreset`. `selected` is read only
  when the length is not a preset, and without `customChoice` there is no Custom button for it to select. Equivalent.
- `BulkReportWizard.tsx:63`, dropping `owner.ownerKind === ownerKind`. The two admin predicates are specific to their
  kind (a Team's id is not a Portfolio's Admin role), so the kind check is defensive. No contrived test added.
- `OwnerStep.tsx:72-75` (four), the `sx` of the group heading row. Layout that jsdom does not compute.

Not mutated: type-only and import-only ranges, and the three files above that yielded no mutants.

## Frontend gates after the kill round

`pnpm test`: 567 files passed, 4 skipped; 8701 tests passed, 40 skipped (the pending scenarios of later slices), 283 s.
`pnpm build`: exit 0; Biome applied no fixes; `git status` shows no mutant left in `src/`.
