# Acceptance brief: story\-6362\-s03d\-a\-rules\-first

## Slices

### S1

##### Covers

P1

```text
The architecture tests run with a made-up Delivery service in Modules.Promises that reaches a repository, Forecasting's adapters or the update code, beside its twin in the old Delivery sources place, a namesake just outside Modules.Promises and a service that reads values only.
```

```text
The module map's Promises namespaces name the service in Modules.Promises and its old-place twin for each of the three reaches, and name neither the namesake nor the values-only service.
```

```text
When the Delivery services move into Modules/Promises, the module map still tells them apart, so no Promises rule built on it goes quiet.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesRulesSeeTheModuleArchUnitTest\.cs\:\:ADeliveryServiceUnderTheModuleReachingWhatPromisesMayNot\_IsNamedByTheModuleMapsPromisesNamespaces\_AsItsTwinInTheOldPlaceIs

```text
A module map pattern that keeps the old Implementation half of the Delivery sources place but drops its Interfaces half: only an Implementation twin is planted, so that narrowing still passes.
```

###### Obligations






##### Covers

P1

```text
The architecture tests run with a made-up Delivery service in Modules.Promises that reaches a repository, Forecasting's adapters or the update code, beside a namesake just outside Modules.Promises and a service that reads values only.
```

```text
The shared module rules' Promises namespace names the service in Modules.Promises for each of the three reaches, and names neither the namesake nor the values-only service.
```

```text
The module's own rules, written later with the shared module rules, have a Promises namespace that sees Modules/Promises and nothing that only shares its prefix.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesRulesSeeTheModuleArchUnitTest\.cs\:\:ADeliveryServiceUnderTheModuleReachingWhatPromisesMayNot\_IsNamedByTheSharedModuleRulesPromisesNamespace\_WhileANamesakeIsNot

```text
A PromisesModule constant that also takes in the old Delivery sources place: the test plants no old-place type for this key, so a constant broader than the module still passes.
```

###### Obligations






##### Covers

P1

```text
The architecture tests run with three made-up Tracker integration connectors: one reaches a Delivery service in Modules.Promises, one a Delivery service in the old Delivery sources place, one a namesake just outside Modules.Promises.
```

```text
Tracker integration's key for Promises and the update code names the connector reaching Modules.Promises exactly as it names the one reaching the old place, and does not name the one reaching the namesake.
```

```text
After the move, a connector that reached into Promises is still reported as reaching another context, so Tracker integration stays a leaf.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesRulesSeeTheModuleArchUnitTest\.cs\:\:AConnectorReachingADeliveryServiceUnderTheModule\_IsNamedByTheTrackerIntegrationRule\_AsOneReachingTheOldPlaceIs

```text
A widening that makes the Tracker integration key match Modules.Promises but drops the update-code alternatives: no connector reaching the update code is planted here, and the existing update-code case in the Tracker integration boundaries test is what catches that.
```

###### Obligations



##### Covers

P5

```text
The whole architecture test suite runs on today's code with the made-up Delivery services compiled into the test assembly and the rules widened.
```

```text
Every existing architecture test stays green, the Jira connector's listed edge to the forecast block renderer included; no production file changes.
```

```text
Nothing a user, a tracker or the database sees changes.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationModuleBoundariesArchUnitTest\.cs\:\:TheOnlyOtherContextTrackerIntegrationReaches\_IsPromisesThroughTheJiraConnectorMergingTheForecastBlock

```text
A widening that loosens an unrelated rule elsewhere in the suite while every list still matches: the existing tests guard only what they already pin.
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
Running the architecture-rules probe lists the Promises-related architecture tests as Passed, and the tests whose names state a made-up offender is reported (a Delivery service under Modules.Promises reaching a repository, Forecasting or the update code) show Passed, so such an offender is caught.
```

#### E2

##### Promises

P2

```text
The backend-tests probe on the Promises membership test shows it Passed, and its test names state that a missing listed type and an unlisted extra type each make it fail, for both the types that move and the Models types admitted into the module.
```

#### E3

##### Promises

P3

```text
The deliveries-answers-compare probe prints DIFFERENT when two revisions differ in one Delivery answer, and IDENTICAL when no Delivery answer differs (including base against candidate of this feature).
```

#### E4

##### Promises

P4

```text
The test-selection-show probe shows a path under Modules/Promises selects the backend suite only, and the Tracker integration port path for the Jira connector or Program.cs selects every live connector suite; the recorded selection scenarios pass.
```

#### E5

##### Promises

P5

```text
The ef-model-unchanged probe reports the database model unchanged, deliveries-answers-compare of base against candidate prints IDENTICAL, and the architecture-rules and backend-tests probes show every existing test Passed with none Failed.
```

## Charter

None.

## Examination

None.
