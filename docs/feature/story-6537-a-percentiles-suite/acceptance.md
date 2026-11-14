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
Apply one deliberate break to the production line of each moved or rewritten rule (a family line removed from the families a team or portfolio reports, the history fill cut to one family per day, the zero-band gate removed, the retention edge drawn from today instead of the last observed day, the filled day reading as of nothing, the pinned baseline replaced by the displayed period) and run the unit row the id table names
```

```text
Every removed host row has an id-table row naming the unit test that now proves its rule, including the fill of every family on each past day and the steady limits of a pinned baseline for a team and for a portfolio, and its break turns that row red, with the assertion that carries the rule failing
```

```text
A rule that used to be proven through the web host is still proven, and a change to its production line is caught by a fast unit test
```

##### Disposition

Tested

docs\/feature\/story\-6537\-a\-percentiles\-suite\/evidence\/s1\-id\-table\.md with s1\-break\-evidence\.txt

```text
A unit row that goes red only on a precondition, a fill loop that reaches one family only, or a steady-limits row that covers the team and not the portfolio would still be accepted
```

###### Obligations

None.

##### Covers

P3

```text
Compare the working tree and HEAD with the slice base for any file under the production project, the test-host guard list or the time budget list, and run the host class
```

```text
The oracle prints no changed production, guard-list or budget-list path, and Slice07 holds the one limits-route row, which passes
```

```text
The test speed-up changes no production behaviour, the guard lists do not grow and the limits route is still proven through the host
```

##### Disposition

Tested

python3 docs\/feature\/story\-6537\-a\-percentiles\-suite\/evidence\/s1\_compare\_ids\.py

```text
A production file edited in the working tree and not committed, or a guard list that grew, would still be accepted by an id comparison alone
```

###### Obligations

None.

### S2

##### Covers

P1

P3

```text
Run the tab-period host class and the unit classes of the id table, compare their sorted ids with the expected list, and check no production, guard-list or budget file changed
```

```text
The host class holds exactly its one tab-route row and passes; every other id equals the expected list and passes; no production, guard-list or budget file is in the diff
```

```text
The backend Integration tier spends no time on the ten moved rules, and the tab route is still proven by one host row
```

##### Disposition

Tested

docs\/feature\/story\-6537\-a\-percentiles\-suite\/evidence\/s2\_compare\_ids\.py

```text
A host row left behind next to the unit rows, or a production file edited to make a unit row pass, would show as an extra id or a changed path; a unit row that only passes on today's code is caught by the breaks, not by this comparison
```

###### Obligations

None.

##### Covers

P2

```text
For each of the ten removed host rows, apply one deliberate break to each production rule its path depends on (days walked, families looped, horizons, windows including the team's cycle time window, age as of the day, stored-day kept, held days, last observation, blank day) and run the unit rows
```

```text
Every break turns at least one named unit row of the id table red, and the break is put back
```

```text
No rule loses its test: a regression in the percentile history fill still turns a fast test red
```

##### Disposition

Tested

docs\/feature\/story\-6537\-a\-percentiles\-suite\/evidence\/s2\_breaks\.py

```text
A unit row that exercises the rule on a sample where the broken line makes no difference would stay green; the cut windows of S5 and S5b first began on the same day, and a team window fixed to 30 days stayed green under every earlier row until the 60-day item row was added, which is why they were rerun
```

###### Obligations

None.

##### Covers

P3

```text
A flow coach opens the four percentile tabs of a team that has filled in
```

```text
Every tab covers the same period, through the real route
```

```text
The tab route still has one host row proving the wiring
```

##### Disposition

Tested

Slice06EveryPercentileTabSpansTheSamePeriodTest\.Every\_tab\_covers\_the\_same\_period\_once\_the\_chart\_has\_filled\_in

```text
A route that returned the stored days of one tab for every tab would be caught only if the tabs held different days; the unit rows pin the family set
```

###### Obligations

None.

### S3

##### Covers

P1

```text
Run the Slice05 host class and the unit and port classes named in the id table, compare sorted test ids with the expected list
```

```text
Slice05ReconstructCycleTimeHistoryTest holds three rows after the craft (the read route, the read-fill-read loop, a refresh landing mid-fill); every other id equals the expected list and passes; the sixteen removed host rows are not in the oracle's observed list
```

```text
The backend Integration tier no longer runs sixteen host rows whose rules a unit or port row proves
```

##### Disposition

Tested

docs\/feature\/story\-6537\-a\-percentiles\-suite\/evidence\/s3\_compare\_ids\.py

```text
A removed host row stays in the class (the oracle lists it as EXTRA), or a unit id is renamed or dropped (MISSING)
```

###### Obligations

None.

##### Covers

P2

```text
Apply one deliberate break to each production line a removed host row walked (the walk of every asked day, the 90-day cap, the time budget, the last-observed and first-supported day bounds, the held-day ask, the blank quiet day on fill and daily recording, no rewrite of a written day, the afterwards window, the restore gate and stand-down, the unique-day absorb, the failing-day continue) and run the unit and port classes
```

```text
Every break (S1 to S16 incl. S5r, S9r, S16r in the break evidence) turns at least one named unit or port row red, and each removed host row has an id-table row naming them
```

```text
No rule the removed host rows proved loses its test
```

##### Disposition

Tested

docs\/feature\/story\-6537\-a\-percentiles\-suite\/evidence\/s3\_breaks\.py

```text
A loop, cap, budget, gate or unique-day line is broken and every unit and port row stays green (the break evidence shows a red row for all of them)
```

###### Obligations

None.

##### Covers

P2

```text
Two copies of the application fill the same day against a real database with no web host
```

```text
One point is held for the day, not two; a refusal caused by another copy storing the day is absorbed and a refusal for any other reason is thrown on
```

```text
A chart never shows two points for one day when two copies of the application run
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/TwoCopiesFillingTheSameDayAtThePortTest\.cs

```text
The writer has no backstop for a duplicate day (S15 turns the port row and the writer row red)
```

###### Obligations

None.

##### Covers

P3

```text
Check which production, guard-list and budget files differ from the slice base, and which host rows stay
```

```text
No file under the backend project, the host-user guard list or the time budget list is in the diff; the three staying host rows cover the read route, the read-fill-read loop and a refresh landing mid-fill
```

```text
The wiring of the trend route stays proven by host rows while no product behaviour changes
```

##### Disposition

Tested

docs\/feature\/story\-6537\-a\-percentiles\-suite\/evidence\/s3\_compare\_ids\.py

```text
A production or guard-list file is changed (the oracle prints CHANGED)
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
