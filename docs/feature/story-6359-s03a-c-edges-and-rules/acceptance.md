# Acceptance brief: story\-6359\-s03a\-c\-edges\-and\-rules

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Running the architecture rules for the tracker integration module shows every module rule passing: Domain reaches only the kernel and base library; outside code reaches the module only through legal namespaces and the measured lists; Application reaches no EF Core, Data, API or other module's Adapters beyond listed entries; the module reaches no other context except the one listed Promises renderer edge; no module type reaches a repository except the listed Application services. Each list is checked both ways (an unlisted real edge and a stale listed entry both fail), and the rules include a made-up offender in each position that is reported as caught.
```

#### E2

##### Promises

P2

```text
Asking test-selection for a changed connector path under the tracker integration module shows that tracker's live suite (and the backend suite) selected, while the recorded selection scenarios all pass; the old WorkTrackingIntegrationPattern leaf pattern and old Services/Factories alternatives no longer select anything, so a change to a path matching only those old forms selects no tracker live suite.
```

#### E3

##### Promises

P3

```text
Running the architecture rules shows the folder-placement rules passing: every unit test of the module sits under Lighthouse.Backend.Tests/Modules/TrackerIntegration, and no context file remains under Services/Interfaces, Services/Implementation or Factories apart from the named exceptions; the rules include a made-up test file and a made-up source file in an old folder reported as caught.
```

#### E4

##### Promises

P4

```text
Users, trackers and the database see no change: the EF model comparison reports no difference from the last migration on SQLite and Postgres, the full architecture rules run has no failing test, and the newest finished CI run shows each tracker's live suite (jira, ado, linear, servicenow, github) with passed counts and no failures. Absence of unforced production code edits is not observable by these probes and is left to candidate review.
```

## Charter

None.

## Examination

None.
