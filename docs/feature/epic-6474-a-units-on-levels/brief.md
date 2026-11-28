# Product brief: epic\-6474\-a\-units\-on\-levels

## Request

```text
Epic 6474 One structure, feature A of four: Teams and Portfolios become units on two seeded levels (slices 01, 02, 03, 04, 07 of docs/feature/epic-6474-one-structure), with nobody's numbers changing. Product authority: docs/feature/epic-6474-one-structure/feature-delta.md (DISCUSS, DESIGN, DEVOPS and the maintainer decisions of 2026-11-06, 2026-11-27 and 2026-11-28, which win over the slice files), the slice files and design/upstream-changes.md.
```

## Work

L

```text
Every Team and Portfolio sits as a unit on one of two levels whose names come from the organisation's own Terminology and whose item types each connection maps, while every unit reads exactly the items and shows exactly the numbers it did before the upgrade.
```

### Rework

None.

## Promises

### P1

Outcome

```text
Nobody's numbers change: for every Team and Portfolio of the demo data and of an upgrade fixture shaped like real installs, item counts by type, 90-day throughput, Work Item Age and every Feature's When-forecast percentiles (50/70/85/95 on pinned draws) are recorded before the restructuring and stay equal after each slice; a deliberate change that moves a number is caught, naming the unit and the number. Units whose type spelling changes to the mapping's spelling keep identical numbers and may refetch once.
```

#### Amendment

None.

### P2

Outcome

```text
An installation has exactly two levels: level 1 where the work is done (Teams, Work Items) and level 2 that groups the level below (Portfolios, Features). Their unit and item names (singular and plural) come from today's Terminology, custom or seeded, and every screen and API that shows those eight Terminology entries shows the same words as before; renaming through Terminology lands on the level.
```

#### Amendment

None.

### P3

Outcome

```text
Each connection maps item types onto the levels: per connection, level 1 lists every type its Teams read and level 2 every type its Portfolios read, once each ignoring case; a type read on both levels of one connection sits on both, without blocking anything; a connection with no units on a level gets today's create defaults for it (User Story, Bug on level 1; Epic on level 2).
```

#### Amendment

None.

### P4

Outcome

```text
Every Team is a unit on level 1 and every Portfolio a unit on level 2, keeping its id, its /teams/{id} or /portfolios/{id} route and its payloads; creating a Team or Portfolio creates its unit, and deleting one removes it with the same clean-up as today (members, reports, snapshots, refresh log).
```

#### Amendment

None.

### P5

Outcome

```text
A unit reads exactly its own subset of the types its connection maps to its level, equal after the upgrade to its old list ignoring case; an empty list keeps today's meaning for connectors that do not use types; a /teams or /portfolios write naming a type the connection does not map to the unit's level is refused with 400 naming the type, the level and the connection (for example: Spike is not on the Team level of letpeoplework.atlassian.net), for everyone, System Admins included, and nothing is saved; a Jira, Azure DevOps or CSV unit left with no types is marked as needing item types and is not refreshed until one is ticked, keeping its data.
```

#### Amendment

None.

### P6

Outcome

```text
A Team's forecasting inputs (Feature WIP, automatically adjust Feature WIP, throughput window in days or fixed dates, forecast filter rules) are owned by Forecasting, one record per level-1 unit keyed by unit id, with today's values copied and the Team settings payloads unchanged, so manual and automatic forecasts give the same answers.
```

#### Amendment

None.

## Scope

None.

```text
Any screen or visible copy (slices 05, 05b, 06, 08, 08b, 09, 11, 15 in features C and D), Feature-level settings (10), one refresh per unit (12, 16), owners keyed by unit (13), clients (14), and any push before the maintainer's release.
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
Slice 01 (Story 6483): a characterisation check records every unit's numbers for the demo data and an upgrade fixture (Jira, Azure DevOps, Linear, ServiceNow and CSV Teams, a Portfolio reading Epic and Change, custom Terminology Squad and Initiative, a Jira Team and an Azure DevOps Portfolio both reading Epic, a Team reading bug while others read Bug, a Linear Team with no types), agrees with itself on unchanged code and fails naming the unit and the number when a Team stops reading a type. Test only, no production change.
```

#### Promises

P1

#### Depends on

None.

#### Slice work

M

```text
The promise that nobody's numbers change becomes a check every later slice runs.
```

##### Rework

None.

### S2

```text
Slice 02 (Story 6484): after the upgrade (expand-only migration) an installation has two levels named from its Terminology, each connection maps its units' types per level once ignoring case (a type both levels read sits on both), a fresh install or a connection with no units on a level gets today's create defaults, GET /terminology/all returns the same eight entries served from the levels, PUT /terminology writes them to the levels under today's Premium and System Admin guard, and slice 01's check stays green.
```

#### Promises

P2

P3

P1

#### Depends on

S1

#### Slice work

L

```text
Names and item types get one source before any screen shows a level.
```

##### Rework

None.

### S3

```text
Slice 03 (Story 6485): every Team is a unit on level 1 and every Portfolio a unit on level 2 with unchanged ids and routes; creating one creates its unit and deleting one removes it, with UnitCreated and UnitDeleted published by Organisation set-up and today's deletion clean-up running as their handlers; slice 01's check stays green.
```

#### Promises

P4

P1

#### Depends on

S2

#### Slice work

M

```text
Teams and Portfolios become one kind of unit on a level before a third level arrives.
```

##### Rework

None.

### S4

```text
Slice 04 (Story 6486): each unit's fetch reads its subset of the types its connection maps to its level, equal to its old list ignoring case (only respelled units refetch once); /teams and /portfolios still return it as workItemTypes in the mapping's spelling; a write naming an unmapped type gets 400 naming type, level and connection and saves nothing; a Jira, Azure DevOps or CSV unit with no types carries needsItemTypes, is not refreshed by schedule or Update (409 unit-needs-item-types) and keeps its data; slice 01's check stays green.
```

#### Promises

P5

P1

#### Depends on

S3

#### Slice work

L

```text
Item types belong to the connection's levels and a unit only narrows them.
```

##### Rework

None.

### S5

```text
Slice 07 (Story 6489): a Team's Feature WIP, auto-adjust flag, throughput window and forecast filter rules live in a Forecasting record keyed by its unit id, copied by an expand-only migration; Team settings payloads are byte-identical and forecasts unchanged; slice 01's check stays green.
```

#### Promises

P6

P1

#### Depends on

S3

#### Slice work

M

```text
The unit holds only structure; level-specific forecasting inputs belong to Forecasting.
```

##### Rework

None.
