# RED classification — slice 02 (ADO Story 5575)

Pre-DELIVER fail-for-the-right-reason gate. Every acceptance test authored in this DISTILL run was
executed against the RED scaffolds and classified. `MISSING_FUNCTIONALITY` is the only acceptable
RED; anything else is a test bug and blocks handoff.

Run date: 2026-07-29. Commands and their output are at the bottom.

**Result: 43 RED, 5 green-against-scaffold (each argued below), 0 wrong-shape failures.**

## Scaffold mechanism (unchanged from slice 01)

The production assembly cannot reference NUnit, so a scaffold cannot raise an `AssertionException`.
The adaptation slice 01 established is **wrong-value scaffolds**: every scaffold returns a sentinel
or the deliberate opposite of the specified behaviour, so the failure surfaces as a genuine NUnit
assertion failure at the assertion site and the expected/actual diff reads as the specification.

Slice 02's scaffolds:

| Scaffold | Deliberate wrong value |
|---|---|
| `ServiceNowWorkItemMapper.MapRecord` | `__scaffold__` strings, `StateCategories.Unknown`, `DateTime.UnixEpoch` for all three dates |
| `ServiceNowWorkItemMapper.ReadStateLabel` | `__scaffold__` |
| `ServiceNowTeamQueryVerdict.FromMissingQuery` | `Failure("__scaffold__", "__scaffold__")` |
| `ServiceNowTeamQueryVerdict.FromTeamProbe` | `Failure("__scaffold__", "__scaffold__")` |
| `ServiceNowWorkTrackingConnector.GetWorkItemsForTeam` | exactly one item, `ReferenceId = "INC0000005"` (the record the team never mapped), carrying one fabricated `WorkItemStateTransition` |
| `ServiceNowWorkTrackingConnector.ValidateTeamSettings` | routes to the scaffolded `FromMissingQuery` |

Every scaffold carries a `// SCAFFOLD (DISTILL slice 02, Story #5575)` comment.
`grep -rn "SCAFFOLD (DISTILL slice 02" Lighthouse.Backend/` finds all of them; zero should remain at
the end of DELIVER.

### The slice-01 lesson was applied deliberately

Slice 01's `ValidateConnection` scaffold returned `ConnectionValidationResult.Success()`, whose
`Code` is `"valid"` — exactly what the happy-path test asserted, so that test passed vacuously.

The first draft of this slice reproduced the same shape: `GetWorkItemsForTeam` scaffolded as an
**empty list**, which is precisely what three of the tests specify for their own inputs
(`ReadsNothingRatherThanEverything`, `WorkInAStateTheTeamNeverMapped_IsLeftOut`,
`SyncedWork_CarriesNoInventedHistory`). All three passed against the scaffold and would have stayed
green through a connector that always returned nothing — which is the epic's headline failure mode
reproduced in the scaffold. The scaffold was changed to return one deliberately wrong item, and all
three went RED. Green-against-scaffold went from 8 to 5.

## Backend — layer 1 (pure functions, no IO)

| Test | AC | Verdict |
|---|---|---|
| `ServiceNowWorkItemMapperTest.WorkThatWasResolvedButNeverFormallyClosed_StillCountsAsFinished` | AC2 / ADR-117 | MISSING_FUNCTIONALITY |
| `…WhenWorkFinished_IsReadFromTheResolutionBeforeTheClosure` (3 cases) | AC2 / ADR-117 | MISSING_FUNCTIONALITY ×3 |
| `…WorkThatIsStillUnderway_HasNotFinished` | AC2 | MISSING_FUNCTIONALITY |
| `…WhenWorkArrived_IsWhenTheRequestWasOpened` | AC2 / ADR-117 | MISSING_FUNCTIONALITY |
| `…WorkThatCarriesNoRequestTime_ArrivedWhenItWasRecorded` | AC2 / ADR-117 | MISSING_FUNCTIONALITY |
| `…TheDayWorkFinished_IsTheDayTheInstanceRecordedInUniversalTime` | AC2 (date trap) | MISSING_FUNCTIONALITY |
| `…TheDayWorkArrived_IsTheDayTheInstanceRecordedInUniversalTime` | AC2 (date trap) | MISSING_FUNCTIONALITY |
| `…TheDayWorkWasRecorded_IsTheDayTheInstanceRecordedInUniversalTime` | AC2 (date trap) | MISSING_FUNCTIONALITY |
| `…TheStateAFlowCoachSees_IsTheLabelTheirServiceDeskUses` | AC3 | MISSING_FUNCTIONALITY |
| `…WorkInAStateTheTeamHasRenamed_IsReportedUnderTheTeamsOwnName` | AC3 | MISSING_FUNCTIONALITY |
| `…WorkIsCategorised_ByTheLabelTheTeamMapped("New", ToDo)` | AC3 | MISSING_FUNCTIONALITY |
| `…WorkIsCategorised_ByTheLabelTheTeamMapped("In Progress", Doing)` | AC3 | MISSING_FUNCTIONALITY |
| `…WorkIsCategorised_ByTheLabelTheTeamMapped("Resolved", Done)` | AC3 | MISSING_FUNCTIONALITY |
| `…WorkIsCategorised_ByTheLabelTheTeamMapped("Awaiting Vendor", Unknown)` | AC3 | SCAFFOLD_SATISFIED — note 1 |
| `…WorkIsIdentified_ByTheNumberTheServiceDeskQuotes` | AC2 | MISSING_FUNCTIONALITY |
| `…WorkIsTitled_ByItsShortDescription` | AC2 | MISSING_FUNCTIONALITY |
| `…TheKindOfWork_IsTheTableItWasReadFrom` | AC2 | MISSING_FUNCTIONALITY |
| `ServiceNowTeamQueryVerdictTest.ATeamThatHasNotSaidWhichWorkIsTheirs_IsAskedForAQuery` | AC6 | MISSING_FUNCTIONALITY |
| `…AQueryThatSelectsNoWork_IsToldItSelectedNoWork` | AC6 | MISSING_FUNCTIONALITY |
| `…AQueryThatSelectsEveryRecordInTheTable_StopsTheFlowCoachRatherThanShowingWholeInstanceMetrics` | AC6 | MISSING_FUNCTIONALITY |
| `…AQueryThatSelectsEverything_NamesBothPossibleCausesRatherThanGuessing` | AC6 | MISSING_FUNCTIONALITY |
| `…AQueryThatSelectsOneTeamsWork_IsAccepted` | AC6 | MISSING_FUNCTIONALITY |
| `…AQueryAgainstATableWithNothingInIt_IsToldTheTableIsEmptyRatherThanAccused` | AC6 | MISSING_FUNCTIONALITY |
| `…AQueryProblem_IsNeverReportedAsAReachabilityOrCredentialProblem` (3 cases) | AC6 | SCAFFOLD_SATISFIED ×3 — note 2 |

## Backend — layer 3 (real adapter, stubbed transport)

| Test | AC | Verdict |
|---|---|---|
| `ServiceNowTeamSyncTest.SyncingATeam_AsksTheConfiguredTableForTheWorkTheFlowCoachDescribed` | AC1 | MISSING_FUNCTIONALITY |
| `…SyncingATeam_AsksForBothTheLabelAndTheUnderlyingValueOfEveryField` | AC2 / AC3 | MISSING_FUNCTIONALITY |
| `…WorkSpreadAcrossMorePagesThanOne_IsAllBroughtBack` | AC7 | MISSING_FUNCTIONALITY |
| `…PagesOfWork_NeitherOverlapNorSkip` | AC7 | MISSING_FUNCTIONALITY |
| `…SyncingATeam_ReadsInBatchesRatherThanOneRecordAtATime` | SPIKE Q7 | SCAFFOLD_SATISFIED — note 3 |
| `…WorkInAStateTheTeamNeverMapped_IsLeftOut` | AC1 | MISSING_FUNCTIONALITY |
| `…ATeamThatHasNotSaidWhichWorkIsTheirs_ReadsNothingRatherThanEverything` | AC1 / AC6 | MISSING_FUNCTIONALITY |
| `…SyncedWork_CarriesNoInventedHistory` | AC5 | MISSING_FUNCTIONALITY |
| `…WorkThatWasResolvedButNeverClosed_ArrivesWithTheDayItFinished` | AC2 | MISSING_FUNCTIONALITY |
| `…ValidatingATeamsSettings_ComparesWhatTheQuerySelectsAgainstWhatTheTableHolds` | AC6 | MISSING_FUNCTIONALITY |
| `…ValidatingATeamThatHasNotSaidWhichWorkIsTheirs_AsksForAQueryWithoutContactingTheInstance` | AC6 | MISSING_FUNCTIONALITY |
| `…ValidatingATeamAgainstATableTheInstanceDoesNotHave_IsToldTheTableIsUnknown` | AC6 | MISSING_FUNCTIONALITY |
| `…ValidatingATeamWithACredentialThatCannotReadTheTable_IsToldItIsAPermissionsProblem` | AC6 | MISSING_FUNCTIONALITY |
| `…ValidatingATeamAgainstAnInstanceThatCannotBeReached_IsToldTheInstanceIsNotThere` | AC6 | MISSING_FUNCTIONALITY |
| `…ValidatingAQueryThatTheInstanceSilentlyIgnored_StopsRatherThanAcceptingWholeInstanceMetrics` | AC6 | MISSING_FUNCTIONALITY |
| `…ValidatingAQueryThatSelectsOneTeamsWork_Passes` | AC6 | MISSING_FUNCTIONALITY |

## Backend — layer 5 (walking skeleton, real stack, real HTTP over loopback)

| Test | AC | Verdict |
|---|---|---|
| `ServiceNowTeamSyncAcceptanceTest.AFlowCoachPointingATeamAtTheirOwnServiceNowQuery_IsToldTheirSettingsAreGood` | AC1 / AC6 (WS) | MISSING_FUNCTIONALITY |
| `…AFlowCoachWhoseQueryTheInstanceSilentlyIgnored_IsStoppedOnTheSettingsPage` | AC6 (WS) | MISSING_FUNCTIONALITY |
| `…ATeamsServiceNowWork_ArrivesAsWorkItemsOnTheDaysThroughputCountsBy` | AC1 / AC2 / AC4 / AC7 (WS) | MISSING_FUNCTIONALITY |
| `…TimeInStateOnServiceNowWork_IsDerivedFromObservedChangesRatherThanInventedOrLeftBlank` | AC5 (WS) | MISSING_FUNCTIONALITY |

## Notes on the SCAFFOLD_SATISFIED rows

`SCAFFOLD_SATISFIED` means the test passes against the scaffold. None is a test bug; each is listed
explicitly rather than hidden, because a green acceptance test at DISTILL time is the shape of
Fixture Theater and deserves an argument, not a silence.

1. **`WorkIsCategorised_ByTheLabelTheTeamMapped("Awaiting Vendor", Unknown)`** — the mapper scaffold
   returns `StateCategories.Unknown`, and this one case of four expects exactly that. The enum has
   four members and the four cases cover all of them, so no sentinel value exists that would fail
   every case. The rule is driven by its three sibling cases, which are RED; this case pins the
   unmapped-label branch and becomes load-bearing the moment the mapper starts returning anything
   other than `Unknown`. Kept.

2. **`AQueryProblem_IsNeverReportedAsAReachabilityOrCredentialProblem`** (3 cases) — asserts a
   *negative*: a query verdict must never carry `connection_failed`, `invalid_url`,
   `authentication_failed` or `insufficient_permissions`. `__scaffold__` is none of those, so the
   assertion holds vacuously today. Same shape and same argument as slice 01's
   `ARightsProblem_IsNeverDressedUpAsAReachabilityProblem`: it catches a future refactor that
   collapses a settings problem into the transport branch, which would send an administrator to
   entirely the wrong people. Kept.

3. **`SyncingATeam_ReadsInBatchesRatherThanOneRecordAtATime`** — asserts an upper bound on request
   count. The scaffold makes zero requests, so zero satisfies "at most three". This is vacuous only
   against a scaffold that does no IO; against any real implementation it is load-bearing, and it
   is precisely the assertion that fails if someone writes an N+1 per-record read — which SPIKE Q7
   measured as a five-minute sync on a real instance. Kept.

## Failures that are not ours, and one that is intentional

The full backend run (`Category!=Integration&Category!=ServiceNowIntegration&Category!=requires-docker`)
reports **47 failed / 3798 passed / 3 skipped**. That is 43 slice-02 REDs plus four others:

**Pre-existing, unrelated (2).** `LicenseServiceTest.ValidLicenseLoaded_LoadNewLicense_IsValid` and
`…_RemoveLicense_LoadNewLicense_IsValid` — full-suite-only, documented in slice 01's classification
and unchanged by this work. Slice 01 diagnosed this as generic test-order dependence; the failure
this run is more specific and worth recording — `System.IO.FileNotFoundException` for
`bin/Debug/net10.0/Services/Implementation/Licensing/valid_not_expired_license.json`. Something
earlier in a full-suite run removes that content file from the output directory, which is why the
cases pass in isolation. Out of scope here, but the next person to chase it should start from the
missing file rather than from ordering in the abstract.

**Intentionally superseded by slice 02 (2), and they live in a file this run did not touch:**

| Test | Why it now fails |
|---|---|
| `ServiceNowWorkTrackingConnectorTest.ReadingWorkFromServiceNow_IsDeclaredUnsupportedRatherThanReturningNothing` | asserts `GetWorkItemsForTeam` throws `NotSupportedException`. Slice 02 is the release that makes it work. |
| `ServiceNowWorkTrackingConnectorTest.PointingATeamAtServiceNow_IsRefusedWithAnActionableReason` | asserts `ValidateTeamSettings` returns `team_settings_not_supported`. Slice 02 replaces that refusal with a verdict about the team's query. |

These are **not regressions** — they are slice-01 declarations whose subject slice 02 delivers, and
they would fail against the finished implementation just as they fail against the scaffold. They are
also not scaffold artefacts: a real `GetWorkItemsForTeam` handed a bare `new Team()` returns an empty
list (the Linear precedent), never a `NotSupportedException`, so no scaffold shape could keep both
the slice-01 assertion and the slice-02 specification green.

**They were deliberately left untouched.** `ServiceNowWorkTrackingConnectorTest.cs` is being
strengthened for mutation coverage in a concurrent session, and editing it here would clobber that
work.

**DELIVER must, as its first act on this slice:**
- delete the `GetWorkItemsForTeam` line from `ReadingWorkFromServiceNow_IsDeclaredUnsupportedRatherThanReturningNothing`
  (the `GetFeaturesForProject` and `GetParentFeaturesDetails` lines stay — slice 03 is cancelled and
  those refusals are permanent), and
- delete `PointingATeamAtServiceNow_IsRefusedWithAnActionableReason` outright; its replacements are
  the `ValidatingATeam…` tests in `ServiceNowTeamSyncTest`.

## Frontend

**No frontend change and no frontend test in this slice.** Two acceptance criteria look like they
might need one and do not:

- **AC3** — the state-mapping UI is generic: it renders whatever raw states the backend reports for
  the team. Surfacing the label rather than the choice value is therefore entirely a backend
  decision (`ReadStateLabel`), and it is pinned at layer 1. A frontend test here would assert the
  behaviour of a component this slice does not touch.
- **AC4's honesty obligation** — where the request-to-resolution qualification surfaces (terminology,
  UI annotation, docs, or all three) is **ADR-117's open question for ratification**, and ADR-117 is
  still `Proposed`. Authoring a UI test against an unratified decision would pin a choice the
  maintainer has not made. See the upstream note in `feature-delta.md`.

## Verification commands

```
# backend — build clean, slice-02 tests red, everything else green
cd Lighthouse.Backend
dotnet build                                                      # 0 warnings, 0 errors
dotnet test --no-build --filter "FullyQualifiedName~ServiceNowWorkItemMapperTest|FullyQualifiedName~ServiceNowTeamQueryVerdictTest|FullyQualifiedName~ServiceNowTeamSyncTest|FullyQualifiedName~ServiceNowTeamSyncAcceptanceTest"
# → Failed: 43, Passed: 5, Total: 48 — zero exceptions, every failure at an assertion site

dotnet test --no-build --filter "Category!=Integration&Category!=ServiceNowIntegration&Category!=requires-docker"
# → Failed: 47, Passed: 3798, Skipped: 3
#   47 = 43 slice-02 RED + 2 pre-existing LicenseServiceTest + 2 intentionally superseded slice-01

# frontend — untouched by this slice
cd Lighthouse.Frontend
pnpm build && pnpm test
```
