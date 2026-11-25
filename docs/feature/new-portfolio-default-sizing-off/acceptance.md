# Acceptance brief: new\-portfolio\-default\-sizing\-off

## Slices

### S1

##### Covers

P2

```text
The create wizard saves a new Portfolio without the person touching the switch
```

```text
The saved settings name the switch off
```

```text
A wizard-created Portfolio starts with sizing off
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/CreateWizards\/CreatePortfolioWizard\.test\.tsx

```text
The wizard omits the field and relies on the server default
```

###### Obligations


##### Covers

P4

```text
A Portfolio stored on is saved by an older client that does not name the switch
```

```text
It stays on
```

```text
Existing Portfolios keep their value and forecasts do not move
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Data\/NewPortfolioStartingValueTest\.cs

```text
A save without the field flips the stored value
```

###### Obligations


##### Covers

P3

```text
An older client payload without the switch is read as the create route reads it and synced into a new Portfolio, and payloads naming true or false likewise
```

```text
Absent gives off, named values are kept and a stored value is kept on later saves
```

```text
Older scripts create Portfolios that start off
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/PortfolioSettingDtoCompatibilityTest\.cs

```text
Absent keeps the old entity default on
```

###### Obligations


##### Covers

P6

```text
The owner of a new Portfolio switches default sizing on and the Portfolio is refreshed again
```

```text
Features without child Work Items get the Default Feature Size again
```

```text
A person can opt in to inferred sizing
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DefaultFeatureSizingTest\.cs

```text
Switching on is ignored after create
```

###### Obligations


##### Covers

P7

```text
Demo data is created and a demo Portfolio is opened
```

```text
Its switch is off except where a screenshot needs inferred sizing
```

```text
Demo Portfolios start off
```

##### Disposition

NotTested

```text
Follows from the entity default; which demo Portfolio needs on is measured per screenshot by the crafter, so no fixed expectation exists
```

crafter

##### Covers

P1

P2

```text
A person creates a Portfolio without touching the sizing switch and it is refreshed with a Feature that has no child Work Items
```

```text
The Feature counts as 0 (0 of 0, No child Work Items yet), with or without a tracker estimate, and no Team carries invented work
```

```text
Features without child Work Items show no inferred size in a new Portfolio
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/DefaultFeatureSizingTest\.cs

```text
The switch reads off but the refresh still applies the Default Feature Size
```

###### Obligations


##### Covers

P3

P5

```text
A Portfolio is created without naming the switch, naming on and naming off, and clones of an on and an off Portfolio are created, on real SQLite saved and reloaded
```

```text
Absent gives off, named values are kept and clones copy the source value
```

```text
API, Clients and clone behave as promised
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Data\/NewPortfolioStartingValueTest\.cs

```text
Absent is stored on by the column default, or an explicit true is stored off
```

###### Obligations






## Expectations

### Rows

#### E1

##### Promises

P1

```text
A person who creates a Portfolio and does not turn on Default Feature sizing sees its Features with no child Work Items counted as 0, shown as 0 of 0 and No child Work Items yet, not as an inferred Default Feature Size.
```

#### E2

##### Promises

P2

```text
A Portfolio created in the create wizard without touching the sizing switch is saved with Infer a size for Features without child Work Items off, and its childless Features count as 0 and show 0 of 0 and No child Work Items yet.
```

#### E3

##### Promises

P3

```text
A Portfolio created through the API or Lighthouse-Clients without naming the sizing switch is saved with the switch off, the same as one created in the wizard.
```

#### E4

##### Promises

P4

```text
A Portfolio that already exists with the switch on still has it on after the upgrade, its forecasts do not change, and its stored value is not rewritten.
```

#### E5

##### Promises

P5

```text
A cloned Portfolio keeps the sizing switch value of its source: a clone of a switch-on Portfolio is on, and a clone of a switch-off Portfolio is off.
```

#### E6

##### Promises

P6

```text
A person who switches Default Feature sizing on for a new Portfolio and saves gets Default Feature Size again for Features without child Work Items, as before this change.
```

#### E7

##### Promises

P7

```text
Demo Portfolios have the sizing switch off, except a demo Portfolio that must show Default Feature Size behaviour, which has it on explicitly.
```

## Charter

None.

## Examination

None.
