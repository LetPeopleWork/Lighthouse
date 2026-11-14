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


### S2

##### Covers

P2

```text
Read the compiled production assembly and the production sources and check where the forecast, reality-check and filter-rule services, their ports, the lane and the updater port live
```

```text
All ten Application types are in the Forecasting Application namespace and folder and none is left under Services
```

```text
The forecast services sit in the Forecasting module's Application layer; a type left behind is named
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingApplicationLayoutArchUnitTest\.cs

```text
A type is moved in namespace but its file stays in the old folder, or the nested inputs record is forgotten; both are caught by the file and the type checks
```

###### Obligations


##### Covers

P2

```text
Read the two Flow metrics rule lists
```

```text
Both lists name the moved forecast types by their new Application full names and no entry names a Services namespace for them
```

```text
The Flow metrics rules compare against lists that name the forecast services where they now live
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ForecastingApplicationLayoutArchUnitTest\.cs

```text
A list keeps a stale Services name next to the new one, or drops an entry; both are caught
```

###### Obligations


##### Covers

P4

```text
Run the architecture rules and the seeded forecasts check after the move
```

```text
Layout and list checks pass with no behaviour change; the seeded forecast answers stay the same
```

```text
Forecasts and forecast answers are identical before and after the move
```

##### Disposition

NotTested

```text
Seeded forecasts check and forecast answers comparison already exist from feature A and run as declared commands; this oracle adds no new observation of them
```

crafter

### S3

##### Covers

P3

```text
Read the compiled production assembly and check where the forecast updater and the start-up seeder live
```

```text
Both live in the Forecasting module's background adapters and none is left under Services; a missing or misplaced one is named
```

```text
Forecast refresh and start-up seeding behave as before
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.ForecastingAdaptersAndPoliciesLayoutArchUnitTest\.TheForecastUpdaterAndTheStartUpSeeder\_LiveInTheModuleBackgroundAdapters\_AndNoneIsLeftUnderServices

```text
A file moved to the right folder that keeps a stale namespace only passes the file check; this namespace check catches it
```

###### Obligations


##### Covers

P3

```text
List the production source files and look for the updater and seeder files
```

```text
Each file sits only in the module's background adapters folder
```

```text
No visible change
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.ForecastingAdaptersAndPoliciesLayoutArchUnitTest\.TheForecastUpdaterAndTheStartUpSeederFiles\_SitInTheModuleBackgroundFolder\_AndNowhereElse

```text
A stale namespace in the right folder; the namespace check catches that
```

###### Obligations


##### Covers

P3

```text
Read the compiled production assembly and check where the four forecast triggers live
```

```text
All four live in the module's Application policies and none is left in the old update namespace; a trigger left behind is named
```

```text
Forecasts retrigger exactly as before
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.ForecastingAdaptersAndPoliciesLayoutArchUnitTest\.TheFourForecastTriggers\_LiveInTheModulePolicies\_AndNoneIsLeftUnderServices

```text
A changed handler registration order; the order pin owns that
```

###### Obligations


##### Covers

P3

```text
List the production source files and look for the four trigger files
```

```text
Each file sits only in the module's policies folder
```

```text
No visible change
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.ForecastingAdaptersAndPoliciesLayoutArchUnitTest\.TheFourForecastTriggerFiles\_SitInTheModulePoliciesFolder\_AndNowhereElse

```text
A stale namespace in the right folder; the namespace check catches that
```

###### Obligations


##### Covers

P3

```text
Run the domain event handler order pin after the move
```

```text
The pin stays green unmodified
```

```text
Handlers run in the same order, so forecasts refresh as before
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.DomainEventHandlerOrderPinTest

```text
A handler dropped from registration if the pin does not list it; it lists the trigger
```

###### Obligations


##### Covers

P4

```text
Compare seeded forecasts and forecast answers before and after the move
```

```text
Identical
```

```text
Forecast answers do not change
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.SeededForecastsStayTheSameTest

```text
A change outside the seeded cases; the compare script covers base against HEAD
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
