# Product brief: story\-6374\-s05f\-a\-team\-reads

## Request

```text
S05f #6374, part A: TeamMetricsService stops calling GetAllByPredicate and reads through named reads on the owner repositories, behaviour-free, and leaves the GetAllByPredicate allow-list (15 -> 14). Design: docs/feature/story-6374-s05f-metrics-services-off-getallbypredicate/feature-delta.md
```

## Work

M

```text
The Team metrics read work items, transitions and features through named, provider-translatable reads, with every metrics answer unchanged
```

### Rework

None.

## Promises

### P1

Outcome

```text
TeamMetricsService no longer calls GetAllByPredicate, and the GetAllByPredicate allow-list no longer names it
```

### P2

Outcome

```text
Each named Team read returns the same items, in the same order, with the same related data loaded and tracked, as the predicate it replaces, on SQLite and on Postgres
```

### P3

Outcome

```text
Every Team metrics answer and the number of database queries per answer are unchanged
```

## Scope

```text
TeamMetricsService's 14 GetAllByPredicate calls become 7 named reads on IWorkItemRepository, IFeatureRepository and the work-item state-transition repository
```

```text
PortfolioMetricsService (feature B of this Story)
```

```text
The other Flow metrics callers on the allow-list (BlockedCountSeriesBuilder, OverTimeFillTarget, the two demo backfill handlers), left for S05g
```

```text
The Save call in UpdateTeamMetrics (S07b or S03b), any behaviour, SQL-shape or speed change
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
TeamMetricsService reads only through named reads on the owner repositories, and the allow-list drops it
```

#### Promises

P1

P2

P3

#### Depends on

None.

#### Slice work

None.
