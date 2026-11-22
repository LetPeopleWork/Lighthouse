# Acceptance brief: story\-6363\-s03e\-c\-edges\-and\-rules

## Slices

### S1

##### Covers

P1

```text
Run the Access module rules over the compiled code: each Access type is looked for in its layer (Domain, Application, Http adapters) and in the old Services Auth and Authorization namespaces.
```

```text
Every Access type is found in its layer and none in the old namespaces; a made-up type left in its former home is named and its well-placed twin passes.
```

```text
A developer who leaves or puts an Access type outside its layer gets a failing rule that names the type and says where it belongs.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:\:EveryAccessType\_LivesInItsLayer\_AndNoneIsLeftInTheOldServicesNamespaces

```text
an Access type sat in a sub-namespace of its layer, or an extra unlisted type landed in a layer; the rule checks the listed types only, which the membership test of the earlier feature already pins
```

###### Obligations



##### Covers

P1

```text
Run the Access Domain rule over the compiled code, and over a made-up Domain where one type asks the platform clock.
```

```text
The real Access Domain reaches only the shared Models and the base library; the made-up type asking the clock is named and its plain twin is not.
```

```text
A developer who makes an Access calculation read the platform gets a failing rule naming that type.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:\:TheAccessDomain\_ReachesOnlyTheSharedKernelAndTheBaseLibrary

```text
an Access Domain type reached the platform only inside a catch clause or an event, which the IL reader does not see
```

###### Obligations



##### Covers

P1

```text
Compare each Access list with what the compiled code reaches: outside readers of the Domain and of the Application besides the API and the composition root, outside users of the Http adapters besides the API, the Application's edges into the data layer, the platform and EF Core, and Access's edges into other contexts.
```

```text
Each list equals what the code reaches in both directions: one Domain reader, five Application readers, the composition root alone for the Http adapters, the 21 data, platform and EF Core edges, and no edge into another context.
```

```text
A developer who adds a new dependency across the Access border, or removes one without shortening the list, gets a failing rule naming the type or edge.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:\:AnAccessListedRule\_MatchesWhatTheCompiledCodeReaches\_InBothDirections

```text
a new reader reached Access only through a catch clause, an event or a named attribute argument
```

###### Obligations



##### Covers

P1

```text
For each Access list, delete each entry in turn and run the rule; then add an entry the code does not hold and run it again.
```

```text
Each deleted entry is named as offending, and the added entry is named as no longer offending, to be deleted.
```

```text
Every position of every Access list is guarded: an edge the list misses and a list entry with no edge both fail by name.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:\:EveryEntryDeletedFromAnAccessList\_IsNamedAsOffending\_AndAnEntryWithNoEdgeIsNamedForDeletion

```text
a rule reported offenders by type but lost which target each reached; the entry name alone is asserted
```

###### Obligations





##### Covers

P1

```text
Run the Http adapter rule over a made-up type outside the API whose only tie to Access is the access-guard attribute it applies, beside a made-up API type applying the same attribute and a made-up composition root on the list.
```

```text
The attribute-only type is named; the API type and the listed composition root are not; the failure speaks of Access, not of Flow metrics.
```

```text
A developer who guards a non-API class with an Access attribute gets a failing rule naming that class and sending them to the Access module.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:\:AnOutsideTypeReachingAccessOnlyThroughAnAttributeItApplies\_IsNamedByTheHttpAdapterRule\_AndTheApiAndTheListedCompositionRootAreNot

```text
the rule text speaks of Access's adapters with a wrong reason, since only the module wording and the absence of Flow metrics are asserted, not the reason prose
```

###### Obligations





##### Covers

P1

```text
Run the Access Domain and Application inbound rules over a made-up module with a newcomer outside it, a made-up API, a made-up composition root and a listed caller.
```

```text
The newcomer is named; the API, the composition root and the listed caller are not.
```

```text
A developer whose new code reaches Access's Domain or Application from outside the API gets a failing rule naming that code.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:\:AnOutsideTypeOffTheList\_ReachingTheAccessApplication\_IsNamed\_AndTheApiTheCompositionRootAndTheListedCallerAreNot

```text
the newcomer reached the Application only inside an async method body compiled away in Release; the made-up newcomer reaches it inside an async method so that case is exercised
```

###### Obligations





##### Covers

P1

```text
Run Access's outbound rules over made-up Application types that hold a DbSet, ask an unlisted platform service, use another module's adapter, ask Work Items, touch the update code or join the write-back round, and over a twin asking only a listed platform port and Licensing.
```

```text
Each made-up type is named with the exact edge it adds; the twin passes both rules, so asking Licensing is allowed.
```

```text
A developer who ties Access to storage, a new platform service or another context gets a failing rule naming the edge.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessModuleBoundariesArchUnitTest\.cs\:\:AnAccessTypeReachingForbiddenCode\_IsNamedWithTheEdge

```text
Access reached Refinement or Reports types, which no made-up case exercises; the real list comparison still covers them
```

###### Obligations





##### Covers

P2

```text
Run the Licensing module rules over the compiled code: types in their layers and none in the old Services Licensing namespaces, Licensing's edges into Access or another context, outside readers of the Application besides the API, the composition root and Access's Application, outside users of the Http adapters besides the API, and the Application's platform edges.
```

```text
Every Licensing type is in Application or Http adapters; no edge into Access or another context; exactly the 11 readers, the forecast updater among them; no outside user of the Http adapters; exactly the two licence service edges to the clock and the generic repository.
```

```text
A developer who adds a new reader of the licence or a new Licensing dependency gets a failing rule naming it.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:\:ALicensingListedRule\_MatchesWhatTheCompiledCodeReaches\_InBothDirections

```text
a reader reached the licence only through a catch clause or an event, which the IL reader does not see
```

###### Obligations



##### Covers

P2

```text
Run the Licensing reader rule with the forecast updater taken off its list, and every Licensing list with each entry deleted in turn and with an entry the code does not hold.
```

```text
The forecast updater is named as offending; each deleted entry is named; the added entry is named for deletion.
```

```text
No reader of the licence, the background forecast run included, can drop off the list unseen.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:\:AReaderOfTheLicenceDroppedFromItsList\_TheForecastUpdaterFirst\_IsNamedAsOffending

```text
the rule named the forecast updater but not what it reached; only the name is asserted
```

###### Obligations





##### Covers

P2

```text
Run Licensing's outbound rules over made-up Licensing types that ask Access's role service, ask Work Items, hold a DbSet or ask an unlisted platform service, and over a twin asking only the listed clock.
```

```text
Each made-up type is named with the exact edge it adds; the twin passes both rules.
```

```text
A developer who makes Licensing depend on Access or another context, or on storage, gets a failing rule naming the edge.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:\:ALicensingTypeReachingForbiddenCode\_IsNamedWithTheEdge

```text
Licensing reached Access only through the old Services Auth namespaces, which no made-up case exercises; the pattern includes them
```

###### Obligations




##### Covers

P2

```text
Run the Licensing Http adapter rule and the Application rule over a made-up module: a newcomer outside the API applying the made-up licence guard, a newcomer asking the Application, a made-up API, a made-up Access Application, a made-up composition root and a listed caller.
```

```text
Both newcomers are named; the API, Access's Application, the composition root and the listed caller are not; the Http failure speaks of Licensing.
```

```text
A developer who guards non-API code with a licence attribute, or asks the licence from a new place, gets a failing rule naming it.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:\:AnOutsideTypeReachingTheLicensingHttpAdapters\_IsNamed\_AndTheApiIsNot

```text
the Http rule set aside the composition root by name although Licensing lists none; no made-up composition root reaches the made-up Http adapters, and the real list comparison with an empty list would name the root if it ever reached them
```

###### Obligations




##### Covers

P2

```text
Read the Work Items and Forecasting edge lists by name and check every Access or Licensing entry; then check a made-up list with an entry under an old Services Licensing name and one naming a module type the code does not hold.
```

```text
Work Items keeps exactly its two entries and Forecasting its one, all under Modules.Access or Modules.Licensing and held by the code; the made-up old-name and gone entries are named, and the entry under the module is not.
```

```text
The neighbours' lists keep guarding their real Access and Licensing edges; an entry under an old name cannot hide one.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingModuleBoundariesArchUnitTest\.cs\:\:TheEdgeListOfANeighbourModule\_KeepsItsAccessAndLicensingEntriesUnderTheModules

```text
another module's list, outside Work Items and Forecasting, held an old name; only those two lists are read, which the design names
```

###### Obligations




##### Covers

P4

```text
Run the existing suites with the two new rule files present.
```

```text
No production file changes; every existing test keeps its outcome; only the six tests asking for the missing shared Http adapter rule fail until it exists.
```

```text
Nothing a user, a tracker, the database or the licence check sees changes.
```

##### Disposition

NotTested

```text
Preservation is observed by the existing suites in the slice's full verification command, not by a new oracle; the two files are test-only and no design locator names a preservation oracle.
```

nw\-software\-crafter\-reviewer

### S3

##### Covers

P3

```text
Check where every unit test of an Access or Licensing type sits in the test project, and run the same check over made-up test trees and made-up classes
```

```text
Each such test sits under Modules/Access or Modules/Licensing in the folder of its subject's layer, with that folder's namespace; a made-up test left in an old folder, at a root, in the wrong layer or in the other module, and a class declared in a module namespace outside its folder, are named; a test in its layer, its twin in the folder, a lookalike name, a helper, a test of another context and the integration, acceptance, contract and architecture tests are not
```

```text
A maintainer finds every Access and Licensing unit test next to the module and layer it tests
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingUnitTestPlacementTest\.cs

```text
a misplaced test whose file name does not start with an Access or Licensing type name; it is caught only when it sits in an old folder, by the no-file-left check
```

###### Obligations





##### Covers

P3

```text
Look for any file in the old Services Implementation or Interfaces folders Auth, Authorization and Licensing, in the production code and in the tests, and run the same check over made-up source trees
```

```text
No file is found there and no file is excused; a made-up newcomer in each old folder and a file deeper below one are named, and files in the modules, in other Services folders and in folders whose names only start alike are not
```

```text
Nothing of Access or Licensing is left in the old shared Services folders, and a file put there later is caught
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingUnitTestPlacementTest\.cs

```text
an Access type placed in another Services folder, such as Services/Implementation directly; the rule judges folders, and the module rules judge namespaces
```

###### Obligations





##### Covers

P3

```text
Read every constant and static string namespace key of the shared module map and match each against made-up type names in the old Services Auth, Authorization and Licensing namespaces, and do the same over made-up module maps
```

```text
No key of the shared module map takes in an old name; today the Access and Licensing key does and is named until it is retired; in the made-up maps, keys that take in an old name, as constants or static fields, are named, and module, neighbour and lookalike keys are not
```

```text
The old Access and Licensing entry of the shared module map is gone; Access and Licensing are guarded only by their own module rules
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessAndLicensingUnitTestPlacementTest\.cs

```text
an old-namespace pattern kept inside a static string array of the map; only constants and static string fields are read, as the design states
```

###### Obligations





##### Covers

P4

```text
Run the whole backend suite without the live connector categories after the slice
```

```text
Every existing test stays green apart from the two module-map tests retired with the old key; no production file changes
```

```text
Users, trackers, the database and the licence check see no change
```

##### Disposition

Tested

Lighthouse\.Backend \(dotnet test with the live\-connector categories excluded\)

```text
a production edit that no existing test observes; review of the diff catches it
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
Running the architecture tests for the Access module rules passes, and each Access rule list (types per layer, Domain reach, outsider reach, Application reach, no other context) is inline by full type name and compared both ways against the compiled code.
```

#### E2

##### Promises

P1

```text
A made-up Access offender at each list position (including one that reaches only through an attribute) is reported as a failure by a named Access rule, and no type is left under the old Services Auth or Authorization namespaces.
```

#### E3

##### Promises

P2

```text
Running the architecture tests for the Licensing module rules passes: Licensing types sit only in Application and Adapters/Http, none under the old Services Licensing namespaces, no reach to Access or other contexts, and the outsider lists (ForecastUpdater included) and platform reach list are compared both ways against compiled code.
```

#### E4

##### Promises

P2

```text
A made-up Licensing offender against each Licensing rule fails a named rule, and the Forecasting and Work Items lists naming Access or Licensing types use the new type names.
```

#### E5

##### Promises

P3

```text
The placement test passes: every Access and Licensing unit test is under Tests/Modules/Access or Tests/Modules/Licensing, no file of either context is left in Services/Interfaces or Services/Implementation (production or tests), no exception exists for the old Licensing test folder, and RbacIdentityPattern is gone with its assertion carried by the new module rules.
```

#### E6

##### Promises

P3

```text
A made-up Access or Licensing file placed in an old folder is caught and named by the placement test.
```

#### E7

##### Promises

P4

```text
The existing rules EveryActionDeclaresItsAccess, AccessTablesSingleReader, LicenseGateSingleSource and the Reports and Refinement guard rules still run and pass with all assertions kept, and the other existing tests stay green; no route, guard, scheme, RBAC or licence answer changes.
```

## Charter

None.

## Examination

None.
