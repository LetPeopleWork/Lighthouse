# Mutation testing — 6410 (Epic 6400 slice 06: the Portfolio size warning)

Run 2026-10-27 against local `main` @ `6f06878cb` plus the two kill-round tests below. Both stacks. Gate: 80 % kill rate
per stack on what the slice changed (`git diff 2d6a42c6b..HEAD`). `PortfolioController.cs`, `PortfolioUpdater.cs` and
`PortfolioRepository.cs` carry much older code, so whole-file scores are reported but survivors are triaged only in the
slice's changed regions.

## Backend (Stryker.NET 5.0.0)

Stryker's "will be tested" line: **89** (of 22 785 created for the whole backend). Config
`stryker.6410.backend.json`, from `Lighthouse.Backend/Lighthouse.Backend.Tests/`, `StrykerOutput` removed first,
`perTestInIsolation`, concurrency 4, unit classes only (`PortfolioSizeTest`, `PortfolioRepositoryTest`,
`PortfolioUpdaterTest`, `API.PortfolioControllerTest`; `API.Integration`, `Integration.Containers`, `Dogfood` and the
live-connector categories excluded):

```
dotnet stryker --config-file ../../docs/feature/epic-6400-fast-with-many-features/mutation/stryker.6410.backend.json
```

First run: 4 m 06 s, overall 58.59 %. `--since` / `--with-baseline` not used.

### Whole file (first run)

| file | scored | killed | survived | no coverage | score |
| --- | --- | --- | --- | --- | --- |
| `Models/PortfolioSize.cs` | 2 | 2 | 0 | 0 | 100 % |
| `API/DTO/PortfolioSizeDto.cs` | 0 | | | | no mutants |
| `BackgroundServices/Update/PortfolioUpdater.cs` | 47 | 40 | 5 | 2 | 85.11 % |
| `API/PortfolioController.cs` | 40 | 24 | 4 | 12 | 60.00 % |
| `Repositories/PortfolioRepository.cs` | 39 | 9 | 5 | 25 | 23.08 % |

The low `PortfolioRepository.cs` and `PortfolioController.cs` scores are older code whose tests live outside this
filter (the relational readers, the `API.Integration` suite), not slice 06 code.

### Changed regions

| file | region (lines in `HEAD`) | first run | after kill round |
| --- | --- | --- | --- |
| `PortfolioSize.cs` | `IsOverThreshold` | 2 / 2 | 2 / 2 |
| `PortfolioUpdater.cs` | call at `:129`, `WarnWhenTheUpdateLeavesTheLargePortfolio` `:192-201` | 5 / 5 | 5 / 5 |
| `PortfolioRepository.cs` | `FeatureCountOf` `:75-78` | 2 / 2 | 2 / 2 |
| `PortfolioController.cs` | `GetPortfolioSize` `:197-208` | **0 / 2** (both no coverage) | **2 / 2** |
| **total** | | 9 / 11 = 81.8 % | **11 / 11 = 100 %** |

Ignored mutants (block removals Stryker will not score) and the one `Count() to Sum()` compile error are not counted.

### Kill round

The endpoint's `!portfolioRepository.Exists(portfolioId)` guard and its not-found block were reached only by the
acceptance scenarios, which the unit filter leaves out, so both mutants had no coverage. Added to
`PortfolioControllerTest`: `GetPortfolioSize_PortfolioExists_ReturnsItsFeatureCountAgainstTheThreshold` and
`GetPortfolioSize_NoSuchPortfolio_ReturnsNotFoundWithoutCounting`. Re-run with `stryker.6410.backend.kill.json`
(controller only, `API.PortfolioControllerTest` only): 30 mutants, 2 m 04 s, `PortfolioController.cs` 65.00 % whole file;
both region mutants killed. The other four files were not re-run (nothing in them changed).

### Survivors outside the changed regions (older code, not triaged)

- `PortfolioController.cs`: `:50`, `:144`, `:149`, `:185` (older endpoints) and no-coverage mutants at `:73-159` (older
  endpoints, covered by the integration suite).
- `PortfolioUpdater.cs`: `:53` `TrySaveRecomputedDeliveries` negation, `:55`/`:56`/`:70`/`:162` log statements and messages,
  `:214` log in the orphan clean-up.
- `PortfolioRepository.cs`: `:98`, `:116` log lines, `:120` `p.Id != id`; the 25 no-coverage mutants are the forecast-input
  and catalogue code read by relational tests outside this filter.

## Frontend (StrykerJS 10.0.0)

Config `stryker.6410.frontend.json` and `vitest.stryker.6410.mutation.ts` (copied into `Lighthouse.Frontend/`, gitignored
there). The vitest config lists only `PortfolioSizeNote.test.tsx`, `ModifyProjectSettings.sizeNote.test.tsx`,
`ModifyProjectSettings.test.tsx` and `PortfolioService.test.ts`. `mutate`: `PortfolioService.ts:59-65` (`getSize`), the
whole of `PortfolioSizeNote.tsx`, `ModifyProjectSettings.tsx:208` (the one-line usage, which gives Stryker nothing to
mutate). From `Lighthouse.Frontend/`:

```
TZ=Europe/Zurich pnpm exec stryker run ../docs/feature/epic-6400-fast-with-many-features/mutation/stryker.6410.frontend.json --incremental
```

`perTest`, incremental file `reports/mutation/stryker-incremental-6410.json` (gitignored), concurrency 2, `inPlace`,
`disableTypeChecks: false`.

| run | score | tested | killed | survived | wall clock |
| --- | --- | --- | --- | --- | --- |
| first run | 80.77 % | 26 | 21 | 5 | 1 m 52 s |
| after kill round (incremental) | **92.31 %** | 26 | 24 | 2 | 47 s |

| file | first run | after kill round |
| --- | --- | --- |
| `PortfolioService.ts` (`getSize`) | 2 / 2, 100 % | 2 / 2, 100 % |
| `PortfolioSizeNote.tsx` | 19 / 24, 79.17 % | 22 / 24, 91.67 % |

Everything the slice changed on the frontend is within these two files, so whole-file and changed-region scores agree.

### Kill round (`PortfolioSizeNote.test.tsx`, two tests)

- `if (message === null)` to `false` and to an empty block (3 survivors): a Portfolio under the threshold rendered an
  empty alert. New test: no `role="alert"` while the size is on its way and once it is known to be under 100.
- `queryKey: []` (the array emptied): another Portfolio would have been served the first one's answer. New test: rerendering
  with another `portfolioId` asks the service for that id.

### Accepted survivors (2)

- `PortfolioSizeNote.tsx:21` `"portfolioSize"` to `""`: the cache key's name is never read back; a different prefix
  still keys by Portfolio id, so nothing observable changes. Equivalent.
- `PortfolioSizeNote.tsx:43` `size={{ xs: 12 }}` to `size={{}}`: grid column width, a layout concern jsdom does not compute.
  Equivalent for behaviour.

## Wall clock

Backend 4 m 06 s plus a 2 m 04 s kill round; frontend 1 m 52 s plus a 47 s re-run.
