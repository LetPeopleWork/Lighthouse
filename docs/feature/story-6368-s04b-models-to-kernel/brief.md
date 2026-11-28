# Product brief: story\-6368\-s04b\-models\-to\-kernel

## Request

```text
Story 6368 S04b: reduce Models to the shared kernel and add a kernel-size rule. Every type under Lighthouse.Backend.Models is either the shared kernel ADR-270 Decision 7 allows (IDomainEvent, IEntity, IConcurrencyTokenEntity) or owned by a named context with a blocker measured from the compiled code and the Story that frees it; the free ones move to their owner (Platform/<Area> or Modules/<Owner>/{Domain,Application}); a rule keeps that catalogue true and shrinking. Behaviour-free. Design: docs/feature/story-6368-s04b-models-to-kernel/feature-delta.md (its slice S4, the event move, is already done by Story 6382 and is dropped; its S5 is this feature's S4).
```

## Work

M

```text
A maintainer sees for every Models type who owns it and why it still sits there, and a new context type placed in Models fails the architecture rules and names itself.
```

### Rework

None.

## Promises

### P1

Outcome

```text
Every type in Lighthouse.Backend.Models is on one catalogue, closed both ways: it stands as Kernel (one of the three base types ADR-270 Decision 7 allows) or as owned by a named context with a blocker that is true when measured from the compiled code and the EF model, and the Story that frees it.
```

#### Amendment

None.

### P2

Outcome

```text
The Models types that nothing blocks leave Models: Platform-owned ones to Platform/<Area>, context-owned ones to Modules/<Owner>/Domain or Application, with their unit tests beside them, and every module's boundary and placement rules still green.
```

#### Amendment

None.

### P3

Outcome

```text
A type added to Models that is not on the catalogue, or a catalogue row whose claim is false, fails the architecture rules and names itself and the fix; the rules name the moved types by their new namespace only, with no old-path allowance left.
```

#### Amendment

None.

### P4

Outcome

```text
Nothing a user or a client sees changes: no schema, migration, route, DTO, payload, usage-data event or answer differs from the revision before the Story.
```

#### Amendment

None.

### P5

Outcome

```text
ARCHITECTURE.md section 4 and ADR-270 Decision 7 describe the kernel as it is after the Story and point to the catalogue for every other Models type.
```

#### Amendment

None.

## Scope

None.

```text
Moving EF entities, wire types (DTOs and controller signatures) or types held by a staying Models type; splitting files of mixed standing; moving event types (done by Story 6382); any schema or behaviour change.
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
The catalogue: one row per Models type with its standing (Kernel or owner and measured blocker), the movers named with old and new namespace, and the move law sees the Models map; red today for the movers.
```

#### Promises

P1

P4

#### Depends on

None.

#### Slice work

M

```text
Every Models type has a known owner and a true reason to stay.
```

##### Rework

None.

### S2

```text
The Platform-owned free files move to Platform/<Area>; their catalogue rows name the new namespace.
```

#### Promises

P2

P4

#### Depends on

S1

#### Slice work

S

```text
The platform lane holds its own types.
```

##### Rework

None.

### S3

```text
The context-owned free files move to Modules/<Owner>/Domain or Application with their unit tests; their catalogue rows name the new namespace.
```

#### Promises

P2

P4

#### Depends on

S2

#### Slice work

M

```text
Each context holds its own types.
```

##### Rework

None.

### S4

```text
The rules name moved types by namespace only (movers list empty), a planted Models type off the catalogue fails naming itself, and ARCHITECTURE.md section 4 and ADR-270 D7 describe the kernel.
```

#### Promises

P3

P5

P4

#### Depends on

S3

#### Slice work

S

```text
A new type in Models cannot land unseen.
```

##### Rework

None.
