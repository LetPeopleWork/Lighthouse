# Mutation testing — Bug 6089 (a write-back mapping for a new additional field saves in one step)

Run 2026-09-28 against `main` @ `47747a30e`. Gate is 80 % kill rate on the changed code.

| stack | scope | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | new/changed code (helper + validator) | **93.88 %** | 49 | 46 | 3 | 0 | — |
| Backend (Stryker.NET) | whole mutated files, incl. both controllers | 72.67 % | 149 | 125 | 24 | 23 | 2 m 40 s |
| Frontend | N/A — the fix changed no frontend file | — | — | — | — | — | — |

Config: `stryker.6089.backend.json` (run from `Lighthouse.Backend.Tests/`). The test filter excludes
`*IntegrationTest` classes; the controller unit tests and the validator tests carry the mutation load.

A first run (at `0bcaaea51`) left three validator survivors: nothing pinned *which* error an id of 0
produces, nor the wording of the duplicate message. Tightening four existing validator assertions
(`47747a30e`) killed all three.

## Backend

Stryker.NET mutates whole files, so the two controllers — of which the fix touched about 15 lines out of
~520 — bring in their untouched code.

| file | killed | survived | no coverage | changed by the fix |
| --- | --- | --- | --- | --- |
| API/Helpers/NewAdditionalFieldTargets.cs | 10 | 3 | 0 | new file |
| API/Helpers/WriteBackMappingValidator.cs | 36 | 0 | 0 | target + duplicate rules |
| API/WorkTrackingSystemConnectionController.cs | 42 | 14 | 18 | field/mapping assignment only |
| API/WorkTrackingSystemConnectionsController.cs | 37 | 7 | 5 | `CreateConnectionFromDto` loops only |

### Closed by this pass

- `WriteBackMappingValidator.cs` `< 0` → `<= 0`: a mapping with field id 0 must report "An additional field
  is required…", not the unknown-field error.
- `WriteBackMappingValidator.cs` `Describe` (both string mutants): the duplicate message names a saved
  field as `id: N` and a new field as `new field: '<reference>'`.

### Accepted survivors (changed code)

All three are equivalent mutants in `NewAdditionalFieldTargets.cs`; an id of exactly 0 never reaches the
branch that differs:

- line 21 `placeholderId < 0` → `<= 0`: registers a field sent with id 0 under key 0, but lookups only
  happen for negative ids (line 38), so the entry is never read.
- line 38 `fieldId is < 0` → `<= 0`: a lookup for 0 misses (nothing is registered under 0 unless line 21
  is also mutated) and falls through to the same raw-id assignment.
- line 44 `AdditionalFieldDefinitionId < 0` → `<= 0`: only reached after a successful placeholder match,
  where the mapping's id is either the negative placeholder or a stored positive id — never 0.

### Not attributable to this fix

Every surviving or uncovered mutant in the two controllers sits in code the fix did not change:
predefined-field auto-registration (`WorkTrackingSystemConnectionController.cs` ~29-72), option merging
and secret handling (~107-174 and `WorkTrackingSystemConnectionsController.cs` ~168-192), connection
validation messages (`WorkTrackingSystemConnectionsController.cs` ~75-141), and the predefined-field guard
inside `UpdateAdditionalFieldDefinitions` (~251-264). No mutant on a line the fix changed survived.
