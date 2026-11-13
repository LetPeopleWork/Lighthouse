# Acceptance brief: story\-6602\-b\-report\-window\-and\-overlap

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A maintainer can run the report-window boundary scenarios as application-service-port tests on a new Reports port base (ReportsAtThePortTest) without starting a web host, and the host-user backlog list (HostUsersStillToMove) no longer names ReportWindowBoundaryScenarios.
```

#### E2

##### Promises

P2

```text
After the move, the same 15 report-window boundary test ids still run at the port, no new wire-level row appears, every earlier assertion is still made, and the port fixture check fails if the Reports port registrations differ from the host's.
```

#### E3

##### Promises

P3

```text
The id comparison for Slice05NeedAndVerdictTest lists exactly the rows dropped; each dropped row has a NeedOverOneCycleTest row proving the same rule with the same inputs, and every other id still exists.
```

#### E4

##### Promises

P4

```text
Each Refinement port test helper that feature A's reviews found duplicated (Gravity ready-rule builder, tab and need readers, shared constants and script, TheTabAsReadBy) is defined once, in the port base or port builder, and not repeated in other Refinement test classes.
```

## Charter

None.

## Examination

None.
