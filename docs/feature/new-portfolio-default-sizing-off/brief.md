# Product brief: new\-portfolio\-default\-sizing\-off

## Request

```text
Default Feature sizing (Story 6504's per-Portfolio switch) confuses people. New Portfolios start with it off; existing Portfolios keep their value. Only new Portfolios, no data migration (maintainer, 2026-11-25).
```

## Work

S

```text
A person who creates a Portfolio sees Features without child Work Items counted as 0, not as an inferred guess, unless they switch default Feature sizing on.
```

### Rework

None.

## Promises

### P1

Outcome

```text
A person who creates a Portfolio sees Features without child Work Items counted as 0 (shown as 0 of 0 and No child Work Items yet), not as an inferred Default Feature Size, unless they switch Default Feature sizing on.
```

### P2

Scenario

```text
New Portfolio from the wizard starts with Default Feature sizing off
```

```text
a person creates a Portfolio in the create wizard and does not touch the sizing switch
```

```text
the Portfolio is saved and they open its Features that have no child Work Items
```

```text
the switch Infer a size for Features without child Work Items is off, and each such Feature counts as 0 and shows 0 of 0 and No child Work Items yet
```

### P3

Scenario

```text
New Portfolio created through the API without naming the switch starts off
```

```text
a client creates a Portfolio through the API or Lighthouse-Clients and does not name the sizing switch
```

```text
the Portfolio is saved
```

```text
the switch is off, the same as for the wizard
```

### P4

Scenario

```text
Existing Portfolio keeps its stored value
```

```text
a Portfolio that exists before this change has the switch on
```

```text
Lighthouse is upgraded and the person opens the Portfolio
```

```text
the switch is still on, its forecasts do not move, and no stored value was changed
```

### P5

Scenario

```text
Cloned Portfolio keeps the source value
```

```text
a Portfolio has the switch on
```

```text
a person clones it
```

```text
the clone has the switch on; and a clone of a Portfolio with the switch off has it off
```

### P6

Scenario

```text
A person can switch Default Feature sizing on for a new Portfolio
```

```text
a new Portfolio has the switch off
```

```text
the person switches it on and saves
```

```text
Features without child Work Items get Default Feature Size again, as before this change
```

### P7

Scenario

```text
Demo Portfolios start off unless a demo needs inferred sizing
```

```text
demo data is created
```

```text
a person opens a demo Portfolio
```

```text
its switch is off, except a demo Portfolio that must show Default Feature Size behaviour, which has it on explicitly
```

## Scope

```text
New Portfolios start with Default Feature sizing off: wizard, API and Lighthouse-Clients create without naming the switch.
```

```text
Clone keeps the source Portfolio's value. A person can still switch the setting on or off after creation.
```

```text
Demo-data Portfolios start off; the switch is set on explicitly only where a demo or docs screenshot must show Default Feature Size behaviour.
```

```text
Existing Portfolios: no data migration and no heuristic for already using it, because that would move forecasts on upgrade. They keep their stored value (on).
```

```text
Removing the switch, or changing what on or off does (Story 6504 behaviour stays as built).
```

```text
Usage event design for DefaultFeatureSizingSwitchedOff (id 29): left to DEVOPS, not designed here.
```

## Decisions

```text
D1 (maintainer, 2026-11-25): Default Feature sizing confuses people, so new Portfolios start with the switch off.
```

#### Status

Confirmed


```text
D2 (maintainer, 2026-11-25): Only new Portfolios. Existing Portfolios keep their stored value (on). No data migration, no heuristic for already using it (rejected as too risky).
```

#### Status

Confirmed


```text
D3 (maintainer, 2026-11-25): Cloning a Portfolio keeps the source Portfolio's value; a clone is a copy, not a fresh start.
```

#### Status

Confirmed


```text
D4 (maintainer, 2026-11-25): Creating a Portfolio through the API or Lighthouse-Clients without naming the switch gives off, the same as the wizard.
```

#### Status

Confirmed


```text
Maintainer, 2026-11-25: an API or Lighthouse-Clients create that names the switch (true or false) keeps the named value; only an absent field gets off.
```

#### Status

Confirmed


```text
Maintainer, 2026-11-25 (Off, on where needed): demo-data Portfolios start off like any new Portfolio; only a demo Portfolio that must show Default Feature Size behaviour, for example in a docs screenshot, has the switch on explicitly. Consequence: some screenshots and E2E that relied on inferred sizes change.
```

#### Status

Confirmed


```text
Open for DEVOPS: usage event DefaultFeatureSizingSwitchedOff (29) fires on a save that changes on to off. With off as the new default it no longer measures opting out for new Portfolios. Keep, change, or add a switched-on event?
```

#### Status

Open

Design

```text
Quint relevance, P2 (wizard create, off) and P6 (switch on later): not relevant. Settled happy path only; no refusal, conflict, race, stale input or missing operand.
```

#### Status

Confirmed


```text
Quint relevance, P4 (existing Portfolio keeps value) and P5 (clone keeps source value): not relevant. A stored value is kept or copied; no sad path, nothing is written for existing Portfolios.
```

#### Status

Confirmed


```text
Quint scenario exploration refused by the maintainer: decision D4 already settles a create without the switch (off). Applies to P3, the only value judged Quint-relevant.
```

#### Status

Confirmed


## Journey

```text
Person opens Create Portfolio and walks the wizard; the sizing switch is off and they do not touch it
```

```text
Not supplied by the maintainer; the stated pain is that Default Feature sizing confuses people.
```

```text
Person saves the Portfolio and opens its Features; those without child Work Items show 0 of 0 and No child Work Items yet
```

```text
Not supplied.
```

```text
Person who wants an inferred size for such Features switches Default Feature sizing on in the Portfolio settings
```

```text
Not supplied.
```

## Slices

### S1

```text
A person creates a Portfolio in the wizard and sees Features without child Work Items counted as 0 (0 of 0, No child Work Items yet); an API create without the switch gives the same and one naming the switch keeps it; an existing Portfolio is unchanged; a clone copies the source value; demo Portfolios start off except where one needs inferred sizing; the person can switch it on.
```

#### Promises

P1

P2

P3

P4

P5

P6

P7

#### Depends on

None.

#### Slice work

S

```text
A new Portfolio no longer applies an inferred guess to Features without child Work Items, so forecasts show real child Work Items only unless the person opts in.
```

##### Rework

```text
None
```

```text
Builds on Story 6504 as built; changes only the starting value for new Portfolios.
```
