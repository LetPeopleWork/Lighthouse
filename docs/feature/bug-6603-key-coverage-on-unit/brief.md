# Product brief: bug\-6603\-key\-coverage\-on\-unit

## Request

```text
Bug 6603: A limited API key can pass for an owner whose role on that unit does not apply to it. EffectiveAccess.Covers compares the owner's and the key's roles by general rank (Priority), while the owner's role on a unit only counts if it belongs to that kind of unit (PriorityOn). An owner whose only row on Team 5 is Portfolio Admin cannot read Team 5, yet a key limited to Viewer on Team 5 reads it, and a key limited to Team Admin on Team 5 administers it. Only reachable through a row written outside Lighthouse; every write path refuses it.
```

## Work

S

```text
A limited API key reaches a unit only at a role its owner's role on that unit actually gives, judged the same way as the owner's own reads.
```

### Rework

None.

## Promises

### P1

Outcome

```text
A limited API key reaches a unit only at a role its owner's role on that unit gives there
```

## Scope

None.

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
A key limited to a Team or Portfolio reads or administers it only when its owner's role on that unit gives at least that role there
```

#### Promises

P1

#### Depends on

None.

#### Slice work

None.
