# Acceptance brief: story\-6359\-s03a\-b\-move

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Users, trackers and the database see no change from the tracker integration move: same behaviour and same stored data.
```

#### E2

##### Promises

P2

```text
Category crossing, state-transition mapping, parent and dependency selection and dependency field references give users the same results as before the move. Namespace placement is architectural and goes to candidate review, not user observation.
```

#### E3

##### Promises

P3

```text
Connector behaviour, auth setup and board listing are unchanged for users after the port and factories move. Namespace placement of the port, factories and board types is architectural and goes to candidate review.
```

#### E4

##### Promises

P4

```text
Each of the five trackers still connects, authenticates and syncs exactly as before the move, and CI still runs that tracker's live suite.
```

#### E5

##### Promises

P5

```text
OAuth connect, token refresh, provider handling and connection health behave for users exactly as before the move. Namespace placement is architectural and goes to candidate review.
```

#### E6

##### Promises

P6

```text
Every existing test passes unmodified apart from moves. Refresh answers, forecasts, routes, DTOs, DI lifetimes, handler order and the EF model are unchanged, and the live connector suites are green in CI.
```

## Charter

None.

## Examination

None.
