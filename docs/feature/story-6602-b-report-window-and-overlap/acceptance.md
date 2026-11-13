# Acceptance brief: story\-6602\-b\-report\-window\-and\-overlap

## Slices

### S1

##### Covers

P1

P2

```text
For each of the 15 rows of the shared window table, Team Lightspeed keeps finished Work Items for 365 days (or for ever), the types of report are read and a report is made with the row's window, with no web host
```

```text
Today reads 2 Oct 2026, the earliest start reads 2 Oct 2025 (none for a Team keeping every finished Work Item) and the shortest window reads 14 days, on every row
```

```text
A Team owner sees the same window bounds the browser wizard shows, on every edge of the table
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportWindowBoundaryScenarios\.cs\:\:A\_window\_at\_an\_edge\_is\_answered\_as\_the\_table\_says

```text
a server that reports today one day off but the same refusals, or reads an earliest start of 365 days for the keep-everything Team: both are caught by the three bound assertions; still accepted is a window rule changed only in a row the table does not hold
```

###### Obligations

None.

##### Covers

P1

P2

```text
A window one day short of the shortest, ending tomorrow, starting before the oldest kept day, or a Now shorter than 14 days is submitted as a new report
```

```text
The outcome is Refused with exactly the refusal code the table names for that row (for example then-too-short), and no report is handed back
```

```text
The owner is told the reason from the closed list instead of getting a report
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportWindowBoundaryScenarios\.cs\:\:A\_window\_at\_an\_edge\_is\_answered\_as\_the\_table\_says

```text
a refusal with the right status but a neighbouring code is caught by the code equality; still accepted is a refusal for a different, equally invalid window that the table does not list
```

###### Obligations

None.

##### Covers

P1

P2

```text
A window exactly on an accepted edge (14 days, ending today, starting on the oldest kept day, any start for a Team keeping everything) is submitted
```

```text
The outcome is Done and a report is handed back
```

```text
The owner gets the report for a window the wizard allows
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportWindowBoundaryScenarios\.cs\:\:A\_window\_at\_an\_edge\_is\_answered\_as\_the\_table\_says

```text
an accepted window whose report is empty of content: the port hands back a report, and what it shows is pinned by other Reports scenarios
```

###### Obligations

None.

##### Covers

P2

```text
The port-fixture lifetime check compares the services the Reports port base registers with the host's
```

```text
No Reports registration has another lifetime than on the host, so the Reports port case passes
```

```text
The port scenarios stand for the product's own wiring, not a lookalike
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Forecasting\/RealityCheckWireContractTest\.cs\:\:Every\_service\_a\_port\_fixture\_registers\_lives\_as\_long\_as\_it\_does\_on\_the\_host

```text
a Reports service missing from the port base: the check compares only what is registered, so an absent service shows as a failing scenario, not here
```

###### Obligations

None.

##### Covers

P1

```text
The host-use guard runs after the class left the list of host users still to move
```

```text
The class is not named on that list and starts no web host, and the guard passes
```

```text
The suite no longer starts a host for the report window rows
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.cs

```text
a class that drives the port but keeps a host fixture elsewhere in its hierarchy is caught by the guard; still accepted is a stale ratchet line for a class that no longer exists, which the guard already refuses
```

###### Obligations


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
