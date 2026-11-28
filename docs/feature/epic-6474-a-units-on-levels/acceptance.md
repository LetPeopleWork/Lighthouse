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
