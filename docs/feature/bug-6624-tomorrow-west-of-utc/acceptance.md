# Acceptance brief: bug\-6624\-tomorrow\-west\-of\-utc

## Slices

### S1

##### Covers

P1

```text
With the browser clock in New York, early in the morning and late in the evening when the UTC date is already the next day, the launch-date rule is asked about today, tomorrow and yesterday written as the day strings the date field holds
```

```text
Tomorrow is accepted with no date error; today and yesterday are refused with the message that the launch date must be in the future
```

```text
A user in the Americas can save a Delivery dated tomorrow; today and past dates stay blocked
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFormRules\.westOfUtc\.test\.ts

```text
A fix that accepts every future-looking date including today, or that only works at one time of day, is caught by the today row and the two clock moments; a run on an east-of-UTC clock is caught by the offset check
```

###### Obligations

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A user in any time zone, including zones west of UTC, can choose tomorrow's date as a launch date and it is accepted.
```

#### E2

##### Promises

P1

```text
Choosing today's date as a launch date is refused in every time zone.
```

#### E3

##### Promises

P1

```text
Choosing a past date as a launch date is refused in every time zone.
```

## Charter

None.

## Examination

None.
