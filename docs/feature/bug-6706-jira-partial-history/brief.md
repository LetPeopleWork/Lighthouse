# Product brief: bug\-6706\-jira\-partial\-history

## Request

```text
Bug 6706 — Jira: an in-progress item gets no start date when the search returns only part of its history. Found while fixing Bug #6705 (Jira Data Center). An item moved Open → waiting for approval → In analysis → In Progress on 16 May 2025, then In Progress → Suspended (a Doing state) on 13 Nov 2025, with many other edits in between. Lighthouse stored it as Doing with no start date. Effect: an old item still in progress gets no start date (since #6705 the server falls back to its created date, which overstates its age). A finished item gets its start from the closed-date fallback, so its cycle time can be wrong too. Fix direction: when the search answer holds only part of an item's history, fetch the item's full changelog before working out its start and closed dates.
```

## Work

S

```text
A Jira item whose search answer holds only part of its history is dated from its whole history, so its start date, closed date and cycle time are right on Data Center and Cloud.
```

### Rework

None.

## Promises

### P1

Outcome

```text
A Jira item whose search answer holds only part of its history is dated from its whole history, on Data Center and on Cloud
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
A Jira item whose search answer holds fewer changelog entries than its changelog total gets its start and closed dates from its whole history, on Data Center and on Cloud
```

#### Promises

P1

#### Depends on

None.

#### Slice work

None.
