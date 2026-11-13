# Acceptance brief: story\-6537\-a\-percentiles\-suite

## Slices

### S1

##### Covers

P1

```text
Compare the ids of the host class and the unit classes with the expected list
```

```text
The host class holds only the one limits-route row; every other id equals the expected list and passes
```

```text
The backend Integration tier no longer fills 200 days of history nine times to prove rules a unit test proves
```

##### Disposition

Tested

python3 docs\/feature\/story\-6537\-a\-percentiles\-suite\/evidence\/s1\_compare\_ids\.py

```text
A host row is removed but its rule has no unit row, or an extra row stays at the host
```

###### Obligations

None.

##### Covers

P2

```text
Apply one deliberate break per moved or rewritten rule and run the named unit row
```

```text
Each named unit row turns red for its break; the id table maps each of the nine removed host rows to such a row
```

```text
No rule lost its proof when its host test left
```

##### Disposition

Tested

docs\/feature\/story\-6537\-a\-percentiles\-suite\/evidence\/s1\-id\-table\.md with s1\-break\-evidence\.txt

```text
A unit row mirrors the production computation and stays green after the production line is deleted
```

###### Obligations

None.

##### Covers

P3

```text
Keep the limits-route host row and check the working tree diff
```

```text
The route row passes and no file under Lighthouse.Backend/Lighthouse.Backend/ changes
```

```text
The limits route is still proven end to end and the product behaves as before
```

##### Disposition

Tested

python3 docs\/feature\/story\-6537\-a\-percentiles\-suite\/evidence\/s1\_compare\_ids\.py

```text
The route row is deleted along with the rest, or a production file is edited to make a unit row pass
```

###### Obligations

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
