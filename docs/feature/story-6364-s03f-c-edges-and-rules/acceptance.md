# Acceptance brief: story\-6364\-s03f\-c\-edges\-and\-rules

## Slices

### S1

##### Covers

P1

```text
The layout rule reads the compiled code for every type the platform membership map lists, area by area
```

```text
Each listed type is found in its Platform area and no type of that name is left under Services
```

```text
A maintainer sees the platform lane's layout check pass on today's code, and fail naming the type and its Platform area once one is put back under Services
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundariesArchUnitTest\.cs\:\:EveryTypeTheMapListsForThePlatformLane\_LivesInItsPlatformArea\_AndNoneIsLeftUnderServices

```text
A listed type also present as an identical copy outside both Services and Platform; the rule reads only the expected Platform name and the Services namespace
```

###### Obligations


##### Covers

P2

```text
The no-API rule reads every type of the platform lane in the compiled code
```

```text
No platform lane type reaches an API type; the empty list matches
```

```text
A maintainer sees that platform code reaching a controller or API contract cannot go in unseen
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundariesArchUnitTest\.cs\:\:ThePlatformLane\_ReachesNoApiType

```text
A lane reach into the API made only through reflection or dependency injection, which the compiled references do not carry
```

###### Obligations


##### Covers

P2

```text
The no-adapters rule reads every type of the platform lane in the compiled code
```

```text
No platform lane type reaches any context's Adapters; the empty list matches
```

```text
A maintainer sees that platform code reaching a context's storage or background adapters cannot go in unseen
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundariesArchUnitTest\.cs\:\:ThePlatformLane\_ReachesNoContextsAdapters

```text
A lane reach into an adapter made only through reflection or dependency injection
```

###### Obligations


##### Covers

P1

```text
The layout rule is handed a made-up listed type that sits only under the made-up Services namespace
```

```text
It fails in the platform lane's wording and names the type as left behind instead of moving to its expected Platform area
```

```text
A maintainer who leaves a listed type under Services is told which type and which Platform area it belongs in
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundariesArchUnitTest\.cs\:\:AMadeUpListedTypeLeftUnderServices\_FailsTheLayoutRule\_NamingItsExpectedArea

```text
A rule that also named an unrelated same-named type outside Services as left behind
```

###### Obligations


##### Covers

P1

```text
The layout rule is handed a made-up listed type that sits in another Platform area than the one it expects
```

```text
It fails in the platform lane's wording, naming the expected full name in its Platform area as missing, and names nothing as left behind
```

```text
A maintainer who files a listed type in the wrong Platform area is told where it is expected
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundariesArchUnitTest\.cs\:\:AMadeUpListedTypeInAnotherArea\_FailsTheLayoutRule\_NamingItMissingFromItsExpectedArea

```text
A rule that would also accept the type in a nested namespace below its expected area
```

###### Obligations


##### Covers

P2

```text
The no-API rule runs over a made-up lane type that reaches a made-up API type
```

```text
It fails in the platform lane's wording, naming no module, and names the edge from the lane type to the API type as not on the list
```

```text
A maintainer whose platform code reaches the API is told the exact edge to undo
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundariesArchUnitTest\.cs\:\:AMadeUpLaneTypeReachingAnApiType\_FailsTheNoApiRule\_NamingTheEdge

```text
A reach into the API only inside an async method, which this made-up case does not exercise but the shared rule's own falsifiers do
```

###### Obligations


##### Covers

P2

```text
The no-adapters rule runs over a made-up lane type that reaches a made-up context's adapter
```

```text
It fails in the platform lane's wording, naming no module, and names the edge to the adapter as not on the list
```

```text
A maintainer whose platform code reaches a context's adapter is told the exact edge to undo
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundariesArchUnitTest\.cs\:\:AMadeUpLaneTypeReachingAContextsAdapter\_FailsTheNoAdaptersRule\_NamingTheEdge

```text
A pattern that matched only one named context's Adapters, since the made-up case has a single context
```

###### Obligations


##### Covers

P3

```text
The Services rule runs over a made-up lane type whose listed edge stays and whose nested type adds an unlisted Services edge
```

```text
It fails in the platform lane's wording and names only the nested type's new edge as not on the list, asking to delete nothing
```

```text
A maintainer whose platform code takes a new dependency on Services code is told the exact new edge
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundariesArchUnitTest\.cs\:\:AMadeUpLaneTypeReachingAnUnlistedServicesType\_FailsTheServicesRule\_NamingItNotOnTheList

```text
A rule that reported the nested newcomer under its outer type's name
```

###### Obligations


##### Covers

P3

```text
For each of the 19 positions of the measured list, the Services rule runs over the real code with that one entry removed
```

```text
Each run fails naming exactly the removed edge as not on the list, and nothing as no longer offending
```

```text
A maintainer can see that every listed edge is real and still guarded
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundariesArchUnitTest\.cs\:\:EachListedEdgeOfTheLane\_LeftOffTheList\_FailsTheServicesRule\_NamingItNotOnTheList

```text
A list entry duplicated at two positions, since the comparison is by set
```

###### Obligations


##### Covers

P3

```text
For each of the 19 positions of the measured list, the Services rule runs over the real code with a made-up gone edge of that position's dependent added
```

```text
Each run fails naming exactly the made-up gone edge as no longer offending, with a request to delete it, and names no newcomer
```

```text
A maintainer who keeps an edge on the list after it disappears is told to delete it, so the list only shrinks
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundariesArchUnitTest\.cs\:\:EachListedEdgeOfTheLane\_WithAGoneEdgeOfItsDependentAdded\_FailsTheServicesRule\_NamingItNoLongerOffending

```text
A gone edge whose dependent is also gone, which this falsifier does not construct
```

###### Obligations


##### Covers

P3

```text
The Services rule is handed a lane namespace that holds no type
```

```text
It fails saying the rule checked nothing
```

```text
A maintainer who renames or empties the lane namespace sees the rule fail rather than pass while guarding nothing
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundariesArchUnitTest\.cs\:\:ALaneRuleHandedALaneHoldingNoType\_FailsAsHavingCheckedNothing

```text
The no-API and no-adapters rules handed an empty lane, which share the same verdict check but are not run empty here
```

###### Obligations


##### Covers

P3

P6

```text
The Services rule reads every edge from the platform lane into Services in the compiled code and compares it with the 19 listed edges
```

```text
The edges found equal the list both ways: the repository edges of the demo data, update naming and usage data types, three edges to the app settings service, and the update queue's four write-back edges with its held update's one
```

```text
A maintainer sees exactly which Services code the platform lane still leans on, and that the update pipelines are not among it
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundariesArchUnitTest\.cs\:\:ThePlatformLane\_ReachesCodeStillInServices\_OnlyThroughTheListedEdges

```text
A new edge into Services that exists only through dependency injection or reflection, as the update pipelines do
```

###### Obligations



##### Covers

P1

P2

P3

```text
Every lane rule runs over a made-up lane that reaches only a listed Services helper (also from a nested type), a context's Application, and a same-named helper outside Services
```

```text
The Services rule finds exactly the two listed edges, nested one included; every rule passes
```

```text
A maintainer can trust that the rules do not fail compliant code, read nested types and match by namespace, not short name
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PlatformLaneBoundariesArchUnitTest\.cs\:\:AMadeUpLaneThatOnlyDoesWhatIsAllowed\_PassesEveryLaneRule

```text
A rule that also ignored a reach into an Adapters namespace one level deeper than the context, which the made-up lane does not exercise
```

###### Obligations


##### Covers

P6

```text
The existing layout, platform lane membership and preservation, and shared module rule tests run beside the new rules
```

```text
They keep their verdicts; only test-side rule files were added, no production file changed and no list was loosened
```

```text
Nothing a user, an API client, a tracker or the database sees changes
```

##### Disposition

Tested

dotnet test Lighthouse\.Backend\/Lighthouse\.Backend\.Tests \-\-filter FullyQualifiedName\~Layout\|FullyQualifiedName\~PlatformLane\|FullyQualifiedName\~ModuleRulesCatchANewOffender

```text
A behaviour change in production code that no architecture rule reads; the full filtered backend suite and EXAMINE carry that
```

###### Obligations


### S2

##### Covers

P4

```text
The placement rule reads today's test project: every compiled test class with the file it was compiled from, judged against every platform type of the lane's map and of the Platform namespace.
```

```text
No unit test of a platform type sits outside its Platform area folder unless it is named as staying with its reason. Today it names exactly two left-behind tests: the GitHub release service test, which belongs in the Releases area, and the platform-extension test, which belongs in the Host area.
```

```text
A maintainer sees each platform unit test outside its area named with the folder and namespace it belongs in; the rule turns green once those two test files are moved there.
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.PlatformLaneUnitTestPlacementTest\.NoUnitTestOfAPlatformType\_SitsOutsideItsPlatformFolder\_ApartFromTheNamedStayingTests

```text
A platform test whose class name differs from its subject's name by more than case, a trailing s or added words after the type name (a test named after a feature, not a type) is not seen.
```

###### Obligations





##### Covers

P4

```text
The placement rule runs on made-up test trees for a made-up platform type: a test in an old Services folder, in its Platform area folder, in another area's folder, in a folder that only looks like Platform, at the test root, with a namespace that disagrees with its file, under the integration root, on the staying list, and classes whose names only look like a test of the type.
```

```text
The tests in an old folder, another area, a look-alike folder, at the root or with a disagreeing namespace are each named with the Platform area folder they belong in; the test in its folder, the integration test, the staying test and the look-alike names are not named.
```

```text
A platform test written into or left in a wrong place is caught and its right folder is named, while correctly placed and exempt tests are left alone.
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.PlatformLaneUnitTestPlacementTest\.AMadeUpTestOfAPlatformTypeOutsideItsFolder\_IsNamedWithItsFolder\_AndOneInItsFolderIsNot

```text
A test in a deeper sub-folder of its area folder is accepted; deeper folders inside the area are allowed on purpose.
```

###### Obligations






##### Covers

P4

```text
The placement rule is handed a made-up platform type and reads the made-up test class that is really compiled into the test assembly from a fixture file outside the Platform folders.
```

```text
The fixture file is named once, with the made-up type's Platform area folder, found through the compiled assembly's symbols.
```

```text
The rule finds a test by the file it was really compiled from, so a class cannot hide its place behind its name or namespace.
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.PlatformLaneUnitTestPlacementTest\.TheMadeUpTestCompiledFromTheFixtureFile\_IsNamedWithItsPlatformFolder

```text
A class whose methods carry no sequence points (an empty class) is not seen; such a class holds no test to place.
```

###### Obligations




##### Covers

P4

```text
The list of test files that stay outside the Platform folders is checked against the files on disk and against what the placement rule would name, and each entry is dropped in turn.
```

```text
Every staying entry has a reason, its file exists, and it holds a unit test of a platform type; dropping an entry makes the rule name that file. Today the list is empty: the update-pipeline, gold, seeder, repository and encryption tests that stay test no platform type and are not named, and no platform test outside the Platform folders has a reason to stay.
```

```text
No exception to the placement rule exists without a reason or outlives the test it excused, and the tests that stay by design are not flagged.
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.PlatformLaneUnitTestPlacementTest\.EveryNamedStayingTestFile\_ExistsAndTestsAPlatformType\; Lighthouse\.Backend\.Tests\.Architecture\.PlatformLaneUnitTestPlacementTest\.AStayingTestFileDroppedFromTheList\_IsNamed

```text
An empty staying list passes trivially; the per-entry drop check gains cases only once an entry is added.
```

###### Obligations




##### Covers

P5

```text
The rule reads every production source file in the Services Interfaces and Services Implementation folders and compares them with the named staying list of 151 files, each with its owner.
```

```text
The files found equal the list both ways, every entry has an owner, and no entry holds a platform type. Written with the list empty, the rule named all 151 files.
```

```text
Nothing is left in the old Services folders without a named owner, and no platform file stays there.
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.PlatformLaneUnitTestPlacementTest\.TheProductionFilesInServicesInterfacesAndImplementation\_AreExactlyTheNamedStayingFiles

```text
A wrong owner on a correct path; whether an owner is right is judged by review, not by the rule.
```

###### Obligations




##### Covers

P5

```text
Each of the 151 entries of the production staying list is dropped in turn, and separately a made-up path whose file does not exist is added to the list.
```

```text
Every dropped entry's file is named as not on the list, and the added gone path is named as no longer found.
```

```text
Every entry on the list is load-bearing: removing one, or keeping one whose file is gone, is caught.
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.PlatformLaneUnitTestPlacementTest\.AListedProductionFileDroppedFromTheList\_IsNamed\; Lighthouse\.Backend\.Tests\.Architecture\.PlatformLaneUnitTestPlacementTest\.AGoneFileAddedToTheProductionList\_IsNamed

```text
A reordered list is accepted on purpose, since order carries no meaning.
```

###### Obligations




##### Covers

P5

```text
The production rule runs on made-up trees: every file listed, an unlisted newcomer among the interfaces, an unlisted newcomer deeper among the implementations, a listed file that is gone, an entry without an owner, a made-up platform type listed as staying, and files in folders that only look like the two old folders.
```

```text
Each newcomer, the gone file, the ownerless entry and the listed platform type are named; the fully listed tree and the look-alike folders pass.
```

```text
A new file added to either old folder, a stale entry, an ownerless entry or a platform type kept there is caught; files elsewhere are not.
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.PlatformLaneUnitTestPlacementTest\.AMadeUpProductionTree\_NamesEveryUnlistedGoneOwnerlessOrPlatformFile\_AndNothingElse

```text
A platform type kept in a file whose name differs from the type's name; the platform check reads the file name.
```

###### Obligations






##### Covers

P6

```text
The slice adds only two test-side files, the placement rule and its made-up fixture, and the existing architecture, placement, layout and platform suites and the backend build run unchanged.
```

```text
Every existing rule keeps its verdict (1807 tests of the placement, platform-lane, layout and platform filter pass, apart from the known environmental path-classifier failure), and the backend builds with zero warnings and zero errors.
```

```text
Nothing a user, an API client, a tracker or the database sees changes.
```

##### Disposition

Tested

dotnet test Lighthouse\.Backend \-\-filter TestCategory\!\=JiraIntegration\&TestCategory\!\=LinearIntegration\&TestCategory\!\=AdoIntegration\&TestCategory\!\=ServiceNowIntegration\&TestCategory\!\=GithubIntegration\; dotnet build Lighthouse\.Backend

```text
A behaviour change no existing test observes; the slice edits no production file, so none can arise from it.
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
A maintainer sees, from the architecture-rules run, that the layout rule for the platform lane passes today: every type in the membership map sits in its Platform area, namespace equal to folder, and none is left under Services. A made-up listed type under Services or in the wrong area would fail the rule and the failure names the type and its expected area.
```

#### E2

##### Promises

P2

```text
A maintainer sees, from the architecture-rules run, that the two boundary rules pass with an empty baseline: no Platform type reaches an API type or a module's Adapters. A made-up offending platform type would fail the matching rule and the message speaks of the platform lane, not a module.
```

#### E3

##### Promises

P3

```text
A maintainer sees that the platform lane's edges into Services are one inline list by full type name, matching the 19 edges the IL shows, checked both ways. A made-up new edge not on the list fails, and a listed edge that no longer exists fails. The list is not loosened.
```

#### E4

##### Promises

P4

```text
A future contributor who puts a unit test of a platform type in an old Tests/Services folder is stopped by the placement rule, whose message names the test and the Tests/Platform/<Area> folder it belongs in. The tests that stay by design (update pipelines, context gold tests, seeders, repositories, zip host fixtures) are not flagged, and today's suite passes the rule.
```

#### E5

##### Promises

P5

```text
A maintainer sees that every production file left in Services/Interfaces and Services/Implementation is named per file with its owner, and that none is a platform file. A made-up new file there fails the rule, and a listed file that no longer exists fails too.
```

#### E6

##### Promises

P6

```text
Nothing a user, API client, tracker or the database sees changes: the platform-answers and refresh-reactions comparisons report IDENTICAL, the EF model is unchanged, feature A's preservation law and B's five layout rules and every existing architecture rule keep their verdicts with nothing loosened, the lane's module edges are not listed a second time, the filtered backend suite is green and the build has zero warnings. The test-selection show confirms the two new oracles are part of the selection.
```

## Charter

None.

## Examination

### E1

met

#### Observations

```text
architecture-rules run: 1717/1717 passed, 142 platform-lane rule/falsifier rows passed incl. PlatformLane_LivesInItsPlatformArea_AndNoneIsLeftUnderServices, _ReachesNoApiType, _ReachesNoContextsAdapters, _ReachesCodeStillInServices_OnlyThroughTheListedEdges (both-ways, per-position rows).
```

### E2

met

#### Observations

```text
architecture-rules run: 1717/1717 passed, 142 platform-lane rule/falsifier rows passed incl. PlatformLane_LivesInItsPlatformArea_AndNoneIsLeftUnderServices, _ReachesNoApiType, _ReachesNoContextsAdapters, _ReachesCodeStillInServices_OnlyThroughTheListedEdges (both-ways, per-position rows).
```

### E3

met

#### Observations

```text
architecture-rules run: 1717/1717 passed, 142 platform-lane rule/falsifier rows passed incl. PlatformLane_LivesInItsPlatformArea_AndNoneIsLeftUnderServices, _ReachesNoApiType, _ReachesNoContextsAdapters, _ReachesCodeStillInServices_OnlyThroughTheListedEdges (both-ways, per-position rows).
```

### E4

met

#### Observations

```text
backend-tests PlatformLaneUnitTestPlacement: 170/170 pass: real-tree placement, staying test files exist, production Services/Interfaces+Implementation equal named list; made-up old-folder test, unlisted/gone/ownerless/platform file, lookalike folders behave.
```

### E5

met

#### Observations

```text
backend-tests PlatformLaneUnitTestPlacement: 170/170 pass: real-tree placement, staying test files exist, production Services/Interfaces+Implementation equal named list; made-up old-folder test, unlisted/gone/ownerless/platform file, lookalike folders behave.
```

### E6

met

#### Observations

```text
Full backend-tests: 14589 total, 14577 passed, 12 skipped, successful. platform-answers-compare: IDENTICAL, 12 answers 0 different. ef-model-unchanged: Sqlite and Postgres no changes. test-selection-show printed only flags (backend false etc), did not name the two oracles. refresh-reactions comparison and build warning count not separately probed. CI at 1132dff6e: backend success.
```

```text
refresh-reactions-compare: VERDICT IDENTICAL, 3 observable reactions same on both sides (4 blocked-period reactions not observable on either side). test-selection-show with PlatformLaneBoundariesArchUnitTest.cs and PlatformLaneUnitTestPlacementTest.cs (Lighthouse.Backend.Tests/Architecture): backend: true. test-selection-show scenarios: passed 92, failed 0. No declared probe shows a build warning count (ci-live-suites-show has none); evidence is CI backend job success on 1132dff6e plus local backend-tests/architecture-rules builds succeeding under TreatWarningsAsErrors (project rule makes any warning a build failure), which suffices from the user's view.
```
