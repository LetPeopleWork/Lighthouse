# Architecture brief: story\-6537\-a\-percentiles\-suite

## Paradigm

object_oriented

## Shared records

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/Slice07WhereTheLimitsActuallyMovedScenarios\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/Slice07WhereTheLimitsActuallyMovedSpecifications\.cs

Extend

None.

docs\/feature\/story\-6537\-a\-percentiles\-suite\/evidence\/s1\_compare\_ids\.py

#### Verification

```text
python3 docs/feature/story-6537-a-percentiles-suite/evidence/s1_compare_ids.py
```

#### Supports

docs\/feature\/story\-6537\-a\-percentiles\-suite\/evidence\/s1\-expected\-ids\.txt

#### Public oracle

```text
Run Slice07WhereTheLimitsActuallyMovedTest, ProcessBehaviorSnapshotWriterTest and RetentionEdgeTest (Debug, tokens removed), compare their sorted test ids with the expected id list, then apply one deliberate break per moved or rewritten rule in the working tree and run the named unit row
```

```text
Slice07WhereTheLimitsActuallyMovedTest holds exactly one row, Limits_stop_where_the_team_stopped_being_watched_and_a_second_look_changes_nothing, and it passes; every other id of the three classes equals the expected list and passes; each of the nine removed host rows maps in the id table to a named unit row; each deliberate break (a family line removed from FamiliesFor, the zero-band gate removed, the retention edge anchored on today instead of the last observed day, FillDayIfAbsent passing asOf null) turns its named unit row red
```

```text
A removed host row with no unit row that turns red for its rule's production line; an id missing or extra beyond the nine removed and the added unit rows; Slice07 holding more or fewer than the one route row; a production file in the diff
```

##### Promises

P1

P2

P3

#### Prefactoring

None.

#### Records

None.

## ADRs

None.
