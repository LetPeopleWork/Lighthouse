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


### S2

##### Covers

P2

```text
The membership check reads the Promises types from the compiled code: every top-level type in the delivery-source namespaces or under Modules.Promises, plus the rule service, its interface, the metric-values projector with its values record and the two reactions to updated forecasts in the namespaces they live in today, and compares them with the Moves and Models lists.
```

```text
The check passes when the lists hold exactly the 15 measured types and the 2 admitted Models types, and no type is on both lists; with the lists not yet filled it fails and names each of the 15 types as not on the list.
```

```text
Before any Promises file moves, the maintainer has one test that names every type of the context, so a move that forgets or drops a type fails.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesMembershipArchUnitTest\.cs\:\:ThePromisesTypesInTheCompiledCode\_AreExactlyTheTypesListed\, NoTypeIsOnBothLists

```text
A new Promises type added to the flat Services.Implementation or Services.Interfaces namespace under a name not among the six found by name is not collected, so it is not named.
```

###### Obligations


##### Covers

P2

```text
A listed type is renamed in the compiled code, and separately a listed type is held twice under one name in two namespaces, as a move that leaves a copy behind would.
```

```text
The rename fails and names the old name as on the list but no longer found and the new name as not on the list; the copy fails and names both types by their full names and the short name as no longer found.
```

```text
A rename or a half-done move during the Promises move cannot pass unseen; the failure says which name to fix.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesMembershipArchUnitTest\.cs\:\:AListedTypeRenamedInTheCode\_IsNamedAsMissing\_AndItsNewNameAsUnlisted\, ACopyOfAListedTypeInAnotherNamespace\_IsNamedByItsFullName

```text
A listed type replaced by a different type with the same name in the same namespace is accepted, because the check compares names, not content.
```

###### Obligations


##### Covers

P2

```text
A made-up Delivery service is placed under Modules.Promises and a namesake just outside it, under Modules.PromisesFixtures, and the check runs over the real code plus these made-up types.
```

```text
The check fails and names the made-up service as not on the list; it names neither the namesake nor any listed real type as missing.
```

```text
A type added to the Promises module during the move joins the context only through the list; a namespace that only starts with the same letters is not Promises.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesMembershipArchUnitTest\.cs\:\:AMadeUpTypeInTheModule\_IsNamedAsUnlisted\_AndANamesakeOutsideAndANestedTypeAreNot

```text
A made-up Promises type placed in a namespace that is neither a delivery-source namespace nor under Modules.Promises, for example Services.Implementation.Promises, is not collected and not named.
```

###### Obligations


##### Covers

P2

```text
The made-up Delivery service under Modules.Promises holds a nested record, and the check runs over it.
```

```text
The nested record is not named on its own; only its outer type is named as not on the list.
```

```text
The lists hold top-level types only, so a nested type moves with the type that holds it and needs no entry.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesMembershipArchUnitTest\.cs\:\:AMadeUpTypeInTheModule\_IsNamedAsUnlisted\_AndANamesakeOutsideAndANestedTypeAreNot

```text
A list entry written for a nested type is reported only as no longer found, so the failure does not say that nested types need no entry.
```

###### Obligations


##### Covers

P2

```text
Every Delivery type of the shared Models, and whatever sits in the Promises Domain once moved, is judged against four conditions: stored by the database context, sent by a controller, reached by a Models type that stays, used by other code that stays. The admitted ones are compared with the Models list.
```

```text
Of 25 candidates, DeliveryForecastBlock and DeliveryForecastBlockPercentile are admitted and must be listed; each of the other 23 is refused for the condition its row names, and the failure lists every refused candidate with its reasons.
```

```text
The maintainer sees which Delivery Models types may move with Promises and why each other one stays.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PromisesMembershipArchUnitTest\.cs\:\:TheModelsTypesListedToMove\_AreExactlyTheDeliveryCandidatesNeitherStoredNorOnTheWireNorUsedOutsideTheModule\, ADeliveryModelsType\_IsJudgedAsTheCompiledCodeShows

```text
A Delivery Models type sent by a controller only through a variable typed object, or built by a helper the controller does not call directly, is not seen as on the wire and could be admitted.
```

###### Obligations


##### Covers

P5

```text
The backend builds and its whole suite runs without the live-connector categories, with only the two new test files added.
```

```text
The build has no warnings and every existing test stays green; no production file changes.
```

```text
Nothing a user, a tracker or the database sees changes.
```

##### Disposition

Tested

Lighthouse\.Backend \(dotnet build\; dotnet test with the live\-connector categories excluded\)

```text
A behaviour change that no existing test observes would pass; this slice changes no production file, so none can come from it.
```

###### Obligations


### S3

##### Covers

P3

```text
The Delivery-answers examine command compares two captures of the Delivery answers in which one Delivery's metrics history changed and everything else matches.
```

```text
It exits 1, its last line is a DIFFERENT verdict, it reports that 1 of the 2 Delivery metrics histories compared differs, and it names the changed answer, Delivery 7's metrics history.
```

```text
The examiner sees that a Delivery answer changed and which one.
```

##### Disposition

Tested

Scripts\/examine\/test\-deliveries\-answers\-compare\.sh\:\:one Delivery answer changed

```text
A compare that finds the change only through the answer files' bytes, so a change hidden by a masked key or by reordered JSON keys of equal meaning would be reported the same way as a real change.
```

###### Obligations


##### Covers

P3

```text
The Delivery-answers examine command compares two separate copies of the same capture of five Delivery answers: a Portfolio's Delivery list, its Delivery rules schema, its Delivery sources and the metrics history of its two Deliveries.
```

```text
It exits 0 and its last line is an IDENTICAL verdict over all 5 answers.
```

```text
The examiner sees that no Delivery answer changed, and that every captured answer was compared.
```

##### Disposition

Tested

Scripts\/examine\/test\-deliveries\-answers\-compare\.sh\:\:identical Delivery answers

```text
A compare that skips an answer present in only one of the two captures as long as the count of compared answers still comes to 5.
```

###### Obligations


##### Covers

P3

```text
The Delivery-answers examine command compares two empty captures.
```

```text
It does not exit 0, it gives no IDENTICAL verdict, and it says that nothing was compared.
```

```text
The examiner cannot mistake an empty capture for unchanged Delivery answers.
```

##### Disposition

Tested

Scripts\/examine\/test\-deliveries\-answers\-compare\.sh\:\:nothing captured

```text
A compare that fails on empty captures but passes a capture holding only error answers; the shared stage refuses those, and this check does not plant one.
```

###### Obligations


##### Covers

P3

```text
The examiner looks up the commands it may run.
```

```text
The list admits deliveries-answers-compare at Scripts/examine/bin/deliveries-answers-compare.
```

```text
The examiner may run the Delivery-answers comparison.
```

##### Disposition

Tested

Scripts\/examine\/test\-deliveries\-answers\-compare\.sh\:\:admitted examine probe

```text
An admitted entry whose built command captures the wrong Delivery endpoints; only the examiner's real run shows what is captured.
```

###### Obligations

None.

##### Covers

P3

```text
The Delivery-answers examine command captures, on two revisions and per demo scenario and Portfolio, the Delivery list, each Delivery's metrics history, the Delivery rules schema and the Delivery sources, and compares them.
```

```text
IDENTICAL with exit 0 when the revisions answer the same, DIFFERENT with exit 1 when a Delivery answer changed, and a failure when nothing was captured or an answer is not a success.
```

```text
The examiner can show that moving the Promises code changed no Delivery answer.
```

##### Disposition

NotTested

```text
A real capture builds and runs two revisions of the backend and takes minutes, beyond a test's time budget; the oracle drives the compare stage on fixture captures, and the capture runs as the examiner's probe.
```

EXAMINE\: bash Scripts\/examine\/build\-examine\-commands \&\& bash Scripts\/examine\/bin\/deliveries\-answers\-compare

##### Covers

P5

```text
The existing backend test suite runs after the Delivery-answers examine command is added.
```

```text
Every existing test stays green; only files under Scripts and the examine commands list change.
```

```text
Nothing a user, a tracker or the database sees changes.
```

##### Disposition

Tested

dotnet test Lighthouse\.Backend \-\-filter TestCategory\!\=JiraIntegration\&TestCategory\!\=LinearIntegration\&TestCategory\!\=AdoIntegration\&TestCategory\!\=ServiceNowIntegration\&TestCategory\!\=GithubIntegration

```text
A change to the shared compare piece that alters the output of the existing forecast and refresh comparisons, which no backend test runs.
```

###### Obligations

None.

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
