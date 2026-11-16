# Product brief: story\-6376\-s06\-b\-capabilities

## Request

```text
Story #6376 S06 Connector capability interfaces, nwdes feature B of two (capabilities). Split IWorkTrackingConnector so that no connector claims a member it cannot serve: the base keeps the members all five connectors implement; IWriteBackTarget (Jira, Azure DevOps), IFeatureSource (Jira, Azure DevOps, Linear, CSV) and one IIncrementalSource (Jira, Azure DevOps) holding the Team, Portfolio and parent-Feature sweep and by-id members with the per-connection probe. Callers test the type; a missing capability ends in the same outcome as today. Each slice removes its throws and their entries from the ratchet list feature A added. Behaviour-free: same sync path per connector and connection, same validation and write-back answers, same responses. Design: docs/feature/story-6376-s06-connector-capability-interfaces/feature-delta.md (Decisions, D-1 changed to b, Slices B1-B3, Ratchet design).
```

## Work

M

```text
A connector implements only what it can do, so the CSV, Linear and ServiceNow connectors throw no 'not supported' from a member they claim, and S03a packages the final connector port
```

### Rework

None.

## Promises

### P1

Outcome

```text
Only Jira and Azure DevOps are write-back targets; a write-back to a CSV, Linear or ServiceNow connection fails each update as it does today; their write-back throws are gone from the ratchet list
```

### P2

Outcome

```text
Jira, Azure DevOps, Linear and CSV read Portfolio Features; ServiceNow does not claim to; a ServiceNow Portfolio refresh fails as it does today; ServiceNow's Feature-reading throws are gone from the ratchet list
```

### P3

Outcome

```text
Only Jira and Azure DevOps offer incremental sync, with the same per-connection answer as today; CSV, Linear and ServiceNow Teams and Portfolios take the full refresh path as today; the base connector port has no member any connector throws 'not supported' from, and the ratchet list holds only the two genuine entries
```

## Scope

```text
The three capability interfaces, the connectors implementing them, the callers testing the type, the ratchet list shrinking, ADR-285 written at finalize
```

```text
Implementing missing capabilities (sweeps for Linear or ServiceNow, write-back for CSV); the other Supports* flags (additional fields, write-back mappings, delivery publishing); moving connectors into Modules (S03a); a unit-generic fetch (Epic 6474)
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
Only Jira and Azure DevOps are write-back targets, and a write-back to another connection fails as before
```

#### Promises

P1

#### Depends on

None.

#### Slice work

None.

### S2

```text
ServiceNow no longer claims to read Portfolio Features; the other four do; a ServiceNow Portfolio refresh fails as before
```

#### Promises

P2

#### Depends on

None.

#### Slice work

None.

### S3

```text
Only Jira and Azure DevOps offer incremental sync for Teams, Portfolios and parent Features, with the same per-connection answer; the others take the full path as before
```

#### Promises

P3

#### Depends on

None.

#### Slice work

None.
