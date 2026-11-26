# Product brief: story\-6382\-s09a\-a\-rules\-first

## Request

```text
Story 6382 S09a feature A, rules first: measure the published set of every context from the compiled code (ContextContractsMembership), classify every rule pattern FrontDoor or Place and widen FrontDoor patterns to Application|Contracts, then add the contracts-only rule (R-C1..R-C3) with its complete measured baseline, before any type moves.
```

## Work

L

```text
Before any context publishes a Contracts namespace, the rules already know what each context publishes and refuse any new cross-context edge outside it, so features B to D move types under a rule that bites.
```

### Rework

None.

## Promises

### P1

Outcome

```text
Every context's published set is measured from the compiled code and every rule pattern is classified FrontDoor or Place, with front doors widened to Application|Contracts, and no rule verdict changes.
```

#### Amendment

None.

### P2

Outcome

```text
The contracts-only rule (R-C1 to R-C3) exists, lists every measured cross-context edge, and catches a planted offender per rule and per list position.
```

#### Amendment

None.

## Scope

```text
The Architecture test files of this feature: ContextContractsMembership and its test, the namespace-move law contracts map, ContextContractsArchUnitTest, and the widened FrontDoor patterns.
```

```text
Any production move (feature C and D), module start-up (feature B), domain events (D1), the forecast refresh-log reaction (D2), and any behaviour, schema, route or DTO change.
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
A reviewer reading the architecture-rules output sees each context's published set listed from the compiled code (type, context, old and new namespace, seed or closure), every rule pattern classed FrontDoor or Place, and the front-door patterns widened to Application|Contracts. The namespace-move law over the contracts map is RED before the widening and GREEN after, every existing rule keeps its verdict, and the start-condition checks for the four earlier Stories and the re-measured counts are recorded.
```

#### Promises

P1

#### Depends on

None.

#### Slice work

M

```text
Later features move types into Contracts under rules that already know what each context publishes.
```

##### Rework

None.

### S2

```text
A reviewer sees the contracts rule fail on a planted cross-context type outside a context's published set, once per rule R-C1, R-C2, R-C3 and once per list position. R-C3 (ten contexts) is observed RED against an enumerator that returns nothing, then GREEN when it reads the assembly. R-C1 lists every measured cross-context edge, and R-C2 is empty.
```

#### Promises

P2

#### Depends on

S1

#### Slice work

M

```text
Any new cross-context edge outside what a context publishes is refused from now on.
```

##### Rework

None.
