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
E3 (P3, handler order): when a Work Items domain event fires, the six Work Items event handlers run in the same order as before the move, so the user sees the same resulting state (for example the same item, team or refresh-log update) after the event. The Policies folder location is an architectural claim referred to candidate review.
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
