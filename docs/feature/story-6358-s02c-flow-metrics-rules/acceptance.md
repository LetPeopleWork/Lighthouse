# Acceptance brief: story\-6358\-s02c\-flow\-metrics\-rules

## Slices

### S1

##### Covers

P1

```text
Read the compiled production assembly and list every type outside the Flow metrics module that references a type in the module's Domain
```

```text
The list equals the six listed types: the Portfolio, Portfolios, Team and Teams controllers, the daily series summary of the Metrics reports and the work item service; a seventh type fails and is named with what it reaches, and a listed type that stops reaching the Domain fails and is named with delete
```

```text
A maintainer who adds a direct dependency on the Flow metrics calculations from outside the module, or removes one, sees the architecture test fail and name the type
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs\:\:TheModuleDomain\_IsReachedFromOutsideOnlyByTheListedTypes

```text
A new outside type reaches the Domain only through a member the IL reader does not see, such as a reflection lookup by string name (accepted design limit of reading the IL)
```

###### Obligations


##### Covers

P1

```text
Read the compiled production assembly and list every type outside the Flow metrics module that references a type in the module's Adapters
```

```text
The only such type is the composition root Lighthouse.Backend.Program; any other fails and is named with what it reaches, and a composition root that stops reaching the Adapters fails and is named with delete
```

```text
A maintainer who uses a Flow metrics storage or background adapter directly from outside the module sees the architecture test fail and name the type
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs\:\:TheModuleAdapters\_AreReachedFromOutsideOnlyByTheCompositionRoot

```text
An outside type reaches an adapter only through dependency injection by a runtime-built Type or a reflection lookup by name, which leaves no type reference in the IL (accepted design limit)
```

###### Obligations


##### Covers

P1

```text
Run the Domain rule and the Adapters rule on made-up types where only the allowed outside type reaches the layer, a type inside the made-up module reaches it too, and another outside type reaches only the module's Application
```

```text
Each rule finds exactly the allowed type and passes
```

```text
A maintainer whose code follows the rule sees the architecture test pass; a type inside the module or a caller of the Application is never flagged
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs\:\:OnlyTheAllowedOutsideTypeReachingTheLayer\_PassesTheRule

```text
The rule also counted a type sitting in a namespace that merely starts with the module's name (for example a FlowMetricsX namespace) as inside; no such made-up type exists, and the namespace patterns end at a dot or the end
```

###### Obligations


##### Covers

P1

```text
Run each rule on made-up types where an outside type that is not on the list reaches the layer next to the allowed one
```

```text
The rule fails; the message names the newcomer, and only it, as not on the list, says which layer type it reaches, and asks to delete nothing
```

```text
A maintainer who adds a new outside dependency on the module's Domain or Adapters sees the test fail naming the new type and what it reaches
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs\:\:AnOutsideTypeNotOnTheList\_FailsTheRuleAndIsNamed

```text
The message wording around the names changed while still naming the newcomer and its target; the test pins the shared comparison's sentence, so a reworded sentence fails here as well as in the comparison's own tests
```

###### Obligations


##### Covers

P1

```text
Run each rule on made-up types where an outside type reaches the layer only inside the body of an async method
```

```text
The rule fails and names that type as not on the list, with the layer type it reaches
```

```text
A dependency written only inside an async method is caught like any other
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs\:\:AnOutsideTypeReachingTheLayerOnlyInsideAnAsyncMethod\_FailsTheRuleAndIsNamed

```text
An edge written only inside a lambda in a non-async method was missed; the IL reader walks every compiler-made nested type the same way, but this test only builds the async state machine
```

###### Obligations


##### Covers

P1

```text
Run the Domain rule with a list that also names an outside type that no longer reaches the Domain, and the Adapters rule with a composition root that no longer reaches the Adapters
```

```text
Each rule fails; the message names that entry, and only it, with delete, and names no newcomer
```

```text
A maintainer who removes the last dependency of a listed type is told to delete it from the list, so the dependency cannot come back unseen
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundDomainAndAdaptersArchUnitTest\.cs\:\:AListedTypeThatNoLongerReachesTheLayer\_FailsTheRuleAndIsNamedForDeletion

```text
A listed type was renamed so the list holds a name that no longer exists; that is reported as stale the same way, so the test accepts it, which is the wanted behaviour
```

###### Obligations


##### Covers

P1

```text
Compare a list of found entries with a written list through the shared two-way comparison: equal lists, a newcomer, a stale entry, both at once, reordered and repeated entries, both empty
```

```text
Equal, reordered or repeated lists and two empty lists pass; a newcomer fails named with the fix hint; a stale entry fails named with delete; both at once are named in one failure
```

```text
Every inline list behaves the same: it can only shrink, and its failure says what to add to the code or delete from the list
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/RepositoryLeakRatchetArchUnitTest\.cs\:\:AListThatMatchesTheCodeExactly\_Passes\, ANewOffenderNotOnTheList\_FailsAndNamesIt\, AListedEntryThatNoLongerOffends\_FailsAndNamesItUntilItIsDeleted\, ANewOffenderAndAStaleEntryAtOnce\_AreBothNamedInOneFailure\, TheOrderOfTheListsAndRepeatedEntries\_DoNotMatter\, NothingOffendingAgainstAnEmptyList\_Passes

```text
The comparison treated names case-insensitively; no case pins two names that differ only by case, and type names are compared ordinally
```

###### Obligations







### S2

##### Covers

P1

```text
Read the compiled production assembly, in Debug and in Release, and list every type outside the Flow metrics module, the API, the composition root, Reports and Refinement that references a type in the module's Application or its Policies
```

```text
The list equals the ten listed types: ForecastService and its nested GatheredInputs, ForecastInputs, ForecastInputFingerprint, ForecastRealityCheckService, TeamDataService, WorkItemService, WriteBackTriggerService, FeatureListActiveWork and DatabaseMaintenanceGate; the API controllers, the composition root, the two Reports series and the two Refinement types are not counted
```

```text
A developer who adds a direct caller of the Flow metrics Application outside its allowed callers sees the build fail naming that type, and one who removes a listed caller is told to delete it from the list
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:\:TheModuleApplication\_IsReachedFromOutsideOnlyByTheApiTheCompositionRootReportsRefinementAndTheListedTypes

```text
A rule whose Application pattern drops the Policies sub-namespace: no production outside type reaches only the Policies today, so this list stays green; the made-up newcomer that reaches the Policies fails that rule
```

###### Obligations


##### Covers

P1

```text
Run the same rule on made-up types: a caller in a legal caller's namespace, a composition root with a nested type that reaches the Policies, a listed caller written twice on the list, an outside type that reaches only the Domain, and a type in the module's own Adapters that calls the Application
```

```text
The rule finds exactly the listed caller and passes
```

```text
Code that calls the Application only from allowed places keeps the build green, so the rule is not switched off
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:\:OnlyAllowedCallersReachingTheApplication\_PassTheRule

```text
A rule that sets legal callers aside by a looser test than their namespaces, for example any namespace that merely contains the legal name; no made-up lookalike namespace exists to catch it
```

###### Obligations



##### Covers

P1

```text
Run the rule on made-up outside types that reach only the module's Domain or nothing, with an empty list
```

```text
The rule finds no type and passes
```

```text
A module with no undeclared caller passes against an empty list
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:\:NoOutsideTypeReachingTheApplicationAndAnEmptyList\_PassTheRule

```text
A rule that never reports anything; the other made-up cases fail such a rule (observed with a blind rule)
```

###### Obligations


##### Covers

P1

```text
Add a made-up outside type, not on the list, that calls a type in the Application's Policies
```

```text
The rule fails, names the newcomer as not on the list with the Policies type it reaches, and does not ask to delete the listed caller
```

```text
A new undeclared caller of the Application fails the build and is named with what it reaches
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:\:AnOutsideTypeNotOnTheList\_FailsTheRuleAndIsNamed

```text
A rule that names the newcomer but whose fix hint gives poor advice; the hint wording is judged by review, not pinned
```

###### Obligations


##### Covers

P1

```text
Add a made-up outside type, not on the list, that calls the Application only after an await inside an async method, and run the rule from a Debug build and from a Release build
```

```text
In both builds the rule fails and names the async-only caller as not on the list, with the Application type it reaches
```

```text
A new caller of the Application written only inside an async method fails the Release build that CI runs, not just the Debug one
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:\:AnOutsideTypeReachingTheApplicationOnlyInsideAnAsyncMethod\_FailsTheRuleAndIsNamed

```text
A reader that walks async state machines but not lambda closures; the design names async methods, and closures are read by the same nested-type walk
```

###### Obligations



##### Covers

P1

```text
List a made-up type that no longer calls the Application, beside the listed caller that still does
```

```text
The rule fails, names the gone type with 'delete', and names no newcomer
```

```text
A caller that stopped using the Application must be deleted from the list, so the list only shrinks
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:\:AListedTypeThatNoLongerReachesTheApplication\_FailsTheRuleAndIsNamedForDeletion

```text
A rule that orders several stale entries differently; only one stale entry is made up
```

###### Obligations


##### Covers

P1

```text
Add the made-up newcomer and list the made-up former caller at the same time
```

```text
One failure names the newcomer as not on the list and the former caller with 'delete'
```

```text
One run shows both changes to who calls the Application
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsInboundApplicationArchUnitTest\.cs\:\:ANewcomerAndAStaleEntryAtOnce\_AreBothNamedInOneFailure

```text
A failure that names both but omits what the newcomer reaches; the single-newcomer cases pin that part
```

###### Obligations


### S3

##### Covers

P1

P2

```text
Read the compiled production assembly and list every edge from a type in the Flow metrics module's Application into EF Core, the data layer, the module's Adapters or the API
```

```text
No such edge exists, and the module's Application holds types, so the rule checked something
```

```text
A maintainer sees that the module's Application depends on no storage, database context, adapter or API type, which covers the transition DbSets the old scanner checked in two services
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs\:\:TheModuleApplication\_DependsOnNoEntityFrameworkCoreDataAdaptersOrApiType

```text
the forbidden pattern leaves out one of the four layers while production happens not to reach it; the made-up offender rows run the same forbidden pattern and catch that
```

###### Obligations


##### Covers

P1

```text
Read the compiled production assembly and list every edge from a type in the module's Application into Forecasting, matched exactly as the module map matches Forecasting
```

```text
The edges equal the inline list of three: BaseMetricsService to IForecastService through the service locator, TeamMetricsService to IForecastFilterRuleService, TeamMetricsService to ForecastDataSufficiencyPolicy
```

```text
A maintainer sees the only three ways the module reaches Forecasting, and any fourth one fails the build
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs\:\:TheModuleApplication\_ReachesForecastingOnlyThroughTheListedEdges

```text
the rule compares dependent types instead of dependent-to-target pairs, so a listed service reaching a new Forecasting type would pass; the second-Forecasting-type row fails that
```

###### Obligations


##### Covers

P1

```text
Run both rules on a made-up Application whose types reach only their own Application and its Policies, a Models type, the base library, the service provider, and Forecasting through the one listed edge
```

```text
The rule below the Application finds no edge and passes; the Forecasting rule finds exactly the listed edge and passes
```

```text
Compliant code never fails the rules, so nobody switches them off
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs\:\:AnApplicationReachingOnlyItsOwnTypesTheModelsAndAListedForecastingEdge\_PassesBothRules

```text
the Forecasting rule reports an edge only once per type although the type reaches the listed target in two methods; the compliant type reaches it in one method only
```

###### Obligations


##### Covers

P1

P2

```text
Run the rule below the Application, with the production forbidden layers, on five made-up Application types: one holding a DbSet of Feature transitions, one reading a DbSet of Work Item transitions only inside an async method, one reaching the database context, one in a Policies sub-namespace of the Application reaching a module adapter, one reaching an API type
```

```text
Each fails, naming its edge as not on the list, and asks to delete nothing
```

```text
A new reach of an Application type into storage, the database context, an adapter or the API fails the build and names the type and what it reaches, async bodies included; a transition DbSet is caught in any Application type, not only in the two services the old scanner named
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs\:\:AnApplicationTypeReachingALayerBelowIt\_FailsTheRuleAndIsNamed

```text
the rule catches a transition DbSet only as a field, a cast or a type token, not as a generic argument of a method it calls; no made-up type reaches it that way
```

###### Obligations


##### Covers

P1

```text
Run the rule below the Application on the compliant made-up Application with one listed edge into EF Core that no longer exists
```

```text
The rule fails, naming that edge with 'delete it', and names no new edge
```

```text
A list entry below the Application that is gone has to be deleted, so the dependency cannot come back unseen
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs\:\:AListedEdgeBelowTheApplicationThatIsGone\_FailsTheRuleAndIsNamedForDeletion

```text
the rule treats a listed edge as stale only when its dependent reaches nothing forbidden any more; the made-up former user reaches nothing, so a dependent that still reaches a different forbidden type is not made up
```

###### Obligations


##### Covers

P1

```text
Run the Forecasting rule on a made-up Application type that is listed for IForecastService and also reaches IForecastFilterRuleService
```

```text
The rule fails, naming only the new edge to IForecastFilterRuleService as not on the list, and asks to delete nothing
```

```text
A listed service that starts using one more Forecasting type fails the build, not only a new service
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs\:\:AListedTypeReachingASecondForecastingType\_FailsTheRuleAndNamesOnlyTheNewEdge

```text
the rule compares pairs but drops a target reached through a constructor parameter that is never used in a method body; the made-up type uses both in a method
```

###### Obligations


##### Covers

P1

```text
Run the Forecasting rule on a made-up Application type that asks the service provider for IForecastService only inside an async method, with an empty list
```

```text
The rule fails, naming the edge to IForecastService as not on the list, in Debug and in Release builds
```

```text
A Forecasting edge written only inside an async method cannot slip past the rule in the Release build CI tests
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs\:\:ATypeReachingForecastingOnlyInsideAnAsyncMethod\_FailsTheRuleAndIsNamed

```text
the async edge is caught only because the type also reaches Forecasting outside the async method; the made-up type holds only a service provider outside it
```

###### Obligations


##### Covers

P1

```text
Run the Forecasting rule on the compliant made-up Application with the live edge and one gone edge listed
```

```text
The rule fails, naming only the gone edge with 'delete it', and names no new edge
```

```text
A Forecasting edge that was removed has to come off the list, so the list only shrinks
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs\:\:AListedForecastingEdgeThatIsGone\_FailsTheRuleAndIsNamedForDeletion

```text
a gone edge whose dependent still reaches a different Forecasting type is reported as stale only together with a newcomer; that combined case is not made up here
```

###### Obligations


##### Covers

P1

P2

```text
Run both rules on a made-up Application namespace that holds no type
```

```text
Both rules fail and say that no type lives in that namespace, so the rule checked nothing
```

```text
If the module's Application is renamed or emptied, the rules fail instead of passing while they guard nothing; the module is found by its namespace
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs\:\:AnApplicationThatHoldsNoType\_FailsBothRules

```text
only an empty namespace is guarded, not a namespace that holds types with no edge at all; that case is the compliant row for the rule below, which must pass
```

###### Obligations


##### Covers

P2

```text
Look in the test assembly for the old scanner test by name, list what the Metrics rules class reaches of raw method bodies and EF Core, and list which of its two name rules are still tests
```

```text
No test has the old scanner's name, the Metrics rules reach neither raw method bodies nor EF Core, and both name rules are still tests
```

```text
The hand-written opcode scanner and its transition DbSet list are gone, and the two rules about the shared per-state helper remain
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsApplicationOutboundArchUnitTest\.cs\:\:TheHandWrittenScannerOfTransitionReads\_IsGone\_AndTheTwoNameRulesOfTheMetricsServicesStay

```text
the scanner is kept under a new name in a different class; the rule below the Application then makes it redundant but not absent
```

###### Obligations


##### Covers

P2

```text
Check that the module map's non-empty canary names the five Flow metrics namespaces, and that its Forecasting pattern is visible to the other architecture tests
```

```text
The canary fails if any of Domain, Application, Application.Policies, Adapters.Background or Adapters.Persistence holds no type
```

```text
A renamed or emptied module namespace fails the canary
```

##### Disposition

NotTested

```text
The change is a list edit inside another test's assertions; a test can only see it by reading that test's source or the strings in its IL, which pins wording, not behaviour. Partly observed: the Application's emptiness fails both outbound rules here, and this oracle reads the Forecasting pattern by name so it compiles before and after the pattern is made internal
```

independent reviewer of the S3 candidate diff

### S4

##### Covers

P3

```text
Look up each of the 33 listed Flow metrics unit-test files in the test project's source tree and its classes in the compiled test assembly
```

```text
Each file sits only in the Flow metrics module test folder of its layer (Domain, Application, Application Policies, Background adapters or Persistence adapters), no copy is left in its old place, and every class compiled from it lives in that layer's test namespace
```

```text
A developer finds every Flow metrics unit test next to the module, in the layer of the code it tests
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.FlowMetricsUnitTestPlacementTest\.AFlowMetricsUnitTestFile\_SitsOnlyInItsModuleLayerFolder\_AndItsClassesLiveInThatLayer

```text
a listed test file declares only a top-level enum, interface or delegate with no method body, which the symbols cannot tie to a file; or a test file that is not on the list of 33 stays under Services
```

###### Obligations

None.

##### Covers

P3

```text
Read every class the compiled test assembly ties to a file under the Flow metrics module test folder
```

```text
At least one class is found there, and each one's namespace is the namespace its folder gives
```

```text
A test's namespace tells a developer and a test filter exactly where its file sits
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.FlowMetricsUnitTestPlacementTest\.EveryClassUnderTheFlowMetricsModuleTestFolder\_HasTheNamespaceOfItsFolder

```text
the module test folder holds a type with no method body whose namespace disagrees with its folder; or a test class sits in the module folder although its subject is not a module type
```

###### Obligations

None.

##### Covers

P3

```text
Run the file placement rule on made-up test trees: a file left in its old place, a moved file with a copy left behind, a file moved into the wrong layer folder, and a file moved into its layer folder next to a neighbour
```

```text
The first three each name the missing layer path and every other path that still holds the file; the compliant tree reports nothing
```

```text
An unfinished or wrong move cannot pass unseen, and a finished move is not flagged
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.FlowMetricsUnitTestPlacementTest\.AMadeUpTestFileOutsideItsLayerFolder\_IsReported\_AndOneThatSitsThereIsNot

```text
the rule compares paths case-sensitively, so a file whose folder differs only in letter case would be reported as missing on a case-insensitive disk rather than accepted
```

###### Obligations

None.

##### Covers

P3

```text
Run the namespace and folder rule on the classes compiled from the placement test's own file, which holds a made-up class whose namespace names the module Domain test folder and a compliant twin in the folder's own namespace
```

```text
Exactly the made-up class is reported, naming its namespace, its file and the namespace its folder gives; the twin and the placement test class are not reported
```

```text
A test class whose namespace disagrees with its folder is named, and a correct one is not
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.FlowMetricsUnitTestPlacementTest\.AMadeUpClassWhoseNamespaceDoesNotMatchItsFolder\_IsReported\_AndItsCompliantTwinIsNot

```text
the folder namespace is derived from the path under the test project with a fixed root namespace, so a rule that ignored the root and compared only the trailing folder names would also pass
```

###### Obligations

None.

##### Covers

P3

```text
Run the whole backend test suite without the live tracker categories, before and after the move
```

```text
Every test passes and the number of tests is the same after the move as before it, apart from the placement tests added with this oracle
```

```text
Nothing a user sees changes: every existing test still runs and passes
```

##### Disposition

Tested

dotnet test Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Lighthouse\.Backend\.Tests\.csproj \-\-filter TestCategory\!\=JiraIntegration\&TestCategory\!\=LinearIntegration\&TestCategory\!\=AdoIntegration\&TestCategory\!\=ServiceNowIntegration\&TestCategory\!\=GithubIntegration

```text
a test row inside a moved class is replaced by another with the same count, which neither the count nor the placement test sees
```

###### Obligations

None.

##### Covers

P3

```text
Compare each moved file with its original, and check that no production file and no EF model file changed
```

```text
Each move is a rename whose only edits are the namespace line and using lines; class names, test bodies, production code and the EF model are unchanged
```

```text
The move changes where the tests live and nothing about what they check or what the product does
```

##### Disposition

NotTested

```text
No declared architecture or contract locator observes the content of a diff; pinning file text would couple the oracle to every line of 33 files
```

slice reviewer\, reading git diff \-M of the slice commit

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Adding a type that reaches into the Flow metrics module from outside its allowed edges, or leaving a listed exception that no longer exists, makes the architecture checks fail and name the type, including when the dependency is used only inside an async method; the module's Domain, Adapters and Application each accept only their listed callers and its Application stays free of EF Core, Data, Adapters and API types.
```

#### E2

##### Promises

P2

```text
The old Metrics type-set rule and the hand-written scanner no longer exist, the Flow metrics module is recognised by its namespaces, and a dependency the old checks would have caught still makes the architecture checks fail.
```

#### E3

##### Promises

P3

```text
The product behaves exactly as before: the full existing test suite passes, the Flow metrics unit tests live under Lighthouse.Backend.Tests/Modules/FlowMetrics with only folder, namespace and using changes, and the database model shows no change.
```

## Charter

None.

## Examination

None.
