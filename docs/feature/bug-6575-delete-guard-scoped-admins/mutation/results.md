# Mutation testing — 6575 (only a System Admin deletes a Team or Portfolio)

Run 2026-11-07 against `4c1fe10eb`. Gate is 80 % kill rate on each stack touched. Frontend: N/A, because no frontend file changed.

| stack | scope | score | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0) | Changed lines | **3 of 3 covered, 2 by this run and 1 by hand** | 2 | 0 | 1 | |
| Backend (Stryker.NET 5.0.0) | Whole file | 68.15 % | 336 | 49 | 108 | 5 m 35 s |

Config: `stryker.6575.backend.json`.

## Backend

`RbacAdministrationService.cs` is the only file with mutable changes: the controllers changed an attribute argument and the
enum gained a value, and neither produces mutants. Stryker.NET cannot scope to lines, so the whole-file figure mostly scores
1,400 older lines with their integration suites left out of the filter. It says nothing about this fix.

| Line | Code | Mutation | Result |
| --- | --- | --- | --- |
| 376 | `if (!await IsRbacEnforcedAsync(...))` | remove the `!` | Killed |
| 378 | `return true;` | `false` | No coverage in this filter, killed by hand |
| 381 | `... && await CanManageRbacAsync(...)` | `\|\|` | Killed |

The switch arm at line 362 produced no mutant.

### Accepted survivors

Line 378 runs only with RBAC off. `DeleteUnitRightsTests` runs with RBAC on, and the suites that cover this path boot a host
per test, so they stay outside the filter. Removing that branch by hand failed 9 tests in `TeamDeletionIntegrationTest` and the
two `*DeleteSerialisationTests` classes.

### Other mutations applied by hand

| Mutation | Result |
| --- | --- |
| Both delete guards swapped to `SystemAdmin` | The two Premium-lapsed tests fail, so they separate the two requirements |
| Explicit grant removed from BE_1_4 | Fails |
| S5 key widened to Portfolio Admin | Fails |

### Not mutated

The 49 survivors and 108 uncovered mutants elsewhere in the file are outside this change and were not triaged.
