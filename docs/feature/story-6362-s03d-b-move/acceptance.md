# Acceptance brief: story\-6362\-s03d\-b\-move

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Every Promises type sits in its designed place: the forecast block in Domain, the services, interfaces and projector in Application, and the two forecasts-updated handlers in Application Policies. No Promises type is left in its old namespace, and Domain reaches only the shared kernel. The architecture-rules probe reports each rule that places or bounds these types as Passed, by name.
```

#### E2

##### Promises

P2

```text
The unit tests of each moved Promises type run and pass, and each architecture rule that names a moved type reports Passed for its new place, with the rule comparing both ways. The backend-tests probe lists each moved type unit test class as Passed, and the architecture-rules probe lists those named rules as Passed.
```

#### E3

##### Promises

P3

```text
When a Portfolio forecasts are updated, its Delivery metric snapshot is recorded before its forecasts are published. The order-pin test reports Passed by name, and the forecast-triggers-show probe lists the snapshot recording ahead of the forecast publication.
```

#### E4

##### Promises

P4

```text
Nothing a user, a tracker or the database sees changes. The deliveries-answers-compare probe against the base revision prints IDENTICAL for the Delivery answers over the demo scenarios, the ef-model-unchanged probe reports the database model unchanged, the refresh-reactions-show and forecast-triggers-show probes show the same reactions as before, and the existing backend test classes report Passed.
```

## Charter

None.

## Examination

None.
