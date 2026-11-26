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
The chart totals every item but treats a missing start date as age zero in the total while still counting it in the item count would fail: the count would read 3, not 2. Only a chart that left the item out of the total but kept it in the count is not caught beyond that count check
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

None.

## Charter

None.

## Examination

None.
