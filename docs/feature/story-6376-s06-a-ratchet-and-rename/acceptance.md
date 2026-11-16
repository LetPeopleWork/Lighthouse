# Acceptance brief: story\-6376\-s06\-a\-ratchet\-and\-rename

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A connector-throws check lists every connector method that throws a not-supported error, per connector folder; adding a new throwing method not on the list makes the check fail naming that method.
```

#### E2

##### Promises

P1

```text
Removing the not-supported throw from a listed connector method (or deleting the method) makes the check fail naming that listed method as stale; the unchanged codebase passes.
```

#### E3

##### Promises

P2

```text
No member or parameter of the connector port or its implementations names a Portfolio as Project; Portfolio terminology appears instead, visible in the public connector surface.
```

#### E4

##### Promises

P2

```text
After the rename, refreshes, validations and responses behave exactly as before: the existing connector backend tests pass with unchanged outcomes and response shapes.
```

## Charter

None.

## Examination

None.
