# Acceptance brief: story\-6358\-s02b\-flow\-metrics\-move

## Slices

### S1

##### Covers

P1

```text
Read the compiled production assembly for the six pure Flow metrics calculation types, by name
```

```text
Each of the six sits in the Flow metrics module's Domain namespace and no type of the same name is left under Services; a type that has not moved is named twice, as missing from the Domain and as left behind
```

```text
A developer finds the pure Flow metrics calculations in the module's Domain layer, nowhere else
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.FlowMetricsModuleLayoutArchUnitTest\.ThePureFlowMetricsCalculations\_LiveInTheModuleDomain\_AndNoneIsLeftUnderServices

```text
A copy of a calculation kept under a namespace other than Services, for example the API, next to the moved one
```

###### Obligations

None.

##### Covers

P1

```text
Read the production project's source folders for the four files that hold the calculations
```

```text
Each file sits in the module's Domain folder and no file of the same name sits anywhere else in the production project
```

```text
The folder a developer opens matches the namespace the type declares
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.FlowMetricsModuleLayoutArchUnitTest\.ThePureFlowMetricsCalculationFiles\_SitInTheModuleDomainFolder\_AndNowhereElse

```text
A file in the right folder whose namespace line was not changed; the type placement test catches that one
```

###### Obligations

None.

##### Covers

P2

```text
Read every type the real module's Domain namespace references, async bodies included
```

```text
The Domain holds at least one type and reaches only its own Domain, the Models, System types and compiler-made helper types
```

```text
The module's pure calculations can be tested by handing them values, with no database, setting or logger behind them
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.FlowMetricsModuleLayoutArchUnitTest\.TheModuleDomain\_ReachesOnlyTheSharedKernelAndTheBaseLibrary

```text
A Domain type that reads a file or the clock through the base library, or reaches a forbidden type only by a type name in a string
```

###### Obligations

None.

##### Covers

P2

```text
Run the same Domain rule on made-up Domain types that each reach one forbidden layer (Services, the data layer, EF Core, Microsoft.Extensions, the module's Application, the module's Adapters, and Services only inside an async method), each next to a compliant twin
```

```text
Exactly the offender is reported, named with the forbidden type it reached, also in a Release build; the twin, which uses a Models type, System types, a sibling, a lambda, an iterator, a written-out array and a generic parameter, is examined and not reported
```

```text
A Domain change that reaches a forbidden layer fails the tests, and a clean Domain change does not
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.FlowMetricsModuleLayoutArchUnitTest\.ADomainTypeThatReachesAForbiddenLayer\_IsReportedAndItsCompliantTwinIsNot

```text
A Domain type that reaches a forbidden type only through an attribute argument of a kind the IL reader does not walk
```

###### Obligations

None.

##### Covers

P2

```text
Run the Domain rule on a Domain namespace that holds no type
```

```text
The rule fails and names the Domain pattern that held nothing
```

```text
A Domain rule pointed at the wrong namespace cannot stay green unseen
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.FlowMetricsModuleLayoutArchUnitTest\.ADomainThatHoldsNoType\_FailsTheRuleInsteadOfPassingUnseen

```text
A Domain pattern that matches one unrelated type counts as not empty
```

###### Obligations

None.

##### Covers

P3

```text
Run the existing unit tests of the four moved calculation files after the move, with only using and namespace edits
```

```text
Every row stays green with the same expected values
```

```text
Percentiles, process limits, SLE risk and baseline checks give the same answers as before
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Services\.Implementation \(PercentileCalculatorTest\, XmRCalculatorTest\, SleRiskCalculatorTest\, BaselineValidationServiceTest\)

```text
A behaviour change in a branch none of the existing rows reaches
```

###### Obligations

None.

##### Covers

P3

```text
Capture every metrics API answer on the feature base and on the candidate, back to back on the same day, and compare them
```

```text
The two captures are identical
```

```text
Nothing a user sees in the metrics changes
```

##### Disposition

NotTested

```text
This oracle reads layout and dependencies only; the design runs the answer capture and comparison once at the end of the feature, and the filtered full test run and the handler-order pin at the end of each slice
```

host \(root\)\, at slice end and feature end

### S2

##### Covers

P1

```text
Read the compiled production assembly for every type the eleven Flow metrics contract files declare, eighteen types with their records and companion interface, by name
```

```text
Each sits in the Flow metrics module's Application namespace exactly, not in another layer, and no type of the same name is left under Services; a type that has not moved is named as missing from Application and as left behind
```

```text
A developer finds the Flow metrics contract in the module's Application layer, nowhere else
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.FlowMetricsModuleLayoutArchUnitTest\.TheFlowMetricsContract\_LivesInTheModuleApplication\_AndNoneOfItIsLeftUnderServices

```text
A copy of a contract type kept under a namespace other than Services, for example the API or the module's Domain, next to the moved one
```

###### Obligations

None.

##### Covers

P1

```text
Read the production project's source folders for the eleven contract files
```

```text
Each file sits in the module's Application folder and no file of the same name sits anywhere else in the production project
```

```text
The folder a developer opens matches the namespace the contract declares
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.FlowMetricsModuleLayoutArchUnitTest\.TheFlowMetricsContractFiles\_SitInTheModuleApplicationFolder\_AndNowhereElse

```text
A file in the right folder whose namespace line was not changed; the contract placement test catches that one
```

###### Obligations

None.

##### Covers

P3

```text
Run the Architecture tests after the move, the calculation placement, the Domain rule and the event-handler order pin included
```

```text
Every earlier layer check and the handler-order pin stay green with only using and namespace edits
```

```text
Moving the contract changes nothing already in place, and the metrics handlers still run in the same order
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture \(FlowMetricsModuleLayoutArchUnitTest earlier tests\, DomainEventHandlerOrderPinTest\)

```text
A behaviour change in a metrics service that no architecture test reads; the existing unit and integration suites and the answer capture own that
```

###### Obligations

None.

##### Covers

P3

```text
Capture every metrics API answer on the feature base and on the candidate, back to back on the same day, and compare them
```

```text
The two captures are identical
```

```text
Nothing a user sees in the metrics changes
```

##### Disposition

NotTested

```text
This oracle reads layout only; the design runs the answer capture and comparison once at the end of the feature, and the filtered full test run at the end of each slice
```

host \(root\)\, at slice end and feature end

### S3

##### Covers

P1

```text
Read the compiled production assembly for the sixteen Flow metrics implementation types, by name
```

```text
Each sits in the Flow metrics module's Application namespace exactly, not in another layer, and no type of the same name is left under Services; a type that has not moved is named as missing from Application and as left behind
```

```text
A developer finds the Flow metrics services, writers, queries and their helpers in the module's Application layer, nowhere else
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.FlowMetricsModuleLayoutArchUnitTest\.TheFlowMetricsImplementations\_LiveInTheModuleApplication\_AndNoneIsLeftUnderServices

```text
A copy of an implementation kept under a namespace other than Services, for example the module's Domain or the API, next to the moved one
```

###### Obligations

None.

##### Covers

P1

```text
Read the production project's source folders for the sixteen implementation files
```

```text
Each file sits in the module's Application folder and no file of the same name sits anywhere else in the production project
```

```text
The folder a developer opens matches the namespace the implementation declares
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture\.FlowMetricsModuleLayoutArchUnitTest\.TheFlowMetricsImplementationFiles\_SitInTheModuleApplicationFolder\_AndNowhereElse

```text
A file in the right folder whose namespace line was not changed; the implementation placement test catches that one
```

###### Obligations

None.

##### Covers

P3

```text
Run the Architecture tests after the move, the earlier placement checks, the Domain rule, the application-core rules and the event-handler order pin included
```

```text
Every earlier check and the handler-order pin stay green with only using, namespace and path-string edits
```

```text
Moving the implementations changes nothing already in place, and the metrics handlers still run in the same order
```

##### Disposition

Tested

Lighthouse\.Backend\.Tests\.Architecture \(FlowMetricsModuleLayoutArchUnitTest earlier tests\, ApplicationCoreRulesReachModulesArchUnitTest\, DomainEventHandlerOrderPinTest\)

```text
A behaviour change in a metrics service that no architecture test reads; the existing unit and integration suites and the answer capture own that
```

###### Obligations

None.

##### Covers

P3

```text
Capture every metrics API answer on the feature base and on the candidate, back to back on the same day, and compare them
```

```text
The two captures are identical
```

```text
Nothing a user sees in the metrics changes
```

##### Disposition

NotTested

```text
This oracle reads layout only; the design runs the answer capture and comparison once at the end of the feature, and the filtered full test run at the end of each slice
```

host \(root\)\, at slice end and feature end

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
