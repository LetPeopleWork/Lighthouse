# Frontend test audit: how much of the `.tsx` test layer is logic

Read-only. No test, source file or config under `Lighthouse.Frontend` was edited. Written 2026-11-08 during DELIVER of slice A. The verdict block is the last
section of this record.

Method as decided in `feature-delta.md` (DESIGN "The audit", DISTILL "The audit verdict going to the maintainer"). The scripts and data that produced every
number below are in this folder and are named where used; a reader with the commit can redo them.

## 1. Population (scenario A-1)

- **Commit.** The Vitest listing ran with `HEAD` at `ced6e04acb72b5adda3fa87fe45e749e584e9f80`. `git diff --stat ced6e04ac HEAD -- Lighthouse.Frontend` is empty
  at the time of writing (other sessions have committed backend work since), so the population is the same at `HEAD`.
- **Listing command.** From `Lighthouse.Frontend`: `TZ=Europe/Zurich pnpm exec vitest list --json=<file>`. It collects the tests and runs none.
- **File list.** `git ls-files 'Lighthouse.Frontend/src/**/*.test.tsx'`, saved as `git-ls-files-tsx.txt`.

Output of `population.py` (`population.txt`), counts:

```
git ls-files .test.tsx: 410
vitest listing files .test.tsx: 410
vitest listing files total: 617  tests total: 7970
tsx tests in listing: 5392
in git ls-files, not in listing: []
in listing, not in git ls-files: []
```

**Difference between the listing and `git ls-files`: none.** The two sets of 410 files are equal, file by file (both difference lists are empty), so
nothing is named and the audit goes on.

One count differs from the brief and is explained here, not left open: the listing holds **5,392** `.tsx` tests and the brief and the timing CSV say **5,857**.
The CSV has one row per executed case, the listing one entry per test title, and a parametrised title (`it.each`, a loop) is one entry in the listing and
several rows in the CSV. The weights below use the listing's counts, so a parametrised title counts once. The 465 difference is not itemised.

### Groups, quotas recomputed from 410

Quota = 50 x group files / 410, rounded half up; the rounded quotas add to 50, so the largest group takes no correction.

| Group (path under `Lighthouse.Frontend/src`) | Files | Tests (listing) | 50 x files / 410 | Quota | Quota in DESIGN (408) |
|---|---|---|---|---|---|
| `components/Common/Charts` | 37 | 591 | 4.51 | 5 | 5 |
| `components/Common`, without `Charts` | 125 | 1,673 | 15.24 | 15 | 15 |
| `pages/Common/MetricsView` | 31 | 507 | 3.78 | 4 | 4 |
| `pages/Portfolios` | 54 | 542 | 6.59 | 7 | 7 |
| `pages/Common/Reports` and `pages/Reports` | 36 | 511 | 4.39 | 4 | 4 |
| `pages/Settings` | 28 | 350 | 3.41 | 3 | 3 |
| `pages/Teams` | 45 | 573 | 5.49 | 5 | 6 |
| the rest | 54 | 645 | 6.59 | 7 | 6 |
| **Total** | **410** | **5,392** | | **50** | 50 |

"The rest" is every file outside the seven named groups; it holds 54 files, two more than DESIGN's 52, which are the two files the per-area count of 408
missed. Teams moves from 6 to 5 and the rest from 6 to 7 because of the recount. Sample: 50 files, 682 tests.

## 2. The picks, and that they can be redone (scenario A-2)

Rule: sort the group's files by path; the i-th pick, for i = 1 to the quota, is the file at position ceil(i x files / quota), counting from 1. The full
list, in pick order, is `picks.json`; the table in section 4 lists the 50 files in that order.

Positions per group, computed by `population.py` at DELIVER time from the 410-file list (not hard-coded from 408):

| Group | Files | Quota | Positions |
|---|---|---|---|
| `components/Common/Charts` | 37 | 5 | 8, 15, 23, 30, 37 |
| `components/Common`, without `Charts` | 125 | 15 | 9, 17, 25, 34, 42, 50, 59, 67, 75, 84, 92, 100, 109, 117, 125 |
| `pages/Common/MetricsView` | 31 | 4 | 8, 16, 24, 31 |
| `pages/Portfolios` | 54 | 7 | 8, 16, 24, 31, 39, 47, 54 |
| `pages/Common/Reports` and `pages/Reports` | 36 | 4 | 9, 18, 27, 36 |
| `pages/Settings` | 28 | 3 | **10, 19, 28** |
| `pages/Teams` | 45 | 5 | 9, 18, 27, 36, 45 |
| the rest | 54 | 7 | 8, 16, 24, 31, 39, 47, 54 |

`pages/Settings` has 28 files with quota 3, so its positions are 10, 19 and 28, as expected. In every group the last pick is the group's last file
(`population.py` asserts it; the rightmost position equals the group's file count in every row above).

**The smallest and the largest group redone by hand**, with `redo_by_hand.sh` (shell tools only: `git ls-files`, `grep`, `LC_ALL=C sort`, `sed`, integer
arithmetic for the ceiling), output in `redo_by_hand.txt`:

```
pages/Settings: 28 files, quota 3
1 @10 pages/Settings/DemoData/DemoDataSettings.test.tsx
2 @19 pages/Settings/System/SystemSettingsTab.test.tsx
3 @28 pages/Settings/SystemInfo/ThirdPartyPackagesSection.test.tsx
components/Common without Charts: 125 files, quota 15
1 @9 components/Common/Connection/ModifyConnectionSettings.parentSources.test.tsx
2 @17 components/Common/Connections/OAuthPopupComplete.test.tsx
3 @25 components/Common/DataGrid/ColumnOrderDialog.test.tsx
4 @34 components/Common/DateRangeSelector/DateRangePresets.test.tsx
5 @42 components/Common/FeatureGrid/FeatureGrid.test.tsx
6 @50 components/Common/FeatureListDataGrid/columns.forecastedStart.beyondTheHorizon.test.tsx
7 @59 components/Common/FilterBar/FilterBar.test.tsx
8 @67 components/Common/FormSelectField/FormSelectField.test.tsx
9 @75 components/Common/OnboardingStepper/OnboardingStepper.licenseCheck.test.tsx
10 @84 components/Common/ProjectSettings/ModifyProjectSettings.queryCommit.test.tsx
11 @92 components/Common/QuickSettings/ThroughputQuickSetting.test.tsx
12 @100 components/Common/StateMappings/StateMappingsEditor.test.tsx
13 @109 components/Common/Team/ModifyTeamSettings.test.tsx
14 @117 components/Common/ValidationActions/SaveStateIndicator.test.tsx
15 @125 components/Common/WorkItemsDialog/WorkItemsDialog.test.tsx
```

Both lists match `picks.json` file for file and position for position (compare with the `population.txt` picks above). The first attempt of the hand redo for
Settings gave 27 files because a git pathspec with `**/` skips a file that sits directly in `pages/Settings/`; the script now lists the folder and filters
by suffix, and gives 28. That was a fault of the redo's pathspec, not of the pick list.
