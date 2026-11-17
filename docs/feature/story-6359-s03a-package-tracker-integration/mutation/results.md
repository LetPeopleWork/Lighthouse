# Mutation testing — 6359 (S03a Package Tracker integration)

Run 2026-11-17 against `main` @ `b08c6f99c` (code as of `303c88bf7`). Bar (D-13, approved at DESIGN): no survivor or
no-coverage mutant **added** in the moved code, on frozen code, acceptance suites excluded. The project gate of 80 %
is reported alongside.

| stack | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **65.30 %** | 308 | 238 | 69 | 58 | 29 m (20 m coverage capture) |
| Frontend | N/A — no frontend file changed | | | | | |

Config: `stryker.6359.backend.json`. Scope (maintainer default, 2026-11-17 13:19): the 50 moved files under
`Modules/TrackerIntegration/Domain` and `Modules/TrackerIntegration/Application` (the translation rules, the connector
port and factories, the auth schema, OAuth and connection health). Test filter: the module's and Work Items' unit tests,
no live connector categories, no `API.Integration`, no dogfood suites.

## Per file (files with a survivor or a no-coverage mutant)

| File | killed | survived | no coverage |
| --- | --- | --- | --- |
| Application/AuthenticationMethodSchema.cs | 34 | 20 | 1 |
| Application/WorkTrackingSystemFactory.cs | 83 | 14 | 1 |
| Application/OAuth/OAuthService.cs | 47 | 11 | 7 |
| Application/WorkTrackingConnectorFactory.cs | 1 | 6 | 0 |
| Application/WorkTrackingRefusedException.cs | 5 | 6 | 1 |
| Application/OAuth/OAuthStateTokenIssuer.cs | 11 | 4 | 1 |
| Application/OAuth/OAuthProviderRegistry.cs | 10 | 3 | 0 |
| Application/OAuth/OAuthStateClaims.cs | 0 | 2 | 0 |
| Application/OAuth/OAuthProviderResponseException.cs | 1 | 1 | 0 |
| Application/WorkItemCategoryCrossing.cs | 14 | 1 | 0 |
| Application/BoardInformation.cs | 0 | 1 | 0 |
| Application/AuthenticationMethodKeys.cs | 0 | 0 | 1 |
| Application/ConnectionHealth/ConnectionHealthService.cs | 0 | 0 | 42 |
| Application/ConnectionHealth/ConnectionHealthCadence.cs | 0 | 0 | 4 |

The three `Domain` files (`ParentSourceSelector`, `DependencyFieldReferences`, `WorkItemStateTransitionMapper`) have
no survivor and no uncovered mutant.

Survivors by mutator: String 32, Boolean 14, Statement 12, Equality 5, Logical 2, Linq 1, Conditional 1, Block 1,
Arithmetic 1.

## Against the D-13 bar: met

Feature B moved these files with only their `namespace` and `using` lines changed (proved line by line by both
whole-candidate reviews), and feature C changed no production file. The tests that cover them moved unchanged. So every
mutant and its outcome is the same as before the move: none was added. No baseline run on the pre-move revision was
made; the argument is the byte-identity of the code and its tests.

## Against the 80 % project gate: not met, recorded as a follow-up

- **No coverage (58):** 46 are `ConnectionHealthService` and `ConnectionHealthCadence`, whose only test is the
  acceptance spec `API/Integration/TaskManager/Slice08HealthThatChecksItselfSpecifications`, which the filter leaves
  out by the skill's rule. A unit test at the service port would cover them.
- **Survivors (69):** 32 are string mutations (exception messages, schema labels and keys); the rest sit mainly in
  `AuthenticationMethodSchema`, `WorkTrackingSystemFactory` and `OAuthService`.
- Writing these tests is outside a behaviour-free move (the test-writing skill's "improve the tests you touch" is waived
  for move-only edits, D-9). Proposed as an improvement item; not filed.

## Not mutated

The connectors (`Adapters/Connectors`, about 13,000 lines), `Adapters/OAuth` and `Adapters/Background`: moved
byte-for-byte apart from namespaces, so their mutation results cannot have changed (maintainer default, option 1).
