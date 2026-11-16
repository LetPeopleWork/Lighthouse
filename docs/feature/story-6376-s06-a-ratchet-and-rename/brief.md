# Product brief: story\-6376\-s06\-a\-ratchet\-and\-rename

## Request

```text
Story #6376 S06 Connector capability interfaces, nwdes feature A of two (ratchet and rename). Make KPI 7 a test: no connector type creates a NotSupportedException apart from an inline list, read from the compiled IL so throws in private helpers and async bodies count, with made-up connectors as falsifiers. Rename the connector port's GetFeaturesForProject to GetFeaturesForPortfolio and its project parameters to portfolio, as a pure rename commit. Behaviour-free: routes, DTOs, responses, sync paths and refresh results stay the same. Design: docs/feature/story-6376-s06-connector-capability-interfaces/feature-delta.md (slices A1-A2, Ratchet design, D-6, D-7).
```

## Work

M

```text
The connector port's unsupported members are counted by a test that names each one, and the port speaks the glossary's Portfolio, so feature B can remove the throws one capability at a time
```

### Rework

None.

## Promises

### P1

Outcome

```text
A test lists every connector method that creates a NotSupportedException by name, fails naming any connector method that creates one and is not on the list, and fails naming any listed method that no longer creates one
```

### P2

Outcome

```text
The connector port names a Portfolio as a Portfolio: no member or parameter of the connector port or its implementations says Project for a Portfolio, and refreshes, validations and responses are unchanged
```

## Scope

```text
The KPI 7 ratchet test over the compiled connector types, and the GetFeaturesForProject rename across the port, the five connectors, their callers and tests
```

```text
Removing any throw or splitting the port into capability interfaces (feature B)
```

```text
Moving connectors into Modules (S03a #6359)
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
A made-up connector method that throws NotSupportedException, directly, from a private helper or inside an async body, and is not on the inline list, fails the ratchet test with its name; a listed method that stops throwing also fails it
```

#### Promises

P1

#### Depends on

None.

#### Slice work

None.

### S2

```text
No member or parameter of the connector port or of the five connectors names a Portfolio as a Project
```

#### Promises

P2

#### Depends on

None.

#### Slice work

None.
