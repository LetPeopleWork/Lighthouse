# Mutation testing — 6071 (Lighthouse disagrees with itself about the default throughput history window)

Run 2026-09-29 against `main` @ `8e66a9d08` (backend on `5638ea118`; the later commit adds frontend
tests only). Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0), lines changed by the fix | **100 %** | 12 | 12 | 0 | 0 | 3 m 04 s |
| Backend, whole mutated files | 62.04 % | 108 | 67 | 29 | 12 | (same run) |
| Frontend (StrykerJS 9.6.1), first run | 40.91 % | 44 | 18 | 12 | 14 | — |
| Frontend, after new tests | **93.18 %** | 44 | 41 | 3 | 0 | 1 m 17 s |

Configs: `stryker.6071.backend.json`, `stryker.6071.frontend.json`, `vitest.stryker.mutation.ts`.

## Backend

Stryker.NET mutates whole files, so the whole-file score measures the pre-existing controller code
around a 5-line change. Scope was checked in `mutation-report.json`: 96 mutants were tested, all in the
five listed files.

| file | changed lines | mutants on changed lines | killed | whole file killed / survived / no cov |
| --- | --- | --- | --- | --- |
| `API/Helpers/ThroughputHistoryValidator.cs` | whole file (new) | 5 | 5 | 5 / 0 / 0 |
| `API/TeamController.cs` | 141-146 | 5 | 5 | 33 / 14 / 7 |
| `API/TeamsController.cs` | 107-111 | 2 | 2 | 21 / 10 / 0 |
| `Models/Team.cs` | 5-6, 9, 19 | 0 (constant and initialiser values) | — | 6 / 3 / 0 |
| `Models/Portfolio.cs` | 7-8, 31 | 0 (constant and initialiser values) | — | 2 / 2 / 5 |

### Accepted survivors

All 29 survivors and 12 uncovered mutants sit on lines this fix did not touch: staleness-range checks,
cancellation-token defaults, rule-set validation, the CSV licence message, the premium cycle-time
branch, and `Portfolio`'s feature-size helpers. They belong to the pre-existing suites of those
controllers and models, not to this fix.

### Not mutated

`Services/Implementation/WorkItems/WorkItemService.cs` — one changed line (the percentile-history
fallback now treats 0 or less like null) in a 1,198-line file. Whole-file mutation would score the
unrelated service. The line is pinned by `WorkItemServiceTest`'s null / 0 / −5 cases, which assert the
exact 90-day window passed to the cycle-time query; each non-null case failed before the fix.

## Frontend

Line-scoped `mutate` entries, one span per entry, covering only the lines the fix changed.

| file | killed | survived | no cov |
| --- | --- | --- | --- |
| `pages/Teams/Edit/EditTeam.tsx` | 14 | 1 | 0 |
| `pages/Portfolios/Edit/EditPortfolio.tsx` | 14 | 1 | 0 |
| `components/Common/QuickSettings/ThroughputQuickSetting.tsx` | 9 | 0 | 0 |
| `components/Common/ProjectSettings/Advanced/FeatureSizeComponent.tsx` | 3 | 1 | 0 |
| `components/Common/CreateWizards/CreateTeamWizard.tsx` | 1 | 0 | 0 |

`CreatePortfolioWizard.tsx:42` produced no mutants (a constant reference); the constants files hold
only numeric literals, which StrykerJS does not mutate, so they are not listed.

### Closed by this pass (commit `8e66a9d08`)

- The Edit pages' settings loader never ran in any test: the mocked form dropped the loader prop. Tests
  now call it for an existing item (loads by route id, returns it unchanged) and for a clone (loads the
  source, returns it with id 0 and the name "Copy of …").
- The `cloneFrom` key itself: both page suites mocked `URLSearchParams.get` to answer any key, so a
  mutated key went unnoticed. The clone tests answer only for `cloneFrom`.
- The feature-size display fallback: a stored 0 shows 90, a stored 45 shows 45.
- The throughput quick setting: exactly 1 day saves; the rolling-days input carries `min="1"`.
- The Team wizard's new-Team settings: `useFixedDatesForThroughput` and `automaticallyAdjustFeatureWIP`
  are false.

### Accepted survivors

- `EditTeam.tsx:25` and `EditPortfolio.tsx:23`, `?? ""` → `?? "Stryker was here!"`: equivalent. A
  missing `cloneFrom` parses to NaN either way and opens the wizard.
- `FeatureSizeComponent.tsx:140`, `projectSettings?.` → `projectSettings.`: equivalent. The history
  field only renders when `projectSettings?.usePercentileToCalculateDefaultAmountOfWorkItems` is
  truthy, so `projectSettings` is never null on that line.
