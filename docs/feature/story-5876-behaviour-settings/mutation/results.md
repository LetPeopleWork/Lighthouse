# Mutation testing — story 5876, slice 01 (a refused toggle says so)

Run 2026-09-01 against `main` @ `b0aba4f78`. Gate is 80 % kill rate.

| stack | score | tested | killed | survived | no coverage | ignored | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| **Backend (Stryker.NET), whole file** | **91.67 %** | 11 | 11 | 0 | 1 | 3 | 2 m 00 s |
| Frontend (StrykerJS) | **N/A** | — | — | — | — | — | — |

Config: `stryker.slice-01.backend.json`. **Frontend is N/A, not skipped**: slice 01 changed zero files
under `Lighthouse.Frontend/`. The slice is a controller contract change, and the control it governs is
already disabled for unlicensed instances, so no user-facing flow reaches the branch it fixes.

## Why the whole-file number is the gate here

Stryker.NET ignores line ranges in `mutate` — only whole-file globs work. That normally buries a
slice's own score under code it never touched, which is why other features in this repository report a
changed-lines figure recovered from the report by intersecting with `git diff`. It does not apply here:
`OptionalFeaturesController.cs` is 61 lines and three actions, so the whole file *is* the slice's
neighbourhood. No second run and no line-intersection was needed.

## Backend

| file | killed | survived | no coverage | ignored | score |
| --- | --- | --- | --- | --- | --- |
| `API/OptionalFeaturesController.cs` | 11 | 0 | 1 | 3 | 91.67 % |

Zero survivors. Every mutant with behaviour behind it died, including the three that matter most:

- `feature.IsPremium && !licenseService.CanUsePremiumFeatures()` → `||` — killed.
- the same condition negated wholesale — killed.
- `!licenseService.CanUsePremiumFeatures()` → `licenseService.CanUsePremiumFeatures()` — killed.

The refusal string mutated to `""` is also killed, which is the point of comparing the two doors by
hand: the test that pins the wording is not tautological, and this run is the evidence.

The three ignored mutants are block removals on `{ }` bodies that a sibling mutant already covers.

## The one uncovered mutant, and why it outlives this slice

`OptionalFeaturesController.cs:25` — `f.Key == featureKey` → `f.Key != featureKey`, **NoCoverage**.

That is the predicate inside `GetOptionalFeatureByKey`. Nothing executes it. The unit tests mock
`IRepository<OptionalFeature>.GetByPredicate`, so Moq returns a canned row without ever invoking the
lambda, and no acceptance scenario reaches `GET /optionalfeatures/{featureKey}` — the scenarios read
the list through `GetAll`. Inverting the comparison would return the wrong row, or none, and the suite
would stay green.

Slice 01 neither introduced this nor made it worse; the line is untouched by all three of its commits.
It is recorded here because **slice 02 routes the toggle write through exactly this predicate shape**.
An uncovered `==` becomes load-bearing the moment the write depends on it, and it is cheap to pin then
— the by-key write already has acceptance scenarios that exercise the real repository through
`WebApplicationFactory`, so covering the read is a matter of one scenario reading a setting by its key
rather than a new mechanism.

## Command

```
cd Lighthouse.Backend/Lighthouse.Backend.Tests
dotnet stryker -f stryker-config.story-5876-slice-01.json
```

The test filter spans both layers this slice pinned — `TestCategory=story-5876-behaviour-settings` for
the acceptance scenarios and `FullyQualifiedName~OptionalFeaturesControllerTest` for the unit matrix —
because the slice's defence is deliberately split across the door and the seam.

---

# Mutation testing — story 5876, slice 02 (one list of switches)

Run 2026-09-04 against `main` @ `b3e2e7ba8`. Gate is 80 % kill rate.

| stack | changed-lines score | whole-file score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| **Backend (Stryker.NET)** | **97.22 %** (35/36) | 62.60 % | 131 | 79 + 3 timeout | 18 | 31 | 24 m 14 s |
| **Frontend (StrykerJS)** | **90.67 %** (68/75) | 65.28 % | 216 | 141 | 64 | 11 | 6 m 52 s |

Both stacks clear the gate. **The changed-lines figure is the gate here, and the whole-file figure is
not** — the reason is mechanical and is worth stating rather than asserting.

## Why the whole-file number is not the gate this time

Slice 01 could report a whole-file score because its only file was 61 lines. Slice 02 touches ten
backend files, and two of them are large files whose own neighbourhoods bury the slice:

- `AppSettingService.cs` contributes **35** of the backend's 49 bad mutants. Thirty of those are
  `NoCoverage` in `SurveyNudge*` and `InstallTimestamp` code. The slice changed lines 99-112 of that
  file and nothing else; every mutant on those lines is killed.
- `TerminologyContext.tsx` contributed **44** of the frontend's 75 bad mutants on the first run, all
  in `defaultTerminologyMap`. That file is in the surface only because a review fix moved the map
  above the provider.

Stryker.NET ignores line ranges in `mutate` and honours only whole-file globs, so there is no way to
ask it for the slice's own lines. The changed-lines figures above were recovered by intersecting each
report with `git diff -U0 origin/main..HEAD` per file.

## Backend

Two runs. The first scored 94.87 % on changed lines with one real gap; the second, after the gap was
closed, scored **97.22 %**.

| file | killed | survived | no coverage | changed-lines score |
| --- | --- | --- | --- | --- |
| `API/OptionalFeaturesController.cs` | 2 | 0 | 0 | 100 % |
| `Services/Implementation/AppSettingService.cs` | 6 | 0 | 0 | 100 % |
| `Services/Implementation/FeatureOrdering.cs` | 5 | 0 | 0 | 100 % |
| `Services/Implementation/FeatureOrderingPolicyProvider.cs` | 5 | 0 | 0 | 100 % |
| `.../OptionalFeatures/DefaultOptionalFeatureApplier.cs` | 3 | 0 | 0 | 100 % |
| `.../OptionalFeatures/FeatureOrderingApplier.cs` | 7 | 0 | 0 | 100 % |
| `.../OptionalFeatures/OptionalFeatureApplierRegistry.cs` | 2 | 0 | 0 | 100 % |
| `.../Seeding/OptionalFeatureSeeder.cs` | 5 | 1 | 0 | 83.3 % |

### The one gap, and how it was closed

`DefaultOptionalFeatureApplier.cs:14` — `Key => string.Empty`, **NoCoverage**. Any replacement string
survived. This is the sentinel both the refactor pass and the adversarial review flagged
independently, from opposite directions, without either running mutation testing.

`TheApplierThatOnlyStoresTheValueClaimsNoSettingOfItsOwn` closes it, and pins **two** accidents rather
than one, because the design is safe for two separate reasons that are invisible when reading it: the
key is empty, so no setting can be named after it; and the type is registered only as itself, never as
`IOptionalFeatureApplier`, so it never joins the lookup the registry builds from the claiming
appliers. Either mistake turns the fallback into a claimant that stores the value and carries none of
the consequences the real applier was written for.

Verified by making Stryker's own mutation by hand — `Key => "Stryker was here!"` — and watching the
test go red with its intended message, then reverting.

### Survivors judged equivalent, with the argument

**`OptionalFeatureSeeder.cs:68`** — `Enabled = false` → `true` on the seeded `FeatureOrdering` row.
**Equivalent, provably.** The initializer's value is never read for that key: the add path always
overwrites it with `ThisInstanceAlreadyOwnedTheFeatureOrder()`, and the update path never touches
`Enabled` at all. There is no input for which the two differ.

**`FeatureRankSeeder.cs:39`** — `unplaced.Contains(feature.Id) && feature.ManualRank == null` → `||`.
Off the changed lines, but chased anyway because it sits in the seeding path this slice's central
promise depends on. **Equivalent.** Widening the predicate only adds entries to a dictionary that the
write loop never reads: the loop iterates `unplaced.Where(features.ContainsKey)`, so nothing outside
`unplaced` can be written. The worst the mutant achieves is one redundant `MaxAsync` and a
`SaveChangesAsync` with no changes.

The remaining backend survivors are all outside the slice's lines. The largest group is log narration
in `OptionalFeatureSeeder.Seed` and `RemoveDeprecatedFeatures` — the repository's established position
is that a mutant which rewrites a log sentence kills nothing, and 17 709 mutants were skipped under
that same policy in this run.

### The slice-01 finding is closed

Slice 01 recorded `OptionalFeaturesController.cs:25` — the `f.Key == featureKey` predicate inside
`GetOptionalFeatureByKey` — as `NoCoverage`, and predicted it would become load-bearing once the write
was routed through the same shape. It is now covered and killed: the controller reports 100 % on
changed lines, and `The_setting_a_caller_names_is_the_setting_it_gets_back` exercises the predicate
against the real repository rather than a Moq'd `GetByPredicate`.

## Frontend

Two runs. The first scored 61.33 % on changed lines — genuinely below the gate, not an artefact of
burial — and the second, after 22 survivors were killed, scored **90.67 %**.

| file | killed | survived | changed-lines score |
| --- | --- | --- | --- |
| `services/TerminologyContext.tsx` | 31 | 0 | 100 % |
| `services/Terminology/resolveTerms.ts` | 4 | 0 | 100 % |
| `services/Api/OptionalFeatureService.ts` | 1 | 0 | 100 % |
| `pages/Settings/System/SystemSettingsTab.tsx` | 7 | 0 | 100 % |
| `pages/Settings/System/BehaviourSettingsTable.tsx` | 18 | 5 | 78.3 % |
| `hooks/useFeatureOrdering.ts` | 8 | 2 | 80.0 % |

### What the gaps turned out to be

**Every entry of `defaultTerminologyMap` could be blanked to `""` and the suite stayed green** — 18
survivors. Those lines are load-bearing precisely because of the review fix that made the map the
thing the UI renders while the terminology fetch is in flight or after it has failed. One test now
pins all 21 entries against literals. Diffing them against `TerminologySeeder.cs` found no drift
today, so the pin's lasting value is that it is the first thing that will notice when the client's
fallback map and the server's seed diverge — the same duplicated-contract shape as Bug #5613.

**`SystemSettingsTab.tsx:31` was covered in neither direction.** The suite's "no licence" helper
served `{ canUsePremiumFeatures: false }` — a non-nullish value — so the `?? false` fallback was never
reached, and separately, nothing anywhere asserted that a premium switch is ever *enabled*. Two tests
were owed, not one.

**`BehaviourSettingsTable.tsx:44`** — `isReachable` mutated from `||` to `&&`. Under it, Faster
Updates renders disabled on every unlicensed instance. Every other case in that file either holds a
licence or looks at a premium row, so non-premium-plus-unlicensed had never been asked.

**`useFeatureOrdering.ts:14`** — `ORDERING_SETTING_KEY` blanked to `""`. The hook would ask the store
for the empty key, get nothing, and fall back to `SourceOrder`, so an instance that owns its order
would quietly stop offering moves. The existing failure-path test asserted only `toHaveBeenCalled()`.

### Survivors judged, with the argument

**`useFeatureOrdering.ts:53`** — `setting?.enabled` → `setting.enabled`. **Equivalent.** With a
non-null row the two are identical. With `null`, the original evaluates `undefined === true` and sets
`SourceOrder`; the mutant throws a `TypeError` that the enclosing `catch` converts into the same
`setPolicy("SourceOrder")`. Same value, same single state write, and `refresh()` resolves either way.
Nothing on the hook's surface differs. Distinguishing them requires observing the throw, which means
changing production code to report something it deliberately swallows.

**`useFeatureOrdering.ts:59`** — the `useCallback` dependency array emptied. **Equivalent**, traced
rather than assumed: `getApiServices()` returns a module-level singleton built once at import, and
`App.tsx:172` is the only production `ApiServiceContext.Provider`. The service reference is stable for
the process lifetime, so both arrays yield an identically stable callback. Only a second provider
swapping the service into a still-mounted tree could tell them apart, and none exists.

**`BehaviourSettingsTable.tsx:55, 64, 76`** — 5 mutants, all `sx` style objects and their contents
(`"flex"`, `"center"`, `{ ml: 1 }`). **Unkillable at this layer rather than strictly equivalent**, and
the distinction is deliberate: dropping `display: flex` is a real visual change in a browser. It
cannot be observed in jsdom without asserting on emotion class names or on the `sx` prop itself, which
tests presentation internals rather than what a user sees. The honest layer is a Playwright
computed-style assertion, and a settings row's flexbox does not warrant one. The two genuinely
user-visible strings on that component — the Preview and Premium tooltip titles — are killed.

## Commands

```
cd Lighthouse.Backend/Lighthouse.Backend.Tests
dotnet stryker -f stryker-config.story-5876-slice-02.json

cd Lighthouse.Frontend
pnpm exec stryker run stryker-config.5876-slice-02.json
```

Configs archived beside this file as `stryker.slice-02.backend.json`,
`stryker.slice-02.frontend.json` and `vitest.slice-02.frontend-runner.config.ts`, because the working
copies are gitignored and nothing is inherited between slices. The archived names deliberately avoid
the `stryker-config*.json` / `vitest.stryker*.ts` globs that ignore the working copies — copy them
back under their working names to re-run:

```
cp docs/feature/story-5876-behaviour-settings/mutation/stryker.slice-02.backend.json \
   Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.story-5876-slice-02.json
cp docs/feature/story-5876-behaviour-settings/mutation/stryker.slice-02.frontend.json \
   Lighthouse.Frontend/stryker-config.5876-slice-02.json
cp docs/feature/story-5876-behaviour-settings/mutation/vitest.slice-02.frontend-runner.config.ts \
   Lighthouse.Frontend/vitest.stryker.5876-slice-02.config.ts
```

**The frontend runner's include list is load-bearing.** StrykerJS runs the whole include set per
mutant, so the list is deliberately narrow — but a spec left out of it makes every mutant in the code
it covers survive for want of a test *run* rather than for want of a test, and the report cannot be
told apart from a real gap. Both new spec files added during this slice are in it. The list was
validated by running it directly (`pnpm vitest run --config vitest.stryker.5876-slice-02.config.ts`,
9 files / 84 tests green) before the first mutation run.

**The backend test filter was validated the same way** — `dotnet test --list-tests` with it resolves
to 223 tests, and all 223 pass standalone, so a survivor means a real gap rather than a broken
baseline.
