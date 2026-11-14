# Epic 6473 Settings split — mutation results

Stack: frontend only (the feature changed no backend production code). Tool: StrykerJS with the Vitest runner,
config `stryker.6473.frontend.json` and `vitest.stryker.mutation.ts` in this folder, `coverageAnalysis: perTest`,
incremental file `reports/mutation/stryker-incremental-6473.json` (gitignored). Run from `Lighthouse.Frontend/`.

| Run | Date | Killed | Survived | No coverage | Scored mutants | Score |
|---|---|---|---|---|---|---|
| 1, full | 2026-11-13 | 364 | 153 | 6 | 523 | 69.60 % |
| 2, incremental after one kill round | 2026-11-14 | 421 | 96 | 6 | 523 | **80.50 %** |

Gate: ≥ 80 % — **met**. Run 2 reused 408 of the earlier results and re-tested the 115 mutants that the new tests
could reach. The config did not change between the runs, so the two scores are comparable.

## Kill round (commit 99bf380d1)

New or widened tests: `SettingsMenu.test.tsx`, `LicenseStatusContent.busy.test.tsx`, `useBehaviourSettings.test.tsx`,
`useConfiguredConnections.test.tsx`, `resolveSettingsLocation.test.ts`, `settingsAreas.test.ts`,
`OverviewDashboard.test.tsx`, `ConnectionsSettings.list.test.tsx`, `settingsRoutes.legacyAddress.test.tsx`.
All 9 files pass (87 tests); the mutants they kill in run 2 are their proof of failing on a broken line.

## Per file, run 2

| File | Score | Survived | No cov |
|---|---|---|---|
| models/Settings/settingsAreas.ts | 98.39 % | 1 | 0 |
| models/Settings/resolveSettingsLocation.ts | 93.10 % | 2 | 0 |
| hooks/useBehaviourSettings.ts | 92.11 % | 1 | 2 |
| hooks/useConfiguredConnections.ts | 91.30 % | 2 | 0 |
| hooks/useLicenseRestrictions.ts | 70.59 % | 3 | 2 |
| pages/Settings/System/BehaviourSettingsSection.tsx | 94.74 % | 2 | 0 |
| pages/Settings/System/BlackoutSettings.tsx | 84.21 % | 3 | 0 |
| pages/Settings/System/LicenseTab.tsx | 50.00 % | 1 | 0 |
| pages/Settings/System/UsageDataTab.tsx | 0.00 % | 1 | 0 |
| pages/Settings/Organization/CalendarTab.tsx | 80.00 % | 1 | 0 |
| pages/Settings/Organization/RefreshAndOrderingTab.tsx | 100.00 % | 0 | 0 |
| pages/Settings/Organization/TerminologyTab.tsx | 0.00 % | 2 | 0 |
| pages/Settings/Connections/ConnectionsSettings.tsx | 75.61 % | 20 | 0 |
| pages/Settings/SettingsAreaPage.tsx | 60.00 % | 4 | 0 |
| pages/Settings/settingsRoutes.tsx | 76.19 % | 5 | 0 |
| pages/Settings/DemoData/DemoDataSettings.tsx | 100.00 % | 0 | 0 |
| pages/Connections/Edit/EditConnection.tsx | 83.33 % | 1 | 0 |
| pages/Overview/OverviewDashboard.tsx | 76.92 % | 9 | 0 |
| pages/Overview/ConnectFirstHint.tsx | 100.00 % | 0 | 0 |
| components/App/Header/SettingsMenu.tsx | 83.33 % | 5 | 0 |
| components/App/Header/Header.tsx | 0.00 % | 1 | 1 |
| components/App/Header/TaskManager/ConnectionsSection.tsx | 76.47 % | 4 | 0 |
| components/Common/LicenseStatus/LicenseStatusContent.tsx | 57.14 % | 21 | 0 |
| components/Common/LicenseStatus/LicenseStatusPopover.tsx | 0.00 % | 5 | 0 |
| components/Common/LicenseStatus/LicenseStatusIcon.tsx | 0.00 % | 0 | 1 |
| components/Common/Connection/CreateConnectionWizard.tsx | 50.00 % | 2 | 0 |
| components/Common/Connection/OAuthPremiumNotice.tsx | 100.00 % | 0 | 0 |
| components/Common/DataOverviewTable/DataOverviewTable.tsx | 100.00 % | 0 | 0 |
| components/Common/OnboardingStepper/OnboardingStepper.tsx | 100.00 % | 0 | 0 |

## Remaining survivors, recorded not chased

The gate is met, so iteration stopped here. Most of the 96 survivors are in `sx` style objects and layout props
(`display`, `gap`, `fontSize`, `textTransform`, `wordWrap`, `maxWidth`, colours): jsdom computes no layout, so a
test cannot see them, and they are equivalent mutants for a unit test. The rest cluster in
`LicenseStatusContent.tsx` (the upload-error caption and the hidden file input, mostly styling) and
`ConnectionsSettings.tsx` (row and button styling, plus the `forgetConfiguredConnections()` cache drop after a
change, which the Connections oracle covers end to end but the unit tests do not). The `useEffect` dependency array
in `LicenseStatusContent.tsx` survives because the tests render once with fixed inputs.
