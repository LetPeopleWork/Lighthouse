# Acceptance brief: story\-6368\-s04b\-models\-to\-kernel

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
Every type left in the Models folder is listed on the kernel catalogue as either Kernel or owned by a named context with a stated blocker and the Story that frees it; no type is missing and no row names a type that is gone.
```

```text
A passing rule run does not show the catalogue matches the compiled Models types, nor that each blocker is true; it sees only what the rules enumerate.
```

None.

None.

```text
architecture-rules
```

#### E2

##### Promises

P1

```text
A catalogue row claiming a type is blocked, or is Kernel, is true when checked against the compiled code and the EF model (the named dependency or mapping really exists).
```

```text
Rules pass or fail on stated claims; they do not show a true blocker was not silently dropped or a Kernel type is really one of the three allowed base types.
```

None.

None.

```text
architecture-rules
```

#### E3

##### Promises

P2

```text
Types nothing blocks are no longer in Models: Platform types sit under Platform/<Area>, context types under Modules/<Owner>/Domain or Application, and their unit tests sit beside them.
```

```text
Green rules do not show every unblocked type actually moved or that its tests moved and still run.
```

None.

None.

```text
architecture-rules; backend-tests
```

#### E4

##### Promises

P2

```text
The full backend test suite and every module's boundary and placement rules stay green after the moves.
```

```text
A narrowed or summarised run can hide a skipped or excluded test class; only the named run counts.
```

None.

None.

```text
architecture-rules; backend-tests
```

#### E5

##### Promises

P3

```text
Adding an uncatalogued type to Models, or making a catalogue claim false, makes the architecture rules fail with a message naming the type and the fix; the moved types are accepted only at their new namespace, with no old-path allowance.
```

```text
The oracle may only check the passing state; a rule that never fails is not seen without a deliberate violation.
```

None.

None.

```text
architecture-rules
```

#### E6

##### Promises

P4

```text
Every API answer a user or client reads (platform, access and licensing, deliveries, metrics, forecast, refresh) is identical to the revision before the Story, with no status change.
```

```text
Tests of the moved types do not compare real wire answers across revisions; compared answers cover only the probed endpoints and demo data.
```

None.

None.

```text
platform-answers-compare, access-answers-compare, deliveries-answers-compare, metrics-answers-compare, forecast-answers-compare, refresh-answers-compare, each with base origin/main at start
```

#### E7

##### Promises

P4

```text
The database model is unchanged for both providers: no pending model change and no new migration against the last one.
```

```text
Moving types can change a mapped name or namespace-derived value; unit tests do not ask EF.
```

None.

None.

```text
ef-model-unchanged
```

#### E8

##### Promises

P4

```text
No usage-data event, route or DTO payload is added, removed or renamed by the Story; the event names and values match the revision before.
```

```text
Answer compares do not list the usage-data event catalogue or route table; a renamed namespace could alter a serialised name unseen.
```

None.

None.

```text
platform-answers-compare, backend-tests
```

#### E9

##### Promises

P5

```text
ARCHITECTURE.md section 4 and ADR-270 Decision 7 describe the kernel as it now stands and point to the catalogue for every other Models type; a reader finds no statement that contradicts the catalogue.
```

```text
No automated check reads the prose; a green build says nothing about whether the documents match what shipped.
```

None.

None.

```text
reading the two documents with examine read
```

### Waivers

None.

## Charter

None.

## Examination

None.
