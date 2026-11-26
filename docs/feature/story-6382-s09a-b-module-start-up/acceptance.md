# Acceptance brief: story\-6382\-s09a\-b\-module\-start\-up

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
The architecture-rules probe for the composition-root rule (fragment CompositionRoot) prints passing test lines for the five contexts Flow metrics, Forecasting, Promises, Licensing and Access, each showing that its start-up registrations are made by its own Add<X> method; and ef-model-unchanged reports the data model unchanged. No web host is started to see this.
```

```text
The rule reads compiled code only; it does not show that every service resolves at run time or that registration order inside the container is unchanged, and a passing line says nothing about contexts it does not list.
```

yes

no

None.

#### E2

##### Promises

P2

```text
The architecture-rules probe (fragments CompositionRoot and DomainEventHandlerOrderPin) prints passing test lines for the five contexts Work Items, Tracker integration, Refinement, Reports and Organisation set-up, each registered through its own Add<X> method, and a passing line for the single ordered domain-event handler block; ef-model-unchanged reports the data model unchanged.
```

```text
The handler-order pin proves the handler list and order, not that every handler behaves the same; the container's contents are seen only through the rule, not by resolving services from a running host.
```

yes

no

None.

#### E3

##### Promises

P3

```text
The architecture-rules probe (fragment ProgramNamesModulesOnly) prints a passing line saying Program names only the Add<X> calls, host set-up and the handler block, and the temporary preservation test no longer appears in the backend-tests probe output for its fragment (no such test is found), while the composition-root and handler-order lines still pass.
```

```text
A passing rule line does not show the last green run of the pin was recorded before it was deleted; that belongs to review records, and the probe cannot show Program text or order.
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
