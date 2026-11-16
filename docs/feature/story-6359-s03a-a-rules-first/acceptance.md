# Acceptance brief: story\-6359\-s03a\-a\-rules\-first

## Slices

### S1

##### Covers

P2

P1

```text
The maintainer runs the architecture rule tests with made-up connectors under the Tracker integration module that reach Work Items, Forecasting or the update code, beside the same connector in the old connectors place, a connector reading only a Feature, one reaching a delivery source, and a namesake just outside the module.
```

```text
The leaf rule names the three module connectors and the old-place connector; it does not name the Feature reader, the delivery-source connector or the namesake.
```

```text
A connector that moves into the module and breaks the leaf boundary is still reported by the architecture tests.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationRulesSeeTheModuleArchUnitTest\.cs\:\:AConnectorUnderTheModuleReachingWorkItemsForecastingOrTheUpdateCode\_IsNamedByTheLeafRule

```text
A pattern that sees only part of the module, such as Adapters, would still name these Adapters.Connectors offenders while a module Domain or Application type reaching Work Items escapes.
```

###### Obligations


##### Covers

P2

P1

```text
The maintainer runs the architecture rule tests with a made-up factory in the Services factories namespace reaching Work Items, the same factory in the old factories namespace, and a namesake whose namespace only starts alike.
```

```text
The leaf rule names both factories and not the namesake.
```

```text
The half of the connector factory that lives in the Services factories namespace is guarded like the other half.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationRulesSeeTheModuleArchUnitTest\.cs\:\:AFactoryInTheServicesFactoriesNamespaceReachingWorkItems\_IsNamedByTheLeafRule

```text
The Services factories alternative also takes in sub-namespaces below it; the namesake only checks a sibling prefix.
```

###### Obligations


##### Covers

P2

P1

```text
The maintainer runs the architecture rule tests with a made-up connector under the module reaching the database context, the same connector in the old connectors place, a module connector reading its OAuth token through the module token place, and a namesake outside the module reaching the database.
```

```text
The no-repository rule names the module connector and the old-place connector; it does not name the OAuth token reader or the namesake.
```

```text
A connector that moves into the module and reads stored records is still reported; reading OAuth tokens stays allowed.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationRulesSeeTheModuleArchUnitTest\.cs\:\:AConnectorUnderTheModuleReachingTheDatabase\_IsNamedByTheNoRepositoryRule\_WhileItsOAuthTokensStayAllowed

```text
A rule that missed a module repository under Adapters.Persistence while still catching the database context; that path belongs to the shared stored-records pattern, not to this case.
```

###### Obligations


##### Covers

P1

```text
The maintainer runs the architecture rule tests over the module OAuth token places (Application, Adapters and a provider below Adapters), the old OAuth places, and two namesakes that only share a prefix.
```

```text
Every module and old OAuth place counts as the persistence platform; neither namesake does.
```

```text
The OAuth token service stays recognised as a platform service, not a repository, after it moves into the module.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationRulesSeeTheModuleArchUnitTest\.cs\:\:TheOAuthTokenPlaceUnderTheModule\_CountsAsThePersistencePlatform\_WhileANamesakeDoesNot

```text
The connector-side OAuth folder under Adapters.Connectors is also taken for the platform; the design leaves that open, so it is not pinned.
```

###### Obligations

None.

### S2

##### Covers

P3

P1

```text
A change to a Jira, Azure DevOps, Linear or ServiceNow connector's own code or its test, on the Tracker integration module path
```

```text
The CI test selection picks that tracker's live connector suite and no other, as for the same change on the old path
```

```text
A moved connector still runs its live suite in CI, and only that one
```

##### Disposition

Tested

Scripts\/test\-selection\/test\-path\-classifier\.sh \(Scenario 20\: module connector code\/test\: only the \<tracker\> live suite runs\)

```text
A widening that matches the module connector folders with a pattern so broad it also matches unrelated module code (caught by Scenario 23)
```

###### Obligations

None.

##### Covers

P3

P1

```text
A change to the parent-source selector on the module's domain path
```

```text
The CI test selection picks the live Jira suite
```

```text
A change to what the Jira connector reads still runs the live Jira suite
```

##### Disposition

Tested

Scripts\/test\-selection\/test\-path\-classifier\.sh \(Scenario 21\: module parent\-source selector\: jira\=true\)

```text
Jira selected only because the whole module domain folder were added to the Jira pattern
```

###### Obligations

None.

##### Covers

P3

P1

```text
A change to code all connectors share on the module path: connector Auth and OAuth folders (code and tests), the module's application and domain code, and the module's OAuth adapter
```

```text
The CI test selection marks the change as shared, so every live connector suite runs
```

```text
A change to shared connector code still runs every live suite
```

##### Disposition

Tested

Scripts\/test\-selection\/test\-path\-classifier\.sh \(Scenario 22\: module shared code\: shared\=true for \<path\>\)

```text
A pattern matching the whole module, which Scenario 23 refuses
```

###### Obligations

None.

##### Covers

P3

```text
A change on the module path that no live connector depends on: the CSV connector and the background services
```

```text
No live connector suite is selected, as on the old path
```

```text
Unrelated module changes do not spend live tracker quota
```

##### Disposition

Tested

Scripts\/test\-selection\/test\-path\-classifier\.sh \(Scenario 23\: module code outside the live connectors\: no live suite\)

```text
Over-selection limited to other module folders than the two sampled
```

###### Obligations

None.

##### Covers

P3

P1

```text
The examine command that counts unsupported connector methods runs in a checkout whose CSV, Linear and ServiceNow connectors live on the module path, and in one where they live on the old path
```

```text
It prints one count per connector folder and the total, ignoring commented-out code, and succeeds, instead of reporting folder not found
```

```text
The examiner can still count what each connector cannot do after the move
```

##### Disposition

Tested

Scripts\/test\-selection\/test\-path\-classifier\.sh \(Scenario 24\: connector\-throws\-count counts the connectors on the module path \/ still on the old path\)

```text
A command that finds the folders only when both the old and the module folder of a connector exist at once
```

###### Obligations

None.

### S3

##### Covers

P4

P1

```text
The maintainer runs the membership test on the compiled code
```

```text
Every top-level type of the connector, OAuth, connection-health and factory namespaces, old or module, plus the source selectors and the health prober, is on the moving or staying list, and every listed name exists; the demo-data types are the only ones that stay, each with its owner, and no type is on two lists
```

```text
The maintainer sees exactly which types feature B moves; a forgotten or made-up type is named in the failure
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembershipArchUnitTest\.cs\:\:TheTrackerIntegrationTypesInTheCompiledCode\_AreExactlyTheTypesListed\, OnlyTheDemoDataTypesStay\_AndEachTypeThatStaysNamesTheContextThatKeepsIt\, NoTypeIsOnTwoLists

```text
a type referenced only in a catch clause or an event, which the compiled-code reading does not see, is not judged
```

###### Obligations


##### Covers

P4

```text
The membership check is given the compiled code plus made-up types in the module namespace and the old connector namespace, a namesake namespace and a nested type; and separately the compiled code with the Jira connector renamed
```

```text
The made-up types in both namespaces are named as unlisted, the namesake and the nested type are not, no real listed type looks missing; the renamed connector is named as missing and its new name as unlisted
```

```text
A type added, renamed or dropped during the move fails the membership test by name until the list follows it
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembershipArchUnitTest\.cs\:\:AMadeUpTypeInTheModuleOrAnOldNamespace\_IsNamedAsUnlisted\_AndANamesakeOutsideIsNot\, AListedTypeRenamedInTheCode\_IsNamedAsMissing\_AndItsNewNameAsUnlisted

```text
a rename to a name already listed elsewhere is reported only as the old name missing
```

###### Obligations


##### Covers

P4

```text
The maintainer runs the Models admission check over the shared Models candidates of the context (OAuth, connection health, validation, write-back and the connection option types)
```

```text
The list of Models types that move equals exactly the candidates the database context does not store, no controller sends (signature, value handed to a result, or reached through its public properties) and no staying Models type reaches; the stored OAuth credential, health verdict and connection, the sent health answer and validation result, and the credential status reached by the credential stay; the OAuth flow, refresh, state and token values are admitted
```

```text
Feature B moves only the shared Models types that can leave the kernel without changing the stored model, the wire format or making the kernel reach the module
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembershipArchUnitTest\.cs\:\:TheModelsTypesListedToMove\_AreExactlyTheCandidatesNeitherStoredNorOnTheWireNorReachedByTheKernel\, ASharedModelsTypeOfTheContext\_IsJudgedAsTheDesignExpects

```text
a type sent through a variable typed object or built by a helper the controller does not call directly is judged not sent
```

###### Obligations


##### Covers

P4

```text
The admission check judges made-up Models types: one in a database set, one configured on the model builder, one in a controller signature, one handed to a status result, one reached through a sent envelope's property, one reached by a staying Models record, and a flow with its token that a controller only reads to redirect
```

```text
The two records are stored, the three answers are on the wire, the reached value is reached by the kernel, the flow and its token are admitted
```

```text
The admission rule refuses an entity, a wire type or a kernel-reached type, and does not refuse a value a controller only reads
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembershipArchUnitTest\.cs\:\:AMadeUpModelsType\_IsJudgedByWhatTheCompiledCodeShows

```text
an entity configured only through an owned-type call or a separate configuration class is judged not stored
```

###### Obligations


##### Covers

P1

```text
The maintainer runs the edge check on the inbound and outbound edges named at design
```

```text
The Jira connector references the delivery-forecast block renderer, connection health references the concrete verdict repository, the connection save references the fetch fingerprint and the fetch-shape event, and connection health does not reference the Jira connector; the two demo back-fill handlers carry the CSV option name as text and hold no reference to the CSV option names type
```

```text
The edges the module rules will list are confirmed from the compiled code, and the CSV edge is known to be a copied constant the compiled code cannot see
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationMembershipArchUnitTest\.cs\:\:AnEdgeNamedAtDesign\_IsInTheCompiledCode\, ADemoBackfillHandler\_CarriesTheCsvOptionNameAsText\_AndNoReferenceToTheCsvConnector

```text
an edge reached only through a type named in a catch clause or an event is not seen
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
Before any file moves, every architecture rule, the CI live-suite selection, the examine commands and the membership list already recognise the Modules/TrackerIntegration paths; a maintainer running the architecture rules and the membership test sees them pass on today's code.
```

#### E2

##### Promises

P2

```text
When a made-up connector type under Lighthouse.Backend.Modules.TrackerIntegration reaches Work Items, Forecasting, the update code or a repository, the Tracker integration architecture rules report it as an offender, as they do for the same type under the old Services/Implementation/WorkTrackingConnectors path.
```

#### E3

##### Promises

P3

```text
Running connector-throws-count and the test-time budgets finds the Modules/TrackerIntegration connector folders (Jira, Linear, Azure DevOps, ServiceNow) and does not report folder not found.
```

#### E4

##### Promises

P3

```text
A changed production or test file under Modules/TrackerIntegration/Adapters/Connectors/<Tracker>/ selects that tracker's live suite in CI, and a change under the shared Application, Domain or OAuth paths selects all live suites, as for the old path.
```

#### E5

##### Promises

P4

```text
The membership test for Tracker integration, with its admitted Models types, passes on today's compiled code, fails when a listed type is missing, and fails when the list names a type that does not exist.
```

## Charter

None.

## Examination

None.
