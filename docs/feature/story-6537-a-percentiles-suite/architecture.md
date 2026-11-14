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

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/Slice06EveryPercentileTabSpansTheSamePeriodScenarios\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/Slice06EveryPercentileTabSpansTheSamePeriodSpecifications\.cs

Extend

None.

docs\/feature\/story\-6537\-a\-percentiles\-suite\/evidence\/s2\_compare\_ids\.py

#### Verification

```text
python3 docs/feature/story-6537-a-percentiles-suite/evidence/s2_compare_ids.py
```

#### Supports

docs\/feature\/story\-6537\-a\-percentiles\-suite\/evidence\/s2\-expected\-ids\.txt

#### Public oracle

```text
Run Slice06EveryPercentileTabSpansTheSamePeriodTest and the unit classes named in the S2 id table (Debug, tokens removed), compare their sorted test ids with the expected id list, check that no production, guard-list or budget file changed since the slice base, then apply one deliberate break per moved or rewritten rule and run its named unit row
```

```text
Slice06EveryPercentileTabSpansTheSamePeriodTest holds exactly one row, Every_tab_covers_the_same_period_once_the_chart_has_filled_in, and it passes; every other id equals the expected list and passes; each of the ten removed host rows maps in the id table to a named unit row, and each deliberate break of that rule's production line turns that row red
```

```text
A removed host row whose rule no unit row turns red for, including a loop in the filler or the writer (days, horizons, families) that only the host rows walked; an id missing or extra; Slice06 holding more or fewer than the one tab-route row; a production, guard-list or budget file in the diff
```

##### Promises

P1

P2

P3

#### Prefactoring

None.

#### Records

None.

### S3

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/Slice05ReconstructCycleTimeHistoryScenarios\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/PercentilesOverTime\/Slice05ReconstructCycleTimeHistorySpecifications\.cs

Extend

None.

docs\/feature\/story\-6537\-a\-percentiles\-suite\/evidence\/s3\_compare\_ids\.py

#### Verification

```text
python3 docs/feature/story-6537-a-percentiles-suite/evidence/s3_compare_ids.py
```

#### Supports

docs\/feature\/story\-6537\-a\-percentiles\-suite\/evidence\/s3\-expected\-ids\.txt

#### Public oracle

```text
Run Slice05ReconstructCycleTimeHistoryTest and the unit and port classes named in the S3 id table (Debug, tokens removed), compare their sorted test ids with the expected id list, check that no production, guard-list or budget file changed since the slice base, then apply one deliberate break per moved or rewritten rule and run its named row
```

```text
Slice05ReconstructCycleTimeHistoryTest holds exactly three rows (Opening_the_trend_answers_with_what_is_there_and_writes_nothing_while_the_coach_waits, The_days_the_first_visit_could_not_show_are_there_on_the_next_one, A_refresh_landing_mid_fill_neither_loses_its_day_nor_duplicates_one) and they pass; every other id equals the expected list and passes; each of the sixteen removed host rows maps in the id table to a named unit or port row, and each deliberate break of a production line on its path turns that row red
```

```text
A removed host row whose rule, or a loop, cap, budget, gate or unique-day path it walked, no unit or port row turns red for; an id missing or extra; Slice05 holding other than the three staying rows; a production, guard-list or budget file in the diff
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
