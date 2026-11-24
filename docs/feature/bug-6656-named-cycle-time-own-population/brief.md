# Product brief: bug\-6656\-named\-cycle\-time\-own\-population

## Request

```text
Bug 6656: Custom Cycle Time measure not filtering items in scatterplot. With a custom Cycle Time measure (end state Ready for Release), the scatterplot shows exactly the same items as the default Cycle Time (including items Done, In Production, Complete, Cancelled); only the values are recalculated. Expected (reporter): the scatterplot shows all items that have reached Ready for Release, each measured from entering Analysis to reaching Ready for Release, including items that later moved to In Production or Done; items that have not yet reached Ready for Release do not appear as data points.
```

## Work

M

```text
A named Cycle Time shows its own population: every item whose last move into the measure's end state falls in the date range, closed or not, placed on the date it reached the end state; its percentiles use the same population. Applies to team and portfolio.
```

### Rework

None.

## Promises

### P1

Outcome

```text
A named Cycle Time is answered from its own population: items whose end-state crossing falls in the range, closed or not, each with its end date; the default Cycle Time is unchanged
```

### P2

Outcome

```text
With a named Cycle Time selected, the scatterplot shows that measure's own items on the date each reached the end state, with a loading and a failure look
```

## Scope

None.

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
With a named Cycle Time definition, the team and portfolio metrics service answers cycleTimeData, the named percentiles (and percentiles info) and the named outliers from the items whose last forward crossing into the definition's end state falls in the date range, closed or not; each named value carries the end date
```

#### Promises

P1

#### Depends on

None.

#### Slice work

None.

### S2

```text
With a named Cycle Time selected, the Cycle Time scatterplot shows the population the API returns for that definition, places each dot on the date the item reached the end state, labels the x-axis 'Date reached <end state>', shows a loading look (previous dots dimmed under a small spinner) and a could-not-load look, and titles the dot dialog 'Work Items that reached <end state> on <date> with Cycle Time of N days'
```

#### Promises

P2

#### Depends on

None.

#### Slice work

None.
