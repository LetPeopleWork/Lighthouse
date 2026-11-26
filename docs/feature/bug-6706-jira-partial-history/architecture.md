# Architecture brief: bug\-6706\-jira\-partial\-history

## Paradigm

object_oriented

## Shared records

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraWorkTrackingConnector\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraRestRoutes\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraPartialHistoryDatingTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~JiraPartialHistoryDatingTest
```

#### Supports

None.

None.

None.

#### Public oracle

```text
The connector reads a team's items; Jira's search answer for an item in a Doing state holds only its latest history entries (a move within Doing), with changelog.total larger than the entries returned, and the item's full history (which holds the earlier move into Doing) is available from the deployment's own whole-history route: rest/api/2/issue/<KEY>?expand=changelog on Data Center, the paged issue changelog route on Cloud
```

```text
The item's start date is the date it first entered Doing in its whole history (16 May 2025 in the reported case), not empty; a finished item's start date likewise comes from its whole history rather than its closed date; when the whole-history read is itself still partial, the item stays marked as not holding its whole history
```

```text
The item has no start date (or a Done item's start equals its closed date) although its whole history holds the move into Doing; or a Data Center item is dated only from the truncated search answer; or a Cloud item with a truncated answer of 30 or fewer total entries is never re-read
```

##### Promises

P1

#### Prefactoring

None.

#### Records

None.

## ADRs

None.
