# Acceptance brief: story\-6362\-s03d\-c\-edges\-and\-rules

## Slices

### S1

##### Covers

P1

```text
Run the Promises module rules over the compiled backend: where each Promises type lives, and what each Promises layer holds
```

```text
Each of the 17 Promises types lives in its Domain, Application or Policies namespace, none is left under Services or Models, and each layer holds exactly its listed types; a made-up type left in its former home, or an unlisted type in a layer, is named and its twin is not
```

```text
The architecture rules show Promises as one module with its three layers, and a type placed elsewhere fails by name
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:\:EveryPromisesType\_LivesInItsLayer\_AndNoneIsLeftBehind

```text
A type that lands in the wrong Promises layer while its layer list is edited to match it
```

###### Obligations



##### Covers

P1

```text
Run the Promises Domain rule over the compiled backend and over a made-up Domain
```

```text
Promises' Domain reaches only the shared Models and the base library; a made-up Domain type asking the licence is named and its compliant twin is not
```

```text
Promises' Domain stays pure calculation, and a Domain type that reaches a service fails by name
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:\:ThePromisesDomain\_ReachesOnlyTheSharedKernelAndTheBaseLibrary

```text
A Domain type that reaches a Models entity carrying persistence behaviour, since all of Models is legal
```

###### Obligations



##### Covers

P1

```text
Run the Promises inbound rules over the compiled backend: who outside Promises reaches its Domain, its Application and its Policies; then delete each listed entry and add one with no edge
```

```text
Domain: no outside type; Application: only the API, the composition root and PortfolioUpdater; Policies: only the composition root; each deleted entry is named as offending, an entry with no edge is named for deletion, and a made-up outsider is named while the API, the composition root and a listed caller are not, in words that speak of Promises
```

```text
Who may call Promises is a short, exact list that only shrinks, and a new caller fails by name
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:\:APromisesListedRule\_MatchesWhatTheCompiledCodeReaches\_InBothDirections

```text
A Tracker integration type reaching Promises' Application, since that edge is left to Tracker integration's own exact rule; and a reference only in a catch clause, an event or an attribute named argument, which the IL reader does not see
```

###### Obligations





##### Covers

P1

```text
Run the Promises outbound rules over the compiled backend: what Promises' Application reaches below it and in the platform, and what the module reaches upstream; then delete each listed edge and add one that is gone
```

```text
Application: the 19 measured edges into the clock, the platform repositories, the generic repository, the blackout periods and the domain event handler contract, nothing in EF Core, Data, API or any Adapters; upstream (other modules' Adapters, update code, write-back round, Access, Licensing): none; each made-up offender (a DbSet, an unlisted platform service, a Forecasting adapter, the write-back trigger, the update key, the licence) is named with its edge, and a compliant service calling Work Items and Tracker integration Applications passes
```

```text
Promises reaches the rest of the system only through a short, exact list of ports, and a new reach fails by name
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:\:APromisesTypeReachingForbiddenCode\_IsNamedWithTheEdge

```text
Promises reaching a platform type outside the Services namespaces, or another module's Domain directly, since neither is a target of these rules
```

###### Obligations





##### Covers

P2

```text
Read the inbound lists of the Work Items and Tracker integration rules by field name and look for Promises types
```

```text
Neither list names a Promises type: Work Items loses DeliveryRuleService, Tracker integration loses DeliveryForecastPublishingService and DeliverySourceResolver; a made-up list naming a Promises type is named and a list without one passes
```

```text
Work Items and Tracker integration treat Promises as a legal caller by namespace, and a Promises entry left on their lists fails by name
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesModuleBoundariesArchUnitTest\.cs\:\:TheInboundListOfANeighbourModule\_NamesNoPromisesType

```text
The neighbours making the whole Promises module legal rather than only its Application; their own both-ways rules then decide what reaches them
```

###### Obligations



##### Covers

P5

```text
Change only test rule files in this slice
```

```text
No production file changes; every existing test keeps its outcome
```

```text
Nothing a user, a tracker or the database sees changes
```

##### Disposition

NotTested

```text
This slice changes no production code; preservation is observed by the declared full backend test run and by EXAMINE's compare probes, not by an architecture oracle
```

host full gates and EXAMINE

### S2

##### Covers

P3

```text
Read every dependency in the compiled backend from a Tracker integration type, in any layer and with its nested types, into a Promises type
```

```text
No such dependency exists; the check also fails when Tracker integration holds no type at all
```

```text
Tracker integration no longer depends on Promises, so the two modules form no cycle; today the Jira connector holding the forecast block renderer is named as the one offending dependency
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationReachesNoPromisesArchUnitTest\.cs\:\:TrackerIntegration\_ReachesNoPromisesType

```text
A Tracker integration type that names a Promises type only in a catch clause, an event or a named attribute argument, which the compiled-code reader cannot see
```

###### Obligations



##### Covers

P3

```text
Look up the merge port, the forecast block renderer, the renderer's own port and the Jira connector by name in the compiled backend
```

```text
The merge port is an interface in Tracker integration's Application declaring only the merge of a block into an existing description with the renderer's parameter names; the Promises renderer implements it; the Jira connector takes it in its constructor; the renderer's own port no longer offers the merge
```

```text
The Jira connector merges the Delivery forecast block through a port Tracker integration owns and Promises answers; today the port is missing, the renderer does not implement it, the renderer's port still offers the merge and the connector takes the renderer
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationReachesNoPromisesArchUnitTest\.cs\:\:TheJiraConnector\_MergesTheForecastBlockThroughTrackerIntegrationsOwnPort\_ThatThePromisesRendererImplements

```text
The port's description parameter loses its may-be-empty marking, which the check does not read; the compiler checks callers instead
```

###### Obligations


##### Covers

P3

```text
Run both rules on a made-up Tracker integration whose connector merges only through its own port, which a made-up Promises renderer implements
```

```text
The dependency rule looks at that connector and finds nothing; the port rule finds no problem
```

```text
A compliant connector is not reported, so the rules can stay switched on
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationReachesNoPromisesArchUnitTest\.cs\:\:ATrackerIntegrationTypeMergingOnlyThroughItsOwnPort\_PassesBothRules

```text
A rule that ignores adapters would still pass this row, but it fails the offender rows
```

###### Obligations


##### Covers

P3

```text
Run the dependency rule on made-up Tracker integration types that take a Promises renderer in a constructor, call a static Promises helper, or nest a type holding a Promises renderer
```

```text
Each run fails and names exactly that type and the Promises type it reaches
```

```text
A new dependency from Tracker integration on Promises cannot come back unseen
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationReachesNoPromisesArchUnitTest\.cs\:\:ATrackerIntegrationTypeReachingPromises\_FailsAndIsNamedWithWhatItReaches

```text
A dependency reached only through reflection or a type name held as text, which no compiled-code rule can see
```

###### Obligations



##### Covers

P3

```text
Run the dependency rule with a Tracker integration namespace that holds no type
```

```text
The rule fails, saying it checked nothing
```

```text
A renamed or emptied module cannot leave the rule green while it guards nothing
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationReachesNoPromisesArchUnitTest\.cs\:\:ATrackerIntegrationPatternThatMatchesNoType\_FailsInsteadOfPassing

```text
A Promises namespace that matches no type; the port row catches that, because the renderer must be found under Promises
```

###### Obligations


##### Covers

P3

```text
Run the port rule on a made-up case whose merge port is declared in Promises instead of Tracker integration
```

```text
The rule fails, naming the missing Tracker integration port and where the type of that name lives
```

```text
The merge port cannot slip back into Promises, which would make the connector depend on Promises again
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationReachesNoPromisesArchUnitTest\.cs\:\:AMergePortDeclaredInPromisesInsteadOfTrackerIntegration\_FailsAndIsNamedWithWhereItLives

```text
A port declared in a third module; the rule then says no type of that name exists in Tracker integration's Application, still failing, only with a less helpful message
```

###### Obligations


##### Covers

P3

```text
Run the port rule on a made-up case whose renderer port still declares the merge beside rendering
```

```text
The rule fails, naming the renderer port that still declares the merge
```

```text
No client can reach the merge through the Promises renderer's port again
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationReachesNoPromisesArchUnitTest\.cs\:\:ARendererPortThatStillOffersTheMerge\_FailsAndIsNamed

```text
The merge offered under another method name on the renderer port; the rule looks for the merge by name
```

###### Obligations


##### Covers

P5

```text
Merge a forecast block into a description through the renderer: replace in place, append when the markers are broken, rewrite the same block again
```

```text
The same text as before the port was extracted, pinned by the unchanged renderer merge examples
```

```text
The text written to Jira does not change
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/Promises\/Application\/DeliveryForecastBlockRendererTest\.cs

```text
A change to the merge reached only through the Jira connector's publishing path; the unchanged Jira write-back and publishing unit tests cover that path
```

###### Obligations



## Expectations

### Rows

#### E1

##### Promises

P1

```text
Probe architecture-rules PromisesModuleBoundaries (also PromisesUnitTestPlacement is separate): every test of the Promises module rules is listed Passed by name, covering layer placement, Domain reaching only the shared kernel, outsiders reaching Domain/Application/Policies only as listed, and Application reaching no EF Core, Data, API, other module Adapters, update code, write-back round, Access or Licensing except as listed; the names also show the lists are compared both ways against compiled code and that a made-up offender is named per rule. Whether the lists are measured from compiled code is observable only through test names; the test internals are not observable.
```

#### E2

##### Promises

P2

```text
Probe architecture-rules WorkItemsModuleBoundaries and architecture-rules TrackerIntegrationModuleBoundaries: both rule sets pass by name, and the names show Promises is legal by namespace rather than by listed type. The exact list size (smaller by exactly the Promises entries) is not observable by any probe line or test name; stated as outside what a probe can show.
```

#### E3

##### Promises

P3

```text
Probe architecture-rules TrackerIntegrationReachesNoPromises passes by name (Tracker integration reaches no Promises type), and backend-tests DeliveryForecastBlockRenderer and backend-tests JiraDeliverySourceProvider pass by name, showing the Jira connector still merges the Delivery forecast block through the Tracker-defined port implemented by the Promises renderer. Which side defines and implements the port is observable only through test names.
```

#### E5

##### Promises

P5

```text
Probes deliveries-answers-compare, forecast-answers-compare, refresh-reactions-compare each print IDENTICAL (base before the Promises move vs HEAD); ef-model-unchanged shows no model change per provider; backend-tests JiraQuietWriteBack and backend-tests JiraBatchedWriteBack pass by name for the Jira text; backend-tests with no fragment shows zero failures. Outside the expectation and not observable: the four blocked-period reactions that refresh-reactions-compare names not observable on both sides; routes and DI behaviour are observable only via the compare probes and test names; text written to a real Jira runs only in CI (ci-live-suites-show shows which suites CI runs, not their result).
```

## Charter

None.

## Examination

None.
