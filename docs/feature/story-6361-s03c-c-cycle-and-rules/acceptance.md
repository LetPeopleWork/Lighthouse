# Acceptance brief: story\-6361\-s03c\-c\-cycle\-and\-rules

## Slices

### S2

##### Covers

P2

```text
Read the compiled production code and look for any Flow metrics type that uses a Forecasting type, with no list of allowed exceptions
```

```text
No Flow metrics type uses a Forecasting type; today the rule fails and names the three uses: the base metrics service on the forecast service, and the Team metrics service on the forecast filter rule service and on the forecast data sufficiency policy
```

```text
Flow metrics no longer depends on Forecasting, so the two modules no longer form a cycle
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsDoesNotReachForecastingArchUnitTest\.cs\:\:FlowMetrics\_ReachesNoForecastingType

```text
A Flow metrics type reaches Forecasting only through a type the IL reader cannot see (a catch clause, an event or a named attribute argument)
```

###### Obligations


##### Covers

P2

```text
Read the compiled production code for the three questions Flow metrics asks Forecasting: the throughput filter, the forecast data sufficiency and the how-many forecaster
```

```text
Each is an interface in Flow metrics' Application and a Forecasting type implements each; today the rule fails and names all three as missing
```

```text
Flow metrics owns the questions it asks, in its own words, and Forecasting is the one that answers them
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsDoesNotReachForecastingArchUnitTest\.cs\:\:TheThreeQuestionsFlowMetricsAsks\_AreItsOwnPorts\_AndForecastingAnswersEach

```text
A port carries a member that is never called, or the Forecasting answer is registered with a different lifetime; those are left to the answer comparisons and the review
```

###### Obligations


##### Covers

P2

```text
Run the same no-reach rule over made-up modules: a metrics type taking a Forecasting service in its constructor, one calling a static Forecasting policy, and one resolving a Forecasting service from the service provider
```

```text
Each fails and is named with the Forecasting type it reaches
```

```text
Any of the three ways the cycle could come back is caught and named
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsDoesNotReachForecastingArchUnitTest\.cs\:\:AMetricsTypeReachingForecasting\_FailsAndIsNamedWithWhatItReaches

```text
A rule that names the edge but also reports an unrelated second edge in the same message
```

###### Obligations


##### Covers

P2

```text
Run the port rule over a made-up module whose port is declared in Forecasting instead of Flow metrics' Application
```

```text
The rule fails and names where the port actually lives
```

```text
A port that moves back into Forecasting is caught and named
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsDoesNotReachForecastingArchUnitTest\.cs\:\:APortDeclaredInForecastingInsteadOfFlowMetrics\_FailsAndIsNamedWithWhereItLives

```text
A port declared in a sub-namespace of Flow metrics' Application is refused although it is still Flow metrics' own; the design places the ports in the Application namespace itself
```

###### Obligations


##### Covers

P5

```text
Capture the metrics answers on demo data before and after the change and compare them
```

```text
Every metrics answer is identical, including the forecast predictability score that uses the how-many forecast and the forecast throughput status that uses the filter and the sufficiency check
```

```text
Users see the same metrics after the change
```

##### Disposition

Tested

Scripts\/examine\/bin\/metrics\-answers\-compare

```text
A difference only in an answer the capture does not ask for
```

###### Obligations

None.

##### Covers

P5

```text
Capture the forecasts and forecast answers before and after the change, and compute the seeded forecasts and the Flow metrics unit and filter-forecast tests
```

```text
Forecasts and forecast answers are identical, the seeded forecasts keep their pinned values, and the Flow metrics and filter-forecast tests stay green unchanged in their assertions
```

```text
Users get the same forecasts after the change
```

##### Disposition

Tested

Scripts\/examine\/bin\/forecast\-answers\-compare

```text
A difference only for an input neither the capture nor the seeded set covers
```

###### Obligations

None.

##### Covers

P5

```text
On demo data, refresh a Team and restart the application, then read the forecasts through the public API
```

```text
Forecasts are recomputed after the Team refresh and seeded at start-up, as before the change
```

```text
Forecasts still refresh after a Team refresh and at start-up
```

##### Disposition

Tested

Scripts\/examine\/bin\/forecast\-triggers\-show

```text
A trigger fires but recomputes with a stale answer; the answer comparisons cover the values
```

###### Obligations

None.

##### Covers

P2

```text
Run both rules over a made-up module whose metrics type asks only through its own port, which a made-up Forecasting type implements
```

```text
The no-reach rule looks at the metrics type, finds no edge and passes; the port rule passes
```

```text
Asking Forecasting through a port Flow metrics owns stays allowed
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsDoesNotReachForecastingArchUnitTest\.cs\:\:AMetricsTypeAskingOnlyThroughItsOwnPort\_PassesBothRules

```text
A rule that counts implementing a metrics port as reaching Forecasting would fail here, so only an over-lenient rule passes; the failing cases pin that side
```

###### Obligations


##### Covers

P2

```text
Run the port rule over a made-up module whose port only a Flow metrics type implements, with a Forecasting type present that answers something else
```

```text
The rule fails and names the Flow metrics type that answers the port instead of Forecasting
```

```text
An answer copied into Flow metrics instead of asked of Forecasting is caught and named
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsDoesNotReachForecastingArchUnitTest\.cs\:\:APortThatOnlyFlowMetricsAnswers\_FailsAndIsNamedWithWhoAnswersIt

```text
Forecasting implements the port but the composition root registers a Flow metrics copy instead; the answer comparisons cover that
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
A user running the public command on demo data sees that forecasts are recomputed after a Team refresh, and that the start-up forecast seeding fires.
```

#### E2

##### Promises

P2

```text
Flow metrics code depends on no Forecasting type; every question it puts to Forecasting goes through exactly three Flow-metrics-owned ports that Forecasting answers.
```

#### E3

##### Promises

P3

```text
Forecasting has its own module rules with inline lists, Flow metrics' inbound rule list names no Forecasting type, and the shared rule's wording names the module it checks.
```

#### E4

##### Promises

P4

```text
Forecasting's unit tests sit under Lighthouse.Backend.Tests/Modules/Forecasting, and a placement test fails if they are moved elsewhere.
```

#### E5

##### Promises

P5

```text
For the same inputs, metrics answers, forecasts and forecast answers are identical before and after the change.
```

#### E6

##### Promises

P5

P1

```text
After the change, forecasts still refresh after a Team refresh and at start-up.
```

## Charter

None.

## Examination

None.
