# Product brief: story\-6374\-s05f\-b\-portfolio\-reads

## Request

```text
S05f #6374, part B: PortfolioMetricsService stops calling GetAllByPredicate and reads through named reads on the owner repositories, behaviour-free, and leaves the GetAllByPredicate allow-list (14 -> 13). Design: docs/feature/story-6374-s05f-metrics-services-off-getallbypredicate/feature-delta.md
```

## Work

M

```text
The Portfolio metrics read features and their transitions through named, provider-translatable reads, with every metrics answer unchanged
```

### Rework

None.

## Promises

### P1

Outcome

```text
PortfolioMetricsService no longer calls GetAllByPredicate, and the GetAllByPredicate allow-list no longer names it
```

### P2

Outcome

```text
Each named Portfolio read returns the same features or transitions, in the same order, with the same related data loaded and tracked, as the predicate it replaces, on SQLite and on Postgres
```

### P3

Outcome

```text
Every Portfolio metrics answer and the number of database queries per answer are unchanged
```

## Scope

```text
PortfolioMetricsService's 13 GetAllByPredicate calls become 6 named reads on IFeatureRepository and the feature state-transition repository
```

```text
TeamMetricsService (feature A of this Story, done)
```

```text
The other Flow metrics callers on the allow-list (BlockedCountSeriesBuilder, OverTimeFillTarget, the two demo backfill handlers), left for S05g
```

```text
Any behaviour, SQL-shape or speed change; reusing the untracked FeaturesOfPortfolio list read
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
PortfolioMetricsService reads only through named reads on the owner repositories, and the allow-list drops it
```

#### Promises

P1

P2

P3

#### Depends on

None.

#### Slice work

None.
