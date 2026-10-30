# Mutation testing: 6430 (Epic 6350 slice 02b: saying why Create is unavailable)

Run 2026-10-30 against local `main` @ `ac07b3f24` (production code frozen) plus the kill-round tests below. Gate: 80 %
kill rate on the lines slice 02b changed (`git diff 7ed1ce7c7..HEAD` on the four files). Frontend only; slice 02b touched
no backend file, so there is no backend run (N/A).

## Frontend (StrykerJS 10.0.0)

Config `stryker.6430.frontend.json` and `vitest.stryker.6430.mutation.ts` (the vitest config is gitignored in
`Lighthouse.Frontend/`). `mutate` lists the changed line ranges of `useRbac.ts`, `OwnerStep.tsx`,
`BulkReportWizard.tsx` and `ReportsPage.tsx`, so the scores below are changed-line scores. Settings: `perTest`,
`--incremental` (file `reports/mutation/stryker-incremental-6430.json`, gitignored), concurrency 2, `inPlace`,
`disableTypeChecks: false`, `TZ=Europe/Zurich`. Command, from `Lighthouse.Frontend/`:

```
TZ=Europe/Zurich pnpm exec stryker run ../docs/feature/epic-6350-bulk-reports/mutation/stryker.6430.frontend.json --incremental
```

The vitest include is `src/pages/Reports/**/*.test.{ts,tsx}` and `src/hooks/useRbac.test.ts` (93 tests, 94 after the
kill round). Tests of the other 17 files that import `useRbac` are left out: the ranges changed in `useRbac.ts` are
reached by `useRbac.test.ts` and by every Reports test through the wizard and page.

| file | first run | after kill round |
| --- | --- | --- |
| `hooks/useRbac.ts` | 9 / 10 | 9 / 10 (90.00 %) |
| `pages/Reports/BulkReportWizard.tsx` | 9 / 10 | 9 / 10 (90.00 %) |
| `pages/Reports/OwnerStep.tsx` | 39 / 46 | 43 / 46 (93.48 %) |
| `pages/Reports/ReportsPage.tsx` | 9 / 9 | 9 / 9 (100 %) |
| **total** | 66 / 75 = 88.00 % | **70 / 75 = 93.33 %** |

Run times: first run 6 m 24 s; kill-round re-runs (incremental, 33 of 75 results reused) 3 m 23 s each, of which about
45 s is the dry run.

### Kill round (tests added to `BulkReportWizard.ownerStepLooks.test.tsx`)

The "You are not Admin of any Team or Portfolio" sentence is shown only once the owners are ready and none is listed.
Four mutants of that condition in `OwnerStep.tsx:132` survived because no test said it is absent in the other states.
Added:

- The Admin-owners-only listing, the could-not-load look and the Retry loading look each assert the sentence is absent.
- A new test: permissions cannot be loaded and no Team is on offer either; the could-not-load look shows and the
  sentence does not.

### Accepted survivors (5)

- `useRbac.ts:43`, `useCallback` deps `[]` to `["Stryker was here"]`. A constant dependency never changes, so `retry`
  stays stable exactly as before. Equivalent.
- `BulkReportWizard.tsx:98`, dropping `ownersStatus !== "ready" ||` from Create's `disabled`. Equivalent/defensive:
  the owner list is empty unless the owners are ready, nothing can be picked from an empty list, and `pickedCount === 0`
  already disables Create. The guard only matters if a pick outlived a reload of the owners; no path does that. No
  contrived test added, production code unchanged.
- `OwnerStep.tsx:127` (three), the `sx` of the could-not-load row (`display: "flex"`, `alignItems: "center"`, the
  whole object). Layout that jsdom does not compute.

Not mutated: nothing else in the four files lies on a changed line outside the ranges in the config.

## Frontend gates after the kill round

`pnpm test`: 566 files passed, 5 skipped; 8681 tests passed, 47 skipped (the pending scenarios of later slices), 204 s.
`pnpm build`: exit 0; Biome checked 1209 files, no fixes applied; `git status` shows no mutant left in `src/`.
