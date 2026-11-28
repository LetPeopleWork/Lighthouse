# Acceptance brief: epic\-6474\-a\-units\-on\-levels

## Slices

None.

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
