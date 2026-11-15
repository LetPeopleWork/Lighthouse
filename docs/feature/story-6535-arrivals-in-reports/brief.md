# Product brief: story\-6535\-arrivals\-in\-reports

## Request

```text
Show Arrivals in reports: have the option to show Arrivals in the (Then & Now) reports as well, compared against Throughput, Then against Now.
```

## Work

S

```text
A report reader sees whether the balance between Work Items started and finished got better from Then to Now
```

### Rework

None.

## Promises

### P1

Outcome

```text
When creating a Then & Now report for a Team or a Portfolio, the reader can tick Arrivals; the report then shows an Arrivals panel that, for Then and for Now, gives the Work Items started in total and per day, Throughput per day, and the gap per day (started minus finished) in words with its sign, with a change mark on the gap where a lower gap is better and no percent change.
```

### P2

Scenario

```text
The balance improved
```

```text
a Team report whose Then started 1.10 and finished 0.80 Work Items per day, and whose Now started 0.90 and finished 1.00 per day
```

```text
the reader opens the report
```

```text
the Arrivals panel reads 0.30 more started than finished per day for Then, 0.10 fewer started than finished per day for Now, and marks the gap's change of 0.40 per day as better, without a percent
```

### P3

Scenario

```text
Balanced follows the shown value
```

```text
a window whose gap per day is 0.004
```

```text
the reader opens the report
```

```text
that side reads as many started as finished; a gap of 0.006 reads 0.01 more started than finished per day
```

### P4

Scenario

```text
Reports made before Arrivals existed
```

```text
a report created before this release
```

```text
the reader opens or edits it
```

```text
Arrivals is not offered on it, and wherever it appears it reads Not captured for this report
```

### P5

Scenario

```text
Arrivals reaches the PDF
```

```text
a report that shows Arrivals
```

```text
the reader exports it as a PDF
```

```text
the Arrivals panel prints with the same values and words as on screen
```

## Scope

```text
A new Arrivals metric on Then & Now reports for Teams and Portfolios: wizard checkbox, panel, change mark on the gap, header change summary, Not captured on older reports, PDF
```

```text
Ratio of arrivals to Throughput; maximum or median per day; percent change on the gap; a 'closer to zero' direction of good; rebuilding Then for reports created before release; charts or limit counts on the panel; changes outside Then & Now reports
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
A reader ticks Arrivals when creating a Then & Now report and sees whether the gap between Work Items started and finished per day got smaller from Then to Now, on screen and in the PDF, for Team and Portfolio reports
```

#### Promises

P1

P2

P3

P4

P5

#### Depends on

None.

#### Slice work

S

```text
The arrivals-to-Throughput balance, Then against Now, on the report
```

##### Rework

None.
