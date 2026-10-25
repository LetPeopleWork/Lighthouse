# Mutation testing — 6407 (Epic 6400 slice 03: the Features tab and the Team in-progress read)

Run 2026-10-25 against local `main` @ `cd8cbe55a`. Gate is an 80 % kill rate on each stack the slice touched.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0), first run | 72.50 % | 40 | 29 | 2 (+9 no coverage) | 0 | 3 m 33 s |
| Backend, kill round (`FeatureRepository.cs` only) + `InProgressFeatureResolution.cs` from the first run | **90.00 %** | 40 | 36 | 3 (+1 no coverage) | 0 | 2 m 34 s (kill round) |
| Frontend | N/A | | | | | |

Frontend: N/A because the slice changes no frontend file.

Configs: `stryker.6407.backend.json` (first run), `stryker.6407.backend.kill.json` (kill round).
Stryker's own "will be tested" count was 31 on the first run (Stryker reports the 40 scored mutants
after the ones it ignores or that fail to compile are left out of that line; the per-file table below
counts every scored mutant). Sanity check: two files, 174 lines, so tens of mutants is the right order.

## Backend

Run from `Lighthouse.Backend/Lighthouse.Backend.Tests/`, `coverage-analysis: perTestInIsolation`,
concurrency 4, additional-timeout 30000, no `--with-baseline` / `--since`.

`mutate`, whole files (DEVOPS decision): `FeatureRepository.cs`, `InProgressFeatureResolution.cs`.

`test-case-filter` of the first run: `FeatureRepositoryTest`, `FeatureGraphReaderRelationalTest`,
`FeaturesWithReferenceIdsRelationalTest`, `InProgressFeatureResolutionTest`, `TeamMetricsServiceTests`,
`DependencyVerdictIdentityTest`, with the usual tail excluding Dogfood, `API.Integration`,
`Integration.Containers` and the live-connector categories. `FeatureRepositoryTest` is an
`IntegrationTestBase` fixture but carries no Integration category and runs in about a second, so it is in.

| file | scored | killed (first run) | survived | no coverage | killed after the kill round |
| --- | --- | --- | --- | --- | --- |
| `Repositories/FeatureRepository.cs` | 22 | 12 | 1 | 9 | 19 (2 survivors, 1 no coverage) |
| `InProgressFeatureResolution.cs` | 18 | 17 | 1 | 0 | 17 (1 survivor) |
| **Total** | **40** | **29** | **2** | **9** | **36 (3 survivors, 1 no coverage)** |

Per method group in `FeatureRepository.cs`: 8 of the 9 first-run no-coverage mutants and the two `<` to
`<=` survivors sit in `ConfirmForecastsAsOf` (Story 6354, not touched by this slice; the first run's
filter simply left out its tests). The ninth is `GetAllReferenceIds` (below). The one survivor in the
slice's own code is the `First()` below; the members the slice added or changed
(`WholeGraphWithoutHistograms`, `FeaturesWithReferenceIds`, `GetStoredFeatureCreatedDates`, `GetFeatures`)
have no survivors.

### Closed by this pass

- `ConfirmForecastsAsOf` (8 no-coverage mutants: both statements removed, the `&&` turned to `||`, and the
  comparison flipped) was never reached because the filter omitted `ForecastConfirmationRepositoryRelationalTest`,
  which already pins it (a forecast older than `confirmedAt` is moved up, a newer one and a forecast of
  another Feature are left alone). The kill config adds that class; no new test was needed. 6 of the 8 die;
  the other 2 are the accepted `<=` survivors below.
- `GetByPredicate` `SingleOrDefault()` to `Single()`: new `FeatureRepositoryTest.GetByPredicate_NoFeatureMatches_ReturnsNull`
  pins that asking for a Feature that does not exist answers null rather than throwing.
- `GetAllReferenceIds` block removal (line 56) was no coverage in both runs: only a mocked controller test
  used the method. Added `FeatureRepositoryTest.GetAllReferenceIds_ListsTheReferenceIdOfEveryStoredFeature`
  after the kill round; it is not re-measured (one kill round only), so the figures above still count the
  mutant as uncovered.

### Accepted survivors

- `InProgressFeatureResolution.cs:42` `group.First()` to `group.FirstOrDefault()`: equivalent. A `GroupBy`
  group always has at least one element, so the two never differ.
- `FeatureRepository.cs:32` and `:36` `CreationTime < confirmedAt` to `<=`: equivalent. A forecast whose
  `CreationTime` already equals `confirmedAt` is rewritten to the very same value, so no observable
  state differs.

### Not mutated

- `TeamMetricsService.cs`: about 1000 lines, too big to mutate whole (Stryker.NET ignores line ranges). Its
  in-progress logic was extracted into `InProgressFeatureResolution.cs` for exactly this; the rest of its
  change is covered by `TeamMetricsServiceTests`, including the never-read-Features test.
- `FeaturesController.cs`: one read swap, covered by the Slice03 goldens and `FeaturesControllerTest`.
- `ForecastService.cs`: a constructor parameter type only.
- `IFeatureRepository.cs`: an interface, nothing to mutate.

## Frontend

N/A because the slice changes no frontend file.
