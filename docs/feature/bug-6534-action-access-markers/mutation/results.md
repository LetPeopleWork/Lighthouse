# Mutation testing: 6534 (every controller action declares its access)

Run 2026-11-04 on the frozen code, before the rebase onto `origin/main`. Gate is 80 % kill rate on each touched stack.

| stack | scope | score | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | lines this Bug changed, after the kill round | **100 %** | 31 | 0 | 0 | 3 m 41 s + 2 m 31 s |
| Backend (Stryker.NET) | lines this Bug changed, first run | 83.87 % | 26 | 2 | 3 | 3 m 41 s |
| Backend (Stryker.NET) | whole files, first run | 68.25 % | 215 | 42 | 58 | 3 m 41 s |
| Frontend | N/A, because the only frontend changes are the deletion of an unused component and one comment | | | | | |

Configs: `stryker.6534.backend.json` (first run), `stryker.6534.backend.killround.json` (kill round, narrowed to the two
files that still had survivors on changed lines).

## Why the whole-file score is below 80 %

Stryker.NET mutates whole files, and the filter leaves out the HTTP acceptance suites, because each of those boots the
app and costs seconds per mutant. Nearly all of the whole-file survivors sit in older code this Bug did not touch, and
that code is covered by those suites: archive and unarchive, publishing to a source and rule-based selection in
`DeliveriesController.cs`, and the work item and list paths in `FeaturesController.cs`. The score that speaks for this
change is the one on the changed lines.

## Backend, per file

| file | whole file, first run | whole file, kill round | changed lines, final |
| --- | --- | --- | --- |
| `API/ApiKeyController.cs` | 54.9 % | 88.2 % | 6 of 6 |
| `API/FeaturesController.cs` | 60.6 % | 65.2 % | 12 of 12 |
| `API/DeliveriesController.cs` | 70.6 % | not re-run | 2 of 2 |
| `API/LicenseController.cs` | 94.1 % | not re-run | 3 of 3 |
| `API/DTO/LicenseStatusDto.cs` | 100 % | not re-run | 1 of 1 |
| `API/Helpers/RbacAdministrationServiceExtensions.cs` | 100 % | not re-run | 7 of 7 |

### Closed by the kill round

- A restricted key asking for no scope, or an empty one, is refused and creates nothing. An unrestricted caller gets
  its key, and a restricted key naming a scope it holds gets one too.
- Each role and scope type asks the service for the right requirement and scope id; pairs that map to nothing answer 400.
- A scope the caller cannot grant is refused, with the exact refusal text.
- A move with no target places the Feature and never looks a target up.

### Accepted survivors

- `ApiKeyController.cs` L50, `> 0` to `>= 0`: equivalent. With no entries the loop body never runs.
- `ApiKeyController.cs` L35, L73, L149, L150: older code, a message text and the claim fallbacks.
- `FeaturesController.cs`, older code on the `MoveFeature` path with no unit coverage: L140 (the 400 for a malformed
  move), L146 (the 404 for an unknown Feature), L158 (the 403 while the tracker owns the order), and L192, L200 and L213
  (the move parser). The acceptance scenarios cover these.

### Compile errors, so never tested

- `ApiKeyController.cs` L45, one logical mutant of the `is null or { Count: 0 }` pattern.
- `RbacAdministrationServiceExtensions.cs` L39, two logical mutants of the exception filter. Each exception type has its
  own test case.

### Not mutated

- `RbacAdministrationService.cs` (`IsRestrictedApiKeyAsync`, 10 lines of a file of over 1,300): mutating it would bury
  the change. Three direct unit tests and the HTTP tests in `S18_ApiKeyCreationScopeTests` cover it.
- `FeatureRepository.GetById`: one line, behaviour unchanged, covered by the existing tests.
- The guards and markers are attributes, which Stryker does not mutate. The HTTP tests (`S2`, `S16`, `S17`, `S18`) and
  `EveryActionDeclaresItsAccessArchitectureTest` prove them, and each guard's test failed before the guard existed.
