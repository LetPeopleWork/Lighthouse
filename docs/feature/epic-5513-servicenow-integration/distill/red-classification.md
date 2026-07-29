# RED classification — slice 01 (ADO Story 5574)

Pre-DELIVER fail-for-the-right-reason gate. Every acceptance test authored in this DISTILL run was
executed against the RED scaffolds and classified. `MISSING_FUNCTIONALITY` is the only acceptable
RED; anything else is a test bug and blocks handoff.

Run date: 2026-07-29. Commands and their output are at the bottom.

## How "RED, not BROKEN" was achieved in C#

The nWave scaffold recipe says a scaffold body should raise an assertion error rather than
`NotImplementedError`, because the Python Red-Gate classifier maps the latter to BROKEN. Neither
mechanism transfers literally here:

- The production assembly cannot reference NUnit, so `AssertionException` is not available to a
  scaffold body.
- C# has no import-error failure class. The BROKEN equivalent is "does not compile", and a
  non-compiling suite is a hard fail — `dotnet build` is clean, so that class is empty by
  construction.

The adaptation used instead is **wrong-value scaffolds**: every scaffold returns a sentinel or the
deliberate opposite of the specified behaviour, so the failure surfaces as a genuine NUnit
`AssertionException` at the assertion site in the test. That is strictly stronger than a thrown
exception — the test reaches its assertion, and the diff between expected and actual reads as the
specification.

- `ServiceNowValidationVerdict` returns a `__scaffold__` verdict from all three entry points.
- `ServiceNowWorkTrackingConnector` returns success where failure is specified, `true` where `false`
  is specified, a placeholder field where emptiness is specified, and returns normally where an
  explicit `NotSupportedException` is specified.
- `ServiceNowBasicAuthStrategy.ApplyAsync` leaves the request unauthenticated.
- The frontend schema entries carry `__scaffold__` keys and labels.

Every scaffold file carries a `// SCAFFOLD` comment. `grep -rn "SCAFFOLD (DISTILL slice 01"` finds
all of them; zero should remain at the end of DELIVER.

## Backend

| Test | AC | Verdict |
|---|---|---|
| `ServiceNowValidationVerdictTest.AnInstanceAddressThatIsNotAnAddress_IsRejectedBeforeAnythingIsSent` | AC4 | MISSING_FUNCTIONALITY |
| `…AnInstanceThatCannotBeReached_IsReportedAsAConnectionFailure` | AC4 | MISSING_FUNCTIONALITY |
| `…ACredentialTheInstanceRejects_IsReportedAsAnAuthenticationFailure` | AC4 | MISSING_FUNCTIONALITY |
| `…ARejectedCredential_NamesTheBasicAuthRoleWithoutClaimingToKnowItIsTheCause` | AC4 / ADR-115 | MISSING_FUNCTIONALITY |
| `…AnInstanceThatRefusesTheReadOutright_IsReportedAsInsufficientPermissions` | AC4 | MISSING_FUNCTIONALITY |
| `…ATableTheInstanceDoesNotHave_IsReportedAsAnUnknownTable` | AC4 | MISSING_FUNCTIONALITY |
| `…Hypothesis_ALoginPageWearingASuccessStatus_IsNotMistakenForData` | AC4 (hypothesis rung) | MISSING_FUNCTIONALITY |
| `…AnInstanceThatAnswersSuccessfullyWithNothingVisible_IsNeverReportedAsValid` | AC4 | MISSING_FUNCTIONALITY |
| `…NothingVisible_NamesBothPossibleCausesAndTheRoleToGrant` | AC4 (amended) | MISSING_FUNCTIONALITY |
| `…AnInstanceThatShowsWorkToTheCredential_IsReportedAsValid` | AC3 | MISSING_FUNCTIONALITY |
| `…EveryRungOfTheLadder_ProducesItsOwnVerdict` (7 cases) | AC3 + AC4 | MISSING_FUNCTIONALITY ×7 |
| `…TheThreeFailuresAnAdministratorWillMeet_AreToldApart` | AC4 | MISSING_FUNCTIONALITY |
| `…ARightsProblem_IsNeverDressedUpAsAReachabilityProblem` | AC4 | SCAFFOLD_SATISFIED — see note 1 |
| `ServiceNowWorkTrackingConnectorTest.AnInstanceThatAnswersSuccessfullyWithNothingVisible_IsNotReportedAsAWorkingConnection` | AC4 | MISSING_FUNCTIONALITY |
| `…AnInstanceThatShowsWorkToTheCredential_IsReportedAsAWorkingConnection` | AC3 | MISSING_FUNCTIONALITY |
| `…AnInstanceThatRejectsTheCredential_IsReportedAsAnAuthenticationFailure` | AC4 | MISSING_FUNCTIONALITY |
| `…AnInstanceThatCannotBeReached_IsReportedAsAConnectionFailure` | AC4 | MISSING_FUNCTIONALITY |
| `…AnInstanceAddressThatIsNotAnAddress_IsRejectedWithoutContactingAnything` | AC4 | MISSING_FUNCTIONALITY |
| `…ValidatingAConnection_AsksTheConfiguredTableForASingleRecordAndNothingElse` | AC3 | MISSING_FUNCTIONALITY |
| `…AConnectionWithNoTableChosen_IsProbedAgainstTheIncidentTable` | ADR-116 | MISSING_FUNCTIONALITY |
| `…ValidatingAConnection_LeavesTheCredentialHandlingToTheResolvedAuthenticationStrategy` | AC5 | MISSING_FUNCTIONALITY |
| `…ReadingWorkFromServiceNow_IsDeclaredUnsupportedRatherThanReturningNothing` | DoD 5 / KPI 3 | MISSING_FUNCTIONALITY |
| `…WritingBackToServiceNow_IsDeclaredUnsupported` | D8 | MISSING_FUNCTIONALITY |
| `…PointingATeamAtServiceNow_IsRefusedWithAnActionableReason` | DoD 5 | MISSING_FUNCTIONALITY |
| `…PointingAPortfolioAtServiceNow_IsRefusedWithAnActionableReason` | US-03 AC5 | MISSING_FUNCTIONALITY |
| `…TimeInStateOnServiceNowWork_IsDeclaredUnavailable` | D6 | MISSING_FUNCTIONALITY |
| `…AServiceNowConnection_BringsNoPredefinedAdditionalFields` | DoD 5 | MISSING_FUNCTIONALITY |
| `ServiceNowBasicAuthStrategyTest.AServiceNowCredential_IsPresentedToTheInstanceAsBasicAuthentication` | D3 | MISSING_FUNCTIONALITY |
| `…AServiceNowCredential_CarriesTheUsernameAndTheDecryptedPassword` | D3 | MISSING_FUNCTIONALITY |
| `…TheStoredPassword_ReachesTheInstanceThroughTheCryptoService` | AC5 | MISSING_FUNCTIONALITY |
| `ServiceNowConnectionConfigurationTest.ServiceNow_SitsAtTheEndOfTheStoredWorkTrackingSystemOrder` | enum-ordering guard | SCAFFOLD_SATISFIED — see note 2 |
| `…AShopThatTracksWorkInServiceNow_FindsItAmongTheSystemsTheyCanConnect` | AC1 | SCAFFOLD_SATISFIED — see note 2 |
| `…ANewServiceNowConnection_StartsOutUsingUsernameAndPassword` | AC2 | SCAFFOLD_SATISFIED — see note 2 |
| `…TheServiceNowConnectionForm_AsksForAnInstanceAddressAUsernameAndAPassword` | AC2 | MISSING_FUNCTIONALITY |
| `…TheServiceNowConnectionForm_OffersExactlyOneWayToAuthenticate` | AC2 | MISSING_FUNCTIONALITY |
| `…ANewServiceNowConnection_ComesPreFilledWithTheIncidentTable` | ADR-116 | MISSING_FUNCTIONALITY |
| `…ANewServiceNowConnection_KeepsThePasswordAsASecret` | AC5 | MISSING_FUNCTIONALITY |
| `ServiceNowConnectionAcceptanceTest.AnAdministratorOpeningTheConnectionWizard_CanChooseServiceNow` | AC1 | SCAFFOLD_SATISFIED — see note 2 |
| `…TheServiceNowEntryInTheWizard_CarriesTheFieldsTheFormNeedsToRender` | AC2 | MISSING_FUNCTIONALITY |
| `…AnAdministratorValidatingAConnectionToAnInstanceThatIsNotThere_IsToldTheInstanceIsNotThere` | AC4 (walking skeleton) | MISSING_FUNCTIONALITY |
| `…TheCredentialAnAdministratorEnters_IsNeverHandedBackToTheBrowser` | AC5 | SCAFFOLD_SATISFIED — see note 3 |
| `ServiceNowValidationVerdictPurityArchUnitTest` (3 tests) | ADR-114 | SCAFFOLD_SATISFIED — see note 4 |

## Frontend

| Test | AC | Verdict |
|---|---|---|
| `DataRetrievalSchemaDefaults.serviceNow` — asks for a ServiceNow query in the shop's own words | AC2 | MISSING_FUNCTIONALITY |
| `…` — does not ask for a separate list of work item types | C-3 | MISSING_FUNCTIONALITY |
| `…` — offers no discovery wizard | ADR-116 | MISSING_FUNCTIONALITY |
| `…` — declines rather than offering a field that leads nowhere | US-03 AC5 | MISSING_FUNCTIONALITY |
| `WorkTrackingSystemConnection.serviceNow` — calls it a ServiceNow query, not just a query | AC2 | MISSING_FUNCTIONALITY |
| `…` — authenticates with a username and password | AC2 | SCAFFOLD_SATISFIED — see note 2 |
| `ConnectionEditors.serviceNow` — does not let them add an additional field ServiceNow cannot fill | DoD 5 | MISSING_FUNCTIONALITY |
| `…` — does not let them map a value back into ServiceNow | D8 | MISSING_FUNCTIONALITY |

## Notes on the SCAFFOLD_SATISFIED rows

`SCAFFOLD_SATISFIED` means the test passes against the scaffold. None of these is a test bug; each
is either a declaration-grade assertion or a regression guard over an existing mechanism. They are
listed explicitly rather than hidden, because a green acceptance test at DISTILL time is exactly the
shape of Fixture Theater and deserves an argument, not a silence.

1. **`ARightsProblem_IsNeverDressedUpAsAReachabilityProblem`** — asserts a *negative*: the denial
   rungs must not carry `connection_failed` or `invalid_url`. The `__scaffold__` sentinel is not one
   of those codes, so the assertion holds vacuously today. It becomes load-bearing the moment the
   ladder is implemented, and it is the one assertion that catches a future refactor collapsing a
   rights failure into the transport failure branch. Kept.

2. **The AC1 / auth-method-key rows** — AC1 is satisfied by the enum addition alone (DESIGN Reuse
   Analysis row #1: `GetSupportedWorkTrackingSystemConnections` iterates `Enum.GetValues`). The enum
   member, the `AuthenticationMethodKeys.ServiceNowBasic` constant and its `GetDefaultForSystem` arm
   are declaration-level and had to exist for the test suite to compile at all, so shipping them as
   scaffolding necessarily turns AC1 green. The valuable assertion in this group is the
   **enum-ordering guard** — `(int)WorkTrackingSystems.ServiceNow == 4` — which is a permanent
   regression guard against the silent data-corruption bug of inserting a member mid-enum.

3. **`TheCredentialAnAdministratorEnters_IsNeverHandedBackToTheBrowser`** — AC5 is satisfied by the
   *existing* `EncryptSecrets` change-tracker hook and the existing DTO redaction, which is exactly
   what DESIGN predicted ("no new mechanism"). The test is therefore a regression guard on a
   mechanism this slice inherits rather than builds. It would go red if a future change stopped
   marking the ServiceNow password secret — which is the failure worth guarding.

4. **`ServiceNowValidationVerdictPurityArchUnitTest`** — a structural guard, not a behaviour driver.
   It has to be green at DISTILL, because a red purity rule would mean the scaffold itself is
   already impure. Its job starts in DELIVER, when the ladder acquires a body and the temptation to
   let it fetch its own input appears.

## Failures that are not ours

Two tests fail on `main` independently of this work and continue to fail here:

- `LicenseServiceTest.ValidLicenseLoaded_LoadNewLicense_IsValid`
- `LicenseServiceTest.ValidLicenseLoaded_RemoveLicense_LoadNewLicense_IsValid`

Verified by stashing every change in this run and re-running the two tests at `main` — both fail
identically. They depend on a license fixture whose validity window has passed. Out of scope for
slice 01; flagged upstream in the DISTILL wave section of `feature-delta.md`.

## Verification commands

```
# backend — build must be clean, new tests red, everything else green
cd Lighthouse.Backend
dotnet build                      # 0 warnings, 0 errors
dotnet test --filter "Category!=Integration"

# frontend
cd Lighthouse.Frontend
pnpm test                         # 3 new files red, 282 pre-existing files green
pnpm build                        # Biome + tsc + vite, zero errors and zero warnings
```
