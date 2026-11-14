# Acceptance brief: bug\-6624\-tomorrow\-west\-of\-utc

## Slices

### S1

##### Covers

P1

```text
With the browser clock in New York, early in the morning and late in the evening when the UTC date is already the next day, the launch-date rule and the Save gate (a named launch with a feature picked) are asked about today, tomorrow, yesterday and a month ago, written as the day strings the date field holds; two days that are not on any calendar (month 13 and month 00) are asked about too
```

```text
Tomorrow is accepted with no date error and Save is open; today, yesterday and a month ago are refused, and Save is blocked for today, with the message that the launch date must be in the future; a day with no such month is refused, never rolled into another year
```

```text
A user in the Americas can save a Delivery dated tomorrow; today and past dates stay blocked; a mistyped month cannot be saved
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFormRules\.westOfUtc\.test\.ts

```text
A fix that accepts every future-looking date including today, only works at one time of day, fixes the field rule but not the Save gate, or accepts a month that rolls over is caught by the today and far-past rows, the two clock moments, the Save gate row and the impossible-month rows; an east-of-UTC run is caught by the offset check
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
