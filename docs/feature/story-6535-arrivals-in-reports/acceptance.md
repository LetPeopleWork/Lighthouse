# Acceptance brief: story\-6535\-arrivals\-in\-reports

## Slices

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

None.
