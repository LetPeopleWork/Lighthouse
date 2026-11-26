# Acceptance brief: story\-6382\-s09a\-a\-rules\-first

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
The architecture-rules probe for the module-boundary and unit-test-placement rule classes lists a passing test line for every context's published set, each measured from the compiled code, and for every rule pattern classified as a front door or a place, with front doors covering Application and Contracts. Every rule line in those classes passes, and the ef-model-unchanged probe reports the data model unchanged.
```

```text
A passing rule line does not show that the measured sets are complete or that a pattern was classified correctly; a rule that matches nothing also passes, and the probe cannot show a loosened rule from its output alone.
```

yes

no

None.

#### E2

##### Promises

P2

```text
The architecture-rules probe with the ContextContracts fragment prints passing lines for the contracts-only rules R-C1, R-C2 and R-C3, including lines showing that the listed cross-context edges match the measured edges in both directions and that a made-up offender is caught for each rule and for each list position.
```

```text
Passing lines name tests but not the edges; the probe cannot show whether the list is the true complete set of cross-context edges, or that a planted offender represents a real violation.
```

yes

no

None.

### Waivers

None.

## Charter

None.

## Examination

None.
