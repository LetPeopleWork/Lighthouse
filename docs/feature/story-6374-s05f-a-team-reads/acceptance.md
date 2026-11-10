# Acceptance brief: story\-6374\-s05f\-a\-team\-reads

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A user reading Team metrics gets the same behavior as before, while the Team reads no longer go through the generic predicate lookup; the generic predicate allow-list no longer lists the Team metrics service.
```

#### E2

##### Promises

P2

```text
Each Team read returns the same items, in the same order, with the same related data and the same change-tracking behavior as the predicate read it replaces, on both SQLite and Postgres.
```

#### E3

##### Promises

P3

```text
Every Team metrics answer a user sees is identical to before, and each answer costs the same number of database queries.
```

## Charter

None.

## Examination

None.
