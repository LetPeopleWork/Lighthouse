# Mutation testing — 5688 (Manual sorting: a Features view showing the order that drives the forecast)

Run 2026-08-06 against `worktree-deep-brewing-starfish`, rebased on `main` @ `ba2b30157`. Gate is 80 %
kill rate on both stacks.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **80.39 %** | 50 | 41 | 9 | 0 | 2 m 50 s |
| Frontend (StrykerJS 9.6.1) | **80.00 %** | 70 | 56 | 10 | 0 | ~2 m |

Configs: `stryker.5688.backend.json`, `stryker.5688.frontend.json`, `vitest.stryker.mutation.ts`.

The backend gate passed on the second run. The first scored **66.67 %** (34 killed / 10 survived /
7 no-coverage); eight survivors were closed by new tests and the run repeated. Both runs are recorded
below, because the first one is where the useful finding was.

## Backend

### Per file

| File | Killed | Survived | No coverage | Score |
| --- | --- | --- | --- | --- |
| `Services/Implementation/FeaturePositionMap.cs` | 4 | 0 | 0 | **100 %** |
| `Models/FeatureComparer.cs` | 17 | 1 | 0 | **94.4 %** |
| `Services/Implementation/Repositories/FeatureRepository.cs` | 7 | 1 | 0 | **87.5 %** |
| `API/FeaturesController.cs` | 13 | 7 | 1 | 61.9 % |
| `Program.cs` | — | — | — | n/a (all mutants ignored or compile-error; DI registration only) |

**Read the aggregate with that table next to it.** The three files this slice created or reshaped score
28/29 = **96.6 %**. The 80.39 % aggregate is dragged down almost entirely by `FeaturesController.cs`,
whose seven survivors are all in `GetFeatureWorkItems` and the blocked-item projection — code paths this
slice never touched. Stryker.NET mutates whole files and **ignores line ranges** (`file.cs:20-40` silently
widens to the whole file), so there is no way to scope to the changed lines; the choice is mutate the
whole file and report per-file, or exclude it entirely. Excluding it would have hidden the 13 genuine
kills the new endpoint earned, so it stays in with this note.

### Closed by this pass

Eight survivors, each verified by re-applying the mutant by hand and confirming the intended test goes
red — not merely by watching the score move.

| Mutant | Now killed by |
| --- | --- |
| `FeatureComparer:43-46` block removal on `if (yIsInt) return 1;` | `Compare_WhenOnlyTheRightOrderIsAnInt_RanksTheIntAheadOfTheDecimal`. Most pairs do not discriminate — `Compare("abc","5")` and `Compare("0.5","5")` both stay positive by a different route. `Compare("9.5","5")` is the pair that does: `1` normally, `-1` mutated. |
| `FeatureComparer:51` `&&` → `\|\|` | `Compare_WhenOnlyTheLeftOrderIsADouble_FallsBackToStringComparison`. `Compare("9.5","1abc")` is positive normally and negative mutated, because the failed `TryParse` leaves `yDouble` at 0. Both operands start with a digit so no punctuation-collation assumption is involved. |
| `FeatureRepository:19` `OrderBy` → `OrderByDescending` | `GetAll_OrdersFeaturesByTheFeatureComparerLadder` |
| `FeatureRepository:19` `ThenBy` → `ThenByDescending` | `GetAll_FeaturesTiedOnOrder_ComeBackInAscendingId` |
| `FeatureRepository:24` `OrderBy`/`ThenBy` → descending (×2) | `GetAllByPredicate_AppliesTheSameOrderingToTheFilteredSet` |
| `FeatureRepository:31` `f.Id == id` → `!=` | `GetById_ReturnsTheFeatureWithThatId` |
| `FeatureRepository:36` predicate dropped | `GetByPredicate_ReturnsTheSingleMatchingFeature` |

**The finding worth more than the score**: `FeatureRepository` had **no test file at all**. `GetAll()` is
what `ForecastService` reads to decide which Features a team's simulated throughput lands on, and slice 01
had just added a `.ThenBy(f => f.Id)` tie-break to it that nothing exercised. The mutation run is what
surfaced that; the suite was green throughout.

Second observation, recorded because it changes what the tie-break test is worth: removing
`if (yIsInt) return 1;` does not merely misorder one pair — it makes the comparer **asymmetric**, so
`Compare("3","")` and `Compare("","3")` both return −1. Fed to `OrderBy`'s quicksort that scrambles the
whole sequence, which is why the two repository ordering tests catch it as well. The new `GetAll`
coverage therefore guards the comparer's int rung too.

### Accepted survivors

| Mutant | Why it stays |
| --- | --- |
| `FeatureComparer:54` `* -1` → `/ -1` | **Equivalent mutant.** `CompareTo` yields only −1, 0 or 1, and for those three values `* -1` and `/ -1` are identical. Unkillable by construction. |
| `FeaturesController:100` `AsEnumerable()` → `Reverse()` | In `GetFeatureWorkItems`, pre-existing. Work-item order within a Feature is not part of any AC in this slice and no test asserts it. |
| `FeaturesController:102` ×2 (logical + equality on `w.Team != null && IsBlocked(...)`) | Pre-existing blocked-item projection, untouched by this slice. |
| `FeaturesController:119` `Any()` → `All()` on `IsBlocked` | Pre-existing blocked-item projection. Belongs to Epic 5074's surface, not this one. |
| `FeaturesController:138` `.ConfigureAwait(false)` → `true` | No observable behaviour change in a test host. |
| `FeaturesController:140` conditional → always-true | Requires an `IRbacAdministrationService` returning null from the batch call; the shipped service never does. |
| `FeaturesController:143` `HttpContext?.RequestAborted ?? default` → `default` | Equivalent under test: the token is never signalled during a request, so both branches behave identically. |
| `FeaturesController:76` (no coverage) | Block removal in a path the filtered test set does not reach. |
| `FeatureRepository:36` `SingleOrDefault()` → `Single()` | Pre-existing `GetByPredicate`; differs only when no element matches, which no caller in this slice exercises. |

### Not mutated

`RbacAdministrationService.cs` (1469 lines, ~39 changed), `DemoDataService.cs` (scenario 15 only),
`FeatureDto.cs` (one auto-property) and the two interface files are excluded from `mutate`. In each case
the change is a small fraction of a large file, so mutating it would bury this slice's score under
untouched code with no diagnostic value.

`GetWritablePortfolioIdsAsync` is the one exclusion worth justifying explicitly, because it is
security-relevant: it is covered instead by five dedicated acceptance scenarios that drive the **real**
`RbacAdministrationService` over an isolated store, one per early-return branch (RBAC not enforced,
enforcement gate unsatisfied, RBAC manager, unrecognised caller) plus the predicate swap itself. That is
stronger evidence than mutating a method whose branches are already enumerated one-per-test.

## Frontend

Also passed on the second run. The first scored **57.14 %** (40 killed / 26 survived / 4 no-coverage).

### Per file

| File | Killed | Survived | Score | First run |
| --- | --- | --- | --- | --- |
| `pages/Features/FeaturesView.tsx` | 21 | 0 | **100 %** | 33.3 % |
| `components/Common/FeatureListDataGrid/columns.tsx` | 12 | 3 (+1 no-cov) | 75.0 % | 62.5 % |
| `components/App/Header/Header.tsx` | 7 | 2 | 77.8 % | 77.8 % |
| `components/Common/FeatureListDataGrid/FeatureListDataGrid.tsx` | 16 | 5 | 76.2 % | 76.2 % |

### Closed by this pass

`FeaturesView.tsx` went from 33.3 % to **100 %** — all 21 mutants killed, zero survivors. It was the one
genuine coverage gap in new code: a 77-line component shipped with two tests. Seven were added, and every
assertion was proven falsifiable by applying the mutant to the component and confirming the test goes red.

The survivors that needed the most care, because the obvious assertion does not discriminate:

| Mutant | What the killing test had to observe |
| --- | --- |
| `useState(true)` → `false` | The grid must show its loading state *before* rows arrive — asserting the final rendered list passes either way. |
| `join(", ")` → `join("")` | The Portfolio cell text must be asserted **exactly** (`"Platform, Payments"`). Asserting that both names appear passes under both. |
| `sortable: false` → `true` | Paired with a positive check that another column *does* offer a sort control, so "no control found" cannot pass vacuously. |
| `useMemo` deps `[featureTerm, portfoliosTerm]` → `[]` | The column titles must change when the instance renames the concepts mid-session — which required the terminology mock to become mutable. |
| `` `No ${featuresTerm} found` `` → `""` | The empty state must be read through the terminology provider, so a hard-coded "Features" fails. |

One survivor the sub-agent reported as unreachable — `color="text.secondary"` → `""`, on the grounds that
jsdom does not resolve the MUI palette — turned out to be killed by one of the sibling assertions. Recorded
because the reasoning was sound and the conclusion still wrong: it is worth re-scoring before accepting an
"equivalent mutant" claim.

### Accepted survivors

All ten are pre-existing behaviour swept into scope, not new code:

| Mutant | Why it stays |
| --- | --- |
| `Header.tsx:62`, `:63` — `path: "/"` and `path: "/settings"` → `""` | The **pre-existing** Overview and System Settings nav entries. This slice added only the third entry, whose path and terminology-driven label are both killed. Pinning the other two is Header's own coverage debt. |
| `columns.tsx:25`, `:28`, `:31` (+1 no-coverage) | Inside `createNameColumn` — `hideable`, `width`, `flex`. Pre-existing behaviour that the L4 refactor *moved* into a shared factory; the refactor did not change it, and no AC in this slice constrains it. |
| `FeatureListDataGrid.tsx:28`, `:44`, `:57` ×2, `:60` | The grid's pre-existing column-assembly and storage-key wiring. The one line this slice added — the `showPosition` conditional injection — is killed by the two tests added for review finding F3. |

### Not mutated

`App.tsx` (one route line), `models/Feature.ts` (one optional zod key), `index.ts`/`types.ts` (exports only).
No behaviour to mutate meaningfully.

## Workflow traps hit this run — worth carrying forward

1. **`vitest.configFile` resolves relative to the run directory, not to the Stryker config.** Pointing it
   at `docs/feature/<feature>/mutation/vitest.stryker.mutation.ts` fails with `UNRESOLVED_ENTRY` when
   Stryker runs from `Lighthouse.Frontend/`. The committed copy is the canonical one; copy it to
   `Lighthouse.Frontend/vitest.stryker.mutation.ts` for the run and delete it afterwards.
2. **A clean `git diff` does not mean a clean binary.** Restoring a mutated source file with a tool that
   preserves the backup's mtime (e.g. `shutil.move`) leaves MSBuild believing the output is up to date, so
   `dotnet build` reports "0 Warnings" and the next `dotnet test` runs against a **mutant**. Three minutes
   of green-looking output, all meaningless. After any tool restores source files, `touch` them or build
   `--no-incremental` before trusting the result.
3. **Never run `dotnet build` and `dotnet test` concurrently** — the assemblies are replaced mid-run and
   hundreds of tests fail in `SetUpTearDownItem.RunSetUp`, which reads exactly like a real regression.
4. Contrary to an earlier note in this repo, **whole-file `mutate` globs do scope correctly** on
   Stryker.NET: 14203 mutants created project-wide, 50 tested. It is *line ranges* that are ignored.
