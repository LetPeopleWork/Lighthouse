# Acceptance brief: story\-6393\-wide\-units\-b

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A user sees the same content and can do the same things on the metrics and delivery screens after the split as before it.
```

#### E2

##### Promises

P1

```text
The metrics data requests start in the same order as before the split.
```

#### E3

##### Promises

P1

```text
Existing tests for the split units pass without edits, except tests that moved with the code they cover. Not user-observable; the test run record is the only evidence, so candidate review owns it.
```

#### E4

##### Promises

P2

```text
A user can still use the named-percentiles scope and the state drill-down on the metrics view exactly as before. Where the logic lives is an implementation constraint, not user-observable; candidate review owns it.
```

#### E5

##### Promises

P3

```text
The three named files are each split to well under 1000 lines and the size rule list is empty. Not user-observable; an architectural constraint for candidate review, not a user expectation.
```

#### E6

##### Promises

P4

```text
A user gets the same Delivery form behaviour: invalid or missing dates, a missing or duplicate name, a blocking error, the offered tabs and the bound source behave as before. Testability without rendering the dialog is a developer constraint; candidate review owns it.
```

## Charter

None.

## Examination

None.
