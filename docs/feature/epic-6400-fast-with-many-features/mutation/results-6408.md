# Mutation testing — 6408 (Epic 6400 slice 04: one Features list per owner)

Run 2026-10-25 against local `main` @ `a5e025498` (first runs); the kill rounds ran against the test commits that follow it. Gate is an 80 % kill rate on each stack the slice touched.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0), first run | 64.86 % | 37 | 24 | 12 (+1 no coverage) | 0 | 4 m 25 s |
| Backend, after the kill round (one round, five files re-run, `FeatureListOwners.cs` and `DependenciesAsReadExtensions.cs` carried over from the first run) | **86.49 %** | 37 | 32 | 5 | 0 | 3 m 52 s (kill round) |
| Frontend (StrykerJS), first run | 66.67 % | 111 | 74 | 23 (+14 no coverage) | 0 | 4 m 19 s |
| Frontend, after the kill round (incremental) | **84.68 %** | 111 | 94 | 10 (+7 no coverage) | 0 | 2 m 20 s (kill round) |

Scores count no-coverage mutants as not killed, as Stryker does.

Configs: `stryker.6408.backend.json` (first run), `stryker.6408.backend.kill.json` (kill round),
`stryker.6408.frontend.json`, `vitest.stryker.6408.mutation.ts` (copy it to `Lighthouse.Frontend/`, where it is
gitignored, before a run; the 6404 one is untouched).

## Backend

Run from `Lighthouse.Backend/Lighthouse.Backend.Tests/`, `coverage-analysis: perTestInIsolation`, concurrency 4,
additional-timeout 30000, no `--with-baseline` / `--since`, `StrykerOutput` removed first.
Stryker's own "will be tested" count was 36 on the first run (of 22 734 created for the whole backend), which is the
right order for seven small files: about 500 lines in all.

`test-case-filter` of the first run: `FeatureListServiceTest`, `FeatureListReaderRelationalTest`,
`FeatureListReaderRegistrationTest`, `API.FeaturesControllerTest`, `DependencyVerdictIdentityTest` and
`Slice04OneListPerOwnerScenarios`, with the usual tail excluding Dogfood, `Integration.Containers` and the live-connector
categories.

The acceptance class `Slice04OneListPerOwnerScenarios` is in on purpose: it has 10 tests and is the only thing that
reaches `FeatureListController` and `FeatureListRows` (no unit test did before this pass). The rest of
`API.Integration` is out. The kill round keeps it in as well, next to the new unit tests.

| file | scored | killed (first run) | survived | no coverage | killed after the kill round |
| --- | --- | --- | --- | --- | --- |
| `API/FeatureListController.cs` | 4 | 3 | 0 | 1 | 4 |
| `API/FeatureListRows.cs` | 8 | 6 | 2 | 0 | 8 |
| `Services/Implementation/FeatureListService.cs` | 12 | 9 | 3 | 0 | 11 |
| `Services/Implementation/FeatureListActiveWork.cs` | 3 | 1 | 2 | 0 | 2 |
| `Services/Implementation/FeatureListOwners.cs` | 0 | 0 | 0 | 0 | 0 (no mutants: two expression-bodied forwards) |
| `Services/Implementation/Dependencies/FeatureDependencyRendering.cs` | 8 | 3 | 5 | 0 | 5 |
| `API/DTO/DependenciesAsReadExtensions.cs` | 2 | 2 | 0 | 0 | 2 (carried over, not re-run) |
| **Total** | **37** | **24** | **12** | **1** | **32 (5 survivors)** |

### Closed by this pass

New tests, all green with the rest of the suite:

- `FeatureListRowsTest.A_Feature_held_by_two_Portfolios_is_blocked_when_only_one_of_them_blocks_it` pins `Any()`
  across a Feature's Portfolios (the `Any` to `All` survivor).
- `FeatureListRowsTest.A_row_names_the_Portfolios_that_stand_in_the_way_of_moving_it` pins the removed
  `BlockingPortfolios.AddRange` statement.
- `FeatureListControllerTest` (two tests): a Portfolio or a Team that does not exist answers not found and no list is
  read. Closes the one no-coverage mutant, the Team's not-found block.
- `FeatureListServiceTest.A_parent_held_by_several_Portfolios_is_named_when_the_reader_may_read_any_one_of_them`
  pins `Any()` over the parent's Portfolios.
- `FeatureListServiceTest.Where_nothing_limits_what_the_reader_may_read_the_parents_of_the_rows_are_all_named` pins the
  "no limit on what the reader may read" branch of the readable-Portfolio fallback.
- `FeatureListServiceTest.A_Feature_without_a_reference_id_is_never_taken_for_one_a_Team_has_in_progress` pins the
  `Length > 0` guard in the Teams-at-work read.
- `FeatureDependencyRenderingTest` (two tests): a list that is already the whole graph is used as it stands and the graph
  is not read again; and the no-limit fallback for the blockers' Portfolios.

### Accepted survivors

- `FeatureListService.cs:68`, `FeatureListActiveWork.cs:27`, `FeatureDependencyRendering.cs:57`:
  `ConfigureAwait(false)` to `ConfigureAwait(true)`. Equivalent: there is no synchronization context to resume on in
  ASP.NET Core nor in the test host, so no assertion can tell the two apart.
- `FeatureDependencyRendering.cs:71` and `:76`: `First()` to `FirstOrDefault()` over the elements of a `GroupBy`
  group. Equivalent: a group is never empty, so the two return the same element.

### Not mutated

- `API/DTO/FeatureListDto.cs`: DTO records with no behaviour beyond the constructor mapping, which the row tests read.
- `API/FeaturesController.cs`: the slice only removed code from it, and there is nothing left to mutate that it added.
- `Services/Implementation/Repositories/FeatureRepository.cs`: one changed method in a large file; the slice's reads are
  covered by `FeatureListReaderRelationalTest` and the Postgres parity test.

## Frontend

Run from `Lighthouse.Frontend/`, `coverageAnalysis: perTest`, incremental
(`reports/mutation/stryker-incremental-6408.json`), `inPlace`, `disableTypeChecks: false`, anchored `ignorePatterns`,
concurrency 2. `git status` after both runs showed no mutant left in `src/`.

`mutate`: `useFeatureList.ts` whole; line ranges covering what slice 04 changed in `PortfolioFeatureList.tsx`,
`TeamFeatureList.tsx`, `FeatureListDataGrid.tsx` and the `getFeatureList` span of `FeatureService.ts`. The vitest
include holds the hook, Portfolio and Team list, grid and service suites (77 tests in the first dry run).

| file | scored | killed (first run) | survived | no coverage | killed after the kill round |
| --- | --- | --- | --- | --- | --- |
| `hooks/useFeatureList.ts` | 27 | 20 | 1 | 6 | 26 |
| `pages/Portfolios/Detail/PortfolioFeatureList.tsx` | 26 | 22 | 2 | 2 | 22 |
| `pages/Teams/Detail/TeamFeatureList.tsx` | 28 | 10 | 16 | 2 | 24 |
| `components/Common/FeatureListDataGrid/FeatureListDataGrid.tsx` | 20 | 12 | 4 | 4 | 12 |
| `services/Api/FeatureService.ts` | 10 | 10 | 0 | 0 | 10 |
| **Total** | **111** | **74** | **23** | **14** | **94 (10 survivors, 7 no coverage)** |

### Closed by this pass

- `TeamFeatureList.rows.test.tsx` (new):
  - a row names the Team only when this Team has the Feature in progress, not when another Team does or nobody does,
    and the tooltip links the Team (the whole of the Team's Teams-at-work callback: 12 mutants);
  - a row names its parent from the same answer (the column dependency list);
  - Hide Completed hiding every Feature says how many there are and how to see them.
- `useFeatureList.refresh.test.tsx` (new): `useRefreshFeatureList` invalidates only its owner's list, and does nothing
  without a query client (6 no-coverage mutants); the hook names no parent before the answer arrives.

### Accepted survivors

- `FeatureListDataGrid.tsx:127` `filteredFeatures.length === 0` to `true`: equivalent. The message only matters when no
  row is shown, and `features.length > 0` already holds then.
- `PortfolioFeatureList.tsx:83` and `TeamFeatureList.tsx:105` (optional chaining, and the `?? []` fallback): equivalent.
  The callback only runs for a row, and a row only exists once the answer has arrived.
- `PortfolioFeatureList.tsx:106` and `TeamFeatureList.tsx:125`/`:127` (the empty message while the list is not ready):
  not pinned. The grid stays mounted under the loading and failure looks, and no test reads its text while the list is
  loading. A test that reads the DOM during loading would kill them; not written in this round.
- `FeatureListDataGrid.tsx:143` (5 mutants): the sort-model callback that tells the grid a column is sorting, which
  decides whether a row's move is offered. No test sorts a column and then reads the move gate. A real gap, left open after the one kill round.
- `FeatureListDataGrid.tsx:166` (2 mutants): `hasContentToDim` for an empty list being refreshed. The difference between
  a dimmed empty grid and a lone spinner is not asserted anywhere.
- `PortfolioFeatureList.tsx:116` (no coverage): the `onOrderChanged` callback that re-reads the list after a move. No
  test completes a move through the Portfolio's list; the move itself is covered at the grid.
- `useFeatureList.ts:57` `[]` for the callback's dependency list: a callback kept from an earlier owner would invalidate
  the old list. Only a rerender with a different owner shows it; not written.

### Not mutated

- The lines of `PortfolioFeatureList.tsx`, `TeamFeatureList.tsx`, `FeatureListDataGrid.tsx` and `FeatureService.ts`
  that slice 04 did not change.
