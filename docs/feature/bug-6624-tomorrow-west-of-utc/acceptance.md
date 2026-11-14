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

##### Covers

P1

```text
In a browser on a New York clock late in the evening, when the UTC calendar is already tomorrow, a user opens Add Launch, types a name, ticks a Deliverable and enters first tomorrow's date and then today's date
```

```text
Tomorrow shows no date message and Save is open; today shows the message 'Launch date must be in the future' and Save stays blocked
```

```text
The user can save a launch dated tomorrow and still cannot save one dated today
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliveryCreateModal\.westOfUtc\.test\.tsx

```text
A fix that clears the rule but leaves the dialog gate or message wrong would pass the rule rows and fail here; a dialog that accepts today along with tomorrow fails here
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

### E1

unmet

#### Observations

```text
Probe frontend-tests deliveryFormRules: passing rows for the launch-date rule: no date, yesterday, today and tomorrow each judged by whether after today; yesterday, today and tomorrow each answered with the message that names its fix. No failing row in the output shown; output cut at 6000 bytes by my head filter.
```

```text
Probe frontend-tests westOfUtc: 1 file, 7 of 7 passed. Passing rows: clock-behind-UTC guard; tomorrow accepted and today and past refused in early morning and in late evening when UTC is already the next day; named launch dated tomorrow saved and today blocked; 2027-13-01 and 2027-00-10 refused as not on any calendar.
```

```text
Limit: no Delivery dialog in a browser was driven. Only Vitest rule-level rows under a faked west-of-UTC clock were observed. I did not capture the YYYY-MM-DD stimulus or its response myself, no time zone other than the faked clock, and no verbatim error text. Row names and pass marks are project test-suite output, not user-observed behavior.
```

```text
Non-probe: E1, E2 and E3 at dialog level (typed YYYY-MM-DD in the Delivery dialog, clock in America/New_York, tomorrow/today/yesterday) were not probed; no browser probe exists on the public surface. Only rule-level rows were observed.
```

observation_gap

### E2

unmet

#### Observations

```text
Probe frontend-tests deliveryFormRules: passing rows for the launch-date rule: no date, yesterday, today and tomorrow each judged by whether after today; yesterday, today and tomorrow each answered with the message that names its fix. No failing row in the output shown; output cut at 6000 bytes by my head filter.
```

```text
Probe frontend-tests westOfUtc: 1 file, 7 of 7 passed. Passing rows: clock-behind-UTC guard; tomorrow accepted and today and past refused in early morning and in late evening when UTC is already the next day; named launch dated tomorrow saved and today blocked; 2027-13-01 and 2027-00-10 refused as not on any calendar.
```

```text
Limit: no Delivery dialog in a browser was driven. Only Vitest rule-level rows under a faked west-of-UTC clock were observed. I did not capture the YYYY-MM-DD stimulus or its response myself, no time zone other than the faked clock, and no verbatim error text. Row names and pass marks are project test-suite output, not user-observed behavior.
```

```text
Non-probe: E1, E2 and E3 at dialog level (typed YYYY-MM-DD in the Delivery dialog, clock in America/New_York, tomorrow/today/yesterday) were not probed; no browser probe exists on the public surface. Only rule-level rows were observed.
```

observation_gap

### E3

unmet

#### Observations

```text
Probe frontend-tests deliveryFormRules: passing rows for the launch-date rule: no date, yesterday, today and tomorrow each judged by whether after today; yesterday, today and tomorrow each answered with the message that names its fix. No failing row in the output shown; output cut at 6000 bytes by my head filter.
```

```text
Probe frontend-tests westOfUtc: 1 file, 7 of 7 passed. Passing rows: clock-behind-UTC guard; tomorrow accepted and today and past refused in early morning and in late evening when UTC is already the next day; named launch dated tomorrow saved and today blocked; 2027-13-01 and 2027-00-10 refused as not on any calendar.
```

```text
Limit: no Delivery dialog in a browser was driven. Only Vitest rule-level rows under a faked west-of-UTC clock were observed. I did not capture the YYYY-MM-DD stimulus or its response myself, no time zone other than the faked clock, and no verbatim error text. Row names and pass marks are project test-suite output, not user-observed behavior.
```

```text
Non-probe: E1, E2 and E3 at dialog level (typed YYYY-MM-DD in the Delivery dialog, clock in America/New_York, tomorrow/today/yesterday) were not probed; no browser probe exists on the public surface. Only rule-level rows were observed.
```

observation_gap
