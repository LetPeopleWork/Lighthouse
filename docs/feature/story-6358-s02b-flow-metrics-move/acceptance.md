# Acceptance brief: story\-6358\-s02b\-flow\-metrics\-move

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A user or maintainer looking at the Flow metrics module finds every Flow metrics type the design names in its declared layer namespace (Domain, Application, Application.Policies, Adapters.Background, Adapters.Persistence) under Lighthouse.Backend.Modules.FlowMetrics, and each file sits in the folder that matches its namespace.
```

#### E2

##### Promises

P2

```text
The module's Domain layer refers only to the shared kernel (Lighthouse.Backend.Models) and the .NET base library; none of its types refers to an Application, Adapters, Services, Data, EF Core or Microsoft.Extensions type.
```

#### E3

##### Promises

P3

```text
A user sees no change: every metrics API answer on the same day is identical before and after the move, with no change to the stored data model.
```

#### E4

##### Promises

P3

```text
The existing test suite, including the event-handler order check, stays green after the move, and the only edits to existing tests are using, namespace and path-string edits.
```

## Charter

None.

## Examination

None.
