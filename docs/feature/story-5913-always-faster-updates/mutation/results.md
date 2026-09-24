# Mutation results — story-5913-always-faster-updates

Run 2026-09-24 11:36, Stryker.NET 5.0.0, config `stryker.5913.backend.json` (copied to
`Lighthouse.Backend/Lighthouse.Backend/stryker-config.5913.json` to run). The test filter is unit-only
(`SyncModeResolverTest`, `OptionalFeatureSeederTests`, `WorkItemServiceTest`, with API.Integration
excluded), which found 82 tests. Scope confirmed from `mutation-report.json`, not from the created
count: 45 mutants tested, and 19,349 were skipped by the `mutate` filter. Elapsed: 3 min 20 s.

**Frontend: N/A.** The story changes no frontend production code. The one frontend file it touched
is a test mock row that was renamed.

## Per file

| File | Tested | Killed | Score | On lines this story changed |
|------|--------|--------|-------|-----------------------------|
| `Services/Implementation/WorkItems/SyncModeResolver.cs` | 11 | 11 | **100 %** | The whole resolver: the opt-in branch was removed, and the five remaining branches are all killed |
| `Services/Implementation/Seeding/OptionalFeatureSeeder.cs` | 34 | 17 | 50 % whole-file | **0 survivors.** Changed lines are L36 (`DeltaSyncKey` added to the retired keys), the deleted seed entry, and one comment |

Headline 62.22 % (28/45). **The gate verdict is the changed-line figure: 100 % (no survivor on any line this
story changed).** Stryker.NET ignores line spans, which is why the files were mutated whole and triaged by line.

## The 17 survivors in `OptionalFeatureSeeder.cs`, all pre-existing

| Lines | Mutation | Why it survives | Class |
|-------|----------|-----------------|-------|
| 15, 25, 46, 105, 115 | log statement removed or message emptied (10 mutants) | No unit test asserts seeder log output | Log wording, equivalent for behaviour |
| 43 | `toRemove.Count > 0` → `>= 0` | `RemoveRange` of an empty list is a no-op; the only difference is one log line | Equivalent |
| 60 | FeatureOrdering `Enabled = false` → `true` | Overwritten on add by `ThisInstanceAlreadyOwnedTheFeatureOrder()`, and never touched on update | Equivalent |
| 75–78 | UsageData description fragments emptied | Asserted by the usage-data veto acceptance scenarios (`UsageDataVeto/*`), which this unit-only run excludes | Covered at acceptance layer |
| 79 | UsageData `Enabled = false` → `true` (seeding the veto engaged) | Asserted by `UsageDataVeto/Slice03VetoSetting*` ("ships disengaged"), excluded here | Covered at acceptance layer |
| 81 | UsageData `IsPremium = true` → `false` | Asserted by `BehaviourSettings/Slice01PremiumRefusal*`, excluded here | Covered at acceptance layer |

None of these were introduced or touched by story 5913. The line-79 and line-81 survivors show that the
seeder's unit tests do not pin the veto's shipped defaults; only the acceptance layer does. That is worth
a unit test when the seeder is next opened for its own sake. It is not this story's gap.

## `WorkItemService.cs` — not mutated, deliberately

The story's changes there are deletions: the opt-in read, its repository dependency, and the conditional
around each of the three `Scan…Identities` calls. The lines that remain are three unconditional `await`
calls with no operator, literal or branch for Stryker.NET to mutate. Mutating the 1,500-line file whole
would score the pre-existing refresh suite, not this change. The behaviour is instead pinned at the port
by Story5913 scenarios 4–6: they fail, as recorded in `red-classification.md`, the moment any of the
three scans stops running.
