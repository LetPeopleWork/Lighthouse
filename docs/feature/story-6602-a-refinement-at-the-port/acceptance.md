# Acceptance brief: story\-6602\-a\-refinement\-at\-the\-port

## Slices

### S1

##### Covers

P1

P2

```text
Run the yardstick class at the service port: a Team with a whole SLE, without one, with half of one, with no finished work, with slow work finished outside, on the first day of and the day before the Throughput window, and two voters reading the same Team, today 7 October 2026
```

```text
Its 10 rows pass under their unchanged names without a web host: the SLE of 7 days at 85%, the fallback of 12 days at 85% (60 days when slow work finished on the window's first day), and no number when nothing finished inside the window; both voters see the same fallback
```

```text
A maintainer sees the yardstick rules proven at the port, faster, with no test lost
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Refinement\/Slice10SleYardstickScenarios\.cs

```text
The finished-work Given counted days back from a fixed 7 October while the clock read another day, so the window edges shift together and the edge rows still agree; held off by setting today explicitly in every Given and by the deliberate break of a one-day-wider window turning the day-before row red
```

###### Obligations



##### Covers

P2

```text
On the wire, save a Team's SLE through the Team settings (set 75% within 10 days, then clear it) and read the Refinement tab before and after; also read the tab of a Team with no SLE and nothing finished
```

```text
The 2 rows Setting_an_SLE_replaces_the_fallback_on_the_next_read and Clearing_the_SLE_brings_the_fallback_back_on_the_next_read pass under their names in the wire class: the next read answers the saved SLE or the 12-day fallback, and the same Work Items stay listed; the new row shows the source as the text Unavailable with days and probability sent as null
```

```text
A browser keeps reading the yardstick names and texts it reads today, and an SLE an admin saves is what voters answer against next
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.Yardstick\.cs

```text
The yardstick days or probability left out of the answer when there is no number instead of sent as null; held off by asserting the JSON kind Null, not a null read of a missing member
```

###### Obligations


##### Covers

P1

```text
Run the host-use guard after the yardstick class moves
```

```text
The guard passes and its list of host users still to move no longer names the yardstick class; the list gains no line
```

```text
A maintainer sees one class fewer starting a web host
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/TestHostUseGuardTest\.cs

```text
The class line stays on the list while the class no longer starts a host; the guard asks for the line to go in the same commit
```

###### Obligations


##### Covers

P3

```text
Run the port-Givens guard over the port classes and its planted classes, including a planted port class that adds to a Team's To Do states, sets a nested setting reachable from a Team, or writes a guarded table through the database context
```

```text
No real port class writes Team set-up outside the port builder, and each planted case, the To Do states addition among them, is reported by the guard
```

```text
A maintainer knows a port scenario cannot pass on a Team no settings save would produce
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/PortGivensGuardTest\.cs

```text
The guard reads only direct setters on Team and misses collection mutators and nested setters; held off by one planted case per gap
```

###### Obligations



##### Covers

P3

```text
Run the settings-parity rows of the Refinement wire contract: a Team set up by the port builder against one saved through the settings form, compared on SLE probability and range, states and Throughput history as well as how it refines
```

```text
The parity rows pass and compare every public property of those Team fields, so a field the builder sets otherwise than the save turns them red
```

```text
A maintainer knows the port builder stores what the settings save stores
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Refinement\/RefinementWithoutSignInWireContractTest\.cs

```text
Comparison through a JSON serialisation that skips ignored members; held off by a public-property dump
```

###### Obligations

None.

##### Covers

P2

```text
Compare the sorted test ids of the yardstick class and the wire class after the move with the ids recorded before it
```

```text
The only differences are the 2 rows that moved to the wire class and the 1 new wire row
```

```text
A maintainer sees no test lost or renamed by the move
```

##### Disposition

NotTested

```text
Checked by comparing the run's id list with the recorded before list (design verification step 2), not by a test
```

crafter and reviewer

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A maintainer running the three test classes (SLE yardstick, need and verdict, comments) sees them execute without starting a web host, and the list of tests still waiting to move off a web host no longer names any of them.
```

#### E2

##### Promises

P2

```text
After the move, every test id the three classes had before still exists: 49 rows run at the application-service port and 7 route rows remain in RefinementWithoutSignInWireContractTest, and each earlier assertion is still made somewhere.
```

#### E3

##### Promises

P3

```text
The guard checks named at the earlier slice review (PortGivensGuardTest, E10, E13) each fail when a deliberately planted violating case is introduced, and pass again when it is removed.
```

#### E4

##### Promises

P4

```text
Measured the same way as before, the combined run time of the three classes after the move is lower than the time measured before the move.
```

## Charter

None.

## Examination

None.
