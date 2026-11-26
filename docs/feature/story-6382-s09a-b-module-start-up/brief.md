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

None.

## Scope

None.

## Decisions

None.

## Journey

None.

## Slices

None.
