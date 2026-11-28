# Acceptance brief: epic\-6474\-a\-units\-on\-levels

## Slices

### S1

##### Covers

P1

```text
Load the demo data, and separately an installation shaped like real upgrades (Jira, Azure DevOps, Linear, ServiceNow and CSV Teams, a Jira Portfolio reading Epic and Change on the same connection as a Team reading Epic, an Azure DevOps Portfolio reading Epic, a Team spelling its type bug, a Linear Team with no types, Teams called Squads and Portfolios called Initiatives); refresh every Team and Portfolio from its tracker, forecast every Portfolio with pinned random draws, and read every unit's numbers twice on fresh databases
```

```text
For every Team and Portfolio, the item counts by type, the throughput over the last 90 days, the total Work Item Age and its percentiles, and every Feature's When forecast at 50, 70, 85 and 95 percent equal the numbers recorded in the expectation file beside the test, and the two recordings agree with each other
```

```text
Nobody's numbers change: a later change that moves any number fails and names the unit by kind, id and name, the number, what it was and what it is now
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/OneStructure\/NumbersStayTheSameTest\.cs

NumbersStayTheSameTest\.Every\_units\_numbers\_stay\_as\_recorded\_and\_two\_recordings\_agree\(DemoData\)

```text
A change that alters only the spelling of a stored item type a unit reads while every count, throughput, age and forecast stays equal, or a change to a number this recording does not read (cycle time percentiles, How Many forecasts, start forecasts, a Team's own When forecast)
```

###### Obligations

None.

##### Covers

P1

```text
Same recording over the upgrade fixture: one Team stops reading Bug
```

```text
The recording fails naming Team 1 Payments, its Bug count (was 5, now not shown) and its throughput over 90 days (was 6, now 3), plus the Work Item Age and the Portfolio forecasts that moved with it
```

```text
A deliberate change that moves a number is caught and named with its unit and number
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/OneStructure\/NumbersStayTheSameTest\.cs

NumbersStayTheSameTest\.Every\_units\_numbers\_stay\_as\_recorded\_and\_two\_recordings\_agree\(UpgradeFixture\)

```text
A Team that stops reading a type none of its tracker records carry, which changes no number a user reads
```

###### Obligations

None.

##### Covers

P1

```text
The upgrade laws of the restructuring itself: level lists as the union of their units' types, spelling and create defaults, a unit's subset within its level, the one-commit startup migrator, create-on-first-need mappings, the boundary and expand-only architecture checks, and the dual-written type list read by the previous release
```

```text
Each is observed by the oracle of the slice that builds it, and this recording stays green after each of those slices
```

```text
Not shown by this slice: it adds only the check that numbers stay the same, and changes no production code
```

##### Disposition

NotTested

```text
Slice 01 has no production target; the planner, the connection mapping, the migrator and the dual-write these laws govern do not exist until slices 02 to 07, and the architecture and contract tests are declared at their own locators
```

acceptance designer of slices S2 to S5\, and the architecture and contract test locators declared in the design

### S2

##### Covers

P2

```text
An installation whose Terminology calls Teams Squads and Portfolios Initiatives is upgraded and started, on SQLite and on Postgres
```

```text
Exactly two levels: level 1 Squad/Squads with Work Item/Work Items, level 2 Initiative/Initiatives with Feature/Features
```

```text
The organisation's own words name its two levels after the upgrade
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/OneStructure\/TwoLevelsFromTerminologyTest\.cs

Lighthouse\.Backend\.Tests\.Integration\.Containers\.Acceptance\.OneStructure\.TwoLevelsFromTerminologyTest\.The\_upgrade\_gives\_two\_levels\_named\_in\_the\_organisations\_own\_words

```text
The levels were built from the seeded words while the Terminology rows kept the custom ones; tightened by the custom-words fixture, which seeded words cannot satisfy
```

###### Obligations


##### Covers

P3

```text
After the upgrade, the connections' types on both levels are read
```

```text
Jira reports Epic on both levels (its Team and its Portfolio both read it); Azure DevOps reports none
```

```text
A type a Team and a Portfolio of one connection both read stays on both levels and blocks nothing
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/OneStructure\/TwoLevelsFromTerminologyTest\.cs

Lighthouse\.Backend\.Tests\.Integration\.Containers\.Acceptance\.OneStructure\.TwoLevelsFromTerminologyTest\.A\_type\_a\_Team\_and\_a\_Portfolio\_of\_one\_connection\_both\_read\_sits\_on\_both\_levels

```text
The Feature level winning would drop Epic from the Jira Team level and fail both this and the per-connection test
```

###### Obligations


##### Covers

P2

```text
The eight Terminology words are read before the upgrade, the application starts, and they are read again
```

```text
Each of the eight entries answers the same value and the same default as before; a word nobody changed keeps its empty value with its default
```

```text
Every screen and API that shows these words, the Terminology settings included, shows the same words after the upgrade
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/OneStructure\/TwoLevelsFromTerminologyTest\.cs

Lighthouse\.Backend\.Tests\.Integration\.Containers\.Acceptance\.OneStructure\.TwoLevelsFromTerminologyTest\.The\_eight\_words\_of\_Terminology\_read\_the\_same\_after\_the\_upgrade

```text
Words served from the old Terminology rows instead of the levels; the rename test catches that, as a rename must land on the level
```

###### Obligations


##### Covers

P2

```text
After the upgrade, Terminology is updated: team Crew, teams Crews, feature Outcome, features Outcomes
```

```text
Level 1 is named Crew/Crews with Work Item/Work Items, level 2 keeps Initiative/Initiatives and its items become Outcome/Outcomes; Terminology answers the four new values and every other entry exactly as before, an untouched word with its empty value and its default
```

```text
Renaming through Terminology renames the level, and the Terminology settings show what they showed before plus the rename
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/OneStructure\/TwoLevelsFromTerminologyTest\.cs

Lighthouse\.Backend\.Tests\.Integration\.Containers\.Acceptance\.OneStructure\.TwoLevelsFromTerminologyTest\.Renaming\_through\_Terminology\_after\_the\_upgrade\_lands\_on\_the\_level

```text
A rename written to the Terminology rows only would leave the level names unchanged and fail; answering an untouched word's default as its value fails too
```

###### Obligations


##### Covers

P3

```text
An upgraded installation is started a second time
```

```text
The levels and every connection's mapping read exactly as after the first start
```

```text
Restarting the application never changes the levels or the item types a connection offers
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/OneStructure\/TwoLevelsFromTerminologyTest\.cs

Lighthouse\.Backend\.Tests\.Integration\.Containers\.Acceptance\.OneStructure\.TwoLevelsFromTerminologyTest\.A\_second\_start\_leaves\_the\_levels\_and\_the\_mappings\_as\_the\_first\_start\_left\_them

```text
A second start that rewrites the same values; accepted, since what is read is identical
```

###### Obligations


##### Covers

P3

```text
The upgrade plans 200 generated installations of 1-3 connections with Teams and Portfolios reading types from a pool of six in any case, plus one installation with a type on both levels, bug and Bug on two Teams, a connection with no Portfolio and a Linear Team with no types
```

```text
Per connection with units, each level lists the union of its units' types once each ignoring case, a type both levels read on both, and today's defaults on a level with no units (nothing on Linear)
```

```text
No unit loses or gains a type when its connection gets its item types per level
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/OneStructure\/MappingFromUnitsTest\.cs

Lighthouse\.Backend\.Tests\.Acceptance\.OneStructure\.MappingFromUnitsTest\.Each\_level\_of\_a\_connection\_lists\_every\_type\_its\_units\_read\_once\_whatever\_the\_case

```text
Wrong spelling or order of the listed types; pinned by the finite planner cases instead
```

###### Obligations


##### Covers

P3

```text
Jira Teams 1, 2 and 3 spell bug, Bug and Bug
```

```text
The Team level lists Bug once
```

```text
A type is offered in the spelling most units use
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/OneStructure\/MappingFromUnitsTest\.cs

Lighthouse\.Backend\.Tests\.Acceptance\.OneStructure\.MappingFromUnitsTest\.Two\_Teams\_spelling\_Bug\_and\_one\_spelling\_bug\_give\_the\_level\_the\_spelling\_most\_of\_them\_use

```text
Taking the lowest-numbered unit's spelling would give bug and fail; any rule that picks the majority passes
```

###### Obligations


##### Covers

P3

```text
Team 1 spells bug and Team 2 spells Bug on one connection
```

```text
The Team level lists bug
```

```text
A tie in spelling is settled the same way every time
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/OneStructure\/MappingFromUnitsTest\.cs

Lighthouse\.Backend\.Tests\.Acceptance\.OneStructure\.MappingFromUnitsTest\.A\_tie\_in\_spelling\_goes\_to\_the\_Team\_with\_the\_lowest\_number

```text
Taking the spelling of the unit listed first: the test lists Team 2 first, so that fails
```

###### Obligations


##### Covers

P3

```text
Team 3 reads Bug, User Story and Team 7 reads User Story, Task on one connection
```

```text
The Team level lists Bug, User Story, Task in that order
```

```text
Item types are offered in a stable order
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/OneStructure\/MappingFromUnitsTest\.cs

Lighthouse\.Backend\.Tests\.Acceptance\.OneStructure\.MappingFromUnitsTest\.Types\_are\_listed\_in\_the\_order\_they\_first\_appear\_by\_unit\_number

```text
Alphabetical order would give Bug, Task, User Story and fail
```

###### Obligations


##### Covers

P3

```text
A Jira connection has a Team and no Portfolio
```

```text
Its Portfolio level lists Epic
```

```text
A level without units gets today's create default
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/OneStructure\/MappingFromUnitsTest\.cs

Lighthouse\.Backend\.Tests\.Acceptance\.OneStructure\.MappingFromUnitsTest\.A\_Jira\_connection\_with\_Teams\_and\_no\_Portfolio\_gets\_Epic\_on\_the\_Portfolio\_level

```text
No mapping row at all for the empty level would read as empty and fail
```

###### Obligations


##### Covers

P3

```text
A Linear connection has a Portfolio and no Team
```

```text
Its Team level lists nothing
```

```text
A tracker without item types gets no invented types
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/OneStructure\/MappingFromUnitsTest\.cs

Lighthouse\.Backend\.Tests\.Acceptance\.OneStructure\.MappingFromUnitsTest\.A\_Linear\_connection\_with\_a\_Portfolio\_and\_no\_Team\_lists\_nothing\_on\_the\_Team\_level

```text
Absent and empty both pass; the design treats a level without a row as absent
```

###### Obligations


##### Covers

P2

```text
A user without Premium, or who is not a System Admin, updates Terminology
```

```text
The update is refused as today (403 without Premium; the update carries the System Admin guard)
```

```text
Renaming levels through Terminology keeps today's Premium and System Admin guard
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/TerminologyControllerAuthorizationTests\.cs

Lighthouse\.Backend\.Tests\.API\.Integration\.TerminologyControllerAuthorizationTests\.UpdateTerminology\_AsNonPremiumUser\_Returns403

```text
The controller is unchanged in this slice; the System Admin guard is pinned by TerminologyControllerTest.UpdateTerminology_HasSystemAdminRbacGuardAttribute
```

###### Obligations

None.

##### Covers

P1

```text
Every unit's numbers of the demo data and the upgrade fixture are recorded on the slice's code
```

```text
Equal to the numbers recorded before the restructuring
```

```text
Nobody's numbers change
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/OneStructure\/NumbersStayTheSameTest\.cs

Lighthouse\.Backend\.Tests\.Acceptance\.OneStructure\.NumbersStayTheSameTest\.Every\_units\_numbers\_stay\_as\_recorded\_and\_two\_recordings\_agree

```text
The recorder does not run the start-up step; numbers read from the units are unchanged as long as units are untouched, which holds in this slice
```

###### Obligations

None.

##### Covers

P2

P3

```text
A fresh install with seeded Terminology and no connection is started
```

```text
Two levels with the seeded words Team/Teams, Work Item/Work Items, Portfolio/Portfolios, Feature/Features, and no connection mapping
```

```text
A new installation starts with the same two levels and today's words
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/OneStructure\/TwoLevelsFromTerminologyTest\.cs

Lighthouse\.Backend\.Tests\.Integration\.Containers\.Acceptance\.OneStructure\.TwoLevelsFromTerminologyTest\.A\_fresh\_install\_gets\_two\_levels\_with\_the\_seeded\_words\_and\_no\_connection\_mappings

```text
Mappings invented for connections that do not exist would fail; levels built only when Teams exist fail the level assertion
```

###### Obligations


##### Covers

P3

```text
The upgrade fixture (Jira, Azure DevOps, Linear, ServiceNow and CSV Teams, Jira and Azure DevOps Portfolios) is upgraded and started
```

```text
Jira: Team level User Story, Bug, Epic and Portfolio level Epic, Change; Azure DevOps: User Story, Bug once (Bug and bug) and Epic; CSV: User Story, Bug and the default Epic; Linear: nothing on either level; ServiceNow Team level: Incident, Problem
```

```text
Each connection offers on each level exactly the item types its units there read, once each
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/OneStructure\/TwoLevelsFromTerminologyTest\.cs

Lighthouse\.Backend\.Tests\.Integration\.Containers\.Acceptance\.OneStructure\.TwoLevelsFromTerminologyTest\.Each\_connection\_lists\_on\_each\_level\_the\_types\_its\_units\_there\_read\_once\_whatever\_the\_case

```text
A level listing the right types in another spelling or order; accepted, since spelling and order are pinned by the planner cases
```

###### Obligations



##### Covers

P3

```text
The architecture rules are checked over the planner the upgrade uses
```

```text
The planner depends on no database, clock or input/output type
```

```text
The upgrade's mapping decision is the same every time it is planned, whatever the database or the hour
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/OrganisationStructurePlanPurityArchUnitTest\.cs

OrganisationStructurePlanPurityArchUnitTest

```text
A planner reading the database through an interface the rule does not list; the rule must name every EF, clock and I/O type it forbids
```

###### Obligations


## Expectations

### Rows

#### E1

##### Promises

P1

```text
On the seeded demo data, every Team's and Portfolio's item counts by type, 90-day throughput, Work Item Age and every Feature's When-forecast percentiles (50/70/85/95) are identical before and after the restructuring.
```

```text
Oracle sees only the seeded demo data, not an upgrade fixture shaped like a real install, nor custom Terminology or odd type spellings; a deliberate number change being caught by name is not shown.
```

yes

None.

None.

#### E2

##### Promises

P2

```text
A user sees the same unit and item words (singular and plural, eight Terminology entries) on every screen and API as before, custom or seeded, and renaming a term in Terminology shows up on its level.
```

```text
Oracle cannot see every screen and API surface or a rename round trip from the user's view; custom (non-seeded) terminology installs may be unobserved.
```

yes

None.

None.

#### E3

##### Promises

P3

```text
For each connection, level 1 lists exactly the item types its Teams read and level 2 those its Portfolios read, each once ignoring case; a type read on both levels sits on both without blocking; a connection with no units on a level shows User Story and Bug (level 1) or Epic (level 2).
```

```text
Oracle may not cover mixed-case duplicates, same-type-on-both-levels, or connections that have no units on one level across several connection kinds.
```

yes

None.

None.

#### E4

##### Promises

P4

```text
Every existing Team and Portfolio is a unit with the same id, /teams/{id} or /portfolios/{id} route and payload; creating one creates its unit, and deleting one removes members, reports, snapshots and refresh log as today.
```

```text
Oracle may not observe each clean-up artifact (members, reports, snapshots, refresh log) after delete, nor payload byte-equality for existing records.
```

yes

None.

None.

#### E5

##### Promises

P5

```text
After upgrade each unit reads the same item types as its old list ignoring case; a write naming a type its connection does not map to the unit's level is refused with 400 naming type, level and connection (even for System Admins) and saves nothing; a Jira, Azure DevOps or CSV unit left with no types is marked as needing item types and not refreshed while keeping its data.
```

```text
Oracle may not exercise the System Admin refusal, the exact message wording, the nothing-saved check, or the no-refresh and data-kept behaviour for each connector kind.
```

yes

None.

None.

#### E6

##### Promises

P6

```text
Each Team's Feature WIP, automatic WIP adjustment, throughput window (days or fixed dates) and forecast filter rules keep today's values, the Team settings payloads are unchanged, and manual and automatic forecasts give the same answers as before.
```

```text
Oracle cannot see the ownership by Forecasting or the one-record-per-unit keying from the user's view; it sees values and forecasts only, and may not cover fixed-date windows or filter rules.
```

yes

None.

None.

### Waivers

None.

## Charter

None.

## Examination

None.
