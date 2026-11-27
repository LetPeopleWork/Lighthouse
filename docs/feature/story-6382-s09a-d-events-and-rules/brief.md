# Product brief: story\-6382\-s09a\-d\-events\-and\-rules

## Request

```text
Story 6382 S09a feature D, events and rules: the 15 domain event types move from Models/Events into the Contracts of their publishing context (IDomainEvent stays in the kernel); the one cross-context command inside the modules becomes a reaction, Work Items writing a forecast round's refresh-log entry on ForecastRoundFinished (RX1); the per-module boundary rules give up their module-to-module entries to the single contracts rule R-C1, and ADR-270 and ARCHITECTURE.md section 4 describe contracts and the CI test-selection payoff. Behaviour unchanged apart from who writes the refresh-log entry.
```

## Work

M

```text
Contexts depend on each other only through published contracts and events, each edge has exactly one rule that owns it, and the docs say so.
```

### Rework

None.

## Promises

### P1

Outcome

```text
Each of the 15 domain event types lives in the Contracts of the context that publishes it, and no event type but IDomainEvent is left in Models.Events; behaviour unchanged.
```

#### Amendment

None.

### P2

Outcome

```text
After every forecast round (succeeded, failed, cancelled or skipped) the refresh log holds the same entry as today, written by Work Items reacting to ForecastRoundFinished; ForecastUpdater no longer names the refresh-log service; a failing log write is logged and the round still completes.
```

#### Amendment

None.

### P3

Outcome

```text
Every module-to-module edge is owned by the contracts rule R-C1 alone: a per-module boundary list that names one fails; ADR-270 and ARCHITECTURE.md section 4 describe the contracts layer and the CI test-selection payoff.
```

#### Amendment

None.

## Scope

None.

```text
Any other cross-context command, any fix of a non-contract residue edge, any schema, route, DTO or registration-lifetime change, mutation testing (runs at the end of the Story).
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
The rule that each domain event type lives in its publisher's Contracts fails until the 15 event types move; then it and every other Architecture rule pass.
```

#### Promises

P1

#### Depends on

None.

#### Slice work

S

```text
Event types sit with the context that owns them.
```

##### Rework

None.

### S2

```text
After a forecast round the refresh log holds the same entry as before, written by the Work Items handler of ForecastRoundFinished, and ForecastUpdater names no refresh-log service; a throwing log write is logged and the round completes.
```

#### Promises

P2

#### Depends on

S1

#### Slice work

M

```text
Forecasting no longer commands Work Items; Work Items reacts to an event.
```

##### Rework

None.

### S3

```text
The one-owner-per-edge law fails while any per-module boundary list names a module-to-module edge; once those entries live only in R-C1 it and every other Architecture rule pass.
```

#### Promises

P3

#### Depends on

S2

#### Slice work

S

```text
Each cross-context edge has one owning rule, and the docs describe contracts.
```

##### Rework

None.
