# Acceptance brief: bug\-6705\-total\-age\-nan

## Slices

### S1

##### Covers

P1

```text
Build the work item DTO for an item in progress that has no start date and a created date
```

```text
The DTO's start date equals the item's created date
```

```text
The chart receives a start date for every in-progress item, so no total turns into NaN
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/WorkItemDtoTests\.cs

StartedDate\_FallsBackToCreatedDate\_WhenAnItemInProgressHasNoStartDate

```text
Start date set to the current day or to a fixed date, only checked for being non-null
```

###### Obligations

None.

##### Covers

P1

```text
Build the DTO for an in-progress item with both a start date and an earlier created date
```

```text
The DTO keeps the item's own start date
```

```text
Items that did start are aged from their real start
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/WorkItemDtoTests\.cs

StartedDate\_KeepsTheItemsOwnStartDate\_WhenItHasOne

```text
Always sending the created date
```

###### Obligations

None.

##### Covers

P1

```text
Build the DTO for a To Do item with no start date and a created date
```

```text
The DTO has no start date
```

```text
An item that has not started is not shown as started
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/WorkItemDtoTests\.cs

StartedDate\_StaysEmpty\_WhenAToDoItemHasNoStartDate

```text
A fallback to created date for a state category other than To Do and Doing, such as Done, is not observed
```

###### Obligations

None.

### S2

##### Covers

P2

```text
Show the Total Work Item Age Over Time chart for two days whose in-progress items include one with no start date (empty on one day, missing on the other) and two with start dates
```

```text
Each day's total is the sum of the ages of the items with a start date (4 days and 6 days), the tooltip reads the same total with an item count of 2, and no value shows NaN
```

```text
The reader sees a drawn line with proper totals and item counts; the item without a start date is left out
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/TotalWorkItemAgeRunChart\.test\.tsx

skips a work item with no start date and still totals the others

```text
An item with no start date is left out of both the total and the item count; a chart that counts it fails on a count of 3, not 2.
```

###### Obligations

None.

##### Covers

P2

```text
Total the age of the started items of a day that holds items with a null start date, items with an undefined start date, items with start dates, or no items with a start date
```

```text
An item with a null or undefined start date is left out of both the total and the item count; the others are summed and counted; a day with no started item gives a total of 0 and a count of 0; no value is NaN
```

```text
The day's total and item count in the chart cover only items that have a start date, and never read NaN
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/totalAgeOfStartedItems\.test\.ts

skips an item whose start date is null

```text
A function that skips only null but lets an undefined start date through, or returns NaN or an empty result instead of 0 over 0 for a day with no started item; the four tests in this file separate those
```

###### Obligations

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
An in-progress item with no recorded start date shows, as its start date, the same created date the age is counted from.
```

```text
Does not see items that have a recorded start date, nor items in other states, and cannot show the date displayed for a start date that is later edited.
```

yes

no

None.

#### E2

##### Promises

P2

```text
When an item has no start date, the Total Work Item Age Over Time chart still renders, plots the other items, and shows no missing-value gap or NaN for the skipped item.
```

```text
Does not see a dataset where every item lacks a start date, nor whether the skipped item is counted in the chart's totals or legend.
```

yes

no

None.

### Waivers

#### E1

```text
Benjamin Huser-Berta (maintainer)
```

```text
2026-11-26T17:49:03Z
```

```text
No admitted public probe shows a work item's start date from the API; decided 2026-11-26
```

```text
WorkItemDtoTests oracle (RED before the fix, GREEN after); maintainer checks the real app after the push
```

#### E2

```text
Benjamin Huser-Berta (maintainer)
```

```text
2026-11-26T17:49:09Z
```

```text
No admitted public probe renders the Total Work Item Age Over Time chart; decided 2026-11-26
```

```text
TotalWorkItemAgeRunChart and totalAgeOfStartedItems oracles (RED before the fix, GREEN after); maintainer checks the real app after the push
```

## Charter

None.

## Examination

### E1

unmet

#### Observations

```text
Probe nwdes --help was refused (ProbeRefused, exit 2). Admitted console scripts: architecture-rules, ef-model-unchanged, metrics-answers-compare, forecast-answers-compare, forecast-triggers-show, refresh-answers-compare, refresh-reactions-show, refresh-reactions-compare, deliveries-answers-compare, access-answers-compare, platform-answers-compare, workitems-edge-show, backend-tests, connector-throws-count, frontend-tests, feature-proof, linux-appimage, ci-live-suites-show, ci-auth-suites-show, test-selection-show, deliveries-answers-selftest, access-answers-selftest, platform-answers-selftest. No admitted name is a user-facing stimulus for a work item start date or the Total Work Item Age Over Time chart.
```

```text
Probe workitems-edge-show (exit 0, 99 tests, 99 passed) covers only: Team refresh reaction order and failure handling (E1), Work Items module ArchUnit boundary rules and Flow metrics list (E2), and Work Items unit test folder placement (E3). Its described scope contains no work item DTO StartedDate assertion and no Total Work Item Age Over Time chart render. It is a candidate test run, so it is not recorded as a user-observable stimulus for P1 or P2.
```

```text
Probe metrics-answers-compare --help exited 128 with fatal: Needed a single revision. No output, no stimulus, no response. The probe yielded no observed behaviour for P1 or P2.
```

observation_gap

#### Waiver

```text
Benjamin Huser-Berta (maintainer)
```

```text
2026-11-26T17:49:03Z
```

```text
No admitted public probe shows a work item's start date from the API; decided 2026-11-26
```

```text
WorkItemDtoTests oracle (RED before the fix, GREEN after); maintainer checks the real app after the push
```

### E2

unmet

#### Observations

```text
Probe nwdes --help was refused (ProbeRefused, exit 2). Admitted console scripts: architecture-rules, ef-model-unchanged, metrics-answers-compare, forecast-answers-compare, forecast-triggers-show, refresh-answers-compare, refresh-reactions-show, refresh-reactions-compare, deliveries-answers-compare, access-answers-compare, platform-answers-compare, workitems-edge-show, backend-tests, connector-throws-count, frontend-tests, feature-proof, linux-appimage, ci-live-suites-show, ci-auth-suites-show, test-selection-show, deliveries-answers-selftest, access-answers-selftest, platform-answers-selftest. No admitted name is a user-facing stimulus for a work item start date or the Total Work Item Age Over Time chart.
```

```text
Probe workitems-edge-show (exit 0, 99 tests, 99 passed) covers only: Team refresh reaction order and failure handling (E1), Work Items module ArchUnit boundary rules and Flow metrics list (E2), and Work Items unit test folder placement (E3). Its described scope contains no work item DTO StartedDate assertion and no Total Work Item Age Over Time chart render. It is a candidate test run, so it is not recorded as a user-observable stimulus for P1 or P2.
```

```text
Probe metrics-answers-compare --help exited 128 with fatal: Needed a single revision. No output, no stimulus, no response. The probe yielded no observed behaviour for P1 or P2.
```

observation_gap

#### Waiver

```text
Benjamin Huser-Berta (maintainer)
```

```text
2026-11-26T17:49:09Z
```

```text
No admitted public probe renders the Total Work Item Age Over Time chart; decided 2026-11-26
```

```text
TotalWorkItemAgeRunChart and totalAgeOfStartedItems oracles (RED before the fix, GREEN after); maintainer checks the real app after the push
```
