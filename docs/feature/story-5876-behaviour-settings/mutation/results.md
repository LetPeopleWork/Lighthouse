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
