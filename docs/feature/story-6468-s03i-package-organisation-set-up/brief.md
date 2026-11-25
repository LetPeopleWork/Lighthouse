# Product brief: story\-6468\-s03i\-package\-organisation\-set\-up

## Request

```text
ADO Story #6468 S03i Package Organisation set-up: move the 27 Organisation set-up files from the loose Services.Implementation and Services.Interfaces namespaces into Modules/OrganisationSetup/{Domain, Application, Application/Connections, Application/OptionalFeatures} as they are (namespaces and usings only), with every rule seeing the move before it happens and naming the module only after it; downstream modules reach set-up by namespace; set-up gets its own boundary rules and a TeamDeleted ratchet. No behaviour, schema or configuration change. Design: docs/feature/story-6468-s03i-package-organisation-set-up/feature-delta.md.
```

## Work

M

```text
Organisation set-up is the tenth context packaged in its own module and guarded by its own rules, so the units and levels of the next Feature are built inside it.
```

### Rework

None.

## Promises

### P1

Outcome

```text
Every architecture rule keeps its verdict for every moving Organisation set-up type through the move, and every rule pattern that matches a moving type's old name is classified as standing for the context or for the place.
```

### P2

Outcome

```text
The Organisation set-up types live in Modules/OrganisationSetup in their measured layer, their unit tests beside them, and the application behaves exactly as before.
```

### P3

Outcome

```text
Downstream modules reach Organisation set-up by namespace without a list entry, Tracker integration only along its one listed edge, Organisation set-up has its own boundary rules read from the IL, and no second controller can publish a set-up event unseen.
```

### P4

Outcome

```text
Every rule that stands for Organisation set-up names it by Modules.OrganisationSetup only, and no rule constant or literal still names a moved type by its old place.
```

## Scope

```text
The 27 Organisation set-up files of the design's membership, their unit tests, Program.cs usings and registrations, the rule files whose patterns name them, and the architecture documents at the end.
```

```text
Team and Portfolio as units, levels, UnitCreated and UnitDeleted (the next Feature's slice 03); the repositories, the four context seeders and the Models entities, which stay; any behaviour, schema, route or configuration change; fixing the controller-published TeamDeleted event.
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
Before anything moves, the Organisation set-up membership is measured from the IL and every rule pattern matching an old name is classified and, where it stands for the context, widened to old and new
```

#### Promises

P1

#### Depends on

None.

#### Slice work

M

```text
The rules see the move before it happens
```

##### Rework

None.

### S2

```text
Calendar, Feature ordering and ranking and the optional features live in Modules/OrganisationSetup with their unit tests, and every rule stays green
```

#### Promises

P2

#### Depends on

S1

#### Slice work

M

```text
The first half of set-up lives in its module
```

##### Rework

None.

### S3

```text
Settings, terminology, connection defaults and connection usage live in Modules/OrganisationSetup with their unit tests, and every rule stays green
```

#### Promises

P2

#### Depends on

S2

#### Slice work

M

```text
The second half of set-up lives in its module
```

##### Rework

None.

### S4

```text
Downstream modules reach Organisation set-up by right, set-up has its own boundary rules and test placement, and the TeamDeleted ratchet holds
```

#### Promises

P3

#### Depends on

S3

#### Slice work

M

```text
Set-up is an open host with its own rules
```

##### Rework

None.

### S5

```text
Every rule names Organisation set-up by Modules.OrganisationSetup only and the architecture documents show it packaged
```

#### Promises

P4

#### Depends on

S4

#### Slice work

S

```text
The rules name set-up by its module only
```

##### Rework

None.
