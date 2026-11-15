# Acceptance brief: story\-6361\-s03c\-c\-cycle\-and\-rules

## Slices

### S1

##### Covers

P1

P5

```text
An examiner runs the public forecast-triggers-show command. It loads the smallest demo scenario with Teams and Portfolios into a fresh backend, picks a Team that serves at least one Portfolio, notes the time and asks for a refresh of that Team.
```

```text
After the Team refresh the refresh history holds a new Forecast row for every Portfolio the Team serves, executed after the request. The command prints one line that names the Team, the Portfolio ids, the request time and each row's time, marked OBSERVED. A missing row prints NOT OBSERVED with what is missing, why it matters and how to rerun, and the command exits non-zero.
```

```text
The examiner sees that a Team refresh still makes the forecasts of every Portfolio that Team serves compute again.
```

##### Disposition

Tested

Scripts\/examine\/forecast\-triggers\-show\.cs

```text
A Forecast row for the Portfolio that some other path writes shortly after the Team refresh, while the Team-refresh trigger itself is gone. On main only that trigger writes the row: with its registration removed the line went NOT OBSERVED.
```

###### Obligations

None.

##### Covers

P1

P5

```text
The same command then asks every Portfolio to forecast on unchanged data by sending the Feature-order setting back as it is, a request that does not forget what the stored forecasts were computed from, unlike every refresh button. It then stops the backend, starts a new one on the same database and asks the same again.
```

```text
The Portfolio's round before the restart is confirmed unchanged (Skipped=true). The first round after the restart computes again (Skipped=false), and no Team or Portfolio refresh ran between the two rounds. The line names the Portfolio, both rows' times and Skipped values, the restart time and the refreshes in between, marked OBSERVED. Any other combination, a missing row or a refused request prints NOT OBSERVED with why and how to rerun, and the command exits non-zero. The setting is premium, so this needs the backend tests' premium licence file.
```

```text
The examiner sees that after a restart the forecasts compute again instead of trusting inputs recorded before the stop, even on data a forecast just before the stop found unchanged.
```

##### Disposition

Tested

Scripts\/examine\/forecast\-triggers\-show\.cs

```text
Something other than the start-up seeding that makes every first round after a start compute again, for example inputs that differ per process. On main, with the seeding's registration removed, the first round after the restart was Skipped=true in 5 of 6 runs and the line went NOT OBSERVED. The one run that stayed OBSERVED was the first build after the edit, with no refresh check in the probe at that time; the probe now refuses OBSERVED when a refresh runs between the rounds.
```

###### Obligations

None.

### S2

##### Covers

P2

```text
Read the compiled production code and look for any Flow metrics type that uses a Forecasting type, with no list of allowed exceptions
```

```text
No Flow metrics type uses a Forecasting type; today the rule fails and names the three uses: the base metrics service on the forecast service, and the Team metrics service on the forecast filter rule service and on the forecast data sufficiency policy
```

```text
Flow metrics no longer depends on Forecasting, so the two modules no longer form a cycle
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsDoesNotReachForecastingArchUnitTest\.cs\:\:FlowMetrics\_ReachesNoForecastingType

```text
A Flow metrics type reaches Forecasting only through a type the IL reader cannot see (a catch clause, an event or a named attribute argument)
```

###### Obligations


##### Covers

P2

```text
Read the compiled production code for the three questions Flow metrics asks Forecasting: the throughput filter, the forecast data sufficiency and the how-many forecaster
```

```text
Each is an interface in Flow metrics' Application and a Forecasting type implements each; today the rule fails and names all three as missing
```

```text
Flow metrics owns the questions it asks, in its own words, and Forecasting is the one that answers them
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsDoesNotReachForecastingArchUnitTest\.cs\:\:TheThreeQuestionsFlowMetricsAsks\_AreItsOwnPorts\_AndForecastingAnswersEach

```text
A port carries a member that is never called, or the Forecasting answer is registered with a different lifetime; those are left to the answer comparisons and the review
```

###### Obligations


##### Covers

P2

```text
Run the same no-reach rule over made-up modules: a metrics type taking a Forecasting service in its constructor, one calling a static Forecasting policy, and one resolving a Forecasting service from the service provider
```

```text
Each fails and is named with the Forecasting type it reaches
```

```text
Any of the three ways the cycle could come back is caught and named
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsDoesNotReachForecastingArchUnitTest\.cs\:\:AMetricsTypeReachingForecasting\_FailsAndIsNamedWithWhatItReaches

```text
A rule that names the edge but also reports an unrelated second edge in the same message
```

###### Obligations


##### Covers

P2

```text
Run the port rule over a made-up module whose port is declared in Forecasting instead of Flow metrics' Application
```

```text
The rule fails and names where the port actually lives
```

```text
A port that moves back into Forecasting is caught and named
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsDoesNotReachForecastingArchUnitTest\.cs\:\:APortDeclaredInForecastingInsteadOfFlowMetrics\_FailsAndIsNamedWithWhereItLives

```text
A port declared in a sub-namespace of Flow metrics' Application is refused although it is still Flow metrics' own; the design places the ports in the Application namespace itself
```

###### Obligations


##### Covers

P5

```text
Capture the metrics answers on demo data before and after the change and compare them
```

```text
Every metrics answer is identical, including the forecast predictability score that uses the how-many forecast and the forecast throughput status that uses the filter and the sufficiency check
```

```text
Users see the same metrics after the change
```

##### Disposition

Tested

Scripts\/examine\/bin\/metrics\-answers\-compare

```text
A difference only in an answer the capture does not ask for
```

###### Obligations

None.

##### Covers

P5

```text
Capture the forecasts and forecast answers before and after the change, and compute the seeded forecasts and the Flow metrics unit and filter-forecast tests
```

```text
Forecasts and forecast answers are identical, the seeded forecasts keep their pinned values, and the Flow metrics and filter-forecast tests stay green unchanged in their assertions
```

```text
Users get the same forecasts after the change
```

##### Disposition

Tested

Scripts\/examine\/bin\/forecast\-answers\-compare

```text
A difference only for an input neither the capture nor the seeded set covers
```

###### Obligations

None.

##### Covers

P5

```text
On demo data, refresh a Team and restart the application, then read the forecasts through the public API
```

```text
Forecasts are recomputed after the Team refresh and seeded at start-up, as before the change
```

```text
Forecasts still refresh after a Team refresh and at start-up
```

##### Disposition

Tested

Scripts\/examine\/bin\/forecast\-triggers\-show

```text
A trigger fires but recomputes with a stale answer; the answer comparisons cover the values
```

###### Obligations

None.

##### Covers

P2

```text
Run both rules over a made-up module whose metrics type asks only through its own port, which a made-up Forecasting type implements
```

```text
The no-reach rule looks at the metrics type, finds no edge and passes; the port rule passes
```

```text
Asking Forecasting through a port Flow metrics owns stays allowed
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsDoesNotReachForecastingArchUnitTest\.cs\:\:AMetricsTypeAskingOnlyThroughItsOwnPort\_PassesBothRules

```text
A rule that counts implementing a metrics port as reaching Forecasting would fail here, so only an over-lenient rule passes; the failing cases pin that side
```

###### Obligations


##### Covers

P2

```text
Run the port rule over a made-up module whose port only a Flow metrics type implements, with a Forecasting type present that answers something else
```

```text
The rule fails and names the Flow metrics type that answers the port instead of Forecasting
```

```text
An answer copied into Flow metrics instead of asked of Forecasting is caught and named
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsDoesNotReachForecastingArchUnitTest\.cs\:\:APortThatOnlyFlowMetricsAnswers\_FailsAndIsNamedWithWhoAnswersIt

```text
Forecasting implements the port but the composition root registers a Flow metrics copy instead; the answer comparisons cover that
```

###### Obligations


### S3

##### Covers

P3

```text
Read the compiled production code for every outside type that reaches Forecasting's Domain
```

```text
Only the composition root reaches it, as the inline list says; today this already holds
```

```text
Forecasting's calculations can change without breaking code outside the module
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:\:TheForecastingDomain\_IsReachedFromOutsideOnlyByTheListedTypes

```text
An outside type reaches the Domain only through a catch clause, an event or a named attribute argument, which the IL reader cannot see
```

###### Obligations


##### Covers

P3

```text
Read the compiled production code for every outside type that reaches Forecasting's Adapters
```

```text
Only the composition root reaches them; today this already holds
```

```text
Nothing outside Forecasting depends on how it stores data or runs in the background
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:\:TheForecastingAdapters\_AreReachedFromOutsideOnlyByTheCompositionRoot

```text
An outside type reaches an adapter only through a type the IL reader cannot see
```

###### Obligations


##### Covers

P3

```text
Read the compiled production code for every outside type that reaches Forecasting's Application, with Forecasting's own wording
```

```text
Only the API, the composition root, Refinement and the listed Portfolio updater reach it; today the rule fails because it cannot take Forecasting's wording yet
```

```text
Forecasting's front door is reached only by its legal callers, and a new caller is caught
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:\:TheForecastingApplication\_IsReachedFromOutsideOnlyByTheApiTheCompositionRootRefinementAndTheListedTypes

```text
A new caller sits in a Refinement or API namespace while doing other work, so it is legal by namespace
```

###### Obligations


##### Covers

P3

```text
Read the compiled production code for every edge from Forecasting's Application into EF Core, the data layer, the API, its own Adapters or the licence
```

```text
Only the filter rule service's edge to the licence service, as listed; today this already holds
```

```text
Forecast answers do not depend on storage or presentation, and the licence is asked in one place
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:\:TheForecastingApplication\_ReachesNoEntityFrameworkCoreDataApiOrModuleAdapterType\_AndTheLicenceOnlyThroughTheListedEdges

```text
A listed type reaches the same licence service from a second method
```

###### Obligations


##### Covers

P3

```text
Read the compiled production code for every edge from Forecasting's Adapters into the update code it shares with the Team and Portfolio updaters
```

```text
Only the forecast updater's edges to the updater base class, the update key and the update cancellation context, as listed; today it fails naming an edge to the base class's type parameter, which is a placeholder and not a dependency
```

```text
Forecasting's background work does not tie itself tighter to the shared update code
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:\:TheForecastingAdapters\_ReachTheSharedUpdateCodeOnlyThroughTheListedEdges

```text
The forecast updater uses a listed update type in more places
```

###### Obligations


##### Covers

P3

```text
Read Flow metrics' inbound list and apply Flow metrics' Application rule with Forecasting as a legal caller
```

```text
The list names no Forecasting type and still matches what reaches Flow metrics both ways; today it fails naming seven Forecasting entries
```

```text
Flow metrics' list shows only the callers still to remove, not Forecasting, which may call Flow metrics
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:\:TheFlowMetricsInboundList\_NamesNoForecastingType\_AndStillMatchesWhatReachesFlowMetricsFromOutside

```text
The list is renamed so the check cannot read it, which fails loudly rather than passing
```

###### Obligations


##### Covers

P3

```text
Run Forecasting's Domain, Adapters and Application rules over a made-up module where only allowed types reach each layer
```

```text
Each rule finds exactly the allowed types and passes
```

```text
The rules do not fail on compliant code
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:\:OnlyTheAllowedOutsideTypesReachingTheLayer\_PassForecastingsRule

```text
A rule that also ignores the composition root's nested types for Domain and Adapters
```

###### Obligations


##### Covers

P3

```text
Run Forecasting's Domain, Adapters and Application rules over a made-up module with an outside newcomer reaching each layer, the Application one only inside an async method
```

```text
Each rule fails and names the newcomer and what it reaches
```

```text
A new dependency on Forecasting is caught and named
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:\:AnOutsideTypeNotOnTheList\_FailsForecastingsRuleAndIsNamedWithWhatItReaches

```text
A newcomer reaching the layer only through a type the IL reader cannot see
```

###### Obligations


##### Covers

P3

```text
Run Forecasting's Domain, Adapters and Application rules over a made-up module whose list keeps a type that no longer reaches the layer
```

```text
Each rule fails and names the gone entry for deletion
```

```text
Forecasting's lists only shrink
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:\:AListedTypeThatNoLongerReachesTheLayer\_FailsForecastingsRuleAndIsNamedForDeletion

```text
A gone entry repeated twice in the list, which is named once
```

###### Obligations


##### Covers

P3

```text
Run Forecasting's Domain, Adapters and Application rules with Forecasting's wording over a made-up module with a newcomer
```

```text
No failure speaks of Flow metrics, and the Application failure names Forecasting's legal callers and not Reports; today the Application rule cannot take a wording
```

```text
A failing rule sends the reader to the module it checks
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:\:AFailingForecastingRule\_SpeaksOfForecastingAndNotOfFlowMetrics

```text
The Domain and Adapters texts name the module only through its namespace pattern
```

###### Obligations


##### Covers

P3

```text
Run the shared Application rule without a wording over a made-up module with an unlisted caller
```

```text
The failure speaks of Flow metrics, whose wording is the default; today it does not
```

```text
Flow metrics' rule tests keep their meaning without an edit
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:\:AFailingFlowMetricsApplicationRule\_SpeaksOfFlowMetricsByDefault

```text
The default wording names Flow metrics but lists the wrong legal callers
```

###### Obligations


##### Covers

P3

```text
Run Forecasting's Application outbound rule over a made-up Application that reaches only the Models and a listed licence edge
```

```text
The rule finds exactly that edge and passes
```

```text
The outbound rule does not fail on compliant code
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:\:AForecastingApplicationReachingOnlyItsModelsAndAListedLicenceEdge\_PassesTheOutboundRule

```text
A rule that ignores Models types entirely
```

###### Obligations


##### Covers

P3

```text
Run Forecasting's Application outbound rule over made-up Application types holding an EF Core DbSet, running a module adapter, or a listed type reaching a second licence type
```

```text
Each fails and names only its new edge
```

```text
Forecasting's Application cannot start depending on storage, its adapters or more of the licence unseen
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:\:AForecastingApplicationTypeReachingALayerBelowOrAnUnlistedLicenceType\_FailsAndIsNamed

```text
An Application type reaching the data layer or the API, which share the rule but have no made-up case
```

###### Obligations


##### Covers

P3

```text
Run Forecasting's Application outbound rule with a listed licence edge that is gone
```

```text
The rule fails and names the gone edge for deletion
```

```text
The licence list only shrinks
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:\:AListedLicenceEdgeOfTheForecastingApplicationThatIsGone\_FailsAndIsNamedForDeletion

```text
A gone edge listed alongside a new one, reported together
```

###### Obligations


##### Covers

P3

```text
Run Forecasting's Adapters outbound rule over a made-up adapter reaching the shared update code only through a listed edge
```

```text
The rule finds exactly that edge and passes
```

```text
The rule does not fail on compliant background code
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:\:ForecastingAdaptersReachingTheSharedUpdateCodeOnlyThroughAListedEdge\_PassTheOutboundRule

```text
A rule that only looks at type references and not method calls
```

###### Obligations


##### Covers

P3

```text
Run Forecasting's Adapters outbound rule over a made-up adapter that also reaches a second type of the shared update code
```

```text
The rule fails and names only the new edge
```

```text
A new edge into the shared update code is caught
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:\:AForecastingAdapterReachingASecondTypeOfTheSharedUpdateCode\_FailsAndNamesOnlyTheNewEdge

```text
A new edge through a type the IL reader cannot see
```

###### Obligations


##### Covers

P3

```text
Run Forecasting's Adapters outbound rule with a listed edge into the shared update code that is gone
```

```text
The rule fails and names the gone edge for deletion
```

```text
The shared update code list only shrinks
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:\:AListedEdgeOfTheForecastingAdaptersIntoTheSharedUpdateCodeThatIsGone\_FailsAndIsNamedForDeletion

```text
A gone edge repeated in the list
```

###### Obligations


##### Covers

P5

```text
Compute the seeded forecasts after the slice, which changes only test code
```

```text
The forecasts are the same as before
```

```text
Forecasts the user sees do not change
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/SeededForecastsStayTheSameTest\.cs

```text
A change to a forecast path the seeded data does not exercise
```

###### Obligations

None.

##### Covers

P3

```text
Run Forecasting's Adapters outbound rule over a made-up adapter built on a generic base class in the shared update code, calling a base member whose signature uses the base's type parameter
```

```text
The rule finds only the edge to the base class, never one to its type parameter, and passes with that edge listed; today it also reports the type parameter
```

```text
Renaming a type parameter in the shared update code cannot break Forecasting's list
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingModuleBoundariesArchUnitTest\.cs\:\:AForecastingAdapterBuiltOnAGenericBaseOfTheSharedUpdateCode\_HasAnEdgeToTheBaseOnly\_NotToItsTypeParameter

```text
The rule skips a generic parameter only when it is the base class's, and still reports one of a generic method or a generic interface
```

###### Obligations


### S4

##### Covers

P4

P5

```text
For each of the 24 Forecasting unit-test files with the layer of its subject, look for the file on disk and for the classes compiled from it
```

```text
Each file sits only in Modules/Forecasting/<its layer>, no copy or old file remains elsewhere, its class keeps the file's name and every class in it has the namespace of that layer folder; otherwise the failure names the file, the path still holding it and each class with a stale namespace or missing name
```

```text
A maintainer finds every Forecasting unit test next to the module, in the layer of what it tests, under the same class name as before
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.ForecastingUnitTestPlacementTest\.AForecastingUnitTestFile\_SitsOnlyInItsModuleLayerFolder\_AndKeepsItsClassInThatLayer

```text
the file sits in the right layer folder with the right namespace but its test methods were renamed or deleted, which the moved tests' own run and review must catch
```

###### Obligations


##### Covers

P4

```text
Look for any test file in the folder the Forecasting tests left, and run the same rule over made-up test trees: a newcomer there, a newcomer in a subfolder of it, and tests only in other folders, one of them in a folder whose name starts the same
```

```text
The real folder holds no test file; the made-up newcomers are each named and the trees with tests only elsewhere report nothing
```

```text
A Forecasting test written into the old folder after the move is caught by name instead of slipping back outside the module
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.ForecastingUnitTestPlacementTest\.TheFolderTheForecastingTestsLeft\_HoldsNoTestFile

```text
a new Forecasting test is put in a folder other than the one the tests left, for example next to the shared update tests, and is not on the list
```

###### Obligations


##### Covers

P4

```text
Collect every compiled test class from a file under Modules/Forecasting or declared in its namespace
```

```text
The module test folder holds at least one class and every one has the namespace of its folder; otherwise the failure names the empty folder or each class whose namespace differs from its folder
```

```text
An empty module test folder fails instead of passing, and a test class that claims the module namespace from outside the folder is named
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.ForecastingUnitTestPlacementTest\.EveryClassOfTheForecastingModuleTests\_HasTheNamespaceOfItsFolder

```text
a class is placed under Modules/Forecasting in the wrong layer folder with that wrong folder's namespace, which only the listed-file check catches
```

###### Obligations


##### Covers

P4

```text
Run the file placement rule over made-up test trees for one made-up test file: left in its old place, moved with a copy left behind, moved into the wrong layer, and moved into its layer beside a neighbour, a shared update test and an integration test
```

```text
The first three report the missing layer path and every other path holding the file; the compliant tree reports nothing, so the shared update tests and integration tests that stay where they are are not flagged
```

```text
An unfinished or wrong move is named by path, and a finished move with the shared tests left in place is not
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.ForecastingUnitTestPlacementTest\.AMadeUpTestFileOutsideItsLayerFolder\_IsReported\_AndOneThatSitsThereIsNot

```text
a same-named file differs only in letter case on a case-insensitive file system
```

###### Obligations


##### Covers

P4

P5

```text
Run the class rule over made-up classes compiled from one made-up test file: its class kept in its layer namespace with a helper beside it, its class in a stale namespace, its class renamed, and no class at all
```

```text
The kept case reports nothing; the stale namespace reports that class and the missing expected class name; the renamed case reports the missing expected class name; the empty case reports the file
```

```text
A moved test keeps its name, so a filter or a reader finds it under the same name, and a test that lost its name or namespace in the move is named
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.ForecastingUnitTestPlacementTest\.AMadeUpTestFileWhoseClassLostItsNameOrLayerNamespace\_IsReported\_AndOneThatKeptThemIsNot

```text
the class keeps its name but its test methods are renamed
```

###### Obligations


##### Covers

P4

```text
Read the made-up classes compiled from the placement test's own file: a class in a made-up module layer namespace whose file sits outside that module's folder, and a compliant twin
```

```text
Both are tied to the file; only the made-up newcomer is reported as a namespace that does not match its folder, and it is taken as a test of that module though its file is outside the module folder
```

```text
A newcomer declared in the module namespace from outside the module folder is caught, and a compliant class is not, so the rule passing on the real tree means something
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.ForecastingUnitTestPlacementTest\.AMadeUpClassDeclaredInTheModuleNamespaceOutsideItsFolder\_IsReported\_AndItsCompliantTwinIsNot

```text
the symbols tie a class to a generated file outside the test project, which the scan leaves out
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
A user running the public command on demo data sees that forecasts are recomputed after a Team refresh, and that the start-up forecast seeding fires.
```

#### E2

##### Promises

P2

```text
Flow metrics code depends on no Forecasting type; every question it puts to Forecasting goes through exactly three Flow-metrics-owned ports that Forecasting answers.
```

#### E3

##### Promises

P3

```text
Forecasting has its own module rules with inline lists, Flow metrics' inbound rule list names no Forecasting type, and the shared rule's wording names the module it checks.
```

#### E4

##### Promises

P4

```text
Forecasting's unit tests sit under Lighthouse.Backend.Tests/Modules/Forecasting, and a placement test fails if they are moved elsewhere.
```

#### E5

##### Promises

P5

```text
For the same inputs, metrics answers, forecasts and forecast answers are identical before and after the change.
```

#### E6

##### Promises

P5

P1

```text
After the change, forecasts still refresh after a Team refresh and at start-up.
```

## Charter

None.

## Examination

### E1

met

#### Observations

```text
forecast-triggers-show on demo data: (1) Team 1 Team Zenith, Portfolio 1 Forecast row at 08:53:40.056Z after refresh at 08:53:39.270Z OBSERVED; (2) unchanged round Skipped=true, after restart Skipped=false OBSERVED; VERDICT OBSERVED.
```

### E2

met

#### Observations

```text
architecture-rules probe: 765 tests passed 0 failed, incl. TheThreeQuestionsFlowMetricsAsks_AreItsOwnPorts_AndForecastingAnswersEach, FlowMetrics inbound/outbound lists name no Forecasting type, Forecasting module rules (Domain/Adapters/Application inbound and outbound, listed-edge-gone, second-edge made-ups), AFailingForecastingRule_SpeaksOfForecastingAndNotOfFlowMetrics, AFailingFlowMetricsApplicationRule_SpeaksOfFlowMetricsByDefault, placement tests (Forecast services, updater, seeder, triggers sit in module folders and nowhere else; empty Domain fails not vacuous). A placement test named for the Forecasting unit-test folder Tests/Modules/Forecasting was not seen by name in my grep of the output.
```

### E3

met

#### Observations

```text
architecture-rules probe: 765 tests passed 0 failed, incl. TheThreeQuestionsFlowMetricsAsks_AreItsOwnPorts_AndForecastingAnswersEach, FlowMetrics inbound/outbound lists name no Forecasting type, Forecasting module rules (Domain/Adapters/Application inbound and outbound, listed-edge-gone, second-edge made-ups), AFailingForecastingRule_SpeaksOfForecastingAndNotOfFlowMetrics, AFailingFlowMetricsApplicationRule_SpeaksOfFlowMetricsByDefault, placement tests (Forecast services, updater, seeder, triggers sit in module folders and nowhere else; empty Domain fails not vacuous). A placement test named for the Forecasting unit-test folder Tests/Modules/Forecasting was not seen by name in my grep of the output.
```

### E4

met

#### Observations

```text
Same architecture-rules output: 24 passing AForecastingUnitTestFile_SitsOnlyInItsModuleLayerFolder_AndKeepsItsClassInThatLayer cases (e.g. AddressableDrawStreamTest Domain, ForecastBeyondTheHorizonTest Application); 24 files tracked under Lighthouse.Backend.Tests/Modules/Forecasting. Supersedes the caution in O2 about not seeing a Forecasting unit-test placement test.
```

```text
architecture-rules probe: 765 tests passed 0 failed, incl. TheThreeQuestionsFlowMetricsAsks_AreItsOwnPorts_AndForecastingAnswersEach, FlowMetrics inbound/outbound lists name no Forecasting type, Forecasting module rules (Domain/Adapters/Application inbound and outbound, listed-edge-gone, second-edge made-ups), AFailingForecastingRule_SpeaksOfForecastingAndNotOfFlowMetrics, AFailingFlowMetricsApplicationRule_SpeaksOfFlowMetricsByDefault, placement tests (Forecast services, updater, seeder, triggers sit in module folders and nowhere else; empty Domain fails not vacuous). A placement test named for the Forecasting unit-test folder Tests/Modules/Forecasting was not seen by name in my grep of the output.
```

### E5

met

#### Observations

```text
metrics-answers-compare base 7ec328cb vs candidate d6838038: IDENTICAL, 366 responses and 3 snapshot tables. forecast-answers-compare: IDENTICAL, all 128 forecast answers match, 0 different per family.
```

### E6

met

#### Observations

```text
forecast-triggers-show on demo data: (1) Team 1 Team Zenith, Portfolio 1 Forecast row at 08:53:40.056Z after refresh at 08:53:39.270Z OBSERVED; (2) unchanged round Skipped=true, after restart Skipped=false OBSERVED; VERDICT OBSERVED.
```

```text
metrics-answers-compare base 7ec328cb vs candidate d6838038: IDENTICAL, 366 responses and 3 snapshot tables. forecast-answers-compare: IDENTICAL, all 128 forecast answers match, 0 different per family.
```
