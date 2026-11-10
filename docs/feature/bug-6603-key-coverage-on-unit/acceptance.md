# Acceptance brief: bug\-6603\-key\-coverage\-on\-unit

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
When a limited API key is used on a unit where its owner holds a role, the key can act on that unit at that role, and no higher.
```

#### E2

##### Promises

P1

```text
When a limited API key is used on a unit where its owner holds no role, the key is refused, however the key itself is scoped.
```

#### E3

##### Promises

P1

```text
When the owner role on a unit changes (raised, lowered or removed), the key follows the owner role on its next use, with no new key needed.
```

## Charter

None.

## Examination

None.
