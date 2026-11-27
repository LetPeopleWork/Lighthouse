# Acceptance brief: story\-6382\-s09a\-d\-events\-and\-rules

## Slices

### S1

##### Covers

P1

```text
The compiled backend is read for the place of each of the fifteen named domain events
```

```text
Each event resolves to exactly one type, in the Contracts of the context that owns it; one still in the kernel events namespace is named with its owner and the move to make
```

```text
Event types sit with the context that owns them
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventsLiveWithTheirPublisherArchUnitTest\.cs

TheEvent\_ResolvesToExactlyOneTypeInTheContractsOfItsOwner

```text
an event sits in a different context's Contracts; or a copy stays in the kernel
```

###### Obligations




##### Covers

P1

```text
The compiled backend is read for every domain event and the code that constructs it
```

```text
Every event is declared in the Contracts of the context whose code constructs it, or of the owner the context map names when only code outside every context constructs it
```

```text
A new event cannot be filed under the wrong context unnoticed
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventsLiveWithTheirPublisherArchUnitTest\.cs

EveryDomainEvent\_LivesInTheContractsOfTheContextThatPublishesIt

```text
an event is placed in the Contracts of a context that only reacts to it
```

###### Obligations




##### Covers

P1

```text
The compiled backend is read for what the kernel events namespace holds
```

```text
The kernel events namespace holds the event base type and nothing else
```

```text
The kernel carries only the shared event contract
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventsLiveWithTheirPublisherArchUnitTest\.cs

TheKernelEventsNamespace\_HoldsNothingButTheEventBase

```text
a sixteenth event type is left in the kernel
```

###### Obligations



##### Covers

P1

```text
Made-up contexts hold events filed in another context's Contracts, constructed by a context and by outside code, next to correctly filed ones
```

```text
Exactly the two misfiled events are named with their place, owner's Contracts, how the owner was found and the move to make
```

```text
A developer who misfiles an event is told where it sits, where it belongs and what to do
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventsLiveWithTheirPublisherArchUnitTest\.cs

AnEventFiledInAnotherContextsContracts\_IsNamedWithItsPlaceItsOwnerAndTheWayOut

```text
the rule names every event or none
```

###### Obligations



##### Covers

P1

```text
The compiled backend and the context map are read for event names
```

```text
The map names exactly the events the compiled code declares
```

```text
Every event has a declared owner
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventsLiveWithTheirPublisherArchUnitTest\.cs

TheContextMap\_NamesExactlyTheEventsTheCompiledCodeDeclares

```text
an event is renamed in code and map together
```

###### Obligations


### S2

##### Covers

P2

```text
A person asks for a forecast of a Portfolio with three Features and the forecast computes new forecasts
```

```text
Exactly one forecast refresh-log entry: the Portfolio's id and name, three items, a full sync of nothing, the finishing instant, successful, not cancelled, not skipped
```

```text
The System info page shows one successful forecast entry counting the three Features, as before
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/ForecastRoundsKeepTheirRefreshLogEntryTest\.cs

A\_round\_that\_forecasts\_its\_Features\_logs\_a\_successful\_entry\_counting\_them

```text
the entry's duration were wrong by any non-negative amount
```

###### Obligations


##### Covers

P2

```text
A person asks for a forecast and the forecast fails
```

```text
Exactly one forecast entry counting no items, not successful, not cancelled, not skipped
```

```text
The System info page shows one failed forecast entry, as before
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/ForecastRoundsKeepTheirRefreshLogEntryTest\.cs

A\_round\_whose\_forecast\_fails\_logs\_a\_failed\_entry\_counting\_nothing

```text
the entry's duration were wrong by any non-negative amount
```

###### Obligations


##### Covers

P2

```text
A person asks for a forecast and the operator stops it
```

```text
Exactly one forecast entry, not successful, cancelled, not skipped, written although the round was stopped
```

```text
The System info page shows one stopped forecast entry, as before
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/ForecastRoundsKeepTheirRefreshLogEntryTest\.cs

A\_round\_the\_operator\_stops\_logs\_a\_cancelled\_entry

```text
the event were published with the round's stop signal, because the refresh-log write takes no stop signal; only a reaction that honoured the stop would lose the row
```

###### Obligations


##### Covers

P2

```text
A person asks for a forecast of a Portfolio with two Features and the forecast finds them unchanged
```

```text
Exactly one forecast entry counting two items, successful, not cancelled, skipped
```

```text
The System info page shows one skipped forecast entry, as before
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/ForecastRoundsKeepTheirRefreshLogEntryTest\.cs

A\_round\_that\_finds\_nothing\_new\_logs\_a\_successful\_skipped\_entry\_counting\_the\_Features

```text
the entry's duration were wrong by any non-negative amount
```

###### Obligations


##### Covers

P2

```text
A person asks for a forecast that succeeds while writing the refresh log fails
```

```text
The round completes, exactly one logged error names the Work Items refresh-log reaction and the finished-round event, and no forecast entry exists
```

```text
The forecast still lands; the operator finds the failed log write in the log
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/ForecastRoundsKeepTheirRefreshLogEntryTest\.cs

A\_round\_whose\_refresh\_log\_write\_fails\_still\_completes\_and\_the\_failure\_is\_logged

```text
another component logged an error whose message named both the reaction and the event
```

###### Obligations


##### Covers

P2

```text
The compiled forecast updater is read
```

```text
Neither the forecast updater nor its compiler-made nested types name the Work Items refresh-log service
```

```text
Forecasting no longer commands Work Items to write its forecast entry
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/ForecastRoundsKeepTheirRefreshLogEntryTest\.cs

The\_forecast\_round\_no\_longer\_writes\_the\_refresh\_log\_itself

```text
the updater wrote the entry through another Work Items type; the entry rows would then see two entries
```

###### Obligations


##### Covers

P2

```text
The start-up registrations are read without a host
```

```text
The finished-forecast-round event exists and its handlers are exactly the Work Items refresh-log reaction
```

```text
Every forecast round has exactly one reaction that writes its entry
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs

HandlersOfAFinishedForecastRound\_AreTheRefreshLogEntryAlone

```text
the reaction were registered with a lifetime other than scoped
```

###### Obligations


### S3

##### Covers

P3

```text
Every list of edges in the architecture tests is read by value and checked for an entry from one context into another
```

```text
No list other than the contracts rule's own names such an entry; each one found is named with its list, the entry and the contracts rule as owner
```

```text
Each cross-context edge has exactly one owning rule
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/EachCrossContextEdgeHasOneOwningRuleArchUnitTest\.cs

NoPerModuleBoundaryListNamesAnEdgeFromOneContextIntoAnother

```text
a list that names only edges into another context's Contracts, or a list outside the Architecture namespace
```

###### Obligations



##### Covers

P3

```text
A made-up list names an edge from Refinement into Access, an edge inside Refinement, an API edge and an edge between made-up fixture roots
```

```text
Only the Refinement to Access edge is named, with list, entry and the contracts rule as owner
```

```text
A planted second owner of an edge is reported with where to fix it
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/EachCrossContextEdgeHasOneOwningRuleArchUnitTest\.cs

AMadeUpListNamingAnEdgeFromRefinementIntoAccess\_IsNamedWithItsListItsEntryAndTheContractsRuleAsOwner

```text
a check that names every edge or skips the made-up list
```

###### Obligations


##### Covers

P3

```text
The two lists left out by name are looked up among the lists read
```

```text
Both exist and each still names an edge between contexts
```

```text
An exclusion cannot silently hide a new edge
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/EachCrossContextEdgeHasOneOwningRuleArchUnitTest\.cs

TheListsLeftOutByName\_StillExistAndStillNameAnEdgeBetweenContexts

```text
an exclusion that points at a renamed or emptied list
```

###### Obligations


##### Covers

P3

```text
Tracker integration's boundary list is read for entries into Organisation set-up
```

```text
It names no edge into set-up
```

```text
Tracker integration reaches set-up only through the open host
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupIsAnOpenHostArchUnitTest\.cs

TrackerIntegrationsList\_NamesNoEdgeIntoSetUp

```text
a list that keeps the cadence edge under another target
```

###### Obligations


##### Covers

P3

```text
A made-up Tracker integration type asks set-up's Contracts and Tracker integration's rule is applied
```

```text
The rule names no edge for it
```

```text
Set-up's Contracts are open to Tracker integration without a second owner
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupIsAnOpenHostArchUnitTest\.cs

AMadeUpTrackerIntegrationTypeAskingSetUpsContracts\_PassesTrackerIntegrationsRule

```text
a rule that reads every set-up namespace as foreign
```

###### Obligations


##### Covers

P3

```text
Tracker integration's rule is applied to set-up's Contracts, Application, Domain and Adapters
```

```text
Set-up's Contracts are not read as another context; Application, Domain and Adapters still are
```

```text
Set-up is an open host through its Contracts only
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupIsAnOpenHostArchUnitTest\.cs

TrackerIntegrationsRule\_ReadsSetUpsApplicationDomainAndAdaptersAsAnotherContext\_ButNotItsContracts

```text
a rule that reads no set-up namespace as foreign
```

###### Obligations


##### Covers

P3

```text
Each downstream module's boundary list is read for entries into Organisation set-up
```

```text
No list names an edge into set-up, Refinement's included
```

```text
Edges into set-up are owned by the contracts rule alone
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupIsAnOpenHostArchUnitTest\.cs

ADownstreamModulesOutboundList\_NamesNoEdgeIntoSetUp

```text
a list that keeps only Domain edges into set-up
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
Using the public architecture rules and backend tests, a user sees every one of the 15 domain event types in the Contracts of the context that publishes it, IDomainEvent being the only event type left in Models.Events, and the backend test suite and the architecture rules pass as before.
```

```text
A rule or test count passing does not show each event sits in its publishing context's Contracts rather than another context's; it also cannot show that no event was renamed or its payload changed.
```

yes

None.

```text
architecture-rules and backend-tests probes against the built candidate
```

#### E2

##### Promises

P2

```text
After a forecast round that succeeds, fails, is cancelled or is skipped, the refresh log shows the same entry as before this feature; a failing log write is logged and the round still completes; the forecast updater no longer names the refresh-log service.
```

```text
Compare answers show entries, but cannot show which component wrote them, nor reliably force a log-write failure from outside; the absence of the service name in the updater is a structural fact public probes see only through an architecture rule.
```

yes

None.

```text
refresh-answers-compare and forecast-answers-compare against the base revision; architecture-rules probe
```

#### E3

##### Promises

P3

```text
The architecture rules show every module-to-module edge governed by contracts rule R-C1 only, and a per-module boundary list naming a module-to-module edge makes the rules fail; ADR-270 and ARCHITECTURE.md section 4 describe the contracts layer and the CI test-selection payoff.
```

```text
A passing rule run does not show the rule would fail on a planted per-module list entry unless a negative control is captured; reading the documents shows wording but not that it matches shipped behaviour.
```

yes

None.

```text
architecture-rules probe with a negative control; public reading of the two documents
```

#### E4

##### Promises

P1

P2

P3

```text
Across the feature the product behaves as before: forecast and refresh answers equal those of the base revision and the backend tests pass, with no new user-visible change.
```

```text
Answer comparisons cover only the sampled forecast and refresh scenarios; behaviour outside them, and timing or ordering of event handling, stays unobserved.
```

yes

None.

```text
refresh-answers-compare, forecast-answers-compare and backend-tests probes
```

### Waivers

None.

## Charter

None.

## Examination

None.
