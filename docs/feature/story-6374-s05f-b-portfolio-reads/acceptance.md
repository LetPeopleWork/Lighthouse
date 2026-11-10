# Acceptance brief: story\-6374\-s05f\-b\-portfolio\-reads

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A maintainer reading the Portfolio metrics code and the GetAllByPredicate allow-list finds no remaining use of GetAllByPredicate by the Portfolio metrics service, and no allow-list entry naming it.
```

#### E2

##### Promises

P2

```text
For each named Portfolio read, a user comparing it with the predicate it replaced sees the same features or transitions, in the same order, with the same related data loaded and tracked, on both SQLite and Postgres.
```

#### E3

##### Promises

P3

```text
A user viewing Portfolio metrics sees exactly the same answers as before, and each answer costs the same number of database queries.
```

## Charter

None.

## Examination

None.
