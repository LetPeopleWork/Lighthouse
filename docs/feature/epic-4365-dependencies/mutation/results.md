# Mutation testing — Epic 4365 slice 01 (Show Feature Dependencies)

Run 2026-08-18 against `main` @ `f9b0cc19b`. Gate is 80 % kill rate on each stack touched.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **81.46 %** | 167 | 143 | 22 | 2 | 20 m 49 s |
| Frontend (StrykerJS 9.6.1) | **100.00 %** | 14 | 14 | 0 | 0 | 17 s |

Configs: `stryker.4365.backend.json`, `stryker.4365.frontend.json`, `vitest.stryker.mutation.ts`.

## The number that matters is not 81.46 %

**Every one of the 24 backend survivors is in pre-existing code that merely shares a file with this
slice's changes. Not one is in a line this slice wrote.** Checked by mapping each survivor's line
against `git diff -U0 3041e0b13..HEAD` per file.

On this slice's own lines the kill rate is **100 %**:

| file | tested | survived | this slice's lines |
| --- | --- | --- | --- |
| `DependencyReconciler.cs` | 2 | 0 | whole file is new |
| `FeatureDependencyReference.cs` | 1 | 0 | whole file is new |
| `FeatureRepository.cs` | 14 | 0 | the new projection read |
| `WorkItemExtensions.cs` — `ExtractDependencyReferences` (44-79) | 6 killed, 4 ignored | 0 | new |
| `FeatureDto.cs` / `FeaturesController.cs` / `Feature.cs` | — | 0 in changed lines | additive count path |

The design named one component where a surviving mutant would be unacceptable — the reconciler, because
a wrong verdict there is a wrong warning today and a wrong forecast date once the premium epic reads the
same data. It has **no survivors**.

`DependencySource.cs` produced no mutants: an enum with two members has nothing to mutate.

## Backend — accepted survivors

All 24 are pre-existing behaviour, untouched by this slice, and killing them means writing tests for
other features. Listed rather than absorbed into a percentage, so a future pass can attack them
deliberately.

**`FeatureDto.cs` (7)** — `namedCycleTimes ?? []` null-coalescing (both directions), the
`TeamsWithoutForecast` ordering and its statement, `Forecast?.CreateForecastDtos` null-coalescing, and
the two `RemainingWork[…] +=` / `TotalWork[…] +=` accumulations flipping to `-=`. The last pair is the
most interesting of the set: nothing asserts the *accumulation* across multiple work rows for one team.

**`FeaturesController.cs` (10)** — the parent-reference `AsEnumerable()`, the `w.Team != null &&` blocked
guard (both the logical and equality mutants), the `Ok()`/`NotFound()` conditional at `:154`, two boolean
flips, `feature.Portfolios.Any` → `All` at `:233`, a statement removal at `:244`, and the readable-portfolio
conditional and null-coalescing at `:269`/`:272`.

**`WorkItemExtensions.cs` (5)** — the parent-relation walk's block removal and its
`Attributes.TryGetValue("name", …)` logical mutant, a stack-rank string, and two timeouts in the
backlog-priority and created-date extractors. All belong to the parent path this slice deliberately left
alone; routing it through the new URL guard would have changed behaviour rather than refactored it.

**`Feature.cs` (2)** — a string mutant at `:84` and `FeatureWork.FirstOrDefault()` → `First()` at `:187`.

## Not mutated, and why

Three changed files were excluded because this slice's change is a rounding error inside them, and
Stryker.NET **ignores line-span `mutate` patterns** — a span silently widens to the whole file, so
including them would bury this slice's score under a thousand untouched lines:

| file | changed lines / total |
| --- | --- |
| `AzureDevOpsWorkTrackingConnector.cs` | 88 / 1327 |
| `WorkItemService.cs` | 23 / 1161 |
| `LighthouseAppContext.cs` | 28 / 661 |

Their behaviour is covered instead by the acceptance harness under
`Lighthouse.Backend.Tests/API/Integration/Dependencies/`, which drives a real refresh through the real
ASP.NET host and real EF with only the connector faked — the relation read, the parent-override fork and
the reconcile-on-both-branches wiring each have a scenario that fails when perturbed.

`JiraWorkTrackingConnector.cs` is also in the diff but belongs to an unrelated concurrency fix
(`930cc4142`), not this slice.

## Frontend

`columns.tsx:136-153` — the `renderDependsOnCount` helper and `createDependsOnColumn`, which is the whole
of this slice's frontend surface. Line ranges **do** work on the JS side, so this score describes only
the new code. 14 mutants, all killed, including both branches of the blank-versus-count decision and
every field of the column definition.

## Two traps this run hit

- **The vitest config must sit in `Lighthouse.Frontend/` at run time.** Its own
  `setupFiles: ["./setupTests.ts"]` resolves relative to itself, so pointing Stryker at the copy under
  `docs/` fails with `[UNRESOLVED_ENTRY] Cannot resolve entry module`. Committed here for the record;
  copy it into the frontend root to run, and delete the copy afterwards.
- **`inPlace: true` leaves residue on a failed exit.** The first attempt stranded
  `stryker-setup-0.js` and `stryker-setup-1.js` in the frontend root. `git status` after every frontend
  run, as the skill says.
