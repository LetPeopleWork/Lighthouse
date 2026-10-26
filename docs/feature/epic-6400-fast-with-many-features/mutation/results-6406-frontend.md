# Mutation testing, frontend — 6406 (Epic 6400 slice 02: the license check is one shared read)

Run 2026-10-26 (StrykerJS, vitest runner) against local `main` @ `fd8bdc478`; the kill round ran against the
test commit that follows it. Gate: 80 % per file on the mutated spans. The backend half is in `results-6406.md`.

| run | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| first run | 92.11 % | 152 | 140 | 11 | 1 | 13 m 45 s (dry run 1 m 52 s, 393 tests) |
| after the kill round (one incremental re-run, same config) | **99.34 %** | 152 | 151 | 1 | 0 | 6 m 18 s (dry run 1 m 44 s, 399 tests) |

Configs: `stryker.6406.frontend.json`, `vitest.stryker.6406.mutation.ts` (copy both to `Lighthouse.Frontend/`
before a run, delete them after; the 6408 ones are untouched).

Launched from `Lighthouse.Frontend/` with `nohup pnpm exec stryker run stryker.6406.frontend.json > log 2>&1 &`.
`coverageAnalysis: perTest`, `incremental` (`reports/mutation/stryker-incremental-6406.json`), `inPlace`,
`disableTypeChecks: false`, concurrency 2, anchored `ignorePatterns`. `git status` after both runs showed no
instrumented source. `OverviewDashboard.test.tsx` is not in the vitest include (77 s); its `licenseCheck` suite is.

## Per-file table

Spans are the lines the slice's frontend commits changed (`git diff a887ee5f8~3..HEAD`), current numbers, import
lines left out.

| file | spans | scored | killed (first run) | killed (final) | score (final) |
| --- | --- | --- | --- | --- | --- |
| `hooks/useLicenseRestrictions.ts` | whole file | 84 | 75 | 84 | 100 % |
| `components/App/License/LicenseToolTip.tsx` | 19-32 | 11 | 11 | 11 | 100 % |
| `components/Common/CreateWizards/CreateTeamWizard.tsx` | 65-75 | 2 | 2 | 2 | 100 % |
| `components/Common/CreateWizards/CreatePortfolioWizard.tsx` | 67-77 | 2 | 2 | 2 | 100 % |
| `components/Common/CreateWizards/CreateWizardShell.tsx` | 70-75, 126, 178-182 | 14 | 14 | 14 | 100 % |
| `components/Common/CreateWizards/WizardStepperFrame.tsx` | 11-12, 20-26, 50-71 | 5 | 5 | 5 | 100 % |
| `components/Common/OnboardingStepper/OnboardingStepper.tsx` | 17-18, 46, 58, 93, 101, 109, 113-126, 192-202 | 4 | 4 | 4 | 100 % |
| `components/Common/ProjectSettings/ModifyProjectSettings.tsx` | 50-53, 123-129, 175, 398, 422-423 | 10 | 7 (+2 survived, 1 no coverage) | 9 | 90 % |
| `components/Common/ValidationActions/SaveStateIndicator.tsx` | 20-24, 36-43 | 7 | 7 | 7 | 100 % |
| `pages/Portfolios/Edit/EditPortfolio.tsx` | 36, 153-158 | 7 | 7 | 7 | 100 % |
| `pages/Teams/Edit/EditTeam.tsx` | 42, 146-150 | 6 | 6 | 6 | 100 % |
| `pages/Overview/OverviewDashboard.tsx` | 152, 472, 516, 539 | 0 | - | - | no mutants |
| `components/Common/Team/ModifyTeamSettings.tsx` | 48-49, 121, 399 | 0 | - | - | no mutants |
| `components/Common/LicenseStatus/LicenseStatusIcon.tsx` | 7-8, 22 | 0 | - | - | no mutants |
| **Total** | | **152** | **140** | **151** | **99.34 %** |

The three files with no mutants were checked line by line: their changed spans are a type field, a prop
pass-through, a destructured prop and a hook call, none of which StrykerJS mutates. Each entry was a single span
(`file:a-b`); every file expected in the table either appears or is listed above as having no mutants.

## Survivors of the first run and their verdicts

| line | mutant | verdict |
| --- | --- | --- |
| `useLicenseRestrictions.ts:30` | `code === 403` to `true` | real; killed by "a list read that fails for any reason other than being refused is a failed check" |
| `useLicenseRestrictions.ts:39` | `queryKey: ["licenseStatus"]` to `[]` / `[""]` | real; killed by the test that answers from the licence, Teams and Portfolios already held under their names |
| `useLicenseRestrictions.ts:41` | `staleTime` `* 5` to `/ 5`, `* 60` to `/ 60` | real; killed by "a licence answer read a minute ago is still in date" |
| `useLicenseRestrictions.ts:42` | `refetchInterval` arithmetic (2 mutants) | real; killed by "the licence is asked for once, not again a second later" (1.3 s real wait) |
| `useLicenseRestrictions.ts:60` | `queryKey: ["portfolios"]` to `[]` / `[""]` | real; killed with the key test above |
| `ModifyProjectSettings.tsx:129` | `disableSave === undefined` to `true` | real; killed by the two `disableSave` tests |
| `ModifyProjectSettings.tsx:129` (no coverage) | `!disableSave` to `disableSave` | real; killed by "a caller that switches saving off says why" |

## Remaining survivor

- `ModifyProjectSettings.tsx:126` `useState<ITeam[]>([])` to `["Stryker was here"]`: equivalent. The teams are read
  only after the page has loaded, and loading replaces the initial value first; nothing renders the initial array.

## Tests added

- `useLicenseRestrictions.status.test.tsx` (4): non-403 list failure is a failed check; a one-minute-old license
  answer is not asked for again; no second ask within 1.3 s; answers from cache held under `licenseStatus`, `teams`
  and `portfolios`.
- `ModifyProjectSettings.test.tsx` (2): `disableSave` true shows "Not saved: ..." and saves nothing even when the
  license allows; `disableSave` false saves even when the license refuses.
