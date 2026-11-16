# Acceptance brief: story\-6376\-s06\-b\-capabilities

## Slices

None.

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

None.
