# Acceptance brief: story\-6602\-a\-refinement\-at\-the\-port

## Slices

None.

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
