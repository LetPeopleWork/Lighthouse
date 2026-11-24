# Product brief: bug\-6665\-former\-parents\-stranded

## Request

```text
Bug 6665: Former parent Features never cleaned up: show on Team page with no Portfolio and stale progress/forecasts
```

## Work

M

```text
A Feature that has left every Portfolio no longer shows on a Team's Features tab and no longer takes part in forecasting, unless it is still the parent of a Feature in some Portfolio, in which case it is kept only as that parent's name
```

### Rework

None.

## Promises

### P1

Outcome

```text
A Feature that has left every Portfolio no longer shows on a Team's Features tab and no longer takes part in forecasting, unless it is still the parent of a Feature in some Portfolio, in which case it is kept only as that parent's name
```

### P2

Scenario

```text
A former Feature that is no one's parent is gone after the next refresh
```

```text
a Feature with remaining work for a Team was in one Portfolio and is the parent of no Feature in any Portfolio
```

```text
it leaves that Portfolio's query and the Portfolio refreshes
```

```text
the Feature no longer exists: it is not on the Team's Features tab and takes no place in the forecast order
```

### P3

Scenario

```text
Deleting a Portfolio removes its Features from Team pages at once
```

```text
a Feature that is in only one Portfolio and is the parent of no Feature in any Portfolio shows on a Team's Features tab
```

```text
the Portfolio is deleted
```

```text
the Feature is gone from the Team's Features tab without waiting for another refresh or a restart
```

### P4

Scenario

```text
A former Feature that still parents a Portfolio Feature is kept only as its name
```

```text
a Feature with remaining work for a Team leaves its last Portfolio while it is still the parent of a Feature in another Portfolio
```

```text
the Portfolio refresh runs
```

```text
the child still shows its parent's name, the former Feature is not on any Team's Features tab and it takes no place in any forecast, so the Features ranked below it are no longer pushed later
```

### P5

Scenario

```text
A parent of a Portfolio Feature keeps its history across refreshes
```

```text
a parent item that is not itself in a Portfolio is the parent of a Feature in a Portfolio
```

```text
the Portfolio refreshes several times and the orphaned-Feature clean-up runs in between
```

```text
the parent is kept as the same item each time, it is never deleted and re-created, and its history and forecasts remain
```

### P6

Scenario

```text
An existing instance is cleaned by its next refresh or start
```

```text
an instance already holds a Feature in no Portfolio that is no one's parent, with old progress and forecasts
```

```text
the next Portfolio refresh or the next start runs the clean-up
```

```text
that Feature is gone without any data migration
```

## Scope

```text
The orphaned-Feature clean-up keeps a Feature only while it is in a Portfolio or is the parent of a Feature in a Portfolio; everything else is deleted
```

```text
A Feature that leaves its last Portfolio loses its remaining work, so it leaves Team Features tabs and forecasting
```

```text
Deleting a Portfolio runs the clean-up at once
```

```text
Existing instances are cleaned by the new rule on their next refresh or start
```

```text
No data migration: existing instances are cleaned by the next refresh or start
```

```text
No UI copy or layout changes; items simply stop showing, so no sketch review
```

```text
Feature to Portfolio stays many-to-many; mapping an item to exactly one unit of the level above belongs to Epic 6455
```

```text
No usage-data event, because a data clean-up fix adds nothing to count
```

## Decisions

```text
Defect verbatim from ADO Bug #6665: "Suspected, not reproduced yet. Found by reading the code while preparing a support session. An affected user reports Epics and Features on a Team's Features tab that belong to no Portfolio, with progress and forecast numbers that look arbitrary. [...] So an item that was once a Portfolio Feature and the parent of another Feature (for example a Portfolio holding both Epics and Features, or an Epic level that was later narrowed out of the query) may stay forever after it leaves every Portfolio. If its FeatureWork rows are not cleared, it keeps showing on the Team page with no Portfolio and with old progress and forecasts. [...] Do these Features also take part in forecasting (the instance-wide order) and push real Features later? Workaround: a fresh instance with narrow queries."
```

#### Status

Confirmed


```text
Maintainer option A (2026-11-24) 1: the clean-up keeps a Feature only while it is in a Portfolio or is the parent of a Feature in a Portfolio; the stored parent flag no longer decides
```

#### Status

Confirmed


```text
Maintainer option A 2: when a Feature leaves its last Portfolio its remaining work is cleared; a parent outside every Portfolio is kept only as a name for its children, not on Team pages, not in forecasts
```

#### Status

Confirmed


```text
Maintainer option A 3: deleting a Portfolio runs the clean-up at once
```

#### Status

Confirmed


```text
Maintainer option A 4: existing instances are cleaned on the next refresh or start; no data migration
```

#### Status

Confirmed


```text
Feature to Portfolio stays many-to-many for this fix; no UI change, so no sketch review; usage data N/A
```

#### Status

Confirmed


## Journey

```text
A Team lead opens the Team's Features tab and sees only Features that belong to a Portfolio
```

```text
None supplied; the report describes progress and forecast numbers that look arbitrary
```

```text
The Team lead reads the forecasts of the Team's real Features, no longer pushed later by stranded items
```

```text
None supplied
```

```text
A Portfolio Feature still shows its parent's name
```

```text
None supplied
```

## Slices

### S1

```text
A Feature that has left every Portfolio and is no one's parent is gone after the next refresh, and at once when its Portfolio is deleted; an existing instance is cleaned the same way by its next refresh or start
```

#### Promises

P2

P3

P6

P1

#### Depends on

None.

#### Slice work

M

```text
Team Features tabs and forecasts stop carrying Features that belong to no Portfolio
```

##### Rework

None.

### S2

```text
A Feature that has left its last Portfolio but is still the parent of a Feature in some Portfolio is kept only as that parent's name: it is not on any Team's Features tab and takes no place in any forecast
```

#### Promises

P1

P4

#### Depends on

S1

#### Slice work

S

```text
Real Features are no longer pushed later in the forecast order by a former Feature kept only as a parent
```

##### Rework

None.

### S3

```text
A parent of a Portfolio Feature is no longer deleted and re-created between refreshes, so its history and forecasts are kept
```

#### Promises

P5

#### Depends on

S1

#### Slice work

S

```text
Parents of Portfolio Features keep their history and forecasts across refreshes
```

##### Rework

None.
