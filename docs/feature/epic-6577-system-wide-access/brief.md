# Product brief: epic\-6577\-system\-wide\-access

## Request

```text
Epic 6577: Viewer and Admin access for all Teams and Portfolios through system-wide rules, plus an opt-in switch that lets everyone signed in view. Design: docs/feature/epic-6577-system-wide-access/feature-delta.md, ADR-280, ADR-281.
```

## Work

L

```text
An organisation gives broad read or admin rights with one rule instead of granting each Team and Portfolio or making people System Admin, and stays private by default.
```

### Rework

None.

## Promises

### P1

Outcome

```text
A System Admin adds Viewer on all for one SSO group, and its members read every Team and Portfolio, including ones created later; their limited API keys read what they reach
```

### P2

Outcome

```text
A limited API key whose owner reaches its unit through a rule works at the key's exact scope and reaches nothing beyond its limit
```

### P3

Outcome

```text
A Team or Portfolio Admin sees on the Access tab, read-only, which system-wide rules also reach the unit; Settings shows No direct role instead of Unassigned
```

### P4

Outcome

```text
A System Admin turns on Everyone signed in, after a confirm, and every signed-in person with a profile reads every Team and Portfolio; off by default and no change without opting in
```

### P5

Outcome

```text
Admin on all lets a group's members edit settings and members on every Team and Portfolio, while every System-Admin-only action stays refused
```

## Scope

None.

```text
A system-wide role for one named person, and an API key limited to Viewer on all
```

```text
Hiding one unit from a system-wide rule
```

```text
Creating, cloning or deleting units, connections, system settings, RBAC administration or licence details through Admin on all
```

```text
Team Admins saving group mappings for their own unit (Bug 6576 hides the form instead); refreshing a key's stale group snapshot
```

```text
Moving the page to its target surface (Epic 6473); levels and per-level membership (Epic 6455); pattern-matching rules
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
A System Admin adds Viewer on all for one SSO group, and its members read every Team and Portfolio, including ones created later; their limited API keys read what they reach
```

#### Promises

P1

P2

#### Depends on

None.

#### Slice work

L

```text
Leaders read everything without System Admin, and limited keys follow their owner within their limit (Stories 6578 and 6579)
```

##### Rework

```text
1
```

```text
Admin on all is a valid role and scope pair once S5 adds it
```

### S3

```text
A Team or Portfolio Admin sees on the Access tab, read-only, which system-wide rules also reach the unit; Settings shows No direct role instead of Unassigned
```

#### Promises

P3

#### Depends on

S1

#### Slice work

M

```text
Unit admins trust who reaches their unit (Story 6580, Bug 6576)
```

##### Rework

None.

### S4

```text
A System Admin turns on Everyone signed in, after a confirm, and every signed-in person with a profile reads every Team and Portfolio; off by default and no change without opting in
```

#### Promises

P4

#### Depends on

S1

#### Slice work

M

```text
Open-by-choice organisations need no group (Story 6581)
```

##### Rework

None.

### S5

```text
Admin on all lets a group's members edit settings and members on every Team and Portfolio, while every System-Admin-only action stays refused
```

#### Promises

P5

#### Depends on

S1

#### Slice work

M

```text
Delivery leads administer every unit without running the system (Story 6582)
```

##### Rework

None.
