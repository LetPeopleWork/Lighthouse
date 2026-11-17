# Acceptance brief: story\-6362\-s03d\-b\-move

## Slices

### S1

##### Covers

P1

```text
Read the compiled assembly and look up the two Delivery forecast block records by name
```

```text
Both records live in the Promises Domain, none is left under the old Models folder, the Domain holds exactly them, and it reaches only the shared kernel and the base library
```

```text
Nothing a user sees changes; the Delivery forecast records sit in the Promises module's pure layer
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesLayoutArchUnitTest\.cs\:\:TheForecastBlockRecords\_LiveInThePromisesDomain\_AndNoneIsLeftUnderModelsDeliverySources

```text
the Domain holds the records plus a type that reaches a repository (covered by the Domain-reach and exact-holdings tests), or the records are copied into the Domain while the old copy stays
```

###### Obligations



##### Covers

P1

```text
Read the compiled assembly and look up the rule service, projector with its values, renderer, publishing service, source resolver with the portfolio source preview, source sync and their interfaces by name
```

```text
Every one lives in the Promises Application and none is left under Services; a missing or renamed name fails as missing
```

```text
Delivery rules, sources and forecast publishing keep working from their new home in the Promises module
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesLayoutArchUnitTest\.cs\:\:TheDeliveryRuleServiceAndItsInterface\_LiveInThePromisesApplication\_AndNoneIsLeftUnderServices

```text
an interface moved while its class stays behind, or a list name drifts from the type so the check looks at nothing
```

###### Obligations




##### Covers

P2

```text
Read the compiled test assembly and look up the seven moved unit-test classes by name
```

```text
Each lives in the matching Promises Application test folder and none is left under the old Services test folders
```

```text
Contributors find the unit tests next to the module they protect
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesLayoutArchUnitTest\.cs\:\:TheUnitTestsOfTheMovedServices\_LiveInThePromisesApplicationTestFolder\_AndNoneIsLeftUnderServices

```text
a test class is copied to the new folder while the old one stays (the left-behind check names it)
```

###### Obligations


##### Covers

P4

```text
Run the Domain rule on a made-up Domain type that reaches a repository, next to a compliant twin
```

```text
The failure names the offender and the repository it reached; the twin is not reported
```

```text
No user-visible change; the Domain rule cannot go vacuous
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesLayoutArchUnitTest\.cs\:\:APromisesDomainTypeThatReachesARepository\_IsReported\_AndItsCompliantTwinIsNot

```text
a rule that reports every Domain type, or none
```

###### Obligations




##### Covers

P4

```text
Run the layout rule on a list name that no type in the assembly matches
```

```text
The rule fails naming the missing type
```

```text
No user-visible change; a drifted list cannot pass unseen
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesLayoutArchUnitTest\.cs\:\:ANameThatNoTypeMatches\_FailsTheLayoutRuleAsMissing\_InsteadOfPassingUnseen

```text
a rule that ignores names with no type
```

###### Obligations


### S2

##### Covers

P1

```text
Read the compiled production assembly for the two handlers that react to a Portfolio's forecasts being updated
```

```text
Both handlers live in the Promises module's Application policies and none is left under the Services domain-event folders; a missing or misplaced one is named
```

```text
Nothing visible changes; the Delivery code sits in its module
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesPoliciesLayoutArchUnitTest\.cs\:\:TheTwoForecastsUpdatedHandlers\_LiveInThePromisesApplicationPolicies\_AndNoneIsLeftUnderServices

```text
A handler is copied into the module and also kept elsewhere outside the Services folders; the old-home pattern covers only Services, which is where both live today
```

###### Obligations



##### Covers

P2

```text
Read the compiled test assembly for the two handler unit-test classes
```

```text
Both test classes live in the Promises policies test folder and none is left under the Services tests
```

```text
Nothing visible changes; the unit tests sit beside their module
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesPoliciesLayoutArchUnitTest\.cs\:\:TheUnitTestsOfTheTwoForecastsUpdatedHandlers\_LiveInThePromisesPoliciesTestFolder\_AndNoneIsLeftUnderServices

```text
A test class is moved to the right namespace but its rows are dropped; only placement is observed, the other tests keep their own outcomes
```

###### Obligations


##### Covers

P2

```text
Name a handler that no type matches
```

```text
The layout rule fails naming the missing type instead of passing unseen
```

```text
A stale list cannot hide a missing handler
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesPoliciesLayoutArchUnitTest\.cs\:\:AHandlerNameThatNoTypeMatches\_FailsTheLayoutRuleAsMissing\_InsteadOfPassingUnseen

```text
A rule that skips a missing name; it would fail here because the missing name must be reported by full name (passes on arrival: it pins the rule)
```

###### Obligations


##### Covers

P3

```text
Read the handler registration order of PortfolioForecastsUpdated from the composition root
```

```text
Registered in order: Delivery metric snapshot recording, then Delivery forecast publishing, and no other handler; reversed or a missing registration fails naming the position
```

```text
A Portfolio's Delivery snapshot is recorded before its forecasts are published, as today
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs\:\:HandlersOfAPortfolioForecastUpdate\_RecordTheDeliverySnapshotBeforePublishingTheForecasts

```text
The handlers keep their order but change DI lifetime, which this pin does not observe; passes on arrival because the order already holds
```

###### Obligations




##### Covers

P4

```text
Run the existing order-pin tests unchanged beside the new entry
```

```text
The five existing order pins keep their outcome; nothing user-visible changes
```

```text
Every Delivery answer and forecast publication stays the same
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/DomainEventHandlerOrderPinTest\.cs\:\:HandlersOfATeamRefresh\_UpdateTheTeamMetricsBeforeAnyOtherReaction

```text
A behaviour change outside event order, which the full suite run at the end of the slice observes
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
Every Promises type sits in its designed place: the forecast block in Domain, the services, interfaces and projector in Application, and the two forecasts-updated handlers in Application Policies. No Promises type is left in its old namespace, and Domain reaches only the shared kernel. The architecture-rules probe reports each rule that places or bounds these types as Passed, by name.
```

#### E5

##### Promises

P2

```text
The unit tests of each moved Promises type run and pass. The probe `backend-tests Tests.Modules.Promises` lists each of their tests by name as Passed, and its totals line shows 0 Failed. The probe `architecture-rules Promises` lists each architecture rule that names a moved type by name as Passed.
```

#### E6

##### Promises

P3

```text
When a Portfolio's forecasts are updated, its Delivery metric snapshot is recorded before its forecasts are published. The probe `backend-tests DomainEventHandlerOrderPin` lists the test HandlersOfAPortfolioForecastUpdate_RecordTheDeliverySnapshotBeforePublishingTheForecasts by name as Passed.
```

#### E7

##### Promises

P4

```text
Nothing a user, a tracker or the database sees changes, and the existing backend tests stay green. The probe `deliveries-answers-compare` (no arguments) prints IDENTICAL. The probe `refresh-reactions-compare` (no arguments) prints IDENTICAL. The probe `forecast-answers-compare` (no arguments) prints IDENTICAL. The probe `ef-model-unchanged` prints "No changes have been made to the model since the last migration." for each provider it lists. The probe `backend-tests` (no fragment) prints 0 Failed in its totals line.
```

## Charter

None.

## Examination

None.
