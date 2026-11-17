# Acceptance brief: story\-6535\-arrivals\-in\-reports

## Slices

### S1

##### Covers

P3

```text
Any finite gap per day, near zero and at half steps, rounded to two decimals
```

```text
The sentence reads balanced exactly when the shown gap is zero, more above, fewer below, with the shown magnitude
```

```text
The words always agree with the number the reader sees
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ArrivalsPanel\.test\.tsx\:\:gapSentenceOf

```text
A build that tests the sentence on the unrounded gap, so 0.004 reads more started
```

###### Obligations






##### Covers

P2

```text
Any pair of Then and Now gaps per day to three decimals
```

```text
The change equals the difference of the shown gaps and is better exactly when it fell
```

```text
The shown Then plus the shown change equals the shown Now
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ArrivalsPanel\.test\.tsx\:\:lineComparisonsOf

```text
A regression guard over the existing shared comparison: it passes before the feature exists, and only a build that changes that comparison to work from unrounded gaps would fail it. Absence of a percent is pinned on the rendered change text, not here
```

###### Obligations




##### Covers

P1

P2

P5

```text
Open a Team report, on screen and as its print document, showing Arrivals with Then started 1.10 and finished 0.80 per day and Now 0.90 and 1.00
```

```text
Then reads 0.30 more started than finished per day, Now reads 0.10 fewer, the change reads 0.40 per day marked better with no percent, Work Items started in total and Throughput per day shown, same words in the print
```

```text
The reader sees whether the gap between Work Items started and finished per day got smaller from Then to Now, on screen and in the PDF
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ArrivalsPanel\.test\.tsx

```text
A build that keeps a percent on the gap, shows it only on screen, or labels the rows differently from the approved copy
```

###### Obligations

None.

##### Covers

P2

```text
Open a report whose Arrivals gap fell from Then to Now
```

```text
The header change summary lists Arrivals under Improved
```

```text
The reader sees Arrivals counted as improved in the header
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ArrivalsPanel\.test\.tsx

```text
A build that marks the panel better but leaves Arrivals out of the header, or lists it under Got worse
```

###### Obligations

None.

##### Covers

P3

P5

```text
Open, on screen and as its print document, a report whose gaps are 0.004 per day Then and 0.006 per day Now
```

```text
Then reads as many started as finished per day, Now reads 0.01 more started than finished per day, the change reads 0.01 per day marked worse, the same in the print
```

```text
The reader never sees a signed zero gap and the change agrees with the shown numbers, on screen and in the PDF
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ArrivalsPanel\.test\.tsx

```text
A build that tests the unrounded gap for zero, rounds after comparing, or rounds only in the screen path
```

###### Obligations

None.

##### Covers

P4

P5

```text
Open, on screen and as its print document, a report that never froze Arrivals but shows it
```

```text
Both sides read Not captured for this report with dashes and no change, and the header lists Arrivals in no group, the same in the print
```

```text
The reader sees Not captured for this report, never a Then rebuilt from live data, on screen and in the PDF
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ArrivalsPanel\.test\.tsx

```text
A build that shows zeros or a rebuilt value instead of dashes, or drops the note in the print
```

###### Obligations

None.

##### Covers

P1

P4

```text
Create and read the Arrivals metric over windows of one, 30 and 90 days: nothing finished, nothing started, balanced, started above and below finished, for a Team and a Portfolio, and for a report that froze nothing
```

```text
Total started, started per day, finished per day and the unrounded signed gap per day, each over the window's days; Then stays as frozen; a report that froze nothing shows no Then
```

```text
The numbers behind the panel are the Metrics tab's own, frozen for Then and fresh for Now
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ArrivalsReportMetricTest\.cs

```text
A build that rounds the gap in the backend, computes finished per day differently from Throughput, or rebuilds Then on read
```

###### Obligations








##### Covers

P1

```text
A reader creating a Then & Now report sees the metrics step, with Arrivals on offer, and ticks it
```

```text
An Arrivals checkbox is last in the metrics step under the fixed name Arrivals, and ticking it puts arrivals among the shown metrics
```

```text
The reader can choose to show Arrivals on the report
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/MetricsAndNameStep\.test\.tsx

```text
A build that labels the box with a lowercase key or a terminology word, or offers it first
```

###### Obligations

None.

##### Covers

P1

```text
A Team and a Portfolio that started three Work Items in Then and finished two make a report showing Arrivals, and one with nothing started or finished
```

```text
Then freezes total 3, 0.10 started per day, 0.07 finished per day and a 0.03 gap per day; an empty window is a real zero; Arrivals is offered last
```

```text
A Team or Portfolio report made with Arrivals shows what the owner really started and finished
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs

```text
A build that registers the metric for Teams only, reads the wrong reader for Portfolios, or returns zeros for every window
```

###### Obligations

None.

##### Covers

P1

```text
The reader opens the info control on the Arrivals panel
```

```text
It reads: Arrivals are Work Items started. The gap is started minus finished, per day. Lower is better.
```

```text
The reader learns what the gap is and which way is better
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ArrivalsPanel\.test\.tsx

```text
A build with no info control or different words
```

###### Obligations

None.

##### Covers

P4

```text
A reader edits a report made before Arrivals existed
```

```text
Arrivals is shown disabled and unticked with the helper Not captured for this report
```

```text
The reader is told Arrivals cannot be added to an old report
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/MetricsAndNameStep\.test\.tsx

```text
A build that lets the box be ticked, or hides it
```

###### Obligations

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
On a Then and Now report for a Team or a Portfolio the reader can tick Arrivals, and the report then shows an Arrivals panel with a Then part and a Now part.
```

#### E2

##### Promises

P1

```text
Each Arrivals part shows Work Items started in total and per day, Throughput per day, and the gap per day (started minus finished) in words with its sign. The gap carries a change mark where a lower gap is better, and no percent change is shown.
```

#### E3

##### Promises

P2

```text
For a Team report with Then started 1.10 and finished 0.80 per day and Now started 0.90 and finished 1.00 per day, the opened report reads 0.30 more started than finished per day for Then and 0.10 fewer started than finished per day for Now, and marks the gap change of 0.40 per day as better, with no percent.
```

#### E4

##### Promises

P3

```text
A gap per day that rounds to zero reads as many started as finished (for example 0.004), and a gap of 0.006 reads 0.01 more started than finished per day.
```

#### E5

##### Promises

P4

```text
A report created before this release does not offer Arrivals when opened or edited, and wherever Arrivals would appear on it, it reads Not captured for this report.
```

#### E6

##### Promises

P5

```text
Exporting a report that shows Arrivals as a PDF prints the Arrivals panel with the same values and words as on screen.
```

## Charter

None.

## Examination

### E1

met

#### Observations

```text
Own probe frontend-tests ArrivalsPanel,MetricsAndNameStep: 2 files, 18 passed, 0 failed. Row titles quote rendered text, report and print: Total started 99 Work Items; Arrivals 1.10 per day; Throughput 0.80 per day; 0.30 more started than finished per day (Then); 0.10 fewer started than finished per day (Now); Change gap per day 0.40 per day marked better, no percent; header lists Arrivals under Improved; 0.004 reads as many started as finished per day, 0.006 reads 0.01 more started than finished per day; Not captured for this report on both sides, Arrivals in no header group; wizard offers Arrivals last, ticking adds it; on pre-release report Arrivals disabled and unticked with Not captured for this report. Limit: producer oracle rows, not a rendered-UI capture; no real PDF file, print document rows only; no row names Team vs Portfolio.
```

```text
Own probe backend-tests ArrivalsInReportsScenarios (HEAD 1239446a0): 4 passed, 0 failed. Rows: A_report_shows_how_many_Work_Items_the_owner_started_and_finished_in_Then for team and portfolio; A_window_where_nothing_was_started_or_finished_reads_a_real_zero_and_Arrivals_is_offered_last for team and portfolio. So Portfolio owner reports capture and offer Arrivals at the application-service level. Limit: backend scenarios, no Portfolio rendered panel text observed in the frontend.
```

### E2

met

#### Observations

```text
Own probe frontend-tests ArrivalsPanel,MetricsAndNameStep: 2 files, 18 passed, 0 failed. Row titles quote rendered text, report and print: Total started 99 Work Items; Arrivals 1.10 per day; Throughput 0.80 per day; 0.30 more started than finished per day (Then); 0.10 fewer started than finished per day (Now); Change gap per day 0.40 per day marked better, no percent; header lists Arrivals under Improved; 0.004 reads as many started as finished per day, 0.006 reads 0.01 more started than finished per day; Not captured for this report on both sides, Arrivals in no header group; wizard offers Arrivals last, ticking adds it; on pre-release report Arrivals disabled and unticked with Not captured for this report. Limit: producer oracle rows, not a rendered-UI capture; no real PDF file, print document rows only; no row names Team vs Portfolio.
```

```text
Probe backend-tests Arrivals: 49 passed, 0 failed, 'Test Run Successful'. Rows include Then_freezes_and_Now_shows_started_and_finished_per_day_with_the_gap_between_them_unrounded with fixtures (90,99,72,1.1,0.8,0.3), (30,27,30,0.9,1.0,-0.1), (30,5,0,0.1667,0,0.1667), (30,0,4,0,0.1333,-0.1333); Then_stays_as_it_was_frozen_and_is_never_rebuilt_from_live_data; A_report_that_froze_nothing_for_arrivals_shows_no_Then_value_and_still_computes_Now. Limit: backend-side values only; no rendered words or print text observed.
```

### E3

met

#### Observations

```text
Own probe frontend-tests ArrivalsPanel,MetricsAndNameStep: 2 files, 18 passed, 0 failed. Row titles quote rendered text, report and print: Total started 99 Work Items; Arrivals 1.10 per day; Throughput 0.80 per day; 0.30 more started than finished per day (Then); 0.10 fewer started than finished per day (Now); Change gap per day 0.40 per day marked better, no percent; header lists Arrivals under Improved; 0.004 reads as many started as finished per day, 0.006 reads 0.01 more started than finished per day; Not captured for this report on both sides, Arrivals in no header group; wizard offers Arrivals last, ticking adds it; on pre-release report Arrivals disabled and unticked with Not captured for this report. Limit: producer oracle rows, not a rendered-UI capture; no real PDF file, print document rows only; no row names Team vs Portfolio.
```

```text
Probe backend-tests Arrivals: 49 passed, 0 failed, 'Test Run Successful'. Rows include Then_freezes_and_Now_shows_started_and_finished_per_day_with_the_gap_between_them_unrounded with fixtures (90,99,72,1.1,0.8,0.3), (30,27,30,0.9,1.0,-0.1), (30,5,0,0.1667,0,0.1667), (30,0,4,0,0.1333,-0.1333); Then_stays_as_it_was_frozen_and_is_never_rebuilt_from_live_data; A_report_that_froze_nothing_for_arrivals_shows_no_Then_value_and_still_computes_Now. Limit: backend-side values only; no rendered words or print text observed.
```

### E4

met

#### Observations

```text
Own probe frontend-tests ArrivalsPanel,MetricsAndNameStep: 2 files, 18 passed, 0 failed. Row titles quote rendered text, report and print: Total started 99 Work Items; Arrivals 1.10 per day; Throughput 0.80 per day; 0.30 more started than finished per day (Then); 0.10 fewer started than finished per day (Now); Change gap per day 0.40 per day marked better, no percent; header lists Arrivals under Improved; 0.004 reads as many started as finished per day, 0.006 reads 0.01 more started than finished per day; Not captured for this report on both sides, Arrivals in no header group; wizard offers Arrivals last, ticking adds it; on pre-release report Arrivals disabled and unticked with Not captured for this report. Limit: producer oracle rows, not a rendered-UI capture; no real PDF file, print document rows only; no row names Team vs Portfolio.
```

```text
Probe backend-tests Arrivals: 49 passed, 0 failed, 'Test Run Successful'. Rows include Then_freezes_and_Now_shows_started_and_finished_per_day_with_the_gap_between_them_unrounded with fixtures (90,99,72,1.1,0.8,0.3), (30,27,30,0.9,1.0,-0.1), (30,5,0,0.1667,0,0.1667), (30,0,4,0,0.1333,-0.1333); Then_stays_as_it_was_frozen_and_is_never_rebuilt_from_live_data; A_report_that_froze_nothing_for_arrivals_shows_no_Then_value_and_still_computes_Now. Limit: backend-side values only; no rendered words or print text observed.
```

### E5

met

#### Observations

```text
Own probe frontend-tests ArrivalsPanel,MetricsAndNameStep: 2 files, 18 passed, 0 failed. Row titles quote rendered text, report and print: Total started 99 Work Items; Arrivals 1.10 per day; Throughput 0.80 per day; 0.30 more started than finished per day (Then); 0.10 fewer started than finished per day (Now); Change gap per day 0.40 per day marked better, no percent; header lists Arrivals under Improved; 0.004 reads as many started as finished per day, 0.006 reads 0.01 more started than finished per day; Not captured for this report on both sides, Arrivals in no header group; wizard offers Arrivals last, ticking adds it; on pre-release report Arrivals disabled and unticked with Not captured for this report. Limit: producer oracle rows, not a rendered-UI capture; no real PDF file, print document rows only; no row names Team vs Portfolio.
```

```text
Probe backend-tests Arrivals: 49 passed, 0 failed, 'Test Run Successful'. Rows include Then_freezes_and_Now_shows_started_and_finished_per_day_with_the_gap_between_them_unrounded with fixtures (90,99,72,1.1,0.8,0.3), (30,27,30,0.9,1.0,-0.1), (30,5,0,0.1667,0,0.1667), (30,0,4,0,0.1333,-0.1333); Then_stays_as_it_was_frozen_and_is_never_rebuilt_from_live_data; A_report_that_froze_nothing_for_arrivals_shows_no_Then_value_and_still_computes_Now. Limit: backend-side values only; no rendered words or print text observed.
```

### E6

met

#### Observations

```text
Own probe frontend-tests ArrivalsPanel,MetricsAndNameStep: 2 files, 18 passed, 0 failed. Row titles quote rendered text, report and print: Total started 99 Work Items; Arrivals 1.10 per day; Throughput 0.80 per day; 0.30 more started than finished per day (Then); 0.10 fewer started than finished per day (Now); Change gap per day 0.40 per day marked better, no percent; header lists Arrivals under Improved; 0.004 reads as many started as finished per day, 0.006 reads 0.01 more started than finished per day; Not captured for this report on both sides, Arrivals in no header group; wizard offers Arrivals last, ticking adds it; on pre-release report Arrivals disabled and unticked with Not captured for this report. Limit: producer oracle rows, not a rendered-UI capture; no real PDF file, print document rows only; no row names Team vs Portfolio.
```

```text
Probe backend-tests Arrivals: 49 passed, 0 failed, 'Test Run Successful'. Rows include Then_freezes_and_Now_shows_started_and_finished_per_day_with_the_gap_between_them_unrounded with fixtures (90,99,72,1.1,0.8,0.3), (30,27,30,0.9,1.0,-0.1), (30,5,0,0.1667,0,0.1667), (30,0,4,0,0.1333,-0.1333); Then_stays_as_it_was_frozen_and_is_never_rebuilt_from_live_data; A_report_that_froze_nothing_for_arrivals_shows_no_Then_value_and_still_computes_Now. Limit: backend-side values only; no rendered words or print text observed.
```
