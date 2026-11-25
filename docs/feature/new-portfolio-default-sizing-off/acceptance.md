# Acceptance brief: new\-portfolio\-default\-sizing\-off

## Slices

None.

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
