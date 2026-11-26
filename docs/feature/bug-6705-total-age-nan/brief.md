# Product brief: bug\-6705\-total\-age\-nan

## Request

```text
Bug #6705 (reported by Gunnar R. Fischer on Slack): for one team, 'Work Items Total Work Item Age Over Time' shows nothing. The tooltip reads 'NaN days total (40 items) - Click for details'; the details list the right items. Cause found: an in-progress item with no start date is counted in progress from its created date by the server, but the client gets startedDate = null and sums NaN. Maintainer decision: the server sends the fallback start date for such an item (the frontend should not care), and if a start date can still be null, the frontend handles it gracefully.
```

## Work

S

```text
The Total Work Item Age Over Time chart draws its line for every team, even when an in-progress item has no recorded start date.
```

### Rework

None.

## Promises

### P1

Outcome

```text
An in-progress item with no recorded start date is sent to the client with its created date as its start date, the same date the server already counts its age from
```

#### Amendment

None.

### P2

Outcome

```text
When a start date is still missing, the Total Work Item Age Over Time chart skips that item and still shows a proper chart of the others
```

#### Amendment

None.

## Scope

None.

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
An in-progress item with no start date reaches the client with its created date as its start date
```

#### Promises

P1

#### Depends on

None.

#### Slice work

None.

### S2

```text
A work item with no start date is skipped by the Total Work Item Age Over Time chart, which still totals the other items and draws its line
```

#### Promises

P2

#### Depends on

None.

#### Slice work

None.
