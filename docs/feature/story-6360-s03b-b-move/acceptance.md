# Acceptance brief: story\-6360\-s03b\-b\-move

## Slices

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
