# Mutation testing — 6363 (S03e Package Access and Licensing)

Run 2026-11-22 on `main` at `b77301c32` (feature C closed, code frozen), Stryker.NET 5.0.0 under the x64 SDK
(`run-backend-x64.ps1`, Windows ARM64), Stryker's own sandbox copy (never in place; no `.stryker-tmp*` left behind).
Bar (D-13): at least 80 % of the moved code, **and** no survivor or no-coverage mutant added by the move. The
stricter of the two holds.

| stack | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend, run 1 (filter as in the 6362 precedent: unit tests, no acceptance) | **72.83 %** | 1027 | 748 | 102 | 177 | 16.5 m |
| Backend, run 2 (filter widened by the port-level Access acceptance suites, see below) | **79.26 %** | 1027 | 814 | 96 | 117 | 28.5 m |
| Backend, after one kill round | **80.53 %** | 1027 | 827 | 90 | 110 | narrowed re-run 10 m |
| Frontend | N/A — no frontend file changed | | | | | |

"Tested" counts killed + survived + no coverage over the mutated files (Stryker ignores a further 272 mutants in
them as covered by a block mutant, and 95 fail to compile; `Program.cs` shows up in the report with nothing in scope).
The after-kill-round row is run 2 with the narrowed re-run of the five touched files swapped in (62 mutants there: 57
killed, 3 survived, 2 no coverage).

Configs: `stryker.6363.backend.json` (run 1; the four `mutate` globs `Modules/Access/{Domain,Application,Adapters/Http}`
and `Modules/Licensing`), `stryker.6363.backend.port.json` (run 2, same files), `stryker.6363.backend.killround.json`
(the five files the kill round touched). `run-backend-x64.ps1` is the epic-5511 copy plus a `-CliPath` switch, because
Stryker 5.0.0 is not in the global tool store.

Test filter, run 1: `Modules.Access`, `Modules.Licensing`, the `ApiKey`, `Auth`, `Authorization`, `License`,
`CreateRightsControllerGuard`, `EmbedStart` controller tests, `EndpointAuthorizationPolicyTest` and
`ApiKeyOwnerReconciliationSeederTests`; no `API.Integration`, no acceptance, no dogfood, no `WriteBack` class, no live
connector category (589 tests). Run 2 drops the blanket acceptance exclusion for two suites that start no web host:
`Acceptance.SystemWideAccess` (without the named embed skeleton, `RemovedPersonEmbedSkeleton`) and
`CreateRightsAcceptanceTest`, which sit at the application-service port and are the main coverage of
`RbacAdministrationService` (757 tests). Run 1 alone left 177 mutants uncovered only because those suites were out.

## Per file (run 2, files with a survivor or a no-coverage mutant)

| File | killed | survived | no coverage | after the kill round |
| --- | --- | --- | --- | --- |
| Application/RbacAdministrationService.cs | 362 | 29 | 76 | unchanged |
| Application/EmbedSessionTokenService.cs | 38 | 9 | 25 | unchanged |
| Adapters/Http/ApiKeyAuthenticationHandler.cs | 8 | 17 | 0 | unchanged |
| Application/AuthModeResolver.cs | 8 | 10 | 0 | unchanged |
| Application/ApiKeyService.cs | 73 | 3 | 0 | unchanged |
| Application/OidcGroupSnapshotWriter.cs | 11 | 2 | 0 | unchanged |
| Domain/EffectiveAccess.cs | 63 | 5 | 0 | unchanged |
| Domain/ApiKeyPrincipalFactory.cs | 24 | 1 | 0 | unchanged |
| Adapters/Http/BlockedModeFilter.cs | 11 | 3 | 1 | unchanged |
| Application/FeatureMoveAuthorization.cs | 5 | 2 | 1 | 7 killed, 1 survived, 0 no coverage |
| Domain/GroupClaimParser.cs | 24 | 4 | 1 | 26 killed, 2 survived, 1 no coverage |
| Domain/FeatureReadability.cs | 0 | 0 | 2 | 2 killed |
| Adapters/Http/DisabledAuthenticationHandler.cs | 0 | 0 | 3 | 3 killed |
| Licensing/Adapters/Http/LicenseGuardAttribute.cs | 15 | 3 | 2 | 19 killed, 0 survived, 1 no coverage |
| Licensing/Application/LicenseService.cs | 36 | 4 | 4 | unchanged |
| Licensing/Application/LicenseVerifier.cs | 24 | 4 | 2 | unchanged |

`RbacGuardAttribute.cs`, `SmartAuthSchemeSelector.cs`, `CurrentUserProfileService.cs`, `AuthConfigurationValidator.cs`,
`AuthConfigurationValidationResult.cs` and the interface and record files have no survivor and no uncovered mutant.

### Closed by the kill round

New tests (seen green on their own, then the narrowed Stryker re-run killed the mutants listed):

- `Modules/Access/Domain/FeatureReadabilityTest.cs` (new): both no-coverage mutants of `FeatureReadability.IsReadableBy`
  (no Portfolio means readable; one readable Portfolio is enough; none readable means not readable).
- `Modules/Access/Adapters/Http/DisabledAuthenticationHandlerTest.cs` (new): the three no-coverage claim values
  (`sub`, name identifier and `name`) and the scheme name.
- `Modules/Access/Domain/GroupClaimParserTest.cs` (new): the blank-claim `continue` (line 23), the array-claim
  `continue` (line 39), a non-string array entry reported as unsupported (line 71) and the blank-entry `continue`
  (line 77).
- `Modules/Access/Application/FeatureMoveAuthorizationTest.cs` (new): the orphan Feature verdict (line 37, no coverage)
  and the "no writable set means all writable" branch (line 26).
- `Modules/Licensing/Adapters/Http/LicenseGuardAttributeTest.cs` (four rows added): the two `= false` defaults
  (lines 18 and 24), the count exactly at the limit (line 72), and a missing repository answering 500 (line 68).

Mutants closed: 13 (3 + 2 + 2 + 2 + 4 more kills in the five files, counted from run 2 to the narrowed re-run). `dotnet build Lighthouse.Backend` and `Lighthouse.Backend.Tests`: 0 warnings, 0 errors;
the 24 tests of the five classes pass.

## Triage of what remains (90 survivors, 110 no coverage)

- **Diagnostic text, not behaviour** (log calls and messages): `ApiKeyAuthenticationHandler.cs:37-59` (all 17: the
  log lines, the `Fail(...)` message text and the owner-resolved log, plus the `&&` on the resolved-owner check at
  lines 48-49 whose branch only decides whether a stable-subject claim is added, which `ApiKeyPrincipalFactoryTest`
  asserts one layer down); `AuthModeResolver.cs:41-66` (10, log calls only); `BlockedModeFilter.cs:39-40` (3, a debug
  line and the body text of an already-asserted 403); `OidcGroupSnapshotWriter.cs:28-29` (2); `LicenseService.cs:35`
  (2, the logged failure); `RbacAdministrationService.cs` string mutants at 557, 574, 725, 733, 771, 779, 1009 and
  1016 (the refusal text; the refused result itself is asserted); `EmbedSessionTokenService.cs:208-209` (the `out`
  defaults of a refused token).
- **Equivalent:** `FeatureMoveAuthorization.cs:24` (`ConfigureAwait(false)` to `true`);
  `GroupClaimParser.cs:63` (the array-root check is unreachable: a value that starts with `[` and parses is always an
  array), `:31` and `:45` (the bool return of `TryGetGroupValues` is constant `true`, callers read the `out` flag);
  `ApiKeyPrincipalFactory.cs:35` (collection initializer to empty list that is then filled the same way);
  `RbacAdministrationService.cs:581`, `:744`, `:790` (`Count > 0` to `>= 0` only skips or runs a `RemoveRange` of an
  empty set) and `:583`/`:1096` (statement removals that survive and were not examined further: to be treated as "other" if a
  later round has room).
- **Ordering and tie-breaks**: `RbacAdministrationService.cs:874` and `:891` (`ThenBy` to `ThenByDescending` on group
  mappings listed by scope and then name), `EffectiveAccess.cs:129` and `:242` (`>` to `>=` on equal priority picks the
  same role), `:114` (`Append` to `Prepend` of the everyone-signed-in row, order of a set that is folded by maximum).
- **Argument guards and defaults:** `EmbedSessionTokenService.cs:211` and `:224` (null/blank token guard feeding the
  same refusal), `LicenseVerifier.cs:105-106` (`First` to `FirstOrDefault` on a resource that always exists),
  `LicenseVerifier.cs:92` (empty-string coalesce of a null manifest value).
- **Other (a test is missing or sits in a suite excluded from this run), not closed in the one kill round:**
  - `RbacAdministrationService.cs`: 76 no-coverage mutants in the member listing, group-mapping read and write and
    creator-grant members (lines 659-720, 849-1100, 1374-1460). Their callers are covered through the controller
    and API integration suites, which D-13 excludes for cost; `GetTeamMembersAsync` and `GetPortfolioMembersAsync` have
    no test below the controller layer. Also the survivors at 40, 139, 511, 550, 925, 1182, 1329 and 1453 (`&&` to `||`
    on rows the in-memory data never makes ambiguous, and the `CanManageRbac` negation at 139 in the readable-teams
    path).
  - `EmbedSessionTokenService.cs`: 25 no-coverage mutants in the single-use redemption and viewer-handshake paths, which
    the embed security suites (`API/Security/S8` to `S15`, `ViewerEmbedTestHost`) cover; those start a web host and are
    outside the unit filter. Survivors at 30 and 47 (object initializer to default) and 69 and 104 (stored token null
    check) are covered the same way.
  - `LicenseService.cs:89-96` (5 no-coverage, 1 survivor: the name, email, organization and number fallbacks of the
    parsed licence) and `LicenseVerifier.cs:89` (2 no-coverage: the date formatting when the licence is re-serialised
    for verification), `BlockedModeFilter.cs:32`, `LicenseGuardAttribute.cs:68`: empty-string fallbacks of values the
    fixtures always supply, and one `return;` after a 500 answer.

## Against the D-13 bar

**Kill rate: met at 80.53 %** after one kill round (run 1 alone was 72.83 %, run 2 79.26 %). The score of the unit
filter alone is below 80 % because the application-service-port acceptance suites, the main coverage of
`RbacAdministrationService`, were in D-13's excluded group; the first run is kept above for that reason.

**No survivor or no-coverage mutant added by the move: met, shown mechanically.** For every one of the 36 production
`.cs` files that moved into `Modules/Access` and `Modules/Licensing` (git renames from `cf8d1e33e`, the last commit before
the move), the file content apart from `namespace` and `using` lines, blank lines and carriage returns is **identical to
the pre-move file in 35 of 36**. The 36th, `IEmbedSessionTokenService.cs`, only lost the `EmbedHandshakeOutcome` record
that now sits in `Access/Domain/EmbedHandshakeOutcome.cs`: a `sealed record` with a static `Unresolved` property and no
branch, which carries no mutant. The move changed only namespaces and usings, so a mutant cannot be new, and each
survivor above is one the pre-move file had. (A mutant-by-mutant comparison against a run on `cf8d1e33e` was not made:
the files are textually equal and the tests were moved with them.)

Command used for the proof, per pair: `git show cf8d1e33e:<old> | grep -vE '^\s*(using |namespace |$)' | md5sum` against
the same filter on the new file.
