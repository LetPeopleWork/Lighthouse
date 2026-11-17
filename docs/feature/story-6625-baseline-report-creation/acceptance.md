# Acceptance brief: story\-6625\-baseline\-report\-creation

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A Team or Portfolio Admin creating a Then & Now report is never asked for a Now length; Now is always exactly as long as Then, and the report compares the Throughput Total of Then against Now.
```

#### E2

##### Promises

P2

```text
With today 15 Nov 2026, Then length 30 days and default end date, the Then step shows Then 17 Sep - 16 Oct 2026 and Now 17 Oct - 15 Nov 2026 (both 30 days), no overlap line, no Now question, and the saved report has a 30-day Now ending today.
```

#### E3

##### Promises

P3

```text
Setting Then to end today (15 Nov 2026, 14 days) shows Then and Now both as 2 Nov - 15 Nov 2026 with the text 'Now and Then are the same days today. Now moves forward from tomorrow; the comparison becomes meaningful as days pass.', and the report can still be created.
```

#### E4

##### Promises

P4

```text
Setting Then to end 31 Oct 2026 with 30 days (today 15 Nov) shows Now 17 Oct - 15 Nov 2026 and the text 'Now overlaps Then until 31 Oct 2026'.
```

#### E5

##### Promises

P5

```text
When the kept history starts one day after the first day the Then would need, picking that length shows a message that the Team has too little history for this length, the user cannot proceed until choosing another length or end date, and no window is silently shifted or shortened.
```

#### E6

##### Promises

P6

```text
Creating Then & Now reports for several owners at once from the Reports page asks no Now question, and every saved report has a Now as long as the chosen Then (30 days) ending today.
```

#### E7

##### Promises

P7

```text
A report saved with Then 30 days and Now 45 days shows, after the update, a 30-day Now ending today; its Then window and frozen values are unchanged, past PDF exports are untouched, and no notice is shown.
```

#### E8

##### Promises

P8

```text
Edit report lets the user change only the name and shown metrics, offers no Now length, and does not re-check Then against kept history, so an old report whose Then predates kept history remains editable.
```

#### E9

##### Promises

P9

```text
A create or edit request with Then 90 days and Now length 45 days is refused with a clear message that Now is always as long as Then; the same request with Now 90 days or no Now length is accepted.
```

#### E10

##### Promises

P10

```text
A 30-day report with Throughput Total 52 in Then and 65 in Now shows beside 'Total 65 Work Items' the change '▲ 13 (+25 %)' marked as better; the per-day line keeps its own change '▲ 0.43 per day (+25 %)'; and the CHANGE summary judges Throughput on its Total line.
```

#### E11

##### Promises

P11

```text
Throughput Total 0 in Then and 0 in Now makes both the Total line and per-day line read 'No change (both 0)' with no arrow and no %, and the CHANGE summary counts Throughput as no change.
```

#### E12

##### Promises

P12

```text
A 14-day report with Throughput Total 0 in Then and 30 in Now shows the Total line as '▲ 30 (Then was 0)' with no %.
```

#### E13

##### Promises

P13

```text
A 30-day report with Throughput Total 91 in both Then and Now shows the Total line with the existing grey dash mark and 'No change', counted as no change in the CHANGE summary.
```

#### E14

##### Promises

P14

```text
A report created before Throughput was captured shows no Total change and Throughput is not judged in the CHANGE summary.
```

## Charter

None.

## Examination

None.
