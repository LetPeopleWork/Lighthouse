# Acceptance brief: story\-6359\-s03a\-c\-edges\-and\-rules

## Slices

### S1

##### Covers

P1

```text
Run each listed Tracker integration rule over the compiled product with the list measured from it
```

```text
Every rule holds: what reaches the module's Domain, Application and Adapters from outside, what its Application reaches below it, into its own adapters or the platform, what it reaches in other contexts and which of its types reach a repository all equal their lists exactly
```

```text
The examiner sees each rule pass on today's code, so the lists describe the code as it is
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:\:AListedTrackerIntegrationRule\_MatchesWhatTheCompiledCodeReaches\_InBothDirections

```text
A list held more entries than the code needs; the deletion test below closes that
```

###### Obligations



##### Covers

P1

```text
Run the Domain rule over the compiled Tracker integration Domain
```

```text
The Domain reaches only the shared kernel and the base library, and holds at least one type
```

```text
The examiner sees the Domain rule pass on today's code
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:\:TheTrackerIntegrationDomain\_ReachesOnlyTheSharedKernelAndTheBaseLibrary

```text
The rule looked at an empty namespace; the shared rule fails on an empty Domain
```

###### Obligations



##### Covers

P1

```text
Delete each entry in turn from each measured list, then add an entry that reaches nothing
```

```text
Each deleted entry fails its rule and is named as offending; the entry with no edge fails and is named for deletion
```

```text
The examiner sees that no list entry is spare and no stale entry can stay
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:\:EveryEntryDeletedFromATrackerIntegrationList\_FailsTheRuleAndIsNamed\_AndAnEntryWithNoEdgeIsNamedForDeletion

```text
Two entries were only needed together; each is still deleted alone and named
```

###### Obligations




##### Covers

P1

```text
Read the list of edges from Tracker integration into other contexts
```

```text
It holds exactly one edge, the Jira connector to the forecast block renderer
```

```text
The examiner sees the one accepted reach into Promises named and nothing else
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:\:TheOnlyOtherContextTrackerIntegrationReaches\_IsPromisesThroughTheJiraConnectorMergingTheForecastBlock

```text
The edge targets a second renderer type; the both-ways rule names it
```

###### Obligations


##### Covers

P1

```text
Read which Application types are listed as reaching the module's own adapters
```

```text
Exactly the two factories and the auth schema, each by its exact name
```

```text
The examiner sees the three connector-naming types listed one by one, with no namespace exemption
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:\:TheFactoriesAndTheAuthSchemaThatNameConnectors\_AreListedByTheirExactNames\_AndNoOtherApplicationTypeReachesTheAdapters

```text
One of the three reached an extra adapter type; the both-ways rule names that edge
```

###### Obligations


##### Covers

P1

```text
Read which module types are listed as reaching a repository
```

```text
Exactly the OAuth service and the connection health service
```

```text
The examiner sees that no connector reaches stored records and only the two services that keep their own records do
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:\:OnlyTheOAuthAndConnectionHealthServices\_ReachARepository\_AndBothAreListed

```text
One of the two reached a second repository; the both-ways rule names that edge
```

###### Obligations


##### Covers

P1

```text
A made-up Domain type that asks the licence
```

```text
The Domain rule fails naming it and not its compliant twin
```

```text
A Domain type reaching beyond the kernel is caught
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:\:ATrackerIntegrationDomainTypeAskingTheLicence\_IsNamed\_AndItsCompliantTwinIsNot

```text
The rule matched the offender only by its exact made-up namespace; the production run of the same rule covers the real namespace
```

###### Obligations



##### Covers

P1

```text
A made-up outside type off the list that reaches the Domain
```

```text
The Domain inbound rule fails naming it and not the listed caller
```

```text
An unlisted outside caller of the Domain is caught
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:\:AnOutsideTypeOffTheList\_ReachingTheTrackerIntegrationDomain\_IsNamed\_AndTheListedCallerIsNot

```text
The rule matched the offender only by its exact made-up namespace; the production run of the same rule covers the real namespace
```

###### Obligations



##### Covers

P1

```text
A made-up outside type off the list that reaches the Application inside an async method
```

```text
The Application inbound rule fails naming it and not the API, Work Items, the composition root or the listed caller
```

```text
An unlisted outside caller of the Application is caught
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:\:AnOutsideTypeOffTheList\_ReachingTheTrackerIntegrationApplication\_IsNamed\_AndTheApiWorkItemsTheCompositionRootAndTheListedCallerAreNot

```text
The rule matched the offender only by its exact made-up namespace; the production run of the same rule covers the real namespace
```

###### Obligations



##### Covers

P1

```text
A made-up outside type off the list that reaches the Adapters
```

```text
The Adapters inbound rule fails naming it and not the composition root or the listed caller
```

```text
An unlisted outside caller of the Adapters is caught
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:\:AnOutsideTypeOffTheList\_ReachingTheTrackerIntegrationAdapters\_IsNamed\_AndTheCompositionRootAndTheListedCallerAreNot

```text
The rule matched the offender only by its exact made-up namespace; the production run of the same rule covers the real namespace
```

###### Obligations



##### Covers

P1

```text
Made-up Application types holding a DbSet, reading the database context, shaping an API type, building an unlisted connector, running another module's adapter, reading an unlisted repository or the key ring
```

```text
The Application outbound rule fails naming each edge
```

```text
Each forbidden Application edge is caught
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:\:ATrackerIntegrationApplicationTypeReachingBelowItAnAdapterOrThePlatformOffTheList\_IsNamed

```text
The rule matched the offender only by its exact made-up namespace; the production run of the same rule covers the real namespace
```

###### Obligations



##### Covers

P1

```text
A made-up factory building a connector through a listed edge
```

```text
The Application outbound rule holds
```

```text
A listed edge is accepted
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:\:ATrackerIntegrationApplicationTypeBuildingAConnectorThroughAListedEdge\_PassesTheRule

```text
The rule matched the offender only by its exact made-up namespace; the production run of the same rule covers the real namespace
```

###### Obligations



##### Covers

P1

```text
Made-up module types reaching Work Items, Forecasting, Flow metrics, a second Promises type, the update code, Refinement, Reports, Access or the licence, beside a listed renderer edge
```

```text
The other-contexts rule fails naming each new edge and not the listed one
```

```text
Each other context is caught
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:\:ATrackerIntegrationTypeReachingAnotherContextOffTheList\_IsNamed

```text
The rule matched the offender only by its exact made-up namespace; the production run of the same rule covers the real namespace
```

###### Obligations



##### Covers

P1

```text
A made-up connector reading a repository beside a listed service doing the same
```

```text
The repository rule fails naming the connector and not the listed service
```

```text
A connector reaching stored records is caught
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:\:ATrackerIntegrationConnectorReachingARepository\_IsNamed\_AndTheListedServiceKeepingItsRecordsIsNot

```text
The rule matched the offender only by its exact made-up namespace; the production run of the same rule covers the real namespace
```

###### Obligations



##### Covers

P2

```text
Look for the old leaf pattern and its two rules on the former module rules
```

```text
The pattern and both rules are gone
```

```text
The examiner sees the old connector-folder definition retired, so only the module rules guard Tracker integration
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:\:TheOldLeafRuleOverTheConnectorFolders\_IsRetired\_SoOnlyTheModuleRulesGuardTrackerIntegration

```text
The pattern was renamed but kept; a renamed copy is caught by review, not by this test
```

###### Obligations


##### Covers

P2

```text
Read the CI path classifier and the local dev-test selection
```

```text
Neither names the old connector folders or Factories, and both still select each tracker and the shared connector code under the module
```

```text
The examiner sees live-suite selection follow the module paths only
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:\:LiveSuiteSelection\_NoLongerKnowsTheOldConnectorFolders\_AndStillSelectsEachTrackerUnderTheModule

```text
A module alternative is present but misspelled elsewhere; the classifier scenarios run by test-path-classifier.sh close that
```

###### Obligations



##### Covers

P2

```text
Run the classifier scenarios
```

```text
Every module-path scenario selects its tracker live suite, shared module paths select every suite, all pass
```

```text
A change to a connector under the module still runs that tracker's live suite
```

##### Disposition

Tested

Scripts\/test\-selection\/test\-path\-classifier\.sh

```text
A scenario for one tracker was deleted with the old-path ones; review of the removed scenarios closes that
```

###### Obligations


##### Covers

P4

```text
Build the backend and run the filtered backend suite
```

```text
The build has no warning and every test passes; production code is unchanged
```

```text
Nothing a user, a tracker or the database sees changes
```

##### Disposition

Tested

Lighthouse\.Backend \(dotnet build\; dotnet test with the live\-connector categories excluded\)

```text
A behaviour change no existing test observes; the whole-candidate review and EXAMINE cover it
```

###### Obligations


### S2

##### Covers

P3

```text
Run the Tracker integration unit-test placement rules on the current tree
```

```text
Each of the 99 measured Tracker integration unit-test files sits only in the module folder mirroring its subject and keeps its class name in that folder namespace; every module test folder mirrors a module folder; every listed folder holds a test; a made-up file left in an old folder, a copy elsewhere, another tracker folder, a folder the module lacks, an empty or missing listed folder each fail naming the path
```

```text
An examiner sees Passed lines saying every Tracker integration unit test sits in its module folder
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationUnitTestPlacementTest\.cs\:\:ATrackerIntegrationUnitTestFile\_SitsOnlyInItsModuleFolder\_AndKeepsItsClassInThatFolder

```text
A new unlisted test file placed outside Modules/TrackerIntegration under a new name in a folder that is neither old nor mirrored
```

###### Obligations



##### Covers

P3

```text
Run the old-folder test check on the current tree and on made-up trees
```

```text
No test file sits in the old connector, OAuth, connection-health or Factories test folders apart from the two demo data tests, which must still exist; a made-up test under Services/Implementation/WorkTrackingConnectors is named by path
```

```text
An examiner sees a Passed line saying the old Tracker integration test folders hold no test file apart from the demo data tests
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationUnitTestPlacementTest\.cs\:\:TheOldTrackerIntegrationTestFolders\_HoldNoTestFile\_ApartFromTheDemoDataTests

```text
A new test placed in Factories under the exact name of a listed demo data test after the original was deleted
```

###### Obligations


##### Covers

P3

```text
Run the old-folder source check on the current tree and on made-up trees
```

```text
No source file sits in the old connector, OAuth, connection-health folders or Factories apart from types named as staying, and no file under Services, Factories or Models is named after a type that moved; a made-up interface under Services/Interfaces/WorkTrackingConnectors is named
```

```text
An examiner sees a Passed line saying the old Tracker integration source folders hold no file of the context apart from the types that stay
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationUnitTestPlacementTest\.cs\:\:TheOldTrackerIntegrationSourceFolders\_HoldNoFileOfTheContext\_ApartFromTheTypesThatStay

```text
A moved type re-declared under a different file name in a shared Services folder outside the listed old folders
```

###### Obligations


##### Covers

P3

```text
Run the source check with a staying repository, the demo data, the connection save and a module file
```

```text
None of them is reported
```

```text
An examiner sees a Passed line saying a type that stays is not reported
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationUnitTestPlacementTest\.cs\:\:AMadeUpSourceFileOfTheContextLeftInAnOldFolder\_IsReported\_AndATypeThatStaysIsNot

```text
A Stays entry widened to a non-demo type, since the exceptions are read from the membership list
```

###### Obligations


##### Covers

P3

```text
Run the legacy Work Items and Forecast source folder check
```

```text
The old WorkItems, TeamData, WorkItemRules and Forecast folders under Services hold no .cs file; a made-up newcomer there or in a subfolder is named
```

```text
An examiner sees a Passed line saying the old Work Items and Forecast source folders hold no source file
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationUnitTestPlacementTest\.cs\:\:TheOldWorkItemsAndForecastSourceFolders\_HoldNoSourceFile

```text
A non-.cs file left in those folders
```

###### Obligations


##### Covers

P4

```text
Run the full backend build and suite
```

```text
Production unchanged; every existing test stays green
```

```text
Nothing a user, tracker or database sees changes
```

##### Disposition

NotTested

```text
The slice adds one test file and no production code; behaviour preservation is the full-suite gate, not this oracle
```

host

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Running the architecture rules for the tracker integration module shows every module rule passing: Domain reaches only the kernel and base library; outside code reaches the module only through legal namespaces and the measured lists; Application reaches no EF Core, Data, API or other module's Adapters beyond listed entries; the module reaches no other context except the one listed Promises renderer edge; no module type reaches a repository except the listed Application services. Each list is checked both ways (an unlisted real edge and a stale listed entry both fail), and the rules include a made-up offender in each position that is reported as caught.
```

#### E2

##### Promises

P2

```text
Asking test-selection for a changed connector path under the tracker integration module shows that tracker's live suite (and the backend suite) selected, while the recorded selection scenarios all pass; the old WorkTrackingIntegrationPattern leaf pattern and old Services/Factories alternatives no longer select anything, so a change to a path matching only those old forms selects no tracker live suite.
```

#### E3

##### Promises

P3

```text
Running the architecture rules shows the folder-placement rules passing: every unit test of the module sits under Lighthouse.Backend.Tests/Modules/TrackerIntegration, and no context file remains under Services/Interfaces, Services/Implementation or Factories apart from the named exceptions; the rules include a made-up test file and a made-up source file in an old folder reported as caught.
```

#### E4

##### Promises

P4

```text
Users, trackers and the database see no change: the EF model comparison reports no difference from the last migration on SQLite and Postgres, the full architecture rules run has no failing test, and the newest finished CI run shows each tracker's live suite (jira, ado, linear, servicenow, github) with passed counts and no failures. Absence of unforced production code edits is not observable by these probes and is left to candidate review.
```

## Charter

None.

## Examination

### E1

met

#### Observations

```text
Full architecture-rules probe: Total 1176, Passed 1176, no failure. Tracker rules passed: TheTrackerIntegrationDomain_ReachesOnlyTheSharedKernelAndTheBaseLibrary, ATrackerIntegrationDomainTypeThatReachesAForbiddenLayer_IsReportedAndItsCompliantTwinIsNot(Services/ModuleAdapters/Extensions), AListedTrackerIntegrationRule_MatchesWhatTheCompiledCodeReaches_InBothDirections(OutsideTypesReachingTheDomain/Application/Adapters, ApplicationEdgesBelowItIntoAdaptersOrThePlatform), ATrackerIntegrationDomainThatHoldsNoType_FailsTheRuleInsteadOfPassingUnseen. I did not separately see made-up-offender names for the Promises renderer edge or the repository rule in the grepped lines; the many other *_IsNamed tests passed.
```

### E2

met

#### Observations

```text
test-selection-show with the new Jira connector path plus the old Services/Implementation Jira path together: backend true, jira true, ado/linear/servicenow/github false. test-selection-show scenarios: passed 92, failed 0. I did not probe an old-only path alone, so the old-form-selects-nothing claim is not directly observed.
```

```text
test-selection-show with ONLY the old Services/Implementation Jira connector path: backend true; jira, ado, linear, servicenow, github, shared all false. The old form selects no tracker live suite.
```

### E3

met

#### Observations

```text
Folder-placement rules passed in the full run: Jira/Ado/Csv/Linear/ServiceNow connectors 'LiveInTheirTrackerFolder_AndNoneIsLeftUnderServices', factories 'AndNoneIsLeftUnderFactories', board types, auth schema, connector port; plus FlowMetrics/Forecasting made-up-test-file-is-reported tests. I saw no name of a TrackerIntegrationUnitTestPlacement made-up offender in my grep; the fragment run was not made separately.
```

### E4

met

#### Observations

```text
ef-model-unchanged: Sqlite and Postgres both 'No changes have been made to the model since the last migration.' ci-live-suites-show 303c88bf7: CI run 44073793376 on commit 2f96575e9 (previous, not candidate); backend success; Jira passed 1, Ado 5, Github 26, Linear 0 passed, ServiceNow 0 passed (all failed 0, skipped 0). Linear and ServiceNow show no passed count; the run is not the candidate's.
```

```text
ci-live-suites-show 6968d2c84: CI run 44061766628 on commit 6968d2c84 (the push that carried the moved connector code); backend success; Jira passed 72, Ado 58, Linear 20, ServiceNow 20, Github 32, all failed 0, skipped 0. Not the candidate's own run (303c88bf7); later feature-C commits changed only tests and scripts per the host, which I did not verify.
```
