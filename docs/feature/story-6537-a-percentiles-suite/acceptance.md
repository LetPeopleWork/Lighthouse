# Acceptance brief: story\-6537\-a\-percentiles\-suite

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A user running the backend Integration tier sees that each PercentilesOverTime rule is now proven by the lowest-layer test that can prove it (a unit test of the computing class, else a service-port test), and the tier takes less time than before.
```

#### E2

##### Promises

P2

```text
Every removed PercentilesOverTime host test has a row in the id table naming the lower-layer test that now proves its rule, and breaking that rule's production line in a deliberate probe turns that lower-layer test red, so no rule loses its proof.
```

#### E3

##### Promises

P3

```text
Across the change, no production file differs, the test-host guard lists do not grow, and at least one host test per route remains to prove the wiring end to end.
```

## Charter

None.

## Examination

None.
