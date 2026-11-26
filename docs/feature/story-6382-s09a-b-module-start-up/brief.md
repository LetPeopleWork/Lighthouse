# Product brief: story\-6382\-s09a\-b\-module\-start\-up

## Request

```text
Story 6382 S09a feature B, module start-up: pin every start-up registration at the composition-root port without a host, then move each context's registrations out of Program into its own Add<X> method at the module root (five contexts, then the other five), leaving Program with only the Add<X> calls, host set-up and the one ordered domain-event handler block, and remove the temporary pin.
```

## Work

L

```text
Each context starts itself up, and the composition root only names modules, so moving a context's types later (features C and D) no longer means editing Program, while the container and handler order are proven unchanged.
```

### Rework

None.

## Promises

### P1

Outcome

```text
The start-up registrations are pinned at the composition-root port without a host. Flow metrics, Forecasting, Promises, Licensing and Access register through their own Add<X> method, with the container unchanged.
```

#### Amendment

None.

### P2

Outcome

```text
Work Items, Tracker integration, Refinement, Reports and Organisation set-up register through Add<X>. The domain-event handler block stays in one ordered block in Program, with the container and handler order unchanged.
```

#### Amendment

None.

### P3

Outcome

```text
Program keeps only the Add<X> calls, host set-up and the handler block. The temporary preservation pin is removed once its last green run is recorded.
```

#### Amendment

None.

## Scope

```text
The module start-up of the ten contexts; the preservation pin and the composition-root rule.
```

```text
Any Contracts move (feature C); domain events and RX1 (feature D); any behaviour, registration lifetime, order, schema, route or DTO change.
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
The start-up registrations preservation test (baseline captured before the move) stays green and the composition-root rule, listing Flow metrics, Forecasting, Promises, Licensing and Access as registered by their module, fails until their registrations leave Program.
```

#### Promises

P1

#### Depends on

None.

#### Slice work

M

```text
Five contexts start themselves up and the container is proven unchanged without a host.
```

##### Rework

None.

### S2

```text
The composition-root rule with all ten contexts listed fails until their registrations leave Program; the preservation pin and the handler order pin stay green and unchanged.
```

#### Promises

P2

#### Depends on

S1

#### Slice work

M

```text
The other five contexts start themselves up; the handler block stays in one ordered block in Program.
```

##### Rework

None.

### S3

```text
The composition-root rule's list of Program's remaining module-internal names, emptied to the handlers and measured host types, fails until the strays move; the preservation test is deleted in the same commit as its last recorded green run.
```

#### Promises

P3

#### Depends on

S2

#### Slice work

S

```text
Program names modules only; the temporary pin is gone.
```

##### Rework

None.
