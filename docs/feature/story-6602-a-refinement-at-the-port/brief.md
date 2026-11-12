# Product brief: story\-6602\-a\-refinement\-at\-the\-port

## Request

```text
Story #6602, feature A: move Slice10SleYardstickTest, Slice05NeedAndVerdictTest and Slice12CommentsTest from the web host to the application-service port, behaviour-free, each leaving HostUsersStillToMove in the commit that moves it; the 7 rows that test a route join RefinementWithoutSignInWireContractTest. Design: docs/feature/story-6602-refinement-and-report-window-at-the-port/feature-delta.md
```

## Work

M

```text
The three Refinement acceptance classes prove the same rules without starting a web host, so the backend Integration tier is faster and no assertion is lost
```

### Rework

None.

## Promises

### P1

Outcome

```text
Slice10SleYardstickTest, Slice05NeedAndVerdictTest and Slice12CommentsTest run at the application-service port without a web host, and HostUsersStillToMove no longer names them
```

### P2

Outcome

```text
Every test id of the three classes survives the move: 49 rows at the port and 7 route rows in RefinementWithoutSignInWireContractTest, with no assertion lost
```

### P3

Outcome

```text
The guard gaps named at T5's slice E review are closed: PortGivensGuardTest, E10 and E13 each fail on a planted case
```

### P4

Outcome

```text
The summed time of the three classes, measured as T5 measured it, is lower after the move than before
```

## Scope

```text
Slice10SleYardstickTest, Slice05NeedAndVerdictTest, Slice12CommentsTest; RefinementAtThePortTest, RefinementPortGivens, PortGivensGuardTest, E10, E13, ScriptedForecasts clock; up to 9 new wire rows
```

```text
ReportWindowBoundaryScenarios and a Reports port base (feature B, after the checkpoint)
```

```text
Dropping rows that repeat NeedOverOneCycleTest (feature B, after the move)
```

```text
Any production code change
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
The yardstick class runs at the service port, with Work Item set-up in the port builder and a stricter PortGivensGuardTest and E10
```

#### Promises

P1

P2

P3

#### Depends on

None.

#### Slice work

M

```text
Slice10SleYardstickTest proves its 10 rules at the port and its 2 route rows on the wire; the port builder is the only writer of Team set-up
```

##### Rework

None.

### S2

```text
The need-and-verdict class runs at the service port, with voting at the port and a strict settings stand-in
```

#### Promises

P1

P2

P3

#### Depends on

S1

#### Slice work

M

```text
Slice05NeedAndVerdictTest proves its 17 rules at the port and its route row on the wire; an unanswered settings read fails loud
```

##### Rework

None.

### S3

```text
The comments class runs at the service port, with the sizing log and its errors pinned by wire rows
```

#### Promises

P1

P2

P4

#### Depends on

S2

#### Slice work

M

```text
Slice12CommentsTest proves its 22 rules at the port and its 4 route rows on the wire; the three classes together cost less time than before
```

##### Rework

None.
