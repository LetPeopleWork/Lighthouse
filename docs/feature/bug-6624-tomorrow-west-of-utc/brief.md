# Product brief: bug\-6624\-tomorrow\-west\-of\-utc

## Request

```text
Bug #6624: in a browser whose time zone is west of UTC (for example anywhere in the Americas), the Delivery dialog refuses tomorrow as a launch date and says the date must be in the future. Only the day after tomorrow is accepted. Repro: set the browser or OS time zone to e.g. America/New_York, open a Portfolio, create a Delivery, pick tomorrow's date: Save stays blocked with the future-date message. Expected: tomorrow is accepted in every time zone; today stays refused.
```

## Work

S

```text
A team lead in any time zone can save a Delivery that launches tomorrow, and still cannot save one for today or the past.
```

### Rework

None.

## Promises

### P1

Outcome

```text
Tomorrow is accepted as a launch date in every time zone; today and the past stay refused
```

## Scope

None.

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
In a browser west of UTC, the Delivery dialog accepts tomorrow as a launch date and still refuses today and the past
```

#### Promises

P1

#### Depends on

None.

#### Slice work

None.
