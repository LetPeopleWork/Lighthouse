# Product brief: story\-6657\-write\-back\-round

## Request

```text
Place the write-back round (Story 6657): move the 8 write-back round files from the loose Services namespaces into Modules/Forecasting/Application/WriteBack, behaviour-free, with rules first and rules by namespace after
```

## Work

M

```text
The last named exception of the Promises packaging leaves Services; the round sits inside the context that owns it, so S09a sees it there
```

### Rework

None.

## Promises

### P1

Outcome

```text
Every rule keeps its verdict for the write-back round while it moves.
```

### P2

Outcome

```text
The write-back round and its unit tests live in Forecasting's Application/WriteBack, with no behaviour change.
```

### P3

Outcome

```text
Rules name the round by its Forecasting namespace only, and the round has its own boundary rules.
```

## Scope

```text
The 8 write-back round files (5 classes, 3 ports), their unit tests, the rule files that name them, Program.cs and the consumers' using lines, ARCHITECTURE.md §4 and ADR-270.
```

```text
Models/WriteBack (stays in the kernel), Tracker integration's write-back port and connectors, the acceptance and host tests, registration extensions (S09a), any behaviour, schema, route or DTO change.
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
The rules see the write-back round's move before it happens: its membership is measured from the compiled code, the shared namespace-move law covers the write-back map, and the round pattern sees both the old and the new namespace.
```

#### Promises

P1

#### Depends on

None.

#### Slice work

M

```text
No rule loses a write-back edge while the round moves
```

##### Rework

None.

### S2

```text
The write-back round lives in Modules/Forecasting/Application/WriteBack and its unit tests sit beside it; production changes only in namespace and using lines.
```

#### Promises

P2

#### Depends on

S1

#### Slice work

M

```text
The round sits in Forecasting
```

##### Rework

None.

### S3

```text
Every rule names the write-back round by its Forecasting namespace only; the round has its own boundary rules; the architecture docs show it placed in Forecasting.
```

#### Promises

P3

#### Depends on

S2

#### Slice work

M

```text
No rule names the old home; the round is guarded by its own rules
```

##### Rework

None.
