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

### S4

##### Covers

P1

```text
Run the four host classes and the unit classes, and compare their test ids with the expected lists
```

```text
Each host class holds only the rows the id table keeps as wire or end-to-end rows; the 27 removed rows are listed as extra until the craft removes them; every expected id is present and passes
```

```text
The backend Integration tier runs 27 fewer host rows, each rule now proven by a unit or save-service row
```

##### Disposition

Tested

docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s4\_compare\_ids\.py

```text
A removed host row stays on the host, which the extra-id and guard-line lines of the oracle report
```

###### Obligations

None.

##### Covers

P2

```text
Apply one deliberate break to the production line each removed row walked and run the unit classes
```

```text
Each removed host row maps in the id table to a named unit row, and each break turns that row red
```

```text
No rule loses its test when the host rows go
```

##### Disposition

Tested

docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s4\_breaks\.py

```text
A unit row that passes whatever the production line does; two halves have no break and are named residual risks: no update queued for an Estimation default, and Work Items kept on a refused move
```

###### Obligations

None.

##### Covers

P3

```text
Check the diff against the merge base for production, time budget and guard-list changes
```

```text
No production or budget file changed, no guard-list line was added, only the Slice09 line is admitted to leave, and Slice12, Slice10 and Slice11 keep at least one host row each
```

```text
The wiring is still proven by a host row per route and nothing in production changed
```

##### Disposition

Tested

docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s4\_compare\_ids\.py

```text
A production file edited alongside the tests, which the oracle lists as CHANGED
```

###### Obligations

None.

### S5

##### Covers

P1

P2

```text
A changed Teams default is saved with and without Save and update, and the connection has followers, owners and a Portfolio
```

```text
Only the Teams that follow the default are updated and only on Save and update; the log entry names the followers or says none follow; owners and Portfolios are not touched; each rule has a unit row that turns red when its production line is broken
```

```text
An admin who changes a default sees the followers take it, an owner keep its own value, and one log entry say who follows
```

##### Disposition

Tested

docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s5\_compare\_ids\.py

```text
a save without Save and update still queued the followers, or an owner counted as a follower
```

###### Obligations

None.

##### Covers

P1

P2

```text
A Portfolio and a Team read, show and save the Parent default of their own grain, a default is left out of a save, sent as empty, or named by a field added in the same save
```

```text
A grain never reads the other grain's default; an owner keeps its own value; a left-out default is kept; an empty one is cleared and logged; a new field becomes the Portfolios default only; each rule has a unit row that turns red under a deliberate break
```

```text
An admin sees the Portfolios default separate from the Teams default and a save from an older page never clears a default
```

##### Disposition

Tested

docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s5\_compare\_ids\.py

```text
a Portfolio read the Teams default, or a save that left a default out cleared it
```

###### Obligations

None.

##### Covers

P3

```text
The host classes of the slice, the guard list and the production tree are compared with the slice base
```

```text
Slice06 keeps its end-to-end row and its fingerprint row, Slice08 keeps its Portfolio end-to-end row; their guard lines stay; no production or budget file changes
```

```text
The wiring of the default change still has host rows proving it and the product behaves as before
```

##### Disposition

Tested

docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s5\_compare\_ids\.py

```text
a guard line was added, a production file changed, or the last host row of a route was removed
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

### E1

unmet

#### Observations

```text
Probe feature-proof story-6537-b-configure-suite summary 3cc76466d completed (exit 0, background task bz59at0r1, after the examination wall budget). Diff part 'since 3cc76466d (includes other work merged meanwhile)': production files changed 2 (Lighthouse.Backend/Lighthouse.Backend/Factories/DemoData/Team Zenith.csv; Lighthouse.Backend/Lighthouse.Backend/Models/UsageData/UsageDataEventName.cs); time budget files changed 0; host-user guard list: one line removed (Slice09PortfoliosDependencyFieldDefaultScenarios), no line added. Scope part (feature's own commits): suite commits=72 findings=0 (exit 0). This contradicts observation O2 (scope run earlier in this examination: exit 1, one uncommitted change to JiraWorkTrackingConnector.cs); the O2 finding did not reproduce in this scope run and its cause is not established.
```

```text
Probe feature-proof summary oracles and first breaks (same run as O5). Oracles, exit 0 each: s1_compare_ids observed=280 expected=247 differences=0; s2 observed=84 expected=84 differences=0; s3 observed=58 expected=58 differences=0; s4 observed=223 expected=210 differences=0; s5 observed=176 expected=176 differences=0. Note: s1 and s4 report observed differing from expected counts while differences=0; not explained by the public output. First breaks (one per slice, only the first break of each slice was run): slice1 A1 (save removing a field in use not refused) -> Failed 15, Passed 256 of 271; slice2 H1 (per-Feature finding object) -> Failed 1 of 80; slice3 A1 (naming one end reads both ends) -> Failed 1 of 56; slice4 E1 (Team reads Portfolios Estimation default) -> Failed 10 of 214; slice5 K1 (save without Save and update still queues updates) -> Failed 10 of 168. Table command, the remaining breaks per slice, and the layer (unit vs host) of the named rows were not captured.
```

observation_gap

### E2

unmet

#### Observations

```text
Probe feature-proof story-6537-b-configure-suite summary 3cc76466d completed (exit 0, background task bz59at0r1, after the examination wall budget). Diff part 'since 3cc76466d (includes other work merged meanwhile)': production files changed 2 (Lighthouse.Backend/Lighthouse.Backend/Factories/DemoData/Team Zenith.csv; Lighthouse.Backend/Lighthouse.Backend/Models/UsageData/UsageDataEventName.cs); time budget files changed 0; host-user guard list: one line removed (Slice09PortfoliosDependencyFieldDefaultScenarios), no line added. Scope part (feature's own commits): suite commits=72 findings=0 (exit 0). This contradicts observation O2 (scope run earlier in this examination: exit 1, one uncommitted change to JiraWorkTrackingConnector.cs); the O2 finding did not reproduce in this scope run and its cause is not established.
```

```text
Probe feature-proof summary oracles and first breaks (same run as O5). Oracles, exit 0 each: s1_compare_ids observed=280 expected=247 differences=0; s2 observed=84 expected=84 differences=0; s3 observed=58 expected=58 differences=0; s4 observed=223 expected=210 differences=0; s5 observed=176 expected=176 differences=0. Note: s1 and s4 report observed differing from expected counts while differences=0; not explained by the public output. First breaks (one per slice, only the first break of each slice was run): slice1 A1 (save removing a field in use not refused) -> Failed 15, Passed 256 of 271; slice2 H1 (per-Feature finding object) -> Failed 1 of 80; slice3 A1 (naming one end reads both ends) -> Failed 1 of 56; slice4 E1 (Team reads Portfolios Estimation default) -> Failed 10 of 214; slice5 K1 (save without Save and update still queues updates) -> Failed 10 of 168. Table command, the remaining breaks per slice, and the layer (unit vs host) of the named rows were not captured.
```

observation_gap

### E3

unmet

#### Observations

```text
Probe feature-proof story-6537-b-configure-suite scope: output 'SCOPE uncommitted change to Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs', 'suite commits=72 findings=1', exit 1. Scope check reports one production-file finding for this feature (uncommitted change, Jira connector). Result is a finding against E3 (no production file changes); its scope is the candidate worktree state seen by the check.
```

```text
Probe feature-proof story-6537-b-configure-suite summary 3cc76466d completed (exit 0, background task bz59at0r1, after the examination wall budget). Diff part 'since 3cc76466d (includes other work merged meanwhile)': production files changed 2 (Lighthouse.Backend/Lighthouse.Backend/Factories/DemoData/Team Zenith.csv; Lighthouse.Backend/Lighthouse.Backend/Models/UsageData/UsageDataEventName.cs); time budget files changed 0; host-user guard list: one line removed (Slice09PortfoliosDependencyFieldDefaultScenarios), no line added. Scope part (feature's own commits): suite commits=72 findings=0 (exit 0). This contradicts observation O2 (scope run earlier in this examination: exit 1, one uncommitted change to JiraWorkTrackingConnector.cs); the O2 finding did not reproduce in this scope run and its cause is not established.
```

observation_gap
