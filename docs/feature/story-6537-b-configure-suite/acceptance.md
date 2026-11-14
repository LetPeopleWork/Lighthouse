# Acceptance brief: story\-6537\-b\-configure\-suite

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Each rule that a ConfigureOncePerConnection host test proves is proven by a test at the lowest layer that can prove it (unit test of the computing class or connector, else the service port); the host classes keep only wire, concurrency and end-to-end rows.
```

#### E2

##### Promises

P2

```text
No rule loses its test: every removed host row has an id-table row naming the test that now proves its rule, and a deliberate break of that rule's production line turns that test red.
```

#### E3

##### Promises

P3

```text
No production file changes, the test-host guard lists do not grow, and each route keeps at least one host row that proves its wiring.
```

## Charter

None.

## Examination

None.
