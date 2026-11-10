# Architecture brief: bug\-6603\-key\-coverage\-on\-unit

## Paradigm

object_oriented

## Shared records

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Authorization\/EffectiveAccess\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/Authorization\/EffectiveAccessTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~EffectiveAccessTest
```

#### Supports

None.

#### Public oracle

```text
An owner whose only row on a Team is Portfolio Admin (a role that gives nothing on a Team) limits a key to Viewer, and another key to Team Admin, on that Team; both keys ask to read and to write the Team. Control: an owner who is Team Admin on the Team does the same.
```

```text
Both keys of the Portfolio-Admin-only owner are refused read and write on the Team; the Team Admin owner's Viewer key reads and its Team Admin key writes.
```

```text
Either key of the Portfolio-Admin-only owner reads or writes the Team, or the Team Admin owner's keys lose access.
```

##### Promises

P1

#### Prefactoring

None.

#### Records

None.

## ADRs

None.
