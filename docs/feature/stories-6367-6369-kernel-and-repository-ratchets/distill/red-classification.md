# RED classification: stories 6367 + 6369

2026-10-27. Every new pending test was un-ignored once (not committed) and run with
`dotnet test --filter`. All 31 failed. None failed on setup, loading or types; each reached its own first
assertion or its scaffold body. With the ignores restored, `dotnet test --filter FullyQualifiedName~Architecture`
is green: 238 passed, 31 skipped, 0 failed.

Review round: the three added source-scan and normalisation tests, the two kernel rules' fixture tests and the
rule factories were added after the first pass; the table rows below marked (review) are those.

Classes: **MISSING_FUNCTIONALITY**: the assertion fires, or the call reaches a `// SCAFFOLD: true` body that throws
`AssertionException` (never `NotImplementedException`), because the helper or the rule is not built. **BROKEN** would
be an import, loading or fixture failure; there is none.

Paths are under `Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/`.

## `RepositoryLeakRatchetArchUnitTest.cs` (25 pending)

| Test | Slice | Class | Failure seen |
|---|---|---|---|
| GetAllByPredicate_IsCalledOnlyFromTheFilesOnTheList | S05a-1 | MISSING_FUNCTIONALITY | `ProductionSource.Files is not built yet` (the 14-entry, unique-list guard passed first) |
| AListThatMatchesTheCodeExactly_Passes | S05a-1 | MISSING_FUNCTIONALITY | scaffold: `AssertMatchesBaseline` |
| ANewOffenderNotOnTheList_FailsAndNamesIt | S05a-1 | MISSING_FUNCTIONALITY | expected message to contain `Services/NewOne.cs`, was the scaffold text |
| AListedEntryThatNoLongerOffends_FailsAndNamesItUntilItIsDeleted | S05a-1 | MISSING_FUNCTIONALITY | expected `Services/Cleaned.cs` and `delete` |
| ANewOffenderAndAStaleEntryAtOnce_AreBothNamedInOneFailure | S05a-1 | MISSING_FUNCTIONALITY | expected both names |
| TheOrderOfTheListsAndRepeatedEntries_DoNotMatter | S05a-1 | MISSING_FUNCTIONALITY | scaffold: `AssertMatchesBaseline` |
| NothingOffendingAgainstAnEmptyList_Passes | S05a-1 | MISSING_FUNCTIONALITY | scaffold: `AssertMatchesBaseline` |
| ACallInCode_IsReportedWithItsFileAndLine | S05a-1 | MISSING_FUNCTIONALITY | scaffold: `GetAllByPredicateCallSites` |
| AMentionInALineComment_IsNotACall | S05a-1 | MISSING_FUNCTIONALITY | scaffold: `GetAllByPredicateCallSites` |
| AMentionInABlockComment_IsNotACall | S05a-1 | MISSING_FUNCTIONALITY | scaffold: `GetAllByPredicateCallSites` |
| TheDeclarationOnTheRepositoryContract_IsNotACall | S05a-1 | MISSING_FUNCTIONALITY | scaffold: `GetAllByPredicateCallSites` |
| ACallInsideTheRepositoriesFolder_IsAllowed | S05a-1 | MISSING_FUNCTIONALITY | scaffold: `GetAllByPredicateCallSites` |
| TheApi_DependsOnARepositoryOnlyInTheTypesOnTheList | S05a-2 | MISSING_FUNCTIONALITY | scaffold: `RepositoryDependentsIn` (the 23-entry, unique-list guard passed first) |
| AConstructorParameterOnARepository_IsADependency | S05a-2 | MISSING_FUNCTIONALITY | scaffold: `RepositoryDependentsIn` |
| ARepositoryNamedOnlyAsAGenericArgument_IsADependency | S05a-2 | MISSING_FUNCTIONALITY | scaffold: `RepositoryDependentsIn` |
| AConcreteRepositoryClass_IsADependency | S05a-2 | MISSING_FUNCTIONALITY | scaffold: `RepositoryDependentsIn` (the fixture architecture loaded in 8 s: loading works) |
| ALambdaThatAsksForARepository_IsReportedAgainstTheTypeThatWroteIt | S05a-2 | MISSING_FUNCTIONALITY | scaffold: `RepositoryDependentsIn` |
| AnAsyncMethodThatAsksForARepository_IsReportedAgainstTheTypeThatWroteIt | S05a-2 | MISSING_FUNCTIONALITY | scaffold: `RepositoryDependentsIn` |
| ATypeThatReadsThroughAServiceContractInTheRepositoriesNamespace_IsNotADependency | S05a-2 | MISSING_FUNCTIONALITY | scaffold: `RepositoryDependentsIn` |
| ACompilerMadeNestedType_NormalisesToItsDeclaringType | S05a-2 | MISSING_FUNCTIONALITY | scaffold: `DeclaringTypeOf` (the guard that the fixtures really yield a `<`-named nested type passed first) |
| AnOrdinaryTypeName_IsLeftAlone | S05a-2 | MISSING_FUNCTIONALITY | scaffold: `DeclaringTypeOf` |

The negative cases (`NothingOffending…`, `AMention…`, `ATypeThatReads…`) are red today only because the scaffold
throws. Once the helper exists they pass for a correct implementation, so they guard against a helper that reports
everything; check that each can fail by a throwaway mutation at DELIVER.

Added in review, in the same class (all MISSING_FUNCTIONALITY, scaffold text seen):

| Test (review) | Slice | Failure seen |
|---|---|---|
| ASlashPairInsideAStringLiteral_DoesNotHideACallOnTheSameLine | S05a-1 | scaffold: `GetAllByPredicateCallSites` |
| ACallWrittenOnTheLineAfterItsReceiver_IsReportedAtTheCallsLine | S05a-1 | scaffold: `GetAllByPredicateCallSites` |
| ANestedTypeSomeoneWrote_KeepsItsOwnName | S05a-2 | scaffold: `DeclaringTypeOf` |
| AnEscapedQuoteBeforeASlashPairInsideAString_DoesNotHideACallOnTheSameLine (round 2) | S05a-1 | scaffold: `GetAllByPredicateCallSites` |

## `KernelRulesCatchANewOffenderArchUnitTest.cs` (4 pending, review)

The two kernel rules now come from `KernelRules` (factories, scaffold: `KernelRules.… is not built yet`), taking namespace
patterns so the same rule runs over made-up types. Fixture Models live in
`Lighthouse.Backend.Models.KernelFixtures.{Offending,Clean}`; the fixture service type in
`Lighthouse.Backend.Services.KernelFixtures`. Loading a second architecture from test-assembly types works with
ArchUnitNET here.

| Test | Slice | Class | Failure seen |
|---|---|---|---|
| AModelThatReachesIntoServices_FailsTheKernelRuleAndIsNamed | S04a-6 | MISSING_FUNCTIONALITY | scaffold: `ModelsDoNotDependOnServices` |
| AModelThatStaysInsideTheKernel_PassesTheKernelRule | S04a-6 | MISSING_FUNCTIONALITY | scaffold |
| AModelThatReachesIntoTheUpdateQueue_FailsTheUpdateQueueRuleAndIsNamed | S04a-4 | MISSING_FUNCTIONALITY | scaffold: `ModelsDoNotDependOnTheUpdateQueue` |
| AModelThatStaysInsideTheKernel_PassesTheUpdateQueueRule | S04a-4 | MISSING_FUNCTIONALITY | scaffold |

Round 2: the update-queue offender fixture now names an enum (`AFixtureUpdateKind`) in
`Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.KernelFixtures`, and the test uses the factory's default
update-queue pattern, as the production test does. The fixtures live in the test assembly; the production architecture loads
only the production assembly, so neither production kernel rule can see them.

To prove these tests are well-formed I temporarily gave the two factories a real body (reverted; the scaffold is back):
all four passed (the offender is named as `Lighthouse.Backend.Models.KernelFixtures.Offending.OffendingModel`, the clean
fixture passes), and the two production tests below failed with the messages shown.

## `ModuleBoundariesArchUnitTest.cs` (1 pending)

| Test | Slice | Class | Failure seen |
|---|---|---|---|
| SharedKernel_DoesNotDependOnServices | S04a-6 | MISSING_FUNCTIONALITY | scaffold: `ModelsDoNotDependOnServices`. With the probe body (real rule): 43 Models types fail, naming 8 distinct Services types, below |

Offending Services types named by the real rule today (count of Models types naming each): `Services.Interfaces.IEntity` 37,
`IConcurrencyTokenEntity` 9, `Services.Implementation.BlackoutDaysExtensions` 3, `Services.Interfaces.Reports.ReportOwnerKind` 2,
`Services.Implementation.WorkTrackingConnectors.WorkTrackingSystems` 2, `Services.Interfaces.Reports.ReportOwner` 1,
`Services.Implementation.Encryption.LegacyDefaultEncryptionKey` 1,
`Services.Implementation.BackgroundServices.Update.UpdateType` 1. That is exactly the eight types DESIGN lists, so S04a-6's
"any extra type is a ninth decision" check passes today. Sample line:
`Lighthouse.Backend.Models.AppSetting does depend on "Lighthouse.Backend.Services.Interfaces.IEntity"`.

## `ModelsDoNotDependOnTheUpdateQueueArchUnitTest.cs` (1 pending)

| Test | Slice | Class | Failure seen |
|---|---|---|---|
| TheDomainModel_KnowsNothingOfTheUpdateQueue_NotEvenTheNameOfAnUpdateType | S04a-4 | MISSING_FUNCTIONALITY | scaffold. With the probe body: `Lighthouse.Backend.Models.Logging.RefreshSubject does depend on "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateType"`, the only offender |

## Already pinned

`WorkTrackingSystems`' stored integer values are pinned by an existing active test,
`ServiceNowConnectionConfigurationTest.ServiceNow_SitsAtTheEndOfTheStoredWorkTrackingSystemOrder`, which asserts all five
members (AzureDevOps 0, Jira 1, Linear 2, Csv 3, ServiceNow 4), so no new test was added.

## Not run

`Scripts/test-selection/test-path-classifier.sh` has no skip idiom (a flat list of `assert` lines; one
failing assert exits 1), so its scenario is a DELIVER step in S04a-2, not a scaffold. See feature-delta.
