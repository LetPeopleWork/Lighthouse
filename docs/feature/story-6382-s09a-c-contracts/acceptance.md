# Acceptance brief: story\-6382\-s09a\-c\-contracts

## Slices

### S1

##### Covers

P1

```text
Read the map of what Licensing and Access publish, without events
```

```text
Exactly the two Access types and the one Licensing type the other contexts reach are listed
```

```text
The published list cannot shrink to nothing and pass unseen
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingAndAccessPublishTheirContractsArchUnitTest\.cs

EachContextPublishesTheTypesTheOthersReach\_AndTheListOfThemIsNotEmpty

```text
The list is emptied or loses a type, because every other check then looks at nothing
```

###### Obligations




##### Covers

P1

```text
Look up each published type of Licensing and Access in the compiled code
```

```text
Each is found exactly once, in the Contracts namespace of its own context
```

```text
Other contexts have one published place to name each of these types
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingAndAccessPublishTheirContractsArchUnitTest\.cs

EachPublishedTypeIsFoundOnceAndInTheContractsOfItsOwnContext

```text
A type is kept in its old place, present in both places, or put into another context's Contracts, only if the check looked at the old place or counted loosely
```

###### Obligations




##### Covers

P1

```text
Look in the old namespace of each published type
```

```text
None of them is found there
```

```text
Nothing of Licensing and Access can be reached outside its Contracts
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingAndAccessPublishTheirContractsArchUnitTest\.cs

NoPublishedTypeIsLeftInTheNamespaceItHadBefore

```text
A copy stays behind in the old namespace while the new one also exists
```

###### Obligations




##### Covers

P1

```text
Scan the compiled production and test code outside Licensing and Access for references to the old names
```

```text
No type names any published type through its old namespace
```

```text
Every use of Licensing and Access goes through their Contracts
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/LicensingAndAccessPublishTheirContractsArchUnitTest\.cs

NoTypeOutsideLicensingAndAccessNamesAPublishedTypeByItsOldName

```text
A type outside the two contexts still uses the old name, only if the scan skipped nested, test or generic uses
```

###### Obligations




### S2

##### Covers

P2

```text
Read the map of what Flow metrics and Forecasting publish, without events
```

```text
Thirteen types are listed: ten of Flow metrics and three of Forecasting; the list is neither empty nor shrunk
```

```text
A check over the published types cannot pass by looking at nothing
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsAndForecastingPublishTheirContractsArchUnitTest\.cs

FlowMetricsAndForecastingPublishTheirContractsArchUnitTest\.EachContextPublishesTheTypesTheOthersReach\_AndTheListOfThemIsNotEmpty

```text
An empty or shorter list of published types would pass all later checks; this row fails it
```

###### Obligations


##### Covers

P2

```text
Look up each published type of Flow metrics and Forecasting in the compiled code
```

```text
Each is found exactly once, in the Contracts namespace of its own context
```

```text
Other contexts find Flow metrics and Forecasting types only in their Contracts
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsAndForecastingPublishTheirContractsArchUnitTest\.cs

FlowMetricsAndForecastingPublishTheirContractsArchUnitTest\.EachPublishedTypeIsFoundOnceAndInTheContractsOfItsOwnContext

```text
A type copied into Contracts and kept in the old place, or moved into another context Contracts, is not accepted by this row
```

###### Obligations



##### Covers

P2

```text
Look for each published type in the namespace it had before the move
```

```text
None is left in its old namespace
```

```text
No published type remains reachable outside Contracts
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsAndForecastingPublishTheirContractsArchUnitTest\.cs

FlowMetricsAndForecastingPublishTheirContractsArchUnitTest\.NoPublishedTypeIsLeftInTheNamespaceItHadBefore

```text
An unrelated type of the same name in another namespace would not matter; only the old namespace and name together are checked
```

###### Obligations



##### Covers

P2

```text
Scan the compiled production and test code outside Flow metrics and Forecasting for use of a published type
```

```text
No type names a published type by its old full name
```

```text
Behaviour is unchanged and every user of a moved type follows the move; the scan reads compiled code only, so a full name written inside a string is not covered by this row
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FlowMetricsAndForecastingPublishTheirContractsArchUnitTest\.cs

FlowMetricsAndForecastingPublishTheirContractsArchUnitTest\.NoTypeOutsideFlowMetricsAndForecastingNamesAPublishedTypeByItsOldName

```text
An old full name kept only inside a string literal would pass, because the scan reads compiled type references only
```

###### Obligations



### S3

##### Covers

P3

```text
Read the measured map of what Work Items publishes, without events.
```

```text
Exactly five types: blocked items, refresh log, rule evaluation, rule fields, what the forecast waits for. Passes today (list already measured); guards against an empty list making the other checks vacuous.
```

```text
Work Items has a non-empty, complete list of types other contexts may name.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsPublishesItsContractsArchUnitTest\.cs

WorkItemsPublishesTheTypesTheOthersReach\_AndTheListOfThemIsNotEmpty

```text
A list that drops or adds one type while the other checks still pass on the remaining ones.
```

###### Obligations




##### Covers

P3

```text
Look up each published Work Items type by name in the compiled production code.
```

```text
Each is found exactly once, in the Work Items Contracts namespace. RED today: all five sit in their old Application namespaces.
```

```text
Other contexts can name Work Items through its Contracts only.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsPublishesItsContractsArchUnitTest\.cs

EachPublishedTypeIsFoundOnceAndInTheContractsOfItsOwnContext

```text
A type left in place, copied into both places, or moved into another context's Contracts.
```

###### Obligations




##### Covers

P3

```text
Look in the old namespace of each published Work Items type.
```

```text
None remains there. RED today: all five remain.
```

```text
No way around the Contracts to reach these types.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsPublishesItsContractsArchUnitTest\.cs

NoPublishedTypeIsLeftInTheNamespaceItHadBefore

```text
A duplicate left behind that still compiles.
```

###### Obligations




##### Covers

P3

```text
Scan the IL of production and test assemblies for types outside Work Items that reference a published type by its old name; the scan sees compiled references only, so a full type name written in a string is not seen.
```

```text
No type outside Work Items names one by the old name. RED today: controllers, services and tests still do.
```

```text
Every consumer follows the move; behaviour unchanged.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WorkItemsPublishesItsContractsArchUnitTest\.cs

NoTypeOutsideWorkItemsNamesAPublishedTypeByItsOldName

```text
A type name kept only inside a string literal, which the IL scan cannot see.
```

###### Obligations




### S4

##### Covers

P4

```text
Read the measured map of what each context publishes, for Tracker integration, without its events
```

```text
It lists exactly the eight Tracker integration types the other contexts reach: the connector port, its factory, the feature, incremental, delivery source, delivery forecast publisher, description block merger and write-back target interfaces
```

```text
Every check of Tracker integration's contracts looks at a complete list and cannot pass vacuously
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationPublishesItsContractsArchUnitTest\.cs

TrackerIntegrationPublishesTheTypesTheOthersReach\_AndTheListOfThemIsNotEmpty

```text
the map holds the right eight names but with a wrong old layer, which the namespace checks below then catch
```

###### Obligations


##### Covers

P4

```text
Look up each published Tracker integration type by name in the compiled production code
```

```text
Each is found exactly once, in the Contracts namespace of Tracker integration; before the move each is found only in its Application namespace
```

```text
Other contexts can name Tracker integration's contract types only through its Contracts
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationPublishesItsContractsArchUnitTest\.cs

EachPublishedTypeIsFoundOnceAndInTheContractsOfItsOwnContext

```text
a published type gets a second, nested type of the same name elsewhere; nested types are not top-level and are not compared
```

###### Obligations



##### Covers

P4

```text
Look for each published Tracker integration type in the namespace it had before
```

```text
None is left in its old namespace
```

```text
No copy of a contract type stays reachable outside Contracts
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationPublishesItsContractsArchUnitTest\.cs

NoPublishedTypeIsLeftInTheNamespaceItHadBefore

```text
the old copy is renamed instead of removed; the found-once check still holds the new home
```

###### Obligations


##### Covers

P4

```text
Scan the compiled production and test code outside Tracker integration for references to a published type by its old full name
```

```text
No type names one by its old name
```

```text
Every user of the connector port and its capabilities follows the move; behaviour is unchanged
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationPublishesItsContractsArchUnitTest\.cs

NoTypeOutsideTrackerIntegrationNamesAPublishedTypeByItsOldName

```text
an old name survives only as text that is never compiled into a reference, such as a comment or an inlined constant; the scan reads compiled references only
```

###### Obligations



##### Covers

P4

```text
Look up the CSV option names class by name in the compiled production code
```

```text
It is found exactly once, in the Contracts namespace of Tracker integration, and not in the CSV connector's folder
```

```text
Flow metrics names the CSV option names through Tracker integration's Contracts, although the compiled code shows no reference for constants
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationPublishesItsContractsArchUnitTest\.cs

TheCsvOptionNamesLiveOnlyInTheContractsOfTrackerIntegration

```text
Flow metrics keeps an old using line that the compiler resolves elsewhere; the build catches a missing type
```

###### Obligations


##### Covers

P4

```text
Ask the CI test selection whether a change to the connector port in its new Contracts home is shared
```

```text
It answers shared, so every connector suite runs
```

```text
CI still selects every connector suite for a change to Tracker integration's contracts
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TrackerIntegrationPublishesItsContractsArchUnitTest\.cs

AChangeToTheConnectorPortInItsContractsHome\_StillMakesCiRunEveryConnectorSuite

```text
the pattern selects the whole Tracker integration module, which also selects connector-only changes as shared
```

###### Obligations


### S5

##### Covers

P5

```text
Scan the compiled code for the types Organisation set-up publishes: where each is found, and which types outside Organisation set-up name one through its old place
```

```text
Each of the three types is found once, in Organisation set-up's Contracts, none is left in its old place, and no outside type names one through the old place
```

```text
Other contexts can name Organisation set-up only through its published Contracts
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupPublishesItsContractsArchUnitTest\.cs

EachPublishedTypeIsFoundOnceAndInTheContractsOfItsOwnContext

```text
a type is copied into Contracts and kept in its old place, or moved into another context's Contracts
```

###### Obligations


##### Covers

P5

```text
Check the old place of Organisation set-up's published types and every outside use of it in compiled production and test code
```

```text
Nothing remains in the old place and nothing outside Organisation set-up names it
```

```text
No consumer keeps reaching Organisation set-up behind its Contracts
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupPublishesItsContractsArchUnitTest\.cs

NoPublishedTypeIsLeftInTheNamespaceItHadBefore

```text
the type is moved but a consumer keeps its old using line only in a string or comment
```

###### Obligations


##### Covers

P5

```text
Check the old place of Organisation set-up's published types and every outside use of it in compiled production and test code
```

```text
Nothing remains in the old place and nothing outside Organisation set-up names it
```

```text
No consumer keeps reaching Organisation set-up behind its Contracts
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupPublishesItsContractsArchUnitTest\.cs

NoTypeOutsideOrganisationSetupNamesAPublishedTypeByItsOldName

```text
the type is moved but a consumer keeps its old using line only in a string or comment
```

###### Obligations


##### Covers

P5

```text
Read the namespaces Organisation set-up's open host leaves open and the downstream outbound rules' patterns
```

```text
Only Organisation set-up's Contracts is open; its Application and Domain are read as another context's code
```

```text
Organisation set-up is an open host through its Contracts alone
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupPublishesItsContractsArchUnitTest\.cs

TheOpenHostListAdmitsOnlyTheContractsOfOrganisationSetup

```text
the open list is emptied or only the rule patterns are narrowed
```

###### Obligations


##### Covers

P5

```text
Apply each downstream rule that reads Organisation set-up to its Application, Domain and Contracts namespaces
```

```text
Application and Domain are read as foreign and Contracts is not
```

```text
Downstream contexts cannot reach Organisation set-up's Application or Domain by right
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupPublishesItsContractsArchUnitTest\.cs

NoDownstreamRuleReadingOrganisationSetupLetsItsApplicationOrDomainThrough\_AndEveryOneLetsItsContractsThrough

```text
a rule admits Application and Domain as well as Contracts
```

###### Obligations


##### Covers

P5

```text
Read the list of tolerated cross-context edges
```

```text
It holds no edge into Organisation set-up's published types, only the owned residue
```

```text
The tolerated list shrinks to edges into code that is not published
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupPublishesItsContractsArchUnitTest\.cs

TheListOfToleratedCrossContextEdgesHoldsNoEdgeIntoAPublishedType

```text
entries are deleted for only some of the published types
```

###### Obligations


##### Covers

P5

```text
Read the map of what Organisation set-up publishes
```

```text
Exactly the three non-event types the other contexts reach, so no later check runs over an empty list
```

```text
The published list is complete and non-empty
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationSetupPublishesItsContractsArchUnitTest\.cs

OrganisationSetupPublishesTheTypesTheOthersReach\_AndTheListOfThemIsNotEmpty

```text
the list is padded with a type nobody reaches
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
Licensing and Access contract types (license service, RBAC administration service and the types they use) are available from their Contracts namespaces, and the product behaves as before for licence checks and access decisions.
```

```text
The oracle sees structure and build success; it does not see that licence checks and RBAC decisions still give the same answers for the same users.
```

yes

None.

None.

#### E2

##### Promises

P2

```text
Flow metrics and Forecasting contract types (metrics queries, forecast throughput status, ports Forecasting fills, what Refinement and Promises read of Forecasting) are available from Contracts, and metrics and forecasts show the same numbers as before.
```

```text
The oracle does not see whether forecast and metric values a user reads are unchanged, nor that Refinement and Promises still read the same forecast data.
```

yes

None.

None.

#### E3

##### Promises

P3

```text
Work Items contract types (blocked items, rule evaluation ports, refresh log, what the forecast waits for) are available from Contracts, and blocked-item rules, refreshes and forecast waiting behave as before.
```

```text
The oracle does not see whether a user still gets the same blocked-item results, refresh log entries and forecast timing after refresh.
```

yes

None.

None.

#### E4

##### Promises

P4

```text
Tracker integration contract types (connector port and capabilities, factory, description block merger, CSV option names, write-back target) are available from Contracts; a change touching a Contracts path is treated as shared by CI; connecting, syncing and writing back to trackers behave as before.
```

```text
The oracle does not see real connector sync or write-back results, nor that the CI classifier marks a Contracts-only change as shared.
```

yes

None.

None.

#### E5

##### Promises

P5

```text
Organisation set-up contract types are available from Contracts, its open host exposes only Contracts rather than Application or Domain, the remaining contract-breach list (R-C1) names only owned non-contract residue, and set-up behaves as before.
```

```text
The oracle does not see that set-up still works for a user, nor that the residue list is complete and free of contract types.
```

yes

None.

None.

#### E6

##### Promises

P1

P2

P3

P4

P5

```text
With the entries for these five areas deleted from the contract-breach list (R-C1), the rule still passes, and the product behaves as before for users.
```

```text
Deleting entries proves the list is smaller, not that no contract breach hides elsewhere or that user-visible behaviour is unchanged.
```

yes

None.

None.

### Waivers

None.

## Charter

None.

## Examination

### E1

met

#### Observations

```text
Probe architecture-rules PublishesItsContracts: 17/17 passed (WorkItems, TrackerIntegration, OrganisationSetup: each published type found once in own Contracts, none left in old namespace, no outside type names old name via IL; open host admits only Organisation set-up Contracts; Csv option names live only in Tracker Contracts; connector port Contracts path still makes CI run every connector suite; tolerated cross-context edge list holds no edge into published type).
```

```text
Probe architecture-rules PublishTheirContracts: 8/8 passed (Licensing+Access and FlowMetrics+Forecasting: each published type found once in own Contracts, none left in old namespace, no outside type names old name, lists non-empty). Limitation: only structural architecture rules observed; no behaviour-equivalence probe (answers-compare, backend-tests) run; no R-C1 list deletion mutation observed directly.
```

```text
Probe access-answers-compare 2db405ff5 (base before feature C vs candidate dc2e7e1ff): exit 0, VERDICT IDENTICAL, all 10 Access or Licensing answers match (api-keys, auth-mode, everyone-signed-in, licence status before/after import, my-summary, rbac-status, session, system-wide-rules x2; concurrencyToken masked).
```

```text
Probe forecast-answers-compare 2db405ff5 vs candidate dc2e7e1ff: exit 0, VERDICT IDENTICAL, all 128 forecast answers over 9 scenarios match (backtest, deliveries, feature-list, features, item-creation-prediction, manual how-many/when, reality-check; timestamps/tokens masked).
```

```text
Probe refresh-answers-compare 2db405ff5 vs candidate dc2e7e1ff: exit 0, VERDICT IDENTICAL, all 343 refresh answers over 9 scenarios match (blocked today/a week ago, blocked-count-history, finished, in-progress, work-items 180, features-with-dependencies, parent-placement 2136 incl. 1248 naming a parent).
```

```text
Probe backend-tests (full backend suite on candidate dc2e7e1ff): exit 0, Test Run Successful, 15524 total, 15512 passed, 12 skipped, 0 failed.
```

### E2

not_examined

```text
Structure (O1,O2), forecast answers identical (O4) and backend suite green (O7) observed, but metric values were not compared: metrics-answers-compare failed twice building the base (O6). Owner: evidence capture/tool owner; remedy: fix base capture (Lighthouse.dll not found) and rerun metrics-answers-compare 2db405ff5.
```

### E3

met

#### Observations

```text
Probe architecture-rules PublishesItsContracts: 17/17 passed (WorkItems, TrackerIntegration, OrganisationSetup: each published type found once in own Contracts, none left in old namespace, no outside type names old name via IL; open host admits only Organisation set-up Contracts; Csv option names live only in Tracker Contracts; connector port Contracts path still makes CI run every connector suite; tolerated cross-context edge list holds no edge into published type).
```

```text
Probe architecture-rules PublishTheirContracts: 8/8 passed (Licensing+Access and FlowMetrics+Forecasting: each published type found once in own Contracts, none left in old namespace, no outside type names old name, lists non-empty). Limitation: only structural architecture rules observed; no behaviour-equivalence probe (answers-compare, backend-tests) run; no R-C1 list deletion mutation observed directly.
```

```text
Probe access-answers-compare 2db405ff5 (base before feature C vs candidate dc2e7e1ff): exit 0, VERDICT IDENTICAL, all 10 Access or Licensing answers match (api-keys, auth-mode, everyone-signed-in, licence status before/after import, my-summary, rbac-status, session, system-wide-rules x2; concurrencyToken masked).
```

```text
Probe forecast-answers-compare 2db405ff5 vs candidate dc2e7e1ff: exit 0, VERDICT IDENTICAL, all 128 forecast answers over 9 scenarios match (backtest, deliveries, feature-list, features, item-creation-prediction, manual how-many/when, reality-check; timestamps/tokens masked).
```

```text
Probe refresh-answers-compare 2db405ff5 vs candidate dc2e7e1ff: exit 0, VERDICT IDENTICAL, all 343 refresh answers over 9 scenarios match (blocked today/a week ago, blocked-count-history, finished, in-progress, work-items 180, features-with-dependencies, parent-placement 2136 incl. 1248 naming a parent).
```

```text
Probe backend-tests (full backend suite on candidate dc2e7e1ff): exit 0, Test Run Successful, 15524 total, 15512 passed, 12 skipped, 0 failed.
```

### E4

met

#### Observations

```text
Probe architecture-rules PublishesItsContracts: 17/17 passed (WorkItems, TrackerIntegration, OrganisationSetup: each published type found once in own Contracts, none left in old namespace, no outside type names old name via IL; open host admits only Organisation set-up Contracts; Csv option names live only in Tracker Contracts; connector port Contracts path still makes CI run every connector suite; tolerated cross-context edge list holds no edge into published type).
```

```text
Probe architecture-rules PublishTheirContracts: 8/8 passed (Licensing+Access and FlowMetrics+Forecasting: each published type found once in own Contracts, none left in old namespace, no outside type names old name, lists non-empty). Limitation: only structural architecture rules observed; no behaviour-equivalence probe (answers-compare, backend-tests) run; no R-C1 list deletion mutation observed directly.
```

```text
Probe access-answers-compare 2db405ff5 (base before feature C vs candidate dc2e7e1ff): exit 0, VERDICT IDENTICAL, all 10 Access or Licensing answers match (api-keys, auth-mode, everyone-signed-in, licence status before/after import, my-summary, rbac-status, session, system-wide-rules x2; concurrencyToken masked).
```

```text
Probe forecast-answers-compare 2db405ff5 vs candidate dc2e7e1ff: exit 0, VERDICT IDENTICAL, all 128 forecast answers over 9 scenarios match (backtest, deliveries, feature-list, features, item-creation-prediction, manual how-many/when, reality-check; timestamps/tokens masked).
```

```text
Probe refresh-answers-compare 2db405ff5 vs candidate dc2e7e1ff: exit 0, VERDICT IDENTICAL, all 343 refresh answers over 9 scenarios match (blocked today/a week ago, blocked-count-history, finished, in-progress, work-items 180, features-with-dependencies, parent-placement 2136 incl. 1248 naming a parent).
```

```text
Probe backend-tests (full backend suite on candidate dc2e7e1ff): exit 0, Test Run Successful, 15524 total, 15512 passed, 12 skipped, 0 failed.
```

### E5

met

#### Observations

```text
Probe architecture-rules PublishesItsContracts: 17/17 passed (WorkItems, TrackerIntegration, OrganisationSetup: each published type found once in own Contracts, none left in old namespace, no outside type names old name via IL; open host admits only Organisation set-up Contracts; Csv option names live only in Tracker Contracts; connector port Contracts path still makes CI run every connector suite; tolerated cross-context edge list holds no edge into published type).
```

```text
Probe architecture-rules PublishTheirContracts: 8/8 passed (Licensing+Access and FlowMetrics+Forecasting: each published type found once in own Contracts, none left in old namespace, no outside type names old name, lists non-empty). Limitation: only structural architecture rules observed; no behaviour-equivalence probe (answers-compare, backend-tests) run; no R-C1 list deletion mutation observed directly.
```

```text
Probe access-answers-compare 2db405ff5 (base before feature C vs candidate dc2e7e1ff): exit 0, VERDICT IDENTICAL, all 10 Access or Licensing answers match (api-keys, auth-mode, everyone-signed-in, licence status before/after import, my-summary, rbac-status, session, system-wide-rules x2; concurrencyToken masked).
```

```text
Probe forecast-answers-compare 2db405ff5 vs candidate dc2e7e1ff: exit 0, VERDICT IDENTICAL, all 128 forecast answers over 9 scenarios match (backtest, deliveries, feature-list, features, item-creation-prediction, manual how-many/when, reality-check; timestamps/tokens masked).
```

```text
Probe refresh-answers-compare 2db405ff5 vs candidate dc2e7e1ff: exit 0, VERDICT IDENTICAL, all 343 refresh answers over 9 scenarios match (blocked today/a week ago, blocked-count-history, finished, in-progress, work-items 180, features-with-dependencies, parent-placement 2136 incl. 1248 naming a parent).
```

```text
Probe backend-tests (full backend suite on candidate dc2e7e1ff): exit 0, Test Run Successful, 15524 total, 15512 passed, 12 skipped, 0 failed.
```

### E6

not_examined

```text
Structure (O1,O2), forecast answers identical (O4) and backend suite green (O7) observed, but metric values were not compared: metrics-answers-compare failed twice building the base (O6). Owner: evidence capture/tool owner; remedy: fix base capture (Lighthouse.dll not found) and rerun metrics-answers-compare 2db405ff5.
```
