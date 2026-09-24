# Mutation results — optional-feature-toggled-usage-event

Both stacks were run sequentially on 2026-09-24, on code frozen after the refactor commit (`9e4caea0c`).

## Backend — Stryker.NET 5.0.0, `stryker.toggle-event.backend.json`

The mutated files are `UsageDataEventShapes.cs`, `UsageDataController.cs` and `PostHogUsageDataPublisher.cs`,
each whole-file. The test filter selects this feature's acceptance scenarios, slice 04's event scenarios,
the controller and publisher unit tests, and the emit-seam ArchUnit rules: 94 tests. The acceptance
scenarios are included deliberately, because they are the only tests that drive `UsageDataEventShapes.Fits`.
60 mutants were tested. Elapsed: 4 min 38 s.

A first run before the refactor scored 58/66 and is superseded. The refactor rewrote `Fits` and
`AsTakenIn`, so that run's figures described code that no longer exists.

| File | Tested | Killed | No coverage | On lines this feature changed |
|------|--------|--------|-------------|-------------------------------|
| `Models/UsageData/UsageDataEventShapes.cs` | 9 | 9 | 0 | **100 %** |
| `API/UsageDataController.cs` | 13 | 13 | 6 | **100 %**. The 6 uncovered mutants are the consent-token lines (L52, L116, L118), which this feature does not touch |
| `Services/Implementation/UsageData/PostHogUsageDataPublisher.cs` | 38 | 30 | 2 | **100 %**. All 8 survivors are outside the changed ranges (L203–204, L233–240, L252–257) |

Headline 76.47 %. **The verdict is the changed-line figure: 100 %.** Stryker.NET ignores line spans,
which is why the files were mutated whole and triaged by line.

**The publisher's pre-existing survivors** are L54 (log text), L133 (the address-scheme check), L193
(the built-in API-key fallback), L210 and L275. All are in the collector-address and key-selection
code, which shipped with Epic #5733 slice 01c. They are left for the next time that code is opened.

**A known small gap.** `AsTakenIn` refuses an `optionalFeature` whose value names no member
(`Enum.IsDefined`), because the registered JSON enum converter also accepts integers. No scenario posts
an integer, and Stryker.NET generated no mutant that would expose the guard's absence. It is covered by
reading, not by a test.

## Frontend — StrykerJS 9.6.1, `stryker.toggle-event.frontend.json` + `vitest.stryker.toggle.config.ts`

In place, with `disableTypeChecks: false`, and one span per entry.

| File (span) | Mutants | Killed | Score |
|-------------|---------|--------|-------|
| `pages/Settings/System/SystemSettingsTab.tsx:65-82` (the report after an accepted write) | 6 | 6 | **100 %** |
| `services/UsageData/usageDataOptionalFeatures.ts` (whole) | 4 | 4 | **100 %** |
| `models/UsageData/UsageData.ts:94-109`, `services/Api/UsageDataService.ts:30` | 0 | — | constant declarations: no mutants generated |

Total: 10/10, **100 %**. Elapsed: 27 s. The working tree was restored clean afterwards.

The two constant spans produced no mutants, so they are absent from the per-file table. That absence
is expected here, not the silent-exclusion trap. Their values are still pinned: the backend scenarios
refuse anything but `FeatureOrder`, and the hand-in spec asserts `"FeatureOrder"` and
`"OptionalFeatureToggled"` all the way into `postEvents`.
