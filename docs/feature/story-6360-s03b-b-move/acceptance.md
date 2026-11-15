# Acceptance brief: story\-6360\-s03b\-b\-move

## Slices

### S1

##### Covers

P1

```text
Read the compiled assembly and ask where sync mode, the fetch fingerprint, the dependency policies and their plain values live
```

```text
Each sits in the Work Items module Domain (dependency ones in its Dependencies folder, never flat) and none is left under Services
```

```text
The pure Work Items calculations sit in their module Domain, nothing is left in the old Services folders
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsDomainLayoutArchUnitTest\.cs

```text
Types placed in the Domain root instead of Domain/Dependencies, or a copy left under Services, are refused by the two layout rule cases
```

###### Obligations


##### Covers

P1

```text
Read the compiled Work Items Domain and list what each of its types reaches
```

```text
Only the Domain itself, the shared Models kernel and the base library are reached; an empty Domain fails
```

```text
Work Items calculations cannot read a database, setting or logger
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsDomainLayoutArchUnitTest\.cs

```text
A Domain that is silently empty or a type reaching Services is caught; a rejected type belongs on the Application list, which the design owner records
```

###### Obligations


##### Covers

P1

```text
Run the same Domain rule on made-up Domain types reaching Services, the module Application and the module Adapters, next to a compliant twin
```

```text
Exactly the offender is reported and named with what it reached; the twin is not; the failure message names both
```

```text
A Domain type that leaks to Application or an adapter is named so it can be undone
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsDomainLayoutArchUnitTest\.cs

```text
A rule that flags the twin or misses an async or Application reach would be accepted by a count-only check, which this does not do
```

###### Obligations


##### Covers

P1

```text
Look at the files of the Domain types on disk
```

```text
The files sit in Modules/WorkItems/Domain and Domain/Dependencies and nowhere else
```

```text
The module layout shows in folders as in namespaces
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsDomainLayoutArchUnitTest\.cs

```text
A namespace edit without git mv is refused
```

###### Obligations


##### Covers

P1

```text
Change the fetch fingerprint file and ask the CI test selection which live Jira suite to run
```

```text
The new Domain path selects the live Jira suite and the old Services path no longer does
```

```text
A change to the fingerprint still runs the Jira tests after the move
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsDomainLayoutArchUnitTest\.cs

```text
A classifier keeping the old path, or matching any Domain file, would pass a text-only check; this runs the real classifier on both paths
```

###### Obligations


### S2

##### Covers

P2

```text
The compiled backend is read for where the Work Item, Team data, blocked, rule, parent-placement and refresh-log services and their ports live, and for copies left under the old service folders
```

```text
Every one lives in the Work Items Application layer folder and namespace and none remains under Services; a made-up type left under the old folder is named
```

```text
A maintainer sees the Work Items use cases in the Work Items module and none left in the old Services area
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsApplicationLayoutArchUnitTest\.cs

```text
A service stays under Services in a copy while the right namespace exists is caught; a type in the right namespace but wrong source folder is caught by the file check row, not this one
```

###### Obligations


##### Covers

P2

```text
The compiled backend and the source folders are read for where the dependency services and their ports live
```

```text
They sit in the Application Dependencies sub-folder and namespace, never flat in the Application, and no file of theirs remains elsewhere
```

```text
A maintainer finds the dependency decision, reconciler, reporter and rendering together in one dependencies folder
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsApplicationLayoutArchUnitTest\.cs

```text
A dependency type is flat in the Application and the layout is otherwise right
```

###### Obligations


##### Covers

P2

```text
The Work Items Application is checked for reaching EF Core, the data layer, the API or module Adapters, on the real code and on a made-up offender with a compliant twin
```

```text
The real Application reaches none; the made-up offender is named with what it reached and its twin is not; an Application that holds no type fails
```

```text
A maintainer is told if a use case starts to depend on storage or the web layer
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsApplicationLayoutArchUnitTest\.cs

```text
A reach into Services or another module that is not in the forbidden list is not caught here
```

###### Obligations


##### Covers

P2

```text
Every tracked test and production source file of the backend is scanned for a moved type named at its old home
```

```text
No full name or file path in a rule list, registration, comment or cref names a moved type under the old Services namespace or folder; a sample old name is found and a new name or a type that stays is not
```

```text
A maintainer sees every rule list and registration name the services where they now live
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsApplicationLayoutArchUnitTest\.cs

```text
A list names the new home but drops the type, or a rule is loosened: both-direction comparison stays with the rules own tests
```

###### Obligations


##### Covers

P4

```text
Refresh, metrics and forecast answers are compared before and after the move
```

```text
Identical answers
```

```text
Users see the same refresh results, metrics and forecasts
```

##### Disposition

NotTested

```text
Behaviour preservation is a completion check on the candidate (RefreshesStayTheSameTest and the answers-compare commands), not an architecture-layout observation
```

final reviewer and EXAMINE

### S3

##### Covers

P3

```text
Read the compiled production assembly and check where the six Work Items event handlers live
```

```text
All six live in the module's Application policies and none is left under Services; a handler left under Services in made-up types fails and is named, a well-placed twin is not
```

```text
Work Items reactions are found in one place in the module and nothing reacts from the old Services folders
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsPoliciesLayoutArchUnitTest\.cs

```text
A handler left behind in Services or a missing handler would pass if the rule only compared counts or looked at the new namespace alone
```

###### Obligations


##### Covers

P3

```text
Read the compiled assembly and the tracked source for the six handlers files and the types in the policies; completion guard, re-run at completion
```

```text
Each handler file sits only in the policies folder; every type in the policies handles a domain event, a non-handler is named and an empty policies namespace fails; no tracked source names a handler's old full name or path. The no-stale-name check passes before the move, so it is a post-move completion guard; its old-home fixture cases (a made-up old namespace and path in a rule list or path, each found and named) are its discriminator
```

```text
Policies hold only reactions and no stale list or path points at the old homes
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsPoliciesLayoutArchUnitTest\.cs

```text
A service dropped into the policies, an empty namespace, or a rule list keeping the old full name would pass if the check looked at file names alone; tracked non-source files outside the scanned projects are not scanned and are covered by a recorded repository search at completion
```

###### Obligations


##### Covers

P3

```text
Run the existing handler-order pin unmodified after the move
```

```text
The order pin stays green, so handlers still run in the registered order
```

```text
Reactions run in the same order as before the move
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs

```text
A move that reorders registrations in Program.cs would pass if only the layout rule were checked; the pin says nothing about refresh answers, which the answer rows below observe
```

###### Obligations


##### Covers

P4

```text
Run the refresh scenarios through the service port, with the four blocked-transition handlers registered, after the move
```

```text
Refresh results and blocked-transition records equal those before the move for the same data and request
```

```text
Refresh answers, metrics answers and forecasts are the same as before the move
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/WorkItems\/RefreshesStayTheSameTest\.cs

```text
A move that changes a blocked-transition handler's behaviour in a case the scenarios do not build would pass; the scenarios register only the four blocked-transition handlers
```

###### Obligations

None.

##### Covers

P4

```text
Delete a team and run the existing team-deleted clean-up handler test after the move
```

```text
The clean-up reaction removes the team's refresh logs: the test's single Verify on the refresh log removal passes
```

```text
Refresh history of a deleted team is cleaned as before the move
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/DomainEvents\/TeamDeletedRefreshLogCleanupHandlerTest\.cs

```text
A clean-up that does not remove the team's refresh logs fails that Verify, so it is not accepted; the only wrong behaviour still accepted is a handler registered in a different order or left unregistered, which the order pin catches
```

###### Obligations

None.

##### Covers

P4

```text
Change a connection's fetch shape and run the existing update-trigger handler test after the move
```

```text
The reaction triggers exactly the updates it triggered before: the trigger and count assertions pass
```

```text
A changed connection still leads to the same refresh as before the move
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/BackgroundServices\/Update\/ConnectionFetchShapeChangedUpdateTriggerHandlerTest\.cs

```text
A missing, extra or reordered trigger fails the trigger and count assertions, so it is not accepted; the only wrong behaviour still accepted is a handler left unregistered or registered in a different order, which the order pin catches
```

###### Obligations

None.

##### Covers

P4

```text
At completion, compare metrics answers of the base commit 8a785450c with HEAD using the metrics answers comparison command
```

```text
Metrics answers are identical before and after the move
```

```text
Metrics answers a user reads are the same as before the move
```

##### Disposition

Tested

Scripts\/examine\/bin\/metrics\-answers\-compare 8a785450c HEAD

```text
A change in a metric the comparison does not sample would pass; the host runs it on the implemented candidate and records the result
```

###### Obligations

None.

##### Covers

P4

```text
At completion, compare forecast answers of the base commit 8a785450c with HEAD using the forecast answers comparison command
```

```text
Forecast answers are identical before and after the move
```

```text
Forecasts a user reads are the same as before the move
```

##### Disposition

Tested

Scripts\/examine\/bin\/forecast\-answers\-compare 8a785450c HEAD

```text
A change in a forecast the comparison does not sample would pass; the host runs it on the implemented candidate and records the result
```

###### Obligations

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
E1 (P1): after the move, the sync mode, fetch fingerprint, rule evaluation and dependency decisions give the same visible results a user or operator sees (refresh answers, metrics answers and forecasts) as before the move, for the same data and the same request. The layer placement that P1 describes (a type the Domain architecture rule rejects is moved to Application and recorded) is an architectural claim referred to candidate review, not judged here.
```

#### E2

##### Promises

P2

```text
E2 (P2, architectural placement, user-view proxy): Work Item, Team data, blocked, rule, dependency, parent-placement and refresh-log services keep producing the same visible results for a user after the move; every rule list that names them still resolves to the moved services. The Modules/WorkItems/Application location and full-name rule lists are architectural claims referred to candidate review.
```

#### E3

##### Promises

P3

```text
E3 (P3, user-view handler outcome): when a Work Items domain event fires, the user sees the same resulting state after the event as before the move: the same Team refresh and its refresh-history entry, the same Teams refresh after a changed download of the connection, and the deleted Team's refresh history removed. The handler order of the six Work Items event handlers, and the blocked-period reactions (opening and closing blocked periods for Work Items and Features), have no public surface on the day they are opened and cannot be read through a public probe; they are referred to candidate review and to the unchanged handler-order pin. The Policies folder location is an architectural claim referred to candidate review.
```

#### E4

##### Promises

P4

```text
E4 (P4, user-observable): refresh answers, metrics answers and forecasts a user reads are identical before and after the move for the same data and the same request.
```

## Charter

None.

## Examination

None.
