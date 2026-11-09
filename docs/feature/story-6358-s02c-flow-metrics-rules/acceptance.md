# Acceptance brief: story\-6358\-s02c\-flow\-metrics\-rules

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Adding a type that reaches into the Flow metrics module from outside its allowed edges, or leaving a listed exception that no longer exists, makes the architecture checks fail and name the type, including when the dependency is used only inside an async method; the module's Domain, Adapters and Application each accept only their listed callers and its Application stays free of EF Core, Data, Adapters and API types.
```

#### E2

##### Promises

P2

```text
The old Metrics type-set rule and the hand-written scanner no longer exist, the Flow metrics module is recognised by its namespaces, and a dependency the old checks would have caught still makes the architecture checks fail.
```

#### E3

##### Promises

P3

```text
The product behaves exactly as before: the full existing test suite passes, the Flow metrics unit tests live under Lighthouse.Backend.Tests/Modules/FlowMetrics with only folder, namespace and using changes, and the database model shows no change.
```

## Charter

None.

## Examination

None.
