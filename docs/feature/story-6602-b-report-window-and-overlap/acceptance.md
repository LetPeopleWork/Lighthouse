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


##### Covers

P2

```text
The port-fixture lifetime check compares a Reports port that registers its services as the host does with host registrations shaped as the host makes them
```

```text
The check gives no difference, so the Reports port base can pass it
```

```text
A Reports port that mirrors the host is not reported as drifted
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PortFixtureLifetimesTest\.cs\:\:A\_port\_registering\_the\_services\_as\_the\_host\_does\_differs\_in\_nothing

```text
A check that reports every forwarder or repeated service type would fail this row; a check that skips every forwarder, as it does today, still passes it
```

###### Obligations

None.

##### Covers

P2

```text
The port-fixture lifetime check compares host registrations with a port that plants one lookalike per row: the portfolio repository service built from the repository type, the report reader built from the report repository type, the portfolio repository service as a factory with its forward target missing, and a report metric type the host does not register
```

```text
Each planted row gives exactly one difference, and that difference names the planted service
```

```text
A Reports port that only looks like the host is reported, naming the service to fix, so a scenario cannot pass on state the product never shares
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PortFixtureLifetimesTest\.cs\:\:A\_registration\_that\_only\_looks\_like\_the\_hosts\_is\_reported\_by\_service

```text
A check that skips every declared forwarder unconditionally still reports the unregistered metric type but misses the three forwarder rows; a check that reports a lookalike without naming the service would fail the name assertion; a check that reports two differences for one plant would fail the count
```

###### Obligations

None.

### S2

##### Covers

P3

```text
After the drop, run the need-and-verdict, need-over-one-cycle and SLE-yardstick classes at the application-service port and compare their sorted test ids with the ids recorded before the slice, less the three repeated rows
```

```text
The run holds exactly the 49 ids before less Below_range_says_how_many_are_ready_against_the_range_the_Team_is_likely_to_pull, Too_little_Throughput_history_gives_the_familiar_guard_instead_of_a_number and A_Team_nobody_set_up_has_no_number_because_it_has_no_refinement_states: 46 ids, need-and-verdict 17 to 14, need-over-one-cycle 22 and SLE-yardstick 10 unchanged, every id passed; no other id is missing and none is added
```

```text
A maintainer sees each need rule proven once, and no rule silently lost its test when the repeats went
```

##### Disposition

Tested

docs\/feature\/story\-6602\-b\-report\-window\-and\-overlap\/evidence\/compare\-ids\.py with ids\-expected\-after\-drop\.txt over the TRX of the first declared verification command

```text
a drop that also renames a kept row to a new name in the same class would be caught as one missing and one extra id; still accepted is a kept row whose body was weakened under an unchanged name, which the twin breaks and the review must catch
```

###### Obligations




##### Covers

P3

```text
Gravity has GR-058 and GR-059 tagged ready under its Ready rule, finished one Work Item a day, refines on Thursdays; today is Friday 2 October 2026; over its seven-day cycle the Team is likely to pull 3, 5, 6, 8 and 10 at 30, 50, 70, 85 and 95 percent; the coach opens the Refinement tab
```

```text
The need reads Below, no unavailable reason, 5 to 8 at the 50th and 85th percentile over 7 working days, cycle from Thursday 8 to Thursday 15 October, ready count 2, and the heading names Thursday 8 October, not a Refinement day: every assertion of the dropped below-range row plus the cycle. Deliberate break: the default high percentile of the Refinement settings from 85 to 70 turns this row red (high end 6 at 70)
```

```text
The coach sees how many are ready against the range the Team is likely to pull, proven once by the row that also names the cycle
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/NeedOverOneCycleScenarios\.cs\:\:The\_need\_covers\_the\_working\_days\_from\_the\_next\_Refinement\_to\_the\_one\_after

```text
a calculator that reads the high end at a fixed 85 percent whatever the setting would still pass when the setting is 85; the break of the default to 70 is the falsifier and must turn this row red
```

###### Obligations



##### Covers

P3

```text
Today is Wednesday 7 October 2026; Gravity has GR-058 tagged ready under its Ready rule, finished 3 Work Items a day on only 4 days, and refines on Thursdays; the coach opens the Refinement tab
```

```text
The need gives the reason InsufficientData, no verdict, no low and no high end, and names no cycle: every assertion of the dropped too-little-history row plus the empty cycle. Deliberate break: the minimum number of days with finished work for a forecast from 5 to 4 turns this row red
```

```text
The coach gets the familiar too-little-history guard instead of a number, proven once
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/NeedOverOneCycleScenarios\.cs\:\:Without\_a\_number\_the\_need\_names\_no\_cycle\(ThroughputHistory\,\"InsufficientData\"\)

```text
a guard that demands more than five days would still pass with four days of history; still accepted is any limit above four, which the four-day input cannot tell apart and the forecast policy unit rows pin
```

###### Obligations



##### Covers

P3

```text
Today is Wednesday 7 October 2026; Team Zenith has only Backlog, Implementation and Done states and 30 days of history, and nobody set up its refinement states; the coach opens the Refinement tab
```

```text
The need gives the reason NoRefinementStates, no verdict, no low and no high end, and names no cycle: every assertion of the dropped nobody-set-up row plus the empty cycle. Deliberate break: the need calculator no longer answering NoRefinementStates turns this row red
```

```text
The coach of a Team nobody set up is told it has no refinement states instead of getting a number, proven once
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/NeedOverOneCycleScenarios\.cs\:\:Without\_a\_number\_the\_need\_names\_no\_cycle\(RefinementStates\,\"NoRefinementStates\"\)

```text
a calculator that checks the cadence before the refinement states would answer NoCadence for Zenith and is caught by the reason equality; still accepted is a calculator that also answers NoRefinementStates for a Team that has refinement states but nothing in them, which this input cannot tell apart
```

###### Obligations



##### Covers

P3

P4

```text
After the hoist and before the drop, run the need-and-verdict, need-over-one-cycle and SLE-yardstick classes at the application-service port and compare their sorted test ids with the ids recorded before the slice
```

```text
The run holds exactly the same 49 ids as before: need-and-verdict 17, need-over-one-cycle 22, SLE-yardstick 10, and every one passed; the hoist changed no test name and no assertion
```

```text
A maintainer can trust the hoist moved shared steps only and changed no rule any test proves
```

##### Disposition

Tested

docs\/feature\/story\-6602\-b\-report\-window\-and\-overlap\/evidence\/compare\-ids\.py with ids\-before\.txt over the TRX of the first declared verification command at the hoist commit

```text
a hoist that changes the value of a shared constant or the five-to-eight script in a way every row still tolerates would pass; the named twin breaks and the review of the hoist diff catch a changed value
```

###### Obligations




##### Covers

P4

```text
Read the Refinement port test classes (port base, port builder and the classes on the port base) and count where each shared member is declared
```

```text
The Gravity ready-rule builder and its ready tag are declared once, in the port builder; the need-text constants Below, In, Above, NoCadence, InsufficientData and NoRefinementStates, the seven cycle working days, the five-to-eight script, the Gravity-two-ready-on-Thursdays Given, the coach tab reader, the need reader and its facts record are declared once, in the port base; the port base reads the tab both for a voter and for a browser key; the SLE-yardstick class has no tab reader of its own and the need-over-one-cycle class has no need reader of its own
```

```text
A maintainer changes a shared Refinement test step in one place and every port class follows
```

##### Disposition

Tested

docs\/feature\/story\-6602\-b\-report\-window\-and\-overlap\/evidence\/members\-once\.py over the Refinement test folder

```text
a copy kept under a new name (for example a second Gravity builder called differently) is not counted; still accepted is a duplicate that does not reuse a known name, which the review of the hoist diff must catch
```

###### Obligations


##### Covers

P4

```text
Run the port Givens guard over the Refinement port classes after the hoist
```

```text
The guard passes: only the port builder writes Team set-up in a Refinement port class, including the hoisted Gravity ready-rule builder; it already passes before the slice and must keep passing
```

```text
A maintainer finds every Team set-up decision of the Refinement port tests in one builder
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PortGivensGuardTest\.cs

```text
a builder hoisted into the port base instead of the port builder that writes Team set-up through the builder's own calls would pass the guard; the members-once check places the builder in the port builder
```

###### Obligations


##### Covers

P3

```text
After the hoist and after the drop, run the report window boundary rows at the application-service port
```

```text
The 15 report window rows keep their ids and all pass, as after S1; the Refinement test changes touch none of them. Their deliberate break stays as recorded for S1: the shortest report window from 14 to 15 days turns a row with a Then of 14 days red
```

```text
A Team owner still sees the same report window bounds the browser wizard shows
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportWindowBoundaryScenarios\.cs\:\:A\_window\_at\_an\_edge\_is\_answered\_as\_the\_table\_says

```text
a change to a Reports file folded into the S2 commits that keeps the 15 ids green would pass; the review of the S2 diff, which must name only Refinement test files, catches it
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
