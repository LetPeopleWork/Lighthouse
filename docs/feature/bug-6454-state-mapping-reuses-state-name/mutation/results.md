# Mutation testing — Bug 6454 (a state mapping may reuse a state name)

Run 2026-10-31 against the worktree branch `worktree-vast-gathering-avalanche` @ `ebc52fea3` (production code frozen; only tests changed in the kill round). Gate is 80 % kill rate on both stacks.

| stack | score (first run) | score (after kill round) | tested | killed | survived | no coverage | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **87.50 %** | **100.00 %** | 24 | 21 → 24 | 2 → 0 | 1 → 0 | 0 | 2 m 59 s, re-run 2 m 58 s |
| Frontend (StrykerJS, vitest runner, perTest + incremental) | **87.45 %** | **97.25 %** | 255 | 223 → 248 | 29 → 5 | 3 → 2 | 0 | 13 m 28 s mutation phase (about 14 m with the dry run), incremental re-run 2 m 20 s (2 m 44 s total) |

Configs: `stryker.6454.backend.json`, `stryker.6454.frontend.json`, `vitest.stryker.6454.mutation.ts` (copy it to `Lighthouse.Frontend/` before running; StrykerJS resolves the vitest config from there). Both stacks were run one at a time at concurrency 2.

Backend scope was confirmed from `mutation-report.json`: only `StateMappingValidator.cs` carries mutants (24 tested, 6 compile errors, 7 removed by Stryker's block-already-covered filter). Frontend scope: 5 files, 255 mutants.

## Backend

Test filter: `StateMappingValidatorTest` and `WorkTrackingSystemOptionsOwnerTest`, without `API.Integration`, `Integration.Containers` and the live-connector categories. The controllers that call the validator are covered only by acceptance suites, which are excluded on purpose.

| file | tested | first run | after kill round |
| --- | --- | --- | --- |
| `API/Helpers/StateMappingValidator.cs` | 24 | 21 killed, 2 survived, 1 no coverage (87.50 %) | 24 killed (100.00 %) |

### Closed by this pass

- `StateMappingValidatorTest.Validate_SourceStateInMultipleMappings_NamesTheStateAndEachMappingOnce` — the clash message is pinned exactly (`Source state 'Resolved' appears in multiple mappings: 'Group A', 'Group B'.`), with a mapping that lists the state twice so the mapping is named once. Kills the two string mutants on the separator and the per-mapping label.
- `StateMappingValidatorTest.ValidateSettings_SourceStateInMultipleMappings_ReportsTheClashOfTheSettingsMappings` — `ValidateSettings` is driven with a `TeamSettingDto` and must report the clash of the mappings it carries. Kills the object-initializer mutant that turned every mapping into a blank one (it had no unit coverage at all).

### Accepted survivors

None.

## Frontend

Test files: `stateMappingReconciliation.test.ts`, `stateMappingValidation.test.ts`, `StateMappingsEditor.test.tsx`, `ModifyTeamSettings.test.tsx`, `ModifyProjectSettings.test.tsx` (no other spec imports the two utils).

| file (mutated lines) | first run | after kill round |
| --- | --- | --- |
| `utils/stateMappingReconciliation.ts` (whole file) | 87.77 % (16 survived, 1 no cov) | 97.84 % (3 survived) |
| `utils/stateMappingValidation.ts` (whole file) | 89.58 % (10 survived) | 98.96 % (1 survived) |
| `StateMappingsEditor.tsx` 51-61 | 78.57 % (2 survived, 1 no cov) | 85.71 % (1 survived, 1 no cov) |
| `ModifyTeamSettings.tsx` 101, 174-177 | 60.00 % (1 survived, 1 no cov) | 80.00 % (1 no cov) |
| `ModifyProjectSettings.tsx` 104, 194-197 | 100.00 % | 100.00 % |

Every first-run survivor was hand-checked by applying its mutation to the source and running the covering specs, so none of them is a false survivor. The same harness confirmed each closed survivor is now killed, and the incremental Stryker re-run agrees.

### Closed by this pass

`stateMappingReconciliation.test.ts`:
- `normalise > trims and lowercases` — `"  In Progress "` gives `"in progress"` (kills the `toUpperCase` swap).
- `wouldShadow for a name of only spaces > is false even when Doing lists an empty state` — pins the blank-name guard.
- `sharesStateWithAnother > is true when only one of the mapping's states is grouped by another mapping` and `is false when no other mapping groups one of its states` — `some` versus `every`.
- `isApplied > is false while one of its states is still listed in Doing` and `is true when its own name is one of its states` — `every` versus `some`, and the own-name exemption.
- `editing a mapping without changing how many states it has > takes the replaced state back and groups the new one` — an edit of equal length, which the length check alone cannot detect (kills the three comparison mutants in `statesChanged`).
- `... leaves Doing as it is when only the order of the states changed` and `... the other way` — both sort calls in `statesChanged`.
- `... leaves a state where it is while the mapping stays the same and its name was taken out of Doing by hand` — an unchanged mapping is not re-applied (the `!prev || statesChanged || sharesState` gate).

`stateMappingValidation.test.ts`:
- `treats a name of only spaces as empty and labels the mapping by its position`, `labels a mapping without states by its name without surrounding spaces`, `reports each name of only spaces as empty without calling them duplicates`, `names a duplicate mapping without surrounding spaces`, `names the mapping and the Doing state without surrounding spaces`, `names both mappings without surrounding spaces when a name is a state another mapping groups` — each `trim()` in the messages and the blank-name `continue`, with exact arrays.
- `reports no clash with a To Do state for a mapping whose name is only spaces` — the blank-name filter in `stateNameClashErrors`.

`StateMappingsEditor.test.tsx`: `does not offer a state the mapping already groups` — the options for a mapping leave out the states it already holds (kills `?? []` turned into `&& []`).

`ModifyTeamSettings.test.tsx`: `tells the user which source state is used in more than one mapping` — the form passes the mappings' validation errors to the editor (kills `stateMappings || []` turned into `&& []`).

### Accepted survivors

- `stateMappingReconciliation.ts:137` (three mutants: the `!result.some(...)` condition forced true, its arrow function, and its inner comparison forced false) — equivalent. By the time the guard is reached, the earlier `wouldShadow` check has already skipped every mapping whose name is a Doing state it does not contain, and a mapping that does contain it has had that state filtered out, so the name is never in `result` and the guard is always true. It is a defensive duplicate guard that no input reaches.
- `stateMappingValidation.ts:67` (`other !== mapping` forced true in `groupsAnotherMappingsName`) — equivalent. The helper is only consulted after `wouldShadow` is true, which means the mapping does not list its own name, so comparing it with itself can never match.
- `StateMappingsEditor.tsx:57` (`?.` removed, and `?? []` replaced by a non-empty array) — equivalent. The function is only called with the index of a rendered mapping, so the entry and its `states` always exist; the fallback is unreachable.
- `ModifyTeamSettings.tsx:175` (`|| []` replaced by a non-empty array, no coverage) — the fallback for a settings object without `stateMappings`, which the `ITeamSettings` type forbids. Reaching it would need a test that violates the type.

### Not mutated

The import lines of the three components and `StateMappingsEditor.tsx` line 17: they carry no behaviour.
