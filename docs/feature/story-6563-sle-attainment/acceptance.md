# Acceptance brief: story\-6563\-sle\-attainment

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Flow Overview of a Team or Portfolio shows a separate widget titled Within SLE: large share with count like '72.7 % (8 / 11)', smaller sentence 'of Work Items closed within 10 days or less', then 'Target 70 %'.
```

```text
Oracle may check text but not that the number is visibly large, the sentence smaller, or the layout order as a reader sees it.
```

None.

None.

None.

#### E2

##### Promises

P1

P12

```text
The large share is green at or above target, amber slightly below, red much below, and the header status word (Sustain/Observe/Act) agrees with the colour.
```

```text
Oracle may check a status string, not the rendered colour, its agreement with the header word, or the 20-point thresholds.
```

None.

None.

None.

#### E3

##### Promises

P2

```text
With an SLE set and at least 5 closed items, status is Sustain at or above target, Observe up to 20 points below, Act more than 20 below; Cycle Time Percentiles status stays unchanged.
```

```text
Boundaries (exactly target, exactly 20 below) and the untouched Percentiles widget may be unseen by an oracle.
```

None.

None.

None.

#### E4

##### Promises

P3

```text
The header trend indicator compares the share with the preceding period of equal length like other widgets; the body shows no change figure of its own.
```

```text
Oracle may not see the visible absence of a body delta nor that the preceding-period comparison is right for the user's range.
```

None.

None.

None.

#### E5

##### Promises

P4

```text
With no SLE set, the widget says no SLE is set, shows status Act, and prompts to define an SLE.
```

```text
Wording and whether the prompt helps the reader reach SLE definition are not visible to a data-level oracle.
```

None.

None.

None.

#### E6

##### Promises

P5

```text
With an SLE and fewer than 5 closed items, the widget shows how many closed, says this is too few to judge, and shows no pass/fail status.
```

```text
Oracle may miss that no status badge or colour appears at all, including at exactly 4 versus 5 items.
```

None.

None.

None.

#### E7

##### Promises

P6

```text
The Predictability category shows a chart of daily within-SLE share against that day's SLE target for items closed in the trailing 30, 60 or 90 days, chosen with a toggle, over days inside the selected date range.
```

```text
Toggle behaviour and limiting days to the range are interaction and rendering facts an API-level oracle does not see.
```

None.

None.

None.

#### E8

##### Promises

P7

```text
After the SLE is changed, previously recorded days keep their recorded share and target; only later days use the new SLE.
```

```text
Oracle may not observe the chart's historical line across a real SLE change.
```

None.

None.

None.

#### E9

##### Promises

P8

```text
Missing days are filled from stored history the same way as Percentiles Over Time, measured against the SLE at fill time, and the chart states that filled-in days use that SLE.
```

```text
Equivalence to Percentiles Over Time and the on-chart statement are not visible to a narrow oracle.
```

None.

None.

None.

#### E10

##### Promises

P9

```text
A day whose trailing window holds fewer than 5 closed items draws no point and the chart says why.
```

```text
Oracle may see missing data but not the explanatory text shown to the reader.
```

None.

None.

None.

#### E11

##### Promises

P10

```text
With no SLE set, the chart says no SLE is set instead of drawing a line.
```

```text
Oracle may not see absence of a line or the message as rendered.
```

None.

None.

None.

#### E12

##### Promises

P11

```text
Without a Premium licence, the widget and the chart are both shown.
```

```text
Oracle may run only with a licence; the unlicensed instance experience is easy to miss.
```

None.

None.

None.

#### E13

##### Promises

P13

```text
View Data in the widget header opens the Cycle Time Percentiles closed-items dialog listing the closed items with close day and Cycle Time coloured against the SLE, longest first.
```

```text
Oracle may not check dialog identity, ordering, colouring, or that the list matches the share's count.
```

None.

None.

None.

#### E14

##### Promises

P14

```text
Days before the first finished item or after the last successful sync draw no point; a range with nothing recorded shows the same sentence as Percentiles Over Time.
```

```text
Reuse of the exact sentence and edge-day behaviour are not visible without a rendered chart.
```

None.

None.

None.

#### E15

##### Promises

P15

```text
Filled-in days draw as open markers, recorded days as filled markers, and a note under the chart says filled-in days are measured against today's SLE.
```

```text
Marker style and note placement are visual facts a data oracle cannot see.
```

None.

None.

None.

### Waivers

None.

## Charter

None.

## Examination

None.
