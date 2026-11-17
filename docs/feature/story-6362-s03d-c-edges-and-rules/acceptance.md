# Acceptance brief: story\-6362\-s03d\-c\-edges\-and\-rules

## Slices

None.

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

#### E4

##### Promises

P4

```text
Probe architecture-rules PromisesUnitTestPlacement: every Promises unit test sits under the Promises test folders and no Promises source file remains under Services except named exceptions; the probe names a test or source file left in an old folder as Failed. The named exceptions themselves are visible only if the test name states them.
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
