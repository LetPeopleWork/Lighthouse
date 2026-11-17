# Acceptance brief: story\-6359\-s03a\-b\-move

## Slices

### S1

##### Covers

P1

```text
Look up the state-transition mapper, the parent source selector and the dependency field references in the Tracker integration Domain
```

```text
Each is found in the Domain and none is left under Services; a missing or renamed one is reported by name
```

```text
The maintainer sees the three pure translation rules in the Tracker integration Domain and no copy under Services
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationDomainLayoutArchUnitTest\.cs\:\:TheStateTransitionMapperTheParentSourceSelectorAndTheDependencyFieldReferences\_LiveInTheTrackerIntegrationDomain\_AndNoneIsLeftUnderServices

```text
A fourth, foreign type sits in the Domain (closed by the exact-holdings test)
```

###### Obligations


##### Covers

P1

```text
Look up the category crossing and the dependency source selector in the Tracker integration Application
```

```text
Both are found in the Application and none is left under Services; a missing one is reported by name
```

```text
The maintainer sees the two rules that use shared extensions in the Application, not the Domain
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationDomainLayoutArchUnitTest\.cs\:\:TheCategoryCrossingAndTheDependencySourceSelector\_LiveInTheTrackerIntegrationApplication\_AndNoneIsLeftUnderServices

```text
One of them is put in the Domain instead (closed by the Domain holdings and reach tests)
```

###### Obligations


##### Covers

P1

```text
List every type the Tracker integration Domain holds
```

```text
The Domain holds exactly the three translation rules
```

```text
The Domain stays a short list of the pure rules
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationDomainLayoutArchUnitTest\.cs\:\:TheTrackerIntegrationDomain\_HoldsExactlyTheThreeTranslationRulesThatReachOnlyTheKernel

```text
A type that reaches Services or Extensions is moved in with the three (closed by the reach test)
```

###### Obligations


##### Covers

P1

```text
Run the Domain-reaches-only-the-kernel rule on the real Tracker integration Domain
```

```text
No Domain type reaches Services, the module's Adapters or the shared Extensions, and the Domain is not empty
```

```text
Nothing the Domain calculates can change because of a service or an extension
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationDomainLayoutArchUnitTest\.cs\:\:TheTrackerIntegrationDomain\_ReachesOnlyTheSharedKernelAndTheBaseLibrary

```text
The Domain holds no type at all (the rule fails on an empty Domain)
```

###### Obligations


##### Covers

P1

```text
Run the same rule on made-up Domain types that reach Services, Adapters or Extensions, each beside a compliant twin
```

```text
Each offender is reported by name with the forbidden type it reached and the twin is not
```

```text
A Domain type that starts reaching a forbidden layer is named in the failure
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationDomainLayoutArchUnitTest\.cs\:\:ATrackerIntegrationDomainTypeThatReachesAForbiddenLayer\_IsReportedAndItsCompliantTwinIsNot

```text
The rule flags every type, twin included (the twin must stay unreported)
```

###### Obligations


##### Covers

P1

```text
Run the Domain rule on a Domain namespace that holds no type
```

```text
The rule fails and names the empty Domain
```

```text
A rule pointed at the wrong place cannot pass unseen
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationDomainLayoutArchUnitTest\.cs\:\:ATrackerIntegrationDomainThatHoldsNoType\_FailsTheRuleInsteadOfPassingUnseen

```text
An empty Domain is treated as clean
```

###### Obligations


##### Covers

P1

```text
Work Items' membership rule compares its list of notes with the types that stay in Work Items
```

```text
Once the selectors leave Work Items, a stale note for one fails the existing rule, which compares both ways
```

```text
Work Items' notes list no selector after the move
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsMembershipArchUnitTest\.cs

```text
Stale selector notes stay in the list (only fails once the move lands; craft edits that file)
```

###### Obligations


### S2

##### Covers

P1

```text
Run the layout test for the connector port: the fifteen port and capability types looked up by name
```

```text
All fifteen live in the Tracker integration Application and none is left under Services; a missing type fails
```

```text
The maintainer sees the connector port in the module's Application after the move
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationApplicationLayoutArchUnitTest\.cs

```text
Only some of the fifteen move, or a renamed type is skipped silently, or a copy stays under Services
```

###### Obligations


##### Covers

P1

```text
Run the layout test for the factories: the four factory types looked up by name
```

```text
Connector and system factories and their interfaces live in the Application and none is left under Factories
```

```text
The maintainer sees the factories beside the port they build
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationApplicationLayoutArchUnitTest\.cs

```text
The factory interfaces move while a class stays under Factories
```

###### Obligations


##### Covers

P1

```text
Run the layout test for the authentication schema: keys, schema, method and option looked up by name
```

```text
All four live in the Application and none is left under Services
```

```text
The authentication schema the API reads sits in the module's Application
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationApplicationLayoutArchUnitTest\.cs

```text
The schema moves but AuthenticationMethod or AuthenticationMethodOption stays behind
```

###### Obligations


##### Covers

P1

```text
Run the layout test for the board types: Board and BoardInformation looked up by name
```

```text
Both live in the Application and none is left under Services
```

```text
The board types sit beside the board port
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationApplicationLayoutArchUnitTest\.cs

```text
Board moves but BoardInformation stays in the connector implementation namespace
```

###### Obligations


##### Covers

P1

```text
Run the rules on made-up offenders: a type left in its former home, a port naming an implementation type, and their clean twins
```

```text
The offender is named and the twin is not
```

```text
A rule that cannot fail would let a left-behind type pass unseen
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationApplicationLayoutArchUnitTest\.cs

```text
A rule that flags every type or no type at all
```

###### Obligations


##### Covers

P1

```text
Run the leak check on the board port signatures
```

```text
IBoardInformationProvider names no implementation namespace, and the port is found
```

```text
Callers of the board port do not depend on connector code
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationApplicationLayoutArchUnitTest\.cs

```text
The port names Board from the old implementation namespace but Board is also in Services so the move test alone passes
```

###### Obligations


##### Covers

P1

```text
Run the leak check on each factory interface and its class
```

```text
Interface and class share one namespace
```

```text
A caller imports one namespace for one factory
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationApplicationLayoutArchUnitTest\.cs

```text
The pair is checked only for the Application layer so a split pair that moves together in name only is accepted
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
Users, trackers and the database see no change from the tracker integration move: same behaviour and same stored data.
```

#### E2

##### Promises

P2

```text
Category crossing, state-transition mapping, parent and dependency selection and dependency field references give users the same results as before the move. Namespace placement is architectural and goes to candidate review, not user observation.
```

#### E3

##### Promises

P3

```text
Connector behaviour, auth setup and board listing are unchanged for users after the port and factories move. Namespace placement of the port, factories and board types is architectural and goes to candidate review.
```

#### E4

##### Promises

P4

```text
Each of the five trackers still connects, authenticates and syncs exactly as before the move, and CI still runs that tracker's live suite.
```

#### E5

##### Promises

P5

```text
OAuth connect, token refresh, provider handling and connection health behave for users exactly as before the move. Namespace placement is architectural and goes to candidate review.
```

#### E6

##### Promises

P6

```text
Every existing test passes unmodified apart from moves. Refresh answers, forecasts, routes, DTOs, DI lifetimes, handler order and the EF model are unchanged, and the live connector suites are green in CI.
```

## Charter

None.

## Examination

None.
