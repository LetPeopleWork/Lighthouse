# Acceptance brief: story\-6537\-b\-configure\-suite

## Slices

### S1

##### Covers

P1

```text
Run the two host classes and the unit classes that now prove their rules, compare sorted test ids with the expected list
```

```text
Each host class holds only its wire, concurrency and skeleton rows; every moved rule has a unit row that passes; the host rows the move removes are listed as extra until the craft removes them
```

```text
The backend Integration tier runs 41 fewer host rows, so it takes less time
```

##### Disposition

Tested

docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s1\_compare\_ids\.py

```text
A host row is removed while its unit twin is missing or renamed, or a unit row stays on the host
```

###### Obligations

None.

##### Covers

P2

```text
Apply one deliberate break per rule and per loop, gate or branch a removed host row walks, including where a Team's item hangs and the refusal when only the Teams default names a field, and run the unit classes
```

```text
Every break turns at least one named unit row red, all failed rows are printed, and the file is restored after each
```

```text
No rule loses its test: each removed host row maps in the id table to a unit row that fails when the production line is broken
```

##### Disposition

Tested

docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s1\_breaks\.py

```text
A unit row only restates the production result so that a break on its path leaves it green, or a removed host row that walks the hang of an item has only a reading test
```

###### Obligations

None.

##### Covers

P3

```text
Compare the working tree with the slice base for production files, the time budget list and the host-user guard list
```

```text
No production or budget file differs, no guard line is added, and the rows that stay still prove the wiring of each route
```

```text
Nothing the product does changes; the wiring rows of each route still run on the host
```

##### Disposition

Tested

docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s1\_compare\_ids\.py

```text
A production file is edited to make a moved row pass, or a guard line is added for a new host user
```

###### Obligations

None.

### S2

##### Covers

P1

P2

```text
Run the two Jira named-link host classes and the unit classes of the S2 id table, compare their test ids with the expected list, then apply one deliberate break per moved rule
```

```text
Every removed host row has an id-table row naming the unit row that proves its rule, every expected id is present and passes, and each break of a production line on a rule's path turns its named row red
```

```text
The Jira named-link and refresh-history rules are still protected by tests, now at connector and service level, so the backend Integration tier runs less
```

##### Disposition

Tested

docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s2\_compare\_ids\.py

```text
A removed host row whose rule has no unit row that turns red when its production line is broken
```

###### Obligations

None.

##### Covers

P3

```text
Compare the files changed since the slice base and the host-user guard list, and run the host rows that stay
```

```text
No production or budget file changed, no guard line was added, and the end-to-end row of each class still passes through the route
```

```text
The wiring of the refresh and the Refresh History and Dependencies routes is still proven by a host row, and nothing in the product changed
```

##### Disposition

Tested

docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s2\_compare\_ids\.py

```text
A production file edited alongside the test move, or a host row that proves the wiring removed with the rest
```

###### Obligations

None.

### S3

##### Covers

P1

```text
Run the two Azure DevOps host classes and the connector unit rows, compare the sorted test ids with the expected list
```

```text
Each host class holds only the rows kept as wire or end-to-end rows; every expected unit id is present and passes; each removed host row maps in the id table to a named unit row
```

```text
The backend Integration tier takes less time and the Azure DevOps relation rules are still checked
```

##### Disposition

Tested

python3 docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s3\_compare\_ids\.py

```text
a host row is removed whose rule no unit row covers, or an id is missing or extra
```

###### Obligations

None.

##### Covers

P2

```text
Break one production line on the path of each removed host row, including the once-per-refresh collapse and the refused connection check, then run the unit rows
```

```text
Each deliberate break turns at least one named unit or port row red, the id table names that row for every removed host row, a refused connection check reports not valid in its unit row, and an existing controller row pins the 400 answer
```

```text
No Azure DevOps relation rule loses its test when the host rows leave
```

##### Disposition

Tested

python3 docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s3\_breaks\.py

```text
a removed host row whose rule, loop, gate or branch no unit row turns red for, including one finding per refresh, one warning line, a refusal reported as valid, or a failed check answered with success
```

###### Obligations

None.

##### Covers

P3

```text
Check that no production or budget file changed since the merge base and that the host-user guard list gained no line
```

```text
The comparison reports no changed production file, no added guard line, and the three wire or end-to-end host rows still present
```

```text
Behaviour is unchanged, the test-host guard does not grow and each route keeps one host row for the wiring
```

##### Disposition

Tested

python3 docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s3\_compare\_ids\.py

```text
a production file is edited or a guard line is added
```

###### Obligations

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
