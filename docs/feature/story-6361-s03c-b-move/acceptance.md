# Acceptance brief: story\-6361\-s03c\-b\-move

## Slices

### S1

##### Covers

P1

```text
Read the compiled production assembly and ask where the sixteen simulation, draw stream, run plan and policy types live
```

```text
All sixteen live in the Forecasting Domain namespace and none remains under Services
```

```text
A developer opening the Forecasting module sees its pure simulation parts in one Domain folder, not scattered in Services
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.ForecastingDomainLayoutArchUnitTest\.TheForecastSimulationAndItsPureParts\_LiveInTheModuleDomain\_AndNoneIsLeftUnderServices

```text
A copy left under Services, or a type moved to the folder but keeping the old namespace
```

###### Obligations


##### Covers

P1

```text
Check the sixteen source files by name across the production project
```

```text
Each file sits in the Forecasting Domain folder and in no other folder
```

```text
The folder layout matches the namespace so the module reads as built
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.ForecastingDomainLayoutArchUnitTest\.TheForecastSimulationAndItsPurePartsFiles\_SitInTheModuleDomainFolder\_AndNowhereElse

```text
A namespace edit without moving the file, or a duplicate file elsewhere
```

###### Obligations


##### Covers

P1

```text
Apply the Domain dependency rule to the compiled Forecasting Domain
```

```text
Domain types reach only the Domain itself, the shared Models and the base library
```

```text
The simulation stays a pure calculation that cannot read a database, setting or logger
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.ForecastingDomainLayoutArchUnitTest\.TheForecastingDomain\_ReachesOnlyTheSharedKernelAndTheBaseLibrary

```text
A Domain type reaching Application, Adapters or Services
```

###### Obligations


##### Covers

P1

```text
Run the same Domain rule on made-up Domain types, one per forbidden layer, each beside a compliant twin, and on an empty Domain
```

```text
Only the offender is named with the forbidden type it reached, the twin is not, and an empty Domain fails naming its pattern
```

```text
The rule is trusted because it is shown to catch an offender and spare a clean type
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.ForecastingDomainLayoutArchUnitTest\.AForecastingDomainTypeThatReachesAForbiddenLayer\_IsReportedAndItsCompliantTwinIsNot

```text
A rule that flags everything or nothing
```

###### Obligations


##### Covers

P4

```text
Seed the same forecast inputs and run the forecast
```

```text
Forecast results equal the recorded answers
```

```text
Users see the same forecasts after the move
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\/Acceptance\/Forecasting\/SeededForecastsStayTheSameTest\.cs

```text
A move that changes draw order but is not caught by the recorded answers
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
Forecast simulation, draw streams, run plan and both forecast policies behave for the user as before the move. Their Domain folder placement is not user-observable and is referred to candidate review.
```

#### E2

##### Promises

P2

```text
Forecast, reality-check and filter-rule behaviour is unchanged for the user, and the Flow metrics rule lists still apply the same rules after the move. Application folder placement is not user-observable and is referred to candidate review.
```

#### E3

##### Promises

P3

```text
Forecast updates, start-up forecast seeding and the four forecast triggers happen for the user as before, with the same domain-event handler order. Adapter and policy folder placement is not user-observable and is referred to candidate review.
```

#### E4

##### Promises

P4

```text
Given identical inputs, the forecasts and forecast answers a user sees are identical before and after the move.
```

## Charter

None.

## Examination

None.
