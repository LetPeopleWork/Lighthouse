# Mutation testing: 6432 (Epic 6350 slice 04: owners that cannot hold the window)

Run 2026-10-30 against local `main` @ `bb271584f` (production code frozen) plus the kill-round tests below. Gate: 80 %
kill rate on the lines slice 04 changed (`git diff -U0 5714fb7a4..HEAD -- Lighthouse.Frontend/src`, production files
only, type-only and import-only ranges left out).

## Backend: N/A

Slice 04 only un-ignored a backend acceptance test and deleted a test constant.
`git diff --stat 5714fb7a4..HEAD -- Lighthouse.Backend/Lighthouse.Backend` shows no file, so there is no backend
production change to mutate.

## Frontend (StrykerJS 10.0.0)

Config `stryker.6432.frontend.json` and `vitest.stryker.6432.mutation.ts` (the vitest config is gitignored in
`Lighthouse.Frontend/`). `mutate` lists the changed line ranges of `BulkReportWizard.tsx` (the `limitOfFor` function,
the `limitOf` prop passed to `OwnerStep`, `limitOf`/`canHold`/`changeDraft`/`selectAll`/`pickedOwners`, and the
`onChange` and `limitOf` props handed to `StepContent`), `OwnerStep.tsx` (`settingsPathOf`, `SettingsLink`, and the
per-owner row of `OwnerGroup`), `ReportsPage.tsx` (the `byOwner` default order and its call) and
`reportWindowCheck.ts` (`ownerCannotHold`), so the scores are changed-line scores. Skipped as type-only or
import-only: the `OwnerLimit`/`OwnerLimitOf` types, the prop interfaces, destructured props and the imports.
Settings: `perTest`, `--incremental` (file `reports/mutation/stryker-incremental-6432.json`, gitignored), concurrency
2, `inPlace`, `disableTypeChecks: false`, `TZ=Europe/Zurich`. Command, from `Lighthouse.Frontend/`:

```
TZ=Europe/Zurich pnpm exec stryker run ../docs/feature/epic-6350-bulk-reports/mutation/stryker.6432.frontend.json --incremental
```

The vitest include is `src/pages/Reports/**/*.test.{ts,tsx}` and `src/pages/Common/Reports/wizard/**/*.test.{ts,tsx}`
(which holds `reportWindowCheck.test.ts`). `ActionButton.test.tsx` is left out on purpose, as in 6431: one of its tests
leaves a rejected promise unhandled, which crashes Stryker's dry run.

| file | first run | after kill round |
| --- | --- | --- |
| `Common/Reports/wizard/reportWindowCheck.ts` | 11 / 11 | 11 / 11 (100 %) |
| `pages/Reports/BulkReportWizard.tsx` | 53 / 70 | 57 / 70 (81.43 %) |
| `pages/Reports/OwnerStep.tsx` | 25 / 31 | 29 / 31 (93.55 %) |
| `pages/Reports/ReportsPage.tsx` | 16 / 16 | 16 / 16 (100 %) |
| **total** | 105 / 128 = 82.03 % | **113 / 128 = 88.28 %** |

The first run already met the gate. Run times: first run 8 m 01 s; the kill-round re-run (incremental) 6 m 57 s.

### Kill round

Tests added to `BulkReportWizard.ownerLimits.test.tsx`:

- An owner that Select all left out because it cannot hold the window is still unpicked after the window shortens
  again (kills dropping the `canHold` filter in `selectAll`).
- With Then and Now both reaching past an owner's kept history, the one starting furthest back is named (kills the
  `sort`, its comparator and the arithmetic operator in it).
- The reason text is the accessible description of the greyed checkbox, a pickable owner has no description, and the
  reason is followed by a single space and the settings link (kills the `limitId`, the `aria-describedby` branch and the
  `{" "}` separator).

### Accepted survivors (15)

- `BulkReportWizard.tsx:89` and `:90` (eight): `keptSince === null` and the `thenLength`/`thenEnd` null guards. An owner
  without a kept-history start can never refuse a window (`ownerCannotHold` is false when `earliestStart` is null), so
  the guard only returns early where the code below would also return null; `thenLength` and `thenEnd` come from a
  draft that the earlier steps only let through when valid. The guard narrows the types for the lines after it.
  Equivalent.
- `BulkReportWizard.tsx:106` and `:107` (four): the `nowLength !== null && today !== null` guards. A blank Now length
  is refused as too short, not as before the cutoff, so `ownerCannotHold` is false for it anyway, and `today` is
  always a valid day. Type narrowing only. Equivalent.
- `BulkReportWizard.tsx:108`, `side: "now"` to `side: ""`. `reportWindowCheck` tests `side === "then"` and treats
  everything else as Now, so the string is only a type tag at this point. Equivalent.
- `OwnerStep.tsx:144`, `aria-describedby` always set to the id. A pickable owner then points at an id that exists on
  no element, which the accessibility tree reads as no description; jsdom cannot tell the two apart. Equivalent.
- `OwnerStep.tsx:151`, the `sx={{ pl: 4 }}` indent. Layout that jsdom does not compute.

No survivor revealed a bug.

## Frontend gates after the kill round

`pnpm test`: 568 files passed, 3 skipped; 8732 tests passed, 35 skipped (the pending scenarios of later slices), 231 s.
`pnpm build`: exit 0; Biome applied no fixes; `git diff` of `src/` shows only the added tests, no mutant left behind.
