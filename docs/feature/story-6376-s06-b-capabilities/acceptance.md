# Acceptance brief: story\-6376\-s06\-b\-capabilities

## Slices

### S1

##### Covers

P1

```text
Read the built backend for the write-back capability by its full name and for the connector base
```

```text
The write-back capability exists as an interface declaring the write-back of a connection's field updates with one result per update, and the connector base no longer declares it
```

```text
No connector claims to write back merely by being a connector
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackTargetCapabilityArchUnitTest\.cs\:\:WritingBackIsItsOwnCapability\_AndNoLongerAskedOfEveryConnector

```text
The member kept on both the capability and the base would fail; it still passes if the capability's member had a different parameter name
```

###### Obligations


##### Covers

P1

```text
Read every concrete connector class in the built backend and which of them take on the write-back capability
```

```text
All five connectors are read, and exactly Jira and Azure DevOps take on the capability; CSV, Linear and ServiceNow do not
```

```text
Only Jira and Azure DevOps connections are write-back targets
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackTargetCapabilityArchUnitTest\.cs\:\:OnlyJiraAndAzureDevOpsAreWriteBackTargets

```text
A connector added later that writes back by another means without the capability; that is outside this capability's promise
```

###### Obligations





##### Covers

P1

```text
Write back two changed field values for a Feature and a Work Item on a CSV connection through the write-back service with the real CSV connector
```

```text
Two item results come back, each failed for its own work item and field; neither stored value changes and nothing is saved
```

```text
A write-back to a CSV connection fails each update as it does today and stores nothing
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackTargetCapabilityArchUnitTest\.cs\:\:AWriteBackToACsvConnection\_FailsEachUpdateAndStoresNothing

```text
Linear or ServiceNow write-backs succeeding or being skipped; covered only through their absence from the capability
```

###### Obligations



### S2

##### Covers

P2

```text
Read the built backend for the Feature-reading capability by its full name and for the connector base
```

```text
The Feature-reading capability exists as an interface declaring the read of every Portfolio Feature and the read of named parent Features, and the connector base declares neither any more
```

```text
No connector claims to read Portfolio Features merely by being a connector
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FeatureSourceCapabilityArchUnitTest\.cs\:\:ReadingPortfolioFeaturesIsItsOwnCapability\_AndNoLongerAskedOfEveryConnector

```text
The capability members had different parameter names; the read-by-named-Features overload stays on the base, which a later slice moves
```

###### Obligations



##### Covers

P2

```text
Read every concrete connector class in the built backend and which of them take on the Feature-reading capability
```

```text
All five connectors are read, and exactly Jira, Azure DevOps, Linear and CSV take on the capability; ServiceNow does not
```

```text
Jira, Azure DevOps, Linear and CSV connections read Portfolio Features; ServiceNow does not claim to
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FeatureSourceCapabilityArchUnitTest\.cs\:\:OnlyJiraAzureDevOpsLinearAndCsvReadPortfolioFeatures

```text
A connector added later that reads Features by another means without the capability; that is outside this capability's promise
```

###### Obligations




##### Covers

P2

```text
Refresh a Portfolio holding one stored Feature on a ServiceNow connection through the Work Item service with the real ServiceNow connector
```

```text
The refresh ends in a not-supported failure; the Portfolio still holds exactly its stored Feature, its last refresh time is unchanged and nothing is saved
```

```text
A ServiceNow Portfolio refresh fails as it does today and the Portfolio keeps what it showed before
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FeatureSourceCapabilityArchUnitTest\.cs\:\:APortfolioRefreshOnAServiceNowConnection\_FailsAndLeavesItsFeaturesAsTheyWere

```text
A different failure message; the message is log-only and outside the preserved observation
```

###### Obligations



##### Covers

P2

```text
Validate a Portfolio's settings on a ServiceNow connection
```

```text
The validation refuses with portfolio_not_supported, as today
```

```text
Pointing a Portfolio at ServiceNow is still refused with an actionable reason
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/ServiceNow\/ServiceNowWorkTrackingConnectorTest\.cs\:\:PointingAPortfolioAtServiceNow\_IsRefusedWithAnActionableReason

```text
A change to the explanation text; only the refusal code is the contract
```

###### Obligations


##### Covers

P2

```text
Read every connector method in the built backend that creates a not-supported failure and compare with the listed exceptions
```

```text
ServiceNow's two Feature-read refusals are gone from the connectors and from the list, which holds 17 entries
```

```text
ServiceNow's Feature-reading refusals no longer exist
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorsDeclareNoUnsupportedMembersArchUnitTest\.cs\:\:EveryConnectorMethodThatCreatesANotSupportedException\_IsOnTheListWithItsReason

```text
Throws and list entries removed together for a different pair of members; the Feature-read members' absence is pinned by the capability rows above
```

###### Obligations


### S3

##### Covers

P3

```text
Read the built backend and look up the sweeping capability and the general connector port
```

```text
The sweeping capability exists and holds the per-connection probe, the Team, Portfolio and parent-Feature sweeps and the two fetches by id; the general connector port holds exactly the six members all five connectors serve
```

```text
No connector claims a sweep it can only refuse; refreshes are unchanged
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/IncrementalSourceCapabilityTest\.cs\:\:SweepingIsItsOwnCapability\_AndEveryConnectorIsAskedOnlyForWhatAllFiveServe

```text
A member is moved with a changed signature that no caller can tell apart, such as a parameter swapped for an equal-meaning type
```

###### Obligations


##### Covers

P3

```text
Enumerate every connector in the built backend and ask which ones take on the sweeping capability
```

```text
All five connectors are found; exactly Jira and Azure DevOps take it on; CSV, Linear and ServiceNow do not
```

```text
Only Jira and Azure DevOps offer incremental sync; CSV, Linear and ServiceNow Teams and Portfolios take the full refresh as before
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/IncrementalSourceCapabilityTest\.cs\:\:OnlyJiraAndAzureDevOpsCanSweep\_CsvLinearAndServiceNowDownloadEverythingAsBefore

```text
The refresh service ignores the type and keeps sweeping through some other path; held by the existing refresh-service tests that pin probe to path
```

###### Obligations



##### Covers

P3

```text
Ask a real Jira connector, through the sweeping capability, about a site it has already downloaded from and about a site nobody has reached
```

```text
Yes for the reached site, no for the unreached one
```

```text
A Jira Team or Portfolio is swept once Lighthouse knows the site, and fully downloaded before that, as today
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/IncrementalSourceCapabilityTest\.cs\:\:AJiraSiteIsSweptOnlyOnceLighthouseHasReachedIt

```text
The probe answers from some other per-site fact that happens to coincide with a first download
```

###### Obligations



##### Covers

P3

```text
Ask a real Azure DevOps connector, through the sweeping capability, about a connection
```

```text
Yes
```

```text
Azure DevOps Teams and Portfolios are swept on every refresh, as today
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/IncrementalSourceCapabilityTest\.cs\:\:AnAzureDevOpsConnectionIsAlwaysSwept

```text
Azure DevOps answers yes but the refresh service does not consult it; held by the existing refresh-service tests
```

###### Obligations


##### Covers

P3

```text
Read every connector's compiled members for a not-supported refusal and compare with the listed genuine cases
```

```text
Only Jira SweepIdentities and the Azure DevOps credential conversion still refuse; the list holds exactly those two
```

```text
No connector claims a member it refuses
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ConnectorsDeclareNoUnsupportedMembersArchUnitTest\.cs

```text
A refusal moved into a helper the reading does not follow
```

###### Obligations




##### Covers

P3

```text
Ask which connectors take on write-back
```

```text
Exactly Jira and Azure DevOps
```

```text
Write-back answers as before
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackTargetCapabilityArchUnitTest\.cs

```text
The write-back service ignores the type
```

###### Obligations


##### Covers

P3

```text
Ask which connectors read Portfolio Features
```

```text
Jira, Azure DevOps, Linear and CSV; ServiceNow refresh fails as before
```

```text
Portfolio refreshes answer as before
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/FeatureSourceCapabilityArchUnitTest\.cs

```text
A parent read routed elsewhere
```

###### Obligations


##### Covers

P3

```text
Ask which connectors take on sweeping and what the probe answers per connection
```

```text
Azure DevOps sweeps; Jira only once reached; CSV, Linear, ServiceNow are not of the type
```

```text
Each refresh takes the same sweep or full path as today
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/IncrementalSourceCapabilityTest\.cs\:\:OnlyJiraAndAzureDevOpsCanSweep\_CsvLinearAndServiceNowDownloadEverythingAsBefore

```text
The refresh service ignores the probe; held by WorkItemServiceTest
```

###### Obligations


##### Covers

P3

```text
Refresh a Team, a Portfolio, and a Portfolio whose Feature has a parent, through the Work Items refresh service, on a connector that reads Features but cannot sweep
```

```text
Each refresh asks once for the full download (work items, Features, parent Features) and never for a sweep or a fetch by id
```

```text
CSV, Linear and ServiceNow Teams and Portfolios take the full refresh path as before
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/WorkTrackingConnectors\/IncrementalSourceCapabilityTest\.cs\:\:ATeamRefreshOnAConnectorThatCannotSweep\_DownloadsTheWholeQuery

```text
The service consults a probe on a connector that cannot sweep and only falls back after; guarded by no sweep or by-id call, not by the probe itself
```

###### Obligations



## Expectations

### Rows

#### E1

##### Promises

P1

```text
Updating a work item through a CSV, Linear or ServiceNow connection still fails each update as before, while Jira and Azure DevOps updates still write back; the connector-throws-count output no longer lists any write-back throw in the Csv, Linear or ServiceNow folders.
```

#### E2

##### Promises

P2

```text
A Portfolio refresh reads Features for Jira, Azure DevOps, Linear and CSV; a ServiceNow Portfolio refresh still fails as before and ServiceNow is not reported as reading Portfolio Features; connector-throws-count lists no Feature-reading throw in the ServiceNow folder.
```

#### E4

##### Promises

P3

```text
Only Jira and Azure DevOps report incremental sync, with the same per-connection answer as before; CSV, Linear and ServiceNow Teams and Portfolios take the full refresh as before; connector-throws-count shows 0 for the Csv, Linear and ServiceNow folders, and the ratchet test (backend-tests ConnectorsDeclareNoUnsupportedMembers) passes with only its two genuine entries.
```

## Charter

None.

## Examination

### E1

met

#### Observations

```text
Probe connector-throws-count: Csv 0, Linear 0, ServiceNow 0, total 0. architecture-rules ConnectorsDeclareNoUnsupportedMembers: 10/10 passed. backend-tests Capability: 21/21 pass; WriteBackService: 22/22 pass (incl. connector-failure wrapped in failed result). Not probed: live Jira/ADO write, ServiceNow Portfolio refresh, incremental-sync per-connection answers directly; relies on suite tests only.
```

### E2

met

#### Observations

```text
Probe connector-throws-count: Csv 0, Linear 0, ServiceNow 0, total 0. architecture-rules ConnectorsDeclareNoUnsupportedMembers: 10/10 passed. backend-tests Capability: 21/21 pass; WriteBackService: 22/22 pass (incl. connector-failure wrapped in failed result). Not probed: live Jira/ADO write, ServiceNow Portfolio refresh, incremental-sync per-connection answers directly; relies on suite tests only.
```

### E4

met

#### Observations

```text
Probe connector-throws-count: Csv 0, Linear 0, ServiceNow 0, total 0. architecture-rules ConnectorsDeclareNoUnsupportedMembers: 10/10 passed. backend-tests Capability: 21/21 pass; WriteBackService: 22/22 pass (incl. connector-failure wrapped in failed result). Not probed: live Jira/ADO write, ServiceNow Portfolio refresh, incremental-sync per-connection answers directly; relies on suite tests only.
```
